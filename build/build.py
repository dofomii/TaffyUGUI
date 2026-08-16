#!/usr/bin/env python3
"""Canonical TaffyUGUI build entry point.

Phase 0 implements host quality/build and header generation. Cross-platform target
commands are added behind this same entry point during the native platform phase.
"""

from __future__ import annotations

import argparse
import shutil
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
MANIFEST = ROOT / "native" / "Cargo.toml"
HEADER = ROOT / "include" / "taffy_ugui.h"
CBINDGEN_CONFIG = ROOT / "cbindgen.toml"


def run(*args: str) -> None:
    print("+", " ".join(args), flush=True)
    subprocess.run(args, cwd=ROOT, check=True)


def require(executable: str) -> None:
    if shutil.which(executable) is None:
        raise SystemExit(
            f"Required tool '{executable}' was not found on PATH. "
            "See docs/PROJECT_DECISIONS.md and docs/NATIVE_LIBRARY_BUILD_PLAN.md."
        )


def cargo_command(command: str, *args: str) -> None:
    require("cargo")
    run("cargo", command, "--manifest-path", str(MANIFEST), *args)


def preflight() -> None:
    run(sys.executable, str(ROOT / "scripts" / "phase1_static_preflight.py"))
    run(sys.executable, str(ROOT / "scripts" / "phase2_static_preflight.py"))


def quality() -> None:
    preflight()
    cargo_command("fmt", "--all", "--", "--check")
    cargo_command("clippy", "--locked", "--all-targets", "--", "-D", "warnings")
    cargo_command("test", "--locked")
    cargo_command("build", "--locked", "--release")


def host() -> None:
    cargo_command("build", "--locked", "--release")


def header() -> None:
    require("cbindgen")
    HEADER.parent.mkdir(parents=True, exist_ok=True)
    run(
        "cbindgen",
        str(ROOT / "native"),
        "--config",
        str(CBINDGEN_CONFIG),
        "--output",
        str(HEADER),
    )


def not_implemented(target: str) -> None:
    raise SystemExit(
        f"Target '{target}' is reserved by the canonical build interface but is "
        "implemented in Phase 4 after the ABI candidate is verified."
    )


def main() -> int:
    parser = argparse.ArgumentParser(description="Build and verify TaffyUGUI")
    sub = parser.add_subparsers(dest="command", required=True)

    sub.add_parser("preflight", help="Run static Phase 1/2 architecture and ABI checks")
    sub.add_parser("quality", help="Run static preflight, fmt, Clippy, tests, and host release build")
    sub.add_parser("header", help="Generate include/taffy_ugui.h with cbindgen")

    native = sub.add_parser("native", help="Build a native target")
    native.add_argument(
        "target",
        choices=[
            "host",
            "windows-x64",
            "macos",
            "android-arm64",
            "ios",
            "webgl",
            "all",
        ],
    )

    sub.add_parser("stage-unity", help="Stage verified native artifacts into UnityPackage/Plugins")
    sub.add_parser("package", help="Assemble the final Unity package payload")

    args = parser.parse_args()

    if args.command == "preflight":
        preflight()
    elif args.command == "quality":
        quality()
    elif args.command == "header":
        header()
    elif args.command == "native" and args.target == "host":
        host()
    elif args.command == "native":
        not_implemented(args.target)
    elif args.command in {"stage-unity", "package"}:
        not_implemented(args.command)
    else:
        parser.error("Unsupported command")

    return 0


if __name__ == "__main__":
    sys.exit(main())
