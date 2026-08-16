# TaffyUGUI

Responsive Flexbox, Grid, Block, and CSS-style layout for existing Unity uGUI, powered by Rust and Taffy. Unity continues to own rendering, interaction, TextMeshPro, ScrollRect, prefabs, animation, and EventSystem behavior; the native library owns layout geometry only.

## ⚠️ AI-Generated Project Disclaimer

**This project is currently fully AI-generated, including source code, project structure, documentation, build scripts, and configuration.** Automated and manual tests can reduce risk but do not guarantee correctness, security, reliability, production readiness, or suitability for any purpose. Review and test the project independently before shipping it. The software is provided **AS IS**, without warranty of any kind, and is used entirely at your own risk.

## Current development boundary

The native interface is **ABI-v1-RC (version 1, stage 1)** with Taffy **0.13.0** pinned exactly. The Unity runtime wrapper has been aligned to the `tu_*` ABI-v1-RC surface. Phase 4 starts with cross-platform native artifact compilation and packaging.

Development and verification are **local-first**. GitHub is only a repository backup/mirror; GitHub Actions are not required or used by the canonical build path.

## Local verification

```bash
python3 build/build.py doctor
python3 build/build.py static-gate
python3 build/build.py prepare
python3 build/build.py verify-abi-rc
```

The complete Phase 3 gate runs rustfmt, Clippy, Rust tests, release build, cbindgen drift verification, and linked C/C++ host smoke tests locally. See [docs/LOCAL_DEVELOPMENT.md](docs/LOCAL_DEVELOPMENT.md).

## Phase 4 targets

```bash
python3 build/build.py list-targets
python3 build/build.py native windows-x64
python3 build/build.py native macos-universal
python3 build/build.py native android-arm64
python3 build/build.py native ios-arm64
python3 build/build.py native webgl
```

Each target is ABI-gated, architecture-checked, export-checked, and staged under `dist/native/` with a manifest and SHA-256 checksum.

## Unity package

The package lives in `UnityPackage/` and targets Unity 2021.3+ until older Unity versions are explicitly validated. Add it as a local package or as a Git package path when using a backup mirror.

## License

MIT. See [LICENSE](LICENSE).
