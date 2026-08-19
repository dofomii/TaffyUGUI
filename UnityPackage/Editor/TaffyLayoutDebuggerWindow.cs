using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TaffyUGUI.Editor
{
    internal readonly struct TaffyDebuggerData
    {
        internal TaffyDebuggerData(TaffyComputedLayoutSnapshot computed, TaffyLayoutHealth health)
        {
            Computed = computed;
            Health = health;
        }

        internal TaffyComputedLayoutSnapshot Computed { get; }
        internal TaffyLayoutHealth Health { get; }

        internal static TaffyDebuggerData From(TaffyLayoutGroup group)
        {
            return new TaffyDebuggerData(
                TaffyComputedLayoutSnapshot.From(group),
                TaffyLayoutHealth.Evaluate(group));
        }
    }

    public sealed class TaffyLayoutDebuggerWindow : EditorWindow
    {
        private Vector2 _scroll;
        private bool _selectionOnly;

        [MenuItem("Tools/TaffyUGUI/Layout Debugger")]
        public static void Open()
        {
            GetWindow<TaffyLayoutDebuggerWindow>("Taffy Debugger").Show();
        }

        private void OnGUI()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                _selectionOnly = GUILayout.Toggle(_selectionOnly, "Selection Only", EditorStyles.toolbarButton);
                if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(70f)))
                    Repaint();
                GUILayout.FlexibleSpace();
                GUILayout.Label("Shared computed state + diagnostics", EditorStyles.miniLabel);
            }

            List<TaffyLayoutGroup> groups = FindGroups();
            if (groups.Count == 0)
            {
                EditorGUILayout.HelpBox(_selectionOnly ? "No selected hierarchy contains a TaffyLayoutGroup." : "No active loaded-scene TaffyLayoutGroup components found.", MessageType.Info);
                return;
            }

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            for (int i = 0; i < groups.Count; i++)
                DrawGroup(groups[i]);
            EditorGUILayout.EndScrollView();
        }

        private static void DrawGroup(TaffyLayoutGroup group)
        {
            if (!group)
                return;

            TaffyDebuggerData data = TaffyDebuggerData.From(group);
            TaffyComputedLayoutSnapshot computed = data.Computed;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.ObjectField(group, typeof(TaffyLayoutGroup), true);
                if (GUILayout.Button("Select", GUILayout.Width(56f)))
                    Select(group, false);
                if (GUILayout.Button("Frame", GUILayout.Width(56f)))
                    Select(group, true);
            }

            if (computed.Available)
            {
                EditorGUILayout.LabelField("Position", $"{computed.Position.x:0.##}, {computed.Position.y:0.##}");
                EditorGUILayout.LabelField("Size", $"{computed.Size.x:0.##} × {computed.Size.y:0.##}");
                EditorGUILayout.LabelField("Responsive Profile", computed.ResponsiveProfile);
                EditorGUILayout.LabelField("Effective Display", computed.EffectiveDisplay.ToString());
                if (computed.EffectiveDisplay == TaffyContainerDisplay.Flex)
                    EditorGUILayout.LabelField("Effective Direction", computed.EffectiveDirection.ToString());
                if (!string.IsNullOrEmpty(computed.GridDiagnostics))
                    EditorGUILayout.LabelField("Grid State", computed.GridDiagnostics);
            }

            EditorGUILayout.LabelField("Layout Input", $"min {group.minWidth:0.##} × {group.minHeight:0.##}, preferred {group.preferredWidth:0.##} × {group.preferredHeight:0.##}");
            EditorGUILayout.LabelField("Suppressed Rebuilds", group.SuppressedRebuildRequestCount.ToString());

            DrawDiagnostics(data.Health);

            if (computed.Available && computed.EffectiveDisplay == TaffyContainerDisplay.Grid)
                DrawGridRuntimeDiagnostics(group);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Rebuild"))
                {
                    group.SetLayoutDirty();
                    if (group.transform is RectTransform rect)
                        LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
                    SceneView.RepaintAll();
                }
                if (GUILayout.Button("Reset Counters"))
                    group.ResetRebuildDiagnostics();
            }
            EditorGUILayout.EndVertical();
        }

        private static void DrawDiagnostics(TaffyLayoutHealth health)
        {
            if (health == null || health.IsHealthy)
            {
                EditorGUILayout.LabelField("Layout Health", "Healthy");
                return;
            }

            IReadOnlyList<TaffyDiagnosticResult> results = health.Results;
            for (int i = 0; i < results.Count; i++)
            {
                TaffyDiagnosticResult result = results[i];
                EditorGUILayout.HelpBox(result.Title + "\n" + result.Message, MessageTypeFor(result.Severity));
                using (new EditorGUILayout.HorizontalScope())
                {
                    for (int fixIndex = 0; fixIndex < result.Fixes.Count; fixIndex++)
                    {
                        TaffyDiagnosticFix fix = result.Fixes[fixIndex];
                        if (GUILayout.Button(fix.Label, EditorStyles.miniButton))
                            fix.Invoke();
                    }
                    if (!string.IsNullOrEmpty(result.DocumentationUrl) && GUILayout.Button("Docs", EditorStyles.miniButton, GUILayout.Width(52f)))
                        Application.OpenURL(result.DocumentationUrl);
                }
            }
        }

        private static void DrawGridRuntimeDiagnostics(TaffyLayoutGroup group)
        {
            if (group.TryGetGridDiagnostics(out TaffyGridDiagnostics diagnostics, out string diagnosticsError))
            {
                EditorGUILayout.LabelField("Grid Rows", $"{diagnostics.rowTrackSizes.Length} tracks ({diagnostics.explicitRows} explicit)");
                EditorGUILayout.LabelField("Grid Columns", $"{diagnostics.columnTrackSizes.Length} tracks ({diagnostics.explicitColumns} explicit)");
                EditorGUILayout.LabelField("Grid Items", diagnostics.items.Length.ToString());
                EditorGUILayout.LabelField("Row Sizes", Join(diagnostics.rowTrackSizes));
                EditorGUILayout.LabelField("Column Sizes", Join(diagnostics.columnTrackSizes));
            }
            else if (!string.IsNullOrEmpty(diagnosticsError))
            {
                EditorGUILayout.HelpBox(diagnosticsError, MessageType.Warning);
            }
        }

        private static MessageType MessageTypeFor(TaffyDiagnosticSeverity severity)
        {
            switch (severity)
            {
                case TaffyDiagnosticSeverity.Error: return MessageType.Error;
                case TaffyDiagnosticSeverity.Warning: return MessageType.Warning;
                default: return MessageType.Info;
            }
        }

        private static void Select(TaffyLayoutGroup group, bool frame)
        {
            if (!group)
                return;
            Selection.activeGameObject = group.gameObject;
            EditorGUIUtility.PingObject(group.gameObject);
            if (frame && SceneView.lastActiveSceneView)
                SceneView.lastActiveSceneView.FrameSelected();
        }

        private List<TaffyLayoutGroup> FindGroups()
        {
            var result = new List<TaffyLayoutGroup>();
            var seen = new HashSet<int>();
            if (_selectionOnly)
            {
                GameObject[] selection = Selection.gameObjects;
                for (int i = 0; i < selection.Length; i++)
                {
                    TaffyLayoutGroup[] nested = selection[i].GetComponentsInChildren<TaffyLayoutGroup>(true);
                    for (int j = 0; j < nested.Length; j++)
                        AddIfLoaded(nested[j], result, seen);
                    TaffyLayoutGroup parent = selection[i].GetComponentInParent<TaffyLayoutGroup>();
                    AddIfLoaded(parent, result, seen);
                }
                return result;
            }

            TaffyLayoutGroup[] all = Resources.FindObjectsOfTypeAll<TaffyLayoutGroup>();
            for (int i = 0; i < all.Length; i++)
                AddIfLoaded(all[i], result, seen);
            result.Sort((a, b) => string.CompareOrdinal(HierarchyPath(a.transform), HierarchyPath(b.transform)));
            return result;
        }

        private static void AddIfLoaded(TaffyLayoutGroup group, List<TaffyLayoutGroup> result, HashSet<int> seen)
        {
            if (!group || !group.gameObject.scene.IsValid() || !group.gameObject.scene.isLoaded)
                return;
            if (seen.Add(group.GetInstanceID()))
                result.Add(group);
        }

        private static string HierarchyPath(Transform transform)
        {
            string path = transform.name;
            while (transform.parent)
            {
                transform = transform.parent;
                path = transform.name + "/" + path;
            }
            return transform.gameObject.scene.name + "/" + path;
        }

        private static string Join(float[] values)
        {
            if (values == null || values.Length == 0)
                return "—";
            int count = Mathf.Min(values.Length, 12);
            var parts = new string[count + (values.Length > count ? 1 : 0)];
            for (int i = 0; i < count; i++)
                parts[i] = values[i].ToString("0.##");
            if (values.Length > count)
                parts[parts.Length - 1] = "…";
            return string.Join(", ", parts);
        }
    }
}
