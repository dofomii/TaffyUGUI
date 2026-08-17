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
- Phase 12 real Unity platform validation: **complete for the Android ARM64-only release scope**.
- Phase 13 performance/reliability hardening: **active; P13.1 is next**.
- Phase 14: **not started**.

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

## Phase 12 verification

Phase 12 validates package compatibility across Unity `2021.3.39f1`, `2022.3.62f1`, and `6000.4.3f1`. All three Editors compile the package and pass **38/38 Edit Mode** plus **9/9 Play Mode** permanent tests. The Unity 2021.3 gate required the known local Linux `bee_backend --stdin-canary` workaround on this newer Linux host; the Editor installation was restored byte-for-byte afterward. The only tracked compatibility adjustment is version-correct built-in font selection in the permanent legacy-uGUI `Text` measurement tests.

The fresh Android ARM64 Unity 6 IL2CPP Player gate also passes on physical `CPH2723`. The APK contains `libil2cpp.so` and `libtaffy_ugui.so`, its packaged Taffy ELF program headers match the staged payload, both runtime-loaded `PT_LOAD` segments are byte-identical, Android loads the library successfully, and the runtime scene reports `TAFFY_PHASE12_DEVICE_PASS width=120.00 height=48.00` with no fatal/linker error.

Final native closeout remains green: rustfmt, Clippy `-D warnings`, **44/44 Rust tests**, release build/cbindgen drift check, Android ARM64 build/verify, Phase 4, and Phase 5 staging/verification all pass. Android ARM64 remains the sole advertised Player target. Windows, macOS, iOS, WebGL, and Linux Player are explicitly not advertised on this branch rather than being claimed from unexecuted platform tests.

See `PHASE12_REAL_UNITY_VALIDATION.md` for the complete matrix and evidence. Disposable validation scenes/probes remain local-only under ignored `.build/` paths and are never tracked project source.

## Next authoritative work

**Phase 13 P13.1 — establish the 100-node benchmark baseline.**

Phase 13 now owns performance, allocation, lifecycle, leak, and failure-path hardening before Phase 14 release work.
