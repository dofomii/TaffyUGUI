# TaffyUGUI Production C ABI Candidate

This document describes the production C ABI implemented by `native/src/ffi.rs` and mirrored by the authoritative cbindgen output in `include/taffy_ugui.h`.

The complete Phase 3 native verification gate has passed and the interface is locked as **ABI-v1-RC (version 1, stage 1)** for cross-platform native compilation. This is still not the final ABI v1 promise: Phase 6 managed conformance must prove the P/Invoke binary contract before final ABI v1 is frozen and every native artifact is rebuilt.

## Binary rules

- Every exported symbol uses the `tu_*` prefix and the C calling convention.
- Persistent context, node, and resource identities are opaque `uint64_t` generation-safe handles.
- Counts and capacities are `uint32_t`.
- Status and enum values use explicit `int32_t` numeric values.
- Geometry values use 32-bit `float`.
- Temporary arrays are caller-owned `pointer + uint32_t count/capacity` buffers.
- No Rust pointer, `usize`, Rust `bool`, `Vec`, `String`, reference, or Taffy `NodeId` is a persistent ABI value.
- ABI booleans are `uint8_t` and accept only `0` or `1`.

## ABI-v1-RC version handshake

`tu_get_abi_version()` reports `1` and `tu_get_abi_stage()` reports `1`, identifying the verified **ABI-v1-RC** contract used by Phase 4 platform builds. Callers must also inspect `tu_get_capabilities()` before assuming an optional feature is available.

The RC lock means all Phase 4 artifacts must expose this same binary contract. Final ABI v1 remains owned by Phase 6 managed conformance and the subsequent full native rebuild.

## Error contract

Every fallible `tu_*` operation returns a `TuStatus` numeric value. Expected invalid input is converted to a status rather than panicking.

The last diagnostic for the calling thread is available through:

- `tu_get_last_error_length()`;
- `tu_copy_last_error(...)`.

The diagnostic string is supplementary. Callers should branch on the stable numeric status rather than parse diagnostic text.

On targets where unwinding is enabled, the candidate exports use a common `catch_unwind` guard so an unexpected Rust unwind does not cross the C boundary. Target-specific abort-only behavior remains a later platform-build concern and must be reflected by build/capability metadata before release.

## Thread ownership

Contexts live in a thread-local native registry because the selected Taffy 0.13 configuration is not treated as `Send`/`Sync`.

A context must be created, used, and destroyed on its owner thread. A valid context handle used from another thread returns `TuStatus_WrongThread`; it is never rebound to a same-index context on that other thread.

## Buffer ownership

Input buffers and string views are borrowed only for the duration of a call. The native library does not retain caller array pointers or string pointers.

For input arrays:

- `count == 0` permits a null pointer;
- `count > 0` requires a non-null pointer to at least `count` initialized entries.

For output arrays:

- capacity must be large enough for the requested result;
- a non-zero required output requires a non-null output pointer;
- `out_written` must point to writable `uint32_t` storage.

## Style values

`TuValue` is a typed length descriptor. The permitted kinds depend on the destination field:

- dimensions and flex basis: Auto, Length, Percent, Calc;
- margin/inset: Auto, Length, Percent, Calc;
- padding/border/gap: zero/Auto-as-zero, Length, Percent, Calc.

Length-like values that must be non-negative are validated before entering Taffy. Invalid enums, non-finite values, malformed booleans, and invalid handles return defined errors.

## Measurement records

Measurement data is uploaded and cached; Rust never performs a synchronous managed callback per measured node.

A `TuMeasurement` contains min-content, max-content, preferred dimensions, optional intrinsic aspect ratio/replaced-element metadata, and optional width-dependent samples. Nested sample storage is caller-owned only for the duration of the upload call.

Passing a null measurement pointer to `tu_node_set_measurement` clears the cached record.

## Calc resources

Calc is represented as typed native resources, never CSS strings. `TuCalcSpec` supports length, percent, add, subtract, scale, min, max, and clamp expressions.

Composite expressions may reference only currently live Calc handles from the same context. A Calc resource that is referenced by another active Calc expression cannot be removed first.

**Lifetime rule:** callers must not remove a Calc resource while a node style or Grid track still refers to that resource. The opaque pointer used internally by Taffy remains memory-safe after removal, but the removed resource no longer has valid layout semantics.

## Grid resources

`TuGridTemplate` uploads explicit/implicit tracks, repeat definitions, named lines, and named areas. Track descriptors support auto, length, percentage, fraction, minmax, min-content, max-content, Calc, and repeat forms. Min/max components can themselves use Calc handles where Taffy supports them.

Detailed Grid output is queried after layout through the Grid summary, track-size, gutter, and item-placement calls.

## Layout computation

`tu_compute_layout(context, root, width, height)` performs one native layout computation for the requested root/available-size generation. Finite non-negative values are definite available space; positive infinity represents max-content available space. NaN, negative values, and negative infinity are rejected.

Results are copied through `tu_get_layout` or `tu_get_layouts_bulk` and include geometry plus content and scroll extents.

## Header ownership

`cbindgen.toml` contains an explicit production allowlist and `python build/build.py header` is the authoritative header-generation command.

The checked-in `include/taffy_ugui.h` is authoritative cbindgen output. CI installs pinned cbindgen `0.29.2`, regenerates the header, fails on drift, and compiles the consumer surface as both C11 and C++17 before the ABI-RC gate can pass.

## Phase 3 verification and RC lock

Phase 3 adds a single canonical candidate-verification command:

```bash
python build/build.py verify-abi-rc
```

It runs all Phase 1–3 static preflights, rustfmt, Clippy with warnings denied, locked Rust tests (including golden geometry, ABI layout/numeric contracts, malformed-input and lifecycle stress coverage), a locked release build, cbindgen regeneration/diff verification, and linked C11/C++17 smoke executables against the compiled host shared library.

The Linux CI lane installs pinned `cbindgen 0.29.2` and runs that same command. Windows/macOS and Rust 1.82 lanes remain independent regression coverage.

The source continues to report ABI version/stage `0/0` until this complete Phase 3 gate passes. Only after that verified gate may the constants be changed to ABI version/stage `1/1` and described as **ABI-v1-RC**. Final ABI v1 is still reserved for Phase 6 managed conformance and the subsequent full native rebuild.
