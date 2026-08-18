# Phase 13 — Performance and Reliability Hardening

**Status:** COMPLETE
**Date:** 2026-08-18
**Release scope:** Android ARM64 only
**Native ABI:** final ABI v1 (`version=1`, `stage=2`) on Taffy `0.13.0`

Phase 13 establishes repeatable performance baselines, allocation profiles, lifecycle/leak evidence, panic containment, and startup/package-size checks for the validated Android ARM64 product. Timing values are local-machine observations, not cross-machine performance guarantees.

## Permanent benchmark infrastructure

Three dependency-free Cargo benchmark targets are maintained under `native/benches/`:

- `layout_scaling` — first-layout, dirty-leaf, and fully cached recompute timing;
- `allocation_profile` — native allocator call/byte counts during first layout compute;
- `bulk_abi` — bulk layout retrieval compared with scalar per-node retrieval.

All benchmark runners emit human-readable output plus stable `TAFFY_*_RESULT` records for local capture. Tree construction is outside the timed compute interval. The scaling tree is an exact node-count Flex tree with deterministic child sizes.

## P13.1–P13.3 native scaling baseline

Final optimized local results:

| Nodes | Samples | First median | First p95 | Dirty-leaf median | Dirty-leaf p95 | Cached median | Cached p95 |
|---:|---:|---:|---:|---:|---:|---:|---:|
| 100 | 500 | 59.158 µs | 115.752 µs | 25.160 µs | 55.811 µs | 76 ns | 78 ns |
| 1,000 | 200 | 592.140 µs | 1.302 ms | 332.798 µs | 523.225 µs | 77 ns | 102 ns |
| 10,000 | 100 | 9.297 ms | 10.807 ms | 5.315 ms | 7.755 ms | 69 ns | 71 ns |

The fully cached path remains effectively constant-time at this scale. A one-leaf dirty recompute is substantially cheaper than a first full compute but still scales with affected-tree work; the 10,000-node case is intentionally a stress scenario, not a per-frame target recommendation.

## P13.4 allocation profiling

Native first-compute allocation profile:

| Nodes | Samples | Median allocation calls | Median allocated bytes | Median realloc calls | Median deallocation calls/bytes |
|---:|---:|---:|---:|---:|---:|
| 100 | 200 | 2 | 60,600 | 6 | 2 / 60,600 |
| 1,000 | 100 | 2 | 493,560 | 13 | 2 / 493,560 |
| 10,000 | 50 | 2 | 7,887,864 | 20 | 2 / 7,887,864 |

The first compute uses two fresh allocator calls at all measured scales; growth is handled through reallocations. The benchmark shows that large first layouts can generate meaningful transient native allocation traffic, especially at 10,000 nodes.

The permanent Unity Edit Mode allocation profile warms a real 100-node `TaffyLayoutGroup`, then performs 100 dirty forced rebuilds. Final Unity 6 result:

- managed-thread allocation: **0 bytes total / 0 bytes per measured rebuild**;
- Editor forced-rebuild timing: **14,691.70 µs per measured rebuild** on this run.

The Editor value includes Unity layout traversal, managed/native integration, and forced-rebuild overhead and must not be compared directly with the native-only compute timings.

## P13.5 dirty propagation

`layout_scaling --mode dirty-leaf` marks one leaf dirty on a persistent computed tree before each sample. Results are recorded in the scaling table above. `--mode cached` validates the no-dirty-change path and demonstrates that repeated cached compute calls return in roughly 70–80 ns median at 100 through 10,000 nodes on the local host.

## P13.6 bulk ABI transfer

Final layout retrieval results:

| Nodes | Samples | Bulk median | Bulk p95 | Scalar median | Scalar p95 | Median speedup |
|---:|---:|---:|---:|---:|---:|---:|
| 100 | 500 | 1.794 µs | 2.749 µs | 7.204 µs | 11.168 µs | 4.02× |
| 1,000 | 200 | 20.894 µs | 40.227 µs | 89.290 µs | 152.662 µs | 4.27× |
| 10,000 | 50 | 288.098 µs | 658.643 µs | 943.741 µs | 1.503 ms | 3.28× |

This validates the production Unity integration's use of `tu_get_layouts_bulk` instead of scalar layout retrieval loops.

## P13.7 domain/context lifecycle stress

Permanent native coverage now includes a 1,000-cycle registered-context create/destroy test. It asserts that active thread-local registry slots and global owner-map entries return to their initial counts and that stale handles are not reused.

Permanent Unity Edit Mode coverage performs 100 `TaffyLayoutGroup` enable/disable cycles, validates geometry each cycle, asserts the native context is zeroed on disable, and asserts every regenerated context handle is fresh.

The same Unity lifecycle test was then executed across **5 fresh Unity 6 batch editor launches**, producing 500 managed enable/disable cycles across fresh editor domains. All 5 launches passed with zero compiler errors and zero recursive-serialization warnings.

## P13.8 scene/prefab lifecycle stress

A permanent Edit Mode test saves a real Taffy prefab and cycles it through **50 fresh Editor scenes**. Each iteration instantiates the prefab, creates a native context, computes expected geometry, then destroys the scene/instance. The persisted prefab is reloaded afterward and its Taffy components and serialized dimensions are verified.

This test exposed recursive by-value Unity serialization depth warnings in the Phase 9 authoring schema. The production schema was hardened by applying managed-reference serialization to:

- `TaffyCalcExpression.operands`;
- `TaffyGridTrack.repeatTracks`.

