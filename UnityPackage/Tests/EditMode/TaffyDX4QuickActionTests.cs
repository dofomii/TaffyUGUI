using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TaffyUGUI.Editor;
using UnityEditor;
using UnityEngine;

namespace TaffyUGUI.Tests
{
    public sealed class TaffyDX4QuickActionTests
    {
        private readonly List<GameObject> _owned = new List<GameObject>();
        private const string PrefabFolder = "Assets/__TaffyUGUIDX4Tests";
        private static Assembly EditorAssembly => typeof(TaffyLayoutGroupEditor).Assembly;

        [TearDown]
        public void TearDown()
        {
            Undo.ClearAll();
            Selection.activeObject = null;
            for (int i = _owned.Count - 1; i >= 0; i--)
            {
                if (_owned[i])
                    UnityEngine.Object.DestroyImmediate(_owned[i]);
            }
            _owned.Clear();
            if (AssetDatabase.IsValidFolder(PrefabFolder))
                AssetDatabase.DeleteAsset(PrefabFolder);
            AssetDatabase.Refresh();
        }

        [Test]
        public void EveryGroupQuickLayoutProducesExpectedSerializedValues()
        {
            AssertGroupLayout("Horizontal", group =>
            {
                Assert.That(group.containerDisplay, Is.EqualTo(TaffyContainerDisplay.Flex));
                Assert.That(group.direction, Is.EqualTo(TaffyFlexDirection.Row));
                Assert.That(group.wrap, Is.EqualTo(TaffyFlexWrap.NoWrap));
            });
            AssertGroupLayout("Vertical", group =>
            {
                Assert.That(group.containerDisplay, Is.EqualTo(TaffyContainerDisplay.Flex));
                Assert.That(group.direction, Is.EqualTo(TaffyFlexDirection.Column));
                Assert.That(group.wrap, Is.EqualTo(TaffyFlexWrap.NoWrap));
            });
            AssertGroupLayout("CenteredPanel", group =>
            {
                Assert.That(group.direction, Is.EqualTo(TaffyFlexDirection.Column));
                Assert.That(group.justifyContent, Is.EqualTo(TaffyJustify.Center));
                Assert.That(group.alignItems, Is.EqualTo(TaffyAlign.Center));
            });
            AssertGroupLayout("Toolbar", group =>
            {
                Assert.That(group.direction, Is.EqualTo(TaffyFlexDirection.Row));
                Assert.That(group.justifyContent, Is.EqualTo(TaffyJustify.SpaceBetween));
                Assert.That(group.alignItems, Is.EqualTo(TaffyAlign.Center));
                Assert.That(group.horizontalGap, Is.EqualTo(8f));
            });
            AssertGroupLayout("Cards", group =>
            {
                Assert.That(group.wrap, Is.EqualTo(TaffyFlexWrap.Wrap));
                Assert.That(group.alignItems, Is.EqualTo(TaffyAlign.Start));
                Assert.That(group.horizontalGap, Is.EqualTo(12f));
                Assert.That(group.verticalGap, Is.EqualTo(12f));
            });
            AssertGroupLayout("Grid", group =>
            {
                Assert.That(group.containerDisplay, Is.EqualTo(TaffyContainerDisplay.Grid));
                Assert.That(group.gridAutoFlow, Is.EqualTo(TaffyGridAutoFlow.Row));
                Assert.That(group.gridColumns.Count, Is.EqualTo(2));
                Assert.That(group.gridColumns.All(track => track.kind == TaffyGridTrackKind.Fraction && Mathf.Approximately(track.value, 1f)), Is.True);
                Assert.That(group.horizontalGap, Is.EqualTo(12f));
                Assert.That(group.verticalGap, Is.EqualTo(12f));
            });
        }

