#!/usr/bin/env bash
set -euo pipefail
checker_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$checker_root"
mkdir -p artifacts/checker-noemit-validation/reproduction tools/.cache
python3 - <<'PY'
import json,pathlib
r=pathlib.Path.cwd();paths={'internal/testutil/tsbaseline/authored_noemit_baseline_bridge.go':'baseline_bridge.go','internal/testutil/harnessutil/authored_noemit_harness_bridge.go':'harness_noemit_bridge.go','internal/testrunner/authored_noemit_compiler_bridge.go':'compiler_noemit_bridge.go','internal/testrunner/authored_noemit_diagnostics.go':'diagnostics.go','internal/testrunner/authored_noemit_runner_test.go':'runner_test.go','internal/testrunner/authored_decoded_inventory_test.go':'decoded_inventory_test.go','internal/testutil/harnessutil/authored_noemit_api_environment.go':'api_environment.go'}
replace={str(r/'upstream/TypeScript/tsc'/target):str(r/'tools/checker-noemit-oracle'/source) for target,source in paths.items()}
assert all(not pathlib.Path(p).exists() for p in replace)
(r/'artifacts/checker-noemit-validation/reproduction/overlay.json').write_text(json.dumps({'Replace':replace},indent=2)+'\n')
PY
cd upstream/TypeScript/tsc
"$checker_root/tools/.cache/go/bin/go" test \
  -overlay="$checker_root/artifacts/checker-noemit-validation/reproduction/overlay.json" \
  -c -o "$checker_root/tools/.cache/checker-noemit-original" ./internal/testrunner
