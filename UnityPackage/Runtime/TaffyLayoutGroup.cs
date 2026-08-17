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
        public TaffyFlexDirection direction = TaffyFlexDirection.Row;
        public TaffyFlexWrap wrap = TaffyFlexWrap.NoWrap;
        [Min(0)] public float horizontalGap;
        [Min(0)] public float verticalGap;
        public TaffyJustify justifyContent = TaffyJustify.Start;
        public TaffyAlign alignItems = TaffyAlign.Stretch;

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
        private bool _measurementsValid;
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

        protected override void OnEnable()
        {
            base.OnEnable();
            EnsureContext();
            SetLayoutDirty();
        }

        protected override void OnDisable()
        {
            DestroyContext();
            base.OnDisable();
        }

        protected override void OnDestroy()
        {
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
            // Refresh after the horizontal phase so nested groups have had a chance
            // to publish their own vertical layout inputs before we consume them.
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
            _measurementsValid = false;
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
            _measurementsValid = false;
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
            _measurementsValid = false;
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
            _measurementsValid = true;
            _arrangedLayoutValid = false;
        }

        private TaffyNative.Layout ComputeRootLayout(NativePass pass, float width, float height)
        {
            SynchronizeNativeTree(pass);
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
                SynchronizeNativeTree(NativePass.Arrange);

                float width = Mathf.Max(0f, rectTransform.rect.width);
                float height = Mathf.Max(0f, rectTransform.rect.height);
                if (!_arrangedLayoutValid || !SameFloat(_arrangedWidth, width) || !SameFloat(_arrangedHeight, height))
                {
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

        private void SynchronizeNativeTree(NativePass pass)
        {
            EnsureContext();

            var rootStyle = BuildRootStyle(pass);
            if (!_hasRootStyle || !RootStyleEquals(_rootStyle, rootStyle))
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
                var style = BuildChildStyle(child, pass);

                if (!_nodes.TryGetValue(child, out NodeRecord record))
                {
                    TaffyNative.Check(TaffyNative.tu_node_create(_context, ref style, out ulong handle), "create child node");
                    if (handle == 0)
                        throw new InvalidOperationException("TaffyUGUI native child creation returned a null handle.");
                    record = new NodeRecord { handle = handle, style = style, hasStyle = true };
                    _nodes.Add(child, record);
                    topologyChanged = true;
                    _arrangedLayoutValid = false;
                }
                else if (!record.hasStyle || !ChildStyleEquals(record.style, style))
                {
                    TaffyNative.Check(TaffyNative.tu_node_set_style(_context, record.handle, ref style), "update child style");
                    record.style = style;
                    record.hasStyle = true;
                    _arrangedLayoutValid = false;
                }

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
            var style = TaffyNative.Style.FlexDefaults();
            style.display = (int)TaffyNative.Display.Flex;
            style.flexDirection = (int)direction;
            style.flexWrap = (int)wrap;
            style.gapX = TaffyNative.Value.Points(Mathf.Max(0f, horizontalGap));
            style.gapY = TaffyNative.Value.Points(Mathf.Max(0f, verticalGap));
            style.paddingLeft = TaffyNative.Value.Points(Mathf.Max(0f, padding.left));
            style.paddingRight = TaffyNative.Value.Points(Mathf.Max(0f, padding.right));
            style.paddingTop = TaffyNative.Value.Points(Mathf.Max(0f, padding.top));
            style.paddingBottom = TaffyNative.Value.Points(Mathf.Max(0f, padding.bottom));
            style.alignItems = (int)alignItems;
            style.justifyContent = ToNativeJustify(justifyContent);

            if (pass == NativePass.Arrange)
            {
                style.width = TaffyNative.Value.Points(Mathf.Max(0f, rectTransform.rect.width));
                style.height = TaffyNative.Value.Points(Mathf.Max(0f, rectTransform.rect.height));
            }

            return style;
        }

        private TaffyNative.Style BuildChildStyle(RectTransform child, NativePass pass)
        {
            var style = TaffyNative.Style.FlexDefaults();

            float minWidth = Mathf.Max(0f, LayoutUtility.GetMinWidth(child));
            float minHeight = Mathf.Max(0f, LayoutUtility.GetMinHeight(child));
            float preferredWidth = Mathf.Max(minWidth, LayoutUtility.GetPreferredWidth(child));
            float preferredHeight = Mathf.Max(minHeight, LayoutUtility.GetPreferredHeight(child));


            style.minWidth = TaffyNative.Value.Points(minWidth);
            style.minHeight = TaffyNative.Value.Points(minHeight);

            if (pass == NativePass.Minimum)
            {
                style.width = TaffyNative.Value.Points(minWidth);
                style.height = TaffyNative.Value.Points(minHeight);
                style.flexGrow = 0f;
                style.flexShrink = 0f;
            }
            else
            {
                style.width = TaffyNative.Value.Points(preferredWidth);
                style.height = TaffyNative.Value.Points(preferredHeight);
                style.flexGrow = MainAxisFlexible(child);
                style.flexShrink = 1f;
            }

            TaffyLayoutItem item = child.GetComponent<TaffyLayoutItem>();
            if (item)
                style = item.ApplyTo(style);

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
                default: return (int)TaffyNative.AlignContent.Start;
            }
        }

        private static bool RootStyleEquals(TaffyNative.Style a, TaffyNative.Style b)
        {
            return a.display == b.display &&
                   a.flexDirection == b.flexDirection &&
                   a.flexWrap == b.flexWrap &&
                   a.alignItems == b.alignItems &&
                   a.justifyContent == b.justifyContent &&
                   SameValue(a.gapX, b.gapX) &&
                   SameValue(a.gapY, b.gapY) &&
                   SameValue(a.paddingLeft, b.paddingLeft) &&
                   SameValue(a.paddingRight, b.paddingRight) &&
                   SameValue(a.paddingTop, b.paddingTop) &&
                   SameValue(a.paddingBottom, b.paddingBottom) &&
                   SameValue(a.width, b.width) &&
                   SameValue(a.height, b.height);
        }

        private static bool ChildStyleEquals(TaffyNative.Style a, TaffyNative.Style b)
        {
            return a.display == b.display &&
                   SameValue(a.width, b.width) &&
                   SameValue(a.height, b.height) &&
                   SameValue(a.minWidth, b.minWidth) &&
                   SameValue(a.minHeight, b.minHeight) &&
                   SameValue(a.maxWidth, b.maxWidth) &&
                   SameValue(a.maxHeight, b.maxHeight) &&
                   SameValue(a.flexBasis, b.flexBasis) &&
                   SameFloat(a.flexGrow, b.flexGrow) &&
                   SameFloat(a.flexShrink, b.flexShrink) &&
                   a.alignSelf == b.alignSelf &&
                   SameFloat(a.aspectRatio, b.aspectRatio);
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
