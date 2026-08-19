# Changelog

## [Unreleased]

### Added

- Added the importable **Responsive Dashboard** sample scene: a desktop-to-mobile admin dashboard built from one TaffyUGUI hierarchy using nested Flex layouts, wrapping, responsive profiles, and ScrollRect content expansion.

## [1.1.2] - 2026-08-19

Windows PluginImporter correction release. Runtime layout semantics, serialized field/enum contracts, and ABI v1 remain unchanged.

### Fixed

- Windows x86 native plugin metadata now marks the DLL as **Win32 Player only** and explicitly disables it for the Unity Editor and Win64.
- Windows x64 remains the only Windows native plugin eligible for the 64-bit Windows Editor and Win64 Player, preventing the `expected x64 architecture, but was x86 architecture` load failure.

### Validation

- The release gate now rejects Windows x86 importer metadata that is Editor- or Win64-compatible and requires the Windows x64 importer to remain Windows/x86_64 Editor + Win64 compatible.
- All five native libraries are rebuilt from 1.1.2 source so their embedded build version matches the package version.
- Unity 6000.4.3f1 PluginImporter API validation confirms Windows x86 is Editor/Win64-incompatible while Windows x64 is Windows/x86_64 Editor + Win64 compatible.
- Unity 6000.4.3f1 package-folder validation passes 140/140 Edit Mode and 9/9 Play Mode tests.

## [1.1.1] - 2026-08-19

Packaging correction release. Runtime layout semantics, serialized field/enum contracts, and ABI v1 remain unchanged.

### Changed

- The UPM payload now ships all requested native desktop binaries alongside Android ARM64: Windows x86/x64 and Linux x86/x64.
- All five native libraries are rebuilt from the 1.1.1 source so the embedded build version matches the package version; Android ARM64 retains 16 KB page compatibility.
- Windows x86/x64 are cross-built locally with `cargo-xwin`; Linux x86/x64 are built locally. Linux x86 is retained for legacy consumers even though current Unity Linux Players are 64-bit.

### Validation

- Android ARM64, Windows x86/x64, and Linux x86/x64 binaries are architecture/format checked and expose the complete 31-symbol ABI v1 export set.
- Windows binaries are not runtime-executed on the Linux release host; their validation is PE architecture/export/version verification.
- Unity 6000.4.3f1 package-folder validation with the bundled Linux x64 plugin passes 140/140 Edit Mode and 9/9 Play Mode tests.

## [1.1.0] - 2026-08-19

Developer Experience release. Runtime layout semantics, serialized field/enum contracts, and ABI v1 remain unchanged.

### Added

- Beginner-first Simple/Advanced inspectors with contextual help, smart summaries, and intent-first sizing, spacing, alignment, Grid, and responsive authoring.
- One-click Quick Layout and Item actions, hierarchy recipes, guided onboarding, and the reusable UI Builder workflow.
- Layout Health diagnostics with Undo/prefab-safe repairs for common ownership, sizing, Grid, Calc, responsive, and measurement problems.
- Apply-once built-in/project presets with a searchable preset browser and cached project-asset discovery.
- Scene View layout overlays, responsive preview presets, safe padding/gap handles, computed-layout inspection, and Explain Layout.
- Advanced Inspector search/aliases, Essentials/Modified/All filtering, section reset, safe layout copy/paste, density preference, and shared Debugger diagnostics.

### Changed

- Getting Started, package documentation, and samples now use the recommended guided Editor workflows.
- Editor diagnostics, preset scanning, overlays, and preference/reload behavior were hardened for large selections and normal domain reloads.

### Compatibility

- Full maintained native regression: 46/46 tests passed.
- Unity 6000.4.3f1: 140/140 Edit Mode and 9/9 Play Mode tests passed.
- Unity 2021.3.39f1 and 2022.3.62f1: 140/140 Edit Mode compatibility validation passed.
- Advertised Player target remains Android ARM64 only.

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
