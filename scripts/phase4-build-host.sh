#!/usr/bin/env bash
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

# Keep Rust/cbindgen isolated inside the project just like the canonical bootstrap.
source "$ROOT/scripts/bootstrap-local-toolchain.sh"

case "$(uname -s)" in
  Darwin)
    rustup target add aarch64-apple-darwin x86_64-apple-darwin aarch64-apple-ios
    ;;
  Linux)
    rustup target add aarch64-linux-android wasm32-unknown-emscripten
    if [[ -z "${ANDROID_NDK_HOME:-${ANDROID_NDK_ROOT:-}}" ]]; then
      echo "ANDROID_NDK_HOME or ANDROID_NDK_ROOT must point to NDK 21.3.6528147 (r21d)." >&2
      exit 2
    fi
    if ! command -v emcc >/dev/null 2>&1 || ! command -v emar >/dev/null 2>&1 || ! command -v emnm >/dev/null 2>&1; then
      echo "Emscripten 2.0.19 (emcc/emar/emnm) must be active in PATH." >&2
      exit 2
    fi
    ;;
  *)
    echo "Use scripts/phase4-build-host.ps1 on Windows. Unsupported Unix host: $(uname -s)" >&2
    exit 2
    ;;
esac

python3 build/build.py prepare
python3 build/build.py verify-abi-rc
python3 build/build.py phase4-host
python3 build/build.py phase4-status
