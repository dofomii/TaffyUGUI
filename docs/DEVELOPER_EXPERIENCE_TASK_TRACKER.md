# TaffyUGUI Developer Experience Improvement Task Tracker

**Program:** post-v1 Editor/authoring experience improvement  
**Status date:** 2026-08-19  
**Program status:** COMPLETE — DX0 through DX11 complete
**Runtime baseline:** TaffyUGUI `1.0.0`, final ABI-v1 `1/2`, Taffy `0.13.0`  
**Unity compatibility baseline:** `2021.3 LTS` minimum; maintained validation also covers newer supported Editors  
**Authoritative implementation rule:** improve the Editor/authoring layer first; preserve the existing runtime/native layout model unless a task explicitly requires otherwise.

---

# Purpose

This tracker converts the TaffyUGUI Developer Experience improvement plan into an executable, dependency-aware implementation program.

The goal is to make TaffyUGUI feel simple and self-explanatory for a developer seeing it for the first time while preserving full access to the underlying Flex, Grid, Block, Calc, measurement, responsive, and integration features.

The target experience is:

```text
What layout do I want?
        │
        ▼
Choose a common layout / intent
        │
        ▼
See only essential controls
        │
        ▼
Get contextual help and health diagnostics
        │
        ▼
Reveal advanced Taffy controls only when needed
```

The runtime components remain the source of truth:

```text
Inspector / Quick Action / Preset / Builder / Scene Handle
                         │
                         ▼
          TaffyLayoutGroup / TaffyLayoutItem
                         │
                         ▼
                  Existing Runtime
                         │
                         ▼
                 Existing Native ABI
```

---

# Program Guardrails

These apply to every DX phase.

- Preserve existing serialized field names unless a migration is explicitly designed and tested.
- Preserve existing serialized enum numeric values.
- Simple and Advanced inspector modes must edit the same runtime properties.
- Do not create a second layout configuration model that can diverge from `TaffyLayoutGroup` / `TaffyLayoutItem`.
- Keep primary inspector implementation compatible with Unity `2021.3`.
- Prefer modular IMGUI for the core inspector until there is a proven reason to migrate that surface to UI Toolkit.
- Every user-facing layout option must have meaningful tooltip/help content before its phase closes.
- All mutation workflows must support Unity Undo.
- Inspector and action workflows must be prefab-safe.
- Multi-object editing must be supported wherever the operation is semantically valid.
- Editor tooling must remain isolated in `TaffyUGUI.Editor`.
- No Editor assembly may become a Player/runtime dependency.
- Quick actions, presets, diagnostics fixes, builders, and Scene handles must write serialized component state rather than native state directly.
- Keep native/runtime behavior unchanged unless a task explicitly requires a runtime change.
- Keep `.build/`, disposable validation projects, probes, harnesses, and generated `dist/` artifacts untracked.
- Do not mix the separate 32-bit ABI redesign into this DX program.
- Do not implement linked/live presets in the first preset phase; start with Apply Once.
- Existing native, Edit Mode, and Play Mode regression gates must remain green throughout the program.

---

# Status Legend

- **COMPLETE** — all implementation and validation requirements for the phase passed.
- **ACTIVE** — current authoritative phase.
- **READY** — prerequisites complete; phase may start.
- **BLOCKED** — prerequisite or external dependency prevents completion.
- **NOT STARTED** — intentionally gated by earlier work.

A task being implemented does not make its phase complete. The phase closes only when its exit criteria and validation gate pass.

---

# Program Phase Map

| Phase | Name | Primary Outcome | Depends On | Status |
|---|---|---|---|---|
| DX0 | Architecture & Compatibility Baseline | Safe implementation contract and permanent baseline tests | — | COMPLETE |
| DX1 | Editor Core & Inspector Refactor | Modular editor foundation with no semantic UX/runtime regression | DX0 | COMPLETE |
| DX2 | Beginner-First Inspector | Simple/Advanced mode, essentials, contextual visibility, complete help | DX1 | COMPLETE |
| DX3 | Intent-First Property Editing | Human-readable sizing, spacing, alignment, smart visual drawers | DX2 | COMPLETE |
| DX4 | One-Click Common Workflows | Quick actions and common hierarchy creation | DX3 | COMPLETE |
| DX5 | Layout Health & Smart Diagnostics | Self-explaining setup problems with actionable fixes | DX4 | COMPLETE |
| DX6 | Presets & Reusable Layout Patterns | Apply-once built-in/project presets and searchable browser | DX5 | COMPLETE |
| DX7 | Visual Responsive & Grid Authoring | Visual responsive-profile and Grid authoring | DX6 | COMPLETE |
| DX8 | Scene View Authoring & Responsive Preview | Scene geometry overlays and responsive preview workflows | DX7 | COMPLETE |
| DX9 | Guided Creation & Onboarding | Hierarchy recipes, first-use guide, UI Builder workflow | DX8 | COMPLETE |
| DX10 | Explain Layout & Expert Productivity | Computed reasoning, search, clipboard, hierarchy/editor polish | DX9 | COMPLETE |
| DX11 | Hardening, Documentation & Final DX Gate | Full regression, usability audit, docs, release-ready DX system | DX10 | COMPLETE |

**Current authoritative task:** Developer Experience improvement program complete; all DX0–DX11 gates passed.
---

# Program Milestones

## Milestone A — Safe Editor Foundation
**Status:** COMPLETE
Complete when `DX0 + DX1` are complete.

Result: Editor internals are modular enough to evolve safely without changing layout semantics.

## Milestone B — Beginner-Ready Inspector

Complete when `DX2 + DX3` are complete.

Result: a first-time user can create and understand common Flex layouts without reading raw Taffy/CSS terminology.

## Milestone C — Fast Common Workflows

Complete when `DX4 + DX5` are complete.

Result: common layouts are one-click operations and common mistakes explain/fix themselves.

## Milestone D — Reusable Team Workflows

Complete when `DX6` is complete.

Result: teams can reuse standard layout patterns without manually repeating configuration.

## Milestone E — Complex Layouts Become Visual

Complete when `DX7 + DX8` are complete.

Result: Grid, responsive layout, and Scene geometry become much easier to reason about visually.

## Milestone F — Full Guided Authoring

Complete when `DX9 + DX10` are complete.

Result: new layouts can be created from recipes/builders and difficult layout behavior can be explained from the Editor.

## Milestone G — Production DX Complete

Complete when `DX11` is complete.

Result: the redesigned experience is documented, regression-tested, package-safe, and ready for normal project use.

---

# DX0 — Architecture & Compatibility Baseline

**Status:** COMPLETE
**Goal:** freeze the runtime/serialization assumptions and create permanent Editor regression coverage before restructuring the inspectors.

This phase intentionally avoided major UX/runtime changes.

## Serialization contract

- [x] DX0.1 Inventory and document all serialized `TaffyLayoutGroup` field names.
- [x] DX0.2 Inventory and document all serialized `TaffyLayoutItem` field names.
- [x] DX0.3 Inventory responsive, Grid, Calc, measurement, and nested serializable field names used by scenes/prefabs.
- [x] DX0.4 Record enum numeric values that must remain serialization-compatible.
- [x] DX0.5 Document fields that may be relabeled in the Editor but must not be renamed in runtime serialization.

## Current Editor behavior baseline

