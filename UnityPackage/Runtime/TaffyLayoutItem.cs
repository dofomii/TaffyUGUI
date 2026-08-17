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

        [Header("Grid Item")]
        public TaffyGridPlacement gridRowStart = default;
        public TaffyGridPlacement gridRowEnd = default;
        public TaffyGridPlacement gridColumnStart = default;
        public TaffyGridPlacement gridColumnEnd = default;
        public TaffyAlign justifySelf = TaffyAlign.Auto;

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
            justifySelf = TaffyAlign.Auto;
            gridRowStart = gridRowEnd = gridColumnStart = gridColumnEnd = TaffyGridPlacement.Auto;
            measurement = TaffyMeasurementMode.Auto;
        }

        private void OnEnable() => SetDirty();
        private void OnDisable() => SetDirty();
        private void OnValidate() => SetDirty();
        private void OnTransformParentChanged() => SetDirty();
        private void OnDidApplyAnimationProperties() => SetDirty();

        internal bool MeasurementEnabled => measurement != TaffyMeasurementMode.Disabled;

        internal TaffyNative.Style ApplyTo(
            TaffyNative.Style style,
            bool measuredAsReplaced,
            TaffyCalcResourceCache calcResources,
            TaffyNativeMarshallingScope marshalling,
            out string gridPlacementSignature)
        {
            style.display = (int)display;
            style.boxSizing = (int)boxSizing;
            style.direction = (int)writingDirection;
            style.overflowX = (int)overflowX;
            style.overflowY = (int)overflowY;
            style.scrollbarWidth = Mathf.Max(0f, scrollbarWidth);
            style.position = (int)position;

            style.insetLeft = inset.left.ToLengthPercentageAuto(calcResources);
            style.insetRight = inset.right.ToLengthPercentageAuto(calcResources);
            style.insetTop = inset.top.ToLengthPercentageAuto(calcResources);
            style.insetBottom = inset.bottom.ToLengthPercentageAuto(calcResources);

            if (!width.IsAuto) style.width = width.ToDimension(calcResources);
            if (!height.IsAuto) style.height = height.ToDimension(calcResources);
            if (!minWidth.IsAuto) style.minWidth = minWidth.ToDimension(calcResources);
            if (!minHeight.IsAuto) style.minHeight = minHeight.ToDimension(calcResources);
            if (!maxWidth.IsAuto) style.maxWidth = maxWidth.ToDimension(calcResources);
            if (!maxHeight.IsAuto) style.maxHeight = maxHeight.ToDimension(calcResources);

            style.marginLeft = margin.left.ToLengthPercentageAuto(calcResources);
            style.marginRight = margin.right.ToLengthPercentageAuto(calcResources);
            style.marginTop = margin.top.ToLengthPercentageAuto(calcResources);
            style.marginBottom = margin.bottom.ToLengthPercentageAuto(calcResources);
            style.paddingLeft = padding.left.ToNonNegativeLengthPercentage(calcResources);
            style.paddingRight = padding.right.ToNonNegativeLengthPercentage(calcResources);
            style.paddingTop = padding.top.ToNonNegativeLengthPercentage(calcResources);
            style.paddingBottom = padding.bottom.ToNonNegativeLengthPercentage(calcResources);
            style.borderLeft = border.left.ToNonNegativeLengthPercentage(calcResources);
            style.borderRight = border.right.ToNonNegativeLengthPercentage(calcResources);
            style.borderTop = border.top.ToNonNegativeLengthPercentage(calcResources);
            style.borderBottom = border.bottom.ToNonNegativeLengthPercentage(calcResources);

            if (!flexBasis.IsAuto) style.flexBasis = flexBasis.ToDimension(calcResources);
            style.flexGrow = Mathf.Max(0f, flexGrow);
            style.flexShrink = Mathf.Max(0f, flexShrink);
            style.alignSelf = (int)alignSelf;
            style.justifySelf = (int)justifySelf;
            style.aspectRatio = Mathf.Max(0f, aspectRatio);
            style.floatMode = (int)floatMode;
            style.clearMode = (int)clearMode;
            style.textAlign = (int)textAlign;
            style.itemIsTable = itemIsTable ? (byte)1 : (byte)0;
            style.itemIsReplaced = forceReplacedElement || measuredAsReplaced ? (byte)1 : (byte)0;

            style.gridRowStart = gridRowStart.ToNative(marshalling, name + ".gridRowStart");
            style.gridRowEnd = gridRowEnd.ToNative(marshalling, name + ".gridRowEnd");
            style.gridColumnStart = gridColumnStart.ToNative(marshalling, name + ".gridColumnStart");
            style.gridColumnEnd = gridColumnEnd.ToNative(marshalling, name + ".gridColumnEnd");
            gridPlacementSignature = GridPlacementSignature();
            return style;
        }

        internal bool TryValidateGridPlacement(string label, out string error)
        {
            return gridRowStart.TryValidate(label + ".gridRowStart", out error) &&
                   gridRowEnd.TryValidate(label + ".gridRowEnd", out error) &&
                   gridColumnStart.TryValidate(label + ".gridColumnStart", out error) &&
                   gridColumnEnd.TryValidate(label + ".gridColumnEnd", out error);
        }

        internal bool TryValidateCalc(out string error)
        {
            return inset.TryValidateCalc(name + ".inset", out error) &&
                   width.TryValidateCalc(name + ".width", out error) &&
                   height.TryValidateCalc(name + ".height", out error) &&
                   minWidth.TryValidateCalc(name + ".minWidth", out error) &&
                   minHeight.TryValidateCalc(name + ".minHeight", out error) &&
                   maxWidth.TryValidateCalc(name + ".maxWidth", out error) &&
                   maxHeight.TryValidateCalc(name + ".maxHeight", out error) &&
                   margin.TryValidateCalc(name + ".margin", out error) &&
                   padding.TryValidateCalc(name + ".padding", out error) &&
                   border.TryValidateCalc(name + ".border", out error) &&
                   flexBasis.TryValidateCalc(name + ".flexBasis", out error);
        }

        public void InvalidateMeasurement()
        {
            Transform parent = transform.parent;
            if (!parent) return;
            TaffyLayoutGroup group = parent.GetComponentInParent<TaffyLayoutGroup>();
            if (group) group.InvalidateMeasurement(transform as RectTransform);
        }

        private string GridPlacementSignature()
        {
            return gridRowStart.Signature() + "|" + gridRowEnd.Signature() + "|" +
                   gridColumnStart.Signature() + "|" + gridColumnEnd.Signature();
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
