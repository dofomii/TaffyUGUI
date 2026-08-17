# Phase 10 — Responsive and Integration Hardening

**Status:** COMPLETE
**Date:** 2026-08-17
**Active release scope:** Android ARM64 only
**Native ABI:** final ABI v1 (`version=1`, `stage=2`), exact Taffy `0.13.0`

## Outcome

Phase 10 hardens the Phase 7–9 Unity bridge around real uGUI integration pressure: responsive breakpoints, Canvas scaling, screen safe areas, ScrollRect content sizing, fitter ownership, animation-driven changes, deterministic pixel rounding, rebuild-loop protection, and a small runtime override surface.

The native ABI and Rust/Taffy engine did not need to change. Phase 10 remains a managed Unity integration layer over the frozen ABI v1 `1/2`.

## P10.1 — Responsive profile / breakpoint system

`TaffyResponsiveProfile` is a serializable managed profile with width/height breakpoint bounds, priority, and opt-in overrides for:

- container display;
- Flex direction and wrapping;
- horizontal/vertical gaps;
- justify/align settings;
- Grid auto flow;
- container padding.

Breakpoint selection uses the current `RectTransform` size. The highest-priority matching profile wins; equal-priority profiles retain serialized list order. A runtime-forced profile can temporarily bypass automatic matching without mutating serialized data.

`ValidateResponsiveProfiles` rejects empty/duplicate names, non-finite or negative bounds, and bounded maxima smaller than their corresponding minima.

## P10.2 — Intrinsic resize and CanvasScaler responsiveness

The group observes its effective rectangle and root Canvas scale. Changes invalidate the arranged layout and request a uGUI rebuild without recreating the native context.

Responsive profile selection is resolved from the current root rectangle on every synchronized style pass, so CanvasScaler-driven or parent-driven size changes naturally move between profiles.

For ScrollRect content, intrinsic preferred measurement is constrained on the non-scrolling viewport axis. This allows text/wrapped content to compute a useful scrolling-axis preferred extent instead of always measuring from an unbounded two-axis pass.

The Play Mode gate verifies responsive profile switching across frames and exercises the Canvas-scale observation path. Unity batch mode does not consistently apply `CanvasScaler.scaleFactor` to `Canvas.scaleFactor`, so the test verifies the scaler property and then drives the effective Canvas scale directly before invoking the same production observation path.

## P10.3 — Safe-area integration

`TaffySafeAreaMode.Padding` maps `Screen.safeArea` into local Taffy root padding. Safe-area insets are additive with serialized or responsive-profile padding and do not mutate the serialized `RectOffset`.

A runtime safe-area override is provided for deterministic testing and host/platform bridges. It can be changed or cleared at runtime and invalidates layout without recreating the native context.

## P10.4 — ScrollRect content / viewport bridge

`TaffyScrollRectContentMode.AutoExpandContent` is enabled by default and activates only when the Taffy group `RectTransform` is the owning `ScrollRect.content`.

For enabled scrolling axes, the content dimension becomes:

`max(viewport size, Taffy preferred size)`

The bridge changes only the content `RectTransform`; child geometry continues to come from the same native Taffy layout pass. A self-sizing guard prevents the resulting `RectTransform` callback from immediately scheduling a second rebuild loop.

## P10.5 — ContentSizeFitter interaction rules

A `ContentSizeFitter` with a constrained axis owns that axis. The Taffy ScrollRect bridge yields rather than fighting it.

`GetIntegrationWarnings()` reports this ownership when both systems are configured on the same ScrollRect content. The rule is deterministic and avoids alternating size ownership between two uGUI controllers.

## P10.6 — AspectRatioFitter interaction rules

For child items, an enabled `AspectRatioFitter` supplies its `aspectRatio` to the native Taffy style when `TaffyLayoutItem.aspectRatio` is not explicitly set.

`WidthControlsHeight` and `HeightControlsWidth` therefore cooperate with Taffy geometry through the same native aspect-ratio field.

`FitInParent` and `EnvelopeParent` are diagnosed because they mutate anchors/size after layout and can conflict with Taffy-owned geometry. When an `AspectRatioFitter` controls the Taffy ScrollRect content itself, automatic ScrollRect content expansion yields to the fitter.

## P10.7 — Animation-driven dirty invalidation

`TaffyLayoutGroup.OnDidApplyAnimationProperties` continues to route animated group-property changes through `SetLayoutDirty`. `TaffyLayoutItem` already has the equivalent invalidation hook.

