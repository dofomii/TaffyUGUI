# TaffyUGUI

Responsive Flexbox, Grid, Block, and CSS-style layout for Unity uGUI, powered by Rust and Taffy. Unity remains responsible for rendering, input, TextMeshPro, ScrollRect, animation, prefabs, and EventSystem behavior; TaffyUGUI computes layout geometry.

## AI-generated project disclaimer

**This project is currently fully AI-generated, including source code, project structure, documentation, build scripts, and configuration.** Automated and manual tests reduce risk but do not guarantee correctness, security, reliability, production readiness, or suitability for a particular product. Review and test the code independently before shipping it. The software is provided **AS IS**, without warranty.

## v1.0 release scope

- Final native ABI v1 (`version=1`, `stage=2`), exact Taffy `0.13.0`.
- Unity package baseline: **2021.3+**.
- Permanent tests pass on Unity **2021.3.39f1**, **2022.3.62f1**, and **6000.4.3f1** at **41/41 Edit Mode + 9/9 Play Mode** on each.
- Advertised Player target: **Android ARM64 only**.
- Unity 6 Android ARM64 IL2CPP packaging and physical-device execution have been validated.

## Install

After you choose to create the `v1.0.0` Git tag, the Git/UPM dependency will be:

```json
"com.dofomii.taffyugui": "https://github.com/dofomii/TaffyUGUI.git?path=/UnityPackage#v1.0.0"
```

For local testing, open **Window > Package Manager > + > Add package from disk...** and select:

```text
<checkout>/UnityPackage/package.json
```

The package folder is self-contained and includes documentation, samples, legal notices, and the Android ARM64 native plugin.

## Main features

- Flex row/column, wrapping, gaps, alignment, growth/shrink, and box model.
- Grid tracks, repeat/minmax/fraction/content sizing, named lines/areas, explicit and auto placement.
- Typed Calc expression trees for lengths and Grid sizing.
- Block/FlowRoot, float/clear, relative/absolute positioning, overflow, writing direction, and aspect ratio.
- Intrinsic measurement for TMP, legacy Text, Image, RawImage, and custom providers without native-to-managed callbacks during compute.
- Responsive breakpoints, safe area, pixel rounding, ScrollRect content integration, and rebuild diagnostics.
- Editor inspectors/property drawers, Scene visualization, Layout Debugger, and conservative legacy LayoutGroup migration.

## Documentation

User documentation ships inside the UPM package at `UnityPackage/Documentation~/`:

- [Getting Started](UnityPackage/Documentation~/getting-started.md)
- [Flexbox and Block](UnityPackage/Documentation~/flexbox.md)
- [Grid and Calc](UnityPackage/Documentation~/grid-and-calc.md)
- [Measurement and TextMeshPro](UnityPackage/Documentation~/measurement.md)
- [ScrollRect and Responsive Integration](UnityPackage/Documentation~/responsive-and-scrollrect.md)
- [Migration](UnityPackage/Documentation~/migration.md)
- [Platform Support](UnityPackage/Documentation~/platform-support.md)
- [Troubleshooting](UnityPackage/Documentation~/troubleshooting.md)

Maintainer verification and phase history live under [`docs/`](docs/).

## Samples

Import samples from Package Manager after installing TaffyUGUI:

- **Flex Quick Start**
- **Grid and Responsive**
- **Custom Measurement**

## Local verification

```bash
python3 build/build.py quality
python3 build/build.py verify-abi-final
python3 build/build.py native android-arm64
python3 build/build.py verify-native android-arm64
python3 build/build.py verify-phase4
python3 build/build.py stage-phase5
python3 build/build.py verify-phase5
```

The project is local-first; remote CI is not the release authority.

## License

MIT. See [LICENSE](LICENSE) and [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).
