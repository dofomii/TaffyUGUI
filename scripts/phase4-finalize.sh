#!/usr/bin/env bash
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

# The aggregation checkout must independently prove the exact source tree before accepting
# artifacts copied from the canonical Windows/macOS/Linux build hosts.
source "$ROOT/scripts/bootstrap-local-toolchain.sh"
python3 build/build.py prepare
python3 build/build.py verify-abi-rc
python3 build/build.py verify-phase4
python3 build/build.py phase4-status
