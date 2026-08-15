# TaffyUGUI — Development Task Tracker

**Purpose:** Single operational source of truth for current progress, next task, blockers, phase gates, and regression obligations.  
**Development model:** Native engine first; managed ABI proof second; user-facing Unity features only after frozen ABI v1 artifacts are rebuilt.  
**Normative decisions:** [PROJECT_DECISIONS.md](PROJECT_DECISIONS.md)  
**Master plan:** [DEVELOPMENT_PLAN.md](DEVELOPMENT_PLAN.md)  
**Native build contract:** [NATIVE_LIBRARY_BUILD_PLAN.md](NATIVE_LIBRARY_BUILD_PLAN.md)

---

## 1. Tracker rules

- Only one development phase is active at a time.
- A task is complete only after its required verification passes.
- A phase is complete only after every task and its stability gate pass.
- Completed phase gates become permanent regression requirements.
- If later work breaks an earlier completed gate, feature work stops until the regression is repaired.
- No platform is advertised until its native artifact **and** Unity player path are validated.
- Existing Unity C# files are bootstrap scaffolding until the managed ABI conformance phase.

Task states:

- `[x]` complete and verified.
- `[ ]` incomplete.
- **NEXT TASK** is the single task that should be worked on next.
- **BLOCKED** means the task cannot proceed until the stated dependency is resolved.

Phase states:

- **NOT STARTED**
- **IN PROGRESS**
- **STABILIZATION**
- **COMPLETE**

---

## 2. Final development order

```text
Phase 0  Rust/toolchain foundation
    ↓
Phase 1  Complete Rust/Taffy 0.13 layout engine
    ↓
Phase 2  Production C ABI candidate + safety
    ↓
Phase 3  Native verification + ABI release-candidate lock
    ↓
Phase 4  Cross-platform native compilation using ABI RC
    ↓
Phase 5  Unity-ready native artifact staging using ABI RC
    ↓
         NATIVE ENGINE CANDIDATE COMPLETE
    ↓
Phase 6  Minimal managed ABI conformance
         → prove P/Invoke/struct/calling convention
         → freeze ABI v1
         → rebuild/re-stage every native artifact
    ↓
         ABI V1 / FINAL NATIVE PAYLOAD GATE
    ↓
Phase 7  Minimal working Unity uGUI product
    ↓
Phase 8  Production Flex/Block/Float/measurement compatibility
    ↓
Phase 9  Complete Grid + Calc Unity authoring
    ↓
Phase 10 Responsive + Unity integration hardening
    ↓
Phase 11 Editor tooling + migration
    ↓
Phase 12 Real Unity platform validation
    ↓
Phase 13 Performance/reliability hardening
    ↓
Phase 14 v1.0 release
```

No user-facing Unity layout feature work begins before Phase 6 freezes ABI v1 and regenerates the native payload.

---

# 3. Current state

**Current phase:** Phase 0 — Rust Project and Toolchain Foundation  
**Phase state:** IN PROGRESS  
**Current dependency baseline:** Taffy `0.13.0` exact pin  
**Project MSRV:** Rust `1.82.0`  
**Pinned normal/release toolchain:** Rust `1.97.1`  
**Bootstrap ABI:** `0` (explicitly unstable)  
**Unity feature development:** PAUSED  
**NEXT TASK:** **P0.8 — split the bootstrap native crate into the production module structure.**

### Pre-development decisions now closed

