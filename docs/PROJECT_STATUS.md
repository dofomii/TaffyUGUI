# TaffyUGUI Project Status

**Status date:** 2026-08-17
**Canonical workflow:** local development, local build, local verification. GitHub is backup storage only.
**Implementation baseline audited before this documentation repair:** local Phase 4 infrastructure state at commit `1db105b69d50c8319dfc88fac34396b4eb66b8d0`. Documentation-only commits after that point do not change the implementation status described here.

## Executive status

TaffyUGUI has completed the **native architecture, production C ABI candidate, and local ABI release-candidate verification work**. The native interface is locked as **ABI-v1-RC (`version=1`, `stage=1`)** on exact Taffy **0.13.0**.

The project is currently in **Phase 4 (cross-platform native artifact production)**:

- Phase 0 foundation: **complete**.
- Phase 1 native engine implementation: **complete**.
- Phase 2 production C ABI implementation: **complete**.
- Phase 3 ABI/verification gate: **complete** on the local Linux host.
- Phase 4 build/staging infrastructure: **complete**; Android ARM64 is built and locally accepted, while the remaining targets are pending.
- Phase 5 and later product/package phases: **not started**, except for explicitly documented scaffolding that was implemented early.

The local Linux host can run the full compiled Phase 3 gate and has staged a real Android ARM64 artifact. Phase 4 remains incomplete until every assigned host produces its required artifact from the same clean source tree.

## Progress model

A phase is tracked using three separate states:

1. **Implementation** — the source code/build tooling for the phase exists.
2. **Verification** — the required tests/builds have actually executed on the canonical local environment.
3. **Gate** — all mandatory implementation and verification requirements are satisfied, allowing the next phase to become authoritative.

This distinction is important. Code existing in the repository does not by itself mean the corresponding production phase is complete.

## Current verified facts

The local provider-independent gate currently proves:

- ABI source state is `1/1`.
- Taffy is pinned exactly to `0.13.0`.
- the Rust FFI source contains **31 public `tu_*` exports** and the public C header exposes the same 31 functions;
- the Unity P/Invoke layer uses the current `tu_*` ABI instead of the old bootstrap API;
- C11 and C++17 consumers compile the public header with warnings treated as errors;
- the current x86_64 ABI size probe matches the managed/native contract (`TuValue=16`, `TuGridPlacement=32`, `TuStyle=632`, `TuLayout=48`, `TuGridTrack=72`, `TuGridTemplate=104`);
- Phase 4 target definitions, host ownership, architecture validators, checksum/manifests, and final aggregation logic are internally consistent.
- The complete Phase 3 suite passes locally: formatting, Clippy with warnings denied, 44 Rust unit/integration tests, release build, cbindgen header-drift check, and linked C11/C++17 ABI smoke executables.
- Android ARM64 `libtaffy_ugui.so` has been built with NDK r21d (`21.3.6528147`), API 21, and the pinned Rust target; its ELF architecture, 31 ABI exports, checksum, and manifest were locally re-verified.

See `LOCAL_VERIFICATION_STATUS.md` for the exact distinction between passed and unavailable checks.

## Current blockers

Phase 3 is no longer the blocker. The remaining Phase 4 work is platform-specific:

- Linux WebGL: Emscripten `2.0.19` (LLVM 13) crashes with `unknown symbol kind` while linking Rust 1.97.1 WebAssembly runtime objects. This is a verified compiler/linker incompatibility, not a missing SDK.
- Windows: an MSVC-capable Windows host for Windows x64.
- macOS: Xcode tooling for macOS arm64/x64/universal and iOS arm64.

## Next authoritative action

1. Decide the supported WebGL compatibility path: update the Emscripten baseline after Unity compatibility validation, or explicitly allow a WebGL-specific Rust compatibility toolchain.
2. Build Windows x64 on Windows and macOS/iOS targets on macOS from the same source tree.
3. Collect all target directories and run `python3 build/build.py verify-phase4`.
4. Only then mark Phase 4 complete and begin Phase 5 Unity-ready native payload staging.
