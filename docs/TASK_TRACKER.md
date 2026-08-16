# TaffyUGUI Task Tracker

**Canonical workflow:** local development, local build, local verification. GitHub is backup storage only.
**Status date:** 2026-08-16
**Taffy baseline:** exactly `0.13.0`
**Canonical Rust toolchain:** `1.97.1`
**MSRV:** `1.82.0`
**Unity primary baseline:** `2021.3 LTS`
**Native ABI source state:** `ABI-v1-RC`, version `1`, stage `1`

## How to read this tracker

The tracker distinguishes three things:

- **Implemented** — source/build infrastructure exists.
- **Verified** — the required checks actually executed in the canonical local environment.
- **Gate complete** — all mandatory implementation + verification requirements are satisfied and the project may advance authoritatively.

A later-phase prototype or early scaffold does not automatically mean that later phase is complete.

### Status legend

- **COMPLETE** — phase gate complete.
- **IMPLEMENTATION COMPLETE / VERIFICATION PENDING** — required code exists but the current canonical verification gate still needs execution.
- **ACTIVE / BLOCKED** — current phase work is prepared but cannot finish until a prerequisite is available.
- **PARTIAL EARLY SCAFFOLDING** — some work exists out of sequence, but the formal phase is not open/complete.
- **NOT STARTED** — intentionally gated.

---

# Current Project State

- **Phase 0:** COMPLETE.
- **Phase 1:** native engine implementation COMPLETE.
- **Phase 2:** production C ABI implementation COMPLETE.
- **Phase 3:** COMPLETE; ABI-v1-RC `1/1` passed the canonical local compiled gate.
- **Phase 4:** COMPLETE for the active **Android ARM64-only** release scope; other platform targets are deferred and are not advertised/supported by this branch.
- **Phase 5:** COMPLETE for Android ARM64; verified Unity package native payload staged.
- **Phase 6:** READY / PARTIAL EARLY SCAFFOLDING (production `tu_*` P/Invoke surface already aligned); formal managed conformance can begin next.
- **Phase 7:** prototype components exist; formal production Unity phase NOT STARTED.
- **Phase 8–14:** NOT STARTED.

## Current authoritative boundary
## Current authoritative boundary

The active v1 release scope is now **Android ARM64 only**. Windows, macOS, iOS, and WebGL target definitions remain in the repository for future work, but they no longer gate this branch and must not be advertised as supported.

Every accepted Android native artifact must still pass the full Phase 3 gate on the exact clean source tree:

```bash
python3 build/build.py verify-abi-rc
```

Phase 4 closes only when the Android ARM64 artifact is accepted by:

```bash
python3 build/build.py verify-phase4
```

and `dist/native/phase4-index.json` records `android-arm64` as the sole release target.

**Next authoritative task:** stage and verify that Phase-4-accepted Android ARM64 binary under `UnityPackage/Plugins/Android/arm64-v8a/`, including deterministic Unity importer metadata and provenance.


---

# Phase 0 — Rust Project and Toolchain Foundation

**Status:** COMPLETE

**Goal:** establish a deterministic native project/toolchain foundation before feature implementation.

- [x] P0.1 Create Rust native crate/workspace.
- [x] P0.2 Configure native outputs for `cdylib` + `staticlib`.
- [x] P0.3 Pin Taffy exactly to `0.13.0`.
- [x] P0.4 Commit deterministic Cargo lockfile.
- [x] P0.5 Establish Rust MSRV `1.82.0`.
- [x] P0.6 Pin canonical development/release Rust toolchain `1.97.1`.
- [x] P0.7 Establish rustfmt/Clippy/test/release quality requirements.
- [x] P0.8 Split native implementation into production modules (`context`, `handles`, `style`, `grid`, `calc`, `measurement`, `error`, `version`, `ffi`).
- [x] P0.9 Establish public cbindgen configuration/header path.
- [x] P0.10 Establish canonical local `build/build.py` entry point.
- [x] P0.11 Establish clean local development/bootstrap documentation.
- [x] P0.12 Preserve native-only architecture: Unity owns rendering/input; Rust owns geometry.

### Phase 0 gate

- [x] Deterministic native workspace exists.
- [x] Dependency/toolchain baselines are explicit.
- [x] Native module structure supports production implementation.
- [x] Canonical build entry point exists.