Phase 9 Grid/Calc regressions and Phase 13 lifecycle tests pass after the change with **zero serialization-depth warnings** on Unity 2021.3.39f1, 2022.3.62f1, and 6000.4.3f1.

## P13.9 leak/resource checks

Valgrind Memcheck was run against the optimized permanent layout benchmark for 100 full 100-node tree lifecycles:

- 4,096 allocation calls / 4,093 frees;
- 41,637,712 bytes allocated over the run;
- **0 bytes definitely lost**;
- **0 bytes indirectly lost**;
- **0 Memcheck errors** when definite/indirect loss is the failure policy.

Valgrind reports a process-exit residual of 132 bytes possibly lost plus 544 bytes still reachable. A disposable same-toolchain Rust baseline using only `ThreadId`, `thread_local!`, and `OnceLock<Mutex<HashMap<...>>>` reproduces the **exact same 676-byte residual**, while a trivial Rust process reproduces the 544-byte reachable block. This residual is therefore recorded as pinned Rust ownership/runtime baseline behavior, not a Taffy context/resource leak. Disposable baselines remain under ignored `.build/` only.

## P13.10 error and panic containment

The ABI already wraps normal FFI entry points in `catch_unwind`. Phase 13 adds a direct regression that intentionally panics inside the guard and verifies:

- the panic is contained and returned as `TuStatus::InternalPanic`;
- the diagnostic becomes `unexpected native panic`;
- the next guarded call succeeds and clears the prior diagnostic.

All existing malformed-input, stale-handle, cross-context, wrong-thread, enum, and error-string tests remain green. Maintained Rust coverage is now **46/46 tests** total.

## P13.11 package/library load and startup checks

A 100 fresh-process Linux host probe measured dynamic-library load and first ABI/context operations using the same release native library consumed by the Unity validation hosts:

| Operation | Median | p95 |
|---|---:|---:|
| `ctypes.CDLL` library load | 291.115 µs | 508.301 µs |
| first `tu_get_abi_version` | 22.683 µs | 45.432 µs |
| first `tu_context_create` | 74.336 µs | 115.354 µs |
| first `tu_context_destroy` | 3.609 µs | 16.172 µs |

Current payload sizes:

- Linux validation-host native `.so`: **902,504 bytes**;
- Android ARM64 production `.so`: **762,488 bytes**;
- `UnityPackage/` tree: **1,100,864 bytes across 72 files** at measurement time;
- fresh Unity 6 Android Development APK: **31,886,853 bytes**.

The fresh APK contains both `lib/arm64-v8a/libil2cpp.so` and `lib/arm64-v8a/libtaffy_ugui.so`. Android Gradle stripping changes non-loaded ELF metadata, so whole-file APK/staged SHA values differ; both `PT_LOAD` headers and both runtime-loaded segment SHA-256 values are byte-identical between staged and packaged Taffy libraries.

The Android device was not attached during this final Phase 13 closeout, so no new physical-device execution is claimed here. Phase 12's physical `CPH2723` execution remains the latest device evidence; Phase 13 adds a fresh build/package/ELF regression from the current source tree.

## Cross-version and Android closeout

Final permanent Unity regression matrix on the Phase 13 source state:

| Unity Editor | Edit Mode | Play Mode | Serialization-depth warnings |
|---|---:|---:|---:|
| 2021.3.39f1 | 41/41 | 9/9 | 0 |
| 2022.3.62f1 | 41/41 | 9/9 | 0 |
| 6000.4.3f1 | 41/41 | 9/9 | 0 |

Unity 2021.3 still requires the local Linux Bee `--stdin-canary` compatibility workaround on this newer Linux host. The original `bee_backend` was restored after validation with SHA-256 `8561ed19e6d35e1e947b450dd528867e7c43c9fe43b5cce9086b58d3cad4fa67`.

Final native/Android gates also pass:

- `python3 build/build.py quality`;
- `python3 build/build.py verify-abi-final`;
- fresh `android-arm64` native build and deep verification;
- Phase 4 provenance verification;
- Phase 5 Unity payload staging/verification;
- fresh Unity 6 Android ARM64 IL2CPP Development Player build;
- APK native payload and `PT_LOAD` byte-identity verification.

Content-addressed project-input snapshot:

`sha256:e1e047831c23047e8ab0a1e2fbad256453b63bd03b679b49e3b0af3ee778ffcb`

Android ARM64 native library SHA-256:

`7bdca92aae2939e5098292294ee7f7d730d5eee1c718d87f65a3f22349338f66`

The production binary hash is unchanged because Phase 13's native-code additions are benchmark/test-only; provenance was nevertheless rebuilt and restaged from the exact current verified source tree.

## Known limits

- Benchmark timings are machine-, OS-, compiler-, and load-dependent. Treat them as regression baselines, not universal performance guarantees.
- The benchmark tree is intentionally deterministic and does not represent every real UI hierarchy, measurement workload, Grid configuration, or ScrollRect behavior.
- The 10,000-node result is a stress envelope. Large first layouts have non-trivial native transient allocation and compute cost.
- Unity Editor forced rebuild timing includes substantial engine/editor overhead and should not be used as Player frame-time prediction.
- Android ARM64 remains the sole advertised Player target on this branch.
- A new physical-device Phase 13 run remains desirable when a device is attached, but it is not a Phase 13 task gate; Phase 12 already established physical Android execution.

## Phase 13 gate

P13.1 through P13.12 are complete. Phase 14 v1.0 release work is now authoritative, beginning with P14.1 final compatibility matrix.

Disposable validation/profiling material remains local-only under ignored `.build/` paths. Permanent benchmarks and regression tests are tracked project code; harness/probe scripts are not.
