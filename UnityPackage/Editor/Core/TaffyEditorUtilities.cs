using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace TaffyUGUI.Editor
{
    internal static class TaffySerializedPropertyUtility
    {
        internal static void DrawProperties(SerializedObject serializedObject, IReadOnlyList<string> propertyNames)
        {
            if (serializedObject == null || propertyNames == null)
                return;

            for (int i = 0; i < propertyNames.Count; i++)
            {
                SerializedProperty property = serializedObject.FindProperty(propertyNames[i]);
                if (property != null)
                    EditorGUILayout.PropertyField(property, TaffyEditorContent.ForProperty(property.name, property.displayName), true);
            }
        }

        internal static void DrawProperty(SerializedObject serializedObject, string propertyName, GUIContent label)
        {
            SerializedProperty property = serializedObject?.FindProperty(propertyName);
            if (property != null)
                EditorGUILayout.PropertyField(property, label ?? TaffyEditorContent.ForProperty(propertyName, property.displayName), true);
        }

        internal static SerializedProperty FindRequired(SerializedObject serializedObject, string propertyName)
        {
            SerializedProperty property = serializedObject?.FindProperty(propertyName);
            if (property == null)
                throw new InvalidOperationException($"TaffyUGUI Editor could not find serialized property '{propertyName}'.");
            return property;
        }
    }

    internal static class TaffyDefaultValueUtility
    {
        internal static bool FloatEquals(SerializedProperty property, float expected)
        {
            return property != null && property.propertyType == SerializedPropertyType.Float && Mathf.Approximately(property.floatValue, expected);
        }

        internal static bool IntegerEquals(SerializedProperty property, int expected)
        {
            return property != null && property.propertyType == SerializedPropertyType.Integer && property.intValue == expected;
        }

        internal static bool BoolEquals(SerializedProperty property, bool expected)
        {
            return property != null && property.propertyType == SerializedPropertyType.Boolean && property.boolValue == expected;
        }

        internal static bool EnumEquals<TEnum>(SerializedProperty property, TEnum expected) where TEnum : struct, IConvertible
        {
            return property != null && property.propertyType == SerializedPropertyType.Enum && property.intValue == Convert.ToInt32(expected);
        }

        internal static bool IsAutoLength(SerializedProperty property)
        {
            SerializedProperty unit = property?.FindPropertyRelative("unit");
            return unit != null && unit.propertyType == SerializedPropertyType.Enum && unit.intValue == (int)TaffyUnit.Auto;
        }

        internal static bool IsAutoGridPlacement(SerializedProperty property)
        {
            SerializedProperty kind = property?.FindPropertyRelative("kind");
            return kind != null && kind.propertyType == SerializedPropertyType.Enum && kind.intValue == (int)TaffyGridPlacementKind.Auto;
        }
    }

    internal static class TaffyInspectorVisibility
    {
        internal static bool GroupHasMixedDisplay(TaffyInspectorContext context)
        {
            SerializedProperty display = context?.SerializedObject.FindProperty("containerDisplay");
            return display != null && display.hasMultipleDifferentValues;
        }

        internal static bool GroupShowsFlexEssentials(TaffyInspectorContext context)
        {
            return context != null && !GroupHasMixedDisplay(context) && context.ResolvedAuthoringDisplay == TaffyContainerDisplay.Flex;
        }

        internal static bool GroupShowsGridEssentials(TaffyInspectorContext context)
        {
            return context != null && !GroupHasMixedDisplay(context) && context.ResolvedAuthoringDisplay == TaffyContainerDisplay.Grid;
        }

        internal static bool HasInactiveGroupFlexSettings(TaffyInspectorContext context)
        {
            if (context == null || GroupShowsFlexEssentials(context))
                return false;

            SerializedObject serializedObject = context.SerializedObject;
            return !TaffyDefaultValueUtility.EnumEquals(serializedObject.FindProperty("direction"), TaffyFlexDirection.Row)
                || !TaffyDefaultValueUtility.EnumEquals(serializedObject.FindProperty("wrap"), TaffyFlexWrap.NoWrap)
                || !TaffyDefaultValueUtility.FloatEquals(serializedObject.FindProperty("horizontalGap"), 0f)
                || !TaffyDefaultValueUtility.FloatEquals(serializedObject.FindProperty("verticalGap"), 0f)
                || !TaffyDefaultValueUtility.EnumEquals(serializedObject.FindProperty("justifyContent"), TaffyJustify.Start)
                || !TaffyDefaultValueUtility.EnumEquals(serializedObject.FindProperty("alignItems"), TaffyAlign.Stretch)
                || !TaffyDefaultValueUtility.EnumEquals(serializedObject.FindProperty("alignContent"), TaffyAlignContent.Auto)
                || !TaffyDefaultValueUtility.EnumEquals(serializedObject.FindProperty("justifyItems"), TaffyAlign.Auto);
        }

        internal static bool HasInactiveGroupGridSettings(TaffyInspectorContext context)
        {
            if (context == null || GroupShowsGridEssentials(context))
                return false;

            SerializedObject serializedObject = context.SerializedObject;
            return !TaffyDefaultValueUtility.EnumEquals(serializedObject.FindProperty("gridAutoFlow"), TaffyGridAutoFlow.Row)
                || serializedObject.FindProperty("gridRows").arraySize != 0
                || serializedObject.FindProperty("gridColumns").arraySize != 0
                || serializedObject.FindProperty("gridAutoRows").arraySize != 0
                || serializedObject.FindProperty("gridAutoColumns").arraySize != 0
                || serializedObject.FindProperty("gridNamedLines").arraySize != 0
                || serializedObject.FindProperty("gridAreas").arraySize != 0
                || !TaffyDefaultValueUtility.IntegerEquals(serializedObject.FindProperty("gridAreaRows"), 0)
                || !TaffyDefaultValueUtility.IntegerEquals(serializedObject.FindProperty("gridAreaColumns"), 0);
        }

        internal static bool ParentIsFlex(TaffyInspectorContext context)
        {
            return context != null && context.ParentDisplay == TaffyContainerDisplay.Flex;
        }

        internal static bool ParentIsGrid(TaffyInspectorContext context)
        {
            return context != null && context.ParentDisplay == TaffyContainerDisplay.Grid;
        }

        internal static bool ParentIsBlockLike(TaffyInspectorContext context)
        {
            if (context == null || !context.ParentDisplay.HasValue)
                return false;
            return context.ParentDisplay.Value == TaffyContainerDisplay.Block || context.ParentDisplay.Value == TaffyContainerDisplay.FlowRoot;
        }

        internal static string ParentSummary(TaffyInspectorContext context)
        {
            if (context == null || !context.ParentGroup)
                return "No TaffyLayoutGroup parent";

            TaffyLayoutGroup parent = context.ParentGroup;
            switch (parent.containerDisplay)
            {
                case TaffyContainerDisplay.Flex:
                    return parent.name + " • Flex • " + FlexDirectionSummary(parent.direction);
                case TaffyContainerDisplay.Grid:
                    return parent.name + " • Grid • " + parent.gridAutoFlow;
                case TaffyContainerDisplay.Block:
                    return parent.name + " • Block";
                case TaffyContainerDisplay.FlowRoot:
                    return parent.name + " • FlowRoot";
                default:
                    return parent.name + " • " + parent.containerDisplay;
            }
        }

        internal static string FlexDirectionSummary(TaffyFlexDirection direction)
        {
            switch (direction)
            {
                case TaffyFlexDirection.Row: return "Horizontal →";
                case TaffyFlexDirection.RowReverse: return "Horizontal ←";
                case TaffyFlexDirection.Column: return "Vertical ↓";
                case TaffyFlexDirection.ColumnReverse: return "Vertical ↑";
                default: return direction.ToString();
            }
        }

        internal static string FlexGrowHelp(TaffyInspectorContext context)
        {
            if (context == null || !context.ParentGroup || context.ParentGroup.containerDisplay != TaffyContainerDisplay.Flex)
                return "Grow is active when the parent uses Flex layout.";

            TaffyFlexDirection direction = context.ParentGroup.direction;
            bool horizontal = direction == TaffyFlexDirection.Row || direction == TaffyFlexDirection.RowReverse;
            return horizontal
                ? "Parent is a horizontal Flex layout. Grow distributes remaining horizontal space between flexible siblings."
                : "Parent is a vertical Flex layout. Grow distributes remaining vertical space between flexible siblings.";
        }

        internal static bool HasInactiveFlexOverrides(TaffyInspectorContext context)
        {
            if (context == null || ParentIsFlex(context))
                return false;
            SerializedObject serializedObject = context.SerializedObject;
            return !TaffyDefaultValueUtility.IsAutoLength(serializedObject.FindProperty("flexBasis"))
                || !TaffyDefaultValueUtility.FloatEquals(serializedObject.FindProperty("flexGrow"), 0f)
                || !TaffyDefaultValueUtility.FloatEquals(serializedObject.FindProperty("flexShrink"), 1f)
                || !TaffyDefaultValueUtility.EnumEquals(serializedObject.FindProperty("alignSelf"), TaffyAlign.Auto);
        }

        internal static bool HasInactiveGridOverrides(TaffyInspectorContext context)
        {
            if (context == null || ParentIsGrid(context))
                return false;
            SerializedObject serializedObject = context.SerializedObject;
            return !TaffyDefaultValueUtility.IsAutoGridPlacement(serializedObject.FindProperty("gridRowStart"))
                || !TaffyDefaultValueUtility.IsAutoGridPlacement(serializedObject.FindProperty("gridRowEnd"))
                || !TaffyDefaultValueUtility.IsAutoGridPlacement(serializedObject.FindProperty("gridColumnStart"))
                || !TaffyDefaultValueUtility.IsAutoGridPlacement(serializedObject.FindProperty("gridColumnEnd"))
                || !TaffyDefaultValueUtility.EnumEquals(serializedObject.FindProperty("justifySelf"), TaffyAlign.Auto);
        }

        internal static bool HasInactiveBlockOverrides(TaffyInspectorContext context)
        {
            if (context == null || ParentIsBlockLike(context))
                return false;
            SerializedObject serializedObject = context.SerializedObject;
            return !TaffyDefaultValueUtility.EnumEquals(serializedObject.FindProperty("floatMode"), TaffyFloat.None)
                || !TaffyDefaultValueUtility.EnumEquals(serializedObject.FindProperty("clearMode"), TaffyClear.None)
                || !TaffyDefaultValueUtility.EnumEquals(serializedObject.FindProperty("textAlign"), TaffyTextAlign.Auto);
        }
    }

    internal static class TaffyEditorGUI
    {
        private static readonly string[] ModeNames = { "Simple", "Advanced" };
        private static readonly string[] DensityNames = { "Comfortable", "Compact" };

        internal static void DrawScript(SerializedObject serializedObject)
        {
            SerializedProperty script = serializedObject?.FindProperty("m_Script");
            if (script == null)
                return;

            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.PropertyField(script);
        }

        internal static void DrawInspectorMode()
        {
            EditorGUILayout.Space(2f);
            EditorGUILayout.LabelField(TaffyEditorContent.InspectorMode, EditorStyles.miniBoldLabel);
            int current = (int)TaffyEditorPreferences.InspectorMode;
            int next = GUILayout.Toolbar(current, ModeNames);
            if (next != current)
            {
                TaffyEditorPreferences.InspectorMode = (TaffyInspectorMode)next;
                GUI.FocusControl(null);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PrefixLabel("Density");
                int density = (int)TaffyEditorPreferences.InspectorDensity;
                int nextDensity = GUILayout.Toolbar(density, DensityNames);
                if (nextDensity != density)
                    TaffyEditorPreferences.InspectorDensity = (TaffyInspectorDensity)nextDensity;
            }
        }

        internal static void DrawSimpleModeHint()
        {
            EditorGUILayout.HelpBox(TaffyEditorContent.SimpleModeHint, MessageType.Info);
        }

        internal static void DrawSectionLabel(GUIContent title)
        {
            EditorGUILayout.Space(TaffyEditorPreferences.InspectorDensity == TaffyInspectorDensity.Compact ? 1f : 4f);
            EditorGUILayout.LabelField(title ?? GUIContent.none, EditorStyles.boldLabel);
        }

        internal static string WithSummary(string title, string summary)
        {
            return string.IsNullOrEmpty(summary) ? title : $"{title}    {summary}";
        }

        internal static void DrawValidation(TaffyLayoutGroup group)
        {
            if (!group)
                return;

            if (!group.ValidateResponsiveProfiles(out string responsiveError))
                EditorGUILayout.HelpBox(responsiveError, MessageType.Error);
            if (group.containerDisplay == TaffyContainerDisplay.Grid && !group.ValidateGridAuthoring(out string gridError))
                EditorGUILayout.HelpBox(gridError, MessageType.Error);

            string[] warnings = group.GetIntegrationWarnings();
            for (int i = 0; i < warnings.Length; i++)
                EditorGUILayout.HelpBox(warnings[i], MessageType.Warning);
        }
    }
}
