# TaffyUGUI

Responsive Flexbox and Grid layout engine for Unity uGUI, powered by Rust and [Taffy](https://github.com/DioxusLabs/taffy), while Unity keeps rendering, interaction, prefabs, TextMeshPro, ScrollRect, and EventSystem.

## ⚠️ AI-Generated Project Disclaimer

**This project is currently fully AI-generated, including its source code, project structure, documentation, build scripts, and configuration.**

The project may be tested through automated tests, CI, and manual validation where applicable, but **there is no guarantee that the code is correct, secure, reliable, production-ready, or suitable for any particular purpose**. AI-generated code can contain bugs, incorrect assumptions, security vulnerabilities, platform-specific issues, or other unexpected behavior.

This software is provided **"AS IS", without warranty or guarantee of any kind**. Anyone using, modifying, distributing, or integrating this project is responsible for independently reviewing and testing it for their own requirements.

**Use this project entirely at your own risk.** Passing tests or CI checks should not be interpreted as a guarantee of safety, correctness, compatibility, or production readiness.

## Status

Early development. The repository currently provides the native Rust bridge, Unity package scaffolding, Flexbox-facing runtime components, CI, and cross-platform build scripts. Grid and release binaries will be added after the Flexbox ABI is validated across supported Unity targets.

TaffyUGUI is developed as **two first-class deliverables**:

1. a compiled **Rust/Taffy native layout library** with a stable TaffyUGUI-owned C ABI;
2. a **Unity UPM package** containing the managed uGUI integration plus the verified platform-specific Rust binaries under `UnityPackage/Plugins/`.

A phase is not considered complete when only the C# side works or only the Rust source compiles. The required native library for that phase must compile, pass its native verification, and execute through the Unity managed/native boundary.

The complete implementation sequence, acceptance gates, Unity integration architecture, testing strategy, editor tooling, migration workflow, platform build matrix, and v1.0 definition of done are documented in **[docs/DEVELOPMENT_PLAN.md](docs/DEVELOPMENT_PLAN.md)**.

The Rust compilation, ABI, platform artifact, native smoke-test, and Unity binary-packaging contract is defined in **[docs/NATIVE_LIBRARY_BUILD_PLAN.md](docs/NATIVE_LIBRARY_BUILD_PLAN.md)**.

**Live development progress, the active phase, blockers, stability gates, and the single next task are tracked in [docs/TASK_TRACKER.md](docs/TASK_TRACKER.md).**

## Why TaffyUGUI

- Keep existing Unity uGUI instead of migrating to UI Toolkit.
- Use Taffy only for layout computation.
- Preserve Button, Image, TMP, ScrollRect, Canvas, EventSystem, and normal Unity rendering.
- Use a stable C ABI between C# and Rust.
- Keep the native layout tree persistent and only recompute dirty layouts.
- Target Windows, macOS, Android, iOS, and WebGL.

## Architecture

```text
Rust source + Taffy
        |
compile/test native library
        |
platform DLL / dylib / SO / static library
        |
UnityPackage/Plugins
        |
      C ABI
        |
TaffyLayoutGroup + TaffyLayoutItem
        |
Unity uGUI / RectTransform
        |
normal Unity rendering and interaction
```

Rust never renders UI. It computes layout rectangles; Unity applies and renders them.

## Repository layout

```text
native/               Rust cdylib/staticlib wrapper around Taffy
UnityPackage/         Unity Package Manager package and packaged native binaries
scripts/              Cross-platform native build/package helpers
docs/                 Architecture, development, task and native build plans
.github/workflows/     CI
```

## Unity installation

During development, install from a local checkout using Unity Package Manager:

```text
<repo>/UnityPackage
```

Once tagged releases are available, Git URL installation will be documented here.

## Native development

Requirements:

- Rust stable
- Cargo
- Target platform toolchains (MSVC/Xcode/Android NDK/Emscripten as applicable)

Build the host library:

```bash
cargo build --manifest-path native/Cargo.toml --release
```

Run Rust tests:

```bash
cargo test --manifest-path native/Cargo.toml
```

The production pipeline will build each supported target, verify its ABI with a native smoke test or target-equivalent validation, then package the artifact under `UnityPackage/Plugins/<platform>/`. See [docs/NATIVE_LIBRARY_BUILD_PLAN.md](docs/NATIVE_LIBRARY_BUILD_PLAN.md).

## Initial Unity API

Use `TaffyLayoutGroup` on a parent `RectTransform` and optionally `TaffyLayoutItem` on children.

Initial layout properties include:

- Row / Column
- NoWrap / Wrap / WrapReverse
- gap
- padding
- grow / shrink / basis
- width / height
- min / max
- justify-content
- align-items
- align-self

The implementation is intentionally independent from Taffy's Rust types at the C# boundary.

## License

TaffyUGUI is licensed under the [MIT License](LICENSE). It may be used, modified, distributed, sublicensed, and sold, including in commercial and closed-source projects, subject to the license notice requirements.

Taffy is a separate dependency and is also available under permissive licensing; downstream users remain responsible for preserving applicable third-party notices.

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md), [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md), [docs/DEVELOPMENT_PLAN.md](docs/DEVELOPMENT_PLAN.md), [docs/NATIVE_LIBRARY_BUILD_PLAN.md](docs/NATIVE_LIBRARY_BUILD_PLAN.md), and [docs/TASK_TRACKER.md](docs/TASK_TRACKER.md).