        [Test]
        public void EveryItemQuickActionProducesExpectedSerializedValues()
        {
            AssertItemAction("FillWidth", item =>
            {
                AssertLength(item.width, TaffyUnit.Percent, 1f);
                Assert.That(item.height.unit, Is.EqualTo(TaffyUnit.Auto));
            });
            AssertItemAction("FillParent", item =>
            {
                AssertLength(item.width, TaffyUnit.Percent, 1f);
                AssertLength(item.height, TaffyUnit.Percent, 1f);
            });
            AssertItemAction("FitContent", item =>
            {
                Assert.That(item.width.unit, Is.EqualTo(TaffyUnit.Auto));
                Assert.That(item.height.unit, Is.EqualTo(TaffyUnit.Auto));
            });
            AssertItemAction("FixedSize", item =>
            {
                AssertLength(item.width, TaffyUnit.Points, 100f);
                AssertLength(item.height, TaffyUnit.Points, 100f);
            });
            AssertItemAction("Flexible", item =>
            {
                Assert.That(item.flexBasis.unit, Is.EqualTo(TaffyUnit.Auto));
                Assert.That(item.flexGrow, Is.EqualTo(1f));
                Assert.That(item.flexShrink, Is.EqualTo(1f));
            });
            AssertItemAction("Spacer", item =>
            {
                AssertLength(item.flexBasis, TaffyUnit.Points, 0f);
                Assert.That(item.flexGrow, Is.EqualTo(1f));
                Assert.That(item.flexShrink, Is.EqualTo(1f));
                Assert.That(item.measurement, Is.EqualTo(TaffyMeasurementMode.Disabled));
            });

            GameObject parentObject = CreateRect("CenterParent");
            TaffyLayoutGroup parent = parentObject.AddComponent<TaffyLayoutGroup>();
            parent.containerDisplay = TaffyContainerDisplay.Flex;
            TaffyLayoutItem centered = CreateChildItem(parentObject, "Centered");
            InvokeItem(centered, "Center");
            Assert.That(centered.alignSelf, Is.EqualTo(TaffyAlign.Center));

            parent.containerDisplay = TaffyContainerDisplay.Grid;
            centered.justifySelf = TaffyAlign.Auto;
            InvokeItem(centered, "Center");
            Assert.That(centered.justifySelf, Is.EqualTo(TaffyAlign.Center));
        }

        [Test]
        public void ChildInitializationSupportsPreserveStretchAndFitContent()
        {
            GameObject parentObject = CreateRect("InitParent");
            TaffyLayoutGroup group = parentObject.AddComponent<TaffyLayoutGroup>();
            GameObject child = CreateRect("InitChild");
            child.transform.SetParent(parentObject.transform, false);
            RectTransform rect = (RectTransform)child.transform;
            rect.sizeDelta = new Vector2(123f, 45f);

            InvokeChildInitialization(group, "PreserveSizes");
            TaffyLayoutItem item = child.GetComponent<TaffyLayoutItem>();
            Assert.That(item, Is.Not.Null);
            AssertLength(item.width, TaffyUnit.Points, 123f);
            AssertLength(item.height, TaffyUnit.Points, 45f);

            InvokeChildInitialization(group, "Stretch");
            AssertLength(item.width, TaffyUnit.Percent, 1f);
            AssertLength(item.height, TaffyUnit.Percent, 1f);

            InvokeChildInitialization(group, "FitContent");
            Assert.That(item.width.unit, Is.EqualTo(TaffyUnit.Auto));
            Assert.That(item.height.unit, Is.EqualTo(TaffyUnit.Auto));
        }

        [Test]
        public void MissingParentRepairAddsGroupWithoutChangingItemParent()
        {
            GameObject parent = CreateRect("RepairParent");
            TaffyLayoutItem item = CreateChildItem(parent, "RepairItem");
            Transform originalParent = item.transform.parent;

            Type actions = EditorAssembly.GetType("TaffyUGUI.Editor.TaffyItemActions");
            MethodInfo method = actions.GetMethod("AddGroupToParent", BindingFlags.Static | BindingFlags.NonPublic);
            TaffyLayoutGroup group = (TaffyLayoutGroup)method.Invoke(null, new object[] { item });

            Assert.That(group, Is.Not.Null);
            Assert.That(group.gameObject, Is.EqualTo(parent));
            Assert.That(item.transform.parent, Is.EqualTo(originalParent));
        }

