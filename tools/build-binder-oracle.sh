#!/usr/bin/env bash
set -euo pipefail
repo_root="$(cd "$(dirname "$0")/.." && pwd)"
"$repo_root/tools/bootstrap-go.sh"
"$repo_root/tools/bootstrap-upstream.sh"
mkdir -p "$repo_root/upstream/TypeScript/tsc/cmd/binder-oracle"
python3 "$repo_root/tools/binder-oracle/prepare.py"
cp "$repo_root"/tools/binder-oracle/*.go "$repo_root/upstream/TypeScript/tsc/cmd/binder-oracle/"
"$repo_root/tools/.cache/go/bin/go" -C "$repo_root/upstream/TypeScript/tsc" build -o "$repo_root/tools/.cache/binder-oracle" ./cmd/binder-oracle
