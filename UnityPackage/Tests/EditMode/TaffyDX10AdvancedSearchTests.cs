using System;
using System.Reflection;
using NUnit.Framework;
using TaffyUGUI.Editor;

namespace TaffyUGUI.Tests
{
    public sealed class TaffyDX10AdvancedSearchTests
    {
        [TestCase("Group", "Formatting", "overflow", true)]
        [TestCase("Group", "Formatting", "clip", true)]
        [TestCase("Group", "Flex", "center", true)]
        [TestCase("Item", "PositionSize", "width", true)]
        [TestCase("Item", "Measurement", "intrinsic", true)]
        [TestCase("Item", "Grid", "measurement", false)]
        public void AdvancedSearchMatchesSectionsAndAliases(string inspector, string section, string query, bool expected)
        {
            Type type = typeof(TaffyLayoutGroupEditor).Assembly.GetType("TaffyUGUI.Editor.TaffyAdvancedInspectorSearch");
            Assert.That(type, Is.Not.Null);
            MethodInfo matches = type.GetMethod("Matches", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(matches, Is.Not.Null);
            Assert.That((bool)matches.Invoke(null, new object[] { inspector, section, query }), Is.EqualTo(expected));
        }

        [Test]
        public void EmptySearchShowsEveryAdvancedSection()
        {
            Type type = typeof(TaffyLayoutGroupEditor).Assembly.GetType("TaffyUGUI.Editor.TaffyAdvancedInspectorSearch");
            MethodInfo matches = type.GetMethod("Matches", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That((bool)matches.Invoke(null, new object[] { "Group", "Grid", string.Empty }), Is.True);
            Assert.That((bool)matches.Invoke(null, new object[] { "Item", "Block", null }), Is.True);
        }
    }
}
