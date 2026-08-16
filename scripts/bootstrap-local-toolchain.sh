#!/usr/bin/env bash
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
TOOLCHAIN="$ROOT/.toolchain"
export CARGO_HOME="$TOOLCHAIN"
export RUSTUP_HOME="$TOOLCHAIN/rustup"
export PATH="$CARGO_HOME/bin:$PATH"
mkdir -p "$TOOLCHAIN"

if ! command -v rustup >/dev/null 2>&1; then
  tmp="$(mktemp -d)"
  trap 'rm -rf "$tmp"' EXIT
  echo "Installing rustup into $TOOLCHAIN"
  curl --proto '=https' --tlsv1.2 -fsSL https://sh.rustup.rs -o "$tmp/rustup.sh"
  sh "$tmp/rustup.sh" -y --no-modify-path --profile minimal --default-toolchain 1.97.1
fi

rustup toolchain install 1.97.1 --profile minimal --component rustfmt --component clippy
rustup override set 1.97.1

if ! command -v cbindgen >/dev/null 2>&1 || ! cbindgen --version 2>/dev/null | grep -q '0.29.2'; then
  cargo install cbindgen --version 0.29.2 --locked
fi

echo "Local toolchain ready in $TOOLCHAIN"
python3 "$ROOT/build/build.py" doctor
