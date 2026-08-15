#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
TARGET="${1:-host}"

echo "scripts/build-native.sh is a bootstrap compatibility wrapper; canonical builds use build/build.py." >&2

case "$TARGET" in
  host)
    exec python3 "$ROOT/build/build.py" native host
    ;;
  macos-arm64|ios-arm64|webgl)
    echo "Target '$TARGET' is intentionally disabled in the bootstrap wrapper." >&2
    echo "Production platform builds will be implemented in build/build.py during Phase 4 using Unity-matched toolchains." >&2
    exit 2
    ;;
  *)
    echo "Unknown target: $TARGET" >&2
    exit 2
    ;;
esac
