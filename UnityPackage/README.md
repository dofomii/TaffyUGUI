# TaffyUGUI 1.1.2

TaffyUGUI adds Flexbox, Grid, Block/Float, intrinsic measurement, and responsive layout to existing Unity uGUI hierarchies. Unity continues to own rendering, input, animation, prefabs, TextMeshPro, ScrollRect, and the EventSystem; a Rust/Taffy native library computes layout geometry.

## Bundled native targets

- Unity Editor compatibility validated on **2021.3.39f1**, **2022.3.62f1**, and **6000.4.3f1**.
- The package bundles **Android ARM64, Windows x86/x64, and Linux x86/x64** native plugins.
- Android ARM64 has physical-device release evidence. Windows x86/x64 are PE/export verified on the Linux release host. Linux x86 is provided for legacy consumers; current Unity Linux Players are 64-bit.

## Install

After a `v1.1.2` tag is intentionally created, add this dependency to `Packages/manifest.json`:

```json
"com.dofomii.taffyugui": "https://github.com/dofomii/TaffyUGUI.git?path=/UnityPackage#v1.1.2"
```

For a local checkout, use **Window > Package Manager > + > Add package from disk...** and select this package's `package.json`.

## Start here

Read [Getting Started](Documentation~/getting-started.md), then import one of the samples from Package Manager. The main components are:

- `TaffyLayoutGroup` on the uGUI container;
- `TaffyLayoutItem` on children that need explicit Taffy style or measurement control.

Detailed documentation is indexed at [Documentation](Documentation~/index.md).

## License

MIT. See [LICENSE.md](LICENSE.md) and [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).
