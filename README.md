# TaffyUGUI

Responsive Flexbox, Grid, Block, and CSS-style layout for existing Unity uGUI, powered by Rust and Taffy. Unity continues to own rendering, interaction, TextMeshPro, ScrollRect, prefabs, animation, and EventSystem behavior; the native library owns layout geometry only.

## ⚠️ AI-Generated Project Disclaimer

**This project is currently fully AI-generated, including source code, project structure, documentation, build scripts, and configuration.** Automated and manual tests can reduce risk but do not guarantee correctness, security, reliability, production readiness, or suitability for any purpose. Review and test the project independently before shipping it. The software is provided **AS IS**, without warranty of any kind, and is used entirely at your own risk.

## Current progress

The native interface is **ABI-v1-RC (version 1, stage 1)** with Taffy **0.13.0** pinned exactly. The production native engine and `tu_*` C ABI are implemented. The current authoritative boundary is the **local Phase 3 compiled verification gate**, followed by real Phase 4 cross-platform artifact production.

| Phase | Status |
|---|---|
| 0 — Rust/toolchain foundation | **Complete** |
| 1 — Complete native Taffy engine | **Implementation complete** |
| 2 — Production C ABI | **Implementation complete** |
| 3 — Native verification / ABI RC | **ABI locked; canonical local compiled gate pending** |
| 4 — Cross-platform native builds | **Build infrastructure complete; real artifacts pending** |
| 5 — Unity-ready native staging | Not started |
| 6 — Managed ABI conformance / final ABI v1 | Partial early P/Invoke scaffolding only |
| 7 — Minimal working Unity product | Prototype scaffolding exists; formal phase gated |
| 8–14 — Production integration through v1.0 | Not started |

The current sandbox passes the provider-independent static gate, including C11/C++17 public-header compilation and native/managed contract checks. It cannot honestly complete the Rust/platform build gates because the required binary toolchains and SDKs are not available in this environment.

For the full current state, use:

- [Project status](docs/PROJECT_STATUS.md)
- [Complete task tracker](docs/TASK_TRACKER.md)
- [Production roadmap](docs/ROADMAP.md)
- [Documentation index](docs/README.md)
- [Local verification status](docs/LOCAL_VERIFICATION_STATUS.md)

## Completed native phase documentation

- [Phase 0 — Foundation](docs/PHASE0_FOUNDATION.md)
- [Phase 1 — Native engine](docs/PHASE1_NATIVE_ENGINE.md)
- [Phase 2 — Production C ABI](docs/PHASE2_PRODUCTION_C_ABI.md)
- [Phase 3 — Native verification](docs/PHASE3_NATIVE_VERIFICATION.md)
- [Phase 4 — Cross-platform builds](docs/PHASE4_PLATFORM_BUILDS.md)

## Local-first development

Development and verification are **local-first**. GitHub is only a repository backup/mirror; GitHub Actions are not part of the canonical build authority.

Provider-independent checks:

```bash
python3 build/build.py doctor
python3 build/build.py static-gate
```

Complete native Phase 3 verification on a correctly provisioned local host:

```bash
python3 build/build.py prepare
python3 build/build.py verify-abi-rc
```

That gate runs rustfmt, Clippy, Rust tests, release build, cbindgen drift verification, and linked C/C++ host smoke tests locally. See [docs/LOCAL_DEVELOPMENT.md](docs/LOCAL_DEVELOPMENT.md).

## Phase 4 targets

Phase 4 is a local multi-host build. Every artifact-producing machine must first pass `verify-abi-rc` on the byte-identical clean source tree.

```bash
python3 build/build.py list-targets
python3 build/build.py phase4-status
python3 build/build.py phase4-host
```

Canonical ownership is:

- Windows → Windows x64
- macOS → macOS arm64/x64/universal + iOS ARM64
- Linux → Android ARM64 + WebGL

Each accepted artifact is ABI-gated, architecture-checked, checked against the complete public `tu_*` export set, and staged under `dist/native/` with a manifest and SHA-256 checksum. After collecting all local-host outputs:

```bash
python3 build/build.py verify-phase4
```

The final Phase 4 gate verifies same-source/same-ABI/export parity and writes `dist/native/phase4-index.json`. See [docs/PHASE4_PLATFORM_BUILDS.md](docs/PHASE4_PLATFORM_BUILDS.md).

## Unity package

The package lives in `UnityPackage/` and targets Unity 2021.3+ until older Unity versions are explicitly validated. The low-level P/Invoke wrapper has already been aligned to the `tu_*` ABI-v1-RC as early Phase 6 scaffolding, but production managed conformance and user-facing Unity phases remain gated behind the final native payload sequence.

## License

MIT. See [LICENSE](LICENSE).
