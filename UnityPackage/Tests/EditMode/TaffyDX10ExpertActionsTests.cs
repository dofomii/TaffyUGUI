using System;
using System.Reflection;
using NUnit.Framework;
using TaffyUGUI.Editor;
using UnityEditor;
using UnityEngine;

namespace TaffyUGUI.Tests
{
    public sealed class TaffyDX10ExpertActionsTests
    {
        [Test]
        public void SectionResetChangesOnlyOwnedFieldsAndUndoRestoresThem()
        {
            GameObject go = new GameObject("Item", typeof(RectTransform), typeof(TaffyLayoutItem));
            try
            {
                TaffyLayoutItem item = go.GetComponent<TaffyLayoutItem>();
                item.width = TaffyLength.Points(210f);
                item.height = TaffyLength.Percent(0.75f);
                item.flexGrow = 3f;

                InvokeReset(new UnityEngine.Object[] { item }, "Item", "PositionSize");
                Assert.That(item.width.unit, Is.EqualTo(TaffyUnit.Auto));
                Assert.That(item.height.unit, Is.EqualTo(TaffyUnit.Auto));
                Assert.That(item.flexGrow, Is.EqualTo(3f));

                Undo.PerformUndo();
                Assert.That(item.width.unit, Is.EqualTo(TaffyUnit.Points));
                Assert.That(item.width.value, Is.EqualTo(210f));
                Assert.That(item.height.unit, Is.EqualTo(TaffyUnit.Percent));
                Assert.That(item.height.value, Is.EqualTo(0.75f));
                Assert.That(item.flexGrow, Is.EqualTo(3f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void CopyPasteSizeDeepCopiesCalcAndUndoRestoresTarget()
        {
            GameObject sourceGo = new GameObject("Source", typeof(RectTransform), typeof(TaffyLayoutItem));
            GameObject targetGo = new GameObject("Target", typeof(RectTransform), typeof(TaffyLayoutItem));
            try
            {
                TaffyLayoutItem source = sourceGo.GetComponent<TaffyLayoutItem>();
                TaffyLayoutItem target = targetGo.GetComponent<TaffyLayoutItem>();
                source.width = TaffyLength.Calc(TaffyCalcExpression.Add(TaffyCalcExpression.Length(100f), TaffyCalcExpression.Percent(0.25f)));
                source.height = TaffyLength.Points(44f);
                target.width = TaffyLength.Points(20f);

                InvokeClipboard("CopySize", source);
                source.width.calc.operands[0].value = 999f;
                InvokeClipboard("PasteSize", new[] { target });

                Assert.That(target.width.unit, Is.EqualTo(TaffyUnit.Calc));
                Assert.That(target.width.calc, Is.Not.Null);
                Assert.That(target.width.calc.operands[0].value, Is.EqualTo(100f));
                Assert.That(target.height.unit, Is.EqualTo(TaffyUnit.Points));
                Assert.That(target.height.value, Is.EqualTo(44f));

                Undo.PerformUndo();
                Assert.That(target.width.unit, Is.EqualTo(TaffyUnit.Points));
                Assert.That(target.width.value, Is.EqualTo(20f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(sourceGo);
                UnityEngine.Object.DestroyImmediate(targetGo);
            }
        }

        [Test]
        public void CopyPasteSpacingAndFlexWriteExactOwnedValues()
        {
            GameObject sourceGo = new GameObject("Source", typeof(RectTransform), typeof(TaffyLayoutItem));
            GameObject targetGo = new GameObject("Target", typeof(RectTransform), typeof(TaffyLayoutItem));
            try
            {
                TaffyLayoutItem source = sourceGo.GetComponent<TaffyLayoutItem>();
                TaffyLayoutItem target = targetGo.GetComponent<TaffyLayoutItem>();
                source.margin = Uniform(12f);
                source.padding = Uniform(6f);
                source.border = Uniform(2f);
                source.inset = TaffyEdges.Auto;
                source.flexBasis = TaffyLength.Percent(0.4f);
                source.flexGrow = 2f;
                source.flexShrink = 0.5f;
                source.alignSelf = TaffyAlign.Center;

                InvokeClipboard("CopySpacing", source);
                InvokeClipboard("PasteSpacing", new[] { target });
                InvokeClipboard("CopyFlex", source);
                InvokeClipboard("PasteFlex", new[] { target });

                Assert.That(target.margin.left.unit, Is.EqualTo(TaffyUnit.Points));
                Assert.That(target.margin.left.value, Is.EqualTo(12f));
                Assert.That(target.padding.top.value, Is.EqualTo(6f));
                Assert.That(target.border.bottom.value, Is.EqualTo(2f));
                Assert.That(target.inset.left.unit, Is.EqualTo(TaffyUnit.Auto));
                Assert.That(target.flexBasis.unit, Is.EqualTo(TaffyUnit.Percent));
                Assert.That(target.flexBasis.value, Is.EqualTo(0.4f));
                Assert.That(target.flexGrow, Is.EqualTo(2f));
                Assert.That(target.flexShrink, Is.EqualTo(0.5f));
                Assert.That(target.alignSelf, Is.EqualTo(TaffyAlign.Center));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(sourceGo);
                UnityEngine.Object.DestroyImmediate(targetGo);
            }
        }

        [Test]
        public void CopyPasteGridPlacementCopiesOnlySafePlacementFields()
        {
            GameObject sourceGo = new GameObject("Source", typeof(RectTransform), typeof(TaffyLayoutItem));
            GameObject targetGo = new GameObject("Target", typeof(RectTransform), typeof(TaffyLayoutItem));
            try
            {
                TaffyLayoutItem source = sourceGo.GetComponent<TaffyLayoutItem>();
                TaffyLayoutItem target = targetGo.GetComponent<TaffyLayoutItem>();
                source.gridRowStart = TaffyGridPlacement.Line(2);
                source.gridRowEnd = TaffyGridPlacement.Span(3);
                source.gridColumnStart = TaffyGridPlacement.NamedLine("content");
                source.gridColumnEnd = TaffyGridPlacement.NamedSpan("content", 2);
                source.justifySelf = TaffyAlign.End;
                source.flexGrow = 9f;
                target.flexGrow = 4f;

                InvokeClipboard("CopyGrid", source);
                InvokeClipboard("PasteGrid", new[] { target });

                Assert.That(target.gridRowStart.kind, Is.EqualTo(TaffyGridPlacementKind.Line));
                Assert.That(target.gridRowStart.line, Is.EqualTo(2));
                Assert.That(target.gridRowEnd.kind, Is.EqualTo(TaffyGridPlacementKind.Span));
                Assert.That(target.gridRowEnd.span, Is.EqualTo(3));
                Assert.That(target.gridColumnStart.name, Is.EqualTo("content"));
                Assert.That(target.gridColumnEnd.span, Is.EqualTo(2));
                Assert.That(target.justifySelf, Is.EqualTo(TaffyAlign.End));
                Assert.That(target.flexGrow, Is.EqualTo(4f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(sourceGo);
                UnityEngine.Object.DestroyImmediate(targetGo);
            }
        }

        private static TaffyEdges Uniform(float value)
        {
            TaffyLength length = TaffyLength.Points(value);
            return new TaffyEdges { left = length, right = length, top = length, bottom = length };
        }

        private static void InvokeReset(UnityEngine.Object[] targets, string inspectorKey, string sectionKey)
        {
            Type type = typeof(TaffyLayoutGroupEditor).Assembly.GetType("TaffyUGUI.Editor.TaffySectionResetActions");
            MethodInfo method = type.GetMethod("Reset", BindingFlags.Static | BindingFlags.NonPublic);
            method.Invoke(null, new object[] { targets, inspectorKey, sectionKey });
        }

        private static void InvokeClipboard(string methodName, object argument)
        {
            Type type = typeof(TaffyLayoutGroupEditor).Assembly.GetType("TaffyUGUI.Editor.TaffyExpertClipboard");
            Assert.That(type, Is.Not.Null);
            MethodInfo method = type.GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, methodName);
            method.Invoke(null, new[] { argument });
        }
    }
}
