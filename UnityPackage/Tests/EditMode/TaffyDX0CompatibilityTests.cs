using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TaffyUGUI.Editor;
using UnityEditor;
using UnityEngine;

namespace TaffyUGUI.Tests
{
    public sealed class TaffyDX0CompatibilityTests
    {
        private readonly List<UnityEngine.Object> _owned = new List<UnityEngine.Object>();

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
        public void LayoutGroupDeclaredAuthoringFieldNamesRemainStable()
        {
            AssertDeclaredPublicInstanceFields<TaffyLayoutGroup>(
                "alignContent", "alignItems", "border", "boxSizing", "containerDisplay",
                "direction", "gridAreaColumns", "gridAreaRows", "gridAreas", "gridAutoColumns",
                "gridAutoFlow", "gridAutoRows", "gridColumns", "gridNamedLines", "gridRows",
                "horizontalGap", "justifyContent", "justifyItems", "maxRebuildRequestsPerFrame",
                "overflowX", "overflowY", "pixelRounding", "responsiveProfiles", "safeAreaMode",
                "scrollbarWidth", "scrollRectContentMode", "textAlign", "verticalGap",
                "wrap", "writingDirection");
        }

        [Test]
        public void LayoutItemDeclaredAuthoringFieldNamesRemainStable()
        {
            AssertDeclaredPublicInstanceFields<TaffyLayoutItem>(
                "alignSelf", "aspectRatio", "border", "boxSizing", "clearMode", "display",
                "flexBasis", "flexGrow", "flexShrink", "floatMode", "forceReplacedElement",
                "gridColumnEnd", "gridColumnStart", "gridRowEnd", "gridRowStart", "height",
                "inset", "itemIsTable", "justifySelf", "margin", "maxHeight", "maxWidth",
                "measurement", "minHeight", "minWidth", "overflowX", "overflowY", "padding",
                "position", "scrollbarWidth", "textAlign", "width", "writingDirection");
        }

        [Test]
        public void NestedAuthoringFieldNamesRemainStable()
        {
            AssertDeclaredPublicInstanceFields<TaffyLength>("calc", "unit", "value");
            AssertDeclaredPublicInstanceFields<TaffyEdges>("bottom", "left", "right", "top");
            AssertDeclaredPublicInstanceFields<TaffyCalcExpression>("operands", "operation", "value");
            AssertDeclaredPublicInstanceFields<TaffyGridTrackBreadth>("calc", "kind", "value");
            AssertDeclaredPublicInstanceFields<TaffyGridTrack>(
                "calc", "kind", "max", "min", "repeatCount", "repeatMode", "repeatTracks", "value");
            AssertDeclaredPublicInstanceFields<TaffyGridNamedLine>("axis", "lineIndex", "name");
            AssertDeclaredPublicInstanceFields<TaffyGridArea>("columnEnd", "columnStart", "name", "rowEnd", "rowStart");
            AssertDeclaredPublicInstanceFields<TaffyGridPlacement>("kind", "line", "name", "occurrence", "span");
            AssertDeclaredPublicInstanceFields<TaffyResponsiveProfile>(
                "alignContent", "alignItems", "containerDisplay", "direction", "gridAutoFlow",
                "horizontalGap", "justifyContent", "justifyItems", "maxHeight", "maxWidth",
                "minHeight", "minWidth", "name", "overrideAlignment", "overrideContainerDisplay",
                "overrideFlexDirection", "overrideFlexWrap", "overrideGaps", "overrideGridAutoFlow",
                "overridePadding", "padding", "priority", "verticalGap", "wrap");
            AssertDeclaredPublicInstanceFields<TaffyPixelInsets>("bottom", "left", "right", "top");
            AssertDeclaredPublicInstanceFields<TaffyMeasurementSample>("availableWidth", "size");
        }

