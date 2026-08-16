# Local Verification Status

**Date:** 2026-08-16

This document records only checks executed in the local project environment. Remote CI results are intentionally excluded.

## Passed locally

- `python3 build/build.py static-gate`
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

## Not executable in this sandbox

The sandbox currently has no `cargo`, `rustc`, `rustup`, `rustfmt`, Clippy, `cbindgen`, Unity Editor, or C# compiler. Therefore the following checks are deliberately **not claimed as passed here**:

- `cargo fmt --check`
- Clippy with warnings denied
- Rust unit/integration tests
- Rust release build
- cbindgen regeneration/drift check
- linked C/C++ smoke execution against the built Rust shared library
- Unity Editor compilation / EditMode / PlayMode tests
- Phase 4 platform SDK builds

The canonical local command for the compiled Phase 3 gate is:

```bash
scripts/bootstrap-local-toolchain.sh
python3 build/build.py prepare
python3 build/build.py verify-abi-rc
```

On Windows, use the corresponding PowerShell bootstrap/verify scripts. The build driver refuses to fall back to a remote CI service.

## Phase 4 pipeline hardening completed locally

The local build driver now additionally enforces:

- clean-source Phase 3 evidence before any Phase 4 build;
- exact source-tree matching between evidence and artifacts;
- canonical local host ownership for Windows/macOS/Linux target families, enforced by target host restrictions;
- the complete 31-function public `tu_*` export set for every built artifact;
- manifest schema 2 with artifact size, checksum, source revision/tree fingerprint, export fingerprint, target-specific architecture evidence, and toolchain evidence;
- exact `SHA256SUMS` validation;
- Windows PE/x64 proof even without `file`;
- iOS device-ARM64 `lipo` evidence;
- WebGL `emar`/`emnm` plus Wasm/LLVM-bitcode archive-member evidence;
- Android toolchain evidence without leaking machine-local NDK paths;
- macOS universal architecture/export evidence;
- a build-driver contract self-test covering target assignment completeness and malformed iOS/WebGL evidence rejection;
- a final `verify-phase4` gate that requires the complete multi-host artifact set and writes `dist/native/phase4-index.json` only after all target manifests agree.

`python3 build/build.py static-gate` passes after these changes. The Phase 4 artifact tasks remain unchecked until the required Rust/platform toolchains can actually execute the builds locally.