**Documentation:** `PHASE0_FOUNDATION.md`

---

# Phase 1 — Complete Rust/Taffy Native Engine

**Status:** IMPLEMENTATION COMPLETE
**Current compiled proof:** inherited by pending Phase 3 local gate

**Goal:** finish the persistent Taffy 0.13 native engine before freezing the production ABI.

## State/ownership

- [x] P1.1 Implement persistent native context registry.
- [x] P1.2 Implement generation-safe fixed-width context handles.
- [x] P1.3 Implement generation-safe node/resource handles.
- [x] P1.4 Implement persistent parent/child topology.
- [x] P1.5 Implement dirty/cache invalidation and node dirty query.
- [x] P1.6 Implement stale-handle rejection.
- [x] P1.7 Implement cross-context handle rejection.
- [x] P1.8 Implement explicit cross-thread context rejection.
- [x] P1.9 Prevent generation-wrap ABA/stale-handle resurrection.

## Core style/layout

- [x] P1.10 Implement dimensions, min/max sizes, margin, padding, border.
- [x] P1.11 Implement position/insets.
- [x] P1.12 Implement overflow/scrollbar width.
- [x] P1.13 Implement box-sizing/direction/aspect-ratio/display-none surface.

## Flex

- [x] P1.14 Flex row/column/reverse directions.
- [x] P1.15 Flex wrap/wrap-reverse.
- [x] P1.16 Flex grow/shrink/basis.
- [x] P1.17 Flex align-items/align-self/align-content.
- [x] P1.18 Flex justify-content.
- [x] P1.19 Flex gaps and auto margins.

## Block / FlowRoot / Float

- [x] P1.20 Block layout dispatch.
- [x] P1.21 FlowRoot layout dispatch.
- [x] P1.22 Float left/right.
- [x] P1.23 Clear behavior.
- [x] P1.24 Block text-align surface required by Taffy.

## Grid

- [x] P1.25 Grid auto-flow row/column/dense modes.
- [x] P1.26 Explicit row/column tracks.
- [x] P1.27 Implicit/auto tracks.
- [x] P1.28 Fixed/percent/fraction/minmax/content tracks.
- [x] P1.29 Repeat/count/auto-fill/auto-fit.
- [x] P1.30 Named lines, named spans, named template areas, and item placement.
- [x] P1.31 Grid justify-items/justify-self and alignment surface.
- [x] P1.32 Detailed Grid track/gutter/item diagnostic extraction.

## Calc/resources

- [x] P1.33 Typed Calc resource graph without CSS text parsing.
- [x] P1.34 Calc/resource lifetime ownership.
- [x] P1.35 Bounded Calc graph/evaluation behavior.

## Measurement/bulk operations

- [x] P1.36 Cached min-content/max-content/preferred measurement records.
- [x] P1.37 Width-dependent cached samples and replaced-element intrinsic aspect-ratio support.
- [x] P1.38 Bulk style/topology/measurement upload and bulk layout retrieval.
- [x] P1.39 Persistent cache/duplicate-compute behavior and deterministic result retrieval.

### Phase 1 gate

- [x] Complete required native v1 style/algorithm surface is represented in source.
- [x] Native unit/integration/golden verification inventory exists.
- [ ] Current-machine compiled execution of the complete native verification inventory — **covered by Phase 3 `verify-abi-rc`, pending in this sandbox**.

**Documentation:** `PHASE1_NATIVE_ENGINE.md`

---

# Phase 2 — Production C ABI Candidate and Safety Boundary

**Status:** IMPLEMENTATION COMPLETE
**Current ABI source:** promoted to ABI-v1-RC `1/1`

**Goal:** expose the complete native engine through a production-shaped fixed-width C ABI.

