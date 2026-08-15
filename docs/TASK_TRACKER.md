# TaffyUGUI — Development Task Tracker

**Purpose:** Single source of truth for current progress, next task, blockers, phase gates, and regression obligations.  
**Development model:** Rust-first, native-complete, then Unity integration.  
**Master plan:** [DEVELOPMENT_PLAN.md](DEVELOPMENT_PLAN.md)  
**Native build contract:** [NATIVE_LIBRARY_BUILD_PLAN.md](NATIVE_LIBRARY_BUILD_PLAN.md)

---

# 1. Mandatory Development Order

TaffyUGUI is built as two first-class deliverables:

```text
1. Rust/Taffy native layout library
        ↓
2. Unity UPM package containing and using that native library
```

The project must not advance Unity feature development while the native foundation required by those features is incomplete.

The required order is:

```text
Rust project/toolchain setup
    ↓
Taffy dependency and feature setup
    ↓
Full Rust layout engine wrapper
    ↓
Stable C ABI
    ↓
Native unit/golden/ABI tests
    ↓
Cross-platform native builds
    ↓
Unity-ready native artifact staging
    ↓
NATIVE MILESTONE COMPLETE
    ↓
Unity managed/native integration
    ↓
Minimal working uGUI layout
    ↓
Full uGUI compatibility/features
    ↓
Editor tooling/migration
    ↓
Unity platform player validation
    ↓
Performance/reliability hardening
    ↓
Final UPM release
```

**Important:** existing Unity C# files already in the repository are bootstrap scaffolding. They may be corrected when required for repository health, but feature development on them is paused until the native milestone defined below is complete.

---

# 2. Tracker Rules

## Task states

- `[x]` — implemented and verified at the phase-required level.
- `[ ]` — incomplete.
- `NEXT TASK` — the single task to work on next.
- `BLOCKED` — cannot proceed until the stated dependency is resolved.

## Phase states

- **NOT STARTED** — no work should be assumed complete.
- **IN PROGRESS** — active development phase.
- **STABILIZATION** — feature work stopped; only fixes/tests/gate work allowed.
- **COMPLETE** — every task and the phase gate passed.

## No-flow-break rule

1. Only one phase is active at a time.
2. The next phase does not start until the current phase gate passes.
3. Every completed phase must remain independently buildable/testable.
4. A regression in any earlier completed phase immediately stops new feature work.
5. ABI or serialized API breaking changes require explicit versioning/migration.
6. A task is not complete because code exists; the defined verification must pass.
7. Native artifacts are never accepted solely because compilation succeeded; they must pass the appropriate verification path.
8. A platform is not advertised as supported until both its native artifact and Unity player integration have been validated.

---

# 3. Current State

**Current phase:** Phase 0 — Rust Project and Toolchain Foundation  
**Phase state:** IN PROGRESS  
**Native milestone:** NOT COMPLETE  
**Unity feature development:** PAUSED until Native Milestone Gate passes  
**Current CI state:** Failing at Rust formatting/Clippy before the full test/build pipeline completes.  
**NEXT TASK:** **P0.5 — Repair the Rust quality gate and obtain a fully green host CI run.**

## Current known blockers

- `native/src/lib.rs` is not canonical `rustfmt` output.
- exported `unsafe extern "C"` functions require accurate `# Safety` documentation for Clippy.
- because those checks fail, later test/release-build steps are not yet trusted as green.

## Existing bootstrap work

- [x] Repository created.
- [x] MIT license added.
- [x] AI-generated-code disclaimer added.
- [x] Rust crate exists under `native/`.
- [x] crate emits `cdylib` and `staticlib`.
- [x] Taffy is exact-version pinned in `Cargo.toml`.
- [x] initial C ABI implementation exists.
- [x] initial Unity UPM scaffold exists.
- [x] initial host CI workflow exists.
- [ ] Rust host CI fully green.
- [ ] full native feature surface complete.
- [ ] stable native ABI complete.
- [ ] all required Unity target-family native artifacts compiled/staged.
- [ ] Native Milestone Gate passed.

