using System;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace TaffyUGUI.Editor
{
    internal enum TaffyLengthIntent
    {
        Auto = 0,
        Fixed = 1,
        Percent = 2,
        FillParent = 3,
        Calculated = 4,
    }

    internal enum TaffyEdgesAuthoringMode
    {
        Uniform = 0,
        Axis = 1,
        Individual = 2,
    }

    internal static class TaffyLengthAuthoringUtility
    {
        internal static readonly GUIContent[] IntentLabels =
        {
            new GUIContent("Auto / Content", "Let layout and intrinsic content measurement determine the size."),
            new GUIContent("Fixed", "Use an explicit Unity UI unit (Taffy point/pixel before Canvas scaling)."),
            new GUIContent("Percent", "Use a percentage of the relevant containing size."),
            new GUIContent("Fill Parent", "Shortcut for 100% of the relevant containing size."),
            new GUIContent("Calculated", "Use the existing Calc expression system for advanced computed sizing."),
        };

        internal static TaffyLengthIntent GetIntent(SerializedProperty property)
        {
            SerializedProperty unit = property?.FindPropertyRelative("unit");
            SerializedProperty value = property?.FindPropertyRelative("value");
            if (unit == null)
                return TaffyLengthIntent.Auto;

            switch ((TaffyUnit)unit.intValue)
            {
                case TaffyUnit.Points:
                    return TaffyLengthIntent.Fixed;
                case TaffyUnit.Percent:
                    return value != null && Mathf.Approximately(value.floatValue, 1f)
                        ? TaffyLengthIntent.FillParent
                        : TaffyLengthIntent.Percent;
                case TaffyUnit.Calc:
                    return TaffyLengthIntent.Calculated;
                default:
                    return TaffyLengthIntent.Auto;
            }
        }

        internal static void SetIntent(SerializedProperty property, TaffyLengthIntent intent)
        {
            SerializedProperty unit = property?.FindPropertyRelative("unit");
            SerializedProperty value = property?.FindPropertyRelative("value");
            SerializedProperty calc = property?.FindPropertyRelative("calc");
            if (unit == null)
                return;

            TaffyUnit previous = (TaffyUnit)unit.intValue;
            switch (intent)
            {
                case TaffyLengthIntent.Fixed:
                    unit.intValue = (int)TaffyUnit.Points;
                    if (previous == TaffyUnit.Auto || previous == TaffyUnit.Calc)
                        value.floatValue = 0f;
                    break;
                case TaffyLengthIntent.Percent:
                    unit.intValue = (int)TaffyUnit.Percent;
                    if (previous != TaffyUnit.Percent)
                        value.floatValue = 0.5f;
                    break;
                case TaffyLengthIntent.FillParent:
                    unit.intValue = (int)TaffyUnit.Percent;
                    value.floatValue = 1f;
                    break;
                case TaffyLengthIntent.Calculated:
                    unit.intValue = (int)TaffyUnit.Calc;
                    if (calc != null && calc.managedReferenceValue == null)
                        calc.managedReferenceValue = TaffyCalcExpression.Length(0f);
                    break;
                default:
                    unit.intValue = (int)TaffyUnit.Auto;
                    break;
            }
        }

        internal static float GetDisplayValue(SerializedProperty property)
        {
            SerializedProperty value = property?.FindPropertyRelative("value");
            if (value == null)
                return 0f;
            TaffyLengthIntent intent = GetIntent(property);
            return intent == TaffyLengthIntent.Percent || intent == TaffyLengthIntent.FillParent
                ? value.floatValue * 100f
                : value.floatValue;
        }

        internal static void SetDisplayValue(SerializedProperty property, float displayValue)
        {
            SerializedProperty value = property?.FindPropertyRelative("value");
            if (value == null)
                return;

            TaffyLengthIntent intent = GetIntent(property);
            if (intent == TaffyLengthIntent.Percent || intent == TaffyLengthIntent.FillParent)
                value.floatValue = displayValue / 100f;
            else
                value.floatValue = displayValue;
        }

        internal static string Summary(SerializedProperty property)
        {
            switch (GetIntent(property))
            {
                case TaffyLengthIntent.Fixed:
                    return Format(GetDisplayValue(property)) + " px";
                case TaffyLengthIntent.Percent:
                    return Format(GetDisplayValue(property)) + "%";
                case TaffyLengthIntent.FillParent:
                    return "100%";
                case TaffyLengthIntent.Calculated:
                    return "Calc";
                default:
                    return "Auto";
            }
        }

        private static string Format(float value)
        {
            return value.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
        }
    }

    internal static class TaffyEdgesAuthoringUtility
    {
        private const string SessionPrefix = "TaffyUGUI.Editor.EdgesMode.";

        internal static TaffyEdgesAuthoringMode GetMode(SerializedProperty property)
        {
            int stored = SessionState.GetInt(SessionKey(property), -1);
            if (stored >= 0 && stored <= (int)TaffyEdgesAuthoringMode.Individual)
                return (TaffyEdgesAuthoringMode)stored;
            return DetectMode(property);
        }

        internal static void SetMode(SerializedProperty property, TaffyEdgesAuthoringMode mode)
        {
            SessionState.SetInt(SessionKey(property), (int)mode);
            ApplyMode(property, mode);
        }

        internal static TaffyEdgesAuthoringMode DetectMode(SerializedProperty property)
        {
            SerializedProperty left = property?.FindPropertyRelative("left");
            SerializedProperty right = property?.FindPropertyRelative("right");
            SerializedProperty top = property?.FindPropertyRelative("top");
            SerializedProperty bottom = property?.FindPropertyRelative("bottom");
            if (left == null || right == null || top == null || bottom == null)
                return TaffyEdgesAuthoringMode.Individual;

            if (LengthEquals(left, right) && LengthEquals(left, top) && LengthEquals(left, bottom))
                return TaffyEdgesAuthoringMode.Uniform;
            if (LengthEquals(left, right) && LengthEquals(top, bottom))
                return TaffyEdgesAuthoringMode.Axis;
            return TaffyEdgesAuthoringMode.Individual;
        }

        internal static void ApplyMode(SerializedProperty property, TaffyEdgesAuthoringMode mode)
        {
            SerializedProperty left = property?.FindPropertyRelative("left");
            SerializedProperty right = property?.FindPropertyRelative("right");
            SerializedProperty top = property?.FindPropertyRelative("top");
            SerializedProperty bottom = property?.FindPropertyRelative("bottom");
            if (left == null || right == null || top == null || bottom == null)
                return;

            if (mode == TaffyEdgesAuthoringMode.Uniform)
            {
                CopyLength(left, right);
                CopyLength(left, top);
                CopyLength(left, bottom);
            }
            else if (mode == TaffyEdgesAuthoringMode.Axis)
            {
                CopyLength(left, right);
                CopyLength(top, bottom);
            }
        }

        internal static void SynchronizeLinkedSides(SerializedProperty property, TaffyEdgesAuthoringMode mode, bool horizontalChanged, bool verticalChanged)
        {
            SerializedProperty left = property?.FindPropertyRelative("left");
            SerializedProperty right = property?.FindPropertyRelative("right");
            SerializedProperty top = property?.FindPropertyRelative("top");
            SerializedProperty bottom = property?.FindPropertyRelative("bottom");
            if (left == null || right == null || top == null || bottom == null)
                return;

            if (mode == TaffyEdgesAuthoringMode.Uniform && (horizontalChanged || verticalChanged))
            {
                CopyLength(left, right);
                CopyLength(left, top);
                CopyLength(left, bottom);
            }
            else if (mode == TaffyEdgesAuthoringMode.Axis)
            {
                if (horizontalChanged)
                    CopyLength(left, right);
                if (verticalChanged)
                    CopyLength(top, bottom);
            }
        }

        internal static bool LengthEquals(SerializedProperty first, SerializedProperty second)
        {
            if (first == null || second == null)
                return false;
            SerializedProperty firstUnit = first.FindPropertyRelative("unit");
            SerializedProperty secondUnit = second.FindPropertyRelative("unit");
            SerializedProperty firstValue = first.FindPropertyRelative("value");
            SerializedProperty secondValue = second.FindPropertyRelative("value");
            if (firstUnit == null || secondUnit == null || firstUnit.intValue != secondUnit.intValue)
                return false;
            if (firstValue != null && secondValue != null && !Mathf.Approximately(firstValue.floatValue, secondValue.floatValue))
                return false;

            return SerializedCalcSignature(first.FindPropertyRelative("calc")) == SerializedCalcSignature(second.FindPropertyRelative("calc"));
        }

        internal static void CopyLength(SerializedProperty source, SerializedProperty target)
        {
            if (source == null || target == null)
                return;
            target.FindPropertyRelative("unit").intValue = source.FindPropertyRelative("unit").intValue;
            target.FindPropertyRelative("value").floatValue = source.FindPropertyRelative("value").floatValue;
            CopyCalc(source.FindPropertyRelative("calc"), target.FindPropertyRelative("calc"));
        }

        internal static string Summary(SerializedProperty property)
        {
            SerializedProperty left = property?.FindPropertyRelative("left");
            SerializedProperty right = property?.FindPropertyRelative("right");
            SerializedProperty top = property?.FindPropertyRelative("top");
            SerializedProperty bottom = property?.FindPropertyRelative("bottom");
            if (left == null || right == null || top == null || bottom == null)
                return string.Empty;

            string l = TaffyLengthAuthoringUtility.Summary(left);
            string r = TaffyLengthAuthoringUtility.Summary(right);
            string t = TaffyLengthAuthoringUtility.Summary(top);
            string b = TaffyLengthAuthoringUtility.Summary(bottom);
            if (l == r && l == t && l == b)
                return l;
            if (l == r && t == b)
                return "H " + l + " • V " + t;
            return "L " + l + " • R " + r + " • T " + t + " • B " + b;
        }

        private static string SessionKey(SerializedProperty property)
        {
            int targetId = property?.serializedObject?.targetObject ? property.serializedObject.targetObject.GetInstanceID() : 0;
            return SessionPrefix + targetId + "." + (property?.propertyPath ?? "unknown");
        }

        private static void CopyCalc(SerializedProperty source, SerializedProperty target)
        {
            if (source == null || target == null)
                return;

            SerializedProperty sourceOperation = source.FindPropertyRelative("operation");
            SerializedProperty targetOperation = target.FindPropertyRelative("operation");
            SerializedProperty sourceValue = source.FindPropertyRelative("value");
            SerializedProperty targetValue = target.FindPropertyRelative("value");
            if (sourceOperation != null && targetOperation != null)
                targetOperation.intValue = sourceOperation.intValue;
            if (sourceValue != null && targetValue != null)
                targetValue.floatValue = sourceValue.floatValue;

            SerializedProperty sourceOperands = source.FindPropertyRelative("operands");
            SerializedProperty targetOperands = target.FindPropertyRelative("operands");
            if (sourceOperands == null || targetOperands == null)
                return;

            targetOperands.arraySize = sourceOperands.arraySize;
            for (int i = 0; i < sourceOperands.arraySize; i++)
            {
                SerializedProperty sourceElement = sourceOperands.GetArrayElementAtIndex(i);
                SerializedProperty targetElement = targetOperands.GetArrayElementAtIndex(i);
                if (targetElement != null)
                    targetElement.managedReferenceValue = CloneCalc(sourceElement?.managedReferenceValue as TaffyCalcExpression);
            }
        }

        private static string SerializedCalcSignature(SerializedProperty property)
        {
            if (property == null)
                return string.Empty;
            SerializedProperty operation = property.FindPropertyRelative("operation");
            SerializedProperty value = property.FindPropertyRelative("value");
            SerializedProperty operands = property.FindPropertyRelative("operands");
            var builder = new StringBuilder();
            builder.Append(operation?.intValue ?? 0)
                .Append(':')
                .Append((value?.floatValue ?? 0f).ToString("R", System.Globalization.CultureInfo.InvariantCulture))
                .Append('[');
            if (operands != null)
            {
                for (int i = 0; i < operands.arraySize; i++)
                {
                    if (i != 0)
                        builder.Append(',');
                    builder.Append(CalcSignature(operands.GetArrayElementAtIndex(i).managedReferenceValue as TaffyCalcExpression));
                }
            }
            return builder.Append(']').ToString();
        }

        private static TaffyCalcExpression CloneCalc(TaffyCalcExpression source)
        {
            if (source == null)
                return null;
            var clone = new TaffyCalcExpression
            {
                operation = source.operation,
                value = source.value,
                operands = new System.Collections.Generic.List<TaffyCalcExpression>(),
            };
            if (source.operands != null)
            {
                for (int i = 0; i < source.operands.Count; i++)
                    clone.operands.Add(CloneCalc(source.operands[i]));
            }
            return clone;
        }

        private static string CalcSignature(TaffyCalcExpression expression)
        {
            if (expression == null)
                return string.Empty;
            var builder = new StringBuilder();
            AppendCalcSignature(builder, expression);
            return builder.ToString();
        }

        private static void AppendCalcSignature(StringBuilder builder, TaffyCalcExpression expression)
        {
            if (expression == null)
            {
                builder.Append("null");
                return;
            }
            builder.Append((int)expression.operation).Append(':').Append(expression.value.ToString("R", System.Globalization.CultureInfo.InvariantCulture)).Append('[');
            if (expression.operands != null)
            {
                for (int i = 0; i < expression.operands.Count; i++)
                {
                    if (i != 0)
                        builder.Append(',');
                    AppendCalcSignature(builder, expression.operands[i]);
                }
            }
            builder.Append(']');
        }
    }

    internal static class TaffyInspectorSummaryUtility
    {
        internal static string SizeSummary(SerializedObject serializedObject)
        {
            if (serializedObject == null)
                return string.Empty;
            return TaffyLengthAuthoringUtility.Summary(serializedObject.FindProperty("width")) + " × " +
                   TaffyLengthAuthoringUtility.Summary(serializedObject.FindProperty("height"));
        }

        internal static string FlexSummary(SerializedObject serializedObject)
        {
            if (serializedObject == null)
                return string.Empty;
            SerializedProperty grow = serializedObject.FindProperty("flexGrow");
            SerializedProperty shrink = serializedObject.FindProperty("flexShrink");
            if (grow == null || shrink == null)
                return string.Empty;
            return "Grow " + grow.floatValue.ToString("0.##") + " • Shrink " + shrink.floatValue.ToString("0.##");
        }

        internal static string EdgesSummary(SerializedObject serializedObject, string propertyName)
        {
            return TaffyEdgesAuthoringUtility.Summary(serializedObject?.FindProperty(propertyName));
        }

        internal static string RectOffsetSummary(SerializedProperty property)
        {
            if (property == null)
                return string.Empty;
            SerializedProperty left = property.FindPropertyRelative("m_Left");
            SerializedProperty right = property.FindPropertyRelative("m_Right");
            SerializedProperty top = property.FindPropertyRelative("m_Top");
            SerializedProperty bottom = property.FindPropertyRelative("m_Bottom");
            if (left == null || right == null || top == null || bottom == null)
                return string.Empty;
            if (left.intValue == right.intValue && left.intValue == top.intValue && left.intValue == bottom.intValue)
                return left.intValue.ToString();
            if (left.intValue == right.intValue && top.intValue == bottom.intValue)
                return "H " + left.intValue + " • V " + top.intValue;
            return "L " + left.intValue + " • R " + right.intValue + " • T " + top.intValue + " • B " + bottom.intValue;
        }

        internal static string GridPlacementSummary(SerializedObject serializedObject)
        {
            if (serializedObject == null)
                return string.Empty;
            string rowStart = Placement(serializedObject.FindProperty("gridRowStart"));
            string rowEnd = Placement(serializedObject.FindProperty("gridRowEnd"));
            string columnStart = Placement(serializedObject.FindProperty("gridColumnStart"));
            string columnEnd = Placement(serializedObject.FindProperty("gridColumnEnd"));
            if (rowStart == "Auto" && rowEnd == "Auto" && columnStart == "Auto" && columnEnd == "Auto")
                return "Auto";
            return "Rows " + rowStart + "→" + rowEnd + " • Cols " + columnStart + "→" + columnEnd;
        }

        private static string Placement(SerializedProperty property)
        {
            SerializedProperty kind = property?.FindPropertyRelative("kind");
            if (kind == null)
                return "Auto";
            switch ((TaffyGridPlacementKind)kind.intValue)
            {
                case TaffyGridPlacementKind.Line:
                    return "L" + property.FindPropertyRelative("line").intValue;
                case TaffyGridPlacementKind.Span:
                    return "Span " + property.FindPropertyRelative("span").intValue;
                case TaffyGridPlacementKind.NamedLine:
                    return property.FindPropertyRelative("name").stringValue;
                case TaffyGridPlacementKind.NamedSpan:
                    return property.FindPropertyRelative("name").stringValue + "×" + property.FindPropertyRelative("span").intValue;
                default:
                    return "Auto";
            }
        }
    }
}
