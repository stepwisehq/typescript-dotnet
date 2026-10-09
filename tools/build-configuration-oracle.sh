#!/usr/bin/env bash
set -euo pipefail
configuration_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$configuration_root"
python3 tools/configuration-oracle/prepare.py
cd upstream/TypeScript/tsc
"$configuration_root/tools/.cache/go/bin/go" build -o "$configuration_root/tools/.cache/configuration-oracle" ./cmd/configuration-oracle
