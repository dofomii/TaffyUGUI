using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TaffyUGUI.Editor;
using UnityEditor;
using UnityEngine;

namespace TaffyUGUI.Tests
{
    public sealed class TaffyDX3IntentEditingTests
    {
        private readonly List<GameObject> _owned = new List<GameObject>();
        private static Assembly EditorAssembly => typeof(TaffyLayoutGroupEditor).Assembly;

        [TearDown]
        public void TearDown()
        {
            for (int i = _owned.Count - 1; i >= 0; i--)
            {
                if (_owned[i])
                    UnityEngine.Object.DestroyImmediate(_owned[i]);
            }
            _owned.Clear();
        }

        [Test]
        public void PercentageAuthoringMapsHumanPercentToRuntimeFraction()
        {
            TaffyLayoutItem item = CreateItem("PercentItem");
            var serialized = new SerializedObject(item);
            SerializedProperty width = serialized.FindProperty("width");
            Type utility = EditorAssembly.GetType("TaffyUGUI.Editor.TaffyLengthAuthoringUtility");
            Type intentType = EditorAssembly.GetType("TaffyUGUI.Editor.TaffyLengthIntent");

            Invoke(utility, "SetIntent", width, Enum.Parse(intentType, "Percent"));
            Invoke(utility, "SetDisplayValue", width, 37.5f);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            serialized.Update();

            Assert.That(width.FindPropertyRelative("unit").intValue, Is.EqualTo((int)TaffyUnit.Percent));
            Assert.That(width.FindPropertyRelative("value").floatValue, Is.EqualTo(0.375f).Within(0.0001f));
            Assert.That((float)Invoke(utility, "GetDisplayValue", width), Is.EqualTo(37.5f).Within(0.001f));
            Assert.That((string)Invoke(utility, "Summary", width), Is.EqualTo("37.5%"));
        }

        [Test]
        public void FillParentMapsExactlyToOneHundredPercent()
        {
            TaffyLayoutItem item = CreateItem("FillItem");
            var serialized = new SerializedObject(item);
            SerializedProperty height = serialized.FindProperty("height");
            Type utility = EditorAssembly.GetType("TaffyUGUI.Editor.TaffyLengthAuthoringUtility");
            Type intentType = EditorAssembly.GetType("TaffyUGUI.Editor.TaffyLengthIntent");

            object fill = Enum.Parse(intentType, "FillParent");
            Invoke(utility, "SetIntent", height, fill);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            serialized.Update();

            Assert.That(height.FindPropertyRelative("unit").intValue, Is.EqualTo((int)TaffyUnit.Percent));
            Assert.That(height.FindPropertyRelative("value").floatValue, Is.EqualTo(1f));
            Assert.That(Convert.ToInt32(Invoke(utility, "GetIntent", height)), Is.EqualTo(Convert.ToInt32(fill)));
            Assert.That((string)Invoke(utility, "Summary", height), Is.EqualTo("100%"));
        }

        [Test]
        public void EdgeModesLinkAndUnlinkSidesWithoutChangingRuntimeShape()
        {
            TaffyLayoutItem item = CreateItem("EdgesItem");
            item.margin = new TaffyEdges
            {
                left = TaffyLength.Points(1f),
                right = TaffyLength.Points(2f),
                top = TaffyLength.Points(3f),
                bottom = TaffyLength.Points(4f),
            };
            var serialized = new SerializedObject(item);
            SerializedProperty margin = serialized.FindProperty("margin");
            Type utility = EditorAssembly.GetType("TaffyUGUI.Editor.TaffyEdgesAuthoringUtility");
            Type modeType = EditorAssembly.GetType("TaffyUGUI.Editor.TaffyEdgesAuthoringMode");

            object axis = Enum.Parse(modeType, "Axis");
            Invoke(utility, "SetMode", margin, axis);
            margin.FindPropertyRelative("left").FindPropertyRelative("value").floatValue = 11f;
            margin.FindPropertyRelative("top").FindPropertyRelative("value").floatValue = 22f;
            Invoke(utility, "SynchronizeLinkedSides", margin, axis, true, true);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            serialized.Update();

            Assert.That(item.margin.left.value, Is.EqualTo(11f));
            Assert.That(item.margin.right.value, Is.EqualTo(11f));
            Assert.That(item.margin.top.value, Is.EqualTo(22f));
            Assert.That(item.margin.bottom.value, Is.EqualTo(22f));

            object individual = Enum.Parse(modeType, "Individual");
            Invoke(utility, "SetMode", margin, individual);
            margin.FindPropertyRelative("right").FindPropertyRelative("value").floatValue = 99f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Assert.That(item.margin.left.value, Is.EqualTo(11f));
            Assert.That(item.margin.right.value, Is.EqualTo(99f));
        }

