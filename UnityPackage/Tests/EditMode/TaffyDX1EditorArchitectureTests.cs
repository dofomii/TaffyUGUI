using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TaffyUGUI.Editor;
using UnityEditor;
using UnityEngine;

namespace TaffyUGUI.Tests
{
    public sealed class TaffyDX1EditorArchitectureTests
    {
        private readonly List<GameObject> _owned = new List<GameObject>();
        private const string PrefabFolder = "Assets/__TaffyUGUIDX1Tests";

        private static readonly string[] ExpectedGroupCoverage =
        {
            "containerDisplay", "boxSizing", "writingDirection", "overflowX", "overflowY", "scrollbarWidth", "m_Padding", "border", "textAlign",
            "direction", "wrap", "horizontalGap", "verticalGap", "justifyContent", "alignItems", "alignContent", "justifyItems",
            "gridAutoFlow", "gridRows", "gridColumns", "gridAutoRows", "gridAutoColumns", "gridNamedLines", "gridAreas", "gridAreaRows", "gridAreaColumns",
            "responsiveProfiles", "safeAreaMode", "scrollRectContentMode", "pixelRounding", "maxRebuildRequestsPerFrame",
        };

        private static readonly string[] ExpectedItemCoverage =
        {
            "display", "boxSizing", "writingDirection", "overflowX", "overflowY", "scrollbarWidth",
            "position", "inset", "width", "height", "minWidth", "minHeight", "maxWidth", "maxHeight", "aspectRatio",
            "margin", "padding", "border",
            "flexBasis", "flexGrow", "flexShrink", "alignSelf",
            "gridRowStart", "gridRowEnd", "gridColumnStart", "gridColumnEnd", "justifySelf",
            "floatMode", "clearMode", "textAlign",
            "measurement", "forceReplacedElement", "itemIsTable",
        };

