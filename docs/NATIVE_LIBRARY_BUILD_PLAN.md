# TaffyUGUI — Rust Native Library Build and Packaging Plan

**Status:** Active engineering contract  
**Repository:** `dofomii/TaffyUGUI`  
**Native crate:** `native/` / `taffy_ugui_native`  
**Library name:** `taffy_ugui`  
**Unity package:** `UnityPackage/`  
**Related documents:** [DEVELOPMENT_PLAN.md](DEVELOPMENT_PLAN.md), [TASK_TRACKER.md](TASK_TRACKER.md)

---

## 1. Product Model

TaffyUGUI is not only a Unity C# project.

The final product is composed of **two first-class software deliverables** that must remain compatible:

```text
TaffyUGUI repository
│
├── Rust native library
│   ├── Taffy dependency
│   ├── TaffyUGUI-owned C ABI
│   ├── context/node/style/measurement implementation
│   ├── native tests
│   └── platform binaries
│
└── Unity UPM package
    ├── C# runtime API
    ├── uGUI integration
    ├── editor tools
    ├── tests/samples/docs
    └── compiled Rust binaries in Plugins/
```

A release is incomplete if either side is missing.

The Unity package does **not** reimplement Taffy in C#. Unity calls the compiled Rust library through the stable TaffyUGUI C ABI.

---

## 2. Native Source Is Part of the Project

The canonical native source lives under:

```text
native/
├── Cargo.toml
├── Cargo.lock
└── src/
    ├── lib.rs
    ├── ffi.rs
    ├── context.rs
    ├── handles.rs
    ├── style.rs
    ├── grid.rs
    ├── measurement.rs
    ├── error.rs
    └── version.rs
```

The exact module split will be introduced incrementally, but the ownership boundary is fixed:

- `native/` owns Rust/Taffy execution.
- `UnityPackage/Runtime/Native/` owns managed P/Invoke and safe managed wrappers.
- `UnityPackage/Plugins/` contains compiled native artifacts shipped to users.

The Rust source remains in the repository even though end users normally consume precompiled binaries through the Unity package.

---

## 3. Native Library Output Contract

The crate must support both dynamic and static output:

```toml
[lib]
name = "taffy_ugui"
crate-type = ["cdylib", "staticlib"]
```

Expected release artifacts:

| Unity target | Rust target | Artifact |
|---|---|---|
| Windows x64 | `x86_64-pc-windows-msvc` | `taffy_ugui.dll` |
| Windows ARM64 | `aarch64-pc-windows-msvc` | `taffy_ugui.dll` |
| macOS Apple Silicon | `aarch64-apple-darwin` | `libtaffy_ugui.dylib` |
| macOS Intel | `x86_64-apple-darwin` | `libtaffy_ugui.dylib` |
| Android ARM64 | `aarch64-linux-android` | `libtaffy_ugui.so` |
| Android ARMv7 | `armv7-linux-androideabi` | `libtaffy_ugui.so` |
| Android x86_64 | `x86_64-linux-android` | `libtaffy_ugui.so` |
| iOS device | `aarch64-apple-ios` | `libtaffy_ugui.a` |
| iOS simulator ARM64 | `aarch64-apple-ios-sim` | `libtaffy_ugui.a` |
| iOS simulator Intel, when supported | `x86_64-apple-ios` | `libtaffy_ugui.a` |
| Unity Web/WebGL | Unity-compatible Emscripten Rust target/toolchain | static library linked into the Unity Web build |

Not every optional architecture has to ship in the first release. The supported matrix is determined by the task tracker and must match the documented Unity compatibility matrix.

---

## 4. Native Build Pipeline

The native build is a required project pipeline, not a manual side task.

The logical pipeline is:

```text
Rust source
    ↓
cargo fmt
    ↓
cargo clippy
    ↓
cargo test
    ↓
release compile for target
    ↓
native ABI smoke test
    ↓
artifact naming/architecture verification
    ↓
copy/package into UnityPackage/Plugins/<platform>/
    ↓
Unity package/platform validation
```

A native artifact must never be copied into the Unity package merely because compilation succeeded. It must first pass the ABI smoke test.

---

## 5. Required Native Quality Gates

For normal native development, CI must validate:

```text
cargo fmt --check
cargo clippy --all-targets -- -D warnings
cargo test
cargo build --release
```

