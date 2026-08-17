# Local Verification Status

**Date:** 2026-08-17

This document records checks already executed locally. Remote CI is not build authority.

## Final ABI / native verification

- Final ABI v1 `1/2` on exact Taffy `0.13.0`.
- Rust `1.97.1`; rustfmt passes; Clippy passes with warnings denied.
- All 44 maintained Rust tests pass.
- Host release build passes.
- Pinned-cbindgen public-header drift verification passes.
- Complete 31-function `tu_*` contract is aligned across Rust FFI, C header, and managed P/Invoke.
- `tu_copy_last_error` regression is fixed and covered.

## Final Android release verification

- `python3 build/build.py verify-abi-final` — PASS.
- `python3 build/build.py native android-arm64` — PASS.
- `python3 build/build.py verify-phase4` — PASS.
- `python3 build/build.py stage-phase5` — PASS.
- `python3 build/build.py verify-phase5` — PASS.
- Source snapshot: `sha256:68fb502c6bc48c83b2239f5212d98fd6a7f3f777c587cb286876121c58752731`.
- Accepted Android ARM64 library SHA-256: `7bdca92aae2939e5098292294ee7f7d730d5eee1c718d87f65a3f22349338f66`.
- ABI recorded by Phase 3/4/5 evidence: `1/2`.
- Fresh Unity `6000.4.3f1` ARM64 IL2CPP Player build from the accepted staged payload — PASS.
- APK contains exactly one `lib/arm64-v8a/libtaffy_ugui.so`.
- APK ELF program headers and both `PT_LOAD` runtime segments match the accepted staged library.
- Earlier physical-device Phase 6 execution on CPH2723 / Android 16 proved final ABI `1/2`, native loading, managed/native round trips, and last-error diagnostics for the frozen runtime source.

## Phase status

**Phase 6 is complete. Phase 7 is active; P7.1 is next.**

Disposable validation harnesses/probes are local-only and excluded from Git by project policy.
