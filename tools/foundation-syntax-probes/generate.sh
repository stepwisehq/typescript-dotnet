#!/usr/bin/env bash
set -euo pipefail
repo_root="$(cd "$(dirname "$0")/../.." && pwd)"
cd "$repo_root"
./tools/bootstrap-upstream.sh
mkdir -p upstream/TypeScript/tsc/cmd/foundation-syntax-probes
cp tools/foundation-syntax-probes/*.go upstream/TypeScript/tsc/cmd/foundation-syntax-probes/
tools/.cache/go/bin/go -C upstream/TypeScript/tsc run ./cmd/foundation-syntax-probes > "${1:-validation/foundation-corpus/syntax-service-fixtures.json}"