---

# 4. Phase Summary

| Phase | Name | Stable outcome | State |
|---|---|---|---|
| 0 | Rust Project and Toolchain Foundation | Reproducible Rust/Taffy crate builds cleanly and CI is trustworthy | **IN PROGRESS** |
| 1 | Full Rust/Taffy Layout Engine | Native library implements the complete v1 layout feature surface independent of Unity | NOT STARTED |
| 2 | Production C ABI and Native Safety | Stable, versioned, safe FFI contract exposes the native engine | NOT STARTED |
| 3 | Native Verification and ABI Freeze | Golden/ABI/smoke tests prove the host native library and freeze ABI v1 | NOT STARTED |
| 4 | Cross-Platform Native Compilation | Unity-target native artifacts compile reproducibly for Windows, macOS, Android, iOS, WebGL | NOT STARTED |
| 5 | Unity-Ready Native Artifact Package | Verified artifacts are staged under `UnityPackage/Plugins` with deterministic metadata | NOT STARTED |
| 6 | Unity Managed/Native Foundation | Unity loads the packaged native library and owns contexts safely | NOT STARTED |
| 7 | Minimal Working uGUI Flex Product | Existing uGUI panel works through Rust/Taffy end-to-end | NOT STARTED |
| 8 | Production Flex/Box/Measurement Compatibility | Flex, box model, LayoutElement, Text/TMP, image and nested layouts are stable | NOT STARTED |
| 9 | CSS Grid | Grid becomes a first-class Unity authoring/runtime feature | NOT STARTED |
| 10 | Responsive and Unity Integration Hardening | Breakpoints, runtime overrides, ScrollRect, Canvas Scaler, safe area and lifecycle are stable | NOT STARTED |
| 11 | Editor Tooling and Migration | Inspectors, diagnostics, debugger and safe migration tools are production-usable | NOT STARTED |
| 12 | Unity Cross-Platform Player Validation | Same package passes real Unity player/device/browser lanes on supported platforms | NOT STARTED |
| 13 | Performance and Reliability Hardening | Dirty-driven, allocation-controlled, benchmarked implementation | NOT STARTED |
| 14 | v1.0 Release Hardening | Reproducible UPM release reaches the complete project goal | NOT STARTED |

---

# Phase 0 — Rust Project and Toolchain Foundation

**State:** IN PROGRESS  
**Goal:** establish a clean, reproducible Rust project before expanding functionality.

## Tasks

- [x] **P0.1** Create `native/` crate.
- [x] **P0.2** Configure library outputs as `cdylib` + `staticlib`.
- [x] **P0.3** Pin Taffy to an exact version and keep Unity isolated from Taffy Rust types.
- [x] **P0.4** Add host CI for `fmt`, Clippy, tests and release compile.
- [ ] **P0.5 — NEXT TASK** Fix current Rust formatting and Clippy failures without globally suppressing safety checks.
- [ ] **P0.6** Run host CI through all test/release-build steps on Linux, Windows and macOS.
- [ ] **P0.7** Commit and validate `Cargo.lock` for reproducible dependency resolution.
- [ ] **P0.8** Split the growing native crate toward clear modules: `ffi`, `context`, `handles`, `style`, `grid`, `measurement`, `error`, `version`.
- [ ] **P0.9** Document supported Rust toolchain policy/MSRV and release toolchain policy.
- [ ] **P0.10** Add local developer commands/scripts equivalent to CI.

## Phase 0 gate

- `cargo fmt --check` green.
- Clippy green with warnings denied.
- Rust unit tests green.
- host release build green on Linux/Windows/macOS.
- locked dependency graph committed.
- clean clone can reproduce the build.

**Stable deliverable:** trustworthy Rust project foundation.

---

# Phase 1 — Full Rust/Taffy Layout Engine

**State:** NOT STARTED  
**Goal:** implement the complete native v1 layout capability before Unity depends on it.

The Rust library must be useful and testable through Rust/C tests without Unity.

