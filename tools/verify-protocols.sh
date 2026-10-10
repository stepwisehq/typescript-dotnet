#!/usr/bin/env bash
set -euo pipefail
protocols_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$protocols_root"
export DECLARATIONS_CURRENT_CHECKING=1
out=artifacts/protocols-validation
mkdir -p "$out"
python3 tools/run-protocols-gate.py --preflight
bash tools/build-program-oracle.sh
bash tools/build-resolution-cli-oracle.sh
python3 tools/run-protocols-gate.py --reproduce-original

# New assertion/corpus xUnit and managed comparisons precede native publication.
# Retained full consumer suites run only in the single predecessor cascade.
dotnet test tests/Protocols.Tests/Protocols.Tests.csproj -c Release \
  --logger 'trx;LogFileName=protocols.trx' --results-directory "$out"
dotnet build validation/Protocols.Validation/Protocols.Validation.csproj -c Release -o "$out/managed"
dotnet build src/TypeScript.Check/TypeScript.Check.csproj -c Release -o "$out/cli-managed"
python3 tools/run-protocols-gate.py --capture-managed
python3 tools/run-protocols-gate.py --execute-managed
python3 tools/run-protocols-gate.py --check-managed

# One unchanged retained cascade, with its reviewed authored CLI replacements.
bash tools/verify-typesystem.sh

# The predecessor cheap consumer checks complete before new AOT publication.
native_properties=()
if ! command -v clang >/dev/null && [[ -x "$protocols_root/tools/.cache/native/bin/cc" ]]; then
  native_properties+=("-p:CppCompilerAndLinker=$protocols_root/tools/.cache/native/bin/cc")
fi
dotnet publish validation/Protocols.Validation/Protocols.Validation.csproj \
  -c Release -r linux-x64 -p:PublishAot=true "${native_properties[@]}" -o "$out/native"
dotnet publish src/TypeScript.Check/TypeScript.Check.csproj \
  -c Release -r linux-x64 -p:PublishAot=true "${native_properties[@]}" -o "$out/cli-native"
python3 tools/run-protocols-gate.py --capture-native
python3 tools/run-protocols-gate.py --execute-native

python3 tools/run-protocols-gate.py --check-execution
