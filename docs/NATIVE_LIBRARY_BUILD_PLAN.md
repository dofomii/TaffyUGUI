# TaffyUGUI — Final Native Library Build and Packaging Plan

**Repository:** `dofomii/TaffyUGUI`  
**Native crate:** `native/` / `taffy_ugui_native`  
**Library:** `taffy_ugui`  
**Normative decisions:** [PROJECT_DECISIONS.md](PROJECT_DECISIONS.md)  
**Master plan:** [DEVELOPMENT_PLAN.md](DEVELOPMENT_PLAN.md)  
**Tracker:** [TASK_TRACKER.md](TASK_TRACKER.md)

This document defines how the Rust library becomes the verified native payload shipped inside the Unity package.

---

# 1. Native-first rule

The native engine is developed to feature completion before user-facing Unity layout work.

Final sequence:

```text
Rust/Taffy engine
    ↓
production C ABI candidate
    ↓
generated C header + native verification
    ↓
ABI-v1-RC
    ↓
compile Windows/macOS/Android/iOS/WebGL RC artifacts
    ↓
stage RC payload in Unity package structure
    ↓
minimal managed ABI conformance
    ↓
freeze ABI v1
    ↓
rebuild + reverify + restage every native target
    ↓
FINAL NATIVE PAYLOAD
    ↓
user-facing Unity layout development
```

Managed ABI conformance is intentionally narrow and exists only to prove binary compatibility before the permanent ABI freeze.

---

# 2. Fixed native baseline

- Taffy: exact `0.13.0`.
- Rust MSRV: `1.82.0`.
- pinned normal/release Rust: `1.97.1`.
- committed `native/Cargo.lock`.
- crate types: `cdylib` and `staticlib`.
- primary Unity compatibility baseline: Unity 2021.3 LTS.

Enabled Taffy features:

```text
std
taffy_tree
flexbox
grid
block_layout
float_layout
calc
content_size
detailed_layout_info
```

The runtime ABI does not depend on Taffy serde or CSS-string parsing.

---

# 3. Native source ownership

Canonical native source:

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
    ├── calc.rs
    ├── measurement.rs
    ├── error.rs
    └── version.rs
```

Additional ownership:

```text
include/taffy_ugui.h     generated public C header
build/build.py            authoritative build entry point
dist/native/...           generated/ignored build staging
UnityPackage/Plugins/...  verified native payload shipped to users
```

`scripts/build-native.sh` is only a bootstrap compatibility wrapper and is not an authoritative platform builder.

---

# 4. Native feature completeness before Unity features

The native engine must implement the intended Taffy 0.13 layout surface required by v1 before the ABI release candidate is locked.

### Core

- display/box generation;
- box sizing;
- direction;
- overflow/scrollbar reservation;
- relative/absolute positioning/insets;
- Auto/Length/Percent;
- typed Calc representation/resource model;
- size/min/max/aspect ratio;
- margin/padding/border geometry;
- content-size metadata.

### Flexbox

- row/column/reverse;
- wrap variants;
- grow/shrink/basis;
- align-items/self/content;
- justify-content;
- gap.

### Block / FlowRoot / Float

Expose the geometry-capable behavior supported by the selected Taffy feature set.

### Grid

- explicit/implicit tracks;
- fixed/percent/auto/fr/minmax/repeat;
- auto tracks and auto-flow;
- placement/spans;
- align/justify content/items/self;
- named lines;
- named template areas;
- detailed layout information for diagnostics where useful.

### Measurement

Native cached measurement records accept caller-supplied known/available/intrinsic information. Rust must not require a synchronous per-node managed callback during layout.

### Transfer

Bulk style, measurement, and result paths are production requirements. Topology batching is added where design/profiling demonstrates value.

---

# 5. Production ABI contract

The final public ABI uses:

- C ABI exports;
- fixed-width integer fields;
- `float`/Rust `f32` layout values;
- explicit numeric enum values;
- opaque generation-safe `uint64_t` context/node/resource handles;
- pointer + `uint32_t` count for temporary caller-owned arrays;
- stable status/error values;
- last-error diagnostics.

It does not expose persistent raw Rust pointers, `usize`, Rust `bool`, Taffy IDs, Rust references, `Vec`, or `String`.

Conceptual API families:

```text
version / build / Taffy / capabilities
context create / destroy / clear
node/resource create / remove
children/topology
styles
measurements
dirty marking
compute
single/bulk result retrieval
errors/diagnostics
```

Exact names become canonical `tu_*` exports during the ABI candidate phase.

---

# 6. Generated C header

The public C header is generated from the Rust FFI surface with cbindgen:

```text
python build/build.py header
    ↓
