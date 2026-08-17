using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TaffyUGUI.Editor
{
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
                GUILayout.Label("Runtime/Editor diagnostics", EditorStyles.miniLabel);
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

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.ObjectField(group, typeof(TaffyLayoutGroup), true);
                if (GUILayout.Button("Select", GUILayout.Width(56f)))
                {
                    Selection.activeObject = group.gameObject;
                    EditorGUIUtility.PingObject(group.gameObject);
                }
            }

            RectTransform rect = group.transform as RectTransform;
            Vector2 size = rect ? rect.rect.size : Vector2.zero;
            EditorGUILayout.LabelField("Display", group.containerDisplay.ToString());
            EditorGUILayout.LabelField("Rect", $"{size.x:0.##} × {size.y:0.##}");
            EditorGUILayout.LabelField("Active Profile", string.IsNullOrEmpty(group.ActiveResponsiveProfileName) ? "<base>" : group.ActiveResponsiveProfileName);
            EditorGUILayout.LabelField("Layout Input", $"min {group.minWidth:0.##} × {group.minHeight:0.##}, preferred {group.preferredWidth:0.##} × {group.preferredHeight:0.##}");
            EditorGUILayout.LabelField("Suppressed Rebuilds", group.SuppressedRebuildRequestCount.ToString());

            if (!group.ValidateResponsiveProfiles(out string responsiveError))
                EditorGUILayout.HelpBox(responsiveError, MessageType.Error);
            if (group.containerDisplay == TaffyContainerDisplay.Grid)
            {
                if (!group.ValidateGridAuthoring(out string gridError))
                    EditorGUILayout.HelpBox(gridError, MessageType.Error);
                else if (group.TryGetGridDiagnostics(out TaffyGridDiagnostics diagnostics, out string diagnosticsError))
                {
                    EditorGUILayout.LabelField("Grid Rows", $"{diagnostics.rowTrackSizes.Length} tracks ({diagnostics.explicitRows} explicit)");
                    EditorGUILayout.LabelField("Grid Columns", $"{diagnostics.columnTrackSizes.Length} tracks ({diagnostics.explicitColumns} explicit)");
                    EditorGUILayout.LabelField("Grid Items", diagnostics.items.Length.ToString());
                    EditorGUILayout.LabelField("Row Sizes", Join(diagnostics.rowTrackSizes));
                    EditorGUILayout.LabelField("Column Sizes", Join(diagnostics.columnTrackSizes));
                }
                else
                {
                    EditorGUILayout.HelpBox(diagnosticsError, MessageType.Warning);
                }
            }

            string[] warnings = group.GetIntegrationWarnings();
            for (int i = 0; i < warnings.Length; i++)
                EditorGUILayout.HelpBox(warnings[i], MessageType.Warning);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Rebuild"))
                {
                    group.SetLayoutDirty();
                    if (rect)
                        LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
                    SceneView.RepaintAll();
                }
                if (GUILayout.Button("Reset Counters"))
                    group.ResetRebuildDiagnostics();
            }
            EditorGUILayout.EndVertical();
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
