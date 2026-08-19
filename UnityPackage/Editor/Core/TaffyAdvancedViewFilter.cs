using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace TaffyUGUI.Editor
{
    internal enum TaffyAdvancedViewMode
    {
        Essentials = 0,
        Modified = 1,
        All = 2,
    }

    internal static class TaffyAdvancedViewFilter
    {
        private static readonly string[] ModeNames = { "Essentials", "Modified", "All" };
        private static readonly Dictionary<string, TaffyAdvancedViewMode> Modes = new Dictionary<string, TaffyAdvancedViewMode>();

        internal static TaffyAdvancedViewMode Draw(string inspectorKey)
        {
            TaffyAdvancedViewMode current = Get(inspectorKey);
            int next = GUILayout.Toolbar((int)current, ModeNames);
            if (next != (int)current)
                Modes[inspectorKey ?? string.Empty] = (TaffyAdvancedViewMode)next;
            return (TaffyAdvancedViewMode)next;
        }

        internal static TaffyAdvancedViewMode Get(string inspectorKey)
        {
            return Modes.TryGetValue(inspectorKey ?? string.Empty, out TaffyAdvancedViewMode mode)
                ? mode
                : TaffyAdvancedViewMode.All;
        }

        internal static void Set(string inspectorKey, TaffyAdvancedViewMode mode)
        {
            Modes[inspectorKey ?? string.Empty] = mode;
        }

        internal static bool ShouldShow(TaffyInspectorContext context, string inspectorKey, string sectionKey, TaffyAdvancedViewMode mode)
        {
            if (mode == TaffyAdvancedViewMode.All)
                return true;
            if (mode == TaffyAdvancedViewMode.Essentials)
                return IsEssential(context, inspectorKey, sectionKey);
            return IsModified(context?.SerializedObject, inspectorKey, sectionKey);
        }

        internal static bool IsModified(SerializedObject serializedObject, string inspectorKey, string sectionKey)
        {
            if (serializedObject == null)
                return false;

            string key = (inspectorKey ?? string.Empty) + "." + (sectionKey ?? string.Empty);
            switch (key)
            {
                case "Group.Formatting":
                    return EnumChanged(serializedObject, "containerDisplay", TaffyContainerDisplay.Flex)
                        || EnumChanged(serializedObject, "boxSizing", TaffyBoxSizing.BorderBox)
                        || EnumChanged(serializedObject, "writingDirection", TaffyWritingDirection.LeftToRight)
                        || EnumChanged(serializedObject, "overflowX", TaffyOverflow.Visible)
                        || EnumChanged(serializedObject, "overflowY", TaffyOverflow.Visible)
                        || FloatChanged(serializedObject, "scrollbarWidth", 0f)
                        || RectOffsetChanged(serializedObject.FindProperty("m_Padding"))
                        || EdgesChanged(serializedObject.FindProperty("border"), TaffyUnit.Auto)
                        || EnumChanged(serializedObject, "textAlign", TaffyTextAlign.Auto);
                case "Group.Flex":
                    return EnumChanged(serializedObject, "direction", TaffyFlexDirection.Row)
                        || EnumChanged(serializedObject, "wrap", TaffyFlexWrap.NoWrap)
                        || FloatChanged(serializedObject, "horizontalGap", 0f)
                        || FloatChanged(serializedObject, "verticalGap", 0f)
                        || EnumChanged(serializedObject, "justifyContent", TaffyJustify.Start)
                        || EnumChanged(serializedObject, "alignItems", TaffyAlign.Stretch)
                        || EnumChanged(serializedObject, "alignContent", TaffyAlignContent.Auto)
                        || EnumChanged(serializedObject, "justifyItems", TaffyAlign.Auto);
                case "Group.Grid":
                    return EnumChanged(serializedObject, "gridAutoFlow", TaffyGridAutoFlow.Row)
                        || ArrayChanged(serializedObject, "gridRows")
                        || ArrayChanged(serializedObject, "gridColumns")
                        || ArrayChanged(serializedObject, "gridAutoRows")
                        || ArrayChanged(serializedObject, "gridAutoColumns")
                        || ArrayChanged(serializedObject, "gridNamedLines")
                        || ArrayChanged(serializedObject, "gridAreas")
                        || IntChanged(serializedObject, "gridAreaRows", 0)
                        || IntChanged(serializedObject, "gridAreaColumns", 0);
                case "Group.Responsive":
                    return ArrayChanged(serializedObject, "responsiveProfiles")
                        || EnumChanged(serializedObject, "safeAreaMode", TaffySafeAreaMode.Disabled)
                        || EnumChanged(serializedObject, "scrollRectContentMode", TaffyScrollRectContentMode.AutoExpandContent)
                        || EnumChanged(serializedObject, "pixelRounding", TaffyPixelRounding.None)
                        || IntChanged(serializedObject, "maxRebuildRequestsPerFrame", 8);
                case "Item.Display":
                    return EnumChanged(serializedObject, "display", TaffyDisplay.Flex)
                        || EnumChanged(serializedObject, "boxSizing", TaffyBoxSizing.BorderBox)
                        || EnumChanged(serializedObject, "writingDirection", TaffyWritingDirection.LeftToRight)
                        || EnumChanged(serializedObject, "overflowX", TaffyOverflow.Visible)
                        || EnumChanged(serializedObject, "overflowY", TaffyOverflow.Visible)
                        || FloatChanged(serializedObject, "scrollbarWidth", 0f);
                case "Item.PositionSize":
                    return EnumChanged(serializedObject, "position", TaffyPosition.Relative)
                        || EdgesChanged(serializedObject.FindProperty("inset"), TaffyUnit.Auto)
                        || LengthChanged(serializedObject.FindProperty("width"), TaffyUnit.Auto)
                        || LengthChanged(serializedObject.FindProperty("height"), TaffyUnit.Auto)
                        || LengthChanged(serializedObject.FindProperty("minWidth"), TaffyUnit.Auto)
                        || LengthChanged(serializedObject.FindProperty("minHeight"), TaffyUnit.Auto)
                        || LengthChanged(serializedObject.FindProperty("maxWidth"), TaffyUnit.Auto)
                        || LengthChanged(serializedObject.FindProperty("maxHeight"), TaffyUnit.Auto)
                        || FloatChanged(serializedObject, "aspectRatio", 0f);
                case "Item.BoxModel":
                    return EdgesChanged(serializedObject.FindProperty("margin"), TaffyUnit.Points)
                        || EdgesChanged(serializedObject.FindProperty("padding"), TaffyUnit.Points)
                        || EdgesChanged(serializedObject.FindProperty("border"), TaffyUnit.Points);
                case "Item.Flex":
                    return LengthChanged(serializedObject.FindProperty("flexBasis"), TaffyUnit.Auto)
                        || FloatChanged(serializedObject, "flexGrow", 0f)
                        || FloatChanged(serializedObject, "flexShrink", 1f)
                        || EnumChanged(serializedObject, "alignSelf", TaffyAlign.Auto);
                case "Item.Grid":
                    return PlacementChanged(serializedObject.FindProperty("gridRowStart"))
                        || PlacementChanged(serializedObject.FindProperty("gridRowEnd"))
                        || PlacementChanged(serializedObject.FindProperty("gridColumnStart"))
                        || PlacementChanged(serializedObject.FindProperty("gridColumnEnd"))
                        || EnumChanged(serializedObject, "justifySelf", TaffyAlign.Auto);
                case "Item.Block":
                    return EnumChanged(serializedObject, "floatMode", TaffyFloat.None)
                        || EnumChanged(serializedObject, "clearMode", TaffyClear.None)
                        || EnumChanged(serializedObject, "textAlign", TaffyTextAlign.Auto);
                case "Item.Measurement":
                    return EnumChanged(serializedObject, "measurement", TaffyMeasurementMode.Auto)
                        || BoolChanged(serializedObject, "forceReplacedElement", false)
                        || BoolChanged(serializedObject, "itemIsTable", false);
                default:
                    return true;
            }
        }

        private static bool IsEssential(TaffyInspectorContext context, string inspectorKey, string sectionKey)
        {
            if (string.Equals(inspectorKey, "Group", StringComparison.Ordinal))
            {
                if (sectionKey == "Formatting" || sectionKey == "Responsive")
                    return true;
                if (sectionKey == "Flex")
                    return context == null || TaffyInspectorVisibility.GroupShowsFlexEssentials(context);
                if (sectionKey == "Grid")
                    return context == null || TaffyInspectorVisibility.GroupShowsGridEssentials(context);
                return false;
            }

            if (string.Equals(inspectorKey, "Item", StringComparison.Ordinal))
                return sectionKey == "Display" || sectionKey == "PositionSize" || sectionKey == "BoxModel" || sectionKey == "Flex" || sectionKey == "Grid";
            return true;
        }

        private static bool EnumChanged<TEnum>(SerializedObject serializedObject, string propertyName, TEnum expected) where TEnum : struct, IConvertible
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            return property != null && (property.hasMultipleDifferentValues || property.intValue != Convert.ToInt32(expected));
        }

        private static bool FloatChanged(SerializedObject serializedObject, string propertyName, float expected)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            return property != null && (property.hasMultipleDifferentValues || !Mathf.Approximately(property.floatValue, expected));
        }

        private static bool IntChanged(SerializedObject serializedObject, string propertyName, int expected)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            return property != null && (property.hasMultipleDifferentValues || property.intValue != expected);
        }

        private static bool BoolChanged(SerializedObject serializedObject, string propertyName, bool expected)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            return property != null && (property.hasMultipleDifferentValues || property.boolValue != expected);
        }

        private static bool ArrayChanged(SerializedObject serializedObject, string propertyName)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            return property != null && (property.hasMultipleDifferentValues || property.arraySize != 0);
        }

        private static bool LengthChanged(SerializedProperty property, TaffyUnit expectedUnit)
        {
            if (property == null || property.hasMultipleDifferentValues)
                return property != null;
            SerializedProperty unit = property.FindPropertyRelative("unit");
            SerializedProperty value = property.FindPropertyRelative("value");
            if (unit == null || unit.intValue != (int)expectedUnit)
                return true;
            return expectedUnit == TaffyUnit.Points && value != null && !Mathf.Approximately(value.floatValue, 0f);
        }

        private static bool EdgesChanged(SerializedProperty property, TaffyUnit expectedUnit)
        {
            if (property == null)
                return false;
            return LengthChanged(property.FindPropertyRelative("left"), expectedUnit)
                || LengthChanged(property.FindPropertyRelative("right"), expectedUnit)
                || LengthChanged(property.FindPropertyRelative("top"), expectedUnit)
                || LengthChanged(property.FindPropertyRelative("bottom"), expectedUnit);
        }

        private static bool PlacementChanged(SerializedProperty property)
        {
            SerializedProperty kind = property?.FindPropertyRelative("kind");
            return kind != null && (property.hasMultipleDifferentValues || kind.intValue != (int)TaffyGridPlacementKind.Auto);
        }

        private static bool RectOffsetChanged(SerializedProperty property)
        {
            if (property == null)
                return false;
            return IntRelativeChanged(property, "m_Left") || IntRelativeChanged(property, "m_Right")
                || IntRelativeChanged(property, "m_Top") || IntRelativeChanged(property, "m_Bottom");
        }

        private static bool IntRelativeChanged(SerializedProperty property, string childName)
        {
            SerializedProperty child = property.FindPropertyRelative(childName);
            return child != null && (child.hasMultipleDifferentValues || child.intValue != 0);
        }
    }
}
