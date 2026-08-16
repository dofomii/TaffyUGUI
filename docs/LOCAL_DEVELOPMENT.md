# Local Development and Verification

TaffyUGUI uses the local machine as the source of truth for builds and tests. No GitHub Actions result is required to advance a phase.

## Required host tools

- Python 3.10+
- Rust **1.97.1** for the canonical development gate (`rust-toolchain.toml` pins it)
- The crate MSRV remains **1.82** and should be checked separately when validating compatibility
- `rustfmt` and Clippy
- `cbindgen` **0.29.2**
- Clang/Clang++ for public C/C++ ABI smoke tests
- Git

Install cbindgen after Rust is available:

```bash
cargo install cbindgen --version 0.29.2 --locked
```

The build driver also checks `.toolchain/bin` before `PATH`, so a project-local toolchain may be used without modifying the system installation.

## Canonical local commands

Environment diagnostic:

```bash
python3 build/build.py doctor
```

Provider-independent static gate:

```bash
python3 build/build.py static-gate
```

After installing the pinned local toolchain, canonicalize formatting and the generated header once:

```bash
python3 build/build.py prepare
```

Optional MSRV check:

```bash
python3 build/build.py verify-msrv
```

Full Phase 3 gate:

```bash
python3 build/build.py verify-abi-rc
```

That command requires ABI `1/1` and performs, locally:

1. C11 and C++17 public-header compilation.
2. Static native/managed contract checks.
3. `cargo fmt --check`.
4. `cargo clippy --locked --all-targets -- -D warnings`.
5. `cargo test --locked`.
6. Host release native build.
7. cbindgen regeneration/diff verification.
8. Linked C and C++ smoke executables against the produced host library.

If a prerequisite is absent, the command stops with an installation requirement. It never falls back to a remote CI service.

## Phase 4 platform builds

List the registered targets:

```bash
python3 build/build.py list-targets
```

Build one target:

```bash
python3 build/build.py native <target>
```

The build driver supports Windows x64, macOS arm64/x64 and universal assembly, Android ARM64, iOS ARM64, and WebGL. Platform SDK/toolchain requirements are intentionally strict:

- Android baseline: NDK r21d / revision `21.3.6528147`, API 21.
- WebGL baseline: Emscripten `2.0.19` for the current Unity 2021.3 compatibility target.
- iOS and macOS outputs must be built on macOS with Xcode tooling.
- Windows MSVC output must be built on Windows.

Verified artifacts are staged in `dist/native/<platform>/<architecture>/` with `manifest.json` and `SHA256SUMS`.

## Unity development boundary

Unity uGUI owns rendering and interaction. Do not replace Button, Image, Text/TMP, ScrollRect, EventSystem, prefabs, or existing scripts. `TaffyLayoutGroup` and `TaffyLayoutItem` translate layout inputs to the native `tu_*` ABI and apply computed rectangles using normal uGUI layout APIs.