- [x] Taffy baseline finalized at `0.13.0`.
- [x] Taffy full intended feature flags selected: Flex, Grid, Block, Float, Calc, content size, detailed layout info.
- [x] Rust MSRV and release toolchain policy finalized.
- [x] current rustfmt and Clippy failures fixed.
- [x] host CI has passed Linux/Windows/macOS plus MSRV using Taffy 0.13.0.
- [x] `Cargo.lock` generated and committed.
- [x] final production context/node handle model chosen: generation-safe opaque `uint64`.
- [x] final ABI count policy chosen: fixed-width `uint32`, not `usize`/`UIntPtr`.
- [x] generated C-header strategy chosen: `cbindgen` → `include/taffy_ugui.h`.
- [x] panic policy finalized in `PROJECT_DECISIONS.md`.
- [x] ABI release-candidate vs final-v1 freeze timing resolved.
- [x] primary Unity baseline finalized at Unity 2021.3 LTS.
- [x] Android baseline finalized at Unity-compatible NDK r21d (`21.3.6528147`).
- [x] WebGL baseline finalized around Unity 2021.3 bundled Emscripten 2.0.19.
- [x] uGUI dependency explicitly declared as `com.unity.ugui` `1.0.0`.
- [x] canonical build entry point selected: `build/build.py`.
- [x] native release-binary Git/UPM policy finalized.
- [x] Grid named lines/areas and justify-items/self explicitly included in v1 scope.
- [x] production `TaffyLayoutGroup` own min/preferred/flexible input reporting explicitly required.

There are currently **no unresolved architecture decisions that block implementation**.

---

# 4. Phase summary

| Phase | Name | Stable outcome | State |
|---|---|---|---|
| 0 | Rust Project and Toolchain Foundation | Clean reproducible Rust project with fixed dependency/toolchain policy | **IN PROGRESS** |
| 1 | Complete Rust/Taffy Engine | Full intended Taffy 0.13 native layout surface works without Unity | NOT STARTED |
| 2 | Production C ABI Candidate | Safe fixed-width C ABI exposes the complete native engine | NOT STARTED |
| 3 | Native Verification + ABI RC | Golden/safety/header/smoke tests prove an ABI release candidate | NOT STARTED |
| 4 | Cross-Platform Native Compilation | ABI RC builds for Windows/macOS/Android/iOS/WebGL | NOT STARTED |
| 5 | Unity-Ready Native RC Payload | Verified ABI RC artifacts staged under final Unity plugin layout | NOT STARTED |
| 6 | Managed ABI Conformance + ABI v1 | P/Invoke contract proven; ABI v1 frozen; all native artifacts rebuilt | NOT STARTED |
| 7 | Minimal Working uGUI Product | Existing uGUI panel uses Rust/Taffy end-to-end | NOT STARTED |
| 8 | Production Flex/Block/Float/Measurement | Real-world uGUI compatibility and intrinsic measurement stable | NOT STARTED |
| 9 | Complete Grid + Calc Authoring | Full Grid surface and typed Calc authoring stable in Unity | NOT STARTED |
| 10 | Responsive + Unity Integration | ScrollRect/CanvasScaler/safe-area/runtime responsiveness stable | NOT STARTED |
| 11 | Editor Tools + Migration | Inspectors/debugger/diagnostics/migration production-usable | NOT STARTED |
| 12 | Unity Platform Validation | Advertised targets validated in real Unity players | NOT STARTED |
| 13 | Performance + Reliability | Dirty-driven, allocation-controlled, benchmarked implementation | NOT STARTED |
| 14 | v1.0 Release | Reproducible UPM release with verified native payload | NOT STARTED |

---

# Phase 0 — Rust Project and Toolchain Foundation

**State:** IN PROGRESS

- [x] **P0.1** Create `native/` Rust crate.
- [x] **P0.2** Configure `cdylib` + `staticlib` outputs.
- [x] **P0.3** Upgrade and exact-pin Taffy `0.13.0` with intended layout feature flags.
- [x] **P0.4** Pin project toolchain through `rust-toolchain.toml` and declare MSRV 1.82.
- [x] **P0.5** Fix rustfmt/Clippy failures and document current unsafe bootstrap exports.
- [x] **P0.6** Pass host CI on Linux, Windows, macOS and Rust 1.82 MSRV.
- [x] **P0.7** Commit `native/Cargo.lock` and move CI to `--locked` dependency use.
- [ ] **P0.8 — NEXT TASK** Split native code into clear production modules: `ffi`, `context`, `handles`, `style`, `grid`, `measurement`, `error`, `version`.
- [x] **P0.9** Establish canonical build entry point `build/build.py` and make legacy shell tooling non-authoritative.
- [ ] **P0.10** Add CI/script verification for `python build/build.py quality` behavior where practical.
- [ ] **P0.11** Add repository-level native developer documentation for clean-clone setup.

