using System;
using System.Runtime.InteropServices;

namespace TaffyUGUI
{
    internal static class TaffyNative
    {
#if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
        private const string Library = "__Internal";
#else
        private const string Library = "taffy_ugui";
#endif

        internal enum ValueKind : int { Auto = 0, Length = 1, Percent = 2, Calc = 3 }
        internal enum Display : int { None = 0, Flex = 1, Grid = 2, Block = 3, FlowRoot = 4 }
        internal enum BoxSizing : int { BorderBox = 0, ContentBox = 1 }
        internal enum Direction : int { Ltr = 0, Rtl = 1 }
        internal enum Overflow : int { Visible = 0, Clip = 1, Hidden = 2, Scroll = 3 }
        internal enum Position : int { Relative = 0, Absolute = 1 }
        internal enum FlexDirection : int { Row = 0, Column = 1, RowReverse = 2, ColumnReverse = 3 }
        internal enum FlexWrap : int { NoWrap = 0, Wrap = 1, WrapReverse = 2 }
        internal enum Align : int { Unset = -1, Start = 0, End = 1, Center = 2, Stretch = 3, Baseline = 4, FlexStart = 5, FlexEnd = 6 }
        internal enum AlignContent : int { Unset = -1, Start = 0, End = 1, Center = 2, Stretch = 3, SpaceBetween = 4, SpaceAround = 5, SpaceEvenly = 6, FlexStart = 7, FlexEnd = 8 }
        internal enum FloatMode : int { None = 0, Left = 1, Right = 2 }
        internal enum ClearMode : int { None = 0, Left = 1, Right = 2, Both = 3 }
        internal enum TextAlign : int { Auto = 0, LegacyLeft = 1, LegacyRight = 2, LegacyCenter = 3 }
        internal enum GridAutoFlow : int { Row = 0, Column = 1, RowDense = 2, ColumnDense = 3 }
        internal enum GridPlacementKind : int { Auto = 0, Line = 1, Span = 2, NamedLine = 3, NamedSpan = 4 }

        [StructLayout(LayoutKind.Sequential)]
        internal struct Value
        {
            public int kind;
            public float value;
            public ulong resource;

