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
ABI_RC_STAGE = 2
TAFFY_VERSION = "0.13.0"
ANDROID_NDK_REVISION = "21.3.6528147"  # Unity 2021.3 NDK r21d baseline
ANDROID_API = 21
WEBGL_EMSCRIPTEN_VERSION = "2.0.19"     # Unity 2021.3 Emscripten baseline

PUBLIC_ABI_EXPORTS = (
    "tu_calc_create",
    "tu_calc_remove",
    "tu_compute_layout",
    "tu_context_clear",
    "tu_context_create",
    "tu_context_destroy",
    "tu_copy_build_version",
    "tu_copy_last_error",
    "tu_get_abi_stage",
    "tu_get_abi_version",
    "tu_get_build_version_length",
    "tu_get_capabilities",
    "tu_get_grid_gutters",
    "tu_get_grid_info",
    "tu_get_grid_items",
    "tu_get_grid_track_sizes",
    "tu_get_last_error_length",
    "tu_get_layout",
    "tu_get_layouts_bulk",
    "tu_get_taffy_version_packed",
    "tu_node_create",
    "tu_node_is_dirty",
    "tu_node_mark_dirty",
    "tu_node_remove",
    "tu_node_set_children",
    "tu_node_set_grid_template",
    "tu_node_set_measurement",
    "tu_node_set_style",
    "tu_nodes_set_children",
    "tu_nodes_set_measurements",
    "tu_nodes_set_styles",
)

# Active v1 native release scope. Other target definitions remain available for
# future compatibility branches, but they are not release-gating targets.
PHASE4_REQUIRED_TARGETS = (
    "android-arm64",
)
PHASE3_EVIDENCE = ROOT / ".build" / "evidence" / "phase3-local.json"

PHASE4_HOST_TARGETS = {
    "linux": ("android-arm64",),
}


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
    "android-arm64": TargetSpec("android-arm64", "aarch64-linux-android", "android", "arm64-v8a", "libtaffy_ugui.so", "cdylib", ("linux",)),
    "ios-arm64": TargetSpec("ios-arm64", "aarch64-apple-ios", "ios", "arm64", "libtaffy_ugui.a", "staticlib", ("darwin",)),
    "webgl": TargetSpec("webgl", "wasm32-unknown-emscripten", "webgl", "wasm32", "libtaffy_ugui.a", "staticlib", ("linux",)),
}


