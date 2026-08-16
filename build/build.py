#!/usr/bin/env python3
"""Local-first build, verification, and native artifact staging for TaffyUGUI.

This script intentionally has no CI/provider dependency.  It is the canonical
local development gate.  GitHub may mirror the repository, but validation is
performed by commands executed on the developer machine.
"""
from __future__ import annotations

import argparse
import difflib
import hashlib
import json
import os
import platform
import re
import shutil
import subprocess
import sys
import tempfile
from dataclasses import dataclass
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
MANIFEST = ROOT / "native" / "Cargo.toml"
HEADER = ROOT / "include" / "taffy_ugui.h"
CBINDGEN_CONFIG = ROOT / "cbindgen.toml"
VERSION_RS = ROOT / "native" / "src" / "version.rs"
PACKAGE_JSON = ROOT / "UnityPackage" / "package.json"
CARGO_TARGET_DIR = ROOT / ".build" / "cargo-target"
DIST_NATIVE = ROOT / "dist" / "native"

DEV_RUST_VERSION = "1.97.1"
MSRV = "1.82"
CBINDGEN_VERSION = "0.29.2"
ABI_RC_VERSION = 1
ABI_RC_STAGE = 1
TAFFY_VERSION = "0.13.0"
ANDROID_NDK_REVISION = "21.3.6528147"  # Unity 2021.3 NDK r21d baseline
ANDROID_API = 21
WEBGL_EMSCRIPTEN_VERSION = "2.0.19"     # Unity 2021.3 Emscripten baseline

REQUIRED_EXPORTS = (
    "tu_get_abi_version",
    "tu_get_abi_stage",
    "tu_get_capabilities",
    "tu_get_taffy_version_packed",
    "tu_context_create",
    "tu_context_destroy",
    "tu_node_create",
    "tu_node_set_children",
    "tu_compute_layout",
    "tu_get_layout",
)


@dataclass(frozen=True)
class TargetSpec:
    name: str
    triple: str
    platform_name: str
    architecture: str
    artifact: str
    crate_type: str
    host_os: tuple[str, ...] = ()

    @property
    def stage_dir(self) -> Path:
        return DIST_NATIVE / self.platform_name / self.architecture


TARGETS: dict[str, TargetSpec] = {
    "windows-x64": TargetSpec("windows-x64", "x86_64-pc-windows-msvc", "windows", "x86_64", "taffy_ugui.dll", "cdylib", ("windows",)),
    "macos-arm64": TargetSpec("macos-arm64", "aarch64-apple-darwin", "macos", "arm64", "libtaffy_ugui.dylib", "cdylib", ("darwin",)),
    "macos-x64": TargetSpec("macos-x64", "x86_64-apple-darwin", "macos", "x86_64", "libtaffy_ugui.dylib", "cdylib", ("darwin",)),
    "android-arm64": TargetSpec("android-arm64", "aarch64-linux-android", "android", "arm64-v8a", "libtaffy_ugui.so", "cdylib"),
    "ios-arm64": TargetSpec("ios-arm64", "aarch64-apple-ios", "ios", "arm64", "libtaffy_ugui.a", "staticlib", ("darwin",)),
    "webgl": TargetSpec("webgl", "wasm32-unknown-emscripten", "webgl", "wasm32", "libtaffy_ugui.a", "staticlib"),
}


def run(*args: str, env: dict[str, str] | None = None, capture: bool = False) -> str:
    command = [str(a) for a in args]
    print("+", " ".join(command), flush=True)
    completed = subprocess.run(
        command,
        cwd=ROOT,
        check=True,
        env=env,
        text=True,
        stdout=subprocess.PIPE if capture else None,
        stderr=subprocess.STDOUT if capture else None,
    )
    return completed.stdout or ""


def executable(name: str) -> str | None:
    local = ROOT / ".toolchain" / "bin" / name
    if os.name == "nt" and not local.suffix:
        local = local.with_suffix(".exe")
    if local.exists():
        return str(local)
    return shutil.which(name)


def require(name: str, install_hint: str | None = None) -> str:
    value = executable(name)
    if value:
        return value
    hint = f" {install_hint}" if install_hint else ""
    raise SystemExit(
        f"Local prerequisite missing: '{name}' is not installed or available in .toolchain/bin.{hint}\n"
        "TaffyUGUI does not fall back to GitHub Actions; install the prerequisite locally and rerun."
    )


def base_env() -> dict[str, str]:
    env = os.environ.copy()
    local_bin = ROOT / ".toolchain" / "bin"
    if local_bin.exists():
        env["PATH"] = str(local_bin) + os.pathsep + env.get("PATH", "")
    env["CARGO_TARGET_DIR"] = str(CARGO_TARGET_DIR)
    return env


