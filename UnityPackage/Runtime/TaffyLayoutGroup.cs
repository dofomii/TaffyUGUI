using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TaffyUGUI
{
    [AddComponentMenu("Layout/Taffy Layout Group")]
    public sealed class TaffyLayoutGroup : LayoutGroup
    {
        public TaffyFlexDirection direction = TaffyFlexDirection.Row;
        public TaffyFlexWrap wrap = TaffyFlexWrap.NoWrap;
        [Min(0)] public float horizontalGap = 0;
        [Min(0)] public float verticalGap = 0;
        public TaffyJustify justifyContent = TaffyJustify.Start;
        public TaffyAlign alignItems = TaffyAlign.Stretch;

        private IntPtr _context;
        private ulong _root;
        private readonly List<ulong> _nodes = new List<ulong>();
        private bool _dirty = true;

        public void SetLayoutDirty()
        {
            _dirty = true;
            if (isActiveAndEnabled) LayoutRebuilder.MarkLayoutForRebuild(rectTransform);
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

        protected override void OnValidate()
        {
            base.OnValidate();
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
            SetLayoutDirty();
        }

        public override void CalculateLayoutInputHorizontal()
        {
            base.CalculateLayoutInputHorizontal();
            RebuildNativeTree();
        }

        public override void CalculateLayoutInputVertical() { }
        public override void SetLayoutHorizontal() => ApplyLayout();
        public override void SetLayoutVertical() => ApplyLayout();

        private void EnsureContext()
        {
            if (_context != IntPtr.Zero) return;
            if (TaffyNative.taffy_ugui_api_version() != 0)
                throw new InvalidOperationException("TaffyUGUI bootstrap native API version mismatch.");
            _context = TaffyNative.taffy_ugui_create_context();
            if (_context == IntPtr.Zero) throw new InvalidOperationException("Unable to create TaffyUGUI native context.");
            _dirty = true;
        }

        private void DestroyContext()
        {
            if (_context == IntPtr.Zero) return;
            TaffyNative.taffy_ugui_destroy_context(_context);
            _context = IntPtr.Zero;
            _root = 0;
            _nodes.Clear();
        }

        private TaffyNative.Style BaseStyle()
        {
            var auto = TaffyNative.Dimension.Auto;
            return new TaffyNative.Style
            {
                width = auto, height = auto, minWidth = auto, minHeight = auto,
                maxWidth = auto, maxHeight = auto, flexBasis = auto,
                flexShrink = 1, alignItems = -1, alignSelf = -1, justifyContent = -1
            };
        }

        private void RebuildNativeTree()
        {
            EnsureContext();
            if (!_dirty) return;

            DestroyContext();
            EnsureContext();

            var rootStyle = BaseStyle();
            rootStyle.flexDirection = (int)direction;
            rootStyle.flexWrap = (int)wrap;
            rootStyle.gapX = horizontalGap;
            rootStyle.gapY = verticalGap;
            rootStyle.paddingLeft = padding.left;
            rootStyle.paddingRight = padding.right;
            rootStyle.paddingTop = padding.top;
            rootStyle.paddingBottom = padding.bottom;
            rootStyle.alignItems = (int)alignItems;
            rootStyle.justifyContent = (int)justifyContent;
            rootStyle.width = TaffyNative.Dimension.Points(rectTransform.rect.width);
            rootStyle.height = TaffyNative.Dimension.Points(rectTransform.rect.height);
            TaffyNative.Check(TaffyNative.taffy_ugui_create_node(_context, rootStyle, out _root), "create root");

            _nodes.Clear();
            var children = new List<ulong>(rectChildren.Count);
            foreach (var child in rectChildren)
            {
                var style = BaseStyle();
                var element = child.GetComponent<LayoutElement>();
                if (element && !element.ignoreLayout)
                {
                    if (element.minWidth >= 0) style.minWidth = TaffyNative.Dimension.Points(element.minWidth);
                    if (element.minHeight >= 0) style.minHeight = TaffyNative.Dimension.Points(element.minHeight);
                    if (element.preferredWidth >= 0) style.width = TaffyNative.Dimension.Points(element.preferredWidth);
                    if (element.preferredHeight >= 0) style.height = TaffyNative.Dimension.Points(element.preferredHeight);
                    if (element.flexibleWidth >= 0) style.flexGrow = element.flexibleWidth;
                }
                else
                {
                    float preferredWidth = LayoutUtility.GetPreferredWidth(child);
                    float preferredHeight = LayoutUtility.GetPreferredHeight(child);
                    if (preferredWidth > 0) style.width = TaffyNative.Dimension.Points(preferredWidth);
                    if (preferredHeight > 0) style.height = TaffyNative.Dimension.Points(preferredHeight);
                }

                var item = child.GetComponent<TaffyLayoutItem>();
                if (item) style = item.ApplyTo(style);

                TaffyNative.Check(TaffyNative.taffy_ugui_create_node(_context, style, out var node), "create child");
                _nodes.Add(node);
                children.Add(node);
            }

            TaffyNative.Check(TaffyNative.taffy_ugui_set_children(_context, _root, children.ToArray(), (UIntPtr)children.Count), "set children");
            _dirty = false;
        }

        private void ApplyLayout()
        {
            RebuildNativeTree();
            if (_context == IntPtr.Zero || _root == 0) return;

            TaffyNative.Check(TaffyNative.taffy_ugui_compute_layout(_context, _root, rectTransform.rect.width, rectTransform.rect.height), "compute layout");
            for (int i = 0; i < rectChildren.Count && i < _nodes.Count; i++)
            {
                TaffyNative.Check(TaffyNative.taffy_ugui_get_layout(_context, _nodes[i], out var layout), "get layout");
                SetChildAlongAxis(rectChildren[i], 0, layout.x, layout.width);
                SetChildAlongAxis(rectChildren[i], 1, layout.y, layout.height);
            }
        }
    }
}
