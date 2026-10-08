#!/usr/bin/env bash
set -euo pipefail
repo_root="$(cd "$(dirname "$0")/.." && pwd)"
cd "$repo_root"
accepted_hash=d441e371cf28045777b3c8041d51e524ce96904ddecf6ff7e4f105edaf2edf8d
[[ "$(python3 tools/parser-source-hash.py)" == "$accepted_hash" ]] || { echo 'Accepted parser source changed' >&2; exit 1; }
"$repo_root/tools/bootstrap-go.sh"
"$repo_root/tools/bootstrap-upstream.sh"
"$repo_root/tools/build-parser-oracle.sh"
mkdir -p upstream/TypeScript/tsc/cmd/parser-bench artifacts/parser-benchmarks
cp tools/parser-benchmark/main.go upstream/TypeScript/tsc/cmd/parser-bench/main.go
"$repo_root/tools/.cache/go/bin/go" -C "$repo_root/upstream/TypeScript/tsc" build -o "$repo_root/tools/.cache/parser-bench" ./cmd/parser-bench
native_properties=()
if ! command -v clang >/dev/null && [[ -x "$repo_root/tools/.cache/native/bin/cc" ]]; then
    native_properties+=("-p:CppCompilerAndLinker=$repo_root/tools/.cache/native/bin/cc")
fi
dotnet publish benchmarks/Parser.Benchmarks/Parser.Benchmarks.csproj -c Release -r linux-x64 -p:PublishAot=true "${native_properties[@]}" -o artifacts/parser-benchmarks/native
python3 tools/parser-benchmark.py "$@"
[[ "$(python3 tools/parser-source-hash.py)" == "$accepted_hash" ]] || { echo 'Accepted parser source changed' >&2; exit 1; }