- [x] P2.1 Replace bootstrap/raw-pointer API with production `tu_*` surface.
- [x] P2.2 Use opaque `uint64_t` context/node/resource handles.
- [x] P2.3 Use fixed `uint32_t` counts/capacities.
- [x] P2.4 Use fixed 32-bit enum/status representation.
- [x] P2.5 Use `uint8_t` bool-like ABI fields.
- [x] P2.6 Implement explicit status/error model.
- [x] P2.7 Implement thread-local last-error diagnostics.
- [x] P2.8 Validate arguments/handles/counts/enums before use.
- [x] P2.9 Add panic guard so Rust panic is not intended to unwind over C.
- [x] P2.10 Expose ABI version/stage/capability/build/Taffy-version queries.
- [x] P2.11 Expose context lifecycle.
- [x] P2.12 Expose node lifecycle/style/topology/dirty APIs.
- [x] P2.13 Expose cached measurement APIs.
- [x] P2.14 Expose Calc resource APIs.
- [x] P2.15 Expose Grid template and diagnostics APIs.
- [x] P2.16 Expose compute/single-layout/bulk-layout APIs.
- [x] P2.17 Generate public C header through cbindgen configuration.
- [x] P2.18 Keep public ABI independent from Rust/Taffy implementation types.
- [x] P2.19 Maintain complete public export inventory: **31 `tu_*` functions**.

### Phase 2 gate

- [x] Production ABI implementation exists.
- [x] Public C/C++ header exists.
- [x] Static export inventory matches header inventory.
- [x] C11/C++17 header compile passes in current local static gate.
- [ ] Current-machine cbindgen regeneration/drift proof — pending in Phase 3 compiled gate.

**Documentation:** `PHASE2_PRODUCTION_C_ABI.md`

---

# Phase 3 — Native Verification and ABI Release-Candidate Lock

**Status:** COMPLETE

**Goal:** prove the native engine/ABI release candidate before using it for platform artifacts.

- [x] P3.1 ABI/version/capability/context/handle unit verification inventory.
- [x] P3.2 Flex golden geometry verification inventory.
- [x] P3.3 Block/FlowRoot/Float golden verification inventory.
- [x] P3.4 Grid named-area/placement/diagnostic verification inventory.
- [x] P3.5 Calc and cached-measurement golden verification inventory.
- [x] P3.6 ABI size/alignment/offset and enum numeric-contract assertions.
- [x] P3.7 Invalid/stale/cross-context/malformed/wrong-thread verification inventory.
- [x] P3.8 Repeated lifecycle/topology stress verification inventory.
- [x] P3.9 C11 public-header smoke source.
- [x] P3.10 C++17 public-header smoke source.
- [x] P3.11 cbindgen header regeneration/drift command.
- [x] P3.12 Linked host C/C++ smoke command.
- [x] P3.13 One canonical local `verify-abi-rc` command.
- [x] P3.14 ABI source promoted to release-candidate state `1/1`.
- [x] P3.15 Provider-independent local static gate passes.
- [x] P3.16 Run `cargo fmt --check` locally on current canonical machine/source.
- [x] P3.17 Run Clippy with `-D warnings` locally.
- [x] P3.18 Run full Rust test inventory locally.
- [x] P3.19 Build host release native library locally.
- [x] P3.20 Regenerate/diff public header with pinned cbindgen locally.
- [x] P3.21 Link and execute C/C++ smoke programs against the built Rust library locally.
- [x] P3.22 Record Phase 3 evidence for the clean source tree.

### Current Phase 3 gate

**COMPLETE on the local Linux host.**

Phase 4 artifacts may not be accepted until P3.16–P3.22 pass on every artifact-producing source tree/host as required by the build driver.

**Documentation:** `PHASE3_NATIVE_VERIFICATION.md`

---

# Phase 4 — Native Build and Artifact Staging

**Status:** COMPLETE FOR ACTIVE ANDROID ARM64-ONLY RELEASE SCOPE

**Goal:** produce the reproducible ABI-v1-RC native library required by the currently supported release target before Unity plugin payload packaging.

## Build infrastructure — complete

- [x] Phase 4 builds require clean Phase 3 evidence for the exact source tree.
- [x] Full 31-function public export contract checked per accepted artifact.
- [x] Target manifests record source tree, ABI, Taffy/Rust target, checksum, size, exports, architecture evidence and toolchain evidence.
- [x] `SHA256SUMS` verification is part of artifact acceptance.
- [x] Android records canonical NDK/API evidence without leaking machine-local SDK paths.
- [x] Final Phase 4 index generation implemented.
- [x] Active release target set is explicitly Android ARM64 only.

## Active release artifact

