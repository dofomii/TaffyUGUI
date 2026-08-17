using UnityEditor;
using UnityEngine;

namespace TaffyUGUI.Editor
{
    internal static class TaffyDrawerUtility
    {
        internal static float Line => EditorGUIUtility.singleLineHeight;
        internal static float Gap => EditorGUIUtility.standardVerticalSpacing;

        internal static float Height(SerializedProperty property, bool includeChildren = true)
        {
            return property == null ? Line : EditorGUI.GetPropertyHeight(property, GUIContent.none, includeChildren);
        }

        internal static Rect TakeLine(ref Rect cursor)
        {
            Rect result = new Rect(cursor.x, cursor.y, cursor.width, Line);
            cursor.y += Line + Gap;
            return result;
        }

        internal static Rect Take(ref Rect cursor, float height)
        {
            Rect result = new Rect(cursor.x, cursor.y, cursor.width, height);
            cursor.y += height + Gap;
            return result;
        }

        internal static void Draw(ref Rect cursor, SerializedProperty property, GUIContent label = null, bool includeChildren = true)
        {
            if (property == null)
                return;
            float height = EditorGUI.GetPropertyHeight(property, label ?? GUIContent.none, includeChildren);
            EditorGUI.PropertyField(Take(ref cursor, height), property, label ?? GUIContent.none, includeChildren);
        }

        internal static float StackHeight(params float[] heights)
        {
            if (heights == null || heights.Length == 0)
                return Line;
            float total = 0f;
            int count = 0;
            for (int i = 0; i < heights.Length; i++)
            {
                if (heights[i] <= 0f)
                    continue;
                total += heights[i];
                count++;
            }
            if (count > 1)
                total += Gap * (count - 1);
            return Mathf.Max(Line, total);
        }
    }

    [CustomPropertyDrawer(typeof(TaffyLength))]
    public sealed class TaffyLengthDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            SerializedProperty unit = property.FindPropertyRelative("unit");
            if (unit == null)
                return TaffyDrawerUtility.Line;

