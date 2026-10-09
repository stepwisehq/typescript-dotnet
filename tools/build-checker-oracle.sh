#!/usr/bin/env bash
set -euo pipefail
repo_root="$(cd "$(dirname "$0")/.." && pwd)"
"$repo_root/tools/bootstrap-go.sh"
"$repo_root/tools/bootstrap-upstream.sh"
python3 "$repo_root/tools/checker-oracle/prepare.py"
cp "$repo_root"/tools/checker-oracle/main.go "$repo_root/upstream/TypeScript/tsc/cmd/checker-oracle/"
cp "$repo_root"/tools/checker-oracle/object_graph.go "$repo_root/upstream/TypeScript/tsc/cmd/checker-oracle/"
cp "$repo_root"/tools/checker-oracle/data_graph.go "$repo_root/upstream/TypeScript/tsc/cmd/checker-oracle/"
cp "$repo_root"/tools/checker-oracle/callable_graph.go "$repo_root/upstream/TypeScript/tsc/cmd/checker-oracle/"
"$repo_root/tools/.cache/go/bin/go" -C "$repo_root/upstream/TypeScript/tsc" build -o "$repo_root/tools/.cache/checker-oracle" ./cmd/checker-oracle
