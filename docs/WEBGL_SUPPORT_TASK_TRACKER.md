# TaffyUGUI Web Support Task Tracker

**Program status:** ACTIVE  
**Support target:** Unity `2021.3 LTS` through the latest current Unity release  
**Current latest release at matrix verification:** Unity `6000.5.8f1` (2026-08-12)
**Current phase:** WEB5 ACTIVE
**Authoritative next task:** WEB5.2



This tracker is the authoritative implementation plan for adding Unity Web/WebGL Player support without changing existing Android/desktop runtime behavior. Complete phases in order. Commit only when the active phase is complete.

## Support claim we are working toward

After all required gates pass, the package may advertise:

> TaffyUGUI supports Unity 2021.3 LTS and newer on the Web/WebGL Player platform, through the latest Unity version validated by the current package release.

This does **not** mean every Unity patch is executed individually. Release validation must cover the oldest supported editor, each important Unity/Emscripten generation boundary, the latest supported LTS, and the latest current Unity release.

Unity `2021.1` is intentionally outside scope. Unity `2021.3` is the minimum because Unity 2021.2+ supports the modern WebAssembly object archive (`.a`) native plug-in format.

## Locked architecture

### Primary production path

Use one generic Rust WebAssembly static archive built from the existing native source:

- Rust target: `wasm32-unknown-unknown`;
- output: GNU static archive containing Wasm object files (`libtaffy_ugui.a`);
- Web-only crate type: `staticlib`;
- conservative CPU baseline: `-C target-cpu=mvp`;
- panic strategy: `-C panic=abort`;
- keep the public `tu_*` ABI unchanged;
- keep managed Web P/Invoke using `DllImport("__Internal")`;
- stage the final artifact under `UnityPackage/Plugins/WebGL/` with Web-only importer metadata.

The Web build should use a small Web-only Cargo manifest that points at the existing `native/src/lib.rs` and requests only `staticlib`. This avoids changing the existing native crate's `cdylib/staticlib/rlib` outputs for other platforms.

### Why this is preferred

The generic `wasm32-unknown-unknown` archive does not bind the Rust standard library to a particular Emscripten ABI. Unity can link the Wasm object archive with the Emscripten version bundled with each Editor.

This is materially safer for a Unity `2021.3 -> current` support range than building the Rust library directly as `wasm32-unknown-emscripten`, because Rust documents that its Emscripten standard library ABI must match the local Emscripten version and flags.

### Fallback only if the oldest Unity linker rejects the generic object archive

1. First try a Web-only Rust toolchain pinned to the native crate's MSRV (`1.82`) with the same `wasm32-unknown-unknown` architecture.
2. If generic Wasm object compatibility still fails, use `wasm32-unknown-emscripten` with the exact Unity-bundled Emscripten toolchain and `build-std` per compatibility family.
3. Do not introduce a JavaScript layout-engine rewrite. `.jslib` is only for browser API integration if later required.

## Web threading scope

Default/single-thread Unity Web builds are the required support gate.

Unity Web multithreading is a separate compatibility gate because a shared-memory Wasm link requires atomics/bulk-memory-compatible Rust core/std objects. The feasibility spike proved that a shared-memory-compatible archive can be produced and linked, but runtime browser validation is still required. Do not block default Web support on threaded Web support, and do not claim threaded Web support until WEB7 passes.

TaffyUGUI Unity layout work remains main-thread UI work. Any Web-specific change to `CapThreadLocalContexts` or wrong-thread diagnostics must be deliberate, documented, and must not change non-Web behavior.

---

# WEB0 — Scope, architecture, and feasibility

**Status: COMPLETE**  
**Completed: 2026-08-19**

## WEB0 acceptance criteria

- [x] Define minimum Unity version as `2021.3 LTS`.
- [x] Define support through the latest current Unity release at package release time.
- [x] Verify Unity 2021.2+ accepts `.a` archives containing Wasm object files for native Web plug-ins.
- [x] Confirm current managed bridge already resolves Web Player native calls through `__Internal`.
- [x] Inspect native Rust code for Web blockers (`thread_local!`, `ThreadId`, mutexes, panic handling, static library output).
- [x] Test direct `wasm32-unknown-emscripten` build against a real Unity Web toolchain and identify ABI hazards.
- [x] Test `build-std + panic=abort` against Unity's Emscripten and prove the Emscripten-bound fallback is viable.
- [x] Prove a generic `wasm32-unknown-unknown` static archive links with Unity Emscripten.
- [x] Prove Taffy Flex/Grid/Block/Calc core can compile for generic Wasm.
- [x] Prove the existing TaffyUGUI native source can compile to a generic Wasm static archive with detailed Grid support retained.
- [x] Prove all current `tu_*` symbols are present in the Web archive.
- [x] Execute a real public-ABI layout smoke through Unity Emscripten.
- [x] Verify a Web-only Cargo manifest can point to the existing native source without refactoring the native engine.
- [x] Separate default Web support from optional multithreaded Web support.

## WEB0 executed evidence

Local environment used for feasibility:

- Rust `1.97.1`;
- Unity `6000.3.9f1` Web module;
- Unity-bundled Emscripten `3.1.39-git`;
- existing TaffyUGUI ABI v1 source.

Executed results:

