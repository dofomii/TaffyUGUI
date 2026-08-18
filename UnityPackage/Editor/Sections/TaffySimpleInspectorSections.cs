using UnityEditor;
using UnityEngine;

namespace TaffyUGUI.Editor
{
    internal sealed class TaffyGroupQuickSetupSection : TaffyInspectorSection
    {
        internal static readonly string[] SimplePropertyCoverage =
        {
            "containerDisplay", "direction", "gridAutoFlow", "justifyContent", "alignItems", "horizontalGap", "verticalGap", "m_Padding",
        };

        internal TaffyGroupQuickSetupSection()
            : base("Group", "QuickSetup", TaffyEditorContent.QuickSetup, false)
        {
        }

        protected override void DrawContent(TaffyInspectorContext context)
        {
            SerializedObject serializedObject = context.SerializedObject;
            TaffySerializedPropertyUtility.DrawProperty(serializedObject, "containerDisplay", TaffyEditorContent.LayoutType);

            if (TaffyInspectorVisibility.GroupShowsFlexEssentials(context))
            {
                SerializedProperty directionProperty = serializedObject.FindProperty("direction");
                TaffyVisualAuthoringControls.DrawDirection(directionProperty, TaffyEditorContent.Direction);
                if (directionProperty != null && !directionProperty.hasMultipleDifferentValues)
                    EditorGUILayout.LabelField("Flow", TaffyInspectorVisibility.FlexDirectionSummary((TaffyFlexDirection)directionProperty.intValue), EditorStyles.miniLabel);

                TaffyVisualAuthoringControls.DrawJustify(serializedObject.FindProperty("justifyContent"), TaffyEditorContent.MainAxisAlignment);
                TaffyVisualAuthoringControls.DrawAlign(serializedObject.FindProperty("alignItems"), TaffyEditorContent.CrossAxisAlignment);
                TaffySerializedPropertyUtility.DrawProperty(serializedObject, "horizontalGap", TaffyEditorContent.HorizontalGap);
                TaffySerializedPropertyUtility.DrawProperty(serializedObject, "verticalGap", TaffyEditorContent.VerticalGap);
            }
            else if (TaffyInspectorVisibility.GroupShowsGridEssentials(context))
            {
                TaffySerializedPropertyUtility.DrawProperty(serializedObject, "gridAutoFlow", TaffyEditorContent.GridFlow);
                TaffyVisualAuthoringControls.DrawJustify(serializedObject.FindProperty("justifyContent"), TaffyEditorContent.MainAxisAlignment);
                TaffyVisualAuthoringControls.DrawAlign(serializedObject.FindProperty("alignItems"), TaffyEditorContent.CrossAxisAlignment);
                TaffySerializedPropertyUtility.DrawProperty(serializedObject, "horizontalGap", TaffyEditorContent.HorizontalGap);
                TaffySerializedPropertyUtility.DrawProperty(serializedObject, "verticalGap", TaffyEditorContent.VerticalGap);
            }

            TaffySerializedPropertyUtility.DrawProperty(serializedObject, "m_Padding", TaffyEditorContent.Padding);
            DrawContainerSize(context);

            if (context.IsSimpleMode)
                DrawInactiveSettingWarnings(context);
        }

        private static void DrawInactiveSettingWarnings(TaffyInspectorContext context)
        {
            if (TaffyInspectorVisibility.GroupHasMixedDisplay(context))
                EditorGUILayout.HelpBox(TaffyEditorContent.MixedGroupDisplayMessage, MessageType.Info);
            if (TaffyInspectorVisibility.HasInactiveGroupFlexSettings(context))
                EditorGUILayout.HelpBox(TaffyEditorContent.InactiveGroupFlexMessage, MessageType.Info);
            if (TaffyInspectorVisibility.HasInactiveGroupGridSettings(context))
                EditorGUILayout.HelpBox(TaffyEditorContent.InactiveGroupGridMessage, MessageType.Info);
        }

        private static void DrawContainerSize(TaffyInspectorContext context)
        {
            using (new EditorGUI.DisabledScope(true))
            {
                if (context.IsMultiEditing)
                {
                    EditorGUILayout.TextField(TaffyEditorContent.ContainerSize, "Multiple RectTransforms");
                    return;
                }

                RectTransform rect = context.Group ? context.Group.transform as RectTransform : null;
                if (!rect)
                {
                    EditorGUILayout.TextField(TaffyEditorContent.ContainerSize, "Unavailable");
                    return;
                }

                Vector2 size = rect.rect.size;
                EditorGUILayout.TextField(TaffyEditorContent.ContainerSize, $"{size.x:0.##} × {size.y:0.##}  (RectTransform)");
            }
        }
    }

