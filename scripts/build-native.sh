#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
TARGET="${1:-host}"

case "$TARGET" in
  host)
    cargo build --manifest-path "$ROOT/native/Cargo.toml" --release
    ;;
  macos-arm64)
    rustup target add aarch64-apple-darwin
    cargo build --manifest-path "$ROOT/native/Cargo.toml" --release --target aarch64-apple-darwin
    ;;
  ios-arm64)
    rustup target add aarch64-apple-ios
    cargo build --manifest-path "$ROOT/native/Cargo.toml" --release --target aarch64-apple-ios
    ;;
  webgl)
    rustup target add wasm32-unknown-emscripten
    cargo build --manifest-path "$ROOT/native/Cargo.toml" --release --target wasm32-unknown-emscripten
    ;;
  *)
    echo "Unknown target: $TARGET" >&2
    exit 2
    ;;
esac
