# TaffyUGUI — End-to-End Development Plan

**Status:** Active development plan  
**Repository:** `dofomii/TaffyUGUI`  
**Package:** `com.dofomii.taffyugui`  
**Target:** Production-quality responsive layout for existing Unity uGUI using Rust + Taffy  
**Primary rule:** Unity owns rendering and interaction; Taffy owns geometry only.

---

## 1. Final Product Outcome

The final TaffyUGUI package will let a Unity developer take an existing uGUI hierarchy and replace Unity's layout calculation layer without replacing the rest of the UI.

The desired end-user workflow is:

```text
Existing Canvas / prefab
    |
Existing panel using HorizontalLayoutGroup / VerticalLayoutGroup / GridLayoutGroup
    |
Replace or migrate that layout component to TaffyLayoutGroup
    |
Keep existing Button / Image / TMP / ScrollRect / Animator / EventSystem / scripts
    |
Configure Flexbox / Grid / Block / responsive sizing in the Inspector
    |
Taffy computes x / y / width / height
    |
Unity applies the result to RectTransform
    |
Unity renders and handles input exactly as before
```

The finished package must support the following without requiring UI Toolkit or recreation of old prefabs:

- Flexbox layout.
- CSS Grid layout.
- Block layout where it maps cleanly to uGUI.
- Row/column direction and wrapping.
- Percentage sizing.
- Auto/intrinsic sizing.
- Min/max sizing.
- Margin, padding and gap.
- Flex grow/shrink/basis.
- Relative and absolute positioning.
- Aspect ratio.
- Grid templates, tracks, placement and auto flow.
- Alignment and justification.
- Existing `LayoutElement` data.
- Unity `Text` intrinsic measurement.
- TextMeshPro intrinsic/wrapped text measurement through an optional adapter assembly.
- Image/RawImage intrinsic measurement.
- Nested Taffy layout groups.
- `ScrollRect` integration.
- Canvas Scaler-compatible logical sizing.
- Safe-area support.
- Responsive/container breakpoint extensions.
- Edit Mode and Prefab Mode preview.
- Runtime style changes.
- Migration tools for existing uGUI layout groups.
- Diagnostics and conflict detection.
- Windows, macOS, Android, iOS and Unity Web/WebGL native builds.
- Automated testing and reproducible release packaging.

TaffyUGUI will **not** render UI, replace the Canvas renderer, replace EventSystem, replace TMP, implement fonts, implement clipping, implement scrollbars, replace animation, or require a new UI hierarchy.

---

## 2. Current Verified Baseline

### 2.1 Taffy version

The helper specification referenced Taffy `0.13.0`, but the currently published Taffy documentation exposes `0.12.2` as the latest release. The repository is already correctly pinned to:

```toml
taffy = { version = "=0.12.2", ... }
```

Development will remain on the exact pinned release until a newer published release is deliberately evaluated. Upgrading Taffy will be a controlled task with ABI/style regression tests; it will never be done by changing to a floating branch.

Current native feature baseline:

```text
std
taffy_tree
flexbox
grid
block_layout
content_size
```

Features not verified in the currently pinned release will not be promised as v1 features merely because they are mentioned by a future or draft Taffy API.

### 2.2 Unity compatibility

The current UPM manifest declares:

```json
"unity": "2021.3"
```

The architecture will avoid newer APIs where possible so we can attempt a Unity 2019.4 LTS compatibility lane later. We will **not lower the manifest minimum to 2019.4 until the complete Runtime assembly and core Edit Mode tests actually compile and pass there**.

Planned compatibility validation tiers:

```text
Tier A: Unity 2021.3 LTS — primary initial baseline
Tier B: Unity 2022.3 LTS — continuous compatibility
Tier C: Unity 6 current LTS/release — forward compatibility
Tier D: Unity 2019.4 LTS — backward-compatibility validation target
```

No UI Toolkit dependency will be introduced.

### 2.3 Existing repository baseline

The repository already contains:

```text
native/               Rust native wrapper foundation
UnityPackage/         UPM package foundation
docs/                 architecture documentation
scripts/              native build helpers
.github/workflows/     CI foundation
```

The existing `UnityPackage/` boundary will be retained. This keeps native source/build infrastructure outside the installable Unity package and allows Git-based UPM installation using a package subdirectory.

---

## 3. Non-Negotiable Architecture Rules

These rules must remain true through development unless a measured compatibility problem proves a change is necessary.

1. **Unity owns rendering and interaction.**
2. **Taffy owns geometry only.**
3. `TaffyLayoutGroup` derives from `UnityEngine.UI.LayoutGroup`.
4. `TaffyLayoutItem` is optional for simple children.
5. Existing `LayoutElement` values are reused where possible.
6. Child `RectTransform`s are controlled through Unity layout APIs, not a parallel rendering system.
7. The Rust/Taffy tree is persistent and dirty-driven.
8. The managed public API does not expose Rust/Taffy internal types.
9. The native C ABI does not expose Rust pointers, `Vec`, `String`, Rust enums or Taffy node IDs.
10. Native calls are batched for steady-state layout.
11. Rust does not callback into C# once per text node during layout.
12. Text/image measurement is gathered in managed code and supplied to the native tree.
13. Layout is not recomputed from `Update()` every frame.
14. A live Taffy context is main-thread-owned in v1.
15. Editor native resources have explicit lifetime management.
16. Platform binaries are produced by CI and packaged automatically.
17. Unity Web/WebGL is treated as its own Emscripten integration target.
18. Migration tools are non-destructive and Undo-aware.
19. No feature is marked production-ready until it has automated tests and at least one Unity integration validation case.

---

## 4. Target Repository Structure

The current repository will evolve toward the following structure while keeping `UnityPackage/` as the installable package root:

```text
TaffyUGUI/
├── README.md
├── LICENSE
├── CHANGELOG.md
├── CONTRIBUTING.md
├── SECURITY.md
├── THIRD_PARTY_NOTICES.md
│
├── native/
│   ├── Cargo.toml
│   ├── Cargo.lock
│   └── src/
│       ├── lib.rs
│       ├── ffi.rs
│       ├── context.rs
│       ├── handles.rs
│       ├── style.rs
│       ├── grid.rs
│       ├── measurement.rs
│       ├── error.rs
│       └── version.rs
│
├── UnityPackage/
│   ├── package.json
│   ├── CHANGELOG.md
│   ├── LICENSE.md
│   ├── Third Party Notices.md
│   │
│   ├── Runtime/
│   │   ├── TaffyUGUI.Runtime.asmdef
│   │   ├── Core/
│   │   ├── Styles/
│   │   ├── Measurement/
│   │   ├── Native/
│   │   ├── Responsive/
│   │   ├── Integration/
│   │   └── Diagnostics/
│   │
│   ├── Runtime.TMP/
│   │   └── TaffyUGUI.TMP.asmdef
│   │
│   ├── Editor/
│   │   ├── TaffyUGUI.Editor.asmdef
│   │   ├── Inspectors/
│   │   ├── Drawers/
│   │   ├── Windows/
│   │   ├── SceneView/
│   │   ├── Migration/
│   │   ├── Validation/
│   │   └── Build/
│   │
│   ├── Plugins/
│   │   ├── Windows/
│   │   ├── macOS/
│   │   ├── Android/
│   │   ├── iOS/
│   │   └── WebGL/
│   │
│   ├── Tests/
│   │   ├── Runtime/
│   │   └── Editor/
│   │
│   ├── Samples~/
│   └── Documentation~/
│
├── tests/
│   ├── native/
│   ├── golden/
│   └── abi/
│
├── scripts/
│   ├── build.py
│   ├── build-windows.ps1
│   ├── build-android.py
│   ├── build-apple.sh
│   ├── build-web.py
│   └── package.py
│
├── docs/
│   ├── ARCHITECTURE.md
│   ├── DEVELOPMENT_PLAN.md
│   ├── ABI.md
│   ├── PLATFORM_SUPPORT.md
│   └── PERFORMANCE.md
│
└── .github/workflows/
```

The structure may be introduced gradually; package behavior takes priority over cosmetic reorganization.

---

# 5. Development Phases

Each phase has a specific deliverable and an exit gate. Work should not expand into the next major feature until the current vertical slice is stable.

---

## Phase 0 — Repository and Contract Hardening

### Goal

Turn the bootstrap repository into a development base whose public contracts, versioning and package boundaries are explicit.

### Work

- Normalize the current native source into modules instead of keeping all FFI behavior in one file.
- Add `Cargo.lock` and make dependency pinning mandatory.
- Define package SemVer and native ABI version as independent numbers.
- Add:
  - `CHANGELOG.md`
  - `SECURITY.md`
  - `THIRD_PARTY_NOTICES.md`
  - package-side license/notice files.
- Document supported/experimental platform status.
- Add a clear API stability policy:
  - `0.x`: public API can evolve, but intentional breaking changes are documented.
  - `1.x`: managed API and ABI compatibility follow SemVer/ABI rules.
- Introduce central constants for:
  - managed package version
  - native build version
  - ABI version
  - Taffy version
  - native capability flags.
- Ensure the AI-generated-code disclaimer remains visible in the README and does not replace the legal MIT license disclaimer.

### Deliverable

A repository where a managed/native mismatch can be diagnosed before any layout operation is attempted.

### Exit criteria

- Native library exports its ABI/Taffy/build version.
- C# loader validates ABI before creating layout state.
- Third-party dependency/version is documented.
- CI builds the native host library and runs native tests.

---

## Phase 1 — Stable Native C ABI and Context Lifetime

### Goal

Create the production native boundary before adding more Unity-facing features.

### Native API

The first stable family of functions will cover:

```text
tu_get_abi_version()
tu_get_taffy_version(...)
tu_get_build_version(...)
tu_get_capabilities()

tu_context_create()
tu_context_destroy()
tu_context_clear()

tu_node_create()
tu_nodes_create_bulk()
tu_node_remove()
tu_nodes_remove_bulk()

tu_node_set_children()
tu_nodes_set_children_bulk()

tu_node_set_style()
tu_nodes_set_styles_bulk()

tu_node_set_measurement()
tu_nodes_set_measurements_bulk()

tu_node_mark_dirty()

tu_compute_layout()
tu_get_layout()
tu_get_layouts_bulk()

tu_get_last_error_code()
tu_get_last_error_message(...)
```

Names may be adjusted once, before the ABI is declared stable, but the responsibilities will remain.

### Handle model

- Contexts and nodes use opaque 64-bit handles.
- Native handles are generation-aware so stale handles are rejected.
- No raw Rust pointer becomes a public C# value.
- Removing a node invalidates its generation.
- Destroying a context invalidates all nodes owned by it.

### Error model

Define numeric errors such as:

```text
TU_OK
TU_INVALID_CONTEXT
TU_INVALID_NODE
TU_INVALID_ARGUMENT
TU_ABI_MISMATCH
TU_TAFFY_ERROR
TU_UNSUPPORTED
TU_OUT_OF_MEMORY
TU_PANIC
```

FFI functions must validate NaN/invalid enum/range inputs before translating them into Taffy types.

### Panic safety

No Rust unwind may cross FFI. Release behavior will use explicit validation and an agreed panic strategy. Error context will include the failing operation and enough node/context information for diagnostics.

### C# wrapper