        [Test]
        public void QuickActionUndoRestoresPriorState()
        {
            TaffyLayoutGroup group = CreateRect("UndoGroup").AddComponent<TaffyLayoutGroup>();
            group.containerDisplay = TaffyContainerDisplay.Grid;
            group.direction = TaffyFlexDirection.ColumnReverse;
            group.wrap = TaffyFlexWrap.Wrap;

            InvokeGroup(group, "Horizontal");
            Assert.That(group.containerDisplay, Is.EqualTo(TaffyContainerDisplay.Flex));
            Assert.That(group.direction, Is.EqualTo(TaffyFlexDirection.Row));

            Undo.PerformUndo();
            Assert.That(group.containerDisplay, Is.EqualTo(TaffyContainerDisplay.Grid));
            Assert.That(group.direction, Is.EqualTo(TaffyFlexDirection.ColumnReverse));
            Assert.That(group.wrap, Is.EqualTo(TaffyFlexWrap.Wrap));
        }

        [Test]
        public void PrefabInstanceActionKeepsConnectionAndDoesNotMutateAsset()
        {
            if (!AssetDatabase.IsValidFolder(PrefabFolder))
                AssetDatabase.CreateFolder("Assets", "__TaffyUGUIDX4Tests");

            GameObject source = new GameObject("DX4Prefab", typeof(RectTransform));
            source.SetActive(false);
            TaffyLayoutGroup sourceGroup = source.AddComponent<TaffyLayoutGroup>();
            sourceGroup.direction = TaffyFlexDirection.ColumnReverse;
            string path = PrefabFolder + "/DX4.prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(source, path);
            UnityEngine.Object.DestroyImmediate(source);

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.SetActive(false);
            _owned.Add(instance);
            TaffyLayoutGroup instanceGroup = instance.GetComponent<TaffyLayoutGroup>();
            InvokeGroup(instanceGroup, "Horizontal");

            Assert.That(PrefabUtility.GetPrefabInstanceStatus(instance), Is.EqualTo(PrefabInstanceStatus.Connected));
            Assert.That(instanceGroup.direction, Is.EqualTo(TaffyFlexDirection.Row));
            Assert.That(AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponent<TaffyLayoutGroup>().direction, Is.EqualTo(TaffyFlexDirection.ColumnReverse));
        }

        [Test]
        public void GroupAndItemActionsSupportMultiObjectApplication()
        {
            TaffyLayoutGroup first = CreateRect("FirstGroup").AddComponent<TaffyLayoutGroup>();
            TaffyLayoutGroup second = CreateRect("SecondGroup").AddComponent<TaffyLayoutGroup>();
            InvokeGroupMany(new List<TaffyLayoutGroup> { first, second }, "Vertical");
            Assert.That(first.direction, Is.EqualTo(TaffyFlexDirection.Column));
            Assert.That(second.direction, Is.EqualTo(TaffyFlexDirection.Column));

            TaffyLayoutItem firstItem = CreateItem("FirstItem");
            TaffyLayoutItem secondItem = CreateItem("SecondItem");
            InvokeItemMany(new List<TaffyLayoutItem> { firstItem, secondItem }, "FillParent");
            AssertLength(firstItem.width, TaffyUnit.Percent, 1f);
            AssertLength(secondItem.height, TaffyUnit.Percent, 1f);
        }

        [Test]
        public void HierarchyActionInfrastructureExposesRequestedCreationRecipes()
        {
            Type hierarchy = EditorAssembly.GetType("TaffyUGUI.Editor.TaffyHierarchyActions");
            Assert.That(hierarchy, Is.Not.Null);
            string[] names = hierarchy.GetMethods(BindingFlags.Static | BindingFlags.NonPublic)
                .Select(method => method.Name)
                .ToArray();
            CollectionAssert.IsSupersetOf(names, new[] { "CreateHorizontal", "CreateVertical", "CreateGrid", "CreateSpacer" });
        }

        private void AssertGroupLayout(string action, Action<TaffyLayoutGroup> assertion)
        {
            TaffyLayoutGroup group = CreateRect("Group" + action).AddComponent<TaffyLayoutGroup>();
            InvokeGroup(group, action);
            assertion(group);
        }

