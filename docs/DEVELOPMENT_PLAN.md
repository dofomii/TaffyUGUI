# TaffyUGUI — Rust-First End-to-End Production Development Plan

**Document type:** Master implementation and release plan  
**Repository:** `dofomii/TaffyUGUI`  
**Installable package:** `UnityPackage/` / `com.dofomii.taffyugui`  
**Native crate:** `native/` / `taffy_ugui_native`  
**Operational tracker:** [TASK_TRACKER.md](TASK_TRACKER.md)  
**Native build contract:** [NATIVE_LIBRARY_BUILD_PLAN.md](NATIVE_LIBRARY_BUILD_PLAN.md)

---

# 1. Final Product Definition

TaffyUGUI is a **Rust native layout engine integration plus a Unity uGUI package**.

It is not a C# layout library that happens to depend on Rust, and it is not a Unity-only project.

The final product contains two first-class deliverables:

```text
TaffyUGUI repository
│
├── Rust/Taffy native engine
│   ├── persistent Taffy trees
│   ├── full v1 layout/style mapping
│   ├── stable C ABI
│   ├── native tests
│   ├── platform build system
│   └── compiled native artifacts
│
└── Unity UPM package
    ├── managed runtime API
    ├── TaffyLayoutGroup / TaffyLayoutItem
    ├── uGUI/TMP/ScrollRect integration
    ├── Editor tooling and migration
    ├── tests/samples/docs
    └── Plugins/ containing the compiled Rust artifacts
```

The final user experience is:

```text
Existing Unity Canvas / prefab
        ↓
Existing panel using uGUI components
        ↓
Replace/migrate Unity LayoutGroup with TaffyLayoutGroup
        ↓
Keep Button / Image / TMP / ScrollRect / Animator / EventSystem / scripts
        ↓
Configure Flexbox / Grid / Block / responsive sizing
        ↓
C# uploads styles + measurements through stable C ABI
        ↓
Compiled Rust library runs Taffy
        ↓
Rust returns x / y / width / height
        ↓
Unity applies results to RectTransform
        ↓
Normal Unity rendering, input and interaction continue unchanged
```

---

# 2. Non-Negotiable Development Strategy

Development is strictly **native-first**.

The sequence is:

```text
A. Rust project foundation
B. Taffy integration and complete native feature engine
C. production C ABI and native safety
D. native verification and ABI freeze
E. cross-platform native compilation
F. Unity-ready native artifact staging
---------------- Native Milestone Gate ----------------
G. Unity managed/native bridge
H. minimal working uGUI product
I. full uGUI feature/measurement compatibility
J. Grid/responsive/editor/integration tooling
K. real Unity player validation per platform
L. performance/reliability hardening
M. final UPM release
```

The existing C# files in the repository are considered bootstrap scaffolding until the **Native Milestone Gate** passes.

This order exists to avoid designing Unity components around an unstable or incomplete native ABI.

---

# 3. Product Boundary

## Unity owns

- GameObjects and hierarchy.
- Canvas and Canvas Scaler.
- RectTransform serialization/application.
- Image/RawImage rendering.
- Button/Toggle/Slider and EventSystem.
- TextMeshPro and Unity Text rendering.
- masks/clipping.
- ScrollRect and scrollbar rendering.
- Animator and input.
- prefabs/scenes.

## Rust/Taffy owns

- layout tree.
- layout styles.
- intrinsic measurement records supplied by Unity.
- Flexbox computation.
- Grid computation.
- Block computation.
- box geometry.
- x/y/width/height results.

## TaffyUGUI C ABI owns the compatibility boundary

Unity never directly depends on Taffy Rust types. Rust never directly depends on Unity types.

---

# 4. Source and Package Structure