        [Test]
        public void SerializedEnumNumericContractsRemainStable()
        {
            AssertEnum<TaffyUnit>(
                ("Auto", 0), ("Points", 1), ("Percent", 2), ("Calc", 3));
            AssertEnum<TaffyContainerDisplay>(
                ("Flex", 1), ("Grid", 2), ("Block", 3), ("FlowRoot", 4));
            AssertEnum<TaffyDisplay>(
                ("None", 0), ("Flex", 1), ("Grid", 2), ("Block", 3), ("FlowRoot", 4));
            AssertEnum<TaffyBoxSizing>(("BorderBox", 0), ("ContentBox", 1));
            AssertEnum<TaffyWritingDirection>(("LeftToRight", 0), ("RightToLeft", 1));
            AssertEnum<TaffyOverflow>(("Visible", 0), ("Clip", 1), ("Hidden", 2), ("Scroll", 3));
            AssertEnum<TaffyPosition>(("Relative", 0), ("Absolute", 1));
            AssertEnum<TaffyFlexDirection>(("Row", 0), ("Column", 1), ("RowReverse", 2), ("ColumnReverse", 3));
            AssertEnum<TaffyFlexWrap>(("NoWrap", 0), ("Wrap", 1), ("WrapReverse", 2));
            AssertEnum<TaffyAlign>(
                ("Auto", -1), ("Start", 0), ("End", 1), ("Center", 2), ("Stretch", 3),
                ("Baseline", 4), ("FlexStart", 5), ("FlexEnd", 6), ("SelfStart", 7), ("SelfEnd", 8),
                ("SafeStart", 9), ("SafeEnd", 10), ("SafeCenter", 11), ("SafeFlexStart", 12),
                ("SafeFlexEnd", 13), ("SafeSelfStart", 14), ("SafeSelfEnd", 15));
            AssertEnum<TaffyJustify>(
                ("Auto", -1), ("Start", 0), ("End", 1), ("Center", 2), ("SpaceBetween", 3),
                ("SpaceAround", 4), ("SpaceEvenly", 5), ("FlexStart", 6), ("FlexEnd", 7),
                ("SafeStart", 8), ("SafeEnd", 9), ("SafeCenter", 10), ("SafeFlexStart", 11), ("SafeFlexEnd", 12));
            AssertEnum<TaffyAlignContent>(
                ("Auto", -1), ("Start", 0), ("End", 1), ("Center", 2), ("Stretch", 3),
                ("SpaceBetween", 4), ("SpaceAround", 5), ("SpaceEvenly", 6), ("FlexStart", 7),
                ("FlexEnd", 8), ("SafeStart", 9), ("SafeEnd", 10), ("SafeCenter", 11),
                ("SafeFlexStart", 12), ("SafeFlexEnd", 13));
            AssertEnum<TaffyFloat>(("None", 0), ("Left", 1), ("Right", 2));
            AssertEnum<TaffyClear>(("None", 0), ("Left", 1), ("Right", 2), ("Both", 3));
            AssertEnum<TaffyTextAlign>(("Auto", 0), ("LegacyLeft", 1), ("LegacyRight", 2), ("LegacyCenter", 3));
            AssertEnum<TaffyMeasurementMode>(("Auto", 0), ("Disabled", 1));
            AssertEnum<TaffyGridAutoFlow>(("Row", 0), ("Column", 1), ("RowDense", 2), ("ColumnDense", 3));
            AssertEnum<TaffyGridAxis>(("Row", 0), ("Column", 1));
            AssertEnum<TaffyGridRepeatMode>(("Count", 0), ("AutoFill", 1), ("AutoFit", 2));
            AssertEnum<TaffyGridTrackKind>(
                ("Auto", 0), ("Points", 1), ("Percent", 2), ("Fraction", 3), ("MinMax", 4),
                ("MinContent", 5), ("MaxContent", 6), ("Calc", 7), ("Repeat", 8));
            AssertEnum<TaffyGridTrackBreadthKind>(
                ("Auto", 0), ("Points", 1), ("Percent", 2), ("Fraction", 3),
                ("MinContent", 5), ("MaxContent", 6), ("Calc", 7));
            AssertEnum<TaffyGridPlacementKind>(
                ("Auto", 0), ("Line", 1), ("Span", 2), ("NamedLine", 3), ("NamedSpan", 4));
            AssertEnum<TaffyCalcOperation>(
                ("Length", 0), ("Percent", 1), ("Add", 2), ("Subtract", 3),
                ("Scale", 4), ("Min", 5), ("Max", 6), ("Clamp", 7));
            AssertEnum<TaffySafeAreaMode>(("Disabled", 0), ("Padding", 1));
            AssertEnum<TaffyScrollRectContentMode>(("Disabled", 0), ("AutoExpandContent", 1));
            AssertEnum<TaffyPixelRounding>(
                ("None", 0), ("Round", 1), ("Floor", 2), ("Ceil", 3), ("CanvasPixel", 4));
        }

