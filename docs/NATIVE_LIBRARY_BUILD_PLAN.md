# TaffyUGUI — Rust Native Library Build and Packaging Plan

**Status:** Active engineering contract  
**Repository:** `dofomii/TaffyUGUI`  
**Native crate:** `native/` / `taffy_ugui_native`  
**Library:** `taffy_ugui`  
**Unity package:** `UnityPackage/`  
**Master plan:** [DEVELOPMENT_PLAN.md](DEVELOPMENT_PLAN.md)  
**Tracker:** [TASK_TRACKER.md](TASK_TRACKER.md)

---

# 1. Native-First Product Rule

The Rust library is developed to feature completion, verified, cross-compiled, and staged for Unity **before active Unity feature development begins**.

Required order:

```text
Rust project
    ↓
Taffy setup
    ↓
full native layout engine
    ↓
production C ABI
    ↓
native tests + ABI freeze
    ↓
Windows/macOS/Android/iOS/WebGL builds
    ↓
UnityPackage/Plugins staging
    ↓
NATIVE MILESTONE COMPLETE
    ↓
Unity managed/runtime/editor development
```

Existing C# files are bootstrap scaffolding until this gate passes.

---

# 2. Native Source Ownership

Canonical source:

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

Ownership boundaries:

- `native/` — Rust/Taffy implementation and C ABI.
- `dist/native/` — generated/staged build outputs.
- `UnityPackage/Plugins/` — verified native artifacts shipped to Unity users.
- `UnityPackage/Runtime/Native/` — later managed wrapper; it does not own layout logic.

---

# 3. Library Output Contract

The crate emits both:

```toml
[lib]
name = "taffy_ugui"
crate-type = ["cdylib", "staticlib"]
```

Expected target-family outputs:

| Target family | Primary artifact |
|---|---|
| Windows x64 | `taffy_ugui.dll` |
| macOS Apple Silicon / Intel as supported | `libtaffy_ugui.dylib` / universal packaging |
| Android ARM64 | `libtaffy_ugui.so` |
| iOS ARM64 | `libtaffy_ugui.a` or selected XCFramework packaging |
| Unity Web/WebGL | Unity-compatible Emscripten static/linkage artifact |

Additional Windows ARM64, Android ARMv7/x86_64 and simulator slices are included only when part of the validated compatibility matrix.

---

# 4. Full Native Feature Scope Before Unity

The native library must expose the intended v1 behavior before Unity integration begins:

## Tree/context

- persistent Taffy tree.
- context create/destroy/clear.
- generation-safe opaque node handles.
- create/remove nodes.
- set children/topology.
- dirty state.

## Core style

- display.
- logical length/percent/auto sizing.
- min/max.
- aspect ratio.
- margin/padding/layout border.
- relative/absolute position and insets.
- direction.
- overflow/scrollbar reservation where supported.

## Flexbox

- directions/reverse.
- wrap.
- grow/shrink/basis.
- gap.
- alignment/justification.

## Block

- supported Block layout behavior.

## Grid

- explicit/implicit tracks.
- fixed/percent/auto/fr/minmax/repeat where supported by the pinned Taffy baseline.
- auto flow.
- placement/spans.
- alignment/gap.
- variable-length Grid resource lifetime.

## Measurement

- managed-supplied intrinsic measurement records.
- known/available size data required by Taffy.
- cached measurement updates.
- no per-measurement Rust→C# callback requirement.

## Production transfer

- bulk style uploads.
- bulk measurement uploads.
- one compute call per root/layout generation.
- bulk result retrieval.

---

# 5. ABI Contract

The C ABI is owned by TaffyUGUI and isolated from Taffy's Rust API.

Conceptual exports:

```text
tu_get_abi_version
tu_get_taffy_version
tu_get_build_version
tu_get_capabilities

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

Rules:

- `#[repr(C)]` POD structs.
- fixed-width numeric types.
- explicit enum numbers.
- no ABI `bool`.
- no Rust-owned `Vec`, `String`, reference or Taffy ID across FFI.
- stable numeric errors.
- last-error diagnostics.
- stale handle detection.
- documented `# Safety` for unsafe exports.
- no Rust unwind across FFI.

ABI v1 is frozen only after native verification passes.

---

# 6. Native Quality and Verification Pipeline

Every native change must eventually satisfy:

```text
cargo fmt --check
cargo clippy --all-targets -- -D warnings
cargo test
cargo build --release
native golden tests
ABI contract tests
compiled-artifact smoke test
```

The artifact smoke test performs:

```text
load artifact
→ query ABI/build/Taffy/capabilities
→ create context
→ create known tree
→ upload style/topology
→ compute layout
→ bulk read layouts
→ assert geometry
→ destroy context
```

Invalid/stale handle and malformed-input tests are mandatory before ABI freeze.