Permanent Play Mode coverage changes an animatable layout field, invokes Unity's animation-property callback path, advances a frame, and verifies geometry recomputes.

The regression test intentionally does not require the legacy Unity Animation module; the package stays free of an unnecessary runtime dependency.

## P10.8 — Pixel rounding strategy

`TaffyPixelRounding` supports:

- None;
- Round;
- Floor;
- Ceil;
- CanvasPixel.

Rounding happens only when applying native geometry to `RectTransform`s. Native layout remains full-precision.

Critically, rounding is edge-based: the start edge and end edge are rounded, then size is derived from their difference. This prevents independent position/size rounding from introducing gaps or overlaps across adjacent children.

`CanvasPixel` converts through the root Canvas scale factor before rounding.

## P10.9 — Layout rebuild-loop protection

`SetLayoutDirty` now has two protections:

1. dirty requests received while a Taffy layout is being applied are coalesced into one deferred request after the apply pass;
2. repeated same-frame rebuild requests are capped by `maxRebuildRequestsPerFrame` (minimum 1).

Excess requests are suppressed and counted in `SuppressedRebuildRequestCount`; `ResetRebuildDiagnostics()` resets the diagnostic counter.

The ScrollRect self-sizing path additionally suppresses its own dimension-change callback from scheduling another rebuild.

## P10.10 — Runtime override API

The production runtime API includes:

- `SetRuntimeResponsiveProfile(name, out error)`;
- `ClearRuntimeResponsiveProfile()`;
- `SetRuntimeSafeAreaInsets(...)`;
- `ClearRuntimeSafeAreaInsets()`;
- `ClearRuntimeOverrides()`;
- `ActiveResponsiveProfileName`;
- `RuntimeResponsiveProfileOverride`;
- `GetIntegrationWarnings()`;
- rebuild suppression diagnostics.

Overrides change effective runtime behavior but never overwrite serialized responsive profiles, padding, or safe-area configuration.

## Permanent Phase 10 regression suite

Unity `6000.4.3f1` final package tests:

- Edit Mode: **29/29 passed** — 22 Phase 7–9 tests plus 7 Phase 10 tests;
- Play Mode: **9/9 passed** — 5 Phase 7–9 tests plus 4 Phase 10 tests.

Phase 10 coverage includes:

- serializable responsive profile data;
- breakpoint matching, priority, resize switching, and runtime forcing;
- invalid breakpoint/profile diagnostics;
- additive safe-area padding and runtime overrides;
- ScrollRect content expansion and dynamic child changes;
- ContentSizeFitter axis ownership diagnostics;
- AspectRatioFitter native aspect handoff and unsafe-mode diagnostics;
- edge-based pixel rounding;
- Canvas-scale observation;
- animation-property invalidation;
- rebuild request suppression and loop protection.

VS Code Problems is clean for runtime/tests.

## Native and Android regression

Native engine/ABI code is unchanged by Phase 10. The maintained native quality gate remains green with rustfmt, Clippy `-D warnings`, **44/44 Rust tests**, and release build.

Final content-addressed Phase 10 source snapshot:

`sha256:3228f12128c07fd6c470a7bc9119a4ba810f7718d98c6ae9537086030beaa0fc`

Android ARM64 library SHA-256 remains:

`7bdca92aae2939e5098292294ee7f7d730d5eee1c718d87f65a3f22349338f66`

Final Phase 4/5 provenance and a fresh Unity Android ARM64 IL2CPP APK are verified against the exact final source snapshot during Phase 10 closure. Physical execution also passes on `CPH2723` (Android 16 / API 36, ARM64-v8a): Android loads `libtaffy_ugui.so`, the Player emits `TAFFY_PHASE10_DEVICE_PASS:profile=phone:height=328.0:suppressed=4`, remains alive, and targeted Unity/AndroidRuntime/native-fatal log scans are empty. The 328 px result is the expected three 100 px items + two 8 px responsive gaps + 12 px vertical safe-area padding.

## Phase 10 exit gate

Phase 10 closes only when P10.1–P10.10 are complete, all Unity regression tests pass, final ABI/Phase 4/Phase 5 provenance passes on the exact final source snapshot, a fresh Android ARM64 IL2CPP package build succeeds, and temporary validation material is removed.

## Next authoritative task

**Phase 11 P11.1 — implement the `TaffyLayoutGroup` custom inspector.**
