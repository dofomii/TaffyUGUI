using UnityEngine;

namespace TaffyUGUI.Editor
{
    internal static class TaffyEditorContent
    {
        internal static readonly GUIContent FormattingContext = new GUIContent(
            "Formatting Context",
            "Container display, box sizing, writing direction, overflow, padding, border, and text alignment settings.");

        internal static readonly GUIContent FlexAlignment = new GUIContent(
            "Flex / Alignment",
            "Flex direction, wrapping, gaps, and alignment settings. These values remain serialized even when another display mode is active.");

        internal static readonly GUIContent GridAuthoring = new GUIContent(
            "Grid Authoring",
            "Grid auto-flow, explicit and implicit tracks, named lines, and named areas.");

        internal static readonly GUIContent ResponsiveIntegration = new GUIContent(
            "Responsive / Integration",
            "Responsive profiles plus safe-area, ScrollRect, pixel-rounding, and rebuild integration settings.");

        internal static readonly GUIContent LiveDiagnostics = new GUIContent(
            "Live Diagnostics",
            "Read-only runtime/editor layout state and diagnostic actions for the selected Taffy layout group.");

        internal static readonly GUIContent Display = new GUIContent(
            "Display",
            "How this item participates in layout, including display type, box sizing, direction, and overflow behavior.");

        internal static readonly GUIContent PositionAndSize = new GUIContent(
            "Position and Size",
            "Positioning, inset offsets, width/height constraints, and aspect ratio.");

        internal static readonly GUIContent BoxModel = new GUIContent(
            "Box Model",
            "Margin, padding, and border dimensions around the item.");

        internal static readonly GUIContent FlexItem = new GUIContent(
            "Flex Item",
            "Per-item Flex basis, grow, shrink, and cross-axis alignment override.");

        internal static readonly GUIContent GridItem = new GUIContent(
            "Grid Item",
            "Per-item Grid row/column placement and alignment override.");

        internal static readonly GUIContent BlockFloat = new GUIContent(
            "Block / Float",
            "Block formatting context controls including float, clear, and text alignment.");

        internal static readonly GUIContent IntrinsicMeasurement = new GUIContent(
            "Intrinsic Measurement",
            "Controls whether TaffyUGUI resolves intrinsic content measurements for this item.");

        internal const string InactiveFlexMessage =
            "These values remain serialized and can become active through a responsive profile or if Display changes to Flex.";

        internal const string InactiveGridMessage =
            "Grid template data is preserved but inactive until Display resolves to Grid.";
    }
}