## Core engine tasks

- [ ] **P1.1** Finalize `TaffyTree` ownership model per native context.
- [ ] **P1.2** Implement generation-safe opaque node handles.
- [ ] **P1.3** Implement persistent topology: create/remove/set children/clear.
- [ ] **P1.4** Implement dirty marking and cached native state.
- [ ] **P1.5** Implement full v1 dimension/unit model: Auto, Length/Point, Percent and supported Taffy equivalents.
- [ ] **P1.6** Implement size, min/max size and aspect ratio.
- [ ] **P1.7** Implement margin, padding and layout-border geometry.
- [ ] **P1.8** Implement relative/absolute positioning and insets.
- [ ] **P1.9** Implement direction, overflow and scrollbar reservation semantics supported by the pinned Taffy version.

## Flexbox tasks

- [ ] **P1.10** Flex row/column and reverse directions.
- [ ] **P1.11** Flex wrap/no-wrap/wrap-reverse.
- [ ] **P1.12** flex grow/shrink/basis.
- [ ] **P1.13** gap.
- [ ] **P1.14** align-items/self/content and justify-content.

## Block tasks

- [ ] **P1.15** `Display.Block` mapping and required block-flow behavior.

## Grid tasks

- [ ] **P1.16** native grid track representation.
- [ ] **P1.17** explicit rows/columns.
- [ ] **P1.18** fixed/percent/auto/fr/minmax/repeat cases supported by the pinned Taffy version.
- [ ] **P1.19** implicit tracks and auto tracks.
- [ ] **P1.20** grid auto-flow.
- [ ] **P1.21** row/column placement, spans and alignment.
- [ ] **P1.22** variable-length grid resource lifetime management.

## Measurement tasks

- [ ] **P1.23** native measurement record format supplied from managed/native callers.
- [ ] **P1.24** known-size/available-space measurement inputs required by Taffy.
- [ ] **P1.25** intrinsic/replaced-element metadata required for images/text-like leaves.
- [ ] **P1.26** measurement cache update/invalidation path with no Rust→C# per-node callback dependency.

## Bulk/result tasks

- [ ] **P1.27** bulk node creation/removal where useful.
- [ ] **P1.28** bulk style upload.
- [ ] **P1.29** bulk measurement upload.
- [ ] **P1.30** compute layout once per root.
- [ ] **P1.31** bulk layout result retrieval.

## Phase 1 gate

- all intended native v1 layout properties have deterministic Rust conversion tests.
- Flexbox, Block and Grid golden cases return correct `x/y/width/height` within epsilon.
- measurement records can participate in layout without managed callbacks.
- persistent trees support repeated style/topology updates.
- no Unity code is required to prove the native behavior.

**Stable deliverable:** fully functional Rust/Taffy layout engine implementation.

---

# Phase 2 — Production C ABI and Native Safety

**State:** NOT STARTED  
**Goal:** expose the complete Rust engine through a stable Unity-safe binary contract.

## Tasks

- [ ] **P2.1** Freeze canonical `tu_*` export naming before ABI v1 freeze.
- [ ] **P2.2** Add `tu_get_abi_version`.
- [ ] **P2.3** Add `tu_get_taffy_version`.
- [ ] **P2.4** Add native build/package version query.
- [ ] **P2.5** Add capability flags so managed code can verify supported features.
- [ ] **P2.6** Replace raw/public Rust pointers with opaque context handles where required by the final contract.
- [ ] **P2.7** Define stable numeric error codes.
- [ ] **P2.8** Implement last-error code/message diagnostic path.
- [ ] **P2.9** Validate all incoming enums, lengths, counts, pointers and finite numeric values.
- [ ] **P2.10** Use `#[repr(C)]` POD FFI structs only.
- [ ] **P2.11** remove ABI `bool`; use fixed-width integer representations.
- [ ] **P2.12** define exact numeric enum mappings.
- [ ] **P2.13** prevent stale-handle/use-after-remove behavior.
- [ ] **P2.14** ensure no Rust `Vec`, `String`, references or Taffy node IDs cross ABI.
- [ ] **P2.15** establish panic boundary/no-unwind policy for every FFI entry point.
- [ ] **P2.16** add context owner-thread diagnostics/policy for v1 main-thread ownership.
- [ ] **P2.17** document every unsafe entry point with exact safety requirements.

