

def require_phase3_evidence() -> dict[str, object]:
    revision = source_revision()
    if not PHASE3_EVIDENCE.exists():
        raise SystemExit(
            "Phase 4 is locked until the complete Phase 3 gate is executed locally on this exact revision. "
            "Run: python3 build/build.py verify-abi-final"
        )
    evidence = json.loads(PHASE3_EVIDENCE.read_text(encoding="utf-8"))
    if evidence.get("source_revision") != revision or evidence.get("source_tree") != source_tree_sha():
        raise SystemExit(
            "Phase 3 evidence belongs to different local source content. "
            "Rerun 'python3 build/build.py verify-abi-final' for the current content-addressed source snapshot."
        )
    abi = evidence.get("abi", {})
    if not isinstance(abi, dict) or (abi.get("version"), abi.get("stage")) != (ABI_RC_VERSION, ABI_RC_STAGE):
        raise SystemExit("Phase 3 evidence is not for final ABI v1 1/2.")
    if tuple(evidence.get("public_exports", [])) != header_export_contract():
        raise SystemExit("Phase 3 evidence export inventory no longer matches the public ABI header.")
    return evidence


def verify_dev_toolchain() -> None:
    rustc = require("rustc", f"Install Rust {DEV_RUST_VERSION}; see docs/LOCAL_DEVELOPMENT.md.")
    version = run(rustc, "--version", capture=True, env=base_env()).strip()
    if f"rustc {DEV_RUST_VERSION} " not in version:
        raise SystemExit(
            f"Canonical local gate requires Rust {DEV_RUST_VERSION}; got: {version}. "
            "Use scripts/bootstrap-local-toolchain.* or the pinned rust-toolchain.toml."
        )


def quality() -> None:
    require_abi_rc()
    verify_dev_toolchain()
    cargo("fmt", "--all", "--", "--check")
    cargo("clippy", "--locked", "--all-targets", "--", "-D", "warnings")
    cargo("test", "--locked")
    cargo("build", "--locked", "--release")


def generate_header(path: Path) -> None:
    cbindgen = require(
        "cbindgen",
        f"Install locally with: cargo install cbindgen --version {CBINDGEN_VERSION} --locked",
    )
    version = run(cbindgen, "--version", capture=True, env=base_env()).strip()
    if CBINDGEN_VERSION not in version:
        raise SystemExit(f"Canonical header generation requires cbindgen {CBINDGEN_VERSION}; got: {version}")
    run(cbindgen, str(ROOT / "native"), "--config", str(CBINDGEN_CONFIG), "--output", str(path), env=base_env())


def header() -> None:
    HEADER.parent.mkdir(parents=True, exist_ok=True)
    generate_header(HEADER)


def verify_header() -> None:
    with tempfile.TemporaryDirectory(prefix="taffyugui-cbindgen-") as directory:
        generated = Path(directory) / HEADER.name
        generate_header(generated)
        expected = HEADER.read_text(encoding="utf-8").splitlines(keepends=True)
        actual = generated.read_text(encoding="utf-8").splitlines(keepends=True)
        if expected != actual:
            diff = "".join(difflib.unified_diff(expected, actual, fromfile=str(HEADER.relative_to(ROOT)), tofile="cbindgen-generated/taffy_ugui.h"))
            raise SystemExit("Public header drift detected. Run 'python build/build.py header'.\n" + diff)
    print("cbindgen public-header drift check: PASS")


def host_shared_library() -> Path:
    if sys.platform.startswith("linux"):
        return CARGO_TARGET_DIR / "release" / "libtaffy_ugui.so"
    if sys.platform == "darwin":
        return CARGO_TARGET_DIR / "release" / "libtaffy_ugui.dylib"
    if os.name == "nt":
        return CARGO_TARGET_DIR / "release" / "taffy_ugui.dll"
    raise SystemExit(f"Unsupported local host: {sys.platform}")


def verify_abi_final() -> None:
    require_abi_rc()
    quality()
    verify_header()
    write_phase3_evidence()
    print("\nPHASE 3 LOCAL GATE: PASS — final ABI v1 is ready for Phase 4 platform builds.")


def prepare() -> None:
    """Canonicalize Rust formatting and regenerate the public header locally."""
    require_abi_rc()
    verify_dev_toolchain()
    cargo("fmt", "--all")
    header()
    print("Local source formatting and generated public header are canonicalized.")


def verify_msrv() -> None:
    rustup = require("rustup", "MSRV verification requires rustup so Rust 1.82.0 can be selected locally.")
    run(rustup, "toolchain", "install", "1.82.0", "--profile", "minimal", env=base_env())
    cargo("check", "--locked", toolchain="1.82.0")
    cargo("test", "--locked", toolchain="1.82.0")
    print("MSRV 1.82.0 local check/test: PASS")


def current_os() -> str:
    if os.name == "nt":
        return "windows"
    if sys.platform == "darwin":
        return "darwin"
    if sys.platform.startswith("linux"):
        return "linux"
    raise SystemExit(f"Unsupported canonical Phase 4 host OS: {sys.platform}")


def ensure_target_installed(triple: str) -> None:
    rustup = require("rustup", f"Install rustup and Rust {DEV_RUST_VERSION} for cross-platform target management.")
    installed = run(rustup, "target", "list", "--installed", capture=True, env=base_env()).splitlines()
    if triple not in installed:
        raise SystemExit(f"Rust target '{triple}' is not installed. Run locally: rustup target add {triple}")


def find_android_ndk() -> Path:
    raw = os.environ.get("ANDROID_NDK_HOME") or os.environ.get("ANDROID_NDK_ROOT")
    if not raw:
        local_ndk = ROOT / ".toolchain" / "android-ndk-r21d"
        if (local_ndk / "source.properties").is_file():
            raw = str(local_ndk)
    if not raw:
        raise SystemExit(
            f"ANDROID_NDK_HOME must point to Unity-compatible Android NDK r21d ({ANDROID_NDK_REVISION}), "
            "or install it at .toolchain/android-ndk-r21d."
        )
    ndk = Path(raw).resolve()
    props = ndk / "source.properties"
    if not props.exists():
        raise SystemExit(f"Android NDK source.properties missing: {props}")
    text = props.read_text(encoding="utf-8", errors="replace")
    if f"Pkg.Revision = {ANDROID_NDK_REVISION}" not in text:
        raise SystemExit(f"Android NDK must be {ANDROID_NDK_REVISION}; found:\n{text.strip()}")
    return ndk


def android_unwind_shim(ndk: Path) -> Path:
    """Expose r21d's AArch64 unwinder under Rust's expected library name."""
    prebuilt_root = ndk / "toolchains" / "llvm" / "prebuilt"
    prebuilts = sorted(prebuilt_root.glob("*/"))
    if not prebuilts:
        raise SystemExit("Android NDK LLVM prebuilt directory not found.")
    source = prebuilts[0] / "lib" / "gcc" / "aarch64-linux-android" / "4.9.x" / "libgcc_real.a"
    if not source.is_file():
        raise SystemExit(f"Android NDK r21d AArch64 unwinder archive is missing: {source}")

    shim_dir = ROOT / ".toolchain" / "android-link-shims" / "aarch64-linux-android"
    shim_dir.mkdir(parents=True, exist_ok=True)
    shim = shim_dir / "libunwind.a"
    if shim.exists() or shim.is_symlink():
        if not shim.is_symlink() or shim.resolve() != source.resolve():
            raise SystemExit(f"Android unwind shim has an unexpected target: {shim}")
    else:
        shim.symlink_to(source)
    return shim_dir