        [Test]
        public void RepresentativeLayoutGroupDataSurvivesEditorSerializationRoundTrip()
        {
            TaffyLayoutGroup source = CreateInactiveGroup("GroupSource");
            source.containerDisplay = TaffyContainerDisplay.Grid;
            source.boxSizing = TaffyBoxSizing.ContentBox;
            source.writingDirection = TaffyWritingDirection.RightToLeft;
            source.overflowX = TaffyOverflow.Hidden;
            source.overflowY = TaffyOverflow.Scroll;
            source.scrollbarWidth = 11f;
            source.padding = new RectOffset(3, 5, 7, 9);
            source.border = TaffyEdges.Points(2f);
            source.textAlign = TaffyTextAlign.LegacyCenter;
            source.direction = TaffyFlexDirection.ColumnReverse;
            source.wrap = TaffyFlexWrap.WrapReverse;
            source.horizontalGap = 13f;
            source.verticalGap = 17f;
            source.justifyContent = TaffyJustify.SpaceEvenly;
            source.alignItems = TaffyAlign.SafeCenter;
            source.alignContent = TaffyAlignContent.SpaceAround;
            source.justifyItems = TaffyAlign.End;
            source.gridAutoFlow = TaffyGridAutoFlow.ColumnDense;
            source.gridColumns = new List<TaffyGridTrack>
            {
                TaffyGridTrack.Repeat(
                    TaffyGridRepeatMode.Count,
                    2,
                    TaffyGridTrack.MinMax(TaffyGridTrackBreadth.Points(30f), TaffyGridTrackBreadth.Fraction(1f)))
            };
            source.gridRows = new List<TaffyGridTrack>
            {
                TaffyGridTrack.Calc(TaffyCalcExpression.Add(TaffyCalcExpression.Length(10f), TaffyCalcExpression.Percent(0.25f)))
            };
            source.gridNamedLines = new List<TaffyGridNamedLine>
            {
                new TaffyGridNamedLine(TaffyGridAxis.Column, 1, "content-start")
            };
            source.gridAreas = new List<TaffyGridArea>
            {
                new TaffyGridArea("main", 1, 2, 1, 3)
            };
            source.gridAreaRows = 1;
            source.gridAreaColumns = 2;
            source.responsiveProfiles = new List<TaffyResponsiveProfile>
            {
                new TaffyResponsiveProfile
                {
                    name = "Mobile",
                    priority = 5,
                    minWidth = 100f,
                    maxWidth = 600f,
                    minHeight = 200f,
                    maxHeight = 900f,
                    overrideContainerDisplay = true,
                    containerDisplay = TaffyContainerDisplay.Flex,
                    overrideFlexDirection = true,
                    direction = TaffyFlexDirection.Column,
                    overrideFlexWrap = true,
                    wrap = TaffyFlexWrap.Wrap,
                    overrideGaps = true,
                    horizontalGap = 4f,
                    verticalGap = 6f,
                    overrideAlignment = true,
                    justifyContent = TaffyJustify.Center,
                    alignItems = TaffyAlign.Stretch,
                    alignContent = TaffyAlignContent.Center,
                    justifyItems = TaffyAlign.Center,
                    overrideGridAutoFlow = true,
                    gridAutoFlow = TaffyGridAutoFlow.RowDense,
                    overridePadding = true,
                    padding = new TaffyPixelInsets(1f, 2f, 3f, 4f),
                }
            };
            source.safeAreaMode = TaffySafeAreaMode.Padding;
            source.scrollRectContentMode = TaffyScrollRectContentMode.Disabled;
            source.pixelRounding = TaffyPixelRounding.CanvasPixel;
            source.maxRebuildRequestsPerFrame = 3;

            string json = EditorJsonUtility.ToJson(source);
            TaffyLayoutGroup clone = CreateInactiveGroup("GroupClone");
            EditorJsonUtility.FromJsonOverwrite(json, clone);

            Assert.That(clone.containerDisplay, Is.EqualTo(TaffyContainerDisplay.Grid));
            Assert.That(clone.padding.left, Is.EqualTo(3));
            Assert.That(clone.padding.right, Is.EqualTo(5));
            Assert.That(clone.border.left.value, Is.EqualTo(2f));
            Assert.That(clone.direction, Is.EqualTo(TaffyFlexDirection.ColumnReverse));
            Assert.That(clone.horizontalGap, Is.EqualTo(13f));
            Assert.That(clone.justifyContent, Is.EqualTo(TaffyJustify.SpaceEvenly));
            Assert.That(clone.gridColumns.Count, Is.EqualTo(1));
            Assert.That(clone.gridColumns[0].kind, Is.EqualTo(TaffyGridTrackKind.Repeat));
            Assert.That(clone.gridColumns[0].repeatTracks.Count, Is.EqualTo(1));
            Assert.That(clone.gridColumns[0].repeatTracks[0].kind, Is.EqualTo(TaffyGridTrackKind.MinMax));
            Assert.That(clone.gridRows[0].calc.operation, Is.EqualTo(TaffyCalcOperation.Add));
            Assert.That(clone.gridRows[0].calc.operands.Count, Is.EqualTo(2));
            Assert.That(clone.gridNamedLines[0].name, Is.EqualTo("content-start"));
            Assert.That(clone.gridAreas[0].name, Is.EqualTo("main"));
            Assert.That(clone.responsiveProfiles.Count, Is.EqualTo(1));
            Assert.That(clone.responsiveProfiles[0].name, Is.EqualTo("Mobile"));
            Assert.That(clone.responsiveProfiles[0].overridePadding, Is.True);
            Assert.That(clone.responsiveProfiles[0].padding.bottom, Is.EqualTo(4f));
            Assert.That(clone.safeAreaMode, Is.EqualTo(TaffySafeAreaMode.Padding));
            Assert.That(clone.pixelRounding, Is.EqualTo(TaffyPixelRounding.CanvasPixel));
            Assert.That(clone.maxRebuildRequestsPerFrame, Is.EqualTo(3));
        }

