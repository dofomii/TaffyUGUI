# TaffyUGUI Web Support Task Tracker

**Program status:** ACTIVE  
**Support target:** Unity `2021.3 LTS` through the latest current Unity release  
**Current latest release at plan verification:** Unity `6000.5.7f1` (2026-08-05)  
**Current phase:** WEB0 COMPLETE — WEB1 READY  
**Authoritative next task:** WEB1.1

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

**Status: READY**

- [ ] **WEB1.1** Add a Web-only Cargo manifest that reuses `native/src/lib.rs` and emits only `staticlib`.
- [ ] WEB1.2 Add a reproducible build command/target for `wasm32-unknown-unknown`.
- [ ] WEB1.3 Pin Web build flags to `-Ctarget-cpu=mvp -Cpanic=abort` unless a stricter compatibility gate proves another setting is needed.
- [ ] WEB1.4 Ensure the Web build remains independent of Unity/Emscripten headers and libraries.
- [ ] WEB1.5 Verify the archive contains exactly the expected public `tu_*` ABI surface and no accidental public ABI expansion.
- [ ] WEB1.6 Add a permanent native/Web link harness based on `include/taffy_ugui.h` covering ABI version, context lifecycle, node creation, layout compute, and layout retrieval.
- [ ] WEB1.7 Audit Web panic behavior. Because the compatibility build uses `panic=abort`, remove avoidable internal `expect`/panic paths at the Web boundary where practical and document the remaining invariant behavior.
- [ ] WEB1.8 Decide and test Web handling for `CapThreadLocalContexts` without weakening non-Web behavior.
- [ ] WEB1.9 Measure release archive size and apply safe size optimizations only if they do not reduce old-linker compatibility.
- [ ] WEB1.10 Keep all generated/probe artifacts outside tracked source except the intentional staged package artifact.

**WEB1 phase gate:** generic archive reproducibly builds from clean source; ABI/link harness passes; non-Web Rust tests and native artifacts remain unchanged.

---

# WEB2 — Unity package integration

**Status: BLOCKED BY WEB1**

- [ ] WEB2.1 Stage `libtaffy_ugui.a` under `UnityPackage/Plugins/WebGL/`.
- [ ] WEB2.2 Add deterministic `.meta` importer settings: Web/WebGL enabled, Editor and all non-Web targets disabled.
- [ ] WEB2.3 Verify Unity does not attempt to load the Web archive in Editor Play Mode.
- [ ] WEB2.4 Verify `TaffyNative.Library == "__Internal"` remains the Web Player path and no separate DLL name is introduced.
- [ ] WEB2.5 Add package tests that validate importer configuration and artifact presence.
- [ ] WEB2.6 Verify UPM/Git package packaging includes the `.a` and its `.meta`.

**WEB2 phase gate:** package imports cleanly and the Web artifact is included only in Web Player builds.

---

# WEB3 — Permanent Web runtime regression scene/harness

**Status: BLOCKED BY WEB2**

Create a deterministic Web Player test that exercises the package through normal `TaffyLayoutGroup` / `TaffyLayoutItem` APIs, not only direct native calls.

- [ ] WEB3.1 ABI version/stage/capability handshake.
- [ ] WEB3.2 Flex row/column layout.
- [ ] WEB3.3 Grid tracks, placement, gaps, and detailed Grid diagnostics.
- [ ] WEB3.4 Block/FlowRoot behavior retained where applicable.
- [ ] WEB3.5 Calc values.
- [ ] WEB3.6 responsive profiles and forced/automatic profile resolution.
- [ ] WEB3.7 TextMeshPro intrinsic measurement and width-constrained wrapping.
- [ ] WEB3.8 uGUI Text/Image/RawImage measurement regressions where supported by the existing package tests.
- [ ] WEB3.9 bulk style/topology/measurement/layout ABI calls.
- [ ] WEB3.10 repeated context/node/resource create-destroy cycles.
- [ ] WEB3.11 nested groups and ScrollRect integration.
- [ ] WEB3.12 no `DllNotFoundException`, undefined `tu_*`, abort, or native-link error markers.

