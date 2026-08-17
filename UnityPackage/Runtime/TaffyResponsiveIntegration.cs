using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TaffyUGUI
{
    public enum TaffySafeAreaMode
    {
        Disabled = 0,
        Padding = 1,
    }

    public enum TaffyScrollRectContentMode
    {
        Disabled = 0,
        AutoExpandContent = 1,
    }

    public enum TaffyPixelRounding
    {
        None = 0,
        Round = 1,
        Floor = 2,
        Ceil = 3,
        CanvasPixel = 4,
    }

    [Serializable]
    public struct TaffyPixelInsets
    {
        public float left;
        public float right;
        public float top;
        public float bottom;

        public static TaffyPixelInsets Zero => default;

        public TaffyPixelInsets(float left, float right, float top, float bottom)
        {
            this.left = left;
            this.right = right;
            this.top = top;
            this.bottom = bottom;
        }

        internal TaffyPixelInsets ClampNonNegative()
        {
            return new TaffyPixelInsets(
                Mathf.Max(0f, FiniteOrZero(left)),
                Mathf.Max(0f, FiniteOrZero(right)),
                Mathf.Max(0f, FiniteOrZero(top)),
                Mathf.Max(0f, FiniteOrZero(bottom)));
        }

        internal static TaffyPixelInsets Add(TaffyPixelInsets a, TaffyPixelInsets b)
        {
            return new TaffyPixelInsets(
                a.left + b.left,
                a.right + b.right,
                a.top + b.top,
                a.bottom + b.bottom);
        }

        private static float FiniteOrZero(float value)
        {
            return float.IsNaN(value) || float.IsInfinity(value) ? 0f : value;
        }
    }

    [Serializable]
    public sealed class TaffyResponsiveProfile
    {
        public string name = "Profile";
        public int priority;
        [Min(0)] public float minWidth;
        [Min(0)] public float maxWidth;
        [Min(0)] public float minHeight;
        [Min(0)] public float maxHeight;

        [Header("Formatting")]
        public bool overrideContainerDisplay;
        public TaffyContainerDisplay containerDisplay = TaffyContainerDisplay.Flex;

        [Header("Flex")]
        public bool overrideFlexDirection;
        public TaffyFlexDirection direction = TaffyFlexDirection.Row;
        public bool overrideFlexWrap;
        public TaffyFlexWrap wrap = TaffyFlexWrap.NoWrap;
        public bool overrideGaps;
        [Min(0)] public float horizontalGap;
        [Min(0)] public float verticalGap;
        public bool overrideAlignment;
        public TaffyJustify justifyContent = TaffyJustify.Start;
        public TaffyAlign alignItems = TaffyAlign.Stretch;
        public TaffyAlignContent alignContent = TaffyAlignContent.Auto;
        public TaffyAlign justifyItems = TaffyAlign.Auto;

        [Header("Grid")]
        public bool overrideGridAutoFlow;
        public TaffyGridAutoFlow gridAutoFlow = TaffyGridAutoFlow.Row;

        [Header("Padding")]
        public bool overridePadding;
        public TaffyPixelInsets padding;

        internal bool Matches(Vector2 size)
        {
            float width = Mathf.Max(0f, size.x);
            float height = Mathf.Max(0f, size.y);
            if (width + 0.0001f < Mathf.Max(0f, minWidth) || height + 0.0001f < Mathf.Max(0f, minHeight))
                return false;
            if (maxWidth > 0f && width - 0.0001f > maxWidth)
                return false;
            if (maxHeight > 0f && height - 0.0001f > maxHeight)
                return false;
            return true;
        }
    }

    internal struct TaffyResolvedContainerSettings
    {
        internal TaffyContainerDisplay display;
        internal TaffyFlexDirection direction;
        internal TaffyFlexWrap wrap;
        internal float horizontalGap;
        internal float verticalGap;
        internal TaffyJustify justifyContent;
        internal TaffyAlign alignItems;
        internal TaffyAlignContent alignContent;
        internal TaffyAlign justifyItems;
        internal TaffyGridAutoFlow gridAutoFlow;
        internal TaffyPixelInsets padding;
        internal string profileName;
    }

    internal static class TaffyResponsiveUtility
    {
        internal static TaffyPixelInsets ResolveScreenSafeArea(RectTransform rectTransform)
        {
            if (!rectTransform || Screen.width <= 0 || Screen.height <= 0)
                return default;

            Rect safe = Screen.safeArea;
            Rect local = rectTransform.rect;
            float width = Mathf.Max(0f, local.width);
            float height = Mathf.Max(0f, local.height);
            float screenWidth = Mathf.Max(1f, Screen.width);
            float screenHeight = Mathf.Max(1f, Screen.height);

            return new TaffyPixelInsets(
                safe.xMin / screenWidth * width,
                (screenWidth - safe.xMax) / screenWidth * width,
                (screenHeight - safe.yMax) / screenHeight * height,
                safe.yMin / screenHeight * height).ClampNonNegative();
        }

        internal static float RoundEdge(float value, TaffyPixelRounding mode, float canvasScale)
        {
            if (mode == TaffyPixelRounding.None || float.IsNaN(value) || float.IsInfinity(value))
                return value;

            if (mode == TaffyPixelRounding.CanvasPixel)
            {
                float scale = canvasScale > 0f && !float.IsNaN(canvasScale) && !float.IsInfinity(canvasScale) ? canvasScale : 1f;
                return Mathf.Round(value * scale) / scale;
            }

            switch (mode)
            {
                case TaffyPixelRounding.Floor: return Mathf.Floor(value);
                case TaffyPixelRounding.Ceil: return Mathf.Ceil(value);
                default: return Mathf.Round(value);
            }
        }

        internal static ScrollRect FindOwningScrollRect(RectTransform content)
        {
            if (!content)
                return null;

            Transform current = content.parent;
            while (current)
            {
                ScrollRect scroll = current.GetComponent<ScrollRect>();
                if (scroll && scroll.content == content)
                    return scroll;
                current = current.parent;
            }
            return null;
        }

        internal static bool ContentSizeFitterOwnsAxis(ContentSizeFitter fitter, int axis)
        {
            if (!fitter || !fitter.enabled)
                return false;
            ContentSizeFitter.FitMode mode = axis == 0 ? fitter.horizontalFit : fitter.verticalFit;
            return mode != ContentSizeFitter.FitMode.Unconstrained;
        }

        internal static bool AspectRatioFitterOwnsSelfSize(AspectRatioFitter fitter)
        {
            return fitter && fitter.enabled && fitter.aspectMode != AspectRatioFitter.AspectMode.None;
        }

        internal static bool TryResolveChildAspectRatio(RectTransform child, out float ratio, out string warning)
        {
            ratio = 0f;
            warning = null;
            if (!child)
                return false;

            AspectRatioFitter fitter = child.GetComponent<AspectRatioFitter>();
            if (!fitter || !fitter.enabled || fitter.aspectMode == AspectRatioFitter.AspectMode.None)
                return false;

            ratio = Mathf.Max(0f, fitter.aspectRatio);
            if (fitter.aspectMode == AspectRatioFitter.AspectMode.FitInParent ||
                fitter.aspectMode == AspectRatioFitter.AspectMode.EnvelopeParent)
            {
                warning = $"{child.name}: AspectRatioFitter {fitter.aspectMode} also changes anchors/size after layout. Prefer TaffyLayoutItem.aspectRatio or WidthControlsHeight/HeightControlsWidth for deterministic Taffy geometry.";
            }
            return ratio > 0f;
        }

        internal static string[] CollectIntegrationWarnings(TaffyLayoutGroup group)
        {
            var warnings = new List<string>();
            if (!group)
                return warnings.ToArray();

            ScrollRect scroll = FindOwningScrollRect(group.transform as RectTransform);
            ContentSizeFitter contentFitter = group.GetComponent<ContentSizeFitter>();
            AspectRatioFitter selfAspect = group.GetComponent<AspectRatioFitter>();

            if (scroll && group.scrollRectContentMode == TaffyScrollRectContentMode.AutoExpandContent)
            {
                if (scroll.horizontal && ContentSizeFitterOwnsAxis(contentFitter, 0))
                    warnings.Add("Horizontal ScrollRect content sizing is owned by ContentSizeFitter; the Taffy ScrollRect bridge yields that axis.");
                if (scroll.vertical && ContentSizeFitterOwnsAxis(contentFitter, 1))
                    warnings.Add("Vertical ScrollRect content sizing is owned by ContentSizeFitter; the Taffy ScrollRect bridge yields that axis.");
                if (AspectRatioFitterOwnsSelfSize(selfAspect))
                    warnings.Add("AspectRatioFitter controls the Taffy ScrollRect content RectTransform; automatic content expansion yields to the fitter to avoid a rebuild loop.");
            }

            for (int i = 0; i < group.transform.childCount; i++)
            {
                RectTransform child = group.transform.GetChild(i) as RectTransform;
                if (TryResolveChildAspectRatio(child, out _, out string warning) && !string.IsNullOrEmpty(warning))
                    warnings.Add(warning);
            }

            return warnings.ToArray();
        }
    }
}
