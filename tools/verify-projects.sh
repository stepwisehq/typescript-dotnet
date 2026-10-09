#!/usr/bin/env bash
set -euo pipefail
repo_root="$(cd "$(dirname "$0")/.." && pwd)"
cd "$repo_root"
out=artifacts/projects-validation
mkdir -p "$out"
python3 tools/run-projects-gate.py --preflight
# One complete retained cascade; no previous campaign evidence recorder is called.
bash tools/verify-collections.sh
bash tools/build-projects-oracle.sh
python3 tools/run-projects-gate.py --reproduce-original
native_properties=()
if ! command -v clang >/dev/null && [[ -x "$repo_root/tools/.cache/native/bin/cc" ]]; then
  native_properties+=("-p:CppCompilerAndLinker=$repo_root/tools/.cache/native/bin/cc")
fi
dotnet publish validation/Projects.Validation/Projects.Validation.csproj \
  -c Release -r linux-x64 -p:PublishAot=true "${native_properties[@]}" -o "$out/native"
dotnet publish src/TypeScript.Check/TypeScript.Check.csproj \
  -c Release -r linux-x64 -p:PublishAot=true "${native_properties[@]}" -o "$out/cli-native"
python3 tools/run-projects-gate.py --capture-native "$out/native-before-projects.json"
python3 tools/run-projects-gate.py --execute-native
python3 tools/run-projects-gate.py --check-execution