        private void AssertItemAction(string action, Action<TaffyLayoutItem> assertion)
        {
            TaffyLayoutItem item = CreateItem("Item" + action);
            InvokeItem(item, action);
            assertion(item);
        }

        private TaffyLayoutItem CreateItem(string name) => CreateRect(name).AddComponent<TaffyLayoutItem>();

        private TaffyLayoutItem CreateChildItem(GameObject parent, string name)
        {
            GameObject child = CreateRect(name);
            child.transform.SetParent(parent.transform, false);
            return child.AddComponent<TaffyLayoutItem>();
        }

        private GameObject CreateRect(string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.SetActive(false);
            _owned.Add(go);
            return go;
        }

        private static void AssertLength(TaffyLength length, TaffyUnit unit, float value)
        {
            Assert.That(length.unit, Is.EqualTo(unit));
            Assert.That(length.value, Is.EqualTo(value).Within(0.001f));
        }

        private static void InvokeGroup(TaffyLayoutGroup group, string action) => InvokeSingle("TaffyUGUI.Editor.TaffyLayoutActions", "ApplyQuickLayout", typeof(TaffyLayoutGroup), group, "TaffyUGUI.Editor.TaffyGroupQuickLayout", action);
        private static void InvokeItem(TaffyLayoutItem item, string action) => InvokeSingle("TaffyUGUI.Editor.TaffyItemActions", "Apply", typeof(TaffyLayoutItem), item, "TaffyUGUI.Editor.TaffyItemQuickAction", action);

        private static void InvokeSingle(string typeName, string methodName, Type firstParameterType, object target, string enumTypeName, string enumName)
        {
            Type type = EditorAssembly.GetType(typeName);
            Type enumType = EditorAssembly.GetType(enumTypeName);
            MethodInfo method = type.GetMethods(BindingFlags.Static | BindingFlags.NonPublic)
                .Single(candidate => candidate.Name == methodName && candidate.GetParameters().Length == 2 && candidate.GetParameters()[0].ParameterType == firstParameterType);
            method.Invoke(null, new[] { target, Enum.Parse(enumType, enumName) });
        }

        private static void InvokeGroupMany(List<TaffyLayoutGroup> groups, string action)
        {
            Type type = EditorAssembly.GetType("TaffyUGUI.Editor.TaffyLayoutActions");
            Type enumType = EditorAssembly.GetType("TaffyUGUI.Editor.TaffyGroupQuickLayout");
            MethodInfo method = type.GetMethods(BindingFlags.Static | BindingFlags.NonPublic)
                .Single(candidate => candidate.Name == "ApplyQuickLayout" && candidate.GetParameters().Length == 2 && candidate.GetParameters()[0].ParameterType != typeof(TaffyLayoutGroup));
            method.Invoke(null, new object[] { groups, Enum.Parse(enumType, action) });
        }

        private static void InvokeItemMany(List<TaffyLayoutItem> items, string action)
        {
            Type type = EditorAssembly.GetType("TaffyUGUI.Editor.TaffyItemActions");
            Type enumType = EditorAssembly.GetType("TaffyUGUI.Editor.TaffyItemQuickAction");
            MethodInfo method = type.GetMethods(BindingFlags.Static | BindingFlags.NonPublic)
                .Single(candidate => candidate.Name == "Apply" && candidate.GetParameters().Length == 2 && candidate.GetParameters()[0].ParameterType != typeof(TaffyLayoutItem));
            method.Invoke(null, new object[] { items, Enum.Parse(enumType, action) });
        }

        private static void InvokeChildInitialization(TaffyLayoutGroup group, string initialization)
        {
            Type type = EditorAssembly.GetType("TaffyUGUI.Editor.TaffyLayoutActions");
            Type enumType = EditorAssembly.GetType("TaffyUGUI.Editor.TaffyChildInitialization");
            MethodInfo method = type.GetMethod("InitializeChildren", BindingFlags.Static | BindingFlags.NonPublic);
            method.Invoke(null, new object[] { group, Enum.Parse(enumType, initialization) });
        }
    }
}
