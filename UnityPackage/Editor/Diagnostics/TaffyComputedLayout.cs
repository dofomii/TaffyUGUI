using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace TaffyUGUI.Editor
{
    internal readonly struct TaffyComputedLayoutSnapshot
    {
        internal TaffyComputedLayoutSnapshot(
            bool available,
            Vector2 position,
            Vector2 size,
            string responsiveProfile,
            string parentContext,
            TaffyContainerDisplay effectiveDisplay,
            TaffyFlexDirection effectiveDirection,
            bool measurementAvailable,
            TaffyMeasurementData measurement,
            float measurementWidth,
            string gridDiagnostics)
        {
            Available = available;
            Position = position;
            Size = size;
            ResponsiveProfile = responsiveProfile;
            ParentContext = parentContext;
            EffectiveDisplay = effectiveDisplay;
            EffectiveDirection = effectiveDirection;
            MeasurementAvailable = measurementAvailable;
            Measurement = measurement;
            MeasurementWidth = measurementWidth;
            GridDiagnostics = gridDiagnostics;
        }

        internal bool Available { get; }
        internal Vector2 Position { get; }
        internal Vector2 Size { get; }
        internal string ResponsiveProfile { get; }
        internal string ParentContext { get; }
        internal TaffyContainerDisplay EffectiveDisplay { get; }
        internal TaffyFlexDirection EffectiveDirection { get; }
        internal bool MeasurementAvailable { get; }
        internal TaffyMeasurementData Measurement { get; }
        internal float MeasurementWidth { get; }
        internal string GridDiagnostics { get; }

        internal static TaffyComputedLayoutSnapshot From(Component component)
        {
            if (!component || !(component.transform is RectTransform rect))
                return default;

            TaffyLayoutGroup group = component as TaffyLayoutGroup;
            TaffyLayoutItem item = component as TaffyLayoutItem;
            TaffyLayoutGroup parent = item ? FindParentGroup(item.transform) : null;
            TaffyLayoutGroup contextGroup = group ? group : parent;

            ResolveEffectiveContext(contextGroup, out string profile, out TaffyContainerDisplay display, out TaffyFlexDirection direction);
            string parentContext = group
                ? "Root / container"
                : parent
                    ? parent.name + " • " + display + (display == TaffyContainerDisplay.Flex ? " • " + direction : string.Empty)
                    : "No Taffy parent";

            bool measurementAvailable = false;
            TaffyMeasurementData measurement = default;
            float measurementWidth = 0f;
            if (item && item.measurement != TaffyMeasurementMode.Disabled)
            {
                measurementWidth = parent && parent.transform is RectTransform parentRect
                    ? Mathf.Max(0f, parentRect.rect.width)
                    : Mathf.Max(0f, rect.rect.width);
                measurementAvailable = TryResolveMeasurement(rect, measurementWidth, out measurement);
            }

            string gridDiagnostics = string.Empty;
            if (contextGroup && display == TaffyContainerDisplay.Grid)
            {
                if (!contextGroup.ValidateGridAuthoring(out string validationError))
                    gridDiagnostics = validationError;
                else if (!string.IsNullOrEmpty(contextGroup.GridValidationError))
                    gridDiagnostics = contextGroup.GridValidationError;
                else
                    gridDiagnostics = "Grid authoring valid";
            }

            Rect localRect = rect.rect;
            return new TaffyComputedLayoutSnapshot(
                true,
                rect.anchoredPosition,
                new Vector2(Mathf.Max(0f, localRect.width), Mathf.Max(0f, localRect.height)),
                string.IsNullOrEmpty(profile) ? "Base settings" : profile,
                parentContext,
                display,
                direction,
                measurementAvailable,
                measurement,
                measurementWidth,
                gridDiagnostics);
        }

        private static TaffyLayoutGroup FindParentGroup(Transform transform)
        {
            Transform current = transform ? transform.parent : null;
            while (current)
            {
                TaffyLayoutGroup group = current.GetComponent<TaffyLayoutGroup>();
                if (group)
                    return group;
                current = current.parent;
            }
            return null;
        }

        private static void ResolveEffectiveContext(
            TaffyLayoutGroup group,
            out string profileName,
            out TaffyContainerDisplay display,
            out TaffyFlexDirection direction)
        {
            profileName = string.Empty;
            display = TaffyContainerDisplay.Flex;
            direction = TaffyFlexDirection.Row;
            if (!group)
                return;

            profileName = group.ActiveResponsiveProfileName;
            display = group.containerDisplay;
            direction = group.direction;
            if (string.IsNullOrEmpty(profileName) || group.responsiveProfiles == null)
                return;

            for (int i = 0; i < group.responsiveProfiles.Count; i++)
            {
                TaffyResponsiveProfile profile = group.responsiveProfiles[i];
                if (profile == null || !string.Equals(profile.name, profileName, StringComparison.Ordinal))
                    continue;
                if (profile.overrideContainerDisplay)
                    display = profile.containerDisplay;
                if (profile.overrideFlexDirection)
                    direction = profile.direction;
                return;
            }
        }

        private static bool TryResolveMeasurement(RectTransform rect, float availableWidth, out TaffyMeasurementData measurement)
        {
            measurement = default;
            Type resolverType = typeof(TaffyLayoutItem).Assembly.GetType("TaffyUGUI.TaffyMeasurementResolver");
            MethodInfo method = resolverType?.GetMethod("TryResolve", BindingFlags.Static | BindingFlags.NonPublic);
            if (method == null)
                return false;

            object[] args = { rect, availableWidth, default(TaffyMeasurementData), 0 };
            bool resolved;
            try
            {
                resolved = (bool)method.Invoke(null, args);
            }
            catch (TargetInvocationException)
            {
                return false;
            }

            if (!resolved)
                return false;
            measurement = (TaffyMeasurementData)args[2];
            return true;
        }
    }

    internal static class TaffyComputedLayoutGUI
    {
        internal static void Draw(TaffyInspectorContext context)
        {
            if (context == null || context.IsMultiEditing)
                return;

            Component component = context.Group ? (Component)context.Group : context.Item;
            TaffyComputedLayoutSnapshot snapshot = TaffyComputedLayoutSnapshot.From(component);
            if (!snapshot.Available)
                return;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Computed Layout", EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.Vector2Field("Position", snapshot.Position);
                EditorGUILayout.Vector2Field("Size", snapshot.Size);
                EditorGUILayout.TextField("Responsive Profile", snapshot.ResponsiveProfile);
                EditorGUILayout.TextField("Parent Context", snapshot.ParentContext);
                EditorGUILayout.EnumPopup("Effective Display", snapshot.EffectiveDisplay);
                if (snapshot.EffectiveDisplay == TaffyContainerDisplay.Flex)
                    EditorGUILayout.EnumPopup("Effective Direction", snapshot.EffectiveDirection);

                if (snapshot.MeasurementAvailable)
                {
                    EditorGUILayout.Vector2Field("Content Min", snapshot.Measurement.minContent);
                    EditorGUILayout.Vector2Field("Content Preferred", snapshot.Measurement.preferred);
                    EditorGUILayout.Vector2Field("Content Max", snapshot.Measurement.maxContent);
                    EditorGUILayout.FloatField("Measured At Width", snapshot.MeasurementWidth);
                }

                if (!string.IsNullOrEmpty(snapshot.GridDiagnostics))
                    EditorGUILayout.TextField("Grid", snapshot.GridDiagnostics);
            }
            TaffyExplainLayoutGUI.Draw(context, component, snapshot);
            EditorGUILayout.LabelField("Read-only current layout state; no layout settings are changed.", EditorStyles.miniLabel);
        }
    }

    internal static class TaffyExplainLayoutGUI
    {
        private const string SessionPrefix = "TaffyUGUI.Editor.ExplainLayout.";

        internal static void Draw(TaffyInspectorContext context, Component component, TaffyComputedLayoutSnapshot snapshot)
        {
            if (context == null || context.IsMultiEditing || !component || !snapshot.Available)
                return;

            string key = SessionPrefix + component.GetInstanceID();
            bool expanded = SessionState.GetBool(key, false);
            if (GUILayout.Button(expanded ? "Hide Layout Explanation" : "Explain Layout"))
            {
                expanded = !expanded;
                SessionState.SetBool(key, expanded);
            }

            if (expanded)
                EditorGUILayout.HelpBox(TaffyLayoutExplanation.Build(component, snapshot), MessageType.Info);
        }
    }
}
