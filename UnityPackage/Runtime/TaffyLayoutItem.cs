using UnityEngine;

namespace TaffyUGUI
{
    public enum TaffyUnit { Auto, Points, Percent }
    public enum TaffyFlexDirection { Row, Column, RowReverse, ColumnReverse }
    public enum TaffyFlexWrap { NoWrap, Wrap, WrapReverse }
    public enum TaffyAlign { Auto = -1, Start = 0, End = 1, Center = 2, Stretch = 3 }
    public enum TaffyJustify { Auto = -1, Start = 0, End = 1, Center = 2, SpaceBetween = 3, SpaceAround = 4, SpaceEvenly = 5 }

    [System.Serializable]
    public struct TaffyLength
    {
        public TaffyUnit unit;
        public float value;

        public static TaffyLength Auto => new TaffyLength { unit = TaffyUnit.Auto };
        public static TaffyLength Points(float value) => new TaffyLength { unit = TaffyUnit.Points, value = value };
        public static TaffyLength Percent(float value) => new TaffyLength { unit = TaffyUnit.Percent, value = value };

        internal TaffyNative.Dimension ToNative() => new TaffyNative.Dimension { unit = (int)unit, value = value };
    }

    [DisallowMultipleComponent]
    public sealed class TaffyLayoutItem : MonoBehaviour
    {
        public TaffyLength width = default;
        public TaffyLength height = default;
        public TaffyLength minWidth = default;
        public TaffyLength minHeight = default;
        public TaffyLength maxWidth = default;
        public TaffyLength maxHeight = default;
        public TaffyLength flexBasis = default;
        [Min(0)] public float flexGrow = 0;
        [Min(0)] public float flexShrink = 1;
        public TaffyAlign alignSelf = TaffyAlign.Auto;
        [Min(0)] public float aspectRatio = 0;

        private void Reset()
        {
            width = height = minWidth = minHeight = maxWidth = maxHeight = flexBasis = TaffyLength.Auto;
        }

        private void OnEnable() => SetDirty();
        private void OnDisable() => SetDirty();
        private void OnValidate() => SetDirty();
        private void OnTransformParentChanged() => SetDirty();

        internal TaffyNative.Style ApplyTo(TaffyNative.Style style)
        {
            style.width = width.ToNative();
            style.height = height.ToNative();
            style.minWidth = minWidth.ToNative();
            style.minHeight = minHeight.ToNative();
            style.maxWidth = maxWidth.ToNative();
            style.maxHeight = maxHeight.ToNative();
            style.flexBasis = flexBasis.ToNative();
            style.flexGrow = flexGrow;
            style.flexShrink = flexShrink;
            style.alignSelf = (int)alignSelf;
            style.aspectRatio = aspectRatio;
            return style;
        }

        private void SetDirty()
        {
            if (!isActiveAndEnabled) return;
            var group = GetComponentInParent<TaffyLayoutGroup>();
            if (group) group.SetLayoutDirty();
        }
    }
}
