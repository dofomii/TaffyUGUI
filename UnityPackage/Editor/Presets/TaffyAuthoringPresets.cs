using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace TaffyUGUI.Editor
{
    internal enum TaffyPresetTargetKind
    {
        Container,
        Item,
    }

    [Serializable]
    internal sealed class TaffyAuthoringPresetData
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField] private string category;
        [SerializeField] private string description;
        [SerializeField] private string preview;
        [SerializeField] private TaffyPresetTargetKind targetKind;
        [SerializeField] private string serializedJson;
        [SerializeField] private List<string> ownedPropertyPaths = new List<string>();

        internal string Id { get => id; set => id = value; }
        internal string DisplayName { get => displayName; set => displayName = value; }
        internal string Category { get => category; set => category = value; }
        internal string Description { get => description; set => description = value; }
        internal string Preview { get => preview; set => preview = value; }
        internal TaffyPresetTargetKind TargetKind { get => targetKind; set => targetKind = value; }
        internal string SerializedJson { get => serializedJson; set => serializedJson = value; }
        internal List<string> OwnedPropertyPaths => ownedPropertyPaths;
    }

    internal sealed class TaffyProjectPreset : ScriptableObject
    {
        [SerializeField] private TaffyAuthoringPresetData data = new TaffyAuthoringPresetData();
        internal TaffyAuthoringPresetData Data => data;
    }

    internal static class TaffyPresetApplication
    {
        internal static void Apply(TaffyAuthoringPresetData preset, IEnumerable<UnityEngine.Object> targets)
        {
            if (preset == null || targets == null || string.IsNullOrEmpty(preset.SerializedJson))
                return;

            List<UnityEngine.Object> valid = targets.Where(target => IsCompatible(preset, target)).ToList();
            if (valid.Count == 0)
                return;

            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Apply Taffy preset " + preset.DisplayName);
            GameObject sourceObject = null;
            try
            {
                sourceObject = new GameObject("__TaffyPresetSource", typeof(RectTransform));
                sourceObject.hideFlags = HideFlags.HideAndDontSave;
                sourceObject.SetActive(false);
                Component source = preset.TargetKind == TaffyPresetTargetKind.Container
                    ? (Component)sourceObject.AddComponent<TaffyLayoutGroup>()
                    : sourceObject.AddComponent<TaffyLayoutItem>();
                EditorJsonUtility.FromJsonOverwrite(preset.SerializedJson, source);
                var sourceSerialized = new SerializedObject(source);
                sourceSerialized.Update();

                for (int targetIndex = 0; targetIndex < valid.Count; targetIndex++)
                {
                    UnityEngine.Object target = valid[targetIndex];
                    Undo.RecordObject(target, "Apply Taffy preset");
                    var destination = new SerializedObject(target);
                    destination.Update();
                    for (int pathIndex = 0; pathIndex < preset.OwnedPropertyPaths.Count; pathIndex++)
                    {
                        SerializedProperty sourceProperty = sourceSerialized.FindProperty(preset.OwnedPropertyPaths[pathIndex]);
                        if (sourceProperty != null)
                            destination.CopyFromSerializedProperty(sourceProperty);
                    }
                    destination.ApplyModifiedProperties();
                    PrefabUtility.RecordPrefabInstancePropertyModifications(target);
                    EditorUtility.SetDirty(target);
                    if (target is TaffyLayoutGroup group)
                        group.SetLayoutDirty();
                }
            }
            finally
            {
                if (sourceObject)
                    UnityEngine.Object.DestroyImmediate(sourceObject);
                Undo.CollapseUndoOperations(undoGroup);
            }
        }

        internal static bool IsCompatible(TaffyAuthoringPresetData preset, UnityEngine.Object target)
        {
            if (preset == null || !target)
                return false;
            return preset.TargetKind == TaffyPresetTargetKind.Container
                ? target is TaffyLayoutGroup
                : target is TaffyLayoutItem;
        }
    }

    internal static class TaffyPresetCapture
    {
        internal static TaffyAuthoringPresetData Capture(UnityEngine.Object target, string displayName, string category = "Project")
        {
            if (!(target is TaffyLayoutGroup) && !(target is TaffyLayoutItem))
                return null;

            var data = new TaffyAuthoringPresetData
            {
                Id = Guid.NewGuid().ToString("N"),
                DisplayName = string.IsNullOrWhiteSpace(displayName) ? target.name + " Preset" : displayName,
                Category = string.IsNullOrWhiteSpace(category) ? "Project" : category,
                Description = "Project preset captured from " + target.name + ".",
                TargetKind = target is TaffyLayoutGroup ? TaffyPresetTargetKind.Container : TaffyPresetTargetKind.Item,
                SerializedJson = EditorJsonUtility.ToJson(target),
            };
            data.Preview = data.TargetKind == TaffyPresetTargetKind.Container ? "▣ Container" : "□ Item";
            IEnumerable<string> coverage = target is TaffyLayoutGroup
                ? TaffyLayoutGroupEditor.PropertyCoverage
                : TaffyLayoutItemEditor.PropertyCoverage;
            data.OwnedPropertyPaths.AddRange(coverage);
            return data;
        }

        internal static TaffyProjectPreset SaveProjectPreset(UnityEngine.Object target, string assetPath, string displayName = null)
        {
            TaffyAuthoringPresetData captured = Capture(target, displayName);
            if (captured == null || string.IsNullOrEmpty(assetPath))
                return null;

            TaffyProjectPreset asset = ScriptableObject.CreateInstance<TaffyProjectPreset>();
            Copy(captured, asset.Data);
            AssetDatabase.CreateAsset(asset, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(assetPath);
            return asset;
        }

        internal static void Copy(TaffyAuthoringPresetData source, TaffyAuthoringPresetData destination)
        {
            if (source == null || destination == null)
                return;
            destination.Id = source.Id;
            destination.DisplayName = source.DisplayName;
            destination.Category = source.Category;
            destination.Description = source.Description;
            destination.Preview = source.Preview;
            destination.TargetKind = source.TargetKind;
            destination.SerializedJson = source.SerializedJson;
            destination.OwnedPropertyPaths.Clear();
            destination.OwnedPropertyPaths.AddRange(source.OwnedPropertyPaths);
        }
    }

    internal static class TaffyBuiltInPresets
    {
        private static List<TaffyAuthoringPresetData> _cache;

        internal static IReadOnlyList<TaffyAuthoringPresetData> All => _cache ?? (_cache = Build());

        private static List<TaffyAuthoringPresetData> Build()
        {
            var presets = new List<TaffyAuthoringPresetData>
            {
                Container("builtin.horizontal-row", "Horizontal Row", "Containers", "→  □  □  □", new[] { "containerDisplay", "direction", "wrap" }, group =>
                {
                    group.containerDisplay = TaffyContainerDisplay.Flex;
                    group.direction = TaffyFlexDirection.Row;
                    group.wrap = TaffyFlexWrap.NoWrap;
                }),
                Container("builtin.vertical-stack", "Vertical Stack", "Containers", "↓  □\n   □\n   □", new[] { "containerDisplay", "direction", "wrap" }, group =>
                {
                    group.containerDisplay = TaffyContainerDisplay.Flex;
                    group.direction = TaffyFlexDirection.Column;
                    group.wrap = TaffyFlexWrap.NoWrap;
                }),
                Container("builtin.centered-panel", "Centered Panel", "Containers", "   □\n  [■]\n   □", new[] { "containerDisplay", "direction", "justifyContent", "alignItems" }, group =>
                {
                    group.containerDisplay = TaffyContainerDisplay.Flex;
                    group.direction = TaffyFlexDirection.Column;
                    group.justifyContent = TaffyJustify.Center;
                    group.alignItems = TaffyAlign.Center;
                }),
                Container("builtin.toolbar", "Toolbar", "Containers", "□  □       □", new[] { "containerDisplay", "direction", "wrap", "justifyContent", "alignItems", "horizontalGap" }, group =>
                {
                    group.containerDisplay = TaffyContainerDisplay.Flex;
                    group.direction = TaffyFlexDirection.Row;
                    group.wrap = TaffyFlexWrap.NoWrap;
                    group.justifyContent = TaffyJustify.SpaceBetween;
                    group.alignItems = TaffyAlign.Center;
                    group.horizontalGap = 8f;
                }),
                Container("builtin.sidebar-content", "Sidebar + Content", "Containers", "▌│████", new[] { "containerDisplay", "direction", "wrap", "alignItems", "horizontalGap" }, group =>
                {
                    group.containerDisplay = TaffyContainerDisplay.Flex;
                    group.direction = TaffyFlexDirection.Row;
                    group.wrap = TaffyFlexWrap.NoWrap;
                    group.alignItems = TaffyAlign.Stretch;
                    group.horizontalGap = 16f;
                }),
                Container("builtin.scroll-list", "Scrollable List Content", "Containers", "↕  □\n   □\n   □", new[] { "containerDisplay", "direction", "wrap", "verticalGap", "overflowY" }, group =>
                {
                    group.containerDisplay = TaffyContainerDisplay.Flex;
                    group.direction = TaffyFlexDirection.Column;
                    group.wrap = TaffyFlexWrap.NoWrap;
                    group.verticalGap = 8f;
                    group.overflowY = TaffyOverflow.Scroll;
                }),
                Container("builtin.wrapping-cards", "Responsive / Wrapping Cards", "Containers", "□ □ □\n□ □", new[] { "containerDisplay", "direction", "wrap", "alignItems", "horizontalGap", "verticalGap" }, group =>
                {
                    group.containerDisplay = TaffyContainerDisplay.Flex;
                    group.direction = TaffyFlexDirection.Row;
                    group.wrap = TaffyFlexWrap.Wrap;
                    group.alignItems = TaffyAlign.Start;
                    group.horizontalGap = 12f;
                    group.verticalGap = 12f;
                }),
                Item("builtin.flexible-item", "Flexible Item", "Items", "←  □  →", new[] { "flexBasis", "flexGrow", "flexShrink" }, item =>
                {
                    item.flexBasis = TaffyLength.Auto;
                    item.flexGrow = 1f;
                    item.flexShrink = 1f;
                }),
                Item("builtin.spacer", "Spacer", "Items", "←────→", new[] { "flexBasis", "flexGrow", "flexShrink", "measurement" }, item =>
                {
                    item.flexBasis = TaffyLength.Points(0f);
                    item.flexGrow = 1f;
                    item.flexShrink = 1f;
                    item.measurement = TaffyMeasurementMode.Disabled;
                }),
                Item("builtin.fit-content", "Fit Content Item", "Items", "[ content ]", new[] { "width", "height" }, item =>
                {
                    item.width = TaffyLength.Auto;
                    item.height = TaffyLength.Auto;
                }),
            };
            return presets;
        }

        private static TaffyAuthoringPresetData Container(string id, string name, string category, string preview, IEnumerable<string> owned, Action<TaffyLayoutGroup> configure)
        {
            GameObject go = new GameObject("__Preset", typeof(RectTransform));
            go.hideFlags = HideFlags.HideAndDontSave;
            go.SetActive(false);
            try
            {
                TaffyLayoutGroup group = go.AddComponent<TaffyLayoutGroup>();
                configure(group);
                return Create(id, name, category, preview, TaffyPresetTargetKind.Container, group, owned);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        private static TaffyAuthoringPresetData Item(string id, string name, string category, string preview, IEnumerable<string> owned, Action<TaffyLayoutItem> configure)
        {
            GameObject go = new GameObject("__Preset", typeof(RectTransform));
            go.hideFlags = HideFlags.HideAndDontSave;
            go.SetActive(false);
            try
            {
                TaffyLayoutItem item = go.AddComponent<TaffyLayoutItem>();
                configure(item);
                return Create(id, name, category, preview, TaffyPresetTargetKind.Item, item, owned);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        private static TaffyAuthoringPresetData Create(string id, string name, string category, string preview, TaffyPresetTargetKind kind, UnityEngine.Object source, IEnumerable<string> owned)
        {
            var data = new TaffyAuthoringPresetData
            {
                Id = id,
                DisplayName = name,
                Category = category,
                Description = "Built-in apply-once TaffyUGUI preset.",
                Preview = preview,
                TargetKind = kind,
                SerializedJson = EditorJsonUtility.ToJson(source),
            };
            data.OwnedPropertyPaths.AddRange(owned);
            return data;
        }
    }

    internal static class TaffyPresetCatalog
    {
        internal static List<TaffyPresetEntry> LoadAll()
        {
            var entries = new List<TaffyPresetEntry>();
            IReadOnlyList<TaffyAuthoringPresetData> builtIns = TaffyBuiltInPresets.All;
            for (int i = 0; i < builtIns.Count; i++)
                entries.Add(new TaffyPresetEntry(builtIns[i], null));

            string[] guids = AssetDatabase.FindAssets(string.Empty, new[] { "Assets" });
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (!path.EndsWith(".asset", StringComparison.OrdinalIgnoreCase))
                    continue;
                TaffyProjectPreset asset = AssetDatabase.LoadMainAssetAtPath(path) as TaffyProjectPreset;
                if (asset)
                    entries.Add(new TaffyPresetEntry(asset.Data, asset));
            }
            return entries;
        }
    }

    internal sealed class TaffyPresetEntry
    {
        internal TaffyPresetEntry(TaffyAuthoringPresetData data, TaffyProjectPreset asset)
        {
            Data = data;
            Asset = asset;
        }

        internal TaffyAuthoringPresetData Data { get; }
        internal TaffyProjectPreset Asset { get; }
        internal bool IsProjectPreset => Asset;
    }
}