def run(*args: str, env: dict[str, str] | None = None, capture: bool = False, cwd: Path | None = None) -> str:
    command = [str(a) for a in args]
    print("+", " ".join(command), flush=True)
    completed = subprocess.run(
        command,
        cwd=cwd or ROOT,
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
    local_toolchain = ROOT / ".toolchain"
    local_bin = local_toolchain / "bin"
    if local_bin.exists():
        env["PATH"] = str(local_bin) + os.pathsep + env.get("PATH", "")
    # Rustup proxy binaries resolve the active installation through CARGO_HOME and
    # RUSTUP_HOME. Set both whenever the project-local bootstrap owns rustup;
    # merely prepending .toolchain/bin is otherwise insufficient.
    if (local_bin / ("rustup.exe" if os.name == "nt" else "rustup")).exists():
        env["CARGO_HOME"] = str(local_toolchain)
        env["RUSTUP_HOME"] = str(local_toolchain / "rustup")
    env["CARGO_TARGET_DIR"] = str(CARGO_TARGET_DIR)
    return env


def tool_version(name: str, *args: str) -> str:
    binary = require(name)
    command_args = args or ("--version",)
    output = run(binary, *command_args, capture=True, env=base_env()).strip()
    return output.splitlines()[0] if output else "unknown"


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
        raise SystemExit(f"Phase 4 is locked: expected final ABI v1 {expected[0]}/{expected[1]}, found {actual[0]}/{actual[1]}.")


def header_export_contract() -> tuple[str, ...]:
    text = HEADER.read_text(encoding="utf-8")
    exports = tuple(sorted(set(re.findall(r"\b(tu_[A-Za-z0-9_]+)\s*\(", text))))
    expected = tuple(sorted(PUBLIC_ABI_EXPORTS))
    if exports != expected:
        missing = sorted(set(expected) - set(exports))
        extra = sorted(set(exports) - set(expected))
        raise SystemExit(
            "Public header export inventory does not match the ABI contract. "
            f"Missing={missing or 'none'} Extra={extra or 'none'}"
        )
    return exports


def git_state() -> tuple[str, bool, str]:
    git = executable("git")
    if not git or not (ROOT / ".git").exists():
        return ("local-unversioned", True, "git metadata unavailable")
    head = run(git, "rev-parse", "HEAD", capture=True).strip()
    status = run(git, "status", "--porcelain=v1", "--untracked-files=normal", capture=True)
    return (head, bool(status.strip()), status)


SOURCE_SNAPSHOT_FILES = (
    ROOT / "Cargo.toml",
    ROOT / "Cargo.lock",
    ROOT / "rust-toolchain.toml",
    ROOT / "cbindgen.toml",
    ROOT / "native" / "Cargo.toml",
    ROOT / "include" / "taffy_ugui.h",
    ROOT / "UnityPackage" / "package.json",
)
SOURCE_SNAPSHOT_DIRS = (
    ROOT / "native" / "src",
    ROOT / "native" / "tests",
    ROOT / "build",
    ROOT / "UnityPackage" / "Runtime",
)


def source_snapshot_files() -> tuple[Path, ...]:
    files: set[Path] = set()
    for path in SOURCE_SNAPSHOT_FILES:
        if not path.is_file():
            raise SystemExit(f"Required source snapshot input is missing: {path.relative_to(ROOT)}")
        files.add(path)
    for directory in SOURCE_SNAPSHOT_DIRS:
        if not directory.is_dir():
            raise SystemExit(f"Required source snapshot directory is missing: {directory.relative_to(ROOT)}")
        for path in directory.rglob("*"):
            if not path.is_file():
                continue
            if "__pycache__" in path.parts or path.suffix in {".pyc", ".pyo"}:
                continue
            files.add(path)
    return tuple(sorted(files, key=lambda path: path.relative_to(ROOT).as_posix()))


def source_tree_sha() -> str:
    digest = hashlib.sha256()
    for path in source_snapshot_files():
        relative = path.relative_to(ROOT).as_posix().encode("utf-8")
        payload = path.read_bytes()
        digest.update(len(relative).to_bytes(4, "big"))
        digest.update(relative)
        digest.update(len(payload).to_bytes(8, "big"))
        digest.update(payload)
    return "sha256:" + digest.hexdigest()


def write_phase3_evidence() -> None:
    head, dirty, _ = git_state()
    PHASE3_EVIDENCE.parent.mkdir(parents=True, exist_ok=True)
    evidence = {
        "schema": 2,
        "source_revision": head + ("+working-tree" if dirty else ""),
        "source_tree": source_tree_sha(),
        "source_snapshot_kind": "content-addressed-project-inputs-v1",
        "git_dirty": dirty,
        "abi": {"designation": "ABI-v1", "version": ABI_RC_VERSION, "stage": ABI_RC_STAGE},
        "taffy_version": TAFFY_VERSION,
        "rustc": tool_version("rustc", "--version"),
        "cargo": tool_version("cargo", "--version"),
        "cbindgen": tool_version("cbindgen", "--version"),
        "host": platform.platform(),
        "public_exports": list(header_export_contract()),
    }
    PHASE3_EVIDENCE.write_text(json.dumps(evidence, indent=2, sort_keys=True) + "\n", encoding="utf-8")
    print(f"Recorded local Phase 3 evidence: {PHASE3_EVIDENCE.relative_to(ROOT)}")
