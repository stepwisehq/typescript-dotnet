#!/usr/bin/env bash
set -euo pipefail
repo_root="$(cd "$(dirname "$0")/.." && pwd)"
cd "$repo_root"
"$repo_root/tools/bootstrap-go.sh"
"$repo_root/tools/bootstrap-upstream.sh"
mkdir -p "$repo_root/upstream/TypeScript/tsc/cmd/scanner-bench" artifacts/benchmarks
cp tools/benchmark/main.go "$repo_root/upstream/TypeScript/tsc/cmd/scanner-bench/main.go"
"$repo_root/tools/.cache/go/bin/go" -C "$repo_root/upstream/TypeScript/tsc" build -o "$repo_root/tools/.cache/scanner-bench" ./cmd/scanner-bench
native_properties=()
if ! command -v clang >/dev/null && [[ -x "$repo_root/tools/.cache/native/bin/cc" ]]; then
  native_properties+=("-p:CppCompilerAndLinker=$repo_root/tools/.cache/native/bin/cc")
fi
dotnet publish benchmarks/Scanner.Benchmarks/Scanner.Benchmarks.csproj -c Release -r linux-x64 -p:PublishAot=true "${native_properties[@]}" -o artifacts/benchmarks/native
python3 tools/benchmark.py "$@"
