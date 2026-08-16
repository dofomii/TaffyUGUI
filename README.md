# TaffyUGUI

Responsive Flexbox, Grid, Block, and CSS-style layout for existing Unity uGUI, powered by Rust and Taffy. Unity continues to own rendering, interaction, TextMeshPro, ScrollRect, prefabs, animation, and EventSystem behavior; the native library owns layout geometry only.

## ⚠️ AI-Generated Project Disclaimer

**This project is currently fully AI-generated, including source code, project structure, documentation, build scripts, and configuration.** Automated and manual tests can reduce risk but do not guarantee correctness, security, reliability, production readiness, or suitability for any purpose. Review and test the project independently before shipping it. The software is provided **AS IS**, without warranty of any kind, and is used entirely at your own risk.

## Current progress

The native interface is **ABI-v1-RC (version 1, stage 1)** with Taffy **0.13.0** pinned exactly. The production native engine and `tu_*` C ABI are implemented and locally verified. The active release scope is intentionally **Android ARM64 only**.

| Phase | Status |
|---|---|
| 0 — Rust/toolchain foundation | **Complete** |
| 1 — Complete native Taffy engine | **Implementation complete** |
| 2 — Production C ABI | **Implementation complete** |
| 3 — Native verification / ABI RC | **Complete; canonical local compiled gate passed** |
| 4 — Native artifact release gate | **Complete for Android ARM64-only scope** |
| 5 — Unity-ready native staging | **Complete for Android ARM64** |
| 6 — Managed ABI conformance / final ABI v1 | Ready; partial early P/Invoke scaffolding exists |
| 7 — Minimal working Unity product | Prototype scaffolding exists; formal phase gated |
| 8–14 — Production integration through v1.0 | Not started |

The verified Android ARM64 library is staged under `UnityPackage/Plugins/Android/arm64-v8a/` together with Android-only PluginImporter metadata and source/checksum provenance. Windows, macOS, iOS, and WebGL remain deferred and are not supported by this branch.

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

## Android native release and Unity staging

The active release target is Android ARM64. The exact clean source tree must first pass:

```bash
python3 build/build.py verify-abi-rc
python3 build/build.py native android-arm64
python3 build/build.py verify-phase4
```

`verify-phase4` accepts the Android ARM64 artifact and writes `dist/native/phase4-index.json`. Phase 5 then stages and verifies the package payload with:

```bash
python3 build/build.py stage-phase5
python3 build/build.py verify-phase5
```

The packaged binary is `UnityPackage/Plugins/Android/arm64-v8a/libtaffy_ugui.so`. Its checksum must match the verified Phase 4 artifact exactly. See [docs/PHASE4_PLATFORM_BUILDS.md](docs/PHASE4_PLATFORM_BUILDS.md) and [docs/TASK_TRACKER.md](docs/TASK_TRACKER.md).

Deferred Windows, macOS, iOS, and WebGL build definitions remain available for future branches but do not gate or define support for the active Android-only release.

### Unity 2022 WebGL: deferred legacy branch

**Decision:** Unity 2022 WebGL native-plugin support will be delivered later on a dedicated, maintained legacy branch. It is not part of the active Phase 4 branch and must not change the mainline Rust, Taffy, or Unity 2021.3 WebGL baseline merely to make Unity 2022 build.

**Why a separate branch is necessary:** Unity requires a WebGL native plugin to be compiled with the Emscripten version embedded in the consuming Unity Editor, because their LLVM-generated objects are binary-compatible only with the matching compiler version. Unity 2022's WebGL baseline is Emscripten `3.1.8`; the current project is Rust `1.97.1` with Taffy `0.13.0`, while mainline's current Unity 2021.3 WebGL baseline is Emscripten `2.0.19`. The latter fails before an archive is produced: LLVM 13 `wasm-ld` aborts on the Rust WebAssembly runtime objects with `unknown symbol kind`. Replacing only Emscripten is not valid for Unity 2022 because its Editor requires the matching `3.1.8` toolchain. Therefore supporting Unity 2022 natively requires an independently pinned legacy Rust/dependency/compiler stack, rather than a mainline toolchain change.

**Tests performed:** these are all compiler-compatibility tests completed to date. No Unity 2022 `3.1.8` candidate build or Unity 2022 WebGL Player test has been run.

- Rust `1.97.1` + Emscripten `2.0.19`: release WebGL link failed with `wasm-ld: unknown symbol kind` in Rust runtime archives.
- The same build with `panic=abort`: failed with the same linker error; the runtime still supplied an unwind archive.
- Unity 6.3/6.4 bundled Emscripten `3.1.39-git` and standalone Emscripten `3.1.38`: linking advanced further but optimized `wasm-opt` failed on unsupported `--enable-bulk-memory-opt`.
- Standalone Emscripten `4.0.19`: the optimized Rust release build succeeded and its archive exposed all 31 required `tu_*` symbols. This is compiler-only evidence, not Unity Player validation, and it cannot be used by Unity 2022.

**Required before that branch can claim Unity 2022 support:** pin and record the exact Unity 2022 Editor/WebGL Support package and its Emscripten `3.1.8` binaries; create a clean reproducible legacy build; validate the static archive and all 31 ABI exports; compile and build a Unity 2022 WebGL Player; then run representative Flexbox, Grid, Block, Calc, lifetime/error, and managed ABI-conformance tests in the browser. Until all of these pass, Unity 2022 WebGL is not a supported target. The compiler-matching requirement and Unity 2022 Emscripten baseline are from Unity's [Unity 2022.3 native Web plugin documentation](https://docs.unity3d.com/2022.3/Documentation/Manual/webgl-native-plugins-with-emscripten.html).

## Unity package

The package lives in `UnityPackage/` and targets Unity 2021.3+ until older Unity versions are explicitly validated. The low-level P/Invoke wrapper has already been aligned to the `tu_*` ABI-v1-RC as early Phase 6 scaffolding, but production managed conformance and user-facing Unity phases remain gated behind the final native payload sequence.

## License

MIT. See [LICENSE](LICENSE).
