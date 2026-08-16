# Phase 0 — Rust Project and Toolchain Foundation

**Implementation:** COMPLETE
**Phase gate:** COMPLETE

## Goal

Create a deterministic, production-oriented native Rust foundation that can evolve into a cross-platform Unity plugin without coupling Taffy internals directly to Unity.

## Delivered

- Rust workspace and native crate layout.
- Exact Taffy baseline pinned to `0.13.0`.
- `cdylib` and `staticlib` native outputs.
- Project MSRV established as Rust `1.82.0`.
- Canonical development/release toolchain pinned to Rust `1.97.1`.
- Cargo lockfile committed for deterministic dependency resolution.
- Production module split:
  - `calc.rs`
  - `context.rs`
  - `error.rs`
  - `ffi.rs`
  - `grid.rs`
  - `handles.rs`
  - `measurement.rs`
  - `style.rs`
  - `version.rs`
- Local-first build/verification driver under `build/build.py`.
- Public-header generation configuration via `cbindgen.toml`.
- C/C++ smoke-test source inventory.
- Local toolchain/bootstrap scripts.

## Architectural decisions established in this phase

- Native library computes geometry only; Unity remains responsible for rendering, input, EventSystem, animation, prefabs, and existing uGUI behavior.
- Native state is persistent rather than rebuilding Taffy from scratch for every query.
- The public ABI is a stable C surface and must not expose Rust/Taffy implementation types directly.
- Fixed-width types are required at the ABI boundary.
- User-facing Unity feature development remains gated behind the native payload and managed ABI validation sequence.

## Completion evidence

The current repository structure retains all Phase 0 outputs and the provider-independent local static gate passes.

Phase 0 does not need to be reopened unless a future toolchain/compatibility change deliberately changes the established baseline.
