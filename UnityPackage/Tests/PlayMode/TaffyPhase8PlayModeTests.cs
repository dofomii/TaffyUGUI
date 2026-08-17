using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TaffyUGUI.Tests
{
    public sealed class TaffyPhase8PlayModeTests
    {
        [UnityTest]
        public IEnumerator RuntimeTextContentFontSizeAndStyleInvalidateMeasurement()
        {
            var rootObject = new GameObject("Phase8RuntimeRoot", typeof(RectTransform), typeof(TaffyLayoutGroup));
            RectTransform root = rootObject.GetComponent<RectTransform>();
            root.anchorMin = new Vector2(0f, 1f);
            root.anchorMax = new Vector2(0f, 1f);
            root.pivot = new Vector2(0f, 1f);
            root.sizeDelta = new Vector2(600f, 140f);

            TaffyLayoutGroup group = rootObject.GetComponent<TaffyLayoutGroup>();
            group.alignItems = TaffyAlign.Start;

            var textObject = new GameObject("RuntimeText", typeof(RectTransform), typeof(Text));
            RectTransform child = textObject.GetComponent<RectTransform>();
            child.SetParent(root, false);
            Text text = textObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 16;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.text = "Runtime";

            yield return null;
            LayoutRebuilder.ForceRebuildLayoutImmediate(root);
            float initialWidth = child.rect.width;
            float initialHeight = child.rect.height;

            text.text = "Runtime measurement changed across frames";
            text.fontSize = 28;
            text.fontStyle = FontStyle.Bold;
            yield return null;
            LayoutRebuilder.ForceRebuildLayoutImmediate(root);

            Assert.That(child.rect.width, Is.GreaterThan(initialWidth + 20f));
            Assert.That(child.rect.height, Is.GreaterThanOrEqualTo(initialHeight));

            Object.Destroy(rootObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator CachedProviderIsNotReenteredByRepeatedAxisApplication()
        {
            var rootObject = new GameObject("Phase8ProviderRoot", typeof(RectTransform), typeof(TaffyLayoutGroup));
            RectTransform root = rootObject.GetComponent<RectTransform>();
            root.anchorMin = new Vector2(0f, 1f);
            root.anchorMax = new Vector2(0f, 1f);
            root.pivot = new Vector2(0f, 1f);
            root.sizeDelta = new Vector2(260f, 100f);

            TaffyLayoutGroup group = rootObject.GetComponent<TaffyLayoutGroup>();
            group.alignItems = TaffyAlign.Start;

            var childObject = new GameObject("Measured", typeof(RectTransform), typeof(PlayModeCountingMeasurementProvider));
            childObject.transform.SetParent(root, false);
            PlayModeCountingMeasurementProvider provider = childObject.GetComponent<PlayModeCountingMeasurementProvider>();

            yield return null;
            LayoutRebuilder.ForceRebuildLayoutImmediate(root);
            int callsAfterWarmup = provider.calls;
            Assert.That(callsAfterWarmup, Is.GreaterThan(0));

            group.SetLayoutHorizontal();
            group.SetLayoutVertical();
            group.SetLayoutHorizontal();
            group.SetLayoutVertical();
            Assert.That(provider.calls, Is.EqualTo(callsAfterWarmup));

            Object.Destroy(rootObject);
            yield return null;
        }
    }

    public sealed class PlayModeCountingMeasurementProvider : MonoBehaviour, ITaffyMeasurementProvider
    {
        public int calls;
        public int MeasurementVersion => 1;

        public bool TryGetTaffyMeasurement(float availableWidth, out TaffyMeasurementData measurement)
        {
            calls++;
            measurement = new TaffyMeasurementData
            {
                minContent = new Vector2(40f, 20f),
                maxContent = new Vector2(100f, 20f),
                preferred = new Vector2(100f, 20f),
                samples = new[]
                {
                    new TaffyMeasurementSample(100f, new Vector2(100f, 20f)),
                    new TaffyMeasurementSample(260f, new Vector2(100f, 20f)),
                },
            };
            return true;
        }
    }
}