The repository evolves toward:

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
├── tests/
│   ├── native/
│   ├── golden/
│   └── abi/
│
├── build/
│   ├── build.py
│   ├── targets.py
│   ├── verify.py
│   ├── package_native.py
│   └── package_unity.py
│
├── dist/
│   └── native/              # generated, normally not source-controlled
│
├── UnityPackage/
│   ├── package.json
│   ├── Runtime/
│   │   ├── Core/
│   │   ├── Styles/
│   │   ├── Native/
│   │   ├── Measurement/
│   │   ├── Responsive/
│   │   ├── Integration/
│   │   └── Diagnostics/
│   ├── Runtime.TMP/
│   ├── Editor/
│   ├── Plugins/
│   │   ├── Windows/
│   │   ├── macOS/
│   │   ├── Android/
│   │   ├── iOS/
│   │   └── WebGL/
│   ├── Tests/
│   ├── Samples~/
│   └── Documentation~/
│
└── .github/workflows/
    ├── native-quality.yml
    ├── native-platforms.yml
    ├── unity-tests.yml
    └── release.yml
```

`native/` is canonical source. `UnityPackage/Plugins/` is the shipped compiled payload.

---

# 5. Rust/Taffy Baseline and Dependency Policy

The repository currently pins an exact Taffy version. That exact pin remains until an explicit dependency-upgrade task changes it.

Before the native ABI is frozen:

1. verify the chosen Taffy release exposes the required Flexbox/Grid/Block/style APIs.
2. verify required features compile on all target families.
3. commit `Cargo.lock`.
4. document feature flags.
5. record the embedded Taffy version through the native version API.

Production rules:

- never track a floating Git branch.
- never let a Taffy update silently change the Unity-facing ABI.
- upgrade Taffy only with full native golden/ABI regression tests.

The current crate remains configured for both:

```toml
crate-type = ["cdylib", "staticlib"]
```

Dynamic libraries are used where Unity loads shared plugins; static output supports iOS/WebGL-style linkage.

---

# 6. Phase 0 — Rust Project and Toolchain Foundation

## Objective

Make the native crate clean, deterministic and reproducible before adding more feature surface.

## Work

- repair `rustfmt` and Clippy failures.
- keep warnings denied in CI.
- verify tests/release builds on Linux, Windows and macOS hosts.
- commit `Cargo.lock`.
- establish module boundaries.
- define Rust toolchain/MSRV policy.
- add local developer scripts equivalent to CI.
- verify clean-clone build.

## Required commands/gates

```text
cargo fmt --check
cargo clippy --all-targets -- -D warnings
cargo test
cargo build --release
```

## Exit result

A trustworthy Rust project foundation that can be expanded without build ambiguity.

---

# 7. Phase 1 — Build the Full Rust/Taffy Layout Engine

This phase finishes the native feature engine **before Unity relies on it**.

## 7.1 Context and tree model

Each native context owns:

```text
TaffyTree<NodeContext>
handle arena / generations
style cache
measurement cache
temporary upload/result buffers
last error state
version/capability data
```

Required operations:

```text
context create/destroy/clear
node create/remove
set children
set style
set measurement
mark dirty
compute layout
read layout(s)
```

The tree is persistent. It is not recreated every frame.

## 7.2 Handle model

Unity-visible node/context values are opaque numeric handles.

Requirements:

- stale handle detection.
- generation protection.
- deterministic destruction.
- no raw Taffy `NodeId` exposure.
- invalid handles return errors, never undefined behavior.

## 7.3 Complete v1 style mapping

Implement the native representations/conversions needed for:

### Core

```text
display
box sizing
direction
overflow X/Y
scrollbar reservation
position
insets
size
min size
max size
aspect ratio
margin
padding
layout border
```

### Flexbox

```text
flex direction
flex wrap
flex basis
flex grow
flex shrink
align items
align self
align content
justify content
gap
```

### Block

- Block display/flow behavior exposed by the selected Taffy baseline.

### Grid

```text
template columns/rows
auto columns/rows
auto flow
placement/spans
gap
alignment
fixed tracks
percent tracks
auto tracks
fraction tracks
minmax/repeat where supported
```

Grid variable-length state must not be embedded as arbitrary Rust vectors in fixed FFI structs. Use explicit resources or serialized bulk descriptions with clear lifetime rules.

## 7.4 Unit model

Managed/native representation supports the meaningful Taffy variants:

```text
Auto
Length / Point logical unit
Percent
future Calc only if deliberately enabled
```

Percent values cross the ABI in normalized form.

One native logical length unit corresponds to one Unity local RectTransform layout unit once Unity integration begins. Device pixels remain a Canvas Scaler concern.

## 7.5 Measurement model

Rust must be able to compute layouts using cached measurement data supplied by Unity later.

The native API therefore supports records describing:

- known width/height.
- available width/height behavior.
- intrinsic preferred sizes.
- replaced-element/intrinsic ratio information where applicable.

Critical rule:

> Rust does not call back into C# for every measurement request.

Unity/TMP measurement is a later managed responsibility; the native engine already provides the input mechanism now.

## 7.6 Bulk steady-state design

Before native completion, support coarse-grained operations needed for production:

```text
bulk create/remove where justified
bulk child/topology updates where useful
bulk style updates
bulk measurement updates
compute once
bulk layout result readback
```

## Exit result

The complete intended v1 layout engine works through Rust/native tests without Unity.

---

# 8. Phase 2 — Production C ABI and Safety

The ABI is a product API, not an incidental `extern C` layer.

## 8.1 Version/capability handshake

Required conceptual exports:

```text
tu_get_abi_version()
tu_get_taffy_version()
tu_get_build_version()
tu_get_capabilities()
```

ABI version is independent of package SemVer.

Example conceptual release metadata:

```text
Package: 1.0.0
Native:  1.0.0
ABI:     1
Taffy:   exact pinned version
Commit:  source SHA
Target:  target triple
```

## 8.2 Context/node API

Conceptual ABI:

```text
tu_context_create
tu_context_destroy
tu_context_clear

