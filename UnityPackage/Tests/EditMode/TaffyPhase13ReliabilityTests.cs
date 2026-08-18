using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TaffyUGUI.Tests
{
    public sealed class TaffyPhase13ReliabilityTests
    {
        private const string PrefabFolder = "Assets/__TaffyUGUIPhase13Tests";
        private const string PrefabPath = PrefabFolder + "/Lifecycle.prefab";
        private static readonly FieldInfo ContextField = typeof(TaffyLayoutGroup).GetField(
            "_context", BindingFlags.Instance | BindingFlags.NonPublic);

        [TearDown]
        public void TearDown()
        {
            if (AssetDatabase.IsValidFolder(PrefabFolder))
                AssetDatabase.DeleteAsset(PrefabFolder);
        }

        [Test]
        public void RepeatedEnableDisableRecreatesAndReleasesNativeContext()
        {
            Assert.That(ContextField, Is.Not.Null);
            GameObject rootObject = CreateLayout("Phase13Lifecycle", out RectTransform root, out RectTransform child);
            TaffyLayoutGroup group = rootObject.GetComponent<TaffyLayoutGroup>();
            var observedHandles = new HashSet<ulong>();

            try
            {
                for (int iteration = 0; iteration < 100; iteration++)
                {
                    if (!group.enabled)
                        group.enabled = true;

                    ulong context = GetContext(group);
                    Assert.That(context, Is.Not.EqualTo(0UL), $"iteration {iteration} did not create a native context");
                    Assert.That(observedHandles.Add(context), Is.True,
                        $"iteration {iteration} reused a stale native context handle");

                    LayoutRebuilder.ForceRebuildLayoutImmediate(root);
                    Assert.That(child.rect.width, Is.EqualTo(40f).Within(0.1f));
                    Assert.That(child.rect.height, Is.EqualTo(20f).Within(0.1f));

                    group.enabled = false;
                    Assert.That(GetContext(group), Is.EqualTo(0UL),
                        $"iteration {iteration} did not release its native context");
                }
            }
            finally
            {
                Object.DestroyImmediate(rootObject);
            }
        }

        [Test]
        public void RepeatedPrefabAndSceneLifecyclePreservesPrefabAndLayout()
        {
            Assert.That(ContextField, Is.Not.Null);
            if (!AssetDatabase.IsValidFolder(PrefabFolder))
                AssetDatabase.CreateFolder("Assets", "__TaffyUGUIPhase13Tests");

            GameObject source = CreateLayout("Phase13PrefabSource", out _, out _);
            GameObject prefab;
            try
            {
                prefab = PrefabUtility.SaveAsPrefabAsset(source, PrefabPath);
                Assert.That(prefab, Is.Not.Null);
            }
            finally
            {
                Object.DestroyImmediate(source);
            }

            for (int iteration = 0; iteration < 50; iteration++)
            {
                Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                Assert.That(instance, Is.Not.Null);
                Assert.That(instance.scene, Is.EqualTo(scene));

                TaffyLayoutGroup group = instance.GetComponent<TaffyLayoutGroup>();
                RectTransform root = instance.GetComponent<RectTransform>();
                RectTransform child = instance.transform.GetChild(0).GetComponent<RectTransform>();
                Assert.That(GetContext(group), Is.Not.EqualTo(0UL));

                LayoutRebuilder.ForceRebuildLayoutImmediate(root);
                Assert.That(child.rect.width, Is.EqualTo(40f).Within(0.1f));
                Assert.That(child.rect.height, Is.EqualTo(20f).Within(0.1f));
            }

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            GameObject persistedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Assert.That(persistedPrefab, Is.Not.Null);
            Assert.That(persistedPrefab.GetComponent<TaffyLayoutGroup>(), Is.Not.Null);
            Assert.That(persistedPrefab.transform.childCount, Is.EqualTo(1));
            TaffyLayoutItem persistedItem = persistedPrefab.transform.GetChild(0).GetComponent<TaffyLayoutItem>();
            Assert.That(persistedItem, Is.Not.Null);
            Assert.That(persistedItem.width.unit, Is.EqualTo(TaffyUnit.Points));
            Assert.That(persistedItem.width.value, Is.EqualTo(40f).Within(0.001f));
            Assert.That(persistedItem.height.unit, Is.EqualTo(TaffyUnit.Points));
            Assert.That(persistedItem.height.value, Is.EqualTo(20f).Within(0.001f));
        }

        private static GameObject CreateLayout(string name, out RectTransform root, out RectTransform child)
        {
            var rootObject = new GameObject(name, typeof(RectTransform), typeof(TaffyLayoutGroup));
            root = rootObject.GetComponent<RectTransform>();
            root.anchorMin = new Vector2(0f, 1f);
            root.anchorMax = new Vector2(0f, 1f);
            root.pivot = new Vector2(0f, 1f);
            root.sizeDelta = new Vector2(320f, 120f);

            TaffyLayoutGroup group = rootObject.GetComponent<TaffyLayoutGroup>();
            group.direction = TaffyFlexDirection.Row;
            group.alignItems = TaffyAlign.Start;

            var childObject = new GameObject("Child", typeof(RectTransform), typeof(TaffyLayoutItem));
            child = childObject.GetComponent<RectTransform>();
            child.SetParent(root, false);
            TaffyLayoutItem item = childObject.GetComponent<TaffyLayoutItem>();
            item.width = TaffyLength.Points(40f);
            item.height = TaffyLength.Points(20f);
            item.flexShrink = 0f;
            item.measurement = TaffyMeasurementMode.Disabled;
            return rootObject;
        }

        private static ulong GetContext(TaffyLayoutGroup group)
        {
            return (ulong)ContextField.GetValue(group);
        }
    }
}
