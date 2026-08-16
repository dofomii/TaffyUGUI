# Phase 4 — Local Cross-Platform Native Builds

**Infrastructure status:** COMPLETE
**Artifact status:** 0/7 required native target artifacts accepted
**Phase gate:** OPEN / BLOCKED by unavailable Phase 4 platform toolchains and required real artifacts

Phase 4 is a **local multi-host release build**, not a GitHub Actions workflow. GitHub may store source backups, but it is not a build or verification authority.

## Entry gate

Every machine that produces a Phase 4 artifact must use the byte-identical clean source tree and must first pass:

```bash
python3 build/build.py verify-abi-rc
```

That command records local evidence under `.build/evidence/phase3-local.json`. `build/build.py native ...` refuses to produce a Phase 4 release artifact if the evidence is missing, belongs to different local source content, or the working tree is dirty.

## Canonical host ownership

| Local host | Canonical Phase 4 outputs |
|---|---|
| Windows x64 | `windows-x64` |
| macOS | `macos-arm64`, `macos-x64`, `macos-universal`, `ios-arm64` |
| Linux x64 | `android-arm64`, `webgl` |

Run all outputs assigned to the current host with:

```bash
python3 build/build.py phase4-host
```

Or build one target explicitly on its canonical host:

```bash
python3 build/build.py native <target>
python3 build/build.py verify-native <target>
```

The native build command performs architecture/format and **full public ABI export** checks before staging the artifact. Architecture validation is target-specific; it is never skipped just because a generic `file` utility is unavailable on Windows.

## Toolchains

### Windows x64

- Rust 1.97.1
- `x86_64-pc-windows-msvc` target
- Visual Studio/MSVC linker environment
- `dumpbin`, `llvm-nm`, `llvm-objdump`, or `objdump` for export verification

Output:

```text
dist/native/windows/x86_64/taffy_ugui.dll
```

### macOS arm64/x64 and universal

- macOS host with Xcode command-line tools
- Rust 1.97.1
- `aarch64-apple-darwin`
- `x86_64-apple-darwin`
- `lipo`

Outputs:

```text
dist/native/macos/arm64/libtaffy_ugui.dylib
dist/native/macos/x86_64/libtaffy_ugui.dylib
dist/native/macos/universal/libtaffy_ugui.dylib
```

The universal dylib must contain both `arm64` and `x86_64` slices and expose the complete `tu_*` contract.

### Android ARM64

- Rust 1.97.1
- `aarch64-linux-android`
- Unity 2021.3-compatible Android NDK **r21d**, exact revision `21.3.6528147`
- API level **21**
- The local build driver maps r21d's AArch64 `libgcc_real.a` unwinder to Rust's expected `libunwind.a` name through an ignored, non-copying `.toolchain` symlink.
- manifest records the NDK revision/API and Android Clang version, but not the machine-local NDK path
- `ANDROID_NDK_HOME` or `ANDROID_NDK_ROOT` pointing to that exact NDK

Output:

```text
dist/native/android/arm64-v8a/libtaffy_ugui.so
```

### iOS ARM64

- macOS/Xcode host
- Rust 1.97.1
- `aarch64-apple-ios`
- installed iPhoneOS SDK
- `lipo -info` must prove the archive is device ARM64 and not an x86_64 simulator archive

Output:

```text
dist/native/ios/arm64/libtaffy_ugui.a
```

### WebGL

- Rust 1.97.1
- `wasm32-unknown-emscripten`
- Emscripten **2.0.19**, matching the current Unity 2021.3 baseline
- `emcc`, `emar`, and the bundled `llvm-nm` from that toolchain; an archive member must identify as WebAssembly/Wasm or LLVM bitcode

Output:

```text
dist/native/webgl/wasm32/libtaffy_ugui.a
```

## One-command host execution

After installing the platform SDK itself, use the checked-in host wrapper instead of manually replaying the build sequence:

```text
Windows:  powershell -ExecutionPolicy Bypass -File scripts/phase4-build-host.ps1
macOS:    ./scripts/phase4-build-host.sh
Linux:    ./scripts/phase4-build-host.sh
```

The wrapper installs/activates the pinned project-local Rust toolchain, adds the canonical Rust target(s), runs `prepare`, executes the complete local Phase 3 gate, builds every Phase 4 artifact assigned to that host, and prints `phase4-status`. On Linux it automatically discovers `.toolchain/android-ndk-r21d` and `.toolchain/emsdk` when present; macOS still requires Xcode and Windows still requires the MSVC developer environment.

After copying all three hosts' `dist/native/...` outputs into one clean aggregation checkout, run:

```bash
./scripts/phase4-finalize.sh
```

The finalizer independently reruns Phase 3 on the aggregation source tree before accepting the collected target set.

## Artifact evidence

Every normal target directory contains:

```text
<artifact>
manifest.json
SHA256SUMS
```

The manifest records:

- package version;
- ABI designation/version/stage;
- exact Taffy baseline;
- exact Rust target;
- clean source commit;
- artifact size and SHA-256;
- platform/architecture/crate type;
- target-specific architecture evidence (PE/Mach-O/ELF inspection, `lipo -info` for iOS, and Emscripten archive-member inspection for WebGL);
- complete public `tu_*` export list and export-contract fingerprint;
- local Rust/Cargo and platform-toolchain evidence without embedding machine-specific absolute SDK paths;
- `built_locally: true`.

`verify-native` validates the artifact and manifest again on the machine that built it.

## Collecting the multi-host output

All host builds must be made from the **same clean source tree**. Copy the generated `dist/native/...` target directories from the Windows, macOS, and Linux build machines into one local project checkout without modifying their contents.

Then run:

```bash
python3 build/build.py verify-phase4
```

The final gate verifies:

1. every required target exists;
2. every artifact checksum matches both `manifest.json` and `SHA256SUMS`;
3. every manifest is ABI-v1-RC `1/1` and Taffy `0.13.0`;
4. all artifacts carry the same Phase-3-verified Git tree SHA (commit SHAs are also recorded for traceability);
5. every target records the exact same complete public ABI export set;
6. platform/architecture/crate-type metadata and build-host architecture evidence are correct;
7. iOS carries ARM64 `lipo` evidence and WebGL carries Emscripten archive-member evidence;
8. all artifacts are explicitly recorded as local builds.

A successful gate writes:

```text
dist/native/phase4-index.json
```

That index is the Phase 4 completion evidence used by the next packaging phase.

## No advertising before verification

A platform is not supported merely because a target definition exists. It becomes an advertised native target only after its artifact is locally built, architecture/export checked, checksummed, and accepted by the final `verify-phase4` gate.
