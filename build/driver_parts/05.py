# pyright: reportUndefinedVariable=false
# Executed after driver parts 00-04 in one shared namespace.


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
WEB_PACKAGE_DIR = ROOT / "UnityPackage" / "Plugins" / "WebGL"
WEB_PACKAGE_ARCHIVE = WEB_PACKAGE_DIR / "libtaffy_ugui.a"
WEB_PACKAGE_ARCHIVE_META = WEB_PACKAGE_DIR / "libtaffy_ugui.a.meta"
WEB_PACKAGE_DIR_META = ROOT / "UnityPackage" / "Plugins" / "WebGL.meta"
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


WEB_PACKAGE_DIR_META_TEXT = """fileFormatVersion: 2
guid: 73df503c656a4d0e99fbcff1565d2ec7
folderAsset: yes
DefaultImporter:
  externalObjects: {}
  userData:
  assetBundleName:
  assetBundleVariant:
"""
WEB_PACKAGE_ARCHIVE_META_TEXT = """fileFormatVersion: 2
guid: 2c24ac56756d48b0b811909a1cf103ef
PluginImporter:
  externalObjects: {}
  serializedVersion: 2
  iconMap: {}
  executionOrder: {}
  defineConstraints: []
  isPreloaded: 0
  isOverridable: 1
  isExplicitlyReferenced: 0
  validateReferences: 1
  platformData:
  - first:
      :
    second:
      enabled: 0
      settings: {}
  - first:
      Editor: Editor
    second:
      enabled: 0
      settings:
        DefaultValueInitialized: true
  - first:
      WebGL: WebGL
    second:
      enabled: 1
      settings: {}
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


def stage_web_package() -> None:
    artifact = build_web_native()
    WEB_PACKAGE_DIR.mkdir(parents=True, exist_ok=True)
    shutil.copy2(artifact, WEB_PACKAGE_ARCHIVE)
    WEB_PACKAGE_DIR_META.write_text(WEB_PACKAGE_DIR_META_TEXT, encoding="utf-8")
    WEB_PACKAGE_ARCHIVE_META.write_text(WEB_PACKAGE_ARCHIVE_META_TEXT, encoding="utf-8")
    verify_web_package(rebuild=False)
    print("\nWEB2 PACKAGE PAYLOAD: STAGED — UnityPackage/Plugins/WebGL")


def verify_web_package(rebuild: bool = True) -> None:
    required = (WEB_PACKAGE_DIR_META, WEB_PACKAGE_ARCHIVE, WEB_PACKAGE_ARCHIVE_META)
    missing = [str(path.relative_to(ROOT)) for path in required if not path.is_file()]
    if missing:
        raise SystemExit(f"WEB2 package payload is incomplete: {', '.join(missing)}")
    if WEB_PACKAGE_DIR_META.read_text(encoding="utf-8") != WEB_PACKAGE_DIR_META_TEXT:
        raise SystemExit("WEB2 WebGL folder .meta drifted from the deterministic checked-in importer contract.")
    if WEB_PACKAGE_ARCHIVE_META.read_text(encoding="utf-8") != WEB_PACKAGE_ARCHIVE_META_TEXT:
        raise SystemExit("WEB2 Web archive .meta drifted from the deterministic checked-in importer contract.")
    if WEB_PACKAGE_ARCHIVE.stat().st_size == 0:
        raise SystemExit("WEB2 packaged Web archive is empty.")

    source = build_web_native() if rebuild else WEB_CARGO_TARGET_DIR / WEB_TARGET / "release" / "libtaffy_ugui.a"
    source_sha = hashlib.sha256(source.read_bytes()).hexdigest()
    packaged_sha = hashlib.sha256(WEB_PACKAGE_ARCHIVE.read_bytes()).hexdigest()
    if source_sha != packaged_sha:
        raise SystemExit("WEB2 packaged archive does not match the canonical clean Web build artifact.")

    native_text = (ROOT / "UnityPackage" / "Runtime" / "TaffyNative.cs").read_text(encoding="utf-8")
    web_internal_contract = '#if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR\n        internal const string Library = "__Internal";'
    if web_internal_contract not in native_text:
        raise SystemExit("WEB2 requires TaffyNative.Library to remain __Internal for WebGL Player builds only.")
    if 'UNITY_WEBGL' not in native_text or 'internal const string Library = "taffy_ugui";' not in native_text:
        raise SystemExit("WEB2 TaffyNative library-selection contract is incomplete.")

    for path in required:
        ignored = subprocess.run(
            [require("git"), "check-ignore", "-q", str(path.relative_to(ROOT))],
            cwd=ROOT,
            env=base_env(),
            check=False,
        ).returncode == 0
        if ignored:
            raise SystemExit(f"WEB2 package file is ignored and would be omitted from Git/UPM packaging: {path.relative_to(ROOT)}")

    print("WEB2 package verification: PASS — archive present, WebGL-only importer deterministic, Editor disabled, __Internal retained")


def web_native_env(target_dir: Path, poison_external_toolchain: bool = False) -> dict[str, str]:
    env = base_env()
    env["CARGO_TARGET_DIR"] = str(target_dir)
    for key in WEB_SANITIZED_ENV_KEYS:
        env.pop(key, None)
    env["CARGO_TARGET_WASM32_UNKNOWN_UNKNOWN_RUSTFLAGS"] = " ".join(WEB_RUSTFLAGS)
    if poison_external_toolchain:
        poison = str(ROOT / ".build" / "WEB_EXTERNAL_TOOLCHAIN_MUST_NOT_BE_USED")
        for key in WEB_EXTERNAL_TOOLCHAIN_ENV_KEYS:
            env[key] = poison
    return env


def require_web_rust_toolchain() -> str:
    rustup = require("rustup", f"Web builds require rustup with Rust {WEB_RUST_VERSION}.")
    env = base_env()
    toolchain = subprocess.run(
        [rustup, "run", WEB_RUST_VERSION, "rustc", "--version"],
        cwd=ROOT,
        env=env,
        text=True,
        capture_output=True,
        check=False,
    )
    if toolchain.returncode != 0 or f"rustc {WEB_RUST_VERSION} " not in toolchain.stdout:
        raise SystemExit(
            f"Canonical Web builds require Rust {WEB_RUST_VERSION}. Install it with: "
            f"rustup toolchain install {WEB_RUST_VERSION} --profile minimal --target {WEB_TARGET}"
        )
    targets = subprocess.run(
        [rustup, "target", "list", "--installed", "--toolchain", WEB_RUST_VERSION],
        cwd=ROOT,
        env=env,
        text=True,
        capture_output=True,
        check=False,
    )
    if targets.returncode != 0 or WEB_TARGET not in targets.stdout.splitlines():
        raise SystemExit(
            f"Rust {WEB_RUST_VERSION} is missing target {WEB_TARGET}. Install it with: "
            f"rustup target add {WEB_TARGET} --toolchain {WEB_RUST_VERSION}"
        )
    return rustup


def build_web_native(
    target_dir: Path = WEB_CARGO_TARGET_DIR,
    poison_external_toolchain: bool = False,
) -> Path:
    if not WEB_MANIFEST.is_file():
        raise SystemExit(f"Web Cargo manifest is missing: {WEB_MANIFEST.relative_to(ROOT)}")
    rustup = require_web_rust_toolchain()
    env = web_native_env(target_dir, poison_external_toolchain)
    run(
        rustup,
        "run",
        WEB_RUST_VERSION,
        "cargo",
        "build",
        "--manifest-path",
        str(WEB_MANIFEST),
        "--locked",
        "--release",
        "--target",
        WEB_TARGET,
        env=env,
    )
    artifact = target_dir / WEB_TARGET / "release" / "libtaffy_ugui.a"
    if not artifact.is_file() or artifact.stat().st_size == 0:
        raise SystemExit(f"Expected Web native archive was not produced: {artifact}")
    forbidden_features = tuple(
        feature
        for feature in (b"bulk-memory-opt", b"call-indirect-overlong")
        if feature in artifact.read_bytes()
    )
    if forbidden_features:
        names = ", ".join(feature.decode("ascii") for feature in forbidden_features)
        raise SystemExit(
            "Web archive contains LLVM Wasm target features rejected by older Unity Binaryen: " + names
        )
    print(
        f"Built Web native archive with Rust {WEB_RUST_VERSION}: "
        f"{artifact.relative_to(ROOT)}"
    )
    return artifact



def rust_llvm_nm() -> str:
    rustc = require("rustc", f"Install Rust {DEV_RUST_VERSION}; see docs/LOCAL_DEVELOPMENT.md.")
    sysroot = Path(run(rustc, "--print", "sysroot", capture=True, env=base_env()).strip())
    candidates = sorted((sysroot / "lib" / "rustlib").glob("*/bin/llvm-nm*"))
    for candidate in candidates:
        if candidate.is_file() and candidate.name in ("llvm-nm", "llvm-nm.exe"):
            return str(candidate)
    raise SystemExit(
        "Rust llvm-nm is missing. Install the pinned llvm-tools-preview component "
        f"for Rust {DEV_RUST_VERSION}."
    )


def verify_web_abi_surface() -> tuple[str, ...]:
    artifact = build_web_native()
    output = run(
        rust_llvm_nm(),
        "-g",
        "--defined-only",
        "-P",
        str(artifact),
        capture=True,
        env=web_native_env(WEB_CARGO_TARGET_DIR),
    )
    symbols: list[str] = []
    for line in output.splitlines():
        fields = line.split()
        if fields and fields[0].startswith("tu_"):
            symbols.append(fields[0])

    expected = tuple(sorted(header_export_contract()))
    actual = tuple(sorted(set(symbols)))
    missing = sorted(set(expected) - set(actual))
    extra = sorted(set(actual) - set(expected))
    duplicates = sorted(name for name in actual if symbols.count(name) != 1)
    if missing or extra or duplicates:
        raise SystemExit(
            "Web public ABI surface mismatch. "
            f"Missing={missing or 'none'} Extra={extra or 'none'} "
            f"DuplicateDefinitions={duplicates or 'none'}"
        )
    print(f"WEB ABI SURFACE VERIFY: PASS — exactly {len(actual)} expected tu_* exports")
    return actual




def _unity_version_key(value: str) -> tuple[int, ...]:
    return tuple(int(part) for part in re.findall(r"\d+", value))


def _unity_hub_editor_roots() -> tuple[Path, ...]:
    roots = [
        Path.home() / "Unity" / "Hub" / "Editor",
        Path("/Applications/Unity/Hub/Editor"),
    ]
    for env_name in ("PROGRAMFILES", "PROGRAMFILES(X86)"):
        if value := os.environ.get(env_name):
            roots.append(Path(value) / "Unity" / "Hub" / "Editor")
    if value := os.environ.get("UNITY_HUB_EDITORS_PATH"):
        roots.insert(0, Path(value).expanduser())

    existing: list[Path] = []
    seen: set[Path] = set()
    for root in roots:
        root = root.expanduser()
        if root in seen:
            continue
        seen.add(root)
        if root.is_dir():
            existing.append(root)
    return tuple(existing)


def _installed_unity_editors() -> dict[str, Path]:
    editors: dict[str, Path] = {}
    for hub_root in _unity_hub_editor_roots():
        for editor in hub_root.iterdir():
            if editor.is_dir():
                editors.setdefault(editor.name, editor)
    return editors


def _unity_web_emscripten_root(editor: Path) -> Path:
    return editor / "Editor" / "Data" / "PlaybackEngines" / "WebGLSupport" / "BuildTools" / "Emscripten"


def _unity_web_toolchain_details(root: Path) -> tuple[Path, Path, Path, str, tuple[Path, ...]]:
    emcc = root / "emscripten" / "emcc.py"
    node = root / "node" / ("node.exe" if os.name == "nt" else "node")
    config = root / ".emscripten"
    version_file = root / "emscripten" / "emscripten-version.txt"
    missing = tuple(path for path in (emcc, node, config) if not path.is_file())
    version = (
        version_file.read_text(encoding="utf-8").strip().strip('"')
        if version_file.is_file()
        else "unknown"
    )
    return emcc, node, config, version, missing


def _select_unity_editor(editors: dict[str, Path], selector: str) -> tuple[str, Path] | None:
    matches = [
        (version, editor)
        for version, editor in editors.items()
        if version == selector or version.startswith(selector + ".")
    ]
    return max(matches, key=lambda item: _unity_version_key(item[0])) if matches else None


def report_web_unity_toolchains(require_complete: bool = False) -> None:
    editors = _installed_unity_editors()
    gate_rows: list[dict[str, object]] = []

    print("WEB UNITY TOOLCHAIN MATRIX")
    print(f"{'gate':20} {'selector':14} {'editor':16} {'emscripten':16} status")
    print(f"{'-' * 20} {'-' * 14} {'-' * 16} {'-' * 16} {'-' * 16}")
    for gate, selector in WEB_UNITY_MATRIX_GATES:
        selected = _select_unity_editor(editors, selector)
        editor_version: str | None = None
        editor_root: Path | None = None
        emscripten_version: str | None = None
        status = "missing-editor"
        missing_components: tuple[Path, ...] = ()

        if selected is not None:
            editor_version, editor_root = selected
            emscripten_root = _unity_web_emscripten_root(editor_root)
            if not emscripten_root.is_dir():
                status = "missing-webgl"
            else:
                _, _, _, emscripten_version, missing_components = _unity_web_toolchain_details(emscripten_root)
                status = "ready" if not missing_components else "incomplete"

        gate_rows.append(
            {
                "gate": gate,
                "selector": selector,
                "editor": editor_version,
                "editor_root": str(editor_root) if editor_root is not None else None,
                "emscripten": emscripten_version,
                "status": status,
                "missing_components": [str(path) for path in missing_components],
            }
        )
        print(
            f"{gate:20} {selector:14} {(editor_version or '-'):16} "
            f"{(emscripten_version or '-'):16} {status}"
        )

    installed_rows: list[dict[str, object]] = []
    for editor_version, editor_root in sorted(editors.items(), key=lambda item: _unity_version_key(item[0])):
        emscripten_root = _unity_web_emscripten_root(editor_root)
        emscripten_version: str | None = None
        status = "no-webgl"
        missing_components: tuple[Path, ...] = ()
        if emscripten_root.is_dir():
            _, _, _, emscripten_version, missing_components = _unity_web_toolchain_details(emscripten_root)
            status = "ready" if not missing_components else "incomplete"
        installed_rows.append(
            {
                "editor": editor_version,
                "editor_root": str(editor_root),
                "emscripten": emscripten_version,
                "status": status,
                "missing_components": [str(path) for path in missing_components],
            }
        )

    evidence_dir = ROOT / ".build" / "evidence"
    evidence_dir.mkdir(parents=True, exist_ok=True)
    evidence_path = evidence_dir / "web-unity-toolchains.json"
    evidence_path.write_text(
        json.dumps(
            {
                "gates": gate_rows,
                "installed_editors": installed_rows,
            },
            indent=2,
            sort_keys=True,
        )
        + "\n",
        encoding="utf-8",
    )
    print(f"Evidence: {evidence_path.relative_to(ROOT)}")

    incomplete = [row for row in gate_rows if row["status"] != "ready"]
    if require_complete and incomplete:
        summary = ", ".join(f"{row['gate']}={row['status']}" for row in incomplete)
        raise SystemExit("Unity Web toolchain matrix is incomplete: " + summary)


def find_unity_web_emscripten() -> tuple[Path, Path, Path, str]:
    override = os.environ.get(WEB_UNITY_EMSCRIPTEN_ROOT_ENV)
    candidates: list[tuple[tuple[int, ...], str, Path]] = []

    if override:
        root = Path(override).expanduser().resolve()
        candidates.append(((), "override", root))
    else:
        for editor_version, editor in _installed_unity_editors().items():
            emscripten_root = _unity_web_emscripten_root(editor)
            if emscripten_root.is_dir():
                candidates.append((_unity_version_key(editor_version), editor_version, emscripten_root))

    if not candidates:
        raise SystemExit(
            "No Unity WebGL Emscripten toolchain was found. Install WebGL Build Support for a local Unity Editor "
            f"or set {WEB_UNITY_EMSCRIPTEN_ROOT_ENV} to its BuildTools/Emscripten directory."
        )

    _, label, root = max(candidates, key=lambda item: item[0])
    emcc, node, _, version, missing = _unity_web_toolchain_details(root)
    if missing:
        raise SystemExit(
            f"Unity WebGL toolchain '{label}' is incomplete; missing: "
            + ", ".join(str(path) for path in missing)
        )
    return root, emcc, node, version


def _verify_web_link_with_toolchain(
    archive: Path,
    emscripten_root: Path,
    output_dir: Path,
) -> str:
    if not WEB_LINK_HARNESS.is_file():
        raise SystemExit(f"Permanent Web link harness is missing: {WEB_LINK_HARNESS.relative_to(ROOT)}")
    if not archive.is_file() or archive.stat().st_size == 0:
        raise SystemExit(f"Web archive is missing or empty: {archive}")

    emcc, node, _, emscripten_version, missing = _unity_web_toolchain_details(emscripten_root)
    if missing:
        raise SystemExit(
            "Unity WebGL toolchain is incomplete; missing: "
            + ", ".join(str(path) for path in missing)
        )

    env = base_env()
    env["EM_CONFIG"] = str(emscripten_root / ".emscripten")
    env["EM_CACHE"] = str(emscripten_root / "emscripten" / "cache")

    shutil.rmtree(output_dir, ignore_errors=True)
    output_dir.mkdir(parents=True, exist_ok=True)
    output_js = output_dir / "taffy_web_link_harness.js"

    run(
        sys.executable,
        str(emcc),
        "-std=c11",
        "-Wall",
        "-Wextra",
        "-Werror",
        "-I",
        str(HEADER.parent),
        f"-DTAFFY_EXPECTED_ABI_VERSION={parse_u32_const('TU_ABI_VERSION')}",
        f"-DTAFFY_EXPECTED_ABI_STAGE={parse_u32_const('TU_ABI_STAGE')}",
        str(WEB_LINK_HARNESS),
        str(archive),
        "-O0",
        "-sASSERTIONS=1",
        "-sENVIRONMENT=node",
        "-sEXIT_RUNTIME=1",
        "-o",
        str(output_js),
        env=env,
    )
    output = run(str(node), str(output_js), capture=True, cwd=output_dir, env=env)
    marker = "TAFFY_WEB_LINK_HARNESS_PASS"
    if marker not in output:
        raise SystemExit(f"Web link harness did not report {marker}. Output: {output.strip() or '<empty>'}")
    return emscripten_version


def verify_web_link_harness() -> None:
    artifact = build_web_native()
    emscripten_root, _, _, _ = find_unity_web_emscripten()
    emscripten_version = _verify_web_link_with_toolchain(artifact, emscripten_root, WEB_LINK_HARNESS_DIR)
    print(
        "WEB LINK HARNESS VERIFY: PASS — public header ABI linked and executed "
        f"with Unity Emscripten {emscripten_version}"
    )


def verify_web_unity_matrix_links() -> None:
    verify_web_package()
    editors = _installed_unity_editors()
    archive_sha = hashlib.sha256(WEB_PACKAGE_ARCHIVE.read_bytes()).hexdigest()
    archive_size = WEB_PACKAGE_ARCHIVE.stat().st_size
    rows: list[dict[str, object]] = []

    for gate, selector in WEB_UNITY_MATRIX_GATES:
        selected = _select_unity_editor(editors, selector)
        if selected is None:
            rows.append({
                "gate": gate,
                "selector": selector,
                "editor": None,
                "emscripten": None,
                "status": "missing-editor",
            })
            continue

        editor_version, editor_root = selected
        emscripten_root = _unity_web_emscripten_root(editor_root)
        if not emscripten_root.is_dir():
            rows.append({
                "gate": gate,
                "selector": selector,
                "editor": editor_version,
                "emscripten": None,
                "status": "missing-webgl",
            })
            continue

        _, _, _, emscripten_version, missing = _unity_web_toolchain_details(emscripten_root)
        if missing:
            rows.append({
                "gate": gate,
                "selector": selector,
                "editor": editor_version,
                "emscripten": emscripten_version,
                "status": "incomplete",
                "missing_components": [str(path) for path in missing],
            })
            continue

        output_dir = ROOT / ".build" / "web-unity-matrix-links" / selector
        linked_version = _verify_web_link_with_toolchain(WEB_PACKAGE_ARCHIVE, emscripten_root, output_dir)
        rows.append({
            "gate": gate,
            "selector": selector,
            "editor": editor_version,
            "emscripten": linked_version,
            "status": "pass",
        })
        print(
            f"WEB UNITY MATRIX LINK: PASS — {gate} {editor_version} / "
            f"Emscripten {linked_version} / archive {archive_sha[:12]}"
        )

    evidence_dir = ROOT / ".build" / "evidence"
    evidence_dir.mkdir(parents=True, exist_ok=True)
    evidence_path = evidence_dir / "web-unity-matrix-links.json"
    evidence_path.write_text(
        json.dumps(
            {
                "archive": str(WEB_PACKAGE_ARCHIVE.relative_to(ROOT)),
                "archive_sha256": archive_sha,
                "archive_bytes": archive_size,
                "rows": rows,
            },
            indent=2,
            sort_keys=True,
        )
        + "\n",
        encoding="utf-8",
    )
    print(f"Evidence: {evidence_path.relative_to(ROOT)}")

    failed = [row for row in rows if row["status"] != "pass"]
    if failed:
        summary = ", ".join(f"{row['gate']}={row['status']}" for row in failed)
        raise SystemExit(
            "Unity Web matrix link verification is incomplete or incompatible: "
            + summary
        )

    print(
        "WEB UNITY MATRIX LINK VERIFY: PASS — every exact gate linked and executed "
        f"the same staged archive sha256={archive_sha}"
    )


def verify_web_independence() -> None:
    rustup = require_web_rust_toolchain()
    env = web_native_env(WEB_INDEPENDENCE_TARGET_DIR, poison_external_toolchain=True)
    metadata = json.loads(
        run(
            rustup,
            "run",
            WEB_RUST_VERSION,
            "cargo",
            "metadata",
            "--manifest-path",
            str(WEB_MANIFEST),
            "--locked",
            "--format-version",
            "1",
            capture=True,
            env=env,
        )
    )
    resolved_ids = {node["id"] for node in metadata.get("resolve", {}).get("nodes", [])}
    linked_packages = sorted(
        package["name"]
        for package in metadata.get("packages", [])
        if package.get("id") in resolved_ids and package.get("links")
    )
    if linked_packages:
        raise SystemExit(
            "Generic Web dependency graph declares native-library links: "
            + ", ".join(linked_packages)
        )

    shutil.rmtree(WEB_INDEPENDENCE_TARGET_DIR, ignore_errors=True)
    build_web_native(WEB_INDEPENDENCE_TARGET_DIR, poison_external_toolchain=True)
    print("WEB NATIVE INDEPENDENCE VERIFY: PASS — no Unity/Emscripten toolchain or native-library dependency")


def _production_rust_text(path: Path) -> str:
    text = path.read_text(encoding="utf-8")
    test_module = re.search(r"(?m)^#\[cfg\(test\)\]\s*\nmod\s+[A-Za-z0-9_]+\s*\{", text)
    return text[: test_module.start()] if test_module else text


def verify_web_panic_boundary() -> None:
    if "-Cpanic=abort" not in WEB_RUSTFLAGS:
        raise SystemExit("Web panic audit requires the canonical -Cpanic=abort compatibility build.")

    panic_tokens = (
        ".unwrap(",
        "panic!(",
        "assert!(",
        "assert_eq!(",
        "assert_ne!(",
        "unreachable!(",
        "unimplemented!(",
        "todo!(",
    )
    violations: list[str] = []
    production_sources: dict[Path, str] = {}
    for path in sorted((ROOT / "native" / "src").glob("*.rs")):
        text = _production_rust_text(path)
        production_sources[path] = text
        panic_scan_text = (
            text.replace("debug_assert!(", "")
            .replace("debug_assert_eq!(", "")
            .replace("debug_assert_ne!(", "")
        )
        for token in panic_tokens:
            if token in panic_scan_text:
                violations.append(f"{path.relative_to(ROOT)} contains production {token}")
        if path.name != "context.rs" and ".expect(" in text:
            violations.append(f"{path.relative_to(ROOT)} contains a production .expect() outside Taffy callbacks")

    context_path = ROOT / "native" / "src" / "context.rs"
    context_text = production_sources[context_path]
    expected_invariant_counts = {
        "Taffy only requests live nodes": 2,
        "live parent": 3,
        "live node": 11,
        "live child": 3,
    }
    actual_invariant_counts: dict[str, int] = {}
    for message in re.findall(r'\.expect\("([^\"]+)"\)', context_text):
        actual_invariant_counts[message] = actual_invariant_counts.get(message, 0) + 1
    if actual_invariant_counts != expected_invariant_counts:
        violations.append(
            "native/src/context.rs Taffy invariant expect inventory changed: "
            f"expected={expected_invariant_counts} actual={actual_invariant_counts}"
        )
    if context_text.count("children[child_index]") != 1:
        violations.append("native/src/context.rs Taffy child-index invariant inventory changed")

    if violations:
        raise SystemExit("Web panic-boundary audit failed:\n- " + "\n- ".join(violations))

    verify_web_link_harness()
    print(
        "WEB PANIC BOUNDARY VERIFY: PASS — recoverable ABI errors return status under panic=abort; "
        "remaining panic-capable sites are pinned Taffy live-tree invariants or release-disabled debug assertions"
    )


def verify_web_thread_local_contexts() -> None:
    version_text = (ROOT / "native" / "src" / "version.rs").read_text(encoding="utf-8")
    context_text = (ROOT / "native" / "src" / "context.rs").read_text(encoding="utf-8")
    managed_text = (ROOT / "UnityPackage" / "Runtime" / "TaffyNative.cs").read_text(encoding="utf-8")

    required_source_contract = (
        (version_text, "pub const TU_CAP_THREAD_LOCAL_CONTEXTS: u64 = 1 << 8;", "native capability bit"),
        (version_text, "| TU_CAP_THREAD_LOCAL_CONTEXTS", "native capability aggregation"),
        (context_text, "thread_local!", "thread-local context registry"),
        (context_text, "Some(_) => Err(NativeError::WrongThread)", "native wrong-thread rejection"),
        (managed_text, "CapThreadLocalContexts = 1UL << 8", "managed capability bit"),
        (managed_text, "CapThreadLocalContexts;", "managed required-capability handshake"),
    )
    missing = [label for text, needle, label in required_source_contract if needle not in text]
    if missing:
        raise SystemExit(
            "Web thread-local-context contract drifted; missing: " + ", ".join(missing)
        )

    verify_web_link_harness()

    cargo_bin = require("cargo", f"Install Rust {DEV_RUST_VERSION}; see docs/LOCAL_DEVELOPMENT.md.")
    run(
        cargo_bin,
        "test",
        "--manifest-path",
        str(MANIFEST),
        "--locked",
        "--test",
        "native_verification",
        "p3_7_wrong_thread_use_is_rejected",
        "--",
        "--exact",
        env=base_env(),
    )
    print(
        "WEB THREAD-LOCAL CONTEXT VERIFY: PASS — default Web keeps CapThreadLocalContexts; "
        "native wrong-thread enforcement remains unchanged"
    )


def verify_web_size() -> None:
    manifest_text = WEB_MANIFEST.read_text(encoding="utf-8")
    required_profile = (
        'lto = "thin"',
        "codegen-units = 1",
        'strip = "symbols"',
    )
    missing_profile = [entry for entry in required_profile if entry not in manifest_text]
    if missing_profile:
        raise SystemExit(
            "Web release-size profile drifted; missing: " + ", ".join(missing_profile)
        )
    if WEB_RUSTFLAGS != ("-Ctarget-cpu=mvp", "-Cpanic=abort"):
        raise SystemExit(
            "Web size review refuses compatibility-changing Rust flags; expected only target-cpu=mvp and panic=abort."
        )

    artifact = build_web_native()
    archive_bytes = artifact.stat().st_size
    archive_sha256 = hashlib.sha256(artifact.read_bytes()).hexdigest()
    review_threshold = 6 * 1024 * 1024
    if archive_bytes > review_threshold:
        raise SystemExit(
            f"Web archive is {archive_bytes} bytes, above the {review_threshold}-byte review threshold. "
            "Review size growth before changing compatibility-sensitive build settings."
        )

    verify_web_link_harness()
    linked_wasm = WEB_LINK_HARNESS_DIR / "taffy_web_link_harness.wasm"
    linked_bytes = linked_wasm.stat().st_size if linked_wasm.is_file() else 0
    print(
        "WEB SIZE VERIFY: PASS — "
        f"archive={archive_bytes} bytes ({archive_bytes / (1024 * 1024):.4f} MiB) "
        f"sha256={archive_sha256} harness_wasm={linked_bytes} bytes; "
        "no additional size flags adopted before the Unity 2021.3 old-linker gate"
    )


def verify_web_source_cleanliness() -> None:
    git_bin = require("git", "Web source-cleanliness verification requires Git.")
    tracked = set(run(git_bin, "ls-files", capture=True, env=base_env()).splitlines())

    disposable_prefixes = (
        ".build/",
        "target/",
        "native/target/",
        "native/web/target/",
        ".local-validation/",
        ".harness/",
        ".probes/",
        "tests/harness/",
        "tests/probes/",
        "scripts/harness/",
        "scripts/probes/",
    )
    generated_web_suffixes = (".a", ".wasm", ".o", ".bc", ".js")
    violations = [
        path
        for path in sorted(tracked)
        if path.startswith(disposable_prefixes)
        or (path.startswith("native/web/") and path.endswith(generated_web_suffixes))
    ]

    status = run(
        git_bin,
        "status",
        "--porcelain=v1",
        "--untracked-files=all",
        capture=True,
        env=base_env(),
    )
    leaked_untracked: list[str] = []
    for line in status.splitlines():
        if not line.startswith("?? "):
            continue
        path = line[3:]
        if path.startswith(disposable_prefixes) or (
            path.startswith("native/web/") and path.endswith(generated_web_suffixes)
        ):
            leaked_untracked.append(path)
    if leaked_untracked:
        violations.extend(f"unignored:{path}" for path in leaked_untracked)

    expected_ignored_paths = (
        ".build/web-cargo-target/probe.o",
        ".build/web-link-harness/probe.wasm",
        "native/web/target/probe.o",
        "native/web/libtaffy_ugui.a",
        "native/web/probe.wasm",
        "native/web/probe.o",
        "native/web/probe.bc",
        "native/web/probe.js",
        ".harness/web-probe",
        ".probes/web-probe",
    )
    for relative_path in expected_ignored_paths:
        completed = subprocess.run(
            [git_bin, "check-ignore", "--no-index", "-q", relative_path],
            cwd=ROOT,
            env=base_env(),
            check=False,
        )
        if completed.returncode != 0:
            violations.append(f"not-ignored:{relative_path}")

    required_web_sources = (
        ROOT / "native" / "web" / "Cargo.toml",
        ROOT / "native" / "web" / "Cargo.lock",
        WEB_LINK_HARNESS,
    )
    missing_sources = [str(path.relative_to(ROOT)) for path in required_web_sources if not path.is_file()]
    if missing_sources:
        violations.extend(f"missing-source:{path}" for path in missing_sources)

    if violations:
        raise SystemExit(
            "Web generated/probe source-cleanliness check failed:\n- " + "\n- ".join(violations)
        )

    print(
        "WEB SOURCE CLEANLINESS VERIFY: PASS — disposable build/probe paths are ignored, "
        "no generated Web archive/object/Wasm/JS artifact is tracked or leaking as untracked source"
    )


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
    sub.add_parser("verify-web-abi-surface", help="Verify the Web archive exposes exactly the checked-in public tu_* ABI")
    sub.add_parser("web-native", help="Build the generic wasm32-unknown-unknown Web static archive")
    sub.add_parser("stage-web-package", help="Build and stage the Web archive under UnityPackage/Plugins/WebGL with deterministic importer metadata")
    sub.add_parser("verify-web-package", help="Verify WEB2 Unity package artifact, importer isolation, __Internal binding, and Git/UPM inclusion")
    sub.add_parser("verify-web-link-harness", help="Link and execute the permanent public-header Web ABI harness with Unity Emscripten")
    sub.add_parser("verify-web-unity-matrix-links", help="Link and execute the same staged Web archive across every exact WEB5 Unity/Emscripten gate")
    toolchains = sub.add_parser("web-unity-toolchains", help="Report WEB5 Unity gate editors and exact bundled Emscripten toolchains")
    toolchains.add_argument("--require-complete", action="store_true", help="Fail when any WEB5 Unity gate editor/WebGL toolchain is unavailable")
    sub.add_parser("verify-web-panic-boundary", help="Audit panic=abort Web boundary invariants and run malformed-input ABI regressions")
    sub.add_parser("verify-web-thread-local-contexts", help="Verify Web thread-local capability semantics without weakening native wrong-thread enforcement")
    sub.add_parser("verify-web-size", help="Measure the canonical Web archive and reject unreviewed size or compatibility-profile drift")
    sub.add_parser("verify-web-source-cleanliness", help="Reject tracked or unignored generated/probe Web artifacts and verify disposable paths stay ignored")
    sub.add_parser("verify-web-independence", help="Prove the Web archive builds without Unity/Emscripten or external native libraries")
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
    elif args.command == "verify-web-abi-surface": verify_web_abi_surface()
    elif args.command == "phase4-host": phase4_host()
    elif args.command == "verify-phase4": verify_phase4()
    elif args.command == "verify-web-link-harness": verify_web_link_harness()
    elif args.command == "verify-web-unity-matrix-links": verify_web_unity_matrix_links()
    elif args.command == "web-unity-toolchains": report_web_unity_toolchains(args.require_complete)
    elif args.command == "web-native": build_web_native()
    elif args.command == "verify-web-panic-boundary": verify_web_panic_boundary()
    elif args.command == "stage-web-package": stage_web_package()
    elif args.command == "verify-web-package": verify_web_package()
    elif args.command == "verify-web-thread-local-contexts": verify_web_thread_local_contexts()
    elif args.command == "verify-web-size": verify_web_size()
    elif args.command == "verify-web-source-cleanliness": verify_web_source_cleanliness()
    elif args.command == "verify-web-independence": verify_web_independence()
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
