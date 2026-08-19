using UnityEditor;
using UnityEngine;

namespace TaffyUGUI.Editor
{
    internal static class TaffySceneSizeOverlay
    {
        internal static void DrawComputedSizeLabels(RectTransform root)
        {
            if (!root)
                return;

            for (int i = 0; i < root.childCount; i++)
            {
                RectTransform child = root.GetChild(i) as RectTransform;
                if (!child)
                    continue;

                Rect rect = child.rect;
                Vector3 center = child.TransformPoint(rect.center);
                Handles.Label(center, FormatSize(rect.size));
            }
        }

        internal static string FormatSize(Vector2 size)
        {
            return $"{size.x:0.##} × {size.y:0.##}";
        }
    }
}
