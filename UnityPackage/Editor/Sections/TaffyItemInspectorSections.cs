using UnityEditor;
using UnityEngine;

namespace TaffyUGUI.Editor
{
    internal sealed class TaffyItemDisplaySection : TaffyInspectorSection
    {
        internal static readonly string[] Properties = { "display", "boxSizing", "writingDirection", "overflowX", "overflowY", "scrollbarWidth" };
        internal TaffyItemDisplaySection() : base("Item", "Display", TaffyEditorContent.Display, false) { }
        protected override void DrawContent(TaffyInspectorContext context) => TaffySerializedPropertyUtility.DrawProperties(context.SerializedObject, Properties);
    }

    internal sealed class TaffyItemPositionSizeSection : TaffyInspectorSection
    {
        internal static readonly string[] Properties = { "position", "inset", "width", "height", "minWidth", "minHeight", "maxWidth", "maxHeight", "aspectRatio" };
        internal TaffyItemPositionSizeSection() : base("Item", "PositionSize", TaffyEditorContent.PositionAndSize, false) { }
        protected override void DrawContent(TaffyInspectorContext context)
        {
            TaffySerializedPropertyUtility.DrawProperties(context.SerializedObject, Properties);
            EditorGUILayout.LabelField("Size", TaffyInspectorSummaryUtility.SizeSummary(context.SerializedObject), EditorStyles.miniLabel);
        }
    }

    internal sealed class TaffyItemBoxModelSection : TaffyInspectorSection
    {
        internal static readonly string[] Properties = { "margin", "padding", "border" };
        internal TaffyItemBoxModelSection() : base("Item", "BoxModel", TaffyEditorContent.BoxModel, false) { }
        protected override void DrawContent(TaffyInspectorContext context)
        {
            TaffySerializedPropertyUtility.DrawProperties(context.SerializedObject, Properties);
            EditorGUILayout.LabelField("Margin", TaffyInspectorSummaryUtility.EdgesSummary(context.SerializedObject, "margin"), EditorStyles.miniLabel);
            EditorGUILayout.LabelField("Padding", TaffyInspectorSummaryUtility.EdgesSummary(context.SerializedObject, "padding"), EditorStyles.miniLabel);
            EditorGUILayout.LabelField("Border", TaffyInspectorSummaryUtility.EdgesSummary(context.SerializedObject, "border"), EditorStyles.miniLabel);
        }
    }

    internal sealed class TaffyItemFlexSection : TaffyInspectorSection
    {
        internal static readonly string[] Properties = { "flexBasis", "flexGrow", "flexShrink", "alignSelf" };
        internal TaffyItemFlexSection() : base("Item", "Flex", TaffyEditorContent.FlexItem, false) { }
        protected override void DrawContent(TaffyInspectorContext context)
        {
            TaffySerializedPropertyUtility.DrawProperties(context.SerializedObject, Properties);
            EditorGUILayout.LabelField("Flex", TaffyInspectorSummaryUtility.FlexSummary(context.SerializedObject), EditorStyles.miniLabel);
        }
    }

    internal sealed class TaffyItemGridSection : TaffyInspectorSection
    {
        internal static readonly string[] Properties = { "gridRowStart", "gridRowEnd", "gridColumnStart", "gridColumnEnd", "justifySelf" };
        internal TaffyItemGridSection() : base("Item", "Grid", TaffyEditorContent.GridItem, false) { }
                protected override void DrawContent(TaffyInspectorContext context)
        {
            TaffyGridAuthoringGUI.DrawItem(context);
            EditorGUILayout.LabelField("Placement", TaffyInspectorSummaryUtility.GridPlacementSummary(context.SerializedObject), EditorStyles.miniLabel);
        }
    }

    internal sealed class TaffyItemBlockSection : TaffyInspectorSection
    {
        internal static readonly string[] Properties = { "floatMode", "clearMode", "textAlign" };
        internal TaffyItemBlockSection() : base("Item", "Block", TaffyEditorContent.BlockFloat, false) { }
        protected override void DrawContent(TaffyInspectorContext context) => TaffySerializedPropertyUtility.DrawProperties(context.SerializedObject, Properties);
    }

    internal sealed class TaffyItemMeasurementSection : TaffyInspectorSection
    {
        internal static readonly string[] Properties = { "measurement", "forceReplacedElement", "itemIsTable" };
        internal TaffyItemMeasurementSection() : base("Item", "Measurement", TaffyEditorContent.IntrinsicMeasurement, false) { }
        protected override void DrawContent(TaffyInspectorContext context) => TaffySerializedPropertyUtility.DrawProperties(context.SerializedObject, Properties);
    }

    internal sealed class TaffyItemPostSection : TaffyInspectorSection
    {
        internal TaffyItemPostSection() : base("Item", "Post", GUIContent.none, false, false) { }

        internal override bool IsRelevant(TaffyInspectorContext context)
        {
            return context != null && !context.IsMultiEditing && context.Item;
        }

        protected override void DrawContent(TaffyInspectorContext context)
        {
            TaffyLayoutItem item = context.Item;
            TaffyLayoutGroup parent = item.GetComponentInParent<TaffyLayoutGroup>();
            if (parent && parent.containerDisplay == TaffyContainerDisplay.Grid && !parent.ValidateGridAuthoring(out string error))
                EditorGUILayout.HelpBox(error, MessageType.Error);

            if (GUILayout.Button("Invalidate Measurement"))
                item.InvalidateMeasurement();
        }
    }
}
