using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace TaffyUGUI.Editor
{
    internal sealed class TaffyPresetBrowserWindow : EditorWindow
    {
        private string _search = string.Empty;
        private string _category = "All";
        private Vector2 _scroll;
        private List<TaffyPresetEntry> _entries = new List<TaffyPresetEntry>();

        [MenuItem("Window/TaffyUGUI/Preset Browser")]
        internal static void Open()
        {
            TaffyPresetBrowserWindow window = GetWindow<TaffyPresetBrowserWindow>();
            window.titleContent = new GUIContent("Taffy Presets");
            window.minSize = new Vector2(360f, 300f);
            window.Refresh();
            window.Show();
        }

        internal static void OpenForSelection()
        {
            Open();
        }

        private void OnEnable()
        {
            Refresh();
        }

        private void OnProjectChange()
        {
            Refresh();
            Repaint();
        }

        private void Refresh()
        {
            _entries = TaffyPresetCatalog.LoadAll();
        }

        private void OnGUI()
        {
            DrawToolbar();
            DrawSaveCurrent();
            EditorGUILayout.Space(4f);
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            List<TaffyPresetEntry> visible = FilteredEntries();
            if (visible.Count == 0)
                EditorGUILayout.HelpBox("No presets match the current search/filter.", MessageType.Info);
            for (int i = 0; i < visible.Count; i++)
                DrawEntry(visible[i]);
            EditorGUILayout.EndScrollView();
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                _search = GUILayout.TextField(_search ?? string.Empty, GUI.skin.FindStyle("ToolbarSeachTextField") ?? EditorStyles.toolbarTextField);
                string[] categories = Categories();
                int current = Mathf.Max(0, Array.IndexOf(categories, _category));
                int next = EditorGUILayout.Popup(current, categories, EditorStyles.toolbarPopup, GUILayout.Width(110f));
                _category = categories[Mathf.Clamp(next, 0, categories.Length - 1)];
                if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(60f)))
                    Refresh();
            }
        }

        private void DrawSaveCurrent()
        {
            UnityEngine.Object current = CurrentTarget();
            using (new EditorGUI.DisabledScope(!current))
            {
                string target = current is TaffyLayoutGroup ? "Container" : current is TaffyLayoutItem ? "Item" : "Selection";
                if (GUILayout.Button("Save Current " + target + " As Project Preset"))
                    SaveCurrent(current);
            }
        }

        private void DrawEntry(TaffyPresetEntry entry)
        {
            if (entry?.Data == null)
                return;

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label(entry.Data.Preview ?? string.Empty, GUILayout.Width(92f), GUILayout.Height(34f));
                    using (new EditorGUILayout.VerticalScope())
                    {
                        EditorGUILayout.LabelField(entry.Data.DisplayName, EditorStyles.boldLabel);
                        EditorGUILayout.LabelField(entry.Data.Category + " • " + entry.Data.TargetKind, EditorStyles.miniLabel);
                    }
                }

                if (!string.IsNullOrEmpty(entry.Data.Description))
                    EditorGUILayout.LabelField(entry.Data.Description, EditorStyles.wordWrappedMiniLabel);

                using (new EditorGUILayout.HorizontalScope())
                {
                    bool compatible = CompatibleSelectionCount(entry.Data) > 0;
                    using (new EditorGUI.DisabledScope(!compatible))
                    {
                        if (GUILayout.Button("Apply to Selection"))
                            ApplyToSelection(entry.Data);
                    }

                    if (entry.IsProjectPreset && GUILayout.Button("Open", GUILayout.Width(64f)))
                    {
                        Selection.activeObject = entry.Asset;
                        EditorGUIUtility.PingObject(entry.Asset);
                    }
                }
            }
        }

        private List<TaffyPresetEntry> FilteredEntries()
        {
            string search = (_search ?? string.Empty).Trim();
            return _entries.Where(entry =>
            {
                if (entry?.Data == null)
                    return false;
                if (_category != "All" && entry.Data.Category != _category)
                    return false;
                if (string.IsNullOrEmpty(search))
                    return true;
                return entry.Data.DisplayName.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0 ||
                       entry.Data.Category.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0 ||
                       (entry.Data.Description ?? string.Empty).IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;
            }).ToList();
        }

        private string[] Categories()
        {
            List<string> categories = _entries
                .Where(entry => entry?.Data != null && !string.IsNullOrEmpty(entry.Data.Category))
                .Select(entry => entry.Data.Category)
                .Distinct()
                .OrderBy(value => value)
                .ToList();
            categories.Insert(0, "All");
            return categories.ToArray();
        }

        private static UnityEngine.Object CurrentTarget()
        {
            if (Selection.activeGameObject)
            {
                TaffyLayoutGroup group = Selection.activeGameObject.GetComponent<TaffyLayoutGroup>();
                if (group)
                    return group;
                TaffyLayoutItem item = Selection.activeGameObject.GetComponent<TaffyLayoutItem>();
                if (item)
                    return item;
            }
            return Selection.activeObject is TaffyLayoutGroup || Selection.activeObject is TaffyLayoutItem
                ? Selection.activeObject
                : null;
        }

        private static int CompatibleSelectionCount(TaffyAuthoringPresetData data)
        {
            if (data == null)
                return 0;
            int count = 0;
            GameObject[] selected = Selection.gameObjects;
            for (int i = 0; i < selected.Length; i++)
            {
                UnityEngine.Object target = data.TargetKind == TaffyPresetTargetKind.Container
                    ? (UnityEngine.Object)selected[i].GetComponent<TaffyLayoutGroup>()
                    : selected[i].GetComponent<TaffyLayoutItem>();
                if (target)
                    count++;
            }
            return count;
        }

        private static void ApplyToSelection(TaffyAuthoringPresetData data)
        {
            var targets = new List<UnityEngine.Object>();
            GameObject[] selected = Selection.gameObjects;
            for (int i = 0; i < selected.Length; i++)
            {
                UnityEngine.Object target = data.TargetKind == TaffyPresetTargetKind.Container
                    ? (UnityEngine.Object)selected[i].GetComponent<TaffyLayoutGroup>()
                    : selected[i].GetComponent<TaffyLayoutItem>();
                if (target)
                    targets.Add(target);
            }
            TaffyPresetApplication.Apply(data, targets);
        }

        private void SaveCurrent(UnityEngine.Object current)
        {
            if (!current)
                return;
            string defaultName = current.name + " Preset";
            string path = EditorUtility.SaveFilePanelInProject(
                "Save Taffy Preset",
                defaultName,
                "asset",
                "Choose where to save the project-owned apply-once preset.");
            if (string.IsNullOrEmpty(path))
                return;
            TaffyProjectPreset preset = TaffyPresetCapture.SaveProjectPreset(current, path, defaultName);
            if (preset)
            {
                Selection.activeObject = preset;
                EditorGUIUtility.PingObject(preset);
                Refresh();
            }
        }
    }

    [CustomEditor(typeof(TaffyProjectPreset))]
    internal sealed class TaffyProjectPresetEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            SerializedProperty data = serializedObject.FindProperty("data");
            if (data != null)
            {
                EditorGUILayout.PropertyField(data.FindPropertyRelative("displayName"));
                EditorGUILayout.PropertyField(data.FindPropertyRelative("category"));
                EditorGUILayout.PropertyField(data.FindPropertyRelative("description"));
                EditorGUILayout.PropertyField(data.FindPropertyRelative("preview"));
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.PropertyField(data.FindPropertyRelative("targetKind"));
                    EditorGUILayout.PropertyField(data.FindPropertyRelative("ownedPropertyPaths"), true);
                }
            }
            serializedObject.ApplyModifiedProperties();

            if (GUILayout.Button("Apply to Current Selection"))
                ApplyAssetToSelection((TaffyProjectPreset)target);
        }

        private static void ApplyAssetToSelection(TaffyProjectPreset preset)
        {
            if (!preset)
                return;
            var targets = new List<UnityEngine.Object>();
            GameObject[] selected = Selection.gameObjects;
            for (int i = 0; i < selected.Length; i++)
            {
                UnityEngine.Object candidate = preset.Data.TargetKind == TaffyPresetTargetKind.Container
                    ? (UnityEngine.Object)selected[i].GetComponent<TaffyLayoutGroup>()
                    : selected[i].GetComponent<TaffyLayoutItem>();
                if (candidate)
                    targets.Add(candidate);
            }
            TaffyPresetApplication.Apply(preset.Data, targets);
        }
    }
}
