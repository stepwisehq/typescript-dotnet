#!/usr/bin/env bash
set -euo pipefail
repo_root="$(cd "$(dirname "$0")/.." && pwd)"
cd "$repo_root"
mkdir -p artifacts/object-checker-validation
"$repo_root/tools/build-checker-oracle.sh"
verified_source_hash="$(python3 tools/checker-source-hash.py)"
# These accepted scalar fixture/evidence bytes must remain unchanged.
python3 - <<'PY'
import pathlib,subprocess
root=pathlib.Path('.')
files=[p for p in (root/'validation/checker-corpus').rglob('*') if p.is_file()]
files.append(root/'validation/results/checker-verified.json')
for path in files:
    original=subprocess.check_output(['git','show','7e692dd:'+path.as_posix()])
    if path.read_bytes()!=original:raise RuntimeError('Historical scalar asset changed: '+str(path))
PY
for corpus in checker-corpus object-checker-corpus; do
  for requests in "validation/$corpus/"*-requests.jsonl; do
    group="$(basename "$requests" -requests.jsonl)"
    current="artifacts/object-checker-validation/$corpus-$group-oracle-current.jsonl"
    "$repo_root/tools/.cache/checker-oracle" < "$requests" > "$current"
    cmp "validation/$corpus/$group-expected.jsonl" "$current"
  done
done
python3 tools/probe-checker-utilities.py --output artifacts/object-checker-validation/go-utilities-current.json
cmp validation/checker-corpus/utility-expected.json artifacts/object-checker-validation/go-utilities-current.json
dotnet test tests/Checker.Tests/Checker.Tests.csproj -c Release --logger 'trx;LogFileName=checker.trx' --results-directory artifacts/object-checker-validation
native_properties=()
if ! command -v clang >/dev/null && [[ -x "$repo_root/tools/.cache/native/bin/cc" ]]; then
  native_properties+=("-p:CppCompilerAndLinker=$repo_root/tools/.cache/native/bin/cc")
fi
dotnet publish validation/Checker.Validation/Checker.Validation.csproj -c Release -r linux-x64 -p:PublishAot=true "${native_properties[@]}" -o artifacts/object-checker-validation/native
native="$repo_root/artifacts/object-checker-validation/native/Checker.Validation"
python3 - <<'PY'
import hashlib,pathlib
root=pathlib.Path('artifacts/object-checker-validation')
(root/'native-sha256-before-execution.txt').write_text(hashlib.sha256((root/'native/Checker.Validation').read_bytes()).hexdigest()+'\n')
PY
python3 tools/compare-checker.py --executable "$native" --corpus validation/object-checker-corpus --report artifacts/object-checker-validation/native-result.json
python3 tools/compare-checker.py --executable "$native" --report artifacts/object-checker-validation/scalar-native-result.json
python3 tools/check-object-checker-boundary.py --executable "$native" --report artifacts/object-checker-validation/native-boundary.json
# Runs current binder, parser and scanner xUnit/native gates without overwriting
# their historical durable acceptance records.
"$repo_root/tools/verify-binder.sh"
[[ "$(python3 tools/checker-source-hash.py)" == "$verified_source_hash" ]] || { echo "Executable source changed during object checker gate" >&2; exit 1; }
