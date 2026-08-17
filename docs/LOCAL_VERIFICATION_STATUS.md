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
- Phase 10 required no native ABI expansion.

## Phase 10 Unity verification

The Phase 10 runtime was validated with Unity `6000.4.3f1` on Linux using the real package plus a local-only host native library in an ignored temporary Unity project.

Permanent package tests:

- Edit Mode: **29 total, 29 passed, 0 failed, 0 skipped**.
- Play Mode: **9 total, 9 passed, 0 failed, 0 skipped**.

The complete suite retains Phase 7–9 coverage and adds:

- responsive profile serialization, validation, priority, breakpoint switching, and runtime forcing;
- rect/Canvas-scale responsive observation;
- safe-area padding and runtime safe-area overrides;
- ScrollRect viewport/content sizing and runtime child changes;
- ContentSizeFitter axis ownership diagnostics;
- AspectRatioFitter aspect handoff and conflict diagnostics;
- animation-property dirty invalidation;
- edge-based pixel rounding;
- same-frame rebuild suppression and self-sizing loop protection.

Additional checks executed during Phase 10:

- existing Phase 0–9 Edit Mode regression before Phase 10 tests: **22/22 passed**;
- existing Phase 0–9 Play Mode regression before Phase 10 tests: **5/5 passed**;
- VS Code Problems for package runtime/tests: **0 diagnostics**;
- `python3 build/build.py quality`: **PASS**, including all 44 native tests.

## Phase 10 Android release verification

Final content-addressed source snapshot:

`sha256:3228f12128c07fd6c470a7bc9119a4ba810f7718d98c6ae9537086030beaa0fc`

Final checks recorded after the exact Phase 10 source state is closed:

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

Physical Android execution: **PASS** on `CPH2723`, Android 16 / API 36, ARM64-v8a, using Unity `6000.4.3f1` IL2CPP. The device log reports successful `libtaffy_ugui.so` loading and `TAFFY_PHASE10_DEVICE_PASS:profile=phone:height=328.0:suppressed=4`; `328.0` matches three 100 px items, two 8 px profile gaps, and 12 px vertical safe-area padding. The Player remained alive and targeted `Unity:E`, `AndroidRuntime:E`, `libc:F`, and native `DEBUG:F` log scans were empty. Comprehensive real-platform validation remains Phase 12.

## Phase status

**Phase 10 is complete. Phase 11 is active; P11.1 is next.**

Permanent Unity regression tests are tracked product tests. Disposable validation material remains local-only under ignored `.build/` paths and is excluded from Git by project policy.
