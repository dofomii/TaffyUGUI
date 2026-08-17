# Phase 11 — Editor Tooling and Migration

**Status:** COMPLETE
**Unity validation host:** Unity `6000.4.3f1` on Linux
**Runtime release scope:** Android ARM64 only
**Native ABI:** unchanged final ABI v1 (`version=1`, `stage=2`)

## Goal

Phase 11 turns the completed runtime integration into a practical Unity authoring and migration workflow without moving editor dependencies into Player assemblies.

All Phase 11 production editor code lives in the Editor-only `TaffyUGUI.Editor` assembly. The runtime assembly remains free of `UnityEditor` dependencies.

## Custom inspectors

`TaffyLayoutGroupEditor` provides grouped authoring for:

- formatting context and box-model settings;
- Flex direction/wrap/gaps/alignment;
- Grid templates, implicit tracks, named lines, named areas, and auto-flow;
- responsive profiles and integration settings;
- validation messages and integration warnings;
- live responsive-profile/rebuild diagnostics;
- Grid diagnostics access;
- direct access to the layout debugger and Scene overlay.

`TaffyLayoutItemEditor` provides grouped authoring for:

- display, writing direction, overflow, and box sizing;
- position/inset/size/min/max/aspect ratio;
- margin/padding/border;
- Flex basis/grow/shrink/alignment;
- Grid placement and justify-self;
- Block/Float settings;
- intrinsic measurement settings and manual measurement invalidation.

Both editors support multi-object editing for serialized authoring. Live diagnostics are shown only for a single selected component.

## Typed property drawers

Phase 11 adds dedicated drawers for the serialized authoring types rather than exposing raw nested structs:

- `TaffyLength` with Auto/Points/Percent/Calc-aware editing;
- `TaffyEdges`;
- `TaffyPixelInsets`;
- recursive `TaffyCalcExpression` authoring;
- `TaffyGridTrackBreadth`;
- `TaffyGridTrack`, including MinMax and Repeat modes;
- `TaffyGridPlacement` with line/span/named-line/named-span forms;
- `TaffyGridNamedLine`;
- `TaffyGridArea`.

Grid and Calc fields only expose the data relevant to the selected enum kind, reducing invalid serialized combinations during normal Inspector authoring. The drawers bind directly to the production Phase 9 serialized schema (`TaffyCalcExpression.operation/value/operands`, `TaffyGridTrack.min/max/repeatTracks`, and named-placement occurrence/span payloads), so editor authoring cannot silently drift from the runtime data model. Runtime validation remains the final authority.

## Scene-view visualization

`TaffySceneVisualization` is registered through `SceneView.duringSceneGui` and can be toggled from:

`Tools > TaffyUGUI > Toggle Scene Visualization`

For selected `TaffyLayoutGroup` objects it draws:

- the container RectTransform boundary;
- child RectTransform boundaries;
- the current display/profile label;
- resolved Grid track lines when detailed Grid diagnostics are available.

The toggle is stored in `EditorPrefs`; it does not modify scene or prefab data.

## Layout debugger

`Tools > TaffyUGUI > Layout Debugger` opens the editor diagnostics window.

The debugger can inspect all active loaded-scene Taffy groups or only selected hierarchies and reports:

- resolved display and RectTransform size;
- active responsive profile;
- min/preferred uGUI layout input;
- suppressed rebuild request count;
- responsive/Grid validation errors;
- integration warnings;
- detailed Grid track/item counts and resolved track sizes.

It also exposes explicit rebuild and rebuild-counter reset actions.

## Migration workflow

`Tools > TaffyUGUI > Migration Window` provides analysis, individual migration, and batch migration of loaded scene/prefab-stage objects.

Migration is deliberately conservative. Unsupported legacy semantics are diagnosed and left unchanged.

### HorizontalLayoutGroup

Compatible Horizontal groups migrate to a single-line Flex row. The migration preserves or maps:

- enabled state;
- RectOffset padding;
- spacing to horizontal gap;
- horizontal child alignment to `justifyContent`;
- vertical child alignment to `alignItems`;
- reverse arrangement where the Unity version serializes it;
- fixed child RectTransform width/height when legacy child-control flags are disabled;
- main-axis force-expand through `TaffyLayoutItem.flexGrow`.

