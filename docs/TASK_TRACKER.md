# TaffyUGUI Task Tracker

**Canonical workflow:** local development, local build, local verification. GitHub is backup storage only.
**Status date:** 2026-08-17
**Taffy baseline:** exactly `0.13.0`
**Canonical Rust toolchain:** `1.97.1`
**MSRV:** `1.82.0`
**Unity primary baseline:** `2021.3 LTS`
**Native ABI source state:** final `ABI-v1`, version `1`, stage `2`

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
- **Phase 4:** COMPLETE for the active **Android ARM64-only** release scope at final ABI v1 `1/2`.
- **Phase 5:** COMPLETE for Android ARM64 at final ABI v1 `1/2`; package provenance matches the accepted Phase 4 artifact.
- **Phase 6:** COMPLETE; final ABI v1 `1/2` is frozen, the final Android artifact/payload were rebuilt/restaged, and the final native payload gate passed.
- **Phase 7:** COMPLETE; persistent Unity/uGUI integration, incremental native synchronization, nested groups, LayoutElement semantics, and permanent Unity regression tests are verified.
- **Phase 8:** COMPLETE; production Flex/Block/Float authoring and callback-free cached Unity measurement adapters are verified.
- **Phase 9:** COMPLETE; production Grid/Calc Unity authoring, diagnostics, lifecycle, and regression gates are verified.
- **Phase 10:** COMPLETE; responsive/integration hardening and permanent regression gates are verified.
- **Phase 11:** COMPLETE; editor tooling, diagnostics, migration, Undo/prefab safety, and permanent regression gates are verified.
- **Phase 12:** ACTIVE; P12.1 is the next authoritative task.
- **Phase 13–14:** NOT STARTED.

## Current authoritative boundary

The active v1 release scope is **Android ARM64 only**. Windows, macOS, iOS, and WebGL target definitions remain in the repository for future work, but they do not gate this branch and must not be advertised as supported.

Every accepted final Android native artifact must pass the full regression gate on the exact content-addressed project-input snapshot:

```bash
python3 build/build.py verify-abi-final
```

Phase 4 closes only when the final Android ARM64 artifact is accepted by:

```bash
python3 build/build.py verify-phase4
```

**Next authoritative task:** Phase 12 P12.1 — validate the package in Unity 2021.3 LTS.


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
- [x] Current-machine compiled execution of the maintained native verification inventory completed during Phase 3 and was rerun during the final ABI-v1 freeze.

**Documentation:** `PHASE1_NATIVE_ENGINE.md`

---

# Phase 2 — Production C ABI Candidate and Safety Boundary

**Status:** IMPLEMENTATION COMPLETE
**Current ABI source:** final ABI v1 `1/2`; this phase originally produced the ABI-v1-RC `1/1` contract later promoted in Phase 6.

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
- [x] Public header generation and export inventory are maintained.
- [x] Pinned cbindgen regeneration/drift proof completed in Phase 3 and remains part of `verify-abi-final`.

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
- [x] P3.9 Pinned cbindgen header regeneration/drift command.
- [x] P3.10 Canonical local ABI verification command established.
- [x] P3.11 ABI source promoted to release-candidate state `1/1`.
- [x] P3.12 Run `cargo fmt --check` locally on the canonical source.
- [x] P3.13 Run Clippy with `-D warnings` locally.
- [x] P3.14 Run the maintained Rust test inventory locally.
- [x] P3.15 Build the host release native library locally.
- [x] P3.16 Regenerate/diff the public header with pinned cbindgen locally.
- [x] P3.17 Record Phase 3 evidence for the exact content-addressed source snapshot.

### Current Phase 3 gate

**COMPLETE on the local Linux host.**

Phase 4 artifacts may not be accepted until the maintained final ABI verification gate passes on the exact clean artifact-producing source revision.

**Documentation:** `PHASE3_NATIVE_VERIFICATION.md`

---

# Phase 4 — Native Build and Artifact Staging

**Status:** COMPLETE FOR ACTIVE ANDROID ARM64-ONLY RELEASE SCOPE

**Goal:** produce the reproducible Android ARM64 native library required by the supported release target. The accepted Phase 4 artifact is historical ABI-v1-RC `1/1`; Phase 6 requires one final clean-tree rebuild at ABI v1 `1/2` before release closure.

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

---

# Phase 6 — Minimal Managed ABI Conformance and Final ABI v1 Freeze

**Status:** COMPLETE — ABI v1 / Final Native Payload Gate passed

