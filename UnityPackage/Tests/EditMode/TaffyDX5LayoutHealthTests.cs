using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TaffyUGUI.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace TaffyUGUI.Tests
{
    public sealed class TaffyDX5LayoutHealthTests
    {
        private const string PrefabFolder = "Assets/__TaffyUGUIDX5Tests";
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
            if (AssetDatabase.IsValidFolder(PrefabFolder))
                AssetDatabase.DeleteAsset(PrefabFolder);
        }

        [Test]
        public void MissingParentRuleExplainsAndRepairsItemSetup()
        {
            TaffyLayoutItem item = CreateRect("Orphan").AddComponent<TaffyLayoutItem>();
            AssertHasId(Evaluate(item), "item.missing-parent");

            GameObject parent = CreateRect("Parent");
            item.transform.SetParent(parent.transform, false);
            object result = FindResult(Evaluate(item), "item.missing-parent");
            InvokeFix(result, "item.add-parent");
            Assert.That(parent.GetComponent<TaffyLayoutGroup>(), Is.Not.Null);
        }

        [Test]
        public void CompetingUnityLayoutRuleDetectsAndDisablesOwnerWithUndo()
        {
            GameObject parent = CreateRect("UnityParent");
            HorizontalLayoutGroup unity = parent.AddComponent<HorizontalLayoutGroup>();
            TaffyLayoutItem item = CreateChildItem(parent, "Child");

            object result = FindResult(Evaluate(item), "group.competing-unity-layout.HorizontalLayoutGroup");
            Assert.That(result, Is.Not.Null);
            InvokeFirstFix(result);
            Assert.That(unity.enabled, Is.False);

            Undo.PerformUndo();
            Assert.That(unity.enabled, Is.True);
        }

        [Test]
        public void ContentSizeFitterAndAspectRatioRulesExposeOwnershipFixes()
        {
            GameObject go = CreateRect("Content");
            TaffyLayoutGroup group = go.AddComponent<TaffyLayoutGroup>();
            ContentSizeFitter fitter = go.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            AspectRatioFitter aspect = go.AddComponent<AspectRatioFitter>();
            aspect.aspectMode = AspectRatioFitter.AspectMode.FitInParent;

            object health = Evaluate(group);
            AssertHasId(health, "group.content-size-fitter.horizontal");
            AssertHasId(health, "aspect-ratio-fitter");

            object fitterResult = FindResult(health, "group.content-size-fitter.horizontal");
            InvokeFix(fitterResult, "fitter.taffy.horizontal");
            Assert.That(fitter.horizontalFit, Is.EqualTo(ContentSizeFitter.FitMode.Unconstrained));
        }

        [Test]
        public void ExistingScrollRectIntegrationWarningsAreSurfaced()
        {
            GameObject scrollGo = CreateRect("Scroll");
            ScrollRect scroll = scrollGo.AddComponent<ScrollRect>();
            GameObject contentGo = CreateRect("Content");
            contentGo.transform.SetParent(scrollGo.transform, false);
            scroll.content = contentGo.GetComponent<RectTransform>();
            scroll.vertical = true;
            scroll.horizontal = false;
            TaffyLayoutGroup group = contentGo.AddComponent<TaffyLayoutGroup>();
            ContentSizeFitter fitter = contentGo.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            object health = Evaluate(group);
            Assert.That(ResultIds(health).Any(id => id.StartsWith("group.integration.", StringComparison.Ordinal)), Is.True);
        }

        [Test]
        public void IntrinsicMeasurementRuleOnlyFlagsContentDependentItemsWithoutSources()
        {
            TaffyLayoutItem item = CreateRect("Measured").AddComponent<TaffyLayoutItem>();
            item.width = TaffyLength.Auto;
            item.height = TaffyLength.Auto;
            AssertHasId(Evaluate(item), "item.measurement-source");

            item.gameObject.AddComponent<Image>();
            AssertLacksId(Evaluate(item), "item.measurement-source");
        }

        [Test]
        public void ResponsiveGridAndCalcRulesReuseRuntimeValidation()
        {
            GameObject groupGo = CreateRect("Group");
            TaffyLayoutGroup group = groupGo.AddComponent<TaffyLayoutGroup>();
            group.responsiveProfiles.Add(new TaffyResponsiveProfile { name = "bad", minWidth = 100f, maxWidth = 50f });
            AssertHasId(Evaluate(group), "group.responsive-profiles");

            group.responsiveProfiles.Clear();
            group.containerDisplay = TaffyContainerDisplay.Grid;
            group.gridColumns.Add(TaffyGridTrack.Repeat(TaffyGridRepeatMode.Count, 0, TaffyGridTrack.Points(20f)));
            AssertHasId(Evaluate(group), "grid.validation");

            group.gridColumns.Clear();
            group.gridColumns.Add(TaffyGridTrack.Points(100f));
            TaffyLayoutItem item = CreateChildItem(groupGo, "Item");
            var cycle = TaffyCalcExpression.Length(1f);
            cycle.operation = TaffyCalcOperation.Scale;
            cycle.operands.Add(cycle);
            item.width = TaffyLength.Calc(cycle);
            AssertHasId(Evaluate(item), "calc.validation");

            item.width = TaffyLength.Points(20f);
            item.gridRowStart = TaffyGridPlacement.Span(0);
            object placement = FindResult(Evaluate(item), "grid.validation.placement");
            Assert.That(placement, Is.Not.Null);
            InvokeFix(placement, "grid.reset-placement");
            Assert.That(item.gridRowStart.kind, Is.EqualTo(TaffyGridPlacementKind.Auto));
        }

        [Test]
        public void FixedResponsiveAndRebuildRulesAreConservativeSignals()
        {
            GameObject groupGo = CreateRect("Group");
            TaffyLayoutGroup group = groupGo.AddComponent<TaffyLayoutGroup>();
            group.responsiveProfiles.Add(new TaffyResponsiveProfile { name = "phone", maxWidth = 320f });
            TaffyLayoutItem item = CreateChildItem(groupGo, "Wide");
            item.width = TaffyLength.Points(640f);
            AssertHasId(Evaluate(item), "item.fixed-responsive.width.0");

            FieldInfo suppressed = typeof(TaffyLayoutGroup).GetField("_suppressedRebuildRequests", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(suppressed, Is.Not.Null);
            suppressed.SetValue(group, 20);
            object health = Evaluate(group);
            AssertHasId(health, "group.rebuild-suppression");
            InvokeFix(FindResult(health, "group.rebuild-suppression"), "rebuild.reset");
            Assert.That(group.SuppressedRebuildRequestCount, Is.EqualTo(0));
        }

        [Test]
        public void EvaluationIsReadOnlyAndDoesNotMutateSerializedTargets()
        {
            GameObject groupGo = CreateRect("Group");
            TaffyLayoutGroup group = groupGo.AddComponent<TaffyLayoutGroup>();
            group.containerDisplay = TaffyContainerDisplay.Grid;
            group.gridColumns.Add(TaffyGridTrack.Repeat(TaffyGridRepeatMode.Count, 0, TaffyGridTrack.Points(20f)));
            TaffyLayoutItem item = CreateChildItem(groupGo, "Item");
            item.gridRowStart = TaffyGridPlacement.Span(0);

            string groupBefore = EditorJsonUtility.ToJson(group, true);
            string itemBefore = EditorJsonUtility.ToJson(item, true);
            Evaluate(group, item);
            string groupAfter = EditorJsonUtility.ToJson(group, true);
            string itemAfter = EditorJsonUtility.ToJson(item, true);

            Assert.That(groupAfter, Is.EqualTo(groupBefore));
            Assert.That(itemAfter, Is.EqualTo(itemBefore));
        }

        [Test]
        public void MultipleDiagnosticsAggregateToHighestSeverity()
        {
            GameObject parent = CreateRect("UnityParent");
            parent.AddComponent<HorizontalLayoutGroup>();
            TaffyLayoutItem item = CreateChildItem(parent, "Child");

            object health = Evaluate(item);
            Assert.That(ResultIds(health).Count, Is.GreaterThanOrEqualTo(2));
            PropertyInfo severity = HealthType.GetProperty("HighestSeverity", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(severity.GetValue(health).ToString(), Is.EqualTo("Error"));
        }

        [Test]
        public void PrefabFixRemainsConnectedAndDoesNotMutateAsset()
        {
            if (!AssetDatabase.IsValidFolder(PrefabFolder))
                AssetDatabase.CreateFolder("Assets", "__TaffyUGUIDX5Tests");

            GameObject source = new GameObject("DX5Prefab", typeof(RectTransform), typeof(TaffyLayoutGroup), typeof(ContentSizeFitter));
            source.SetActive(false);
            source.GetComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            string path = PrefabFolder + "/DX5.prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(source, path);
            UnityEngine.Object.DestroyImmediate(source);

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.SetActive(false);
            _owned.Add(instance);
            TaffyLayoutGroup group = instance.GetComponent<TaffyLayoutGroup>();
            object result = FindResult(Evaluate(group), "group.content-size-fitter.horizontal");
            InvokeFix(result, "fitter.taffy.horizontal");

            Assert.That(PrefabUtility.GetPrefabInstanceStatus(instance), Is.EqualTo(PrefabInstanceStatus.Connected));
            Assert.That(instance.GetComponent<ContentSizeFitter>().horizontalFit, Is.EqualTo(ContentSizeFitter.FitMode.Unconstrained));
            Assert.That(AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponent<ContentSizeFitter>().horizontalFit,
                Is.EqualTo(ContentSizeFitter.FitMode.PreferredSize));
        }

        private static Type HealthType => EditorAssembly.GetType("TaffyUGUI.Editor.TaffyLayoutHealth");

        private static object Evaluate(params UnityEngine.Object[] targets)
        {
            Assert.That(HealthType, Is.Not.Null);
            MethodInfo method = HealthType.GetMethod("Evaluate", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            return method.Invoke(null, new object[] { targets });
        }

        private static List<object> Results(object health)
        {
            PropertyInfo property = HealthType.GetProperty("Results", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(property, Is.Not.Null);
            var list = new List<object>();
            foreach (object result in (IEnumerable)property.GetValue(health))
                list.Add(result);
            return list;
        }

        private static List<string> ResultIds(object health)
        {
            return Results(health).Select(ResultId).ToList();
        }

        private static string ResultId(object result)
        {
            PropertyInfo property = result.GetType().GetProperty("Id", BindingFlags.Instance | BindingFlags.NonPublic);
            return (string)property.GetValue(result);
        }

        private static object FindResult(object health, string id)
        {
            return Results(health).FirstOrDefault(result => ResultId(result) == id);
        }

        private static void AssertHasId(object health, string id)
        {
            CollectionAssert.Contains(ResultIds(health), id);
        }

        private static void AssertLacksId(object health, string id)
        {
            CollectionAssert.DoesNotContain(ResultIds(health), id);
        }

        private static void InvokeFirstFix(object result)
        {
            object fix = Fixes(result).First();
            fix.GetType().GetMethod("Invoke", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(fix, null);
        }

        private static void InvokeFix(object result, string fixId)
        {
            Assert.That(result, Is.Not.Null);
            object fix = Fixes(result).FirstOrDefault(candidate =>
            {
                PropertyInfo id = candidate.GetType().GetProperty("Id", BindingFlags.Instance | BindingFlags.NonPublic);
                return (string)id.GetValue(candidate) == fixId;
            });
            Assert.That(fix, Is.Not.Null, "Expected diagnostic fix " + fixId);
            fix.GetType().GetMethod("Invoke", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(fix, null);
        }

        private static List<object> Fixes(object result)
        {
            PropertyInfo property = result.GetType().GetProperty("Fixes", BindingFlags.Instance | BindingFlags.NonPublic);
            var list = new List<object>();
            foreach (object fix in (IEnumerable)property.GetValue(result))
                list.Add(fix);
            return list;
        }

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
    }
}
