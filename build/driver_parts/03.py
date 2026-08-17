

def target_toolchain_evidence(spec: TargetSpec, env: dict[str, str]) -> dict[str, object]:
    evidence: dict[str, object] = {
        "host": platform.platform(),
        "rustc": tool_version("rustc", "--version"),
        "cargo": tool_version("cargo", "--version"),
        "rust_target": spec.triple,
    }
    if spec.name == "android-arm64":
        ndk = find_android_ndk()
        bins = sorted((ndk / "toolchains/llvm/prebuilt").glob("*/bin"))
        suffix = ".cmd" if os.name == "nt" else ""
        linker = bins[0] / f"aarch64-linux-android{ANDROID_API}-clang{suffix}"
        evidence["android_ndk_revision"] = ANDROID_NDK_REVISION
        evidence["android_api"] = ANDROID_API
        evidence["android_clang"] = run(str(linker), "--version", capture=True, env=env).splitlines()[0]
    elif spec.name == "webgl":
        emcc = require("emcc")
        evidence["emscripten"] = run(emcc, "--version", capture=True, env=env).splitlines()[0]
        evidence["emscripten_required"] = WEBGL_EMSCRIPTEN_VERSION
    elif spec.platform_name == "macos" or spec.name == "ios-arm64":
        xcodebuild = require("xcodebuild", "macOS/iOS Phase 4 builds require Xcode.")
        evidence["xcode"] = run(xcodebuild, "-version", capture=True, env=env).replace("\n", "; ").strip()
        if spec.name == "ios-arm64":
            xcrun = require("xcrun", "iOS Phase 4 builds require Xcode command-line tools.")
            evidence["iphoneos_sdk"] = run(xcrun, "--sdk", "iphoneos", "--show-sdk-version", capture=True, env=env).strip()
    return evidence


def validate_manifest_architecture_evidence(spec: TargetSpec, evidence: object) -> None:
    if not isinstance(evidence, dict) or not evidence.get("method"):
        raise SystemExit(f"Architecture evidence missing from manifest: {spec.name}")
    method = str(evidence.get("method"))
    if spec.name == "ios-arm64":
        detail = str(evidence.get("detail", "")).lower()
        if method != "lipo -info" or "arm64" not in detail or "x86_64" in detail:
            raise SystemExit(f"iOS ARM64 architecture evidence is invalid: {evidence}")
    elif spec.name == "webgl":
        description = str(evidence.get("sample_description", "")).lower()
        member_count = evidence.get("member_count")
        if (
            method != "emar + file"
            or not isinstance(member_count, int)
            or member_count <= 0
            or not any(token in description for token in ("webassembly", "wasm", "llvm ir bitcode", "llvm bitcode"))
        ):
            raise SystemExit(f"WebGL architecture evidence is invalid: {evidence}")


def stage_manifest(spec: TargetSpec, artifact: Path, description: str, exports: tuple[str, ...], env: dict[str, str]) -> None:
    manifest = {
        "schema": 2,
        "package_version": package_version(),
        "abi": {"designation": "ABI-v1", "version": ABI_RC_VERSION, "stage": ABI_RC_STAGE},
        "taffy_version": TAFFY_VERSION,
        "rust_target": spec.triple,
        "source_revision": source_revision(),
        "source_tree": source_tree_sha(),
        "artifact": artifact.name,
        "artifact_size": artifact.stat().st_size,
        "platform": spec.platform_name,
        "architecture": spec.architecture,
        "crate_type": spec.crate_type,
        "file_description": description,
        "architecture_evidence": target_architecture_evidence(spec, artifact, env, description),
        "sha256": sha256(artifact),
        "built_locally": True,
        "public_exports": list(exports),
        "public_exports_sha256": export_fingerprint(exports),
        "toolchain": target_toolchain_evidence(spec, env),
    }
    spec.stage_dir.mkdir(parents=True, exist_ok=True)
    (spec.stage_dir / "manifest.json").write_text(json.dumps(manifest, indent=2, sort_keys=True) + "\n", encoding="utf-8")
    (spec.stage_dir / "SHA256SUMS").write_text(f"{manifest['sha256']}  {artifact.name}\n", encoding="utf-8")


