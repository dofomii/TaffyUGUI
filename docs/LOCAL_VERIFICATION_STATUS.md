# Local Verification Status

**Date:** 2026-08-17

This document records checks actually executed locally. Remote CI is not build authority.

## Final ABI / native verification

- Final ABI v1 `1/2` on exact Taffy `0.13.0`.
- Rust `1.97.1`; rustfmt passes; Clippy passes with warnings denied.
- All 44 maintained Rust tests pass.
- Host release build passes.
- Pinned-cbindgen public-header drift verification passes.
- Complete 31-function `tu_*` contract remains aligned across Rust FFI, C header, and managed P/Invoke.
- Phase 9 required no native ABI expansion.

## Phase 9 Unity verification

The Phase 9 runtime was validated with Unity `6000.4.3f1` on Linux using the real package plus a local-only host native library in an ignored temporary Unity project.

Permanent package tests:

- Edit Mode: **22 total, 22 passed, 0 failed, 0 skipped**.
- Play Mode: **5 total, 5 passed, 0 failed, 0 skipped**.

The complete suite retains the prior Phase 7/8 coverage and adds:

- explicit Grid rows/columns and numeric placement geometry;
- detailed Grid track/item diagnostics;
- implicit/auto tracks and grid-auto-flow;
- `fr`, minmax, min-content, and max-content sizing;
- fixed Repeat, auto-fill, and auto-fit;
- named lines, named spans, and named template areas;
- justify-items/justify-self Grid alignment;
- typed Calc-backed dimensions and Grid tracks;
- Calc expression mutation and native-context recreation;
- runtime Grid reconfiguration across frames;
- invalid Repeat, template-area, placement, and cyclic Calc validation.

Additional checks executed during Phase 9:

- existing Phase 7/8 Edit Mode regression before new tests: **14/14 passed**;
- existing Phase 7/8 Play Mode regression before new tests: **3/3 passed**;
- VS Code Problems for package runtime/tests: **0 diagnostics**;
- `python3 build/build.py quality`: **PASS**, including all 44 native tests.

## Phase 9 Android release verification

Final content-addressed source snapshot:

`sha256:c9f0607bc7808c2e3fb857c5785780d31b9b1112fbe39275b8b23b6088cb9698`

Final checks recorded after the exact Phase 9 source state is closed:

- `python3 build/build.py verify-abi-final` — PASS;
- `python3 build/build.py native android-arm64` — PASS;
- `python3 build/build.py verify-phase4` — PASS;
- `python3 build/build.py stage-phase5` — PASS;
- `python3 build/build.py verify-phase5` — PASS;
- Unity Android ARM64 IL2CPP development APK build — PASS;
- APK contains `lib/arm64-v8a/libil2cpp.so` and `lib/arm64-v8a/libtaffy_ugui.so`;
- packaged Taffy ELF program headers and runtime-loaded `PT_LOAD` segments match the accepted staged Android payload byte-for-byte.

Android ARM64 native library SHA-256 remains:

`7bdca92aae2939e5098292294ee7f7d730d5eee1c718d87f65a3f22349338f66`

No Android device was attached during the final Phase 9 validation, so the Phase 9 APK was **not** executed on physical hardware. Comprehensive real-platform Unity Player validation remains Phase 12; the project retains earlier hardware proof for the frozen native ABI/runtime path.

## Phase status

**Phase 9 is complete. Phase 10 is active; P10.1 is next.**

Permanent Unity regression tests are tracked product tests. Disposable validation harnesses/probes remain local-only under ignored `.build/` paths and are excluded from Git by project policy.
