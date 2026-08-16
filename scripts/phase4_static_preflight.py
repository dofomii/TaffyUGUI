#!/usr/bin/env python3
"""Static contract checks for Phase 4 cross-platform native build support."""

from __future__ import annotations

import ast
import py_compile
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
BUILD = ROOT / "build" / "build.py"
WORKFLOW = ROOT / ".github" / "workflows" / "native-ci.yml"
PLAN = ROOT / "docs" / "NATIVE_LIBRARY_BUILD_PLAN.md"


def fail(message: str) -> None:
    raise SystemExit("Phase 4 static preflight failed: " + message)


for path in (BUILD, WORKFLOW, PLAN):
    if not path.exists():
        fail(f"missing {path.relative_to(ROOT)}")

py_compile.compile(str(BUILD), doraise=True)
ast.parse(BUILD.read_text(encoding="utf-8"))
text = BUILD.read_text(encoding="utf-8")
workflow = WORKFLOW.read_text(encoding="utf-8")

required_targets = {
    "windows-x64": "x86_64-pc-windows-msvc",
    "macos-arm64": "aarch64-apple-darwin",
    "macos-x64": "x86_64-apple-darwin",
    "android-arm64": "aarch64-linux-android",
    "ios-arm64": "aarch64-apple-ios",
    "webgl": "wasm32-unknown-emscripten",
}
for name, triple in required_targets.items():
    if name not in text or triple not in text:
        fail(f"target registry is missing {name}/{triple}")

for token in (
    "DIST_NATIVE",
    "manifest.json",
    "SHA256SUMS",
    "ABI-v1-RC",
    "21.3.6528147",
    "2.0.19",
    "verify_symbols",
    "inspect_artifact",
    "source_commit",
    "sha256",
):
    if token not in text:
        fail(f"build driver is missing required Phase 4 contract token: {token}")

for symbol in (
    "tu_get_abi_version",
    "tu_get_abi_stage",
    "tu_context_create",
    "tu_compute_layout",
):
    if symbol not in text:
        fail(f"required exported-symbol verification does not include {symbol}")

if "require_abi_rc()" not in text:
    fail("platform builds are not gated on ABI-v1-RC")

for lane in ("windows-x64", "macos-arm64", "macos-x64", "android-arm64", "ios-arm64", "webgl"):
    if lane not in workflow:
        fail(f"CI workflow has no Phase 4 lane for {lane}")

if "actions/upload-artifact" not in workflow:
    fail("CI does not upload staged Phase 4 artifacts")

print("Phase 4 static preflight passed: target registry, ABI-RC gate, deterministic staging, manifests/checksums, artifact/symbol verification, and CI lanes are present.")
print("Note: target toolchains must still execute on their compatible CI/SDK hosts before Phase 4 can be marked verified.")