tu_node_create
tu_nodes_create_bulk
tu_node_remove
tu_nodes_remove_bulk
tu_node_set_children
tu_node_set_children_bulk
tu_node_set_style
tu_nodes_set_styles_bulk
tu_node_set_measurement
tu_nodes_set_measurements_bulk
tu_node_mark_dirty

tu_compute_layout
tu_get_layout
tu_get_layouts_bulk
```

Exact names/signatures are frozen only after tests prove the design.

## 8.3 FFI rules

All Rust structs crossing FFI:

```rust
#[repr(C)]
```

Managed side later mirrors them with sequential layout.

Rules:

- fixed-width integers.
- `f32`/float.
- no Rust `bool` in ABI.
- no Rust enum layout dependency.
- no Rust `Vec`/`String`/slice/reference ownership crossing ABI.
- explicit numeric enums.
- pointer+count only for temporary caller-owned buffers.
- no unwind across FFI.

## 8.4 Error model

Stable numeric errors, conceptually:

```text
TU_OK
TU_INVALID_CONTEXT
TU_INVALID_NODE
TU_INVALID_ARGUMENT
TU_ABI_MISMATCH
TU_TAFFY_ERROR
TU_OUT_OF_MEMORY
TU_PANIC
TU_UNSUPPORTED
```

Provide last-error diagnostics for development/support paths.

## 8.5 Panic and safety

Every unsafe export gets a documented `# Safety` contract.

FFI validation covers:

- null pointers.
- invalid counts.
- invalid enums.
- NaN/infinite values where invalid.
- stale handles.
- context mismatch.
- unsupported capabilities.

No Rust panic may unwind across the C boundary.

## Exit result

The entire Phase 1 native engine is exposed through a stable, safe binary interface.

---

# 9. Phase 3 — Native Verification and ABI Freeze

Before cross-compiling, prove the host library independently.

## 9.1 Unit tests

Cover:

- contexts.
- generation handles.
- stale handles.
- style conversion.
- dimensions/percent.
- Flex enums/layout.
- Grid resources/layout.
- Block layout.
- measurement cache.
- error codes/messages.
- version/capabilities.

## 9.2 Golden geometry tests

Known trees/styles assert epsilon-tolerant:

```text
x
y
width
height
```

Cases:

- flex row/column/reverse.
- wrap.
- grow/shrink/basis.
- percent/min/max.
- margin/padding/gap.
- absolute/relative.
- aspect ratio.
- Block.
- Grid explicit/implicit/auto-flow.
- intrinsic measurement.

## 9.3 Independent ABI smoke harness

A tiny C/C++ or equivalent binary loads the compiled host artifact and performs:

```text
read version/capabilities
create context
create root + children
upload styles
set children
compute known layout
bulk-read results
assert geometry
destroy context
```

It verifies the **compiled artifact**, not only Rust source behavior.

## 9.4 ABI contract tests

- struct size/alignment.
- numeric enum values.
- version handshake.
- invalid input.
- repeated create/destroy.
- stress topology mutation.

## 9.5 ABI freeze

Only after all tests pass, assign ABI v1.

After that, incompatible binary changes require a deliberate ABI increment.

---

# 10. Phase 4 — Cross-Platform Native Build System

Now compile the complete, verified ABI v1 library for every planned Unity platform family.

This happens **before Unity feature development**.

## 10.1 Authoritative build entry point

Converge on commands conceptually like:

```text
python build/build.py native host
python build/build.py native windows-x64
python build/build.py native macos
python build/build.py native android-arm64
python build/build.py native ios
python build/build.py native webgl
python build/build.py native all
python build/build.py stage-unity
```

Responsibilities:

1. verify prerequisites.
2. choose Rust target/toolchain.
3. build locked release crate.
4. verify output exists and matches architecture.
5. verify symbols/ABI where possible.
6. execute smoke harness when target is host-runnable.
7. stage deterministic artifact path.
8. emit target/version manifest.

## 10.2 Target matrix

### Windows

Primary:

```text
x86_64-pc-windows-msvc → taffy_ugui.dll
```

Additional ARM64 is included only when supported/advertised by the selected Unity compatibility matrix.

### macOS

```text
aarch64-apple-darwin → libtaffy_ugui.dylib
x86_64-apple-darwin  → libtaffy_ugui.dylib where supported
```

A universal binary may be assembled after both slices validate.

### Android

Required primary:

```text
aarch64-linux-android → libtaffy_ugui.so → arm64-v8a
```

Additional ABI lanes such as ARMv7/x86_64 are added only when part of the compatibility/test matrix.

The NDK must match supported Unity toolchain requirements rather than simply using the newest installed NDK.

### iOS

```text
aarch64-apple-ios → libtaffy_ugui.a
```

Simulator slices/XCFramework packaging are added according to the Unity/Xcode validation matrix.

### Unity Web/WebGL

WebGL is treated as a dedicated Emscripten linkage target.

Do not claim compatibility from a generic WASM build. The build must use the Emscripten toolchain compatible with the Unity lane being validated.

## 10.3 Cross-platform checks

For every artifact:

- file format.
- architecture.
- expected symbols.
- ABI version contract.
- build/Taffy version metadata.
- checksum.
- smoke execution where runnable.
- static/link validation where not runnable.

## Exit result

A reproducible `dist/native/...` set exists for every planned platform family.

---

# 11. Phase 5 — Unity-Ready Native Artifact Staging

The compiled Rust binaries now become part of the future UPM package.

Target structure:

```text
UnityPackage/Plugins/
├── Windows/
│   ├── x86_64/taffy_ugui.dll
│   └── ARM64/taffy_ugui.dll        # only if supported
├── macOS/
│   └── libtaffy_ugui.dylib
├── Android/
│   ├── arm64-v8a/libtaffy_ugui.so
│   ├── armeabi-v7a/...             # optional matrix
│   └── x86_64/...                  # optional matrix
├── iOS/
│   └── static/XCFramework assets
└── WebGL/
    └── Emscripten-compatible linkage assets
```

## Required staging work

- deterministic copy/stage script.
- Unity library naming consistent with P/Invoke.
- plugin importer `.meta` configuration committed.
- artifact manifest with checksum/version/ABI/target metadata.
- no hand-copied binary dependency.
- clean-clone native build can regenerate the package payload.

## Native Milestone Gate

No Unity feature phase begins until:

1. full native engine complete.
2. full intended v1 C ABI complete.
3. ABI tests/golden/smoke tests green.
4. ABI v1 frozen.
5. required platform-family artifacts compiled.
6. artifacts staged in final UPM plugin layout.
7. plugin importer metadata prepared.
8. artifact manifest/checksums generated.
9. process reproducible from clean source.

---

# 12. Phase 6 — Unity Managed/Native Foundation

