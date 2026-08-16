# Local Verification Status

**Date:** 2026-08-16

This document records only checks executed in the local project environment. Remote CI results are intentionally excluded.

## Passed locally

- `python3 build/build.py static-gate`
- Rust 1.97.1, rustfmt, Clippy, and cbindgen 0.29.2 installed in the ignored project-local `.toolchain` directory.
- Rust source delimiter/integrity sanity pass.
- C# source delimiter/preprocessor sanity pass.
- ABI-v1-RC source lock check (`1/1`).
- Exact Taffy dependency/lock baseline check (`0.13.0`).
- Native `tu_*` export inventory matches the public C header (31 functions on both sides).
- Every exported Rust FFI function has `#[no_mangle]` in the current source inventory.
- Unity P/Invoke uses the current `tu_*` ABI rather than the obsolete bootstrap API.
- C11 public-header compilation with `-Wall -Wextra -Werror`.
- C++17 public-header compilation with `-Wall -Wextra -Werror`.
- Native ABI structure-size probe: `TuValue=16`, `TuGridPlacement=32`, `TuStyle=632`, `TuLayout=48`, `TuGridTrack=72`, `TuGridTemplate=104` on the current x86_64 host.
- Python build/preflight scripts compile successfully.
- Shell helper scripts pass `bash -n`.
- No GitHub Actions workflow exists in the local canonical project.
- `cargo fmt --check` with the pinned Rust toolchain.
- Clippy with `-D warnings`.
- Full Rust test inventory: 44 tests passed.
- Host release build of `libtaffy_ugui.so`.
- Pinned cbindgen public-header regeneration/drift check.
- Linked C11 and C++17 ABI smoke programs against the host library.

## Not executable on this host yet

The local host does not have the Phase 4 Android/WebGL SDKs or the non-Linux artifact hosts. Therefore the following checks are deliberately **not claimed as passed here**:

- Android ARM64 build: requires Android NDK r21d (`21.3.6528147`), API 21, and the Rust target.
- WebGL build: requires Emscripten `2.0.19` and the Rust target.
- Windows x64, macOS, and iOS builds: require their assigned Windows/macOS hosts.
- Unity Editor compilation / EditMode / PlayMode tests.

The completed canonical local Phase 3 command is:

```bash
python3 build/build.py verify-abi-rc
```

On Windows, use the corresponding PowerShell bootstrap/verify scripts. The build driver refuses to fall back to a remote CI service.
