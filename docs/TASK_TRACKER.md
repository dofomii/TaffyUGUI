# TaffyUGUI — Development Task Tracker

**Purpose:** Single source of truth for development progress, current phase, next task, blockers, and phase stability gates.  
**Development model:** Build a small but complete working product first, then improve it phase-by-phase without breaking the working flow established by earlier phases.  
**Related plan:** [DEVELOPMENT_PLAN.md](DEVELOPMENT_PLAN.md)

---

## 1. How This Tracker Is Used

This file is the operational tracker for TaffyUGUI development.

Every development session should begin by reading:

1. **Current State** below.
2. The active phase.
3. The **NEXT TASK** marker.
4. Any blocker attached to that task.
5. The phase exit gate.

Every completed development task must update this document in the same change or immediately afterward.

### Task states

- `[x]` — completed and verified at the level required by the current phase.
- `[ ]` — not completed.
- `NEXT TASK` — the single task that should be worked on next.
- `BLOCKED` — cannot proceed until the stated blocker is resolved.

### Phase states

- **NOT STARTED** — no implementation work should be assumed complete.
- **IN PROGRESS** — this is the active development phase.
- **STABILIZATION** — feature work is stopped; only tests, fixes, documentation, and gate work are allowed.
- **COMPLETE** — all tasks and the stability gate have passed.

---

# 2. Core Phase Rule — Never Break the Working Flow

TaffyUGUI is developed as a sequence of stable vertical slices.

A phase is **not complete because its feature code exists**. It is complete only when the complete user flow for that phase works from installation through Unity layout output and all required regression checks pass.

The following rules are mandatory:

1. **Only one phase is active at a time.**
2. **Do not start the next phase until the current phase exit gate passes.**
3. Every phase must leave the repository in a usable and internally consistent state.
4. Features from completed phases become regression requirements for every later phase.
5. If later work breaks an earlier completed phase, new feature work stops until the regression is repaired.
6. New native ABI behavior must remain compatible unless an intentional ABI version bump is documented and tested.
7. Existing Unity-facing serialized data must not be silently broken by later phases.
8. Experimental features must not destabilize the production path. Keep them disabled, isolated, or postponed.
9. No phase may depend on a future phase to make its advertised workflow functional.
10. A task is checked only after implementation **and** the verification described by the phase.

This means an early phase may support fewer features or fewer platforms, but what it claims to support must work reliably before development expands further.

---

# 3. Current State

**Current phase:** Phase 0 — Foundation and Native Trust  
**Phase state:** IN PROGRESS  
**Current usable scope:** Repository/package/native scaffolding exists, but the native foundation has not yet passed its stability gate.  
**Current CI state:** Failing. The latest native CI currently fails `cargo fmt --check` and Clippy safety-documentation checks before tests/release builds can complete.  
**NEXT TASK:** **P0.4 — Repair the native Rust quality gate until format and Clippy are green.**

### Current known blocker details

- `native/src/lib.rs` does not match `rustfmt` output.
- Exported `unsafe extern "C"` functions are rejected by Clippy because their documentation is missing required `# Safety` sections.
- Tests and release builds are currently skipped/cancelled after the earlier CI failures.

### Work already present in the repository

- [x] MIT license added.
- [x] AI-generated-code disclaimer added to README.
- [x] End-to-end development plan added.
- [x] Unity UPM package skeleton added.
- [x] Initial `TaffyLayoutGroup`, `TaffyLayoutItem`, and native wrapper scaffolding added.
- [x] Rust crate created and Taffy pinned to an exact version (`0.12.2`).
- [x] Initial native CI workflow created for Linux, Windows, and macOS host builds.
- [ ] Native CI fully green.
- [ ] Unity vertical slice proven in a real Unity project.

---

# 4. Phase Summary