Only now do we treat Unity development as active.

## 12.1 Runtime native wrapper

Organize:

```text
Runtime/Native/
├── TaffyNative.cs
├── TaffyNativeTypes.cs
├── TaffyNativeContext.cs
├── TaffyNativeException.cs
├── TaffyNativeVersion.cs
└── TaffyNativeCapabilities.cs
```

`TaffyNative` contains internal P/Invoke only.

Platform selection:

```text
Windows/macOS/Android: DllImport("taffy_ugui")
iOS:                  DllImport("__Internal")
WebGL:                 DllImport("__Internal")
```

Exact compile symbols are centralized.

## 12.2 Safe managed context

`TaffyNativeContext : IDisposable`:

- checks ABI/capabilities.
- creates/destroys context.
- guards use-after-dispose.
- records owner thread.
- translates errors.
- manages reusable buffers.
- cleans up on Unity/editor lifecycle changes.

Do not depend on finalizers alone.

## 12.3 Managed/native contract tests

Verify:

- struct size/layout.
- enum values.
- ABI version.
- capabilities.
- native load.
- context lifecycle.

## Exit result

Unity can safely load and call the completed native engine.

---

# 13. Phase 7 — Minimal Working Unity uGUI Product

The first user-facing vertical slice deliberately stays small.

## TaffyLayoutGroup

Base:

```text
UnityEngine.UI.LayoutGroup
```

Initial responsibilities:

- call `base.CalculateLayoutInputHorizontal()`.
- collect direct `rectChildren`.
- maintain persistent native nodes.
- support Row/Column.
- support simple Auto/Point size.
- padding/gap.
- compute through native ABI.
- read results.
- apply with Unity layout APIs.

Initial lifecycle:

```text
CalculateLayoutInputHorizontal
CalculateLayoutInputVertical
SetLayoutHorizontal
SetLayoutVertical
OnEnable/Disable
OnValidate
OnRectTransformDimensionsChange
OnTransformChildrenChanged
```

Edit/Play Mode must work without continuous `Update()` layout.

## Exit validation prefab

```text
Canvas
└── panel + TaffyLayoutGroup
    ├── Button
    ├── Image
    ├── Text/TMP object
    └── nested panel
```

Only the layout controller changes. Rendering and input remain standard uGUI.

---

# 14. Phase 8 — Production Flexbox, Box Model and Measurement

## 14.1 TaffyLayoutItem

Optional child component for explicit overrides beyond inferred values.

Sections:

```text
participation
position/insets
size/min/max/aspect
margin/padding/border geometry
flex basis/grow/shrink/align self
grid placement later
advanced overflow/direction/replaced behavior
```

A child does not require it for basic layouts.

## 14.2 Existing LayoutElement compatibility

Default precedence:

```text
explicit TaffyLayoutItem override
    > LayoutElement
    > intrinsic measurement
    > Taffy default
```

Map relevant min/preferred/flexible data without requiring prefab recreation.

## 14.3 Text/Image measurement

### Unity Text

Use available uGUI preferred-size APIs within supported Unity range.

### TMP

Separate assembly:

```text
TaffyUGUI.TMP
```

Flow:

1. provisional width constraint.
2. `GetPreferredValues` or appropriate TMP measurement.
3. upload measurement record.
4. compute native layout.
5. if wrapping width materially changes, perform one bounded second measurement/layout pass.
6. stop at strict iteration limit.

Default maximum: 2 production iterations.

### Image/RawImage

Use intrinsic sprite/texture dimensions where requested and preserve aspect semantics.

### Custom

Public `ITaffyMeasureProvider` extension point.

## 14.4 Dirty architecture

Flags:

```text
Hierarchy
Style
Responsive
Measurement
AvailableSize
Transform
NativeTree
```

No layout every frame by default.

Cache component references, style hashes, measurements, native handles and available size.

## 14.5 Unity layout lifecycle bridge

Taffy computes both axes, while uGUI splits horizontal/vertical passes.

Bounded model:

- horizontal calculation collects hierarchy/style.
- horizontal set computes provisional widths.
- vertical calculation updates width-dependent measurement.
- vertical set applies final height/position.
- if another rebuild is required, schedule it rather than recursively forcing immediate rebuild.

