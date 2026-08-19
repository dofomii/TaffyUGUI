using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TaffyUGUI.Editor;
using UnityEditor;
using UnityEngine;

namespace TaffyUGUI.Tests
{
    public sealed class TaffyDX8SceneAuthoringTests
    {
        private readonly List<GameObject> _owned = new List<GameObject>();
        private static Assembly EditorAssembly => typeof(TaffyLayoutGroupEditor).Assembly;

        [TearDown]
        public void TearDown()
        {
            Type overlayPreferences = EditorAssembly.GetType("TaffyUGUI.Editor.TaffySceneOverlayPreferences");
            Invoke(overlayPreferences, "ResetForTests");
            SetStaticProperty(EditorAssembly.GetType("TaffyUGUI.Editor.TaffySceneHandles"), "PaddingHandlesEnabled", false);
            SetStaticProperty(EditorAssembly.GetType("TaffyUGUI.Editor.TaffySceneHandles"), "GapHandlesEnabled", false);

            for (int i = _owned.Count - 1; i >= 0; i--)
            {
                if (_owned[i])
                    UnityEngine.Object.DestroyImmediate(_owned[i]);
            }
            _owned.Clear();
        }

        [Test]
        public void OverlayPreferencesHaveSafeDefaultsAndRoundTripWithoutSceneMutation()
        {
            Type preferences = EditorAssembly.GetType("TaffyUGUI.Editor.TaffySceneOverlayPreferences");
            Invoke(preferences, "ResetForTests");

            Assert.That((bool)GetStaticProperty(preferences, "ContainerBounds"), Is.True);
            Assert.That((bool)GetStaticProperty(preferences, "ChildBounds"), Is.True);
            Assert.That((bool)GetStaticProperty(preferences, "PaddingBounds"), Is.False);
            Assert.That((bool)GetStaticProperty(preferences, "ItemMargins"), Is.False);
            Assert.That((bool)GetStaticProperty(preferences, "FlexAxes"), Is.False);
            Assert.That((bool)GetStaticProperty(preferences, "GapMarkers"), Is.False);
            Assert.That((bool)GetStaticProperty(preferences, "GridTracks"), Is.True);
            Assert.That((bool)GetStaticProperty(preferences, "ResponsiveProfileLabel"), Is.True);
            Assert.That((bool)GetStaticProperty(preferences, "ComputedSizeLabels"), Is.False);

            TaffyLayoutGroup group = CreateGroup("PreferenceSafety");
            group.horizontalGap = 12f;
            string before = JsonUtility.ToJson(group);
            SetStaticProperty(preferences, "PaddingBounds", true);
            SetStaticProperty(preferences, "FlexAxes", true);
            SetStaticProperty(preferences, "ComputedSizeLabels", true);
            Assert.That(JsonUtility.ToJson(group), Is.EqualTo(before));
        }

        [Test]
        public void ResponsivePreviewPresetsAndProfileResolutionMatchRuntimeSelectionRules()
        {
            TaffyLayoutGroup group = CreateGroup("PreviewGroup");
            group.responsiveProfiles = new List<TaffyResponsiveProfile>
            {
                new TaffyResponsiveProfile { name = "Mobile", priority = 10, maxWidth = 600f },
                new TaffyResponsiveProfile { name = "Tablet", priority = 20, minWidth = 601f, maxWidth = 1100f },
                new TaffyResponsiveProfile { name = "Desktop", priority = 30, minWidth = 1101f },
                new TaffyResponsiveProfile { name = "TabletPriority", priority = 25, minWidth = 700f, maxWidth = 1050f },
            };

            Type preview = EditorAssembly.GetType("TaffyUGUI.Editor.TaffyResponsivePreview");
            Type presetType = EditorAssembly.GetType("TaffyUGUI.Editor.TaffyResponsivePreviewPreset");

            AssertPreviewPreset(preview, presetType, "Desktop", new Vector2(1440f, 900f));
            AssertPreviewPreset(preview, presetType, "Tablet", new Vector2(1024f, 768f));
            AssertPreviewPreset(preview, presetType, "Mobile", new Vector2(390f, 844f));

            Assert.That((string)Invoke(preview, "ResolveProfileName", group, new Vector2(390f, 844f)), Is.EqualTo("Mobile"));
            Assert.That((string)Invoke(preview, "ResolveProfileName", group, new Vector2(800f, 700f)), Is.EqualTo("TabletPriority"));
            Assert.That((string)Invoke(preview, "ResolveProfileName", group, new Vector2(1440f, 900f)), Is.EqualTo("Desktop"));
            Assert.That((string)Invoke(preview, "ResolveProfileName", group, new Vector2(650f, 500f)), Is.EqualTo("Tablet"));
        }

