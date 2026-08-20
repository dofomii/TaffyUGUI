using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TaffyUGUI
{
    [Serializable]
    public struct TaffyMeasurementSample
    {
        [Min(0)] public float availableWidth;
        public Vector2 size;

        public TaffyMeasurementSample(float availableWidth, Vector2 size)
        {
            this.availableWidth = Mathf.Max(0f, availableWidth);
            this.size = new Vector2(Mathf.Max(0f, size.x), Mathf.Max(0f, size.y));
        }
    }

    public struct TaffyMeasurementData
    {
        public Vector2 minContent;
        public Vector2 maxContent;
        public Vector2 preferred;
        public float aspectRatio;
        public bool isReplaced;
        public TaffyMeasurementSample[] samples;
    }

    /// <summary>
    /// Optional custom intrinsic measurement source. The provider is called before native layout
    /// computation; Taffy never calls managed code from inside its compute pass.
    /// Increment MeasurementVersion whenever the provider's intrinsic result changes.
    /// </summary>
    public interface ITaffyMeasurementProvider
    {
        int MeasurementVersion { get; }
        bool TryGetTaffyMeasurement(float availableWidth, out TaffyMeasurementData measurement);
    }
    internal static class TaffyMeasurementInvalidationHub
    {
        private static readonly HashSet<TaffyLayoutGroup> Groups = new HashSet<TaffyLayoutGroup>();
        private static bool _installed;

        internal static void Register(TaffyLayoutGroup group)
        {
            if (!group || !Groups.Add(group))
                return;
            if (_installed)
                return;

            Font.textureRebuilt += OnFontTextureRebuilt;
            TMPro_EventManager.TEXT_CHANGED_EVENT.Add(OnTmpTextChanged);
            TMPro_EventManager.FONT_PROPERTY_EVENT.Add(OnTmpFontPropertyChanged);
            _installed = true;
        }

        internal static void Unregister(TaffyLayoutGroup group)
        {
            if (group)
                Groups.Remove(group);
            if (!_installed || Groups.Count != 0)
                return;

            Font.textureRebuilt -= OnFontTextureRebuilt;
            TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(OnTmpTextChanged);
            TMPro_EventManager.FONT_PROPERTY_EVENT.Remove(OnTmpFontPropertyChanged);
            _installed = false;
        }

        private static void OnFontTextureRebuilt(Font font)
        {
            InvalidateAll();
        }

        private static void OnTmpTextChanged(UnityEngine.Object changed)
        {
            if (!(changed is TMP_Text text) || !text)
                return;
            Transform parent = text.transform.parent;
            TaffyLayoutGroup group = parent ? parent.GetComponentInParent<TaffyLayoutGroup>() : null;
            if (group)
                group.InvalidateMeasurement(text.rectTransform);
        }

        private static void OnTmpFontPropertyChanged(bool changed, UnityEngine.Object font)
        {
            if (changed)
                InvalidateAll();
        }

        private static void InvalidateAll()
        {
            if (Groups.Count == 0)
                return;
            var snapshot = new List<TaffyLayoutGroup>(Groups);
            for (int i = 0; i < snapshot.Count; i++)
            {
                if (snapshot[i])
                    snapshot[i].InvalidateMeasurement();
            }
        }
    }


    internal static class TaffyMeasurementResolver
    {
        private const float Unbounded = 100000f;
        private const int MaxSamples = 7;
        internal static bool TryGetSignature(RectTransform rect, float availableWidth, out int signature)
        {
            signature = 0;
            if (!rect)
                return false;

            MonoBehaviour[] behaviours = rect.GetComponents<MonoBehaviour>();
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (!(behaviours[i] is ITaffyMeasurementProvider provider) || !behaviours[i].isActiveAndEnabled)
                    continue;
                signature = HashCustomProvider(behaviours[i], provider, availableWidth);
                return true;
            }

            TMP_Text tmp = rect.GetComponent<TMP_Text>();
            if (tmp && tmp.isActiveAndEnabled && tmp.font)
            {
                signature = HashTmp(tmp, availableWidth);
                return true;
            }

            Text text = rect.GetComponent<Text>();
            if (text && text.isActiveAndEnabled && text.font)
            {
                signature = HashText(text, availableWidth);
                return true;
            }

            Image image = rect.GetComponent<Image>();
            if (image && image.isActiveAndEnabled && image.sprite)
            {
                signature = HashImage(image);
                return true;
            }

            RawImage rawImage = rect.GetComponent<RawImage>();
            if (rawImage && rawImage.isActiveAndEnabled && rawImage.texture)
            {
                signature = HashRawImage(rawImage);
                return true;
            }

            return false;
        }


        internal static bool TryResolve(
            RectTransform rect,
            float availableWidth,
            out TaffyMeasurementData measurement,
            out int signature)
        {
            measurement = default;
            signature = 0;
            if (!rect)
                return false;

            MonoBehaviour[] behaviours = rect.GetComponents<MonoBehaviour>();
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (!(behaviours[i] is ITaffyMeasurementProvider provider) || !behaviours[i].isActiveAndEnabled)
                    continue;

                if (!provider.TryGetTaffyMeasurement(FiniteWidth(availableWidth), out measurement))
                    continue;
                Sanitize(ref measurement);
                signature = HashCustomProvider(behaviours[i], provider, availableWidth);
                return true;
            }

            TMP_Text tmp = rect.GetComponent<TMP_Text>();
            if (tmp && tmp.isActiveAndEnabled && tmp.font)
            {
                measurement = MeasureTmp(tmp, availableWidth);
                signature = HashTmp(tmp, availableWidth);
                return true;
            }

            Text text = rect.GetComponent<Text>();
            if (text && text.isActiveAndEnabled && text.font)
            {
                measurement = MeasureText(text, availableWidth);
                signature = HashText(text, availableWidth);
                return true;
            }

            Image image = rect.GetComponent<Image>();
            if (image && image.isActiveAndEnabled && image.sprite)
            {
                measurement = MeasureImage(image);
                signature = HashImage(image);
                return true;
            }

            RawImage rawImage = rect.GetComponent<RawImage>();
            if (rawImage && rawImage.isActiveAndEnabled && rawImage.texture)
            {
                measurement = MeasureRawImage(rawImage);
                signature = HashRawImage(rawImage);
                return true;
            }

            return false;
        }

        internal static void Upload(ulong context, ulong node, TaffyMeasurementData data)
        {
            Sanitize(ref data);
            TaffyMeasurementSample[] managedSamples = data.samples ?? Array.Empty<TaffyMeasurementSample>();
            var nativeSamples = new TaffyNative.MeasurementSample[managedSamples.Length];
            for (int i = 0; i < managedSamples.Length; i++)
            {
                nativeSamples[i] = new TaffyNative.MeasurementSample
                {
                    availableWidth = Mathf.Max(0f, managedSamples[i].availableWidth),
                    width = Mathf.Max(0f, managedSamples[i].size.x),
                    height = Mathf.Max(0f, managedSamples[i].size.y),
                };
            }

            GCHandle pin = default;
            try
            {
                IntPtr samples = IntPtr.Zero;
                if (nativeSamples.Length > 0)
                {
                    pin = GCHandle.Alloc(nativeSamples, GCHandleType.Pinned);
                    samples = pin.AddrOfPinnedObject();
                }

                var native = new TaffyNative.Measurement
                {
                    minWidth = data.minContent.x,
                    minHeight = data.minContent.y,
                    maxWidth = data.maxContent.x,
                    maxHeight = data.maxContent.y,
                    preferredWidth = data.preferred.x,
                    preferredHeight = data.preferred.y,
                    aspectRatio = data.aspectRatio,
                    isReplaced = data.isReplaced ? (byte)1 : (byte)0,
                    samples = samples,
                    sampleCount = (uint)nativeSamples.Length,
                };
                TaffyNative.Check(TaffyNative.tu_node_set_measurement(context, node, ref native), "upload cached measurement");
            }
            finally
            {
                if (pin.IsAllocated)
                    pin.Free();
            }
        }

        private static TaffyMeasurementData MeasureTmp(TMP_Text text, float widthHint)
        {
            string value = text.text ?? string.Empty;
            Vector2 max = ClampSize(text.GetPreferredValues(value, Unbounded, Unbounded));
            float minWidth = MeasureLongestTokenTmp(text, value, max.x);
            Vector2 min = minWidth > 0f
                ? ClampSize(text.GetPreferredValues(value, minWidth, Unbounded))
                : Vector2.zero;
            min.x = Mathf.Min(max.x, Mathf.Max(0f, minWidth));

            float preferredWidth = FiniteWidth(widthHint);
            Vector2 preferred = preferredWidth > 0f && preferredWidth < Unbounded
                ? ClampSize(text.GetPreferredValues(value, preferredWidth, Unbounded))
                : max;

            return new TaffyMeasurementData
            {
                minContent = min,
                maxContent = max,
                preferred = preferred,
                aspectRatio = 0f,
                isReplaced = false,
                samples = BuildSamples(
                    width => ClampSize(text.GetPreferredValues(value, width, Unbounded)),
                    min.x,
                    max.x,
                    widthHint),
            };
        }

        private static TaffyMeasurementData MeasureText(Text text, float widthHint)
        {
            string value = text.text ?? string.Empty;
            Vector2 max = MeasureUnityTextAtWidth(text, value, Unbounded);
            float minWidth = MeasureLongestTokenText(text, value, max.x);
            Vector2 min = minWidth > 0f ? MeasureUnityTextAtWidth(text, value, minWidth) : Vector2.zero;
            min.x = Mathf.Min(max.x, Mathf.Max(0f, minWidth));

            float preferredWidth = FiniteWidth(widthHint);
            Vector2 preferred = preferredWidth > 0f && preferredWidth < Unbounded
                ? MeasureUnityTextAtWidth(text, value, preferredWidth)
                : max;

            return new TaffyMeasurementData
            {
                minContent = min,
                maxContent = max,
                preferred = preferred,
                isReplaced = false,
                samples = BuildSamples(
                    width => MeasureUnityTextAtWidth(text, value, width),
                    min.x,
                    max.x,
                    widthHint),
            };
        }

        private static TaffyMeasurementData MeasureImage(Image image)
        {
            Vector2 size = new Vector2(Mathf.Max(0f, image.preferredWidth), Mathf.Max(0f, image.preferredHeight));
            float ratio = size.y > 0f ? size.x / size.y : 0f;
            return new TaffyMeasurementData
            {
                minContent = size,
                maxContent = size,
                preferred = size,
                aspectRatio = ratio,
                isReplaced = true,
                samples = Array.Empty<TaffyMeasurementSample>(),
            };
        }

        private static TaffyMeasurementData MeasureRawImage(RawImage image)
        {
            Texture texture = image.texture;
            Rect uv = image.uvRect;
            Vector2 size = new Vector2(
                Mathf.Max(0f, texture.width * Mathf.Abs(uv.width)),
                Mathf.Max(0f, texture.height * Mathf.Abs(uv.height)));
            float ratio = size.y > 0f ? size.x / size.y : 0f;
            return new TaffyMeasurementData
            {
                minContent = size,
                maxContent = size,
                preferred = size,
                aspectRatio = ratio,
                isReplaced = true,
                samples = Array.Empty<TaffyMeasurementSample>(),
            };
        }

        private static float MeasureLongestTokenTmp(TMP_Text text, string value, float fallback)
        {
            float widest = 0f;
            foreach (string token in Tokens(value))
                widest = Mathf.Max(widest, ClampSize(text.GetPreferredValues(token, Unbounded, Unbounded)).x);
            return widest > 0f ? Mathf.Min(widest, fallback) : Mathf.Max(0f, fallback);
        }

        private static float MeasureLongestTokenText(Text text, string value, float fallback)
        {
            float widest = 0f;
            foreach (string token in Tokens(value))
                widest = Mathf.Max(widest, MeasureUnityTextAtWidth(text, token, Unbounded).x);
            return widest > 0f ? Mathf.Min(widest, fallback) : Mathf.Max(0f, fallback);
        }

        private static IEnumerable<string> Tokens(string value)
        {
            if (string.IsNullOrEmpty(value))
                yield break;

            int start = -1;
            for (int i = 0; i <= value.Length; i++)
            {
                bool separator = i == value.Length || char.IsWhiteSpace(value[i]);
                if (!separator && start < 0)
                    start = i;
                if (!separator || start < 0)
                    continue;

                yield return value.Substring(start, i - start);
                start = -1;
            }
        }

        private static Vector2 MeasureUnityTextAtWidth(Text text, string value, float width)
        {
            float pixelsPerUnit = Mathf.Max(0.0001f, text.pixelsPerUnit);
            TextGenerationSettings settings = text.GetGenerationSettings(new Vector2(Mathf.Max(0f, width), Unbounded));
            TextGenerator generator = text.cachedTextGeneratorForLayout;
            float preferredWidth = generator.GetPreferredWidth(value, settings) / pixelsPerUnit;
            float preferredHeight = generator.GetPreferredHeight(value, settings) / pixelsPerUnit;
            return ClampSize(new Vector2(Mathf.Min(width, preferredWidth), preferredHeight));
        }

        private static TaffyMeasurementSample[] BuildSamples(
            Func<float, Vector2> measure,
            float minWidth,
            float maxWidth,
            float widthHint)
        {
            var widths = new List<float>(MaxSamples);
            AddWidth(widths, widthHint);
            AddWidth(widths, minWidth);
            AddWidth(widths, maxWidth);
            AddWidth(widths, maxWidth * 0.75f);
            AddWidth(widths, maxWidth * 0.5f);
            AddWidth(widths, maxWidth * 0.25f);
            AddWidth(widths, 64f);
            AddWidth(widths, 128f);
            AddWidth(widths, 256f);
            AddWidth(widths, 512f);

            widths.Sort();
            if (widths.Count > MaxSamples)
                widths.RemoveRange(MaxSamples, widths.Count - MaxSamples);

            var samples = new TaffyMeasurementSample[widths.Count];
            for (int i = 0; i < widths.Count; i++)
                samples[i] = new TaffyMeasurementSample(widths[i], measure(widths[i]));
            return samples;
        }

        private static void AddWidth(List<float> widths, float width)
        {
            width = FiniteWidth(width);
            if (width <= 0f || width >= Unbounded)
                return;
            width = Mathf.Max(1f, width);
            for (int i = 0; i < widths.Count; i++)
            {
                if (Mathf.Abs(widths[i] - width) < 0.5f)
                    return;
            }
            widths.Add(width);
        }

        private static void Sanitize(ref TaffyMeasurementData data)
        {
            data.minContent = ClampSize(data.minContent);
            data.maxContent = ClampSize(data.maxContent);
            data.preferred = ClampSize(data.preferred);
            data.maxContent.x = Mathf.Max(data.minContent.x, data.maxContent.x);
            data.maxContent.y = Mathf.Max(data.minContent.y, data.maxContent.y);
            data.preferred.x = Mathf.Max(data.minContent.x, data.preferred.x);
            data.preferred.y = Mathf.Max(data.minContent.y, data.preferred.y);
            if (float.IsNaN(data.aspectRatio) || float.IsInfinity(data.aspectRatio) || data.aspectRatio < 0f)
                data.aspectRatio = 0f;
            if (data.samples == null)
                data.samples = Array.Empty<TaffyMeasurementSample>();
            for (int i = 0; i < data.samples.Length; i++)
            {
                data.samples[i].availableWidth = Mathf.Max(0f, FiniteWidth(data.samples[i].availableWidth));
                data.samples[i].size = ClampSize(data.samples[i].size);
            }
        }

        private static Vector2 ClampSize(Vector2 value)
        {
            return new Vector2(FiniteNonNegative(value.x), FiniteNonNegative(value.y));
        }

        private static float FiniteNonNegative(float value)
        {
            return float.IsNaN(value) || float.IsInfinity(value) ? 0f : Mathf.Max(0f, value);
        }

        private static float FiniteWidth(float value)
        {
            return float.IsNaN(value) || float.IsInfinity(value) ? Unbounded : Mathf.Max(0f, value);
        }

        private static int ObjectId(UnityEngine.Object value)
        {
#if UNITY_6000_5_OR_NEWER
            return value.GetEntityId().GetHashCode();
#else
            return value.GetInstanceID();
#endif
        }

        private static int HashTmp(TMP_Text text, float widthHint)
        {
            int hash = 17;
            AddHash(ref hash, text.text);
            AddHash(ref hash, text.font ? ObjectId(text.font) : 0);
            AddHash(ref hash, text.fontSharedMaterial ? ObjectId(text.fontSharedMaterial) : 0);
            AddHash(ref hash, text.fontSize);
            AddHash(ref hash, (int)text.fontStyle);
            AddHash(ref hash, text.characterSpacing);
            AddHash(ref hash, text.wordSpacing);
            AddHash(ref hash, text.lineSpacing);
            AddHash(ref hash, (int)text.alignment);
            AddHash(ref hash, (int)text.overflowMode);
            AddHash(ref hash, text.richText ? 1 : 0);
            AddHash(ref hash, text.margin.x);
            AddHash(ref hash, text.margin.y);
            AddHash(ref hash, text.margin.z);
            AddHash(ref hash, text.margin.w);
            AddHash(ref hash, QuantizedWidth(widthHint));
            return hash;
        }

        private static int HashText(Text text, float widthHint)
        {
            int hash = 17;
            AddHash(ref hash, text.text);
            AddHash(ref hash, text.font ? ObjectId(text.font) : 0);
            AddHash(ref hash, text.fontSize);
            AddHash(ref hash, (int)text.fontStyle);
            AddHash(ref hash, text.lineSpacing);
            AddHash(ref hash, text.supportRichText ? 1 : 0);
            AddHash(ref hash, (int)text.alignment);
            AddHash(ref hash, (int)text.horizontalOverflow);
            AddHash(ref hash, (int)text.verticalOverflow);
            AddHash(ref hash, text.resizeTextForBestFit ? 1 : 0);
            AddHash(ref hash, text.resizeTextMinSize);
            AddHash(ref hash, text.resizeTextMaxSize);
            AddHash(ref hash, QuantizedWidth(widthHint));
            return hash;
        }

        private static int HashImage(Image image)
        {
            int hash = 17;
            AddHash(ref hash, image.sprite ? ObjectId(image.sprite) : 0);
            AddHash(ref hash, (int)image.type);
            AddHash(ref hash, image.preserveAspect ? 1 : 0);
            AddHash(ref hash, image.pixelsPerUnitMultiplier);
            return hash;
        }

        private static int HashRawImage(RawImage image)
        {
            int hash = 17;
            AddHash(ref hash, image.texture ? ObjectId(image.texture) : 0);
            AddHash(ref hash, image.uvRect.x);
            AddHash(ref hash, image.uvRect.y);
            AddHash(ref hash, image.uvRect.width);
            AddHash(ref hash, image.uvRect.height);
            return hash;
        }
        private static int HashCustomProvider(
            MonoBehaviour behaviour,
            ITaffyMeasurementProvider provider,
            float widthHint)
        {
            int hash = 17;
            AddHash(ref hash, ObjectId(behaviour));
            AddHash(ref hash, provider.MeasurementVersion);
            AddHash(ref hash, QuantizedWidth(widthHint));
            return hash;
        }

        private static int QuantizedWidth(float width)
        {
            width = FiniteWidth(width);
            if (width >= Unbounded)
                return int.MaxValue;
            return Mathf.RoundToInt(width * 2f);
        }
        private static void AddHash(ref int hash, string value)
        {
            unchecked { hash = hash * 31 + (value == null ? 0 : value.GetHashCode()); }
        }

        private static void AddHash(ref int hash, int value)
        {
            unchecked { hash = hash * 31 + value; }
        }

        private static void AddHash(ref int hash, float value)
        {
            AddHash(ref hash, value.GetHashCode());
        }
    }
}