- [x] P4.5 Android ARM64 `libtaffy_ugui.so` builds with NDK r21d `21.3.6528147`, API 21, and verifies.
- [x] P4.8 The active Android artifact has accepted `manifest.json` + `SHA256SUMS`.
- [x] P4.9 The active Android artifact exposes the complete required 31-function ABI export set.
- [x] P4.10 Final `python3 build/build.py verify-phase4` passed for the Android-only scope and produced `dist/native/phase4-index.json`.

## Deferred targets — outside active branch release scope

- P4.1 Windows x64 — deferred.
- P4.2 macOS ARM64 — deferred.
- P4.3 macOS x64 — deferred.
- P4.4 macOS universal — deferred.
- P4.6 iOS ARM64 — deferred.
- P4.7 WebGL — deferred; the existing Emscripten 2.0.19/Rust 1.97.1 compatibility investigation is retained for future work.

These targets may be revived later, but they are not required for Phase 4 completion on this Android-only branch and must not be presented as supported platforms.

### Phase 4 exit gate

The Android ARM64 artifact exists, is locally architecture/export/checksum verified, matches the Phase-3-verified source tree/ABI contract, and is accepted by Android-only `verify-phase4`.

**Documentation:** `PHASE4_PLATFORM_BUILDS.md`

---

# Phase 5 — Unity-Ready Native Payload Staging

**Status:** COMPLETE — Android ARM64 only

**Status:** COMPLETE — Android ARM64 only

**Goal:** convert the verified Android `dist/native/**` output into the native payload actually shipped by the Unity package.

- [x] P5.1 Canonical `UnityPackage/Plugins/Android/arm64-v8a/` structure established.
- [x] P5.2 Only the Phase-4-verified Android ARM64 binary is copied into the package plugin path.
- [x] P5.3 Deterministic Unity `.meta` importer configuration enables Android ARM64 and disables unsupported platforms/editor loading.
- [x] P5.4 Source revision/tree, ABI, Taffy version, and checksum provenance are preserved beside the staged binary.
- [x] P5.5 Verification rejects debug, unverified, or additional non-Android native artifacts in the package.
- [x] P5.6 Git/UPM package path contains the required Android native binary and metadata.
- [x] P5.7 `stage-phase5` reproducibly stages from `dist/native/phase4-index.json`; `verify-phase5` confirms checksum/provenance/importer integrity.
- [x] P5.8 **Android Native Engine Candidate Complete**.

### Phase 5 exit gate

A complete, verified Android ARM64 Unity-native payload exists at `UnityPackage/Plugins/Android/arm64-v8a/` and matches the accepted Phase 4 artifact byte-for-byte. No other platform is claimed by this gate.
A complete, verified Android ARM64 Unity-native payload exists at `UnityPackage/Plugins/Android/arm64-v8a/` and matches the accepted Phase 4 artifact byte-for-byte. No other platform is claimed by this gate.

---

# Phase 6 — Minimal Managed ABI Conformance and Final ABI v1 Freeze

**Status:** READY / PARTIAL EARLY SCAFFOLDING — Phase 5 gate is complete; formal managed conformance is next.

**Early work already present:**

- [x] P6.E1 Unity low-level P/Invoke migrated from obsolete bootstrap symbols to `tu_*` ABI-v1-RC.
- [x] P6.E2 Managed/native ABI structure-size guards exist for the current ABI surface.

**Formal Phase 6 tasks:**

- [ ] P6.1 Load each staged native library through the managed wrapper.
- [ ] P6.2 Validate ABI version/stage/capability handshake from Unity/C#.
- [ ] P6.3 Validate managed struct layout/size/enum mapping against native ABI.
- [ ] P6.4 Context create/destroy/clear managed round trip.
- [ ] P6.5 Node create/remove/style managed round trip.
- [ ] P6.6 Topology/bulk upload managed round trip.
- [ ] P6.7 Cached measurement managed round trip.
- [ ] P6.8 Calc/Grid resource managed round trip.
- [ ] P6.9 Compute and single/bulk layout retrieval managed round trip.
- [ ] P6.10 Validate error/last-error diagnostics through P/Invoke.
- [ ] P6.11 Validate library naming/loading rules per platform.
- [ ] P6.12 Resolve every ABI discrepancy before final freeze.
- [ ] P6.13 Freeze final ABI v1.
- [ ] P6.14 Rebuild **every** Phase 4 artifact from final ABI v1.
- [ ] P6.15 Re-stage **every** Phase 5 package artifact from final ABI v1.
- [ ] P6.16 Declare **ABI v1 / Final Native Payload Gate** complete.

