

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
        unwind_shim = android_unwind_shim(ndk)
        link_flag = f"-C link-arg=-L{unwind_shim}"
        env["RUSTFLAGS"] = f"{env.get('RUSTFLAGS', '').strip()} {link_flag}".strip()
    elif spec.name == "webgl":
        emcc = require("emcc", f"Install the Unity-compatible Emscripten {WEBGL_EMSCRIPTEN_VERSION} toolchain.")
        require("emar", f"Install the Unity-compatible Emscripten {WEBGL_EMSCRIPTEN_VERSION} toolchain.")
        bundled_llvm_nm = Path(emcc).resolve().parent.parent / "bin" / "llvm-nm"
        if bundled_llvm_nm.is_file():
            env["TAFFYUGUI_WEBGL_NM"] = str(bundled_llvm_nm)
        else:
            env["TAFFYUGUI_WEBGL_NM"] = require(
                "llvm-nm",
                f"Install the Unity-compatible Emscripten {WEBGL_EMSCRIPTEN_VERSION} toolchain.",
            )
        version = run(emcc, "--version", capture=True)
        if WEBGL_EMSCRIPTEN_VERSION not in version:
            raise SystemExit(f"WebGL baseline requires Emscripten {WEBGL_EMSCRIPTEN_VERSION}; got {version.splitlines()[0] if version else 'unknown'}")
        env["CARGO_TARGET_WASM32_UNKNOWN_EMSCRIPTEN_LINKER"] = emcc
        env["CC_wasm32_unknown_emscripten"] = emcc
    return env


def webgl_nm(env: dict[str, str]) -> str:
    configured = env.get("TAFFYUGUI_WEBGL_NM")
    if configured and Path(configured).is_file():
        return configured
    return require("llvm-nm", f"WebGL verification requires Emscripten {WEBGL_EMSCRIPTEN_VERSION} llvm-nm.")


def cargo_artifact(spec: TargetSpec) -> Path:
    return CARGO_TARGET_DIR / spec.triple / "release" / spec.artifact


def windows_binary_description(artifact: Path, env: dict[str, str]) -> str:
    file_bin = executable("file")
    if file_bin:
        return run(file_bin, "-b", str(artifact), capture=True, env=env).strip()
    dumpbin = executable("dumpbin")
    if dumpbin:
        return run(dumpbin, "/headers", str(artifact), capture=True, env=env).strip()
    llvm_readobj = executable("llvm-readobj")
    if llvm_readobj:
        return run(llvm_readobj, "--file-headers", str(artifact), capture=True, env=env).strip()
    for tool in (executable("llvm-objdump"), executable("objdump")):
        if tool:
            return run(tool, "-f", str(artifact), capture=True, env=env).strip()
    raise SystemExit("Windows architecture verification requires file, dumpbin, llvm-readobj, llvm-objdump, or objdump.")


def inspect_artifact(spec: TargetSpec, artifact: Path, env: dict[str, str] | None = None) -> str:
    if not artifact.exists() or artifact.stat().st_size == 0:
        raise SystemExit(f"Expected artifact was not produced: {artifact}")
    env = env or base_env()
    if spec.name == "windows-x64":
        description = windows_binary_description(artifact, env)
        lower = description.lower()
        if not any(token in lower for token in ("x86-64", "x64", "amd64", "8664")):
            raise SystemExit(f"Windows artifact is not proven x86_64: {description[:500]}")
        if not any(token in lower for token in ("pe32+", "portable executable", "file format pei", "machine")):
            raise SystemExit(f"Windows artifact is not proven to be a PE image: {description[:500]}")
        return description

    file_bin = require("file", f"'{spec.name}' architecture verification requires the local file utility.")
    description = run(file_bin, "-b", str(artifact), capture=True, env=env).strip()
    lower = description.lower()
    expected_tokens = {
        "macos-arm64": ("mach-o", "arm64"),
        "macos-x64": ("mach-o", "x86_64"),
        "android-arm64": ("elf", "aarch64"),
        "ios-arm64": ("archive",),
        "webgl": ("archive",),
    }[spec.name]
    if not all(token in lower for token in expected_tokens):
        raise SystemExit(f"Artifact architecture/format mismatch for {spec.name}: {description}")
    return description


