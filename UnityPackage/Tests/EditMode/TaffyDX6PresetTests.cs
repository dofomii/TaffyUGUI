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
    public sealed class TaffyDX6PresetTests
    {
        private const string PresetFolder = "Assets/__TaffyUGUIDX6Tests";
        private static readonly Assembly EditorAssembly = typeof(TaffyLayoutGroupEditor).Assembly;
        private readonly List<GameObject> _owned = new List<GameObject>();

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
            if (AssetDatabase.IsValidFolder(PresetFolder))
                AssetDatabase.DeleteAsset(PresetFolder);
        }

        [Test]
        public void BuiltInLibraryContainsRequestedContainerAndItemPatterns()
        {
            List<object> presets = BuiltIns();
            string[] names = presets.Select(preset => GetString(preset, "DisplayName")).ToArray();
            CollectionAssert.IsSupersetOf(names, new[]
            {
                "Horizontal Row",
                "Vertical Stack",
                "Centered Panel",
                "Toolbar",
                "Sidebar + Content",
                "Scrollable List Content",
                "Responsive / Wrapping Cards",
                "Flexible Item",
                "Spacer",
                "Fit Content Item",
            });

            foreach (object preset in presets)
            {
                Assert.That(GetString(preset, "Preview"), Is.Not.Empty);
                Assert.That(OwnedPaths(preset).Count, Is.GreaterThan(0));
            }
        }

        [Test]
        public void BuiltInContainerPresetWritesExactOwnedValuesOnly()
        {
            TaffyLayoutGroup group = CreateRect("Group").AddComponent<TaffyLayoutGroup>();
            group.containerDisplay = TaffyContainerDisplay.Grid;
            group.direction = TaffyFlexDirection.ColumnReverse;
            group.wrap = TaffyFlexWrap.Wrap;
            group.horizontalGap = 37f;
            group.verticalGap = 29f;
            group.textAlign = TaffyTextAlign.LegacyCenter;

            object preset = FindBuiltIn("Horizontal Row");
            Apply(preset, group);

            Assert.That(group.containerDisplay, Is.EqualTo(TaffyContainerDisplay.Flex));
            Assert.That(group.direction, Is.EqualTo(TaffyFlexDirection.Row));
            Assert.That(group.wrap, Is.EqualTo(TaffyFlexWrap.NoWrap));
            Assert.That(group.horizontalGap, Is.EqualTo(37f));
            Assert.That(group.verticalGap, Is.EqualTo(29f));
            Assert.That(group.textAlign, Is.EqualTo(TaffyTextAlign.LegacyCenter));
        }

        [Test]
        public void BuiltInItemPresetsProduceExactSerializedIntent()
        {
            TaffyLayoutItem flexible = CreateRect("Flexible").AddComponent<TaffyLayoutItem>();
            flexible.flexGrow = 0f;
            flexible.flexShrink = 0f;
            Apply(FindBuiltIn("Flexible Item"), flexible);
            Assert.That(flexible.flexBasis.unit, Is.EqualTo(TaffyUnit.Auto));
            Assert.That(flexible.flexGrow, Is.EqualTo(1f));
            Assert.That(flexible.flexShrink, Is.EqualTo(1f));

            TaffyLayoutItem spacer = CreateRect("Spacer").AddComponent<TaffyLayoutItem>();
            Apply(FindBuiltIn("Spacer"), spacer);
            Assert.That(spacer.flexBasis.unit, Is.EqualTo(TaffyUnit.Points));
            Assert.That(spacer.flexBasis.value, Is.EqualTo(0f));
            Assert.That(spacer.flexGrow, Is.EqualTo(1f));
            Assert.That(spacer.measurement, Is.EqualTo(TaffyMeasurementMode.Disabled));

            TaffyLayoutItem fit = CreateRect("Fit").AddComponent<TaffyLayoutItem>();
            fit.width = TaffyLength.Points(120f);
            fit.height = TaffyLength.Percent(0.5f);
            Apply(FindBuiltIn("Fit Content Item"), fit);
            Assert.That(fit.width.unit, Is.EqualTo(TaffyUnit.Auto));
            Assert.That(fit.height.unit, Is.EqualTo(TaffyUnit.Auto));
        }

        [Test]
        public void ProjectPresetSavesReloadsAndAppliesCapturedSerializedState()
        {
            EnsurePresetFolder();
            TaffyLayoutGroup source = CreateRect("Source").AddComponent<TaffyLayoutGroup>();
            source.containerDisplay = TaffyContainerDisplay.Flex;
            source.direction = TaffyFlexDirection.Column;
            source.horizontalGap = 13f;
            source.verticalGap = 17f;
            source.justifyContent = TaffyJustify.Center;
            source.alignItems = TaffyAlign.End;

            string path = PresetFolder + "/ProjectContainer.asset";
            UnityEngine.Object saved = SaveProjectPreset(source, path, "Project Container");
            Assert.That(saved, Is.Not.Null);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            UnityEngine.Object reloaded = AssetDatabase.LoadMainAssetAtPath(path);
            Assert.That(reloaded, Is.Not.Null);
            object data = GetProperty(reloaded, "Data");
            Assert.That(GetString(data, "DisplayName"), Is.EqualTo("Project Container"));

            TaffyLayoutGroup target = CreateRect("Target").AddComponent<TaffyLayoutGroup>();
            target.direction = TaffyFlexDirection.RowReverse;
            target.horizontalGap = 1f;
            Apply(data, target);

            Assert.That(target.direction, Is.EqualTo(TaffyFlexDirection.Column));
            Assert.That(target.horizontalGap, Is.EqualTo(13f));
            Assert.That(target.verticalGap, Is.EqualTo(17f));
            Assert.That(target.justifyContent, Is.EqualTo(TaffyJustify.Center));
            Assert.That(target.alignItems, Is.EqualTo(TaffyAlign.End));
        }

        [Test]
        public void CatalogAggregatesBuiltInAndProjectPresets()
        {
            EnsurePresetFolder();
            TaffyLayoutItem source = CreateRect("SourceItem").AddComponent<TaffyLayoutItem>();
            SaveProjectPreset(source, PresetFolder + "/ProjectItem.asset", "My Project Item");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Type catalog = EditorAssembly.GetType("TaffyUGUI.Editor.TaffyPresetCatalog");
            MethodInfo load = catalog.GetMethod("LoadAll", BindingFlags.Static | BindingFlags.NonPublic);
            var names = new List<string>();
            int projectCount = 0;
            foreach (object entry in (IEnumerable)load.Invoke(null, null))
            {
                object data = GetProperty(entry, "Data");
                names.Add(GetString(data, "DisplayName"));
                if ((bool)GetProperty(entry, "IsProjectPreset"))
                    projectCount++;
            }

            CollectionAssert.Contains(names, "Horizontal Row");
            CollectionAssert.Contains(names, "My Project Item");
            Assert.That(projectCount, Is.GreaterThanOrEqualTo(1));
        }

        [Test]
        public void PresetApplicationSupportsMultiObjectUndoAndPreservesUnownedFields()
        {
            TaffyLayoutItem first = CreateRect("First").AddComponent<TaffyLayoutItem>();
            TaffyLayoutItem second = CreateRect("Second").AddComponent<TaffyLayoutItem>();
            first.flexGrow = 0.25f;
            second.flexGrow = 0.5f;
            first.aspectRatio = 1.25f;
            second.aspectRatio = 1.5f;

            object preset = FindBuiltIn("Flexible Item");
            ApplyMany(preset, new UnityEngine.Object[] { first, second });
            Assert.That(first.flexGrow, Is.EqualTo(1f));
            Assert.That(second.flexGrow, Is.EqualTo(1f));
            Assert.That(first.aspectRatio, Is.EqualTo(1.25f));
            Assert.That(second.aspectRatio, Is.EqualTo(1.5f));

            Undo.PerformUndo();
            Assert.That(first.flexGrow, Is.EqualTo(0.25f));
            Assert.That(second.flexGrow, Is.EqualTo(0.5f));
            Assert.That(first.aspectRatio, Is.EqualTo(1.25f));
            Assert.That(second.aspectRatio, Is.EqualTo(1.5f));
        }

        [Test]
        public void PresetBrowserProvidesSearchCategoryPreviewApplyAndOpenInfrastructure()
        {
            Type browser = EditorAssembly.GetType("TaffyUGUI.Editor.TaffyPresetBrowserWindow");
            Type editor = EditorAssembly.GetType("TaffyUGUI.Editor.TaffyProjectPresetEditor");
            Assert.That(browser, Is.Not.Null);
            Assert.That(editor, Is.Not.Null);
            string[] browserMethods = browser.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static)
                .Select(method => method.Name)
                .ToArray();
            CollectionAssert.IsSupersetOf(browserMethods, new[] { "FilteredEntries", "Categories", "DrawEntry", "ApplyToSelection", "SaveCurrent" });
        }

        private static List<object> BuiltIns()
        {
            Type type = EditorAssembly.GetType("TaffyUGUI.Editor.TaffyBuiltInPresets");
            PropertyInfo property = type.GetProperty("All", BindingFlags.Static | BindingFlags.NonPublic);
            var result = new List<object>();
            foreach (object preset in (IEnumerable)property.GetValue(null))
                result.Add(preset);
            return result;
        }

        private static object FindBuiltIn(string name)
        {
            return BuiltIns().Single(preset => GetString(preset, "DisplayName") == name);
        }

        private static List<string> OwnedPaths(object preset)
        {
            return ((IEnumerable)GetProperty(preset, "OwnedPropertyPaths")).Cast<object>().Select(value => value.ToString()).ToList();
        }

        private static void Apply(object preset, UnityEngine.Object target)
        {
            ApplyMany(preset, new[] { target });
        }

        private static void ApplyMany(object preset, UnityEngine.Object[] targets)
        {
            Type application = EditorAssembly.GetType("TaffyUGUI.Editor.TaffyPresetApplication");
            MethodInfo method = application.GetMethod("Apply", BindingFlags.Static | BindingFlags.NonPublic);
            method.Invoke(null, new object[] { preset, targets });
        }

        private static UnityEngine.Object SaveProjectPreset(UnityEngine.Object source, string path, string name)
        {
            Type capture = EditorAssembly.GetType("TaffyUGUI.Editor.TaffyPresetCapture");
            MethodInfo method = capture.GetMethod("SaveProjectPreset", BindingFlags.Static | BindingFlags.NonPublic);
            return (UnityEngine.Object)method.Invoke(null, new object[] { source, path, name });
        }

        private static object GetProperty(object target, string name)
        {
            Assert.That(target, Is.Not.Null);
            PropertyInfo property = target.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.That(property, Is.Not.Null, "Missing property " + name + " on " + target.GetType().FullName);
            return property.GetValue(target);
        }

        private static string GetString(object target, string name)
        {
            return (string)GetProperty(target, name);
        }

        private void EnsurePresetFolder()
        {
            if (!AssetDatabase.IsValidFolder(PresetFolder))
                AssetDatabase.CreateFolder("Assets", "__TaffyUGUIDX6Tests");
        }

        private GameObject CreateRect(string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.SetActive(false);
            _owned.Add(go);
            return go;
        }
    }
}