| Phase | Name | Stable outcome | State |
|---|---|---|---|
| 0 | Foundation and Native Trust | Native engine/API is buildable, testable, versioned, and trustworthy enough for Unity integration | **IN PROGRESS** |
| 1 | Minimal Working Unity Flex Layout | A real Unity uGUI panel can use Taffy for a basic Row/Column layout on Windows | NOT STARTED |
| 2 | Stable Flexbox Core | Flexbox becomes practically useful: wrap, grow/shrink, percent, min/max, nesting, dirty-driven lifecycle | NOT STARTED |
| 3 | uGUI Measurement and Compatibility | Existing LayoutElement, Text, TMP, and images participate correctly in layout | NOT STARTED |
| 4 | Complete Box/Position/Block Core | Margin, positioning, aspect ratio, overflow reservation, box model, and Block are stable | NOT STARTED |
| 5 | CSS Grid | Grid templates, tracks, placement, auto-flow, and Grid authoring are stable | NOT STARTED |
| 6 | Responsive and Runtime Styling | Container breakpoints and runtime overrides work without prefab duplication | NOT STARTED |
| 7 | Unity Integration Hardening | ScrollRect, Canvas Scaler, safe area, animation conflicts, and lifecycle edge cases are stable | NOT STARTED |
| 8 | Editor Tools and Migration | Inspectors, debugger, diagnostics, and safe old-uGUI migration workflow are usable | NOT STARTED |
| 9 | Cross-Platform Delivery | Windows, macOS, Android, iOS, and WebGL artifacts build and load correctly | NOT STARTED |
| 10 | Performance and Reliability Hardening | Large layouts are efficient, allocation-controlled, and regression-tested | NOT STARTED |
| 11 | v1.0 Release Hardening | Installable, documented, reproducible production package reaches the full project goal | NOT STARTED |

---

# Phase 0 — Foundation and Native Trust

**State:** IN PROGRESS  
**Goal:** Establish a small, deterministic Rust/Taffy native library and ABI that we can safely build the Unity package on top of.

This phase intentionally avoids expanding Unity features. The native foundation must stop moving underneath the Unity layer first.

## Tasks

- [x] **P0.1** Create repository structure, license, README, contribution basics, and package skeleton.
- [x] **P0.2** Pin Taffy exactly in `Cargo.toml` and keep Unity isolated from Taffy Rust types.
- [x] **P0.3** Add native CI for format, Clippy, tests, and release host build.
- [ ] **P0.4 — NEXT TASK** Fix all current `rustfmt` and Clippy failures.
  - Run/apply canonical Rust formatting.
  - Add accurate `# Safety` contracts for exported unsafe FFI functions.
  - Do not silence safety lints globally merely to make CI green.
- [ ] **P0.5** Run CI through tests and release build on Linux, Windows, and macOS; repair compiler/API failures.
- [ ] **P0.6** Formalize native ABI naming/version handshake:
  - ABI version.
  - native build/package version.
  - Taffy version.
  - capability flags.
- [ ] **P0.7** Replace ad-hoc native errors with a stable numeric error model and diagnostic last-error path.
- [ ] **P0.8** Harden context/node lifetime:
  - invalid context rejection.
  - invalid/stale node rejection.
  - deterministic destroy/clear behavior.
  - no Rust panic crossing FFI.
- [ ] **P0.9** Add native unit/golden tests for a minimal three-node layout:
  - context create/destroy.
  - node create/remove.
  - set children.
  - set style.
  - compute layout.
  - read layout.
  - invalid handle behavior.
- [ ] **P0.10** Add a native ABI smoke test executable/harness suitable for later testing packaged artifacts.
- [ ] **P0.11** Commit `Cargo.lock` and verify dependency resolution is reproducible.
- [ ] **P0.12** Produce and verify a Windows x64 native DLL as the first Unity development artifact.

## Phase 0 stability gate

Phase 0 is COMPLETE only when:

- `cargo fmt --check` passes.
- Clippy passes with warnings treated as errors.
- Rust tests pass.
- Release builds pass on the host CI matrix.
- A simple layout returns expected geometry through the public C ABI.
- Invalid handles/arguments fail safely rather than panic or corrupt state.
- ABI/version information can be queried before using the context.
- The Windows x64 native artifact can be loaded by a tiny smoke test.

