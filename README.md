# TaffyUGUI

Responsive Flexbox, Grid, Block, and related layout geometry for Unity uGUI, powered by Rust and [Taffy](https://github.com/DioxusLabs/taffy), while Unity keeps rendering, interaction, prefabs, TextMeshPro, ScrollRect, Canvas, and EventSystem.

## ⚠️ AI-Generated Project Disclaimer

**This project is currently fully AI-generated, including its source code, project structure, documentation, build scripts, and configuration.**

The project may be tested through automated tests, CI, and manual validation where applicable, but **there is no guarantee that the code is correct, secure, reliable, production-ready, or suitable for any particular purpose**. AI-generated code can contain bugs, incorrect assumptions, security vulnerabilities, platform-specific issues, or other unexpected behavior.

This software is provided **"AS IS", without warranty or guarantee of any kind**. Anyone using, modifying, distributing, or integrating this project is responsible for independently reviewing and testing it for their own requirements.

**Use this project entirely at your own risk.** Passing tests or CI checks should not be interpreted as a guarantee of safety, correctness, compatibility, or production readiness.

## Status

**Active native engine development.** Phase 0 — Rust Project and Toolchain Foundation is complete. The project is now in **Phase 1 — Complete Rust/Taffy 0.13 Engine**; user-facing Unity feature development remains intentionally gated until the complete native engine, cross-platform ABI release candidate, managed ABI conformance, final ABI v1 freeze, and native artifact rebuild are complete.

Current fixed baseline:

- Taffy `0.13.0`, exact-pinned.
- Rust MSRV `1.82.0`.
- pinned normal/release Rust toolchain `1.97.1`.
- primary Unity baseline `2021.3 LTS`.
- Android primary ABI `arm64-v8a`, using Unity 2021.3-compatible NDK r21d.
- Unity 2021.3 WebGL toolchain baseline: bundled/matched Emscripten 2.0.19.
- bootstrap ABI version `0`; final ABI v1 is frozen only after managed conformance testing.

The normative engineering decisions are in **[docs/PROJECT_DECISIONS.md](docs/PROJECT_DECISIONS.md)**. If another planning document ever appears to conflict with that file, `PROJECT_DECISIONS.md` is the controlling contract until the conflict is corrected.

TaffyUGUI is developed as two first-class deliverables:

1. a compiled **Rust/Taffy native layout library** with a TaffyUGUI-owned C ABI;
2. a **Unity UPM package** containing the managed uGUI integration plus verified native binaries under `UnityPackage/Plugins/`.

The implementation sequence is documented in [docs/DEVELOPMENT_PLAN.md](docs/DEVELOPMENT_PLAN.md), native build rules in [docs/NATIVE_LIBRARY_BUILD_PLAN.md](docs/NATIVE_LIBRARY_BUILD_PLAN.md), and live progress/next task in [docs/TASK_TRACKER.md](docs/TASK_TRACKER.md).

## Why TaffyUGUI

- Keep existing Unity uGUI instead of migrating to UI Toolkit.
- Use Taffy only for layout computation.
- Preserve Button, Image, TMP, ScrollRect, Canvas, EventSystem, and normal Unity rendering/input.
- Keep Rust/Taffy implementation details behind a stable fixed-width C ABI.
- Maintain a persistent native layout tree and use dirty-driven recomputation.
- Ship prebuilt native artifacts so ordinary Unity users do not need Rust installed.
- Target validated Windows, macOS, Android, iOS, and WebGL Unity player paths.

## Architecture

```text
Rust source + Taffy 0.13
        |
quality / golden / ABI tests
        |
ABI release candidate
        |
cross-platform native compilation
        |
minimal managed ABI conformance
        |
freeze ABI v1 + rebuild native artifacts
        |
UnityPackage/Plugins
        |
managed C# wrapper
        |
TaffyLayoutGroup + optional TaffyLayoutItem
        |
RectTransform
        |
normal Unity uGUI rendering and interaction
```

Rust never renders Unity UI. It computes geometry and layout metadata; Unity owns GameObjects, rendering, input, text rendering, scrolling, clipping, prefabs, and scenes.

## Repository layout

```text
native/                 canonical Rust/Taffy implementation
include/                generated public C API header location
build/build.py          authoritative build entry point
dist/                   generated build output (ignored)
UnityPackage/           installable UPM package / committed release plugin payload
docs/                   normative decisions, architecture, plans, tracker, audits
.github/workflows/       CI
scripts/                 temporary/bootstrap compatibility helpers only
```

## Native development

The normal repository toolchain is pinned automatically through `rust-toolchain.toml`. See [docs/NATIVE_DEVELOPMENT.md](docs/NATIVE_DEVELOPMENT.md) for clean-clone setup, module ownership, lockfile rules, and the native development workflow.

Run the complete local native quality gate with:

```bash
python build/build.py quality
```

Build the host release library with:

```bash
python build/build.py native host
```

The committed `native/Cargo.lock` is used for reproducible dependency resolution. Platform build commands remain behind the same `build/build.py` interface and are implemented as their development phase begins.

The production public C header will be generated with cbindgen at:

```text
include/taffy_ugui.h
```

using:

```bash
python build/build.py header
```

## Unity package

Package name:

```text
com.dofomii.taffyugui
```

The primary package baseline is Unity 2021.3 LTS and the package explicitly depends on Unity uGUI (`com.unity.ugui`). TMP integration remains optional through a separate adapter assembly later in development.

Release tags will contain verified native binaries and Unity importer metadata inside `UnityPackage/Plugins/`, allowing normal Git-URL UPM installation without requiring Rust or access to CI artifacts.

## License

TaffyUGUI is licensed under the [MIT License](LICENSE). It may be used, modified, distributed, sublicensed, and sold, including in commercial and closed-source projects, subject to the license notice requirements.

Taffy is a separate MIT-licensed dependency. Applicable third-party notices will be included in the release package.

## Project documents

- [Engineering decisions](docs/PROJECT_DECISIONS.md) — normative contract.
- [Task tracker](docs/TASK_TRACKER.md) — current phase and single next task.
- [Native development guide](docs/NATIVE_DEVELOPMENT.md) — clean-clone setup and daily native workflow.
- [End-to-end development plan](docs/DEVELOPMENT_PLAN.md).
- [Native library build plan](docs/NATIVE_LIBRARY_BUILD_PLAN.md).
- [Architecture](docs/ARCHITECTURE.md).
- [Project readiness audit](docs/PROJECT_READINESS_AUDIT.md).
- [Contributing](CONTRIBUTING.md).
