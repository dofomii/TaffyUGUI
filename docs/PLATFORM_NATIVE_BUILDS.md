# TaffyUGUI Phase 4 Platform Native Builds

Phase 4 compiles the **ABI-v1-RC** native library for every required Unity platform family. The authoritative entry point is `python build/build.py`.

## ABI-RC gate

Platform builds intentionally refuse to run unless `native/src/version.rs` declares ABI version `1` and stage `1` (`ABI-v1-RC`). This prevents candidate-stage binaries from being mistaken for platform release-candidate payloads.

## Target registry

| Command target | Rust target | Output staging |
|---|---|---|
| `windows-x64` | `x86_64-pc-windows-msvc` | `dist/native/windows/x86_64/` |
| `macos-arm64` | `aarch64-apple-darwin` | `dist/native/macos/arm64/` |
| `macos-x64` | `x86_64-apple-darwin` | `dist/native/macos/x86_64/` |
| `android-arm64` | `aarch64-linux-android` | `dist/native/android/arm64-v8a/` |
| `ios-arm64` | `aarch64-apple-ios` | `dist/native/ios/arm64/` |
| `webgl` | `wasm32-unknown-emscripten` | `dist/native/webgl/wasm32/` |

`python build/build.py native macos` builds both macOS slices and assembles a universal dylib with `lipo`. `python build/build.py native ios` aliases the required iOS ARM64 target. `native all` builds only targets compatible with the current host/toolchains; CI is responsible for combining the platform-family lanes.

## Toolchain requirements

Windows x64 uses the MSVC Rust target on a Windows runner. macOS and iOS require Xcode command-line tools on macOS. Android ARM64 requires `ANDROID_NDK_HOME` or `ANDROID_NDK_ROOT` pointing specifically to Unity 2021.3-compatible NDK r21d revision `21.3.6528147`; API 21 Clang is selected. WebGL requires `emcc` version `2.0.19`, matching the Unity 2021.3 baseline.

Rust targets are never installed implicitly by the build command. A missing target fails with an explicit `rustup target add <triple>` instruction so CI and release images remain deterministic.

## Artifact verification and manifests

Every target build uses locked dependencies, checks file format/architecture, verifies mandatory `tu_*` exported symbols, then copies only the verified binary into `dist/native/<platform>/<arch>/`.

Each staged directory contains `manifest.json` and `SHA256SUMS`. The manifest records package/native version, ABI designation/version/stage, Taffy version, Rust target, source commit, artifact filename, architecture, crate type, file description, checksum, and panic strategy.

Use `python build/build.py verify-native <target...>` to re-check staged manifest/checksum consistency. `dist/` remains generated/ignored output and is uploaded as CI artifacts rather than committed.