- [x] DX0.6 Record the current Group inspector sections and serialized-property coverage.
- [x] DX0.7 Record the current Item inspector sections and serialized-property coverage.
- [x] DX0.8 Record current custom property drawers and the types they own.
- [x] DX0.9 Record current debugger, migration, and Scene visualization responsibilities.

## Permanent safety tests

- [x] DX0.10 Add Edit Mode tests proving representative existing serialized Group data survives Editor serialization/deserialization.
- [x] DX0.11 Add Edit Mode tests proving representative existing serialized Item data survives Editor serialization/deserialization.
- [x] DX0.12 Add enum-contract Editor/runtime regression tests where not already covered.
- [x] DX0.13 Add tests ensuring the Editor assembly remains excluded from Player runtime dependencies.
- [x] DX0.14 Add a permanent baseline test for current custom inspectors instantiating without exceptions on representative components.
- [x] DX0.15 Add multi-object baseline coverage for Group and Item inspectors.

## Documentation

- [x] DX0.16 Add a short architecture note linking runtime source-of-truth rules to this tracker.
- [x] DX0.17 Document the explicit non-goal that 32-bit ABI work is separate from DX implementation.

### DX0 validation note

- Compatibility reference: `DX0_EDITOR_COMPATIBILITY_BASELINE.md`.
- Permanent compatibility suite added in `TaffyDX0CompatibilityTests.cs` with 9 DX0 tests.
- Unity `6000.4.3f1` Edit Mode: **50/50 PASS** (existing 41 maintained tests + 9 DX0 compatibility tests).
- Unity `6000.4.3f1` Play Mode: **9/9 PASS**.
- New tests pin Group/Item declared authoring fields, nested serializable authoring fields, serialized enum numeric values, representative Editor JSON round-trips, Editor/runtime assembly isolation, custom editor creation, and multi-object editor creation.
- Validation uses inactive test objects for DX0 serialization/editor construction where possible, so the compatibility tests themselves do not require layout computation.
- No runtime/native implementation or layout semantics changed in DX0.
- Disposable Unity result XML/log files remain under ignored `.build/`.

### DX0 exit criteria

- [x] Serialized field/enum compatibility contract is explicit.
- [x] Existing Editor surfaces are inventoried.
- [x] Permanent tests protect representative existing data.
- [x] No runtime/native semantics changed.
- [x] VS Code Problems contains no new warnings/errors.
- [x] Maintained Edit Mode and Play Mode suites remain green.

### Recommended commit boundary

One focused commit containing compatibility documentation and permanent baseline tests only.

---

# DX1 — Editor Core & Inspector Refactor

**Status:** COMPLETE
**Depends on:** DX0  
**Goal:** build a modular Editor architecture while preserving the existing visible behavior as closely as practical.

## Core infrastructure

- [x] DX1.1 Create `Editor/Core/` structure.
- [x] DX1.2 Introduce `TaffyInspectorContext` for shared target/parent/mode/editor state.
- [x] DX1.3 Introduce centralized serialized-property lookup/helpers.
- [x] DX1.4 Introduce centralized default-value comparison helpers.
- [x] DX1.5 Introduce centralized `TaffyEditorContent` for labels/tooltips/help text.
- [x] DX1.6 Introduce reusable Editor styles/layout helpers without introducing custom visual dependencies.
- [x] DX1.7 Introduce Editor preference storage for future inspector mode/density settings.

## Inspector modularization

- [x] DX1.8 Split `TaffyLayoutGroupEditor` into focused section classes.
- [x] DX1.9 Split `TaffyLayoutItemEditor` into focused section classes.
- [x] DX1.10 Move existing drawer implementations into a maintainable `Drawers/` structure without semantic changes.
- [x] DX1.11 Keep existing diagnostics/debugger buttons functional after the split.
- [x] DX1.12 Keep Scene visualization integration functional after the split.
- [x] DX1.13 Preserve multi-object editing behavior.

## Section architecture

- [x] DX1.14 Implement common section relevance API.
- [x] DX1.15 Implement section foldout-state persistence.
- [x] DX1.16 Implement section summary hook, even if initial summaries are minimal.
- [x] DX1.17 Ensure sections use serialized properties and do not directly mutate runtime fields.

## Tests

- [x] DX1.18 Add tests proving modular Group inspector exposes all previously reachable serialized properties in Advanced-equivalent coverage.
- [x] DX1.19 Add tests proving modular Item inspector exposes all previously reachable serialized properties in Advanced-equivalent coverage.
- [x] DX1.20 Add Undo/prefab regression tests for representative inspector edits.

### DX1 validation note

- Editor sources are split into `Editor/Core/`, `Editor/Inspectors/`, `Editor/Sections/`, and `Editor/Drawers/`; runtime/native sources are unchanged.
- Permanent DX1 coverage added in `TaffyDX1EditorArchitectureTests.cs` for Group/Item property coverage, persisted foldout infrastructure, representative Undo/prefab editing, and debugger/Scene-tool availability.
- Unity `6000.4.3f1` Edit Mode: **56/56 PASS**; Play Mode: **9/9 PASS**.
- Unity `2021.3.39f1` Edit Mode: **56/56 PASS** using the same temporary local `bee_backend --stdin-canary` workaround documented by Phase 12; the Editor installation was restored to SHA-256 `8561ed19e6d35e1e947b450dd528867e7c43c9fe43b5cce9086b58d3cad4fa67`.
- VS Code Problems: **0 warnings/errors** before phase close.
- No runtime/native layout behavior or serialized runtime contract changed in DX1.

### DX1 exit criteria

- [x] Inspector code is modular enough for independent sections.
- [x] No serialized field coverage is accidentally lost.
- [x] No runtime/native behavior changes are required.
- [x] Existing debugger, migration, and Scene tools still work.
- [x] Permanent tests pass.

### Recommended commit boundary

Editor architecture/refactor only. Do not mix large visual UX changes into this commit.

---

# DX2 — Beginner-First Inspector

**Status:** COMPLETE
**Depends on:** DX1  
**Goal:** make both core inspectors immediately understandable to a first-time TaffyUGUI user.

## Simple / Advanced modes

- [x] DX2.1 Add `Simple | Advanced` inspector mode.
- [x] DX2.2 Make Simple mode the default for new Editor preference state.
- [x] DX2.3 Ensure both modes edit identical serialized properties.
- [x] DX2.4 Add an obvious route from Simple mode to the complete advanced surface.

## Group essentials

- [x] DX2.5 Create a permanently visible Quick Setup section for `TaffyLayoutGroup`.
- [x] DX2.6 Surface layout type/display in human-readable language.
- [x] DX2.7 Surface direction visually/clearly.
- [x] DX2.8 Surface primary alignment.
- [x] DX2.9 Surface gap/spacing.
- [x] DX2.10 Surface padding.
- [x] DX2.11 Surface essential size behavior where meaningful for the container workflow.

## Item essentials

- [x] DX2.12 Add parent-layout summary at the top of `TaffyLayoutItem`.
- [x] DX2.13 Surface Width and Height first.
- [x] DX2.14 Surface common sizing behavior first.
- [x] DX2.15 Surface Grow when parent context makes it relevant.
- [x] DX2.16 Surface alignment override in a beginner-readable form.

## Progressive disclosure

- [x] DX2.17 Hide Flex-only details when irrelevant in Simple mode.
- [x] DX2.18 Hide Grid-only details when irrelevant in Simple mode.
- [x] DX2.19 Hide Block/Float details when irrelevant in Simple mode.
- [x] DX2.20 Preserve modified but currently inactive fields through visible summaries/warnings where required.
- [x] DX2.21 Add clear Advanced sections for full access to all properties.