### Phase 0 gate

- `cargo fmt --check` green.
- Clippy green with warnings denied.
- tests green.
- locked host release builds green on Linux/Windows/macOS.
- Rust 1.82 MSRV lane green.
- lockfile committed.
- module ownership is clear enough for Phase 1 implementation.
- clean clone has one documented local quality command.

---

# Phase 1 — Complete Rust/Taffy 0.13 Engine

**State:** NOT STARTED

## Context/tree/handles

- [ ] **P1.1** Production context registry/arena.
- [ ] **P1.2** Generation-safe opaque `uint64` context handles.
- [ ] **P1.3** Generation-safe opaque `uint64` node/resource handles.
- [ ] **P1.4** Persistent `TaffyTree` topology: create/remove/clear/set children.
- [ ] **P1.5** Dirty state and persistent cached native state.

## Core styles

- [ ] **P1.6** display / box generation / `Display::None`.
- [ ] **P1.7** box sizing and direction.
- [ ] **P1.8** Auto/Length/Percent and typed Calc resource/value model.
- [ ] **P1.9** size, min/max, aspect ratio.
- [ ] **P1.10** margin, padding, border geometry.
- [ ] **P1.11** relative/absolute position and insets.
- [ ] **P1.12** overflow and scrollbar reservation.
- [ ] **P1.13** content-size result exposure required for scrolling/diagnostics.

## Flexbox

- [ ] **P1.14** row/column/reverse directions.
- [ ] **P1.15** wrap/no-wrap/wrap-reverse.
- [ ] **P1.16** grow/shrink/basis.
- [ ] **P1.17** align-items/self/content.
- [ ] **P1.18** justify-content and gap.

## Block / FlowRoot / Float

- [ ] **P1.19** Block layout mapping.
- [ ] **P1.20** FlowRoot mapping.
- [ ] **P1.21** float/clear geometry supported by Taffy `float_layout`.

## Grid

- [ ] **P1.22** grid track/resource representation.
- [ ] **P1.23** explicit/implicit rows and columns.
- [ ] **P1.24** fixed/percent/auto/fr/minmax/repeat tracks supported by Taffy.
- [ ] **P1.25** auto-flow and auto tracks.
- [ ] **P1.26** row/column placement and spans.
- [ ] **P1.27** align/justify content.
- [ ] **P1.28** align/justify items and self.
- [ ] **P1.29** named lines.
- [ ] **P1.30** named areas/template areas using Taffy 0.13 representation.
- [ ] **P1.31** detailed Grid layout info required by diagnostics.

## Measurement and transfer

- [ ] **P1.32** native cached measurement-record model supplied by callers.
- [ ] **P1.33** known/available size and intrinsic/replaced-element data.
- [ ] **P1.34** measurement invalidation without Rust→C# per-node callbacks.
- [ ] **P1.35** bulk style upload.
- [ ] **P1.36** bulk measurement upload.
- [ ] **P1.37** bulk topology operations where profiling/design justifies them.
- [ ] **P1.38** one compute call per root/generation.
- [ ] **P1.39** bulk layout/content result retrieval.

### Phase 1 gate

All intended native v1 Taffy features have deterministic native tests and can execute without Unity.

---

# Phase 2 — Production C ABI Candidate and Safety

**State:** NOT STARTED

