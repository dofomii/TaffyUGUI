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
    public sealed class TaffyDX2BeginnerInspectorTests
    {
        private readonly List<GameObject> _owned = new List<GameObject>();
        private const string InspectorModePreferenceKey = "TaffyUGUI.Editor.InspectorMode";

        [TearDown]
        public void TearDown()
        {
            for (int i = _owned.Count - 1; i >= 0; i--)
            {
                if (_owned[i])
                    UnityEngine.Object.DestroyImmediate(_owned[i]);
            }
            _owned.Clear();
        }

        [Test]
        public void SimpleModeIsDefaultAndModePreferencePersists()
        {
            bool hadValue = EditorPrefs.HasKey(InspectorModePreferenceKey);
            int original = EditorPrefs.GetInt(InspectorModePreferenceKey, 0);
            Type preferences = EditorAssembly.GetType("TaffyUGUI.Editor.TaffyEditorPreferences");
            PropertyInfo mode = preferences.GetProperty("InspectorMode", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(mode, Is.Not.Null);

            try
            {
                EditorPrefs.DeleteKey(InspectorModePreferenceKey);
                object defaultMode = mode.GetValue(null, null);
                Assert.That(Convert.ToInt32(defaultMode), Is.EqualTo(0), "A fresh Editor preference must start in Simple mode.");

                object advanced = Enum.ToObject(mode.PropertyType, 1);
                mode.SetValue(null, advanced, null);
                Assert.That(EditorPrefs.GetInt(InspectorModePreferenceKey, -1), Is.EqualTo(1));
                Assert.That(Convert.ToInt32(mode.GetValue(null, null)), Is.EqualTo(1));
            }
            finally
            {
                if (hadValue)
                    EditorPrefs.SetInt(InspectorModePreferenceKey, original);
                else
                    EditorPrefs.DeleteKey(InspectorModePreferenceKey);
            }
        }

        [Test]
        public void SimpleAndAdvancedViewsUseTheSameSerializedPropertyContracts()
        {
            string[] groupAdvanced = GetCoverage(typeof(TaffyLayoutGroupEditor), "PropertyCoverage");
            string[] itemAdvanced = GetCoverage(typeof(TaffyLayoutItemEditor), "PropertyCoverage");
            string[] groupSimple = GetCoverage(EditorAssembly.GetType("TaffyUGUI.Editor.TaffyGroupQuickSetupSection"), "SimplePropertyCoverage");
            string[] itemSimple = GetCoverage(EditorAssembly.GetType("TaffyUGUI.Editor.TaffyItemEssentialsSection"), "SimplePropertyCoverage");

            CollectionAssert.IsSubsetOf(groupSimple, groupAdvanced);
            CollectionAssert.IsSubsetOf(itemSimple, itemAdvanced);
            Assert.That(groupSimple, Does.Contain("containerDisplay"));
            Assert.That(groupSimple, Does.Contain("direction"));
            Assert.That(groupSimple, Does.Contain("justifyContent"));
            Assert.That(groupSimple, Does.Contain("alignItems"));
            Assert.That(groupSimple, Does.Contain("horizontalGap"));
            Assert.That(groupSimple, Does.Contain("verticalGap"));
            Assert.That(groupSimple, Does.Contain("m_Padding"));
            Assert.That(itemSimple.Take(2).ToArray(), Is.EqualTo(new[] { "width", "height" }));
            Assert.That(itemSimple, Does.Contain("flexGrow"));
            Assert.That(itemSimple, Does.Contain("alignSelf"));
            Assert.That(itemSimple, Does.Contain("justifySelf"));

            Assert.That(typeof(TaffyLayoutGroupEditor).GetField("_quickSetupSection", BindingFlags.Instance | BindingFlags.NonPublic), Is.Not.Null);
            Assert.That(typeof(TaffyLayoutItemEditor).GetField("_parentSummarySection", BindingFlags.Instance | BindingFlags.NonPublic), Is.Not.Null);
            Assert.That(typeof(TaffyLayoutItemEditor).GetField("_essentialsSection", BindingFlags.Instance | BindingFlags.NonPublic), Is.Not.Null);
            Assert.That(typeof(TaffyLayoutGroupEditor).GetField("_authoringSections", BindingFlags.Instance | BindingFlags.NonPublic), Is.Not.Null);
            Assert.That(typeof(TaffyLayoutItemEditor).GetField("_authoringSections", BindingFlags.Instance | BindingFlags.NonPublic), Is.Not.Null);
        }

        [Test]
        public void GroupSimpleVisibilityTracksFlexGridAndInactiveModifiedSettings()
        {
            TaffyLayoutGroup group = CreateRect("GroupVisibility").AddComponent<TaffyLayoutGroup>();
            UnityEditor.Editor editor = UnityEditor.Editor.CreateEditor(group);
            Type visibility = EditorAssembly.GetType("TaffyUGUI.Editor.TaffyInspectorVisibility");
            try
            {
                object context = CreateContext(editor);

                group.containerDisplay = TaffyContainerDisplay.Flex;
                editor.serializedObject.Update();
                Assert.That(InvokeBool(visibility, "GroupShowsFlexEssentials", context), Is.True);
                Assert.That(InvokeBool(visibility, "GroupShowsGridEssentials", context), Is.False);
                Assert.That(InvokeBool(visibility, "HasInactiveGroupGridSettings", context), Is.False);

                group.gridColumns.Add(TaffyGridTrack.Fraction(1f));
                editor.serializedObject.Update();
                Assert.That(InvokeBool(visibility, "HasInactiveGroupGridSettings", context), Is.True);

                group.containerDisplay = TaffyContainerDisplay.Grid;
                editor.serializedObject.Update();
                Assert.That(InvokeBool(visibility, "GroupShowsGridEssentials", context), Is.True);
                Assert.That(InvokeBool(visibility, "GroupShowsFlexEssentials", context), Is.False);
                Assert.That(InvokeBool(visibility, "HasInactiveGroupFlexSettings", context), Is.False);

                group.direction = TaffyFlexDirection.Column;
                editor.serializedObject.Update();
                Assert.That(InvokeBool(visibility, "HasInactiveGroupFlexSettings", context), Is.True);
            }
            finally
            {
                DestroyEditor(editor);
            }
        }

        [Test]
        public void MixedGroupDisplaySelectionAvoidsAmbiguousSimpleControls()
        {
            TaffyLayoutGroup flex = CreateRect("FlexGroup").AddComponent<TaffyLayoutGroup>();
            TaffyLayoutGroup grid = CreateRect("GridGroup").AddComponent<TaffyLayoutGroup>();
            flex.containerDisplay = TaffyContainerDisplay.Flex;
            grid.containerDisplay = TaffyContainerDisplay.Grid;

            UnityEditor.Editor editor = UnityEditor.Editor.CreateEditor(new UnityEngine.Object[] { flex, grid });
            Type visibility = EditorAssembly.GetType("TaffyUGUI.Editor.TaffyInspectorVisibility");
            try
            {
                object context = CreateContext(editor);
                editor.serializedObject.Update();
                Assert.That(InvokeBool(visibility, "GroupHasMixedDisplay", context), Is.True);
                Assert.That(InvokeBool(visibility, "GroupShowsFlexEssentials", context), Is.False);
                Assert.That(InvokeBool(visibility, "GroupShowsGridEssentials", context), Is.False);
            }
            finally
            {
                DestroyEditor(editor);
            }
        }

        [Test]
        public void ParentContextDetectsFlexGridBlockAndNoParent()
        {
            GameObject parentObject = CreateRect("Parent");
            TaffyLayoutGroup parent = parentObject.AddComponent<TaffyLayoutGroup>();
            GameObject childObject = CreateRect("Child");
            childObject.transform.SetParent(parentObject.transform, false);
            TaffyLayoutItem item = childObject.AddComponent<TaffyLayoutItem>();

            UnityEditor.Editor editor = UnityEditor.Editor.CreateEditor(item);
            try
            {
                object context = CreateContext(editor);
                Type visibility = EditorAssembly.GetType("TaffyUGUI.Editor.TaffyInspectorVisibility");

                parent.containerDisplay = TaffyContainerDisplay.Flex;
                parent.direction = TaffyFlexDirection.Row;
                Assert.That(InvokeBool(visibility, "ParentIsFlex", context), Is.True);
                Assert.That(InvokeBool(visibility, "ParentIsGrid", context), Is.False);
                StringAssert.Contains("Flex", InvokeString(visibility, "ParentSummary", context));
                StringAssert.Contains("Horizontal", InvokeString(visibility, "ParentSummary", context));
                StringAssert.Contains("horizontal", InvokeString(visibility, "FlexGrowHelp", context).ToLowerInvariant());

                parent.containerDisplay = TaffyContainerDisplay.Grid;
                Assert.That(InvokeBool(visibility, "ParentIsGrid", context), Is.True);
                Assert.That(InvokeBool(visibility, "ParentIsFlex", context), Is.False);
                StringAssert.Contains("Grid", InvokeString(visibility, "ParentSummary", context));

                parent.containerDisplay = TaffyContainerDisplay.Block;
                Assert.That(InvokeBool(visibility, "ParentIsBlockLike", context), Is.True);
                StringAssert.Contains("Block", InvokeString(visibility, "ParentSummary", context));
            }
            finally
            {
                DestroyEditor(editor);
            }

            GameObject orphanObject = CreateRect("Orphan");
            TaffyLayoutItem orphan = orphanObject.AddComponent<TaffyLayoutItem>();
            UnityEditor.Editor orphanEditor = UnityEditor.Editor.CreateEditor(orphan);
            try
            {
                object orphanContext = CreateContext(orphanEditor);
                Type visibility = EditorAssembly.GetType("TaffyUGUI.Editor.TaffyInspectorVisibility");
                Assert.That(InvokeBool(visibility, "ParentIsFlex", orphanContext), Is.False);
                Assert.That(InvokeBool(visibility, "ParentIsGrid", orphanContext), Is.False);
                Assert.That(InvokeString(visibility, "ParentSummary", orphanContext), Is.EqualTo("No TaffyLayoutGroup parent"));
            }
            finally
            {
                DestroyEditor(orphanEditor);
            }
        }

        [Test]
        public void InactiveModifiedFlexGridAndBlockSettingsRemainDetectableInSimpleMode()
        {
            TaffyLayoutItem item = CreateRect("InactiveOverrides").AddComponent<TaffyLayoutItem>();
            UnityEditor.Editor editor = UnityEditor.Editor.CreateEditor(item);
            Type visibility = EditorAssembly.GetType("TaffyUGUI.Editor.TaffyInspectorVisibility");
            try
            {
                object context = CreateContext(editor);
                editor.serializedObject.Update();
                Assert.That(InvokeBool(visibility, "HasInactiveFlexOverrides", context), Is.False);
                Assert.That(InvokeBool(visibility, "HasInactiveGridOverrides", context), Is.False);
                Assert.That(InvokeBool(visibility, "HasInactiveBlockOverrides", context), Is.False);

                item.flexGrow = 2f;
                item.gridColumnStart = TaffyGridPlacement.Line(1);
                item.floatMode = TaffyFloat.Left;
                editor.serializedObject.Update();

                Assert.That(InvokeBool(visibility, "HasInactiveFlexOverrides", context), Is.True);
                Assert.That(InvokeBool(visibility, "HasInactiveGridOverrides", context), Is.True);
                Assert.That(InvokeBool(visibility, "HasInactiveBlockOverrides", context), Is.True);
            }
            finally
            {
                DestroyEditor(editor);
            }
        }

        [Test]
        public void EveryAdvancedSerializedPropertyHasMeaningfulCentralTooltipContent()
        {
            Type content = EditorAssembly.GetType("TaffyUGUI.Editor.TaffyEditorContent");
            MethodInfo tooltip = content.GetMethod("TooltipForProperty", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(tooltip, Is.Not.Null);

            string[] allProperties = GetCoverage(typeof(TaffyLayoutGroupEditor), "PropertyCoverage")
                .Concat(GetCoverage(typeof(TaffyLayoutItemEditor), "PropertyCoverage"))
                .Distinct(StringComparer.Ordinal)
                .ToArray();

            foreach (string propertyName in allProperties)
            {
                string value = (string)tooltip.Invoke(null, new object[] { propertyName });
                Assert.That(value, Is.Not.Null.And.Not.Empty, propertyName + " is missing tooltip content.");
                Assert.That(value, Is.Not.EqualTo("TaffyUGUI layout authoring property."), propertyName + " is using placeholder tooltip content.");
            }
        }

        [Test]
        public void ItemContextUsesParentAxisForGrowExplanation()
        {
            GameObject parentObject = CreateRect("ColumnParent");
            TaffyLayoutGroup parent = parentObject.AddComponent<TaffyLayoutGroup>();
            parent.containerDisplay = TaffyContainerDisplay.Flex;
            parent.direction = TaffyFlexDirection.Column;

            GameObject childObject = CreateRect("ColumnChild");
            childObject.transform.SetParent(parentObject.transform, false);
            TaffyLayoutItem item = childObject.AddComponent<TaffyLayoutItem>();
            UnityEditor.Editor editor = UnityEditor.Editor.CreateEditor(item);
            try
            {
                object context = CreateContext(editor);
                Type visibility = EditorAssembly.GetType("TaffyUGUI.Editor.TaffyInspectorVisibility");
                string help = InvokeString(visibility, "FlexGrowHelp", context);
                StringAssert.Contains("vertical", help.ToLowerInvariant());
            }
            finally
            {
                DestroyEditor(editor);
            }
        }

        private static Assembly EditorAssembly => typeof(TaffyLayoutGroupEditor).Assembly;

        private GameObject CreateRect(string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.SetActive(false);
            _owned.Add(go);
            return go;
        }

        private static object CreateContext(UnityEditor.Editor editor)
        {
            Type contextType = EditorAssembly.GetType("TaffyUGUI.Editor.TaffyInspectorContext");
            ConstructorInfo constructor = contextType.GetConstructor(
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                new[] { typeof(UnityEditor.Editor) },
                null);
            Assert.That(constructor, Is.Not.Null);
            return constructor.Invoke(new object[] { editor });
        }

        private static bool InvokeBool(Type type, string methodName, object context)
        {
            MethodInfo method = type.GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, methodName);
            return (bool)method.Invoke(null, new[] { context });
        }

        private static string InvokeString(Type type, string methodName, object context)
        {
            MethodInfo method = type.GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, methodName);
            return (string)method.Invoke(null, new[] { context });
        }

        private static string[] GetCoverage(Type type, string fieldName)
        {
            Assert.That(type, Is.Not.Null);
            FieldInfo field = type.GetField(fieldName, BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, type.FullName + "." + fieldName);
            return (string[])field.GetValue(null);
        }

        private static void DestroyEditor(UnityEditor.Editor editor)
        {
            if (editor)
                UnityEngine.Object.DestroyImmediate(editor);
        }
    }
}