## Tooltip/help baseline

- [x] DX2.22 Add meaningful tooltip/help content for every Group field surfaced by the new inspector.
- [x] DX2.23 Add meaningful tooltip/help content for every Item field surfaced by the new inspector.
- [x] DX2.24 Add contextual help for axis-sensitive Flex concepts.
- [x] DX2.25 Add contextual text explaining parent-dependent item behavior.

## Tests

- [x] DX2.26 Test Simple and Advanced modes map to the same serialized properties.
- [x] DX2.27 Test contextual Flex/Grid section visibility.
- [x] DX2.28 Test parent summary for Flex, Grid, and no-parent cases.
- [x] DX2.29 Test mode preference persistence.

### DX2 validation note

- Added persisted `Simple | Advanced` inspector mode with Simple as the default preference state; Advanced retains the complete DX1 property surface.
- Group Quick Setup now exposes layout type, context-appropriate direction/flow, primary alignment, gaps, padding, and current RectTransform size while preserving inactive Flex/Grid settings with Simple-mode warnings.
- Item Essentials now leads with Width/Height and adds parent-aware Flex Grow/alignment or Grid alignment context, plus explicit no-parent and inactive-setting guidance.
- Centralized tooltip/help content covers every serialized Group/Item property reachable through the Advanced inspector, with axis-sensitive Flex guidance.
- Mixed-display Group multi-selection avoids ambiguous Simple-only controls and keeps full layout-specific multi-editing available in Advanced mode.
- Permanent DX2 coverage added in `TaffyDX2BeginnerInspectorTests.cs`; Unity `6000.4.3f1` Edit Mode: **64/64 PASS**, Play Mode: **9/9 PASS**.
- Unity `2021.3.39f1` Edit Mode: **64/64 PASS** using the previously documented temporary local `bee_backend --stdin-canary` workaround; the Editor installation was restored to SHA-256 `8561ed19e6d35e1e947b450dd528867e7c43c9fe43b5cce9086b58d3cad4fa67`.
- No runtime/native source or serialized runtime contract changed in DX2.

### DX2 exit criteria

- [x] A new user can identify layout direction, alignment, spacing, padding, and essential size controls immediately.
- [x] Item behavior is shown in parent context.
- [x] All advanced properties remain reachable.
- [x] Every surfaced control has useful explanatory content.
- [x] No runtime changes were necessary.

### Usability checkpoint

A developer unfamiliar with Taffy terminology should be able to create a basic horizontal or vertical arrangement using only Simple mode.

---

# DX3 — Intent-First Property Editing

**Status:** COMPLETE
**Depends on:** DX2  
**Goal:** replace low-level serialized-type editing with visual/human authoring controls for the most common concepts.

## `TaffyLength`

- [x] DX3.1 Redesign `TaffyLengthDrawer` around intent-first choices.
- [x] DX3.2 Present Fixed values with explicit pixel/point semantics.
- [x] DX3.3 Present Percent values as human percentages while preserving runtime fraction representation.
- [x] DX3.4 Provide Fill Parent shortcut/mode where semantics are valid.
- [x] DX3.5 Clearly distinguish Auto/Fit Content semantics from fixed/percent values.
- [x] DX3.6 Keep Calc accessible without overwhelming Simple mode.
- [x] DX3.7 Preserve all existing serialized `TaffyLength` data through drawer changes.

## Box model

- [x] DX3.8 Redesign `TaffyEdgesDrawer` with Uniform / Axis / Individual editing modes.
- [x] DX3.9 Add linked-side editing.
- [x] DX3.10 Add compact summary text for collapsed spacing sections.
- [x] DX3.11 Ensure Margin, Padding, Border, and Inset reuse consistent interaction patterns.

## Alignment

- [x] DX3.12 Add visual direction buttons for Row/Column where appropriate.
- [x] DX3.13 Add visual main-axis alignment controls.
- [x] DX3.14 Add visual cross-axis alignment controls.
- [x] DX3.15 Keep full Safe/Flex/Self alignment variants available in Advanced mode.

## Smart summaries

- [x] DX3.16 Implement Size summaries such as `100% × Auto`.
- [x] DX3.17 Implement Flex summaries such as `Grow 1 • Shrink 1`.
- [x] DX3.18 Implement Padding/Margin summaries.
- [x] DX3.19 Implement Grid-placement summaries.

## Tests

- [x] DX3.20 Test user-facing percentage ↔ runtime fraction mapping.
- [x] DX3.21 Test Fill Parent mapping.
- [x] DX3.22 Test linked/unlinked edge editing.
- [x] DX3.23 Test visual alignment controls write exact existing enum values.
- [x] DX3.24 Test existing complex Calc/Grid serialized values remain intact.

### DX3 validation note

- `TaffyLengthDrawer` now exposes Auto / Content, Fixed, Percent, Fill Parent, and Calculated authoring intents while writing the existing `TaffyLength` serialized fields.
- Percent editing is human-facing while the runtime fraction representation is preserved (`50%` ↔ `0.5`); Fill Parent maps to exact `Percent(1.0)`.
- `TaffyEdgesDrawer` now supports Uniform, Axis, and Individual modes with linked-side synchronization while preserving Calc expressions and the existing serialized `TaffyEdges` shape.
- Simple Group authoring now uses visual direction, main-axis alignment, and cross-axis alignment controls for common enum values; Advanced mode still exposes the full existing enum surface including Safe/Flex/Self variants.
- Smart summaries are surfaced for item size, Flex grow/shrink, Margin/Padding/Border, Grid placement, and Group padding.
- Permanent DX3 coverage added in `TaffyDX3IntentEditingTests.cs`.
- Unity `6000.4.3f1` Edit Mode: **69/69 PASS**; Play Mode: **9/9 PASS**.
- Unity `2021.3.39f1` Edit Mode: **69/69 PASS** using the documented temporary local `bee_backend --stdin-canary` workaround; the Editor installation was restored to SHA-256 `8561ed19e6d35e1e947b450dd528867e7c43c9fe43b5cce9086b58d3cad4fa67`.
- No runtime/native source or serialized runtime contract changed in DX3.

### DX3 exit criteria

- [x] Common sizing and spacing can be edited without understanding internal serialized structure.
- [x] Visual alignment controls cover the common path.
- [x] Advanced modes still expose every supported value.
- [x] Existing data is preserved.

---

# DX4 — One-Click Common Workflows

**Status:** COMPLETE
**Depends on:** DX3  
**Goal:** make the most common layout operations one-click and Undo-safe.

## Group quick layouts

- [x] DX4.1 Implement Horizontal layout action.
- [x] DX4.2 Implement Vertical layout action.
- [x] DX4.3 Implement Centered Panel action.
- [x] DX4.4 Implement Toolbar action.
- [x] DX4.5 Implement basic wrapping Cards action.
- [x] DX4.6 Implement basic Grid action with sensible defaults.

## Item quick actions

- [x] DX4.7 Implement Fill Width.
- [x] DX4.8 Implement Fill Parent.
- [x] DX4.9 Implement Fit Content.
- [x] DX4.10 Implement Fixed Size starter action.
- [x] DX4.11 Implement Flexible Item.
- [x] DX4.12 Implement Spacer.
- [x] DX4.13 Implement Center/Alignment convenience where parent semantics allow it.

## Setup helpers

