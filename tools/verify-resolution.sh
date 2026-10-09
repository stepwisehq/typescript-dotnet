#!/usr/bin/env bash
set -euo pipefail
resolution_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$resolution_root"
out=artifacts/resolution-validation
mkdir -p "$out"
python3 tools/run-resolution-gate.py --preflight
bash tools/build-resolution-oracle.sh
bash tools/build-resolution-cli-oracle.sh
python3 tools/run-resolution-gate.py --reproduce-original

# Complete new subsystem evidence first; a failure here does not launch the
# retained regression cascade. Expectations and prior proof files are read only.
dotnet test tests/Resolution.Tests/Resolution.Tests.csproj -c Release \
  --logger 'trx;LogFileName=resolution.trx' --results-directory "$out"
native_properties=()
if ! command -v clang >/dev/null && [[ -x "$resolution_root/tools/.cache/native/bin/cc" ]]; then
  native_properties+=("-p:CppCompilerAndLinker=$resolution_root/tools/.cache/native/bin/cc")
fi
dotnet publish validation/Resolution.Validation/Resolution.Validation.csproj \
  -c Release -r linux-x64 -p:PublishAot=true "${native_properties[@]}" -o "$out/native"
dotnet publish src/TypeScript.Check/TypeScript.Check.csproj \
  -c Release -r linux-x64 -p:PublishAot=true "${native_properties[@]}" -o "$out/cli-native"
python3 tools/run-resolution-gate.py --capture-native "$out/native-before-resolution.json"
python3 tools/run-resolution-gate.py --execute-native

# Exactly one existing cascade, with its current-source proof and fingerprints.
# No historical campaign evidence recorder is invoked.
bash tools/verify-configuration.sh
python3 tools/run-resolution-gate.py --check-execution