## Phase 2 gate

- C header/ABI specification can be generated or maintained deterministically.
- all fallible exports return defined status codes.
- invalid/stale inputs fail safely.
- ABI structs/enums have automated layout/value tests.
- panic/unwind cannot cross the C boundary.
- the entire Phase 1 feature surface is available through the C ABI.

**Stable deliverable:** production C ABI for the complete native layout engine.

---

# Phase 3 — Native Verification and ABI v1 Freeze

**State:** NOT STARTED  
**Goal:** prove the compiled host library independently before cross-compilation.

## Tasks

- [ ] **P3.1** Rust unit suite for contexts, handles, styles, grid, measurement, errors and versions.
- [ ] **P3.2** golden layout suite covering Flex, Block, Grid, percent, min/max, margin/padding/gap, absolute and intrinsic measurement.
- [ ] **P3.3** C/C++ ABI smoke-test harness that dynamically loads host shared libraries where applicable.
- [ ] **P3.4** smoke flow: versions → context → nodes → styles → children → compute → bulk results → destroy.
- [ ] **P3.5** struct size/alignment contract tests.
- [ ] **P3.6** enum/value contract tests.
- [ ] **P3.7** stale handle/invalid argument/error-message tests.
- [ ] **P3.8** repeated create/destroy and topology mutation leak/stress tests.
- [ ] **P3.9** initial native benchmark baselines for 100/1,000/5,000 nodes.
- [ ] **P3.10** freeze ABI version `1` only after all above pass.

## Phase 3 gate

- host artifacts pass independent ABI smoke tests.
- golden geometry suite green.
- safety/invalid-input suite green.
- ABI layout/value suite green.
- ABI v1 documented and frozen.

**Stable deliverable:** independently verified native library with ABI v1.

---

# Phase 4 — Cross-Platform Native Compilation

**State:** NOT STARTED  
**Goal:** compile the same ABI v1 native engine into Unity-compatible artifacts for every planned platform family before Unity feature development starts.

## Build-system tasks

- [ ] **P4.1** create one authoritative build driver under `build/` or `scripts/`.
- [ ] **P4.2** toolchain prerequisite detection and actionable errors.
- [ ] **P4.3** deterministic `dist/native/<platform>/<arch>/` staging.
- [ ] **P4.4** record package version, ABI version, Taffy version, target triple and source commit in manifest metadata.
- [ ] **P4.5** CI artifact upload for every supported target lane.

## Windows

- [ ] **P4.6** Windows x86_64 MSVC DLL.
- [ ] **P4.7** Windows ARM64 if included by the supported Unity compatibility matrix.
- [ ] **P4.8** exported symbol and architecture verification.

## macOS

- [ ] **P4.9** Apple Silicon dylib.
- [ ] **P4.10** Intel dylib where supported.
- [ ] **P4.11** universal dylib packaging strategy if chosen.
- [ ] **P4.12** Mach-O architecture/export verification.

## Android

- [ ] **P4.13** Unity-compatible NDK selection/configuration.
- [ ] **P4.14** `arm64-v8a` shared library.
- [ ] **P4.15** `armeabi-v7a` if included by compatibility matrix.
- [ ] **P4.16** `x86_64` if included for emulator/testing support.
- [ ] **P4.17** ELF ABI/export verification.

## iOS

- [ ] **P4.18** device ARM64 static library.
- [ ] **P4.19** simulator ARM64 artifact when supported by baseline workflow.
- [ ] **P4.20** simulator Intel artifact only when needed/supported.
- [ ] **P4.21** static library/XCFramework packaging decision and link verification.

## Unity Web/WebGL