- [x] DX4.14 Offer Add `TaffyLayoutGroup` to Parent when an item has no Taffy parent.
- [x] DX4.15 When adding/configuring a Group with existing children, offer Preserve Sizes / Stretch / Fit Content initialization choices.
- [x] DX4.16 Add basic Hierarchy create menu entries for Horizontal, Vertical, Grid, Spacer.

## Action infrastructure

- [x] DX4.17 Centralize actions in `Editor/Actions/`.
- [x] DX4.18 Ensure all actions use Undo.
- [x] DX4.19 Ensure actions support multi-selection where valid.
- [x] DX4.20 Ensure actions change only the properties they own.

## Tests

- [x] DX4.21 Test every built-in action produces exact intended serialized values.
- [x] DX4.22 Test Undo restores the prior state.
- [x] DX4.23 Test prefab instances remain connected.
- [x] DX4.24 Test multi-object action behavior.

### DX4 validation note

- Quick actions are centralized in `Editor/Actions/TaffyAuthoringActions.cs`; inspectors invoke the shared action layer rather than mutating runtime fields directly.
- Group actions cover Horizontal, Vertical, Centered Panel, Toolbar, wrapping Cards, and a basic Grid with sensible defaults.
- Item actions cover Fill Width, Fill Parent, Fit Content, Fixed Size, Flexible Item, Spacer, and context-aware centering.
- Setup helpers can add a `TaffyLayoutGroup` to a missing parent, initialize existing children as Preserve Sizes / Stretch / Fit Content, and create Horizontal / Vertical / Grid / Spacer hierarchy recipes.
- All action mutations use Unity Undo and prefab-instance recording; supported item actions accept multi-selection while limiting changes to owned serialized properties.
- Permanent DX4 coverage added in `TaffyDX4QuickActionTests.cs`.
- Unity `6000.4.3f1` Edit Mode: **77/77 PASS**; Play Mode: **9/9 PASS**.
- Unity `2021.3.39f1` Edit Mode: **77/77 PASS** using the documented temporary local `bee_backend --stdin-canary` workaround; the Editor installation was restored to SHA-256 `8561ed19e6d35e1e947b450dd528867e7c43c9fe43b5cce9086b58d3cad4fa67`.
- No runtime/native source or serialized runtime contract changed in DX4.

### DX4 exit criteria

- [x] Common Group layouts take one click.
- [x] Common Item sizing behaviors take one click.
- [x] Missing-parent setup is directly repairable.
- [x] All actions are Undo/prefab safe.

---

# DX5 — Layout Health & Smart Diagnostics

**Status:** COMPLETE
**Depends on:** DX4  
**Goal:** turn common configuration mistakes into understandable, actionable guidance.

## Diagnostic framework

- [x] DX5.1 Create `TaffyDiagnosticRule` abstraction.
- [x] DX5.2 Create structured diagnostic result/severity model.
- [x] DX5.3 Create `TaffyDiagnosticFix` abstraction.
- [x] DX5.4 Create aggregated `TaffyLayoutHealth` evaluation.
- [x] DX5.5 Ensure diagnostics are read-only unless a user explicitly invokes a fix.

## Initial rules

- [x] DX5.6 Detect missing Taffy parent for Layout Items.
- [x] DX5.7 Detect competing Unity `HorizontalLayoutGroup` / `VerticalLayoutGroup` / `GridLayoutGroup` ownership.
- [x] DX5.8 Detect `ContentSizeFitter` axis conflicts.
- [x] DX5.9 Detect `AspectRatioFitter` ownership conflicts.
- [x] DX5.10 Surface ScrollRect content ownership conflicts using existing integration logic.
- [x] DX5.11 Detect missing intrinsic measurement source for content-dependent sizing where determinable.
- [x] DX5.12 Integrate existing responsive-profile validation.
- [x] DX5.13 Integrate existing Grid validation.
- [x] DX5.14 Integrate Calc validation.
- [x] DX5.15 Flag suspicious fixed-size vs narrow responsive-profile configurations where confidence is high.
- [x] DX5.16 Surface excessive rebuild suppression diagnostics.

## Layout Health UI

- [x] DX5.17 Add top-level Healthy / Info / Warning / Error summary.
- [x] DX5.18 Add expandable diagnostic list.
- [x] DX5.19 Add direct fix buttons for safe repairs.
- [x] DX5.20 Add documentation/help link support per diagnostic.

## Fixes

- [x] DX5.21 Add Let Taffy Own Axis repair where safe.
- [x] DX5.22 Add Let Unity Fitter Own Axis repair where safe.
- [x] DX5.23 Add Add Taffy Parent repair.
- [x] DX5.24 Add safe Grid-placement/template repair actions only where unambiguous.
- [x] DX5.25 Require Undo and prefab safety for every fix.

## Tests

- [x] DX5.26 Unit/Edit Mode test every diagnostic rule.
- [x] DX5.27 Test fixes independently from diagnostic rendering.
- [x] DX5.28 Test no false mutation occurs from simply opening/drawing the inspector.
- [x] DX5.29 Test multiple simultaneous diagnostics aggregate correctly.

### DX5 validation note

- Added `Editor/Diagnostics/TaffyLayoutHealth.cs` with rule, result/severity, fix, aggregation, runtime-validation bridge, and Inspector UI layers.
- Layout Health appears in both Group and Item inspectors with Healthy / Info / Warning / Error summaries, expandable details, direct safe-fix buttons, and documentation-link support.
- Diagnostics cover missing Taffy parents, Unity layout ownership, ContentSizeFitter/AspectRatioFitter conflicts, existing ScrollRect integration warnings, missing intrinsic measurement sources, responsive-profile validation, Grid placement/template validation, Calc validation, suspicious fixed sizing under narrow profiles, and rebuild suppression.
- Grid/Calc checks reuse existing runtime validators through an Editor reflection bridge so evaluating diagnostics does not mutate the runtime validation cache.
- Safe fixes use Unity Undo and prefab-instance recording; covered repairs include Taffy-vs-fitter ownership, missing parent setup, disabling competing Unity layout owners, Grid placement reset, AspectRatioFitter disable, and rebuild-counter reset.
- Permanent DX5 coverage added in `TaffyDX5LayoutHealthTests.cs`; evaluation read-only behavior, simultaneous aggregation, Undo, and prefab safety are explicitly tested.
- Unity `6000.4.3f1` Edit Mode: **87/87 PASS**; Play Mode: **9/9 PASS**.
- Unity `2021.3.39f1` Edit Mode: **87/87 PASS** using the documented temporary local `bee_backend --stdin-canary` workaround; the Editor installation was restored to SHA-256 `8561ed19e6d35e1e947b450dd528867e7c43c9fe43b5cce9086b58d3cad4fa67`.
- No runtime/native source or serialized runtime contract changed in DX5.

### DX5 exit criteria

- [x] Common setup conflicts explain what owns each affected axis.
- [x] Safe problems have one-click repairs.
- [x] Diagnostics reuse existing runtime validation rather than duplicating layout semantics.
- [x] Inspector remains clean when there are no problems.

### Core DX MVP milestone

After DX5, TaffyUGUI should already be substantially easier to learn and configure even before presets or visual builders exist.

---

# DX6 — Presets & Reusable Layout Patterns

**Status:** COMPLETE
**Depends on:** DX5  
**Goal:** let developers reuse common TaffyUGUI configurations without manually repeating dozens of values.

## Preset model

