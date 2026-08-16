# TaffyUGUI Task Tracker

**Canonical workflow:** local development/build/test. GitHub is backup storage only.

## Current state

- **Current boundary:** Phase 3 — Native Verification and ABI Release-Candidate Lock
- **ABI source state:** `ABI-v1-RC`, version `1`, stage `1`
- **Taffy baseline:** exactly `0.13.0`
- **Unity package minimum:** `2021.3`
- **Next phase:** Phase 4 — Cross-Platform Native Builds and Artifact Staging
- **Phase 4 build pipeline:** hardened locally; artifact execution remains gated by Phase 3 evidence.
- **Phase 4 may start only after:** `python3 build/build.py verify-abi-rc` passes on each local artifact-producing machine for byte-identical clean source content (same Git tree SHA).
- **Current environment blocker:** this sandbox has no Rust/rustup/cbindgen or platform SDKs and blocks outbound binary downloads. This is an execution-environment limitation, not a reason to bypass the gate.

## Phase 3 gate

- [x] Generation-safe fixed-width context/node/resource handles.
- [x] Stable `tu_*` C ABI with explicit status codes and last-error diagnostics.
- [x] Persistent native layout tree and bulk mutation/result paths.
- [x] Flex, Block/FlowRoot/Float, Grid, Calc, and cached measurement golden verification inventory.
- [x] ABI size/alignment/offset and enum-number assertions.
- [x] Repeated lifecycle/topology stress verification inventory.
- [x] C11/C++17 public-header smoke harness.
- [x] Unity P/Invoke migrated from obsolete bootstrap symbols to ABI-v1-RC `tu_*` symbols.
- [x] Managed/native structure-size guards for the current ABI.
- [x] Local-only verification/build driver; no GitHub Actions dependency.
- [ ] Full local Rust gate (`fmt`, Clippy, tests, release, cbindgen drift, linked smoke) recorded on the active development machine.

## Phase 4 — Cross-Platform Native Builds and Artifact Staging

**Goal:** produce reproducible, locally verified native binaries before Unity plugin importer packaging work.

### Phase 4 local build infrastructure

- [x] Phase 4 builds require recorded local Phase 3 evidence from the exact clean source tree; the local commit SHA is also recorded for traceability.
- [x] Windows/macOS/Linux canonical host ownership is explicit and enforced; `phase4-host` builds the targets assigned to that OS.
- [x] Native verification checks the complete 31-function public `tu_*` export contract, not a subset.
- [x] Artifact manifest schema records source revision/tree fingerprint, ABI, Taffy/Rust target, checksum, size, full export fingerprint, target-specific architecture evidence, and local toolchain evidence without machine-local SDK paths.
- [x] `SHA256SUMS` is validated as part of staged-artifact verification.
- [x] Windows architecture proof cannot silently degrade when `file` is unavailable; PE/x64 header tools are required instead.
- [x] iOS archive verification requires device ARM64 `lipo -info` evidence.
- [x] WebGL verification requires pinned Emscripten `emcc`/`emar`/`emnm` plus Wasm/LLVM-bitcode archive-member evidence.
- [x] `verify-phase4` requires every target, same source-tree fingerprint, same ABI/export contract, and writes the final `dist/native/phase4-index.json`.
- [x] Build-driver self-test proves canonical host assignments cover every required Phase 4 target and rejects malformed iOS/WebGL architecture evidence.
- [x] One-command local host wrappers cover Windows and macOS/Linux execution, plus a local aggregation finalizer.
- [x] Multi-host local build procedure is documented in `docs/PHASE4_PLATFORM_BUILDS.md`.

- [ ] P4.1 Windows x64 DLL builds, architecture/export checks pass, manifest/checksum staged.
- [ ] P4.2 macOS arm64 dylib builds and verifies.
- [ ] P4.3 macOS x64 dylib builds and verifies.
- [ ] P4.4 macOS universal dylib assembles with both architectures.
- [ ] P4.5 Android ARM64 `.so` builds with pinned NDK r21d/API21 and verifies.
- [ ] P4.6 iOS ARM64 static library builds and verifies.
- [ ] P4.7 WebGL static library builds with the pinned Unity-compatible Emscripten baseline and verifies.
- [ ] P4.8 Every artifact has `manifest.json` and `SHA256SUMS` under `dist/native/`.
- [ ] P4.9 Same required ABI export set is present on every advertised target.
- [ ] P4.10 No target is advertised until its local build and verification pass.

### Phase 4 exit gate

Every advertised native target must be generated from ABI-v1-RC, architecture-checked, export-checked, checksummed, reproducible through `build/build.py`, and ready to be copied into Unity plugin directories without changing the public C# API.
