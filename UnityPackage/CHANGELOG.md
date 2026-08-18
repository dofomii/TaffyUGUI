# Changelog

## [1.0.0] - 2026-08-18

First production release.

### Added

- Final ABI v1 (`version=1`, `stage=2`) native Rust/Taffy bridge pinned to Taffy 0.13.0.
- uGUI `TaffyLayoutGroup` and `TaffyLayoutItem` with Flexbox, Grid, Block/FlowRoot, float/clear, box-model, absolute/relative positioning, alignment, overflow, and aspect-ratio authoring.
- Typed Calc expressions, Grid tracks, named lines/areas, placement helpers, and Editor property drawers.
- Intrinsic measurement for TextMeshPro, legacy Text, Image, RawImage, and custom `ITaffyMeasurementProvider` implementations.
- Responsive profiles, safe-area padding, pixel rounding, ScrollRect content expansion, integration warnings, and rebuild-loop diagnostics.
- Layout debugger, Scene visualization, typed inspectors, and conservative migration from Horizontal/Vertical/GridLayoutGroup with Undo and prefab-instance safety.
- Permanent native scaling/allocation/bulk benchmarks and lifecycle/panic/leak regressions.
- Importable UPM samples plus complete user documentation.

### Compatibility

- Package/Edit/Play regression validation: Unity 2021.3.39f1, 2022.3.62f1, and 6000.4.3f1.
- Advertised Player target: Android ARM64 only.
- Unity 6 Android ARM64 IL2CPP build/package validation and physical-device execution completed.

### Notes

- Windows, macOS, iOS, WebGL, and Linux Player are not advertised in v1.0.
- The package is AI-generated; review and test it independently before shipping in your product.
