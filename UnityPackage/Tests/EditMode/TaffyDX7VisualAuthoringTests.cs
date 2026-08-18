using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TaffyUGUI.Editor;
using UnityEditor;
using UnityEngine;

namespace TaffyUGUI.Tests
{
    public sealed class TaffyDX7VisualAuthoringTests
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
        public void ResponsiveAuthoringMapsExactlyToExistingProfileFields()
        {
            TaffyLayoutGroup group = CreateGroup("ResponsiveGroup");
            var serialized = new SerializedObject(group);
            SerializedProperty profiles = serialized.FindProperty("responsiveProfiles");
            Type utility = EditorAssembly.GetType("TaffyUGUI.Editor.TaffyResponsiveAuthoringUtility");
            Type overrideKind = EditorAssembly.GetType("TaffyUGUI.Editor.TaffyResponsiveOverrideKind");

            SerializedProperty profile = (SerializedProperty)Invoke(utility, "AddProfile", profiles, "Tablet");
            profile.FindPropertyRelative("priority").intValue = 20;
            profile.FindPropertyRelative("minWidth").floatValue = 600f;
            profile.FindPropertyRelative("maxWidth").floatValue = 1024f;
            profile.FindPropertyRelative("minHeight").floatValue = 400f;
            profile.FindPropertyRelative("maxHeight").floatValue = 0f;

            object directionKind = Enum.Parse(overrideKind, "FlexDirection");
            object gapsKind = Enum.Parse(overrideKind, "Gaps");
            Invoke(utility, "SetOverrideEnabled", profile, directionKind, true);
            Invoke(utility, "SetOverrideEnabled", profile, gapsKind, true);
            profile.FindPropertyRelative("direction").intValue = (int)TaffyFlexDirection.Column;
            profile.FindPropertyRelative("horizontalGap").floatValue = 12f;
            profile.FindPropertyRelative("verticalGap").floatValue = 18f;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            Assert.That(group.responsiveProfiles, Has.Count.EqualTo(1));
            TaffyResponsiveProfile runtime = group.responsiveProfiles[0];
            Assert.That(runtime.name, Is.EqualTo("Tablet"));
            Assert.That(runtime.priority, Is.EqualTo(20));
            Assert.That(runtime.minWidth, Is.EqualTo(600f));
            Assert.That(runtime.maxWidth, Is.EqualTo(1024f));
            Assert.That(runtime.minHeight, Is.EqualTo(400f));
            Assert.That(runtime.maxHeight, Is.EqualTo(0f));
            Assert.That(runtime.overrideFlexDirection, Is.True);
            Assert.That(runtime.direction, Is.EqualTo(TaffyFlexDirection.Column));
            Assert.That(runtime.overrideGaps, Is.True);
            Assert.That(runtime.horizontalGap, Is.EqualTo(12f));
            Assert.That(runtime.verticalGap, Is.EqualTo(18f));
            Assert.That(runtime.overridePadding, Is.False);
            Assert.That((int)Invoke(utility, "EnabledOverrideCount", profile), Is.EqualTo(2));
            StringAssert.Contains("W 600–1024", (string)Invoke(utility, "BreakpointSummary", profile));
        }

        [Test]
        public void ResponsiveOverlapWarningsOnlyFlagAmbiguousSamePriorityMatches()
        {
            TaffyLayoutGroup group = CreateGroup("OverlapGroup");
            group.responsiveProfiles = new List<TaffyResponsiveProfile>
            {
                new TaffyResponsiveProfile { name = "Small", priority = 10, minWidth = 0f, maxWidth = 700f },
                new TaffyResponsiveProfile { name = "Medium", priority = 10, minWidth = 600f, maxWidth = 1000f },
                new TaffyResponsiveProfile { name = "Large", priority = 20, minWidth = 900f, maxWidth = 0f },
            };

            var serialized = new SerializedObject(group);
            Type utility = EditorAssembly.GetType("TaffyUGUI.Editor.TaffyResponsiveAuthoringUtility");
            object result = Invoke(utility, "CollectOverlapWarnings", serialized.FindProperty("responsiveProfiles"));
            var warnings = result as System.Collections.IEnumerable;
            var collected = new List<string>();
            foreach (object warning in warnings)
                collected.Add((string)warning);

            Assert.That(collected, Has.Count.EqualTo(1));
            StringAssert.Contains("Small", collected[0]);
            StringAssert.Contains("Medium", collected[0]);
            StringAssert.DoesNotContain("Large", collected[0]);
        }