        [Test]
        public void SceneInspectionUtilitiesDoNotMutateLayoutData()
        {
            TaffyLayoutGroup group = CreateGroup("ReadOnlySceneInspection");
            group.padding = new RectOffset(10, 20, 30, 40);
            group.horizontalGap = 18f;
            group.verticalGap = 24f;
            group.responsiveProfiles = new List<TaffyResponsiveProfile>
            {
                new TaffyResponsiveProfile { name = "Small", maxWidth = 800f, priority = 10 },
            };

            string before = JsonUtility.ToJson(group);
            Type overlay = EditorAssembly.GetType("TaffyUGUI.Editor.TaffySceneOverlayDrawing");
            Type preview = EditorAssembly.GetType("TaffyUGUI.Editor.TaffyResponsivePreview");

            Invoke(overlay, "GetPaddingRect", new Rect(0f, 0f, 500f, 300f), group.padding);
            InvokeWithOutVectors(overlay, "GetGapMarkerSegments", new Rect(0f, 0f, 500f, 300f), group.horizontalGap, group.verticalGap);
            Invoke(preview, "ResolveProfileName", group, new Vector2(390f, 844f));

            Assert.That(JsonUtility.ToJson(group), Is.EqualTo(before), "Scene inspection must remain read-only unless a handle drag is accepted.");
        }

        [Test]
        public void AcceptedPaddingAndGapHandleChangesAreUndoSafe()
        {
            TaffyLayoutGroup group = CreateGroup("HandleUndo");
            group.padding = new RectOffset(10, 20, 30, 40);
            group.horizontalGap = 12f;
            Type handles = EditorAssembly.GetType("TaffyUGUI.Editor.TaffySceneHandles");

            Assert.That((bool)Invoke(handles, "ApplyPaddingDelta", group, "m_Left", 5f), Is.True);
            Assert.That(group.padding.left, Is.EqualTo(15));
            Undo.PerformUndo();
            Assert.That(group.padding.left, Is.EqualTo(10));

            Assert.That((bool)Invoke(handles, "ApplyGapDelta", group, "horizontalGap", 8f), Is.True);
            Assert.That(group.horizontalGap, Is.EqualTo(20f).Within(0.001f));
            Undo.PerformUndo();
            Assert.That(group.horizontalGap, Is.EqualTo(12f).Within(0.001f));
        }

        [Test]
        public void HandlePreferencesAreOptInAndDoNotChangeLayoutByThemselves()
        {
            Type handles = EditorAssembly.GetType("TaffyUGUI.Editor.TaffySceneHandles");
            SetStaticProperty(handles, "PaddingHandlesEnabled", false);
            SetStaticProperty(handles, "GapHandlesEnabled", false);
            Assert.That((bool)GetStaticProperty(handles, "PaddingHandlesEnabled"), Is.False);
            Assert.That((bool)GetStaticProperty(handles, "GapHandlesEnabled"), Is.False);

            TaffyLayoutGroup group = CreateGroup("HandlePreferenceSafety");
            group.padding = new RectOffset(4, 5, 6, 7);
            group.horizontalGap = 9f;
            string before = JsonUtility.ToJson(group);

            SetStaticProperty(handles, "PaddingHandlesEnabled", true);
            SetStaticProperty(handles, "GapHandlesEnabled", true);
            Assert.That(JsonUtility.ToJson(group), Is.EqualTo(before));
        }

        private void AssertPreviewPreset(Type preview, Type presetType, string presetName, Vector2 expected)
        {
            object preset = Enum.Parse(presetType, presetName);
            SetStaticProperty(preview, "Preset", preset);
            MethodInfo method = FindMethod(preview, "TryGetPreviewSize", 1);
            object[] args = { Vector2.zero };
            bool result = (bool)method.Invoke(null, args);
            Assert.That(result, Is.True);
            Vector2 actual = (Vector2)args[0];
            Assert.That(actual.x, Is.EqualTo(expected.x));
            Assert.That(actual.y, Is.EqualTo(expected.y));
        }

        private TaffyLayoutGroup CreateGroup(string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.SetActive(false);
            _owned.Add(go);
            return go.AddComponent<TaffyLayoutGroup>();
        }

        private static object Invoke(Type type, string methodName, params object[] args)
        {
            MethodInfo method = FindMethod(type, methodName, args.Length);
            return method.Invoke(null, args);
        }

        private static void InvokeWithOutVectors(Type type, string methodName, Rect rect, float horizontalGap, float verticalGap)
        {
            MethodInfo method = FindMethod(type, methodName, 7);
            object[] args = { rect, horizontalGap, verticalGap, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero };
            method.Invoke(null, args);
        }

        private static MethodInfo FindMethod(Type type, string methodName, int parameterCount)
        {
            Assert.That(type, Is.Not.Null, methodName + " type");
            MethodInfo[] methods = type.GetMethods(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            for (int i = 0; i < methods.Length; i++)
            {
                if (methods[i].Name == methodName && methods[i].GetParameters().Length == parameterCount)
                    return methods[i];
            }
            Assert.Fail("Missing method: " + type.FullName + "." + methodName);
            return null;
        }

        private static object GetStaticProperty(Type type, string propertyName)
        {
            Assert.That(type, Is.Not.Null, propertyName + " type");
            PropertyInfo property = type.GetProperty(propertyName, BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.That(property, Is.Not.Null, propertyName);
            return property.GetValue(null);
        }

        private static void SetStaticProperty(Type type, string propertyName, object value)
        {
            Assert.That(type, Is.Not.Null, propertyName + " type");
            PropertyInfo property = type.GetProperty(propertyName, BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.That(property, Is.Not.Null, propertyName);
            property.SetValue(null, value);
        }
    }
}
