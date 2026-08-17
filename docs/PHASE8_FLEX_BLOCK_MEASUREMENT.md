# Phase 8 — Production Flex / Block / Float / Measurement Unity Integration

**Status:** COMPLETE
**Date:** 2026-08-17
**Active release scope:** Android ARM64 only
**Native ABI:** final ABI v1 (`version=1`, `stage=2`), exact Taffy `0.13.0`

## Outcome

Phase 8 expands the Phase 7 minimal uGUI bridge into the production Flex/Block/Float and intrinsic-measurement layer. Unity still owns rendering, input, text components, images, GameObjects, prefabs, and the uGUI rebuild lifecycle. Rust/Taffy remains responsible only for layout geometry.

No ABI expansion was required. The final 31-function ABI already contained the complete Phase 8 style surface and cached-measurement upload functions.

## P8.1 — Core size, min/max, and box model

`TaffyLayoutItem` now exposes production authoring for:

- width/height;
- min/max width and height;
- margin;
- padding;
- border;
- border-box/content-box sizing;
- aspect ratio.

Typed `TaffyLength`/`TaffyEdges` values preserve Auto, point, and percentage semantics appropriate to the native field being populated. Margins/insets allow signed values where the ABI permits them; padding and border are non-negative.

Permanent tests verify content-box padding/border expansion and min/max clamping.

## P8.2–P8.3 — Flex container and item authoring

`TaffyLayoutGroup` exposes production Flex container fields including:

- row/column and reverse directions;
- wrapping;
- horizontal/vertical gap;
- justify-content;
- align-items;
- align-content;
- justify-items;
- writing direction.

`TaffyLayoutItem` exposes:

- flex-basis;
- flex-grow;
- flex-shrink;
- align-self.

Permanent regression tests verify Flex growth and wrapping geometry.

## P8.4 — Block / FlowRoot / Float / Clear

Container authoring supports Flex, Block, and FlowRoot formatting contexts. Item authoring supports display selection plus left/right float and left/right/both clear.

Permanent regression coverage verifies FlowRoot + float + clear geometry.

## P8.5 — Positioning and remaining production style surface

Phase 8 authoring also covers:

- relative/absolute positioning;
- left/right/top/bottom insets;
- overflow X/Y;
- scrollbar width;
- box sizing;
- LTR/RTL direction;
- aspect ratio;
- text alignment;
- table/replaced-element flags used by the native engine.

Regression coverage verifies absolute inset/aspect geometry, RTL Flex-start placement, and accepted Scroll/Hidden overflow execution.

Grid-specific style authoring remains intentionally deferred to Phase 9.

## P8.6 — Managed intrinsic-measurement cache

`TaffyMeasurement.cs` adds the managed measurement layer.

A child may be measured through, in priority order:

1. `ITaffyMeasurementProvider` custom provider;
2. `TMP_Text`;
3. uGUI `Text`;
4. `Image`;
5. `RawImage`.

The cache is per persistent native node and supports multiple measurement signatures, including separate unbounded/intrinsic and finite arranged-width records. This prevents repeated Unity text/image measurement when the source and relevant available-width bucket have not changed.

Each cached measurement can provide:

- min-content size;
- max-content size;
- preferred size;
- aspect ratio;
- replaced-element status;
- a bounded set of width-dependent samples.

Managed samples are pinned only while they are copied through `tu_node_set_measurement`; no pinned managed memory is retained by the native context.

## P8.7 — TextMeshPro adapter

The runtime assembly now references `Unity.TextMeshPro`. `TMP_Text` measurements use `GetPreferredValues` for max/preferred and width-dependent records, with a longest-token min-content estimate.

The measurement signature includes text, font/font material, font size/style, character/word/line spacing, alignment, overflow, rich-text state, margin, and available-width bucket.

Permanent Edit Mode coverage exercises a real `TextMeshProUGUI` component and TMP font asset.

## P8.8 — uGUI Text adapter

The retained legacy `UnityEngine.UI.Text` path uses its `TextGenerator`/generation settings to produce intrinsic and width-dependent cached records.

The signature includes text, font, size, style, line spacing, rich-text state, alignment, overflow, best-fit settings, and available width.

Permanent Edit Mode and Play Mode tests verify that text/content/font-size/style changes update Taffy geometry.

