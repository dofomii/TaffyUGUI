#!/usr/bin/env python3
"""Canonical TaffyUGUI native build, verification, and staging driver."""

from __future__ import annotations

import argparse
import difflib
import hashlib
import json
import os
import re
import shutil
import subprocess
import sys
import tempfile
from dataclasses import dataclass
from pathlib import Path
from typing import Iterable

ROOT = Path(__file__).resolve().parents[1]
MANIFEST = ROOT / "native" / "Cargo.toml"
HEADER = ROOT / "include" / "taffy_ugui.h"
CBINDGEN_CONFIG = ROOT / "cbindgen.toml"
DIST_NATIVE = ROOT / "dist" / "native"
VERSION_RS = ROOT / "native" / "src" / "version.rs"
PACKAGE_JSON = ROOT / "UnityPackage" / "package.json"

ABI_RC_VERSION = 1
ABI_RC_STAGE = 1
TAFFY_VERSION = "0.13.0"
ANDROID_NDK_REVISION = "21.3.6528147"
ANDROID_API = 21
WEBGL_EMSCRIPTEN_VERSION = "2.0.19"
REQUIRED_EXPORTS = (
    "tu_get_abi_version",
    "tu_get_abi_stage",
    "tu_get_capabilities",
    "tu_context_create",
    "tu_compute_layout",
)


@dataclass(frozen=True)
class TargetSpec:
    name: str
    triple: str
    platform: str
    architecture: str
    artifact: str
    crate_type: str
    host_os: tuple[str, ...] = ()
    prerequisite: str | None = None

    @property
    def stage_dir(self) -> Path:
        return DIST_NATIVE / self.platform / self.architecture


TARGETS: dict[str, TargetSpec] = {
    "windows-x64": TargetSpec(
        "windows-x64", "x86_64-pc-windows-msvc", "windows", "x86_64",
        "taffy_ugui.dll", "cdylib", ("windows",), "MSVC build tools",
    ),
    "macos-arm64": TargetSpec(
        "macos-arm64", "aarch64-apple-darwin", "macos", "arm64",
        "libtaffy_ugui.dylib", "cdylib", ("darwin",), "Xcode command-line tools",
    ),
    "macos-x64": TargetSpec(
        "macos-x64", "x86_64-apple-darwin", "macos", "x86_64",
        "libtaffy_ugui.dylib", "cdylib", ("darwin",), "Xcode command-line tools",
    ),
    "android-arm64": TargetSpec(
        "android-arm64", "aarch64-linux-android", "android", "arm64-v8a",
        "libtaffy_ugui.so", "cdylib", prerequisite=f"Android NDK r21d ({ANDROID_NDK_REVISION})",
    ),
    "ios-arm64": TargetSpec(
        "ios-arm64", "aarch64-apple-ios", "ios", "arm64",
        "libtaffy_ugui.a", "staticlib", ("darwin",), "Xcode/iPhoneOS SDK",
    ),
    "webgl": TargetSpec(
        "webgl", "wasm32-unknown-emscripten", "webgl", "wasm32",
        "libtaffy_ugui.a", "staticlib", prerequisite=f"Emscripten {WEBGL_EMSCRIPTEN_VERSION}",
    ),
}

REQUIRED_TARGETS = (
    "windows-x64",
    "macos-arm64",
    "macos-x64",
    "android-arm64",
    "ios-arm64",
    "webgl",
)


def run(*args: str, env: dict[str, str] | None = None, capture: bool = False) -> str:
    print("+", " ".join(str(arg) for arg in args), flush=True)
    completed = subprocess.run(
        args,
        cwd=ROOT,
        check=True,
        env=env,
        text=True,
        stdout=subprocess.PIPE if capture else None,
        stderr=subprocess.STDOUT if capture else None,
    )
    return completed.stdout or ""


def require(executable: str) -> str:
    path = shutil.which(executable)
    if path is None:
        raise SystemExit(
            f"Required tool '{executable}' was not found on PATH. "
            "See docs/PROJECT_DECISIONS.md and docs/NATIVE_LIBRARY_BUILD_PLAN.md."
        )
    return path


def cargo_command(command: str, *args: str, env: dict[str, str] | None = None) -> None:
    require("cargo")
    run("cargo", command, "--manifest-path", str(MANIFEST), *args, env=env)


def preflight() -> None:
    for script in (
        "phase1_static_preflight.py",
        "phase2_static_preflight.py",
        "phase3_static_preflight.py",
        "phase4_static_preflight.py",
    ):
        path = ROOT / "scripts" / script
        if path.exists():
            run(sys.executable, str(path))