            public static Value Auto => new Value { kind = (int)ValueKind.Auto };
            public static Value Points(float value) => new Value { kind = (int)ValueKind.Length, value = value };
            public static Value Percent(float value) => new Value { kind = (int)ValueKind.Percent, value = value };
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct StringView
        {
            public IntPtr data;
            public uint len;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct GridPlacement
        {
            public int kind;
            public int line;
            public uint span;
            public int occurrence;
            public StringView name;

            public static GridPlacement Auto => new GridPlacement { kind = (int)GridPlacementKind.Auto };
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct Style
        {
            public int display;
            public int boxSizing;
            public int direction;
            public int overflowX;
            public int overflowY;
            public float scrollbarWidth;
            public int position;
            public Value insetLeft, insetRight, insetTop, insetBottom;
            public Value width, height, minWidth, minHeight, maxWidth, maxHeight;
            public float aspectRatio;
            public Value marginLeft, marginRight, marginTop, marginBottom;
            public Value paddingLeft, paddingRight, paddingTop, paddingBottom;
            public Value borderLeft, borderRight, borderTop, borderBottom;
            public int flexDirection;
            public int flexWrap;
            public Value flexBasis;
            public float flexGrow;
            public float flexShrink;
            public int alignItems;
            public int alignSelf;
            public int alignContent;
            public int justifyContent;
            public int justifyItems;
            public int justifySelf;
            public Value gapX;
            public Value gapY;
            public byte itemIsTable;
            public byte itemIsReplaced;
            public int floatMode;
            public int clearMode;
            public int textAlign;
            public int gridAutoFlow;
            public GridPlacement gridRowStart, gridRowEnd, gridColumnStart, gridColumnEnd;

            public static Style FlexDefaults()
            {
                var auto = Value.Auto;
                var zero = Value.Points(0f);
                var placement = GridPlacement.Auto;
                return new Style
                {
                    display = (int)Display.Flex,
                    boxSizing = (int)BoxSizing.BorderBox,
                    direction = (int)Direction.Ltr,
                    overflowX = (int)Overflow.Visible,
                    overflowY = (int)Overflow.Visible,
                    position = (int)Position.Relative,
                    insetLeft = auto, insetRight = auto, insetTop = auto, insetBottom = auto,
                    width = auto, height = auto, minWidth = auto, minHeight = auto, maxWidth = auto, maxHeight = auto,
                    marginLeft = zero, marginRight = zero, marginTop = zero, marginBottom = zero,
                    paddingLeft = zero, paddingRight = zero, paddingTop = zero, paddingBottom = zero,
                    borderLeft = zero, borderRight = zero, borderTop = zero, borderBottom = zero,
                    flexDirection = (int)FlexDirection.Row,
                    flexWrap = (int)FlexWrap.NoWrap,
                    flexBasis = auto,
                    flexShrink = 1f,
                    alignItems = (int)Align.Unset,
                    alignSelf = (int)Align.Unset,
                    alignContent = (int)AlignContent.Unset,
                    justifyContent = (int)AlignContent.Unset,
                    justifyItems = (int)Align.Unset,
                    justifySelf = (int)Align.Unset,
                    gapX = zero,
                    gapY = zero,
                    floatMode = (int)FloatMode.None,
                    clearMode = (int)ClearMode.None,
                    textAlign = (int)TextAlign.Auto,
                    gridAutoFlow = (int)GridAutoFlow.Row,
                    gridRowStart = placement,
                    gridRowEnd = placement,
                    gridColumnStart = placement,
                    gridColumnEnd = placement,
                };
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct Layout
        {
            public ulong node;
            public uint order;
            public float x, y, width, height;
            public float contentWidth, contentHeight;
            public float scrollWidth, scrollHeight;
        }

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] internal static extern uint tu_get_abi_version();
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] internal static extern uint tu_get_abi_stage();
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] internal static extern ulong tu_get_capabilities();
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] internal static extern uint tu_get_taffy_version_packed();
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] internal static extern uint tu_get_last_error_length();
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] internal static extern int tu_copy_last_error([Out] byte[] buffer, uint capacity, out uint written);
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] internal static extern int tu_context_create(out ulong context);
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] internal static extern int tu_context_destroy(ulong context);
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] internal static extern int tu_context_clear(ulong context);
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] internal static extern int tu_node_create(ulong context, ref Style style, out ulong node);
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] internal static extern int tu_node_remove(ulong context, ulong node);
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] internal static extern int tu_node_set_style(ulong context, ulong node, ref Style style);
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] internal static extern int tu_node_set_children(ulong context, ulong node, [In] ulong[] children, uint count);
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] internal static extern int tu_node_mark_dirty(ulong context, ulong node);
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] internal static extern int tu_compute_layout(ulong context, ulong root, float width, float height);
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] internal static extern int tu_get_layout(ulong context, ulong node, out Layout layout);

        internal static void ValidateAbi()
        {
            var abi = tu_get_abi_version();
            var stage = tu_get_abi_stage();
            if (abi != 1 || stage < 1)
                throw new InvalidOperationException($"TaffyUGUI native ABI mismatch. Expected ABI-v1-RC or newer compatible stage, got {abi}/{stage}.");
            if (tu_get_taffy_version_packed() != (13u << 12))
                throw new InvalidOperationException("TaffyUGUI native Taffy version mismatch. Expected 0.13.x ABI baseline.");
            if ((tu_get_capabilities() & 1UL) == 0)
                throw new InvalidOperationException("TaffyUGUI native library does not advertise the required Flexbox capability.");
            if (Marshal.SizeOf<Value>() != 16 || Marshal.SizeOf<GridPlacement>() != 32 || Marshal.SizeOf<Style>() != 632 || Marshal.SizeOf<Layout>() != 48)
                throw new InvalidOperationException("TaffyUGUI managed/native struct layout mismatch.");
        }

        private static string LastError()
        {
            uint length = tu_get_last_error_length();
            if (length == 0) return string.Empty;
            var buffer = new byte[length];
            int status = tu_copy_last_error(buffer, length, out uint written);
            if (status != 0 || written == 0) return string.Empty;
            return System.Text.Encoding.UTF8.GetString(buffer, 0, (int)written);
        }

        internal static void Check(int status, string operation)
        {
            if (status == 0) return;
            string detail = LastError();
            if (string.IsNullOrEmpty(detail))
                throw new InvalidOperationException($"TaffyUGUI {operation} failed with native status {status}.");
            throw new InvalidOperationException($"TaffyUGUI {operation} failed with native status {status}: {detail}");
        }
    }
}
