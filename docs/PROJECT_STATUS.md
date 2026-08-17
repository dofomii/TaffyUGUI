# TaffyUGUI Project Status

**Status date:** 2026-08-17
**Canonical workflow:** local development, local build, local verification
**Active release scope:** Android ARM64 only
**Native ABI:** final ABI v1 (`version=1`, `stage=2`) on exact Taffy `0.13.0`

## Current state

- Phase 0 foundation: **complete**.
- Phase 1 native engine implementation: **complete**.
- Phase 2 production C ABI implementation: **complete**.
- Phase 3 native verification: **complete**.
- Phase 4 Android ARM64 native artifact: **complete at final ABI v1 `1/2`**.
- Phase 5 Android Unity native payload: **complete at final ABI v1 `1/2`**.
- Phase 6 managed ABI conformance/final freeze: **complete**.
- Phase 7 minimal Unity uGUI product: **complete**.
- Phase 8 production Flex/Block/Float/measurement integration: **complete**.
- Phase 9 Grid/Calc Unity authoring: **active; P9.1 is next**.
- Phases 10–14: **not started**.

## Phase 8 production boundary

The Unity bridge now provides the Phase 7 persistent lifecycle plus production Phase 8 authoring for:

- size/min/max, margin, padding, border, and box sizing;
- Flex container and Flex item fields;
- Block and FlowRoot formatting contexts;
- float/clear;
- relative/absolute positioning and insets;
- overflow, scrollbar width, writing direction, and aspect ratio;
- text alignment and replaced/table flags used by the native engine.

`TaffyMeasurement.cs` provides callback-free managed cached measurement for:

- custom `ITaffyMeasurementProvider` implementations;
- TextMeshPro (`TMP_Text`);
- retained uGUI `Text`;
- `Image`;
- `RawImage`.

Measurements are resolved/cached on the managed side and uploaded before `tu_compute_layout`; no managed callback/delegate is reachable from the native Taffy compute pass. Per-node caches support both intrinsic/unbounded and finite-width records.

Measurement invalidation covers source signatures, TMP text/font events, Unity font texture rebuilds, component validation/animation lifecycle changes, and explicit custom-provider invalidation/versioning.

Grid/Calc Unity authoring remains Phase 9. ScrollRect/responsive hardening remains Phase 10. Editor tooling and migration remain Phase 11.

## Phase 8 verification

Final local verification on Unity `6000.4.3f1`:

- VS Code Problems: **0 diagnostics** for runtime/tests;
- final ABI/native regression gate: **PASS**, including all 44 maintained Rust tests, rustfmt, Clippy, release build, and cbindgen drift check;
- Edit Mode: **14/14 tests passed**;
- Play Mode: **3/3 tests passed**;
- Android ARM64 native build/Phase 4 acceptance: **PASS**;
- Phase 5 staging/verification: **PASS**;
- Android ARM64 IL2CPP development APK build: **PASS**;
- IL2CPP includes `TaffyUGUI.Runtime.dll` and `Unity.TextMeshPro.dll`;
- APK contains `lib/arm64-v8a/libtaffy_ugui.so`;
- packaged Taffy library has identical ELF program headers and byte-identical runtime-loaded `PT_LOAD` segments to the accepted staged library.

No Android device was attached during the final Phase 8 validation, so the new APK was not executed on hardware. Comprehensive Unity Player/device validation remains Phase 12. Earlier physical-device proof for the final native ABI/runtime path remains valid.

## Current Android artifact identity

Because the content-addressed release input set includes `UnityPackage/Runtime`, the Phase 8 managed changes intentionally refreshed the Phase 3/4/5 provenance chain rather than leaving previous evidence stale.

Current accepted source snapshot:

`sha256:0eb0a1c56f841cebf48758d6af045533ab69e68e9e76b8f05611712ce282c8f4`

Android ARM64 library SHA-256 remains unchanged:

`7bdca92aae2939e5098292294ee7f7d730d5eee1c718d87f65a3f22349338f66`

The historical Phase 6 freeze snapshot was `sha256:68fb502c6bc48c83b2239f5212d98fd6a7f3f777c587cb286876121c58752731`; Phase 8 did not change the native ABI or engine bytes.

Disposable validation harness/probe material remains local-only under ignored `.build/` paths and is never tracked project source.

## Next authoritative work

**Phase 9 P9.1 — implement the serializable Grid track/unit data model.**

Windows, macOS, iOS, and WebGL remain deferred outside the active Android ARM64 release scope.
