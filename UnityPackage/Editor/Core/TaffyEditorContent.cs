using UnityEngine;

namespace TaffyUGUI.Editor
{
    internal static class TaffyEditorContent
    {
        internal static readonly GUIContent InspectorMode = new GUIContent(
            "Inspector Mode",
            "Simple shows the most common authoring controls first. Advanced keeps the complete TaffyUGUI authoring surface available.");

        internal static readonly GUIContent QuickSetup = new GUIContent(
            "Quick Setup",
            "The essential container settings needed for the most common layouts. These edit the same serialized properties shown in Advanced mode.");

        internal static readonly GUIContent ItemEssentials = new GUIContent(
            "Essentials",
            "The most common item sizing and parent-dependent layout controls. Advanced mode exposes every additional item property.");

        internal static readonly GUIContent ParentLayout = new GUIContent(
            "Parent Layout",
            "The nearest parent TaffyLayoutGroup that determines how this item participates in Flex, Grid, Block, or FlowRoot layout.");

        internal static readonly GUIContent LayoutType = new GUIContent(
            "Layout Type",
            "Chooses the container formatting model. Flex is best for rows/columns, Grid for two-dimensional tracks, and Block/FlowRoot for block-flow layouts.");

        internal static readonly GUIContent Direction = new GUIContent(
            "Direction",
            "For a Flex container, chooses the main axis and order of children: horizontal Row, vertical Column, or their reverse variants.");

        internal static readonly GUIContent GridFlow = new GUIContent(
            "Grid Flow",
            "Controls whether automatically placed Grid items fill rows or columns first, including dense packing variants.");

        internal static readonly GUIContent MainAxisAlignment = new GUIContent(
            "Main Axis Alignment",
            "Controls how children are distributed along the container's main axis. In a Row this is horizontal; in a Column this is vertical.");

        internal static readonly GUIContent CrossAxisAlignment = new GUIContent(
            "Cross Axis Alignment",
            "Controls how children align across the container's cross axis. In a Row this is vertical; in a Column this is horizontal.");

        internal static readonly GUIContent HorizontalGap = new GUIContent(
            "Horizontal Gap",
            "Space inserted between neighboring layout tracks or items horizontally. This does not add space around the outer edge of the container.");

        internal static readonly GUIContent VerticalGap = new GUIContent(
            "Vertical Gap",
            "Space inserted between neighboring layout tracks or items vertically. This does not add space around the outer edge of the container.");

        internal static readonly GUIContent Padding = new GUIContent(
            "Padding",
            "Inner space between the container edge and its laid-out children. Padding belongs to the container rather than individual children.");

        internal static readonly GUIContent ContainerSize = new GUIContent(
            "Container Size",
            "The current RectTransform size. TaffyLayoutGroup arranges children inside this rectangle; the RectTransform or its parent layout controls the container's own size.");

        internal static readonly GUIContent Width = new GUIContent(
            "Width",
            "Controls the item's preferred horizontal size. Auto lets layout/content determine it; point, percent, and Calc values provide explicit constraints.");

        internal static readonly GUIContent Height = new GUIContent(
            "Height",
            "Controls the item's preferred vertical size. Auto lets layout/content determine it; point, percent, and Calc values provide explicit constraints.");

        internal static readonly GUIContent Grow = new GUIContent(
            "Grow",
            "In a Flex parent, controls how much remaining main-axis space this item receives relative to sibling items. Zero means it does not claim extra space.");

        internal static readonly GUIContent AlignmentOverride = new GUIContent(
            "Alignment Override",
            "Overrides the parent alignment for this item. Auto inherits the parent's normal alignment behavior.");

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

        internal static GUIContent ForProperty(string propertyName, string fallbackLabel)
        {
            return new GUIContent(fallbackLabel, TooltipForProperty(propertyName));
        }

