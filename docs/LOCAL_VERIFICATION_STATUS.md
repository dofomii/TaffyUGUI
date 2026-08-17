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
- Native cached-measurement tests remain green, including callback-free width-dependent measurement behavior.

## Current Android release/provenance verification

Phase 8 changed tracked `UnityPackage/Runtime` inputs, which are part of the content-addressed release snapshot. The native artifact was therefore rebuilt/reaccepted and the Unity payload restaged rather than leaving Phase 4/5 evidence stale.

- `python3 build/build.py verify-abi-final` — **PASS**.
- `python3 build/build.py native android-arm64` — **PASS**.
- `python3 build/build.py verify-phase4` — **PASS**.
- `python3 build/build.py stage-phase5` — **PASS**.
- `python3 build/build.py verify-phase5` — **PASS**.
- Current source snapshot: `sha256:0eb0a1c56f841cebf48758d6af045533ab69e68e9e76b8f05611712ce282c8f4`.
- Android ARM64 library SHA-256: `7bdca92aae2939e5098292294ee7f7d730d5eee1c718d87f65a3f22349338f66`.
- ABI recorded by Phase 3/4/5 evidence: `1/2`.
- The library bytes remain unchanged from the historical Phase 6 accepted engine binary.

Earlier physical-device execution on CPH2723 / Android 16 proved final native loading, managed/native round trips, and last-error diagnostics for the frozen ABI/runtime path.

## Phase 8 Unity verification

The final Phase 8 runtime was validated with Unity `6000.4.3f1` using the real local package and temporary validation material only under ignored `.build/` paths.

Permanent Edit Mode package suite:

- **14 total, 14 passed, 0 failed, 0 skipped**.

Permanent Play Mode package suite:

- **3 total, 3 passed, 0 failed, 0 skipped**.

The permanent suite covers Phase 7 lifecycle/topology/nesting regressions plus:

- core min/max sizing;
- content-box padding/border behavior;
- Flex grow and wrapping;
- Block/FlowRoot/Float/Clear;
- absolute positioning, insets, and aspect ratio;
- RTL direction and overflow mode execution;
- custom managed measurement caching and explicit invalidation;
- TextMeshPro intrinsic measurement;
- uGUI Text measurement and text/font-size/style changes;
- Image/replaced-element intrinsic sizing;
- repeated cached axis application without managed-provider re-entry.

Additional checks:

- VS Code Problems for runtime/tests: **0 diagnostics**.
- `verify-abi-final`: **PASS**, including 44/44 Rust tests.
- Fresh Unity Android ARM64 IL2CPP development APK build: **PASS**.
- IL2CPP conversion includes `TaffyUGUI.Runtime.dll` and `Unity.TextMeshPro.dll`.
- APK contains `lib/arm64-v8a/libtaffy_ugui.so` and `lib/arm64-v8a/libil2cpp.so`.
- APK Taffy library program headers match the accepted staged library.
- Both runtime-loaded ELF `PT_LOAD` segments are byte-identical to the accepted staged library.

No Android device was attached during the final Phase 8 APK validation, so that APK was not launched on physical hardware. Dedicated physical Unity Player validation remains Phase 12.

## Phase status

**Phase 8 is complete. Phase 9 is active; P9.1 is next.**

Permanent Unity regression tests are tracked product tests. Disposable validation harnesses/probes remain local-only under ignored `.build/` paths and are excluded from Git by project policy.
