using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace TaffyUGUI.Editor
{
    internal sealed class TaffyCreationRecipe
    {
        internal TaffyCreationRecipe(string id, string name, string category, string summary, string preview)
        {
            Id = id;
            Name = name;
            Category = category;
            Summary = summary;
            Preview = preview;
        }

        internal string Id { get; }
        internal string Name { get; }
        internal string Category { get; }
        internal string Summary { get; }
        internal string Preview { get; }
    }

    internal static class TaffyCreationRecipeCatalog
    {
        private static readonly TaffyCreationRecipe[] Recipes =
        {
            new TaffyCreationRecipe("horizontal", "Horizontal Layout", "Layouts", "Flex row container for side-by-side content.", "□  □  □"),
            new TaffyCreationRecipe("vertical", "Vertical Layout", "Layouts", "Flex column container for stacked content.", "□\n□\n□"),
            new TaffyCreationRecipe("centered-panel", "Centered Panel", "Layouts", "Centered Flex container ready for a panel or dialog body.", "  [□]"),
            new TaffyCreationRecipe("toolbar", "Toolbar", "Layouts", "Horizontal toolbar with centered cross-axis alignment and spacing.", "□  □      □"),
            new TaffyCreationRecipe("sidebar-content", "Sidebar + Content", "Compositions", "Fixed sidebar with flexible content area.", "▌│████"),
            new TaffyCreationRecipe("scrollable-list", "Scrollable List", "Compositions", "Unity ScrollRect with Taffy-managed vertical content.", "↕  □\n   □\n   □"),
            new TaffyCreationRecipe("responsive-cards", "Responsive Cards", "Compositions", "Wrapping card container using the built-in responsive cards preset.", "□ □ □\n□ □"),
            new TaffyCreationRecipe("modal", "Modal", "Compositions", "Centered modal shell with backdrop and fixed starter panel.", "▒ [■] ▒"),
            new TaffyCreationRecipe("form", "Basic Form", "Compositions", "Vertical form shell with fields and an actions row.", "Field\nField\n  Actions"),
        };

        internal static IReadOnlyList<TaffyCreationRecipe> All => Recipes;

        internal static TaffyCreationRecipe Find(string id)
        {
            return Recipes.FirstOrDefault(recipe => string.Equals(recipe.Id, id, StringComparison.Ordinal));
        }

        internal static GameObject Create(string id)
        {
            TaffyCreationRecipe recipe = Find(id);
            if (recipe == null)
                return null;

            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Create " + recipe.Name);
            GameObject root = null;
            switch (id)
            {
                case "horizontal":
                    root = TaffyHierarchyActions.CreateGroup(recipe.Name, TaffyGroupQuickLayout.Horizontal);
                    break;
                case "vertical":
                    root = TaffyHierarchyActions.CreateGroup(recipe.Name, TaffyGroupQuickLayout.Vertical);
                    break;
                case "centered-panel":
                    root = TaffyHierarchyActions.CreateGroup(recipe.Name, TaffyGroupQuickLayout.CenteredPanel);
                    break;
                case "toolbar":
                    root = TaffyHierarchyActions.CreateGroup(recipe.Name, TaffyGroupQuickLayout.Toolbar);
                    break;
                case "sidebar-content":
                    root = CreateSidebarContent();
                    break;
                case "scrollable-list":
                    root = CreateScrollableList();
                    break;
                case "responsive-cards":
                    root = CreateResponsiveCards();
                    break;
                case "modal":
                    root = CreateModal();
                    break;
                case "form":
                    root = CreateForm();
                    break;
            }

            if (root)
                Selection.activeGameObject = root;
            Undo.CollapseUndoOperations(undoGroup);
            return root;
        }

        private static GameObject CreateSidebarContent()
        {
            GameObject root = TaffyHierarchyActions.CreateGroup("Sidebar + Content", TaffyGroupQuickLayout.Horizontal);
            TaffyLayoutGroup group = root.GetComponent<TaffyLayoutGroup>();
            Undo.RecordObject(group, "Configure Sidebar + Content");
            group.horizontalGap = 16f;
            TaffyLayoutActions.Finish(group);

            TaffyLayoutItem sidebar = TaffyHierarchyActions.CreateChildItem(root.transform, "Sidebar");
            Undo.RecordObject(sidebar, "Configure Sidebar");
            sidebar.width = TaffyLength.Points(240f);
            sidebar.height = TaffyLength.Percent(1f);
            sidebar.flexShrink = 0f;
            TaffyLayoutActions.Finish(sidebar);

            TaffyLayoutItem content = TaffyHierarchyActions.CreateChildItem(root.transform, "Content");
            Undo.RecordObject(content, "Configure Content");
            content.width = TaffyLength.Auto;
            content.height = TaffyLength.Percent(1f);
            content.flexGrow = 1f;
            TaffyLayoutActions.Finish(content);
            return root;
        }

        private static GameObject CreateScrollableList()
        {
            GameObject root = TaffyHierarchyActions.CreateRect("Scrollable List");
            ScrollRect scrollRect = Undo.AddComponent<ScrollRect>(root);
            scrollRect.horizontal = false;
            scrollRect.vertical = true;

            GameObject viewportObject = TaffyHierarchyActions.CreateChildRect(root.transform, "Viewport");
            RectTransform viewport = (RectTransform)viewportObject.transform;
            Stretch(viewport);
            Undo.AddComponent<RectMask2D>(viewportObject);

            GameObject contentObject = TaffyHierarchyActions.CreateChildRect(viewport, "Content");
            RectTransform contentRect = (RectTransform)contentObject.transform;
            StretchWidthTop(contentRect);
            TaffyLayoutGroup content = Undo.AddComponent<TaffyLayoutGroup>(contentObject);
            ApplyBuiltInPreset(content, "builtin.scroll-list");

            scrollRect.viewport = viewport;
            scrollRect.content = contentRect;
            PrefabUtility.RecordPrefabInstancePropertyModifications(scrollRect);
            EditorUtility.SetDirty(scrollRect);
            return root;
        }

        private static GameObject CreateResponsiveCards()
        {
            GameObject root = TaffyHierarchyActions.CreateGroup("Responsive Cards", TaffyGroupQuickLayout.Cards);
            TaffyLayoutGroup group = root.GetComponent<TaffyLayoutGroup>();
            ApplyBuiltInPreset(group, "builtin.wrapping-cards");
            for (int i = 1; i <= 3; i++)
            {
                TaffyLayoutItem card = TaffyHierarchyActions.CreateChildItem(root.transform, "Card " + i);
                Undo.RecordObject(card, "Configure Card");
                card.width = TaffyLength.Points(180f);
                card.height = TaffyLength.Auto;
                card.flexShrink = 1f;
                TaffyLayoutActions.Finish(card);
            }
            return root;
        }

        private static GameObject CreateModal()
        {
            GameObject root = TaffyHierarchyActions.CreateGroup("Modal", TaffyGroupQuickLayout.CenteredPanel);
            TaffyLayoutItem backdrop = TaffyHierarchyActions.CreateChildItem(root.transform, "Backdrop");
            Undo.RecordObject(backdrop, "Configure Backdrop");
            backdrop.width = TaffyLength.Percent(1f);
            backdrop.height = TaffyLength.Percent(1f);
            TaffyLayoutActions.Finish(backdrop);

            TaffyLayoutItem panel = TaffyHierarchyActions.CreateChildItem(root.transform, "Panel");
            Undo.RecordObject(panel, "Configure Modal Panel");
            panel.width = TaffyLength.Points(480f);
            panel.height = TaffyLength.Points(320f);
            panel.alignSelf = TaffyAlign.Center;
            TaffyLayoutActions.Finish(panel);
            return root;
        }

        private static GameObject CreateForm()
        {
            GameObject root = TaffyHierarchyActions.CreateGroup("Basic Form", TaffyGroupQuickLayout.Vertical);
            TaffyLayoutGroup group = root.GetComponent<TaffyLayoutGroup>();
            Undo.RecordObject(group, "Configure Form");
            group.verticalGap = 8f;
            group.alignItems = TaffyAlign.Stretch;
            TaffyLayoutActions.Finish(group);

            TaffyHierarchyActions.CreateChildItem(root.transform, "Field 1");
            TaffyHierarchyActions.CreateChildItem(root.transform, "Field 2");
            TaffyLayoutGroup actions = TaffyHierarchyActions.CreateChildGroup(root.transform, "Actions", TaffyGroupQuickLayout.Horizontal);
            Undo.RecordObject(actions, "Configure Form Actions");
            actions.justifyContent = TaffyJustify.End;
            actions.horizontalGap = 8f;
            TaffyLayoutActions.Finish(actions);
            return root;
        }

        private static void ApplyBuiltInPreset(UnityEngine.Object target, string id)
        {
            TaffyAuthoringPresetData preset = TaffyBuiltInPresets.All.FirstOrDefault(candidate => candidate.Id == id);
            if (preset != null)
                TaffyPresetApplication.Apply(preset, new[] { target });
        }

        private static void Stretch(RectTransform rect)
        {
            Undo.RecordObject(rect, "Stretch RectTransform");
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void StretchWidthTop(RectTransform rect)
        {
            Undo.RecordObject(rect, "Configure Scroll Content RectTransform");
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }

    internal static class TaffyOnboardingPreferences
    {
        private const string DismissedKey = "TaffyUGUI.Editor.FirstUseGuideDismissed";
        private const string ChecklistKeyPrefix = "TaffyUGUI.Editor.GroupChecklist.";

        internal static bool IsGuideDismissed
        {
            get => EditorPrefs.GetBool(DismissedKey, false);
            set => EditorPrefs.SetBool(DismissedKey, value);
        }

        internal static bool IsGroupChecklistDismissed(TaffyLayoutGroup group)
        {
            return group && SessionState.GetBool(ChecklistKeyPrefix + group.GetInstanceID(), false);
        }

        internal static void DismissGroupChecklist(TaffyLayoutGroup group)
        {
            if (group)
                SessionState.SetBool(ChecklistKeyPrefix + group.GetInstanceID(), true);
        }

        internal static void ResetForTests()
        {
            EditorPrefs.DeleteKey(DismissedKey);
        }
    }

    [InitializeOnLoad]
    internal static class TaffyFirstUseGuideLauncher
    {
        static TaffyFirstUseGuideLauncher()
        {
            if (!Application.isBatchMode && !TaffyOnboardingPreferences.IsGuideDismissed)
                EditorApplication.delayCall += ShowOnce;
        }

        private static void ShowOnce()
        {
            if (!TaffyOnboardingPreferences.IsGuideDismissed)
                TaffyFirstUseGuideWindow.ShowWindow();
        }
    }

    internal sealed class TaffyFirstUseGuideWindow : EditorWindow
    {
        [MenuItem("Window/TaffyUGUI/Getting Started")]
        internal static void ShowWindow()
        {
            TaffyFirstUseGuideWindow window = GetWindow<TaffyFirstUseGuideWindow>();
            window.titleContent = new GUIContent("TaffyUGUI Getting Started");
            window.minSize = new Vector2(420f, 270f);
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Create your first TaffyUGUI layout", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Start from a normal Unity hierarchy recipe, then edit the resulting TaffyLayoutGroup and TaffyLayoutItem components directly.", MessageType.Info);

            if (GUILayout.Button("Create Your First Layout"))
            {
                TaffyCreationRecipeCatalog.Create("vertical");
                TaffyOnboardingPreferences.IsGuideDismissed = true;
            }

            if (GUILayout.Button("Open Flex Quick Start Sample"))
                OpenPackagePath("Samples~/Flex Quick Start/README.md");
            if (GUILayout.Button("Open Grid + Responsive Sample"))
                OpenPackagePath("Samples~/Grid and Responsive/README.md");
            if (GUILayout.Button("Open Getting Started Documentation"))
                OpenPackagePath("Documentation~/getting-started.md");

            GUILayout.FlexibleSpace();
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Dismiss", GUILayout.Width(100f)))
                {
                    TaffyOnboardingPreferences.IsGuideDismissed = true;
                    Close();
                }
            }
        }

        internal static bool OpenPackagePath(string packageRelativePath)
        {
            string packageRoot = PackageRoot();
            if (string.IsNullOrEmpty(packageRoot))
                return false;
            string path = packageRoot.TrimEnd('/') + "/" + packageRelativePath;
            UnityEngine.Object asset = AssetDatabase.LoadMainAssetAtPath(path);
            if (!asset)
                return false;
            AssetDatabase.OpenAsset(asset);
            return true;
        }

        private static string PackageRoot()
        {
            string scriptPath = AssetDatabase.GetAssetPath(MonoScript.FromScriptableObject(CreateInstance<TaffyFirstUseGuideWindow>()));
            if (string.IsNullOrEmpty(scriptPath))
                return null;
            const string editorSegment = "/Editor/Onboarding/TaffyGuidedCreation.cs";
            return scriptPath.EndsWith(editorSegment, StringComparison.Ordinal)
                ? scriptPath.Substring(0, scriptPath.Length - editorSegment.Length)
                : null;
        }
    }

    internal static class TaffyOnboardingUI
    {
        internal static void DrawGroupChecklist(TaffyLayoutGroup group)
        {
            if (!group || TaffyOnboardingPreferences.IsGroupChecklistDismissed(group))
                return;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Getting started", EditorStyles.miniBoldLabel);
            EditorGUILayout.LabelField("1. Pick a Quick Layout or preset", EditorStyles.miniLabel);
            EditorGUILayout.LabelField("2. Add TaffyLayoutItem to participating children", EditorStyles.miniLabel);
            EditorGUILayout.LabelField("3. Use Layout Health if something looks wrong", EditorStyles.miniLabel);
            if (GUILayout.Button("Dismiss checklist", EditorStyles.miniButton))
                TaffyOnboardingPreferences.DismissGroupChecklist(group);
            EditorGUILayout.EndVertical();
        }
    }

    internal sealed class TaffyUIBuilderWindow : EditorWindow
    {
        private string _search = string.Empty;
        private int _categoryIndex;
        private Vector2 _scroll;
        private string[] _categories = { "All" };

        internal static GameObject CreateRecipe(string id) => TaffyCreationRecipeCatalog.Create(id);

        [MenuItem("Window/TaffyUGUI/UI Builder")]
        private static void Open()
        {
            TaffyUIBuilderWindow window = GetWindow<TaffyUIBuilderWindow>();
            window.titleContent = new GUIContent("TaffyUGUI UI Builder");
            window.minSize = new Vector2(460f, 320f);
            window.RefreshCategories();
            window.Show();
        }

        private void OnEnable()
        {
            RefreshCategories();
        }

        private void OnGUI()
        {
            _search = EditorGUILayout.TextField("Search", _search);
            _categoryIndex = EditorGUILayout.Popup("Category", Mathf.Clamp(_categoryIndex, 0, _categories.Length - 1), _categories);
            EditorGUILayout.Space();

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            foreach (TaffyCreationRecipe recipe in FilteredRecipes())
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField(recipe.Name, EditorStyles.boldLabel);
                EditorGUILayout.LabelField(recipe.Preview, EditorStyles.miniLabel);
                EditorGUILayout.LabelField(recipe.Summary, EditorStyles.wordWrappedMiniLabel);
                if (GUILayout.Button("Create"))
                    CreateRecipe(recipe.Id);
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndScrollView();
        }

        internal List<TaffyCreationRecipe> FilteredRecipes()
        {
            string category = _categories != null && _categories.Length > 0
                ? _categories[Mathf.Clamp(_categoryIndex, 0, _categories.Length - 1)]
                : "All";
            string query = (_search ?? string.Empty).Trim();
            return TaffyCreationRecipeCatalog.All
                .Where(recipe => category == "All" || recipe.Category == category)
                .Where(recipe => query.Length == 0 || recipe.Name.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 || recipe.Summary.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
                .ToList();
        }

        private void RefreshCategories()
        {
            _categories = new[] { "All" }.Concat(TaffyCreationRecipeCatalog.All.Select(recipe => recipe.Category).Distinct().OrderBy(value => value)).ToArray();
            _categoryIndex = Mathf.Clamp(_categoryIndex, 0, _categories.Length - 1);
        }
    }
}