- Existing `wasm32-unknown-emscripten` build reached the native crate but its `cdylib` path failed in Unity Emscripten because Cargo also attempted to produce an Emscripten side module.
- Staticlib-only `wasm32-unknown-emscripten` build succeeded and produced `libtaffy_ugui.a`.
- A normal Emscripten link exposed Rust/Emscripten exception ABI mismatch (`__cpp_exception`).
- `build-std + panic=abort + target-cpu=mvp` removed that mismatch; context create/destroy linked and executed successfully.
- Generic `wasm32-unknown-unknown` proof archive linked and executed under Unity Emscripten.
- Taffy `0.13.0` Flex/Grid/Block/Float/Calc/content-size core compiled and executed as generic Wasm.
- The existing TaffyUGUI native crate, with only Web staticlib output and `-Ctarget-cpu=mvp -Cpanic=abort`, built successfully as `wasm32-unknown-unknown`.
- Generic existing-engine archive size in the spike: about `11.6 MB` before final Unity link/dead stripping.
- Unity LLVM symbol inspection found all `31` current `tu_*` exports in the static archive.
- Public C ABI smoke created a real `TuStyle`, computed layout, and verified a `42 x 17` result.
- Web-only manifest pointing directly at `native/src/lib.rs` built successfully, so the production Web artifact does not require changing the existing platform crate types.
- A shared-memory-compatible build using rebuilt Rust std/core and atomics/bulk-memory linked in Unity Emscripten with `-pthread`; runtime execution remains a browser gate because Unity 6000.3's bundled Node cannot execute that threaded Wasm configuration.

## WEB0 feasibility conclusion

**No architectural blocker is known.** Default/single-thread Web support is fully feasible with the current Rust/Taffy engine and current C ABI. The remaining work is production implementation and cross-version/browser validation, especially the Unity 2021.3 linker/runtime gate.

The oldest Unity Web module is not installed on this machine yet, so WEB0 does not claim that Unity 2021.3 has already passed a real Player build. That is a required WEB4 gate, not an unresolved architecture issue.

---

# WEB1 — Production generic Wasm native artifact

**Status: COMPLETE**
**Completed: 2026-08-20**

- [x] **WEB1.1** Add a Web-only Cargo manifest that reuses `native/src/lib.rs` and emits only `staticlib`.
- [x] WEB1.2 Add a reproducible build command/target for `wasm32-unknown-unknown`.
- [x] WEB1.3 Pin Web build flags to `-Ctarget-cpu=mvp -Cpanic=abort` unless a stricter compatibility gate proves another setting is needed.
- [x] WEB1.4 Ensure the Web build remains independent of Unity/Emscripten headers and libraries.
- [x] WEB1.5 Verify the archive contains exactly the expected public `tu_*` ABI surface and no accidental public ABI expansion.
- [x] WEB1.6 Add a permanent native/Web link harness based on `include/taffy_ugui.h` covering ABI version, context lifecycle, node creation, layout compute, and layout retrieval.
- [x] WEB1.7 Audit Web panic behavior. Because the compatibility build uses `panic=abort`, remove avoidable internal `expect`/panic paths at the Web boundary where practical and document the remaining invariant behavior.
- [x] WEB1.8 Decide and test Web handling for `CapThreadLocalContexts` without weakening non-Web behavior.
- [x] WEB1.9 Measure release archive size and apply safe size optimizations only if they do not reduce old-linker compatibility.
- [x] WEB1.10 Keep all generated/probe artifacts outside tracked source except the intentional staged package artifact.

WEB1.1:

- Added `native/web/Cargo.toml` as an independent nested Cargo workspace so root `--workspace` builds continue to contain only the existing native package.
- The Web manifest points its library target directly at `../src/lib.rs`, names the library `taffy_ugui`, and requests only `crate-type = ["staticlib"]`.
- Web package version/edition/MSRV and the exact Taffy dependency/features are aligned with `native/Cargo.toml`; this preserves the build version reported by the ABI.
- A release `wasm32-unknown-unknown` build succeeded and emitted `libtaffy_ugui.a` with no Web package `cdylib`/`rlib` output.
- The generated Web lock resolves the same dependency versions as the root native `Cargo.lock`.
- `cargo check --workspace --locked` still passes for the existing root workspace and builds only `taffy_ugui_native`.

WEB1.2:

- Added the canonical `python3 build/build.py web-native` command. It builds `native/web/Cargo.toml` with `--locked --release --target wasm32-unknown-unknown` into the ignored `.build/web-cargo-target/` tree and fails if the expected archive is missing or empty.
- Added `wasm32-unknown-unknown` to `rust-toolchain.toml` so fresh project-local Rust provisioning declares the required Web standard-library target instead of depending on a machine-global installation.
- The command uses the project-local pinned Cargo/Rust toolchain and does not stage anything into the Unity package yet; staging remains WEB2.
- Two clean canonical builds produced byte-identical `libtaffy_ugui.a` archives: `5,078,638` bytes, SHA-256 `8f6c6dcd722c21833ca788b5c1595e0f1fe4a363d3b7b59bab8f15043c9f58a8`.

WEB1.3:

- Pinned the generic Web build to `-Ctarget-cpu=mvp` and `-Cpanic=abort` through the target-specific Cargo environment used only by `web-native`.
- The canonical command discards inherited generic `RUSTFLAGS` / `CARGO_ENCODED_RUSTFLAGS` and overwrites any inherited `CARGO_TARGET_WASM32_UNKNOWN_UNKNOWN_RUSTFLAGS`, preventing caller-specific flags from weakening the compatibility baseline.
- A clean build launched with deliberately conflicting inherited flags still succeeded, and Cargo's recorded fingerprint contained exactly `[-Ctarget-cpu=mvp, -Cpanic=abort]` for `taffy_ugui_web`.
- Restored the existing `DIST_NATIVE` / package build-driver constants after reviewing the cumulative WEB1 diff, and documented the shared-namespace Pyright model for the final build-driver segment so VS Code Problems remains actionable.

