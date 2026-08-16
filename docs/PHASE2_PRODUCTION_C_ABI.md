# Phase 2 — Production C ABI Candidate and Safety Boundary

**Implementation:** COMPLETE
**ABI state:** promoted to ABI-v1-RC (`1/1`) after the candidate verification work
**Current local re-verification:** pending through the Phase 3 compiled gate

## Goal

Expose the complete native engine through a deterministic, fixed-width C ABI suitable for P/Invoke and all target architectures.

## Delivered ABI contract

The production public surface uses `tu_*` symbols and currently contains **31 exported functions**.

### Fixed-width boundary

- context handle: `uint64_t`;
- node handle: `uint64_t`;
- resource handle: `uint64_t`;
- collection counts/capacities: `uint32_t`;
- status/enumeration values: fixed 32-bit integer representation;
- layout scalar values: `float` / Rust `f32`;
- bool-like ABI values: `uint8_t`.

No persistent public handle is a raw Rust pointer.

## API families implemented

- ABI/build/capability/version queries;
- thread-local last-error diagnostics;
- context create/destroy/clear;
- node create/remove;
- single and bulk style upload;
- single and bulk topology upload;
- dirty mark/query;
- single and bulk cached measurement upload;
- Calc resource create/remove;
- Grid template upload;
- detailed Grid information/track/gutter/item retrieval;
- layout computation;
- single and bulk layout retrieval.

## Error and panic boundary

Expected failures are represented by explicit status codes such as invalid context/node/resource, malformed values/counts, capacity exhaustion, wrong-thread use, and native engine failure.

Exported calls are protected by the native panic guard so Rust panics are not intended to unwind across the C boundary.

## Public header

`include/taffy_ugui.h` is the public C/C++ consumer contract generated from the Rust ABI definition through pinned cbindgen configuration.

The local static gate currently proves that:

- the Rust export inventory and header function inventory both contain 31 functions;
- the header compiles as C11 and C++17 with warnings treated as errors;
- current ABI structure sizes agree with the managed/native declarations on the local x86_64 host.

## Managed bridge work completed early

Although final managed conformance is formally Phase 6, the low-level Unity P/Invoke declarations were migrated early from the old `taffy_ugui_*` bootstrap API to the production `tu_*` ABI-v1-RC surface. This is tracked as **early scaffolding**, not as completion of Phase 6.