include/taffy_ugui.h
```

`cbindgen.toml` is committed.

CI eventually regenerates the header and fails if the generated result differs from the committed file.

The C/C++ smoke harness compiles against this exact generated header; there is no independently hand-maintained header contract.

---

# 7. Panic and error behavior

Expected invalid inputs are status-code errors.

Before crossing into Taffy/native state, validate:

- handles/context ownership;
- pointers and counts;
- enum ranges;
- dimensions and finite-number requirements;
- resource lifetimes;
- capabilities.

No Rust unwind may cross the C boundary.

Where a target supports and validates unwinding, exports use a common `catch_unwind` boundary for unexpected internal panics. Where a Unity-compatible target is built abort-only, build/capability metadata must reflect that limitation. Callers must never depend on panic recovery for normal control flow.

The repository does not globally force `panic = "abort"`; target-specific behavior belongs in platform build configuration.

---

# 8. Quality pipeline

Current Phase 0 quality command:

```text
python build/build.py quality
```

Equivalent required checks:

```text
cargo fmt --check
cargo clippy --locked --all-targets -- -D warnings
cargo test --locked
cargo build --locked --release
MSRV check/test at Rust 1.82.0
```

Later native gates add:

- golden layout tests;
- ABI struct/enum tests;
- malformed-input/stale-handle tests;
- C header consistency;
- compiled-artifact smoke tests;
- topology/lifecycle stress tests.

No native phase is stable while its required checks are red.

---

# 9. ABI release-candidate verification

Before platform compilation, verify host artifacts through:

```text
query versions/capabilities
create context
create root/children
upload style/topology/measurement data
compute known layout
bulk read geometry/content results
assert expected results
destroy context
```

This test must use the compiled artifact, not only call Rust functions internally.

When the native engine, generated header, golden/safety/ABI tests, and host smoke harness pass, the interface is designated **ABI-v1-RC**.

That RC is stable enough for target-family compilation but is not yet the permanent ABI v1 promise.

---

# 10. Authoritative build interface

All production builds converge on:

```text
python build/build.py quality
python build/build.py header
python build/build.py native host
python build/build.py native windows-x64
python build/build.py native macos
python build/build.py native android-arm64
python build/build.py native ios
python build/build.py native webgl
python build/build.py native all
python build/build.py stage-unity
python build/build.py package
```

Target commands are implemented phase-by-phase behind this stable interface.

For each target the build driver must:

1. verify required tools/SDKs;
2. select exact target/toolchain;
3. use locked dependencies;
4. compile release output;
5. verify artifact format/architecture;
6. verify exported symbols/version metadata;
7. run smoke tests where host-runnable;
8. perform static/link checks where not directly runnable;
9. stage deterministic output;
10. emit manifest/checksum information.

---

# 11. Target-family build contract

## Windows

Primary v1 target:

```text
x86_64-pc-windows-msvc
→ taffy_ugui.dll
```

Validate on Windows CI with the compiled-library smoke harness. Windows ARM64 is optional until included in the support matrix.

## macOS

Build Apple Silicon and the Intel slice retained by the compatibility matrix. Validate individual Mach-O architectures before optionally assembling a universal binary.

## Android

Primary:

```text
aarch64-linux-android
→ arm64-v8a/libtaffy_ugui.so
```

Use Unity 2021.3-compatible Android NDK r21d (`21.3.6528147`), not an arbitrary newest NDK. ARMv7/x86_64 are optional lanes.

## iOS

Primary:

```text
aarch64-apple-ios
```

Produce the static form that best survives actual Unity→Xcode validation: raw `.a` or XCFramework as selected by evidence. Simulator slices are optional development lanes.

## WebGL

Use the Emscripten toolchain bundled with/matched to the Unity 2021.3 lane, with 2.0.19 as the baseline. Generic system WASM/Emscripten output is not accepted as Unity compatibility proof.

---

# 12. Deterministic build staging

Generated native outputs go under:

```text
dist/native/
├── windows/x86_64/
├── macos/...
├── android/arm64-v8a/
├── ios/arm64/
└── webgl/...
```

Each artifact set carries a manifest with at least:

```text
package/native version
ABI designation/version
Taffy version
Rust target triple
source commit
artifact filename
architecture
checksum
panic strategy/capability where relevant
```

`dist/` is ignored and never acts as release source.

---

# 13. Unity plugin staging

ABI RC artifacts are staged into the final package shape before managed conformance:

```text
UnityPackage/Plugins/
├── Windows/
├── macOS/
├── Android/
├── iOS/
└── WebGL/
```

The staging operation must:

- copy only verified artifacts;
- create/maintain correct Unity importer `.meta` files;
- include native artifact manifest/checksums;
- be reproducible from clean source;
- avoid hand-copied hidden dependencies.

This RC payload allows the minimal managed conformance layer to use the same package structure that the final Unity product will use.

---

# 14. Final ABI v1 freeze and rebuild

After the ABI RC exists across target families, implement a minimal Unity managed conformance layer that proves:

- P/Invoke names and signatures;
- fixed-width struct packing/size/alignment;
- enum numeric values;
- Cdecl usage on shared-library platforms;
- `__Internal` strategy for static-link platforms;
- context lifecycle;
- version/capability handshake;
- temporary buffer ownership;
- error translation;
- one real Unity 2021.3 Editor/native smoke flow.

Only then:

1. assign final **ABI v1**;
2. rebuild every required target from clean source;
3. rerun target verification;
4. regenerate checksums/manifests;
5. restage every Unity plugin artifact;
6. commit the final ABI v1 plugin payload.

This rebuild is mandatory. No ABI-RC binary is shipped as v1 merely because its signatures happened not to change.

---

# 15. Release binary policy

Normal source development uses generated `dist/` outputs.

The installable UPM package is self-contained:

- verified native binaries under `UnityPackage/Plugins` are committed once the native artifact stages are active;
- matching Unity `.meta` importer files are committed;
- release tags contain the verified native payload;
- users installing a tagged `?path=/UnityPackage` Git URL do not need Rust or access to GitHub Actions artifacts;
- release archives/CI artifacts are additional delivery mechanisms.

Release CI rebuilds every advertised artifact and verifies that the package payload matches generated manifests/checksums.

---

# 16. Final native definition of done

The native half of TaffyUGUI v1 is complete only when:

- Taffy 0.13 native feature surface promised by v1 is implemented;
- Rust source and lockfile are reproducible;
- generated public C header is deterministic;
- final ABI v1 uses fixed-width safe handles/types and stable error/version contracts;
- golden/safety/ABI/header/smoke tests pass;
- required Windows/macOS/Android/iOS/WebGL artifacts build through the defined Unity-compatible toolchains;
- artifacts are rebuilt after final ABI v1 freeze;
- manifests/checksums identify every binary;
- `UnityPackage/Plugins` contains the verified final artifacts/import metadata;
- clean source can regenerate the payload;
- later Unity player validation proves every publicly advertised target at runtime.

Only after both native and Unity definitions of done pass is TaffyUGUI v1.0 complete.