- [ ] **P2.1** Move all exports to canonical `tu_*` names.
- [ ] **P2.2** Use only fixed-width ABI types; remove persistent raw pointers/`usize`/ABI bools.
- [ ] **P2.3** version/build/Taffy/capability queries.
- [ ] **P2.4** stable error-code enum and last-error diagnostics.
- [ ] **P2.5** context/node/resource API for complete Phase 1 surface.
- [ ] **P2.6** pointer+`uint32` buffer contracts for caller-owned temporary arrays.
- [ ] **P2.7** validate all enums, counts, pointers and numerical inputs.
- [ ] **P2.8** stale/cross-context handle rejection.
- [ ] **P2.9** common FFI panic boundary; no Rust unwind crosses C.
- [ ] **P2.10** document every unsafe function with exact `# Safety` contract.
- [ ] **P2.11** implement main-thread ownership diagnostics/policy.
- [ ] **P2.12** configure cbindgen public surface.
- [ ] **P2.13** generate authoritative `include/taffy_ugui.h`.

### Phase 2 gate

The full engine is callable through one deterministic C ABI candidate and generated header.

---

# Phase 3 — Native Verification and ABI Release-Candidate Lock

**State:** NOT STARTED

- [ ] **P3.1** unit tests for contexts/handles/styles/resources/errors/versions.
- [ ] **P3.2** golden Flex geometry suite.
- [ ] **P3.3** golden Block/FlowRoot/Float suite.
- [ ] **P3.4** golden Grid/named-area/placement suite.
- [ ] **P3.5** Calc and measurement golden suite.
- [ ] **P3.6** struct size/alignment and enum numeric contract tests.
- [ ] **P3.7** invalid/stale/cross-context/malformed-input tests.
- [ ] **P3.8** repeated lifecycle/topology stress tests.
- [ ] **P3.9** C/C++ smoke harness compiled against generated header.
- [ ] **P3.10** compiled host shared-library smoke test.
- [ ] **P3.11** cbindgen regeneration/diff CI check.
- [ ] **P3.12** lock interface as `ABI-v1-RC` for platform compilation; do **not** claim final ABI v1 yet.

---

# Phase 4 — Cross-Platform Native Compilation (ABI RC)

**State:** NOT STARTED

## Build system

- [ ] **P4.1** extend `build/build.py` target registry/prerequisite detection.
- [ ] **P4.2** deterministic `dist/native/<platform>/<arch>` staging.
- [ ] **P4.3** artifact manifest with target, source SHA, Taffy, package/native version, ABI RC and checksum.
- [ ] **P4.4** symbol/file-format/architecture verification.
- [ ] **P4.5** CI artifact upload.

## Targets

- [ ] **P4.6** Windows x64 MSVC DLL.
- [ ] **P4.7** macOS Apple Silicon dylib.
- [ ] **P4.8** macOS Intel dylib and universal strategy if validated.
- [ ] **P4.9** Android ARM64 `.so` using Unity 2021.3 NDK r21d.
- [ ] **P4.10** optional Android ARMv7/x86_64 only if support matrix requires them.
- [ ] **P4.11** iOS ARM64 static library/XCFramework candidate.
- [ ] **P4.12** optional iOS simulator slices.
- [ ] **P4.13** WebGL static/linkage artifact using Unity 2021.3 bundled Emscripten 2.0.19.

### Phase 4 gate

Every required platform-family artifact compiles with identical ABI RC symbols/version and passes host/static/link verification appropriate to that target.

---

# Phase 5 — Unity-Ready Native RC Payload

**State:** NOT STARTED

- [ ] **P5.1** canonical `UnityPackage/Plugins` platform structure.
- [ ] **P5.2** stage Windows/macOS/Android/iOS/WebGL RC artifacts.
- [ ] **P5.3** generate/commit correct Unity plugin importer `.meta` files.
- [ ] **P5.4** stage native artifact manifest/checksums.
- [ ] **P5.5** `build/build.py stage-unity` deterministic refresh.
- [ ] **P5.6** verify clean clone can regenerate payload without manual copying.
- [ ] **P5.7** establish committed binary verification for Git-tag UPM installs.

