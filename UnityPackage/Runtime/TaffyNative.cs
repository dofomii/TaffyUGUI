using System;
using System.Runtime.InteropServices;

namespace TaffyUGUI
{
    internal static class TaffyNative
    {
#if UNITY_IOS && !UNITY_EDITOR
        private const string Library = "__Internal";
#else
        private const string Library = "taffy_ugui";
#endif

        [StructLayout(LayoutKind.Sequential)]
        internal struct Dimension
        {
            public int unit;
            public float value;

            public static Dimension Auto => new Dimension { unit = 0 };
            public static Dimension Points(float value) => new Dimension { unit = 1, value = value };
            public static Dimension Percent(float value) => new Dimension { unit = 2, value = value };
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct Style
        {
            public int flexDirection, flexWrap;
            public Dimension width, height, minWidth, minHeight, maxWidth, maxHeight, flexBasis;
            public float flexGrow, flexShrink, gapX, gapY;
            public float paddingLeft, paddingRight, paddingTop, paddingBottom;
            public int alignItems, alignSelf, justifyContent;
            public float aspectRatio;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct Layout
        {
            public float x, y, width, height;
        }

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] internal static extern uint taffy_ugui_api_version();
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] internal static extern IntPtr taffy_ugui_create_context();
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] internal static extern void taffy_ugui_destroy_context(IntPtr context);
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] internal static extern int taffy_ugui_create_node(IntPtr context, Style style, out ulong node);
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] internal static extern int taffy_ugui_remove_node(IntPtr context, ulong node);
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] internal static extern int taffy_ugui_set_style(IntPtr context, ulong node, Style style);
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] internal static extern int taffy_ugui_set_children(IntPtr context, ulong node, [In] ulong[] children, UIntPtr count);
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] internal static extern int taffy_ugui_mark_dirty(IntPtr context, ulong node);
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] internal static extern int taffy_ugui_compute_layout(IntPtr context, ulong root, float width, float height);
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] internal static extern int taffy_ugui_get_layout(IntPtr context, ulong node, out Layout layout);

        internal static void Check(int error, string operation)
        {
            if (error != 0) throw new InvalidOperationException($"TaffyUGUI {operation} failed with native error {error}.");
        }
    }
}
