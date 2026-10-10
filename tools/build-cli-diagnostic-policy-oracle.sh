#!/usr/bin/env bash
set -euo pipefail
policy_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$policy_root"
test "$(git -C upstream/TypeScript rev-parse HEAD)" = fed0bf24149fb1ed36039212648bafdafc1ea10e
mkdir -p upstream/TypeScript/tsc/cmd/cli-diagnostic-policy-oracle artifacts/vscode-first-check/cli-diagnostic-policy
cp tools/cli-diagnostic-policy-oracle/main.go upstream/TypeScript/tsc/cmd/cli-diagnostic-policy-oracle/main.go
cd upstream/TypeScript/tsc
"$policy_root/tools/.cache/go/bin/go" build -o "$policy_root/artifacts/vscode-first-check/cli-diagnostic-policy/original" ./cmd/cli-diagnostic-policy-oracle
