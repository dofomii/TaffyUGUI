using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace TaffyUGUI.Tests
{
    public sealed class TaffyPhase9EditModeTests
    {
        private GameObject _rootObject;

        [TearDown]
        public void TearDown()
        {
            if (_rootObject)
                Object.DestroyImmediate(_rootObject);
        }

        [Test]
        public void ExplicitTracksNumericPlacementAndDiagnosticsMatchGeometry()
        {
            RectTransform root = CreateGridRoot(200f, 100f, out TaffyLayoutGroup group);
            group.gridRows.Add(TaffyGridTrack.Points(40f));
            group.gridRows.Add(TaffyGridTrack.Points(60f));
            group.gridColumns.Add(TaffyGridTrack.Points(80f));
            group.gridColumns.Add(TaffyGridTrack.Points(120f));

            RectTransform child = CreateItem(root, "Placed", 25f, 20f, out TaffyLayoutItem item);
            item.gridRowStart = TaffyGridPlacement.Line(2);
            item.gridRowEnd = TaffyGridPlacement.Line(3);
            item.gridColumnStart = TaffyGridPlacement.Line(2);
            item.gridColumnEnd = TaffyGridPlacement.Line(3);

            Force(root);

            Assert.That(Left(child), Is.EqualTo(80f).Within(0.05f));
            Assert.That(Top(child), Is.EqualTo(40f).Within(0.05f));
            Assert.That(group.TryGetGridDiagnostics(out TaffyGridDiagnostics diagnostics, out string error), Is.True, error);
            Assert.That(diagnostics.explicitRows, Is.EqualTo(2));
            Assert.That(diagnostics.explicitColumns, Is.EqualTo(2));
            Assert.That(diagnostics.rowTrackSizes, Is.EqualTo(new[] { 40f, 60f }).Within(0.05f));
            Assert.That(diagnostics.columnTrackSizes, Is.EqualTo(new[] { 80f, 120f }).Within(0.05f));
            Assert.That(diagnostics.items, Has.Length.EqualTo(1));
            Assert.That(diagnostics.items[0].rowStart, Is.EqualTo(2));
            Assert.That(diagnostics.items[0].rowEnd, Is.EqualTo(3));
            Assert.That(diagnostics.items[0].columnStart, Is.EqualTo(2));
            Assert.That(diagnostics.items[0].columnEnd, Is.EqualTo(3));
        }

        [Test]
        public void FractionMinMaxAndContentTracksAreAuthored()
        {
            RectTransform root = CreateGridRoot(300f, 100f, out TaffyLayoutGroup group);
            group.gridRows.Add(TaffyGridTrack.MaxContent());
            group.gridColumns.Add(TaffyGridTrack.MinMax(
                TaffyGridTrackBreadth.Points(50f),
                TaffyGridTrackBreadth.Fraction(1f)));
            group.gridColumns.Add(TaffyGridTrack.Fraction(1f));

            CreateItem(root, "A", 60f, 20f, out TaffyLayoutItem a);
            a.gridColumnStart = TaffyGridPlacement.Line(1);
            a.gridColumnEnd = TaffyGridPlacement.Line(2);
            CreateItem(root, "B", 40f, 30f, out TaffyLayoutItem b);
            b.gridColumnStart = TaffyGridPlacement.Line(2);
            b.gridColumnEnd = TaffyGridPlacement.Line(3);

            Force(root);

            Assert.That(group.TryGetGridDiagnostics(out TaffyGridDiagnostics diagnostics, out string error), Is.True, error);
            Assert.That(diagnostics.columnTrackSizes, Has.Length.EqualTo(2));
            Assert.That(diagnostics.columnTrackSizes[0] + diagnostics.columnTrackSizes[1], Is.EqualTo(300f).Within(0.1f));
            Assert.That(diagnostics.columnTrackSizes[0], Is.GreaterThanOrEqualTo(50f));
            Assert.That(diagnostics.rowTrackSizes, Has.Length.EqualTo(1));
            Assert.That(diagnostics.rowTrackSizes[0], Is.GreaterThanOrEqualTo(30f));
        }

        [Test]
        public void RepeatCountAutoFillAndAutoFitExecute()
        {
            RectTransform root = CreateGridRoot(300f, 100f, out TaffyLayoutGroup group);
            group.gridRows.Add(TaffyGridTrack.Points(30f));
            group.gridColumns.Add(TaffyGridTrack.Repeat(
                TaffyGridRepeatMode.Count,
                3,
                TaffyGridTrack.Fraction(1f)));

            for (int i = 0; i < 3; i++)
                CreateItem(root, "Count" + i, 20f, 20f, out _);

            Force(root);
            Assert.That(group.TryGetGridDiagnostics(out TaffyGridDiagnostics countDiagnostics, out string error), Is.True, error);
            Assert.That(countDiagnostics.columnTrackSizes, Has.Length.EqualTo(3));
            Assert.That(countDiagnostics.columnTrackSizes[0], Is.EqualTo(100f).Within(0.1f));

            group.gridColumns.Clear();
            group.gridColumns.Add(TaffyGridTrack.Repeat(
                TaffyGridRepeatMode.AutoFill,
                1,
                TaffyGridTrack.Points(50f)));
            group.SetLayoutDirty();
            Force(root);
            Assert.That(group.TryGetGridDiagnostics(out TaffyGridDiagnostics fillDiagnostics, out error), Is.True, error);
            Assert.That(fillDiagnostics.columnTrackSizes.Length, Is.GreaterThanOrEqualTo(3));

            group.gridColumns[0].repeatMode = TaffyGridRepeatMode.AutoFit;
            group.SetLayoutDirty();
            Force(root);
            Assert.That(group.TryGetGridDiagnostics(out TaffyGridDiagnostics fitDiagnostics, out error), Is.True, error);
            Assert.That(fitDiagnostics.columnTrackSizes.Length, Is.GreaterThanOrEqualTo(3));
        }

        [Test]
        public void NamedLinesNamedSpansAndTemplateAreasExecute()
        {
            RectTransform root = CreateGridRoot(200f, 100f, out TaffyLayoutGroup group);
            group.gridRows.Add(TaffyGridTrack.Points(50f));
            group.gridRows.Add(TaffyGridTrack.Points(50f));
            group.gridColumns.Add(TaffyGridTrack.Points(70f));
            group.gridColumns.Add(TaffyGridTrack.Points(130f));
            group.gridNamedLines.Add(new TaffyGridNamedLine(TaffyGridAxis.Column, 0, "content-start"));
            group.gridNamedLines.Add(new TaffyGridNamedLine(TaffyGridAxis.Column, 1, "content-end"));
            group.gridNamedLines.Add(new TaffyGridNamedLine(TaffyGridAxis.Row, 0, "rows-start"));
            group.gridNamedLines.Add(new TaffyGridNamedLine(TaffyGridAxis.Row, 2, "rows-end"));
            group.gridAreaRows = 2;
            group.gridAreaColumns = 2;
            group.gridAreas.Add(new TaffyGridArea("hero", 1, 2, 1, 3));

            RectTransform named = CreateItem(root, "Named", 25f, 20f, out TaffyLayoutItem item);
            item.gridColumnStart = TaffyGridPlacement.NamedLine("content-start");
            item.gridColumnEnd = TaffyGridPlacement.NamedLine("content-end");
            item.gridRowStart = TaffyGridPlacement.NamedLine("rows-start");
            item.gridRowEnd = TaffyGridPlacement.NamedSpan("rows-end", 1);

            Force(root);

            Assert.That(group.ValidateGridAuthoring(out string validationError), Is.True, validationError);
            Assert.That(Left(named), Is.EqualTo(0f).Within(0.1f));
            Assert.That(group.TryGetGridDiagnostics(out TaffyGridDiagnostics diagnostics, out string error), Is.True, error);
            Assert.That(diagnostics.explicitRows, Is.EqualTo(2));
            Assert.That(diagnostics.explicitColumns, Is.EqualTo(2));
        }

        [Test]
        public void AutoFlowAndImplicitTracksAreExposed()
        {
            RectTransform root = CreateGridRoot(240f, 100f, out TaffyLayoutGroup group);
            group.gridAutoFlow = TaffyGridAutoFlow.Column;
            group.gridRows.Add(TaffyGridTrack.Points(40f));
            group.gridColumns.Add(TaffyGridTrack.Points(60f));
            group.gridAutoColumns.Add(TaffyGridTrack.Points(45f));

            for (int i = 0; i < 4; i++)
                CreateItem(root, "Auto" + i, 20f, 20f, out _);

            Force(root);

            Assert.That(group.TryGetGridDiagnostics(out TaffyGridDiagnostics diagnostics, out string error), Is.True, error);
            Assert.That(diagnostics.positiveImplicitColumns, Is.GreaterThan(0));
            Assert.That(diagnostics.columnTrackSizes.Length, Is.GreaterThan(1));
            Assert.That(diagnostics.items, Has.Length.EqualTo(4));
        }

        [Test]
        public void JustifyItemsAndJustifySelfControlGridAlignment()
        {
            RectTransform root = CreateGridRoot(100f, 100f, out TaffyLayoutGroup group);
            group.gridRows.Add(TaffyGridTrack.Points(100f));
            group.gridColumns.Add(TaffyGridTrack.Points(100f));
            group.justifyItems = TaffyAlign.Center;
            group.alignItems = TaffyAlign.Center;

            RectTransform child = CreateItem(root, "Aligned", 20f, 20f, out TaffyLayoutItem item);
            Force(root);
            Assert.That(Left(child), Is.EqualTo(40f).Within(0.05f));
            Assert.That(Top(child), Is.EqualTo(40f).Within(0.05f));

            item.justifySelf = TaffyAlign.End;
            group.SetLayoutDirty();
            Force(root);
            Assert.That(Left(child), Is.EqualTo(80f).Within(0.05f));
        }

        [Test]
        public void TypedCalcDrivesDimensionsAndGridTracksAcrossContextRecreation()
        {
            RectTransform root = CreateGridRoot(200f, 100f, out TaffyLayoutGroup group);
            group.gridRows.Add(TaffyGridTrack.Points(50f));
            group.gridColumns.Add(TaffyGridTrack.Calc(TaffyCalcExpression.Add(
                TaffyCalcExpression.Percent(0.5f),
                TaffyCalcExpression.Length(10f))));
            group.gridColumns.Add(TaffyGridTrack.Fraction(1f));

            RectTransform child = CreateItem(root, "Calc", 0f, 20f, out TaffyLayoutItem item);
            item.width = TaffyLength.Calc(TaffyCalcExpression.Add(
                TaffyCalcExpression.Percent(0.5f),
                TaffyCalcExpression.Length(5f)));
            item.gridColumnStart = TaffyGridPlacement.Line(1);
            item.gridColumnEnd = TaffyGridPlacement.Line(2);

            Force(root);
            Assert.That(group.TryGetGridDiagnostics(out TaffyGridDiagnostics first, out string error), Is.True, error);
            Assert.That(first.columnTrackSizes[0], Is.EqualTo(110f).Within(0.2f));
            Assert.That(child.rect.width, Is.GreaterThan(0f));

            item.width = TaffyLength.Calc(TaffyCalcExpression.Length(70f));
            group.gridColumns[0] = TaffyGridTrack.Calc(TaffyCalcExpression.Length(90f));
            group.SetLayoutDirty();
            Force(root);
            Assert.That(child.rect.width, Is.EqualTo(70f).Within(0.1f));
            Assert.That(group.TryGetGridDiagnostics(out TaffyGridDiagnostics changed, out error), Is.True, error);
            Assert.That(changed.columnTrackSizes[0], Is.EqualTo(90f).Within(0.1f));

            group.enabled = false;
            group.enabled = true;
            Force(root);
            Assert.That(child.rect.width, Is.EqualTo(70f).Within(0.1f));
            Assert.That(group.TryGetGridDiagnostics(out TaffyGridDiagnostics recreated, out error), Is.True, error);
            Assert.That(recreated.columnTrackSizes[0], Is.EqualTo(90f).Within(0.1f));
        }

        [Test]
        public void ValidationRejectsInvalidRepeatAreaPlacementAndCalcCycles()
        {
            RectTransform root = CreateGridRoot(200f, 100f, out TaffyLayoutGroup group);
            group.gridRows.Add(TaffyGridTrack.Points(50f));
            group.gridColumns.Add(TaffyGridTrack.Repeat(TaffyGridRepeatMode.Count, 0, TaffyGridTrack.Points(50f)));
            Assert.That(group.ValidateGridAuthoring(out string error), Is.False);
            StringAssert.Contains("repeat count", error.ToLowerInvariant());

            group.gridColumns.Clear();
            group.gridColumns.Add(TaffyGridTrack.Points(100f));
            group.gridAreaRows = 1;
            group.gridAreaColumns = 1;
            group.gridAreas.Add(new TaffyGridArea("bad", 1, 3, 1, 2));
            Assert.That(group.ValidateGridAuthoring(out error), Is.False);
            StringAssert.Contains("area", error.ToLowerInvariant());

            group.gridAreas.Clear();
            RectTransform child = CreateItem(root, "InvalidCalc", 20f, 20f, out TaffyLayoutItem item);
            var cycle = TaffyCalcExpression.Length(1f);
            cycle.operation = TaffyCalcOperation.Scale;
            cycle.operands.Add(cycle);
            item.width = TaffyLength.Calc(cycle);
            Assert.That(group.ValidateGridAuthoring(out error), Is.False);
            StringAssert.Contains("cycle", error.ToLowerInvariant());

            item.width = TaffyLength.Points(20f);
            item.gridRowStart = TaffyGridPlacement.Span(0);
            Assert.That(group.ValidateGridAuthoring(out error), Is.False);
            StringAssert.Contains("span", error.ToLowerInvariant());
            Assert.That(child, Is.Not.Null);
        }

        private RectTransform CreateGridRoot(float width, float height, out TaffyLayoutGroup group)
        {
            _rootObject = new GameObject("GridRoot", typeof(RectTransform), typeof(TaffyLayoutGroup));
            RectTransform root = _rootObject.GetComponent<RectTransform>();
            root.anchorMin = new Vector2(0f, 1f);
            root.anchorMax = new Vector2(0f, 1f);
            root.pivot = new Vector2(0f, 1f);
            root.sizeDelta = new Vector2(width, height);
            group = _rootObject.GetComponent<TaffyLayoutGroup>();
            group.containerDisplay = TaffyContainerDisplay.Grid;
            group.alignItems = TaffyAlign.Stretch;
            return root;
        }

        private static RectTransform CreateItem(
            RectTransform parent,
            string name,
            float width,
            float height,
            out TaffyLayoutItem item)
        {
            var childObject = new GameObject(name, typeof(RectTransform), typeof(TaffyLayoutItem));
            RectTransform rect = childObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            item = childObject.GetComponent<TaffyLayoutItem>();
            item.width = width > 0f ? TaffyLength.Points(width) : TaffyLength.Auto;
            item.height = height > 0f ? TaffyLength.Points(height) : TaffyLength.Auto;
            return rect;
        }

        private static void Force(RectTransform root)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(root);
        }

        private static float Left(RectTransform rect)
        {
            return rect.anchoredPosition.x - rect.rect.width * rect.pivot.x;
        }

        private static float Top(RectTransform rect)
        {
            return -(rect.anchoredPosition.y + rect.rect.height * (1f - rect.pivot.y));
        }
    }
}
