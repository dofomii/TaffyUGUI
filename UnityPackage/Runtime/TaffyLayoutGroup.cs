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
        [Min(0)] public float horizontalGap;
        [Min(0)] public float verticalGap;
        public TaffyJustify justifyContent = TaffyJustify.Start;
        public TaffyAlign alignItems = TaffyAlign.Stretch;

        private ulong _context;
        private ulong _root;
        private readonly List<ulong> _nodes = new List<ulong>();
        private bool _dirty = true;
        private bool _abiValidated;
        private bool _applying;

        public void SetLayoutDirty()
        {
            _dirty = true;
            if (isActiveAndEnabled && rectTransform) LayoutRebuilder.MarkLayoutForRebuild(rectTransform);
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

        protected override void OnValidate() { base.OnValidate(); SetLayoutDirty(); }
        protected override void OnTransformChildrenChanged() { base.OnTransformChildrenChanged(); SetLayoutDirty(); }
        protected override void OnRectTransformDimensionsChange() { base.OnRectTransformDimensionsChange(); SetLayoutDirty(); }

        public override void CalculateLayoutInputHorizontal()
        {
            base.CalculateLayoutInputHorizontal();
            RebuildNativeTree();
        }

        public override void CalculateLayoutInputVertical() { }
        public override void SetLayoutHorizontal() => ApplyLayout(horizontal: true, vertical: false);
        public override void SetLayoutVertical() => ApplyLayout(horizontal: false, vertical: true);

        private void EnsureContext()
        {
            if (_context != 0) return;
            if (!_abiValidated)
            {
                TaffyNative.ValidateAbi();
                _abiValidated = true;
            }
            TaffyNative.Check(TaffyNative.tu_context_create(out _context), "create context");
            if (_context == 0) throw new InvalidOperationException("TaffyUGUI native context creation returned a null handle.");
            _dirty = true;
        }

        private void DestroyContext()
        {
            if (_context == 0) return;
            TaffyNative.tu_context_destroy(_context);
            _context = 0;
            _root = 0;
            _nodes.Clear();
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

        private static TaffyNative.Style BaseStyle()
        {
            return TaffyNative.Style.FlexDefaults();
        }

        private void RebuildNativeTree()
        {
            EnsureContext();
            if (!_dirty) return;

            TaffyNative.Check(TaffyNative.tu_context_clear(_context), "clear context");
            _root = 0;
            _nodes.Clear();

            var rootStyle = BaseStyle();
            rootStyle.display = (int)TaffyNative.Display.Flex;
            rootStyle.flexDirection = (int)direction;
            rootStyle.flexWrap = (int)wrap;
            rootStyle.gapX = TaffyNative.Value.Points(horizontalGap);
            rootStyle.gapY = TaffyNative.Value.Points(verticalGap);
            rootStyle.paddingLeft = TaffyNative.Value.Points(padding.left);
            rootStyle.paddingRight = TaffyNative.Value.Points(padding.right);
            rootStyle.paddingTop = TaffyNative.Value.Points(padding.top);
            rootStyle.paddingBottom = TaffyNative.Value.Points(padding.bottom);
            rootStyle.alignItems = (int)alignItems;
            rootStyle.justifyContent = ToNativeJustify(justifyContent);
            rootStyle.width = TaffyNative.Value.Points(Mathf.Max(0f, rectTransform.rect.width));
            rootStyle.height = TaffyNative.Value.Points(Mathf.Max(0f, rectTransform.rect.height));
            TaffyNative.Check(TaffyNative.tu_node_create(_context, ref rootStyle, out _root), "create root");

            var children = new ulong[rectChildren.Count];
            for (int i = 0; i < rectChildren.Count; i++)
            {
                var child = rectChildren[i];
                var style = BaseStyle();
                var element = child.GetComponent<LayoutElement>();
                if (element && !element.ignoreLayout)
                {
                    if (element.minWidth >= 0) style.minWidth = TaffyNative.Value.Points(element.minWidth);
                    if (element.minHeight >= 0) style.minHeight = TaffyNative.Value.Points(element.minHeight);
                    if (element.preferredWidth >= 0) style.width = TaffyNative.Value.Points(element.preferredWidth);
                    if (element.preferredHeight >= 0) style.height = TaffyNative.Value.Points(element.preferredHeight);
                    if (element.flexibleWidth >= 0) style.flexGrow = element.flexibleWidth;
                    if (element.flexibleHeight >= 0 && direction == TaffyFlexDirection.Column) style.flexGrow = Mathf.Max(style.flexGrow, element.flexibleHeight);
                }
                else
                {
                    float preferredWidth = LayoutUtility.GetPreferredWidth(child);
                    float preferredHeight = LayoutUtility.GetPreferredHeight(child);
                    if (preferredWidth > 0) style.width = TaffyNative.Value.Points(preferredWidth);
                    if (preferredHeight > 0) style.height = TaffyNative.Value.Points(preferredHeight);
                }

                var item = child.GetComponent<TaffyLayoutItem>();
                if (item) style = item.ApplyTo(style);
                TaffyNative.Check(TaffyNative.tu_node_create(_context, ref style, out var node), "create child");
                _nodes.Add(node);
                children[i] = node;
            }

            TaffyNative.Check(TaffyNative.tu_node_set_children(_context, _root, children, (uint)children.Length), "set children");
            _dirty = false;
        }

        private void ApplyLayout(bool horizontal, bool vertical)
        {
            if (_applying) return;
            _applying = true;
            try
            {
                RebuildNativeTree();
                if (_context == 0 || _root == 0) return;
                TaffyNative.Check(TaffyNative.tu_compute_layout(_context, _root, Mathf.Max(0f, rectTransform.rect.width), Mathf.Max(0f, rectTransform.rect.height)), "compute layout");
                for (int i = 0; i < rectChildren.Count && i < _nodes.Count; i++)
                {
                    TaffyNative.Check(TaffyNative.tu_get_layout(_context, _nodes[i], out var layout), "get layout");
                    if (horizontal) SetChildAlongAxis(rectChildren[i], 0, layout.x, layout.width);
                    if (vertical) SetChildAlongAxis(rectChildren[i], 1, layout.y, layout.height);
                }
            }
            finally { _applying = false; }
        }
    }
}
