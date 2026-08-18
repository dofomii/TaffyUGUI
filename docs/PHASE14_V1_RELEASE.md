# Phase 14 — v1.0 Release Closeout

**Status:** COMPLETE — release-ready, intentionally unpublished
**Date:** 2026-08-18
**Package version:** 1.0.0
**Native ABI:** ABI v1 (`version=1`, `stage=2`)
**Taffy:** 0.13.0
**Advertised Player target:** Android ARM64 only

Phase 14 turns the validated package into a self-contained v1.0 UPM distribution and proves local-path, Git-path, tarball, Unity-version, sample, native, and Android packaging gates. No remote publication, push, real repository tag, or GitHub release is part of this closeout.

## P14.1 final compatibility matrix

| Environment | UPM dependency resolution | Imported sample compile | Edit Mode | Play Mode | Player support |
|---|---:|---:|---:|---:|---|
| Unity 2021.3.39f1 / Linux Editor | Pass | Pass | 41/41 | 9/9 | Editor/package validation |
| Unity 2022.3.62f1 / Linux Editor | Pass | Pass | 41/41 | 9/9 | Editor/package validation |
| Unity 6000.4.3f1 / Linux Editor | Pass | Pass | 41/41 | 9/9 | Android ARM64 IL2CPP validated |
| Android ARM64 / Unity 6 IL2CPP | n/a | n/a | n/a | n/a | **Supported** |
| Windows x64 Player | n/a | n/a | n/a | n/a | Not advertised |
| macOS Intel/Apple Silicon Player | n/a | n/a | n/a | n/a | Not advertised |
| iOS ARM64 Player | n/a | n/a | n/a | n/a | Not advertised |
| WebGL Player | n/a | n/a | n/a | n/a | Not advertised |
| Linux Player | n/a | n/a | n/a | n/a | Not advertised |

All three final UPM consumer hosts imported every packaged sample source into `Assets` before running the suite. Therefore the sample compile gate is independent of `Samples~` being hidden while resident inside a package.

Unity 2021.3 on the current newer Linux host still requires the known temporary Bee `--stdin-canary` workaround. The original executable was restored afterward to SHA-256 `8561ed19e6d35e1e947b450dd528867e7c43c9fe43b5cce9086b58d3cad4fa67`.

## P14.2–P14.9 user documentation

The distributable `UnityPackage/` now contains a complete `Documentation~/` set:

- `getting-started.md`;
- `flexbox.md`;
- `grid-and-calc.md`;
- `measurement.md`;
- `responsive-and-scrollrect.md`;
- `migration.md`;
- `platform-support.md`;
- `troubleshooting.md`;
- `index.md`.

The package root also carries its own `README.md`, `CHANGELOG.md`, `LICENSE.md`, and `THIRD_PARTY_NOTICES.md`, so the UPM artifact is understandable without the repository's historical phase documentation.

## P14.10 packaged samples

`package.json` advertises three importable UPM samples under `Samples~/`:

1. **Flex Quick Start** — visible row layout with explicit child sizes, gap, and alignment.
2. **Grid and Responsive** — Grid fraction tracks, Calc sizing, and a narrow breakpoint that switches to a Flex column.
3. **Custom Measurement** — `ITaffyMeasurementProvider` implementation demonstrating managed intrinsic measurement before native compute.

The source for all three samples compiles on Unity 2021.3.39f1, 2022.3.62f1, and 6000.4.3f1.

## P14.11 third-party notices audit

The locked runtime dependency tree is:

- `taffy 0.13.0` — MIT;
- `arrayvec 0.7.8` — MIT OR Apache-2.0, distributed under MIT terms;
- `slotmap 1.1.1` — Zlib;
- `smallvec 1.15.2` — MIT OR Apache-2.0, distributed under MIT terms.

`THIRD_PARTY_NOTICES.md` includes the relevant notice/license text. uGUI and TextMeshPro are UPM dependencies resolved by Unity and are not bundled in the TaffyUGUI archive.

## P14.12 changelog/release notes

`CHANGELOG.md` and the package-local changelog now contain the 1.0.0 release entry dated 2026-08-18, covering the feature surface, compatibility claim, Android-only scope, and AI-generated project disclaimer.

## P14.13 UPM and Git installation validation

Three fresh Unity projects installed the working `UnityPackage/` via a Package Manager local dependency. Final dependency behavior:

- Unity 2021.3 resolves uGUI 1.0.0 and TMP 3.0.6;
- Unity 2022.3 resolves uGUI 1.0.0 and TMP 3.0.7;
- Unity 6000.4 resolves uGUI 2.0.0 and the TMP 5.0.0 compatibility shim.

All resolve and compile without package errors.

Git installation syntax was validated without publishing by creating an ignored temporary Git repository under `.build`, committing the exact package, applying a **temporary local-only `v1.0.0` tag**, and installing it into Unity 6 through `git+file://...?...path=/UnityPackage#v1.0.0`. Unity recorded the dependency source as `git` and compiled with zero errors. No tag was created in the real project repository.

The eventual remote install syntax, only after an intentional real `v1.0.0` tag is created, is:

```text
https://github.com/dofomii/TaffyUGUI.git?path=/UnityPackage#v1.0.0
```

## P14.14 package archive validation

A deterministic local UPM tarball is produced at ignored path:

```text
dist/release/TaffyUGUI-1.0.0.tgz
```

The archive uses the conventional `package/` root and contains the package metadata, Android plugin, docs, legal files, tests, and three samples. An archive scan finds no `.build`, harness, or probe content. Unity 6 installs the tarball as a `local-tarball` package dependency with zero compile errors.

The archive is deliberately not committed; it is a generated release artifact. `dist/release/SHA256SUMS` records its local checksum. Final deterministic archive SHA-256: `b4f60dedfeaa8c81381de6385607103c24938368a4610c6af736003d36bf7c5a`.

## P14.15 version/tag/release packaging

Release metadata is frozen at `1.0.0` in both `UnityPackage/package.json` and the native Rust crate. `Cargo.lock` is updated accordingly. The Android library was rebuilt after the version change, so `tu_copy_build_version` reports the v1.0 package version from the native binary.

Final source snapshot used by native/Android provenance:

`sha256:676771e84efb0f8ab0d8cfb14cbf4c388bce500d88fd1870c50babe0d368fed8`

Final Android ARM64 native library SHA-256:

`85cb8ef34fc03c51cc40baaf4bdbbd45892a616d93958d21f4f86100303e51a7`

A fresh Unity 6 Android ARM64 IL2CPP APK builds with the v1.0 plugin. It contains `libil2cpp.so` and `libtaffy_ugui.so`; both packaged Taffy `PT_LOAD` segments exactly match the staged library. The device visible through ADB during final closeout is offline, so no new Phase 14 device execution is claimed; Phase 12 remains the latest successful physical-device runtime validation.

No real Git tag is created in this phase closeout because publication was explicitly deferred.

## P14.16 publication gate

All technical release gates are complete. **Publication was intentionally not performed by owner instruction.** No push, GitHub release, registry publication, real `v1.0.0` tag, or other public distribution action was executed.

The repository is therefore **v1.0.0 release-ready and unpublished**.

## Final gate

Phase 14 is complete under the owner's unpublished-release constraint. Any future publication should start from the clean Phase 14 commit, rerun the short final verification gate, then intentionally create/push `v1.0.0` and release artifacts.

Disposable Git/tarball/Unity validation hosts remain under ignored `.build`/`dist` paths and are not project source.
