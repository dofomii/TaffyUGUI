# Local Verification Status

**Date:** 2026-08-17

This document records checks actually executed locally. Remote CI is not build authority.

## Final ABI / native verification

- Final ABI v1 `1/2` on exact Taffy `0.13.0`.
- Rust `1.97.1`; rustfmt passes; Clippy passes with warnings denied.
- All 44 maintained Rust tests pass.
- Host release build passes.
- Pinned-cbindgen public-header drift verification passes.
- Complete 31-function `tu_*` contract is aligned across Rust FFI, C header, and managed P/Invoke.
- `tu_copy_last_error` regression is fixed and covered.

## Accepted Phase 6 Android release verification

- `python3 build/build.py verify-abi-final` — PASS for the Phase 6 release snapshot.
- Android ARM64 native build/Phase 4 acceptance — PASS.
- Phase 5 Unity native staging/verification — PASS.
- Phase 6 source snapshot: `sha256:68fb502c6bc48c83b2239f5212d98fd6a7f3f777c587cb286876121c58752731`.
- Accepted Android ARM64 library SHA-256: `7bdca92aae2939e5098292294ee7f7d730d5eee1c718d87f65a3f22349338f66`.
- ABI recorded by Phase 3/4/5 evidence: `1/2`.
- Earlier physical-device execution on CPH2723 / Android 16 proved native loading, managed/native round trips, and last-error diagnostics for the frozen Phase 6 ABI/native payload.

## Phase 7 Unity verification

The Phase 7 runtime was validated with Unity `6000.4.3f1` on Linux using the real package plus a local-only host native library in an ignored temporary Unity project.

Permanent package tests:

- Edit Mode: **4 total, 4 passed, 0 failed, 0 skipped**.
- Play Mode: **1 total, 1 passed, 0 failed, 0 skipped**.

The tests cover:

- intrinsic min/preferred size reporting;
- `LayoutElement` sizing and `ignoreLayout` preservation;
- stable/incremental topology through sibling reorder and same-count child replacement;
- nested groups;
- native context recreation after disable/enable;
- runtime container resizing and flexible child layout.

Additional regression checks:

- VS Code Problems for Phase 7 runtime/tests: **0 diagnostics**.
- `python3 build/build.py quality`: **PASS** after Phase 7 managed changes; native 44/44 tests remain green.
- Unity Android ARM64 IL2CPP development APK build: **PASS**.
- APK contains `lib/arm64-v8a/libtaffy_ugui.so`.
- APK Taffy library program headers and both runtime-loaded ELF `PT_LOAD` segments are byte-identical to the accepted Phase 6 native payload.

No Android device was available during the final Phase 7 validation, so the Phase 7 APK was **not** executed on physical hardware. That does not block the Phase 7 minimal Edit/Play gate; comprehensive real-platform validation remains Phase 12.

## Phase status

**Phase 7 is complete. Phase 8 is active; P8.1 is next.**

Permanent Unity regression tests are tracked product tests. Disposable validation harnesses/probes remain local-only under ignored `.build/` paths and are excluded from Git by project policy.
