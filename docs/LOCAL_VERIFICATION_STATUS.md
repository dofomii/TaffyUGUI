# Local Verification Status

**Date:** 2026-08-18

This document records checks actually executed locally. Remote CI is not build authority.

## Final ABI / native verification

- Final ABI v1 `1/2` on exact Taffy `0.13.0`.
- Rust `1.97.1`; rustfmt passes; Clippy passes with warnings denied.
- All 44 maintained Rust tests pass.
- Host release build passes.
- Pinned-cbindgen public-header drift verification passes.
- Complete 31-function `tu_*` contract remains aligned across Rust FFI, C header, and managed P/Invoke.
- Phase 11 requires no native ABI expansion.

## Phase 11 Unity verification

The Phase 11 editor/runtime package was validated with Unity `6000.4.3f1` on Linux using the real package plus an ignored local validation host. Editor tooling remains isolated in the Editor-only `TaffyUGUI.Editor` assembly.

Permanent package tests:

- Edit Mode: **38 total, 38 passed, 0 failed, 0 skipped**.
- Play Mode: **9 total, 9 passed, 0 failed, 0 skipped**.

The complete suite retains Phase 7–10 coverage and adds 9 Phase 11 Edit Mode tests covering:

- custom `TaffyLayoutGroup` and `TaffyLayoutItem` editor registration;
- typed Length/Edges/Calc/Grid property-drawer availability;
- HorizontalLayoutGroup migration;
- VerticalLayoutGroup migration;
- deterministic fixed-column GridLayoutGroup migration;
- rejection of unsafe Flexible GridLayoutGroup migration;
- one-step Undo restoration;
- prefab-instance migration without mutating the prefab asset;
- batch migration behavior;
- debugger/Scene-visualization editor type and state availability.

Migration implementation details verified by the tests include reuse of existing `TaffyLayoutItem` serialized data, prefab connection preservation, and conservative no-op behavior for unsupported legacy Grid semantics.

## Phase 11 Android release verification

Final content-addressed source snapshot:

`sha256:3228f12128c07fd6c470a7bc9119a4ba810f7718d98c6ae9537086030beaa0fc`

Final checks recorded after the exact Phase 11 source state is closed:

- `python3 build/build.py verify-abi-final` — **PASS**;
- `python3 build/build.py native android-arm64` — **PASS**;
- `python3 build/build.py verify-phase4` — **PASS**;
- `python3 build/build.py stage-phase5` — **PASS**;
- `python3 build/build.py verify-phase5` — **PASS**;
- fresh Unity Android ARM64 IL2CPP Development Player build — **PASS**;
- APK contains `lib/arm64-v8a/libil2cpp.so` and `lib/arm64-v8a/libtaffy_ugui.so`;
- Player stripped assemblies include `TaffyUGUI.Runtime.dll` and exclude `TaffyUGUI.Editor.dll`; the IL2CPP conversion assembly list also contains no Editor assembly;
- packaged Taffy ELF program headers and both runtime-loaded `PT_LOAD` segments match the accepted staged Android payload byte-for-byte.

Android ARM64 native library SHA-256 remains expected at:

`7bdca92aae2939e5098292294ee7f7d730d5eee1c718d87f65a3f22349338f66`

The Player gate specifically verifies that `TaffyUGUI.Editor` and UnityEditor-dependent migration/debug tooling are absent from Player assemblies. Comprehensive Unity-version and multi-platform validation remains Phase 12.

## Phase status

**Phase 11 is complete. Phase 12 is active; P12.1 is next.**

Permanent Unity regression tests are tracked product tests. Disposable validation material remains local-only under ignored `.build/` paths and is excluded from Git by project policy.