- [ ] **P4.22** identify/version the Emscripten toolchain paired with each supported Unity validation lane.
- [ ] **P4.23** compile WebGL-compatible static/linkable artifact.
- [ ] **P4.24** symbol/link verification using the Unity-compatible Emscripten path.

## Phase 4 gate

- every required platform-family artifact compiles from a clean checkout.
- artifacts expose the same ABI version and symbols.
- architecture/file-format checks pass.
- native host-runnable artifacts pass the ABI smoke layout.
- non-host-runnable artifacts pass static/link checks and are queued for later Unity player validation.
- build outputs are reproducibly staged in `dist/`.

**Stable deliverable:** complete cross-platform native binary set ready for Unity packaging.

---

# Phase 5 — Unity-Ready Native Artifact Package

**State:** NOT STARTED  
**Goal:** convert verified Rust build outputs into the exact native payload the Unity UPM package will consume.

## Tasks

- [ ] **P5.1** create canonical `UnityPackage/Plugins/Windows/...` structure.
- [ ] **P5.2** stage Windows artifacts with final Unity library naming.
- [ ] **P5.3** stage macOS artifacts.
- [ ] **P5.4** stage Android ABI folders/artifacts.
- [ ] **P5.5** stage iOS static/XCFramework assets.
- [ ] **P5.6** stage WebGL linkage assets.
- [ ] **P5.7** generate/commit Unity plugin importer `.meta` files with correct platform/CPU selection.
- [ ] **P5.8** add machine-readable native artifact manifest/checksums.
- [ ] **P5.9** add script that rebuilds and refreshes `UnityPackage/Plugins` deterministically.
- [ ] **P5.10** verify no manually copied/untracked native binary is required.
- [ ] **P5.11** verify managed package version/expected ABI metadata can be matched to staged artifacts.

## Native Milestone Gate

**Unity feature development may start only when all are true:**

- Phases 0–4 are COMPLETE.
- complete native v1 feature surface exists.
- ABI v1 is frozen and tested.
- required platform-family artifacts compile.
- artifacts are staged under `UnityPackage/Plugins` in final structure.
- plugin importer metadata is committed.
- native artifact manifest/checksums exist.
- clean build/staging process is documented and reproducible.

**Stable deliverable:** Unity-ready native engine payload.

---

# Phase 6 — Unity Managed/Native Foundation

**State:** NOT STARTED  
**Goal:** build the safe C# layer on top of the already-complete native artifacts.

## Tasks

- [ ] **P6.1** organize `Runtime/Native` managed types and P/Invoke definitions.
- [ ] **P6.2** platform-specific `DllImport` selection: normal native library vs `__Internal` for iOS/WebGL.
- [ ] **P6.3** mirror ABI POD structs with `StructLayout(LayoutKind.Sequential)`.
- [ ] **P6.4** managed ABI/version/capability handshake before first context creation.
- [ ] **P6.5** `TaffyNativeContext : IDisposable` with use-after-dispose protection.
- [ ] **P6.6** owner-thread checks and native error translation.
- [ ] **P6.7** cleanup on disable/destroy, assembly reload and play-mode exit.
- [ ] **P6.8** managed wrappers for bulk styles, measurements and layouts.
- [ ] **P6.9** Unity Editor native-status smoke check.
- [ ] **P6.10** managed/native contract tests for struct sizes, enum values and ABI version.

## Phase 6 gate

- Unity Editor loads the correct native artifact on the primary development platform.
- ABI handshake succeeds/fails clearly.
- context create/destroy works repeatedly.
- Unity does not require gameplay code to call P/Invoke directly.
- domain/play reload paths leave no known native context leaks.

**Stable deliverable:** safe managed bridge to the finished Rust engine.

---

# Phase 7 — Minimal Working uGUI Flex Product

**State:** NOT STARTED  
**Goal:** first complete end-user vertical slice.

## Tasks