        [Test]
        public void VisualAlignmentMappingsUseExistingSerializedEnumValuesExactly()
        {
            Type controls = EditorAssembly.GetType("TaffyUGUI.Editor.TaffyVisualAuthoringControls");
            int[] directions = (int[])controls.GetField("DirectionValues", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            int[] justify = (int[])controls.GetField("JustifyValues", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            int[] align = (int[])controls.GetField("AlignValues", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);

            CollectionAssert.AreEqual(new[] { (int)TaffyFlexDirection.Row, (int)TaffyFlexDirection.Column, (int)TaffyFlexDirection.RowReverse, (int)TaffyFlexDirection.ColumnReverse }, directions);
            CollectionAssert.AreEqual(new[] { (int)TaffyJustify.Start, (int)TaffyJustify.Center, (int)TaffyJustify.End, (int)TaffyJustify.SpaceBetween }, justify);
            CollectionAssert.AreEqual(new[] { (int)TaffyAlign.Start, (int)TaffyAlign.Center, (int)TaffyAlign.End, (int)TaffyAlign.Stretch }, align);

            MethodInfo valueForIndex = controls.GetMethod("ValueForToolbarIndex", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That((int)valueForIndex.Invoke(null, new object[] { 1, directions }), Is.EqualTo((int)TaffyFlexDirection.Column));
            Assert.That((int)valueForIndex.Invoke(null, new object[] { 3, justify }), Is.EqualTo((int)TaffyJustify.SpaceBetween));
            Assert.That((int)valueForIndex.Invoke(null, new object[] { 3, align }), Is.EqualTo((int)TaffyAlign.Stretch));
        }

        [Test]
        public void ComplexCalcAndGridValuesSurviveIntentAndSummaryInspection()
        {
            TaffyLayoutItem item = CreateItem("ComplexItem");
            item.width = TaffyLength.Calc(TaffyCalcExpression.Add(TaffyCalcExpression.Percent(0.5f), TaffyCalcExpression.Length(12f)));
            item.gridColumnStart = TaffyGridPlacement.NamedLine("content", 2);
            item.gridColumnEnd = TaffyGridPlacement.Span(3);

            string before = JsonUtility.ToJson(item);
            var serialized = new SerializedObject(item);
            Type lengthUtility = EditorAssembly.GetType("TaffyUGUI.Editor.TaffyLengthAuthoringUtility");
            Type summaryUtility = EditorAssembly.GetType("TaffyUGUI.Editor.TaffyInspectorSummaryUtility");

            Assert.That((string)Invoke(lengthUtility, "Summary", serialized.FindProperty("width")), Is.EqualTo("Calc"));
            string placement = (string)Invoke(summaryUtility, "GridPlacementSummary", serialized);
            StringAssert.Contains("content", placement);
            StringAssert.Contains("Span 3", placement);
            string after = JsonUtility.ToJson(item);
            Assert.That(after, Is.EqualTo(before), "Reading intent/summaries must not mutate existing Calc or Grid data.");
        }

        private TaffyLayoutItem CreateItem(string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.SetActive(false);
            _owned.Add(go);
            return go.AddComponent<TaffyLayoutItem>();
        }

        private static object Invoke(Type type, string methodName, params object[] args)
        {
            Assert.That(type, Is.Not.Null, methodName + " type");
            MethodInfo[] methods = type.GetMethods(BindingFlags.Static | BindingFlags.NonPublic);
            for (int i = 0; i < methods.Length; i++)
            {
                if (methods[i].Name == methodName && methods[i].GetParameters().Length == args.Length)
                    return methods[i].Invoke(null, args);
            }
            Assert.Fail("Missing method: " + type.FullName + "." + methodName);
            return null;
        }
    }
}