Use generation/reentrancy/epsilon protections.

---

# 15. Phase 9 — CSS Grid Unity Authoring

The native Grid engine already exists from Phase 1; this phase builds Unity serialization/authoring around it.

Managed types:

```text
TaffyGridTrack
TaffyGridTrackList
TaffyGridPlacement
TaffyGridNamedArea
TaffyGridTemplate
```

Normal Inspector should make common patterns easy:

```text
1fr 1fr 1fr
200px 1fr
auto 1fr auto
repeat(3, 1fr)
minmax(120px, 1fr)
```

Do not expose only raw nested arrays.

Grid template native resources must be disposed/reused correctly.

---

# 16. Phase 10 — Responsive Extensions and Unity Integration Hardening

## Responsive system

Intrinsic responsiveness remains Taffy-based:

- percent.
- flex wrap.
- min/max.
- Grid tracks.
- grow/shrink.
- aspect ratio.

Optional Unity extension adds container-based rules:

```text
min/max width
min/max height
orientation
aspect range
```

Resolution precedence:

```text
serialized base
→ responsive profile
→ local responsive overrides
→ runtime overrides
```

## Runtime override API

Allow non-serialized state-driven changes without modifying prefab defaults.

## ScrollRect

`TaffyScrollRectBridge`:

- viewport size dirties content layout.
- scrollbar reservation can affect native layout.
- ScrollRect remains responsible for scrolling/clipping/rendering.
- content position movement while scrolling must not cause relayout.

## Canvas Scaler

Taffy receives RectTransform-local logical dimensions, not raw device pixels.

## Safe Area

Optional helper maps `Screen.safeArea` into Canvas-local padding/offset behavior.

## Animation conflicts

Warn when an Animator/tween directly drives child RectTransform properties that Taffy also owns.

---

# 17. Phase 11 — Editor Tooling and Migration

## Inspectors

### TaffyLayoutGroupEditor

- contextual Flex/Grid/Block sections.
- native version/status.
- current allocated size.
- node count/timing.
- dirty reason.
- inline warnings.
- normalize anchors action.
- copy/migrate old LayoutGroup values.
- responsive width preview.

### TaffyLayoutItemEditor

- per-property override toggles.
- resolved values and source.
- contextual Flex/Grid fields.
- measurement debug data.

## Property drawers

- Dimension/Length.
- Rect.
- Grid tracks/placement.
- responsive rules.

## Debugger

`Window > TaffyUGUI > Layout Debugger`

Show:

- Unity object.
- native handle.
- resolved styles.
- measurements.
- computed geometry.
- Grid/Flex metadata.
- responsive rule.
- dirty flags.
- compute timing.

## Diagnostics

`Window > TaffyUGUI > Diagnostics`

Categories:

```text
Native plugin/ABI
Hierarchy
Conflicts
Measurement
Performance
Platform
Package version
```

## Migration Wizard

Safely convert:

```text
HorizontalLayoutGroup → Flex Row
VerticalLayoutGroup   → Flex Column
GridLayoutGroup       → explicit Taffy Grid where mapping is deterministic
```

Requirements:

- Undo.
- preview.
- preserve source values before removing old component.
- explicit unmappable-property report.
- prefab-safe behavior.
- no mass-save without explicit action.

---

# 18. Phase 12 — Unity Cross-Platform Player Validation

Native artifacts were already built before Unity implementation. Now prove them inside Unity.

## Windows

- Editor native load.
- player build.
- ABI handshake.
- core Flex/Grid/TMP smoke/regression.

## macOS

- correct architecture/universal selection.
- Editor/player load.
- same ABI/layout tests.

## Android

- ARM64 required primary device/player lane.
- native load through Unity plugin importer.
- ABI/layout smoke.
- representative runtime regression.

## iOS

- static linkage through Unity/Xcode pipeline.
- `__Internal` calls resolve.
- device runtime smoke.

## WebGL

- Unity-compatible Emscripten link.
- `__Internal` linkage.
- browser runtime smoke.

A platform becomes documented as supported only after this phase validates it.

---

# 19. Performance and Allocation Architecture

