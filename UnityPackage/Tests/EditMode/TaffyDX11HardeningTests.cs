using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace TaffyUGUI.Tests
{
    public sealed class TaffyDX11HardeningTests
    {
        private static Assembly EditorAssembly => typeof(TaffyUGUI.Editor.TaffyLayoutGroupEditor).Assembly;

        [Test]
        public void LargeMultiSelectionInspectorStateFitsPracticalEditorBudget()
        {
            const int targetCount = 128;
            const int passes = 8;
            var objects = new List<GameObject>(targetCount);
            UnityEditor.Editor editor = null;
            try
            {
                var targets = new UnityEngine.Object[targetCount];
                for (int i = 0; i < targetCount; i++)
                {
                    var go = new GameObject("DX11_Item_" + i, typeof(RectTransform), typeof(TaffyLayoutItem));
                    go.hideFlags = HideFlags.HideAndDontSave;
                    objects.Add(go);
                    targets[i] = go.GetComponent<TaffyLayoutItem>();
                }

                editor = UnityEditor.Editor.CreateEditor(targets, typeof(TaffyUGUI.Editor.TaffyLayoutItemEditor));
                Assert.That(editor, Is.Not.Null);

                Type contextType = EditorAssembly.GetType("TaffyUGUI.Editor.TaffyInspectorContext");
                Type healthType = EditorAssembly.GetType("TaffyUGUI.Editor.TaffyLayoutHealth");
                ConstructorInfo contextCtor = contextType?.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { typeof(UnityEditor.Editor) }, null);
                MethodInfo evaluate = healthType?.GetMethod("Evaluate", BindingFlags.Static | BindingFlags.NonPublic);
                Assert.That(contextCtor, Is.Not.Null);
                Assert.That(evaluate, Is.Not.Null);

                editor.serializedObject.Update();
                contextCtor.Invoke(new object[] { editor });
                evaluate.Invoke(null, new object[] { targets });

                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                long memoryBefore = GC.GetTotalMemory(true);
                var stopwatch = Stopwatch.StartNew();
                for (int pass = 0; pass < passes; pass++)
                {
                    editor.serializedObject.Update();
                    object context = contextCtor.Invoke(new object[] { editor });
                    Assert.That(context, Is.Not.Null);
                    object health = evaluate.Invoke(null, new object[] { targets });
                    Assert.That(health, Is.Not.Null);
                }
                stopwatch.Stop();
                long memoryAfter = GC.GetTotalMemory(false);
                long retainedBytes = Math.Max(0L, memoryAfter - memoryBefore);

                TestContext.Out.WriteLine(
                    "DX11 inspector profile: {0} targets × {1} passes = {2} ms, retained memory delta {3} bytes",
                    targetCount,
                    passes,
                    stopwatch.ElapsedMilliseconds,
                    retainedBytes);

                Assert.That(stopwatch.ElapsedMilliseconds, Is.LessThan(5000), "Representative large multi-selection inspector-state evaluation is too slow for practical Editor use.");
                Assert.That(retainedBytes, Is.LessThan(64L * 1024L * 1024L), "Representative large multi-selection inspector-state evaluation retains an excessive amount of managed memory.");
            }
            finally
            {
                if (editor)
                    UnityEngine.Object.DestroyImmediate(editor);
                for (int i = 0; i < objects.Count; i++)
                {
                    if (objects[i])
                        UnityEngine.Object.DestroyImmediate(objects[i]);
                }
            }
        }

        [Test]
        public void DiagnosticEvaluationDoesNotDirtyOrRebuildTargets()
        {
            var root = new GameObject("DX11_Group", typeof(RectTransform), typeof(TaffyLayoutGroup));
            var child = new GameObject("DX11_Item", typeof(RectTransform), typeof(TaffyLayoutItem));
            root.hideFlags = HideFlags.HideAndDontSave;
            child.hideFlags = HideFlags.HideAndDontSave;
            child.transform.SetParent(root.transform, false);
            try
            {
                TaffyLayoutGroup group = root.GetComponent<TaffyLayoutGroup>();
                TaffyLayoutItem item = child.GetComponent<TaffyLayoutItem>();
                int suppressedBefore = group.SuppressedRebuildRequestCount;
                EditorUtility.ClearDirty(group);
                EditorUtility.ClearDirty(item);

                Type healthType = EditorAssembly.GetType("TaffyUGUI.Editor.TaffyLayoutHealth");
                MethodInfo evaluate = healthType?.GetMethod("Evaluate", BindingFlags.Static | BindingFlags.NonPublic);
                Assert.That(evaluate, Is.Not.Null);
                object health = evaluate.Invoke(null, new object[] { new UnityEngine.Object[] { group, item } });

                Assert.That(health, Is.Not.Null);
                Assert.That(group.SuppressedRebuildRequestCount, Is.EqualTo(suppressedBefore));
                Assert.That(EditorUtility.IsDirty(group), Is.False);
                Assert.That(EditorUtility.IsDirty(item), Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(child);
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void PresetCatalogCachesAssetScanUntilProjectInvalidation()
        {
            Type catalog = EditorAssembly.GetType("TaffyUGUI.Editor.TaffyPresetCatalog");
            MethodInfo reset = catalog?.GetMethod("ResetCacheForTests", BindingFlags.Static | BindingFlags.NonPublic);
            MethodInfo load = catalog?.GetMethod("LoadAll", BindingFlags.Static | BindingFlags.NonPublic);
            MethodInfo invalidate = catalog?.GetMethod("Invalidate", BindingFlags.Static | BindingFlags.NonPublic);
            PropertyInfo scanCount = catalog?.GetProperty("ScanCountForTests", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(reset, Is.Not.Null);
            Assert.That(load, Is.Not.Null);
            Assert.That(invalidate, Is.Not.Null);
            Assert.That(scanCount, Is.Not.Null);

            reset.Invoke(null, null);
            IList first = load.Invoke(null, null) as IList;
            IList second = load.Invoke(null, null) as IList;
            Assert.That(first, Is.Not.Null);
            Assert.That(second, Is.Not.Null);
            Assert.That((int)scanCount.GetValue(null), Is.EqualTo(1), "Repeated preset-browser refreshes must reuse the cached project scan.");

            invalidate.Invoke(null, null);
            IList third = load.Invoke(null, null) as IList;
            Assert.That(third, Is.Not.Null);
            Assert.That((int)scanCount.GetValue(null), Is.EqualTo(2), "A project change must invalidate the preset catalog cache.");
        }

        [Test]
        public void OptionalSceneOverlaysDefaultOffAndPersistThroughEditorPrefs()
        {
            Type preferences = EditorAssembly.GetType("TaffyUGUI.Editor.TaffySceneOverlayPreferences");
            MethodInfo reset = preferences?.GetMethod("ResetForTests", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(reset, Is.Not.Null);
            reset.Invoke(null, null);
            try
            {
                string[] optional = { "PaddingBounds", "ItemMargins", "FlexAxes", "GapMarkers", "ComputedSizeLabels" };
                foreach (string name in optional)
                {
                    PropertyInfo property = preferences.GetProperty(name, BindingFlags.Static | BindingFlags.NonPublic);
                    Assert.That(property, Is.Not.Null, name);
                    Assert.That((bool)property.GetValue(null), Is.False, name + " should remain opt-in to avoid unnecessary Scene View work.");
                    property.SetValue(null, true);
                    Assert.That((bool)property.GetValue(null), Is.True, name + " should persist through EditorPrefs-backed state.");
                }
            }
            finally
            {
                reset.Invoke(null, null);
            }
        }

        [Test]
        public void EditorPreferencesReadPersistentStorageRatherThanStaticCachedState()
        {
            const string modeKey = "TaffyUGUI.Editor.InspectorMode";
            const string densityKey = "TaffyUGUI.Editor.InspectorDensity";
            Type preferences = EditorAssembly.GetType("TaffyUGUI.Editor.TaffyEditorPreferences");
            PropertyInfo mode = preferences?.GetProperty("InspectorMode", BindingFlags.Static | BindingFlags.NonPublic);
            PropertyInfo density = preferences?.GetProperty("InspectorDensity", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(mode, Is.Not.Null);
            Assert.That(density, Is.Not.Null);

            int oldMode = EditorPrefs.GetInt(modeKey, 0);
            int oldDensity = EditorPrefs.GetInt(densityKey, 0);
            try
            {
                EditorPrefs.SetInt(modeKey, 1);
                EditorPrefs.SetInt(densityKey, 1);
                Assert.That(Convert.ToInt32(mode.GetValue(null)), Is.EqualTo(1));
                Assert.That(Convert.ToInt32(density.GetValue(null)), Is.EqualTo(1));

                EditorPrefs.SetInt(modeKey, 0);
                EditorPrefs.SetInt(densityKey, 0);
                Assert.That(Convert.ToInt32(mode.GetValue(null)), Is.EqualTo(0));
                Assert.That(Convert.ToInt32(density.GetValue(null)), Is.EqualTo(0));
            }
            finally
            {
                EditorPrefs.SetInt(modeKey, oldMode);
                EditorPrefs.SetInt(densityKey, oldDensity);
            }
        }
    }
}
