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

        internal bool IsAuto => unit == TaffyUnit.Auto;

        internal TaffyNative.Value ToNative()
        {
            switch (unit)
            {
                case TaffyUnit.Points: return TaffyNative.Value.Points(Mathf.Max(0f, value));
                case TaffyUnit.Percent: return TaffyNative.Value.Percent(Mathf.Max(0f, value));
                default: return TaffyNative.Value.Auto;
            }
        }
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
            // Until Phase 8 adds native measurement adapters, Auto preserves the
            // uGUI-derived intrinsic value already placed in the style by the group.
            if (!width.IsAuto) style.width = width.ToNative();
            if (!height.IsAuto) style.height = height.ToNative();
            if (!minWidth.IsAuto) style.minWidth = minWidth.ToNative();
            if (!minHeight.IsAuto) style.minHeight = minHeight.ToNative();
            if (!maxWidth.IsAuto) style.maxWidth = maxWidth.ToNative();
            if (!maxHeight.IsAuto) style.maxHeight = maxHeight.ToNative();
            if (!flexBasis.IsAuto) style.flexBasis = flexBasis.ToNative();
            style.flexGrow = Mathf.Max(0f, flexGrow);
            style.flexShrink = Mathf.Max(0f, flexShrink);
            style.alignSelf = (int)alignSelf;
            style.aspectRatio = Mathf.Max(0f, aspectRatio);
            return style;
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
