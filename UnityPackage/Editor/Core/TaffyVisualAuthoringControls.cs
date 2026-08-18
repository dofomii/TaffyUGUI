using UnityEditor;
using UnityEngine;

namespace TaffyUGUI.Editor
{
    internal static class TaffyVisualAuthoringControls
    {
        internal static readonly int[] DirectionValues =
        {
            (int)TaffyFlexDirection.Row,
            (int)TaffyFlexDirection.Column,
            (int)TaffyFlexDirection.RowReverse,
            (int)TaffyFlexDirection.ColumnReverse,
        };

        internal static readonly int[] JustifyValues =
        {
            (int)TaffyJustify.Start,
            (int)TaffyJustify.Center,
            (int)TaffyJustify.End,
            (int)TaffyJustify.SpaceBetween,
        };

        internal static readonly int[] AlignValues =
        {
            (int)TaffyAlign.Start,
            (int)TaffyAlign.Center,
            (int)TaffyAlign.End,
            (int)TaffyAlign.Stretch,
        };

        private static readonly string[] DirectionLabels = { "→ Row", "↓ Column", "← Reverse", "↑ Reverse" };
        private static readonly string[] JustifyLabels = { "Start", "Center", "End", "Space" };
        private static readonly string[] AlignLabels = { "Start", "Center", "End", "Stretch" };

        internal static void DrawDirection(SerializedProperty property, GUIContent label)
        {
            DrawToolbar(property, label, DirectionLabels, DirectionValues, "A less-common Flex direction is active. Choose a common visual option here or use Advanced mode to keep/edit it.");
        }

        internal static void DrawJustify(SerializedProperty property, GUIContent label)
        {
            DrawToolbar(property, label, JustifyLabels, JustifyValues, "An advanced alignment variant is active. Choose a common visual option here or edit the full value in Advanced mode.");
        }

        internal static void DrawAlign(SerializedProperty property, GUIContent label)
        {
            DrawToolbar(property, label, AlignLabels, AlignValues, "An advanced alignment variant is active. Choose a common visual option here or edit the full value in Advanced mode.");
        }

        internal static int ToolbarIndexForValue(int value, int[] values)
        {
            if (values == null)
                return -1;
            for (int i = 0; i < values.Length; i++)
            {
                if (values[i] == value)
                    return i;
            }
            return -1;
        }

        internal static int ValueForToolbarIndex(int index, int[] values)
        {
            if (values == null || index < 0 || index >= values.Length)
                return int.MinValue;
            return values[index];
        }

        private static void DrawToolbar(SerializedProperty property, GUIContent label, string[] labels, int[] values, string advancedMessage)
        {
            if (property == null)
                return;

            EditorGUILayout.LabelField(label, EditorStyles.miniBoldLabel);
            int currentIndex = property.hasMultipleDifferentValues ? -1 : ToolbarIndexForValue(property.intValue, values);
            EditorGUI.showMixedValue = property.hasMultipleDifferentValues || currentIndex < 0;
            EditorGUI.BeginChangeCheck();
            int nextIndex = GUILayout.Toolbar(Mathf.Max(0, currentIndex), labels);
            bool changed = EditorGUI.EndChangeCheck();
            EditorGUI.showMixedValue = false;

            if (changed && nextIndex >= 0 && nextIndex < values.Length)
                property.intValue = values[nextIndex];

            if (!property.hasMultipleDifferentValues && currentIndex < 0)
                EditorGUILayout.HelpBox(advancedMessage, MessageType.Info);
        }
    }
}
