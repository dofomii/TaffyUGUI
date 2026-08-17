using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TaffyUGUI.Tests
{
    public sealed class TaffyPhase10PlayModeTests
    {
        [UnityTest]
        public IEnumerator ResponsiveResizeAndCanvasScaleObservationReactAcrossFrames()
        {
            var canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.scaleFactor = 1f;

            RectTransform root = CreateRoot("ResponsiveRuntime", canvasObject.transform, 300f, 140f, out TaffyLayoutGroup group);
            group.alignItems = TaffyAlign.Start;
            group.pixelRounding = TaffyPixelRounding.None;
            group.responsiveProfiles.Add(new TaffyResponsiveProfile
            {
                name = "narrow",
                priority = 5,
                maxWidth = 350f,
                overrideFlexDirection = true,
                direction = TaffyFlexDirection.Column,
            });
            group.responsiveProfiles.Add(new TaffyResponsiveProfile
            {
                name = "wide",
                priority = 5,
                minWidth = 351f,
                overrideFlexDirection = true,
                direction = TaffyFlexDirection.Row,
            });
            RectTransform first = CreateItem(root, "First", 33.3f, 20f);
            RectTransform second = CreateItem(root, "Second", 33.3f, 20f);

            yield return null;
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(root);
            Assert.That(group.ActiveResponsiveProfileName, Is.EqualTo("narrow"));
            Assert.That(first.rect.width, Is.GreaterThan(30f));
            Assert.That(Top(second), Is.GreaterThan(Top(first) + 1f));

            root.sizeDelta = new Vector2(500f, 140f);
            scaler.scaleFactor = 2f;
            yield return null;
            Canvas.ForceUpdateCanvases();
            Assert.That(scaler.scaleFactor, Is.EqualTo(2f).Within(0.01f));

            // Headless batch mode does not consistently propagate CanvasScaler to Canvas.scaleFactor.
            // Drive the effective scale so the production observation path is still exercised deterministically.
            canvas.scaleFactor = 2f;
            MethodInfo lateUpdate = typeof(TaffyLayoutGroup).GetMethod(
                "LateUpdate",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(lateUpdate, Is.Not.Null);
            lateUpdate.Invoke(group, null);
            LayoutRebuilder.ForceRebuildLayoutImmediate(root);

            Assert.That(group.ActiveResponsiveProfileName, Is.EqualTo("wide"));
            Assert.That(Top(second), Is.EqualTo(Top(first)).Within(0.1f));
            Assert.That(Left(second), Is.GreaterThan(30f));

            Object.Destroy(canvasObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator AnimationCallbackInvalidatesAndRecomputesLayout()
        {
            var rootObject = new GameObject("AnimatedRoot", typeof(RectTransform), typeof(TaffyLayoutGroup));
            RectTransform root = rootObject.GetComponent<RectTransform>();
            ConfigureRect(root, 300f, 100f);
            TaffyLayoutGroup group = rootObject.GetComponent<TaffyLayoutGroup>();
            group.alignItems = TaffyAlign.Start;
            RectTransform first = CreateItem(root, "First", 50f, 20f);
            RectTransform second = CreateItem(root, "Second", 50f, 20f);

            LayoutRebuilder.ForceRebuildLayoutImmediate(root);
            float initialSecondLeft = Left(second);
            Assert.That(initialSecondLeft, Is.EqualTo(50f).Within(0.1f));

            group.horizontalGap = 40f;
            MethodInfo callback = typeof(TaffyLayoutGroup).GetMethod(
                "OnDidApplyAnimationProperties",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(callback, Is.Not.Null);
            callback.Invoke(group, null);

            yield return null;
            LayoutRebuilder.ForceRebuildLayoutImmediate(root);
            Assert.That(group.horizontalGap, Is.EqualTo(40f).Within(0.01f));
            Assert.That(Left(second), Is.EqualTo(90f).Within(0.1f));
            Assert.That(first.rect.width, Is.EqualTo(50f).Within(0.05f));

            Object.Destroy(rootObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ScrollRectContentExpandsWhenRuntimeChildrenChangeWithoutRebuildLoop()
        {
            var scrollObject = new GameObject("Scroll", typeof(RectTransform), typeof(ScrollRect));
            RectTransform viewport = scrollObject.GetComponent<RectTransform>();
            ConfigureRect(viewport, 120f, 100f);
            ScrollRect scroll = scrollObject.GetComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.viewport = viewport;

            RectTransform content = CreateRoot("Content", viewport, 120f, 100f, out TaffyLayoutGroup group);
            scroll.content = content;
            group.direction = TaffyFlexDirection.Column;
            group.alignItems = TaffyAlign.Start;
            CreateItem(content, "One", 100f, 70f);

            yield return null;
            LayoutRebuilder.ForceRebuildLayoutImmediate(content);
            Assert.That(content.rect.height, Is.EqualTo(100f).Within(0.1f));

            CreateItem(content, "Two", 100f, 70f);
            group.SetLayoutDirty();
            yield return null;
            LayoutRebuilder.ForceRebuildLayoutImmediate(content);
            Assert.That(content.rect.height, Is.EqualTo(140f).Within(0.1f));

            yield return null;
            LayoutRebuilder.ForceRebuildLayoutImmediate(content);
            Assert.That(content.rect.height, Is.EqualTo(140f).Within(0.1f));

            Object.Destroy(scrollObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator RuntimeSafeAreaOverridesCanChangeAndClearAcrossFrames()
        {
            var rootObject = new GameObject("SafeRuntime", typeof(RectTransform), typeof(TaffyLayoutGroup));
            RectTransform root = rootObject.GetComponent<RectTransform>();
            ConfigureRect(root, 240f, 100f);
            TaffyLayoutGroup group = rootObject.GetComponent<TaffyLayoutGroup>();
            group.alignItems = TaffyAlign.Start;
            group.safeAreaMode = TaffySafeAreaMode.Padding;
            RectTransform child = CreateItem(root, "Child", 40f, 20f);

            group.SetRuntimeSafeAreaInsets(new TaffyPixelInsets(10f, 0f, 0f, 0f));
            yield return null;
            LayoutRebuilder.ForceRebuildLayoutImmediate(root);
            Assert.That(Left(child), Is.EqualTo(10f).Within(0.1f));

            group.SetRuntimeSafeAreaInsets(new TaffyPixelInsets(25f, 0f, 0f, 0f));
            yield return null;
            LayoutRebuilder.ForceRebuildLayoutImmediate(root);
            Assert.That(Left(child), Is.EqualTo(25f).Within(0.1f));

            group.ClearRuntimeOverrides();
            group.safeAreaMode = TaffySafeAreaMode.Disabled;
            yield return null;
            LayoutRebuilder.ForceRebuildLayoutImmediate(root);
            Assert.That(Left(child), Is.EqualTo(0f).Within(0.1f));

            Object.Destroy(rootObject);
            yield return null;
        }

        private static RectTransform CreateRoot(string name, Transform parent, float width, float height, out TaffyLayoutGroup group)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TaffyLayoutGroup));
            RectTransform rect = go.GetComponent<RectTransform>();
            if (parent)
                rect.SetParent(parent, false);
            ConfigureRect(rect, width, height);
            group = go.GetComponent<TaffyLayoutGroup>();
            return rect;
        }

        private static RectTransform CreateItem(RectTransform parent, string name, float width, float height)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TaffyLayoutItem));
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            TaffyLayoutItem item = go.GetComponent<TaffyLayoutItem>();
            item.width = TaffyLength.Points(width);
            item.height = TaffyLength.Points(height);
            return rect;
        }

        private static void ConfigureRect(RectTransform rect, float width, float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = new Vector2(width, height);
        }

        private static float Left(RectTransform rect)
        {
            return rect.anchoredPosition.x - rect.rect.width * rect.pivot.x;
        }

        private static float Top(RectTransform rect)
        {
            return -(rect.anchoredPosition.y + rect.rect.height * (1f - rect.pivot.y));
        }
    }
}
