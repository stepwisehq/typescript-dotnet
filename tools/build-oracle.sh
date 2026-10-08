#!/usr/bin/env bash
set -euo pipefail
repo_root="$(cd "$(dirname "$0")/.." && pwd)"
"$repo_root/tools/bootstrap-go.sh"
"$repo_root/tools/bootstrap-upstream.sh"
upstream_module="$repo_root/upstream/TypeScript/tsc"
expected_sha=fed0bf24149fb1ed36039212648bafdafc1ea10e
[[ "$(git -C "$upstream_module" rev-parse HEAD)" == "$expected_sha" ]] || { echo "Upstream commit does not match the pinned scanner" >&2; exit 1; }
mkdir -p "$upstream_module/cmd/scanner-oracle"
cp "$repo_root/tools/oracle/main.go" "$upstream_module/cmd/scanner-oracle/main.go"
cd "$upstream_module"
"$repo_root/tools/.cache/go/bin/go" build -o "$repo_root/tools/.cache/scanner-oracle" ./cmd/scanner-oracle