def build_target(name: str) -> Path:
    require_abi_rc()
    require_phase3_evidence()
    spec = TARGETS[name]
    host = current_os()
    if spec.host_os and host not in spec.host_os:
        raise SystemExit(f"{name} must be built on {', '.join(spec.host_os)}; current host is {host}.")
    ensure_target_installed(spec.triple)
    env = target_env(spec)
    cargo("build", "--locked", "--release", "--target", spec.triple, env=env)
    built = cargo_artifact(spec)
    description = inspect_artifact(spec, built, env)
    exports = verify_symbols(spec, built, env)
    spec.stage_dir.mkdir(parents=True, exist_ok=True)
    staged = spec.stage_dir / spec.artifact
    shutil.copy2(built, staged)
    stage_manifest(spec, staged, description, exports, env)
    verify_staged(name)
    print(f"Staged local {name}: {staged.relative_to(ROOT)}")
    return staged


def macos_universal() -> Path:
    require_phase3_evidence()
    if current_os() != "darwin":
        raise SystemExit("macOS universal library must be assembled on macOS.")
    arm = build_target("macos-arm64")
    intel = build_target("macos-x64")
    lipo = require("lipo")
    output_dir = DIST_NATIVE / "macos" / "universal"
    output_dir.mkdir(parents=True, exist_ok=True)
    output = output_dir / "libtaffy_ugui.dylib"
    run(lipo, "-create", str(arm), str(intel), "-output", str(output))
    info = run(lipo, "-info", str(output), capture=True).strip()
    if "arm64" not in info or "x86_64" not in info:
        raise SystemExit(f"Universal dylib missing architecture: {info}")
    env = base_env()
    exports = verify_symbols(TARGETS["macos-arm64"], output, env)
    file_bin = require("file")
    description = run(file_bin, "-b", str(output), capture=True, env=env).strip()
    checksum = sha256(output)
    manifest = {
        "schema": 2,
        "package_version": package_version(),
        "abi": {"designation": "ABI-v1", "version": ABI_RC_VERSION, "stage": ABI_RC_STAGE},
        "taffy_version": TAFFY_VERSION,
        "rust_targets": [TARGETS["macos-arm64"].triple, TARGETS["macos-x64"].triple],
        "source_revision": source_revision(),
        "source_tree": source_tree_sha(),
        "artifact": output.name,
        "artifact_size": output.stat().st_size,
        "platform": "macos",
        "architecture": "universal",
        "crate_type": "cdylib",
        "file_description": description,
        "architecture_evidence": {"method": "lipo -info", "detail": info},
        "lipo_info": info,
        "sha256": checksum,
        "built_locally": True,
        "public_exports": list(exports),
        "public_exports_sha256": export_fingerprint(exports),
        "toolchain": target_toolchain_evidence(TARGETS["macos-arm64"], env),
    }
    (output_dir / "SHA256SUMS").write_text(f"{checksum}  {output.name}\n", encoding="utf-8")
    (output_dir / "manifest.json").write_text(json.dumps(manifest, indent=2, sort_keys=True) + "\n", encoding="utf-8")
    verify_macos_universal()
    print(f"Staged local macos-universal: {output.relative_to(ROOT)}")
    return output


def verify_checksum_file(directory: Path, artifact: Path, expected_hash: str) -> None:
    checksum_path = directory / "SHA256SUMS"
    if not checksum_path.exists():
        raise SystemExit(f"Missing SHA256SUMS beside {artifact}")
    expected_line = f"{expected_hash}  {artifact.name}"
    lines = [line.strip() for line in checksum_path.read_text(encoding="utf-8").splitlines() if line.strip()]
    if lines != [expected_line]:
        raise SystemExit(f"SHA256SUMS mismatch for {artifact}: expected exactly '{expected_line}', got {lines}")