def quality() -> None:
    preflight()
    cargo_command("fmt", "--all", "--", "--check")
    cargo_command("clippy", "--locked", "--all-targets", "--", "-D", "warnings")
    cargo_command("test", "--locked")
    cargo_command("build", "--locked", "--release")


def verify_header_diff() -> None:
    require("cbindgen")
    with tempfile.TemporaryDirectory() as directory:
        generated = Path(directory) / "taffy_ugui.h"
        run("cbindgen", str(ROOT / "native"), "--config", str(CBINDGEN_CONFIG), "--output", str(generated))
        expected = HEADER.read_text(encoding="utf-8").splitlines(keepends=True)
        actual = generated.read_text(encoding="utf-8").splitlines(keepends=True)
        if expected != actual:
            diff = "".join(difflib.unified_diff(expected, actual, fromfile="include/taffy_ugui.h", tofile="cbindgen-regenerated/taffy_ugui.h"))
            raise SystemExit("cbindgen header drift detected:\n" + diff)


def host_smoke() -> None:
    cargo_command("build", "--locked", "--release")
    clang = require("clang")
    clangxx = require("clang++")
    release = ROOT / "native" / "target" / "release"
    if sys.platform.startswith("linux"):
        library = release / "libtaffy_ugui.so"
        link_args = ["-L", str(release), "-ltaffy_ugui", "-Wl,-rpath," + str(release)]
    elif sys.platform == "darwin":
        library = release / "libtaffy_ugui.dylib"
        link_args = ["-L", str(release), "-ltaffy_ugui", "-Wl,-rpath," + str(release)]
    else:
        raise SystemExit("Phase 3 linked host smoke is implemented on Linux/macOS; Windows uses its CI artifact lane.")
    if not library.exists():
        raise SystemExit(f"Expected host shared library was not produced: {library}")
    with tempfile.TemporaryDirectory() as directory:
        directory_path = Path(directory)
        c_bin = directory_path / "tu-smoke-c"
        cpp_bin = directory_path / "tu-smoke-cpp"
        run(clang, "-std=c11", "-Wall", "-Wextra", "-Werror", "-I", str(ROOT / "include"), str(ROOT / "tests" / "native-smoke" / "smoke.c"), *link_args, "-lm", "-o", str(c_bin))
        run(clangxx, "-std=c++17", "-Wall", "-Wextra", "-Werror", "-I", str(ROOT / "include"), str(ROOT / "tests" / "native-smoke" / "smoke.cpp"), *link_args, "-o", str(cpp_bin))
        run(str(c_bin))
        run(str(cpp_bin))


def verify_abi_rc() -> None:
    quality()
    verify_header_diff()
    host_smoke()


def header() -> None:
    require("cbindgen")
    HEADER.parent.mkdir(parents=True, exist_ok=True)
    run("cbindgen", str(ROOT / "native"), "--config", str(CBINDGEN_CONFIG), "--output", str(HEADER))


def host() -> None:
    cargo_command("build", "--locked", "--release")


def current_os() -> str:
    if sys.platform.startswith("win"):
        return "windows"
    if sys.platform == "darwin":
        return "darwin"
    return "linux"


def ensure_host_supported(spec: TargetSpec) -> None:
    if spec.host_os and current_os() not in spec.host_os:
        detail = f" ({spec.prerequisite})" if spec.prerequisite else ""
        raise SystemExit(f"Target '{spec.name}' must be built on {', '.join(spec.host_os)}{detail}; current host is {current_os()}.")


def ensure_rust_target(triple: str) -> None:
    require("rustup")
    installed = run("rustup", "target", "list", "--installed", capture=True).splitlines()
    if triple not in installed:
        raise SystemExit(f"Rust target '{triple}' is not installed. Install it explicitly with: rustup target add {triple}")


def find_android_ndk() -> Path:
    raw = os.environ.get("ANDROID_NDK_HOME") or os.environ.get("ANDROID_NDK_ROOT")
    if not raw:
        raise SystemExit(f"ANDROID_NDK_HOME must point to Unity-compatible Android NDK r21d ({ANDROID_NDK_REVISION}).")
    ndk = Path(raw).resolve()
    properties = ndk / "source.properties"
    if not properties.exists():
        raise SystemExit(f"Android NDK source.properties not found under {ndk}")
    text = properties.read_text(encoding="utf-8", errors="replace")
    if f"Pkg.Revision = {ANDROID_NDK_REVISION}" not in text:
        raise SystemExit(f"Android NDK must be revision {ANDROID_NDK_REVISION}; found metadata: {text.strip()}")
    return ndk