- [x] DX6.1 Define apply-once Container preset representation.
- [x] DX6.2 Define apply-once Item preset representation.
- [x] DX6.3 Define responsive preset representation only if it remains cleanly separable.
- [x] DX6.4 Define which properties each preset owns so unrelated properties remain unchanged.
- [x] DX6.5 Keep linked/live preset behavior explicitly out of scope for this phase.

## Built-in preset library

- [x] DX6.6 Horizontal Row.
- [x] DX6.7 Vertical Stack.
- [x] DX6.8 Centered Panel.
- [x] DX6.9 Toolbar.
- [x] DX6.10 Sidebar + Content container pattern.
- [x] DX6.11 Scrollable List content pattern.
- [x] DX6.12 Responsive/Wrapping Cards starter pattern.
- [x] DX6.13 Flexible Item.
- [x] DX6.14 Spacer.
- [x] DX6.15 Fit Content Item.

## Project presets

- [x] DX6.16 Support creating project-owned preset assets/data.
- [x] DX6.17 Add Save Current As Preset.
- [x] DX6.18 Add Apply preset.
- [x] DX6.19 Add Edit/Open preset workflow.

## Preset browser

- [x] DX6.20 Create searchable preset browser.
- [x] DX6.21 Add category filtering.
- [x] DX6.22 Add compact visual preview representation.
- [x] DX6.23 Aggregate built-in and project presets.
- [x] DX6.24 Support applying to current selection.

## Tests

- [x] DX6.25 Test built-in preset serialized output.
- [x] DX6.26 Test unrelated properties remain unchanged.
- [x] DX6.27 Test project preset save/reload/apply.
- [x] DX6.28 Test Undo and multi-object application.

### DX6 validation note

- Added Editor-only apply-once preset data with explicit owned serialized-property paths; applying a preset copies only those paths from its captured snapshot, so unrelated component state remains unchanged.
- Container and Item presets share the same Editor-only application infrastructure; linked/live preset state was intentionally not introduced.
- A separate responsive-preset representation was intentionally not added because responsive-profile authoring remains cleanly owned by DX7; the wrapping-cards built-in uses existing Flex wrapping without creating a second responsive configuration model.
- Built-ins cover Horizontal Row, Vertical Stack, Centered Panel, Toolbar, Sidebar + Content, Scrollable List Content, Responsive/Wrapping Cards, Flexible Item, Spacer, and Fit Content Item.
- Project presets are Editor-only `ScriptableObject` assets with Save Current, Apply, and Open/Edit workflows.
- `Window/TaffyUGUI/Preset Browser` provides search, category filtering, compact previews, unified built-in/project results, and apply-to-selection behavior.
- Preset application uses Undo, prefab-instance recording, and multi-selection while remaining fully isolated from runtime/native assemblies.
- Permanent DX6 coverage added in `TaffyDX6PresetTests.cs`.
- Unity `6000.4.3f1` Edit Mode: **94/94 PASS**; Play Mode: **9/9 PASS**.
- Unity `2021.3.39f1` Edit Mode: **94/94 PASS** using the documented temporary local `bee_backend --stdin-canary` workaround; the Editor installation was restored to SHA-256 `8561ed19e6d35e1e947b450dd528867e7c43c9fe43b5cce9086b58d3cad4fa67`.
- No runtime/native source or serialized runtime contract changed in DX6.

### DX6 exit criteria

- [x] Common reusable layout styles can be shared without manual field replication.
- [x] Built-in and project presets appear in one browser.
- [x] Preset application is deterministic and Undo-safe.
- [x] No runtime dependency on Editor presets is introduced.

---

# DX7 — Visual Responsive & Grid Authoring

**Status:** COMPLETE
**Depends on:** DX6  
**Goal:** make the two most complex authoring areas—responsive overrides and Grid—understandable visually.

## Responsive profiles

- [x] DX7.1 Replace raw responsive-profile list presentation with a breakpoint-oriented editor.
- [x] DX7.2 Show profile name, priority, and width/height bounds clearly.
- [x] DX7.3 Show only enabled overrides in the normal profile view.
- [x] DX7.4 Add `+ Override Property` workflow mapped to existing override booleans.
- [x] DX7.5 Add duplicate/overlap diagnostics where they are objectively meaningful.
- [x] DX7.6 Show the currently active profile in the inspector.
- [x] DX7.7 Preserve full raw profile access in Advanced mode.

## Grid builder

- [x] DX7.8 Replace raw track-list-first workflow with visual Columns / Rows editing.
- [x] DX7.9 Support quick 2/3/4-column Grid starters.
- [x] DX7.10 Support Points / Percent / Fraction / Auto / MinContent / MaxContent cleanly.
- [x] DX7.11 Support MinMax editor.
- [x] DX7.12 Support Repeat editor including Count / AutoFit / AutoFill.
- [x] DX7.13 Add visual gap editing.
- [x] DX7.14 Improve Grid item Row/Column/Span authoring.
- [x] DX7.15 Keep named-line/area advanced authoring reachable.

## Tests

- [x] DX7.16 Test responsive editor maps exactly to existing profile fields.
- [x] DX7.17 Test visual Grid track editing against existing Grid compiler structures.
- [x] DX7.18 Test complex existing Grid data survives new visual editors.
- [x] DX7.19 Test Grid validation continues to reject invalid structures.

### DX7 validation note

- Added `Editor/Authoring/TaffyResponsiveGridAuthoring.cs` with breakpoint-oriented responsive-profile cards and visual Grid authoring utilities/GUI; all controls write the existing serialized `TaffyLayoutGroup` / `TaffyLayoutItem` fields.
- Responsive cards show name, priority, width/height bounds, active breakpoint feedback, only enabled overrides, and a `+ Override Property` menu backed by the existing override booleans.
- Ambiguous same-priority breakpoint overlaps are surfaced while the existing runtime `ValidateResponsiveProfiles` path continues to handle duplicate names and invalid bounds.
- Grid authoring now presents visual Columns/Rows lists, 2/3/4 equal-fraction starters, Points/Percent/Fraction/Auto/MinContent/MaxContent/MinMax/Repeat through the existing track drawers, visual gap controls, and clearer item Row/Column/Span actions.
- Named lines/areas and raw profile data remain explicitly reachable in Advanced foldouts; complex existing Grid/Calc data is read without rewrite.
- Permanent DX7 coverage added in `TaffyDX7VisualAuthoringTests.cs`; responsive mapping, overlap detection, Grid starters/placement, complex Grid preservation, and existing Grid validation are covered.
- Unity `6000.4.3f1` Edit Mode: **99/99 PASS**; Play Mode: **9/9 PASS**.
- Unity `2021.3.39f1` Edit Mode: **99/99 PASS** using the documented temporary local `bee_backend --stdin-canary` workaround; the Editor installation was restored to SHA-256 `8561ed19e6d35e1e947b450dd528867e7c43c9fe43b5cce9086b58d3cad4fa67`.
- No runtime/native source or serialized runtime contract changed in DX7.

### DX7 exit criteria

- [x] A normal responsive workflow does not require manually expanding every override boolean.
- [x] Common Grid layouts can be authored visually.
- [x] Advanced Grid/Responsive capability remains intact.

---

# DX8 — Scene View Authoring & Responsive Preview

**Status:** COMPLETE
**Depends on:** DX7  
**Goal:** make layout geometry and responsive behavior visible directly in the Unity Editor.

