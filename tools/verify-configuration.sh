#!/usr/bin/env bash
set -euo pipefail
configuration_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$configuration_root"
out=artifacts/configuration-validation
mkdir -p "$out"
python3 tools/run-configuration-gate.py --preflight
bash tools/build-configuration-oracle.sh
python3 tools/run-configuration-gate.py --reproduce-original

# Complete new subsystem evidence first; a failure here does not launch the
# retained regression cascade. Expectations and prior proof files are read only.
dotnet test tests/Configuration.Tests/Configuration.Tests.csproj -c Release \
  --logger 'trx;LogFileName=configuration.trx' --results-directory "$out"
native_properties=()
if ! command -v clang >/dev/null && [[ -x "$configuration_root/tools/.cache/native/bin/cc" ]]; then
  native_properties+=("-p:CppCompilerAndLinker=$configuration_root/tools/.cache/native/bin/cc")
fi
dotnet publish validation/Configuration.Validation/Configuration.Validation.csproj \
  -c Release -r linux-x64 -p:PublishAot=true "${native_properties[@]}" -o "$out/native"
dotnet publish src/TypeScript.Check/TypeScript.Check.csproj \
  -c Release -r linux-x64 -p:PublishAot=true "${native_properties[@]}" -o "$out/cli-native"
python3 tools/run-configuration-gate.py --capture-native "$out/native-before-configuration.json"
python3 tools/run-configuration-gate.py --execute-native

# Exactly one existing cascade, with its current-source proof and fingerprints.
# No historical campaign evidence recorder is invoked.
bash tools/verify-projects.sh
python3 tools/run-configuration-gate.py --check-execution