## P8.9 — Image / replaced-element adapters

`Image` and `RawImage` produce replaced-element measurements from their intrinsic sprite/texture size and aspect ratio. The native `itemIsReplaced` path is populated automatically, while `TaffyLayoutItem.forceReplacedElement` remains available for custom cases.

Permanent Edit Mode coverage verifies Image intrinsic size/aspect behavior.

## P8.10 — No managed callback during native compute

The compute boundary remains callback-free:

1. Unity resolves or reuses managed cached measurements.
2. Measurements are uploaded to native nodes.
3. Managed measurement work is complete.
4. `tu_compute_layout` is invoked.
5. Taffy consumes only its native cached measurement records during computation.

No managed delegate/function pointer is passed to the native layout engine. Existing native regression coverage (`cached_measurements_are_used_without_managed_callbacks`) remains green, and Phase 8 Play Mode coverage verifies that repeated cached axis application does not re-enter a custom managed provider.

## P8.11 — Measurement invalidation

Measurement cache invalidation is source-aware:

- text/font/size/style/property changes are represented in measurement signatures;
- TMP text-change events invalidate the affected Taffy child;
- TMP font-property changes invalidate active Taffy measurement caches;
- Unity `Font.textureRebuilt` invalidates active caches;
- `TaffyLayoutItem` validation/animation/enable/parent changes mark its group dirty;
- custom providers expose `MeasurementVersion` and can call `TaffyLayoutItem.InvalidateMeasurement()` or `TaffyLayoutGroup.InvalidateMeasurement()` explicitly.

## P8.12 — Permanent regression suite

The package now contains maintained Phase 8 Edit Mode and Play Mode tests. They are product regression tests, not disposable harnesses.

Final Unity `6000.4.3f1` results:

- Edit Mode: **14/14 passed** (4 Phase 7 + 10 Phase 8 tests).
- Play Mode: **3/3 passed** (1 Phase 7 + 2 Phase 8 tests).
- VS Code Problems: **0 diagnostics** for runtime/tests.

Coverage includes lifecycle regressions plus Flex, min/max, box model, Block/FlowRoot/Float/Clear, absolute/aspect, RTL/overflow, custom measurement caching, TMP, Text, Image/replaced elements, and runtime invalidation.

## Native and Android regression gates

The final Phase 8 project-input snapshot is:

`sha256:0eb0a1c56f841cebf48758d6af045533ab69e68e9e76b8f05611712ce282c8f4`

The managed Phase 8 changes altered the content-addressed project-input snapshot, so the Android artifact/provenance chain was deliberately rebuilt/restaged rather than leaving Phase 4/5 evidence stale.

Final checks:

- `python3 build/build.py verify-abi-final` — PASS;
- rustfmt — PASS;
- Clippy with warnings denied — PASS;
- maintained Rust tests — **44/44 passed**;
- host release build — PASS;
- cbindgen public-header drift — PASS;
- `python3 build/build.py native android-arm64` — PASS;
- `python3 build/build.py verify-phase4` — PASS;
- `python3 build/build.py stage-phase5` — PASS;
- `python3 build/build.py verify-phase5` — PASS.

The rebuilt Android ARM64 native binary remains byte-identical to the previous accepted engine binary:

`SHA-256 7bdca92aae2939e5098292294ee7f7d730d5eee1c718d87f65a3f22349338f66`

A fresh Unity `6000.4.3f1` Android ARM64 IL2CPP development APK also passed. The Player included both `TaffyUGUI.Runtime.dll` and `Unity.TextMeshPro.dll` through IL2CPP, and the APK contained `lib/arm64-v8a/libtaffy_ugui.so`. Unity modifies non-runtime ELF metadata while packaging, so the whole-file APK SHA differs, but the ELF program headers and both runtime-loaded `PT_LOAD` segments match the accepted staged native library byte-for-byte.

No physical Android device was attached for the final Phase 8 APK run. Comprehensive physical Unity Player validation remains Phase 12; the project retains earlier physical-device proof for the frozen native ABI/runtime path.

## Phase 8 exit gate

**PASS.** P8.1 through P8.12 are complete. Phase 9 may begin.

## Next authoritative task

**P9.1 — implement the serializable Grid track/unit data model.**
