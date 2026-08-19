

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
        "abi": {"designation": "ABI-v1", "version": ABI_RC_VERSION, "stage": ABI_RC_STAGE},
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


PHASE5_ANDROID_DIR = ROOT / "UnityPackage" / "Plugins" / "Android" / "arm64-v8a"
PHASE5_ANDROID_PLUGIN = PHASE5_ANDROID_DIR / "libtaffy_ugui.so"
PHASE5_ANDROID_META = PHASE5_ANDROID_DIR / "libtaffy_ugui.so.meta"
PHASE5_ANDROID_PROVENANCE = PHASE5_ANDROID_DIR / "taffy_ugui.provenance.json"
PHASE5_DESKTOP_PLUGINS = {
    "linux-x86": ROOT / "UnityPackage" / "Plugins" / "Linux" / "x86" / "libtaffy_ugui.so",
    "linux-x64": ROOT / "UnityPackage" / "Plugins" / "Linux" / "x86_64" / "libtaffy_ugui.so",
    "windows-x86": ROOT / "UnityPackage" / "Plugins" / "Windows" / "x86" / "taffy_ugui.dll",
    "windows-x64": ROOT / "UnityPackage" / "Plugins" / "Windows" / "x86_64" / "taffy_ugui.dll",
}
PHASE5_WINDOWS_X86_META = Path(str(PHASE5_DESKTOP_PLUGINS["windows-x86"]) + ".meta")
PHASE5_WINDOWS_X64_META = Path(str(PHASE5_DESKTOP_PLUGINS["windows-x64"]) + ".meta")
PHASE5_ANDROID_META_TEXT = """fileFormatVersion: 2
guid: 4e8aa0f9ef154b56a50b8c302f27fe56
PluginImporter:
  externalObjects: {}
  serializedVersion: 2
  iconMap: {}
  executionOrder: {}
  defineConstraints: []
  isPreloaded: 0
  isOverridable: 0
  isExplicitlyReferenced: 0
  validateReferences: 1
  platformData:
  - first:
      Any:
    second:
      enabled: 0
      settings: {}
  - first:
      Android: Android
    second:
      enabled: 1
      settings:
        CPU: ARM64
  - first:
      Editor: Editor
    second:
      enabled: 0
      settings:
        CPU: AnyCPU
        DefaultValueInitialized: true
  userData:
  assetBundleName:
  assetBundleVariant:
"""


def require_phase4_index() -> dict[str, object]:
    index_path = DIST_NATIVE / "phase4-index.json"
    if not index_path.exists():
        raise SystemExit("Phase 4 completion index is missing. Run 'python3 build/build.py verify-phase4' first.")
    index = json.loads(index_path.read_text(encoding="utf-8"))
    if index.get("status") != "complete" or set(index.get("targets", {}).keys()) != {"android-arm64"}:
        raise SystemExit("Phase 5 requires the completed Android-only Phase 4 index.")
    return index


def stage_phase5() -> None:
    index = require_phase4_index()
    manifest = verify_staged("android-arm64", recheck_symbols=True)
    source = TARGETS["android-arm64"].stage_dir / TARGETS["android-arm64"].artifact
    expected_sha = str(manifest["sha256"])
    target_entry = index["targets"]["android-arm64"]
    if target_entry.get("artifact_sha256") != expected_sha:
        raise SystemExit("Phase 4 index and Android artifact manifest checksum disagree.")

    PHASE5_ANDROID_DIR.mkdir(parents=True, exist_ok=True)
    shutil.copy2(source, PHASE5_ANDROID_PLUGIN)
    PHASE5_ANDROID_META.write_text(PHASE5_ANDROID_META_TEXT, encoding="utf-8")
    provenance = {
        "schema": 1,
        "phase": 5,
        "platform": "android",
        "architecture": "arm64-v8a",
        "artifact": PHASE5_ANDROID_PLUGIN.name,
        "sha256": expected_sha,
        "source_manifest": str((TARGETS["android-arm64"].stage_dir / "manifest.json").relative_to(ROOT)),
        "phase4_index": str((DIST_NATIVE / "phase4-index.json").relative_to(ROOT)),
        "source_revision": str(manifest["source_revision"]),
        "source_tree": str(manifest["source_tree"]),
        "abi": manifest["abi"],
        "taffy_version": manifest["taffy_version"],
    }
    PHASE5_ANDROID_PROVENANCE.write_text(json.dumps(provenance, indent=2, sort_keys=True) + "\n", encoding="utf-8")
    verify_phase5()
    print("\nPHASE 5 ANDROID PAYLOAD: STAGED — UnityPackage/Plugins/Android/arm64-v8a")