def cargo(command: str, *args: str, toolchain: str | None = None, env: dict[str, str] | None = None) -> None:
    cargo_bin = require("cargo", f"Install Rust {DEV_RUST_VERSION}; see docs/LOCAL_DEVELOPMENT.md.")
    command_line = [cargo_bin]
    if toolchain:
        # Works with rustup-managed cargo. A standalone local toolchain simply ignores
        # this path by using the default pinned compiler and should omit toolchain.
        command_line.append(f"+{toolchain}")
    command_line += [command, "--manifest-path", str(MANIFEST), *map(str, args)]
    run(*command_line, env=env or base_env())


def parse_u32_const(name: str) -> int:
    text = VERSION_RS.read_text(encoding="utf-8")
    match = re.search(rf"pub const {re.escape(name)}: u32 = (\d+);", text)
    if not match:
        raise SystemExit(f"Could not parse {name} in {VERSION_RS.relative_to(ROOT)}")
    return int(match.group(1))


def require_abi_rc() -> None:
    actual = (parse_u32_const("TU_ABI_VERSION"), parse_u32_const("TU_ABI_STAGE"))
    expected = (ABI_RC_VERSION, ABI_RC_STAGE)
    if actual != expected:
        raise SystemExit(f"Phase 4 is locked: expected ABI-v1-RC {expected[0]}/{expected[1]}, found {actual[0]}/{actual[1]}.")


def static_preflight() -> None:
    script = ROOT / "scripts" / "local_static_preflight.py"
    run(sys.executable, str(script))


def compile_header() -> None:
    clang = require("clang", "Install Clang for the C11 ABI smoke compile.")
    clangxx = require("clang++", "Install Clang++ for the C++17 ABI smoke compile.")
    with tempfile.TemporaryDirectory(prefix="taffyugui-header-") as directory:
        c_obj = Path(directory) / "smoke-c.o"
        cpp_obj = Path(directory) / "smoke-cpp.o"
        run(clang, "-std=c11", "-Wall", "-Wextra", "-Werror", "-I", str(HEADER.parent), "-c", str(ROOT / "tests/native-smoke/smoke.c"), "-o", str(c_obj))
        run(clangxx, "-std=c++17", "-Wall", "-Wextra", "-Werror", "-I", str(HEADER.parent), "-c", str(ROOT / "tests/native-smoke/smoke.cpp"), "-o", str(cpp_obj))
    print("C11/C++17 public-header compile: PASS")


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
    static_preflight()
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
    compile_header()


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


def host_smoke() -> None:
    cargo("build", "--locked", "--release")
    library = host_shared_library()
    if not library.exists():
        raise SystemExit(f"Host native library was not produced: {library}")
    if os.name == "nt":
        # Windows ABI execution is validated by Rust tests and the built DLL here. C/C++
        # link setup differs by installed MSVC/LLVM environment and is left to a Windows host.
        print(f"Host DLL built: {library}")
        return
    clang = require("clang")
    clangxx = require("clang++")
    with tempfile.TemporaryDirectory(prefix="taffyugui-linked-smoke-") as directory:
        out = Path(directory)
        c_bin = out / "smoke-c"
        cpp_bin = out / "smoke-cpp"
        lib_dir = library.parent
        rpath = f"-Wl,-rpath,{lib_dir}"
        link = ["-L", str(lib_dir), "-ltaffy_ugui", rpath]
        run(clang, "-std=c11", "-Wall", "-Wextra", "-Werror", "-I", str(HEADER.parent), str(ROOT / "tests/native-smoke/smoke.c"), *link, "-lm", "-o", str(c_bin))
        run(clangxx, "-std=c++17", "-Wall", "-Wextra", "-Werror", "-I", str(HEADER.parent), str(ROOT / "tests/native-smoke/smoke.cpp"), *link, "-o", str(cpp_bin))
        run(str(c_bin), env=base_env())
        run(str(cpp_bin), env=base_env())
    print("Linked C11/C++17 host ABI smoke: PASS")


def verify_abi_rc() -> None:
    require_abi_rc()
    compile_header()
    quality()
    verify_header()
    host_smoke()
    print("\nPHASE 3 LOCAL GATE: PASS — ABI-v1-RC is ready for Phase 4 platform builds.")


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
    return "linux"


def ensure_target_installed(triple: str) -> None:
    rustup = require("rustup", f"Install rustup and Rust {DEV_RUST_VERSION} for cross-platform target management.")
    installed = run(rustup, "target", "list", "--installed", capture=True, env=base_env()).splitlines()
    if triple not in installed:
        raise SystemExit(f"Rust target '{triple}' is not installed. Run locally: rustup target add {triple}")


