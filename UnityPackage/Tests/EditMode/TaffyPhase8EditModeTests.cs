using System.Collections.Generic;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TaffyUGUI.Tests
{
    public sealed class TaffyPhase8EditModeTests
    {
        private readonly List<Object> _owned = new List<Object>();
        private GameObject _root;

        [TearDown]
        public void TearDown()
        {
            if (_root)
                Object.DestroyImmediate(_root);
            for (int i = 0; i < _owned.Count; i++)
            {
                if (_owned[i])
                    Object.DestroyImmediate(_owned[i]);
            }
            _owned.Clear();
        }

        [Test]
        public void FlexGrowAndWrapAuthoringDriveGeometry()
        {
            RectTransform root = CreateRoot(200f, 120f, out TaffyLayoutGroup group);
            group.direction = TaffyFlexDirection.Row;
            group.alignItems = TaffyAlign.Start;

            RectTransform a = CreateItem(root, "A", 50f, 20f, out TaffyLayoutItem itemA);
            RectTransform b = CreateItem(root, "B", 50f, 20f, out TaffyLayoutItem itemB);
            itemA.flexGrow = 1f;
            itemB.flexGrow = 1f;
            Force(root);

            Assert.That(a.rect.width, Is.EqualTo(100f).Within(0.05f));
            Assert.That(b.rect.width, Is.EqualTo(100f).Within(0.05f));

            group.wrap = TaffyFlexWrap.Wrap;
            itemA.flexGrow = 0f;
            itemB.flexGrow = 0f;
            itemA.width = TaffyLength.Points(110f);
            itemB.width = TaffyLength.Points(110f);
            group.SetLayoutDirty();
            Force(root);

            Assert.That(Top(b), Is.GreaterThan(Top(a) + 0.01f));
        }

        [Test]
        public void AbsoluteInsetsAndAspectAreAuthored()
        {
            RectTransform root = CreateRoot(240f, 140f, out TaffyLayoutGroup group);
            group.alignItems = TaffyAlign.Start;

            RectTransform child = CreateItem(root, "Absolute", 0f, 0f, out TaffyLayoutItem item);
            item.position = TaffyPosition.Absolute;
            item.inset = TaffyEdges.Auto;
            item.inset.left = TaffyLength.Points(20f);
            item.inset.top = TaffyLength.Points(10f);
            item.width = TaffyLength.Points(60f);
            item.height = TaffyLength.Auto;
            item.aspectRatio = 2f;
            group.SetLayoutDirty();
            Force(root);

            Assert.That(Left(child), Is.EqualTo(20f).Within(0.05f));
            Assert.That(Top(child), Is.EqualTo(10f).Within(0.05f));
            Assert.That(child.rect.width, Is.EqualTo(60f).Within(0.05f));
            Assert.That(child.rect.height, Is.EqualTo(30f).Within(0.1f));
        }

        [Test]
        public void ContentBoxPaddingAndBorderExpandExplicitSize()
        {
            RectTransform root = CreateRoot(240f, 140f, out TaffyLayoutGroup group);
            group.alignItems = TaffyAlign.Start;
            RectTransform child = CreateItem(root, "Box", 40f, 20f, out TaffyLayoutItem item);
            item.boxSizing = TaffyBoxSizing.ContentBox;
            item.padding = TaffyEdges.Points(5f);
            item.border = TaffyEdges.Points(2f);
            group.SetLayoutDirty();
            Force(root);

            Assert.That(child.rect.width, Is.EqualTo(54f).Within(0.1f));
            Assert.That(child.rect.height, Is.EqualTo(34f).Within(0.1f));
        }

        [Test]
        public void FlowRootFloatAndClearAreApplied()
        {
            RectTransform root = CreateRoot(220f, 140f, out TaffyLayoutGroup group);
            group.containerDisplay = TaffyContainerDisplay.FlowRoot;

            RectTransform floating = CreateItem(root, "Float", 60f, 30f, out TaffyLayoutItem floatItem);
            floatItem.display = TaffyDisplay.Block;
            floatItem.floatMode = TaffyFloat.Left;

            RectTransform cleared = CreateItem(root, "Clear", 100f, 20f, out TaffyLayoutItem clearItem);
            clearItem.display = TaffyDisplay.Block;
            clearItem.clearMode = TaffyClear.Both;
            group.SetLayoutDirty();
            Force(root);

            Assert.That(floating.rect.width, Is.EqualTo(60f).Within(0.05f));
            Assert.That(Top(cleared), Is.GreaterThanOrEqualTo(Top(floating) + floating.rect.height - 0.05f));
        }

        [Test]
        public void CustomMeasurementIsCachedAndExplicitlyInvalidatable()
        {
            RectTransform root = CreateRoot(240f, 100f, out TaffyLayoutGroup group);
            var childObject = new GameObject("Measured", typeof(RectTransform), typeof(CountingMeasurementProvider));
            RectTransform child = childObject.GetComponent<RectTransform>();
            child.SetParent(root, false);
            CountingMeasurementProvider provider = childObject.GetComponent<CountingMeasurementProvider>();
            provider.preferred = new Vector2(80f, 24f);
            provider.minimum = new Vector2(30f, 18f);

            Force(root);
            int firstPassCalls = provider.calls;
            Assert.That(firstPassCalls, Is.GreaterThan(0));
            Assert.That(child.rect.width, Is.GreaterThan(0f));

            Force(root);
            Assert.That(provider.calls, Is.EqualTo(firstPassCalls), "same intrinsic and arranged width signatures should hit the managed cache");

            provider.preferred = new Vector2(120f, 24f);
            provider.version++;
            group.SetLayoutDirty();
            Force(root);
            Assert.That(provider.calls, Is.GreaterThan(firstPassCalls));
            Assert.That(child.rect.width, Is.GreaterThanOrEqualTo(100f));

            int callsBeforeExplicitInvalidation = provider.calls;
            group.InvalidateMeasurement(child);
            Force(root);
            Assert.That(provider.calls, Is.GreaterThan(callsBeforeExplicitInvalidation));
        }

        [Test]
        public void UnityTextMeasurementRespondsToTextChanges()
        {
            RectTransform root = CreateRoot(500f, 120f, out TaffyLayoutGroup group);
            group.alignItems = TaffyAlign.Start;

            var textObject = new GameObject("Text", typeof(RectTransform), typeof(Text));
            RectTransform child = textObject.GetComponent<RectTransform>();
            child.SetParent(root, false);
            Text text = textObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 20;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.text = "Hi";

            Force(root);
            float shortWidth = child.rect.width;
            text.text = "A considerably longer line of text";
            Force(root);
            float longWidth = child.rect.width;

            Assert.That(shortWidth, Is.GreaterThan(0f));
            Assert.That(longWidth, Is.GreaterThan(shortWidth + 10f));
        }

        [Test]
        public void ImageMeasurementUsesReplacedElementIntrinsicSize()
        {
            RectTransform root = CreateRoot(300f, 120f, out TaffyLayoutGroup group);
            group.alignItems = TaffyAlign.Start;

            Texture2D texture = new Texture2D(64, 32);
            _owned.Add(texture);
            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, 64f, 32f), new Vector2(0.5f, 0.5f), 100f);
            _owned.Add(sprite);

            var imageObject = new GameObject("Image", typeof(RectTransform), typeof(Image));
            RectTransform child = imageObject.GetComponent<RectTransform>();
            child.SetParent(root, false);
            Image image = imageObject.GetComponent<Image>();
            image.sprite = sprite;

            Force(root);
            Assert.That(child.rect.width, Is.EqualTo(64f).Within(0.1f));
            Assert.That(child.rect.height, Is.EqualTo(32f).Within(0.1f));
        }

        [Test]
        public void TextMeshProMeasurementAdapterProducesIntrinsicGeometry()
        {
            RectTransform root = CreateRoot(500f, 140f, out TaffyLayoutGroup group);
            group.alignItems = TaffyAlign.Start;

            TMP_FontAsset fontAsset = Resources.Load<TMP_FontAsset>("Fonts & Materials/Inter-Regular SDF");
            Assert.That(fontAsset, Is.Not.Null, "Phase 8 TMP validation requires a test-host TMP font resource.");

            var textObject = new GameObject("TMP", typeof(RectTransform), typeof(TextMeshProUGUI));
            RectTransform child = textObject.GetComponent<RectTransform>();
            child.SetParent(root, false);
            TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
            text.font = fontAsset;
            text.fontSize = 22f;
            text.text = "TextMeshPro measurement";

            Force(root);
            Assert.That(child.rect.width, Is.GreaterThan(40f));
            Assert.That(child.rect.height, Is.GreaterThan(10f));
        }

        [Test]
        public void MinAndMaxConstraintsClampAuthoredSizes()
        {
            RectTransform root = CreateRoot(300f, 100f, out TaffyLayoutGroup group);
            group.alignItems = TaffyAlign.Start;
            RectTransform child = CreateItem(root, "Clamped", 200f, 20f, out TaffyLayoutItem item);
            item.maxWidth = TaffyLength.Points(80f);
            group.SetLayoutDirty();
            Force(root);
            Assert.That(child.rect.width, Is.EqualTo(80f).Within(0.05f));

            item.width = TaffyLength.Points(20f);
            item.maxWidth = TaffyLength.Auto;
            item.minWidth = TaffyLength.Points(60f);
            group.SetLayoutDirty();
            Force(root);
            Assert.That(child.rect.width, Is.EqualTo(60f).Within(0.05f));
        }

        [Test]
        public void RtlDirectionAndOverflowModesExecute()
        {
            RectTransform root = CreateRoot(200f, 100f, out TaffyLayoutGroup group);
            group.direction = TaffyFlexDirection.Row;
            group.writingDirection = TaffyWritingDirection.RightToLeft;
            group.alignItems = TaffyAlign.Start;
            RectTransform child = CreateItem(root, "RTL", 50f, 20f, out _);
            group.SetLayoutDirty();
            Force(root);
            float rtlLeft = Left(child);
            Assert.That(rtlLeft, Is.EqualTo(150f).Within(0.1f));

            group.overflowY = TaffyOverflow.Scroll;
            group.overflowX = TaffyOverflow.Hidden;
            group.scrollbarWidth = 10f;
            group.SetLayoutDirty();
            Force(root);
            Assert.That(Left(child), Is.EqualTo(rtlLeft).Within(0.1f));
            Assert.That(child.rect.width, Is.EqualTo(50f).Within(0.05f));
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

        private static RectTransform CreateItem(
            RectTransform parent,
            string name,
            float width,
            float height,
            out TaffyLayoutItem item)
        {
            var childObject = new GameObject(name, typeof(RectTransform), typeof(TaffyLayoutItem));
            RectTransform rect = childObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            item = childObject.GetComponent<TaffyLayoutItem>();
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

    public sealed class CountingMeasurementProvider : MonoBehaviour, ITaffyMeasurementProvider
    {
        public Vector2 minimum = new Vector2(20f, 10f);
        public Vector2 preferred = new Vector2(80f, 20f);
        public int version;
        public int calls;

        public int MeasurementVersion => version;

        public bool TryGetTaffyMeasurement(float availableWidth, out TaffyMeasurementData measurement)
        {
            calls++;
            float width = Mathf.Min(preferred.x, Mathf.Max(minimum.x, availableWidth));
            measurement = new TaffyMeasurementData
            {
                minContent = minimum,
                maxContent = preferred,
                preferred = preferred,
                samples = new[]
                {
                    new TaffyMeasurementSample(Mathf.Max(1f, width), new Vector2(width, preferred.y)),
                },
            };
            return true;
        }
    }
}
