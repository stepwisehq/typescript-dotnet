#!/usr/bin/env bash
set -euo pipefail
resolution_cli_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$resolution_cli_root"
resolution_cli_pin="$(git -C upstream/TypeScript rev-parse HEAD)"
test "$resolution_cli_pin" = fed0bf24149fb1ed36039212648bafdafc1ea10e
mkdir -p upstream/TypeScript/tsc/cmd/resolution-cli-oracle
cp tools/resolution-cli-oracle/main.go upstream/TypeScript/tsc/cmd/resolution-cli-oracle/main.go
cd upstream/TypeScript/tsc
"$resolution_cli_root/tools/.cache/go/bin/go" build -o "$resolution_cli_root/tools/.cache/resolution-cli-oracle" ./cmd/resolution-cli-oracle
