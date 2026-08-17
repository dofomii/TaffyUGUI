using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TaffyUGUI.Tests
{
    public sealed class TaffyLayoutGroupPlayModeTests
    {
        [UnityTest]
        public IEnumerator RuntimeResizeAndEnableDisableRemainStable()
        {
            var rootObject = new GameObject("RuntimeRoot", typeof(RectTransform), typeof(TaffyLayoutGroup));
            RectTransform root = rootObject.GetComponent<RectTransform>();
            root.anchorMin = new Vector2(0f, 1f);
            root.anchorMax = new Vector2(0f, 1f);
            root.pivot = new Vector2(0f, 1f);
            root.sizeDelta = new Vector2(200f, 80f);

            TaffyLayoutGroup group = rootObject.GetComponent<TaffyLayoutGroup>();
            group.direction = TaffyFlexDirection.Row;
            group.alignItems = TaffyAlign.Start;

            var childObject = new GameObject("Flexible", typeof(RectTransform), typeof(LayoutElement));
            RectTransform child = childObject.GetComponent<RectTransform>();
            child.SetParent(root, false);
            LayoutElement element = childObject.GetComponent<LayoutElement>();
            element.preferredWidth = 50f;
            element.preferredHeight = 30f;
            element.flexibleWidth = 1f;

            yield return null;
            LayoutRebuilder.ForceRebuildLayoutImmediate(root);
            float firstWidth = child.rect.width;
            Assert.That(firstWidth, Is.EqualTo(200f).Within(0.01f));
            Assert.That(child.rect.height, Is.EqualTo(30f).Within(0.01f));

            root.sizeDelta = new Vector2(320f, 80f);
            yield return null;
            LayoutRebuilder.ForceRebuildLayoutImmediate(root);
            Assert.That(child.rect.width, Is.EqualTo(320f).Within(0.01f));

            group.enabled = false;
            yield return null;
            group.enabled = true;
            yield return null;
            LayoutRebuilder.ForceRebuildLayoutImmediate(root);
            Assert.That(child.rect.width, Is.EqualTo(320f).Within(0.01f));
            Assert.That(child.rect.height, Is.EqualTo(30f).Within(0.01f));

            Object.Destroy(rootObject);
            yield return null;
        }
    }
}
