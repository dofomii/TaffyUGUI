# Platform Support and Compatibility

## v1.1.2 bundled native targets

The package bundles native plugins for **Android ARM64, Windows x86/x64, and Linux x86/x64**. macOS, iOS, and WebGL binaries are not part of this release archive.

## Compatibility and validation matrix

| Environment / binary | Package compile | Edit Mode | Play Mode | Release evidence |
|---|---:|---:|---:|---|
| Unity 2021.3.39f1 / Linux Editor | Pass | 140/140 maintained baseline | 9/9 maintained runtime baseline | Editor/package compatibility |
| Unity 2022.3.62f1 / Linux Editor | Pass | 140/140 maintained baseline | 9/9 maintained runtime baseline | Editor/package compatibility |
| Unity 6000.4.3f1 / Linux Editor | Pass | 140/140 maintained gate | 9/9 maintained gate | Exact-package gate is rerun for release artifacts |
| Android ARM64 / Unity 6 IL2CPP | n/a | n/a | n/a | Bundled; physical-device smoke exists and the release binary is 16 KB page compatible |
| Windows x64 | n/a | n/a | n/a | Bundled; PE x86-64 architecture, ABI exports, and embedded version verified on Linux build host |
| Windows x86 | n/a | n/a | n/a | Bundled for legacy consumers; PE i386 architecture, ABI exports, and embedded version verified on Linux build host |
| Linux x64 | n/a | n/a | n/a | Bundled; ELF x86-64 architecture, ABI exports, and embedded version verified locally |
| Linux x86 | n/a | n/a | n/a | Bundled for legacy consumers; ELF i386 architecture, ABI exports, and embedded version verified locally; modern Unity Linux Players are 64-bit |
| macOS / iOS / WebGL | n/a | n/a | n/a | Not bundled in 1.1.2 |

Windows runtime execution is not available on this Linux release host, so Windows validation is intentionally limited to deterministic cross-build plus binary architecture/export/version inspection. Linux x86 is included because the native library can still be built and consumed by legacy 32-bit hosts, but current Unity Linux Player tooling is 64-bit.

## Unity 2021 Linux validation note

Unity 2021.3.39f1 required a temporary local `bee_backend --stdin-canary` compatibility workaround on the newer Linux validation host. The Unity installation was restored byte-for-byte afterward. This was an Editor/toolchain-host issue, not a TaffyUGUI source workaround.

## Native plugin paths

- `Plugins/Android/arm64-v8a/libtaffy_ugui.so`
- `Plugins/Windows/x86/taffy_ugui.dll`
- `Plugins/Windows/x86_64/taffy_ugui.dll`
- `Plugins/Linux/x86/libtaffy_ugui.so`
- `Plugins/Linux/x86_64/libtaffy_ugui.so`

All five are built against final ABI v1 (`version=1`, `stage=2`) and Taffy 0.13.0.