**Current evidence:** the complete 31-function managed ABI surface passed standalone managed, Unity, direct ARM64 Android, IL2CPP packaging, and real-device validation at final ABI v1 `1/2`. The final Android Phase 4 artifact and Phase 5 Unity payload were then rebuilt/restaged from content-addressed source snapshot `sha256:68fb502c6bc48c83b2239f5212d98fd6a7f3f777c587cb286876121c58752731`; accepted binary SHA-256 is `7bdca92aae2939e5098292294ee7f7d730d5eee1c718d87f65a3f22349338f66`. A fresh Unity ARM64 IL2CPP APK build verified the accepted staged library's ELF program headers and runtime-loaded `PT_LOAD` bytes.


**Early work already present:**

- [x] P6.E1 Unity low-level P/Invoke migrated from obsolete bootstrap symbols to the production `tu_*` ABI surface.
- [x] P6.E2 Managed/native ABI structure-size guards exist for the current ABI surface.

**Formal Phase 6 tasks:**

- [x] P6.1 Load the Android ARM64 native library through the managed wrapper in a real Android Player/device environment.
- [x] P6.2 Validate ABI version/stage/capability handshake from C# and Unity host execution.
- [x] P6.3 Validate managed struct layout/size/enum mapping against native ABI.
- [x] P6.4 Context create/destroy/clear managed round trip.
- [x] P6.5 Node create/remove/style managed round trip.
- [x] P6.6 Topology/bulk style upload managed round trip.
- [x] P6.7 Cached measurement single/bulk managed round trip.
- [x] P6.8 Calc/Grid resource managed round trip.
- [x] P6.9 Compute and single/bulk layout retrieval managed round trip.
- [x] P6.10 Validate status and thread-local last-error diagnostics through P/Invoke.
- [x] P6.11 Validate Android library naming/loading rules in a real Android Player; `libtaffy_ugui.so` loaded successfully on ARM64 Android.
- [x] P6.12 Resolve every ABI discrepancy before final freeze. The stale Android payload exposed the fixed `tu_copy_last_error` regression; rebuilding from current source restored the full diagnostic and passed real-device conformance.
- [x] P6.13 Freeze final ABI v1 (`stage=2`) and require final stage by default in the managed wrapper.
- [x] P6.14 Rebuild the active Android ARM64 Phase 4 release artifact from final ABI v1 using the exact content-addressed source snapshot.
- [x] P6.15 Re-stage and verify the Android Phase 5 package artifact from the accepted final ABI-v1 Phase 4 artifact.
- [x] P6.16 Declare **ABI v1 / Final Native Payload Gate** complete after P6.14–P6.15 and final Unity ARM64 package integration verification against the accepted staged payload.

### Phase 6 validation note

Only permanent ABI guards, maintained regression tests, and release tooling remain tracked. Disposable development-validation artifacts are local-only by repository policy.

### Phase 6 exit gate

**PASS.** The final ABI source passed managed/Unity and physical Android validation; the final Phase 4/5 Android payload was rebuilt/restaged from the same content-addressed snapshot and verified in a fresh Unity ARM64 IL2CPP APK. Phase 7 may begin.

**Documentation:** `PHASE6_MANAGED_ABI.md`

---

# Phase 7 — Minimal Working Unity uGUI Product

**Status:** COMPLETE — minimal production uGUI bridge gate passed

Production behavior now includes persistent native contexts/root nodes, stable `RectTransform`→native-node mapping, incremental style/topology synchronization, native intrinsic size reporting, cached two-axis arrangement, nested Taffy groups, `LayoutElement`/`ignoreLayout` compatibility, and permanent Unity regression tests.

- [x] P7.1 Production persistent managed/native context lifecycle.
- [x] P7.2 Stable mapping between RectTransforms and native node handles.
- [x] P7.3 Correct topology synchronization without rebuilding everything unnecessarily.
- [x] P7.4 Correct `CalculateLayoutInputHorizontal/Vertical` integration.
- [x] P7.5 Correct `SetLayoutHorizontal/Vertical` application.
- [x] P7.6 Report own min/preferred/flexible size through `SetLayoutInputForAxis`.
- [x] P7.7 Support nested Taffy layout groups.
- [x] P7.8 Preserve `LayoutElement` behavior/ignoreLayout semantics.
- [x] P7.9 Apply native geometry to RectTransform without replacing uGUI rendering/input components.
- [x] P7.10 Minimal Unity Edit Mode/Play Mode verification.

