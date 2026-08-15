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

Before the project is considered fully unambiguous, Phase 0 must resolve the baseline decisions and build failures listed in this report.

There is no fundamental architecture failure. The main issues are:

1. Taffy baseline needs to be finalized before the full wrapper is implemented.
2. Native CI is currently red.
3. `Cargo.lock` is absent.
4. Permanent ABI v1 freeze is scheduled too early.
5. The Unity/toolchain compatibility baseline needed for Android/WebGL/iOS native builds is not defined early enough.
6. A few ABI, Grid, Unity LayoutGroup, packaging, and build-system details are still ambiguous.

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

The original production planning document was used as the target product boundary. Official Taffy repository/release metadata was checked for dependency facts.

---

# 3. Areas Already in Good Shape

## Product architecture — PASS

The core boundary is correct:

```text
Rust/Taffy geometry engine
        ↓
TaffyUGUI-owned C ABI
        ↓
managed Unity wrapper
        ↓
TaffyLayoutGroup / TaffyLayoutItem
        ↓
RectTransform
        ↓
normal Unity rendering/input
```

Unity remains responsible for Canvas rendering, EventSystem, TMP rendering, Button/Image/ScrollRect behavior, animation, prefabs, and scenes.

## Native/source/package separation — PASS

The intended ownership is clear:

```text
native/                 canonical Rust source
dist/native/            generated native outputs
UnityPackage/Plugins/   native binaries shipped to users
UnityPackage/Runtime/   managed Unity integration
```

## Licensing — PASS

The project uses MIT and contains the standard no-warranty language. Taffy is also MIT licensed. Third-party/transitive notices still need release-time verification, which is already part of the release goal.

## AI disclaimer — PASS

The README clearly identifies the project as AI-generated and states that testing does not imply warranty or production safety.

## Phase gating — PASS

The tracker correctly enforces one active phase, phase exit gates, regression obligations, a single `NEXT TASK`, a Native Milestone before Unity feature development, and actual player validation before a platform is advertised.

---

# 4. High-Priority Findings

## R1 — Taffy baseline must be finalized before Phase 1

**Severity:** HIGH

The source currently pins:

```toml
taffy = "=0.12.2"
```

The official Taffy project released **v0.13.0 on 2026-08-08**. It includes many Flexbox/Grid/Block fixes and changes Grid API details such as `grid_template_areas`. The original production plan also referenced 0.13.0.

### Required decision

Before implementing the complete native wrapper:

```text
A. evaluate and pin 0.13.0 — recommended; or
B. intentionally remain on 0.12.2 and document the reason.
```

Do not build the entire Grid/measurement ABI accidentally around 0.12.2 and upgrade afterward.

Taffy 0.13.0 declares Rust MSRV 1.71, so the project's current `rust-version = 1.82` satisfies it.

---

## R2 — Native CI is red

**Severity:** HIGH

The current workflow fails before the full test/release pipeline completes.

Known failures:

- `native/src/lib.rs` is not canonical `rustfmt` output.
- Clippy requires `# Safety` documentation on exported unsafe FFI functions.

Phase 0 must not complete until all of these are green:

```text
cargo fmt --check
cargo clippy --all-targets -- -D warnings
cargo test
cargo build --release
```

---

## R3 — `Cargo.lock` is missing

**Severity:** HIGH

No `native/Cargo.lock` exists in the current tree. A binary/native-plugin product needs a reproducible dependency graph. This is correctly planned as a Phase 0 task and must remain a gate.

---

## R4 — Permanent ABI v1 freeze is scheduled too early

**Severity:** HIGH

The roadmap currently freezes ABI v1 before the first managed C# contract proof, then cross-compiles all platforms.

That can prematurely freeze mistakes involving:

- P/Invoke struct packing;
- fixed-width count types;
- managed buffer ownership;
- iOS/WebGL linkage signatures;
- enum marshalling.

### Recommended correction

Keep native-first development, but use:

```text
Phase 3: ABI candidate/RC lock for cross-platform builds
Phase 6: final ABI v1 freeze after managed ABI tests and one actual Unity/native smoke flow
```

Native libraries can still be cross-compiled before Unity feature development; only the word/finality of the ABI freeze changes.

---

## R5 — Unity/toolchain baseline must be established before native platform compilation

**Severity:** HIGH

`UnityPackage/package.json` declares:

```json
"unity": "2021.3"
```

but the new native-first plan does not establish the initial Unity/toolchain matrix early enough.

This is required because "Unity-compatible" cross-compilation depends on:

- Unity-compatible Android NDK;
- Unity-compatible WebGL Emscripten;
- Apple/Xcode requirements;
- supported CPU architectures.