def find_android_ndk() -> Path:
    raw = os.environ.get("ANDROID_NDK_HOME") or os.environ.get("ANDROID_NDK_ROOT")
    if not raw:
        raise SystemExit(f"ANDROID_NDK_HOME must point to Unity-compatible Android NDK r21d ({ANDROID_NDK_REVISION}).")
    ndk = Path(raw).resolve()
    props = ndk / "source.properties"
    if not props.exists():
        raise SystemExit(f"Android NDK source.properties missing: {props}")
    text = props.read_text(encoding="utf-8", errors="replace")
    if f"Pkg.Revision = {ANDROID_NDK_REVISION}" not in text:
        raise SystemExit(f"Android NDK must be {ANDROID_NDK_REVISION}; found:\n{text.strip()}")
    return ndk


def target_env(spec: TargetSpec) -> dict[str, str]:
    env = base_env()
    if spec.name == "android-arm64":
        ndk = find_android_ndk()
        bins = sorted((ndk / "toolchains/llvm/prebuilt").glob("*/bin"))
        if not bins:
            raise SystemExit("Android NDK LLVM prebuilt directory not found.")
        suffix = ".cmd" if os.name == "nt" else ""
        linker = bins[0] / f"aarch64-linux-android{ANDROID_API}-clang{suffix}"
        if not linker.exists():
            raise SystemExit(f"Android API {ANDROID_API} linker missing: {linker}")
        env["CARGO_TARGET_AARCH64_LINUX_ANDROID_LINKER"] = str(linker)
        env["CC_aarch64_linux_android"] = str(linker)
    elif spec.name == "webgl":
        emcc = require("emcc", f"Install the Unity-compatible Emscripten {WEBGL_EMSCRIPTEN_VERSION} toolchain.")
        version = run(emcc, "--version", capture=True)
        if WEBGL_EMSCRIPTEN_VERSION not in version:
            raise SystemExit(f"WebGL baseline requires Emscripten {WEBGL_EMSCRIPTEN_VERSION}; got {version.splitlines()[0] if version else 'unknown'}")
        env["CARGO_TARGET_WASM32_UNKNOWN_EMSCRIPTEN_LINKER"] = emcc
        env["CC_wasm32_unknown_emscripten"] = emcc
    return env


def cargo_artifact(spec: TargetSpec) -> Path:
    return CARGO_TARGET_DIR / spec.triple / "release" / spec.artifact


def inspect_artifact(spec: TargetSpec, artifact: Path) -> str:
    if not artifact.exists() or artifact.stat().st_size == 0:
        raise SystemExit(f"Expected artifact was not produced: {artifact}")
    file_bin = executable("file")
    description = artifact.name
    if file_bin:
        description = run(file_bin, "-b", str(artifact), capture=True).strip()
    lower = description.lower()
    expected_tokens = {
        "windows-x64": ("pe32+", "x86-64"),
        "macos-arm64": ("mach-o", "arm64"),
        "macos-x64": ("mach-o", "x86_64"),
        "android-arm64": ("elf", "aarch64"),
        "ios-arm64": ("archive",),
        "webgl": ("archive",),
    }[spec.name]
    if description != artifact.name and not all(token in lower for token in expected_tokens):
        raise SystemExit(f"Artifact architecture/format mismatch for {spec.name}: {description}")
    return description


def symbol_text(spec: TargetSpec, artifact: Path, env: dict[str, str]) -> str:
    if spec.name == "windows-x64":
        dumpbin = executable("dumpbin")
        if dumpbin:
            return run(dumpbin, "/exports", str(artifact), capture=True, env=env)
        llvm_nm = executable("llvm-nm")
        if llvm_nm:
            return run(llvm_nm, "--defined-only", str(artifact), capture=True, env=env)
        raise SystemExit("Windows export verification requires dumpbin or llvm-nm.")
    if spec.name == "android-arm64":
        ndk = find_android_ndk()
        candidates = sorted((ndk / "toolchains/llvm/prebuilt").glob("*/bin/llvm-nm*"))
        if not candidates:
            raise SystemExit("Android NDK llvm-nm not found.")
        return run(str(candidates[0]), "-D", "--defined-only", str(artifact), capture=True, env=env)
    nm = require("nm")
    args = [nm]
    if spec.platform_name == "macos":
        args += ["-gU"]
    else:
        args += ["-g"]
    args.append(str(artifact))
    return run(*args, capture=True, env=env)


def verify_symbols(spec: TargetSpec, artifact: Path, env: dict[str, str]) -> None:
    output = symbol_text(spec, artifact, env)
    missing = [name for name in REQUIRED_EXPORTS if name not in output]
    if missing:
        raise SystemExit(f"{artifact.name} is missing required ABI exports: {', '.join(missing)}")


def package_version() -> str:
    return str(json.loads(PACKAGE_JSON.read_text(encoding="utf-8"))["version"])