### Phase 7 validation note

- VS Code Problems: 0 diagnostics for runtime/tests.
- Native quality regression gate: 44/44 Rust tests plus rustfmt, Clippy, and release build pass.
- Unity `6000.4.3f1` Edit Mode: 4/4 permanent package tests pass.
- Unity `6000.4.3f1` Play Mode: 1/1 permanent package test passes.
- Android ARM64 IL2CPP development APK build passes and packages the accepted Phase 6 native payload with byte-identical runtime-loaded ELF `PT_LOAD` segments.
- No physical Android device was available for the final Phase 7 run; physical-platform coverage remains Phase 12.

**Documentation:** `PHASE7_MINIMAL_UGUI.md`

---

# Phase 8 — Production Flex / Block / Float / Measurement Unity Integration

**Status:** COMPLETE — production Flex/Block/Float/measurement gate passed

- [x] P8.1 Complete Unity authoring for core size/min/max/box model.
- [x] P8.2 Complete Flex container authoring.
- [x] P8.3 Complete Flex item authoring.
- [x] P8.4 Block/FlowRoot/Float/Clear Unity authoring.
- [x] P8.5 Position/inset/overflow/box-sizing/direction/aspect integration.
- [x] P8.6 Managed measurement cache orchestration.
- [x] P8.7 TextMeshPro measurement adapter.
- [x] P8.8 Unity Text adapter where retained.
- [x] P8.9 Image/replaced-element measurement adapter.
- [x] P8.10 No managed callback during native Taffy computation.
- [x] P8.11 Measurement invalidation on text/font/size/style changes.
- [x] P8.12 Production regression tests for Flex/Block/Float/measurement behavior.

### Phase 8 validation note

- VS Code Problems: 0 diagnostics for runtime/tests.
- Final ABI/native regression: 44/44 Rust tests, rustfmt, Clippy, release build, and cbindgen drift gate pass.
- Unity `6000.4.3f1` Edit Mode: 14/14 permanent package tests pass.
- Unity `6000.4.3f1` Play Mode: 3/3 permanent package tests pass.
- Android ARM64 artifact/provenance was rebuilt/restaged for current source snapshot `sha256:0eb0a1c56f841cebf48758d6af045533ab69e68e9e76b8f05611712ce282c8f4`; Phase 4/5 gates pass.
- Fresh Android ARM64 IL2CPP APK build passes with `TaffyUGUI.Runtime` + TextMeshPro and byte-identical runtime-loaded Taffy ELF segments.
- No physical Android device was attached for the final Phase 8 APK run; comprehensive Unity Player device coverage remains Phase 12.

**Documentation:** `PHASE8_FLEX_BLOCK_MEASUREMENT.md`

---

# Phase 9 — Complete Grid and Calc Unity Authoring

**Status:** COMPLETE — production Grid/Calc Unity gate passed
**Note:** native Grid/Calc engine support was already implemented in Phase 1; Phase 9 required no ABI expansion.

- [x] P9.1 Serializable Grid track/unit data model.
- [x] P9.2 Grid explicit row/column authoring.
- [x] P9.3 Auto/implicit track authoring.
- [x] P9.4 `fr`, minmax, min/max-content authoring.
- [x] P9.5 repeat/count/auto-fill/auto-fit authoring.
- [x] P9.6 named lines/spans.
- [x] P9.7 named template areas.
- [x] P9.8 grid-auto-flow modes.
- [x] P9.9 Grid item placement authoring.
- [x] P9.10 justify-items/justify-self/alignment authoring.
- [x] P9.11 Typed Calc authoring/resource lifecycle.
- [x] P9.12 Grid/Calc diagnostics and validation in Unity.

### Phase 9 validation note

- VS Code Problems: 0 diagnostics for runtime/tests.
- Native quality regression: rustfmt, Clippy `-D warnings`, 44/44 Rust tests, and release build pass.
- Unity `6000.4.3f1` Edit Mode: 22/22 permanent package tests pass.
- Unity `6000.4.3f1` Play Mode: 5/5 permanent package tests pass.
- Final ABI/Phase 4/Phase 5 provenance and fresh Android ARM64 IL2CPP package checks are bound to `sha256:c9f0607bc7808c2e3fb857c5785780d31b9b1112fbe39275b8b23b6088cb9698`.
- No physical Android device was attached for the final Phase 9 APK run; comprehensive Unity Player/device coverage remains Phase 12.

