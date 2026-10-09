#!/usr/bin/env bash
set -euo pipefail
repo_root="$(cd "$(dirname "$0")/.." && pwd)"
cd "$repo_root"
out=artifacts/callables-validation
mkdir -p "$out"
# Exactly one complete retained cascade: all current xUnit, foundation/numeric,
# binding and Legacy semantic checks. It freshly publishes Checker.Validation.
bash tools/verify-binding-completion.sh
native="$repo_root/artifacts/data-types-validation/native/Checker.Validation"
python3 tools/run-callables-gate.py --capture-native "$out/native-before-callables.json"
# The retained cascade built the original-Go checker oracle. Reproduce callable
# expected outputs, without regenerating or accepting stored expected fixtures.
python3 - <<'PY'
import hashlib,json,pathlib,subprocess
corpus=pathlib.Path('validation/callables-corpus');out=pathlib.Path('artifacts/callables-validation')
manifest=json.loads((corpus/'manifest.json').read_text());total=0
for group in manifest['groups']:
    name=group['group'];requests=corpus/(name+'-requests.jsonl');expected=corpus/(name+'-expected.jsonl')
    for path,key in [(requests,'requestsSha256'),(expected,'expectedSha256')]:
        if hashlib.sha256(path.read_bytes()).hexdigest()!=group[key]:raise RuntimeError('Callable manifest digest mismatch: '+str(path))
    if len(requests.read_text().splitlines())!=group['cases'] or len(expected.read_text().splitlines())!=group['cases']:raise RuntimeError('Callable manifest row count mismatch')
    current=out/(name+'-original-go-current.jsonl')
    with requests.open('rb') as source,current.open('wb') as output:
        subprocess.run(['tools/.cache/checker-oracle'],stdin=source,stdout=output,check=True)
    if current.read_bytes()!=expected.read_bytes():raise RuntimeError('Stored callable expectation differs from original Go: '+name)
    total+=group['cases']
if total!=manifest['cases']:raise RuntimeError('Callable manifest total mismatch')
PY
# Newly applicable original arithmetic assertions retain their original Node
# comparisons (including only the original exponentiation one-ULP allowance).
"$repo_root/tools/.cache/go/bin/go" -C upstream/TypeScript/tsc test ./internal/jsnum -run '^(TestToInt32|TestBitwiseNOT|TestBitwiseAND|TestBitwiseOR|TestBitwiseXOR|TestSignedRightShift|TestUnsignedRightShift|TestLeftShift|TestRemainder|TestExponentiate)$' -count=1 > "$out/original-go-arithmetic.log" 2>&1
"$native" --callable-numeric-tests > "$out/native-numeric.json"
python3 tools/compare-callables.py --executable "$native" --output "$out/native"
python3 tools/check-callables-boundary.py --executable "$native" --output "$out/native-boundary.json"
python3 tools/check-callable-order.py --executable "$native" --output "$out/native-order"
python3 tools/run-callables-gate.py --check-execution