## Scene overlays
- [x] DX8.1 Refactor current Scene visualization into modular overlay drawing helpers.
- [x] DX8.2 Draw container bounds.
- [x] DX8.3 Draw child bounds.
- [x] DX8.4 Draw padding bounds.
- [x] DX8.5 Draw margin information for selected items where practical.
- [x] DX8.6 Draw Flex main/cross-axis indicators.
- [x] DX8.7 Draw gap markers.
- [x] DX8.8 Improve Grid track visualization with row/column labels.
- [x] DX8.9 Show active responsive-profile label.
- [x] DX8.10 Show optional computed size labels.
- [x] DX8.11 Provide clean per-feature overlay toggles to avoid visual clutter.

## Responsive preview

- [x] DX8.12 Add Desktop / Tablet / Mobile preview presets where safely implementable.
- [x] DX8.13 Add custom preview size workflow.
- [x] DX8.14 Keep preview tooling Editor-only and non-destructive.
- [x] DX8.15 Ensure active profile feedback uses the same resolution semantics as runtime.

## Interactive handles

- [x] DX8.16 Prototype padding handles.
- [x] DX8.17 Prototype gap handles.
- [x] DX8.18 Evaluate fixed-size handles; intentionally omit them because Auto/Percent/Calc sizing makes direct geometric resizing ambiguous.
- [x] DX8.19 Require SerializedProperty + Undo for accepted interactive handles.
- [x] DX8.20 Drop any handle interaction that proves ambiguous rather than forcing it into the product.

## Tests

- [x] DX8.21 Add non-rendering tests for overlay state/preferences and selection safety.
- [x] DX8.22 Ensure Scene tooling never mutates layout unless a handle is actively changed.
- [x] DX8.23 Test Undo for accepted handles.

### DX8 validation note

- Scene visualization is split into modular Editor-only overlay helpers for container/child/padding/margin bounds, Flex axes, gaps, Grid track labels, responsive-profile feedback, and optional computed-size labels.
- Desktop (1440×900), Tablet (1024×768), Mobile (390×844), and custom responsive preview sizes are stored only in `EditorPrefs` and rendered as non-destructive ghost viewports; preview profile selection mirrors runtime width/height matching and priority rules.
- Accepted interactive handles are limited to padding and gaps, are opt-in, and write existing serialized Group fields through `SerializedProperty` with Undo and prefab-instance recording. Fixed-size handles were deliberately dropped because they would be ambiguous for Auto/Percent/Calc sizing.
- Permanent DX8 coverage added in `TaffyDX8SceneAuthoringTests.cs` for overlay preferences, preview resolution, read-only inspection, handle opt-in safety, and Undo.
- Unity `6000.4.3f1` Edit Mode: **104/104 PASS**; Play Mode: **9/9 PASS**.
- Unity `2021.3.39f1` Edit Mode: **104/104 PASS** using the documented temporary local `bee_backend --stdin-canary` workaround; the Editor installation was restored to SHA-256 `8561ed19e6d35e1e947b450dd528867e7c43c9fe43b5cce9086b58d3cad4fa67`.
- No runtime/native source or serialized runtime contract changed in DX8.

### DX8 exit criteria

- [x] Selected layouts can be understood spatially from Scene View.
- [x] Grid and Flex direction are visually obvious.
- [x] Responsive profile state is visible.
- [x] Interactive handles exist only where safe and unambiguous.

---

# DX9 — Guided Creation & Onboarding

**Status:** COMPLETE
**Depends on:** DX8  
**Goal:** let a new developer create useful Taffy layouts without manually assembling every GameObject/component relationship.

## Hierarchy recipes

- [x] DX9.1 Expand `TaffyUGUI` Hierarchy create menu.
- [x] DX9.2 Horizontal Layout recipe.
- [x] DX9.3 Vertical Layout recipe.
- [x] DX9.4 Centered Panel recipe.
- [x] DX9.5 Toolbar recipe.
- [x] DX9.6 Sidebar + Content recipe.
- [x] DX9.7 Scrollable List recipe.
- [x] DX9.8 Responsive Grid/Cards recipe.
- [x] DX9.9 Modal structure recipe.
- [x] DX9.10 Basic Form layout recipe.
- [x] DX9.11 Ensure recipes create ordinary Unity/Taffy objects only.

## First-use onboarding

- [x] DX9.12 Create dismissible first-use guide.
- [x] DX9.13 Add Create Your First Layout workflow.
- [x] DX9.14 Link directly to relevant package samples.
- [x] DX9.15 Link directly to Getting Started documentation.
- [x] DX9.16 Add optional small inspector getting-started checklist for newly configured Groups.

## UI Builder

- [x] DX9.17 Create `Window > TaffyUGUI > UI Builder`.
- [x] DX9.18 Reuse the preset/recipe libraries instead of duplicating layout definitions.
- [x] DX9.19 Provide category/search workflow.
- [x] DX9.20 Provide concise preview/summary.
- [x] DX9.21 Create selected recipe in the current scene with Undo.
- [x] DX9.22 Avoid introducing a separate persistent Builder document/state model.

## Tests

- [x] DX9.23 Test each hierarchy recipe structure and serialized settings.
- [x] DX9.24 Test Undo removes/restores created recipe objects correctly.
- [x] DX9.25 Test onboarding preference/dismissal behavior.
- [x] DX9.26 Test Builder-created results are ordinary editable TaffyUGUI scene objects.

### DX9 validation note

- Added a central Editor-only creation recipe catalog shared by Hierarchy menu commands and `Window > TaffyUGUI > UI Builder`.
- Recipes cover Horizontal, Vertical, Centered Panel, Toolbar, Sidebar + Content, Scrollable List, Responsive Cards, Modal, and Basic Form using ordinary Unity/Taffy scene objects.
- Scrollable List and Responsive Cards reuse the existing DX6 built-in preset semantics rather than introducing duplicate runtime configuration models.
- Added a dismissible first-use guide, Create Your First Layout action, direct package sample/documentation links, and optional per-Group inspector checklist.
- Permanent DX9 coverage added in `TaffyDX9GuidedCreationTests.cs` for recipe structure/settings, Undo, onboarding preferences, shared Builder infrastructure, and ordinary editable scene objects.
- Unity `6000.4.3f1` Edit Mode: **108/108 PASS**; Play Mode: **9/9 PASS**.
- Unity `2021.3.39f1` Edit Mode: **108/108 PASS** using the documented temporary local `bee_backend --stdin-canary` workaround; the Editor installation was restored to SHA-256 `8561ed19e6d35e1e947b450dd528867e7c43c9fe43b5cce9086b58d3cad4fa67`.
- No runtime/native source or serialized runtime contract changed in DX9.

### DX9 exit criteria
COMPLETE
- [x] First-time users can create useful layouts from guided workflows.
- [x] Builder and Hierarchy menus reuse the same recipe/action infrastructure.
- [x] No special runtime representation is introduced.

---

# DX10 — Explain Layout & Expert Productivity

**Status:** COMPLETE
**Depends on:** DX9  
**Goal:** make difficult layout debugging understandable while keeping expert workflows fast.

## Computed layout panel

- [x] DX10.1 Show computed position/size for the selected Group/Item where available.
- [x] DX10.2 Show resolved responsive profile.
- [x] DX10.3 Show measured content information where available.
- [x] DX10.4 Show parent context and effective display/direction.
- [x] DX10.5 Show relevant Grid diagnostics inline.

### DX10 computed-layout progress note