        [Test]
        public void RepresentativeLayoutItemDataSurvivesEditorSerializationRoundTrip()
        {
            TaffyLayoutItem source = CreateInactiveItem("ItemSource");
            source.display = TaffyDisplay.FlowRoot;
            source.boxSizing = TaffyBoxSizing.ContentBox;
            source.writingDirection = TaffyWritingDirection.RightToLeft;
            source.overflowX = TaffyOverflow.Clip;
            source.overflowY = TaffyOverflow.Hidden;
            source.scrollbarWidth = 8f;
            source.position = TaffyPosition.Absolute;
            source.inset = new TaffyEdges
            {
                left = TaffyLength.Points(10f),
                right = TaffyLength.Auto,
                top = TaffyLength.Percent(0.1f),
                bottom = TaffyLength.Calc(TaffyCalcExpression.Length(4f)),
            };
            source.width = TaffyLength.Calc(TaffyCalcExpression.Add(TaffyCalcExpression.Percent(0.5f), TaffyCalcExpression.Length(12f)));
            source.height = TaffyLength.Points(44f);
            source.minWidth = TaffyLength.Points(20f);
            source.minHeight = TaffyLength.Percent(0.2f);
            source.maxWidth = TaffyLength.Points(500f);
            source.maxHeight = TaffyLength.Points(300f);
            source.aspectRatio = 1.5f;
            source.margin = TaffyEdges.Points(6f);
            source.padding = TaffyEdges.Points(7f);
            source.border = TaffyEdges.Points(1f);
            source.flexBasis = TaffyLength.Percent(0.33f);
            source.flexGrow = 2f;
            source.flexShrink = 0.5f;
            source.alignSelf = TaffyAlign.SafeEnd;
            source.gridRowStart = TaffyGridPlacement.NamedLine("content", 2);
            source.gridRowEnd = TaffyGridPlacement.Span(2);
            source.gridColumnStart = TaffyGridPlacement.Line(2);
            source.gridColumnEnd = TaffyGridPlacement.NamedSpan("content", 3);
            source.justifySelf = TaffyAlign.Center;
            source.floatMode = TaffyFloat.Left;
            source.clearMode = TaffyClear.Both;
            source.textAlign = TaffyTextAlign.LegacyRight;
            source.measurement = TaffyMeasurementMode.Disabled;
            source.forceReplacedElement = true;
            source.itemIsTable = true;

            string json = EditorJsonUtility.ToJson(source);
            TaffyLayoutItem clone = CreateInactiveItem("ItemClone");
            EditorJsonUtility.FromJsonOverwrite(json, clone);

            Assert.That(clone.display, Is.EqualTo(TaffyDisplay.FlowRoot));
            Assert.That(clone.position, Is.EqualTo(TaffyPosition.Absolute));
            Assert.That(clone.inset.left.value, Is.EqualTo(10f));
            Assert.That(clone.inset.bottom.calc.operation, Is.EqualTo(TaffyCalcOperation.Length));
            Assert.That(clone.width.unit, Is.EqualTo(TaffyUnit.Calc));
            Assert.That(clone.width.calc.operation, Is.EqualTo(TaffyCalcOperation.Add));
            Assert.That(clone.width.calc.operands.Count, Is.EqualTo(2));
            Assert.That(clone.height.value, Is.EqualTo(44f));
            Assert.That(clone.aspectRatio, Is.EqualTo(1.5f));
            Assert.That(clone.margin.left.value, Is.EqualTo(6f));
            Assert.That(clone.flexBasis.unit, Is.EqualTo(TaffyUnit.Percent));
            Assert.That(clone.flexGrow, Is.EqualTo(2f));
            Assert.That(clone.flexShrink, Is.EqualTo(0.5f));
            Assert.That(clone.gridRowStart.kind, Is.EqualTo(TaffyGridPlacementKind.NamedLine));
            Assert.That(clone.gridRowStart.name, Is.EqualTo("content"));
            Assert.That(clone.gridRowStart.occurrence, Is.EqualTo(2));
            Assert.That(clone.gridColumnEnd.kind, Is.EqualTo(TaffyGridPlacementKind.NamedSpan));
            Assert.That(clone.gridColumnEnd.span, Is.EqualTo(3));
            Assert.That(clone.floatMode, Is.EqualTo(TaffyFloat.Left));
            Assert.That(clone.clearMode, Is.EqualTo(TaffyClear.Both));
            Assert.That(clone.measurement, Is.EqualTo(TaffyMeasurementMode.Disabled));
            Assert.That(clone.forceReplacedElement, Is.True);
            Assert.That(clone.itemIsTable, Is.True);
        }

