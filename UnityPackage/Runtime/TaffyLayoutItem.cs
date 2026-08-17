using UnityEngine;

namespace TaffyUGUI
{
    [DisallowMultipleComponent]
    public sealed class TaffyLayoutItem : MonoBehaviour
    {
        [Header("Display")]
        public TaffyDisplay display = TaffyDisplay.Flex;
        public TaffyBoxSizing boxSizing = TaffyBoxSizing.BorderBox;
        public TaffyWritingDirection writingDirection = TaffyWritingDirection.LeftToRight;
        public TaffyOverflow overflowX = TaffyOverflow.Visible;
        public TaffyOverflow overflowY = TaffyOverflow.Visible;
        [Min(0)] public float scrollbarWidth;

        [Header("Position and Size")]
        public TaffyPosition position = TaffyPosition.Relative;
        public TaffyEdges inset = TaffyEdges.Auto;
        public TaffyLength width = default;
        public TaffyLength height = default;
        public TaffyLength minWidth = default;
        public TaffyLength minHeight = default;
        public TaffyLength maxWidth = default;
        public TaffyLength maxHeight = default;
        [Min(0)] public float aspectRatio;

        [Header("Box Model")]
        public TaffyEdges margin = TaffyEdges.Zero;
        public TaffyEdges padding = TaffyEdges.Zero;
        public TaffyEdges border = TaffyEdges.Zero;

        [Header("Flex Item")]
        public TaffyLength flexBasis = default;
        [Min(0)] public float flexGrow;
        [Min(0)] public float flexShrink = 1f;
        public TaffyAlign alignSelf = TaffyAlign.Auto;

        [Header("Block / Float")]
        public TaffyFloat floatMode = TaffyFloat.None;
        public TaffyClear clearMode = TaffyClear.None;
        public TaffyTextAlign textAlign = TaffyTextAlign.Auto;

        [Header("Intrinsic Measurement")]
        public TaffyMeasurementMode measurement = TaffyMeasurementMode.Auto;
        public bool forceReplacedElement;
        public bool itemIsTable;

        private void Reset()
        {
            inset = TaffyEdges.Auto;
            width = height = minWidth = minHeight = maxWidth = maxHeight = flexBasis = TaffyLength.Auto;
            margin = TaffyEdges.Zero;
            padding = TaffyEdges.Zero;
            border = TaffyEdges.Zero;
            display = TaffyDisplay.Flex;
            boxSizing = TaffyBoxSizing.BorderBox;
            writingDirection = TaffyWritingDirection.LeftToRight;
            overflowX = overflowY = TaffyOverflow.Visible;
            position = TaffyPosition.Relative;
            flexShrink = 1f;
            alignSelf = TaffyAlign.Auto;
            measurement = TaffyMeasurementMode.Auto;
        }

        private void OnEnable() => SetDirty();
        private void OnDisable() => SetDirty();
        private void OnValidate() => SetDirty();
        private void OnTransformParentChanged() => SetDirty();
        private void OnDidApplyAnimationProperties() => SetDirty();

        internal bool MeasurementEnabled => measurement != TaffyMeasurementMode.Disabled;

        internal TaffyNative.Style ApplyTo(TaffyNative.Style style, bool measuredAsReplaced)
        {
            style.display = (int)display;
            style.boxSizing = (int)boxSizing;
            style.direction = (int)writingDirection;
            style.overflowX = (int)overflowX;
            style.overflowY = (int)overflowY;
            style.scrollbarWidth = Mathf.Max(0f, scrollbarWidth);
            style.position = (int)position;

            style.insetLeft = inset.left.ToLengthPercentageAuto();
            style.insetRight = inset.right.ToLengthPercentageAuto();
            style.insetTop = inset.top.ToLengthPercentageAuto();
            style.insetBottom = inset.bottom.ToLengthPercentageAuto();

            if (!width.IsAuto) style.width = width.ToDimension();
            if (!height.IsAuto) style.height = height.ToDimension();
            if (!minWidth.IsAuto) style.minWidth = minWidth.ToDimension();
            if (!minHeight.IsAuto) style.minHeight = minHeight.ToDimension();
            if (!maxWidth.IsAuto) style.maxWidth = maxWidth.ToDimension();
            if (!maxHeight.IsAuto) style.maxHeight = maxHeight.ToDimension();

            style.marginLeft = margin.left.ToLengthPercentageAuto();
            style.marginRight = margin.right.ToLengthPercentageAuto();
            style.marginTop = margin.top.ToLengthPercentageAuto();
            style.marginBottom = margin.bottom.ToLengthPercentageAuto();
            style.paddingLeft = padding.left.ToNonNegativeLengthPercentage();
            style.paddingRight = padding.right.ToNonNegativeLengthPercentage();
            style.paddingTop = padding.top.ToNonNegativeLengthPercentage();
            style.paddingBottom = padding.bottom.ToNonNegativeLengthPercentage();
            style.borderLeft = border.left.ToNonNegativeLengthPercentage();
            style.borderRight = border.right.ToNonNegativeLengthPercentage();
            style.borderTop = border.top.ToNonNegativeLengthPercentage();
            style.borderBottom = border.bottom.ToNonNegativeLengthPercentage();

            if (!flexBasis.IsAuto) style.flexBasis = flexBasis.ToDimension();
            style.flexGrow = Mathf.Max(0f, flexGrow);
            style.flexShrink = Mathf.Max(0f, flexShrink);
            style.alignSelf = (int)alignSelf;
            style.aspectRatio = Mathf.Max(0f, aspectRatio);
            style.floatMode = (int)floatMode;
            style.clearMode = (int)clearMode;
            style.textAlign = (int)textAlign;
            style.itemIsTable = itemIsTable ? (byte)1 : (byte)0;
            style.itemIsReplaced = forceReplacedElement || measuredAsReplaced ? (byte)1 : (byte)0;
            return style;
        }

        public void InvalidateMeasurement()
        {
            Transform parent = transform.parent;
            if (!parent) return;
            TaffyLayoutGroup group = parent.GetComponentInParent<TaffyLayoutGroup>();
            if (group) group.InvalidateMeasurement(transform as RectTransform);
        }

        private void SetDirty()
        {
            Transform parent = transform.parent;
            if (!parent) return;
            TaffyLayoutGroup group = parent.GetComponentInParent<TaffyLayoutGroup>();
            if (group) group.SetLayoutDirty();
        }
    }
}
