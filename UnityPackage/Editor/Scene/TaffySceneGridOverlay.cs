using UnityEditor;
using UnityEngine;

namespace TaffyUGUI.Editor
{
    internal static class TaffySceneGridOverlay
    {
        internal static void DrawTrackLabels(RectTransform root, TaffyLayoutGroup group, TaffyGridDiagnostics diagnostics)
        {
            if (!root || group == null || diagnostics == null)
                return;

            Rect content = TaffySceneOverlayDrawing.GetPaddingRect(root.rect, group.padding);
            float[] columnCenters = GetTrackCenters(content.xMin, diagnostics.columnTrackSizes, diagnostics.columnGutters, 1f);
            float[] rowCenters = GetTrackCenters(content.yMax, diagnostics.rowTrackSizes, diagnostics.rowGutters, -1f);

            for (int i = 0; i < columnCenters.Length; i++)
            {
                Vector3 position = root.TransformPoint(new Vector3(columnCenters[i], content.yMax, 0f));
                Handles.Label(position, $"C{i + 1}  {diagnostics.columnTrackSizes[i]:0.##}");
            }

            for (int i = 0; i < rowCenters.Length; i++)
            {
                Vector3 position = root.TransformPoint(new Vector3(content.xMin, rowCenters[i], 0f));
                Handles.Label(position, $"R{i + 1}  {diagnostics.rowTrackSizes[i]:0.##}");
            }
        }

        internal static float[] GetTrackCenters(float start, float[] trackSizes, float[] gutters, float direction)
        {
            if (trackSizes == null || trackSizes.Length == 0)
                return System.Array.Empty<float>();

            var centers = new float[trackSizes.Length];
            float cursor = start;
            for (int i = 0; i < trackSizes.Length; i++)
            {
                float size = trackSizes[i];
                centers[i] = cursor + direction * size * 0.5f;
                cursor += direction * size;
                if (gutters != null && i + 1 < gutters.Length)
                    cursor += direction * gutters[i + 1];
            }
            return centers;
        }
    }
}
