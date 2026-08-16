

def verify_phase4() -> None:
    evidence = require_phase3_evidence()
    revision = str(evidence["source_revision"])
    source_tree = str(evidence["source_tree"])
    manifests: dict[str, dict[str, object]] = {}
    for name in PHASE4_REQUIRED_TARGETS:
        manifest = verify_macos_universal() if name == "macos-universal" else verify_staged(name)
        if manifest.get("source_tree") != source_tree:
            raise SystemExit(
                f"Phase 4 artifact {name} was built from source tree {manifest.get('source_tree')}, expected {source_tree}. "
                "All advertised artifacts must come from byte-identical Phase 3-verified source content."
            )
        manifests[name] = manifest

    fingerprints = {str(manifest.get("public_exports_sha256")) for manifest in manifests.values()}
    if fingerprints != {export_fingerprint(header_export_contract())}:
        raise SystemExit("Phase 4 target export fingerprints are not identical.")

    index = {
        "schema": 1,
        "phase": 4,
        "status": "complete",
        "package_version": package_version(),
        "source_revision": revision,
        "source_tree": source_tree,
        "source_revisions": sorted({str(manifest.get("source_revision")) for manifest in manifests.values()}),
        "abi": {"designation": "ABI-v1-RC", "version": ABI_RC_VERSION, "stage": ABI_RC_STAGE},
        "taffy_version": TAFFY_VERSION,
        "public_exports": list(header_export_contract()),
        "public_exports_sha256": export_fingerprint(header_export_contract()),
        "targets": {
            name: {
                "manifest": str((
                    (DIST_NATIVE / "macos" / "universal" / "manifest.json")
                    if name == "macos-universal"
                    else (TARGETS[name].stage_dir / "manifest.json")
                ).relative_to(ROOT)),
                "artifact_sha256": str(manifest["sha256"]),
            }
            for name, manifest in manifests.items()
        },
    }
    DIST_NATIVE.mkdir(parents=True, exist_ok=True)
    index_path = DIST_NATIVE / "phase4-index.json"
    index_path.write_text(json.dumps(index, indent=2, sort_keys=True) + "\n", encoding="utf-8")
    print(f"\nPHASE 4 LOCAL GATE: PASS — index written to {index_path.relative_to(ROOT)}")


def phase4_driver_selftest() -> None:
    expected = set(PHASE4_REQUIRED_TARGETS)
    assigned = set(PHASE4_HOST_TARGETS["windows"]) | set(PHASE4_HOST_TARGETS["darwin"]) | set(PHASE4_HOST_TARGETS["linux"])
    # macos-universal deterministically builds and stages both thin slices first.
    if "macos-universal" in assigned:
        assigned.update(("macos-arm64", "macos-x64"))
    if assigned != expected:
        raise SystemExit(f"Phase 4 host assignment does not cover the required target set: {sorted(assigned)} != {sorted(expected)}")

    validate_manifest_architecture_evidence(
        TARGETS["ios-arm64"], {"method": "lipo -info", "detail": "Non-fat file: libtaffy_ugui.a is architecture: arm64"}
    )
    validate_manifest_architecture_evidence(
        TARGETS["webgl"],
        {
            "method": "emar + file",
            "member_count": 1,
            "sample_member": "sample.o",
            "sample_description": "WebAssembly (wasm) binary module version 0x1",
        },
    )
    for spec, bad in (
        (TARGETS["ios-arm64"], {"method": "lipo -info", "detail": "x86_64"}),
        (TARGETS["webgl"], {"method": "emar + file", "member_count": 0, "sample_description": "current ar archive"}),
    ):
        try:
            validate_manifest_architecture_evidence(spec, bad)
        except SystemExit:
            pass
        else:
            raise SystemExit(f"Phase 4 architecture validator accepted invalid evidence for {spec.name}")
    print("Phase 4 build-driver contract self-test: PASS")


def doctor() -> None:
    print("TaffyUGUI local environment")
    print(f"  host: {platform.platform()}")
    print(f"  project: {ROOT}")
    print(f"  ABI: {parse_u32_const('TU_ABI_VERSION')}/{parse_u32_const('TU_ABI_STAGE')}")
    print(f"  dev Rust pin: {DEV_RUST_VERSION}; MSRV: {MSRV}")
    for name in ("git", "python3", "cargo", "rustc", "rustup", "rustfmt", "clippy-driver", "cbindgen", "clang", "clang++", "cmake"):
        print(f"  {name:14} {executable(name) or 'MISSING'}")
    print("  CI fallback: disabled by design")


def static_gate() -> None:
    require_abi_rc()
    static_preflight()
    phase4_driver_selftest()
    compile_header()
    print("\nLOCAL STATIC GATE: PASS")


def list_targets() -> None:
    for spec in TARGETS.values():
        host = ",".join(spec.host_os) if spec.host_os else "cross-host"
        print(f"{spec.name:16} {spec.triple:30} {spec.platform_name}/{spec.architecture:10} host={host}")


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    sub = parser.add_subparsers(dest="command", required=True)
    sub.add_parser("doctor", help="Show local prerequisites and ABI state")
    sub.add_parser("static-gate", help="Run all verification that does not require Rust compilation")
    sub.add_parser("prepare", help="Locally apply rustfmt and regenerate the cbindgen public header")
    sub.add_parser("quality", help="Run local static preflight, rustfmt, Clippy, tests, and host release build")
    sub.add_parser("verify-msrv", help="Run local Cargo check/test using Rust 1.82.0")
    sub.add_parser("header", help="Regenerate the public ABI header locally with pinned cbindgen")
    sub.add_parser("verify-header", help="Fail if checked-in public header differs from local cbindgen output")
    sub.add_parser("host-smoke", help="Build and execute local linked C/C++ smoke programs")
    sub.add_parser("verify-abi-rc", help="Run the complete local Phase 3 ABI-v1-RC gate")
    sub.add_parser("list-targets", help="List Phase 4 native target definitions")
    sub.add_parser("phase4-status", help="Show local Phase 4 artifact/evidence status")
    sub.add_parser("phase4-host", help="Build all canonical Phase 4 targets assigned to this local OS")
    sub.add_parser("verify-phase4", help="Verify the complete multi-host Phase 4 artifact set and write its local index")
    native = sub.add_parser("native", help="Build and stage a Phase 4 native target locally")
    native.add_argument("target", choices=[*TARGETS.keys(), "macos-universal"])
    verify = sub.add_parser("verify-native", help="Deep-verify staged local artifacts on their build host")
    verify.add_argument("targets", nargs="+", choices=[*TARGETS.keys(), "macos-universal"])
    args = parser.parse_args()

    if args.command == "doctor": doctor()
    elif args.command == "static-gate": static_gate()
    elif args.command == "prepare": prepare()
    elif args.command == "quality": quality()
    elif args.command == "verify-msrv": verify_msrv()
    elif args.command == "header": header()
    elif args.command == "verify-header": verify_header()
    elif args.command == "host-smoke": host_smoke()
    elif args.command == "verify-abi-rc": verify_abi_rc()
    elif args.command == "list-targets": list_targets()
    elif args.command == "phase4-status": phase4_status()
    elif args.command == "phase4-host": phase4_host()
    elif args.command == "verify-phase4": verify_phase4()
    elif args.command == "native":
        if args.target == "macos-universal": macos_universal()
        else: build_target(args.target)
    elif args.command == "verify-native":
        for target in args.targets:
            if target == "macos-universal":
                verify_macos_universal(recheck_symbols=True)
            else:
                verify_staged(target, recheck_symbols=True)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