**Stable deliverable:** A trustworthy native Taffy bridge. It is not yet a complete Unity product, but it is independently buildable and testable and will not require Phase 1 to become valid.

---

# Phase 1 — Minimal Working Unity Flex Layout

**State:** NOT STARTED  
**Goal:** Deliver the first genuinely usable TaffyUGUI vertical slice inside Unity.

The scope stays intentionally small: Windows x64, direct children, basic Flex Row/Column, fixed/auto dimensions, gap and padding.

## End-user outcome

A developer can open an existing uGUI panel, replace its old basic layout group with `TaffyLayoutGroup`, press Play or work in Edit Mode, and see Unity `RectTransform`s positioned by Taffy while Buttons/Images/Text continue to render and interact normally.

## Tasks

- [ ] **P1.1** Refactor/verify `TaffyNative.cs` against the Phase 0 ABI.
- [ ] **P1.2** Add managed `TaffyNativeContext : IDisposable` with ABI check and deterministic cleanup.
- [ ] **P1.3** Implement the minimum stable managed style/value types needed by this phase.
- [ ] **P1.4** Implement `TaffyLayoutGroup : LayoutGroup` lifecycle for direct child collection.
- [ ] **P1.5** Maintain a persistent native node tree instead of rebuilding it every frame.
- [ ] **P1.6** Implement Flex `Row` and `Column`.
- [ ] **P1.7** Implement point/auto width and height used by the minimal layout.
- [ ] **P1.8** Implement padding and gap.
- [ ] **P1.9** Compute one native layout for the group and apply x/y/width/height through Unity `LayoutGroup` APIs.
- [ ] **P1.10** Handle child add/remove/reorder and enable/disable safely.
- [ ] **P1.11** Make the minimal flow work in Play Mode.
- [ ] **P1.12** Make the same flow work in Edit Mode without leaking native contexts or dirtying scenes continuously.
- [ ] **P1.13** Create a minimal Unity sample/prefab proving existing Button, Image, and text components remain normal uGUI components.
- [ ] **P1.14** Add Unity tests for hierarchy-to-native-tree mapping and computed RectTransform geometry.

## Phase 1 stability gate

- Windows x64 plugin loads without `DllNotFoundException` or ABI mismatch.
- A basic existing uGUI panel works after replacing only its layout controller.
- Row and Column produce deterministic expected geometry.
- Adding/removing children does not require recreating the scene.
- Enter/exit Play Mode does not leak or reuse invalid native contexts.
- Edit Mode refresh is deterministic.
- No required child custom component exists for the basic case.
- Existing Unity rendering/input behavior is unchanged.

**Stable deliverable:** **TaffyUGUI Basic** — a small but working Unity package rather than a scaffold.

---

# Phase 2 — Stable Flexbox Core

**State:** NOT STARTED  
**Goal:** Turn the basic vertical slice into a useful responsive Flexbox replacement while keeping Phase 1 completely working.

## Tasks

- [ ] **P2.1** Introduce production `TaffyLayoutItem` override semantics.
- [ ] **P2.2** Add Flex Wrap and Wrap Reverse.
- [ ] **P2.3** Add Flex Grow, Shrink, and Basis.
- [ ] **P2.4** Add percentage sizing.
- [ ] **P2.5** Add min/max width and height.
- [ ] **P2.6** Add justify-content, align-items, align-self, and align-content.
- [ ] **P2.7** Add nested Taffy layout groups.
- [ ] **P2.8** Implement explicit dirty flags: hierarchy, style, measurement, available size, native tree.
- [ ] **P2.9** Eliminate unconditional per-frame layout calculation.
- [ ] **P2.10** Implement bounded Unity layout-generation/reentrancy protection.
- [ ] **P2.11** Cache components/native handles and avoid repeated hot-path `GetComponent` work.
- [ ] **P2.12** Add bulk layout result retrieval or equivalent coarse-grained result transfer.
- [ ] **P2.13** Apply RectTransform changes only when results changed beyond a defined epsilon.
- [ ] **P2.14** Build Flex regression cases: row, column, wrap, grow/shrink, percent, min/max, nested groups.
- [ ] **P2.15** Validate representative sizes: 320×568, 375×812, 768×1024, 1366×768, 1920×1080.

