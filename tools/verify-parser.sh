#!/usr/bin/env bash
set -euo pipefail
repo_root="$(cd "$(dirname "$0")/.." && pwd)"
cd "$repo_root"
mkdir -p artifacts/parser-validation
"$repo_root/tools/build-parser-oracle.sh"
verified_source_hash="$(python3 tools/parser-source-hash.py)"
"$repo_root/tools/.cache/go/bin/go" -C "$repo_root/upstream/TypeScript/tsc" test ./internal/parser -run 'TestHeritageClauseElementKinds|TestParseStaticSourcePhaseImport|TestParseSourceAsImportEqualsBinding' -count=1
for requests in validation/parser-corpus/*-requests.jsonl; do
  group="$(basename "$requests" -requests.jsonl)"
  "$repo_root/tools/.cache/parser-oracle" < "$requests" > "artifacts/parser-validation/$group-oracle-current.jsonl"
  cmp "validation/parser-corpus/$group-expected.jsonl" "artifacts/parser-validation/$group-oracle-current.jsonl"
done
dotnet test tests/Parser.Tests/Parser.Tests.csproj -c Release --logger 'trx;LogFileName=parser.trx' --results-directory artifacts/parser-validation
native_properties=()
if ! command -v clang >/dev/null && [[ -x "$repo_root/tools/.cache/native/bin/cc" ]]; then
  native_properties+=("-p:CppCompilerAndLinker=$repo_root/tools/.cache/native/bin/cc")
fi
dotnet publish validation/Parser.Validation/Parser.Validation.csproj -c Release -r linux-x64 -p:PublishAot=true "${native_properties[@]}" -o artifacts/parser-validation/native
python3 tools/compare-parser.py --executable "$repo_root/artifacts/parser-validation/native/Parser.Validation" --report artifacts/parser-validation/native-result.json
# Parser integration changes scanner helpers. Run the original scanner gate against
# this new snapshot while leaving its historical checkpoint evidence intact.
"$repo_root/tools/verify.sh"
[[ "$(python3 tools/parser-source-hash.py)" == "$verified_source_hash" ]] || { echo "Executable source changed during parser verification; rerun after changes finish" >&2; exit 1; }
python3 - "$verified_source_hash" <<'PY'
import hashlib,json,pathlib,subprocess,sys
root=pathlib.Path.cwd()
path=root/'artifacts/parser-validation/native-result.json'
report=json.loads(path.read_text())
binary=root/'artifacts/parser-validation/native/Parser.Validation'
report.update(sourceSha256=sys.argv[1],nativeSha256=hashlib.sha256(binary.read_bytes()).hexdigest(),nativeFile=subprocess.check_output(['file',str(binary)],text=True).strip(),nativeDependencies=subprocess.check_output(['ldd',str(binary)],text=True).strip(),upstreamRevision=json.loads((root/'upstream.lock.json').read_text()).get('revision'))
path.write_text(json.dumps(report,indent=2)+'\n')
PY