def verify_phase5() -> None:
    index = require_phase4_index()
    manifest = verify_staged("android-arm64")
    required = (PHASE5_ANDROID_PLUGIN, PHASE5_ANDROID_META, PHASE5_ANDROID_PROVENANCE)
    missing = [str(path.relative_to(ROOT)) for path in required if not path.exists()]
    if missing:
        raise SystemExit(f"Phase 5 Android payload is incomplete: {', '.join(missing)}")
    expected_sha = str(manifest["sha256"])
    if sha256(PHASE5_ANDROID_PLUGIN) != expected_sha:
        raise SystemExit("Packaged Android plug-in checksum does not match the verified Phase 4 artifact.")
    if PHASE5_ANDROID_META.read_text(encoding="utf-8") != PHASE5_ANDROID_META_TEXT:
        raise SystemExit("Android PluginImporter metadata drifted from the canonical ARM64/Android-only configuration.")
    provenance = json.loads(PHASE5_ANDROID_PROVENANCE.read_text(encoding="utf-8"))
    for key, expected in (
        ("sha256", expected_sha),
        ("source_revision", manifest["source_revision"]),
        ("source_tree", manifest["source_tree"]),
        ("platform", "android"),
        ("architecture", "arm64-v8a"),
    ):
        if provenance.get(key) != expected:
            raise SystemExit(f"Phase 5 provenance mismatch for {key}: {provenance.get(key)!r} != {expected!r}")
    if index["targets"]["android-arm64"].get("artifact_sha256") != expected_sha:
        raise SystemExit("Packaged payload does not match the Phase 4 index checksum.")

    native_suffixes = {".dll", ".dylib", ".so", ".a"}
    packaged_native = {path for path in (ROOT / "UnityPackage" / "Plugins").rglob("*") if path.is_file() and path.suffix in native_suffixes}
    expected_native = {PHASE5_ANDROID_PLUGIN, *PHASE5_DESKTOP_PLUGINS.values()}
    if packaged_native != expected_native:
        missing_native = sorted(str(path.relative_to(ROOT)) for path in expected_native - packaged_native)
        unexpected_native = sorted(str(path.relative_to(ROOT)) for path in packaged_native - expected_native)
        raise SystemExit(
            "Unity native payload mismatch. "
            f"Missing={missing_native or 'none'} Unexpected={unexpected_native or 'none'}"
        )

    file_bin = require("file", "Full package verification requires the local file utility.")
    expected_descriptions = {
        "linux-x86": ("elf 32-bit", "80386"),
        "linux-x64": ("elf 64-bit", "x86-64"),
        "windows-x86": ("pe32 executable", "80386"),
        "windows-x64": ("pe32+ executable", "x86-64"),
    }
    windows_x86_meta = PHASE5_WINDOWS_X86_META.read_text(encoding="utf-8")
    required_x86_importer_fragments = (
        "PluginImporter:",
        "Editor:\n      enabled: 0",
        "Win:\n      enabled: 1\n      settings:\n        CPU: x86",
        "Win64:\n      enabled: 0",
    )
    missing_x86_importer = [fragment for fragment in required_x86_importer_fragments if fragment not in windows_x86_meta]
    if missing_x86_importer:
        raise SystemExit(
            "Windows x86 PluginImporter must be Win32-only and disabled for Editor/Win64. "
            f"Missing importer rules: {missing_x86_importer}"
        )

    windows_x64_meta = PHASE5_WINDOWS_X64_META.read_text(encoding="utf-8")
    required_x64_importer_fragments = (
        "PluginImporter:",
        "Editor:\n      enabled: 1\n      settings:\n        CPU: x86_64",
        "OS: Windows",
        "Win64:\n      enabled: 1\n      settings:\n        CPU: x86_64",
    )
    missing_x64_importer = [fragment for fragment in required_x64_importer_fragments if fragment not in windows_x64_meta]
    if missing_x64_importer:
        raise SystemExit(
            "Windows x64 PluginImporter must remain Windows/x86_64 Editor + Win64 compatible. "
            f"Missing importer rules: {missing_x64_importer}"
        )

    expected_exports = header_export_contract()
    for name, artifact in PHASE5_DESKTOP_PLUGINS.items():
        description = run(file_bin, "-b", str(artifact), capture=True, env=base_env()).strip().lower()
        if not all(token in description for token in expected_descriptions[name]):
            raise SystemExit(f"Packaged {name} binary architecture/format mismatch: {description}")
        if package_version().encode("ascii") not in artifact.read_bytes():
            raise SystemExit(f"Packaged {name} binary does not embed package version {package_version()}.")
        if name.startswith("windows"):
            objdump = require("objdump", "Windows package export verification requires objdump.")
            symbols = run(objdump, "-p", str(artifact), capture=True, env=base_env())
        else:
            nm = require("nm", "Linux package export verification requires nm.")
            symbols = run(nm, "-D", "--defined-only", str(artifact), capture=True, env=base_env())
        missing_exports = [symbol for symbol in expected_exports if symbol not in symbols]
        if missing_exports:
            raise SystemExit(f"Packaged {name} binary is missing ABI exports: {', '.join(missing_exports)}")

    print("PHASE 5 FULL NATIVE PAYLOAD VERIFY: PASS")

