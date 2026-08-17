using System;
using System.Runtime.InteropServices;
using System.Text;

namespace TaffyUGUI
{
    internal static class TaffyNative
    {
#if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
        internal const string Library = "__Internal";
#else
        internal const string Library = "taffy_ugui";
#endif

        internal const uint AbiVersion = 1;
        internal const uint AbiRcStage = 1;
        internal const uint AbiFinalStage = 2;
        internal const uint TaffyVersionPacked = 13u << 12;

        internal const ulong CapFlex = 1UL << 0;
        internal const ulong CapGrid = 1UL << 1;
        internal const ulong CapBlock = 1UL << 2;
        internal const ulong CapFloat = 1UL << 3;
        internal const ulong CapCalc = 1UL << 4;
        internal const ulong CapContentSize = 1UL << 5;
        internal const ulong CapDetailedGrid = 1UL << 6;
        internal const ulong CapCachedMeasurement = 1UL << 7;
        internal const ulong CapThreadLocalContexts = 1UL << 8;
        internal const ulong RequiredCapabilities = CapFlex | CapGrid | CapBlock | CapFloat | CapCalc |
                                                     CapContentSize | CapDetailedGrid | CapCachedMeasurement |
                                                     CapThreadLocalContexts;

        internal enum Status : int
        {
            Ok = 0,
            NullPointer = -1,
            InvalidContext = -2,
            InvalidNode = -3,
            InvalidResource = -4,
            InvalidEnum = -5,
            InvalidCount = -6,
            InvalidNumber = -7,
            InvalidValue = -8,
            Capacity = -9,
            WrongThread = -10,
            RegistryBusy = -11,
            Engine = -12,
            InternalPanic = -13,
        }

        internal enum ValueKind : int { Auto = 0, Length = 1, Percent = 2, Calc = 3 }
        internal enum Display : int { None = 0, Flex = 1, Grid = 2, Block = 3, FlowRoot = 4 }
        internal enum BoxSizing : int { BorderBox = 0, ContentBox = 1 }
        internal enum Direction : int { Ltr = 0, Rtl = 1 }
        internal enum Overflow : int { Visible = 0, Clip = 1, Hidden = 2, Scroll = 3 }
        internal enum Position : int { Relative = 0, Absolute = 1 }
        internal enum FlexDirection : int { Row = 0, Column = 1, RowReverse = 2, ColumnReverse = 3 }
        internal enum FlexWrap : int { NoWrap = 0, Wrap = 1, WrapReverse = 2 }

        internal enum Align : int
        {
            Unset = -1,
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

        internal enum AlignContent : int
        {
            Unset = -1,
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

        internal enum FloatMode : int { None = 0, Left = 1, Right = 2 }
        internal enum ClearMode : int { None = 0, Left = 1, Right = 2, Both = 3 }
        internal enum TextAlign : int { Auto = 0, LegacyLeft = 1, LegacyRight = 2, LegacyCenter = 3 }
        internal enum GridAutoFlow : int { Row = 0, Column = 1, RowDense = 2, ColumnDense = 3 }
        internal enum GridPlacementKind : int { Auto = 0, Line = 1, Span = 2, NamedLine = 3, NamedSpan = 4 }
        internal enum CalcOp : int { Length = 0, Percent = 1, Add = 2, Sub = 3, Scale = 4, Min = 5, Max = 6, Clamp = 7 }
        internal enum GridTrackKind : int { Auto = 0, Length = 1, Percent = 2, Fraction = 3, MinMax = 4, MinContent = 5, MaxContent = 6, Calc = 7, Repeat = 8 }
        internal enum GridRepeatMode : int { Count = 0, AutoFill = 1, AutoFit = 2 }
        internal enum GridAxis : int { Row = 0, Column = 1 }

        [StructLayout(LayoutKind.Sequential)]
        internal struct Value
        {
            public int kind;
            public float value;
            public ulong resource;

            public static Value Auto => new Value { kind = (int)ValueKind.Auto };
            public static Value Points(float value) => new Value { kind = (int)ValueKind.Length, value = value };
            public static Value Percent(float value) => new Value { kind = (int)ValueKind.Percent, value = value };
            public static Value Calc(ulong resource) => new Value { kind = (int)ValueKind.Calc, resource = resource };
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
            public static GridPlacement Line(int line) => new GridPlacement { kind = (int)GridPlacementKind.Line, line = line };
            public static GridPlacement Span(uint span) => new GridPlacement { kind = (int)GridPlacementKind.Span, span = span };
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

