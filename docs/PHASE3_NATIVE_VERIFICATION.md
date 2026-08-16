# Phase 3 — Native Verification and ABI Release-Candidate Lock

**Implementation:** COMPLETE
**ABI source state:** ABI-v1-RC (`version=1`, `stage=1`)
**Canonical local gate:** COMPLETE on the local Linux host

## Goal

Prove that the native engine and C ABI are stable enough to become the release-candidate contract used for all cross-platform native builds.

## Verification infrastructure delivered

- Rust unit tests;
- native integration/golden tests;
- Flex geometry verification;
- Block/FlowRoot/Float geometry verification;
- Grid named-line/area/placement verification;
- Calc and cached measurement verification;
- ABI size/alignment/offset assertions;
- enum numeric-contract assertions;
- invalid/stale/cross-context/malformed input tests;
- wrong-thread access test;
- repeated lifecycle/topology stress test;
- public-header C11 smoke source;
- public-header C++17 smoke source;
- cbindgen regeneration/drift check;
- linked host C/C++ smoke execution path;
- one canonical local verification command.

## Canonical Phase 3 gate

```bash
python3 build/build.py verify-abi-rc
```

The command requires and performs:

1. provider-independent static preflight;
2. C11/C++17 public-header compilation;
3. `cargo fmt --check`;
4. Clippy with warnings denied;
5. Rust unit/integration tests;
6. host release native build;
7. cbindgen header regeneration/diff verification;
8. linked C and C++ smoke execution against the produced native library;
9. local Phase 3 evidence recording for the exact clean Git tree.

## Local verification result

```bash
python3 build/build.py static-gate
```

passes, including the public-header C/C++ compile and Phase 4 build-driver contract tests. The complete compiled Phase 3 suite also passes: `cargo fmt --check`, Clippy with warnings denied, 44 Rust tests, release build, pinned-cbindgen header drift verification, and linked C/C++ smoke execution.

## Gate rule

Phase 4 artifact production is not accepted merely because the ABI source says `1/1`. Every artifact-producing local machine must first run the complete Phase 3 gate successfully on the exact clean source tree and produce matching Phase 3 evidence.

This is the current authoritative project boundary.