Create:

```text
TaffyNative
TaffyNativeTypes
TaffyNativeContext : IDisposable
TaffyNativeException
TaffyNativeVersion
TaffyNativeCapabilities
```

`TaffyNative` remains internal P/Invoke only. User code never invokes native functions directly.

### Exit criteria

- Create/destroy 10,000 contexts/nodes in native tests without invalid access.
- Stale node handles are rejected.
- ABI mismatch produces a clear managed exception/error before layout begins.
- Bulk result retrieval works for a multi-node tree.
- Context lifetime is deterministic.

---

## Phase 2 — Unity Flexbox Vertical Slice

### Goal

Prove the complete path from an existing uGUI hierarchy to Taffy and back to `RectTransform`.

### Core components

Implement/refactor:

```text
TaffyLayoutGroup : LayoutGroup
TaffyLayoutItem
TaffyLayoutContext
TaffyLayoutNode
TaffyLayoutScheduler
TaffyLayoutResult
TaffyDirtyFlags
```

### Initial property set

Parent/group:

```text
Display = Flex
FlexDirection = Row / Column / reverse variants
FlexWrap = NoWrap / Wrap / WrapReverse
Padding
Gap X / Gap Y
AlignItems
AlignContent
JustifyContent
PixelRounding
```

Child/item:

```text
Width / Height
MinWidth / MinHeight
MaxWidth / MaxHeight
FlexBasis
FlexGrow
FlexShrink
AlignSelf
```

Units:

```text
Auto
Point/Unit
Percent
```

### Unity lifecycle implementation

`TaffyLayoutGroup` will integrate with the uGUI Auto Layout lifecycle:

```text
CalculateLayoutInputHorizontal()
SetLayoutHorizontal()
CalculateLayoutInputVertical()
SetLayoutVertical()
```

Relevant invalidation hooks:

```text
OnEnable()
OnDisable()
OnValidate()
OnRectTransformDimensionsChange()
OnTransformChildrenChanged()
OnDidApplyAnimationProperties()
OnTransformParentChanged() where safe
OnCanvasHierarchyChanged() where safe
```

### RectTransform application

Results are applied using `LayoutGroup` layout methods such as `SetChildAlongAxis(...)` rather than creating an independent positioning system.

Only values changed beyond a small epsilon are written to children.

### First validation prefab

```text
Canvas
└── Panel + TaffyLayoutGroup
    ├── Button
    ├── Image
    ├── TMP text
    └── Nested panel
```

It must resize correctly at representative logical sizes:

```text
320x568
375x812
768x1024
1366x768
1920x1080
```

### Exit criteria

- Can replace a HorizontalLayoutGroup with Flex Row without recreating children.
- Can replace a VerticalLayoutGroup with Flex Column.
- Wrap/gap/grow/shrink/min/max work.
- Nested Taffy groups work for a basic case.
- Edit Mode and Play Mode produce matching geometry.
- No layout call occurs every frame while nothing is dirty.
- No managed allocations occur in unchanged steady-state from TaffyUGUI code after warmup.

---

## Phase 3 — Dirty State, Scheduling and Rebuild Safety

### Goal

Make the vertical slice safe inside Unity's multi-stage layout system before adding text and Grid complexity.

### Dirty flags

Implement:

```text
None
Hierarchy
Style
Responsive
Measurement
AvailableSize
Transform
NativeTree
All
```

### Change detection

Cache compact state for:

- child order/participation
- Taffy style
- resolved responsive style
- LayoutElement inputs
- intrinsic measurement
- available parent size
- last applied layout rectangle.

Do not upload style/children/measurements if their version/hash did not change.

### Rebuild guards

Implement:

```text
layoutGeneration
isRebuilding
rebuildScheduled
measurementIteration
maxIterations
epsilon
```

Never recursively call `LayoutRebuilder.ForceRebuildLayoutImmediate` from inside the active layout pass.

If a width-dependent measurement changes final horizontal geometry, schedule one bounded rebuild instead of immediate recursion.

### Exit criteria

- Add/remove/reorder/enable/disable child correctly dirties topology.
- Parent size change dirties available size only.
- Scroll position change alone does not trigger layout.
- Rebuild stress tests do not recurse indefinitely.
- Unchanged layout does not issue native compute calls.

---

## Phase 4 — LayoutElement and Intrinsic Measurement

### Goal

Make existing old uGUI prefabs work without adding `TaffyLayoutItem` to every child.

### Precedence model

```text
Explicit TaffyLayoutItem override
    >
LayoutElement
    >
Intrinsic measurement
    >
Taffy default
```

### LayoutElement mapping

Use existing values where semantically valid:

```text
minWidth/minHeight       -> Taffy min size
preferredWidth/Height   -> intrinsic/preferred sizing or basis
flexibleWidth/Height    -> relevant flexibility/grow behavior
ignoreLayout             -> participation filtering
```

The exact flex interpretation of Unity's separate horizontal/vertical flexibility will be documented because Flexbox has a main/cross-axis model rather than two fully independent flex-grow axes.

### Measurement abstraction

Public/internal contracts:

```text
ITaffyMeasureProvider
TaffyMeasureInput
TaffyMeasureResult
TaffyMeasurementMode
```

Providers:

```text
TaffyLayoutElementMeasure
TaffyUnityTextMeasure
TaffyImageMeasure
TaffyCustomMeasure
```

### Images

Support Image/RawImage intrinsic size when Auto/intrinsic mode requests it. Preserve logical Canvas units; do not feed physical screen pixels into Taffy.

### Exit criteria

- Common Button/Image/LayoutElement children lay out without `TaffyLayoutItem`.
- Explicit Taffy overrides beat LayoutElement predictably.
- Inspector/diagnostics can report the source of a resolved value.