---

# 7. Authoritative Cross-Platform Build Pipeline

Target command model:

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

For each target the driver must:

1. verify prerequisites.
2. select the correct Rust target/toolchain.
3. use the locked dependency graph.
4. build release output.
5. verify file/architecture/export symbols.
6. run ABI smoke test when executable on the host.
7. perform static/link verification otherwise.
8. place output under deterministic `dist/native/...`.
9. record version/ABI/Taffy/target/source metadata.
10. generate checksum.

---

# 8. Platform Build Requirements

## Windows

Primary:

```text
x86_64-pc-windows-msvc
```

Requirements:

- MSVC-compatible build.
- correct exported C ABI symbols.
- DLL smoke test on Windows CI.

Optional ARM64 is added only when supported/advertised.

## macOS

Required Apple Silicon lane; Intel/universal lane according to compatibility matrix.

Verify Mach-O slices and symbols before staging.

## Android

Primary required architecture:

```text
arm64-v8a / aarch64-linux-android
```

Use a Unity-compatible Android NDK. Additional ABIs are deliberate compatibility lanes, not assumed.

## iOS

Primary:

```text
aarch64-apple-ios
```

Produce static library or selected XCFramework form compatible with the Unity→Xcode build path. Simulator slices are added as required by the supported workflow.

## Unity Web/WebGL

Do not use generic WASM compatibility as evidence.

Build/link using an Emscripten toolchain compatible with the Unity version under validation. Final runtime proof occurs in the later Unity Web player validation phase.

---

# 9. Deterministic Artifact Layout

Generated staging example:

```text
dist/native/
├── windows/x86_64/taffy_ugui.dll
├── macos/arm64/libtaffy_ugui.dylib
├── android/arm64-v8a/libtaffy_ugui.so
├── ios/arm64/libtaffy_ugui.a
└── webgl/... linkage artifact
```

Each build set also produces a manifest containing at least:

```text
package/native version
ABI version
Taffy version
Rust target triple
source commit
artifact filename
checksum
```

---

# 10. Unity Plugin Staging

After all required native target-family artifacts compile and verify, stage them into:

```text
UnityPackage/Plugins/
├── Windows/
├── macOS/
├── Android/
├── iOS/
└── WebGL/
```

Plugin importer `.meta` files must be committed/configured so Unity chooses the correct platform/CPU automatically.

Manual binary copying is not a release workflow.

The `stage-unity` build task must be able to rebuild/refresh the plugin payload deterministically.

---

# 11. Native Milestone Gate

Active Unity feature development starts only when:

- Rust project quality CI is green.
- full native v1 feature surface is implemented.
- full feature surface is exposed through the C ABI.
- native golden/ABI/safety tests are green.
- compiled host artifacts pass smoke tests.
- ABI v1 is frozen.
- Windows required artifact builds.
- macOS required artifact builds.
- Android ARM64 builds.
- iOS ARM64 builds.
- Unity Web/WebGL artifact builds through the selected Unity-compatible toolchain strategy.
- artifacts are staged into `UnityPackage/Plugins`.
- importer metadata is committed.
- manifest/checksums are generated.
- clean source can reproduce the outputs.

This gate is the handoff from **native engine development** to **Unity product integration**.

---

# 12. Later Unity Validation Does Not Replace Native Compilation

The initial native milestone proves:

- source correctness at the native level.
- ABI correctness.
- target compilation/link compatibility.
- artifact structure.

Later Unity platform validation proves the final missing layer:

```text
Unity build system
→ plugin importer/linker
→ managed P/Invoke / __Internal
→ actual player/device/browser runtime
```

A platform is not publicly advertised until both native compilation and Unity runtime validation pass.

---

# 13. Release Reproducibility

For every shipped binary the project must answer:

```text
What TaffyUGUI version produced it?
What ABI version is it?
What Taffy version is embedded?
What target triple/architecture produced it?
What source commit produced it?
What checksum identifies it?
```

Final release CI rebuilds artifacts from clean source; it never depends on a developer workstation's untracked outputs.

---

# 14. Native Definition of Done for v1.0

The native half is complete only when:

- canonical Rust source is in the repository.
- dependency graph is reproducible.
- full intended v1 Flex/Grid/Block/core style behavior is implemented.
- measurement ingestion is implemented.
- stable ABI/version/capability/error contracts exist.
- safety/golden/ABI/smoke tests pass.
- required platform-family artifacts compile.
- Unity-compatible target toolchains are documented/reproducible.
- artifacts are automatically staged in the UPM package.
- importer metadata and checksums are correct.
- clean clone can regenerate the package native payload.
- later Unity platform validation confirms all publicly advertised platforms at runtime.

Only after both the native and Unity definitions of done pass is TaffyUGUI v1.0 complete.