## Phase 2 stability gate

- Every Phase 1 test still passes.
- Flexbox supports the practical responsive feature set promised by this phase.
- Nested groups remain stable during hierarchy changes.
- An unchanged frame performs no Taffy recomputation by default.
- Layout rebuilds do not recurse indefinitely.
- No steady-state managed allocations are introduced by TaffyUGUI when nothing changes.

**Stable deliverable:** A practical responsive Flexbox layout system for uGUI on the first supported development platform.

---

# Phase 3 — uGUI Measurement and Compatibility

**State:** NOT STARTED  
**Goal:** Make TaffyUGUI fit existing production uGUI content instead of only explicit numeric boxes.

## Tasks

- [ ] **P3.1** Implement `LayoutElement` compatibility and documented precedence rules.
- [ ] **P3.2** Add intrinsic Image/RawImage measurement.
- [ ] **P3.3** Add Unity `Text` measurement for supported Unity versions.
- [ ] **P3.4** Create optional `TaffyUGUI.TMP` assembly without making TMP a hard core dependency.
- [ ] **P3.5** Implement TMP preferred-size measurement.
- [ ] **P3.6** Implement width-constrained TMP wrapped-text measurement.
- [ ] **P3.7** Implement bounded two-pass layout/measurement iteration.
- [ ] **P3.8** Add custom `ITaffyMeasureProvider` extension point.
- [ ] **P3.9** Define ContentSizeFitter-supported and cyclic/unsupported combinations.
- [ ] **P3.10** Add conflict detection for AspectRatioFitter and competing layout controllers.
- [ ] **P3.11** Add measurement caching and invalidation for text/content changes.
- [ ] **P3.12** Add regression cases for dynamic text, wrapped text, images, LayoutElement, and mixed content.

## Phase 3 stability gate

- All Phase 1–2 tests remain green.
- Typical existing menu/form/card UI can be migrated without manually specifying every child size.
- TMP wrapping settles within the bounded iteration count.
- Changing TMP text correctly dirties/reflows the layout.
- ContentSizeFitter integration cannot create an uncontrolled rebuild loop.
- Optional absence of TMP does not break the core package.

**Stable deliverable:** TaffyUGUI works with normal real-world uGUI content, not just test rectangles.

---

# Phase 4 — Complete Box Model, Positioning, and Block Core

**State:** NOT STARTED  
**Goal:** Complete the non-Grid core style surface required by the final product.

## Tasks

- [ ] **P4.1** Add margin.
- [ ] **P4.2** Expand padding to full supported unit behavior.
- [ ] **P4.3** Add layout border thickness semantics with clear documentation that Unity still renders visuals.
- [ ] **P4.4** Add relative positioning/insets.
- [ ] **P4.5** Add absolute positioning/insets.
- [ ] **P4.6** Add aspect ratio.
- [ ] **P4.7** Add box sizing.
- [ ] **P4.8** Add direction mapping where meaningful to geometry.
- [ ] **P4.9** Add overflow X/Y layout semantics and scrollbar reservation.
- [ ] **P4.10** Add `Display.Block` and required Block behavior.
- [ ] **P4.11** Validate all exposed enum/struct mappings with managed/native contract tests.
- [ ] **P4.12** Add golden tests for margin, padding, absolute, aspect, overflow, and Block.

## Phase 4 stability gate

- All previous phases remain regression-clean.
- Every exposed non-Grid style property has a managed/native mapping test.
- Absolute/relative items behave predictably in nested Unity hierarchies.
- Overflow documentation correctly separates layout reservation from Unity clipping/rendering.
- Block layouts pass deterministic golden geometry tests.

