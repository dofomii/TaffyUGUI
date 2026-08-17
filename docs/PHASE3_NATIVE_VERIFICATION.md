# Phase 3 — Native Verification and ABI Release-Candidate Lock

**Implementation:** COMPLETE
**Historical gate:** ABI-v1-RC `1/1` completed locally
**Current source:** final ABI v1 `1/2`

## Goal

Establish a maintainable native verification baseline before producing release artifacts.

## Permanent verification retained in the project

- Rust unit and integration/golden tests for Flex, Block/FlowRoot/Float, Grid, Calc, measurement, handles, lifecycle, invalid input, wrong-thread access, and repeated topology/lifecycle stress.
- ABI size/alignment/offset and enum numeric-contract assertions.
- Thread-local diagnostic regression coverage.
- Pinned cbindgen public-header regeneration/drift verification.
- Host release native build.
- Clean-source evidence recording used by Phase 4 artifact acceptance.

## Canonical command

For the current final ABI source, use:

```bash
python3 build/build.py verify-abi-final
```

The historical `verify-abi-rc` command remains only as a compatibility alias.

The final command hashes the maintained project inputs into a deterministic content-addressed snapshot, runs the Rust quality/test suite, host release build, cbindgen drift verification, and records source/evidence metadata required by Phase 4.

## Gate rule

Phase 4 artifacts are accepted only when their recorded source snapshot matches the exact content-addressed project inputs that passed the final ABI verification gate. Disposable external-consumer experiments used during development are not part of the tracked project.
