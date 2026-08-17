# Local Development and Verification

TaffyUGUI uses the local machine as the source of truth for builds and tests. GitHub is a repository mirror, not the build authority.

## Required host tools

- Python 3.10+
- Rust **1.97.1** for canonical development (`rust-toolchain.toml`)
- Rust **1.82.0** for the separate MSRV compatibility check
- `rustfmt` and Clippy
- `cbindgen` **0.29.2**
- Git
- Platform SDK/toolchain required by the target being built

Install cbindgen after Rust is available:

```bash
cargo install cbindgen --version 0.29.2 --locked
```

The build driver checks `.toolchain/bin` before `PATH`, so project-local tools can be used without changing the system installation.

## Canonical local commands

Environment diagnostic:

```bash
python3 build/build.py doctor
```

Format/build/test the permanent native project:

```bash
python3 build/build.py quality
```

Canonicalize Rust formatting and regenerate the public header:

```bash
python3 build/build.py prepare
```

Optional MSRV check:

```bash
python3 build/build.py verify-msrv
```

Final ABI verification on the exact content-addressed project-input snapshot:

```bash
python3 build/build.py verify-abi-final
```

The final ABI gate runs rustfmt, Clippy with warnings denied, the maintained Rust test suite, a host release build, pinned-cbindgen header regeneration/drift verification, and records local evidence for the exact content-addressed project-input snapshot. It never falls back to remote CI.

## Android native release

The active release scope is Android ARM64. After the exact source snapshot passes `verify-abi-final`:

```bash
python3 build/build.py native android-arm64
python3 build/build.py verify-native android-arm64
python3 build/build.py verify-phase4
python3 build/build.py stage-phase5
python3 build/build.py verify-phase5
```

Android uses the pinned NDK r21d baseline (`21.3.6528147`) and API 21 for the native library. Other target definitions remain deferred outside the active release scope.

## Local-only validation experiments

Disposable reproduction programs, device runners, temporary Unity projects, diagnostic executables, and exploratory validation scripts must be created only under ignored `.build/` paths or outside the repository. They are not project source and must not be committed. See `CONTRIBUTING.md`.

## Unity development boundary

Unity uGUI owns rendering and interaction. Do not replace Button, Image, Text/TMP, ScrollRect, EventSystem, prefabs, or existing scripts. `TaffyLayoutGroup` and `TaffyLayoutItem` translate layout inputs to the native `tu_*` ABI and apply computed rectangles through normal uGUI layout APIs.