WEB1.4:

- Added a shared Web build environment that removes inherited Unity/Emscripten/linker/include/library hooks before invoking Cargo, including Emscripten configuration/cache variables, target linker overrides, C/C++ flags, and native include/library search paths.
- Added the permanent `python3 build/build.py verify-web-independence` gate. It checks the resolved Cargo graph for any package declaring a native-library `links` dependency, then performs a clean release build with `CC`, `CXX`, `AR`, and `RANLIB` (including wasm target-specific variants) poisoned to nonexistent tools.
- The independence gate passed from a clean target directory, proving the current Web archive requires only the pinned Rust toolchain and pure-Rust dependency graph; no Unity SDK, Emscripten compiler/archive tool, C/C++ compiler, Unity header, or external native library is needed to produce `libtaffy_ugui.a`.
- A separate clean canonical `web-native` build also passed while hostile `EMSDK`, `EM_CONFIG`, `EM_CACHE`, Emscripten linker, generic wasm linker, C include, and library-link flags were injected by the caller, proving the canonical path sanitizes those inputs.


WEB1.5:

- Added the permanent `python3 build/build.py verify-web-abi-surface` gate. It builds the canonical generic Web archive, inspects globally defined symbols with the pinned Rust `llvm-nm`, and compares the complete `tu_*` set against the checked-in C header contract.
- The gate fails on any missing export, unexpected `tu_*` export, or duplicate public definition, preventing accidental Web-only ABI drift.
- Current verification passes with exactly `31` expected `tu_*` exports and no additions or omissions.
WEB1.6:

- Added the permanent `native/web/link_harness.c` public-C-ABI smoke and the `python3 build/build.py verify-web-link-harness` gate.
- The build driver discovers installed Unity Hub WebGL Emscripten toolchains and supports an explicit `TAFFYUGUI_UNITY_EMSCRIPTEN_ROOT` override so later compatibility phases can select an exact Unity toolchain without changing the harness.
- The harness includes only `include/taffy_ugui.h`, links the canonical generic `libtaffy_ugui.a`, and validates ABI version/stage, context create/clear/destroy, node creation before and after clear, layout compute, and layout retrieval with an exact `42 x 17` fixed-size result.
- Runtime verification passes under Unity `6000.3.9f1` using bundled Emscripten `3.1.39-git` and bundled Node. The harness link intentionally uses `-O0`: optimization is irrelevant to this ABI/link smoke, and this avoids a Unity-bundled Binaryen post-link option mismatch observed at `-O2`; the Rust archive itself remains the canonical optimized release build.

WEB1.7:

- Added the permanent `python3 build/build.py verify-web-panic-boundary` gate. It requires the canonical `-Cpanic=abort` Web configuration, scans production Rust sources for `.unwrap()`, explicit `panic!`, assertion/unreachable/todo-style panic sites, and rejects new `.expect()` calls outside the pinned Taffy tree callbacks.
- The production audit found no recoverable `.unwrap()` or explicit panic/assertion paths in the ABI/runtime implementation. The remaining `expect` sites are the Taffy `LayoutPartialTree`/Flex/Grid/Block/Cache/Round callback invariants that require a live node/parent/child and cannot return `Result`; one child-index access is similarly pinned to Taffy's `child_count`/`get_child_id` contract. Release-disabled `debug_assert!` checks remain internal diagnostics only.
- `guard()`/`tu_copy_last_error` still use `catch_unwind` for native unwind builds, but Web `panic=abort` cannot rely on that containment. The Web contract is therefore to validate caller-controlled input before reaching Taffy, while an actual violated Taffy live-tree invariant remains a fatal internal bug rather than a recoverable ABI status.
- Extended the permanent Web C harness with malformed-input cases for null pointers, zero context/node handles, invalid enum values, NaN layout dimensions, and invalid node handles. The real optimized `panic=abort` archive returns `NullPointer`, `InvalidContext`, `InvalidEnum`, `InvalidNumber`, and `InvalidNode` as expected and continues executing normally under Unity Emscripten `3.1.39-git`.
- `verify-web-panic-boundary` passes end-to-end and then reruns the public-header link harness, proving the audited boundary behavior against the actual Web archive rather than only through static source inspection.

WEB1.8:

- The supported default/single-thread Web path keeps `CapThreadLocalContexts` advertised. The Rust implementation still stores contexts in `thread_local!` state, and Unity's managed ABI handshake continues to require bit 8 exactly as it does on native platforms; no Web-only capability fork was introduced.
- Added the permanent `python3 build/build.py verify-web-thread-local-contexts` gate. It pins the native capability definition/aggregation, thread-local registry, wrong-thread rejection path, and managed required-capability contract, then runs the real Web public-header harness to verify the generated archive actually reports the capability.
- The same gate runs the existing native `p3_7_wrong_thread_use_is_rejected` integration test, which passes and confirms cross-thread context use still returns `WrongThread`; WEB1 therefore does not weaken desktop/native ownership semantics to accommodate Web.
- Threaded Unity Web remains a separate WEB7 compatibility problem. If Web workers/native calls become multi-threaded there, ownership and artifact behavior must be validated under that configuration before making a threaded-Web support claim; the default Web claim does not depend on that future work.

WEB1.9:

- Added the permanent `python3 build/build.py verify-web-size` gate. It verifies the Web-only release profile remains `lto = "thin"`, `codegen-units = 1`, and `strip = "symbols"`, and refuses compatibility-changing Rust flags beyond the locked `-Ctarget-cpu=mvp -Cpanic=abort` baseline.
- A clean canonical release build currently produces `libtaffy_ugui.a` at `5,059,192` bytes (`4.8248 MiB`), SHA-256 `bfba5b81c7c8cf4b7becc3c4ace0fb599819d0be831560c9944f7abdb6b3253a`. A second clean rebuild reproduced the same size and hash, making this the current authoritative WEB1 size baseline.
- The permanent gate uses a `6 MiB` review threshold, not a product/download-size limit. Crossing it intentionally fails validation so archive growth is reviewed before anyone reaches for compatibility-sensitive optimization or Wasm feature flags.
- No additional size flags were adopted in WEB1.9. Options such as a new size-focused optimization level or more aggressive LTO are deferred because the mandatory Unity `2021.3` old-linker gate has not run yet; preserving `target-cpu=mvp` compatibility is more important than reducing the intermediate archive prematurely.
- The existing public-header link harness still passes with the canonical archive under Unity Emscripten `3.1.39-git`. Its intentionally unoptimized `-O0` linked Wasm is `474,015` bytes, showing that normal final linking already dead-strips most unused archive code; the `.a` size is therefore not the eventual Unity Player download size.
- The earlier WEB1.2 size/hash note records the earlier build snapshot. WEB1.9's two clean rebuilds are the current measurement used for ongoing size review.

WEB1.10:

- Added the permanent `python3 build/build.py verify-web-source-cleanliness` gate. It rejects tracked or unignored disposable build/probe paths and generated Web `.a`, `.wasm`, `.o`, `.bc`, or `.js` outputs under `native/web/`.
- Hardened `.gitignore` for direct `native/web/target/` Cargo output and accidental Web archive/object/Wasm/JavaScript products, while preserving the intentional Web source files (`Cargo.toml`, `Cargo.lock`, and `link_harness.c`).
- The cleanliness gate verifies `.build/`, local harness/probe trees, direct nested Cargo targets, and representative Web generated outputs remain ignored. Final verification reports no generated/probe artifact tracked or leaking as untracked source.
- WEB1 closeout reran the exact `31`-symbol ABI surface gate, independent clean Web build, panic-boundary harness, thread-local-context gate, size gate, and source-cleanliness gate successfully.
- Non-Web regression remained green: rustfmt, Clippy `-D warnings`, `37/37` maintained Rust unit tests, `9/9` native verification tests, release build, cbindgen drift check, and final ABI v1 gate all passed. `native/src`, `native/Cargo.toml`, `include/taffy_ugui.h`, and all existing `UnityPackage/Plugins` payloads have no diff, proving WEB1 did not alter native runtime/ABI/plugin artifacts.

**WEB1 phase gate: PASS** — generic archive reproducibly builds from clean source; ABI/link harness passes; non-Web Rust tests and native artifacts remain unchanged.

---

# WEB2 — Unity package integration

**Status: COMPLETE**

- [x] WEB2.1 Stage `libtaffy_ugui.a` under `UnityPackage/Plugins/WebGL/`.
- [x] WEB2.2 Add deterministic `.meta` importer settings: Web/WebGL enabled, Editor and all non-Web targets disabled.
- [x] WEB2.3 Verify Unity does not attempt to load the Web archive in Editor Play Mode. Unity `6000.4.3f1` local-UPM verification passed: the dedicated importer suite is **2/2**, the full Edit Mode suite is **142/142**, and the actual Play Mode suite is **9/9** with the Web archive installed. The Play Mode Editor log contains zero `libtaffy_ugui.a`, `Plugins/WebGL`, `DllNotFoundException`, native-load failure, undefined-symbol, or `EntryPointNotFoundException` markers..
- [x] WEB2.4 Verify `TaffyNative.Library == "__Internal"` remains the Web Player path and no separate DLL name is introduced.
- [x] WEB2.5 Add package tests that validate importer configuration and artifact presence.
- [x] WEB2.6 Verify UPM/Git package packaging includes the `.a` and its `.meta` (both are non-ignored and present in the Git index).

**WEB2 phase gate: PASS** — package imports cleanly; the Web archive is WebGL-only, excluded from Editor Play Mode and all non-Web targets, retained by Git/UPM packaging, and uses the existing `__Internal` Web Player binding.ds.

---

# WEB3 — Permanent Web runtime regression scene/harness
**Status: COMPLETE**
**Completed: 2026-08-20**

Create a deterministic Web Player test that exercises the package through normal `TaffyLayoutGroup` / `TaffyLayoutItem` APIs, not only direct native calls.

- [x] WEB3.1 ABI version/stage/capability handshake.
  - Added permanent player-compatible `TaffyWebRuntimeRegressionTests` coverage using normal `TaffyLayoutGroup` / `TaffyLayoutItem` APIs. The deterministic `TAFFY_WEB_RUNTIME_ABI_PASS abi=1 stage>=2 taffy=0.13.0 capabilities=required` marker is emitted only after the normal layout initialization path completes native ABI, stage, Taffy-version, and required-capability validation.
  - Unity `6000.4.3f1` verification passes the focused WEB3.1 Play Mode test and the complete maintained Play Mode suite. The initial Unity `6000.3.9f1` WebGL attempt exposed compatibility issues that were resolved during WEB3 closeout: the canonical Web archive is built with Rust `1.82.0` to avoid newer generic-Wasm target-feature metadata rejected by Unity's older Binaryen; the Web-only allocator delegates to the final Emscripten runtime so Rust allocation shares Unity's linear-memory model; and the managed ABI validator uses the correct 32-bit pointer struct-size table in Web Players while retaining the existing 64-bit validation everywhere else.