def android_linker(ndk: Path) -> Path:
    prebuilt_root = ndk / "toolchains" / "llvm" / "prebuilt"
    candidates = sorted(prebuilt_root.glob("*/bin"))
    if not candidates:
        raise SystemExit(f"Android NDK LLVM prebuilt toolchain not found under {prebuilt_root}")
    suffix = ".cmd" if current_os() == "windows" else ""
    linker = candidates[0] / f"aarch64-linux-android{ANDROID_API}-clang{suffix}"
    if not linker.exists():
        raise SystemExit(f"Android ARM64 API {ANDROID_API} linker not found: {linker}")
    return linker


def require_emscripten() -> str:
    emcc = require("emcc")
    version = run(emcc, "--version", capture=True)
    if WEBGL_EMSCRIPTEN_VERSION not in version:
        raise SystemExit(f"WebGL requires Unity 2021.3-matched Emscripten {WEBGL_EMSCRIPTEN_VERSION}; got: {version.splitlines()[0] if version else 'unknown'}")
    return emcc


def target_environment(spec: TargetSpec) -> dict[str, str]:
    env = os.environ.copy()
    if spec.name == "android-arm64":
        ndk = find_android_ndk()
        linker = android_linker(ndk)
        env["CARGO_TARGET_AARCH64_LINUX_ANDROID_LINKER"] = str(linker)
        env["CC_aarch64_linux_android"] = str(linker)
    elif spec.name == "webgl":
        emcc = require_emscripten()
        env["CARGO_TARGET_WASM32_UNKNOWN_EMSCRIPTEN_LINKER"] = emcc
        env["CC_wasm32_unknown_emscripten"] = emcc
    return env


def parse_const(name: str) -> int:
    text = VERSION_RS.read_text(encoding="utf-8")
    match = re.search(rf"pub const {re.escape(name)}: u32 = (\d+);", text)
    if not match:
        raise SystemExit(f"Could not parse {name} from native/src/version.rs")
    return int(match.group(1))


def require_abi_rc() -> None:
    abi = parse_const("TU_ABI_VERSION")
    stage = parse_const("TU_ABI_STAGE")
    if (abi, stage) != (ABI_RC_VERSION, ABI_RC_STAGE):
        raise SystemExit(
            f"Platform artifacts require ABI-v1-RC ({ABI_RC_VERSION}/{ABI_RC_STAGE}); "
            f"source currently declares ABI {abi}, stage {stage}. Run and pass Phase 3 before platform compilation."
        )


def source_revision() -> str:
    if os.environ.get("GITHUB_SHA"):
        return os.environ["GITHUB_SHA"]
    if shutil.which("git") and (ROOT / ".git").exists():
        try:
            return run("git", "rev-parse", "HEAD", capture=True).strip()
        except subprocess.CalledProcessError:
            pass
    return "unknown"


def package_version() -> str:
    data = json.loads(PACKAGE_JSON.read_text(encoding="utf-8"))
    return str(data["version"])


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def cargo_artifact(spec: TargetSpec) -> Path:
    return ROOT / "native" / "target" / spec.triple / "release" / spec.artifact


def inspect_artifact(spec: TargetSpec, artifact: Path) -> str:
    if not artifact.exists() or artifact.stat().st_size == 0:
        raise SystemExit(f"Expected artifact was not produced: {artifact}")
    if current_os() != "windows" and shutil.which("file"):
        description = run("file", "-b", str(artifact), capture=True).strip()
    else:
        description = artifact.name
    lower = description.lower()
    expectations = {
        "windows-x64": ("pe32+", "x86-64"),
        "macos-arm64": ("mach-o", "arm64"),
        "macos-x64": ("mach-o", "x86_64"),
        "android-arm64": ("elf", "aarch64"),
        "ios-arm64": ("archive",),
        "webgl": ("archive",),
    }
    if description != artifact.name and not all(token in lower for token in expectations[spec.name]):
        raise SystemExit(f"Artifact format/architecture mismatch for {spec.name}: {description}")
    return description


