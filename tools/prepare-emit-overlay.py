#!/usr/bin/env python3
"""Write an additive Go overlay for original full-emission observations; do not build."""
import argparse, json, pathlib
ROOT = pathlib.Path(__file__).resolve().parents[1]
parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('destination', type=pathlib.Path)
args = parser.parse_args()
paths = {
 'internal/testrunner/authored_emit_runner_test.go': 'tools/emit-oracle/runner_test.go',
 'internal/testrunner/authored_emit_diagnostics.go': 'tools/checker-noemit-oracle/diagnostics.go',
 'internal/testutil/baseline/authored_emit_capture.go': 'tools/emit-oracle/baseline_capture.go',
}
replace = {str(ROOT / 'upstream/TypeScript/tsc' / target): str(ROOT / source) for target, source in paths.items()}
if any(pathlib.Path(target).exists() for target in replace):
 raise ValueError('Additive overlay target already exists')
args.destination.parent.mkdir(parents=True, exist_ok=True)
args.destination.write_text(json.dumps({'Replace': replace}, indent=2) + '\n')