    internal sealed class TaffyItemParentSummarySection : TaffyInspectorSection
    {
        internal TaffyItemParentSummarySection()
            : base("Item", "ParentSummary", TaffyEditorContent.ParentLayout, false)
        {
        }

        protected override void DrawContent(TaffyInspectorContext context)
        {
            if (context.IsMultiEditing)
            {
                EditorGUILayout.HelpBox("Multiple items selected. Parent-dependent controls are shown only when they are unambiguous.", MessageType.Info);
                return;
            }

            string summary = TaffyInspectorVisibility.ParentSummary(context);
            MessageType type = context.ParentGroup ? MessageType.Info : MessageType.Warning;
            EditorGUILayout.HelpBox(summary, type);
        }
    }

    internal sealed class TaffyItemEssentialsSection : TaffyInspectorSection
    {
        internal static readonly string[] SimplePropertyCoverage =
        {
            "width", "height", "flexGrow", "alignSelf", "justifySelf",
        };

        internal TaffyItemEssentialsSection()
            : base("Item", "Essentials", TaffyEditorContent.ItemEssentials, false)
        {
        }

        protected override void DrawContent(TaffyInspectorContext context)
        {
            SerializedObject serializedObject = context.SerializedObject;
            TaffySerializedPropertyUtility.DrawProperty(serializedObject, "width", TaffyEditorContent.Width);
            TaffySerializedPropertyUtility.DrawProperty(serializedObject, "height", TaffyEditorContent.Height);
            EditorGUILayout.LabelField("Size", TaffyInspectorSummaryUtility.SizeSummary(serializedObject), EditorStyles.miniLabel);

            if (!context.IsMultiEditing && TaffyInspectorVisibility.ParentIsFlex(context))
            {
                TaffySerializedPropertyUtility.DrawProperty(serializedObject, "flexGrow", TaffyEditorContent.Grow);
                TaffySerializedPropertyUtility.DrawProperty(serializedObject, "alignSelf", TaffyEditorContent.AlignmentOverride);
                EditorGUILayout.LabelField("Flex", TaffyInspectorSummaryUtility.FlexSummary(serializedObject), EditorStyles.miniLabel);
                EditorGUILayout.HelpBox(TaffyInspectorVisibility.FlexGrowHelp(context), MessageType.None);
            }
            else if (!context.IsMultiEditing && TaffyInspectorVisibility.ParentIsGrid(context))
            {
                TaffySerializedPropertyUtility.DrawProperty(serializedObject, "justifySelf", TaffyEditorContent.AlignmentOverride);
                EditorGUILayout.LabelField("Placement", TaffyInspectorSummaryUtility.GridPlacementSummary(serializedObject), EditorStyles.miniLabel);
                EditorGUILayout.HelpBox("This item is inside a Grid parent. Grid placement and track controls are available in Advanced mode.", MessageType.None);
            }
            else if (!context.IsMultiEditing && TaffyInspectorVisibility.ParentIsBlockLike(context))
            {
                EditorGUILayout.HelpBox("This item is inside a Block/FlowRoot parent. Float and Clear controls are available in Advanced mode.", MessageType.None);
            }

            DrawInactiveSettingWarnings(context);
        }

        private static void DrawInactiveSettingWarnings(TaffyInspectorContext context)
        {
            if (context.IsMultiEditing)
                return;

            if (TaffyInspectorVisibility.HasInactiveFlexOverrides(context))
                EditorGUILayout.HelpBox(TaffyEditorContent.InactiveItemFlexMessage, MessageType.Info);
            if (TaffyInspectorVisibility.HasInactiveGridOverrides(context))
                EditorGUILayout.HelpBox(TaffyEditorContent.InactiveItemGridMessage, MessageType.Info);
            if (TaffyInspectorVisibility.HasInactiveBlockOverrides(context))
                EditorGUILayout.HelpBox(TaffyEditorContent.InactiveItemBlockMessage, MessageType.Info);
        }
    }
}