- [x] WEB3.2 Flex row/column layout.
  - Added deterministic row/column geometry coverage to `TaffyWebRuntimeRegressionTests`. The harness verifies two explicitly sized `TaffyLayoutItem` children in both flex directions and emits `TAFFY_WEB_RUNTIME_FLEX_PASS row=2 column=2`; Unity `6000.4.3f1` passes the focused WEB3 fixture (`2/2`) and the complete maintained Play Mode suite (`11/11`).
- [x] WEB3.3 Grid tracks, placement, gaps, and detailed Grid diagnostics.
  - Added a fixed one-row/two-column Grid case with a `10` point horizontal gap and explicit second-column placement. The harness validates resolved `100`/`80`/`110` point tracks, `[0, 10, 0]` column gutters, item diagnostics, and emits `TAFFY_WEB_RUNTIME_GRID_PASS rows=1 columns=2 horizontalGap=10 items=1`.
- [x] WEB3.4 Block/FlowRoot behavior retained where applicable.
  - Added normal managed API coverage for Block vertical stacking and FlowRoot left-float plus `Clear.Both`, with deterministic geometry checks and `TAFFY_WEB_RUNTIME_BLOCK_PASS block=stack flowRoot=float-clear`.
- [x] WEB3.5 Calc values.
  - Added Calc-backed item width and Grid-track mutation coverage. The harness verifies `70 -> 90` point item recomputation, a mutated `120` point Calc track through detailed diagnostics, and emits `TAFFY_WEB_RUNTIME_CALC_PASS item=90 track=120`. Unity `6000.4.3f1` passes the focused WEB3 fixture (`5/5`) and the complete maintained Play Mode suite (`14/14`).
- [x] WEB3.6 responsive profiles and forced/automatic profile resolution.
  - Added narrow/`Column` and wide/`Row` profiles to the permanent runtime harness. It verifies automatic selection at `200` and `400` point widths, `SetRuntimeResponsiveProfile("wide")`, clearing the override back to automatic narrow resolution, geometry changes for both modes, and emits `TAFFY_WEB_RUNTIME_RESPONSIVE_PASS automatic=narrow/wide forced=wide clear=narrow`. Unity `6000.4.3f1` passes the focused fixture (`6/6`) and full maintained Play Mode suite (`15/15`).
- [x] WEB3.7 TextMeshPro intrinsic measurement and width-constrained wrapping.
  - Added permanent TMP coverage using `TextMeshProUGUI` through the normal `TaffyLayoutGroup` / `TaffyLayoutItem` path. The regression validates max-content intrinsic width, cached intrinsic height, then fixes the item width to the permanent `128` point measurement sample and verifies Taffy reproduces TMP's width-constrained preferred height. It emits `TAFFY_WEB_RUNTIME_TMP_PASS intrinsic=1 wrapped=1` only after both paths pass. Unity `6000.4.3f1` passes the focused test (`1/1`), the complete WEB3 fixture (`7/7`), and the full maintained Play Mode suite (`16/16`).
- [x] WEB3.8 uGUI Text/Image/RawImage measurement regressions where supported by the existing package tests.
  - Added permanent player-compatible uGUI measurement coverage for runtime-mutated `Text`, replaced-element `Image`, and `RawImage` UV-scaled intrinsic sizing. The regression verifies Text width grows after content mutation, a `64 x 32` sprite resolves to `64 x 32`, an `80 x 40` RawImage using a `0.5 x 0.5` UV rect resolves to `40 x 20`, and emits `TAFFY_WEB_RUNTIME_UGUI_MEASURE_PASS text=1 image=64x32 raw=40x20`. Unity `6000.4.3f1` passes the focused test (`1/1`), complete WEB3 fixture (`8/8`), and full maintained Play Mode suite (`17/17`).
- [x] WEB3.9 bulk style/topology/measurement/layout ABI calls.
  - Added a permanent Play Mode C-ABI regression that binds the same public bulk exports directly in the test assembly without altering production runtime behavior. It creates a real context/root/two-child tree, bulk-updates two styles, bulk-uploads one topology change and one intrinsic measurement, computes layout, retrieves three layouts through `tu_get_layouts_bulk`, and verifies `40 x 20` plus measured `30 x 12` child geometry. The marker is `TAFFY_WEB_RUNTIME_BULK_ABI_PASS styles=2 topology=1 measurement=1 layouts=3`; Unity `6000.4.3f1` passes the focused test, WEB3 fixture (`9/9`), and full maintained Play Mode suite (`18/18`).
- [x] WEB3.10 repeated context/node/resource create-destroy cycles.
  - Added 32 deterministic cycles that create/destroy a native context, create/remove a node, and create/remove a Calc resource through the real public ABI. The marker is `TAFFY_WEB_RUNTIME_LIFECYCLE_PASS cycles=32 context=1 node=1 resource=1`; Unity `6000.4.3f1` passes the focused test, WEB3 fixture (`10/10`), and full maintained Play Mode suite (`19/19`).
- [x] WEB3.11 nested groups and ScrollRect integration.
  - Added normal managed-API regression coverage for a nested Column group resolving to `90 x 35` from its child and for vertical `ScrollRect` content remaining `100` points tall with one `70` point child then expanding to `140` after a second runtime child without a rebuild loop. The marker is `TAFFY_WEB_RUNTIME_NESTED_SCROLL_PASS nested=90x35 scroll=140`; Unity `6000.4.3f1` passes the focused test, WEB3 fixture (`11/11`), and full maintained Play Mode suite (`20/20`).
