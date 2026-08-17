using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace TaffyUGUI.Tests
{
    public sealed class TaffyLayoutGroupEditModeTests
    {
        private GameObject _root;

        [TearDown]
        public void TearDown()
        {
            if (_root)
                Object.DestroyImmediate(_root);
        }

        [Test]
        public void ReportsIntrinsicMinAndPreferredSize()
        {
            RectTransform rootRect = CreateRoot(400f, 200f, out TaffyLayoutGroup group);
            group.direction = TaffyFlexDirection.Row;
            group.horizontalGap = 10f;
            group.padding = new RectOffset(5, 5, 5, 5);
            group.alignItems = TaffyAlign.Start;

            CreateChild(rootRect, "A", 40f, 20f, 20f, 10f);
            CreateChild(rootRect, "B", 60f, 20f, 30f, 10f);

            group.CalculateLayoutInputHorizontal();
            group.CalculateLayoutInputVertical();

            Assert.That(group.minWidth, Is.EqualTo(70f).Within(0.01f));
            Assert.That(group.preferredWidth, Is.EqualTo(120f).Within(0.01f));
            Assert.That(group.minHeight, Is.EqualTo(20f).Within(0.01f));
            Assert.That(group.preferredHeight, Is.EqualTo(30f).Within(0.01f));
            Assert.That(group.flexibleWidth, Is.EqualTo(0f).Within(0.01f));
            Assert.That(group.flexibleHeight, Is.EqualTo(0f).Within(0.01f));
        }

        [Test]
        public void ReusesTopologyAcrossReorderAndSameCountReplacement()
        {
            RectTransform rootRect = CreateRoot(300f, 100f, out TaffyLayoutGroup group);
            group.direction = TaffyFlexDirection.Row;
            group.horizontalGap = 10f;
            group.alignItems = TaffyAlign.Start;

            RectTransform a = CreateChild(rootRect, "A", 50f, 20f);
            RectTransform b = CreateChild(rootRect, "B", 70f, 20f);
            Force(rootRect);

            Assert.That(Left(a), Is.EqualTo(0f).Within(0.01f));
            Assert.That(Left(b), Is.EqualTo(60f).Within(0.01f));

            b.SetSiblingIndex(0);
            Force(rootRect);
            Assert.That(Left(b), Is.EqualTo(0f).Within(0.01f));
            Assert.That(Left(a), Is.EqualTo(80f).Within(0.01f));

            Object.DestroyImmediate(a.gameObject);
            RectTransform c = CreateChild(rootRect, "C", 30f, 20f);
            Force(rootRect);

            Assert.That(Left(b), Is.EqualTo(0f).Within(0.01f));
            Assert.That(Left(c), Is.EqualTo(80f).Within(0.01f));
            Assert.That(c.rect.width, Is.EqualTo(30f).Within(0.01f));
        }

        [Test]
        public void PreservesIgnoreLayoutAndLayoutElementSizing()
        {
            RectTransform rootRect = CreateRoot(300f, 100f, out TaffyLayoutGroup group);
            group.direction = TaffyFlexDirection.Row;
            group.horizontalGap = 10f;
            group.alignItems = TaffyAlign.Start;

            RectTransform controlled = CreateChild(rootRect, "Controlled", 80f, 25f);
            RectTransform ignored = CreateChild(rootRect, "Ignored", 40f, 40f);
            LayoutElement ignoredElement = ignored.GetComponent<LayoutElement>();
            ignoredElement.ignoreLayout = true;
            ignored.anchoredPosition = new Vector2(123f, -17f);

            Force(rootRect);

            Assert.That(controlled.rect.width, Is.EqualTo(80f).Within(0.01f));
            Assert.That(controlled.rect.height, Is.EqualTo(25f).Within(0.01f));
            Assert.That(ignored.rect.width, Is.EqualTo(100f).Within(0.01f));
            Assert.That(ignored.anchoredPosition.x, Is.EqualTo(123f).Within(0.01f));
            Assert.That(ignored.anchoredPosition.y, Is.EqualTo(-17f).Within(0.01f));
        }

        [Test]
        public void SupportsNestedGroupsAndContextRecreation()
        {
            RectTransform rootRect = CreateRoot(300f, 160f, out TaffyLayoutGroup group);
            group.direction = TaffyFlexDirection.Row;
            group.alignItems = TaffyAlign.Start;

            var nestedObject = new GameObject("Nested", typeof(RectTransform), typeof(TaffyLayoutGroup));
            RectTransform nestedRect = nestedObject.GetComponent<RectTransform>();
            nestedRect.SetParent(rootRect, false);
            TaffyLayoutGroup nested = nestedObject.GetComponent<TaffyLayoutGroup>();
            nested.direction = TaffyFlexDirection.Column;
            nested.alignItems = TaffyAlign.Start;
            CreateChild(nestedRect, "NestedChild", 90f, 35f);

            Force(rootRect);
            Assert.That(nestedRect.rect.width, Is.EqualTo(90f).Within(0.01f));
            Assert.That(nestedRect.rect.height, Is.EqualTo(35f).Within(0.01f));
            Assert.That(nestedRect.GetChild(0).GetComponent<RectTransform>().rect.width, Is.EqualTo(90f).Within(0.01f));

            nested.enabled = false;
            nested.enabled = true;
            group.enabled = false;
            group.enabled = true;
            Force(rootRect);

            Assert.That(nestedRect.rect.width, Is.EqualTo(90f).Within(0.01f));
            Assert.That(nestedRect.GetChild(0).GetComponent<RectTransform>().rect.height, Is.EqualTo(35f).Within(0.01f));
        }

        private RectTransform CreateRoot(float width, float height, out TaffyLayoutGroup group)
        {
            _root = new GameObject("Root", typeof(RectTransform), typeof(TaffyLayoutGroup));
            RectTransform rect = _root.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = new Vector2(width, height);
            group = _root.GetComponent<TaffyLayoutGroup>();
            return rect;
        }

        private static RectTransform CreateChild(
            RectTransform parent,
            string name,
            float preferredWidth,
            float preferredHeight,
            float minWidth = 0f,
            float minHeight = 0f)
        {
            var childObject = new GameObject(name, typeof(RectTransform), typeof(LayoutElement));
            RectTransform rect = childObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(100f, 100f);

            LayoutElement element = childObject.GetComponent<LayoutElement>();
            element.minWidth = minWidth;
            element.minHeight = minHeight;
            element.preferredWidth = preferredWidth;
            element.preferredHeight = preferredHeight;
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
    }
}
