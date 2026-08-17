# Phase 7 — Minimal Working Unity uGUI Product

**Status:** COMPLETE
**Status date:** 2026-08-17
**Native ABI:** final ABI v1 (`version=1`, `stage=2`)
**Active release scope:** Android ARM64 only

## Purpose

Phase 7 turns the Phase 6 managed/native ABI into a production-shaped Unity `LayoutGroup` bridge. Unity continues to own rendering, interaction, hierarchy, `RectTransform`, `LayoutElement`, and the uGUI rebuild lifecycle. Taffy owns layout geometry only.

This phase deliberately stays narrow. Full Flex/Block/Float authoring, measurement adapters, TextMeshPro/Image intrinsic measurement, Grid/Calc authoring, responsive profiles, ScrollRect integration, and editor tooling remain later phases.

## Production lifecycle

`TaffyLayoutGroup` now owns a persistent native context and root node for its enabled lifetime.

- ABI validation occurs before the first context is created.
- The native context/root are created once and reused across layout passes.
- Disable/destruction tears the context down and clears all managed native-handle state.
- Re-enable recreates a fresh context/root safely.
- Layout passes no longer clear and rebuild the entire native context.

## Stable node mapping and incremental synchronization

Each participating child `RectTransform` has a stable native node record while it remains in the group.

- Missing children create native nodes once.
- Removed/ignored/reparented children remove their native nodes.
- Same-count child replacement is handled correctly.
- Sibling reordering updates only root topology.
- Child/root styles are compared with cached values and uploaded only when relevant Phase 7 properties change.
- Root child topology is sent only when ordered native handles change.
- Child geometry is retrieved with the bulk layout-result ABI.

## uGUI layout-system integration

The group participates in both uGUI calculation and arrangement phases.

- `CalculateLayoutInputHorizontal()` performs native intrinsic min/preferred passes and reports horizontal metrics with `SetLayoutInputForAxis`.
- `CalculateLayoutInputVertical()` refreshes intrinsic data after the horizontal phase so nested groups can publish current vertical metrics, then reports vertical metrics.
- Minimum and preferred intrinsic passes use Taffy max-content available space.
- `SetLayoutHorizontal()` and `SetLayoutVertical()` consume one cached arranged Taffy layout when inputs have not changed.
- Native geometry is applied with `LayoutGroup.SetChildAlongAxis`; no rendering, input, graphic, or event component is replaced.

Phase 7 reports group flexible size as `0`. More advanced parent-facing flexibility and content measurement belong to Phase 8/10.

## LayoutElement and TaffyLayoutItem behavior

uGUI's `rectChildren` inventory remains authoritative, so `LayoutElement.ignoreLayout` semantics are preserved.

For participating children, Phase 7 uses `LayoutUtility` min/preferred/flexible metrics as the intrinsic bridge. `TaffyLayoutItem` can override explicit Taffy dimensions and Flex item properties. An `Auto` Taffy length now preserves the uGUI-derived intrinsic value instead of erasing it; native measurement adapters will supersede this bridge in Phase 8.

A `TaffyLayoutItem` attached to a nested layout-group object dirties the parent Taffy group that consumes that item style, while the nested group independently owns its descendant layout.

## Nested groups

Nested `TaffyLayoutGroup` components are supported as independent layout owners:

1. the child group publishes its own min/preferred uGUI layout inputs;
2. its parent Taffy group consumes those metrics for the child node;
3. the parent positions/sizes the nested group's `RectTransform`;
4. the nested group uses its own persistent native context to arrange its descendants.

This avoids cross-group native-handle lifetime coupling and matches uGUI's hierarchical layout model.

## Permanent Unity regression tests

Phase 7 adds maintained package tests under `UnityPackage/Tests`.

Edit Mode coverage:

- intrinsic min/preferred size reporting;
- sibling reorder and same-count child replacement;
- `LayoutElement` sizing and `ignoreLayout` preservation;
- nested groups and context recreation after disable/enable.

Play Mode coverage:

- runtime container resize;
- flexible child recomputation;
- disable/enable lifecycle stability.

## Local verification evidence

Executed locally with Unity `6000.4.3f1` on Linux using a temporary ignored host project and a locally built host native library:

- VS Code Problems: **0 diagnostics** for Phase 7 runtime/tests.
- `python3 build/build.py quality`: **PASS**.
- Rust maintained suite: **44/44 passed**.
- Unity Edit Mode: **4/4 passed**, 0 failed/skipped.
- Unity Play Mode: **1/1 passed**, 0 failed/skipped.
- Android ARM64 IL2CPP development APK build: **PASS**.
- The APK contains `lib/arm64-v8a/libtaffy_ugui.so`.
- The APK copy has the same ELF program headers and byte-identical runtime-loaded `PT_LOAD` segments as the accepted Phase 6 Android native payload.

No Android device was available during the final Phase 7 validation run, so the Phase 7 APK was **not** executed on physical hardware. Physical Android execution is not a Phase 7 exit requirement; broader real-platform validation remains Phase 12. Phase 6 already contains the earlier physical-device proof for the frozen ABI/native payload.

All temporary Unity build/runtime-check sources and generated host artifacts used for Phase 7 validation are local-only under ignored `.build/` paths and are not project source.

## Exit gate

**PASS — Phase 7 Minimal Working Unity uGUI Product is complete.**

The next authoritative phase is **Phase 8 — Production Flex / Block / Float / Measurement Unity Integration**, beginning with **P8.1: complete Unity authoring for core size/min/max/box model**.