- Shared `TaffyComputedLayoutSnapshot` now reports current RectTransform geometry, resolved responsive profile, parent/effective display and Flex direction, intrinsic measurement data when a source is available, and read-only Grid validation feedback.
- The computed panel is shared by Group and Item inspectors and does not write serialized layout state.
- Permanent DX10 computed-state coverage now includes responsive overrides, parent context, intrinsic measurement, Grid validation, and non-mutation checks.
- Unity `6000.4.3f1` Edit Mode after DX10.5: **112/112 PASS**.
- No runtime/native source or serialized runtime contract changed.

## Explain Layout

- [x] DX10.6 Add `Explain Layout` entry point.
- [x] DX10.7 Explain fixed size.
- [x] DX10.8 Explain percentage size.
- [x] DX10.9 Explain content/intrinsic measurement contribution.
- [x] DX10.10 Explain padding contribution.
- [x] DX10.11 Explain active responsive overrides.
- [x] DX10.12 Explain common Flex Grow allocation at a useful high level.
- [x] DX10.13 Explain Grid placement/track result where deterministically available.
- [x] DX10.14 Avoid claiming exact internal reasoning when the data is insufficient.

## Expert productivity

- [x] DX10.15 Add Inspector search for Advanced settings.
- [x] DX10.16 Add alias keywords such as `clip` → Overflow and `center` → alignment.
- [x] DX10.17 Add `Essentials | Modified | All` view filter.
- [x] DX10.18 Add section-level Reset.
- [x] DX10.19 Add Copy/Paste Size.
- [x] DX10.20 Add Copy/Paste Spacing.
- [x] DX10.21 Add Copy/Paste Flex.
- [x] DX10.22 Add Copy/Paste Grid placement where safe.
- [x] DX10.23 Add Comfortable / Compact inspector density preference.
- [x] DX10.24 Evaluate Beginner / CSS-Taffy terminology preference; deliberately omit until real-use evidence justifies duplicated terminology.
- [x] DX10.25 Add direct contextual documentation links.
- [x] DX10.26 Evaluate compact hierarchy badges/icons; deliberately omit to keep hierarchy presentation unobtrusive.

## Debugger integration

- [x] DX10.27 Upgrade `TaffyLayoutDebuggerWindow` to reuse formal diagnostics and computed-layout data.
- [x] DX10.28 Avoid duplicate diagnostic logic between Inspector and Debugger.
- [x] DX10.29 Add selection/navigation helpers where useful.

## Tests

- [x] DX10.30 Test Explain Layout output for deterministic representative cases.
- [x] DX10.31 Test Modified filtering and search aliases.
- [x] DX10.32 Test copy/paste and reset Undo behavior.
- [x] DX10.33 Test debugger and inspector share the same diagnostic rule results.

### DX10 validation note

- Added a shared read-only computed-layout snapshot for Inspector and Debugger, plus a shared Explain Layout model that only states deterministic conclusions from serialized/current layout state.
- Advanced Inspector productivity now includes search aliases, Essentials/Modified/All filtering, section reset, safe Copy/Paste for Size/Spacing/Flex/Grid placement, density preference, and direct contextual documentation links.
- The formal Debugger reuses the same computed-layout and diagnostic rule results as the Inspector rather than duplicating logic.
- Permanent DX10 coverage includes computed layout, Explain Layout, Advanced search/filter behavior, expert actions/Undo, and Debugger integration.
- Unity `6000.4.3f1` Edit Mode: **130/130 PASS**; Play Mode: **9/9 PASS**.
- Unity `2021.3.39f1` Edit Mode: **130/130 PASS** using the documented temporary local `bee_backend --stdin-canary` workaround; the Editor installation was restored to SHA-256 `8561ed19e6d35e1e947b450dd528867e7c43c9fe43b5cce9086b58d3cad4fa67`.
- No runtime/native source or serialized runtime contract changed in DX10.
### DX10 exit criteria



- [x] Common layout decisions can be explained from current state.
- [x] Advanced users can search/filter/copy layout settings efficiently.
- [x] Debugger and Inspector share underlying diagnostic/computed state instead of diverging.

---

# DX11 — Hardening, Documentation & Final DX Gate

**Status:** ACTIVE
**Depends on:** DX10  
**Goal:** convert the redesigned Editor experience from feature-complete into production-ready tooling.

## UX consistency audit

- [x] DX11.1 Audit every Group field for clear label and tooltip/help coverage.
- [x] DX11.2 Audit every Item field for clear label and tooltip/help coverage.
- [x] DX11.3 Audit every custom drawer for consistent terminology and spacing.
- [x] DX11.4 Audit Simple mode for unnecessary controls.
- [x] DX11.5 Audit Advanced mode for complete property access.
- [x] DX11.6 Audit multi-object editing across all major sections/actions.
- [x] DX11.7 Audit prefab-instance editing and Undo.

### DX11 UX audit note

- Added `TaffyDX11EditorConsistencyTests` to lock down separate Group/Item tooltip coverage, exact Advanced-section property coverage, the intentionally small Simple-mode property sets, multi-object editor registration, and shared custom-drawer spacing infrastructure.
- Reviewed all custom drawers for consistent `TaffyDrawerUtility.Line` / `Gap` spacing and human-facing labels; no runtime serialization changes were required.
- Existing DX4 and DX10 regressions already exercise multi-object quick actions, prefab-instance connection preservation, section reset/copy-paste Undo, and action Undo behavior.
- Unity `6000.4.3f1` Edit Mode after the UX audit: **135/135 PASS**.

## Documentation

- [x] DX11.8 Rewrite Getting Started around the new Simple/Quick Layout workflow.
- [x] DX11.9 Add Inspector modes documentation.
- [x] DX11.10 Add Quick Actions documentation.
- [x] DX11.11 Add Layout Health/diagnostics documentation.
- [x] DX11.12 Add Presets documentation.
- [x] DX11.13 Add visual Grid/Responsive authoring documentation.
- [x] DX11.14 Add Scene View/preview documentation.
- [x] DX11.15 Add UI Builder/recipes documentation.
- [x] DX11.16 Add Explain Layout/debugging documentation.
- [x] DX11.17 Update samples so documentation and samples use the recommended new workflows.

### DX11 documentation note

- Rewrote packaged Getting Started around Hierarchy recipes, Simple mode, Quick Layout, Item intent actions, Layout Health, Computed Layout, Explain Layout, and responsive/Scene workflows.
- Added `Documentation~/editor-workflows.md` as the focused reference for Inspector modes, Quick Actions, diagnostics, presets, visual Grid/Responsive authoring, Scene preview/handles, UI Builder recipes, expert clipboard/reset workflows, Explain Layout, and the shared Debugger.
- Updated the documentation index and all three package sample READMEs to point users toward the same recommended Editor workflows while retaining the runtime sample behavior.
## Editor performance and reload hardening

- [x] DX11.24 Run the full maintained Unity Edit Mode suite.
- [x] DX11.19 Ensure diagnostics do not perform expensive native recomputation unnecessarily.
- [x] DX11.20 Ensure preset/browser asset scanning is cached appropriately.
- [x] DX11.21 Ensure Scene overlays remain optional and do not create editor stalls.
- [x] DX11.22 Test domain reload and assembly reload behavior for Editor preferences/services.

### DX11 hardening note