def symbol_output(spec: TargetSpec, artifact: Path, env: dict[str, str]) -> str:
    if spec.name == "windows-x64":
        dumpbin = require("dumpbin")
        return run(dumpbin, "/exports", str(artifact), capture=True, env=env)
    if spec.name == "android-arm64":
        ndk = find_android_ndk()
        bins = sorted((ndk / "toolchains" / "llvm" / "prebuilt").glob("*/bin/llvm-nm*"))
        if not bins:
            raise SystemExit("llvm-nm was not found in the Android NDK toolchain.")
        return run(str(bins[0]), "-D", "--defined-only", str(artifact), capture=True, env=env)
    nm = require("nm")
    args = [nm]
    if spec.platform == "macos":
        args += ["-gU"]
    else:
        args += ["-g"]
    args.append(str(artifact))
    return run(*args, capture=True, env=env)


def verify_symbols(spec: TargetSpec, artifact: Path, env: dict[str, str]) -> None:
    symbols = symbol_output(spec, artifact, env)
    missing = [symbol for symbol in REQUIRED_EXPORTS if symbol not in symbols]
    if missing:
        raise SystemExit(f"Artifact {artifact.name} is missing required ABI symbols: {', '.join(missing)}")


def panic_strategy() -> str:
    return os.environ.get("TAFFY_UGUI_PANIC_STRATEGY", "unwind")


def write_manifest(spec: TargetSpec, staged_artifact: Path, description: str) -> Path:
    manifest = {
        "schema": 1,
        "package_version": package_version(),
        "native_version": package_version(),
        "abi": {"designation": "ABI-v1-RC", "version": ABI_RC_VERSION, "stage": ABI_RC_STAGE},
        "taffy_version": TAFFY_VERSION,
        "rust_target": spec.triple,
        "source_commit": source_revision(),
        "artifact": staged_artifact.name,
        "platform": spec.platform,
        "architecture": spec.architecture,
        "crate_type": spec.crate_type,
        "file_description": description,
        "sha256": sha256(staged_artifact),
        "panic_strategy": panic_strategy(),
    }
    path = spec.stage_dir / "manifest.json"
    path.write_text(json.dumps(manifest, indent=2, sort_keys=True) + "\n", encoding="utf-8")
    (spec.stage_dir / "SHA256SUMS").write_text(f"{manifest['sha256']}  {staged_artifact.name}\n", encoding="utf-8")
    return path


def build_target(name: str) -> Path:
    spec = TARGETS[name]
    require_abi_rc()
    ensure_host_supported(spec)
    ensure_rust_target(spec.triple)
    env = target_environment(spec)
    cargo_command("build", "--locked", "--release", "--target", spec.triple, env=env)
    built = cargo_artifact(spec)
    description = inspect_artifact(spec, built)
    verify_symbols(spec, built, env)
    spec.stage_dir.mkdir(parents=True, exist_ok=True)
    staged = spec.stage_dir / spec.artifact
    shutil.copy2(built, staged)
    write_manifest(spec, staged, description)
    print(f"Staged verified {name}: {staged.relative_to(ROOT)}", flush=True)
    return staged


def build_macos_universal() -> Path:
    if current_os() != "darwin":
        raise SystemExit("macOS universal assembly requires a macOS host with lipo.")
    arm = build_target("macos-arm64")
    intel = build_target("macos-x64")
    lipo = require("lipo")
    universal_dir = DIST_NATIVE / "macos" / "universal"
    universal_dir.mkdir(parents=True, exist_ok=True)
    output = universal_dir / "libtaffy_ugui.dylib"
    run(lipo, "-create", str(arm), str(intel), "-output", str(output))
    info = run(lipo, "-info", str(output), capture=True)
    if "arm64" not in info or "x86_64" not in info:
        raise SystemExit(f"Universal dylib is missing a required architecture: {info.strip()}")
    manifest = {
        "schema": 1,
        "package_version": package_version(),
        "native_version": package_version(),
        "abi": {"designation": "ABI-v1-RC", "version": ABI_RC_VERSION, "stage": ABI_RC_STAGE},
        "taffy_version": TAFFY_VERSION,
        "rust_targets": [TARGETS["macos-arm64"].triple, TARGETS["macos-x64"].triple],
        "source_commit": source_revision(),
        "artifact": output.name,
        "platform": "macos",
        "architecture": "universal",
        "sha256": sha256(output),
        "panic_strategy": panic_strategy(),
    }
    (universal_dir / "manifest.json").write_text(json.dumps(manifest, indent=2, sort_keys=True) + "\n", encoding="utf-8")
    (universal_dir / "SHA256SUMS").write_text(f"{manifest['sha256']}  {output.name}\n", encoding="utf-8")
    return output