- [ ] **P7.1** production `TaffyLayoutGroup : LayoutGroup` skeleton.
- [ ] **P7.2** child collection using standard uGUI layout lifecycle.
- [ ] **P7.3** persistent Unity-object → native-node mapping.
- [ ] **P7.4** basic Row/Column style authoring.
- [ ] **P7.5** fixed/auto size, padding and gap.
- [ ] **P7.6** compute via Rust library and apply via `SetChildAlongAxis`/Unity layout APIs.
- [ ] **P7.7** hierarchy add/remove/reorder/active-state handling.
- [ ] **P7.8** Play Mode support.
- [ ] **P7.9** Edit Mode/Prefab Mode basic preview.
- [ ] **P7.10** sample with existing Button, Image and text unchanged.
- [ ] **P7.11** geometry regression tests.

## Phase 7 gate

- existing uGUI panel can replace a basic Unity layout group with `TaffyLayoutGroup`.
- layout geometry is actually produced by the Rust library.
- rendering/input remain standard Unity uGUI.
- no custom child component is required for the basic case.
- Play/Edit mode transitions remain stable.

**Stable deliverable:** first genuinely usable TaffyUGUI package.

---

# Phase 8 — Production Flex, Box Model and Measurement Compatibility

**State:** NOT STARTED  
**Goal:** make the package useful for real existing uGUI layouts.

## Flex/box tasks

- [ ] **P8.1** `TaffyLayoutItem` optional per-child overrides.
- [ ] **P8.2** wrap/wrap-reverse.
- [ ] **P8.3** grow/shrink/basis.
- [ ] **P8.4** percent and min/max sizing.
- [ ] **P8.5** alignment/justification.
- [ ] **P8.6** margin, padding, border geometry, aspect ratio.
- [ ] **P8.7** relative/absolute positioning.
- [ ] **P8.8** Block display.
- [ ] **P8.9** nested Taffy groups.

## Unity compatibility/measurement tasks

- [ ] **P8.10** `LayoutElement` mapping and precedence.
- [ ] **P8.11** Image/RawImage intrinsic measurement.
- [ ] **P8.12** Unity Text measurement where supported.
- [ ] **P8.13** optional `TaffyUGUI.TMP` assembly.
- [ ] **P8.14** TMP preferred/wrapped measurement.
- [ ] **P8.15** bounded two-pass text measurement/layout.
- [ ] **P8.16** public `ITaffyMeasureProvider` extension point.
- [ ] **P8.17** ContentSizeFitter/AspectRatioFitter conflict policy.

## Dirty/lifecycle tasks

- [ ] **P8.18** hierarchy/style/measurement/available-size dirty flags.
- [ ] **P8.19** no unconditional `Update()` layout.
- [ ] **P8.20** rebuild generation/reentrancy guards.
- [ ] **P8.21** apply geometry only when changed beyond epsilon.
- [ ] **P8.22** zero steady-state TaffyUGUI allocations target when unchanged.

## Phase 8 gate

- all Phase 7 regressions green.
- common menus/cards/forms with LayoutElement/Text/TMP/images lay out correctly.
- nested layouts stable.
- text wrapping settles within iteration bound.
- unchanged frames do not recompute layout by default.

**Stable deliverable:** production-capable Flex/Block/uGUI compatibility layer.

---

# Phase 9 — CSS Grid

**State:** NOT STARTED  
**Goal:** expose the already-implemented native Grid engine cleanly in Unity.

## Tasks

- [ ] **P9.1** serializable `TaffyGridTrack`, placement and template types.
- [ ] **P9.2** managed/native grid-resource wrappers.
- [ ] **P9.3** explicit rows/columns.
- [ ] **P9.4** supported fixed/percent/auto/fr/minmax/repeat authoring.
- [ ] **P9.5** implicit tracks/auto rows/columns.
- [ ] **P9.6** auto-flow.
- [ ] **P9.7** placement/spans.
- [ ] **P9.8** grid alignment/gap.
- [ ] **P9.9** custom drawers/presets instead of raw arrays.
- [ ] **P9.10** Grid/Flex nesting regressions.

## Phase 9 gate

- Flex/measurement regressions green.
- common Grid layouts author correctly and survive serialization/reload.
- runtime grid template changes do not leak native resources.

