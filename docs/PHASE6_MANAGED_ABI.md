# Phase 6 — Managed ABI Conformance and Final ABI v1 Freeze

**Status:** COMPLETE
**Release scope:** Android ARM64 only
**ABI:** final ABI v1, version `1`, stage `2`
**Taffy baseline:** exactly `0.13.0`

## Purpose

Phase 6 proves the fixed-width native ABI through the managed/Unity boundary, resolves ABI discrepancies, freezes ABI v1, and rebuilds the shipping Android payload from the final contract. Temporary validation harnesses are local-only and are not tracked project source.

## Production ABI surface

`UnityPackage/Runtime/TaffyNative.cs` represents all 31 public `tu_*` exports from `include/taffy_ugui.h`, including version/capability queries, context/node lifecycle, bulk style/topology/measurement operations, Calc resources, Grid templates/diagnostics, compute, and single/bulk layout retrieval.

The wrapper validates structure sizes, enum values, ABI version/stage, exact Taffy version, and the required capability mask. `TaffyNative.ValidateAbi()` requires final stage `2` by default.

## ABI discrepancy resolved

Phase 6 exposed a native defect in `tu_copy_last_error`: the generic FFI guard cleared the thread-local diagnostic before it could be copied. The implementation now preserves the diagnostic through the copy operation, and maintained native regression coverage protects the behavior. The public ABI signature did not change.

## Managed / Android validation

During Phase 6 development the frozen runtime source passed:

- standalone managed ABI round trips;
- Unity package compilation and managed-boundary execution;
- direct ARM64 Android validation at ABI `1/2`;
- ARM64 Unity IL2CPP Player packaging;
- physical-device loading and managed/native round trips on CPH2723 / Android 16.

Disposable validation source and generated projects were removed after use.

## Final release rebuild

The final release gate uses a deterministic content-addressed project-input snapshot, so an uncommitted but byte-identical source state can be verified without weakening provenance. Phase 3 evidence, the Phase 4 Android manifest/index, and Phase 5 provenance all reference:

`sha256:68fb502c6bc48c83b2239f5212d98fd6a7f3f777c587cb286876121c58752731`

The accepted Android ARM64 binary is:

`sha256:7bdca92aae2939e5098292294ee7f7d730d5eee1c718d87f65a3f22349338f66`

The following final gates passed:

1. `python3 build/build.py verify-abi-final`;
2. `python3 build/build.py native android-arm64`;
3. `python3 build/build.py verify-phase4`;
4. `python3 build/build.py stage-phase5`;
5. `python3 build/build.py verify-phase5`.

A fresh Unity `6000.4.3f1` ARM64 IL2CPP APK was then built from a local-only copy of the accepted Phase 5 package. It contained exactly one ARM64 TaffyUGUI library, and its ELF program headers plus all runtime-loaded `PT_LOAD` bytes matched the accepted staged library. This final package integration evidence closes P6.14–P6.16 together with the earlier physical-device runtime proof of the same frozen ABI/runtime source.

## Exit gate

**PASS — ABI v1 / Final Native Payload Gate complete.** Phase 7 may begin.
