# Phase 9 — Complete Grid and Calc Unity Authoring

**Status:** COMPLETE
**Date:** 2026-08-17
**Active release scope:** Android ARM64 only
**Native ABI:** final ABI v1 (`version=1`, `stage=2`), exact Taffy `0.13.0`

## Outcome

Phase 9 exposes the already-implemented native Grid and Calc engine through production Unity authoring. No C ABI expansion was required: the frozen ABI already included Grid template upload, Grid placement fields, typed Calc resource creation/removal, and detailed Grid diagnostics.

Unity continues to own GameObjects, RectTransforms, rendering, input, serialization, and the uGUI rebuild lifecycle. Managed code validates and marshals Grid/Calc authoring into the persistent native context; Rust/Taffy remains the layout engine.

## P9.1 — Serializable Grid track/unit model

`TaffyGridTypes.cs` adds serializable authoring types for:

- Grid track kinds: Auto, points, percent, fraction (`fr`), minmax, min-content, max-content, Calc, Repeat;
- min/max track breadths;
- repeat modes: fixed count, auto-fill, auto-fit;
- Grid axes;
- named Grid lines;
- named template areas;
- Grid item placement: Auto, numeric line, numeric span, named line, named span;
- grid-auto-flow modes;
- public detailed Grid diagnostics data.

The model maps directly onto the existing final ABI and preserves native numeric enum values.

## P9.2–P9.5 — Explicit, implicit, intrinsic, fractional, and repeated tracks

`TaffyLayoutGroup` now exposes:

- explicit row and column track lists;
- implicit/auto row and column track lists;
- `fr` sizing;
- minmax tracks;
- min-content/max-content tracks;
- fixed-count Repeat;
- auto-fill and auto-fit Repeat.

The managed compiler validates constraints before crossing the ABI boundary, including non-negative finite track values, legal minmax minimum/maximum breadths, non-empty Repeat contents, valid repeat counts, and the rule that implicit sizing tracks cannot contain Repeat components.

Permanent tests verify explicit placement geometry, implicit tracks, fractional sizing, minmax/content tracks, fixed Repeat, auto-fill, and auto-fit.

## P9.6–P9.7 — Named lines, spans, and template areas

Grid containers can author named row/column lines and rectangular template areas. Grid items support named-line and named-span placement.

Managed validation rejects empty line/area names, invalid line indices, invalid spans, duplicate area names, and out-of-bounds or empty template-area rectangles before native layout.

Strings are encoded as UTF-8 and pinned only for the duration of `tu_node_set_grid_template` or `tu_node_set_style`; no Grid authoring string pointer is retained by managed code after the native call.

## P9.8 — grid-auto-flow

`TaffyLayoutGroup.gridAutoFlow` maps to Row, Column, RowDense, and ColumnDense native modes. Permanent coverage verifies automatic placement into positive implicit columns when Column flow is selected.

## P9.9–P9.10 — Grid item placement and alignment

`TaffyLayoutItem` now exposes:

- row start/end;
- column start/end;
- numeric lines and spans;
- named lines and spans;
- justify-self.

The existing group `justifyItems`, `alignItems`, and alignment fields are now exercised through Grid as well as Flex. Permanent tests verify cell alignment and per-item justify-self override behavior.

## P9.11 — Typed Calc authoring and resource lifecycle

`TaffyCalc.cs` adds a serializable typed Calc tree with:

- Length;
- Percent;
- Add;
- Subtract;
- Scale;
- Min;
- Max;
- Clamp.

`TaffyLength` now supports `Calc` while preserving the existing Auto/Points/Percent numeric values. Grid tracks and minmax breadths can also reference typed Calc expressions.

Calc expressions are validated for finite values, required operand counts, null operands, and cycles. A per-native-context `TaffyCalcResourceCache` canonicalizes structurally equivalent expressions, creates dependencies before dependents, marks resources used on each synchronization pass, and removes unused resources in reverse creation order so dependent resources are released before their operands.

The cache attaches to each persistent `TaffyLayoutGroup` native context and is discarded/rebuilt when that context is destroyed/recreated. Permanent Edit Mode and Play Mode tests mutate Calc expressions and recreate contexts to verify stable layout and lifecycle behavior.

## P9.12 — Grid/Calc diagnostics and validation

