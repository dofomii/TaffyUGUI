using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TaffyUGUI.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace TaffyUGUI.Tests
{
    public sealed class TaffyDX9GuidedCreationTests
    {
        private static Assembly EditorAssembly => typeof(TaffyLayoutGroupEditor).Assembly;
        private readonly List<GameObject> _owned = new List<GameObject>();

        [SetUp]
        public void SetUp()
        {
            Selection.activeGameObject = null;
            Type preferences = EditorAssembly.GetType("TaffyUGUI.Editor.TaffyOnboardingPreferences");
            Invoke(preferences, "ResetForTests");
        }

        [TearDown]
        public void TearDown()
        {
            Selection.activeGameObject = null;
            for (int i = _owned.Count - 1; i >= 0; i--)
            {
                if (_owned[i])
                    UnityEngine.Object.DestroyImmediate(_owned[i]);
            }
            _owned.Clear();
            Type preferences = EditorAssembly.GetType("TaffyUGUI.Editor.TaffyOnboardingPreferences");
            Invoke(preferences, "ResetForTests");
        }

        [Test]
        public void CatalogContainsEveryRequiredRecipeAndCreatesOrdinarySceneObjects()
        {
            Type catalog = EditorAssembly.GetType("TaffyUGUI.Editor.TaffyCreationRecipeCatalog");
            IEnumerable all = GetStaticProperty(catalog, "All") as IEnumerable;
            var ids = new HashSet<string>();
            foreach (object recipe in all)
                ids.Add((string)recipe.GetType().GetProperty("Id", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(recipe));

            CollectionAssert.IsSupersetOf(ids, new[]
            {
                "horizontal", "vertical", "centered-panel", "toolbar", "sidebar-content",
                "scrollable-list", "responsive-cards", "modal", "form",
            });

            foreach (string id in ids)
            {
                GameObject root = (GameObject)Invoke(catalog, "Create", id);
                Assert.That(root, Is.Not.Null, id);
                _owned.Add(root);
                Assert.That(root.hideFlags, Is.EqualTo(HideFlags.None), id);
                Assert.That(root.scene.IsValid(), Is.True, id);
            }
        }

        [Test]
        public void RecipesProduceExpectedHierarchyAndSerializedSettings()
        {
            GameObject horizontal = Create("horizontal");
            AssertGroup(horizontal, TaffyContainerDisplay.Flex, TaffyFlexDirection.Row);

            GameObject vertical = Create("vertical");
            AssertGroup(vertical, TaffyContainerDisplay.Flex, TaffyFlexDirection.Column);

            GameObject centered = Create("centered-panel");
            TaffyLayoutGroup centeredGroup = centered.GetComponent<TaffyLayoutGroup>();
            Assert.That(centeredGroup.justifyContent, Is.EqualTo(TaffyJustify.Center));
            Assert.That(centeredGroup.alignItems, Is.EqualTo(TaffyAlign.Center));

            GameObject toolbar = Create("toolbar");
            TaffyLayoutGroup toolbarGroup = toolbar.GetComponent<TaffyLayoutGroup>();
            Assert.That(toolbarGroup.justifyContent, Is.EqualTo(TaffyJustify.SpaceBetween));
            Assert.That(toolbarGroup.horizontalGap, Is.EqualTo(8f));

            GameObject sidebar = Create("sidebar-content");
            Assert.That(sidebar.transform.childCount, Is.EqualTo(2));
            TaffyLayoutItem sidebarItem = sidebar.transform.GetChild(0).GetComponent<TaffyLayoutItem>();
            TaffyLayoutItem contentItem = sidebar.transform.GetChild(1).GetComponent<TaffyLayoutItem>();
            Assert.That(sidebarItem.width.unit, Is.EqualTo(TaffyUnit.Points));
            Assert.That(sidebarItem.width.value, Is.EqualTo(240f));
            Assert.That(contentItem.flexGrow, Is.EqualTo(1f));

            GameObject scroll = Create("scrollable-list");
            ScrollRect scrollRect = scroll.GetComponent<ScrollRect>();
            Assert.That(scrollRect, Is.Not.Null);
            Assert.That(scrollRect.horizontal, Is.False);
            Assert.That(scrollRect.vertical, Is.True);
            Assert.That(scroll.transform.Find("Viewport"), Is.Not.Null);
            Transform contentTransform = scroll.transform.Find("Viewport/Content");
            Assert.That(contentTransform, Is.Not.Null);
            Assert.That(scroll.transform.Find("Viewport").GetComponent<RectMask2D>(), Is.Not.Null);
            TaffyLayoutGroup scrollContent = contentTransform.GetComponent<TaffyLayoutGroup>();
            Assert.That(scrollContent.direction, Is.EqualTo(TaffyFlexDirection.Column));
            Assert.That(scrollContent.overflowY, Is.EqualTo(TaffyOverflow.Scroll));

            GameObject cards = Create("responsive-cards");
            TaffyLayoutGroup cardsGroup = cards.GetComponent<TaffyLayoutGroup>();
            Assert.That(cardsGroup.wrap, Is.EqualTo(TaffyFlexWrap.Wrap));
            Assert.That(cardsGroup.horizontalGap, Is.EqualTo(12f));
            Assert.That(cards.transform.childCount, Is.EqualTo(3));

            GameObject modal = Create("modal");
            Assert.That(modal.transform.childCount, Is.EqualTo(2));
            TaffyLayoutItem panel = modal.transform.Find("Panel").GetComponent<TaffyLayoutItem>();
            Assert.That(panel.width.unit, Is.EqualTo(TaffyUnit.Points));
            Assert.That(panel.width.value, Is.EqualTo(480f));
            Assert.That(panel.height.value, Is.EqualTo(320f));

            GameObject form = Create("form");
            Assert.That(form.transform.childCount, Is.EqualTo(3));
            TaffyLayoutGroup actions = form.transform.Find("Actions").GetComponent<TaffyLayoutGroup>();
            Assert.That(actions, Is.Not.Null);
            Assert.That(actions.direction, Is.EqualTo(TaffyFlexDirection.Row));
            Assert.That(actions.justifyContent, Is.EqualTo(TaffyJustify.End));
        }

        [Test]
        public void RecipeCreationIsUndoableAsOneWorkflow()
        {
            GameObject root = Create("sidebar-content");
            int rootId = root.GetInstanceID();
            Assert.That(root.transform.childCount, Is.EqualTo(2));

            Undo.PerformUndo();
            Assert.That(EditorUtility.InstanceIDToObject(rootId), Is.Null);

            Undo.PerformRedo();
            GameObject restored = EditorUtility.InstanceIDToObject(rootId) as GameObject;
            Assert.That(restored, Is.Not.Null);
            _owned.Add(restored);
            Assert.That(restored.transform.childCount, Is.EqualTo(2));
        }

        [Test]
        public void OnboardingDismissalRoundTripsAndBuilderUsesSharedRecipeInfrastructure()
        {
            Type preferences = EditorAssembly.GetType("TaffyUGUI.Editor.TaffyOnboardingPreferences");
            Assert.That((bool)GetStaticProperty(preferences, "IsGuideDismissed"), Is.False);
            SetStaticProperty(preferences, "IsGuideDismissed", true);
            Assert.That((bool)GetStaticProperty(preferences, "IsGuideDismissed"), Is.True);

            Type builder = EditorAssembly.GetType("TaffyUGUI.Editor.TaffyUIBuilderWindow");
            GameObject root = (GameObject)Invoke(builder, "CreateRecipe", "responsive-cards");
            _owned.Add(root);
            Assert.That(root, Is.Not.Null);
            Assert.That(root.GetComponent<TaffyLayoutGroup>(), Is.Not.Null);
            Assert.That(root.GetComponent<TaffyLayoutGroup>().wrap, Is.EqualTo(TaffyFlexWrap.Wrap));
            foreach (Transform child in root.transform)
                Assert.That(child.GetComponent<TaffyLayoutItem>(), Is.Not.Null);
        }

        private GameObject Create(string id)
        {
            Type catalog = EditorAssembly.GetType("TaffyUGUI.Editor.TaffyCreationRecipeCatalog");
            GameObject root = (GameObject)Invoke(catalog, "Create", id);
            _owned.Add(root);
            return root;
        }

        private static void AssertGroup(GameObject root, TaffyContainerDisplay display, TaffyFlexDirection direction)
        {
            TaffyLayoutGroup group = root.GetComponent<TaffyLayoutGroup>();
            Assert.That(group, Is.Not.Null);
            Assert.That(group.containerDisplay, Is.EqualTo(display));
            Assert.That(group.direction, Is.EqualTo(direction));
        }

        private static object Invoke(Type type, string methodName, params object[] args)
        {
            MethodInfo method = type.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, type.FullName + "." + methodName);
            return method.Invoke(null, args);
        }

        private static object GetStaticProperty(Type type, string name)
        {
            PropertyInfo property = type.GetProperty(name, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.That(property, Is.Not.Null, type.FullName + "." + name);
            return property.GetValue(null);
        }

        private static void SetStaticProperty(Type type, string name, object value)
        {
            PropertyInfo property = type.GetProperty(name, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.That(property, Is.Not.Null, type.FullName + "." + name);
            property.SetValue(null, value);
        }
    }
}
