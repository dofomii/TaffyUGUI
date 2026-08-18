using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace TaffyUGUI.Editor
{
    internal sealed class TaffyGroupFormattingSection : TaffyInspectorSection
    {
        internal static readonly string[] Properties =
        {
            "containerDisplay", "boxSizing", "writingDirection", "overflowX", "overflowY", "scrollbarWidth",
            "m_Padding", "border", "textAlign",
        };

        internal TaffyGroupFormattingSection()
            : base("Group", "Formatting", TaffyEditorContent.FormattingContext, true)
        {
        }

        protected override void DrawContent(TaffyInspectorContext context)
        {
            TaffySerializedPropertyUtility.DrawProperties(context.SerializedObject, Properties);
        }
    }

    internal sealed class TaffyGroupFlexSection : TaffyInspectorSection
    {
        internal static readonly string[] Properties =
        {
            "direction", "wrap", "horizontalGap", "verticalGap", "justifyContent", "alignItems", "alignContent", "justifyItems",
        };

        internal TaffyGroupFlexSection()
            : base("Group", "Flex", TaffyEditorContent.FlexAlignment, true)
        {
        }

        protected override void DrawContent(TaffyInspectorContext context)
        {
            TaffySerializedPropertyUtility.DrawProperties(context.SerializedObject, Properties);
            if (context.ResolvedAuthoringDisplay != TaffyContainerDisplay.Flex)
                EditorGUILayout.HelpBox(TaffyEditorContent.InactiveFlexMessage, MessageType.Info);
        }
    }

    internal sealed class TaffyGroupGridSection : TaffyInspectorSection
    {
        internal static readonly string[] Properties =
        {
            "gridAutoFlow", "gridRows", "gridColumns", "gridAutoRows", "gridAutoColumns", "gridNamedLines", "gridAreas", "gridAreaRows", "gridAreaColumns",
        };

        internal TaffyGroupGridSection()
            : base("Group", "Grid", TaffyEditorContent.GridAuthoring, true)
        {
        }

        protected override void DrawContent(TaffyInspectorContext context)
        {
            TaffySerializedPropertyUtility.DrawProperties(context.SerializedObject, Properties);
            if (context.ResolvedAuthoringDisplay != TaffyContainerDisplay.Grid)
                EditorGUILayout.HelpBox(TaffyEditorContent.InactiveGridMessage, MessageType.Info);
        }
    }

    internal sealed class TaffyGroupResponsiveSection : TaffyInspectorSection
    {
        internal static readonly string[] Properties =
        {
            "responsiveProfiles", "safeAreaMode", "scrollRectContentMode", "pixelRounding", "maxRebuildRequestsPerFrame",
        };

        internal TaffyGroupResponsiveSection()
            : base("Group", "Responsive", TaffyEditorContent.ResponsiveIntegration, true)
        {
        }

        protected override void DrawContent(TaffyInspectorContext context)
        {
            TaffySerializedPropertyUtility.DrawProperties(context.SerializedObject, Properties);
        }
    }

    internal sealed class TaffyGroupDiagnosticsSection : TaffyInspectorSection
    {
        internal TaffyGroupDiagnosticsSection()
            : base("Group", "Diagnostics", TaffyEditorContent.LiveDiagnostics, true)
        {
        }

        internal override bool IsRelevant(TaffyInspectorContext context)
        {
            return context != null && !context.IsMultiEditing && context.Group;
        }

        protected override void DrawContent(TaffyInspectorContext context)
        {
            TaffyLayoutGroup group = context.Group;
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.TextField("Active Profile", string.IsNullOrEmpty(group.ActiveResponsiveProfileName) ? "<base>" : group.ActiveResponsiveProfileName);
                EditorGUILayout.IntField("Suppressed Rebuilds", group.SuppressedRebuildRequestCount);
                EditorGUILayout.TextField("Grid Validation", string.IsNullOrEmpty(group.GridValidationError) ? "OK" : group.GridValidationError);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Force Rebuild"))
                {
                    group.SetLayoutDirty();
                    if (group.transform is RectTransform rect)
                        LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
                    SceneView.RepaintAll();
                }

                if (GUILayout.Button("Reset Rebuild Counters"))
                    group.ResetRebuildDiagnostics();
            }

            if (group.containerDisplay == TaffyContainerDisplay.Grid && GUILayout.Button("Read Grid Diagnostics"))
            {
                if (group.TryGetGridDiagnostics(out TaffyGridDiagnostics diagnostics, out string error))
                {
                    EditorUtility.DisplayDialog(
                        "Taffy Grid Diagnostics",
                        $"Rows: {diagnostics.negativeImplicitRows} implicit- / {diagnostics.explicitRows} explicit / {diagnostics.positiveImplicitRows} implicit+\n" +
                        $"Columns: {diagnostics.negativeImplicitColumns} implicit- / {diagnostics.explicitColumns} explicit / {diagnostics.positiveImplicitColumns} implicit+\n" +
                        $"Items: {diagnostics.items.Length}",
                        "OK");
                }
                else
                {
                    EditorUtility.DisplayDialog("Taffy Grid Diagnostics", error, "OK");
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Open Debugger"))
                    TaffyLayoutDebuggerWindow.Open();
                if (GUILayout.Button(TaffySceneVisualization.Enabled ? "Hide Scene Overlay" : "Show Scene Overlay"))
                    TaffySceneVisualization.Enabled = !TaffySceneVisualization.Enabled;
            }
        }
    }
}