**WEB3 phase gate:** runtime marker reports a deterministic pass in an actual browser Player.

---

# WEB4 — Unity 2021.3 minimum-version gate

**Status: BLOCKED BY WEB3**

This is the highest-risk compatibility gate and must pass before the package advertises Unity 2021.3 Web support.

- [ ] WEB4.1 Install WebGL Build Support for `2021.3.39f1` (or the accepted minimum patch used by package validation).
- [ ] WEB4.2 Record the exact bundled Emscripten version from `emscripten-version.txt`.
- [ ] WEB4.3 Link the generic archive with the Unity 2021.3 Web toolchain before running a full Unity build.
- [ ] WEB4.4 Build a Development WebGL Player.
- [ ] WEB4.5 Run the WEB3 regression in a supported desktop browser.
- [ ] WEB4.6 Build a non-Development/release WebGL Player.
- [ ] WEB4.7 Verify TMP measurement and responsive layout in-browser.
- [ ] WEB4.8 Record build size, startup memory, and any Unity-2021-specific workaround.

If WEB4.3 rejects the generic object archive because of LLVM/Wasm object-version compatibility, execute the fallback decision in the locked architecture section before changing the public API.

**WEB4 phase gate:** Unity 2021.3 Development and release Web players both run the permanent regression successfully.

---

# WEB5 — Cross-version Unity Web matrix

**Status: BLOCKED BY WEB4**

Required release gates should cover meaningful Unity/Emscripten generations, not every patch.

| Gate | Required editor | Reason |
|---|---|---|
| Minimum | `2021.3.39f1` | package minimum; Emscripten 2.0 family |
| LTS generation | `2022.3.62f1` or maintained 2022.3 gate | Emscripten 3.1.8 family |
| 2023 generation | latest practical `2023.2` patch | Emscripten 3.1.38 family |
| Unity 6 baseline | maintained `6000.0` LTS patch | Unity 6 Web baseline |
| Latest LTS | latest supported LTS at release time | long-lived customer target |
| Latest current | latest current production editor at release time; currently `6000.5.7f1` | forward compatibility gate |

- [ ] WEB5.1 Automate discovery/reporting of Unity's bundled Emscripten version for each gate.
- [ ] WEB5.2 Run Development Player build + browser regression for each gate.
- [ ] WEB5.3 Run release Player build for minimum, latest LTS, and latest current gates.
- [ ] WEB5.4 Verify the same staged generic archive is used across the matrix.
- [ ] WEB5.5 If a future Unity release breaks archive compatibility, fail validation rather than silently claiming support.

**WEB5 phase gate:** all required version rows pass from the same package artifact.

---

# WEB6 — Browser, memory, performance, and release hardening

**Status: BLOCKED BY WEB5**

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
| Unity 2021.3 old wasm linker | Not executed locally yet | WEB4 mandatory link + Player gate |
| Web multithreading | Feasible but separate | WEB7; do not advertise early |
| TMP measurement | Managed-side feature; expected portable | permanent in-browser WEB3/WEB4 test |
| Artifact size | ~11.6 MB static archive in spike | measure/optimize in WEB1/WEB6; final Wasm dead-strips unused code |
| Existing Android/native behavior | Must not regress | non-Web regression required at each native phase |

## External facts verified for this plan

- Unity 2021.2+ supports GNU `.a` native Web plug-ins containing Wasm object files.
- Unity 2021.2/2021.3 uses the Emscripten 2.0.19.6-unity family; Unity 2022.2+ moved to 3.1.8-unity; Unity 2023.2 moved to 3.1.38-unity.
- Unity recommends rebuilding Emscripten-bound native plug-ins when the bundled Emscripten/LLVM generation changes because binary compatibility is not guaranteed.
- Rust documents the same ABI risk for `wasm32-unknown-emscripten` and recommends rebuilding `std` with the local Emscripten configuration when using that target.
- Unity `6000.5.7f1`, released 2026-08-05, is the latest current production Editor visible in Unity's release archive at WEB0 closeout.

These facts justify the generic Wasm-object primary architecture and the cross-version Player gates above.