        [TearDown]
        public void TearDown()
        {
            Undo.ClearAll();
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
        public void ModularEditorsPreserveCompleteSerializedPropertyCoverage()
        {
            TaffyLayoutGroup group = CreateInactive<TaffyLayoutGroup>("GroupCoverage");
            TaffyLayoutItem item = CreateInactive<TaffyLayoutItem>("ItemCoverage");

            UnityEditor.Editor groupEditor = UnityEditor.Editor.CreateEditor(group);
            UnityEditor.Editor itemEditor = UnityEditor.Editor.CreateEditor(item);
            try
            {
                Assert.That(groupEditor, Is.TypeOf<TaffyLayoutGroupEditor>());
                Assert.That(itemEditor, Is.TypeOf<TaffyLayoutItemEditor>());

                AssertCoverage(groupEditor, ExpectedGroupCoverage);
                AssertCoverage(itemEditor, ExpectedItemCoverage);
            }
            finally
            {
                DestroyEditor(groupEditor);
                DestroyEditor(itemEditor);
            }
        }

        [Test]
        public void EditorCoreAndSectionArchitectureTypesArePresent()
        {
            Assembly assembly = typeof(TaffyLayoutGroupEditor).Assembly;
            Assert.That(assembly.GetType("TaffyUGUI.Editor.TaffyInspectorContext"), Is.Not.Null);
            Assert.That(assembly.GetType("TaffyUGUI.Editor.TaffyEditorContent"), Is.Not.Null);
            Assert.That(assembly.GetType("TaffyUGUI.Editor.TaffyEditorPreferences"), Is.Not.Null);
            Assert.That(assembly.GetType("TaffyUGUI.Editor.TaffyInspectorSection"), Is.Not.Null);
            Assert.That(assembly.GetType("TaffyUGUI.Editor.TaffyGroupFormattingSection"), Is.Not.Null);
            Assert.That(assembly.GetType("TaffyUGUI.Editor.TaffyGroupFlexSection"), Is.Not.Null);
            Assert.That(assembly.GetType("TaffyUGUI.Editor.TaffyGroupGridSection"), Is.Not.Null);
            Assert.That(assembly.GetType("TaffyUGUI.Editor.TaffyGroupResponsiveSection"), Is.Not.Null);
            Assert.That(assembly.GetType("TaffyUGUI.Editor.TaffyItemPositionSizeSection"), Is.Not.Null);
            Assert.That(assembly.GetType("TaffyUGUI.Editor.TaffyItemMeasurementSection"), Is.Not.Null);

            FieldInfo groupSections = typeof(TaffyLayoutGroupEditor).GetField("_authoringSections", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo itemSections = typeof(TaffyLayoutItemEditor).GetField("_authoringSections", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(groupSections, Is.Not.Null);
            Assert.That(itemSections, Is.Not.Null);
            Assert.That(groupSections.FieldType.IsArray, Is.True);
            Assert.That(itemSections.FieldType.IsArray, Is.True);
        }

        [Test]
        public void FoldoutPreferenceInfrastructurePersistsState()
        {
            Type preferences = typeof(TaffyLayoutGroupEditor).Assembly.GetType("TaffyUGUI.Editor.TaffyEditorPreferences");
            Assert.That(preferences, Is.Not.Null);
            MethodInfo getFoldout = preferences.GetMethod("GetFoldout", BindingFlags.Static | BindingFlags.NonPublic);
            MethodInfo setFoldout = preferences.GetMethod("SetFoldout", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(getFoldout, Is.Not.Null);
            Assert.That(setFoldout, Is.Not.Null);

            const string inspector = "DX1Test";
            const string section = "Persistence";
            bool original = (bool)getFoldout.Invoke(null, new object[] { inspector, section, true });
            bool changed = !original;
            try
            {
                setFoldout.Invoke(null, new object[] { inspector, section, changed });
                Assert.That((bool)getFoldout.Invoke(null, new object[] { inspector, section, original }), Is.EqualTo(changed));
            }
            finally
            {
                setFoldout.Invoke(null, new object[] { inspector, section, original });
            }
        }

        [Test]
        public void RepresentativeSerializedEditorEditsAreUndoable()
        {
            TaffyLayoutGroup group = CreateInactive<TaffyLayoutGroup>("UndoGroup");
            TaffyLayoutItem item = CreateInactive<TaffyLayoutItem>("UndoItem");
            group.horizontalGap = 3f;
            item.flexGrow = 0f;

            UnityEditor.Editor groupEditor = UnityEditor.Editor.CreateEditor(group);
            UnityEditor.Editor itemEditor = UnityEditor.Editor.CreateEditor(item);
            try
            {
                Undo.IncrementCurrentGroup();
                int undoGroup = Undo.GetCurrentGroup();
                Undo.SetCurrentGroupName("DX1 representative editor edit");

                Undo.RecordObject(group, "DX1 representative editor edit");
                groupEditor.serializedObject.Update();
                groupEditor.serializedObject.FindProperty("horizontalGap").floatValue = 17f;
                groupEditor.serializedObject.ApplyModifiedProperties();

                Undo.RecordObject(item, "DX1 representative editor edit");
                itemEditor.serializedObject.Update();
                itemEditor.serializedObject.FindProperty("flexGrow").floatValue = 2f;
                itemEditor.serializedObject.ApplyModifiedProperties();
                Undo.CollapseUndoOperations(undoGroup);

                Assert.That(group.horizontalGap, Is.EqualTo(17f));
                Assert.That(item.flexGrow, Is.EqualTo(2f));

                Undo.PerformUndo();
                Assert.That(group.horizontalGap, Is.EqualTo(3f));
                Assert.That(item.flexGrow, Is.EqualTo(0f));
            }
            finally
            {
                DestroyEditor(groupEditor);
                DestroyEditor(itemEditor);
            }
        }

        [Test]
        public void SerializedEditorEditsOnPrefabInstanceDoNotMutatePrefabAsset()
        {
            if (!AssetDatabase.IsValidFolder(PrefabFolder))
                AssetDatabase.CreateFolder("Assets", "__TaffyUGUIDX1Tests");

            GameObject source = new GameObject("DX1Prefab", typeof(RectTransform));
            source.SetActive(false);
            TaffyLayoutGroup sourceGroup = source.AddComponent<TaffyLayoutGroup>();
            sourceGroup.horizontalGap = 4f;
            GameObject sourceChild = new GameObject("Child", typeof(RectTransform));
            sourceChild.transform.SetParent(source.transform, false);
            TaffyLayoutItem sourceItem = sourceChild.AddComponent<TaffyLayoutItem>();
            sourceItem.flexGrow = 0f;

            string prefabPath = PrefabFolder + "/DX1.prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(source, prefabPath);
            UnityEngine.Object.DestroyImmediate(source);

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.SetActive(false);
            _owned.Add(instance);
            TaffyLayoutGroup instanceGroup = instance.GetComponent<TaffyLayoutGroup>();
            TaffyLayoutItem instanceItem = instance.transform.GetChild(0).GetComponent<TaffyLayoutItem>();

            UnityEditor.Editor groupEditor = UnityEditor.Editor.CreateEditor(instanceGroup);
            UnityEditor.Editor itemEditor = UnityEditor.Editor.CreateEditor(instanceItem);
            try
            {
                groupEditor.serializedObject.Update();
                groupEditor.serializedObject.FindProperty("horizontalGap").floatValue = 19f;
                groupEditor.serializedObject.ApplyModifiedProperties();
                PrefabUtility.RecordPrefabInstancePropertyModifications(instanceGroup);

                itemEditor.serializedObject.Update();
                itemEditor.serializedObject.FindProperty("flexGrow").floatValue = 3f;
                itemEditor.serializedObject.ApplyModifiedProperties();
                PrefabUtility.RecordPrefabInstancePropertyModifications(instanceItem);

                Assert.That(instanceGroup.horizontalGap, Is.EqualTo(19f));
                Assert.That(instanceItem.flexGrow, Is.EqualTo(3f));

                GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                Assert.That(asset.GetComponent<TaffyLayoutGroup>().horizontalGap, Is.EqualTo(4f));
                Assert.That(asset.transform.GetChild(0).GetComponent<TaffyLayoutItem>().flexGrow, Is.EqualTo(0f));
                Assert.That(PrefabUtility.HasPrefabInstanceAnyOverrides(instance, false), Is.True);
            }
            finally
            {
                DestroyEditor(groupEditor);
                DestroyEditor(itemEditor);
            }
        }

        [Test]
        public void DebuggerAndSceneVisualizationRemainAvailableAfterModularization()
        {
            Assert.That(typeof(TaffyLayoutDebuggerWindow).IsSubclassOf(typeof(EditorWindow)), Is.True);
            Assert.That(typeof(TaffySceneVisualization).IsAbstract && typeof(TaffySceneVisualization).IsSealed, Is.True);

            bool previous = TaffySceneVisualization.Enabled;
            try
            {
                TaffySceneVisualization.Enabled = !previous;
                Assert.That(TaffySceneVisualization.Enabled, Is.EqualTo(!previous));
            }
            finally
            {
                TaffySceneVisualization.Enabled = previous;
            }
        }

        private T CreateInactive<T>(string name) where T : Component
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.SetActive(false);
            _owned.Add(go);
            return go.AddComponent<T>();
        }

        private static void AssertCoverage(UnityEditor.Editor editor, string[] expected)
        {
            FieldInfo field = editor.GetType().GetField("PropertyCoverage", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, editor.GetType().Name + " must expose an internal property coverage contract.");
            string[] coverage = (string[])field.GetValue(null);
            CollectionAssert.AreEquivalent(expected, coverage);
            Assert.That(coverage.Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(coverage.Length), "Property coverage contains duplicates.");

            editor.serializedObject.Update();
            for (int i = 0; i < coverage.Length; i++)
                Assert.That(editor.serializedObject.FindProperty(coverage[i]), Is.Not.Null, "Missing serialized property: " + coverage[i]);
        }

        private static void DestroyEditor(UnityEditor.Editor editor)
        {
            if (editor)
                UnityEngine.Object.DestroyImmediate(editor);
        }
    }
}