### Phase 6 exit gate

The final ABI has been proven through the real managed boundary and the complete cross-platform native payload has been rebuilt from that exact final ABI.

---

# Phase 7 — Minimal Working Unity uGUI Product

**Status:** NOT STARTED — prototype scaffolding exists, but phase is gated by Phase 6

Current repository scaffolding:

- `TaffyLayoutGroup.cs`
- `TaffyLayoutItem.cs`
- `TaffyNative.cs`

These files are not considered Phase 7 completion.

- [ ] P7.1 Production persistent managed/native context lifecycle.
- [ ] P7.2 Stable mapping between RectTransforms and native node handles.
- [ ] P7.3 Correct topology synchronization without rebuilding everything unnecessarily.
- [ ] P7.4 Correct `CalculateLayoutInputHorizontal/Vertical` integration.
- [ ] P7.5 Correct `SetLayoutHorizontal/Vertical` application.
- [ ] P7.6 Report own min/preferred/flexible size through `SetLayoutInputForAxis`.
- [ ] P7.7 Support nested Taffy layout groups.
- [ ] P7.8 Preserve `LayoutElement` behavior/ignoreLayout semantics.
- [ ] P7.9 Apply native geometry to RectTransform without replacing uGUI rendering/input components.
- [ ] P7.10 Minimal Unity Edit Mode/Play Mode verification.

---

# Phase 8 — Production Flex / Block / Float / Measurement Unity Integration

**Status:** NOT STARTED

- [ ] P8.1 Complete Unity authoring for core size/min/max/box model.
- [ ] P8.2 Complete Flex container authoring.
- [ ] P8.3 Complete Flex item authoring.
- [ ] P8.4 Block/FlowRoot/Float/Clear Unity authoring.
- [ ] P8.5 Position/inset/overflow/box-sizing/direction/aspect integration.
- [ ] P8.6 Managed measurement cache orchestration.
- [ ] P8.7 TextMeshPro measurement adapter.
- [ ] P8.8 Unity Text adapter where retained.
- [ ] P8.9 Image/replaced-element measurement adapter.
- [ ] P8.10 No managed callback during native Taffy computation.
- [ ] P8.11 Measurement invalidation on text/font/size/style changes.
- [ ] P8.12 Production regression tests for Flex/Block/Float/measurement behavior.

---

# Phase 9 — Complete Grid and Calc Unity Authoring

**Status:** NOT STARTED
**Note:** native Grid/Calc engine support is already implemented in Phase 1.

- [ ] P9.1 Serializable Grid track/unit data model.
- [ ] P9.2 Grid explicit row/column authoring.
- [ ] P9.3 Auto/implicit track authoring.
- [ ] P9.4 `fr`, minmax, min/max-content authoring.
- [ ] P9.5 repeat/count/auto-fill/auto-fit authoring.
- [ ] P9.6 named lines/spans.
- [ ] P9.7 named template areas.
- [ ] P9.8 grid-auto-flow modes.
- [ ] P9.9 Grid item placement authoring.
- [ ] P9.10 justify-items/justify-self/alignment authoring.
- [ ] P9.11 Typed Calc authoring/resource lifecycle.
- [ ] P9.12 Grid/Calc diagnostics and validation in Unity.

---

# Phase 10 — Responsive and Integration Hardening

**Status:** NOT STARTED

- [ ] P10.1 Responsive profile/breakpoint system.
- [ ] P10.2 Intrinsic resize/CanvasScaler responsiveness.
- [ ] P10.3 Safe-area integration.
- [ ] P10.4 ScrollRect content/viewport bridge.
- [ ] P10.5 ContentSizeFitter interaction rules.
- [ ] P10.6 AspectRatioFitter interaction rules.
- [ ] P10.7 Animation-driven dirty invalidation.
- [ ] P10.8 Pixel rounding strategy.
- [ ] P10.9 Layout rebuild-loop protection.
- [ ] P10.10 Runtime override API where required.

