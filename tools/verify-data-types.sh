#!/usr/bin/env bash
set -euo pipefail
repo_root="$(cd "$(dirname "$0")/.." && pwd)"
cd "$repo_root"
mkdir -p artifacts/data-types-validation
"$repo_root/tools/build-checker-oracle.sh"
verified_source_hash="$(python3 tools/checker-source-hash.py)"
python3 - <<'PY'
import pathlib,subprocess
root=pathlib.Path('.')
files=[p for folder in ['checker-corpus','object-checker-corpus'] for p in (root/'validation'/folder).rglob('*') if p.is_file()]
files.extend(root/'validation/results'/name for name in ['verified.json','parser-verified.json','binder-verified.json','checker-verified.json','object-checker-verified.json'])
for path in files:
    original=subprocess.check_output(['git','show','1f7c953:'+path.as_posix()])
    if path.read_bytes()!=original:raise RuntimeError('Historical asset changed: '+str(path))
PY
for corpus in checker-corpus object-checker-corpus data-types-corpus; do
  for requests in "validation/$corpus/"*-requests.jsonl; do
    group="$(basename "$requests" -requests.jsonl)"
    current="artifacts/data-types-validation/$corpus-$group-oracle-current.jsonl"
    "$repo_root/tools/.cache/checker-oracle" < "$requests" > "$current"
    cmp "validation/$corpus/$group-expected.jsonl" "$current"
  done
done
python3 tools/probe-checker-utilities.py --output artifacts/data-types-validation/go-utilities-current.json
cmp validation/checker-corpus/utility-expected.json artifacts/data-types-validation/go-utilities-current.json
dotnet test tests/Checker.Tests/Checker.Tests.csproj -c Release --logger 'trx;LogFileName=checker.trx' --results-directory artifacts/data-types-validation
native_properties=()
if ! command -v clang >/dev/null && [[ -x "$repo_root/tools/.cache/native/bin/cc" ]]; then
  native_properties+=("-p:CppCompilerAndLinker=$repo_root/tools/.cache/native/bin/cc")
fi
dotnet publish validation/Checker.Validation/Checker.Validation.csproj -c Release -r linux-x64 -p:PublishAot=true "${native_properties[@]}" -o artifacts/data-types-validation/native
native="$repo_root/artifacts/data-types-validation/native/Checker.Validation"
python3 - <<'PY'
import hashlib,pathlib
root=pathlib.Path('artifacts/data-types-validation')
(root/'native-sha256-before-execution.txt').write_text(hashlib.sha256((root/'native/Checker.Validation').read_bytes()).hexdigest()+'\n')
PY
for group in tiny matrix; do
  python3 tools/compare-checker.py --executable "$native" --corpus validation/data-types-corpus --group "$group" --report "artifacts/data-types-validation/native-$group-result.json"
done
python3 - <<'PY'
import json,pathlib
root=pathlib.Path('artifacts/data-types-validation')
groups=[json.loads((root/('native-'+group+'-result.json')).read_text()) for group in ['tiny','matrix']]
out=dict(executable=groups[0]['executable'],groups=[entry for group in groups for entry in group['groups']],failures=[entry for group in groups for entry in group['failures']])
for key in ['cases','nodes','symbols','tables','types','queries','diagnostics','globalDiagnostics','elapsedSeconds']:out[key]=sum(group[key] for group in groups)
(root/'native-result.json').write_text(json.dumps(out,indent=2)+'\n')
PY
python3 tools/compare-checker.py --executable "$native" --corpus validation/object-checker-corpus --report artifacts/data-types-validation/object-native-result.json
python3 tools/compare-checker.py --executable "$native" --report artifacts/data-types-validation/scalar-native-result.json
python3 tools/check-data-types-boundary.py --executable "$native" --report artifacts/data-types-validation/native-boundary.json
"$repo_root/tools/verify-binder.sh"
[[ "$(python3 tools/checker-source-hash.py)" == "$verified_source_hash" ]] || { echo "Executable source changed during data-types gate" >&2; exit 1; }
