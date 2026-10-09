#!/usr/bin/env bash
set -euo pipefail
repo_root="$(cd "$(dirname "$0")/.." && pwd)"
cd "$repo_root"
./tools/bootstrap-upstream.sh
./tools/bootstrap-go.sh
python3 tools/binding-completion-oracle/prepare.py
tools/.cache/go/bin/go -C upstream/TypeScript/tsc build -o "$repo_root/tools/.cache/binding-completion-oracle" ./cmd/binding-completion-oracle
