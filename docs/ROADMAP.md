# TaffyUGUI Production Roadmap

This document describes the gated production path from the current completed Phase 10 responsive/integration hardening to v1.0.

## Phase sequence

| Phase | Name | Current status |
|---|---|---|
| 0 | Rust Project and Toolchain Foundation | Complete |
| 1 | Complete Rust/Taffy Native Engine | Complete |
| 2 | Production C ABI Candidate and Safety | Complete |
| 3 | Native Verification and ABI RC Lock | Complete |
| 4 | Native Builds | Complete for active Android ARM64 scope |
| 5 | Unity-Ready Native Artifact Staging | Complete for Android ARM64 |
| 6 | Managed ABI Conformance and Final ABI v1 Freeze | Complete; ABI v1 `1/2` |
| 7 | Minimal Working Unity uGUI Product | Complete |
| 8 | Production Flex/Block/Float/Measurement Unity Integration | Complete |
| 9 | Complete Grid and Calc Unity Authoring | Complete |
| 10 | Responsive and Integration Hardening | Complete |
| 11 | Editor Tooling and Migration | Active; P11.1 next |
| 12 | Real Unity Platform Validation | Not started |
| 13 | Performance and Reliability Hardening | Not started |
| 14 | v1.0 Release | Not started |

## Phase 5 — Unity-Ready Native Artifact Staging

After `verify-phase4` accepts the complete native target set:

- copy verified native artifacts into canonical `UnityPackage/Plugins/**` locations;
- create/verify Unity `.meta` plugin importer configuration;
- retain artifact manifests/checksums/source fingerprints;
- verify Git/UPM packaging includes the native payload;
- keep `dist/**` generated while committing the required verified package-native payload.

## Phase 6 — Managed ABI Conformance and Final ABI v1 Freeze

**Complete.** The full managed/native contract was validated and final ABI v1 was frozen at version `1`, stage `2`. The accepted Android ARM64 native payload was rebuilt/restaged from the final source and verified through Unity IL2CPP packaging plus earlier physical-device execution.

See [PHASE6_MANAGED_ABI.md](PHASE6_MANAGED_ABI.md).

## Phase 7 — Minimal Working Unity uGUI Product

**Complete.** The minimal production Unity bridge now includes:

- persistent native context/root lifecycle;
- stable `RectTransform`→native-node mapping;
- incremental style/topology synchronization;
- uGUI min/preferred layout-input reporting;
- cached horizontal/vertical arrangement from native geometry;
- nested Taffy groups;
- `LayoutElement` and `ignoreLayout` compatibility;
- permanent Edit Mode and Play Mode regression tests.

The Phase 7 gate passed 4/4 Edit Mode and 1/1 Play Mode tests on Unity `6000.4.3f1`, plus an Android ARM64 IL2CPP build check. See [PHASE7_MINIMAL_UGUI.md](PHASE7_MINIMAL_UGUI.md).

## Phase 8 — Production Flex/Block/Float/Measurement Unity Integration

**Complete.** The production Unity bridge now includes:

- full Phase 8 Flex container/item authoring;
- Block/FlowRoot/Float/Clear authoring and behavior;
- core size/min/max/margin/padding/border and box-sizing fields;
- positioning, insets, overflow, direction, and aspect-ratio integration;
- per-node multi-signature managed measurement caching;
- TextMeshPro, uGUI Text, Image/RawImage, and custom-provider adapters;
- source-aware measurement invalidation;
- a callback-free native Taffy compute boundary;
- maintained Edit Mode and Play Mode regression coverage.

The Phase 8 gate passed 14/14 Edit Mode and 3/3 Play Mode tests on Unity `6000.4.3f1`, refreshed the Phase 4/5 Android provenance for the current content-addressed source snapshot, and passed a fresh Android ARM64 IL2CPP build. See [PHASE8_FLEX_BLOCK_MEASUREMENT.md](PHASE8_FLEX_BLOCK_MEASUREMENT.md).

## Phase 9 — Complete Grid and Calc Unity Authoring

**Complete.** The final ABI already contained the required Grid/Calc engine, so Phase 9 added the managed Unity product surface without expanding the ABI:

- serializable explicit and implicit Grid tracks;
- point/percent/fr/minmax/min-content/max-content/Calc sizing;
- Repeat count, auto-fill, and auto-fit;
- named lines/spans and named template areas;
- grid-auto-flow;
- Grid item placement plus justify-items/justify-self alignment;
- serializable typed Calc expression trees and per-context resource caching/lifecycle;
- pre-native validation and detailed Grid diagnostics;
- scoped UTF-8/array ABI marshalling with no retained managed pointers;
- maintained Edit Mode and Play Mode regression coverage.

The Phase 9 gate passed 22/22 Edit Mode and 5/5 Play Mode tests on Unity `6000.4.3f1`. See [PHASE9_GRID_CALC_UNITY.md](PHASE9_GRID_CALC_UNITY.md).

## Phase 10 — Responsive and Integration Hardening

**Complete.** The managed Unity integration now includes:

- serializable responsive profiles with width/height breakpoints, priority, and runtime forcing;
- rect/Canvas-scale responsive invalidation;
- additive safe-area padding and runtime safe-area overrides;
- ScrollRect content/viewport sizing using Taffy preferred size;
- deterministic ContentSizeFitter and AspectRatioFitter ownership rules;
- animation-property invalidation;
- edge-based Round/Floor/Ceil/CanvasPixel geometry application;
- re-entrant and same-frame rebuild-loop protection;
- runtime integration diagnostics and override APIs.

The Phase 10 gate passed 29/29 Edit Mode and 9/9 Play Mode tests on Unity `6000.4.3f1`. See [PHASE10_RESPONSIVE_INTEGRATION.md](PHASE10_RESPONSIVE_INTEGRATION.md).

## Phase 11 — Editor Tooling and Migration

- custom inspectors/property drawers;
- Grid authoring UI;
- scene visualization/debugging;
- diagnostics window;
- migration wizard from HorizontalLayoutGroup/VerticalLayoutGroup/GridLayoutGroup;
- prefab/Undo/serialization-safe migration;
- documentation and samples for editor workflows.

## Phase 12 — Real Unity Platform Validation

Run real Unity Editor/player builds using the staged native payload:

- Unity 2021.3 LTS primary baseline;
- selected Unity 2022.3 LTS validation;
- selected Unity 6.0 validation;
- Windows x64;
- macOS Intel/Apple Silicon;
- Android ARM64;
- iOS ARM64;
- WebGL;
- Linux if retained as an advertised platform;
- Edit Mode / Play Mode / player regression scenes.

No target is advertised solely because a native binary compiled.

## Phase 13 — Performance and Reliability Hardening

- allocation profiling;
- dirty propagation profiling;
- bulk-transfer performance;
- 100/1,000/10,000-node benchmarks;
- repeated domain reload/context lifecycle testing;
- long-running stress tests;
- memory/resource leak checks;
- error-path/panic containment review;
- package size and startup/loading checks.

## Phase 14 — v1.0 Release

- final compatibility matrix;
- release notes/changelog;
- third-party license audit/notices;
- complete user documentation;
- samples;
- troubleshooting guide;
- final package validation;
- Git/UPM installation validation;
- version/tag/release packaging;
- publish only platforms that passed Phase 12.