        [Test]
        public void EditorAssemblyRemainsEditorOnlyAndRuntimeDoesNotReferenceIt()
        {
            Assembly runtimeAssembly = typeof(TaffyLayoutGroup).Assembly;
            Assembly editorAssembly = typeof(TaffyLayoutGroupEditor).Assembly;

            Assert.That(runtimeAssembly.GetName().Name, Is.EqualTo("TaffyUGUI.Runtime"));
            Assert.That(editorAssembly.GetName().Name, Is.EqualTo("TaffyUGUI.Editor"));
            CollectionAssert.DoesNotContain(
                runtimeAssembly.GetReferencedAssemblies().Select(name => name.Name).ToArray(),
                "TaffyUGUI.Editor");

            string runtimeAsmdef = ReadAsmdef("TaffyUGUI.Runtime");
            string editorAsmdef = ReadAsmdef("TaffyUGUI.Editor");
            StringAssert.DoesNotContain("TaffyUGUI.Editor", runtimeAsmdef);
            StringAssert.Contains("\"includePlatforms\":[\"Editor\"]", CompactJson(editorAsmdef));
        }

        [Test]
        public void RepresentativeCustomEditorsInstantiateWithoutExceptions()
        {
            TaffyLayoutGroup group = CreateInactiveGroup("GroupEditor");
            TaffyLayoutItem item = CreateInactiveItem("ItemEditor");

            UnityEditor.Editor groupEditor = null;
            UnityEditor.Editor itemEditor = null;
            Assert.DoesNotThrow(() => groupEditor = UnityEditor.Editor.CreateEditor(group));
            Assert.DoesNotThrow(() => itemEditor = UnityEditor.Editor.CreateEditor(item));
            try
            {
                Assert.That(groupEditor, Is.TypeOf<TaffyLayoutGroupEditor>());
                Assert.That(itemEditor, Is.TypeOf<TaffyLayoutItemEditor>());
                Assert.That(groupEditor.serializedObject.FindProperty("containerDisplay"), Is.Not.Null);
                Assert.That(groupEditor.serializedObject.FindProperty("m_Padding"), Is.Not.Null);
                Assert.That(itemEditor.serializedObject.FindProperty("width"), Is.Not.Null);
                Assert.That(itemEditor.serializedObject.FindProperty("measurement"), Is.Not.Null);
            }
            finally
            {
                DestroyEditor(groupEditor);
                DestroyEditor(itemEditor);
            }
        }