Initial targets are budgets to validate, not marketing promises.

Measure separately:

```text
C# hierarchy scan
style resolution
measurement
marshalling/upload
Rust/Taffy compute
result copy
RectTransform apply
Unity Canvas rebuild consequences
```

Steady-state goals:

- no native layout call when unchanged.
- no per-frame managed allocation from TaffyUGUI after warmup in validated idle case.
- reuse arrays/buffers.
- no LINQ/reflection in hot path.
- no repeated GetComponent scans during every layout.

Benchmarks:

```text
100 nodes
1,000 nodes
5,000 nodes
10,000 stress case
Flex
Grid
nested groups
dynamic text
single-node dirty
50% dirty
resolution resize
```

Do not report only Taffy's compute time.

---

# 20. Native and Unity Test Architecture

## Native Rust tests

- style conversion.
- dimensions/percent.
- Flex/Grid/Block.
- handles.
- errors.
- context lifecycle.
- measurement cache.
- bulk operations.

## ABI tests

- layout/alignment of structs.
- numeric enum mapping.
- compiled artifact loading.
- version/capabilities.
- invalid/stale handles.

## Golden layout tests

Deterministic geometry cases for all exposed layout features.

## Unity Edit Mode

- serialization.
- hierarchy changes.
- layout callbacks.
- LayoutElement mapping.
- native lifecycle.
- prefab mode.
- Canvas scaling.
- migration data.

## Unity Play Mode

- runtime add/remove.
- runtime style changes.
- text changes.
- resolution/orientation.
- nested groups.
- ScrollRect.
- responsive rules.

## Platform tests

- Windows.
- macOS.
- Android ARM64.
- iOS ARM64.
- WebGL.

Where CI cannot fully execute a device lane, keep reproducible build/link checks and documented manual/device validation evidence.

---

# 21. Regression Scenes/Samples

Stable geometry cases:

```text
FlexBasics
FlexWrap
FlexNested
PercentageSizing
MinMax
AbsolutePosition
BlockBasics
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

Prefer geometry assertions over screenshot-only tests because TaffyUGUI does not own rendering.

---

# 22. CI/CD Architecture

## Native quality workflow

Every relevant change:

```text
fmt
clippy
tests
host release build
golden/ABI tests
```

## Native platform workflow

Build complete native artifact matrix from clean checkout.

Outputs:

- binaries/static libraries.
- manifest.
- checksums.
- symbol/architecture verification.

## Unity workflow

Once Native Milestone is complete:

- install staged native payload.
- Unity Edit Mode tests.
- Play Mode tests.
- package validation.

## Platform Unity workflow

- target player build.
- smoke/regression execution where possible.

## Release workflow

1. native quality green.
2. rebuild all native target artifacts.
3. verify/stage Plugins.
4. run Unity regressions.
5. run platform validation matrix.
6. package UPM payload.
7. generate checksums/version manifest.
8. publish release only when all required gates pass.

---

# 23. Build Validation Inside Unity

Implement pre-build validation that checks:

- native plugin for selected target exists.
- correct ABI/version metadata.
- supported architecture.
- runtime assembly does not reference Editor assembly.
- required TMP adapter state when TMP measurement is used.
- known fatal layout/plugin conflicts.

Fatal errors are limited to conditions that will definitely break the player.

---

# 24. Documentation and Samples Required for Release

Required documentation:

```text
Getting Started
Architecture
Native Build from Source
Native ABI/versioning
Flexbox
Grid
Block/box model
LayoutElement
Text/TMP measurement
Responsive system
ScrollRect
Migration
Platform support
Performance
Troubleshooting
```

Troubleshooting includes:

```text
DllNotFoundException
EntryPointNotFoundException
ABI mismatch
wrong native architecture
Android ABI mismatch
iOS __Internal/link errors
WebGL/Emscripten link errors
TMP wrap mismatch
ContentSizeFitter cycles
```

Required samples:

1. Flexbox basics.
2. Responsive cards.
3. Grid.
4. ScrollRect.
5. Text/TMP measurement.
6. Breakpoints.
7. Migration parity.

---

# 25. Release Packaging

Supported delivery forms:

- Git URL using `?path=/UnityPackage` or equivalent correct package path.
- UPM tarball/release archive.
- embedded/local package for older projects.

The release package contains prebuilt native binaries. End users should not need Rust installed for ordinary package use.

Developers building from source use the documented native build pipeline.

`package.json` must accurately include:

- package name/display name/version.
- tested minimum Unity version.
- description/license/author/keywords.
- samples/documentation/repository references.

---

# 26. v1.0 Definition of Done

TaffyUGUI v1.0 is ready only when both halves are complete.

## Native half

- Rust source maintained in repository.
- reproducible Cargo dependency graph.
- full intended v1 Flex/Grid/Block/core style behavior implemented.
- stable C ABI v1.
- stable error/version/capability contract.
- golden/ABI/safety/smoke tests green.
- required Windows artifact(s) compiled.
- required macOS artifact(s) compiled.
- Android ARM64 compiled.
- iOS ARM64 compiled.
- Unity Web/WebGL artifact compiled using appropriate Emscripten strategy.
- target artifacts generated by automation/CI.
- artifacts staged in Unity package with metadata/checksums.

## Unity half

- existing uGUI rendering/input unchanged.
- Flexbox production-ready.
- Grid production-ready.
- Block/core box model documented and stable.
- percent/auto/min/max/aspect/absolute sizing works.
- LayoutElement compatibility works.
- Unity Text/TMP/image intrinsic measurement works as documented.
- nested Taffy groups work.
- ScrollRect path works.
- responsive/runtime overrides work.
- Edit/Prefab Mode preview works.
- migration tools work safely.
- diagnostics identify common problems.
- Windows/macOS/Android/iOS/WebGL Unity player paths validated.
- no continuous unchanged-frame Taffy recomputation by default.
- package native/plugin configuration requires no manual user setup.

## Release half

- documentation and samples complete.
- MIT license and third-party notices correct.
- AI-generated-code/use-at-own-risk disclaimer retained.
- security/support limitations documented.
- clean Unity project install validated.
- representative existing uGUI project install/migration validated.
- full final matrix green.

---

# 27. Main Engineering Risks and Mitigations

## Rust/Taffy API drift

Mitigation:

- exact dependency pin.
- Cargo.lock.
- ABI isolation.
- golden tests before upgrades.

## FFI safety/ABI drift

Mitigation:

- POD structs.
- explicit enum numbers.
- version handshake.
- struct-size tests.
- stale-handle tests.
- no unwind.

## Cross-platform toolchain mismatch

Mitigation:

- build target-specific artifacts early, before Unity feature work.
- record exact toolchain prerequisites.
- Unity-compatible NDK/Emscripten paths.
- clean CI builds.

## Unity layout rebuild loops

Mitigation:

- dirty scheduling.
- bounded text iterations.
- no recursive `ForceRebuildLayoutImmediate` inside active pass.
- reentrancy/generation guards.

## TMP wrapping mismatch

Mitigation:

- width-constrained TMP adapter.
- bounded two-pass layout.
- wrapped-text regression cases.

## Old prefab conflicts

Mitigation:

- LayoutElement reuse.
- optional TaffyLayoutItem.
- conflict validator.
- Undo-aware migration.

## Canvas rebuild cost dominating native speed

Mitigation:

- apply RectTransform only when geometry changed.
- dirty-driven native work.
- profile managed/native/application costs separately.

## Editor native leaks

Mitigation:

- IDisposable context ownership.
- assembly/play-mode cleanup hooks.
- diagnostics for live context count.

---

# 28. Immediate Execution Order

The repository is currently in **Phase 0**.

The immediate sequence is:

```text
1. fix rustfmt/Clippy failures
2. get host CI fully green
3. commit/validate Cargo.lock
4. organize native modules
5. implement full Rust/Taffy v1 feature engine
6. implement/freeze production C ABI
7. build golden/ABI/smoke suites
8. freeze ABI v1
9. build Windows/macOS/Android/iOS/WebGL native artifacts
10. stage them into UnityPackage/Plugins
11. pass Native Milestone Gate
12. begin active Unity package feature development
```

That sequence is the controlling implementation order for the project.