**Stable deliverable:** Complete core layout behavior before Grid and higher-level responsive extensions are added.

---

# Phase 5 — CSS Grid

**State:** NOT STARTED  
**Goal:** Make Grid a first-class stable feature rather than a partial array-based experiment.

## Tasks

- [ ] **P5.1** Define serializable managed Grid track/placement/template types.
- [ ] **P5.2** Define native variable-length Grid template ABI/resource representation.
- [ ] **P5.3** Add explicit columns and rows.
- [ ] **P5.4** Add `fr`, auto, fixed, percent, repeat, and minmax cases supported by the chosen Taffy baseline.
- [ ] **P5.5** Add implicit tracks and auto rows/columns.
- [ ] **P5.6** Add Grid auto-flow.
- [ ] **P5.7** Add row/column placement and spans.
- [ ] **P5.8** Add Grid gap and item/content alignment.
- [ ] **P5.9** Add advanced named line/area support only after core Grid is stable.
- [ ] **P5.10** Create user-friendly Grid property drawers/presets required to author the feature without raw serialized arrays.
- [ ] **P5.11** Add Grid golden/regression cases and nested Grid/Flex combinations.

## Phase 5 stability gate

- Previous Flex/measurement behavior is unchanged.
- Common layouts can be authored: equal card columns, sidebar/content, header/content/footer, auto-flow inventory.
- Managed/native tests cover every Grid structure exposed publicly.
- Grid modifications at runtime do not leak native template resources.
- Inspector serialization survives domain reload and prefab save/reopen.

**Stable deliverable:** Production-usable CSS-style Grid alongside Flex and Block.

---

# Phase 6 — Responsive Profiles and Runtime Styling

**State:** NOT STARTED  
**Goal:** Add optional breakpoint-style behavior on top of intrinsic Taffy responsiveness without complicating the core layout engine.

## Tasks

- [ ] **P6.1** Implement `TaffyResponsiveProfile` ScriptableObject.
- [ ] **P6.2** Implement container width/height rules.
- [ ] **P6.3** Add orientation/aspect rules.
- [ ] **P6.4** Define deterministic rule ordering and style-resolution precedence.
- [ ] **P6.5** Add local item responsive overrides.
- [ ] **P6.6** Add non-serialized runtime style overrides.
- [ ] **P6.7** Add stable public runtime setters that automatically dirty/rebuild correctly.
- [ ] **P6.8** Add editor preview widths without changing actual Game View resolution.
- [ ] **P6.9** Add responsive regression sample from phone through desktop widths.

## Phase 6 stability gate

- Previous layouts behave identically when no responsive profile/override is used.
- Reusable panels respond to their allocated container size, not only global screen width.
- Runtime overrides can be cleared without modifying prefab defaults.
- Repeated breakpoint crossing does not leak, allocate uncontrollably, or cause rebuild loops.

**Stable deliverable:** Responsive design beyond Flex/Grid intrinsic behavior, while remaining optional and backward-compatible.

---

# Phase 7 — Unity Integration Hardening

**State:** NOT STARTED  
**Goal:** Stabilize the package around the Unity systems most likely to interact with layout ownership.

## Tasks

- [ ] **P7.1** Implement/validate Canvas Scaler coordinate behavior.
- [ ] **P7.2** Implement `TaffyScrollRectBridge` semantics.
- [ ] **P7.3** Validate vertical, horizontal, two-axis, nested, Grid, and wrapped-Flex ScrollRects.
- [ ] **P7.4** Ensure ScrollRect position-only movement does not trigger layout recompute.
- [ ] **P7.5** Implement optional `TaffySafeArea`.
- [ ] **P7.6** Validate resolution/orientation/safe-area changes.
- [ ] **P7.7** Detect Animator clips that fight Taffy-owned RectTransform properties.
- [ ] **P7.8** Harden Edit Mode, Prefab Mode, assembly reload, and Play Mode transitions.
- [ ] **P7.9** Ensure native contexts are explicitly disposed during reload/exit paths.
- [ ] **P7.10** Add graceful native-plugin-unavailable behavior.

