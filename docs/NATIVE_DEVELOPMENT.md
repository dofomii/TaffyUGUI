# TaffyUGUI Native Development Guide

This is the clean-clone setup and day-to-day workflow for the Rust/Taffy native library.

The normative engineering decisions live in [PROJECT_DECISIONS.md](PROJECT_DECISIONS.md). The current task and phase gate live in [TASK_TRACKER.md](TASK_TRACKER.md). If this guide conflicts with either document, the normative decision/tracker wins until this guide is corrected.

## 1. Prerequisites

Required for normal host-native development:

- Git
- Rustup
- Python 3

The repository pins the normal development/release Rust toolchain in `rust-toolchain.toml`. Rustup selects it automatically when commands are run from the repository. The project MSRV is Rust 1.82.0 and is verified separately by CI.

Additional platform toolchains such as Visual Studio/MSVC, Xcode, Android NDK, and Unity-matched Emscripten are only required when their platform build phase is active.

`cbindgen` is required when generating/verifying the public C header, but is not required for the normal Phase 0 quality command.

## 2. Clean clone

```bash
git clone https://github.com/dofomii/TaffyUGUI.git
cd TaffyUGUI
```

Verify the selected Rust toolchain:

```bash
rustc --version
cargo --version
```

Normal development should use the repository-pinned toolchain rather than an arbitrary globally selected Rust version.

## 3. Canonical quality command

Run the full local native quality gate from the repository root:

```bash
python build/build.py quality
```

On Windows, if `python` is not registered but the Python launcher is available, use:

```powershell
py -3 build/build.py quality
```

The command performs the authoritative host checks:

1. `cargo fmt --check`;
2. Clippy with warnings denied;
3. Rust tests with `--locked` dependencies;
4. optimized host release build with `--locked` dependencies.

CI runs this same canonical driver on Ubuntu and independently verifies Clippy/tests/release builds on Windows and macOS plus the Rust 1.82 MSRV lane.

## 4. Host release build

To build only the host release native library:

```bash
python build/build.py native host
```

Generated Cargo build output stays under `native/target/` and is not a release package payload.

## 5. Focused Rust commands

During implementation it is fine to run narrower commands for fast iteration:

```bash
cargo check --locked --manifest-path native/Cargo.toml
cargo test --locked --manifest-path native/Cargo.toml
cargo clippy --locked --manifest-path native/Cargo.toml --all-targets -- -D warnings
cargo fmt --manifest-path native/Cargo.toml --all
```

Before a tracked task is marked complete, run the canonical quality command and require CI to pass.

## 6. Dependency and lockfile policy

`native/Cargo.lock` is committed because TaffyUGUI ships native binaries and requires reproducible release dependency resolution.

- Do not delete the lockfile.
- Normal CI/release/platform builds use `--locked`.
- Dependency upgrades must be explicit and reviewed.
- Taffy is exact-pinned in `native/Cargo.toml`.

If a deliberate dependency change modifies the lockfile, commit the manifest and lockfile together.

## 7. Native source ownership

The native crate is organized by responsibility:

```text
native/src/
├── lib.rs          crate/module boundary
├── context.rs      persistent Taffy tree and native context ownership
├── handles.rs      handle model and generation-safe handle work
├── style.rs        C-compatible style data and Taffy conversion
├── grid.rs         Grid-specific authoring/conversion
├── measurement.rs  intrinsic measurement input/result model
├── ffi.rs          exported C ABI only
├── error.rs        native errors and status mapping
└── version.rs      ABI/build/version constants
```

Rules:

- Taffy internals do not cross the public C ABI.
- `ffi.rs` should stay thin and delegate engine behavior to native modules.
- Unity-specific measurement/rendering logic does not belong in Rust.
- Text measurement data is supplied by Unity; native layout must not call back into managed code per node during layout.
- Bootstrap ABI version 0 is transitional and must not be treated as the final ABI contract.

## 8. Public C header

The production public header is generated to:

```text
include/taffy_ugui.h
```

using the repository `cbindgen.toml` configuration:

```bash
python build/build.py header
```

Install `cbindgen` before running that command. Header generation/verification becomes a required gate when the production ABI candidate is implemented.

## 9. Cross-platform builds

All production native build commands remain behind the same canonical entry point:

```text
python build/build.py native windows-x64
python build/build.py native macos
python build/build.py native android-arm64
python build/build.py native ios
python build/build.py native webgl
python build/build.py native all
```

Until the corresponding build phase is implemented, these reserved commands intentionally fail instead of pretending to produce Unity-compatible binaries.

Platform artifacts are only considered valid after architecture/symbol/ABI verification. Unity compatibility is only claimed after the later real Unity player validation phase.

## 10. Unity boundary during native-first development

Existing files under `UnityPackage/Runtime/` are bootstrap scaffolding. Do not expand user-facing Unity features before the task tracker reaches the managed ABI/Unity phases.

The development order remains:

```text
complete native engine
→ production ABI release candidate
→ native verification
→ cross-platform compilation
→ staged native RC payload
→ minimal managed ABI conformance
→ freeze ABI v1 and rebuild native payload
→ user-facing Unity development
```

## 11. Task completion rule

Before marking a native task complete:

- implementation is committed;
- deterministic tests/verification appropriate to the task exist;
- `python build/build.py quality` passes;
- required CI lanes pass;
- `docs/TASK_TRACKER.md` is updated with the new single next task;
- earlier completed phase gates remain green.

Do not advance the phase when a gate is partially passing or when a failure is being ignored as "platform-specific".
