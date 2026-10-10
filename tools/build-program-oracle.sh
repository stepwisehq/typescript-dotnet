#!/usr/bin/env bash
set -euo pipefail
program_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$program_root"
test "$(git -C upstream/TypeScript rev-parse HEAD)" = fed0bf24149fb1ed36039212648bafdafc1ea10e
mkdir -p upstream/TypeScript/tsc/cmd/program-oracle
cp tools/program-oracle/*.go upstream/TypeScript/tsc/cmd/program-oracle/
cd upstream/TypeScript/tsc
"$program_root/tools/.cache/go/bin/go" build -o "$program_root/tools/.cache/program-oracle" ./cmd/program-oracle