---

## Phase 5 — TextMeshPro and Width-Dependent Measurement

### Goal

Make wrapped text stable without Rust-to-C# measurement callbacks.

### Assembly boundary

TMP support lives in:

```text
UnityPackage/Runtime.TMP/TaffyUGUI.TMP.asmdef
```

The core Runtime assembly must not fail to compile in a project where the TMP adapter is absent/disabled.

### Measurement algorithm

For wrapped text:

```text
1. Resolve provisional available width.
2. Ask TMP for preferred size at that width.
3. Upload measurement to native tree.
4. Compute layout.
5. If final width changes wrapping materially, measure once more.
6. Compute final layout.
7. Stop at strict iteration limit.
```

Default maximum: **2 normal iterations**.

No unbounded measurement loop is permitted.

### Test cases

- single-line text
- wrapped paragraph
- explicit width + auto height
- percentage width + auto height
- dynamically changing text
- nested text in flex wrap
- TMP autosizing interaction documented/validated
- locale strings with very different lengths

### Exit criteria

- Wrapped TMP height is stable at responsive widths.
- Text change dirties measurement, not the entire application.
- No per-text native-to-managed callback exists.
- No recursive Unity rebuild loop occurs in pathological wrapping tests.

---

## Phase 6 — Complete Core Box and Positioning Surface

### Goal

Move from Flex MVP to the important general Taffy style surface needed by production UI.

### Add

```text
Margin
Padding
Border layout thickness (geometry only)
Box sizing
Position: relative / absolute
Inset: left/right/top/bottom
Aspect ratio
Direction
Overflow X / Y layout semantics
Scrollbar width/reservation where supported
Display = Block
```

### Unity-specific rules

- Taffy border values affect geometry only; they never draw a Unity border.
- Taffy overflow affects layout only; clipping still needs `Mask` or `RectMask2D`.
- Taffy direction does not replace TMP text shaping/direction settings.
- Absolute-positioned children remain normal Unity GameObjects and raycast targets.

### Exit criteria

- Absolute positioning golden tests match native Taffy.
- Block sample works with intrinsic children.
- Aspect-ratio sample works without `AspectRatioFitter`.
- Conflicting `AspectRatioFitter` is detected.
- Overflow-without-mask warning is available.

---

## Phase 7 — CSS Grid as a First-Class Feature

### Goal

Implement Grid only after the native ABI, measurement and Unity lifecycle are stable.

### Managed authoring types

Create serializable types such as:

```text
TaffyGridTrack
TaffyGridTrackList
TaffyGridPlacement
TaffyGridNamedArea
TaffyGridTemplate
```

Common authoring must make these cases straightforward:

```text
1fr 1fr 1fr
200 units + 1fr
auto + 1fr + auto
repeat(N, 1fr)
minmax(120 units, 1fr)
```

### Native grid ABI

Variable-length grid data will not be embedded in a monolithic fixed-size style struct. We will use dedicated bulk/template resources or compact serialized native descriptions.

No parsing of human-readable Grid strings will occur every frame.

### Features

- template columns/rows
- auto columns/rows
- auto flow
- row/column gap
- explicit placement
- implicit tracks
- alignment
- named lines/areas only after the core numeric model is stable.

### Migration target

Unity `GridLayoutGroup` can be converted to an explicit Taffy Grid template for deterministic uniform-cell cases.

### Exit criteria

- Basic Grid geometry matches native golden tests.
- Responsive card Grid works from narrow phone width to desktop width.
- Explicit and auto placement tests pass.
- Grid inside ScrollRect works.
- Every exposed Grid style has managed/native conversion tests.

---

## Phase 8 — Responsive and Runtime Override Layer

### Goal

Add optional breakpoint-style behavior on top of Taffy's intrinsic responsive capabilities.

Most responsive UI should first use:

- percentages
- wrap
- grow/shrink
- min/max
- Grid tracks
- auto placement
- gap
- aspect ratio.

Breakpoints are an extension, not the primary layout mechanism.

### Types

```text
TaffyResponsiveProfile : ScriptableObject
TaffyResponsiveRule
TaffyBreakpoint
TaffyResponsiveResolver
TaffyStyleOverride
```

### Conditions

```text
min/max container width
min/max container height
orientation
aspect ratio/range
```

Default behavior is **container-based**, so reusable panels respond to the space assigned to them rather than only global screen dimensions.

### Resolution order

```text
serialized base style
-> shared profile rules in declared order
-> local responsive rules
-> runtime overrides
```

### Runtime style API

Support setters and non-serialized overrides:

```text
item.SetWidth(...)
item.SetHeight(...)
item.SetFlexGrow(...)
item.SetDisplay(...)
item.SetGridPlacement(...)
group.MarkLayoutDirty()

item.RuntimeStyle...
item.ClearRuntimeOverrides()
```

Setters compare old/new values, set the minimum dirty flags and request the appropriate Unity rebuild automatically.

### Exit criteria

- One responsive prefab works from 320 logical units to desktop without prefab duplication.
- Runtime style changes do not mutate asset defaults unless explicitly serialized by Unity/editor operations.
- Resolved style source is inspectable.

---

## Phase 9 — ScrollRect, Safe Area, Canvas Scaling and Integration Bridges

### Goal

Make TaffyUGUI behave correctly with the uGUI systems most production projects already use.

### `TaffyScrollRectBridge`

Responsibilities:

- observe viewport size changes
- reserve scrollbar thickness when requested
- dirty layout when scrollbar visibility changes geometry
- avoid dirtying layout from ordinary content scrolling
- validate Mask/RectMask2D expectations
- preserve ScrollRect's ownership of content movement.

Test:

```text
vertical
horizontal
both axes
dynamic content
auto-hide scrollbar
nested ScrollRect
Grid content
Flex-wrap content
```

### `TaffySafeArea`

- reads `Screen.safeArea`
- converts to Canvas/local logical coordinates
- exposes safe-area insets as Taffy padding or controlled offsets
- updates only when the safe area actually changes.

### Canvas Scaler

The invariant is:

```text
device pixels
-> Canvas Scaler
-> RectTransform logical size
-> Taffy
```

Taffy never receives raw device resolution when local `RectTransform` dimensions are the real available space.

### Animation conflicts

Animator/tween changes to Taffy style properties are supported.

Animator clips that directly animate layout-owned child position/size while Taffy controls those same values will produce a warning.

### Exit criteria

- Scroll position alone causes zero Taffy recomputation.
- Canvas Scaler tests produce consistent logical layout.
- Safe area updates on orientation/resolution simulation.
- Common ownership conflicts are diagnosable.

---

## Phase 10 — Production Editor Experience

### Goal

Make the package usable without reading native code or manually calculating Taffy structs.

### Inspectors

`TaffyLayoutGroupEditor`:

- General / Box / Flex / Grid / Sizing sections
- context-sensitive properties
- native status
- node count
- last layout timing
- dirty reason
- current available size
- inline conflict warnings
- force refresh
- normalize anchors action
- copy/migrate from existing Unity layout group.

`TaffyLayoutItemEditor`:

- per-property override toggles
- resolved value and source
- Flex fields only when appropriate
- Grid fields only under Grid parent
- absolute fields only in absolute mode
- Advanced foldout
- measurement information
- responsive/runtime state information.

### Property drawers

Create compact authoring for:

```text
TaffyDimension
TaffyLength
TaffyRect
TaffyGridTrack
TaffyGridPlacement
TaffyResponsiveRule
```

### Scene visualization

Editor-only visualization for:

- content/padding/border/margin boxes
- main/cross flex axes
- wrap lines
- gaps
- Grid tracks/cells
- absolute items
- overflow boundary.

### Layout Debugger

`Window > TaffyUGUI > Layout Debugger`

Shows:

- Unity hierarchy node
- native handle
- resolved style
- measurement
- final layout rectangle
- Grid placement
- responsive rule
- dirty flags
- compute timing
- warnings/errors.

### Diagnostics window

`Window > TaffyUGUI > Diagnostics`

Checks:

```text
native plugin availability
ABI match
Taffy/native versions
platform architecture
layout conflicts
TMP adapter
measurement issues
package version
performance warnings
```

Add **Copy Diagnostic Report** for support/issues.

### Exit criteria

- Common misconfiguration is understandable from the Inspector.
- A developer can see why a child got its width/height.
- Native status and ABI mismatch are visible without opening logs.
- Editor tools do not allocate heavily on every SceneView repaint.

---

## Phase 11 — Migration Wizard for Existing uGUI

### Goal

Make the package useful for old projects without forcing prefab reconstruction.

### Entry point

```text
Tools > TaffyUGUI > Migration Wizard
```

### Analyze

- selected GameObject hierarchy
- prefab/prefab stage
- scene
- later: selected project assets.

### HorizontalLayoutGroup

Convert to:

```text
Display = Flex
Direction = Row
```

Map as closely as possible:

```text
spacing
padding
childAlignment
childControlWidth/Height
childForceExpandWidth/Height
reverseArrangement
```

### VerticalLayoutGroup

Convert to Flex Column with equivalent mappings.

### GridLayoutGroup

Convert deterministic uniform-cell layouts to explicit Taffy Grid templates. Any property without exact semantic equivalence is called out in the migration report.

### Safety rules

- full Unity Undo support
- capture source values before changing anything
- preview before apply
- analyze-only mode
- duplicate/convert-copy option
- no silent anchor rewriting
- no scene/prefab mass-save without explicit user action
- migration report with exact/non-exact mappings.

### Exit criteria

- Representative old Horizontal and Vertical prefabs can be migrated and visually/geometry compared.
- Undo restores original components/configuration.
- Grid migration flags non-equivalent cases instead of silently guessing.

---

## Phase 12 — Cross-Platform Native Builds

### Goal

Ship correctly imported native artifacts without requiring the package user to build Rust.

### Windows

Initial required:

```text
x86_64-pc-windows-msvc -> taffy_ugui.dll
```

Optional/validated later:

```text
aarch64-pc-windows-msvc
```

### Android

Required:

```text
aarch64-linux-android -> arm64-v8a/libtaffy_ugui.so
```

Compatibility/optional depending on supported Unity/player needs:

```text
armv7-linux-androideabi
x86_64-linux-android
```

Use a Unity-compatible Android NDK for actual Unity validation.

### macOS

Build:

```text
aarch64-apple-darwin
x86_64-apple-darwin
```

Package as the format that works reliably across the supported Unity versions, using a universal dylib where appropriate.

### iOS

Build device ARM64 and required simulator slices. Prefer an XCFramework when supported by the chosen Unity compatibility baseline; retain a static `.a` path where older Unity compatibility requires it.

Managed calls use `DllImport("__Internal")` for iOS player linkage.

### Web/WebGL

Build using:

```text
wasm32-unknown-emscripten
```

The build must use the Emscripten toolchain compatible with the exact Unity version used for the Web build, not blindly the newest global emsdk.

Web is considered complete only after a Unity Web player actually links and runs a layout smoke test.

### Plugin import metadata

Commit `.meta` files/configuration so users do not manually configure platform/CPU import settings after package installation.

### Native smoke test per artifact

Before packaging, every artifact must prove:

```text
ABI query
context create/destroy
3-node flex tree
compute
bulk layout read
```

### Exit criteria