        [Test]
        public void GridVisualStartersAndPlacementMapToExistingGridStructures()
        {
            TaffyLayoutGroup group = CreateGroup("GridGroup");
            var groupSerialized = new SerializedObject(group);
            SerializedProperty columns = groupSerialized.FindProperty("gridColumns");
            Type utility = EditorAssembly.GetType("TaffyUGUI.Editor.TaffyGridAuthoringUtility");

            Invoke(utility, "SetEqualFractionTracks", columns, 3);
            groupSerialized.ApplyModifiedPropertiesWithoutUndo();

            Assert.That(group.gridColumns, Has.Count.EqualTo(3));
            for (int i = 0; i < group.gridColumns.Count; i++)
            {
                Assert.That(group.gridColumns[i].kind, Is.EqualTo(TaffyGridTrackKind.Fraction));
                Assert.That(group.gridColumns[i].value, Is.EqualTo(1f));
            }

            TaffyLayoutItem item = CreateItem("GridItem", group.transform);
            var itemSerialized = new SerializedObject(item);
            Invoke(utility, "SetPlacementSpan", itemSerialized.FindProperty("gridColumnEnd"), 2);
            Invoke(utility, "SetPlacementSpan", itemSerialized.FindProperty("gridRowEnd"), 3);
            itemSerialized.ApplyModifiedPropertiesWithoutUndo();

            Assert.That(item.gridColumnEnd.kind, Is.EqualTo(TaffyGridPlacementKind.Span));
            Assert.That(item.gridColumnEnd.span, Is.EqualTo(2));
            Assert.That(item.gridRowEnd.kind, Is.EqualTo(TaffyGridPlacementKind.Span));
            Assert.That(item.gridRowEnd.span, Is.EqualTo(3));
        }

        [Test]
        public void ComplexGridDataSurvivesVisualInspectionUnchanged()
        {
            TaffyLayoutGroup group = CreateGroup("ComplexGrid");
            group.containerDisplay = TaffyContainerDisplay.Grid;
            group.gridColumns = new List<TaffyGridTrack>
            {
                TaffyGridTrack.MinMax(TaffyGridTrackBreadth.Points(120f), TaffyGridTrackBreadth.Fraction(1f)),
                TaffyGridTrack.Repeat(
                    TaffyGridRepeatMode.AutoFit,
                    1,
                    TaffyGridTrack.MinMax(TaffyGridTrackBreadth.Points(80f), TaffyGridTrackBreadth.Fraction(1f))),
                TaffyGridTrack.Calc(TaffyCalcExpression.Add(TaffyCalcExpression.Percent(0.5f), TaffyCalcExpression.Length(16f))),
            };
            group.gridNamedLines = new List<TaffyGridNamedLine> { new TaffyGridNamedLine(TaffyGridAxis.Column, 1, "content") };
            group.gridAreas = new List<TaffyGridArea> { new TaffyGridArea("main", 1, 2, 1, 3) };
            group.gridAreaRows = 1;
            group.gridAreaColumns = 2;

            string before = JsonUtility.ToJson(group);
            var serialized = new SerializedObject(group);
            Type utility = EditorAssembly.GetType("TaffyUGUI.Editor.TaffyGridAuthoringUtility");
            SerializedProperty columns = serialized.FindProperty("gridColumns");

            Assert.That((string)Invoke(utility, "TrackSummary", columns.GetArrayElementAtIndex(0)), Is.EqualTo("MinMax"));
            StringAssert.Contains("Repeat", (string)Invoke(utility, "TrackSummary", columns.GetArrayElementAtIndex(1)));
            Assert.That((string)Invoke(utility, "TrackSummary", columns.GetArrayElementAtIndex(2)), Is.EqualTo("Calc"));

            string after = JsonUtility.ToJson(group);
            Assert.That(after, Is.EqualTo(before), "Visual Grid inspection must not rewrite complex existing Grid data.");
        }

        [Test]
        public void ExistingGridValidationStillRejectsInvalidRepeatStructures()
        {
            TaffyLayoutGroup group = CreateGroup("InvalidGrid");
            group.containerDisplay = TaffyContainerDisplay.Grid;
            group.gridColumns = new List<TaffyGridTrack>
            {
                new TaffyGridTrack
                {
                    kind = TaffyGridTrackKind.Repeat,
                    repeatMode = TaffyGridRepeatMode.Count,
                    repeatCount = 0,
                    repeatTracks = new List<TaffyGridTrack> { TaffyGridTrack.Fraction(1f) },
                },
            };

            Assert.That(group.ValidateGridAuthoring(out string error), Is.False);
            StringAssert.Contains("repeat", error.ToLowerInvariant());
            StringAssert.Contains("count", error.ToLowerInvariant());
        }

        private TaffyLayoutGroup CreateGroup(string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.SetActive(false);
            _owned.Add(go);
            return go.AddComponent<TaffyLayoutGroup>();
        }

        private TaffyLayoutItem CreateItem(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
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
