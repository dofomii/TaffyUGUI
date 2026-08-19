using System;
using System.Reflection;
using NUnit.Framework;
using TaffyUGUI.Editor;
using UnityEngine;
using UnityEngine.UI;

namespace TaffyUGUI.Tests
{
    public sealed class TaffyDX10ExplainLayoutTests
    {
        [Test]
        public void ExplainLayoutDescribesFixedPercentContentPaddingAndFlexWithoutMutation()
        {
            GameObject root = new GameObject("Parent", typeof(RectTransform), typeof(TaffyLayoutGroup));
            GameObject child = new GameObject("Item", typeof(RectTransform), typeof(TaffyLayoutItem), typeof(TestMeasurementProvider));
            try
            {
                RectTransform rootRect = (RectTransform)root.transform;
                rootRect.sizeDelta = new Vector2(400f, 240f);
                child.transform.SetParent(root.transform, false);

                TaffyLayoutGroup group = root.GetComponent<TaffyLayoutGroup>();
                group.containerDisplay = TaffyContainerDisplay.Flex;
                group.direction = TaffyFlexDirection.Row;

                TaffyLayoutItem item = child.GetComponent<TaffyLayoutItem>();
                item.width = TaffyLength.Points(240f);
                item.height = TaffyLength.Percent(0.5f);
                item.padding = new TaffyEdges
                {
                    left = TaffyLength.Points(8f),
                    right = TaffyLength.Points(8f),
                    top = TaffyLength.Points(4f),
                    bottom = TaffyLength.Points(4f),
                };
                item.flexGrow = 1f;

                string before = JsonUtility.ToJson(item);
                string explanation = Explain(item);

                Assert.That(explanation, Does.Contain("fixed at 240"));
                Assert.That(explanation, Does.Contain("50%"));
                Assert.That(explanation, Does.Contain("Item padding"));
                Assert.That(explanation, Does.Contain("Flex Grow is 1"));
                Assert.That(explanation, Does.Contain("exact per-item allocation"));
                Assert.That(JsonUtility.ToJson(item), Is.EqualTo(before));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(child);
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void ExplainLayoutDescribesAutoMeasurementAndResponsiveOverrides()
        {
            GameObject root = new GameObject("Responsive", typeof(RectTransform), typeof(TaffyLayoutGroup));
            GameObject child = new GameObject("Measured", typeof(RectTransform), typeof(TaffyLayoutItem), typeof(TestMeasurementProvider));
            try
            {
                RectTransform rootRect = (RectTransform)root.transform;
                rootRect.sizeDelta = new Vector2(300f, 180f);
                child.transform.SetParent(root.transform, false);

                TaffyLayoutGroup group = root.GetComponent<TaffyLayoutGroup>();
                group.responsiveProfiles.Add(new TaffyResponsiveProfile
                {
                    name = "phone",
                    maxWidth = 350f,
                    overrideFlexDirection = true,
                    direction = TaffyFlexDirection.Column,
                    overrideGaps = true,
                    horizontalGap = 6f,
                    verticalGap = 10f,
                });

                TaffyLayoutItem item = child.GetComponent<TaffyLayoutItem>();
                item.width = TaffyLength.Auto;
                item.height = TaffyLength.Auto;
                LayoutRebuilder.ForceRebuildLayoutImmediate(rootRect);
                Assert.That(group.ActiveResponsiveProfileName, Is.EqualTo("phone"));

                string explanation = Explain(item);
                Assert.That(explanation, Does.Contain("Content measurement is available"));
                Assert.That(explanation, Does.Contain("140 × 44"));
                Assert.That(explanation, Does.Contain("phone is active"));
                Assert.That(explanation, Does.Contain("direction"));
                Assert.That(explanation, Does.Contain("gaps"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(child);
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void ExplainLayoutDescribesGridRequestWithoutInventingResolvedGeometry()
        {
            GameObject root = new GameObject("Grid", typeof(RectTransform), typeof(TaffyLayoutGroup));
            GameObject child = new GameObject("Cell", typeof(RectTransform), typeof(TaffyLayoutItem));
            try
            {
                child.transform.SetParent(root.transform, false);
                TaffyLayoutGroup group = root.GetComponent<TaffyLayoutGroup>();
                group.containerDisplay = TaffyContainerDisplay.Grid;
                group.gridColumns.Add(TaffyGridTrack.Fraction(1f));
                group.gridColumns.Add(TaffyGridTrack.Fraction(1f));
                group.gridRows.Add(TaffyGridTrack.Auto());

                TaffyLayoutItem item = child.GetComponent<TaffyLayoutItem>();
                item.gridColumnStart = TaffyGridPlacement.Line(1);
                item.gridColumnEnd = TaffyGridPlacement.Span(2);

                string explanation = Explain(item);
                Assert.That(explanation, Does.Contain("Grid placement request"));
                Assert.That(explanation, Does.Contain("line 1"));
                Assert.That(explanation, Does.Contain("span 2"));
                Assert.That(explanation, Does.Contain("not guessed"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(child);
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static string Explain(Component component)
        {
            Assembly editorAssembly = typeof(TaffyLayoutGroupEditor).Assembly;
            Type snapshotType = editorAssembly.GetType("TaffyUGUI.Editor.TaffyComputedLayoutSnapshot");
            Assert.That(snapshotType, Is.Not.Null);
            MethodInfo from = snapshotType.GetMethod("From", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(from, Is.Not.Null);
            object snapshot = from.Invoke(null, new object[] { component });

            Type explanationType = editorAssembly.GetType("TaffyUGUI.Editor.TaffyLayoutExplanation");
            Assert.That(explanationType, Is.Not.Null);
            MethodInfo build = explanationType.GetMethod("Build", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(build, Is.Not.Null);
            return (string)build.Invoke(null, new[] { (object)component, snapshot });
        }

        public sealed class TestMeasurementProvider : MonoBehaviour, ITaffyMeasurementProvider
        {
            public int MeasurementVersion => 1;

            public bool TryGetTaffyMeasurement(float availableWidth, out TaffyMeasurementData measurement)
            {
                measurement = new TaffyMeasurementData
                {
                    minContent = new Vector2(60f, 22f),
                    preferred = new Vector2(140f, 44f),
                    maxContent = new Vector2(220f, 66f),
                    samples = Array.Empty<TaffyMeasurementSample>(),
                };
                return true;
            }
        }
    }
}
