#!/usr/bin/env bash
set -euo pipefail
repo_root="$(cd "$(dirname "$0")/.." && pwd)"
cd "$repo_root"
out=artifacts/collections-validation
mkdir -p "$out"

# Validate frozen expectations/assets before the one retained regression cascade.
python3 tools/run-collections-gate.py --preflight
bash tools/verify-callables.sh

# Original reproduction is verification work, never a native demo prerequisite.
bash tools/build-collections-oracle.sh
python3 tools/run-collections-gate.py --reproduce-original

native_properties=()
if ! command -v clang >/dev/null && [[ -x "$repo_root/tools/.cache/native/bin/cc" ]]; then
  native_properties+=("-p:CppCompilerAndLinker=$repo_root/tools/.cache/native/bin/cc")
fi
dotnet publish validation/Collections.Validation/Collections.Validation.csproj \
  -c Release -r linux-x64 -p:PublishAot=true "${native_properties[@]}" -o "$out/native"
native="$repo_root/$out/native/Collections.Validation"
python3 tools/run-collections-gate.py --capture-native "$out/native-before-collections.json"
python3 tools/run-collections-gate.py --execute-native --executable "$native"
python3 tools/run-collections-gate.py --check-execution
