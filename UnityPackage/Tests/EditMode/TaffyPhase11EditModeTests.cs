using System.Collections.Generic;
using NUnit.Framework;
using TaffyUGUI.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace TaffyUGUI.Tests
{
    public sealed class TaffyPhase11EditModeTests
    {
        private readonly List<GameObject> _owned = new List<GameObject>();
        private const string PrefabFolder = "Assets/__TaffyUGUIPhase11Tests";

        [TearDown]
        public void TearDown()
        {
            for (int i = _owned.Count - 1; i >= 0; i--)
            {
                if (_owned[i])
                    Object.DestroyImmediate(_owned[i]);
            }
            _owned.Clear();

            if (AssetDatabase.IsValidFolder(PrefabFolder))
                AssetDatabase.DeleteAsset(PrefabFolder);
            AssetDatabase.Refresh();
        }

        [Test]
        public void CustomEditorsAndTypedDrawersAreRegisteredEditorTypes()
        {
            GameObject root = CreateRect("Editors", 300f, 200f);
            TaffyLayoutGroup group = root.AddComponent<TaffyLayoutGroup>();
            GameObject child = CreateRect("Item", 40f, 30f);
            child.transform.SetParent(root.transform, false);
            TaffyLayoutItem item = child.AddComponent<TaffyLayoutItem>();

            UnityEditor.Editor groupEditor = UnityEditor.Editor.CreateEditor(group);
            UnityEditor.Editor itemEditor = UnityEditor.Editor.CreateEditor(item);
            try
            {
                Assert.That(groupEditor, Is.TypeOf<TaffyLayoutGroupEditor>());
                Assert.That(itemEditor, Is.TypeOf<TaffyLayoutItemEditor>());
                Assert.That(new TaffyLengthDrawer(), Is.InstanceOf<PropertyDrawer>());
                Assert.That(new TaffyEdgesDrawer(), Is.InstanceOf<PropertyDrawer>());
                Assert.That(new TaffyCalcExpressionDrawer(), Is.InstanceOf<PropertyDrawer>());
                Assert.That(new TaffyGridTrackDrawer(), Is.InstanceOf<PropertyDrawer>());
                Assert.That(new TaffyGridPlacementDrawer(), Is.InstanceOf<PropertyDrawer>());
                Assert.That(new TaffyGridAreaDrawer(), Is.InstanceOf<PropertyDrawer>());
            }
            finally
            {
                Object.DestroyImmediate(groupEditor);
                Object.DestroyImmediate(itemEditor);
            }
        }

        [Test]
        public void HorizontalLayoutMigrationPreservesPaddingAlignmentChildSizingAndExistingItemData()
        {
            GameObject root = CreateRect("Horizontal", 300f, 100f);
            HorizontalLayoutGroup source = root.AddComponent<HorizontalLayoutGroup>();
            source.spacing = 12f;
            source.padding = new RectOffset(3, 5, 7, 9);
            source.childAlignment = TextAnchor.LowerRight;
            source.childControlWidth = false;
            source.childControlHeight = false;
            source.childForceExpandWidth = true;
            source.childForceExpandHeight = false;

            GameObject child = CreateRect("Child", 40f, 20f);
            child.transform.SetParent(root.transform, false);
            TaffyLayoutItem existing = child.AddComponent<TaffyLayoutItem>();
            existing.margin = TaffyEdges.Points(3f);

            TaffyMigrationAnalysis analysis = TaffyMigrationService.Analyze(source);
            Assert.That(analysis.canMigrate, Is.True, analysis.message);
            TaffyMigrationResult result = TaffyMigrationService.Migrate(source);
            Assert.That(result.success, Is.True, result.message);

            TaffyLayoutGroup group = root.GetComponent<TaffyLayoutGroup>();
            Assert.That(group, Is.Not.Null);
            Assert.That(root.GetComponent<HorizontalLayoutGroup>(), Is.Null);
            Assert.That(group.containerDisplay, Is.EqualTo(TaffyContainerDisplay.Flex));
            Assert.That(group.direction, Is.EqualTo(TaffyFlexDirection.Row));
            Assert.That(group.horizontalGap, Is.EqualTo(12f));
            Assert.That(group.justifyContent, Is.EqualTo(TaffyJustify.End));
            Assert.That(group.alignItems, Is.EqualTo(TaffyAlign.End));
            Assert.That(group.padding.left, Is.EqualTo(3));
            Assert.That(group.padding.right, Is.EqualTo(5));
            Assert.That(group.padding.top, Is.EqualTo(7));
            Assert.That(group.padding.bottom, Is.EqualTo(9));

            TaffyLayoutItem migratedItem = child.GetComponent<TaffyLayoutItem>();
            Assert.That(migratedItem, Is.SameAs(existing));
            Assert.That(migratedItem.width.unit, Is.EqualTo(TaffyUnit.Points));
            Assert.That(migratedItem.width.value, Is.EqualTo(40f).Within(0.01f));
            Assert.That(migratedItem.height.unit, Is.EqualTo(TaffyUnit.Points));
            Assert.That(migratedItem.height.value, Is.EqualTo(20f).Within(0.01f));
            Assert.That(migratedItem.flexGrow, Is.GreaterThanOrEqualTo(1f));
            Assert.That(migratedItem.margin.left.value, Is.EqualTo(3f));
        }

        [Test]
        public void VerticalLayoutMigrationUsesColumnFlexAndMainAxisExpansion()
        {
            GameObject root = CreateRect("Vertical", 180f, 300f);
            VerticalLayoutGroup source = root.AddComponent<VerticalLayoutGroup>();
            source.spacing = 6f;
            source.childAlignment = TextAnchor.MiddleCenter;
            source.childControlWidth = true;
            source.childControlHeight = true;
            source.childForceExpandWidth = true;
            source.childForceExpandHeight = true;
            GameObject child = CreateRect("Child", 50f, 25f);
            child.transform.SetParent(root.transform, false);

            TaffyMigrationResult result = TaffyMigrationService.Migrate(source);
            Assert.That(result.success, Is.True, result.message);
            TaffyLayoutGroup group = root.GetComponent<TaffyLayoutGroup>();
            Assert.That(group.direction, Is.EqualTo(TaffyFlexDirection.Column));
            Assert.That(group.verticalGap, Is.EqualTo(6f));
            Assert.That(group.justifyContent, Is.EqualTo(TaffyJustify.Center));
            Assert.That(group.alignItems, Is.EqualTo(TaffyAlign.Stretch));
            Assert.That(child.GetComponent<TaffyLayoutItem>().flexGrow, Is.GreaterThanOrEqualTo(1f));
        }

        [Test]
        public void SafeFixedColumnGridMigratesToExplicitColumnsAndAutoRows()
        {
            GameObject root = CreateRect("Grid", 300f, 200f);
            GridLayoutGroup source = root.AddComponent<GridLayoutGroup>();
            source.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            source.constraintCount = 2;
            source.startCorner = GridLayoutGroup.Corner.UpperLeft;
            source.startAxis = GridLayoutGroup.Axis.Horizontal;
            source.cellSize = new Vector2(50f, 30f);
            source.spacing = new Vector2(5f, 7f);
            source.childAlignment = TextAnchor.MiddleCenter;
            for (int i = 0; i < 3; i++)
                CreateRect("Cell" + i, 10f, 10f).transform.SetParent(root.transform, false);

            TaffyMigrationAnalysis analysis = TaffyMigrationService.Analyze(source);
            Assert.That(analysis.canMigrate, Is.True, analysis.message);
            TaffyMigrationResult result = TaffyMigrationService.Migrate(source);
            Assert.That(result.success, Is.True, result.message);

            TaffyLayoutGroup group = root.GetComponent<TaffyLayoutGroup>();
            Assert.That(group.containerDisplay, Is.EqualTo(TaffyContainerDisplay.Grid));
            Assert.That(group.gridAutoFlow, Is.EqualTo(TaffyGridAutoFlow.Row));
            Assert.That(group.gridColumns.Count, Is.EqualTo(2));
            Assert.That(group.gridColumns[0].kind, Is.EqualTo(TaffyGridTrackKind.Points));
            Assert.That(group.gridColumns[0].value, Is.EqualTo(50f));
            Assert.That(group.gridAutoRows.Count, Is.EqualTo(1));
            Assert.That(group.gridAutoRows[0].value, Is.EqualTo(30f));
            Assert.That(group.horizontalGap, Is.EqualTo(5f));
            Assert.That(group.verticalGap, Is.EqualTo(7f));
            Assert.That(group.justifyContent, Is.EqualTo(TaffyJustify.Center));
            Assert.That(group.alignContent, Is.EqualTo(TaffyAlignContent.Center));

            for (int i = 0; i < root.transform.childCount; i++)
            {
                TaffyLayoutItem item = root.transform.GetChild(i).GetComponent<TaffyLayoutItem>();
                Assert.That(item, Is.Not.Null);
                Assert.That(item.width.value, Is.EqualTo(50f));
                Assert.That(item.height.value, Is.EqualTo(30f));
            }
        }

        [Test]
        public void UnsafeGridMigrationIsRejectedWithoutModifyingSource()
        {
            GameObject root = CreateRect("UnsafeGrid", 300f, 200f);
            GridLayoutGroup source = root.AddComponent<GridLayoutGroup>();
            source.constraint = GridLayoutGroup.Constraint.Flexible;

            TaffyMigrationAnalysis analysis = TaffyMigrationService.Analyze(source);
            Assert.That(analysis.canMigrate, Is.False);
            StringAssert.Contains("Flexible", analysis.message);
            TaffyMigrationResult result = TaffyMigrationService.Migrate(source);
            Assert.That(result.success, Is.False);
            Assert.That(root.GetComponent<GridLayoutGroup>(), Is.SameAs(source));
            Assert.That(root.GetComponent<TaffyLayoutGroup>(), Is.Null);
        }

        [Test]
        public void MigrationIsUndoableAndRestoresLegacyComponent()
        {
            GameObject root = CreateRect("Undo", 300f, 100f);
            HorizontalLayoutGroup source = root.AddComponent<HorizontalLayoutGroup>();
            source.spacing = 4f;

            Undo.IncrementCurrentGroup();
            int priorGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Prior user rename");
            Undo.RecordObject(root, "Prior user rename");
            root.name = "UndoRenamed";
            Undo.CollapseUndoOperations(priorGroup);

            TaffyMigrationResult result = TaffyMigrationService.Migrate(source);
            Assert.That(result.success, Is.True, result.message);
            Assert.That(root.GetComponent<TaffyLayoutGroup>(), Is.Not.Null);
            Assert.That(root.GetComponent<HorizontalLayoutGroup>(), Is.Null);

            Undo.PerformUndo();
            Assert.That(root.GetComponent<TaffyLayoutGroup>(), Is.Null);
            Assert.That(root.GetComponent<HorizontalLayoutGroup>(), Is.Not.Null);
            Assert.That(root.name, Is.EqualTo("UndoRenamed"), "Migration must occupy its own Undo group and must not consume a preceding user operation.");
        }

        [Test]
        public void PrefabInstanceMigrationCreatesOverridesWithoutMutatingPrefabAsset()
        {
            if (!AssetDatabase.IsValidFolder(PrefabFolder))
                AssetDatabase.CreateFolder("Assets", "__TaffyUGUIPhase11Tests");

            GameObject sourceRoot = new GameObject("PrefabSource", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            GameObject sourceChild = new GameObject("Child", typeof(RectTransform));
            sourceChild.transform.SetParent(sourceRoot.transform, false);
            string path = PrefabFolder + "/Layout.prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(sourceRoot, path);
            Object.DestroyImmediate(sourceRoot);

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            _owned.Add(instance);
            HorizontalLayoutGroup legacy = instance.GetComponent<HorizontalLayoutGroup>();
            TaffyMigrationResult result = TaffyMigrationService.Migrate(legacy);
            Assert.That(result.success, Is.True, result.message);
            TaffyLayoutGroup migrated = instance.GetComponent<TaffyLayoutGroup>();
            Assert.That(migrated, Is.Not.Null);
            Assert.That(PrefabUtility.IsPartOfPrefabInstance(instance), Is.True);
            Assert.That(instance.GetComponent<HorizontalLayoutGroup>(), Is.Null);

            GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.That(asset.GetComponent<HorizontalLayoutGroup>(), Is.Not.Null);
            Assert.That(asset.GetComponent<TaffyLayoutGroup>(), Is.Null);
        }

        [Test]
        public void BatchMigrationProcessesCompatibleGroupsAndReportsUnsafeGroups()
        {
            GameObject hRoot = CreateRect("BatchH", 200f, 100f);
            HorizontalLayoutGroup horizontal = hRoot.AddComponent<HorizontalLayoutGroup>();
            GameObject vRoot = CreateRect("BatchV", 100f, 200f);
            VerticalLayoutGroup vertical = vRoot.AddComponent<VerticalLayoutGroup>();
            GameObject gRoot = CreateRect("BatchUnsafe", 200f, 200f);
            GridLayoutGroup unsafeGrid = gRoot.AddComponent<GridLayoutGroup>();
            unsafeGrid.constraint = GridLayoutGroup.Constraint.Flexible;

            List<TaffyMigrationResult> results = TaffyMigrationService.MigrateAll(new LayoutGroup[] { horizontal, vertical, unsafeGrid, horizontal });
            Assert.That(results.Count, Is.EqualTo(3));
            Assert.That(results.FindAll(x => x.success).Count, Is.EqualTo(2));
            Assert.That(hRoot.GetComponent<TaffyLayoutGroup>(), Is.Not.Null);
            Assert.That(vRoot.GetComponent<TaffyLayoutGroup>(), Is.Not.Null);
            Assert.That(gRoot.GetComponent<GridLayoutGroup>(), Is.Not.Null);
        }

        [Test]
        public void DebuggerWindowAndSceneVisualizationEditorTypesAreAvailable()
        {
            bool previous = TaffySceneVisualization.Enabled;
            try
            {
                TaffySceneVisualization.Enabled = !previous;
                Assert.That(TaffySceneVisualization.Enabled, Is.EqualTo(!previous));
                Assert.That(typeof(TaffyLayoutDebuggerWindow).IsSubclassOf(typeof(EditorWindow)), Is.True);
                Assert.That(typeof(TaffySceneVisualization).IsAbstract && typeof(TaffySceneVisualization).IsSealed, Is.True);
            }
            finally
            {
                TaffySceneVisualization.Enabled = previous;
            }
        }

        private GameObject CreateRect(string name, float width, float height)
        {
            var go = new GameObject(name, typeof(RectTransform));
            _owned.Add(go);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = new Vector2(width, height);
            return go;
        }
    }
}
