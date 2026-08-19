using UnityEditor;
using UnityEngine;

namespace TaffyUGUI.Editor
{
    internal static class TaffySceneHandles
    {
        private const string PaddingHandlesKey = "TaffyUGUI.SceneHandles.Padding";
        private const string GapHandlesKey = "TaffyUGUI.SceneHandles.Gaps";

        internal static bool PaddingHandlesEnabled
        {
            get => EditorPrefs.GetBool(PaddingHandlesKey, false);
            set
            {
                EditorPrefs.SetBool(PaddingHandlesKey, value);
                SceneView.RepaintAll();
            }
        }

        internal static bool GapHandlesEnabled
        {
            get => EditorPrefs.GetBool(GapHandlesKey, false);
            set
            {
                EditorPrefs.SetBool(GapHandlesKey, value);
                SceneView.RepaintAll();
            }
        }

        internal static void DrawPaddingHandles(TaffyLayoutGroup group)
        {
            if (!PaddingHandlesEnabled || !group || !(group.transform is RectTransform root) || group.padding == null)
                return;

            Rect content = TaffySceneOverlayDrawing.GetPaddingRect(root.rect, group.padding);
            DrawPaddingHandle(group, root, new Vector2(content.xMin, content.center.y), Vector2.right, "m_Left");
            DrawPaddingHandle(group, root, new Vector2(content.xMax, content.center.y), Vector2.left, "m_Right");
            DrawPaddingHandle(group, root, new Vector2(content.center.x, content.yMax), Vector2.down, "m_Top");
            DrawPaddingHandle(group, root, new Vector2(content.center.x, content.yMin), Vector2.up, "m_Bottom");
        }

        internal static void DrawGapHandles(TaffyLayoutGroup group)
        {
            if (!GapHandlesEnabled || !group || !(group.transform is RectTransform root))
                return;

            Rect content = TaffySceneOverlayDrawing.GetPaddingRect(root.rect, group.padding);
            TaffySceneOverlayDrawing.GetGapMarkerSegments(
                content,
                group.horizontalGap,
                group.verticalGap,
                out _,
                out Vector2 horizontalEnd,
                out _,
                out Vector2 verticalEnd);

            DrawGapHandle(group, root, horizontalEnd, Vector2.right, "horizontalGap", group.horizontalGap, "H gap");
            DrawGapHandle(group, root, verticalEnd, Vector2.up, "verticalGap", group.verticalGap, "V gap");
        }

        internal static bool ApplyPaddingDelta(TaffyLayoutGroup group, string sidePropertyName, float signedDelta)
        {
            if (!group || string.IsNullOrEmpty(sidePropertyName))
                return false;

            SerializedObject serializedObject = new SerializedObject(group);
            serializedObject.Update();
            SerializedProperty padding = serializedObject.FindProperty("m_Padding");
            SerializedProperty side = padding?.FindPropertyRelative(sidePropertyName);
            if (side == null)
                return false;

            Undo.RecordObject(group, "Change Taffy Padding");
            side.intValue = Mathf.Max(0, Mathf.RoundToInt(side.intValue + signedDelta));
            serializedObject.ApplyModifiedProperties();
            RecordEditorChange(group);
            return true;
        }

        internal static bool ApplyGapDelta(TaffyLayoutGroup group, string propertyName, float gapDelta)
        {
            if (!group || string.IsNullOrEmpty(propertyName))
                return false;

            SerializedObject serializedObject = new SerializedObject(group);
            serializedObject.Update();
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null || property.propertyType != SerializedPropertyType.Float)
                return false;

            Undo.RecordObject(group, "Change Taffy Gap");
            property.floatValue = Mathf.Max(0f, property.floatValue + gapDelta);
            serializedObject.ApplyModifiedProperties();
            RecordEditorChange(group);
            return true;
        }

        private static void DrawPaddingHandle(
            TaffyLayoutGroup group,
            RectTransform root,
            Vector2 localPosition,
            Vector2 localIncreaseDirection,
            string sidePropertyName)
        {
            Vector3 worldPosition = root.TransformPoint(new Vector3(localPosition.x, localPosition.y, 0f));
            Vector3 worldDirection = root.TransformDirection(new Vector3(localIncreaseDirection.x, localIncreaseDirection.y, 0f)).normalized;
            float handleSize = HandleUtility.GetHandleSize(worldPosition) * 0.06f;

            EditorGUI.BeginChangeCheck();
            Vector3 nextWorldPosition = Handles.Slider(
                worldPosition,
                worldDirection,
                handleSize,
                Handles.DotHandleCap,
                1f);
            if (!EditorGUI.EndChangeCheck())
                return;

            Vector3 localDelta = root.InverseTransformPoint(nextWorldPosition) - root.InverseTransformPoint(worldPosition);
            float signedDelta = Vector2.Dot(new Vector2(localDelta.x, localDelta.y), localIncreaseDirection);
            ApplyPaddingDelta(group, sidePropertyName, signedDelta);
        }

        private static void DrawGapHandle(
            TaffyLayoutGroup group,
            RectTransform root,
            Vector2 localPosition,
            Vector2 localIncreaseDirection,
            string propertyName,
            float currentValue,
            string label)
        {
            Vector3 worldPosition = root.TransformPoint(new Vector3(localPosition.x, localPosition.y, 0f));
            Vector3 worldDirection = root.TransformDirection(new Vector3(localIncreaseDirection.x, localIncreaseDirection.y, 0f)).normalized;
            float handleSize = HandleUtility.GetHandleSize(worldPosition) * 0.06f;
            Handles.Label(worldPosition, $"{label} {currentValue:0.##}");

            EditorGUI.BeginChangeCheck();
            Vector3 nextWorldPosition = Handles.Slider(
                worldPosition,
                worldDirection,
                handleSize,
                Handles.DotHandleCap,
                1f);
            if (!EditorGUI.EndChangeCheck())
                return;

            Vector3 localDelta = root.InverseTransformPoint(nextWorldPosition) - root.InverseTransformPoint(worldPosition);
            float halfGapDelta = Vector2.Dot(new Vector2(localDelta.x, localDelta.y), localIncreaseDirection);
            ApplyGapDelta(group, propertyName, halfGapDelta * 2f);
        }

        private static void RecordEditorChange(TaffyLayoutGroup group)
        {
            PrefabUtility.RecordPrefabInstancePropertyModifications(group);
            EditorUtility.SetDirty(group);
            group.SetLayoutDirty();
            SceneView.RepaintAll();
        }

        [MenuItem("Tools/TaffyUGUI/Scene Handles/Padding Handles")]
        private static void TogglePaddingHandles() => PaddingHandlesEnabled = !PaddingHandlesEnabled;

        [MenuItem("Tools/TaffyUGUI/Scene Handles/Padding Handles", true)]
        private static bool ValidatePaddingHandles()
        {
            Menu.SetChecked("Tools/TaffyUGUI/Scene Handles/Padding Handles", PaddingHandlesEnabled);
            return true;
        }

        [MenuItem("Tools/TaffyUGUI/Scene Handles/Gap Handles")]
        private static void ToggleGapHandles() => GapHandlesEnabled = !GapHandlesEnabled;

        [MenuItem("Tools/TaffyUGUI/Scene Handles/Gap Handles", true)]
        private static bool ValidateGapHandles()
        {
            Menu.SetChecked("Tools/TaffyUGUI/Scene Handles/Gap Handles", GapHandlesEnabled);
            return true;
        }
    }
}