---

# Phase 11 — Editor Tooling and Migration

**Status:** NOT STARTED

- [ ] P11.1 `TaffyLayoutGroup` custom inspector.
- [ ] P11.2 `TaffyLayoutItem` custom inspector.
- [ ] P11.3 Length/dimension/rect property drawers.
- [ ] P11.4 Grid track/placement/area editor tooling.
- [ ] P11.5 Scene-view layout visualization.
- [ ] P11.6 Layout debugger/diagnostics window.
- [ ] P11.7 HorizontalLayoutGroup migration.
- [ ] P11.8 VerticalLayoutGroup migration.
- [ ] P11.9 GridLayoutGroup migration where semantically safe.
- [ ] P11.10 Prefab/Undo/serialized-data-safe migration behavior.
- [ ] P11.11 Batch migration workflow.

---

# Phase 12 — Real Unity Platform Validation

**Status:** NOT STARTED

- [ ] P12.1 Unity 2021.3 LTS primary Editor validation.
- [ ] P12.2 Selected Unity 2022.3 LTS compatibility validation.
- [ ] P12.3 Selected Unity 6.0 compatibility validation.
- [ ] P12.4 Windows x64 Unity Player validation.
- [ ] P12.5 macOS Intel Unity Player validation.
- [ ] P12.6 macOS Apple Silicon Unity Player validation.
- [ ] P12.7 Android ARM64 Unity Player validation.
- [ ] P12.8 iOS ARM64 Unity Player validation.
- [ ] P12.9 WebGL Player validation.
- [ ] P12.10 Linux validation if retained as advertised support.
- [ ] P12.11 Edit Mode regression suite.
- [ ] P12.12 Play Mode regression suite.
- [ ] P12.13 Platform regression scenes.
- [ ] P12.14 Advertise only targets that pass real Unity validation.

---

# Phase 13 — Performance and Reliability Hardening

**Status:** NOT STARTED

- [ ] P13.1 100-node benchmark.
- [ ] P13.2 1,000-node benchmark.
- [ ] P13.3 10,000-node benchmark/stress scenario.
- [ ] P13.4 Native/managed allocation profiling.
- [ ] P13.5 Dirty propagation/recompute profiling.
- [ ] P13.6 Bulk ABI transfer profiling.
- [ ] P13.7 Domain reload/context lifecycle stress.
- [ ] P13.8 Repeated scene/prefab lifecycle stress.
- [ ] P13.9 Native resource/memory leak checks.
- [ ] P13.10 Error-path and panic-containment audit.
- [ ] P13.11 Package/library load/startup checks.
- [ ] P13.12 Performance documentation and known limits.

---

# Phase 14 — v1.0 Release

**Status:** NOT STARTED

- [ ] P14.1 Final compatibility matrix.
- [ ] P14.2 Complete Getting Started documentation.
- [ ] P14.3 Flexbox documentation.
- [ ] P14.4 Grid/Calc documentation.
- [ ] P14.5 LayoutElement/measurement/TMP documentation.
- [ ] P14.6 ScrollRect/responsive integration documentation.
- [ ] P14.7 Migration documentation.
- [ ] P14.8 Platform-support documentation.
- [ ] P14.9 Troubleshooting/diagnostics documentation.
- [ ] P14.10 Samples/regression examples packaged.
- [ ] P14.11 Third-party license/notices audit.
- [ ] P14.12 Changelog/release notes.
- [ ] P14.13 Git/UPM installation validation.
- [ ] P14.14 Final package archive validation.
- [ ] P14.15 Version/tag/release packaging.
- [ ] P14.16 Publish v1.0 only after all release gates pass.

---

# Current Next Action

The tracker is intentionally **not** advancing into Phase 5 or user-facing Unity feature work yet.

The next real work is:

1. obtain a local machine/environment containing the pinned Rust verification toolchain;
2. run `python3 build/build.py verify-abi-rc` successfully on a clean tree;
3. run the canonical Phase 4 host builds on Windows, macOS, and Linux;
4. collect artifacts and run `python3 build/build.py verify-phase4`;
5. mark P4.1–P4.10 complete only from real artifact evidence;
6. then open Phase 5.
