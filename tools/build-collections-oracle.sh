#!/usr/bin/env bash
# Authored reproducible original-Go semantic observer binaries. The instrumented
# binary overlays metadata-only stores; the pinned source file stays untouched.
set -euo pipefail
collections_root=$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)
collections_go="$collections_root/tools/.cache/go/bin/go"
collections_overlay="$collections_root/tools/.cache/collections-observation"
cd "$collections_root"
python3 tools/collections-oracle/prepare.py
python3 tools/collections-oracle/instrument_instantiations.py --output "$collections_overlay"
python3 tools/collections-oracle/instrument_relations.py --output "$collections_overlay/relations" --base-overlay "$collections_overlay/overlay.json"
python3 tools/collections-oracle/instrument_flow.py --output "$collections_overlay/flow" --base-overlay "$collections_overlay/relations/overlay.json"
python3 tools/collections-oracle/instrument_global_caches.py --output "$collections_overlay/global" --base-overlay "$collections_overlay/flow/overlay.json"
cd "$collections_root/upstream/TypeScript/tsc"
"$collections_go" build -o "$collections_root/tools/.cache/collections-oracle-uninstrumented" ./cmd/collections-oracle
"$collections_go" build -overlay "$collections_overlay/global/overlay.json" -o "$collections_root/tools/.cache/collections-oracle" ./cmd/collections-oracle