Legacy child-scale control is rejected because TaffyUGUI intentionally does not mutate child `localScale` during layout.

### VerticalLayoutGroup

Vertical groups use the same rules with a single-line Flex column, vertical gap, vertical main-axis justification, horizontal cross-axis alignment, and main-axis grow mapping.

### GridLayoutGroup

Automatic Grid migration is limited to configurations that can be reproduced deterministically:

- `FixedColumnCount` + Horizontal start axis; or
- `FixedRowCount` + Vertical start axis;
- Upper Left start corner;
- positive constraint count;
- non-negative cell size and spacing.

Fixed-column migration creates explicit fixed-width columns plus a fixed implicit-row track and row-major auto-flow. Fixed-row migration creates explicit fixed-height rows plus a fixed implicit-column track and column-major auto-flow. Each child receives explicit Taffy cell dimensions.

`Flexible` legacy grids and incompatible start corner/axis configurations are rejected with a diagnostic instead of silently changing layout semantics.

## Undo, prefab, and serialized-data safety

Unity's layout-group hierarchy prevents a Taffy layout group from being added while another `LayoutGroup` remains on the same GameObject. The migration service therefore:

1. validates and snapshots all required legacy settings and child references;
2. increments to a dedicated named Unity Undo group so migration cannot collapse into a preceding user operation;
3. removes the legacy component through `Undo.DestroyObjectImmediate`;
4. adds the new `TaffyLayoutGroup` through `Undo.AddComponent`;
5. applies the captured settings;
6. adds or updates `TaffyLayoutItem` only where migration semantics require it;
7. records prefab-instance property modifications for generated Taffy components;
8. collapses the operation into one Undo step.

Existing `TaffyLayoutItem` components are reused and unrelated serialized values are preserved.

Prefab-instance migration keeps the prefab instance connected and records added/removed component overrides; the prefab asset itself is not modified.

## Batch migration

The migration window can scan either:

- the current Selection and its descendants; or
- all supported legacy layout groups in loaded scenes.

`Migrate All Safe` migrates only entries that pass analysis. Unsafe entries remain untouched and visible with their diagnostic reason.

The underlying `TaffyMigrationService.MigrateAll` also returns one result per distinct source so editor workflows can report partial success explicitly.

## Permanent Phase 11 tests

Phase 11 adds 9 Edit Mode tests covering:

- custom inspector registration;
- typed property-drawer availability;
- Horizontal migration and preservation of existing item data;
- Vertical migration and main-axis expansion;
- deterministic fixed-column Grid migration;
- rejection of unsafe Flexible Grid migration;
- Undo restoration of the legacy component without consuming a preceding user Undo operation;
- prefab-instance migration without mutating the prefab asset;
- batch migration result handling;
- debugger/Scene-visualization editor type availability and persisted visualization toggle.

Final package regression counts:

- Edit Mode: **38/38 passed**;
- Play Mode: **9/9 passed**.

The Edit Mode total contains all previous Phase 7–10 tests plus the new Phase 11 editor/migration tests.

## Native and Android regression

Phase 11 does not change the Rust engine, final C ABI, or managed runtime ABI. Final native/Android provenance and Player packaging are verified against the exact completed Phase 11 source snapshot:

`sha256:3228f12128c07fd6c470a7bc9119a4ba810f7718d98c6ae9537086030beaa0fc`

Android ARM64 native library SHA-256 remains:

`7bdca92aae2939e5098292294ee7f7d730d5eee1c718d87f65a3f22349338f66`

The fresh Android ARM64 IL2CPP Player gate passes. `TaffyUGUI.Runtime.dll` is present in `ManagedStripped`, while `TaffyUGUI.Editor.dll` is absent there and absent from the IL2CPP conversion assembly list. The APK contains both `libil2cpp.so` and `libtaffy_ugui.so`; the packaged Taffy ELF has identical program headers and byte-identical runtime `PT_LOAD` segments to the accepted staged payload.

## Phase 11 exit gate

Phase 11 closes only when P11.1–P11.11 are complete, all permanent Unity tests pass, the native/ABI and Android Phase 4/5 regression gates remain green on the exact source snapshot, a fresh Android ARM64 IL2CPP Player build succeeds, and disposable validation material is removed.

## Next authoritative task

**Phase 12 P12.1 — Unity 2021.3 LTS primary Editor validation.**