**Stable deliverable:** production-usable CSS-style Grid for uGUI.

---

# Phase 10 — Responsive and Unity Integration Hardening

**State:** NOT STARTED

## Tasks

- [ ] **P10.1** `TaffyResponsiveProfile` ScriptableObject.
- [ ] **P10.2** container width/height breakpoints.
- [ ] **P10.3** orientation/aspect rules.
- [ ] **P10.4** deterministic base/profile/local/runtime override precedence.
- [ ] **P10.5** runtime style override API.
- [ ] **P10.6** Canvas Scaler logical-unit validation.
- [ ] **P10.7** ScrollRect bridge and scrollbar reservation.
- [ ] **P10.8** ensure scrolling position changes do not trigger relayout.
- [ ] **P10.9** optional Safe Area integration.
- [ ] **P10.10** animation ownership/conflict diagnostics.
- [ ] **P10.11** harden Edit/Prefab/assembly reload/play lifecycle.
- [ ] **P10.12** graceful native-plugin-unavailable behavior.

## Phase 10 gate

- existing layouts unchanged when responsive features are unused.
- ScrollRect/Canvas Scaler/safe-area interactions are deterministic.
- no uncontrolled rebuild loops or stale native contexts.

**Stable deliverable:** robust Unity-runtime integration around the complete layout engine.

---

# Phase 11 — Editor Tooling and Migration

**State:** NOT STARTED

## Tasks

- [ ] **P11.1** polished `TaffyLayoutGroupEditor`.
- [ ] **P11.2** polished `TaffyLayoutItemEditor`.
- [ ] **P11.3** dimension/rect/grid/responsive property drawers.
- [ ] **P11.4** native status/ABI diagnostics.
- [ ] **P11.5** Layout Debugger tree and timing/dirty information.
- [ ] **P11.6** Scene View visualization.
- [ ] **P11.7** Diagnostics window + support report.
- [ ] **P11.8** hierarchy/project conflict validator.
- [ ] **P11.9** HorizontalLayoutGroup migration with Undo.
- [ ] **P11.10** VerticalLayoutGroup migration with Undo.
- [ ] **P11.11** deterministic GridLayoutGroup migration where possible.
- [ ] **P11.12** preview/report and prefab-safe migration tests.

## Phase 11 gate

- runtime builds have no Editor assembly dependency.
- common problems are diagnosable without native debugging.
- migrations are explicit, Undo-aware and non-destructive.

**Stable deliverable:** production developer workflow.

---

# Phase 12 — Unity Cross-Platform Player Validation

**State:** NOT STARTED  
**Goal:** validate the native artifacts built in Phase 4 inside real Unity targets.

## Tasks

- [ ] **P12.1** Windows Editor/Player validation.
- [ ] **P12.2** macOS Editor/Player validation.
- [ ] **P12.3** Android ARM64 Unity player/device smoke + regression tests.
- [ ] **P12.4** iOS ARM64 Unity player/device linkage/runtime validation.
- [ ] **P12.5** WebGL browser linkage/runtime smoke tests.
- [ ] **P12.6** optional architectures validated only if advertised.
- [ ] **P12.7** build preprocessor verifies required artifact/ABI for selected target.
- [ ] **P12.8** compatibility matrix records exact validated Unity/platform combinations.

## Phase 12 gate

- every advertised platform loads the correct binary automatically.
- ABI handshake and simple layout pass in each target environment.
- representative Flex/Grid/TMP/ScrollRect regressions pass where automation permits.
- no manual Plugin Inspector configuration is needed.

**Stable deliverable:** validated cross-platform Unity package.

---

# Phase 13 — Performance and Reliability Hardening

**State:** NOT STARTED

## Tasks

