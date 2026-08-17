using System;
using UnityEngine;

namespace TaffyUGUI
{
    public enum TaffyUnit { Auto, Points, Percent }
    public enum TaffyContainerDisplay { Flex = 1, Block = 3, FlowRoot = 4 }
    public enum TaffyDisplay { None = 0, Flex = 1, Grid = 2, Block = 3, FlowRoot = 4 }
    public enum TaffyBoxSizing { BorderBox = 0, ContentBox = 1 }
    public enum TaffyWritingDirection { LeftToRight = 0, RightToLeft = 1 }
    public enum TaffyOverflow { Visible = 0, Clip = 1, Hidden = 2, Scroll = 3 }
    public enum TaffyPosition { Relative = 0, Absolute = 1 }
    public enum TaffyFlexDirection { Row = 0, Column = 1, RowReverse = 2, ColumnReverse = 3 }
    public enum TaffyFlexWrap { NoWrap = 0, Wrap = 1, WrapReverse = 2 }

    public enum TaffyAlign
    {
        Auto = -1,
        Start = 0,
        End = 1,
        Center = 2,
        Stretch = 3,
        Baseline = 4,
        FlexStart = 5,
        FlexEnd = 6,
        SelfStart = 7,
        SelfEnd = 8,
        SafeStart = 9,
        SafeEnd = 10,
        SafeCenter = 11,
        SafeFlexStart = 12,
        SafeFlexEnd = 13,
        SafeSelfStart = 14,
        SafeSelfEnd = 15,
    }

    // Existing numeric values are preserved for serialized Phase 7 data.
    public enum TaffyJustify
    {
        Auto = -1,
        Start = 0,
        End = 1,
        Center = 2,
        SpaceBetween = 3,
        SpaceAround = 4,
        SpaceEvenly = 5,
        FlexStart = 6,
        FlexEnd = 7,
        SafeStart = 8,
        SafeEnd = 9,
        SafeCenter = 10,
        SafeFlexStart = 11,
        SafeFlexEnd = 12,
    }

    public enum TaffyAlignContent
    {
        Auto = -1,
        Start = 0,
        End = 1,
        Center = 2,
        Stretch = 3,
        SpaceBetween = 4,
        SpaceAround = 5,
        SpaceEvenly = 6,
        FlexStart = 7,
        FlexEnd = 8,
        SafeStart = 9,
        SafeEnd = 10,
        SafeCenter = 11,
        SafeFlexStart = 12,
        SafeFlexEnd = 13,
    }

    public enum TaffyFloat { None = 0, Left = 1, Right = 2 }
    public enum TaffyClear { None = 0, Left = 1, Right = 2, Both = 3 }
    public enum TaffyTextAlign { Auto = 0, LegacyLeft = 1, LegacyRight = 2, LegacyCenter = 3 }
    public enum TaffyMeasurementMode { Auto = 0, Disabled = 1 }

    [Serializable]
    public struct TaffyLength
    {
        public TaffyUnit unit;
        public float value;

        public static TaffyLength Auto => new TaffyLength { unit = TaffyUnit.Auto };
        public static TaffyLength Points(float value) => new TaffyLength { unit = TaffyUnit.Points, value = value };
        public static TaffyLength Percent(float value) => new TaffyLength { unit = TaffyUnit.Percent, value = value };

        internal bool IsAuto => unit == TaffyUnit.Auto;

        internal TaffyNative.Value ToDimension()
        {
            switch (unit)
            {
                case TaffyUnit.Points: return TaffyNative.Value.Points(Mathf.Max(0f, FiniteOrZero(value)));
                case TaffyUnit.Percent: return TaffyNative.Value.Percent(Mathf.Max(0f, FiniteOrZero(value)));
                default: return TaffyNative.Value.Auto;
            }
        }

        internal TaffyNative.Value ToLengthPercentageAuto()
        {
            switch (unit)
            {
                case TaffyUnit.Points: return TaffyNative.Value.Points(FiniteOrZero(value));
                case TaffyUnit.Percent: return TaffyNative.Value.Percent(FiniteOrZero(value));
                default: return TaffyNative.Value.Auto;
            }
        }

        internal TaffyNative.Value ToNonNegativeLengthPercentage()
        {
            switch (unit)
            {
                case TaffyUnit.Points: return TaffyNative.Value.Points(Mathf.Max(0f, FiniteOrZero(value)));
                case TaffyUnit.Percent: return TaffyNative.Value.Percent(Mathf.Max(0f, FiniteOrZero(value)));
                default: return TaffyNative.Value.Points(0f);
            }
        }

        private static float FiniteOrZero(float input)
        {
            return float.IsNaN(input) || float.IsInfinity(input) ? 0f : input;
        }
    }

    [Serializable]
    public struct TaffyEdges
    {
        public TaffyLength left;
        public TaffyLength right;
        public TaffyLength top;
        public TaffyLength bottom;

        public static TaffyEdges Zero => Uniform(TaffyLength.Points(0f));
        public static TaffyEdges Auto => Uniform(TaffyLength.Auto);

        public static TaffyEdges Uniform(TaffyLength value)
        {
            return new TaffyEdges { left = value, right = value, top = value, bottom = value };
        }

        public static TaffyEdges Points(float value)
        {
            return Uniform(TaffyLength.Points(value));
        }
    }
}
