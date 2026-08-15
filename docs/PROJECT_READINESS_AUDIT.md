# TaffyUGUI — Project Readiness Audit

**Audit date:** 2026-08-16  
**Repository:** `dofomii/TaffyUGUI`  
**Audited branch:** `main`  
**Purpose:** Verify that source, architecture, roadmap, task tracker, Rust/native strategy, Unity package strategy, CI, licensing, and release plan are internally consistent before active implementation begins.

---

# 1. Executive Verdict

## Overall status: CONDITIONAL GO

TaffyUGUI has a coherent architecture and a strong phase-based development model, but it is **not yet clean enough to begin Phase 1 feature implementation**.

It **is ready to begin Phase 0 stabilization immediately**.

Before the project is considered fully unambiguous, Phase 0 must resolve a small set of important baseline decisions and existing build failures described in this report.

The repository is not suffering from a fundamental architectural mistake. The main issues are:

1. the Taffy dependency baseline is already stale relative to the current official release;
2. current native CI is red;
3. `Cargo.lock` is absent;
4. ABI freeze timing is too early in the roadmap;
5. the exact Unity/toolchain compatibility baseline required for Android/WebGL/iOS native builds is not defined early enough;
6. a few ABI, Grid, Unity LayoutGroup, packaging, and build-system decisions are still ambiguous.

These should be fixed before the project calls its architecture frozen.

---

# 2. Audit Scope

The complete current repository tree was reviewed, including:

```text
.github/workflows/native-ci.yml
.gitignore
CONTRIBUTING.md
LICENSE
README.md

native/Cargo.toml
native/src/lib.rs

scripts/build-native.sh

UnityPackage/package.json
UnityPackage/Runtime/TaffyNative.cs
UnityPackage/Runtime/TaffyLayoutGroup.cs
UnityPackage/Runtime/TaffyLayoutItem.cs
UnityPackage/Runtime/TaffyUGUI.Runtime.asmdef

docs/ARCHITECTURE.md
docs/DEVELOPMENT_PLAN.md
docs/NATIVE_LIBRARY_BUILD_PLAN.md
docs/TASK_TRACKER.md
```

The original production planning document was also used as the target product boundary.

External dependency verification was performed against the official `DioxusLabs/taffy` repository and release metadata.

---

# 3. What Is Already Correct

## 3.1 Product architecture — PASS

The core architecture is correct:

```text
Rust/Taffy owns geometry
        ↓
stable TaffyUGUI C ABI
        ↓
Unity managed wrapper
        ↓
TaffyLayoutGroup / TaffyLayoutItem
        ↓
RectTransform
        ↓
normal Unity rendering/input
```

The project correctly avoids replacing:

- Canvas rendering;
- EventSystem;
- TextMeshPro rendering;
- Button/Image/ScrollRect behavior;
- prefabs and existing uGUI hierarchies.

The native-first direction is now consistently reflected in the master plan, native build plan, task tracker, and README.

## 3.2 Native source/package separation — PASS

The intended ownership boundary is clear:

```text
native/                 canonical Rust source
build/ or dist/         generated native output
UnityPackage/Plugins/   binaries shipped with the UPM package
UnityPackage/Runtime/   managed Unity integration
```

That is the correct long-term repository model.

## 3.3 License model — PASS

The repository uses the MIT License and explicitly includes the standard no-warranty terms.

Taffy is also MIT licensed.

The current license model therefore supports commercial, private, modified, redistributed, sublicensed, and sold usage subject to preservation of license notices.

Third-party notices still need to be generated before release, which is already planned.

## 3.4 AI-generated-code disclaimer — PASS

The README clearly states that the project is AI-generated, may contain defects, is provided without guarantee, and must be independently reviewed/tested.

## 3.5 Phase gating — PASS

The task tracker correctly establishes:

- one active phase at a time;
- explicit phase exit gates;
- regression obligations from completed phases;
- a single `NEXT TASK`;
- native milestone completion before active Unity feature work;
- platform support only after actual Unity player validation.

This is a strong development-control model.

---

# 4. Blocking / High-Priority Findings

## R1 — Taffy baseline must be decided before Phase 1

**Severity:** HIGH  
**Current state:** unresolved

The repository currently pins:

```toml
taffy = "=0.12.2"
```

However the official Taffy repository released **v0.13.0 on 2026-08-08**.

Taffy 0.13.0 includes significant fixes in Flexbox, Grid, Block, replaced-element behavior, content sizing, and absolute/Grid behavior, and changes parts of the Grid API such as `grid_template_areas`.

The project's original production planning document also targeted Taffy 0.13.0.

### Required decision

