using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TaffyUGUI.Editor;
using UnityEditor;

namespace TaffyUGUI.Tests
{
    public sealed class TaffyDX11EditorConsistencyTests
    {
        private static Assembly EditorAssembly => typeof(TaffyLayoutGroupEditor).Assembly;

        [Test]
        public void GroupAndItemAdvancedCoverageHasMeaningfulTooltipContent()
        {
            Type content = EditorAssembly.GetType("TaffyUGUI.Editor.TaffyEditorContent");
            MethodInfo tooltip = content.GetMethod("TooltipForProperty", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(tooltip, Is.Not.Null);

            AssertTooltipCoverage(GetCoverage(typeof(TaffyLayoutGroupEditor), "PropertyCoverage"), tooltip, "Group");
            AssertTooltipCoverage(GetCoverage(typeof(TaffyLayoutItemEditor), "PropertyCoverage"), tooltip, "Item");
        }

        [Test]
        public void AdvancedSectionCoverageExactlyMatchesDeclaredInspectorContracts()
        {
            string[] groupExpected = GetCoverage(typeof(TaffyLayoutGroupEditor), "PropertyCoverage");
            string[] groupActual = FlattenSectionCoverage(
                "TaffyUGUI.Editor.TaffyGroupFormattingSection",
                "TaffyUGUI.Editor.TaffyGroupFlexSection",
                "TaffyUGUI.Editor.TaffyGroupGridSection",
                "TaffyUGUI.Editor.TaffyGroupResponsiveSection");
            CollectionAssert.AreEquivalent(groupExpected, groupActual, "Advanced Group sections must expose every declared Group property exactly through the shared section model.");

            string[] itemExpected = GetCoverage(typeof(TaffyLayoutItemEditor), "PropertyCoverage");
            string[] itemActual = FlattenSectionCoverage(
                "TaffyUGUI.Editor.TaffyItemDisplaySection",
                "TaffyUGUI.Editor.TaffyItemPositionSizeSection",
                "TaffyUGUI.Editor.TaffyItemBoxModelSection",
                "TaffyUGUI.Editor.TaffyItemFlexSection",
                "TaffyUGUI.Editor.TaffyItemGridSection",
                "TaffyUGUI.Editor.TaffyItemBlockSection",
                "TaffyUGUI.Editor.TaffyItemMeasurementSection");
            CollectionAssert.AreEquivalent(itemExpected, itemActual, "Advanced Item sections must expose every declared Item property exactly through the shared section model.");
        }

        [Test]
        public void SimpleModeRemainsLimitedToTheEstablishedEssentialPropertySets()
        {
            string[] groupSimple = GetCoverage(EditorAssembly.GetType("TaffyUGUI.Editor.TaffyGroupQuickSetupSection"), "SimplePropertyCoverage");
            CollectionAssert.AreEqual(
                new[] { "containerDisplay", "direction", "gridAutoFlow", "justifyContent", "alignItems", "horizontalGap", "verticalGap", "m_Padding" },
                groupSimple);

            string[] itemSimple = GetCoverage(EditorAssembly.GetType("TaffyUGUI.Editor.TaffyItemEssentialsSection"), "SimplePropertyCoverage");
            CollectionAssert.AreEqual(
                new[] { "width", "height", "flexGrow", "alignSelf", "justifySelf" },
                itemSimple);
        }

        [Test]
        public void CustomEditorsRemainMultiObjectEnabled()
        {
            Assert.That(typeof(TaffyLayoutGroupEditor).GetCustomAttribute<CanEditMultipleObjects>(), Is.Not.Null);
            Assert.That(typeof(TaffyLayoutItemEditor).GetCustomAttribute<CanEditMultipleObjects>(), Is.Not.Null);
        }

        [Test]
        public void CoreCustomDrawersRemainRegisteredAndShareTheDrawerSpacingUtility()
        {
            string[] drawerTypes =
            {
                "TaffyUGUI.Editor.TaffyLengthDrawer",
                "TaffyUGUI.Editor.TaffyEdgesDrawer",
                "TaffyUGUI.Editor.TaffyPixelInsetsDrawer",
                "TaffyUGUI.Editor.TaffyCalcExpressionDrawer",
            };

            foreach (string name in drawerTypes)
            {
                Type type = EditorAssembly.GetType(name);
                Assert.That(type, Is.Not.Null, name + " must remain available.");
                Assert.That(type.GetCustomAttributes(typeof(CustomPropertyDrawer), false).Length, Is.GreaterThan(0), name + " must remain registered as a custom property drawer.");
            }

            Type utility = EditorAssembly.GetType("TaffyUGUI.Editor.TaffyDrawerUtility");
            Assert.That(utility, Is.Not.Null);
            Assert.That(utility.GetProperty("Line", BindingFlags.Static | BindingFlags.NonPublic), Is.Not.Null);
            Assert.That(utility.GetProperty("Gap", BindingFlags.Static | BindingFlags.NonPublic), Is.Not.Null);
            Assert.That(utility.GetMethod("StackHeight", BindingFlags.Static | BindingFlags.NonPublic), Is.Not.Null);
        }

        private static void AssertTooltipCoverage(IEnumerable<string> properties, MethodInfo tooltip, string label)
        {
            foreach (string propertyName in properties)
            {
                string value = (string)tooltip.Invoke(null, new object[] { propertyName });
                Assert.That(value, Is.Not.Null.And.Not.Empty, label + " property " + propertyName + " is missing tooltip/help content.");
                Assert.That(value, Is.Not.EqualTo("TaffyUGUI layout authoring property."), label + " property " + propertyName + " is using placeholder tooltip/help content.");
            }
        }

        private static string[] FlattenSectionCoverage(params string[] typeNames)
        {
            var result = new List<string>();
            foreach (string typeName in typeNames)
                result.AddRange(GetCoverage(EditorAssembly.GetType(typeName), "Properties"));
            Assert.That(result.Count, Is.EqualTo(result.Distinct(StringComparer.Ordinal).Count()), "An Advanced property is exposed by more than one section.");
            return result.ToArray();
        }

        private static string[] GetCoverage(Type type, string fieldName)
        {
            Assert.That(type, Is.Not.Null);
            FieldInfo field = type.GetField(fieldName, BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.That(field, Is.Not.Null, type.FullName + "." + fieldName + " is missing.");
            return (string[])field.GetValue(null);
        }
    }
}
