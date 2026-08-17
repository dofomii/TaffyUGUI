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
- Phase 8 production Flex/Block/Float/measurement integration: **active; P8.1 is next**.
- Phases 9–14: **not started**.

## Phase 7 product boundary

The minimal production Unity bridge now provides:

- one persistent native context/root per enabled `TaffyLayoutGroup`;
- stable `RectTransform`→native-node mapping;
- incremental node/style/topology synchronization rather than context rebuilds;
- native min/preferred measurement passes integrated with `CalculateLayoutInputHorizontal/Vertical`;
- min/preferred/flexible reporting through `SetLayoutInputForAxis`;
- cached native arrangement shared by `SetLayoutHorizontal/Vertical`;
- bulk native layout retrieval and uGUI `SetChildAlongAxis` geometry application;
- nested Taffy layout groups as independent layout owners;
- uGUI `LayoutElement` sizing and `ignoreLayout` semantics;
- maintained Edit Mode and Play Mode package regression tests.

Full production authoring, Block/Float behavior, native measurement adapters, TMP/Text/Image measurement, Grid/Calc Unity authoring, responsive integration, and editor tooling remain later phases.

## Phase 7 verification

Final local verification on Unity `6000.4.3f1`:

- VS Code Problems: **0 diagnostics** for Phase 7 runtime/tests;
- native `quality` gate: **PASS**, including 44/44 maintained Rust tests;
- Edit Mode: **4/4 tests passed**;
- Play Mode: **1/1 test passed**;
- Android ARM64 IL2CPP development APK build: **PASS**;
- packaged `libtaffy_ugui.so` has identical ELF program headers and byte-identical runtime-loaded `PT_LOAD` segments to the accepted Phase 6 Android payload.

No Android device was available during the final Phase 7 run, so that final APK was not executed on hardware. Physical platform validation remains a Phase 12 responsibility; Phase 6 retains the earlier physical-device proof for the frozen ABI/native payload.

## Phase 6 release identity

The accepted Phase 6 native release remains:

- ABI: `1/2`;
- source snapshot: `sha256:68fb502c6bc48c83b2239f5212d98fd6a7f3f777c587cb286876121c58752731`;
- Android ARM64 library SHA-256: `7bdca92aae2939e5098292294ee7f7d730d5eee1c718d87f65a3f22349338f66`.

Phase 7 changes are managed Unity integration layered on that frozen native payload; they do not redefine the historical Phase 6 artifact identity.

Disposable validation harness/probe material remains local-only under ignored `.build/` paths and is never tracked project source.

## Next authoritative work

**Phase 8 P8.1 — complete Unity authoring for core size/min/max/box model.**

Windows, macOS, iOS, and WebGL remain deferred outside the active Android ARM64 release scope.
