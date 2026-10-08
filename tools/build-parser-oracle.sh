#!/usr/bin/env bash
set -euo pipefail
repo_root="$(cd "$(dirname "$0")/.." && pwd)"
"$repo_root/tools/bootstrap-go.sh"
"$repo_root/tools/bootstrap-upstream.sh"
mkdir -p "$repo_root/upstream/TypeScript/tsc/cmd/parser-oracle"
cp "$repo_root"/tools/parser-oracle/*.go "$repo_root/upstream/TypeScript/tsc/cmd/parser-oracle/"
"$repo_root/tools/.cache/go/bin/go" -C "$repo_root/upstream/TypeScript/tsc" build -o "$repo_root/tools/.cache/parser-oracle" ./cmd/parser-oracle