### Required action

Before Phase 4, define the primary baseline and record the exact toolchains used. A sensible starting policy is:

```text
Primary baseline: Unity 2021.3 LTS
Compatibility lanes: Unity 2022.3 LTS and selected Unity 6 version
Backward validation: Unity 2019.4 only after the complete package proves compatible
```

The exact validated matrix should be updated from actual test results, not assumptions.

---

# 5. ABI and Native Architecture Clarifications

## R6 — Final context handle type is ambiguous

**Severity:** MEDIUM-HIGH

The bootstrap ABI uses `*mut c_void` / managed `IntPtr`, while the original production plan calls for opaque 64-bit handles.

The final contract should be explicit. Recommended:

```text
context handle = opaque uint64 generation-safe handle
node handle    = opaque uint64 generation-safe handle
```

Use a controlled registry/arena and owner-thread validation rather than exposing arbitrary raw pointers as the final public ABI.

---

## R7 — Authoritative C header is missing from the task list

**Severity:** MEDIUM-HIGH

The Phase 2 gate refers to a generated/maintained header and Phase 3 requires a C/C++ smoke harness, but no task explicitly creates the authoritative header.

Add a task to generate or deterministically maintain the C header from a single source of truth. The native smoke harness must compile against that same header.

---

## R8 — Panic policy is unresolved

**Severity:** MEDIUM-HIGH

Current release profile:

```toml
panic = "abort"
```

The architecture also contemplates panic boundaries / `TU_PANIC` behavior. With abort semantics, a panic normally terminates the process and cannot be converted to a normal error code.

Before ABI freeze, define the real policy per target:

- validate expected invalid inputs instead of panicking;
- decide whether any supported targets use unwind + `catch_unwind`;
- define targets where abort is mandatory;
- make `TU_PANIC` semantics match what is actually possible.

---

## R9 — Grid feature scope is slightly underspecified

**Severity:** MEDIUM

The original target document explicitly includes `justify_items`, `justify_self`, and advanced named Grid areas/lines/templates. The rewritten tracker uses broad "Grid alignment" wording and does not explicitly track named lines/areas.

Once the Taffy version is selected, explicitly include every v1 Grid feature or explicitly defer it. Do not silently lose functionality from the product goal.

---

# 6. Unity Architecture Clarification

## R10 — `TaffyLayoutGroup` own layout-input reporting needs an explicit task

**Severity:** MEDIUM-HIGH

A production `LayoutGroup` must not only place children; it also has to participate correctly as an `ILayoutElement` when nested or used with other Unity layout controllers.

The Unity phase should explicitly implement/test group min/preferred/flexible reporting through the normal Unity layout-input path, including `SetLayoutInputForAxis(...)` where appropriate.

This matters for:

- nested Taffy groups;
- parent Unity LayoutGroups;
- ContentSizeFitter;
- preferred-size calculations.

---

# 7. Build and Packaging Clarifications

## R11 — Choose one authoritative build-driver location

**Severity:** MEDIUM

The plans mostly use:

```text
build/build.py
```

but the tracker still allows `build/` or `scripts/`, while the current repository contains only `scripts/build-native.sh`.

Recommended: make `build/build.py` the production entry point and treat `scripts/build-native.sh` as bootstrap tooling to be retired or clearly marked temporary.

---

## R12 — Current WebGL shell script is not Unity-Web proof

**Severity:** MEDIUM

The existing `build-native.sh webgl` simply builds `wasm32-unknown-emscripten`. That is not enough to prove Unity Web compatibility because the Emscripten version must match the selected Unity lane.

The master plan already understands this; the bootstrap script should eventually be labelled accordingly so it does not create false confidence.

---

## R13 — Native binary Git/release policy is ambiguous

**Severity:** MEDIUM

The package plans to support Git URL installation from `UnityPackage/`.

A release tag therefore needs the actual verified native binaries in the package tree, or a dedicated generated release branch/repository containing them. CI-only artifacts are not available to Unity Package Manager during a normal Git install.

Document the policy before release tooling is implemented.

Recommended release behavior:

```text
Release tag/package payload:
    UnityPackage/Plugins binaries + .meta files are present and verified.

Release archive:
    generated from the same verified payload.
```

---

## R14 — UPM package should declare its uGUI dependency

**Severity:** MEDIUM

The runtime assembly references `UnityEngine.UI`, but `package.json` currently has no `dependencies` section.

Before Unity package validation/release, explicitly declare the supported `com.unity.ugui` dependency for the chosen baseline. TMP should remain optional via the separate adapter assembly.