Before implementing the full native wrapper, explicitly choose one of:

```text
A. Upgrade/pin Taffy 0.13.0 now — recommended.
B. Intentionally remain on 0.12.2 and document the compatibility reason.
```

Do **not** build the complete ABI around 0.12.2 accidentally and then upgrade after Grid/measurement ABI work.

Taffy 0.13.0 declares Rust MSRV 1.71, so the current project `rust-version = 1.82` is sufficient.

---

## R2 — Native CI is currently red

**Severity:** HIGH  
**Current state:** known and correctly tracked

The latest `native-ci` run fails before full tests/release builds finish.

Current failures:

- `native/src/lib.rs` is not canonical `rustfmt` output;
- Clippy rejects the exported `unsafe extern "C"` functions because they lack `# Safety` documentation.

The crate reached compilation during Clippy, so there is no evidence of a basic Taffy API compile failure in that run, but the full native quality gate has not passed.

### Required action

Phase 0 must not complete until:

```text
cargo fmt --check
cargo clippy --all-targets -- -D warnings
cargo test
cargo build --release
```

are green on the intended host matrix.

---

## R3 — `Cargo.lock` is missing

**Severity:** HIGH  
**Current state:** planned but not implemented

The repository tree currently contains no `native/Cargo.lock`.

For a binary/native-plugin product, release dependency resolution must be reproducible.

This is already in the tracker and should remain a Phase 0 gate.

---

## R4 — ABI v1 should not be permanently frozen before the first managed ABI proof

**Severity:** HIGH  
**Current state:** roadmap design risk

The current plan freezes ABI v1 in native Phase 3, then cross-compiles every platform, and only afterward begins the C# managed/native bridge.

That sequence can freeze mistakes such as:

- struct packing assumptions;
- count-width mismatches;
- P/Invoke marshalling issues;
- iOS/WebGL linkage-specific signature constraints;
- managed buffer ownership mistakes.

### Recommended correction

Keep the native-first strategy, but distinguish:

```text
Phase 3: ABI candidate / release-candidate contract locked for cross-platform compilation
Phase 6: ABI v1 final freeze after managed struct/enum/PInvoke contract tests and one real Unity native smoke flow
```

This does **not** require delaying native platform compilation. It only avoids calling an unproven interface permanently frozen too early.

---

## R5 — Unity/toolchain compatibility baseline must be defined before Phase 4

**Severity:** HIGH  
**Current state:** missing sequencing decision

`UnityPackage/package.json` currently declares:

```json
"unity": "2021.3"
```

but the rewritten master plan no longer contains a concrete initial Unity compatibility/toolchain matrix before the native platform-build phase.

This matters because "Unity-compatible" native compilation depends on the selected Unity lane, especially:

- Android NDK version/toolchain;
- WebGL Emscripten version;
- Apple/Xcode requirements;
- architecture support.

### Required action

Before Phase 4, define an initial matrix such as:

```text
Primary: Unity 2021.3 LTS
Compatibility validation: Unity 2022.3 LTS / selected Unity 6 lane
Optional backward validation: Unity 2019.4 only after tests prove it
```

For Android/WebGL, record the exact NDK/Emscripten toolchain used by the selected Unity installation/build lane.

Without this, "fully Unity-compatible native binary" is ambiguous.

---

# 5. Important Architecture Clarifications

## R6 — Context handle contract is still ambiguous

**Severity:** MEDIUM-HIGH

The original production plan requires opaque 64-bit context handles.

The current bootstrap ABI exposes a raw pointer:

```rust
*mut c_void
```

and managed code stores it as `IntPtr`.

The tracker says raw/public pointers should be replaced with opaque context handles "where required", which leaves the final contract ambiguous.

### Recommended decision

Make the final ABI explicit:

```text
TaffyUGUI context handle = opaque uint64 handle
TaffyUGUI node handle    = opaque uint64 generation-safe handle
```

Use a controlled registry/arena with owner-thread validation rather than accepting arbitrary raw pointers as public ABI state.

---

## R7 — The C ABI needs an authoritative C header generation task

**Severity:** MEDIUM-HIGH

The Phase 2 gate mentions a generated or maintained header, and Phase 3 requires a C/C++ smoke harness, but there is no explicit task that creates the authoritative header.

Add a task to produce a deterministic C header, manually generated from one source of truth or using a tool such as `cbindgen` after evaluation.

The C smoke harness should compile against that same header.

---

## R8 — Panic policy is not fully resolved

**Severity:** MEDIUM-HIGH

Current `Cargo.toml` contains:

```toml
panic = "abort"
```

