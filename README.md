# TaffyUGUI

Responsive Flexbox and Grid layout engine for Unity uGUI, powered by Rust and [Taffy](https://github.com/DioxusLabs/taffy), while Unity keeps rendering, interaction, prefabs, TextMeshPro, ScrollRect, and EventSystem.

## Status

Early development. The repository currently provides the native Rust bridge, Unity package scaffolding, Flexbox-facing runtime components, CI, and cross-platform build scripts. Grid and release binaries will be added after the Flexbox ABI is validated across supported Unity targets.

## Why TaffyUGUI

- Keep existing Unity uGUI instead of migrating to UI Toolkit.
- Use Taffy only for layout computation.
- Preserve Button, Image, TMP, ScrollRect, Canvas, EventSystem, and normal Unity rendering.
- Use a stable C ABI between C# and Rust.
- Keep the native layout tree persistent and only recompute dirty layouts.
- Target Windows, macOS, Android, iOS, and WebGL.

## Architecture

```text
Unity uGUI / RectTransform
        |
TaffyLayoutGroup + TaffyLayoutItem
        |
      C ABI
        |
Rust wrapper -> TaffyTree
        |
 x / y / width / height
        |
RectTransform
```

Rust never renders UI. It only computes layout rectangles.

## Repository layout

```text
native/               Rust cdylib/staticlib wrapper around Taffy
UnityPackage/         Unity Package Manager package
scripts/              Cross-platform native build helpers
docs/                 Architecture and platform notes
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

See [CONTRIBUTING.md](CONTRIBUTING.md) and [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md).
