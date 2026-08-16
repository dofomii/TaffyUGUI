# TaffyUGUI Production Roadmap

This document describes the remaining gated path from the current native release candidate to v1.0.

## Phase sequence

| Phase | Name | Current status |
|---|---|---|
| 0 | Rust Project and Toolchain Foundation | Complete |
| 1 | Complete Rust/Taffy Native Engine | Complete implementation |
| 2 | Production C ABI Candidate and Safety | Complete implementation |
| 3 | Native Verification and ABI RC Lock | ABI locked; canonical local compiled gate pending |
| 4 | Cross-Platform Native Builds | Active build infrastructure; artifacts pending |
| 5 | Unity-Ready Native Artifact Staging | Not started |
| 6 | Minimal Managed ABI Conformance and Final ABI v1 Freeze | Partial early P/Invoke scaffolding only |
| 7 | Minimal Working Unity uGUI Product | Prototype scaffolding exists; gated/not started |
| 8 | Production Flex/Block/Float/Measurement Unity Integration | Not started |
| 9 | Complete Grid and Calc Unity Authoring | Native support exists; Unity authoring not started |
| 10 | Responsive and Integration Hardening | Not started |
| 11 | Editor Tooling and Migration | Not started |
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

## Phase 6 — Minimal Managed ABI Conformance and Final ABI v1 Freeze

Early work already present: low-level `TaffyNative` declarations target `tu_*` ABI-v1-RC and structure-size guards exist.

Still required:

- execute managed/native handshake against the staged real libraries;
- validate every managed ABI structure/enum/signature needed by Unity;
- test context/node/style/topology/measurement/compute/result round trips;
- validate platform library naming and P/Invoke loading rules;
- resolve every ABI discrepancy;
- freeze final ABI v1 only after the managed proof;
- rebuild and re-stage **all** native artifacts from the final ABI v1 source.

User-facing Unity feature development remains gated until this final native payload gate is complete.

## Phase 7 — Minimal Working Unity uGUI Product

The repository already contains prototype `TaffyLayoutGroup` and `TaffyLayoutItem` components. They do **not** constitute Phase 7 completion.

Required production work includes:

- persistent native context lifecycle in Unity;
- correct root/child node synchronization;
- LayoutGroup lifecycle integration;
- own min/preferred/flexible size reporting through `SetLayoutInputForAxis`;
- nested Taffy groups;
- LayoutElement compatibility;
- deterministic RectTransform application;
- Edit Mode and Play Mode verification.

## Phase 8 — Production Flex/Block/Float/Measurement Unity Integration

- expose full production Flex authoring;
- Block/FlowRoot/Float authoring and behavior;
- complete box-model fields;
- positioning, overflow, direction, box-sizing, aspect-ratio integration;
- managed cached measurement pipeline;
- TextMeshPro adapter;
- Unity Text/Image/replaced-element adapters as appropriate;
- no managed callbacks from inside native layout.

## Phase 9 — Complete Grid and Calc Unity Authoring

The native engine already supports the required Grid/Calc model. This phase exposes it safely in Unity:

- Grid track authoring;
- repeat/auto-fill/auto-fit;
- minmax/fr/content sizing;
- implicit tracks and auto-flow;
- named lines/spans;
- named template areas;
- Grid item placement;
- typed Calc authoring/resources;
- editor-friendly validation and diagnostics.

## Phase 10 — Responsive and Integration Hardening

- responsive profiles/breakpoint overrides;
- CanvasScaler behavior;
- safe area integration;
- ScrollRect bridge/content sizing;
- ContentSizeFitter interaction rules;
- AspectRatioFitter interaction rules;
- animation-driven invalidation;
- pixel rounding;
- rebuild-loop protection.

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
