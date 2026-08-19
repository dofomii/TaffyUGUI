using System;
using System.Reflection;
using NUnit.Framework;
using TaffyUGUI.Editor;
using UnityEngine;
using UnityEngine.UI;

namespace TaffyUGUI.Tests
{
    public sealed class TaffyDX10ComputedLayoutTests
    {
        [Test]
        public void SnapshotReportsCurrentRectTransformPositionAndSizeWithoutMutation()
        {
            GameObject go = new GameObject("Computed", typeof(RectTransform));
            try
            {
                RectTransform rect = (RectTransform)go.transform;
                rect.sizeDelta = new Vector2(320f, 180f);
                rect.anchoredPosition = new Vector2(24f, -12f);
                TaffyLayoutItem item = go.AddComponent<TaffyLayoutItem>();
                string before = JsonUtility.ToJson(item);

                object snapshot = Snapshot(item, out Type snapshotType);

                Assert.That(Read<bool>(snapshotType, snapshot, "Available"), Is.True);
                Assert.That(Read<Vector2>(snapshotType, snapshot, "Position"), Is.EqualTo(rect.anchoredPosition));
                Assert.That(Read<Vector2>(snapshotType, snapshot, "Size"), Is.EqualTo(rect.rect.size));
                Assert.That(JsonUtility.ToJson(item), Is.EqualTo(before));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void SnapshotReportsResolvedResponsiveProfileAndEffectiveDirection()
        {
            GameObject root = new GameObject("Responsive", typeof(RectTransform), typeof(TaffyLayoutGroup));
            try
            {
                RectTransform rect = (RectTransform)root.transform;
                rect.sizeDelta = new Vector2(300f, 160f);
                TaffyLayoutGroup group = root.GetComponent<TaffyLayoutGroup>();
                group.direction = TaffyFlexDirection.Row;
                group.responsiveProfiles.Add(new TaffyResponsiveProfile
                {
                    name = "phone",
                    maxWidth = 350f,
                    overrideFlexDirection = true,
                    direction = TaffyFlexDirection.Column,
                });

                LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
                Assert.That(group.ActiveResponsiveProfileName, Is.EqualTo("phone"));

                object snapshot = Snapshot(group, out Type snapshotType);
                Assert.That(Read<string>(snapshotType, snapshot, "ResponsiveProfile"), Is.EqualTo("phone"));
                Assert.That(Read<TaffyContainerDisplay>(snapshotType, snapshot, "EffectiveDisplay"), Is.EqualTo(TaffyContainerDisplay.Flex));
                Assert.That(Read<TaffyFlexDirection>(snapshotType, snapshot, "EffectiveDirection"), Is.EqualTo(TaffyFlexDirection.Column));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void SnapshotReportsParentContextAndIntrinsicMeasurementWhenAvailable()
        {
            GameObject root = new GameObject("Parent", typeof(RectTransform), typeof(TaffyLayoutGroup));
            GameObject child = new GameObject("Measured", typeof(RectTransform), typeof(TaffyLayoutItem), typeof(TestMeasurementProvider));
            try
            {
                RectTransform rootRect = (RectTransform)root.transform;
                rootRect.sizeDelta = new Vector2(420f, 200f);
                child.transform.SetParent(root.transform, false);
                TaffyLayoutGroup group = root.GetComponent<TaffyLayoutGroup>();
                group.containerDisplay = TaffyContainerDisplay.Flex;
                group.direction = TaffyFlexDirection.Column;
                TaffyLayoutItem item = child.GetComponent<TaffyLayoutItem>();

                object snapshot = Snapshot(item, out Type snapshotType);
                Assert.That(Read<string>(snapshotType, snapshot, "ParentContext"), Does.Contain("Parent"));
                Assert.That(Read<string>(snapshotType, snapshot, "ParentContext"), Does.Contain("Column"));
                Assert.That(Read<bool>(snapshotType, snapshot, "MeasurementAvailable"), Is.True);
                TaffyMeasurementData measurement = Read<TaffyMeasurementData>(snapshotType, snapshot, "Measurement");
                Assert.That(measurement.preferred, Is.EqualTo(new Vector2(140f, 44f)));
                Assert.That(Read<float>(snapshotType, snapshot, "MeasurementWidth"), Is.EqualTo(rootRect.rect.width).Within(0.01f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(child);
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void SnapshotReportsGridValidationWithoutChangingGridData()
        {
            GameObject root = new GameObject("Grid", typeof(RectTransform), typeof(TaffyLayoutGroup));
            try
            {
                TaffyLayoutGroup group = root.GetComponent<TaffyLayoutGroup>();
                group.containerDisplay = TaffyContainerDisplay.Grid;
                group.gridColumns.Add(TaffyGridTrack.Fraction(-1f));
                string before = JsonUtility.ToJson(group);

                object snapshot = Snapshot(group, out Type snapshotType);
                string diagnostics = Read<string>(snapshotType, snapshot, "GridDiagnostics");
                Assert.That(diagnostics, Is.Not.Empty);
                Assert.That(diagnostics, Does.Contain("gridColumns"));
                Assert.That(JsonUtility.ToJson(group), Is.EqualTo(before));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static object Snapshot(Component component, out Type snapshotType)
        {
            snapshotType = typeof(TaffyLayoutGroupEditor).Assembly.GetType("TaffyUGUI.Editor.TaffyComputedLayoutSnapshot");
            Assert.That(snapshotType, Is.Not.Null);
            MethodInfo from = snapshotType.GetMethod("From", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(from, Is.Not.Null);
            return from.Invoke(null, new object[] { component });
        }

        private static T Read<T>(Type snapshotType, object snapshot, string propertyName)
        {
            PropertyInfo property = snapshotType.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(property, Is.Not.Null, propertyName);
            return (T)property.GetValue(snapshot);
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
