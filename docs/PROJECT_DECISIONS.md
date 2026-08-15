# TaffyUGUI — Final Engineering Decisions

**Status:** Normative project contract  
**Applies from:** 2026-08-16  
**Repository:** `dofomii/TaffyUGUI`

This document resolves the pre-development ambiguities found by the readiness audit. If an older planning document conflicts with this file, **this file wins** until that document is updated.

---

## 1. Dependency and Rust baseline

### Taffy

TaffyUGUI targets **Taffy 0.13.0**, pinned exactly in `native/Cargo.toml`.

The native crate enables the stable Taffy capabilities required for the intended integration:

- `std`
- `taffy_tree`
- `flexbox`
- `grid`
- `block_layout`
- `float_layout`
- `calc`
- `content_size`
- `detailed_layout_info`

`serde` and CSS text parsing are not part of the runtime ABI because Unity supplies typed values, not CSS text or serialized Rust style objects.

### Rust versions

- **Project MSRV:** Rust `1.82.0`.
- **Pinned release/development toolchain:** Rust `1.97.1`.
- `rust-toolchain.toml` pins the normal project toolchain.
- CI separately validates the MSRV.
- `Cargo.lock` is committed and all release/platform builds use `--locked` after bootstrap.

The MSRV may only increase through an explicit documented change.

---

## 2. Native ABI lifecycle

The current bootstrap ABI is version **0** and is explicitly unstable.

The ABI lifecycle is:

1. Native engine and FFI are implemented.
2. Native tests establish an **ABI release candidate** contract.
3. The ABI candidate is cross-compiled for every planned platform family.
4. A minimal managed P/Invoke conformance layer validates struct layout, enum values, calling conventions, buffer ownership, and real native calls.
5. Only then is the contract named **ABI v1**.
6. All native artifacts are rebuilt and re-staged from the frozen ABI v1 before user-facing Unity feature work proceeds.

This keeps development native-first without permanently freezing an interface before the managed boundary has proved it.

### Final public handle model

The production C ABI uses:

- `uint64_t` opaque **context handles**;
- `uint64_t` opaque **node/resource handles**;
- generation/version protection for stale handles;
- no raw Rust pointers as persistent public handles.

Handles are scoped to their owning context where applicable. Invalid, stale, or cross-context handles return defined error codes.

### Fixed-width ABI types

The production ABI does **not** expose Rust/C `usize`, C# `UIntPtr`, Rust `bool`, or implementation-defined enum layouts.

Use fixed-width types:

- `uint8_t`, `uint16_t`, `uint32_t`, `uint64_t`;
- `int32_t` for status/enum values;
- `float` / Rust `f32` for layout values.

Array counts use `uint32_t` unless a concrete need for a 64-bit count is demonstrated. This avoids 32/64-bit ABI differences on WebGL versus desktop/mobile targets.

### Calling convention

All exported functions use the C ABI (`extern "C"`). Managed desktop/Android calls use `CallingConvention.Cdecl`. iOS and WebGL use static/internal linkage while keeping the same C signatures.

---

## 3. Authoritative C header

The authoritative public native header will be:

```text
include/taffy_ugui.h
```

It will be generated with **cbindgen** from the deliberately public FFI surface, not hand-maintained independently from Rust.

The production build driver will provide:

```text
python build/build.py header
```

CI will regenerate the header and fail if the committed header differs from generated output.

The independent C/C++ ABI smoke harness must compile against this exact header.

---

## 4. Error and panic policy

Expected failures are normal errors, not panics. Null/invalid handles, invalid enum values, malformed buffers, NaN/infinite inputs where disallowed, unsupported capabilities, and Taffy errors return stable status codes.

Production exports are protected so a Rust unwind never crosses the C ABI boundary.

Policy:

- on targets where Rust unwinding is supported and validated, FFI entry points use a common `catch_unwind` boundary and convert unexpected panics to `TU_INTERNAL_PANIC` plus last-error diagnostics;
- targets where Unity-compatible toolchains require abort-only behavior may build with abort semantics and must advertise that limitation through capabilities/build metadata;
- callers must never rely on panic recovery for normal error handling;
- no documentation may promise recoverable panic handling on a target that is built with abort semantics.

The global release profile is therefore **not forced to `panic = "abort"`**. Per-target panic strategy belongs to the platform build definition.

---

## 5. Unity compatibility baseline

### Primary ABI/build baseline

**Unity 2021.3 LTS** is the primary v1 baseline and remains the minimum declared version in `UnityPackage/package.json` until testing proves otherwise.

The package explicitly depends on:

```json
"com.unity.ugui": "1.0.0"
```

### Compatibility lanes

- **Primary:** Unity 2021.3 LTS.
- **Secondary:** Unity 2022.3 LTS.
- **Forward:** selected supported Unity 6 LTS lane.
- **Backward investigation:** Unity 2019.4 LTS only after the complete runtime package compiles and passes core tests there. The manifest minimum is not lowered based on source inspection alone.

### Windows

Initial validated target:

- Windows 10/11 x64.
- Rust target: `x86_64-pc-windows-msvc`.

Windows 7 compatibility is not a v1 promise even though some Unity 2021.3 configurations can run there.

### macOS

Planned target slices:

- `x86_64-apple-darwin`, deployment baseline matching Unity 2021.3 Intel support (macOS 10.13+);
- `aarch64-apple-darwin`, deployment baseline macOS 11+.

A universal dylib may be assembled after both slices validate.