            TaffyUnit kind = (TaffyUnit)unit.enumValueIndex;
            if (kind == TaffyUnit.Auto)
                return TaffyDrawerUtility.Line;
            SerializedProperty payload = kind == TaffyUnit.Calc
                ? property.FindPropertyRelative("calc")
                : property.FindPropertyRelative("value");
            return TaffyDrawerUtility.StackHeight(TaffyDrawerUtility.Line, TaffyDrawerUtility.Height(payload));
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            Rect cursor = position;
            SerializedProperty unit = property.FindPropertyRelative("unit");
            EditorGUI.PropertyField(TaffyDrawerUtility.TakeLine(ref cursor), unit, label);
            if (unit != null)
            {
                TaffyUnit kind = (TaffyUnit)unit.enumValueIndex;
                if (kind == TaffyUnit.Calc)
                    TaffyDrawerUtility.Draw(ref cursor, property.FindPropertyRelative("calc"), new GUIContent("Expression"));
                else if (kind != TaffyUnit.Auto)
                    TaffyDrawerUtility.Draw(ref cursor, property.FindPropertyRelative("value"), new GUIContent(kind == TaffyUnit.Percent ? "Fraction" : "Points"));
            }
            EditorGUI.EndProperty();
        }
    }

    [CustomPropertyDrawer(typeof(TaffyEdges))]
    public sealed class TaffyEdgesDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return TaffyDrawerUtility.StackHeight(
                TaffyDrawerUtility.Line,
                TaffyDrawerUtility.Height(property.FindPropertyRelative("left")),
                TaffyDrawerUtility.Height(property.FindPropertyRelative("right")),
                TaffyDrawerUtility.Height(property.FindPropertyRelative("top")),
                TaffyDrawerUtility.Height(property.FindPropertyRelative("bottom")));
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            Rect cursor = position;
            EditorGUI.LabelField(TaffyDrawerUtility.TakeLine(ref cursor), label, EditorStyles.boldLabel);
            TaffyDrawerUtility.Draw(ref cursor, property.FindPropertyRelative("left"), new GUIContent("Left"));
            TaffyDrawerUtility.Draw(ref cursor, property.FindPropertyRelative("right"), new GUIContent("Right"));
            TaffyDrawerUtility.Draw(ref cursor, property.FindPropertyRelative("top"), new GUIContent("Top"));
            TaffyDrawerUtility.Draw(ref cursor, property.FindPropertyRelative("bottom"), new GUIContent("Bottom"));
            EditorGUI.EndProperty();
        }
    }

    [CustomPropertyDrawer(typeof(TaffyPixelInsets))]
    public sealed class TaffyPixelInsetsDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) =>
            TaffyDrawerUtility.Line * 3f + TaffyDrawerUtility.Gap * 2f;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            Rect cursor = position;
            EditorGUI.LabelField(TaffyDrawerUtility.TakeLine(ref cursor), label, EditorStyles.boldLabel);
            float half = (position.width - 8f) * 0.5f;
            Rect row1 = TaffyDrawerUtility.TakeLine(ref cursor);
            Rect row2 = TaffyDrawerUtility.TakeLine(ref cursor);
            EditorGUI.PropertyField(new Rect(row1.x, row1.y, half, row1.height), property.FindPropertyRelative("left"), new GUIContent("Left"));
            EditorGUI.PropertyField(new Rect(row1.x + half + 8f, row1.y, half, row1.height), property.FindPropertyRelative("right"), new GUIContent("Right"));
            EditorGUI.PropertyField(new Rect(row2.x, row2.y, half, row2.height), property.FindPropertyRelative("top"), new GUIContent("Top"));
            EditorGUI.PropertyField(new Rect(row2.x + half + 8f, row2.y, half, row2.height), property.FindPropertyRelative("bottom"), new GUIContent("Bottom"));
            EditorGUI.EndProperty();
        }
    }

    [CustomPropertyDrawer(typeof(TaffyCalcExpression))]
    public sealed class TaffyCalcExpressionDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            SerializedProperty operation = property.FindPropertyRelative("operation");
            if (operation == null)
                return TaffyDrawerUtility.Line;

            TaffyCalcOperation kind = (TaffyCalcOperation)operation.enumValueIndex;
            if (kind is TaffyCalcOperation.Length or TaffyCalcOperation.Percent)
                return TaffyDrawerUtility.StackHeight(TaffyDrawerUtility.Line, TaffyDrawerUtility.Height(property.FindPropertyRelative("value")));

            float operandsHeight = TaffyDrawerUtility.Height(property.FindPropertyRelative("operands"), true);
            if (kind == TaffyCalcOperation.Scale)
                return TaffyDrawerUtility.StackHeight(TaffyDrawerUtility.Line, TaffyDrawerUtility.Height(property.FindPropertyRelative("value")), operandsHeight);
            return TaffyDrawerUtility.StackHeight(TaffyDrawerUtility.Line, operandsHeight);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            Rect cursor = position;
            SerializedProperty operation = property.FindPropertyRelative("operation");
            EditorGUI.PropertyField(TaffyDrawerUtility.TakeLine(ref cursor), operation, label);
            if (operation == null)
            {
                EditorGUI.EndProperty();
                return;
            }

            TaffyCalcOperation kind = (TaffyCalcOperation)operation.enumValueIndex;
            if (kind is TaffyCalcOperation.Length or TaffyCalcOperation.Percent)
            {
                TaffyDrawerUtility.Draw(ref cursor, property.FindPropertyRelative("value"), new GUIContent(kind == TaffyCalcOperation.Percent ? "Fraction" : "Points"));
            }
            else
            {
                if (kind == TaffyCalcOperation.Scale)
                    TaffyDrawerUtility.Draw(ref cursor, property.FindPropertyRelative("value"), new GUIContent("Scale"));
                TaffyDrawerUtility.Draw(ref cursor, property.FindPropertyRelative("operands"), OperandLabel(kind), true);
            }
            EditorGUI.EndProperty();
        }

        private static GUIContent OperandLabel(TaffyCalcOperation operation)
        {
            return operation switch
            {
                TaffyCalcOperation.Add => new GUIContent("Operands (2)"),
                TaffyCalcOperation.Subtract => new GUIContent("Operands (2)"),
                TaffyCalcOperation.Scale => new GUIContent("Operand (1)"),
                TaffyCalcOperation.Clamp => new GUIContent("Min / Preferred / Max (3)"),
                TaffyCalcOperation.Min => new GUIContent("Operands (1+)"),
                TaffyCalcOperation.Max => new GUIContent("Operands (1+)"),
                _ => new GUIContent("Operands"),
            };
        }
    }

    [CustomPropertyDrawer(typeof(TaffyGridTrackBreadth))]
    public sealed class TaffyGridTrackBreadthDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            SerializedProperty kindProperty = property.FindPropertyRelative("kind");
            if (kindProperty == null)
                return TaffyDrawerUtility.Line;
            TaffyGridTrackBreadthKind kind = (TaffyGridTrackBreadthKind)kindProperty.enumValueIndex;
            if (kind is TaffyGridTrackBreadthKind.Auto or TaffyGridTrackBreadthKind.MinContent or TaffyGridTrackBreadthKind.MaxContent)
                return TaffyDrawerUtility.Line;
            SerializedProperty payload = kind == TaffyGridTrackBreadthKind.Calc
                ? property.FindPropertyRelative("calc")
                : property.FindPropertyRelative("value");
            return TaffyDrawerUtility.StackHeight(TaffyDrawerUtility.Line, TaffyDrawerUtility.Height(payload));
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            Rect cursor = position;
            SerializedProperty kindProperty = property.FindPropertyRelative("kind");
            EditorGUI.PropertyField(TaffyDrawerUtility.TakeLine(ref cursor), kindProperty, label);
            if (kindProperty != null)
            {
                TaffyGridTrackBreadthKind kind = (TaffyGridTrackBreadthKind)kindProperty.enumValueIndex;
                if (kind == TaffyGridTrackBreadthKind.Calc)
                    TaffyDrawerUtility.Draw(ref cursor, property.FindPropertyRelative("calc"), new GUIContent("Expression"));
                else if (kind is not (TaffyGridTrackBreadthKind.Auto or TaffyGridTrackBreadthKind.MinContent or TaffyGridTrackBreadthKind.MaxContent))
                    TaffyDrawerUtility.Draw(ref cursor, property.FindPropertyRelative("value"), new GUIContent("Value"));
            }
            EditorGUI.EndProperty();
        }
    }

    [CustomPropertyDrawer(typeof(TaffyGridTrack))]
    public sealed class TaffyGridTrackDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            SerializedProperty kindProperty = property.FindPropertyRelative("kind");
            if (kindProperty == null)
                return TaffyDrawerUtility.Line;
            TaffyGridTrackKind kind = (TaffyGridTrackKind)kindProperty.enumValueIndex;
            switch (kind)
            {
                case TaffyGridTrackKind.Auto:
                case TaffyGridTrackKind.MinContent:
                case TaffyGridTrackKind.MaxContent:
                    return TaffyDrawerUtility.Line;
                case TaffyGridTrackKind.Points:
                case TaffyGridTrackKind.Percent:
                case TaffyGridTrackKind.Fraction:
                    return TaffyDrawerUtility.StackHeight(TaffyDrawerUtility.Line, TaffyDrawerUtility.Height(property.FindPropertyRelative("value")));
                case TaffyGridTrackKind.Calc:
                    return TaffyDrawerUtility.StackHeight(TaffyDrawerUtility.Line, TaffyDrawerUtility.Height(property.FindPropertyRelative("calc")));
                case TaffyGridTrackKind.MinMax:
                    return TaffyDrawerUtility.StackHeight(
                        TaffyDrawerUtility.Line,
                        TaffyDrawerUtility.Height(property.FindPropertyRelative("min")),
                        TaffyDrawerUtility.Height(property.FindPropertyRelative("max")));
                case TaffyGridTrackKind.Repeat:
                    SerializedProperty repeatMode = property.FindPropertyRelative("repeatMode");
                    TaffyGridRepeatMode mode = repeatMode == null ? TaffyGridRepeatMode.Count : (TaffyGridRepeatMode)repeatMode.enumValueIndex;
                    return mode == TaffyGridRepeatMode.Count
                        ? TaffyDrawerUtility.StackHeight(
                            TaffyDrawerUtility.Line,
                            TaffyDrawerUtility.Height(repeatMode),
                            TaffyDrawerUtility.Height(property.FindPropertyRelative("repeatCount")),
                            TaffyDrawerUtility.Height(property.FindPropertyRelative("repeatTracks"), true))
                        : TaffyDrawerUtility.StackHeight(
                            TaffyDrawerUtility.Line,
                            TaffyDrawerUtility.Height(repeatMode),
                            TaffyDrawerUtility.Height(property.FindPropertyRelative("repeatTracks"), true));
                default:
                    return TaffyDrawerUtility.Line;
            }
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            Rect cursor = position;
            SerializedProperty kindProperty = property.FindPropertyRelative("kind");
            EditorGUI.PropertyField(TaffyDrawerUtility.TakeLine(ref cursor), kindProperty, label);
            if (kindProperty == null)
            {
                EditorGUI.EndProperty();
                return;
            }

            TaffyGridTrackKind kind = (TaffyGridTrackKind)kindProperty.enumValueIndex;
            switch (kind)
            {
                case TaffyGridTrackKind.Points:
                case TaffyGridTrackKind.Percent:
                case TaffyGridTrackKind.Fraction:
                    TaffyDrawerUtility.Draw(ref cursor, property.FindPropertyRelative("value"), new GUIContent("Value"));
                    break;
                case TaffyGridTrackKind.Calc:
                    TaffyDrawerUtility.Draw(ref cursor, property.FindPropertyRelative("calc"), new GUIContent("Expression"));
                    break;
                case TaffyGridTrackKind.MinMax:
                    TaffyDrawerUtility.Draw(ref cursor, property.FindPropertyRelative("min"), new GUIContent("Minimum"));
                    TaffyDrawerUtility.Draw(ref cursor, property.FindPropertyRelative("max"), new GUIContent("Maximum"));
                    break;
                case TaffyGridTrackKind.Repeat:
                    SerializedProperty repeatMode = property.FindPropertyRelative("repeatMode");
                    TaffyDrawerUtility.Draw(ref cursor, repeatMode, new GUIContent("Repeat"));
                    if (repeatMode == null || (TaffyGridRepeatMode)repeatMode.enumValueIndex == TaffyGridRepeatMode.Count)
                        TaffyDrawerUtility.Draw(ref cursor, property.FindPropertyRelative("repeatCount"), new GUIContent("Count"));
                    TaffyDrawerUtility.Draw(ref cursor, property.FindPropertyRelative("repeatTracks"), new GUIContent("Tracks"), true);
                    break;
            }
            EditorGUI.EndProperty();
        }
    }

    [CustomPropertyDrawer(typeof(TaffyGridPlacement))]
    public sealed class TaffyGridPlacementDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            SerializedProperty kindProperty = property.FindPropertyRelative("kind");
            if (kindProperty == null)
                return TaffyDrawerUtility.Line;
            TaffyGridPlacementKind kind = (TaffyGridPlacementKind)kindProperty.enumValueIndex;
            return kind switch
            {
                TaffyGridPlacementKind.Auto => TaffyDrawerUtility.Line,
                TaffyGridPlacementKind.NamedLine => TaffyDrawerUtility.Line * 3f + TaffyDrawerUtility.Gap * 2f,
                TaffyGridPlacementKind.NamedSpan => TaffyDrawerUtility.Line * 3f + TaffyDrawerUtility.Gap * 2f,
                _ => TaffyDrawerUtility.Line * 2f + TaffyDrawerUtility.Gap,
            };
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            Rect cursor = position;
            SerializedProperty kindProperty = property.FindPropertyRelative("kind");
            EditorGUI.PropertyField(TaffyDrawerUtility.TakeLine(ref cursor), kindProperty, label);
            if (kindProperty != null)
            {
                switch ((TaffyGridPlacementKind)kindProperty.enumValueIndex)
                {
                    case TaffyGridPlacementKind.Line:
                        EditorGUI.PropertyField(TaffyDrawerUtility.TakeLine(ref cursor), property.FindPropertyRelative("line"), new GUIContent("Line"));
                        break;
                    case TaffyGridPlacementKind.Span:
                        EditorGUI.PropertyField(TaffyDrawerUtility.TakeLine(ref cursor), property.FindPropertyRelative("span"), new GUIContent("Span"));
                        break;
                    case TaffyGridPlacementKind.NamedLine:
                        EditorGUI.PropertyField(TaffyDrawerUtility.TakeLine(ref cursor), property.FindPropertyRelative("name"), new GUIContent("Name"));
                        EditorGUI.PropertyField(TaffyDrawerUtility.TakeLine(ref cursor), property.FindPropertyRelative("occurrence"), new GUIContent("Occurrence"));
                        break;
                    case TaffyGridPlacementKind.NamedSpan:
                        EditorGUI.PropertyField(TaffyDrawerUtility.TakeLine(ref cursor), property.FindPropertyRelative("name"), new GUIContent("Name"));
                        EditorGUI.PropertyField(TaffyDrawerUtility.TakeLine(ref cursor), property.FindPropertyRelative("span"), new GUIContent("Span"));
                        break;
                }
            }
            EditorGUI.EndProperty();
        }
    }

    [CustomPropertyDrawer(typeof(TaffyGridNamedLine))]
    public sealed class TaffyGridNamedLineDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) =>
            TaffyDrawerUtility.Line * 3f + TaffyDrawerUtility.Gap * 2f;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            Rect cursor = position;
            EditorGUI.PropertyField(TaffyDrawerUtility.TakeLine(ref cursor), property.FindPropertyRelative("name"), label);
            EditorGUI.PropertyField(TaffyDrawerUtility.TakeLine(ref cursor), property.FindPropertyRelative("axis"), new GUIContent("Axis"));
            EditorGUI.PropertyField(TaffyDrawerUtility.TakeLine(ref cursor), property.FindPropertyRelative("lineIndex"), new GUIContent("Line Index"));
            EditorGUI.EndProperty();
        }
    }

    [CustomPropertyDrawer(typeof(TaffyGridArea))]
    public sealed class TaffyGridAreaDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) =>
            TaffyDrawerUtility.Line * 3f + TaffyDrawerUtility.Gap * 2f;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            Rect cursor = position;
            EditorGUI.PropertyField(TaffyDrawerUtility.TakeLine(ref cursor), property.FindPropertyRelative("name"), label);
            Rect row1 = TaffyDrawerUtility.TakeLine(ref cursor);
            Rect row2 = TaffyDrawerUtility.TakeLine(ref cursor);
            float half = (position.width - 8f) * 0.5f;
            EditorGUI.PropertyField(new Rect(row1.x, row1.y, half, row1.height), property.FindPropertyRelative("rowStart"), new GUIContent("Row Start"));
            EditorGUI.PropertyField(new Rect(row1.x + half + 8f, row1.y, half, row1.height), property.FindPropertyRelative("rowEnd"), new GUIContent("Row End"));
            EditorGUI.PropertyField(new Rect(row2.x, row2.y, half, row2.height), property.FindPropertyRelative("columnStart"), new GUIContent("Column Start"));
            EditorGUI.PropertyField(new Rect(row2.x + half + 8f, row2.y, half, row2.height), property.FindPropertyRelative("columnEnd"), new GUIContent("Column End"));
            EditorGUI.EndProperty();
        }
    }
}
