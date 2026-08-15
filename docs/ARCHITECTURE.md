# Architecture

TaffyUGUI intentionally separates Unity rendering from layout computation.

## Boundary

Unity owns GameObjects, Canvas, RectTransform, graphics, input, EventSystem, TMP, ScrollRect, prefabs, and serialization. Rust owns a persistent Taffy tree and returns only layout rectangles.

The C ABI is the compatibility boundary. C# must never depend on Rust enums, strings, Vec, references, or Taffy NodeId internals.

## Runtime flow

1. `TaffyLayoutGroup` gathers active uGUI children.
2. Existing `LayoutElement` values are mapped where possible.
3. Optional `TaffyLayoutItem` overrides advanced sizing properties.
4. C# synchronizes styles and child relationships through the C ABI.
5. Rust calls Taffy to compute layout.
6. C# reads `x/y/width/height` and applies them with `LayoutGroup.SetChildAlongAxis`.

## Performance rules

- Do not calculate layout every Update.
- Rebuild only when Unity layout dirtiness requires it.
- Keep ABI calls coarse-grained; batched style/layout APIs are planned before large-tree optimization.
- Text measurement stays in Unity. TMP measurement will be integrated without per-node Rust-to-managed callbacks.

## Platform strategy

Native artifacts are built per Unity target:

- Windows: DLL
- macOS: dylib
- Android: SO per ABI
- iOS: static library / XCFramework
- WebGL: Emscripten-compatible static library using the Unity-matched Emscripten toolchain

WebGL is deliberately treated as a separate integration target because Unity toolchain compatibility is version-sensitive.

## Compatibility policy

The public Unity API and the C ABI should evolve conservatively. Breaking ABI changes require incrementing `taffy_ugui_api_version()`.

Taffy itself is pinned in `native/Cargo.toml`; dependency upgrades should not automatically change the Unity-facing ABI.