## Phase 7 stability gate

- All previous regression tests remain green.
- Scrolling does not continuously relayout unchanged content.
- Canvas Scaler uses logical RectTransform units correctly.
- Orientation/safe-area changes relayout only when necessary.
- Domain reload/play-mode transitions leave no known stale native contexts.
- Known competing ownership cases produce actionable warnings.

**Stable deliverable:** TaffyUGUI behaves predictably as part of a real Unity application lifecycle.

---

# Phase 8 — Production Editor Tools and Migration

**State:** NOT STARTED  
**Goal:** Make the package usable by developers who do not want to understand the C ABI or inspect native logs.

## Tasks

- [ ] **P8.1** Production `TaffyLayoutGroupEditor` with contextual Flex/Grid/Block controls.
- [ ] **P8.2** Production `TaffyLayoutItemEditor` with override toggles and resolved-value/source display.
- [ ] **P8.3** Property drawers for dimensions, rectangles, tracks, and placements.
- [ ] **P8.4** Native status/ABI diagnostic UI.
- [ ] **P8.5** Layout Debugger tree with resolved styles, measurements, geometry, dirty reason, and timing.
- [ ] **P8.6** Scene View layout visualization.
- [ ] **P8.7** Diagnostics window and copyable support report.
- [ ] **P8.8** Hierarchy/project conflict validator.
- [ ] **P8.9** Migration analyzer for HorizontalLayoutGroup.
- [ ] **P8.10** Safe conversion with Undo for HorizontalLayoutGroup.
- [ ] **P8.11** Equivalent VerticalLayoutGroup migration.
- [ ] **P8.12** Deterministic GridLayoutGroup migration where semantics can be preserved.
- [ ] **P8.13** Migration preview/report and explicit reporting of values that cannot map exactly.
- [ ] **P8.14** Prefab-safe migration tests.

## Phase 8 stability gate

- Existing runtime behavior remains unchanged without Editor assemblies present in a build.
- A developer can diagnose common problems without native debugging.
- Migration never destructively changes a hierarchy without Undo/explicit action.
- Horizontal/Vertical migration parity tests produce expected geometry.
- Grid migration clearly refuses/flags cases it cannot preserve accurately.

**Stable deliverable:** A usable Unity package workflow, not merely a runtime library.

---

# Phase 9 — Cross-Platform Native Delivery

**State:** NOT STARTED  
**Goal:** Extend the already-stable feature set to the full platform matrix without changing its semantics.

Platform work is intentionally later so feature development does not repeatedly debug five toolchains at once. Each platform must pass the same native ABI smoke layout before being declared supported.

## Tasks

- [ ] **P9.1** Harden Windows x64 packaging/plugin importer metadata.
- [ ] **P9.2** Add Windows ARM64 if supported/required by the release matrix.
- [ ] **P9.3** Build/test macOS Apple Silicon.
- [ ] **P9.4** Build/test macOS Intel/universal strategy as required.
- [ ] **P9.5** Build/test Android ARM64 using a Unity-compatible NDK.
- [ ] **P9.6** Add Android ARMv7/x86_64 only when justified by supported Unity targets/testing needs.
- [ ] **P9.7** Build/test iOS device ARM64 static library/XCFramework strategy.
- [ ] **P9.8** Validate iOS simulator packaging needed by supported Unity workflow.
- [ ] **P9.9** Build Unity Web/WebGL with the Unity-compatible Emscripten toolchain.
- [ ] **P9.10** Commit correct plugin importer `.meta` settings.
- [ ] **P9.11** Add platform ABI smoke test for every shipped native artifact.
- [ ] **P9.12** Add Unity player smoke/regression builds for the supported matrix where CI permits.

