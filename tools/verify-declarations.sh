#!/usr/bin/env bash
set -euo pipefail
declarations_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$declarations_root"
export DECLARATIONS_CURRENT_CHECKING=1
out=artifacts/declarations-validation
mkdir -p "$out"
python3 tools/run-declarations-gate.py --preflight
bash tools/build-program-oracle.sh
bash tools/build-resolution-cli-oracle.sh
python3 tools/run-declarations-gate.py --reproduce-original

# Cheap current assertion/corpus and affected consumer checks run before AOT.
dotnet test tests/Declarations.Tests/Declarations.Tests.csproj -c Release \
  --logger 'trx;LogFileName=declarations.trx' --results-directory "$out"
dotnet build validation/Declarations.Validation/Declarations.Validation.csproj -c Release -o "$out/managed"
dotnet build src/TypeScript.Check/TypeScript.Check.csproj -c Release -o "$out/cli-managed"
python3 tools/run-declarations-gate.py --capture-managed
python3 tools/run-declarations-gate.py --execute-managed
mkdir -p "$out/retained-consumers"
for consumer in Binder Checker Scanner; do
  consumer_lower="${consumer,,}"
  dotnet test "tests/$consumer.Tests/$consumer.Tests.csproj" -c Release \
    --logger "trx;LogFileName=$consumer_lower.trx" --results-directory "$out/retained-consumers"
done
dotnet test tests/Configuration.Tests/Configuration.Tests.csproj -c Release \
  --filter 'FullyQualifiedName~IntegrationTests' \
  --logger 'trx;LogFileName=configuration-integration.trx' --results-directory "$out/retained-consumers"
python3 tools/run-declarations-gate.py --check-managed

native_properties=()
if ! command -v clang >/dev/null && [[ -x "$declarations_root/tools/.cache/native/bin/cc" ]]; then
  native_properties+=("-p:CppCompilerAndLinker=$declarations_root/tools/.cache/native/bin/cc")
fi
dotnet publish validation/Declarations.Validation/Declarations.Validation.csproj \
  -c Release -r linux-x64 -p:PublishAot=true "${native_properties[@]}" -o "$out/native"
dotnet publish src/TypeScript.Check/TypeScript.Check.csproj \
  -c Release -r linux-x64 -p:PublishAot=true "${native_properties[@]}" -o "$out/cli-native"
python3 tools/run-declarations-gate.py --capture-native "$out/native-before-declarations.json"
python3 tools/run-declarations-gate.py --execute-native

# Exactly one retained cascade with the explicitly reviewed current CLI context.
# Its historical expectations/proofs are preserved; no old recorder is invoked.
bash tools/verify-program.sh
python3 tools/run-declarations-gate.py --check-execution