- Windows player smoke test passes.
- macOS player smoke test passes.
- Android ARM64 device/emulator test passes.
- iOS device build links and runs.
- Unity Web build links and executes layout.
- No manual plugin copying/importer setup is required after installing a release package.

---

## Phase 13 — Automated Unity Test Project and Regression Suite

### Goal

Make layout correctness reproducible rather than dependent on visual inspection.

### Rust tests

Cover:

- style conversion
- dimension conversion
- enum mappings
- percentages
- flex properties
- Grid resources/conversion
- generation handles
- stale handles
- invalid input
- context lifetime
- measurement cache
- bulk APIs
- error paths.

### Golden geometry tests

Known input trees assert `x/y/width/height` within epsilon for:

```text
FlexRow
FlexColumn
FlexWrap
GrowShrink
Percentage
MinMax
MarginPaddingGap
AbsolutePosition
AspectRatio
GridBasics
GridAutoFlow
IntrinsicMeasure
```

### Unity Edit Mode tests

Cover:

- serialization
- layout callbacks
- child order
- enable/disable
- hierarchy mutations
- LayoutElement precedence
- Canvas local sizing
- Edit Mode refresh
- prefab behavior
- native cleanup around assembly/playmode lifecycle as testable.

### Unity Play Mode tests

Cover:

- dynamic child add/remove
- runtime style changes
- dynamic text
- parent resolution/size changes
- nested groups
- responsive rules
- ScrollRect
- safe area simulation utilities where feasible.

### Regression scenes/prefabs

Create:

```text
FlexBasics
FlexWrap
FlexNested
PercentageSizing
MinMax
AbsolutePosition
GridBasics
GridAutoFlow
GridResponsiveCards
TextWrap
TMPDynamicText
ScrollRectVertical
ScrollRectGrid
ResponsiveBreakpoints
MixedLayoutElement
MigrationParity
```

Geometry assertions are primary; screenshots may supplement them but will not be the sole correctness signal because TaffyUGUI does not own rendering.

### Exit criteria

- Every exposed managed style has at least one conversion/layout test.
- A bug fix that affects layout adds a regression case before release.
- Core tests run in CI.

---

## Phase 14 — Performance and Allocation Hardening

### Goal

Ensure the bridge is fast enough that Unity Canvas/RectTransform work, not FFI overhead, is usually the dominant cost.

### Work

- bulk style upload
- bulk measurement upload
- bulk layout result retrieval
- persistent arrays/buffers
- reusable child/component caches
- incremental topology updates
- avoid LINQ/reflection in hot paths
- avoid repeated `GetComponent` on every rebuild
- apply RectTransform values only when changed
- separate timings for managed preparation, native compute and Unity application.

### Benchmarks

```text
100 Flex nodes
1,000 Flex nodes
5,000 Flex nodes
100 Grid nodes
1,000 Grid nodes
nested groups
dynamic text
single-node style dirtied
50% styles dirtied
container resize
```

### Initial targets

Targets are goals, not marketing promises, until measured on specified hardware:

```text
unchanged frame: zero native compute and effectively zero TaffyUGUI managed allocation
100-node dirty layout: target < 0.1 ms native/bridge portion where practical
1,000-node dirty layout: target < 1 ms native/bridge portion where practical
```

Always report separately:

```text
hierarchy/style preparation
measurement
marshalling/native upload
Taffy compute
layout result copy
RectTransform application
Unity Canvas consequences
```

### Exit criteria

- No per-frame TaffyUGUI allocation while unchanged after warmup.
- No per-node temporary managed objects per layout pass.
- Performance report is committed with hardware/Unity version/context.
- Known expensive Unity Canvas rebuild costs are not incorrectly attributed to Taffy compute.

---

## Phase 15 — Build Validation and Release Packaging

### Goal

Make installation and release boring and reproducible.

### Unity build preprocessor

Before player builds:

- verify required native plugin exists for active target
- verify architecture
- verify managed/native ABI metadata
- ensure runtime assemblies do not reference Editor-only code
- report unsupported targets
- validate TMP adapter state when relevant
- detect fatal native linkage conditions.

Warnings, not build failures, are used for non-fatal style/hierarchy concerns.

### CI jobs

#### Native core

- rustfmt
- clippy
- Rust tests
- host builds
- ABI smoke tests.

#### Windows

- x64 native artifact
- Unity Edit Mode tests
- Unity Play Mode tests
- sample Windows player smoke test when CI licensing/environment permits.

#### macOS/iOS

- macOS artifacts
- iOS static/XCFramework artifacts
- Unity macOS tests
- iOS link validation.

#### Android

- ARM64 native build
- Unity Android sample build
- symbol/ABI validation.

#### Web

- Unity-compatible Emscripten native build
- Unity Web build
- browser smoke test where CI environment permits.

#### Release

- collect artifacts
- verify version/ABI consistency
- verify notices/licenses
- generate checksums
- produce UPM package archive/tarball
- produce Git-tag-installable package.

### Installation modes

Support:

1. Git URL with package path.
2. UPM release archive/tarball.
3. Local/embedded package for old projects.

### Exit criteria

- Clean Unity project can install a tagged release and immediately load the correct native plugin.
- Existing uGUI project can install without manual Plugin Inspector setup.
- Release contains docs, samples, native binaries, license and third-party notices.

---

# 6. Public Runtime API Design

The public API will be Unity-friendly and must not expose C ABI details.

Conceptual usage:

```text
TaffyLayoutGroup
    Display
    FlexDirection
    FlexWrap
    Gap
    Padding
    GridTemplate...
    MarkLayoutDirty()

TaffyLayoutItem
    Width
    Height
    Min/Max
    FlexGrow
    FlexShrink
    FlexBasis
    AlignSelf
    GridPlacement
    Position
    Insets
    RuntimeStyle
```

