#!/usr/bin/env python3
"""Canonical TaffyUGUI build entry point.

Phase 0 implements host quality/build and header generation. Cross-platform target
commands are added behind this same entry point during the native platform phase.
"""

from __future__ import annotations

import argparse
import difflib
import shutil
import subprocess
import sys
import tempfile
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
    run(sys.executable, str(ROOT / "scripts" / "phase3_static_preflight.py"))


def quality() -> None:
    preflight()
    cargo_command("fmt", "--all", "--", "--check")
    cargo_command("clippy", "--locked", "--all-targets", "--", "-D", "warnings")
    cargo_command("test", "--locked")
    cargo_command("build", "--locked", "--release")



def verify_header_diff() -> None:
    """Regenerate the cbindgen header to a temporary path and require a clean diff."""
    require("cbindgen")
    with tempfile.TemporaryDirectory() as directory:
        generated = Path(directory) / "taffy_ugui.h"
        run(
            "cbindgen",
            str(ROOT / "native"),
            "--config",
            str(CBINDGEN_CONFIG),
            "--output",
            str(generated),
        )
        expected = HEADER.read_text(encoding="utf-8").splitlines(keepends=True)
        actual = generated.read_text(encoding="utf-8").splitlines(keepends=True)
        if expected != actual:
            diff = "".join(
                difflib.unified_diff(
                    expected,
                    actual,
                    fromfile=str(HEADER.relative_to(ROOT)),
                    tofile="cbindgen-regenerated/taffy_ugui.h",
                )
            )
            raise SystemExit("cbindgen header drift detected:\n" + diff)


def host_smoke() -> None:
    """Build the host shared library and link/run the checked-in C and C++ smoke harnesses."""
    cargo_command("build", "--locked", "--release")
    clang = shutil.which("clang")
    clangxx = shutil.which("clang++")
    if clang is None or clangxx is None:
        raise SystemExit("Phase 3 host smoke requires clang and clang++ on PATH.")

    release = ROOT / "native" / "target" / "release"
    if sys.platform.startswith("linux"):
        library = release / "libtaffy_ugui.so"
        link_args = ["-L", str(release), "-ltaffy_ugui", "-Wl,-rpath," + str(release)]
    elif sys.platform == "darwin":
        library = release / "libtaffy_ugui.dylib"
        link_args = ["-L", str(release), "-ltaffy_ugui", "-Wl,-rpath," + str(release)]
    else:
        raise SystemExit("Phase 3 linked host smoke is currently implemented for Linux/macOS hosts; Windows remains covered by CI compile lanes until Phase 4 target work.")
    if not library.exists():
        raise SystemExit(f"Expected host shared library was not produced: {library}")

    with tempfile.TemporaryDirectory() as directory:
        directory = Path(directory)
        c_bin = directory / "tu-smoke-c"
        cpp_bin = directory / "tu-smoke-cpp"
        run(
            clang, "-std=c11", "-Wall", "-Wextra", "-Werror",
            "-I", str(ROOT / "include"), str(ROOT / "tests" / "native-smoke" / "smoke.c"),
            *link_args, "-lm", "-o", str(c_bin),
        )
        run(
            clangxx, "-std=c++17", "-Wall", "-Wextra", "-Werror",
            "-I", str(ROOT / "include"), str(ROOT / "tests" / "native-smoke" / "smoke.cpp"),
            *link_args, "-o", str(cpp_bin),
        )
        run(str(c_bin))
        run(str(cpp_bin))


def verify_abi_rc() -> None:
    """Canonical Phase 3 gate. ABI-v1-RC may be declared only after this command passes."""
    quality()
    verify_header_diff()
    host_smoke()


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
    sub.add_parser("verify-header", help="Regenerate the cbindgen header and require a clean diff")
    sub.add_parser("host-smoke", help="Build and run C/C++ smoke harnesses against the host shared library")
    sub.add_parser("verify-abi-rc", help="Run the complete Phase 3 native verification gate before locking ABI-v1-RC")

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
    elif args.command == "verify-header":
        verify_header_diff()
    elif args.command == "host-smoke":
        host_smoke()
    elif args.command == "verify-abi-rc":
        verify_abi_rc()
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