As the wrapper matures, additional checks will be added:

- ABI struct-size/layout tests.
- enum/value mapping tests.
- stale-handle tests.
- invalid argument tests.
- panic-boundary tests.
- golden layout geometry tests.
- version/capability handshake tests.
- packaged-artifact smoke tests.

No native phase is stable while these required checks are red.

---

## 6. ABI Is Owned by TaffyUGUI

Unity must never call Taffy Rust APIs directly.

The Rust wrapper owns a stable C ABI containing functions conceptually equivalent to:

```text
tu_get_abi_version
tu_get_taffy_version
tu_get_build_version
tu_get_capabilities

tu_context_create
tu_context_destroy
tu_context_clear

tu_node_create
tu_node_remove
tu_node_set_children
tu_node_set_style
tu_node_set_measurement
tu_node_mark_dirty

tu_compute_layout
tu_get_layout
tu_get_layouts_bulk
```

Names may evolve before the ABI is frozen, but the architectural contract does not.

Required ABI rules:

- C-compatible POD structs only.
- explicit numeric enum values.
- no Rust `String`, `Vec`, references, closures, or Taffy node identifiers across FFI.
- no Rust unwind across FFI.
- fallible functions return stable error codes.
- stale/invalid handles are detected.
- managed/native ABI versions are checked before layout begins.

---

## 7. Unity Packaging Contract

Native source is built outside Unity. The resulting binaries become files inside the UPM package.

Target package structure:

```text
UnityPackage/
└── Plugins/
    ├── Windows/
    │   ├── x86_64/
    │   │   └── taffy_ugui.dll
    │   └── ARM64/
    │       └── taffy_ugui.dll
    │
    ├── macOS/
    │   └── libtaffy_ugui.dylib
    │
    ├── Android/
    │   ├── arm64-v8a/
    │   │   └── libtaffy_ugui.so
    │   ├── armeabi-v7a/
    │   │   └── libtaffy_ugui.so
    │   └── x86_64/
    │       └── libtaffy_ugui.so
    │
    ├── iOS/
    │   └── libtaffy_ugui.a / XCFramework packaging as selected
    │
    └── WebGL/
        └── Emscripten-compatible static library / linkage assets
```

Unity plugin importer `.meta` files must be generated/configured and committed so users do not manually configure architectures after installation.

---

## 8. Development Builds vs Release Builds

### Development

During early phases the primary native integration target is Windows x64 because it provides the fastest complete Rust → C ABI → Unity validation loop.

A normal development cycle is:

```text
edit native Rust
    ↓
run native tests
    ↓
build Windows x64 DLL
    ↓
copy/update UnityPackage/Plugins/Windows/x86_64
    ↓
run Unity Edit/Play Mode tests
```

### Release

Release CI builds each advertised platform from a clean checkout, verifies the artifact, then assembles the final UPM package.

Release binaries must not depend on a developer machine's manually copied output.

---

## 9. Build Automation

The repository will converge on one authoritative build entry point:

```text
python build/build.py native host
python build/build.py native windows-x64
python build/build.py native android-arm64
python build/build.py native macos
python build/build.py native ios
python build/build.py native webgl
python build/build.py native all
python build/build.py package
```

The exact CLI may change while implemented, but the required behavior is:

1. resolve the requested target.
2. verify toolchain prerequisites.
3. compile the Rust crate in release mode.
4. verify expected output exists.
5. run the target-compatible ABI smoke test where executable.
6. stage the binary in a deterministic `dist/` location.
7. copy/package it into the correct `UnityPackage/Plugins` location when requested.
8. emit useful failure diagnostics.

Manual binary copying is acceptable only during the earliest bootstrap work and is removed before release hardening.

---

## 10. Platform Toolchain Requirements

### Windows

- Rust MSVC target.
- Visual Studio/MSVC linker toolchain.
- x64 first; ARM64 only when part of supported release matrix.

### Android

- Rust Android targets.
- Unity-compatible Android NDK.
- `cargo-ndk` or an equivalent deterministic configuration.
- ARM64 is the required first Android architecture.

### macOS/iOS

- macOS CI/host.
- Xcode and Apple SDKs.
- separate macOS and iOS Rust targets.
- universal/XCFramework packaging only after individual architecture builds are verified.