def verify_staged_target(name: str) -> None:
    spec = TARGETS[name]
    artifact = spec.stage_dir / spec.artifact
    manifest_path = spec.stage_dir / "manifest.json"
    if not artifact.exists() or not manifest_path.exists():
        raise SystemExit(f"Staged target '{name}' is incomplete under {spec.stage_dir.relative_to(ROOT)}")
    manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
    if manifest.get("sha256") != sha256(artifact):
        raise SystemExit(f"Checksum mismatch for staged target '{name}'")
    if manifest.get("rust_target") != spec.triple or manifest.get("architecture") != spec.architecture:
        raise SystemExit(f"Manifest target metadata mismatch for '{name}'")
    abi = manifest.get("abi", {})
    if abi.get("version") != ABI_RC_VERSION or abi.get("stage") != ABI_RC_STAGE:
        raise SystemExit(f"Manifest for '{name}' is not ABI-v1-RC")


def list_targets() -> None:
    for name in REQUIRED_TARGETS:
        spec = TARGETS[name]
        hosts = ",".join(spec.host_os) if spec.host_os else "cross-host"
        print(f"{name:16} {spec.triple:28} {spec.platform}/{spec.architecture:10} host={hosts}")


def build_requested(target: str) -> None:
    if target == "host":
        host()
    elif target == "macos":
        build_macos_universal()
    elif target == "ios":
        build_target("ios-arm64")
    elif target == "all":
        host_os = current_os()
        compatible: list[str] = []
        for name in REQUIRED_TARGETS:
            spec = TARGETS[name]
            if not spec.host_os or host_os in spec.host_os:
                compatible.append(name)
        if not compatible:
            raise SystemExit("No Phase 4 targets are buildable on this host with the configured toolchains.")
        for name in compatible:
            build_target(name)
        if host_os == "darwin":
            build_macos_universal()
    else:
        build_target(target)


def verify_staged(names: Iterable[str]) -> None:
    for name in names:
        verify_staged_target(name)
    print("Verified staged Phase 4 manifests/checksums for: " + ", ".join(names))


def main() -> int:
    parser = argparse.ArgumentParser(description="Build and verify TaffyUGUI")
    sub = parser.add_subparsers(dest="command", required=True)
    sub.add_parser("preflight", help="Run static native phase architecture/ABI checks")
    sub.add_parser("quality", help="Run static preflight, fmt, Clippy, tests, and host release build")
    sub.add_parser("header", help="Generate include/taffy_ugui.h with cbindgen")
    sub.add_parser("verify-header", help="Regenerate the cbindgen header and require a clean diff")
    sub.add_parser("host-smoke", help="Build and run C/C++ smoke harnesses against the host shared library")
    sub.add_parser("verify-abi-rc", help="Run the complete Phase 3 native verification gate before locking ABI-v1-RC")
    sub.add_parser("list-targets", help="List Phase 4 target registry and staging paths")
    native = sub.add_parser("native", help="Build, verify, and stage a native target")
    native.add_argument("target", choices=["host", *TARGETS.keys(), "macos", "ios", "all"])
    verify_native = sub.add_parser("verify-native", help="Verify checksums/manifests of staged Phase 4 artifacts")
    verify_native.add_argument("targets", nargs="*", choices=list(TARGETS.keys()), default=list(REQUIRED_TARGETS))
    sub.add_parser("stage-unity", help="Stage verified native artifacts into UnityPackage/Plugins (Phase 5)")
    sub.add_parser("package", help="Assemble the final Unity package payload (later release phase)")
    args = parser.parse_args()
    if args.command == "preflight":
        preflight()
    elif args.command == "quality":
        quality()
    elif args.command == "header":
        header()
    elif args.command == "verify-header":
        verify_header_diff()
    elif args.command == "host-smoke":
        host_smoke()
    elif args.command == "verify-abi-rc":
        verify_abi_rc()
    elif args.command == "list-targets":
        list_targets()
    elif args.command == "native":
        build_requested(args.target)
    elif args.command == "verify-native":
        verify_staged(args.targets or REQUIRED_TARGETS)
    elif args.command in {"stage-unity", "package"}:
        raise SystemExit(f"'{args.command}' belongs to a later phase and is intentionally not implemented by Phase 4.")
    else:
        parser.error("Unsupported command")
    return 0


if __name__ == "__main__":
    sys.exit(main())
