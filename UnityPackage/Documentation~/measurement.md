# LayoutElement, Measurement, and TextMeshPro

TaffyUGUI resolves intrinsic size **before** entering the native compute pass. Native layout never calls back into managed code, avoiding managed/native re-entrancy during layout.

## Built-in measurement sources

With `TaffyLayoutItem.measurement = Auto`, the resolver can measure:

- TextMeshPro `TMP_Text`;
- legacy uGUI `Text`;
- `Image` with a Sprite;
- `RawImage` with a Texture;
- a custom `ITaffyMeasurementProvider`.

When no supported intrinsic source is present, normal style/layout sizing is used.

## Custom measurement

Implement `ITaffyMeasurementProvider` on a component on the same `RectTransform`:

```csharp
public sealed class CardMeasure : MonoBehaviour, ITaffyMeasurementProvider
{
    public int MeasurementVersion { get; private set; }

    public bool TryGetTaffyMeasurement(float availableWidth, out TaffyMeasurementData m)
    {
        float width = Mathf.Min(availableWidth, 240f);
        m = new TaffyMeasurementData
        {
            minContent = new Vector2(80f, 48f),
            maxContent = new Vector2(240f, 48f),
            preferred = new Vector2(width, 48f),
            aspectRatio = 0f,
            isReplaced = false,
            samples = null,
        };
        return true;
    }
}
```

Increment `MeasurementVersion` when the provider's intrinsic result changes. You can also call `TaffyLayoutItem.InvalidateMeasurement()` or `TaffyLayoutGroup.InvalidateMeasurement()` explicitly.

## Text and font invalidation

TaffyUGUI listens for legacy font texture rebuilds and TMP text/font property events and invalidates cached measurements. Width-sensitive measurement samples are cached and uploaded to native state before compute.

## LayoutElement interaction

Taffy participates in Unity's layout system and computes minimum/preferred/arranged passes. Avoid using another component to continuously force the same child dimensions after Taffy has arranged them. For parent sizing or ScrollRect content sizing, see the integration guide.

## Replaced elements

Images/RawImages and custom providers may be treated as replaced elements. `TaffyLayoutItem.forceReplacedElement` can force that behavior, and `aspectRatio` can supply deterministic aspect sizing.
