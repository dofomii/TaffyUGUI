# TaffyUGUI Project Status

**Status date:** 2026-08-18
**Canonical workflow:** local development, local build, local verification
**Active release scope:** Android ARM64 only
**Native ABI:** final ABI v1 (`version=1`, `stage=2`) on exact Taffy `0.13.0`

## Current state

- Phase 0 foundation: **complete**.
- Phase 1 native engine implementation: **complete**.
- Phase 2 production C ABI implementation: **complete**.
- Phase 3 native verification: **complete**.
- Phase 4 Android ARM64 native artifact: **complete at final ABI v1 `1/2`**.
- Phase 5 Android Unity native payload: **complete at final ABI v1 `1/2`**.
- Phase 6 managed ABI conformance/final freeze: **complete**.
- Phase 7 minimal Unity uGUI product: **complete**.
- Phase 8 production Flex/Block/Float/measurement integration: **complete**.
- Phase 9 Grid/Calc Unity authoring: **complete**.
- Phase 10 responsive/integration hardening: **complete**.
- Phase 11 editor tooling/migration: **complete**.
- Phase 12 real Unity platform validation: **active; P12.1 is next**.
- Phases 13–14: **not started**.

## Phase 11 production boundary

The package now includes a dedicated Editor-only `TaffyUGUI.Editor` assembly with production authoring and migration tooling:

- `TaffyLayoutGroup` custom inspector;
- `TaffyLayoutItem` custom inspector;
- typed Length/Edges/Calc/Grid property drawers;
- Grid track, placement, named-line, and area authoring UI;
- selected-layout Scene view visualization with Grid track overlays;
- layout debugger/diagnostics window;
- HorizontalLayoutGroup migration to Flex row;
- VerticalLayoutGroup migration to Flex column;
- deterministic GridLayoutGroup migration for safe fixed-row/fixed-column configurations;
- conservative refusal/diagnostics for unsupported legacy Grid semantics;
- one-step Undo migration;
- prefab-instance-safe added/removed component overrides without mutating the prefab asset;
- selection/all-loaded-scene batch migration.

Unity's layout-group hierarchy does not allow a Taffy layout group to coexist temporarily with a legacy `LayoutGroup`. The migration service therefore snapshots the legacy settings/children, removes the old component through Undo, adds Taffy, then applies the snapshot. Existing `TaffyLayoutItem` data is reused rather than overwritten wholesale.

No Rust engine, C ABI, or managed runtime ABI expansion was required for Phase 11. Editor dependencies remain outside Player assemblies.

## Phase 11 verification

Final local Unity `6000.4.3f1` package verification:

- VS Code Problems: **0 diagnostics** for package/runtime/editor/tests;
- native quality regression: **PASS**, including rustfmt, Clippy `-D warnings`, 44/44 maintained Rust tests, and release build;
- Edit Mode: **38/38 tests passed**;
- Play Mode: **9/9 tests passed**;
- 9 new Phase 11 Edit Mode tests cover editor registration, typed drawers, Horizontal/Vertical/Grid migration, unsafe Grid refusal, Undo restoration, prefab-instance safety, batch migration, debugger type availability, and Scene visualization state;
- all Phase 7–10 runtime regression coverage remains green.

Final ABI/Android provenance and fresh Android ARM64 IL2CPP packaging are bound to the exact Phase 11 source snapshot:

`sha256:3228f12128c07fd6c470a7bc9119a4ba810f7718d98c6ae9537086030beaa0fc`

Android ARM64 native library SHA-256 remains:

`7bdca92aae2939e5098292294ee7f7d730d5eee1c718d87f65a3f22349338f66`

The fresh Phase 11 Android ARM64 IL2CPP gate passes. The Player includes `TaffyUGUI.Runtime.dll` but excludes `TaffyUGUI.Editor.dll` from both stripped managed assemblies and the IL2CPP conversion list. The APK contains `libil2cpp.so` and `libtaffy_ugui.so`; the staged and packaged Taffy ELF program headers match and both runtime-loaded `PT_LOAD` segments are byte-identical. Comprehensive Unity-version/platform compatibility validation remains Phase 12.

Disposable validation material remains local-only under ignored `.build/` paths and is never tracked project source.

## Next authoritative work

**Phase 12 P12.1 — validate the package in Unity 2021.3 LTS.**

Windows, macOS, iOS, and WebGL remain deferred outside the active Android ARM64 release scope until their Phase 12 gates pass.
