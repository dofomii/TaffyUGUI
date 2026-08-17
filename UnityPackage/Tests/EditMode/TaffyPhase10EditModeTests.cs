using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace TaffyUGUI.Tests
{
    public sealed class TaffyPhase10EditModeTests
    {
        private readonly List<GameObject> _owned = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            for (int i = _owned.Count - 1; i >= 0; i--)
            {
                if (_owned[i])
                    Object.DestroyImmediate(_owned[i]);
            }
            _owned.Clear();
        }

        [Test]
        public void ResponsiveProfilesSerializeValidateSwitchAndSupportRuntimeOverride()
        {
            RectTransform root = CreateRoot("Responsive", 300f, 160f, out TaffyLayoutGroup group);
            group.alignItems = TaffyAlign.Start;
            RectTransform a = CreateItem(root, "A", 50f, 20f, out _);
            RectTransform b = CreateItem(root, "B", 50f, 20f, out _);

            var narrow = new TaffyResponsiveProfile
            {
                name = "narrow",
                priority = 10,
                maxWidth = 350f,
                overrideFlexDirection = true,
                direction = TaffyFlexDirection.Column,
                overrideGaps = true,
                verticalGap = 10f,
            };
            var wide = new TaffyResponsiveProfile
            {
                name = "wide",
                priority = 10,
                minWidth = 351f,
                overrideFlexDirection = true,
                direction = TaffyFlexDirection.Row,
                overrideGaps = true,
                horizontalGap = 20f,
            };
            group.responsiveProfiles.Add(narrow);
            group.responsiveProfiles.Add(wide);

            string json = JsonUtility.ToJson(narrow);
            TaffyResponsiveProfile roundTrip = JsonUtility.FromJson<TaffyResponsiveProfile>(json);
            Assert.That(roundTrip.name, Is.EqualTo("narrow"));
            Assert.That(roundTrip.overrideFlexDirection, Is.True);
            Assert.That(roundTrip.direction, Is.EqualTo(TaffyFlexDirection.Column));
            Assert.That(group.ValidateResponsiveProfiles(out string validationError), Is.True, validationError);

            Force(root);
            Assert.That(group.ActiveResponsiveProfileName, Is.EqualTo("narrow"));
            Assert.That(Top(b), Is.EqualTo(30f).Within(0.1f));
            Assert.That(Left(b), Is.EqualTo(0f).Within(0.1f));

            root.sizeDelta = new Vector2(500f, 160f);
            Force(root);
            Assert.That(group.ActiveResponsiveProfileName, Is.EqualTo("wide"));
            Assert.That(Left(b), Is.EqualTo(70f).Within(0.1f));
            Assert.That(Top(b), Is.EqualTo(0f).Within(0.1f));

            Assert.That(group.SetRuntimeResponsiveProfile("narrow", out string runtimeError), Is.True, runtimeError);
            Force(root);
            Assert.That(group.ActiveResponsiveProfileName, Is.EqualTo("narrow"));
            Assert.That(Top(b), Is.EqualTo(30f).Within(0.1f));

            group.ClearRuntimeResponsiveProfile();
            Force(root);
            Assert.That(group.ActiveResponsiveProfileName, Is.EqualTo("wide"));
            Assert.That(a.rect.width, Is.EqualTo(50f).Within(0.05f));
        }

        [Test]
        public void ResponsiveValidationRejectsDuplicateNamesAndInvalidRanges()
        {
            RectTransform root = CreateRoot("Validation", 300f, 100f, out TaffyLayoutGroup group);
            group.responsiveProfiles.Add(new TaffyResponsiveProfile { name = "phone", minWidth = 100f, maxWidth = 50f });
            Assert.That(group.ValidateResponsiveProfiles(out string rangeError), Is.False);
            StringAssert.Contains("maxWidth", rangeError);

            group.responsiveProfiles.Clear();
            group.responsiveProfiles.Add(new TaffyResponsiveProfile { name = "same" });
            group.responsiveProfiles.Add(new TaffyResponsiveProfile { name = "same" });
            Assert.That(group.ValidateResponsiveProfiles(out string duplicateError), Is.False);
            StringAssert.Contains("duplicated", duplicateError);
            Assert.That(root, Is.Not.Null);
        }

        [Test]
        public void RuntimeSafeAreaOverrideAddsPaddingWithoutChangingSerializedPadding()
        {
            RectTransform root = CreateRoot("SafeArea", 220f, 120f, out TaffyLayoutGroup group);
            group.alignItems = TaffyAlign.Start;
            group.safeAreaMode = TaffySafeAreaMode.Padding;
            group.padding = new RectOffset(3, 4, 5, 6);
            RectTransform child = CreateItem(root, "Child", 40f, 20f, out _);

            group.SetRuntimeSafeAreaInsets(new TaffyPixelInsets(12f, 7f, 8f, 9f));
            Force(root);
            Assert.That(Left(child), Is.EqualTo(15f).Within(0.1f));
            Assert.That(Top(child), Is.EqualTo(13f).Within(0.1f));
            Assert.That(group.padding.left, Is.EqualTo(3));
            Assert.That(group.padding.top, Is.EqualTo(5));

            group.SetRuntimeSafeAreaInsets(TaffyPixelInsets.Zero);
            Force(root);
            Assert.That(Left(child), Is.EqualTo(3f).Within(0.1f));
            Assert.That(Top(child), Is.EqualTo(5f).Within(0.1f));

            group.ClearRuntimeOverrides();
            group.safeAreaMode = TaffySafeAreaMode.Disabled;
            Force(root);
            Assert.That(Left(child), Is.EqualTo(3f).Within(0.1f));
        }

        [Test]
        public void ScrollRectBridgeExpandsContentAndContentSizeFitterOwnsConfiguredAxis()
        {
            RectTransform viewport = CreateRect("Scroll", 120f, 100f, typeof(ScrollRect));
            ScrollRect scroll = viewport.GetComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;

            RectTransform content = CreateRect("Content", 120f, 100f, typeof(TaffyLayoutGroup));
            content.SetParent(viewport, false);
            scroll.content = content;
            scroll.viewport = viewport;
            TaffyLayoutGroup group = content.GetComponent<TaffyLayoutGroup>();
            group.direction = TaffyFlexDirection.Column;
            group.alignItems = TaffyAlign.Start;
            CreateItem(content, "One", 100f, 80f, out _);
            CreateItem(content, "Two", 100f, 80f, out _);

            Force(content);
            Assert.That(content.rect.height, Is.EqualTo(160f).Within(0.1f));
            Assert.That(content.rect.width, Is.EqualTo(120f).Within(0.1f));

            ContentSizeFitter fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            string[] warnings = group.GetIntegrationWarnings();
            Assert.That(warnings.Any(x => x.Contains("Vertical ScrollRect content sizing is owned by ContentSizeFitter")), Is.True);
        }

        [Test]
        public void AspectRatioFitterFeedsNativeAspectAndUnsafeParentModesAreDiagnosed()
        {
            RectTransform root = CreateRoot("Aspect", 240f, 120f, out TaffyLayoutGroup group);
            group.alignItems = TaffyAlign.Start;
            RectTransform child = CreateItem(root, "AspectChild", 60f, 0f, out TaffyLayoutItem item);
            item.height = TaffyLength.Auto;
            item.aspectRatio = 0f;
            AspectRatioFitter fitter = child.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.WidthControlsHeight;
            fitter.aspectRatio = 2f;

            Force(root);
            Assert.That(child.rect.width, Is.EqualTo(60f).Within(0.1f));
            Assert.That(child.rect.height, Is.EqualTo(30f).Within(0.1f));

            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            string[] warnings = group.GetIntegrationWarnings();
            Assert.That(warnings.Any(x => x.Contains("FitInParent")), Is.True);
        }

        [Test]
        public void PixelRoundingRoundsEdgesRatherThanAccumulatingIndependentSizes()
        {
            RectTransform root = CreateRoot("Rounding", 200f, 100f, out TaffyLayoutGroup group);
            group.alignItems = TaffyAlign.Start;
            group.pixelRounding = TaffyPixelRounding.Round;
            RectTransform first = CreateItem(root, "First", 33.4f, 20f, out _);
            RectTransform second = CreateItem(root, "Second", 33.4f, 20f, out _);

            Force(root);
            Assert.That(Left(first), Is.EqualTo(0f).Within(0.01f));
            Assert.That(first.rect.width, Is.EqualTo(33f).Within(0.01f));
            Assert.That(Left(second), Is.EqualTo(33f).Within(0.01f));
            Assert.That(second.rect.width, Is.EqualTo(34f).Within(0.01f));
        }

        [Test]
        public void RebuildGuardSuppressesExcessSameFrameDirtyRequests()
        {
            RectTransform root = CreateRoot("Guard", 200f, 100f, out TaffyLayoutGroup group);
            group.maxRebuildRequestsPerFrame = 2;
            group.ResetRebuildDiagnostics();

            for (int i = 0; i < 12; i++)
                group.SetLayoutDirty();

            Assert.That(group.SuppressedRebuildRequestCount, Is.GreaterThanOrEqualTo(10));
            Force(root);
        }

        private RectTransform CreateRoot(string name, float width, float height, out TaffyLayoutGroup group)
        {
            RectTransform rect = CreateRect(name, width, height, typeof(TaffyLayoutGroup));
            group = rect.GetComponent<TaffyLayoutGroup>();
            return rect;
        }

        private RectTransform CreateRect(string name, float width, float height, params System.Type[] components)
        {
            var types = new List<System.Type> { typeof(RectTransform) };
            types.AddRange(components);
            var go = new GameObject(name, types.ToArray());
            _owned.Add(go);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = new Vector2(width, height);
            return rect;
        }

        private static RectTransform CreateItem(RectTransform parent, string name, float width, float height, out TaffyLayoutItem item)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TaffyLayoutItem));
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            item = go.GetComponent<TaffyLayoutItem>();
            item.width = width > 0f ? TaffyLength.Points(width) : TaffyLength.Auto;
            item.height = height > 0f ? TaffyLength.Points(height) : TaffyLength.Auto;
            return rect;
        }

        private static void Force(RectTransform root)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(root);
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