while the design also contemplates a `TU_PANIC` error and panic boundaries.

With abort semantics, a Rust panic terminates the process; it cannot generally be converted into a normal `TU_PANIC` result.

### Required decision

Before ABI freeze, explicitly choose/document target policy:

```text
- validate inputs so expected failures never panic;
- define whether supported targets use catch_unwind + unwind builds;
- define which targets must use abort semantics;
- make TU_PANIC capability/behavior match reality.
```

Do not document recoverable panic behavior that the release profile cannot provide.

---

## R9 — Grid feature checklist is slightly incomplete relative to the target document

**Severity:** MEDIUM

The original product plan explicitly includes:

- `justify_items`;
- `justify_self`;
- named Grid areas/lines/templates as advanced Grid authoring.

The rewritten tracker uses broad wording such as "Grid alignment" and does not explicitly track named lines/areas.

Taffy 0.13.0 exposes named Grid-line/area types, and its `grid_template_areas` API changed in 0.13.0.

### Required action

When the Taffy baseline is locked, make the native/Unity task list explicit for every exposed Grid alignment and named-area/line feature that belongs to v1.

If named areas/lines are intentionally postponed beyond v1, state that explicitly rather than silently dropping them.

---

## R10 — Unity LayoutGroup must report its own layout input sizes

**Severity:** MEDIUM-HIGH

The production source plan required `TaffyLayoutGroup` to report min/preferred/flexible size through Unity's layout system.

The current rewritten Unity tasks focus on child collection, native computation, and `SetChildAlongAxis`, but do not explicitly require:

```text
SetLayoutInputForAxis(...)
```

for the Taffy group itself.

This is important for:

- nested Taffy groups;
- parent Unity LayoutGroups;
- ContentSizeFitter behavior;
- correct `ILayoutElement` participation.

Add an explicit Unity task and regression tests for group min/preferred/flexible input reporting.

---

# 6. Build / Packaging Findings

## R11 — One authoritative build-system location should be chosen

**Severity:** MEDIUM

The master/native plans consistently show:

```text
build/build.py
```

but the tracker still says the authoritative driver may live under `build/` **or** `scripts/`, and the repository currently contains only:

```text
scripts/build-native.sh
```

### Recommended decision

Use:

```text
build/build.py
```

as the future authoritative cross-platform driver.

Treat `scripts/build-native.sh` as temporary bootstrap tooling and mark/remove it when Phase 4 begins.

---

## R12 — Current `build-native.sh webgl` is not evidence of Unity Web compatibility

**Severity:** MEDIUM

The current script simply adds/builds `wasm32-unknown-emscripten`.

That is insufficient to prove Unity Web/WebGL compatibility because the Emscripten version must match the Unity lane.

The plans correctly state this, but the bootstrap script should eventually be renamed/documented so developers do not mistake it for the production WebGL pipeline.

---

## R13 — Native binary source-control/release policy is ambiguous

**Severity:** MEDIUM

The final plan supports Git URL UPM installation from `UnityPackage/`.

For that to work from a Git tag, the native binaries must exist in that tagged repository package payload, or the project must use a generated release branch/repository containing them.

The roadmap currently says binaries are staged under `UnityPackage/Plugins`, but does not explicitly state whether they are committed to release tags.

### Required decision

Document one policy, for example:

```text
Development main:
    binaries may be refreshed/staged as required.

Release tag:
    verified Plugins binaries + .meta files are committed as part of the tagged UPM payload,
    enabling Git URL installation without Rust or CI artifact access.
```

A release tarball can additionally be generated from the same verified payload.

---

## R14 — UPM package should explicitly depend on uGUI

**Severity:** MEDIUM

The runtime assembly references `UnityEngine.UI`, but `package.json` currently declares no `dependencies` section.

Because TaffyUGUI fundamentally requires Unity uGUI, the release package should explicitly declare the supported `com.unity.ugui` dependency appropriate to its Unity baseline.

TMP should remain optional through the separate adapter strategy.

---

# 7. Current Source-Code Audit

## 7.1 Rust bootstrap

The current native implementation is correctly treated as scaffold, not final architecture.

Known intentional shortcomings relative to the final plan:

- `HashMap<u64, NodeId>` without generation handles;
- raw context pointer;
- no Grid/Block/measurement API yet;
- no version/build/capability API beyond a single numeric API version;
- no bulk operations;
- no structured error diagnostics;
- no panic recovery policy;
- no native tests;
- tree style is currently Flex-only;
- no `Cargo.lock`.

These are all appropriate Phase 0–3 work rather than evidence that the architecture is wrong.

## 7.2 Current Unity scaffold

