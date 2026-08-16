

def verify_staged(name: str, *, recheck_symbols: bool = False) -> dict[str, object]:
    spec = TARGETS[name]
    artifact = spec.stage_dir / spec.artifact
    manifest_path = spec.stage_dir / "manifest.json"
    if not artifact.exists() or not manifest_path.exists():
        raise SystemExit(f"Staged target is incomplete: {name}")
    manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
    actual_hash = sha256(artifact)
    if manifest.get("sha256") != actual_hash:
        raise SystemExit(f"Checksum mismatch: {name}")
    verify_checksum_file(spec.stage_dir, artifact, actual_hash)
    if manifest.get("schema") != 2:
        raise SystemExit(f"Unsupported Phase 4 manifest schema for {name}: {manifest.get('schema')}")
    if manifest.get("artifact") != artifact.name or manifest.get("artifact_size") != artifact.stat().st_size:
        raise SystemExit(f"Artifact identity/size mismatch in manifest: {name}")
    if manifest.get("package_version") != package_version():
        raise SystemExit(f"Package version mismatch in manifest: {name}")
    if manifest.get("taffy_version") != TAFFY_VERSION:
        raise SystemExit(f"Taffy version mismatch in manifest: {name}")
    if manifest.get("rust_target") != spec.triple:
        raise SystemExit(f"Rust target mismatch in manifest: {name}")
    if (manifest.get("platform"), manifest.get("architecture"), manifest.get("crate_type")) != (
        spec.platform_name,
        spec.architecture,
        spec.crate_type,
    ):
        raise SystemExit(f"Platform/architecture/crate type mismatch in manifest: {name}")
    if manifest.get("built_locally") is not True:
        raise SystemExit(f"Artifact is not marked as a local build: {name}")
    if not manifest.get("source_revision") or not manifest.get("source_tree"):
        raise SystemExit(f"Source revision/tree evidence missing from manifest: {name}")
    abi = manifest.get("abi", {})
    if not isinstance(abi, dict) or (abi.get("version"), abi.get("stage")) != (ABI_RC_VERSION, ABI_RC_STAGE):
        raise SystemExit(f"Staged target is not ABI-v1-RC: {name}")
    exports = header_export_contract()
    if tuple(manifest.get("public_exports", [])) != exports:
        raise SystemExit(f"Public export inventory mismatch in manifest: {name}")
    if manifest.get("public_exports_sha256") != export_fingerprint(exports):
        raise SystemExit(f"Public export fingerprint mismatch in manifest: {name}")
    if not isinstance(manifest.get("toolchain"), dict) or not manifest["toolchain"].get("rustc"):
        raise SystemExit(f"Toolchain evidence missing from manifest: {name}")
    validate_manifest_architecture_evidence(spec, manifest.get("architecture_evidence"))
    inspect_artifact(spec, artifact, base_env())
    if recheck_symbols:
        verify_symbols(spec, artifact, target_env(spec))
    print(f"Verified staged local artifact: {name}")
    return manifest


def verify_macos_universal(*, recheck_symbols: bool = False) -> dict[str, object]:
    directory = DIST_NATIVE / "macos" / "universal"
    artifact = directory / "libtaffy_ugui.dylib"
    manifest_path = directory / "manifest.json"
    if not artifact.exists() or not manifest_path.exists():
        raise SystemExit("Staged target is incomplete: macos-universal")
    manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
    actual_hash = sha256(artifact)
    if manifest.get("sha256") != actual_hash:
        raise SystemExit("Checksum mismatch: macos-universal")
    verify_checksum_file(directory, artifact, actual_hash)
    if manifest.get("schema") != 2:
        raise SystemExit("Unsupported Phase 4 manifest schema: macos-universal")
    if manifest.get("artifact_size") != artifact.stat().st_size:
        raise SystemExit("Artifact size mismatch: macos-universal")
    if manifest.get("package_version") != package_version() or manifest.get("taffy_version") != TAFFY_VERSION:
        raise SystemExit("Version metadata mismatch: macos-universal")
    if (manifest.get("platform"), manifest.get("architecture"), manifest.get("crate_type")) != ("macos", "universal", "cdylib"):
        raise SystemExit("Platform metadata mismatch: macos-universal")
    if manifest.get("rust_targets") != [TARGETS["macos-arm64"].triple, TARGETS["macos-x64"].triple]:
        raise SystemExit("Rust target metadata mismatch: macos-universal")
    if manifest.get("built_locally") is not True:
        raise SystemExit("macos-universal is not marked as a local build")
    if not manifest.get("source_revision") or not manifest.get("source_tree"):
        raise SystemExit("Source revision/tree evidence missing: macos-universal")
    abi = manifest.get("abi", {})
    if not isinstance(abi, dict) or (abi.get("version"), abi.get("stage")) != (ABI_RC_VERSION, ABI_RC_STAGE):
        raise SystemExit("macos-universal is not ABI-v1-RC")
    exports = header_export_contract()
    if tuple(manifest.get("public_exports", [])) != exports or manifest.get("public_exports_sha256") != export_fingerprint(exports):
        raise SystemExit("Public export contract mismatch: macos-universal")
    if "arm64" not in str(manifest.get("lipo_info", "")) or "x86_64" not in str(manifest.get("lipo_info", "")):
        raise SystemExit("Universal architecture evidence is incomplete")
    architecture_evidence = manifest.get("architecture_evidence")
    if not isinstance(architecture_evidence, dict) or architecture_evidence.get("method") != "lipo -info":
        raise SystemExit("Universal architecture evidence manifest is incomplete")
    if recheck_symbols:
        if current_os() != "darwin":
            raise SystemExit("Deep macos-universal recheck requires a macOS host.")
        lipo = require("lipo")
        info = run(lipo, "-info", str(artifact), capture=True).strip()
        if "arm64" not in info or "x86_64" not in info:
            raise SystemExit(f"Universal dylib missing architecture: {info}")
        verify_symbols(TARGETS["macos-arm64"], artifact, base_env())
    print("Verified staged local artifact: macos-universal")
    return manifest


def phase4_host() -> None:
    require_phase3_evidence()
    host = current_os()
    targets = PHASE4_HOST_TARGETS.get(host)
    if not targets:
        raise SystemExit(f"No canonical Phase 4 target assignment for host: {host}")
    print(f"Canonical Phase 4 targets for {host}: {', '.join(targets)}")
    for name in targets:
        if name == "macos-universal":
            macos_universal()
        else:
            build_target(name)
    print(f"\nPHASE 4 HOST BUILD: PASS — {host} canonical artifacts are staged locally.")


def phase4_status() -> None:
    revision, dirty, _ = git_state()
    print(f"Phase 4 local status for source {revision}{'+dirty' if dirty else ''}")
    print(f"  Phase 3 evidence: {'present' if PHASE3_EVIDENCE.exists() else 'missing'}")
    for name in PHASE4_REQUIRED_TARGETS:
        if name == "macos-universal":
            directory = DIST_NATIVE / "macos" / "universal"
            artifact = directory / "libtaffy_ugui.dylib"
        else:
            spec = TARGETS[name]
            directory = spec.stage_dir
            artifact = directory / spec.artifact
        state = "staged" if artifact.exists() and (directory / "manifest.json").exists() and (directory / "SHA256SUMS").exists() else "missing"
        print(f"  {name:16} {state}")
    print(f"  Final index: {'present' if (DIST_NATIVE / 'phase4-index.json').exists() else 'missing'}")
