# TaffyUGUI Project Status

**Status date:** 2026-08-17
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
- Phase 10 responsive/integration hardening: **active; P10.1 is next**.
- Phases 11–14: **not started**.

## Phase 9 production boundary

The Unity runtime now exposes the native Grid/Calc engine through production authoring:

- explicit and implicit/auto Grid rows and columns;
- points, percentages, `fr`, minmax, min-content, max-content, and Calc track sizing;
- fixed Repeat, auto-fill, and auto-fit;
- named row/column lines;
- named template areas;
- row/column numeric lines and spans;
- named lines and named spans;
- grid-auto-flow Row/Column/Dense modes;
- justify-items and justify-self Grid alignment;
- serializable typed Calc expressions for dimensions and Grid sizing;
- managed Grid/Calc validation and detailed native Grid diagnostics.

Typed Calc resources are cached per persistent native context. Equivalent expressions are canonicalized and reused; dependencies are created before dependents, and unused resources are released in reverse creation order only after current styles/templates no longer reference them. Context teardown clears the managed cache and native context ownership handles remaining native resources.

Grid arrays and UTF-8 strings are pinned only for the duration of individual ABI calls. Cached native style structs do not retain transient managed string pointers.

The Phase 9 implementation required **no ABI expansion**. It uses the final Phase 6 ABI v1 `1/2` Grid/Calc calls and style fields already present in the package.

Responsive profiles/CanvasScaler/safe-area/ScrollRect/fitter/rebuild-loop hardening remains Phase 10. Editor tooling and migration remain Phase 11.

## Phase 9 verification

Final local Unity `6000.4.3f1` package verification:

- VS Code Problems: **0 diagnostics** for runtime/tests;
- native `quality` regression: **PASS**, including rustfmt, Clippy `-D warnings`, 44/44 maintained Rust tests, and release build;
- Edit Mode: **22/22 tests passed**;
- Play Mode: **5/5 tests passed**;
- all previous Phase 7/8 regression tests remain green;
- Phase 9 tests cover explicit/implicit Grid, `fr`, minmax/content sizing, Repeat/count/auto-fill/auto-fit, named lines/spans/areas, auto-flow, placement/alignment, typed Calc mutation/lifecycle, detailed diagnostics, validation failures, and runtime reconfiguration.

Final ABI/Android provenance and fresh Android ARM64 IL2CPP packaging are bound to the exact Phase 9 source snapshot:

`sha256:c9f0607bc7808c2e3fb857c5785780d31b9b1112fbe39275b8b23b6088cb9698`

The Android ARM64 native library remains:

`7bdca92aae2939e5098292294ee7f7d730d5eee1c718d87f65a3f22349338f66`

No physical Android device was attached for the final Phase 9 APK run; comprehensive Unity Player/device validation remains Phase 12. Earlier physical-device proof for the frozen native ABI/runtime path remains valid.

Disposable validation harness/probe material remains local-only under ignored `.build/` paths and is never tracked project source.

## Next authoritative work

**Phase 10 P10.1 — implement the responsive profile/breakpoint system.**

Windows, macOS, iOS, and WebGL remain deferred outside the active Android ARM64 release scope.
