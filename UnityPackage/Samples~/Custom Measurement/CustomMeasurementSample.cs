using TaffyUGUI;
using UnityEngine;
using UnityEngine.UI;

namespace TaffyUGUI.Samples
{
    [RequireComponent(typeof(RectTransform))]
    public sealed class CustomMeasurementSample : MonoBehaviour, ITaffyMeasurementProvider
    {
        [Min(1f)] public float preferredWidth = 220f;
        [Min(1f)] public float preferredHeight = 72f;
        private int _version;
        public int MeasurementVersion => _version;

        private void OnValidate() => _version++;

        private void Start()
        {
            TaffyLayoutItem item = gameObject.GetComponent<TaffyLayoutItem>() ?? gameObject.AddComponent<TaffyLayoutItem>();
            item.measurement = TaffyMeasurementMode.Auto;
            if (!gameObject.GetComponent<Image>()) gameObject.AddComponent<Image>().color = new Color(0.25f, 0.7f, 0.7f);
            item.InvalidateMeasurement();
        }

        public bool TryGetTaffyMeasurement(float availableWidth, out TaffyMeasurementData measurement)
        {
            float width = Mathf.Min(Mathf.Max(1f, availableWidth), preferredWidth);
            measurement = new TaffyMeasurementData
            {
                minContent = new Vector2(Mathf.Min(80f, width), preferredHeight),
                maxContent = new Vector2(preferredWidth, preferredHeight),
                preferred = new Vector2(width, preferredHeight),
                aspectRatio = 0f,
                isReplaced = false,
                samples = null,
            };
            return true;
        }
    }
}
