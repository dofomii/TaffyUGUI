using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TaffyUGUI.Tests
{
    public sealed class TaffyPhase9PlayModeTests
    {
        [UnityTest]
        public IEnumerator RuntimeGridTrackMutationUpdatesGeometryAndDiagnostics()
        {
            var rootObject = new GameObject("Phase9RuntimeGrid", typeof(RectTransform), typeof(TaffyLayoutGroup));
            RectTransform root = rootObject.GetComponent<RectTransform>();
            root.anchorMin = new Vector2(0f, 1f);
            root.anchorMax = new Vector2(0f, 1f);
            root.pivot = new Vector2(0f, 1f);
            root.sizeDelta = new Vector2(200f, 100f);

            TaffyLayoutGroup group = rootObject.GetComponent<TaffyLayoutGroup>();
            group.containerDisplay = TaffyContainerDisplay.Grid;
            group.gridRows.Add(TaffyGridTrack.Points(100f));
            group.gridColumns.Add(TaffyGridTrack.Points(100f));
            group.gridColumns.Add(TaffyGridTrack.Points(100f));

            var childObject = new GameObject("GridItem", typeof(RectTransform), typeof(TaffyLayoutItem));
            RectTransform child = childObject.GetComponent<RectTransform>();
            child.SetParent(root, false);
            TaffyLayoutItem item = childObject.GetComponent<TaffyLayoutItem>();
            item.width = TaffyLength.Points(20f);
            item.height = TaffyLength.Points(20f);
            item.gridColumnStart = TaffyGridPlacement.Line(2);
            item.gridColumnEnd = TaffyGridPlacement.Line(3);

            yield return null;
            LayoutRebuilder.ForceRebuildLayoutImmediate(root);
            Assert.That(Left(child), Is.EqualTo(100f).Within(0.1f));

            group.gridColumns[0] = TaffyGridTrack.Points(60f);
            group.gridColumns[1] = TaffyGridTrack.Points(140f);
            group.SetLayoutDirty();
            yield return null;
            LayoutRebuilder.ForceRebuildLayoutImmediate(root);

            Assert.That(Left(child), Is.EqualTo(60f).Within(0.1f));
            Assert.That(group.TryGetGridDiagnostics(out TaffyGridDiagnostics diagnostics, out string error), Is.True, error);
            Assert.That(diagnostics.columnTrackSizes[0], Is.EqualTo(60f).Within(0.1f));
            Assert.That(diagnostics.columnTrackSizes[1], Is.EqualTo(140f).Within(0.1f));

            Object.Destroy(rootObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator RuntimeCalcMutationAndEnableDisableRecreateResourcesSafely()
        {
            var rootObject = new GameObject("Phase9RuntimeCalc", typeof(RectTransform), typeof(TaffyLayoutGroup));
            RectTransform root = rootObject.GetComponent<RectTransform>();
            root.anchorMin = new Vector2(0f, 1f);
            root.anchorMax = new Vector2(0f, 1f);
            root.pivot = new Vector2(0f, 1f);
            root.sizeDelta = new Vector2(220f, 100f);

            TaffyLayoutGroup group = rootObject.GetComponent<TaffyLayoutGroup>();
            group.containerDisplay = TaffyContainerDisplay.Grid;
            group.gridRows.Add(TaffyGridTrack.Points(100f));
            group.gridColumns.Add(TaffyGridTrack.Calc(TaffyCalcExpression.Length(100f)));
            group.gridColumns.Add(TaffyGridTrack.Fraction(1f));

            var childObject = new GameObject("CalcItem", typeof(RectTransform), typeof(TaffyLayoutItem));
            RectTransform child = childObject.GetComponent<RectTransform>();
            child.SetParent(root, false);
            TaffyLayoutItem item = childObject.GetComponent<TaffyLayoutItem>();
            item.width = TaffyLength.Calc(TaffyCalcExpression.Length(70f));
            item.height = TaffyLength.Points(20f);

            yield return null;
            LayoutRebuilder.ForceRebuildLayoutImmediate(root);
            Assert.That(child.rect.width, Is.EqualTo(70f).Within(0.1f));

            item.width = TaffyLength.Calc(TaffyCalcExpression.Length(90f));
            group.gridColumns[0] = TaffyGridTrack.Calc(TaffyCalcExpression.Length(120f));
            group.SetLayoutDirty();
            yield return null;
            LayoutRebuilder.ForceRebuildLayoutImmediate(root);
            Assert.That(child.rect.width, Is.EqualTo(90f).Within(0.1f));
            Assert.That(group.TryGetGridDiagnostics(out TaffyGridDiagnostics changed, out string error), Is.True, error);
            Assert.That(changed.columnTrackSizes[0], Is.EqualTo(120f).Within(0.1f));

            group.enabled = false;
            yield return null;
            group.enabled = true;
            yield return null;
            LayoutRebuilder.ForceRebuildLayoutImmediate(root);
            Assert.That(child.rect.width, Is.EqualTo(90f).Within(0.1f));
            Assert.That(group.TryGetGridDiagnostics(out TaffyGridDiagnostics recreated, out error), Is.True, error);
            Assert.That(recreated.columnTrackSizes[0], Is.EqualTo(120f).Within(0.1f));

            Object.Destroy(rootObject);
            yield return null;
        }

        private static float Left(RectTransform rect)
        {
            return rect.anchoredPosition.x - rect.rect.width * rect.pivot.x;
        }
    }
}