- Added `TaffyDX11HardeningTests` covering a representative 128-item multi-selection Inspector-state workload, read-only diagnostics, preset scan caching/invalidation, optional Scene-overlay defaults/persistence, and EditorPrefs-backed preference state.
- Unity `6000.4.3f1` profiling result: **128 targets × 8 passes = 36 ms**, with **524,288 bytes** retained managed-memory delta; the permanent regression budgets are intentionally conservative to avoid machine-specific flakiness.
- Diagnostic evaluation remains Editor-side/read-only and contains no direct `TaffyNative` calls or layout-dirty mutation.
- `TaffyPresetCatalog` now caches project asset scanning and invalidates on `EditorApplication.projectChanged`, avoiding repeated full `Assets/` scans during browser repaint/refresh.
- Expensive supplemental Scene overlays remain opt-in, while established container/child/Grid/profile overlays retain their existing defaults.
- Inspector mode/density and Scene overlay state remain EditorPrefs-backed rather than dependent on volatile static state, so assembly/domain reloads preserve user choices; static service caches rebuild safely after reload.
- Unity `6000.4.3f1` Edit Mode after DX11.22: **140/140 PASS**.

## Compatibility regression

### DX11 native regression note

- Maintained Rust suite: **46/46 PASS** (`37` crate unit tests + `9` native verification integration tests; doc-tests contained `0` tests).
- No runtime/native source changes were required for DX11.23.

- [x] DX11.23 Run full maintained native Rust test suite.
- [x] DX11.24 Run full Unity Edit Mode suite.
- [x] DX11.25 Run full Unity Play Mode suite.
- [x] DX11.26 Validate Unity `2021.3` package compile/editor behavior.
- [x] DX11.27 Validate selected newer Unity LTS/Unity 6 Editor behavior.
- [x] DX11.28 Confirm Player builds still exclude `TaffyUGUI.Editor`.
- [x] DX11.29 Confirm Android ARM64 native/runtime behavior is unchanged by Editor work.
- [x] DX11.30 Run `git diff --check` and repository hygiene checks.
- [x] DX11.31 Confirm no `.build`, harness, probe, validation project, or generated `dist` content is tracked.

### DX11 compatibility validation note

- Native Rust workspace: **46/46 PASS** (`37` unit + `9` native verification); maintained allocation, bulk-ABI, and layout benchmark targets also executed successfully.
- Unity `6000.4.3f1`: **140/140 Edit Mode PASS**, **9/9 Play Mode PASS**.
- Unity `2021.3.39f1`: **140/140 Edit Mode PASS** using the documented temporary local `bee_backend --stdin-canary` workaround; the Editor installation was restored to SHA-256 `8561ed19e6d35e1e947b450dd528867e7c43c9fe43b5cce9086b58d3cad4fa67`.
- Unity `2022.3.62f1`: **140/140 Edit Mode PASS** as an additional maintained LTS compatibility check; Unity `6000.4.3f1` covers the selected Unity 6 validation.
- A disposable Linux Player build under ignored `.build/` contains `TaffyUGUI.Runtime.dll` and excludes `TaffyUGUI.Editor.dll`, confirming Editor assembly isolation in Player output.
- Android ARM64 plugin remains unchanged: shipped `libtaffy_ugui.so` is ELF64 AArch64 and SHA-256 `b3269ae1caa3b3232e45d06a2cc45d3873cdb25de31c5d262fef82cca2c30e12`, exactly matching its committed provenance; no `native/`, `UnityPackage/Runtime/`, or Android plugin source/artifact changes are present.
- `git diff --check` passes; repository scan finds no tracked `.build`, probe, harness, validation-project, or generated `dist` content.

## Final usability acceptance

- [x] DX11.32 New-user scenario: create Horizontal layout from an empty Canvas without reading external docs.
- [x] DX11.33 New-user scenario: create a responsive card/list layout from a recipe/preset.
- [x] DX11.34 New-user scenario: diagnose a Unity layout ownership conflict from Layout Health.
- [x] DX11.35 Advanced-user scenario: configure detailed Grid/Calc settings without losing access to raw capabilities.
- [x] DX11.36 Advanced-user scenario: use search/Modified view/copy-paste efficiently on a complex hierarchy.

### DX11 usability acceptance note

- Horizontal-from-empty workflow is covered by the shared Hierarchy/UI Builder recipe catalog and `TaffyDX9GuidedCreationTests`; the created object is an ordinary editable Flex Row group.
- Responsive cards/list creation is covered by the `responsive-cards` and `scrollable-list` recipes plus DX6/DX9 preset/recipe tests.
- Layout ownership conflicts are surfaced and repairable through Layout Health; `TaffyDX5LayoutHealthTests` covers competing Unity Layout Groups and `ContentSizeFitter` ownership fixes with Undo/prefab safety.
- Advanced Grid/Calc authoring remains accessible and preservation-tested by DX3/DX5/DX7 coverage, including complex Calc data and Grid validation/placement.
- Expert search, Modified filtering, section reset, and Copy/Paste workflows are covered by DX10 Advanced Filter and Expert Actions tests.

### DX11 exit criteria

- [x] Full regression gate passes.
- [x] Existing runtime/native behavior is preserved.
- [x] Editor tooling is performant enough for practical complex scenes.
- [x] Documentation matches the redesigned UX.
- [x] New-user and advanced-user acceptance scenarios pass.
- [x] Repository remains clean of disposable validation material.

### Recommended commit boundary

Final DX hardening/documentation commit only after all mandatory DX11 gates pass.

---

# Permanent Validation Rules Per Phase

Every phase that changes C# Editor code should perform at minimum:

```text
1. Read VS Code Problems before implementation/fixes.
2. Compile in a maintained Unity Editor baseline.
3. Run directly affected Edit Mode tests.
4. Run the full maintained Edit Mode suite before closing the phase.
5. Run the maintained Play Mode suite when runtime-facing serialization/state could be affected.
6. Run git diff --check.
7. Inspect git status for accidental generated files.
```

If a phase changes runtime/native code, it additionally requires the appropriate native/build verification gate.

Disposable validation projects and probes must remain under ignored `.build/` only.

---

# Phase Commit Strategy

The program should be implemented in phase-sized commits where practical.

Recommended commit themes:

```text
DX0  Protect editor serialization baseline
DX1  Modularize TaffyUGUI editor architecture
DX2  Add beginner-first Taffy inspectors
DX3  Add intent-first layout property controls
DX4  Add Taffy quick layout actions
DX5  Add layout health diagnostics
DX6  Add reusable Taffy layout presets
DX7  Add visual responsive and Grid authoring
DX8  Improve Scene layout authoring and preview
DX9  Add guided Taffy UI creation workflows
DX10 Add layout explanation and expert productivity tools
DX11 Harden and document the TaffyUGUI developer experience
```

Do not commit a phase as complete until its gate is satisfied.

---

# Scope Deferred Beyond This Program

These ideas are intentionally outside the mandatory DX completion gate unless separately approved:

```text
Runtime-linked/live presets with override masks
Full visual named Grid-area designer
Large-scale UI Toolkit rewrite of all inspectors
Runtime visual UI builder format
Native algorithm changes solely for Editor convenience
32-bit ABI redesign
New platform-support claims
Remote package publication
```

They can be revisited after the core developer experience is proven in real use.

---

# Current Next Action

Continue **DX11 — Hardening, Documentation & Final DX Gate**.

First authoritative task:

DX11.18 Profile Inspector allocation/repaint cost on representative large selections.
```
```

Keep documentation aligned with the shipped Editor workflows and preserve the existing runtime/native layout model.
