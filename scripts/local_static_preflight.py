#!/usr/bin/env python3
"""Provider-independent static integrity checks for the Phase 3 -> Phase 4 boundary."""
from __future__ import annotations

import ast
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
errors: list[str] = []


def require(condition: bool, message: str) -> None:
    if not condition:
        errors.append(message)


def text(rel: str) -> str:
    path = ROOT / rel
    if not path.exists():
        errors.append(f"missing required file: {rel}")
        return ""
    return path.read_text(encoding="utf-8")


version = text("native/src/version.rs")
manifest = text("native/Cargo.toml")
lockfile = text("Cargo.lock")
toolchain = text("rust-toolchain.toml")
ffi = text("native/src/ffi.rs")
lib = text("native/src/lib.rs")
tests = text("native/tests/native_verification.rs")
header = text("include/taffy_ugui.h")
managed = text("UnityPackage/Runtime/TaffyNative.cs")
group = text("UnityPackage/Runtime/TaffyLayoutGroup.cs")
build = text("build/build.py") + "\n" + "\n".join(text(str(path.relative_to(ROOT))) for path in sorted((ROOT / "build" / "driver_parts").glob("[0-9][0-9].py")))
cbindgen = text("cbindgen.toml")

require("TU_ABI_VERSION: u32 = 1" in version, "ABI-v1 version is not locked to 1")
require("TU_ABI_STAGE: u32 = 1" in version, "ABI-v1 stage is not locked to RC (1)")
require('taffy = { version = "=0.13.0"' in manifest, "Taffy dependency is not exactly pinned to 0.13.0")
require('name = "taffy"' in lockfile and 'version = "0.13.0"' in lockfile, "workspace Cargo.lock does not resolve Taffy 0.13.0")
require(not (ROOT / "native" / "Cargo.lock").exists(), "nested native/Cargo.lock must not shadow the workspace lockfile")
require('channel = "1.97.1"' in toolchain, "local development Rust toolchain is not pinned to 1.97.1")
require('cpp_compat = true' in cbindgen and 'style = "both"' in cbindgen, "cbindgen C/C++ compatibility settings are missing")
require("ABI-v1-RC" in lib and "ABI-v1-RC" in ffi, "native module docs still describe a pre-RC candidate")

required_exports = [
    "tu_calc_create", "tu_calc_remove", "tu_compute_layout", "tu_context_clear",
    "tu_context_create", "tu_context_destroy", "tu_copy_build_version", "tu_copy_last_error",
    "tu_get_abi_stage", "tu_get_abi_version", "tu_get_build_version_length", "tu_get_capabilities",
    "tu_get_grid_gutters", "tu_get_grid_info", "tu_get_grid_items", "tu_get_grid_track_sizes",
    "tu_get_last_error_length", "tu_get_layout", "tu_get_layouts_bulk", "tu_get_taffy_version_packed",
    "tu_node_create", "tu_node_is_dirty", "tu_node_mark_dirty", "tu_node_remove",
    "tu_node_set_children", "tu_node_set_grid_template", "tu_node_set_measurement", "tu_node_set_style",
    "tu_nodes_set_children", "tu_nodes_set_measurements", "tu_nodes_set_styles",
]
for symbol in required_exports:
    require(re.search(rf'extern\s+"C"\s+fn\s+{re.escape(symbol)}\b', ffi) is not None, f"native FFI export missing: {symbol}")
    require(re.search(rf'\b{re.escape(symbol)}\s*\(', header) is not None, f"public header declaration missing: {symbol}")

for index in range(1, 9):
    require(f"fn p3_{index}_" in tests, f"Phase 3 verification test P3.{index} is missing")
require("500_u32" in tests, "Phase 3 repeated lifecycle stress loop is missing")
require("offset_of!(TuValue" in tests, "ABI field-offset verification is missing")

# Managed/native boundary must use the RC symbols, not the early bootstrap names.
for symbol in ("tu_get_abi_version", "tu_get_abi_stage", "tu_context_create", "tu_node_create", "tu_node_set_children", "tu_compute_layout", "tu_get_layout"):
    require(symbol in managed, f"managed P/Invoke missing: {symbol}")
require("taffy_ugui_api_version" not in managed and "taffy_ugui_create_context" not in managed, "obsolete bootstrap P/Invoke symbols remain in managed wrapper")
require("Marshal.SizeOf<Style>() != 632" in managed and "Marshal.SizeOf<Layout>() != 48" in managed, "managed ABI size guard is missing")
require("ToNativeJustify" in group, "Unity LayoutGroup native justify mapping is missing")
require("tu_context_clear" in group, "Unity LayoutGroup is not using the current persistent context ABI")

# Phase 4 build contract must exist locally and must not depend on a provider.
for token in (
    "windows-x64", "x86_64-pc-windows-msvc", "macos-arm64", "aarch64-apple-darwin",
    "android-arm64", "aarch64-linux-android", "ios-arm64", "aarch64-apple-ios",
    "webgl", "wasm32-unknown-emscripten", "21.3.6528147", "2.0.19",
    "require_abi_rc", "require_phase3_evidence", "verify_symbols", "PUBLIC_ABI_EXPORTS",
    "PHASE4_HOST_TARGETS", "phase4_host", "verify_phase4", "phase4-index.json",
    "source_tree", "SHA256SUMS", "built_locally", "public_exports_sha256",
    "architecture_evidence", "validate_manifest_architecture_evidence", "phase4_driver_selftest",
    "emar", "llvm-nm", "lipo -info",
):
    require(token in build, f"local Phase 4 build contract missing token: {token}")
require("api.github.com" not in build.lower() and "actions/" not in build.lower() and "github.run" not in build.lower(), "local build driver contains an executable GitHub/Actions dependency")

# Lightweight source-damage guard; the real compiler remains mandatory.
import subprocess
result = subprocess.run([sys.executable, str(ROOT / "scripts" / "rust_delimiter_sanity.py")], cwd=ROOT)
require(result.returncode == 0, "Rust delimiter sanity check failed")

result = subprocess.run([sys.executable, str(ROOT / "scripts" / "csharp_sanity.py")], cwd=ROOT)
require(result.returncode == 0, "C# source sanity check failed")

# Syntax-check Python build tooling itself.
try:
    ast.parse(build)
except SyntaxError as exc:
    errors.append(f"build/build.py syntax error: {exc}")

# Phase 4 host wrappers must exist and invoke only the local build driver.
for rel in ("scripts/phase4-build-host.sh", "scripts/phase4-build-host.ps1", "scripts/phase4-finalize.sh"):
    wrapper = text(rel)
    require("build/build.py" in wrapper, f"Phase 4 local wrapper does not invoke the build driver: {rel}")
    require("github" not in wrapper.lower(), f"Phase 4 local wrapper must not depend on GitHub: {rel}")

# No provider workflow is part of the local canonical project.
workflow_dir = ROOT / ".github" / "workflows"
require(not workflow_dir.exists() or not any(workflow_dir.glob("*.y*ml")), "GitHub Actions workflow exists in the local canonical project")

if errors:
    print("LOCAL STATIC PREFLIGHT: FAILED", file=sys.stderr)
    for error in errors:
        print(f" - {error}", file=sys.stderr)
    raise SystemExit(1)

print("LOCAL STATIC PREFLIGHT: PASS")
print("ABI-v1-RC lock, native exports, Phase 3 verification inventory, managed P/Invoke, and Phase 4 local build contract are consistent.")
