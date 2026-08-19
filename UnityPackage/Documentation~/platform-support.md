# Platform Support and Compatibility

## v1.1 support claim

**Advertised Player target: Android ARM64 only.** Other Player targets are intentionally not advertised in v1.1 even where build definitions or historical experiments exist.

## Final compatibility matrix

| Environment | Package compile | Edit Mode | Play Mode | Player claim |
|---|---:|---:|---:|---|
| Unity 2021.3.39f1 / Linux Editor | Pass | 140/140 | 9/9 maintained runtime baseline | Editor/package validation only |
| Unity 2022.3.62f1 / Linux Editor | Pass | 140/140 | 9/9 maintained runtime baseline | Editor/package validation only |
| Unity 6000.4.3f1 / Linux Editor | Pass | 140/140 | 9/9 | Android ARM64 IL2CPP validated |
| Android ARM64 / Unity 6 IL2CPP | n/a | n/a | n/a | **Supported**; physical-device smoke passed during Phase 12 and fresh Phase 13 packaging/ELF validation passed |
| Windows x64 Player | n/a | n/a | n/a | Not advertised |
| macOS Intel / Apple Silicon Player | n/a | n/a | n/a | Not advertised |
| iOS ARM64 Player | n/a | n/a | n/a | Not advertised |
| WebGL Player | n/a | n/a | n/a | Not advertised |
| Linux Player | n/a | n/a | n/a | Not advertised; Linux is an Editor validation host only |

The physical Android validation used a real ARM64 device and confirmed successful native loading plus expected Taffy geometry. The release package contains only the Android ARM64 native plugin and its importer is disabled for Editor and all non-Android platforms.

## Unity 2021 Linux validation note

Unity 2021.3.39f1 required a temporary local `bee_backend --stdin-canary` compatibility workaround on the newer Linux validation host. The Unity installation was restored byte-for-byte afterward. This was an Editor/toolchain-host issue, not a TaffyUGUI source workaround.

## Android binary

The package carries `Plugins/Android/arm64-v8a/libtaffy_ugui.so`, built against final ABI v1 (`version=1`, `stage=2`) and Taffy 0.13.0. Unity 6 Android validation used IL2CPP. Your project is still responsible for normal Unity Android SDK/NDK/JDK configuration and an ARM64-enabled Player build.
