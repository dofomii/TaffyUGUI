using UnityEditor;
using UnityEngine;

namespace TaffyUGUI.Editor
{
    [InitializeOnLoad]
    public static class TaffySceneVisualization
    {
        private const string PreferenceKey = "TaffyUGUI.SceneVisualization.Enabled";

        static TaffySceneVisualization()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            SceneView.duringSceneGui += OnSceneGUI;
        }

        public static bool Enabled
        {
            get => EditorPrefs.GetBool(PreferenceKey, true);
            set
            {
                EditorPrefs.SetBool(PreferenceKey, value);
                SceneView.RepaintAll();
            }
        }

        [MenuItem("Tools/TaffyUGUI/Toggle Scene Visualization")]
        private static void Toggle()
        {
            Enabled = !Enabled;
        }

        [MenuItem("Tools/TaffyUGUI/Toggle Scene Visualization", true)]
        private static bool ToggleValidate()
        {
            Menu.SetChecked("Tools/TaffyUGUI/Toggle Scene Visualization", Enabled);
            return true;
        }

        private static void OnSceneGUI(SceneView sceneView)
        {
            if (!Enabled)
                return;

            Object[] selection = Selection.objects;
            for (int i = 0; i < selection.Length; i++)
            {
                GameObject go = selection[i] as GameObject;
                if (!go && selection[i] is Component component)
                    go = component.gameObject;
                if (!go)
                    continue;

                TaffyLayoutGroup group = go.GetComponent<TaffyLayoutGroup>();
                if (!group || !group.isActiveAndEnabled)
                    continue;

                DrawGroup(group);
            }
        }

        private static void DrawGroup(TaffyLayoutGroup group)
        {
            RectTransform root = group.transform as RectTransform;
            if (!root)
                return;

            using (new Handles.DrawingScope(new Color(0.2f, 0.85f, 1f, 0.95f)))
            {
                DrawRect(root);
                Handles.Label(root.TransformPoint(new Vector3(root.rect.xMin, root.rect.yMax, 0f)),
                    string.IsNullOrEmpty(group.ActiveResponsiveProfileName)
                        ? $"Taffy {group.containerDisplay}"
                        : $"Taffy {group.containerDisplay} [{group.ActiveResponsiveProfileName}]");
            }

            using (new Handles.DrawingScope(new Color(1f, 0.72f, 0.15f, 0.85f)))
            {
                for (int i = 0; i < root.childCount; i++)
                {
                    RectTransform child = root.GetChild(i) as RectTransform;
                    if (child)
                        DrawRect(child);
                }
            }

            if (group.containerDisplay == TaffyContainerDisplay.Grid &&
                group.TryGetGridDiagnostics(out TaffyGridDiagnostics diagnostics, out _))
            {
                DrawGridTracks(root, group, diagnostics);
            }
        }

        private static void DrawRect(RectTransform rect)
        {
            Vector3[] corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            Handles.DrawLine(corners[0], corners[1]);
            Handles.DrawLine(corners[1], corners[2]);
            Handles.DrawLine(corners[2], corners[3]);
            Handles.DrawLine(corners[3], corners[0]);
        }

        private static void DrawGridTracks(RectTransform root, TaffyLayoutGroup group, TaffyGridDiagnostics diagnostics)
        {
            Rect local = root.rect;
            float left = local.xMin + (group.padding != null ? group.padding.left : 0f);
            float top = local.yMax - (group.padding != null ? group.padding.top : 0f);
            float right = local.xMax - (group.padding != null ? group.padding.right : 0f);
            float bottom = local.yMin + (group.padding != null ? group.padding.bottom : 0f);

            using (new Handles.DrawingScope(new Color(0.55f, 1f, 0.45f, 0.8f)))
            {
                float x = left;
                DrawVertical(root, x, bottom, top);
                for (int i = 0; i < diagnostics.columnTrackSizes.Length; i++)
                {
                    x += diagnostics.columnTrackSizes[i];
                    if (i + 1 < diagnostics.columnGutters.Length)
                        x += diagnostics.columnGutters[i + 1];
                    DrawVertical(root, x, bottom, top);
                }

                float y = top;
                DrawHorizontal(root, y, left, right);
                for (int i = 0; i < diagnostics.rowTrackSizes.Length; i++)
                {
                    y -= diagnostics.rowTrackSizes[i];
                    if (i + 1 < diagnostics.rowGutters.Length)
                        y -= diagnostics.rowGutters[i + 1];
                    DrawHorizontal(root, y, left, right);
                }
            }
        }

        private static void DrawVertical(RectTransform root, float x, float bottom, float top)
        {
            Handles.DrawLine(root.TransformPoint(new Vector3(x, bottom, 0f)), root.TransformPoint(new Vector3(x, top, 0f)));
        }

        private static void DrawHorizontal(RectTransform root, float y, float left, float right)
        {
            Handles.DrawLine(root.TransformPoint(new Vector3(left, y, 0f)), root.TransformPoint(new Vector3(right, y, 0f)));
        }
    }
}