### Android

The primary Unity 2021.3 Android lane uses Unity's supported **Android NDK r21d (`21.3.6528147`)**.

Required first ABI:

- `arm64-v8a` / Rust `aarch64-linux-android`.

ARMv7 and x86_64 are optional compatibility lanes and are not release promises until validated.

The build system must use the Unity-compatible NDK, not an arbitrary newest system NDK.

### iOS

Primary target:

- iOS ARM64 / Rust `aarch64-apple-ios`;
- Unity 2021.3 runtime baseline: iOS 12+;
- static library or XCFramework packaging selected by actual Unity/Xcode validation.

Simulator slices are secondary development conveniences, not a requirement for the initial runtime artifact.

### WebGL

Unity 2021.3 uses **Emscripten 2.0.19** for WebGL. The production WebGL build must use the Emscripten toolchain bundled with/matched to the validated Unity installation.

A generic `wasm32-unknown-emscripten` build performed with an unrelated system Emscripten version does not prove Unity compatibility.

---

## 6. Native feature scope for v1

TaffyUGUI v1 aims to expose the stable layout capabilities of the selected Taffy 0.13.0 configuration, not only a Flexbox subset.

### Core

- display / box generation;
- box sizing;
- direction;
- overflow and scrollbar reservation;
- position and insets;
- size/min/max;
- aspect ratio;
- margin/padding/border geometry;
- content size information.

### Flexbox

- directions and reverse variants;
- wrap modes;
- basis/grow/shrink;
- align-items/self/content;
- justify-content;
- gap.

### Block / Flow Root / Float

- Block layout;
- FlowRoot where exposed by the selected Taffy version;
- float/clear behavior supported by `float_layout`.

Unity will still own painting/clipping; these features affect geometry only.

### Grid

- explicit and implicit tracks;
- fixed/percent/auto/fraction tracks;
- minmax/repeat where supported;
- auto-flow;
- row/column placement and spans;
- gap;
- align/justify content;
- align/justify items;
- align/justify self;
- named grid lines;
- named grid areas/templates supported by Taffy 0.13.0;
- detailed Grid layout information when useful for diagnostics.

### Calc

Taffy's `calc` capability is enabled natively. The FFI will expose a typed/resource representation rather than sending CSS strings across the ABI. Unity authoring may initially provide common expression builders/presets before a richer editor is added.

---

## 7. Unity layout-system contract

`TaffyLayoutGroup` remains a real `UnityEngine.UI.LayoutGroup`.

In addition to applying child geometry, the production implementation must participate correctly as a layout element itself by reporting its own layout inputs with Unity's layout APIs, including `SetLayoutInputForAxis` as appropriate.

This is required for:

- nested Taffy groups;
- a Taffy group inside a parent Unity layout group;
- `ContentSizeFitter` compatibility where a non-cyclic configuration is supported;
- correct min/preferred/flexible width and height propagation.

The task tracker must include explicit regression coverage for this behavior.

---

## 8. Canonical build-system location

The authoritative build entry point is:

```text
build/build.py
```

It owns:

- toolchain checks;
- header generation;
- native quality commands;
- per-target compilation;
- artifact verification;
- checksums/manifests;
- staging into the Unity package;
- final package assembly.

`scripts/build-native.sh` is bootstrap compatibility tooling only and will become a thin wrapper or be removed once the Python driver covers its use cases.

Generated output lives under `dist/` and remains ignored by Git.

---

## 9. Native binary source-control and release policy

`dist/` is generated and never treated as release source.

`UnityPackage/Plugins/` is different: once the Native Artifact phase begins, **verified native binaries and their Unity `.meta` files are committed as part of the package payload**.

This is required so users can install a release tag with a normal UPM Git URL such as:

```text
https://github.com/dofomii/TaffyUGUI.git?path=/UnityPackage#<tag>
```

without installing Rust or downloading separate CI artifacts.

Rules:

1. binaries in `UnityPackage/Plugins` may only be refreshed by the canonical staging process;
2. a machine-readable manifest records filename, platform, architecture, package/native version, ABI version, Taffy version, source commit, and checksum;
3. release CI rebuilds all advertised artifacts from clean source and verifies the committed/staged payload;
4. release tags contain the verified plugin payload;
5. CI artifacts and release archives are additional distribution forms, not prerequisites for Git-URL installation.

---

## 10. ABI freeze gates

There are two distinct gates:

### Native Engine / ABI Candidate Gate

Before managed integration:

- complete native feature engine;
- candidate C ABI implemented;
- generated C header;
- native golden/safety/ABI/smoke tests green;
- all planned platform-family artifacts compile with the candidate ABI.

### ABI v1 Final Freeze Gate

Before user-facing Unity layout features:

- managed structs/enums match native header exactly;
- managed/native conformance tests pass;
- one real Unity Editor/native smoke flow succeeds on the primary platform;
- calling convention and buffer ownership are proven;
- ABI v1 is assigned;
- all platform native artifacts are rebuilt/re-staged against frozen ABI v1.

After ABI v1, binary-incompatible changes require an ABI version increment.

---

## 11. Definition of "ready to develop"

The project is ready for sustained native feature development when:

- Taffy 0.13.0 is pinned;
- Rust MSRV/release toolchain are pinned/tested;
- current host CI is green;
- `Cargo.lock` is committed;
- this decisions document is present;
- the task tracker reflects these decisions.

Unity feature development remains gated behind the native/ABI milestones defined above.
