#!/usr/bin/env bash
set -euo pipefail
resolution_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$resolution_root"
python3 tools/resolution-oracle/prepare.py
cd upstream/TypeScript/tsc
"$resolution_root/tools/.cache/go/bin/go" build -overlay "$resolution_root/artifacts/resolution-validation/original-graph-overlay/overlay.json" -o "$resolution_root/tools/.cache/resolution-oracle" ./cmd/resolution-oracle
cd "$resolution_root"
python3 tools/resolution-oracle/prepare.py --pristine
cd upstream/TypeScript/tsc
"$resolution_root/tools/.cache/go/bin/go" build -o "$resolution_root/tools/.cache/resolution-pristine-oracle" ./cmd/resolution-pristine-oracle
