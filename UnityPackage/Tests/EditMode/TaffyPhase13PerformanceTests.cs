using System;
using System.Diagnostics;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace TaffyUGUI.Tests
{
    public sealed class TaffyPhase13PerformanceTests
    {
        [Test]
        public void HundredNodeDirtyLayoutManagedAllocationProfile()
        {
            const int nodeCount = 100;
            const int warmupIterations = 10;
            const int measuredIterations = 100;

            var rootObject = new GameObject("Phase13ManagedAllocationRoot", typeof(RectTransform), typeof(TaffyLayoutGroup));
            try
            {
                RectTransform root = rootObject.GetComponent<RectTransform>();
                root.anchorMin = new Vector2(0f, 1f);
                root.anchorMax = new Vector2(0f, 1f);
                root.pivot = new Vector2(0f, 1f);
                root.sizeDelta = new Vector2(1024f, 768f);

                TaffyLayoutGroup group = rootObject.GetComponent<TaffyLayoutGroup>();
                group.direction = TaffyFlexDirection.Row;
                group.wrap = TaffyFlexWrap.Wrap;
                group.horizontalGap = 4f;
                group.verticalGap = 4f;
                group.alignItems = TaffyAlign.Start;

                TaffyLayoutItem dirtyLeaf = null;
                for (int index = 1; index < nodeCount; index++)
                {
                    var childObject = new GameObject($"Node-{index}", typeof(RectTransform), typeof(TaffyLayoutItem));
                    RectTransform child = childObject.GetComponent<RectTransform>();
                    child.SetParent(root, false);

                    TaffyLayoutItem item = childObject.GetComponent<TaffyLayoutItem>();
                    item.width = TaffyLength.Points(18f + (index % 11) * 3f);
                    item.height = TaffyLength.Points(14f + (index % 7) * 2f);
                    item.flexShrink = 0f;
                    item.measurement = TaffyMeasurementMode.Disabled;
                    dirtyLeaf = item;
                }

                Assert.That(dirtyLeaf, Is.Not.Null);
                for (int iteration = 0; iteration < warmupIterations; iteration++)
                {
                    dirtyLeaf.width = TaffyLength.Points(36f + (iteration & 1));
                    LayoutRebuilder.ForceRebuildLayoutImmediate(root);
                }

                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                var stopwatch = new Stopwatch();
                long allocationStart = GC.GetAllocatedBytesForCurrentThread();
                stopwatch.Start();
                for (int iteration = 0; iteration < measuredIterations; iteration++)
                {
                    dirtyLeaf.width = TaffyLength.Points(36f + (iteration & 1));
                    LayoutRebuilder.ForceRebuildLayoutImmediate(root);
                }
                stopwatch.Stop();
                long allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocationStart;

                double allocatedPerIteration = allocatedBytes / (double)measuredIterations;
                double microsecondsPerIteration = stopwatch.Elapsed.TotalMilliseconds * 1000.0 / measuredIterations;
                RectTransform measuredLeaf = dirtyLeaf.GetComponent<RectTransform>();

                                UnityEngine.Debug.Log(
                    $"TAFFY_MANAGED_ALLOCATION_RESULT nodes={nodeCount} iterations={measuredIterations} " +
                    $"allocated_bytes={allocatedBytes} allocated_bytes_per_iteration={allocatedPerIteration:F2} " +
                    $"microseconds_per_iteration={microsecondsPerIteration:F2}");

                Assert.That(measuredLeaf.rect.width, Is.GreaterThan(0f));
                Assert.That(measuredLeaf.rect.height, Is.GreaterThan(0f));
                Assert.That(allocatedPerIteration, Is.LessThan(1_000_000d),
                    "A warmed 100-node dirty layout should not allocate a megabyte per rebuild on the managed thread.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(rootObject);
            }
        }
    }
}