            public static Style Defaults(Display display)
            {
                var auto = Value.Auto;
                var zero = Value.Points(0f);
                var placement = GridPlacement.Auto;
                return new Style
                {
                    display = (int)display,
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

            public static Style FlexDefaults() => Defaults(Display.Flex);
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct StyleUpdate
        {
            public ulong node;
            public Style style;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct ChildrenUpdate
        {
            public ulong parent;
            public IntPtr children;
            public uint childCount;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct MeasurementSample
        {
            public float availableWidth;
            public float width;
            public float height;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct Measurement
        {
            public float minWidth;
            public float minHeight;
            public float maxWidth;
            public float maxHeight;
            public float preferredWidth;
            public float preferredHeight;
            public float aspectRatio;
            public byte isReplaced;
            public IntPtr samples;
            public uint sampleCount;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct MeasurementUpdate
        {
            public ulong node;
            public Measurement measurement;
            public byte hasMeasurement;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct CalcSpec
        {
            public int op;
            public float value;
            public IntPtr operands;
            public uint operandCount;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct GridTrack
        {
            public int kind;
            public float value;
            public ulong resource;
            public int minKind;
            public float minValue;
            public ulong minResource;
            public int maxKind;
            public float maxValue;
            public ulong maxResource;
            public int repeatMode;
            public uint repeatCount;
            public IntPtr repeatTracks;
            public uint repeatTrackCount;

            public static GridTrack Points(float value) => new GridTrack { kind = (int)GridTrackKind.Length, value = value };
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct NamedGridLine
        {
            public int axis;
            public uint lineIndex;
            public StringView name;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct GridArea
        {
            public StringView name;
            public uint rowStart;
            public uint rowEnd;
            public uint columnStart;
            public uint columnEnd;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct GridTemplate
        {
            public IntPtr rows;
            public uint rowCount;
            public IntPtr columns;
            public uint columnCount;
            public IntPtr autoRows;
            public uint autoRowCount;
            public IntPtr autoColumns;
            public uint autoColumnCount;
            public IntPtr namedLines;
            public uint namedLineCount;
            public IntPtr areas;
            public uint areaCount;
            public uint areaRows;
            public uint areaColumns;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct GridInfo
        {
            public uint negativeImplicitRows;
            public uint explicitRows;
            public uint positiveImplicitRows;
            public uint negativeImplicitColumns;
            public uint explicitColumns;
            public uint positiveImplicitColumns;
            public uint rowTrackCount;
            public uint columnTrackCount;
            public uint itemCount;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct GridItemInfo
        {
            public uint rowStart;
            public uint rowEnd;
            public uint columnStart;
            public uint columnEnd;
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
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] internal static extern uint tu_get_build_version_length();
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] internal static extern int tu_copy_build_version([Out] byte[] buffer, uint capacity, out uint written);
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] internal static extern uint tu_get_last_error_length();
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] internal static extern int tu_copy_last_error([Out] byte[] buffer, uint capacity, out uint written);
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] internal static extern int tu_context_create(out ulong context);
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] internal static extern int tu_context_destroy(ulong context);
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] internal static extern int tu_context_clear(ulong context);
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] internal static extern int tu_node_create(ulong context, ref Style style, out ulong node);
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] internal static extern int tu_node_remove(ulong context, ulong node);
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] internal static extern int tu_node_set_style(ulong context, ulong node, ref Style style);
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] internal static extern int tu_nodes_set_styles(ulong context, [In] StyleUpdate[] updates, uint count);
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] internal static extern int tu_node_set_children(ulong context, ulong parent, [In] ulong[] children, uint count);
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] internal static extern int tu_nodes_set_children(ulong context, [In] ChildrenUpdate[] updates, uint count);
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] internal static extern int tu_node_mark_dirty(ulong context, ulong node);
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] internal static extern int tu_node_is_dirty(ulong context, ulong node, out byte dirty);
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] internal static extern int tu_node_set_measurement(ulong context, ulong node, ref Measurement measurement);
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl, EntryPoint = "tu_node_set_measurement")] internal static extern int tu_node_clear_measurement(ulong context, ulong node, IntPtr measurement);
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] internal static extern int tu_nodes_set_measurements(ulong context, [In] MeasurementUpdate[] updates, uint count);
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] internal static extern int tu_calc_create(ulong context, ref CalcSpec spec, out ulong resource);
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] internal static extern int tu_calc_remove(ulong context, ulong resource);
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] internal static extern int tu_node_set_grid_template(ulong context, ulong node, ref GridTemplate template);
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] internal static extern int tu_get_grid_info(ulong context, ulong node, out GridInfo info);
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] internal static extern int tu_get_grid_track_sizes(ulong context, ulong node, int axis, [Out] float[] sizes, uint capacity, out uint written);
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] internal static extern int tu_get_grid_items(ulong context, ulong node, [Out] GridItemInfo[] items, uint capacity, out uint written);
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] internal static extern int tu_get_grid_gutters(ulong context, ulong node, int axis, [Out] float[] gutters, uint capacity, out uint written);
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] internal static extern int tu_compute_layout(ulong context, ulong root, float width, float height);
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] internal static extern int tu_get_layout(ulong context, ulong node, out Layout layout);
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] internal static extern int tu_get_layouts_bulk(ulong context, [In] ulong[] handles, uint count, [Out] Layout[] output, uint capacity, out uint written);

        internal static void ValidateAbi(bool requireFinal = true)
        {
            var abi = tu_get_abi_version();
            var stage = tu_get_abi_stage();
            var minimumStage = requireFinal ? AbiFinalStage : AbiRcStage;
            if (abi != AbiVersion || stage < minimumStage)
                throw new InvalidOperationException($"TaffyUGUI native ABI mismatch. Expected ABI {AbiVersion} stage >= {minimumStage}, got {abi}/{stage}.");
            if (tu_get_taffy_version_packed() != TaffyVersionPacked)
                throw new InvalidOperationException("TaffyUGUI native Taffy version mismatch. Expected exactly 0.13.0.");

            var capabilities = tu_get_capabilities();
            if ((capabilities & RequiredCapabilities) != RequiredCapabilities)
                throw new InvalidOperationException($"TaffyUGUI native capability mismatch. Required 0x{RequiredCapabilities:X}, got 0x{capabilities:X}.");

            ValidateManagedLayout();
            ValidateEnumContract();
        }

        internal static void ValidateManagedLayout()
        {
            RequireSize<Value>(16);
            RequireSize<StringView>(16);
            RequireSize<GridPlacement>(32);
            RequireSize<Style>(632);
            RequireSize<StyleUpdate>(640);
            RequireSize<ChildrenUpdate>(24);
            RequireSize<MeasurementSample>(12);
            RequireSize<Measurement>(48);
            RequireSize<MeasurementUpdate>(64);
            RequireSize<CalcSpec>(24);
            RequireSize<GridTrack>(72);
            RequireSize<NamedGridLine>(24);
            RequireSize<GridArea>(32);
            RequireSize<GridTemplate>(104);
            RequireSize<GridInfo>(36);
            RequireSize<GridItemInfo>(16);
            RequireSize<Layout>(48);

            if (Marshal.OffsetOf<Value>(nameof(Value.kind)).ToInt32() != 0 ||
                Marshal.OffsetOf<Value>(nameof(Value.value)).ToInt32() != 4 ||
                Marshal.OffsetOf<Value>(nameof(Value.resource)).ToInt32() != 8 ||
                Marshal.OffsetOf<Layout>(nameof(Layout.node)).ToInt32() != 0 ||
                Marshal.OffsetOf<Layout>(nameof(Layout.order)).ToInt32() != 8)
                throw new InvalidOperationException("TaffyUGUI managed/native field-offset contract mismatch.");
        }

        internal static void ValidateEnumContract()
        {
            if ((int)Status.Ok != 0 || (int)Status.InternalPanic != -13 ||
                (int)Display.FlowRoot != 4 || (int)GridTrackKind.Repeat != 8 ||
                (int)Align.Unset != -1 || (int)Align.SafeSelfEnd != 15 ||
                (int)AlignContent.SafeFlexEnd != 13 || (int)CalcOp.Clamp != 7)
                throw new InvalidOperationException("TaffyUGUI managed/native enum numeric contract mismatch.");
        }

        internal static string GetBuildVersion()
        {
            return CopyNativeString(tu_get_build_version_length, tu_copy_build_version);
        }

        internal static string GetLastError()
        {
            return CopyNativeString(tu_get_last_error_length, tu_copy_last_error);
        }

        internal static void Check(int status, string operation)
        {
            if (status == (int)Status.Ok) return;
            string detail = GetLastError();
            if (string.IsNullOrEmpty(detail))
                throw new InvalidOperationException($"TaffyUGUI {operation} failed with native status {status}.");
            throw new InvalidOperationException($"TaffyUGUI {operation} failed with native status {status}: {detail}");
        }

        private static string CopyNativeString(Func<uint> lengthGetter, CopyStringDelegate copy)
        {
            uint length = lengthGetter();
            if (length == 0) return string.Empty;
            var buffer = new byte[length];
            int status = copy(buffer, length, out uint written);
            if (status != (int)Status.Ok || written == 0) return string.Empty;
            return Encoding.UTF8.GetString(buffer, 0, checked((int)written));
        }

        private static void RequireSize<T>(int expected) where T : struct
        {
            int actual = Marshal.SizeOf<T>();
            if (actual != expected)
                throw new InvalidOperationException($"TaffyUGUI managed/native struct size mismatch for {typeof(T).Name}: expected {expected}, got {actual}.");
        }

        private delegate int CopyStringDelegate(byte[] buffer, uint capacity, out uint written);
    }
}