def target_architecture_evidence(
    spec: TargetSpec, artifact: Path, env: dict[str, str], description: str
) -> dict[str, object]:
    if spec.name == "ios-arm64":
        if current_os() != "darwin":
            raise SystemExit("iOS architecture evidence must be captured on the macOS build host.")
        lipo = require("lipo", "iOS ARM64 archive verification requires Xcode lipo.")
        info = run(lipo, "-info", str(artifact), capture=True, env=env).strip()
        lower = info.lower()
        if "arm64" not in lower or "x86_64" in lower:
            raise SystemExit(f"iOS archive is not a device-only ARM64 archive: {info}")
        return {"method": "lipo -info", "detail": info}

    if spec.name == "webgl":
        emar = require("emar", f"WebGL verification requires Emscripten {WEBGL_EMSCRIPTEN_VERSION} emar.")
        webgl_nm(env)
        members = [line.strip() for line in run(emar, "t", str(artifact), capture=True, env=env).splitlines() if line.strip()]
        if not members:
            raise SystemExit("WebGL static library is empty.")
        first_member = members[0]
        file_bin = require("file", "WebGL object-format verification requires the local file utility.")
        with tempfile.TemporaryDirectory(prefix="taffyugui-webgl-archive-") as directory:
            extraction = Path(directory)
            run(emar, "x", str(artifact), first_member, env=env, cwd=extraction)
            extracted = extraction / first_member
            if not extracted.exists():
                candidates = list(extraction.rglob(Path(first_member).name))
                if not candidates:
                    raise SystemExit(f"Unable to extract WebGL archive member: {first_member}")
                extracted = candidates[0]
            member_description = run(file_bin, "-b", str(extracted), capture=True, env=env).strip()
        lower = member_description.lower()
        if not any(token in lower for token in ("webassembly", "wasm", "llvm ir bitcode", "llvm bitcode")):
            raise SystemExit(
                "WebGL archive member is not proven to be a Wasm/LLVM-bitcode object: "
                f"{member_description}"
            )
        return {
            "method": "emar + file",
            "member_count": len(members),
            "sample_member": first_member,
            "sample_description": member_description,
        }

    return {"method": "binary format inspection", "detail": description}


def symbol_text(spec: TargetSpec, artifact: Path, env: dict[str, str]) -> str:
    if spec.name == "windows-x64":
        dumpbin = executable("dumpbin")
        if dumpbin:
            return run(dumpbin, "/exports", str(artifact), capture=True, env=env)
        llvm_nm = executable("llvm-nm")
        if llvm_nm:
            return run(llvm_nm, "--defined-only", str(artifact), capture=True, env=env)
        llvm_objdump = executable("llvm-objdump")
        if llvm_objdump:
            return run(llvm_objdump, "-p", str(artifact), capture=True, env=env)
        objdump = executable("objdump")
        if objdump:
            return run(objdump, "-p", str(artifact), capture=True, env=env)
        raise SystemExit("Windows export verification requires dumpbin, llvm-nm, llvm-objdump, or objdump.")
    if spec.name == "android-arm64":
        ndk = find_android_ndk()
        candidates = sorted((ndk / "toolchains/llvm/prebuilt").glob("*/bin/llvm-nm*"))
        if candidates:
            return run(str(candidates[0]), "-D", "--defined-only", str(artifact), capture=True, env=env)
        nm = require("nm")
        return run(nm, "-D", "--defined-only", str(artifact), capture=True, env=env)
    if spec.name == "webgl":
        return run(webgl_nm(env), "-g", str(artifact), capture=True, env=env)
    nm = require("nm")
    args = [nm]
    if spec.platform_name == "macos":
        args += ["-gU"]
    else:
        args += ["-g"]
    args.append(str(artifact))
    return run(*args, capture=True, env=env)


def verify_symbols(spec: TargetSpec, artifact: Path, env: dict[str, str]) -> tuple[str, ...]:
    output = symbol_text(spec, artifact, env)
    expected = header_export_contract()
    missing = [name for name in expected if name not in output]
    if missing:
        raise SystemExit(f"{artifact.name} is missing required ABI exports: {', '.join(missing)}")
    return expected


def package_version() -> str:
    return str(json.loads(PACKAGE_JSON.read_text(encoding="utf-8"))["version"])


def source_revision() -> str:
    head, dirty, _ = git_state()
    return head + ("+working-tree" if dirty else "")


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def export_fingerprint(exports: tuple[str, ...]) -> str:
    payload = "\n".join(exports).encode("utf-8")
    return hashlib.sha256(payload).hexdigest()