- [x] WEB3.12 no `DllNotFoundException`, undefined `tu_*`, abort, or native-link error markers.
  - Unity `6000.3.9f1` built and executed the complete `TaffyWebRuntimeRegressionTests` fixture in Chromium through the WebGL Player/Test Runner transport: **11/11 passed**. All 11 deterministic `TAFFY_WEB_RUNTIME_*` markers were present, including TMP intrinsic/wrapped measurement after the disposable test host imported the maintained TMP Settings/font resources. Browser Player XML/log scanning found zero `DllNotFoundException`, `EntryPointNotFoundException`, undefined-symbol/`tu_*`, Wasm `RuntimeError`, abort, native-link, or streaming-compile failure markers.
  - Final Editor regression on Unity `6000.4.3f1` passed **20/20** Play Mode tests. Native closeout remained green with rustfmt, Clippy `-D warnings`, **37/37** Rust unit tests, **9/9** native verification tests, release build, cbindgen header drift, and final ABI-v1 verification.
  - Canonical Web closeout gates all pass from the staged package artifact: exact **31** `tu_*` exports, deterministic Web package match/importer isolation, Unity Emscripten public-header link/execute, panic-boundary validation, native wrong-thread preservation, source cleanliness, Unity/Emscripten-independent Rust build, and size review. Current archive: `4,200,052` bytes (`4.0055 MiB`), SHA-256 `49dc545b6a3ada0b8652480931c47dc7e43bb10587470d11178f10b0592cda38`.

**WEB3 phase gate: PASS** — the permanent regression reports deterministic success in an actual Chromium WebGL Player, and the browser/native-link error scan is clean.

---

# WEB4 — Unity 2021.3 minimum-version gate

**Status: COMPLETE — 2026-08-20**

This minimum-version gate passed on Unity `2021.3.39f1` with the package's normal/default single-thread WebGL path.

- [x] WEB4.1 Install WebGL Build Support for `2021.3.39f1` (or the accepted minimum patch used by package validation).
  - Installed the exact `2021.3.39f1` WebGL module from that Editor's Unity Hub module manifest and verified the complete `PlaybackEngines/WebGLSupport` payload.
- [x] WEB4.2 Record the exact bundled Emscripten version from `emscripten-version.txt`.
  - Unity `2021.3.39f1` reports bundled Emscripten `2.0.19`.
- [x] WEB4.3 Link the generic archive with the Unity 2021.3 Web toolchain before running a full Unity build.
  - `verify-web-link-harness` links and executes the canonical Rust `wasm32-unknown-unknown` archive with Unity 2021.3 Emscripten `2.0.19`; the fallback Emscripten-target architecture is not required.
- [x] WEB4.4 Build a Development WebGL Player.
  - PASS after correcting a real minimum-version importer compatibility issue: Unity 2021.3 treated the previous PluginImporter `serializedVersion: 3` map-form metadata as WebGL-disabled and omitted the `.a` from the Player link. The checked-in archive metadata and deterministic staging contract now use Unity 2021.3's own canonical `serializedVersion: 2` list-form WebGL entry. The Development Player then completed the final Emscripten link with no undefined `tu_*` symbols. Unity `6000.3.9f1` also passes the permanent Web archive importer tests `2/2` with this metadata.
- [x] WEB4.5 Run the WEB3 regression in a supported desktop browser.
  - Chromium executed the Unity 2021.3 Development Player and the permanent `TaffyWebRuntimeRegressionTests` fixture passed **11/11** in Test Framework XML with zero failed/skipped tests.
- [x] WEB4.6 Build a non-Development/release WebGL Player.
  - PASS with `options.Development = False`; Unity's Emscripten/Binaryen release path completed through `wasm-opt -O3`, and Chromium emitted all **11/11** deterministic `TAFFY_WEB_RUNTIME_*` PASS markers. Test Framework `1.1.33` does not return release-player results through its Development-Player remote transport, so release evidence is the successful Player build plus all 11 browser runtime markers rather than a second XML file.
- [x] WEB4.7 Verify TMP measurement and responsive layout in-browser.
  - Both Development and non-Development Players emitted `TAFFY_WEB_RUNTIME_RESPONSIVE_PASS automatic=narrow/wide forced=wide clear=narrow` and `TAFFY_WEB_RUNTIME_TMP_PASS intrinsic=1 wrapped=1`; legacy uGUI measurement also emitted `TAFFY_WEB_RUNTIME_UGUI_MEASURE_PASS text=1 image=64x32 raw=40x20`.
- [x] WEB4.8 Record build size, startup memory, and any Unity-2021-specific workaround.
  - Canonical staged archive: `4,200,052` bytes (`4.0055 MiB`), SHA-256 `49dc545b6a3ada0b8652480931c47dc7e43bb10587470d11178f10b0592cda38`. The served non-Development test Player was `37,865,533` bytes across 11 files; its compressed core payload (`data.gz + framework.js.gz + wasm.gz + loader.js`) was `6,317,268` bytes. Emscripten startup linear memory is `32 MiB` (`INITIAL_MEMORY=33554432`) with `ALLOW_MEMORY_GROWTH=1`.
  - Validation-host-only Unity 2021/Linux accommodations were kept out of package source: batch runs explicitly selected `-buildTarget WebGL`; a temporary wrapper removed Unity 2021.3's host-stalling `bee_backend --stdin-canary` argument and was restored byte-for-byte afterward (SHA-256 `8561ed19e6d35e1e947b450dd528867e7c43c9fe43b5cce9086b58d3cad4fa67`); Test Framework `1.1.33` ignored newer WebGL browser-setting keys so Chromium was launched explicitly; and an ignored `.build/` `ITestPlayerBuildModifier` cleared the framework's forced Development flag for the release regression. None changes runtime/native behavior or ships in the package.None of these changes runtime/native behavior or ships in the package.

