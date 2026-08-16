# TaffyUGUI Architecture

**Normative decisions:** [PROJECT_DECISIONS.md](PROJECT_DECISIONS.md)  
**Live implementation state:** [TASK_TRACKER.md](TASK_TRACKER.md)

TaffyUGUI intentionally separates Unity rendering/interaction from Rust layout computation.

## Product boundary

### Unity owns

- GameObjects and hierarchy;
- Canvas / Canvas Scaler;
- RectTransform serialization and final application;
- Image/RawImage/Button/Toggle/etc. rendering and interaction;
- TextMeshPro and Unity Text rendering;
- EventSystem/input;
- masks/clipping;
- ScrollRect/scrollbars;
- Animator/tweens;
- prefabs, scenes, and editor serialization.

### Rust/Taffy owns

- persistent layout trees;
- resolved native layout style state;
- Flexbox, Grid, Block, FlowRoot, Float, and supported Calc geometry;
- cached measurement records supplied by managed code;
- content-size/layout metadata;
- computed `x/y/width/height` geometry.

Rust never renders Unity UI.

---

## Compatibility boundary

The TaffyUGUI C ABI is the only binary boundary between managed Unity code and Rust.

The final production ABI uses:

- fixed-width integer types;
- `float`/`f32` geometry values;
- explicit numeric enums;
- generation-safe opaque `uint64_t` context/node/resource handles;
- caller-owned pointer + `uint32_t` count buffers for temporary arrays;
- stable status codes and last-error diagnostics.

The production ABI does **not** expose persistent Rust pointers, `usize`, Rust `bool`, `Vec`, `String`, Rust references, Taffy `NodeId`, or implementation-defined Rust enum layouts.

The authoritative C header is generated from Rust using cbindgen into:

```text
include/taffy_ugui.h
```

The Phase 2 production `tu_*` interface remains ABI version `0` while it is an unfrozen candidate. The older bootstrap ABI-0 names are no longer exported by the native candidate; existing Unity bootstrap code is intentionally dormant until managed conformance.

---

## Native thread ownership and context registry

Taffy 0.13's compact style/Calc representation is intentionally thread-bound. TaffyUGUI therefore **does not** force `Send`/`Sync` with unsafe implementations. Phase 1 uses a persistent project-owned `NativeTree` implementing Taffy's low-level layout traits so Calc resolution, cached measurement, dirty state, and detailed Grid diagnostics remain under TaffyUGUI control.

Native runtime ownership is intentionally main-thread friendly:

```text
Unity layout thread (normally main thread)
        ↓
thread-local ContextRegistry
        ↓
generation-safe ContextHandle
        ↓
Context
        ↓
persistent NativeTree
        (Taffy low-level layout traits + cache)
```

Rules:

- each `Context` and its `NativeTree` remain on the thread that created them;
- the registry/arena itself is thread-local;
- context handles use a process-wide generation sequence in addition to a local slot index;
- a context handle from another thread cannot accidentally resolve to an unrelated context occupying the same local slot;
- cross-thread context use fails rather than moving native layout state;
- production Unity integration creates, uses, and disposes the native context from the Unity layout/main thread;
- Phase 2 turns wrong-thread use into an explicit production status/diagnostic.

The historical ABI-0 bootstrap used a pointer-shaped compatibility token. The Phase 2 candidate removes that export model and exposes fixed-width `uint64_t` context handles through canonical `tu_*` symbols. Existing bootstrap Unity code is not a compatibility contract and remains paused until Phase 6.

---

## ABI lifecycle

The project deliberately separates **ABI release-candidate lock** from **final ABI v1 freeze**:

```text
complete native engine
    ↓
production C ABI candidate
    ↓
native golden/safety/header/smoke verification
    ↓
ABI-v1-RC lock
    ↓
compile/stage Windows/macOS/Android/iOS/WebGL artifacts
    ↓
minimal managed P/Invoke conformance in Unity
    ↓
freeze ABI v1
    ↓
rebuild/re-stage every native artifact
    ↓
user-facing Unity layout development
```

This keeps development native-first while preventing an untested marshalling/calling-convention mistake from becoming a permanent ABI commitment.

After ABI v1 is frozen, binary-incompatible changes require an ABI version increment.

---

## Runtime flow

The production Unity flow is:

1. `TaffyLayoutGroup` participates in the normal uGUI layout lifecycle.
2. It gathers eligible children and caches Unity-object → native-node mappings.
3. Existing `LayoutElement` values and intrinsic measurements are resolved.
4. Optional `TaffyLayoutItem` values override the documented subset of resolved child style.
5. Managed code uploads topology/styles/measurements through coarse-grained ABI calls.
6. Rust/Taffy computes the layout once for the required layout generation.
7. Managed code bulk-reads geometry/content metadata.
8. `TaffyLayoutGroup` applies changed geometry through Unity layout APIs such as `SetChildAlongAxis`.
9. The group also reports its own min/preferred/flexible layout input using `SetLayoutInputForAxis` where appropriate, so nesting and parent Unity layout systems work correctly.

No native layout is computed from `Update()` merely because a frame elapsed.

---

## Measurement architecture

Unity owns measurement of Unity-rendered content.

- `LayoutElement` values are reused where applicable.
- Images/RawImages provide intrinsic dimensions when requested.
- Unity Text uses uGUI measurement APIs on supported versions.
- TMP lives in an optional adapter assembly.
- width-dependent text uses a bounded iterative flow, normally no more than two production passes.
- custom content may implement a managed measurement provider extension.

Measurement records are cached and uploaded to Rust. Rust does not synchronously call C# once per text node during Taffy layout.

---

## Performance rules

- persistent native tree; do not recreate it every frame;
- explicit dirty reasons for hierarchy/style/measurement/available size;
- bulk style/measurement/result operations;
- reusable managed/native buffers;
- no repeated hot-path hierarchy/component scans once cached;
- no native compute on an unchanged frame by default;
- apply RectTransform values only when geometry changed beyond a defined epsilon;
- profile Unity Canvas/application cost separately from Rust/Taffy compute time.

---

## Platform strategy

Primary package baseline: **Unity 2021.3 LTS**.

Native target families:

- Windows x64: MSVC DLL;
- macOS: dylib slices/universal packaging after validation;
- Android ARM64: `.so` built with Unity 2021.3-compatible NDK r21d;
- iOS ARM64: static library/XCFramework strategy verified through Unity→Xcode;
- WebGL: static/linkage artifact built with the Emscripten toolchain bundled with/matched to Unity 2021.3 (2.0.19 baseline).

A generic WASM/Android build is not evidence of Unity compatibility. Final support is claimed only after the artifact also passes the real Unity player/device/browser validation phase.

---

## Source/build/package ownership

```text
native/                 canonical Rust implementation
include/                generated public C header
build/build.py          authoritative build/package driver
dist/                   generated native output, ignored by Git
UnityPackage/Plugins/   verified native binaries shipped with the UPM package
UnityPackage/Runtime/   managed wrapper and uGUI integration
```

Verified release native binaries and Unity importer `.meta` files are committed inside `UnityPackage/Plugins` so tagged Git-URL UPM installs do not require Rust or separate CI downloads.

---

## Dependency policy

Taffy is exact-pinned at `0.13.0`; `native/Cargo.lock` is committed. Dependency upgrades are explicit engineering changes and must pass native golden/ABI regressions plus the relevant platform matrix. A Taffy update must never silently alter the Unity-facing ABI.
