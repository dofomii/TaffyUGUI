#!/usr/bin/env python3
"""Static Phase 3 verification-harness checks.

This proves that the native verification inventory and host smoke harnesses are present and that
its C/C++ ABI contract compiles. It does not replace running the Rust tests, cbindgen regeneration,
or the compiled shared-library smoke test.
"""
from __future__ import annotations

import re
import shutil
import subprocess
import sys
import tempfile
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
TESTS = ROOT / "native" / "tests" / "native_verification.rs"
C_SMOKE = ROOT / "tests" / "native-smoke" / "smoke.c"
CPP_SMOKE = ROOT / "tests" / "native-smoke" / "smoke.cpp"
HEADER = ROOT / "include" / "taffy_ugui.h"


def require(text: str, needle: str, description: str, errors: list[str]) -> None:
    if needle not in text:
        errors.append(f"missing {description}: {needle}")


def compile_source(compiler: str, standard: str, source: Path, errors: list[str]) -> None:
    with tempfile.TemporaryDirectory() as directory:
        output = Path(directory) / (source.stem + ".o")
        result = subprocess.run(
            [compiler, standard, "-Wall", "-Wextra", "-Werror", "-I", str(HEADER.parent), "-c", str(source), "-o", str(output)],
            cwd=ROOT,
            capture_output=True,
            text=True,
        )
        if result.returncode:
            errors.append(f"{source.name} compile failed: {result.stderr.strip()}")


def main() -> int:
    errors: list[str] = []
    if not TESTS.exists():
        errors.append("native/tests/native_verification.rs missing")
        tests = ""
    else:
        tests = TESTS.read_text(encoding="utf-8")
    build = (ROOT / "build" / "build.py").read_text(encoding="utf-8")
    version = (ROOT / "native" / "src" / "version.rs").read_text(encoding="utf-8")
    tracker = (ROOT / "docs" / "TASK_TRACKER.md").read_text(encoding="utf-8")

    for task in range(1, 9):
        require(tests, f"fn p3_{task}_", f"P3.{task} deterministic Rust verification", errors)
    require(tests, "p3_2_flex_golden_geometry", "Flex golden suite", errors)
    require(tests, "p3_3_block_flowroot_float_golden_geometry", "Block/FlowRoot/Float golden suite", errors)
    require(tests, "p3_4_grid_named_area_and_placement_golden_geometry", "Grid/named-area golden suite", errors)
    require(tests, "p3_5_calc_and_measurement_golden_geometry", "Calc/measurement golden suite", errors)
    require(tests, "offset_of!(TuValue", "ABI field-offset assertions", errors)
    require(tests, "500_u32", "lifecycle/topology stress repetitions", errors)

    for smoke in (C_SMOKE, CPP_SMOKE):
        if not smoke.exists():
            errors.append(f"missing host smoke harness: {smoke.relative_to(ROOT)}")
    clang = shutil.which("clang")
    clangxx = shutil.which("clang++")
    if clang and C_SMOKE.exists():
        compile_source(clang, "-std=c11", C_SMOKE, errors)
    if clangxx and CPP_SMOKE.exists():
        compile_source(clangxx, "-std=c++17", CPP_SMOKE, errors)

    for needle, description in [
        ("def verify_header_diff()", "cbindgen regeneration/diff verification"),
        ("def host_smoke()", "compiled host shared-library smoke runner"),
        ('sub.add_parser("verify-abi-rc"', "canonical Phase 3 verification command"),
        ("phase3_static_preflight.py", "Phase 3 preflight wiring"),
    ]:
        require(build, needle, description, errors)

    # The complete candidate runtime gate has passed; Phase 3 now requires the ABI-v1-RC lock.
    require(version, "TU_ABI_VERSION: u32 = 1", "ABI-v1-RC version lock", errors)
    require(version, "TU_ABI_STAGE: u32 = 1", "ABI-v1-RC stage lock", errors)
    require(tracker, "Phase 3 — Native Verification and ABI Release-Candidate Lock", "Phase 3 tracker section", errors)

    if errors:
        print("Phase 3 static preflight FAILED:", file=sys.stderr)
        for error in errors:
            print(f"- {error}", file=sys.stderr)
        return 1
    print("Phase 3 static preflight passed: golden/safety/ABI/stress tests and C/C++ host smoke harnesses are present.")
    print("Note: Rust tests, cbindgen diff, release build, and linked host shared-library smoke remain mandatory before ABI-v1-RC lock.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