### Unity Web/WebGL

WebGL is not treated as an ordinary generic WASM build.

The build must use the Emscripten toolchain compatible with the Unity version being validated. Generic system/latest Emscripten output is not sufficient evidence that the Unity Web target works.

---

## 11. Native ABI Smoke Test

Every shipped artifact must support a minimal independent test:

```text
load library
    ↓
read ABI/build/Taffy versions
    ↓
create context
    ↓
create root + two children
    ↓
set simple Flex row styles
    ↓
set children
    ↓
compute known-size layout
    ↓
read all three layouts
    ↓
assert expected geometry
    ↓
destroy context
```

The smoke test verifies the **compiled binary**, not merely the Rust source tests.

Where a target artifact cannot execute directly on the CI host, perform architecture/symbol/link verification and then run the equivalent smoke test in the target Unity player/device/browser lane.

---

## 12. Phase-by-Phase Native Contract

The Rust library is not built only in a late cross-platform phase. It participates in every phase.

### Phase 0 — Native foundation

Required native outcome:

- Rust crate formats/lints/tests cleanly.
- release host builds succeed.
- stable initial ABI/error/lifetime contract exists.
- native smoke harness exists.
- Windows x64 DLL is produced and independently verified.

### Phase 1 — First Unity vertical slice

Required native outcome:

- the verified Windows DLL is placed in the Unity package.
- Unity's C# wrapper successfully loads it and performs the ABI handshake.
- Unity's first Flex layout is computed by the Rust library, not a C# fallback.

### Phases 2–8 — Feature development

Whenever a phase changes native behavior:

- native tests are extended first or with the implementation.
- host release library is rebuilt.
- ABI compatibility is checked.
- Windows DLL integration is rerun.
- all previously completed native regression cases stay green.

A phase cannot be marked stable if its Unity feature works only because the native library was not rebuilt/tested.

### Phase 9 — Platform expansion

This phase does **not** introduce native compilation for the first time.

It takes the already-working native library and compiles/packages the same ABI for:

- macOS.
- Android.
- iOS.
- Unity Web/WebGL.
- optional additional Windows/Android architectures.

Every artifact receives the same ABI/version/layout validation.

### Phases 10–11 — Optimization and release

- bulk native operations are benchmarked/hardened.
- platform artifacts are rebuilt from clean source.
- checksums/version manifests are generated.
- final UPM package contains the verified binaries.

---

## 13. Source/Binary Version Reproducibility

The project must be able to answer for every binary:

```text
Which TaffyUGUI version produced this?
Which native ABI version is it?
Which Taffy version is inside it?
Which Rust target produced it?
Which source commit produced it?
```

The final pipeline will therefore attach/version at least:

- package version.
- native build version.
- ABI version.
- Taffy version.
- source commit SHA where practical.
- target triple/architecture in build metadata or release manifest.

`Cargo.lock` is committed and release builds use the locked dependency graph.

---

## 14. Definition of Done for the Native Half of v1.0

The Rust/native portion of TaffyUGUI v1.0 is complete only when:

- the Rust source is maintained in the repository.
- Taffy is pinned and dependency resolution is reproducible.
- all required style/layout features are implemented through the native wrapper.
- ABI version/capability/error contracts are stable.
- native tests and golden layout tests pass.
- Windows x64 artifact builds and passes smoke tests.
- supported macOS artifact(s) build and load.
- Android ARM64 builds and loads in a Unity player.
- iOS device build links and runs in the supported Unity workflow.
- Unity Web/WebGL links using the Unity-compatible Emscripten path.
- every shipped native artifact matches the managed ABI.
- native artifacts are produced by CI/build automation.
- the final Unity package includes those artifacts under correct `Plugins` locations.
- plugin importer metadata selects the correct binary automatically.
- the package can be built from a clean clone without relying on untracked local native binaries.

Only after both this native definition of done and the Unity/package definition of done pass is **TaffyUGUI v1.0** complete.

---

## 15. Non-Negotiable Rule

> **No Unity phase is considered technically complete if the Rust implementation required by that phase does not compile, pass its native tests, produce the required native artifact, and successfully execute through the Unity managed/native boundary.**

This rule prevents TaffyUGUI from becoming a C# package scaffold whose actual Rust engine is deferred until the end.