- [ ] **P13.1** benchmark 100/1,000/5,000/stress nodes.
- [ ] **P13.2** separately measure hierarchy scan, style resolution, measurement, marshaling, Rust compute, result copy and RectTransform application.
- [ ] **P13.3** optimize bulk style/measurement/result paths.
- [ ] **P13.4** pooled/reused managed/native buffers.
- [ ] **P13.5** eliminate avoidable hot-path allocations/LINQ/reflection.
- [ ] **P13.6** incremental dirty upload verification.
- [ ] **P13.7** single-node and partial-tree dirty benchmarks.
- [ ] **P13.8** long-running topology/style/text/context lifecycle soak tests.
- [ ] **P13.9** document measured performance rather than unverified claims.

## Phase 13 gate

- all functional/platform regressions green.
- unchanged frames perform no Taffy layout work by default.
- validated idle case has no steady-state managed allocation attributable to TaffyUGUI after warmup.
- no known native leaks/corruption under soak tests.

**Stable deliverable:** production-hardened implementation.

---

# Phase 14 — v1.0 Release Hardening

**State:** NOT STARTED

## Tasks

- [ ] **P14.1** finalize public managed API and SemVer policy.
- [ ] **P14.2** finalize native ABI compatibility policy.
- [ ] **P14.3** complete CHANGELOG/SECURITY/third-party notices.
- [ ] **P14.4** complete Getting Started/Flex/Grid/Block/measurement/responsive/ScrollRect/platform/performance/troubleshooting docs.
- [ ] **P14.5** complete samples: Flex basics, responsive cards, Grid, ScrollRect, text, breakpoints, migration.
- [ ] **P14.6** final package/build validators.
- [ ] **P14.7** clean rebuild of every advertised native artifact.
- [ ] **P14.8** regenerate artifact manifest/checksums and stage Plugins from CI outputs.
- [ ] **P14.9** produce UPM-ready package/tarball and Git URL installation path.
- [ ] **P14.10** validate clean Unity project install.
- [ ] **P14.11** validate representative existing uGUI project migration/install.
- [ ] **P14.12** run full native + Unity + platform regression matrix.
- [ ] **P14.13** audit README/support claims against actual validation.
- [ ] **P14.14** tag/release v1.0 only after the Definition of Done passes.

## v1.0 gate

- native Rust/Taffy engine is feature-complete and reproducibly buildable.
- ABI v1 is stable and version-checked.
- required native artifacts are compiled and packaged for Windows/macOS/Android/iOS/WebGL.
- Flexbox, Grid and Block/core box model work as documented.
- LayoutElement, Text/TMP, images, nested groups and ScrollRect integration work.
- responsive/runtime override system works.
- editor diagnostics/migration tooling works.
- advertised Unity platforms are validated.
- no continuous unchanged-frame layout computation exists by default.
- automated tests cover exposed style mappings and ABI contracts.
- docs/samples/license/notices/disclaimer are complete.
- a clean clone can build native outputs and assemble the release without hidden local files.

**Stable deliverable:** TaffyUGUI v1.0.

---

# 5. Regression Contract

When any phase becomes COMPLETE, its gate becomes permanent.

Before completing a later phase:

- [ ] all earlier Rust tests pass.
- [ ] all earlier native ABI/golden/smoke tests pass.
- [ ] all earlier required native artifacts still build.
- [ ] all earlier Unity Edit/Play tests pass once Unity phases begin.
- [ ] supported sample geometry remains correct.
- [ ] serialized managed data retains meaning or has a versioned migration.
- [ ] ABI/API compatibility remains valid or an intentional version bump exists.
- [ ] the prior phase's user/developer flow still works end-to-end.

A regression changes the active phase state to **STABILIZATION** until repaired.

---

# 6. Task Completion Protocol

For every task:

1. implement the smallest complete behavior.
2. add/update deterministic tests or verification.
3. run relevant prior regressions.
4. fix warnings/errors rather than hiding them without justification.
5. mark the task `[x]` only after verification.
6. move `NEXT TASK` to the next unfinished task in the active phase.
7. update Current State and blockers.
8. when the last task finishes, enter **STABILIZATION** and run the phase gate.
9. mark the phase COMPLETE only after the gate passes.
10. only then activate the next phase.
