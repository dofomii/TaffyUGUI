# Phase 12 — Real Unity Platform Validation

**Status:** COMPLETE
**Completed:** 2026-08-18
**Release scope validated:** Android ARM64 only

## Goal

Validate TaffyUGUI in real Unity Editor versions and on the one Player platform advertised by this branch. A platform is not considered supported because a native artifact merely compiles; support requires the applicable Unity package, regression, Player, and runtime gates.

## Unity version matrix

| Unity version | Editor/package compile | Edit Mode | Play Mode | Result |
|---|---:|---:|---:|---|
| `2021.3.39f1` | PASS | 38/38 | 9/9 | **PASS** |
| `2022.3.62f1` | PASS | 38/38 | 9/9 | **PASS** |
| `6000.4.3f1` | PASS | 38/38 | 9/9 | **PASS** |

Unity `2021.3.48f1` was also installed during validation, but that XLTS patch requires an extended-LTS Industry/Enterprise entitlement on this machine. The primary 2021.3 gate therefore uses `2021.3.39f1`, which runs under the available Unity Personal entitlement and satisfies the package's declared `2021.3` minimum.

The current Linux host is newer than Unity 2021.3's original supported Linux baseline. On this host, Unity 2021.3's `bee_backend --stdin-canary` launch can stall during asset-database script compilation. P12.1 was executed with a temporary local Editor-installation wrapper that removed only `--stdin-canary`, matching the known Unity Linux workaround. The original `bee_backend` binary was preserved and restored after validation; its restored SHA-256 is `8561ed19e6d35e1e947b450dd528867e7c43c9fe43b5cce9086b58d3cad4fa67`. No workaround code is part of TaffyUGUI.

## Cross-version regression adjustment

Unity 2021.3 exposes the legacy uGUI built-in font as `Arial.ttf`, while newer Editors use `LegacyRuntime.ttf`. The permanent Phase 8 Edit Mode and Play Mode legacy-`Text` measurement tests now select the built-in font name at compile time by Unity version and continue to assert that a valid font is present. This is a test compatibility fix; no runtime layout behavior or ABI changed.

## Android ARM64 Player validation

A fresh Unity `6000.4.3f1` Android ARM64 IL2CPP Development Player was built from the current package and the accepted staged native payload.

- Android ARM64 IL2CPP Player build: **PASS**.
- APK contains `lib/arm64-v8a/libil2cpp.so`: **PASS**.
- APK contains `lib/arm64-v8a/libtaffy_ugui.so`: **PASS**.
- Packaged Taffy ELF program-header table matches the staged payload: **PASS**.
- Both runtime-loaded ELF `PT_LOAD` segments are byte-identical to the staged payload: **PASS**.
- APK install on physical `CPH2723`: **PASS**.
- Android dynamic loader reports `libtaffy_ugui.so` loaded successfully: **PASS**.
- Runtime regression marker: `TAFFY_PHASE12_DEVICE_PASS width=120.00 height=48.00`.
- No `FATAL EXCEPTION`, `UnsatisfiedLinkError`, or `DllNotFoundException` was observed in the device log for the smoke run.

The device regression scene was intentionally disposable and lived only under ignored `.build/` validation material. It exercised a real `TaffyLayoutGroup`/`TaffyLayoutItem` layout and verified the expected 120 x 48 geometry after Player startup.

## Native and payload regression

Final closeout gates on the Phase 12 working tree:

- `python3 build/build.py verify-abi-final` — **PASS**;
- rustfmt — **PASS**;
- Clippy with warnings denied — **PASS**;
- maintained Rust tests — **44/44 PASS**;
- host release build and cbindgen drift check — **PASS**;
- `python3 build/build.py native android-arm64` — **PASS**;
- `python3 build/build.py verify-native android-arm64` — **PASS**;
- `python3 build/build.py verify-phase4` — **PASS**;
- `python3 build/build.py stage-phase5` — **PASS**;
- `python3 build/build.py verify-phase5` — **PASS**.

Android ARM64 native library SHA-256 remains:

`7bdca92aae2939e5098292294ee7f7d730d5eee1c718d87f65a3f22349338f66`

The native input snapshot remains:

`sha256:3228f12128c07fd6c470a7bc9119a4ba810f7718d98c6ae9537086030beaa0fc`

## Player support decision

The active v1 branch remains intentionally **Android ARM64 only**. Phase 12 closes the other Player matrix entries by explicitly excluding them from advertised support rather than claiming unexecuted validation:

| Player target | Phase 12 disposition |
|---|---|
| Android ARM64 | **Validated and advertised** |
| Windows x64 | **Not advertised; deferred outside this branch** |
| macOS Intel | **Not advertised; deferred outside this branch** |
| macOS Apple Silicon | **Not advertised; deferred outside this branch** |
| iOS ARM64 | **Not advertised; deferred outside this branch** |
| WebGL | **Not advertised; deferred/legacy work remains separate** |
| Linux Player | **Not retained as an advertised Player target** |

The Unity 2021.3/2022.3/6 checks above are Editor/package compatibility gates and do not imply Player support for their host desktop platforms.

## Exit criteria

Phase 12 is complete because the declared Unity minimum and selected compatibility Editors compile and pass the permanent regression suites, the only advertised Player target passes a fresh IL2CPP build plus physical-device execution, native/payload gates remain green, unsupported Player targets are explicitly not advertised, and all disposable validation/probe material remains outside tracked project source.

**Next authoritative phase:** Phase 13 — Performance and Reliability Hardening, beginning with P13.1 (100-node benchmark).