`TaffyLayoutGroup` now provides:

- `ValidateGridAuthoring(out string error)`;
- `GridValidationError`;
- `TryGetGridDiagnostics(out TaffyGridDiagnostics diagnostics, out string error)`.

Detailed diagnostics expose:

- negative/explicit/positive implicit row counts;
- negative/explicit/positive implicit column counts;
- row/column track sizes;
- row/column gutters;
- resolved item row/column line coordinates.

Diagnostics are read from the native detailed-layout APIs after a valid Grid layout. Validation errors are surfaced before native compute wherever possible.

## Incremental synchronization and native lifetime safety

Phase 9 preserves the Phase 7/8 persistent topology model.

- Root style changes invalidate the cached Grid template because native style replacement clears native template resources; the template is then reapplied in the same synchronization pass.
- Grid template signatures avoid unnecessary `tu_node_set_grid_template` calls when authoring is unchanged.
- Grid placement string pointers are normalized out of cached native style structs; managed string signatures are used for change detection instead.
- Calc resources are resolved on every style/template synchronization pass so resources referenced by unchanged native styles remain marked live.
- Unused Calc resources are removed only after the current styles/templates have been updated away from them.
- Managed arrays/strings used for ABI marshalling are pinned only within scoped native calls.

A Phase 9 regression test discovered and fixed the root-style/Grid-template invalidation bug during development: the Arrange pass sets a concrete root size through `tu_node_set_style`, which clears the native Grid template. The bridge now explicitly forces template reapplication after any root style replacement.

## Permanent Unity regression suite

Final Unity `6000.4.3f1` package results:

- Edit Mode: **22/22 passed** (14 previous tests + 8 Phase 9 tests).
- Play Mode: **5/5 passed** (3 previous tests + 2 Phase 9 tests).
- VS Code Problems: **0 diagnostics** for runtime/tests.

Phase 9 coverage includes:

- explicit rows/columns and numeric placement;
- detailed Grid information/track/item diagnostics;
- `fr`, minmax, min-content/max-content;
- fixed Repeat, auto-fill, auto-fit;
- named lines, named spans, named template areas;
- implicit tracks and grid-auto-flow;
- justify-items/justify-self and alignment;
- Calc-backed dimensions and Grid tracks;
- Calc mutation and context recreation;
- live runtime Grid reconfiguration across frames;
- invalid Repeat, area, placement, and cyclic Calc authoring.

The full Phase 7/8 regression suite remained green before the Phase 9 tests were added.

## Native and Android release regression

Native source/ABI behavior was not changed by Phase 9. The maintained native quality gate remains green with **44/44 Rust tests**, rustfmt, Clippy with warnings denied, and release build.

Final content-addressed source snapshot after Phase 9 closure:

`sha256:c9f0607bc7808c2e3fb857c5785780d31b9b1112fbe39275b8b23b6088cb9698`
Final Android ARM64 release regression on that exact snapshot:

- `verify-abi-final`: PASS;
- Android ARM64 native build: PASS;
- Phase 4 acceptance: PASS;
- Phase 5 staging/provenance verification: PASS;
- fresh Unity `6000.4.3f1` ARM64 IL2CPP development APK build from a clean temporary import: PASS;
- APK includes `lib/arm64-v8a/libil2cpp.so` and `lib/arm64-v8a/libtaffy_ugui.so`;
- staged native SHA-256 remains `7bdca92aae2939e5098292294ee7f7d730d5eee1c718d87f65a3f22349338f66`;
- APK/native ELF program headers match and both runtime-loaded `PT_LOAD` segments are byte-identical to the accepted staged payload.

No ADB device was attached for the final Phase 9 APK run. Comprehensive physical Unity Player/device validation remains Phase 12.

Final Android ARM64 provenance/build checks and the fresh Unity IL2CPP package result are recorded after the exact final snapshot is generated.

## Phase 9 exit gate

Phase 9 closes only when all P9.1–P9.12 are complete, the 22 Edit Mode and 5 Play Mode tests pass, final ABI/Phase 4/Phase 5 provenance gates pass on the exact final source snapshot, a fresh Android ARM64 IL2CPP package build succeeds, and temporary validation material is removed.

## Next authoritative task

**Phase 10 P10.1 — implement the responsive profile/breakpoint system.**