        [Test]
        public void GroupAndItemCustomEditorsSupportMultiObjectTargets()
        {
            TaffyLayoutGroup firstGroup = CreateInactiveGroup("GroupA");
            TaffyLayoutGroup secondGroup = CreateInactiveGroup("GroupB");
            TaffyLayoutItem firstItem = CreateInactiveItem("ItemA");
            TaffyLayoutItem secondItem = CreateInactiveItem("ItemB");

            UnityEditor.Editor groupEditor = UnityEditor.Editor.CreateEditor(new UnityEngine.Object[] { firstGroup, secondGroup });
            UnityEditor.Editor itemEditor = UnityEditor.Editor.CreateEditor(new UnityEngine.Object[] { firstItem, secondItem });
            try
            {
                Assert.That(groupEditor, Is.TypeOf<TaffyLayoutGroupEditor>());
                Assert.That(groupEditor.targets.Length, Is.EqualTo(2));
                Assert.That(groupEditor.serializedObject.isEditingMultipleObjects, Is.True);
                Assert.That(groupEditor.serializedObject.FindProperty("direction"), Is.Not.Null);

                Assert.That(itemEditor, Is.TypeOf<TaffyLayoutItemEditor>());
                Assert.That(itemEditor.targets.Length, Is.EqualTo(2));
                Assert.That(itemEditor.serializedObject.isEditingMultipleObjects, Is.True);
                Assert.That(itemEditor.serializedObject.FindProperty("flexGrow"), Is.Not.Null);
            }
            finally
            {
                DestroyEditor(groupEditor);
                DestroyEditor(itemEditor);
            }
        }

        private TaffyLayoutGroup CreateInactiveGroup(string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.SetActive(false);
            _owned.Add(go);
            return go.AddComponent<TaffyLayoutGroup>();
        }

        private TaffyLayoutItem CreateInactiveItem(string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.SetActive(false);
            _owned.Add(go);
            return go.AddComponent<TaffyLayoutItem>();
        }

        private static void DestroyEditor(UnityEditor.Editor editor)
        {
            if (editor)
                UnityEngine.Object.DestroyImmediate(editor);
        }

        private static void AssertDeclaredPublicInstanceFields<T>(params string[] expected)
        {
            string[] actual = typeof(T)
                .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                .Select(field => field.Name)
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToArray();
            string[] sortedExpected = expected.OrderBy(name => name, StringComparer.Ordinal).ToArray();
            CollectionAssert.AreEqual(sortedExpected, actual, typeof(T).Name + " serialized/public authoring field contract changed.");
        }

        private static void AssertEnum<T>(params (string name, int value)[] expected) where T : Enum
        {
            string[] actualNames = Enum.GetNames(typeof(T)).OrderBy(name => name, StringComparer.Ordinal).ToArray();
            string[] expectedNames = expected.Select(pair => pair.name).OrderBy(name => name, StringComparer.Ordinal).ToArray();
            CollectionAssert.AreEqual(expectedNames, actualNames, typeof(T).Name + " member set changed.");
            foreach ((string name, int value) pair in expected)
                Assert.That(Convert.ToInt32(Enum.Parse(typeof(T), pair.name)), Is.EqualTo(pair.value), typeof(T).Name + "." + pair.name + " numeric value changed.");
        }

        private static string ReadAsmdef(string assemblyName)
        {
            string[] guids = AssetDatabase.FindAssets(assemblyName + " t:AssemblyDefinitionAsset");
            Assert.That(guids.Length, Is.GreaterThanOrEqualTo(1), "Could not locate " + assemblyName + " asmdef.");
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return File.ReadAllText(path);
        }

        private static string CompactJson(string value)
        {
            return new string(value.Where(c => !char.IsWhiteSpace(c)).ToArray());
        }
    }
}