**Documentation:** `PHASE9_GRID_CALC_UNITY.md`

---

# Phase 10 — Responsive and Integration Hardening

**Status:** COMPLETE — responsive/integration production gate passed

- [x] P10.1 Responsive profile/breakpoint system.
- [x] P10.2 Intrinsic resize/CanvasScaler responsiveness.
- [x] P10.3 Safe-area integration.
- [x] P10.4 ScrollRect content/viewport bridge.
- [x] P10.5 ContentSizeFitter interaction rules.
- [x] P10.6 AspectRatioFitter interaction rules.
- [x] P10.7 Animation-driven dirty invalidation.
- [x] P10.8 Pixel rounding strategy.
- [x] P10.9 Layout rebuild-loop protection.
- [x] P10.10 Runtime override API where required.

### Phase 10 validation note

- VS Code Problems: 0 diagnostics for runtime/tests.
- Native quality regression: rustfmt, Clippy `-D warnings`, 44/44 Rust tests, and release build pass.
- Unity `6000.4.3f1` Edit Mode: 29/29 permanent package tests pass.
- Unity `6000.4.3f1` Play Mode: 9/9 permanent package tests pass.
- Final ABI/Phase 4/Phase 5 provenance and fresh Android ARM64 IL2CPP package checks are bound to `sha256:3228f12128c07fd6c470a7bc9119a4ba810f7718d98c6ae9537086030beaa0fc`.
- Physical Android smoke execution passes on `CPH2723` (Android 16 / API 36, ARM64-v8a) with Unity `6000.4.3f1` IL2CPP. Device marker: `TAFFY_PHASE10_DEVICE_PASS:profile=phone:height=328.0:suppressed=4`; native load succeeds and no Unity/AndroidRuntime/native fatal errors are present. Comprehensive platform coverage remains Phase 12.

**Documentation:** `PHASE10_RESPONSIVE_INTEGRATION.md`

---

# Phase 11 — Editor Tooling and Migration

**Status:** COMPLETE — editor authoring/migration production gate passed

- [x] P11.1 `TaffyLayoutGroup` custom inspector.
- [x] P11.2 `TaffyLayoutItem` custom inspector.
- [x] P11.3 Length/dimension/rect property drawers.
- [x] P11.4 Grid track/placement/area editor tooling.
- [x] P11.5 Scene-view layout visualization.
- [x] P11.6 Layout debugger/diagnostics window.
- [x] P11.7 HorizontalLayoutGroup migration.
- [x] P11.8 VerticalLayoutGroup migration.
- [x] P11.9 GridLayoutGroup migration where semantically safe.
- [x] P11.10 Prefab/Undo/serialized-data-safe migration behavior.
- [x] P11.11 Batch migration workflow.

### Phase 11 validation note

- Editor tooling is isolated in the Editor-only `TaffyUGUI.Editor` assembly.
- Unity `6000.4.3f1` Edit Mode: **38/38** permanent package tests pass, including 9 Phase 11 editor/migration tests.
- Unity `6000.4.3f1` Play Mode: **9/9** permanent package tests pass.
- Horizontal/Vertical migration is Undo-aware and preserves supported padding/alignment/child sizing/grow semantics.
- Grid migration is conservative: only deterministic fixed-row/fixed-column configurations are changed; unsupported configurations remain untouched with diagnostics.
- Prefab-instance migration retains the prefab connection and does not mutate the prefab asset.
- Final ABI/Phase 4/Phase 5 provenance and fresh Android ARM64 IL2CPP Player checks are bound to `sha256:3228f12128c07fd6c470a7bc9119a4ba810f7718d98c6ae9537086030beaa0fc`.
- Fresh Android ARM64 IL2CPP Player build passes with `TaffyUGUI.Runtime.dll` included and `TaffyUGUI.Editor.dll` excluded from Player/IL2CPP assemblies; packaged Taffy runtime `PT_LOAD` bytes match the accepted staged payload.

**Documentation:** `PHASE11_EDITOR_TOOLING_MIGRATION.md`

---

# Phase 12 — Real Unity Platform Validation

**Status:** ACTIVE — Phase 11 gate complete; P12.1 is next

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

Phase 11 is complete. Phase 12 is now authoritative.

**Next task:** P12.1 — validate the package in Unity 2021.3 LTS.

Phase 12 expands from the current Unity 6 / Android ARM64 development gates into the explicit Unity-version and real Player platform compatibility matrix.
