#!/usr/bin/env bash
set -euo pipefail
repo_root="$(cd "$(dirname "$0")/.." && pwd)"
cd "$repo_root"
mkdir -p artifacts/validation
"$repo_root/tools/build-oracle.sh"
verified_source_hash="$(python3 tools/source-hash.py)"
"$repo_root/tools/.cache/go/bin/go" -C "$repo_root/upstream/TypeScript/tsc" test ./internal/scanner
"$repo_root/tools/.cache/scanner-oracle" < validation/corpus/requests.jsonl > artifacts/validation/oracle-current.jsonl
cmp validation/corpus/expected.jsonl artifacts/validation/oracle-current.jsonl
dotnet test tests/Scanner.Tests/Scanner.Tests.csproj -c Release --logger 'trx;LogFileName=scanner.trx' --results-directory artifacts/validation
native_properties=()
if ! command -v clang >/dev/null && [[ -x "$repo_root/tools/.cache/native/bin/cc" ]]; then
  native_properties+=("-p:CppCompilerAndLinker=$repo_root/tools/.cache/native/bin/cc")
fi
dotnet publish validation/Scanner.Validation/Scanner.Validation.csproj -c Release -r linux-x64 -p:PublishAot=true "${native_properties[@]}" -o artifacts/validation/native
python3 tools/compare.py --executable "$repo_root/artifacts/validation/native/Scanner.Validation" --report artifacts/validation/native-result.json

[[ "$(python3 tools/source-hash.py)" == "$verified_source_hash" ]] || { echo "Executable source changed during verification; rerun after changes finish" >&2; exit 1; }