def doctor() -> None:
    print("TaffyUGUI local environment")
    print(f"  host: {platform.platform()}")
    print(f"  project: {ROOT}")
    print(f"  ABI: {parse_u32_const('TU_ABI_VERSION')}/{parse_u32_const('TU_ABI_STAGE')}")
    print(f"  dev Rust pin: {DEV_RUST_VERSION}; MSRV: {MSRV}")
    for name in ("git", "python3", "cargo", "rustc", "rustup", "rustfmt", "clippy-driver", "cbindgen", "clang", "clang++", "cmake"):
        print(f"  {name:14} {executable(name) or 'MISSING'}")
    print("  CI fallback: disabled by design")


def list_targets() -> None:
    for spec in TARGETS.values():
        host = ",".join(spec.host_os) if spec.host_os else "cross-host"
        print(f"{spec.name:16} {spec.triple:30} {spec.platform_name}/{spec.architecture:10} host={host}")


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    sub = parser.add_subparsers(dest="command", required=True)
    sub.add_parser("doctor", help="Show local prerequisites and ABI state")
    sub.add_parser("prepare", help="Locally apply rustfmt and regenerate the cbindgen public header")
    sub.add_parser("quality", help="Run rustfmt, Clippy, tests, and host release build")
    sub.add_parser("verify-msrv", help="Run local Cargo check/test using Rust 1.82.0")
    sub.add_parser("header", help="Regenerate the public ABI header locally with pinned cbindgen")
    sub.add_parser("verify-header", help="Fail if checked-in public header differs from local cbindgen output")
    sub.add_parser("stage-phase5", help="Stage Android ARM64 and verify the complete checked-in native package payload")
    sub.add_parser("verify-phase5", help="Verify the full Unity native payload and Android provenance")
    sub.add_parser("verify-abi-final", help="Run the complete local Phase 3 regression gate for final ABI v1")
    sub.add_parser("verify-abi-rc", help="Compatibility alias for the final ABI v1 verification gate")
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
    elif args.command == "prepare": prepare()
    elif args.command == "quality": quality()
    elif args.command == "verify-msrv": verify_msrv()
    elif args.command == "header": header()
    elif args.command == "stage-phase5": stage_phase5()
    elif args.command == "verify-phase5": verify_phase5()
    elif args.command == "verify-header": verify_header()
    elif args.command in ("verify-abi-final", "verify-abi-rc"): verify_abi_final()
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