### Native Engine Candidate Gate

Complete engine + ABI RC + all required target-family builds + final Unity plugin directory shape are ready. User-facing Unity feature work is still blocked pending Phase 6.

---

# Phase 6 — Managed ABI Conformance, ABI v1 Freeze, Final Native Rebuild

**State:** NOT STARTED

This phase is deliberately limited to proving the binary boundary. It is not layout-feature development.

- [ ] **P6.1** internal managed P/Invoke declarations matching generated header.
- [ ] **P6.2** managed fixed-width structs/enums and size/alignment assertions.
- [ ] **P6.3** desktop/Android `Cdecl`; iOS/WebGL internal linkage selection.
- [ ] **P6.4** minimal `TaffyNativeContext : IDisposable` for conformance smoke flow.
- [ ] **P6.5** ABI/version/capability handshake before context use.
- [ ] **P6.6** native error translation and deterministic disposal.
- [ ] **P6.7** one real Unity 2021.3 Editor smoke flow on the primary desktop platform.
- [ ] **P6.8** freeze final **ABI v1** only after conformance passes.
- [ ] **P6.9** rebuild all Phase 4 native targets against ABI v1 from clean source.
- [ ] **P6.10** re-run native smoke/static/link checks.
- [ ] **P6.11** re-stage/commit all `UnityPackage/Plugins` binaries and manifests for ABI v1.

### ABI v1 / Final Native Payload Gate

No user-facing layout feature work begins until this gate passes.

---

# Phase 7 — Minimal Working Unity uGUI Product

**State:** NOT STARTED

- [ ] **P7.1** production `TaffyLayoutGroup : LayoutGroup` foundation.
- [ ] **P7.2** persistent Unity object → native node mapping.
- [ ] **P7.3** direct child collection and hierarchy updates.
- [ ] **P7.4** basic Row/Column + Auto/Point size + padding/gap.
- [ ] **P7.5** one native compute/application lifecycle per required generation.
- [ ] **P7.6** apply child geometry through Unity layout APIs.
- [ ] **P7.7** report group min/preferred/flexible width/height using `SetLayoutInputForAxis` as appropriate.
- [ ] **P7.8** nested/parent Unity layout-input regression tests.
- [ ] **P7.9** Edit Mode and Play Mode lifecycle.
- [ ] **P7.10** sample preserving normal Button/Image/Text rendering/input.

---

# Phase 8 — Production Flex/Block/Float/Measurement Compatibility

**State:** NOT STARTED

- [ ] full Flex authoring and optional `TaffyLayoutItem` overrides.
- [ ] complete box model / position / overflow / Block / FlowRoot / Float authoring.
- [ ] `LayoutElement` precedence and compatibility.
- [ ] Image/RawImage intrinsic measurement.
- [ ] Unity Text measurement where available.
- [ ] optional TMP assembly and bounded width-dependent two-pass measurement.
- [ ] public custom measurement provider.
- [ ] nested Taffy groups.
- [ ] explicit hierarchy/style/measurement/available-size dirty flags.
- [ ] no unconditional `Update()` layout.
- [ ] no duplicate horizontal/vertical compute unless a bounded measurement pass requires it.
- [ ] geometry epsilon application and reusable buffers.

---

# Phase 9 — Complete Grid + Calc Unity Authoring

**State:** NOT STARTED

- [ ] serializable typed Grid track/template/placement resources.
- [ ] explicit/implicit tracks and auto-flow.
- [ ] fixed/percent/auto/fr/minmax/repeat.
- [ ] placement/spans.
- [ ] align/justify content/items/self.
- [ ] named lines and named template areas.
- [ ] friendly inspectors/property drawers/presets.
- [ ] typed Calc expression authoring/resource lifecycle.
- [ ] Grid/Flex/Block nesting regressions.

