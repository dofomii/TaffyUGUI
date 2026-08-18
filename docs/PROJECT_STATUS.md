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
- Phase 13 performance/reliability hardening: **complete**.
- Phase 14 v1.0 release: **complete; v1.0.0 is release-ready and intentionally unpublished**.

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

## Phase 13 verification

Phase 13 adds permanent native scaling/allocation/bulk benchmarks plus Unity allocation and lifecycle regressions. Final native first-layout medians are 59.158 µs at 100 nodes, 592.140 µs at 1,000 nodes, and 9.297 ms at 10,000 nodes; dirty-leaf recompute is 25.160 µs, 332.798 µs, and 5.315 ms respectively, while fully cached recompute remains about 69–77 ns median. Bulk layout retrieval is 3.28–4.27× faster than scalar retrieval across the same scales.

The warmed 100-node Unity dirty-layout profile records **0 managed-thread allocated bytes across 100 iterations**. Native allocation profiling records two fresh allocation calls per first compute, with median transient allocation of 60,600 bytes at 100 nodes, 493,560 bytes at 1,000 nodes, and 7,887,864 bytes at 10,000 nodes.

Lifecycle/reliability gates include a 1,000-cycle native registered-context test, 100-cycle Unity enable/disable context recreation, 5 fresh Unity-domain launches running that lifecycle test, and 50 prefab/scene lifecycle cycles. Phase 13 also fixed recursive Unity serialization hazards by moving Calc operand-list and Grid repeat-track elements to managed-reference serialization.

Valgrind reports **0 definitely lost bytes, 0 indirectly lost bytes, and 0 definite/indirect leak errors** for repeated full native tree lifecycles. The 676-byte process-exit residual exactly matches a same-toolchain Rust ownership/thread-local baseline. Panic-containment regression confirms unexpected panics are returned as `InternalPanic` and the next ABI call recovers normally.

Final Unity compatibility is **41/41 Edit Mode plus 9/9 Play Mode** on Unity `2021.3.39f1`, `2022.3.62f1`, and `6000.4.3f1`, with zero recursive-serialization warnings. Final native quality is **46/46 Rust tests**, rustfmt/Clippy/release/cbindgen green. Android ARM64 Phase 4/5 provenance was rebuilt from source snapshot `sha256:e1e047831c23047e8ab0a1e2fbad256453b63bd03b679b49e3b0af3ee778ffcb`; the production `.so` SHA remains `7bdca92aae2939e5098292294ee7f7d730d5eee1c718d87f65a3f22349338f66`. A fresh Unity 6 Android IL2CPP APK builds and contains matching runtime-loaded Taffy `PT_LOAD` segments. No device was attached for a new Phase 13 execution, so Phase 12 remains the latest physical-device run.

See `PHASE13_PERFORMANCE_RELIABILITY.md` for the full benchmark tables, allocation/load metrics, lifecycle/leak evidence, and known limits.

## Phase 14 release closeout

The distributable Unity package is now version `1.0.0` and self-contained with package-local README/changelog/license/notices, complete user documentation, explicit uGUI/TMP dependencies, and three importable samples. Fresh UPM consumer projects on Unity 2021.3.39f1, 2022.3.62f1, and 6000.4.3f1 compile the imported samples and pass **41/41 Edit Mode + 9/9 Play Mode** each.

Local-path UPM installation, a temporary ignored Git `?path=/UnityPackage#v1.0.0` installation, and local tarball installation all pass. The native crate/package versions are frozen at `1.0.0`; Android ARM64 was rebuilt from source snapshot `sha256:676771e84efb0f8ab0d8cfb14cbf4c388bce500d88fd1870c50babe0d368fed8`, producing `.so` SHA-256 `85cb8ef34fc03c51cc40baaf4bdbbd45892a616d93958d21f4f86100303e51a7`. A fresh Unity 6 IL2CPP APK packages byte-identical runtime-loaded Taffy segments.

Publication is **not performed**: no real `v1.0.0` tag, push, GitHub release, or registry publication exists from this closeout. See `PHASE14_V1_RELEASE.md`.

## Current authoritative state

**v1.0.0 is release-ready locally and intentionally unpublished.** Future publication requires a separate explicit owner instruction.
