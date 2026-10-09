#!/usr/bin/env bash
set -euo pipefail
project_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$project_root"
python3 tools/projects-oracle/prepare.py
cd upstream/TypeScript/tsc
"$project_root/tools/.cache/go/bin/go" build -o "$project_root/tools/.cache/projects-oracle" ./cmd/projects-oracle