Changing a property must:

```text
compare old/new
-> update the managed value
-> mark minimum required dirty flags
-> request Unity layout rebuild
```

No separate user-facing `Apply()` call is required.

---

# 7. Unity Layout Lifecycle Contract

Because Taffy computes both axes together while Unity Auto Layout separates horizontal and vertical stages, the bridge will follow a bounded staged model.

## Stage A — Calculate horizontal inputs

```text
base.CalculateLayoutInputHorizontal()
refresh rectChildren
resolve participation
resolve style/LayoutElement/responsive values
update topology/style cache
collect width-independent measurement
calculate horizontal layout input for parent
```

## Stage B — Set horizontal layout

```text
read actual available width
perform provisional Taffy layout if required
apply x/width
cache child widths
identify width-dependent measurement changes
```

## Stage C — Calculate vertical inputs

```text
measure TMP/Text/content using resolved width constraints
upload changed measurements
recompute if needed
calculate vertical min/preferred/flexible inputs
```

## Stage D — Set vertical layout

```text
apply y/height
if measurement produced a material horizontal change:
    schedule one bounded rebuild
never recursively force rebuild in the active pass
```

This lifecycle will be treated as a core test surface, not incidental implementation detail.

---

# 8. Context Ownership and Nested Groups

The default v1 ownership model is one persistent native context per independent `TaffyLayoutGroup` subtree root.

A nested Taffy group participates as a node in its parent's layout while owning/using its own child layout context as required by the implementation.

The exact nesting coordination will be validated against Unity's parent-first layout cycle. The important guarantees are:

- no global singleton tree requirement
- deterministic native cleanup
- enabling/disabling one prefab cannot corrupt another layout root
- Prefab Mode can own its own native state
- public API does not depend on the final pooling strategy.

If profiling later shows a clear advantage, native contexts may be pooled internally without changing user-facing components.

---

# 9. Conflict Detection Rules

The package will actively detect configurations where two systems try to own the same geometry.

### Competing layout groups

Warn/error for combinations such as:

```text
TaffyLayoutGroup + HorizontalLayoutGroup
TaffyLayoutGroup + VerticalLayoutGroup
TaffyLayoutGroup + GridLayoutGroup
```

when they control the same children.

### ContentSizeFitter

Classify usage as:

```text
supported
supported with constraints
likely cyclic
unsupported
```

Do not issue a blanket warning when a valid preferred-size configuration exists.

### AspectRatioFitter

Prefer Taffy's `aspect_ratio` for children under Taffy control. Warn if both systems own the same size.

### Anchors

Do not silently normalize user anchors. Provide an explicit Undo-capable **Normalize Anchors** action.

### Overflow

If overflow layout says Hidden but no Unity clipping component exists, explain that geometry and visual clipping are separate responsibilities.

---

# 10. Editor and Domain Reload Safety

Edit Mode is a release requirement, not a later convenience.

The package must:

- update after Inspector style changes
- update when parent RectTransform is resized
- update after child add/remove/reorder
- work in Prefab Mode
- avoid scene dirty spam caused only by preview computation
- dispose native contexts before assembly reload/playmode transition where required
- avoid creating/destroying native trees on every Editor repaint
- keep layout preview deterministic.

`[ExecuteAlways]` will only be used where its lifetime behavior is understood and covered by tests.

---

# 11. Samples and Documentation to Ship

## Samples

1. **Flexbox Basics** — row, column, alignment, gap, grow/shrink.
2. **Responsive Cards** — 4/2/1-style responsive behavior primarily through wrapping/min sizes.
3. **Grid** — sidebar/content/header/footer and auto-placement.
4. **ScrollRect** — large dynamic content.
5. **Text Measurement** — TMP wrapped text and auto height.
6. **Responsive Breakpoints** — container rules.
7. **Migration** — original Unity layout next to Taffy migration.

## Documentation

```text
Getting Started
Flexbox
Grid
Block and Positioning
Sizing and Units
LayoutElement Compatibility
Text/TMP Measurement
Responsive Profiles
ScrollRect
Safe Area / Canvas Scaler
Migration
Platform Support
Performance
Troubleshooting
Native ABI / Versioning
```

Troubleshooting must cover at least:

```text
DllNotFoundException
EntryPointNotFoundException
ABI mismatch
Android architecture mismatch
iOS __Internal linkage
Web/Emscripten link errors
TMP wrap instability
ContentSizeFitter cycles
competing layout controllers
```

---

# 12. Release Gates

Features move through these states:

```text
Experimental
-> Implemented
-> Native-tested
-> Unity-tested
-> Platform-validated
-> Documented
-> Production-ready
```

A feature is not called production-ready because it compiles or because Taffy supports it upstream.

---

# 13. Definition of Done for v1.0

TaffyUGUI v1.0 is complete only when all of the following are true:

### Core

- Flexbox production-ready.
- Grid production-ready.
- Block layout supported for documented uGUI cases.
- Auto, percent, min/max and aspect sizing work.
- Margin/padding/gap work.
- Relative/absolute positioning works.
- Nested groups work.
- Core exposed style mappings have tests.

### Existing Unity compatibility

- Existing Button/Image/TMP/ScrollRect/EventSystem remain normal Unity components.
- `LayoutElement` compatibility is documented and tested.
- TMP wrapping is stable.
- Canvas Scaler behavior is correct in logical coordinates.
- ScrollRect integration path is documented and tested.
- Common competing-layout conflicts are diagnosed.

### Responsive/editor

- Responsive overrides work.
- Runtime overrides work.
- Edit Mode preview works.
- Prefab Mode works.
- Layout debugger exists.
- Diagnostics exists.
- Migration from Horizontal/Vertical/GridLayoutGroup is available for supported mappings.

