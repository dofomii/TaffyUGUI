using UnityEditor;
using UnityEngine;

namespace TaffyUGUI.Editor
{
    internal static class TaffySceneOverlayDrawing
    {
        private static readonly Color GroupColor = new Color(0.2f, 0.85f, 1f, 0.95f);
        private static readonly Color ChildColor = new Color(1f, 0.72f, 0.15f, 0.85f);
        private static readonly Color PaddingColor = new Color(0.8f, 0.45f, 1f, 0.75f);
        private static readonly Color MarginColor = new Color(1f, 0.35f, 0.55f, 0.8f);
        private static readonly Color GridColor = new Color(0.55f, 1f, 0.45f, 0.8f);
        private static readonly Color MainAxisColor = new Color(0.95f, 0.45f, 0.2f, 0.9f);
        private static readonly Color GapColor = new Color(1f, 0.9f, 0.2f, 0.9f);
        private static readonly Color CrossAxisColor = new Color(0.35f, 0.75f, 1f, 0.9f);

        internal static void DrawGroup(TaffyLayoutGroup group)
        {
            if (!group)
                return;

            RectTransform root = group.transform as RectTransform;
            if (!root)
                return;

            if (TaffySceneOverlayPreferences.ContainerBounds)
                DrawGroupBounds(root);
            if (TaffySceneOverlayPreferences.ResponsiveProfileLabel)
                DrawGroupLabel(root, group);
            if (TaffySceneOverlayPreferences.ChildBounds)
                DrawChildBounds(root);
            if (TaffySceneOverlayPreferences.PaddingBounds)
                DrawPaddingBounds(root, group.padding);
            if (TaffySceneOverlayPreferences.FlexAxes)
                DrawFlexAxes(group, root);
            if (TaffySceneOverlayPreferences.GapMarkers)
                DrawGapMarkers(group, root);
            if (TaffySceneOverlayPreferences.GridTracks)
                DrawGrid(group, root);
            if (TaffySceneOverlayPreferences.ComputedSizeLabels)
                TaffySceneSizeOverlay.DrawComputedSizeLabels(root);
            TaffyResponsivePreview.Draw(group, root);
            TaffySceneHandles.DrawPaddingHandles(group);
            TaffySceneHandles.DrawGapHandles(group);
        }

        internal static void DrawGroupBounds(RectTransform root)
        {
            using (new Handles.DrawingScope(GroupColor))
                DrawRect(root);
        }

        internal static void DrawGroupLabel(RectTransform root, TaffyLayoutGroup group)
        {
            string label = string.IsNullOrEmpty(group.ActiveResponsiveProfileName)
                ? $"Taffy {group.containerDisplay}"
                : $"Taffy {group.containerDisplay} [{group.ActiveResponsiveProfileName}]";
            Handles.Label(root.TransformPoint(new Vector3(root.rect.xMin, root.rect.yMax, 0f)), label);
        }

        internal static void DrawChildBounds(RectTransform root)
        {
            using (new Handles.DrawingScope(ChildColor))
            {
                for (int i = 0; i < root.childCount; i++)
                {
                    RectTransform child = root.GetChild(i) as RectTransform;
                    if (child)
                        DrawRect(child);
                }
            }
        }

        internal static void DrawPaddingBounds(RectTransform root, RectOffset padding)
        {
            if (!root || padding == null)
                return;

            Rect rect = GetPaddingRect(root.rect, padding);
            using (new Handles.DrawingScope(PaddingColor))
                DrawLocalRect(root, rect);
        }

        internal static Rect GetPaddingRect(Rect container, RectOffset padding)
        {
            if (padding == null)
                return container;

            float xMin = container.xMin + padding.left;
            float xMax = container.xMax - padding.right;
            float yMin = container.yMin + padding.bottom;
            float yMax = container.yMax - padding.top;
            return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
        }

        internal static void DrawSelectedItemMargin(TaffyLayoutItem item)
        {
            if (!item || !(item.transform is RectTransform rect))
                return;

            if (TryGetPointMarginRect(rect.rect, item.margin, out Rect marginRect))
            {
                using (new Handles.DrawingScope(MarginColor))
                    DrawLocalRect(rect, marginRect);
                return;
            }

            Handles.Label(
                rect.TransformPoint(new Vector3(rect.rect.xMin, rect.rect.yMax, 0f)),
                "Margin: " + DescribeEdges(item.margin));
        }

        internal static bool TryGetPointMarginRect(Rect itemRect, TaffyEdges margin, out Rect marginRect)
        {
            if (margin.left.unit != TaffyUnit.Points ||
                margin.right.unit != TaffyUnit.Points ||
                margin.top.unit != TaffyUnit.Points ||
                margin.bottom.unit != TaffyUnit.Points)
            {
                marginRect = itemRect;
                return false;
            }

            marginRect = Rect.MinMaxRect(
                itemRect.xMin - margin.left.value,
                itemRect.yMin - margin.bottom.value,
                itemRect.xMax + margin.right.value,
                itemRect.yMax + margin.top.value);
            return true;
        }

