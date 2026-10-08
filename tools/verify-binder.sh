#!/usr/bin/env bash
set -euo pipefail
repo_root="$(cd "$(dirname "$0")/.." && pwd)"
cd "$repo_root"
mkdir -p artifacts/binder-validation
"$repo_root/tools/build-binder-oracle.sh"
verified_source_hash="$(python3 tools/binder-source-hash.py)"
for requests in validation/binder-corpus/*-requests.jsonl; do
  group="$(basename "$requests" -requests.jsonl)"
  "$repo_root/tools/.cache/binder-oracle" < "$requests" > "artifacts/binder-validation/$group-oracle-current.jsonl"
  cmp "validation/binder-corpus/$group-expected.jsonl" "artifacts/binder-validation/$group-oracle-current.jsonl"
done
"$repo_root/tools/.cache/binder-oracle" --self-test > artifacts/binder-validation/go-self-tests.txt
dotnet test tests/Binder.Tests/Binder.Tests.csproj -c Release --logger 'trx;LogFileName=binder.trx' --results-directory artifacts/binder-validation
native_properties=()
if ! command -v clang >/dev/null && [[ -x "$repo_root/tools/.cache/native/bin/cc" ]]; then
  native_properties+=("-p:CppCompilerAndLinker=$repo_root/tools/.cache/native/bin/cc")
fi
dotnet publish validation/Binder.Validation/Binder.Validation.csproj -c Release -r linux-x64 -p:PublishAot=true "${native_properties[@]}" -o artifacts/binder-validation/native
artifacts/binder-validation/native/Binder.Validation --self-test > artifacts/binder-validation/native-self-tests.txt
cmp artifacts/binder-validation/go-self-tests.txt artifacts/binder-validation/native-self-tests.txt
python3 tools/compare-binder.py --executable "$repo_root/artifacts/binder-validation/native/Binder.Validation" --report artifacts/binder-validation/native-result.json
# Run current parser/readiness and original scanner regression gates, preserving
# all previously committed acceptance records in validation/results.
"$repo_root/tools/verify-parser.sh"
[[ "$(python3 tools/binder-source-hash.py)" == "$verified_source_hash" ]] || { echo "Executable source changed during binder verification" >&2; exit 1; }
