using System;
using UnityEditor;
using UnityEngine;

namespace TaffyUGUI.Editor
{
    internal static class TaffyInspectorUtility
    {
        internal static void DrawProperties(SerializedObject serializedObject, params string[] names)
        {
            for (int i = 0; i < names.Length; i++)
            {
                SerializedProperty property = serializedObject.FindProperty(names[i]);
                if (property != null)
                    EditorGUILayout.PropertyField(property, true);
            }
        }

        internal static void Section(string title)
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        }

        internal static void DrawValidation(TaffyLayoutGroup group)
        {
            if (!group.ValidateResponsiveProfiles(out string responsiveError))
                EditorGUILayout.HelpBox(responsiveError, MessageType.Error);
            if (group.containerDisplay == TaffyContainerDisplay.Grid && !group.ValidateGridAuthoring(out string gridError))
                EditorGUILayout.HelpBox(gridError, MessageType.Error);

            string[] warnings = group.GetIntegrationWarnings();
            for (int i = 0; i < warnings.Length; i++)
                EditorGUILayout.HelpBox(warnings[i], MessageType.Warning);
        }
    }

    [CustomEditor(typeof(TaffyLayoutGroup)), CanEditMultipleObjects]
    public sealed class TaffyLayoutGroupEditor : UnityEditor.Editor
    {
        private bool _showFormatting = true;
        private bool _showFlex = true;
        private bool _showGrid = true;
        private bool _showResponsive = true;
        private bool _showDiagnostics = true;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            SerializedProperty script = serializedObject.FindProperty("m_Script");
            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.PropertyField(script);

            _showFormatting = EditorGUILayout.BeginFoldoutHeaderGroup(_showFormatting, "Formatting Context");
            if (_showFormatting)
            {
                TaffyInspectorUtility.DrawProperties(serializedObject,
                    "containerDisplay", "boxSizing", "writingDirection", "overflowX", "overflowY", "scrollbarWidth",
                    "m_Padding", "border", "textAlign");
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            SerializedProperty displayProperty = serializedObject.FindProperty("containerDisplay");
            TaffyContainerDisplay display = displayProperty != null
                ? (TaffyContainerDisplay)displayProperty.enumValueIndex
                : TaffyContainerDisplay.Flex;

            _showFlex = EditorGUILayout.BeginFoldoutHeaderGroup(_showFlex, "Flex / Alignment");
            if (_showFlex)
            {
                TaffyInspectorUtility.DrawProperties(serializedObject,
                    "direction", "wrap", "horizontalGap", "verticalGap", "justifyContent", "alignItems", "alignContent", "justifyItems");
                if (display != TaffyContainerDisplay.Flex)
                    EditorGUILayout.HelpBox("These values remain serialized and can become active through a responsive profile or if Display changes to Flex.", MessageType.Info);
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            _showGrid = EditorGUILayout.BeginFoldoutHeaderGroup(_showGrid, "Grid Authoring");
            if (_showGrid)
            {
                TaffyInspectorUtility.DrawProperties(serializedObject,
                    "gridAutoFlow", "gridRows", "gridColumns", "gridAutoRows", "gridAutoColumns", "gridNamedLines", "gridAreas", "gridAreaRows", "gridAreaColumns");
                if (display != TaffyContainerDisplay.Grid)
                    EditorGUILayout.HelpBox("Grid template data is preserved but inactive until Display resolves to Grid.", MessageType.Info);
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            _showResponsive = EditorGUILayout.BeginFoldoutHeaderGroup(_showResponsive, "Responsive / Integration");
            if (_showResponsive)
            {
                TaffyInspectorUtility.DrawProperties(serializedObject,
                    "responsiveProfiles", "safeAreaMode", "scrollRectContentMode", "pixelRounding", "maxRebuildRequestsPerFrame");
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            serializedObject.ApplyModifiedProperties();

            if (targets.Length == 1)
            {
                TaffyLayoutGroup group = (TaffyLayoutGroup)target;
                TaffyInspectorUtility.DrawValidation(group);

                _showDiagnostics = EditorGUILayout.BeginFoldoutHeaderGroup(_showDiagnostics, "Live Diagnostics");
                if (_showDiagnostics)
                {
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
                                UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
                            SceneView.RepaintAll();
                        }
                        if (GUILayout.Button("Reset Rebuild Counters"))
                            group.ResetRebuildDiagnostics();
                    }

                    if (group.containerDisplay == TaffyContainerDisplay.Grid && GUILayout.Button("Read Grid Diagnostics"))
                    {
                        if (group.TryGetGridDiagnostics(out TaffyGridDiagnostics diagnostics, out string error))
                        {
                            EditorUtility.DisplayDialog("Taffy Grid Diagnostics",
                                $"Rows: {diagnostics.negativeImplicitRows} implicit- / {diagnostics.explicitRows} explicit / {diagnostics.positiveImplicitRows} implicit+\n" +
                                $"Columns: {diagnostics.negativeImplicitColumns} implicit- / {diagnostics.explicitColumns} explicit / {diagnostics.positiveImplicitColumns} implicit+\n" +
                                $"Items: {diagnostics.items.Length}", "OK");
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
                EditorGUILayout.EndFoldoutHeaderGroup();
            }
        }
    }

    [CustomEditor(typeof(TaffyLayoutItem)), CanEditMultipleObjects]
    public sealed class TaffyLayoutItemEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Script"));

            TaffyInspectorUtility.Section("Display");
            TaffyInspectorUtility.DrawProperties(serializedObject, "display", "boxSizing", "writingDirection", "overflowX", "overflowY", "scrollbarWidth");
            TaffyInspectorUtility.Section("Position and Size");
            TaffyInspectorUtility.DrawProperties(serializedObject, "position", "inset", "width", "height", "minWidth", "minHeight", "maxWidth", "maxHeight", "aspectRatio");
            TaffyInspectorUtility.Section("Box Model");
            TaffyInspectorUtility.DrawProperties(serializedObject, "margin", "padding", "border");
            TaffyInspectorUtility.Section("Flex Item");
            TaffyInspectorUtility.DrawProperties(serializedObject, "flexBasis", "flexGrow", "flexShrink", "alignSelf");
            TaffyInspectorUtility.Section("Grid Item");
            TaffyInspectorUtility.DrawProperties(serializedObject, "gridRowStart", "gridRowEnd", "gridColumnStart", "gridColumnEnd", "justifySelf");
            TaffyInspectorUtility.Section("Block / Float");
            TaffyInspectorUtility.DrawProperties(serializedObject, "floatMode", "clearMode", "textAlign");
            TaffyInspectorUtility.Section("Intrinsic Measurement");
            TaffyInspectorUtility.DrawProperties(serializedObject, "measurement", "forceReplacedElement", "itemIsTable");

            serializedObject.ApplyModifiedProperties();

            if (targets.Length == 1)
            {
                var item = (TaffyLayoutItem)target;
                TaffyLayoutGroup parent = item.GetComponentInParent<TaffyLayoutGroup>();
                if (parent && parent.containerDisplay == TaffyContainerDisplay.Grid && !parent.ValidateGridAuthoring(out string error))
                    EditorGUILayout.HelpBox(error, MessageType.Error);
                if (GUILayout.Button("Invalidate Measurement"))
                    item.InvalidateMeasurement();
            }
        }
    }
}