def source_revision() -> str:
    git = executable("git")
    if not git or not (ROOT / ".git").exists():
        return "local-unversioned"
    try:
        head = run(git, "rev-parse", "HEAD", capture=True).strip()
        dirty = subprocess.run([git, "diff", "--quiet"], cwd=ROOT).returncode != 0
        return head + ("+dirty" if dirty else "")
    except subprocess.CalledProcessError:
        return "local-uncommitted"


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def stage_manifest(spec: TargetSpec, artifact: Path, description: str) -> None:
    manifest = {
        "schema": 1,
        "package_version": package_version(),
        "abi": {"designation": "ABI-v1-RC", "version": ABI_RC_VERSION, "stage": ABI_RC_STAGE},
        "taffy_version": TAFFY_VERSION,
        "rust_target": spec.triple,
        "source_revision": source_revision(),
        "artifact": artifact.name,
        "platform": spec.platform_name,
        "architecture": spec.architecture,
        "crate_type": spec.crate_type,
        "file_description": description,
        "sha256": sha256(artifact),
        "built_locally": True,
    }
    spec.stage_dir.mkdir(parents=True, exist_ok=True)
    (spec.stage_dir / "manifest.json").write_text(json.dumps(manifest, indent=2, sort_keys=True) + "\n", encoding="utf-8")
    (spec.stage_dir / "SHA256SUMS").write_text(f"{manifest['sha256']}  {artifact.name}\n", encoding="utf-8")


def build_target(name: str) -> Path:
    require_abi_rc()
    spec = TARGETS[name]
    host = current_os()
    if spec.host_os and host not in spec.host_os:
        raise SystemExit(f"{name} must be built on {', '.join(spec.host_os)}; current host is {host}.")
    ensure_target_installed(spec.triple)
    env = target_env(spec)
    cargo("build", "--locked", "--release", "--target", spec.triple, env=env)
    built = cargo_artifact(spec)
    description = inspect_artifact(spec, built)
    verify_symbols(spec, built, env)
    spec.stage_dir.mkdir(parents=True, exist_ok=True)
    staged = spec.stage_dir / spec.artifact
    shutil.copy2(built, staged)
    stage_manifest(spec, staged, description)
    print(f"Staged local {name}: {staged.relative_to(ROOT)}")
    return staged


def macos_universal() -> Path:
    if current_os() != "darwin":
        raise SystemExit("macOS universal library must be assembled on macOS.")
    arm = build_target("macos-arm64")
    intel = build_target("macos-x64")
    lipo = require("lipo")
    output_dir = DIST_NATIVE / "macos" / "universal"
    output_dir.mkdir(parents=True, exist_ok=True)
    output = output_dir / "libtaffy_ugui.dylib"
    run(lipo, "-create", str(arm), str(intel), "-output", str(output))
    info = run(lipo, "-info", str(output), capture=True)
    if "arm64" not in info or "x86_64" not in info:
        raise SystemExit(f"Universal dylib missing architecture: {info.strip()}")
    checksum = sha256(output)
    (output_dir / "SHA256SUMS").write_text(f"{checksum}  {output.name}\n", encoding="utf-8")
    (output_dir / "manifest.json").write_text(json.dumps({
        "schema": 1,
        "package_version": package_version(),
        "abi": {"designation": "ABI-v1-RC", "version": 1, "stage": 1},
        "taffy_version": TAFFY_VERSION,
        "rust_targets": [TARGETS["macos-arm64"].triple, TARGETS["macos-x64"].triple],
        "source_revision": source_revision(),
        "artifact": output.name,
        "platform": "macos",
        "architecture": "universal",
        "sha256": checksum,
        "built_locally": True,
    }, indent=2, sort_keys=True) + "\n", encoding="utf-8")
    return output


def verify_staged(name: str) -> None:
    spec = TARGETS[name]
    artifact = spec.stage_dir / spec.artifact
    manifest_path = spec.stage_dir / "manifest.json"
    if not artifact.exists() or not manifest_path.exists():
        raise SystemExit(f"Staged target is incomplete: {name}")
    manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
    if manifest.get("sha256") != sha256(artifact):
        raise SystemExit(f"Checksum mismatch: {name}")
    abi = manifest.get("abi", {})
    if (abi.get("version"), abi.get("stage")) != (1, 1):
        raise SystemExit(f"Staged target is not ABI-v1-RC: {name}")
    print(f"Verified staged local artifact: {name}")


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
    native = sub.add_parser("native", help="Build and stage a Phase 4 native target locally")
    native.add_argument("target", choices=[*TARGETS.keys(), "macos-universal"])
    verify = sub.add_parser("verify-native", help="Verify a staged local artifact checksum/ABI manifest")
    verify.add_argument("targets", nargs="+", choices=list(TARGETS.keys()))
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
    elif args.command == "native":
        if args.target == "macos-universal": macos_universal()
        else: build_target(args.target)
    elif args.command == "verify-native":
        for target in args.targets: verify_staged(target)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