The generic object archive is accepted by the minimum-version toolchain, so the fallback decision is not activated.

**WEB4 phase gate: PASS** — Unity `2021.3.39f1` Development and non-Development WebGL Players both build and run the complete permanent 11-test Web regression successfully.

---

# WEB5 — Cross-version Unity Web matrix

**Status: COMPLETE**

Required release gates should cover meaningful Unity/Emscripten generations, not every patch.

| Gate | Required editor | Reason |
|---|---|---|
| Minimum | `2021.3.39f1` | package minimum; Emscripten 2.0 family |
| LTS generation | `2022.3.62f1` or maintained 2022.3 gate | Emscripten 3.1.8 family |
| 2023 generation | `2023.2.20f1` | final practical 2023.2 patch; Emscripten 3.1.39 family |
| Unity 6 baseline | `6000.0.81f1` | maintained Unity 6.0 LTS baseline |
| Latest LTS | `6000.3.22f1` | latest supported LTS patch at matrix verification |
| Latest current | `6000.5.8f1` | latest current production editor at matrix verification |

- [x] WEB5.1 Automate discovery/reporting of Unity's bundled Emscripten version for each gate.
  - Added `build/build.py web-unity-toolchains`, which discovers installed Unity Hub editors, resolves every gate to the exact pinned release, records `emscripten-version.txt`, reports `ready` / `missing-webgl` / `missing-editor`, writes `.build/evidence/web-unity-toolchains.json`, and supports `--require-complete` to fail closed when a release matrix is incomplete. WEB5 release pins are `2021.3.39f1`, `2022.3.62f1`, `2023.2.20f1`, `6000.0.81f1`, `6000.3.22f1`, and `6000.5.8f1`; an older installed patch no longer silently satisfies a latest-version gate.
- [x] WEB5.2 Run Development Player build + browser regression for each gate.
  - `2021.3.39f1` minimum row: PASS from WEB4 — Development Player and Chromium permanent regression `11/11`, Emscripten `2.0.19`.
  - `2022.3.62f1` row: PASS — WebGL module installed, direct public-header link/execute passes with Emscripten `3.1.8-git`, Development Player build succeeds, and Chromium/Test Framework XML reports **11/11 passed, 0 failed, 0 skipped**. Browser launch must use the `localhost` Player origin for the WebSocket PlayerConnection transport on this Linux validation host.
  - `2023.2.20f1` row: PASS — WebGL module reports Emscripten `3.1.39-git`; direct public-header link/execute passes; the Development WebGL Test Framework Player exits successfully and XML reports **11/11 passed, 0 failed, 0 skipped** with all permanent runtime markers present.
  - `6000.0.81f1` Unity 6 baseline row: PASS — WebGL module reports Emscripten `3.1.39-git`; the exact staged archive links/executes successfully; after removing a stale duplicate Linux plugin copy from the ignored validation host, the Development WebGL Player builds successfully and Test Framework XML reports **11/11 passed, 0 failed, 0 skipped** with all permanent runtime markers present.
  - `6000.3.22f1` latest-LTS row: PASS — WebGL module reports Emscripten `3.1.39-git`; the exact staged archive links/executes successfully; the Development WebGL Player build succeeds and Test Framework XML reports **11/11 passed, 0 failed, 0 skipped** with all permanent runtime markers present.
  - `6000.5.8f1` latest-current row: PASS — WebGL module reports Emscripten `4.0.20-git`; Unity 6000.5 API-obsolete breaks were handled with version-gated EntityId compatibility; the Development WebGL Player builds successfully and Test Framework XML reports **11/11 passed, 0 failed, 0 skipped** with all permanent runtime markers present and zero undefined symbols.
- [x] WEB5.3 Run release Player build for minimum, latest LTS, and latest current gates.
  - `2021.3.39f1` minimum release row: PASS from WEB4 — non-Development WebGL Player built successfully and Chromium emitted all **11/11** permanent runtime PASS markers, including responsive layout and TMP measurement.
  - `6000.3.22f1` latest-LTS release row: PASS — the release build modifier verified `Development=False`; the non-Development WebGL Player built successfully, and a manual headless Chromium run emitted all **11/11** permanent runtime PASS markers. Browser console evidence is retained under ignored `.build/web5-6000.3.22f1/release-browser.log`.
  - `6000.5.8f1` latest-current release row: PASS — the release build modifier verified `Development=False`; the non-Development WebGL Player linked successfully with Emscripten `4.0.20-git` and zero undefined symbols, and a manual headless Chromium run emitted all **11/11** permanent runtime PASS markers. Browser console evidence is retained under ignored `.build/web5-6000.5.8f1/release-browser.log`.
- [x] WEB5.4 Verify the same staged generic archive is used across the matrix.
  - `build/build.py verify-web-unity-matrix-links` verifies the staged package archive matches the canonical Rust build, then links and executes the exact checked-in `UnityPackage/Plugins/WebGL/libtaffy_ugui.a` against all six WEB5 gates. Every gate passes with the same SHA-256 `49dc545b6a3ada0b8652480931c47dc7e43bb10587470d11178f10b0592cda38`, including latest-current Emscripten `4.0.20-git`; evidence is retained in ignored `.build/evidence/web-unity-matrix-links.json`.