The current C# code must **not** be mistaken for the intended production implementation.

Important bootstrap behavior that later phases must replace:

- dirty layout rebuild destroys/recreates the whole native context/tree;
- full layout is currently computed from both `SetLayoutHorizontal()` and `SetLayoutVertical()`, potentially doing duplicate work;
- node results are read one-by-one rather than bulk;
- temporary lists/arrays allocate during rebuild;
- ABI handshake occurs after context creation rather than before native state is used;
- no managed `IDisposable` native-context wrapper;
- no domain-reload cleanup integration;
- no proper group min/preferred/flexible layout-input reporting;
- no native binary is currently packaged.

The tracker correctly pauses active Unity feature development, so these do not block Phase 0 native work.

---

# 8. CI Findings

## Existing CI — useful but bootstrap-level

Current workflow checks Linux, Windows, and macOS with:

```text
fmt
clippy
test
release build
```

This is the right Phase 0 foundation.

### Improvements already implied by later phases

- locked dependency builds (`--locked`);
- MSRV lane if an MSRV is publicly declared;
- native golden/ABI tests;
- platform artifact jobs;
- artifact upload;
- architecture/symbol validation;
- Unity tests;
- release assembly.

### Small maintenance item

The workflow currently uses `actions/checkout@v4`, while GitHub has released newer major versions and current runners already warn that v4 targets the deprecated Node 20 runtime.

This is not a project blocker, but should be updated during Phase 0 CI cleanup.

---

# 9. Missing Files / Directories That Are NOT Current Blockers

The repository does not yet contain the following planned structures:

```text
native/Cargo.lock
build/
tests/
dist/
UnityPackage/Plugins/
UnityPackage/Editor/
UnityPackage/Runtime.TMP/
UnityPackage/Tests/
UnityPackage/Samples~/nCHANGELOG.md
SECURITY.md
THIRD_PARTY_NOTICES.md
```

Except for `Cargo.lock`, these are **not considered mistakes at the current phase**. They are intentionally created by later tasks.

Release/security/third-party files must exist before v1.0.

---

# 10. Pre-Development Readiness Checklist

Before beginning the large Phase 1 native feature implementation, complete these items:

- [ ] Choose and pin the intended Taffy baseline, preferably evaluate/upgrade to 0.13.0 now.
- [ ] Fix rustfmt failure.
- [ ] Fix Clippy safety-documentation failures.
- [ ] Obtain a fully green host CI run.
- [ ] Generate and commit `Cargo.lock`.
- [ ] Define/test Rust MSRV/release toolchain policy.
- [ ] Decide final context-handle representation.
- [ ] Add an explicit authoritative C-header task.
- [ ] Define panic/unwind/abort behavior.
- [ ] Change Phase 3 "ABI v1 freeze" to an ABI candidate lock; perform final v1 freeze after managed contract validation.
- [ ] Define the primary Unity compatibility/toolchain lane before native platform cross-compilation.
- [ ] Choose `build/` as the authoritative build-driver location (or explicitly choose another location once).
- [ ] Clarify Grid named-area/line and justify-items/self v1 scope.
- [ ] Add TaffyLayoutGroup own min/preferred/flexible-size reporting to the Unity phase.
- [ ] Define Git-release handling of native binaries for UPM Git URL installs.
- [ ] Add explicit uGUI package dependency before Unity package validation/release.

---

# 11. Readiness Decision

## Can development start?

**YES — Phase 0 development can start now.**

The project has enough architectural clarity to begin resolving the Rust foundation immediately.

## Can the team start implementing the full Phase 1 Rust feature engine immediately without revisiting the plan?

**NO.**

First close the high-priority baseline items:

1. Taffy 0.13.0 vs 0.12.2 decision;
2. green CI;
3. `Cargo.lock`;
4. ABI freeze timing;
5. Unity/toolchain compatibility baseline;
6. final handle/panic/header/build-driver decisions.

Once those are closed, the project is ready for sustained implementation.

## Can Unity feature development start?

**NO.**

That remains correctly gated behind the Native Milestone.

---

# 12. Final Assessment

The project direction is strong and the native-first roadmap is materially better than the original bootstrap state.

There is **no reason to redesign the whole project**.

The remaining issues are mostly preflight contract decisions and Phase 0 quality work. The largest avoidable mistake would be implementing the full native ABI on Taffy 0.12.2 and freezing ABI v1 before validating the managed C# contract.

After the readiness checklist above is incorporated into the tracker and completed in Phase 0, TaffyUGUI will have a solid, low-ambiguity foundation for the full Rust → cross-platform native artifacts → Unity integration → UPM release development sequence.
