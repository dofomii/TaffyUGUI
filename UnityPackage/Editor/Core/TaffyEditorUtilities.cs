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
                    EditorGUILayout.PropertyField(property, true);
            }
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
    }

    internal static class TaffyEditorGUI
    {
        internal static void DrawScript(SerializedObject serializedObject)
        {
            SerializedProperty script = serializedObject?.FindProperty("m_Script");
            if (script == null)
                return;

            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.PropertyField(script);
        }

        internal static void DrawSectionLabel(GUIContent title)
        {
            EditorGUILayout.Space(4f);
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