---

# Phase 10 — Responsive and Unity Integration Hardening

**State:** NOT STARTED

- [ ] container responsive profiles and deterministic override precedence.
- [ ] runtime non-serialized style overrides.
- [ ] Canvas Scaler logical-unit validation.
- [ ] ScrollRect integration and content-size handling.
- [ ] scrolling movement does not trigger relayout.
- [ ] safe-area helper.
- [ ] animator/tween ownership diagnostics.
- [ ] domain reload/assembly reload/play-mode cleanup.
- [ ] graceful plugin unavailable/ABI mismatch diagnostics.

---

# Phase 11 — Editor Tooling and Migration

**State:** NOT STARTED

- [ ] polished group/item inspectors.
- [ ] dimension/Grid/Calc/responsive drawers.
- [ ] native status and ABI diagnostics.
- [ ] layout debugger and Scene View visualization.
- [ ] support report/conflict validator.
- [ ] HorizontalLayoutGroup migration with Undo.
- [ ] VerticalLayoutGroup migration with Undo.
- [ ] deterministic GridLayoutGroup migration where semantics can be preserved.
- [ ] preview/report/prefab-safe tests.

---

# Phase 12 — Unity Cross-Platform Player Validation

**State:** NOT STARTED

- [ ] Windows 10/11 x64 Editor/player.
- [ ] macOS supported slices Editor/player.
- [ ] Android ARM64 device/player using Unity 2021.3-compatible toolchain.
- [ ] iOS ARM64 Unity→Xcode→device linkage/runtime.
- [ ] WebGL Unity 2021.3→browser linkage/runtime.
- [ ] secondary Unity 2022.3 compatibility lane.
- [ ] selected Unity 6 forward-compatibility lane.
- [ ] backward Unity 2019.4 investigation only after primary lanes are stable.
- [ ] exact compatibility matrix published from evidence.

---

# Phase 13 — Performance and Reliability Hardening

**State:** NOT STARTED

- [ ] benchmark 100/1,000/5,000/10,000-node cases.
- [ ] separately measure C# scan/style/measurement/marshal, Rust compute, result copy, RectTransform apply and Canvas cost.
- [ ] optimize bulk operations and dirty uploads.
- [ ] reusable managed/native buffers.
- [ ] no steady-state TaffyUGUI allocations in validated unchanged case after warmup.
- [ ] single-node/partial-tree dirty benchmarks.
- [ ] long lifecycle/topology/text/Grid/Calc soak tests.
- [ ] document measured results only.

---

# Phase 14 — v1.0 Release Hardening

**State:** NOT STARTED

- [ ] finalize managed SemVer and ABI compatibility policy.
- [ ] CHANGELOG / SECURITY / third-party notices.
- [ ] complete Getting Started/native build/Flex/Grid/Block/Float/Calc/measurement/responsive/ScrollRect/platform/performance/troubleshooting docs.
- [ ] complete samples.
- [ ] clean rebuild of all advertised native artifacts.
- [ ] verify committed `UnityPackage/Plugins` payload against generated manifest/checksums.
- [ ] clean Unity project install.
- [ ] representative existing uGUI migration/install.
- [ ] full native + Unity + platform matrix.
- [ ] audit README claims against actual validation.
- [ ] tag v1.0 only after all gates pass.

---

## 5. Permanent task completion protocol

For every task:

1. implement the smallest complete behavior;
2. add/update deterministic tests or verification;
3. run relevant earlier regressions;
4. fix warnings/errors rather than suppressing them without a documented reason;
5. mark `[x]` only after verification;
6. move **NEXT TASK** to the next unfinished item in the active phase;
7. update current state/blockers;
8. when the final task finishes, enter **STABILIZATION** and run the phase gate;
9. mark phase COMPLETE only when the gate passes;
10. only then activate the next phase.