- [x] WEB5.5 If a future Unity release breaks archive compatibility, fail validation rather than silently claiming support.
  - `verify-web-unity-matrix-links` fails closed on missing editors, missing/incomplete Web toolchains, link failures, execution failures, or absent harness PASS markers. The complete six-editor matrix now passes, so future gate drift will fail validation rather than silently inheriting support.

**WEB5 phase gate: PASS** — all six pinned Unity/Emscripten generations pass Development WebGL runtime regression, minimum/latest-LTS/latest-current pass non-Development browser execution, and every exact gate links and executes the same staged generic archive.


---

# WEB6 — Browser, memory, performance, and release hardening

**Status: ACTIVE**

- [ ] WEB6.1 Chrome/Chromium current smoke.
- [ ] WEB6.2 Firefox current smoke.
- [ ] WEB6.3 Safari current smoke on a real supported Apple host/device.
- [ ] WEB6.4 deterministic 100 / 1,000 node layout checks.
- [ ] WEB6.5 repeated layout/rebuild stress.
- [ ] WEB6.6 memory growth and context/resource lifecycle checks.
- [ ] WEB6.7 compare Web layout outputs against maintained native/Editor golden cases.
- [ ] WEB6.8 verify no unexpected per-frame managed allocations were introduced specifically for Web.
- [ ] WEB6.9 package/download size review for the staged `.a`.
- [ ] WEB6.10 browser console remains free of Taffy native-link/runtime errors.

**WEB6 phase gate:** browser and reliability evidence is sufficient for production documentation.

---

# WEB7 — Optional multithreaded Unity Web compatibility

**Status: BLOCKED BY WEB6; NOT REQUIRED FOR INITIAL DEFAULT-WEB CLAIM**

- [ ] WEB7.1 Determine the exact Unity versions where the relevant Web multithreading option is available/supported.
- [ ] WEB7.2 Produce a shared-memory-compatible Rust archive using rebuilt core/std with required atomics/bulk-memory features.
- [ ] WEB7.3 Preserve safe registry/context behavior if native calls can occur from multiple Wasm workers.
- [ ] WEB7.4 Resolve artifact selection/packaging without making users install Rust or manually swap files.
- [ ] WEB7.5 Build with Unity's threaded Web setting.
- [ ] WEB7.6 Run in a browser with the required cross-origin isolation headers.
- [ ] WEB7.7 Add concurrent/wrong-thread behavior tests as applicable.
- [ ] WEB7.8 Only after all gates pass, document threaded Web as supported.

Until WEB7 completes, documentation must say that TaffyUGUI's supported Web configuration is the normal/default single-thread Web Player path.

---

# WEB8 — Documentation and release closeout

**Status: BLOCKED BY WEB6 (and WEB7 only if threaded support is advertised)**

- [ ] WEB8.1 Update package README/platform matrix.
- [ ] WEB8.2 Update installation docs with Web/WebGL support and browser requirements.
- [ ] WEB8.3 Add Web troubleshooting: undefined symbols, stale `.a`, browser memory, compression/server headers, threaded-build caveats.
- [ ] WEB8.4 Update changelog/release notes.
- [ ] WEB8.5 Update `PHASE12_REAL_UNITY_VALIDATION.md` or successor validation document with executed Web evidence.
- [ ] WEB8.6 Run final native Rust regression suite.
- [ ] WEB8.7 Run final Unity Edit Mode/Play Mode regression on maintained Editor gates.
- [ ] WEB8.8 Run final Web Player matrix.
- [ ] WEB8.9 Verify package contents contain the Web artifact and exclude temporary build/probe material.
- [ ] WEB8.10 Advertise only the Unity/browser/threading combinations that actually passed.

**WEB8 completion gate:** Web support is documented, reproducible, validated, and release-ready from Unity 2021.3 through the latest current validated Unity release.

---

## Feasibility risks and decisions

| Risk | Current assessment | Required handling |
|---|---|---|
| Rust/Emscripten ABI mismatch | Avoided on primary path | Generic `wasm32-unknown-unknown` archive |
| Cargo tries to create Emscripten `cdylib` side module | Avoided | Web-only staticlib manifest |
| Rust panic/unwind ABI | Avoided for broad compatibility | `panic=abort`; harden avoidable panic paths |
| Detailed Grid diagnostics | Preserved | Existing `std` Taffy configuration builds on generic Wasm |
| Unity 2021.3 old wasm linker | Passed | Emscripten `2.0.19` direct link/execute plus Development and release Player gates |
| Web multithreading | Feasible but separate | WEB7; do not advertise early |
| TMP measurement | Managed-side feature; expected portable | permanent in-browser WEB3/WEB4 test |
| Artifact size | Canonical archive `4,200,052` bytes (`4.0055 MiB`) | keep WEB6 package/download-size review; final Player dead-strips unused archive code |
| Existing Android/native behavior | Must not regress | non-Web regression required at each native phase |

## External facts verified for this plan

- Unity 2021.2+ supports GNU `.a` native Web plug-ins containing Wasm object files.
- Unity 2021.2/2021.3 uses the Emscripten 2.0.19.6-unity family; Unity 2022.2+ moved to 3.1.8-unity; Unity 2023.2 moved to 3.1.38-unity.
- Unity recommends rebuilding Emscripten-bound native plug-ins when the bundled Emscripten/LLVM generation changes because binary compatibility is not guaranteed.
- Rust documents the same ABI risk for `wasm32-unknown-emscripten` and recommends rebuilding `std` with the local Emscripten configuration when using that target.
- Unity `6000.5.7f1`, released 2026-08-05, is the latest current production Editor visible in Unity's release archive at WEB0 closeout.

These facts justify the generic Wasm-object primary architecture and the cross-version Player gates above.
