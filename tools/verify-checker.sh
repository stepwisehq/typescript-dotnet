#!/usr/bin/env bash
set -euo pipefail
repo_root="$(cd "$(dirname "$0")/.." && pwd)"
cd "$repo_root"
mkdir -p artifacts/checker-validation
"$repo_root/tools/build-checker-oracle.sh"
verified_source_hash="$(python3 tools/checker-source-hash.py)"
for requests in validation/checker-corpus/*-requests.jsonl; do
  group="$(basename "$requests" -requests.jsonl)"
  "$repo_root/tools/.cache/checker-oracle" < "$requests" > "artifacts/checker-validation/$group-oracle-current.jsonl"
  cmp "validation/checker-corpus/$group-expected.jsonl" "artifacts/checker-validation/$group-oracle-current.jsonl"
done
python3 tools/probe-checker-utilities.py --output artifacts/checker-validation/go-utilities-current.json
cmp validation/checker-corpus/utility-expected.json artifacts/checker-validation/go-utilities-current.json
dotnet test tests/Checker.Tests/Checker.Tests.csproj -c Release --logger 'trx;LogFileName=checker.trx' --results-directory artifacts/checker-validation
native_properties=()
if ! command -v clang >/dev/null && [[ -x "$repo_root/tools/.cache/native/bin/cc" ]]; then
  native_properties+=("-p:CppCompilerAndLinker=$repo_root/tools/.cache/native/bin/cc")
fi
dotnet publish validation/Checker.Validation/Checker.Validation.csproj -c Release -r linux-x64 -p:PublishAot=true "${native_properties[@]}" -o artifacts/checker-validation/native
python3 tools/compare-checker.py --executable "$repo_root/artifacts/checker-validation/native/Checker.Validation" --report artifacts/checker-validation/native-result.json
python3 tools/check-checker-boundary.py --executable "$repo_root/artifacts/checker-validation/native/Checker.Validation" --report artifacts/checker-validation/native-boundary.json
# Earlier accepted records remain historical; execute current regression gates.
"$repo_root/tools/verify-binder.sh"
[[ "$(python3 tools/checker-source-hash.py)" == "$verified_source_hash" ]] || { echo "Executable source changed during checker gate" >&2; exit 1; }
