# TaffyUGUI Task Tracker

**Canonical workflow:** local development/build/test. GitHub is backup storage only.

## Current state

- **Current boundary:** Phase 3 — Native Verification and ABI Release-Candidate Lock
- **ABI source state:** `ABI-v1-RC`, version `1`, stage `1`
- **Taffy baseline:** exactly `0.13.0`
- **Unity package minimum:** `2021.3`
- **Next phase:** Phase 4 — Cross-Platform Native Builds and Artifact Staging
- **Phase 4 may start only after:** `python3 build/build.py verify-abi-rc` passes on the local development machine.

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
