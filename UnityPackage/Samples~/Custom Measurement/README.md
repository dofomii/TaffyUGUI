# Custom Measurement

This sample demonstrates a custom intrinsic measurement source for content-dependent Taffy sizing.

## Recommended Editor workflow

1. Create a Taffy Group from **GameObject > TaffyUGUI** or the **UI Builder**.
2. Add a child UI object and a `TaffyLayoutItem`.
3. Keep width/height on **Auto / Content** where intrinsic measurement should determine size.
4. Attach `CustomMeasurementSample`. The component implements `ITaffyMeasurementProvider`.
5. Inspect the selected Item's **Computed Layout** view to see measurement information when available. **Explain Layout** reports the content/intrinsic contribution without claiming details it cannot prove.
6. Use **Layout Health** if a content-dependent item has no determinable measurement source.

## Runtime sample

Taffy resolves the provider's preferred size before the native compute pass. Change the public dimensions in Play Mode and invalidate measurement when your own provider data changes.

For production providers, see [LayoutElement, Measurement, and TextMeshPro](../../Documentation~/measurement.md).