        internal static void DrawFlexAxes(TaffyLayoutGroup group, RectTransform root)
        {
            if (!group || !root || group.containerDisplay != TaffyContainerDisplay.Flex)
                return;

            Rect content = GetPaddingRect(root.rect, group.padding);
            GetFlexAxes(
                content,
                group.direction,
                group.writingDirection,
                out Vector2 mainStart,
                out Vector2 mainEnd,
                out Vector2 crossStart,
                out Vector2 crossEnd);

            using (new Handles.DrawingScope(MainAxisColor))
                DrawDirectionalLine(root, mainStart, mainEnd);
            using (new Handles.DrawingScope(CrossAxisColor))
                DrawDirectionalLine(root, crossStart, crossEnd);

            Handles.Label(root.TransformPoint(new Vector3(mainEnd.x, mainEnd.y, 0f)), "Main");
            Handles.Label(root.TransformPoint(new Vector3(crossEnd.x, crossEnd.y, 0f)), "Cross");
        }

        internal static void GetFlexAxes(
            Rect content,
            TaffyFlexDirection direction,
            TaffyWritingDirection writingDirection,
            out Vector2 mainStart,
            out Vector2 mainEnd,
            out Vector2 crossStart,
            out Vector2 crossEnd)
        {
            float midX = content.center.x;
            float midY = content.center.y;
            bool rtl = writingDirection == TaffyWritingDirection.RightToLeft;

            switch (direction)
            {
                case TaffyFlexDirection.Row:
                case TaffyFlexDirection.RowReverse:
                    mainStart = rtl ? new Vector2(content.xMax, midY) : new Vector2(content.xMin, midY);
                    mainEnd = rtl ? new Vector2(content.xMin, midY) : new Vector2(content.xMax, midY);
                    if (direction == TaffyFlexDirection.RowReverse)
                        Swap(ref mainStart, ref mainEnd);
                    crossStart = new Vector2(midX, content.yMax);
                    crossEnd = new Vector2(midX, content.yMin);
                    break;
                default:
                    mainStart = new Vector2(midX, content.yMax);
                    mainEnd = new Vector2(midX, content.yMin);
                    if (direction == TaffyFlexDirection.ColumnReverse)
                        Swap(ref mainStart, ref mainEnd);
                    crossStart = rtl ? new Vector2(content.xMax, midY) : new Vector2(content.xMin, midY);
                    crossEnd = rtl ? new Vector2(content.xMin, midY) : new Vector2(content.xMax, midY);
                    break;
            }
        }

        internal static void DrawGapMarkers(TaffyLayoutGroup group, RectTransform root)
        {
            if (!group || !root)
                return;

            Rect content = GetPaddingRect(root.rect, group.padding);
            using (new Handles.DrawingScope(GapColor))
            {
                if (group.horizontalGap > 0f && content.width > 0f)
                {
                    float visibleGap = Mathf.Min(group.horizontalGap, content.width);
                    float y = content.yMax - Mathf.Min(8f, content.height * 0.08f);
                    Vector2 start = new Vector2(content.center.x - visibleGap * 0.5f, y);
                    Vector2 end = new Vector2(content.center.x + visibleGap * 0.5f, y);
                    DrawGapDimension(root, start, end, Vector2.up);
                    Handles.Label(root.TransformPoint(new Vector3(end.x, end.y, 0f)), $"H gap {group.horizontalGap:0.##}");
                }

                if (group.verticalGap > 0f && content.height > 0f)
                {
                    float visibleGap = Mathf.Min(group.verticalGap, content.height);
                    float x = content.xMax - Mathf.Min(8f, content.width * 0.08f);
                    Vector2 start = new Vector2(x, content.center.y - visibleGap * 0.5f);
                    Vector2 end = new Vector2(x, content.center.y + visibleGap * 0.5f);
                    DrawGapDimension(root, start, end, Vector2.right);
                    Handles.Label(root.TransformPoint(new Vector3(end.x, end.y, 0f)), $"V gap {group.verticalGap:0.##}");
                }
            }
        }

        internal static void GetGapMarkerSegments(
            Rect content,
            float horizontalGap,
            float verticalGap,
            out Vector2 horizontalStart,
            out Vector2 horizontalEnd,
            out Vector2 verticalStart,
            out Vector2 verticalEnd)
        {
            float visibleHorizontal = Mathf.Clamp(horizontalGap, 0f, Mathf.Max(0f, content.width));
            float visibleVertical = Mathf.Clamp(verticalGap, 0f, Mathf.Max(0f, content.height));
            float horizontalY = content.yMax - Mathf.Min(8f, content.height * 0.08f);
            float verticalX = content.xMax - Mathf.Min(8f, content.width * 0.08f);

            horizontalStart = new Vector2(content.center.x - visibleHorizontal * 0.5f, horizontalY);
            horizontalEnd = new Vector2(content.center.x + visibleHorizontal * 0.5f, horizontalY);
            verticalStart = new Vector2(verticalX, content.center.y - visibleVertical * 0.5f);
            verticalEnd = new Vector2(verticalX, content.center.y + visibleVertical * 0.5f);
        }