---

# 8. Current Source-Code Status

## Rust bootstrap

The current native implementation is correctly treated as scaffold. It currently has:

- `HashMap<u64, NodeId>` without generation protection;
- raw context pointer;
- Flex-only style conversion;
- no Grid/Block/measurement ABI;
- no bulk operations;
- minimal error codes;
- no build/Taffy/capability version contract;
- no native test suite;
- no `Cargo.lock`.

These are expected Phase 0–3 tasks, not reasons to redesign the project.

## Unity bootstrap

The existing C# code is also scaffold and must not be treated as production-ready. Current behavior includes:

- dirty rebuild destroys and recreates the native context/tree;
- both `SetLayoutHorizontal()` and `SetLayoutVertical()` compute the full layout, causing duplicate work;
- per-node result calls instead of bulk result retrieval;
- temporary managed allocations;
- ABI check after context creation instead of a safer pre-context handshake;
- no managed `IDisposable` context wrapper;
- no domain-reload lifecycle management;
- no complete group preferred/min/flexible reporting;
- no packaged native binary.

The tracker correctly pauses Unity feature work, so these do not block Phase 0 native stabilization.

---

# 9. CI Status

The existing workflow is a useful Phase 0 foundation:

```text
Linux / Windows / macOS
fmt
clippy
test
release build
```

Later phases correctly need locked builds, ABI/golden tests, target artifact jobs, symbol/architecture checks, Unity tests, and release packaging.

Small maintenance item: the workflow still uses `actions/checkout@v4`; current GitHub runners warn about its deprecated Node 20 runtime and a newer major checkout release exists. Update this during Phase 0 CI cleanup.

---

# 10. Planned Files That Are Not Mistakes Yet

The current repository does not yet contain:

```text
native/Cargo.lock
build/
tests/
dist/
UnityPackage/Plugins/
UnityPackage/Editor/
UnityPackage/Runtime.TMP/
UnityPackage/Tests/
UnityPackage/Samples~/
CHANGELOG.md
SECURITY.md
THIRD_PARTY_NOTICES.md
```

Except for `Cargo.lock`, these are not current-phase failures. They are intentionally created by later phases. Release/security/third-party files must exist before v1.0.

---

# 11. Pre-Phase-1 Readiness Checklist

Before starting the large full-native-engine implementation:

- [ ] Evaluate and lock Taffy 0.13.0 vs 0.12.2.
- [ ] Fix rustfmt.
- [ ] Fix Clippy safety documentation.
- [ ] Obtain fully green host CI.
- [ ] Generate/commit `Cargo.lock` and use locked builds.
- [ ] Define/test Rust MSRV and release toolchain policy.
- [ ] Decide the final context-handle representation.
- [ ] Add an authoritative C-header task.
- [ ] Define panic/unwind/abort policy.
- [ ] Treat Phase 3 as an ABI candidate lock and perform final ABI v1 freeze after managed contract proof.
- [ ] Define primary Unity/toolchain compatibility before Phase 4.
- [ ] Choose one authoritative build-driver location.
- [ ] Clarify Grid named-area/line and justify-items/self v1 scope.
- [ ] Add `TaffyLayoutGroup` own min/preferred/flexible reporting to Unity tasks.
- [ ] Define Git-release handling of packaged native binaries.
- [ ] Add explicit uGUI dependency before package validation/release.

---

# 12. Readiness Decision

## Can development start?

**YES — Phase 0 stabilization can start now.**

The architecture is clear enough to begin correcting the Rust foundation.

## Can full Phase 1 native feature implementation start immediately without revisiting the plan?

**NO.**

First close the high-priority baseline decisions: Taffy version, green CI, lockfile, ABI freeze timing, Unity/toolchain baseline, and final handle/panic/header/build-driver contracts.

## Can Unity feature development start?

**NO.**

It remains correctly gated behind the Native Milestone.

---

# 13. Final Assessment

The native-first roadmap is fundamentally sound. There is no reason to redesign the project.

The remaining work is mainly preflight contract cleanup and Phase 0 engineering. The two largest avoidable mistakes would be:

1. implementing the full native API against Taffy 0.12.2 without first deciding whether to move to 0.13.0; and
2. declaring ABI v1 permanently frozen before the first managed C# ABI proof.

Once the Pre-Phase-1 checklist is incorporated into the active tracker and completed, TaffyUGUI will have a strong, low-ambiguity foundation for the complete sequence:

```text
Rust/Taffy engine
→ native ABI
→ cross-platform native artifacts
→ Unity managed integration
→ uGUI features/editor tooling
→ real player validation
→ production UPM release
```
