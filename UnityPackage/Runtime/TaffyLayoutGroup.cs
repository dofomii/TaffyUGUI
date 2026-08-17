using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TaffyUGUI
{
    [AddComponentMenu("Layout/Taffy Layout Group")]
    [DisallowMultipleComponent]
    public sealed class TaffyLayoutGroup : LayoutGroup
    {
        [Header("Formatting Context")]
        public TaffyContainerDisplay containerDisplay = TaffyContainerDisplay.Flex;
        public TaffyBoxSizing boxSizing = TaffyBoxSizing.BorderBox;
        public TaffyWritingDirection writingDirection = TaffyWritingDirection.LeftToRight;
        public TaffyOverflow overflowX = TaffyOverflow.Visible;
        public TaffyOverflow overflowY = TaffyOverflow.Visible;
        [Min(0)] public float scrollbarWidth;
        public TaffyEdges border = default;
        public TaffyTextAlign textAlign = TaffyTextAlign.Auto;

        [Header("Flex Container")]
        public TaffyFlexDirection direction = TaffyFlexDirection.Row;
        public TaffyFlexWrap wrap = TaffyFlexWrap.NoWrap;
        [Min(0)] public float horizontalGap;
        [Min(0)] public float verticalGap;
        public TaffyJustify justifyContent = TaffyJustify.Start;
        public TaffyAlign alignItems = TaffyAlign.Stretch;
        public TaffyAlignContent alignContent = TaffyAlignContent.Auto;
        public TaffyAlign justifyItems = TaffyAlign.Auto;

        private enum NativePass
        {
            Minimum,
            Preferred,
            Arrange,
        }

        private sealed class NodeRecord
        {
            internal ulong handle;
            internal TaffyNative.Style style;
            internal bool hasStyle;

            internal readonly Dictionary<int, TaffyMeasurementData> measurementCache = new Dictionary<int, TaffyMeasurementData>();
            internal bool measurementResolved;
            internal bool measurementDirty = true;
            internal int measurementSignature;
            internal TaffyMeasurementData measurementData;
            internal bool nativeHasMeasurement;
            internal int nativeMeasurementSignature;
        }

        private ulong _context;
        private ulong _root;
        private TaffyNative.Style _rootStyle;
        private bool _hasRootStyle;
        private readonly Dictionary<RectTransform, NodeRecord> _nodes = new Dictionary<RectTransform, NodeRecord>();
        private readonly List<RectTransform> _orderedChildren = new List<RectTransform>();
        private readonly List<ulong> _orderedHandles = new List<ulong>();
        private readonly HashSet<RectTransform> _seenChildren = new HashSet<RectTransform>();
        private ulong[] _layoutHandles = Array.Empty<ulong>();
        private TaffyNative.Layout[] _layoutResults = Array.Empty<TaffyNative.Layout>();
        private bool _abiValidated;
        private bool _applying;
        private bool _arrangedLayoutValid;
        private Vector2 _minimumSize;
        private Vector2 _preferredSize;
        private float _arrangedWidth;
        private float _arrangedHeight;

        public void SetLayoutDirty()
        {
            InvalidateLayout();
            if (isActiveAndEnabled && rectTransform)
                LayoutRebuilder.MarkLayoutForRebuild(rectTransform);
        }

        public void InvalidateMeasurement(RectTransform child = null)
        {
            if (child)
            {
                if (_nodes.TryGetValue(child, out NodeRecord record))
                {
                    record.measurementDirty = true;
                    record.measurementCache.Clear();
                }
            }
            else
            {
                foreach (NodeRecord record in _nodes.Values)
                {
                    record.measurementDirty = true;
                    record.measurementCache.Clear();
                }
            }
            SetLayoutDirty();
        }
        protected override void OnEnable()
        {
            base.OnEnable();
            TaffyMeasurementInvalidationHub.Register(this);
            EnsureContext();
            SetLayoutDirty();
        }

        protected override void OnDisable()
        {
            TaffyMeasurementInvalidationHub.Unregister(this);
            DestroyContext();
            base.OnDisable();
        }
        protected override void OnDestroy()
        {
            TaffyMeasurementInvalidationHub.Unregister(this);
            DestroyContext();
            base.OnDestroy();
        }
#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            SetLayoutDirty();
        }
#endif

        protected override void OnDidApplyAnimationProperties()
        {
            base.OnDidApplyAnimationProperties();
            SetLayoutDirty();
        }

        protected override void OnTransformChildrenChanged()
        {
            base.OnTransformChildrenChanged();
            SetLayoutDirty();
        }

        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();
            InvalidateLayout();
        }

        public override void CalculateLayoutInputHorizontal()
        {
            base.CalculateLayoutInputHorizontal();
            MeasureLayoutInputs();
            SetLayoutInputForAxis(_minimumSize.x, _preferredSize.x, 0f, 0);
        }

        public override void CalculateLayoutInputVertical()
        {
            // Refresh after horizontal calculation so nested groups have published vertical inputs.
            MeasureLayoutInputs();
            SetLayoutInputForAxis(_minimumSize.y, _preferredSize.y, 0f, 1);
        }

        public override void SetLayoutHorizontal()
        {
            ApplyLayout(0);
        }

        public override void SetLayoutVertical()
        {
            ApplyLayout(1);
        }

        private void InvalidateLayout()
        {
            _arrangedLayoutValid = false;
        }

        private void EnsureContext()
        {
            if (_context != 0)
                return;

            if (!_abiValidated)
            {
                TaffyNative.ValidateAbi();
                _abiValidated = true;
            }

            TaffyNative.Check(TaffyNative.tu_context_create(out _context), "create context");
            if (_context == 0)
                throw new InvalidOperationException("TaffyUGUI native context creation returned a null handle.");

            var initialStyle = BuildRootStyle(NativePass.Preferred);
            TaffyNative.Check(TaffyNative.tu_node_create(_context, ref initialStyle, out _root), "create root");
            if (_root == 0)
            {
                DestroyContext();
                throw new InvalidOperationException("TaffyUGUI native root creation returned a null handle.");
            }

            _rootStyle = initialStyle;
            _hasRootStyle = true;
            _arrangedLayoutValid = false;
        }

        private void DestroyContext()
        {
            ulong context = _context;
            _context = 0;
            _root = 0;
            _nodes.Clear();
            _orderedChildren.Clear();
            _orderedHandles.Clear();
            _seenChildren.Clear();
            _hasRootStyle = false;
            _arrangedLayoutValid = false;

            if (context == 0)
                return;

            try
            {
                int status = TaffyNative.tu_context_destroy(context);
                if (status != (int)TaffyNative.Status.Ok)
                    Debug.LogWarning($"TaffyUGUI failed to destroy native context cleanly (status {status}).", this);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"TaffyUGUI native context teardown was skipped: {exception.Message}", this);
            }
        }

        private void MeasureLayoutInputs()
        {
            EnsureContext();

            TaffyNative.Layout minimum = ComputeRootLayout(NativePass.Minimum, float.PositiveInfinity, float.PositiveInfinity);
            TaffyNative.Layout preferred = ComputeRootLayout(NativePass.Preferred, float.PositiveInfinity, float.PositiveInfinity);

            _minimumSize = new Vector2(
                Mathf.Max(0f, minimum.width),
                Mathf.Max(0f, minimum.height));
            _preferredSize = new Vector2(
                Mathf.Max(_minimumSize.x, preferred.width),
                Mathf.Max(_minimumSize.y, preferred.height));
            _arrangedLayoutValid = false;
        }

        private TaffyNative.Layout ComputeRootLayout(NativePass pass, float width, float height)
        {
            SynchronizeNativeTree(pass, width);
            // Managed measurement providers have completed and their records are uploaded before this call.
            TaffyNative.Check(TaffyNative.tu_compute_layout(_context, _root, width, height), "compute root layout");
            TaffyNative.Check(TaffyNative.tu_get_layout(_context, _root, out var layout), "get root layout");
            return layout;
        }

        private void ApplyLayout(int axis)
        {
            if (_applying || !isActiveAndEnabled)
                return;

            _applying = true;
            try
            {
                EnsureContext();
                float width = Mathf.Max(0f, rectTransform.rect.width);
                float height = Mathf.Max(0f, rectTransform.rect.height);
                SynchronizeNativeTree(NativePass.Arrange, width);

                if (!_arrangedLayoutValid || !SameFloat(_arrangedWidth, width) || !SameFloat(_arrangedHeight, height))
                {
                    // No managed callbacks are reachable from the ABI compute function.
                    TaffyNative.Check(TaffyNative.tu_compute_layout(_context, _root, width, height), "compute arranged layout");
                    ReadChildLayouts();
                    _arrangedWidth = width;
                    _arrangedHeight = height;
                    _arrangedLayoutValid = true;
                }

                int count = Mathf.Min(_orderedChildren.Count, _layoutResults.Length);
                for (int i = 0; i < count; i++)
                {
                    RectTransform child = _orderedChildren[i];
                    TaffyNative.Layout layout = _layoutResults[i];
                    if (axis == 0)
                        SetChildAlongAxis(child, 0, layout.x, Mathf.Max(0f, layout.width));
                    else
                        SetChildAlongAxis(child, 1, layout.y, Mathf.Max(0f, layout.height));
                }
            }
            finally
            {
                _applying = false;
            }
        }

        private void SynchronizeNativeTree(NativePass pass, float availableWidth)
        {
            EnsureContext();

            var rootStyle = BuildRootStyle(pass);
            if (!_hasRootStyle || !StyleEquals(_rootStyle, rootStyle))
            {
                TaffyNative.Check(TaffyNative.tu_node_set_style(_context, _root, ref rootStyle), "update root style");
                _rootStyle = rootStyle;
                _hasRootStyle = true;
                _arrangedLayoutValid = false;
            }

            _seenChildren.Clear();
            for (int i = 0; i < rectChildren.Count; i++)
                _seenChildren.Add(rectChildren[i]);

            var removed = new List<RectTransform>();
            foreach (KeyValuePair<RectTransform, NodeRecord> pair in _nodes)
            {
                if (!pair.Key || !_seenChildren.Contains(pair.Key))
                    removed.Add(pair.Key);
            }

            for (int i = 0; i < removed.Count; i++)
            {
                RectTransform child = removed[i];
                NodeRecord record = _nodes[child];
                TaffyNative.Check(TaffyNative.tu_node_remove(_context, record.handle), "remove child node");
                _nodes.Remove(child);
                _arrangedLayoutValid = false;
            }

            bool topologyChanged = _orderedChildren.Count != rectChildren.Count;
            _orderedChildren.Clear();
            _orderedHandles.Clear();

            for (int i = 0; i < rectChildren.Count; i++)
            {
                RectTransform child = rectChildren[i];
                if (!_nodes.TryGetValue(child, out NodeRecord record))
                    record = new NodeRecord();

                TaffyLayoutItem item = child.GetComponent<TaffyLayoutItem>();
                bool hasMeasurement = ResolveMeasurement(child, item, record, availableWidth);
                TaffyMeasurementData measurement = record.measurementData;
                var style = BuildChildStyle(child, item, pass, hasMeasurement, measurement);

                if (record.handle == 0)
                {
                    TaffyNative.Check(TaffyNative.tu_node_create(_context, ref style, out ulong handle), "create child node");
                    if (handle == 0)
                        throw new InvalidOperationException("TaffyUGUI native child creation returned a null handle.");
                    record.handle = handle;
                    record.style = style;
                    record.hasStyle = true;
                    _nodes.Add(child, record);
                    topologyChanged = true;
                    _arrangedLayoutValid = false;
                }
                else if (!record.hasStyle || !StyleEquals(record.style, style))
                {
                    TaffyNative.Check(TaffyNative.tu_node_set_style(_context, record.handle, ref style), "update child style");
                    record.style = style;
                    record.hasStyle = true;
                    _arrangedLayoutValid = false;
                }

                SynchronizeNativeMeasurement(record);
                _orderedChildren.Add(child);
                _orderedHandles.Add(record.handle);
            }

            if (!topologyChanged)
            {
                if (_layoutHandles.Length < _orderedHandles.Count)
                    topologyChanged = true;
                else
                {
                    for (int i = 0; i < _orderedHandles.Count; i++)
                    {
                        if (_layoutHandles[i] != _orderedHandles[i])
                        {
                            topologyChanged = true;
                            break;
                        }
                    }
                }
            }

            if (topologyChanged)
            {
                EnsureLayoutBuffers(_orderedHandles.Count);
                for (int i = 0; i < _orderedHandles.Count; i++)
                    _layoutHandles[i] = _orderedHandles[i];
                TaffyNative.Check(
                    TaffyNative.tu_node_set_children(_context, _root, _layoutHandles, (uint)_orderedHandles.Count),
                    "synchronize child topology");
                _arrangedLayoutValid = false;
            }
        }

        private bool ResolveMeasurement(
            RectTransform child,
            TaffyLayoutItem item,
            NodeRecord record,
            float availableWidth)
        {
            bool enabled = !item || item.MeasurementEnabled;
            if (!enabled || !TaffyMeasurementResolver.TryGetSignature(child, availableWidth, out int signature))
            {
                record.measurementResolved = false;
                record.measurementDirty = false;
                record.measurementCache.Clear();
                return false;
            }

            if (record.measurementDirty)
            {
                record.measurementCache.Clear();
                record.measurementDirty = false;
            }

            if (record.measurementCache.TryGetValue(signature, out TaffyMeasurementData cached))
            {
                record.measurementData = cached;
                record.measurementSignature = signature;
                record.measurementResolved = true;
                return true;
            }

            if (!TaffyMeasurementResolver.TryResolve(child, availableWidth, out TaffyMeasurementData measurement, out signature))
            {
                record.measurementResolved = false;
                return false;
            }

            record.measurementData = measurement;
            record.measurementSignature = signature;
            record.measurementResolved = true;
            record.measurementCache[signature] = measurement;
            return true;
        }

        private void SynchronizeNativeMeasurement(NodeRecord record)
        {
            if (record.measurementResolved)
            {
                if (!record.nativeHasMeasurement || record.nativeMeasurementSignature != record.measurementSignature)
                {
                    TaffyMeasurementResolver.Upload(_context, record.handle, record.measurementData);
                    record.nativeHasMeasurement = true;
                    record.nativeMeasurementSignature = record.measurementSignature;
                    _arrangedLayoutValid = false;
                }
                return;
            }

            if (!record.nativeHasMeasurement)
                return;

            TaffyNative.Check(TaffyNative.tu_node_clear_measurement(_context, record.handle, IntPtr.Zero), "clear cached measurement");
            record.nativeHasMeasurement = false;
            record.nativeMeasurementSignature = 0;
            _arrangedLayoutValid = false;
        }

        private void ReadChildLayouts()
        {
            int count = _orderedHandles.Count;
            EnsureLayoutBuffers(count);
            if (count == 0)
                return;

            for (int i = 0; i < count; i++)
                _layoutHandles[i] = _orderedHandles[i];

            TaffyNative.Check(
                TaffyNative.tu_get_layouts_bulk(
                    _context,
                    _layoutHandles,
                    (uint)count,
                    _layoutResults,
                    (uint)_layoutResults.Length,
                    out uint written),
                "get child layouts");

            if (written != (uint)count)
                throw new InvalidOperationException($"TaffyUGUI expected {count} child layouts, native returned {written}.");
        }

        private void EnsureLayoutBuffers(int count)
        {
            if (_layoutHandles.Length < count)
                _layoutHandles = new ulong[count];
            if (_layoutResults.Length < count)
                _layoutResults = new TaffyNative.Layout[count];
        }

        private TaffyNative.Style BuildRootStyle(NativePass pass)
        {
            var style = TaffyNative.Style.Defaults((TaffyNative.Display)(int)containerDisplay);
            style.display = (int)containerDisplay;
            style.boxSizing = (int)boxSizing;
            style.direction = (int)writingDirection;
            style.overflowX = (int)overflowX;
            style.overflowY = (int)overflowY;
            style.scrollbarWidth = Mathf.Max(0f, scrollbarWidth);
            style.flexDirection = (int)direction;
            style.flexWrap = (int)wrap;
            style.gapX = TaffyNative.Value.Points(Mathf.Max(0f, horizontalGap));
            style.gapY = TaffyNative.Value.Points(Mathf.Max(0f, verticalGap));
            style.paddingLeft = TaffyNative.Value.Points(Mathf.Max(0f, padding.left));
            style.paddingRight = TaffyNative.Value.Points(Mathf.Max(0f, padding.right));
            style.paddingTop = TaffyNative.Value.Points(Mathf.Max(0f, padding.top));
            style.paddingBottom = TaffyNative.Value.Points(Mathf.Max(0f, padding.bottom));
            style.borderLeft = border.left.ToNonNegativeLengthPercentage();
            style.borderRight = border.right.ToNonNegativeLengthPercentage();
            style.borderTop = border.top.ToNonNegativeLengthPercentage();
            style.borderBottom = border.bottom.ToNonNegativeLengthPercentage();
            style.alignItems = (int)alignItems;
            style.alignContent = (int)alignContent;
            style.justifyContent = ToNativeJustify(justifyContent);
            style.justifyItems = (int)justifyItems;
            style.textAlign = (int)textAlign;

            if (pass == NativePass.Arrange)
            {
                style.width = TaffyNative.Value.Points(Mathf.Max(0f, rectTransform.rect.width));
                style.height = TaffyNative.Value.Points(Mathf.Max(0f, rectTransform.rect.height));
            }

            return style;
        }

        private TaffyNative.Style BuildChildStyle(
            RectTransform child,
            TaffyLayoutItem item,
            NativePass pass,
            bool hasMeasurement,
            TaffyMeasurementData measurement)
        {
            var style = TaffyNative.Style.FlexDefaults();
            LayoutElement element = child.GetComponent<LayoutElement>();
            bool explicitPreferredWidth = element && !element.ignoreLayout && element.preferredWidth >= 0f;
            bool explicitPreferredHeight = element && !element.ignoreLayout && element.preferredHeight >= 0f;

            float layoutMinWidth = Mathf.Max(0f, LayoutUtility.GetMinWidth(child));
            float layoutMinHeight = Mathf.Max(0f, LayoutUtility.GetMinHeight(child));
            float layoutPreferredWidth = Mathf.Max(layoutMinWidth, LayoutUtility.GetPreferredWidth(child));
            float layoutPreferredHeight = Mathf.Max(layoutMinHeight, LayoutUtility.GetPreferredHeight(child));

            float minimumWidth = hasMeasurement ? Mathf.Max(layoutMinWidth, measurement.minContent.x) : layoutMinWidth;
            float minimumHeight = hasMeasurement ? Mathf.Max(layoutMinHeight, measurement.minContent.y) : layoutMinHeight;
            float preferredWidth = hasMeasurement && !explicitPreferredWidth
                ? Mathf.Max(minimumWidth, measurement.preferred.x)
                : layoutPreferredWidth;
            float preferredHeight = hasMeasurement && !explicitPreferredHeight
                ? Mathf.Max(minimumHeight, measurement.preferred.y)
                : layoutPreferredHeight;

            // LayoutElement minima remain hard constraints during arrangement. Intrinsic min-content
            // participates in our own minimum-size report without preventing normal Flex shrink/wrap.
            style.minWidth = TaffyNative.Value.Points(layoutMinWidth);
            style.minHeight = TaffyNative.Value.Points(layoutMinHeight);

            if (pass == NativePass.Minimum)
            {
                style.width = TaffyNative.Value.Points(minimumWidth);
                style.height = TaffyNative.Value.Points(minimumHeight);
                style.flexGrow = 0f;
                style.flexShrink = 0f;
            }
            else if (pass == NativePass.Preferred)
            {
                style.width = preferredWidth > 0f ? TaffyNative.Value.Points(preferredWidth) : TaffyNative.Value.Auto;
                style.height = preferredHeight > 0f ? TaffyNative.Value.Points(preferredHeight) : TaffyNative.Value.Auto;
                style.flexGrow = MainAxisFlexible(child);
                style.flexShrink = 1f;
            }
            else
            {
                style.width = hasMeasurement && !explicitPreferredWidth
                    ? TaffyNative.Value.Auto
                    : preferredWidth > 0f ? TaffyNative.Value.Points(preferredWidth) : TaffyNative.Value.Auto;
                style.height = hasMeasurement && !explicitPreferredHeight
                    ? TaffyNative.Value.Auto
                    : preferredHeight > 0f ? TaffyNative.Value.Points(preferredHeight) : TaffyNative.Value.Auto;
                style.flexGrow = MainAxisFlexible(child);
                style.flexShrink = 1f;
            }

            if (item)
                style = item.ApplyTo(style, hasMeasurement && measurement.isReplaced);
            else if (hasMeasurement && measurement.isReplaced)
                style.itemIsReplaced = 1;

            return style;
        }

        private float MainAxisFlexible(RectTransform child)
        {
            switch (direction)
            {
                case TaffyFlexDirection.Column:
                case TaffyFlexDirection.ColumnReverse:
                    return Mathf.Max(0f, LayoutUtility.GetFlexibleHeight(child));
                default:
                    return Mathf.Max(0f, LayoutUtility.GetFlexibleWidth(child));
            }
        }

        private static int ToNativeJustify(TaffyJustify value)
        {
            switch (value)
            {
                case TaffyJustify.Auto: return (int)TaffyNative.AlignContent.Unset;
                case TaffyJustify.Start: return (int)TaffyNative.AlignContent.Start;
                case TaffyJustify.End: return (int)TaffyNative.AlignContent.End;
                case TaffyJustify.Center: return (int)TaffyNative.AlignContent.Center;
                case TaffyJustify.SpaceBetween: return (int)TaffyNative.AlignContent.SpaceBetween;
                case TaffyJustify.SpaceAround: return (int)TaffyNative.AlignContent.SpaceAround;
                case TaffyJustify.SpaceEvenly: return (int)TaffyNative.AlignContent.SpaceEvenly;
                case TaffyJustify.FlexStart: return (int)TaffyNative.AlignContent.FlexStart;
                case TaffyJustify.FlexEnd: return (int)TaffyNative.AlignContent.FlexEnd;
                case TaffyJustify.SafeStart: return (int)TaffyNative.AlignContent.SafeStart;
                case TaffyJustify.SafeEnd: return (int)TaffyNative.AlignContent.SafeEnd;
                case TaffyJustify.SafeCenter: return (int)TaffyNative.AlignContent.SafeCenter;
                case TaffyJustify.SafeFlexStart: return (int)TaffyNative.AlignContent.SafeFlexStart;
                case TaffyJustify.SafeFlexEnd: return (int)TaffyNative.AlignContent.SafeFlexEnd;
                default: return (int)TaffyNative.AlignContent.Start;
            }
        }

        private static bool StyleEquals(TaffyNative.Style a, TaffyNative.Style b)
        {
            return a.display == b.display &&
                   a.boxSizing == b.boxSizing &&
                   a.direction == b.direction &&
                   a.overflowX == b.overflowX &&
                   a.overflowY == b.overflowY &&
                   SameFloat(a.scrollbarWidth, b.scrollbarWidth) &&
                   a.position == b.position &&
                   SameValue(a.insetLeft, b.insetLeft) &&
                   SameValue(a.insetRight, b.insetRight) &&
                   SameValue(a.insetTop, b.insetTop) &&
                   SameValue(a.insetBottom, b.insetBottom) &&
                   SameValue(a.width, b.width) &&
                   SameValue(a.height, b.height) &&
                   SameValue(a.minWidth, b.minWidth) &&
                   SameValue(a.minHeight, b.minHeight) &&
                   SameValue(a.maxWidth, b.maxWidth) &&
                   SameValue(a.maxHeight, b.maxHeight) &&
                   SameFloat(a.aspectRatio, b.aspectRatio) &&
                   SameValue(a.marginLeft, b.marginLeft) &&
                   SameValue(a.marginRight, b.marginRight) &&
                   SameValue(a.marginTop, b.marginTop) &&
                   SameValue(a.marginBottom, b.marginBottom) &&
                   SameValue(a.paddingLeft, b.paddingLeft) &&
                   SameValue(a.paddingRight, b.paddingRight) &&
                   SameValue(a.paddingTop, b.paddingTop) &&
                   SameValue(a.paddingBottom, b.paddingBottom) &&
                   SameValue(a.borderLeft, b.borderLeft) &&
                   SameValue(a.borderRight, b.borderRight) &&
                   SameValue(a.borderTop, b.borderTop) &&
                   SameValue(a.borderBottom, b.borderBottom) &&
                   a.flexDirection == b.flexDirection &&
                   a.flexWrap == b.flexWrap &&
                   SameValue(a.flexBasis, b.flexBasis) &&
                   SameFloat(a.flexGrow, b.flexGrow) &&
                   SameFloat(a.flexShrink, b.flexShrink) &&
                   a.alignItems == b.alignItems &&
                   a.alignSelf == b.alignSelf &&
                   a.alignContent == b.alignContent &&
                   a.justifyContent == b.justifyContent &&
                   a.justifyItems == b.justifyItems &&
                   a.justifySelf == b.justifySelf &&
                   SameValue(a.gapX, b.gapX) &&
                   SameValue(a.gapY, b.gapY) &&
                   a.itemIsTable == b.itemIsTable &&
                   a.itemIsReplaced == b.itemIsReplaced &&
                   a.floatMode == b.floatMode &&
                   a.clearMode == b.clearMode &&
                   a.textAlign == b.textAlign;
        }

        private static bool SameValue(TaffyNative.Value a, TaffyNative.Value b)
        {
            return a.kind == b.kind && SameFloat(a.value, b.value) && a.resource == b.resource;
        }

        private static bool SameFloat(float a, float b)
        {
            return Mathf.Approximately(a, b);
        }
    }
}