        private static void DrawGapDimension(RectTransform root, Vector2 start, Vector2 end, Vector2 tickDirection)
        {
            DrawLocalLine(root, start, end);
            float tick = 4f;
            DrawLocalLine(root, start - tickDirection * tick, start + tickDirection * tick);
            DrawLocalLine(root, end - tickDirection * tick, end + tickDirection * tick);
        }

        internal static void DrawGrid(TaffyLayoutGroup group, RectTransform root)
        {
            if (group.containerDisplay != TaffyContainerDisplay.Grid ||
                !group.TryGetGridDiagnostics(out TaffyGridDiagnostics diagnostics, out _))
                return;

            DrawGridTracks(root, group, diagnostics);
            TaffySceneGridOverlay.DrawTrackLabels(root, group, diagnostics);
        }

        internal static void DrawGridTracks(RectTransform root, TaffyLayoutGroup group, TaffyGridDiagnostics diagnostics)
        {
            Rect local = root.rect;
            float left = local.xMin + (group.padding != null ? group.padding.left : 0f);
            float top = local.yMax - (group.padding != null ? group.padding.top : 0f);
            float right = local.xMax - (group.padding != null ? group.padding.right : 0f);
            float bottom = local.yMin + (group.padding != null ? group.padding.bottom : 0f);

            using (new Handles.DrawingScope(GridColor))
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

        internal static void DrawRect(RectTransform rect)
        {
            Vector3[] corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            Handles.DrawLine(corners[0], corners[1]);
            Handles.DrawLine(corners[1], corners[2]);
            Handles.DrawLine(corners[2], corners[3]);
            Handles.DrawLine(corners[3], corners[0]);
        }

        internal static void DrawLocalRect(RectTransform root, Rect rect)
        {
            Vector3 bottomLeft = root.TransformPoint(new Vector3(rect.xMin, rect.yMin, 0f));
            Vector3 topLeft = root.TransformPoint(new Vector3(rect.xMin, rect.yMax, 0f));
            Vector3 topRight = root.TransformPoint(new Vector3(rect.xMax, rect.yMax, 0f));
            Vector3 bottomRight = root.TransformPoint(new Vector3(rect.xMax, rect.yMin, 0f));
            Handles.DrawLine(bottomLeft, topLeft);
            Handles.DrawLine(topLeft, topRight);
            Handles.DrawLine(topRight, bottomRight);
            Handles.DrawLine(bottomRight, bottomLeft);
        }

        private static string DescribeEdges(TaffyEdges edges)
        {
            return $"L {DescribeLength(edges.left)}  R {DescribeLength(edges.right)}  T {DescribeLength(edges.top)}  B {DescribeLength(edges.bottom)}";
        }

        private static string DescribeLength(TaffyLength length)
        {
            switch (length.unit)
            {
                case TaffyUnit.Points: return $"{length.value:0.##}px";
                case TaffyUnit.Percent: return $"{length.value * 100f:0.##}%";
                case TaffyUnit.Calc: return "Calc";
                default: return "Auto";
            }
        }

        private static void DrawDirectionalLine(RectTransform root, Vector2 start, Vector2 end)
        {
            Vector2 delta = end - start;
            if (delta.sqrMagnitude < 0.0001f)
                return;

            Vector2 direction = delta.normalized;
            Vector2 perpendicular = new Vector2(-direction.y, direction.x);
            float arrowLength = Mathf.Min(12f, delta.magnitude * 0.2f);
            float arrowWidth = arrowLength * 0.45f;
            Vector2 arrowBase = end - direction * arrowLength;
            Vector2 wingA = arrowBase + perpendicular * arrowWidth;
            Vector2 wingB = arrowBase - perpendicular * arrowWidth;

            DrawLocalLine(root, start, end);
            DrawLocalLine(root, end, wingA);
            DrawLocalLine(root, end, wingB);
        }

        private static void DrawLocalLine(RectTransform root, Vector2 start, Vector2 end)
        {
            Handles.DrawLine(
                root.TransformPoint(new Vector3(start.x, start.y, 0f)),
                root.TransformPoint(new Vector3(end.x, end.y, 0f)));
        }

        private static void Swap(ref Vector2 first, ref Vector2 second)
        {
            Vector2 temp = first;
            first = second;
            second = temp;
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
