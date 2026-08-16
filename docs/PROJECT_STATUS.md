# TaffyUGUI Project Status

**Status date:** 2026-08-17
**Canonical workflow:** local development, local build, local verification. GitHub is backup storage only.
**Implementation baseline audited before this documentation repair:** local Phase 4 infrastructure state at commit `1db105b69d50c8319dfc88fac34396b4eb66b8d0`. Documentation-only commits after that point do not change the implementation status described here.

## Executive status

TaffyUGUI has completed the **native architecture, production C ABI candidate, local ABI release-candidate verification, and the Android ARM64 native release gate**. The native interface is locked as **ABI-v1-RC (`version=1`, `stage=1`)** on exact Taffy **0.13.0**.

The active release scope is now intentionally **Android ARM64 only**:

- Phase 0 foundation: **complete**.
- Phase 1 native engine implementation: **complete**.
- Phase 2 production C ABI implementation: **complete**.
- Phase 3 ABI/verification gate: **complete** on the local Linux host.
- Phase 4: **complete for Android ARM64-only scope**; Windows/macOS/iOS/WebGL are deferred and are not advertised by this branch.
- Phase 5: **complete for Android ARM64**, with the verified native binary staged under the Unity package together with deterministic importer metadata and provenance.
- Phase 6: **ready to begin formal managed ABI conformance**; early P/Invoke scaffolding already exists.

The local Linux host can run the full compiled Phase 3 gate and has staged and re-verified a real Android ARM64 artifact. Phase 5 may proceed from an Android-only `phase4-index.json`.

## Progress model

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

## Deferred platform work

The following targets remain implemented as future build definitions but are outside the active release gate:

- Windows x64.
- macOS arm64/x64/universal.
## Next authoritative action

Begin Phase 6 managed ABI conformance against the staged Android ARM64 library. The Phase 5 package payload is now reproducible with `python3 build/build.py stage-phase5` and independently checked with `python3 build/build.py verify-phase5`.

1. Re-run the Phase 3 gate on the exact clean Android-only release tree.
2. Rebuild and verify Android ARM64 from that tree.
3. Run `python3 build/build.py verify-phase4` to create the Android-only Phase 4 index.
4. Run `python3 build/build.py stage-phase5` and `verify-phase5` to stage the Unity Android payload with importer metadata and provenance.
