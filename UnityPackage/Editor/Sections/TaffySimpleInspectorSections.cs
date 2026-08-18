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
            DrawQuickLayouts(context);

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
            DrawChildInitialization(context);

            if (context.IsSimpleMode)
                DrawInactiveSettingWarnings(context);
        }

        private static void DrawQuickLayouts(TaffyInspectorContext context)
        {
            EditorGUILayout.LabelField("Quick Layout", EditorStyles.miniBoldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Horizontal")) ApplyGroupAction(context, TaffyGroupQuickLayout.Horizontal);
                if (GUILayout.Button("Vertical")) ApplyGroupAction(context, TaffyGroupQuickLayout.Vertical);
                if (GUILayout.Button("Grid")) ApplyGroupAction(context, TaffyGroupQuickLayout.Grid);
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Centered")) ApplyGroupAction(context, TaffyGroupQuickLayout.CenteredPanel);
                if (GUILayout.Button("Toolbar")) ApplyGroupAction(context, TaffyGroupQuickLayout.Toolbar);
                if (GUILayout.Button("Cards")) ApplyGroupAction(context, TaffyGroupQuickLayout.Cards);
            }
        }

        private static void DrawChildInitialization(TaffyInspectorContext context)
        {
            if (context.IsMultiEditing || !context.Group || context.Group.transform.childCount == 0)
                return;

            EditorGUILayout.LabelField("Initialize Existing Children", EditorStyles.miniBoldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Preserve Sizes")) TaffyLayoutActions.InitializeChildren(context.Group, TaffyChildInitialization.PreserveSizes);
                if (GUILayout.Button("Stretch")) TaffyLayoutActions.InitializeChildren(context.Group, TaffyChildInitialization.Stretch);
                if (GUILayout.Button("Fit Content")) TaffyLayoutActions.InitializeChildren(context.Group, TaffyChildInitialization.FitContent);
            }
        }

        private static void ApplyGroupAction(TaffyInspectorContext context, TaffyGroupQuickLayout layout)
        {
            for (int i = 0; i < context.Targets.Length; i++)
            {
                if (context.Targets[i] is TaffyLayoutGroup group)
                    TaffyLayoutActions.ApplyQuickLayout(group, layout);
            }
            context.SerializedObject.Update();
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
            if (!context.ParentGroup && context.Item && GUILayout.Button("Add Taffy Layout Group to Parent"))
                TaffyItemActions.AddGroupToParent(context.Item);
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
            DrawQuickActions(context);
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

        private static void DrawQuickActions(TaffyInspectorContext context)
        {
            EditorGUILayout.LabelField("Quick Actions", EditorStyles.miniBoldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Fill Width")) ApplyItemAction(context, TaffyItemQuickAction.FillWidth);
                if (GUILayout.Button("Fill Parent")) ApplyItemAction(context, TaffyItemQuickAction.FillParent);
                if (GUILayout.Button("Fit Content")) ApplyItemAction(context, TaffyItemQuickAction.FitContent);
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Fixed 100")) ApplyItemAction(context, TaffyItemQuickAction.FixedSize);
                if (GUILayout.Button("Flexible")) ApplyItemAction(context, TaffyItemQuickAction.Flexible);
                if (GUILayout.Button("Spacer")) ApplyItemAction(context, TaffyItemQuickAction.Spacer);
                if (GUILayout.Button("Center")) ApplyItemAction(context, TaffyItemQuickAction.Center);
            }
        }

        private static void ApplyItemAction(TaffyInspectorContext context, TaffyItemQuickAction action)
        {
            for (int i = 0; i < context.Targets.Length; i++)
            {
                if (context.Targets[i] is TaffyLayoutItem item)
                    TaffyItemActions.Apply(item, action);
            }
            context.SerializedObject.Update();
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