## Phase 9 stability gate

- Every advertised platform has a reproducible CI build or documented reproducible build path.
- Each binary passes version handshake, context lifecycle, and simple three-node layout smoke test.
- Unity loads the correct artifact for each target with no manual Plugin Inspector setup.
- Platform-specific implementation does not fork layout behavior.
- WebGL is validated with the Unity-compatible Emscripten path rather than a generic WASM build claim.

**Stable deliverable:** Same TaffyUGUI product behavior across the supported Unity deployment platforms.

---

# Phase 10 — Performance and Reliability Hardening

**State:** NOT STARTED  
**Goal:** Optimize only after feature correctness is stable and measure the full Unity cost, not merely Taffy compute time.

## Tasks

- [ ] **P10.1** Add benchmark harness for 100, 1,000, 5,000 and stress-node cases.
- [ ] **P10.2** Measure hierarchy scan, style resolution, measurement, marshalling, Taffy compute, result copy, and RectTransform application separately.
- [ ] **P10.3** Implement/finish bulk style updates.
- [ ] **P10.4** Implement/finish bulk measurement updates.
- [ ] **P10.5** Implement/finish bulk layout retrieval.
- [ ] **P10.6** Reuse managed/native buffers.
- [ ] **P10.7** Remove avoidable allocations and LINQ/reflection from runtime hot paths.
- [ ] **P10.8** Optimize dirty uploads so unchanged nodes are not resent.
- [ ] **P10.9** Validate no steady-state managed allocations from TaffyUGUI when unchanged.
- [ ] **P10.10** Add single-node-dirty, 50%-dirty, resolution-resize, text, nested, Flex and Grid benchmarks.
- [ ] **P10.11** Document achieved performance rather than marketing unverified targets.
- [ ] **P10.12** Run soak/stress tests for repeated hierarchy/style/text changes and native context lifecycle.

## Phase 10 stability gate

- All functional/platform regression tests remain green.
- No layout call occurs on an unchanged frame by default.
- No steady-state managed allocation is attributable to TaffyUGUI after warmup in the validated idle case.
- Performance report includes Unity application/Canvas consequences, not only Rust compute time.
- Large-tree benchmarks have documented reproducible results and no known leaks/corruption.

**Stable deliverable:** Feature-complete implementation hardened for real production workloads.

---

# Phase 11 — v1.0 Release Hardening

**State:** NOT STARTED  
**Goal:** Convert the validated implementation into the final public v1.0 package described by the development plan.

## Tasks

- [ ] **P11.1** Finalize public managed API and SemVer policy.
- [ ] **P11.2** Finalize native ABI version policy.
- [ ] **P11.3** Add/complete `CHANGELOG.md`.
- [ ] **P11.4** Add/complete `SECURITY.md`.
- [ ] **P11.5** Add `THIRD_PARTY_NOTICES.md` and verify dependency/license obligations.
- [ ] **P11.6** Complete Getting Started documentation.
- [ ] **P11.7** Complete Flexbox documentation.
- [ ] **P11.8** Complete Grid/Block documentation.
- [ ] **P11.9** Complete measurement/TMP/LayoutElement documentation.
- [ ] **P11.10** Complete responsive/runtime API documentation.
- [ ] **P11.11** Complete ScrollRect/platform/performance/troubleshooting documentation.
- [ ] **P11.12** Ship stable samples: Flex basics, responsive cards, Grid, ScrollRect, text measurement, breakpoints, migration.
- [ ] **P11.13** Add Unity build validator and package-validation checks.
- [ ] **P11.14** Build reproducible release artifacts and checksums.
- [ ] **P11.15** Produce UPM-ready release package/tarball and Git URL installation path.
- [ ] **P11.16** Validate installation into a clean Unity project.
- [ ] **P11.17** Validate installation/migration in an existing representative uGUI project.
- [ ] **P11.18** Run full final test/platform matrix.
- [ ] **P11.19** Review README claims against what has actually been tested; remove or qualify unsupported claims.
- [ ] **P11.20** Tag/release v1.0 only after every Definition of Done item is satisfied.

