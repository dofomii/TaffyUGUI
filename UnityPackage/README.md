# TaffyUGUI 1.1.0

TaffyUGUI adds Flexbox, Grid, Block/Float, intrinsic measurement, and responsive layout to existing Unity uGUI hierarchies. Unity continues to own rendering, input, animation, prefabs, TextMeshPro, ScrollRect, and the EventSystem; a Rust/Taffy native library computes layout geometry.

## Supported release target

- Unity Editor compatibility validated on **2021.3.39f1**, **2022.3.62f1**, and **6000.4.3f1**.
- The sole advertised Player target in v1.1 is **Android ARM64**.
- A physical Android ARM64 device run is part of the completed release evidence; Windows, macOS, iOS, WebGL, and Linux Player are not advertised by this package release.

## Install

After a `v1.1.0` tag is intentionally created, add this dependency to `Packages/manifest.json`:

```json
"com.dofomii.taffyugui": "https://github.com/dofomii/TaffyUGUI.git?path=/UnityPackage#v1.1.0"
```

For a local checkout, use **Window > Package Manager > + > Add package from disk...** and select this package's `package.json`.

## Start here

Read [Getting Started](Documentation~/getting-started.md), then import one of the samples from Package Manager. The main components are:

- `TaffyLayoutGroup` on the uGUI container;
- `TaffyLayoutItem` on children that need explicit Taffy style or measurement control.

Detailed documentation is indexed at [Documentation](Documentation~/index.md).

## License

MIT. See [LICENSE.md](LICENSE.md) and [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).
