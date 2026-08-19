using System;
using System.Reflection;
using NUnit.Framework;
using TaffyUGUI.Editor;
using UnityEditor;
using UnityEngine;

namespace TaffyUGUI.Tests
{
    public sealed class TaffyDX10AdvancedFilterTests
    {
        [Test]
        public void ModifiedFilterDetectsItemPositionSizeChangesAgainstSerializedDefaults()
        {
            GameObject go = new GameObject("Item", typeof(RectTransform), typeof(TaffyLayoutItem));
            try
            {
                TaffyLayoutItem item = go.GetComponent<TaffyLayoutItem>();
                var serialized = new SerializedObject(item);
                Assert.That(IsModified(serialized, "Item", "PositionSize"), Is.False);

                item.width = TaffyLength.Points(180f);
                serialized.Update();
                Assert.That(IsModified(serialized, "Item", "PositionSize"), Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void ModifiedFilterDetectsGroupResponsiveAndGridChanges()
        {
            GameObject go = new GameObject("Group", typeof(RectTransform), typeof(TaffyLayoutGroup));
            try
            {
                TaffyLayoutGroup group = go.GetComponent<TaffyLayoutGroup>();
                var serialized = new SerializedObject(group);
                Assert.That(IsModified(serialized, "Group", "Responsive"), Is.False);
                Assert.That(IsModified(serialized, "Group", "Grid"), Is.False);

                group.responsiveProfiles.Add(new TaffyResponsiveProfile { name = "phone", maxWidth = 400f });
                group.gridColumns.Add(TaffyGridTrack.Fraction(1f));
                serialized.Update();
                Assert.That(IsModified(serialized, "Group", "Responsive"), Is.True);
                Assert.That(IsModified(serialized, "Group", "Grid"), Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void AdvancedViewDefaultsToAllAndCanSelectModified()
        {
            Assembly assembly = typeof(TaffyLayoutGroupEditor).Assembly;
            Type modeType = assembly.GetType("TaffyUGUI.Editor.TaffyAdvancedViewMode");
            Type filterType = assembly.GetType("TaffyUGUI.Editor.TaffyAdvancedViewFilter");
            Assert.That(modeType, Is.Not.Null);
            Assert.That(filterType, Is.Not.Null);

            MethodInfo get = filterType.GetMethod("Get", BindingFlags.Static | BindingFlags.NonPublic);
            MethodInfo set = filterType.GetMethod("Set", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(get.Invoke(null, new object[] { "TestFresh" }).ToString(), Is.EqualTo("All"));

            object modified = Enum.Parse(modeType, "Modified");
            set.Invoke(null, new[] { (object)"TestFresh", modified });
            Assert.That(get.Invoke(null, new object[] { "TestFresh" }).ToString(), Is.EqualTo("Modified"));
        }

        private static bool IsModified(SerializedObject serializedObject, string inspectorKey, string sectionKey)
        {
            Type type = typeof(TaffyLayoutGroupEditor).Assembly.GetType("TaffyUGUI.Editor.TaffyAdvancedViewFilter");
            Assert.That(type, Is.Not.Null);
            MethodInfo method = type.GetMethod("IsModified", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            return (bool)method.Invoke(null, new object[] { serializedObject, inspectorKey, sectionKey });
        }
    }
}