## Phase 11 / v1.0 stability gate

v1.0 is complete only when:

- Flexbox is production-ready.
- Grid is production-ready.
- Block/core box model is supported as documented.
- Existing `LayoutElement`, Text/TMP, images, nested groups, and ScrollRect integration work as documented.
- Responsive overrides/runtime API work.
- Editor preview/diagnostics/migration tools work.
- Windows, macOS, Android ARM64, iOS, and WebGL are validated and packaged.
- Native ABI mismatch fails clearly and safely.
- No continuous unchanged-frame layout work exists by default.
- All exposed style mappings have automated coverage.
- Native artifacts are reproducibly generated.
- Plugin import metadata is committed.
- Documentation, samples, license notices, AI disclaimer, and security/support limitations are present.
- A clean Unity project and representative existing uGUI project can both consume the released package without manual native-plugin configuration.

**Stable deliverable:** **TaffyUGUI v1.0** — the complete project outcome defined in `DEVELOPMENT_PLAN.md`.

---

# 5. Regression Contract Between Phases

Once a phase is COMPLETE, its exit criteria become permanent regression requirements.

Before completing any later phase, verify:

- [ ] All earlier native tests still pass.
- [ ] All earlier Unity Edit Mode tests still pass.
- [ ] All earlier Unity Play Mode tests still pass.
- [ ] Previously supported sample scenes still produce expected geometry.
- [ ] No supported serialized component data silently changed meaning.
- [ ] ABI/API compatibility remains valid or an intentional versioned migration exists.
- [ ] The previous phase's documented user flow still works end-to-end.

If one of these fails, the current phase changes to **STABILIZATION** until the regression is resolved.

---

# 6. Task Completion Protocol

When a task is implemented:

1. Add/modify the code.
2. Add the smallest useful automated test or deterministic verification for that behavior.
3. Run relevant existing regression tests.
4. Fix warnings/errors instead of suppressing them without justification.
5. Mark the task `[x]` only after verification.
6. Move `NEXT TASK` to the next uncompleted task in the active phase.
7. Update **Current State** if the blocker/status changed.
8. When the last task is done, move the phase to **STABILIZATION**.
9. Run the complete phase stability gate.
10. Only then mark the phase **COMPLETE** and activate the next phase.

---

# 7. Scope-Control Rule

While a phase is active, unrelated future-phase features should not be added just because they are easy to implement at the same time.

Exceptions are allowed only when work is required to preserve the architecture or avoid an imminent breaking change. In that case:

- keep the future feature disabled/internal;
- do not advertise it as supported;
- do not mark its future task complete until its own phase verification is performed.

This keeps each release slice understandable, testable, and recoverable.

---

# 8. Version Milestone Guidance

Exact versions can be adjusted later, but development should conceptually map to stable milestones:

| Completion | Suggested milestone meaning |
|---|---|
| Phase 0 | native foundation / pre-Unity internal milestone |
| Phase 1 | first usable basic preview |
| Phase 2 | Flexbox preview |
| Phase 3 | real-uGUI compatibility preview |
| Phase 5 | Flex + Grid feature preview |
| Phase 8 | developer-tooling beta |
| Phase 9 | cross-platform beta |
| Phase 10 | release candidate |
| Phase 11 | v1.0 |

Do not assign a production `1.0.0` version before Phase 11 passes.

---

# 9. Final Development Principle

The objective is not to implement the largest number of Taffy features as quickly as possible.

The objective is to continuously maintain **the smallest currently supported version of TaffyUGUI as a complete, stable, testable product**, then add one coherent capability layer at a time until the full v1.0 product boundary is reached.

At every point in development we should be able to answer four questions from this file:

1. **What phase are we in?**
2. **What is working and stable already?**
3. **What is the single next task?**
4. **What must pass before we are allowed to move to the next phase?**