        internal static string TooltipForProperty(string propertyName)
        {
            switch (propertyName)
            {
                case "containerDisplay": return LayoutType.tooltip;
                case "boxSizing": return "Chooses whether explicit width and height include padding/border (Border Box) or apply only to content (Content Box).";
                case "writingDirection": return "Sets left-to-right or right-to-left writing/layout direction used when resolving logical layout behavior.";
                case "overflowX": return "Controls horizontal overflow behavior when content exceeds the available width.";
                case "overflowY": return "Controls vertical overflow behavior when content exceeds the available height.";
                case "scrollbarWidth": return "Reserves additional layout space for a scrollbar where overflow behavior requires it.";
                case "m_Padding": return Padding.tooltip;
                case "padding": return "Inner spacing between this item's content and its border box.";
                case "border": return "Border thickness on each edge. Border contributes to the box model even though TaffyUGUI does not render the border itself.";
                case "textAlign": return "Block/text alignment hint used by compatible block formatting behavior.";
                case "direction": return Direction.tooltip;
                case "wrap": return "Controls whether Flex children remain on one line or wrap onto additional lines when the main axis runs out of space.";
                case "horizontalGap": return HorizontalGap.tooltip;
                case "verticalGap": return VerticalGap.tooltip;
                case "justifyContent": return MainAxisAlignment.tooltip;
                case "alignItems": return CrossAxisAlignment.tooltip;
                case "alignContent": return "Controls how multiple Flex lines or Grid tracks are distributed across the container's cross axis when extra space exists.";
                case "justifyItems": return "Sets the default inline-axis alignment for child items where the active layout mode supports it.";
                case "gridAutoFlow": return GridFlow.tooltip;
                case "gridRows": return "Defines the container's explicit Grid row tracks.";
                case "gridColumns": return "Defines the container's explicit Grid column tracks.";
                case "gridAutoRows": return "Defines the size pattern used for implicit rows created automatically by Grid placement.";
                case "gridAutoColumns": return "Defines the size pattern used for implicit columns created automatically by Grid placement.";
                case "gridNamedLines": return "Assigns names to Grid lines so item placement can reference semantic line names instead of only numeric indices.";
                case "gridAreas": return "Defines named rectangular Grid areas that can be reused for semantic placement.";
                case "gridAreaRows": return "Declares the row count used when validating named Grid areas.";
                case "gridAreaColumns": return "Declares the column count used when validating named Grid areas.";
                case "responsiveProfiles": return "Breakpoint-driven overrides applied to selected container properties when the RectTransform size matches a profile.";
                case "safeAreaMode": return "Controls whether device safe-area insets are added to the container's resolved padding.";
                case "scrollRectContentMode": return "Controls automatic cooperation when this group is used as ScrollRect content.";
                case "pixelRounding": return "Rounds final computed layout edges before applying them to RectTransforms. Canvas Pixel uses the active Canvas scale.";
                case "maxRebuildRequestsPerFrame": return "Limits repeated layout-dirty requests in one frame to protect against rebuild loops.";
                case "display": return "Controls how this item establishes its own child formatting context, or None to remove it from layout.";
                case "position": return "Relative keeps the item in normal layout flow; Absolute positions it from inset offsets without consuming normal flow space.";
                case "inset": return "Offsets used primarily by positioned items. Auto leaves that edge unconstrained.";
                case "width": return Width.tooltip;
                case "height": return Height.tooltip;
                case "minWidth": return "Minimum width constraint applied after normal sizing resolution.";
                case "minHeight": return "Minimum height constraint applied after normal sizing resolution.";
                case "maxWidth": return "Maximum width constraint applied after normal sizing resolution.";
                case "maxHeight": return "Maximum height constraint applied after normal sizing resolution.";
                case "aspectRatio": return "Preferred width-to-height ratio. Zero disables the explicit aspect-ratio constraint.";
                case "margin": return "Outer spacing around this item. Margin separates the item from neighboring layout content.";
                case "flexBasis": return "Initial main-axis size used by Flex before remaining space is distributed by Grow and Shrink.";
                case "flexGrow": return Grow.tooltip;
                case "flexShrink": return "Controls how strongly this item gives up main-axis size when Flex items do not fit. The common default is 1.";
                case "alignSelf": return AlignmentOverride.tooltip;
                case "gridRowStart": return "Grid placement for the item's starting row line. Auto lets Grid place the item automatically.";
                case "gridRowEnd": return "Grid placement for the item's ending row line or span.";
                case "gridColumnStart": return "Grid placement for the item's starting column line. Auto lets Grid place the item automatically.";
                case "gridColumnEnd": return "Grid placement for the item's ending column line or span.";
                case "justifySelf": return AlignmentOverride.tooltip;
                case "floatMode": return "For Block/FlowRoot parents, floats the item left or right so following block content can flow around it.";
                case "clearMode": return "For Block/FlowRoot parents, prevents the item from being placed beside selected preceding floats.";
                case "measurement": return "Controls automatic intrinsic content measurement for text, images, and custom measurement providers.";
                case "forceReplacedElement": return "Treats measured content as a replaced element so intrinsic dimensions participate in sizing like an image or similar atomic element.";
                case "itemIsTable": return "Marks the item as table-like for layout behavior that needs table-item semantics.";
                default: return "TaffyUGUI layout authoring property.";
            }
        }

        internal const string SimpleModeHint =
            "Simple mode shows the controls used most often. Switch to Advanced at any time for the complete TaffyUGUI property set; both modes edit the same serialized data.";

        internal const string InactiveGroupFlexMessage =
            "This container has non-default Flex settings that are currently inactive. They are preserved and remain available in Advanced mode.";

        internal const string InactiveGroupGridMessage =
            "This container has non-default Grid settings that are currently inactive. They are preserved and remain available in Advanced mode.";

        internal const string MixedGroupDisplayMessage =
            "The selected containers use different layout types. Simple mode shows only controls that are unambiguous across the selection; use Advanced for layout-specific multi-editing.";

        internal const string InactiveFlexMessage =
            "These values remain serialized and can become active through a responsive profile or if Display changes to Flex.";

        internal const string InactiveGridMessage =
            "Grid template data is preserved but inactive until Display resolves to Grid.";

        internal const string InactiveItemFlexMessage =
            "This item has non-default Flex settings, but its current Taffy parent is not Flex. The values are preserved and are available in Advanced mode.";

        internal const string InactiveItemGridMessage =
            "This item has non-default Grid placement/alignment, but its current Taffy parent is not Grid. The values are preserved and are available in Advanced mode.";

        internal const string InactiveItemBlockMessage =
            "This item has Block/Float settings, but its current Taffy parent is not Block or FlowRoot. The values are preserved and are available in Advanced mode.";
    }
}
