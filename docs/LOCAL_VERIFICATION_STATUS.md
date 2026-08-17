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

The Player gate specifically verifies that `TaffyUGUI.Editor` and UnityEditor-dependent migration/debug tooling are absent from Player assemblies. Phase 12 retains that Player-isolation guarantee while expanding real Unity-version and device validation.

## Phase 12 Unity/version and Player verification

The final package matrix was executed locally with ignored validation hosts:

- Unity `2021.3.39f1`: package compile **PASS**, Edit Mode **38/38**, Play Mode **9/9**;
- Unity `2022.3.62f1`: Edit Mode **38/38**, Play Mode **9/9**;
- Unity `6000.4.3f1`: Edit Mode **38/38**, Play Mode **9/9**.

Unity 2021.3 on this newer Linux host required the known temporary `bee_backend --stdin-canary` workaround. The Editor's original `bee_backend` was restored after the run and its SHA-256 was verified as `8561ed19e6d35e1e947b450dd528867e7c43c9fe43b5cce9086b58d3cad4fa67`.

A fresh Unity `6000.4.3f1` Android ARM64 IL2CPP APK was built, installed, and executed on physical `CPH2723`. Android loaded `libtaffy_ugui.so` successfully and the regression scene emitted `TAFFY_PHASE12_DEVICE_PASS width=120.00 height=48.00`; no fatal managed/native linker exception was observed. The packaged ELF program-header table and both runtime-loaded `PT_LOAD` segments match the accepted staged payload byte-for-byte.

Final closeout native checks also pass: rustfmt, Clippy with warnings denied, **44/44 Rust tests**, host release/cbindgen drift verification, Android ARM64 native build/verify, Phase 4, and Phase 5 staging/verification.

The active Player support matrix is intentionally Android ARM64 only. Windows, macOS, iOS, WebGL, and Linux Player are not advertised on this branch. See `PHASE12_REAL_UNITY_VALIDATION.md`.

## Phase status

**Phase 12 is complete. Phase 13 is active; P13.1 is next.**

Permanent Unity regression tests are tracked product tests. Disposable validation material remains local-only under ignored `.build/` paths and is excluded from Git by project policy.