### Native/platform

- ABI mismatch is detected safely.
- Windows works.
- macOS works.
- Android ARM64 works.
- iOS works.
- Unity Web/WebGL works.
- Native binaries are generated by CI.
- Plugin import settings ship with the package.

### Performance/quality

- No continual layout compute when nothing changes.
- No TaffyUGUI steady-state managed allocation after warmup when unchanged.
- Batched native APIs are used on normal layout paths.
- Regression suite covers exposed layout features.
- Platform smoke tests exist.

### Distribution

- Git UPM installation works.
- Release package/archive works.
- Samples and user documentation are included.
- MIT license and third-party notices are included.
- AI-generated-code disclaimer remains clearly visible.

---

# 14. Implementation Order — What Will Be Worked On First

The immediate work order is intentionally narrower than the complete feature list.

## Sprint 1 — Make the current native/Unity Flex path trustworthy

1. Run/fix Rust compile, clippy and unit tests against pinned Taffy.
2. Split native implementation into maintainable modules.
3. Finalize ABI/version/error contracts.
4. Add generation-safe native handles.
5. Add bulk style/layout operations.
6. Refactor C# P/Invoke into `TaffyNativeContext`.
7. Validate context cleanup and ABI mismatch handling.

**Gate:** native 3-node Flex smoke test and managed loader are reliable.

## Sprint 2 — Production Flex `LayoutGroup`

1. Refactor `TaffyLayoutGroup` into proper Unity layout lifecycle stages.
2. Implement dirty flags and caches.
3. Implement `TaffyDimension`/style types.
4. Support row/column/wrap/gap/padding/grow/shrink/basis/min/max/percent.
5. Apply results using `SetChildAlongAxis`.
6. Add nested group test.
7. Add Edit Mode preview.

**Gate:** real existing prefab can replace Horizontal/Vertical layout group without rebuilding children.

## Sprint 3 — Existing content compatibility

1. `LayoutElement` precedence.
2. Image intrinsic measurement.
3. Unity Text measurement.
4. Optional TMP assembly.
5. bounded two-pass text measurement.
6. ContentSizeFitter/conflict tests.

**Gate:** production-style menus/forms/cards with text lay out stably.

## Sprint 4 — General layout + Grid

1. margin/box/absolute/aspect/overflow semantics.
2. Block layout.
3. Grid native resource model.
4. Grid managed authoring types.
5. Grid Inspector basics.
6. Grid golden tests.

**Gate:** responsive Grid sample and native/Unity geometry agree.

## Sprint 5 — Responsive, ScrollRect and tooling

1. responsive profiles/runtime overrides.
2. ScrollRect bridge.
3. safe area.
4. polished inspectors.
5. diagnostics/debugger.
6. migration wizard.

**Gate:** old uGUI project can adopt and debug TaffyUGUI without inspecting source.

## Sprint 6 — Platform/release hardening

1. Windows release artifact.
2. Android ARM64.
3. macOS.
4. iOS.
5. WebGL/Emscripten.
6. Unity platform smoke tests.
7. package/release CI.
8. performance pass.
9. docs/samples.

**Gate:** v1 release candidate can be installed into a clean and an existing Unity project without manual native setup.

---

# 15. Main Technical Risks and Required Mitigations

| Risk | Required mitigation |
|---|---|
| Unity layout recursion | Generation/reentrancy guards, bounded passes, no recursive forced rebuild |
| TMP width/height feedback | Width-constrained pre-measurement, max two normal iterations |
| Native ABI drift | Explicit ABI number, POD structs, exact enum values, smoke tests |
| Stale node/context handles | Generation-aware handles and ownership validation |
| Old prefab layout conflicts | Validator, LayoutElement reuse, migration preview, no destructive automation |
| Canvas rebuild dominates performance | Dirty-driven layout, epsilon writeback, separate profiling stages |
| WebGL toolchain mismatch | Unity-matched Emscripten and actual Unity Web player smoke test |
| Editor native leaks | `IDisposable`, reload/playmode cleanup, diagnostic context count |
| Grid authoring becomes unusable | Managed track abstractions, presets, property drawers, advanced mode |
| Too much inspector complexity | Context-sensitive common fields + Advanced foldout |
| Unsupported future Taffy API assumptions | Pin published release and validate each exposed feature against it |

---

# 16. Final Acceptance Scenario

The project will be considered successful when this scenario works end to end:

```text
1. Developer installs TaffyUGUI from a Git tag/UPM package.
2. No Rust toolchain is required on the developer's machine.
3. Native plugin is selected automatically for the current Unity target.
4. Developer opens an old uGUI prefab.
5. Existing Button/Image/TMP/ScrollRect/scripts remain untouched.
6. Migration Wizard analyzes the hierarchy.
7. Developer replaces HorizontalLayoutGroup/VerticalLayoutGroup/GridLayoutGroup with TaffyLayoutGroup.
8. Existing LayoutElement data is reused.
9. Flex/Grid settings are edited in normal Unity Inspectors.
10. Layout updates immediately in Edit Mode.
11. The same prefab adapts across phone/tablet/desktop container sizes.
12. TMP text wraps and auto-sizes correctly.
13. ScrollRect continues scrolling normally.
14. Runtime changes mark only required layout state dirty.
15. Unity renders and receives input normally.
16. Diagnostics clearly identify any unsupported/conflicting configuration.
17. Windows/macOS/Android/iOS/Web builds use the same managed API and layout behavior.
```

That is the product boundary for TaffyUGUI: **modern responsive geometry for existing Unity uGUI without replacing Unity's UI rendering and interaction stack.**
