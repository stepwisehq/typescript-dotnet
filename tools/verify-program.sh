#!/usr/bin/env bash
set -euo pipefail
program_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$program_root"
out=artifacts/program-validation
mkdir -p "$out"
python3 tools/run-program-gate.py --preflight
bash tools/build-program-oracle.sh
python3 tools/run-program-gate.py --reproduce-original

# Genuine original and managed Program checks precede native publication and
# the one retained resolution cascade. All expectations and prior proofs stay read only.
dotnet test tests/Program.Tests/Program.Tests.csproj -c Release \
  --logger 'trx;LogFileName=program.trx' --results-directory "$out"
dotnet build validation/Program.Validation/Program.Validation.csproj -c Release -o "$out/managed"
dotnet build src/TypeScript.Check/TypeScript.Check.csproj -c Release -o "$out/cli-managed"
python3 tools/run-program-gate.py --capture-managed
python3 tools/run-program-gate.py --execute-managed
mkdir -p "$out/retained-consumers"
dotnet test tests/Binder.Tests/Binder.Tests.csproj -c Release \
  --filter 'FullyQualifiedName~DefaultFileNameBindsAsQuotedEmptyModuleName' \
  --logger 'trx;LogFileName=binder-default.trx' --results-directory "$out/retained-consumers"
dotnet test tests/Resolution.Tests/Resolution.Tests.csproj -c Release \
  --filter 'FullyQualifiedName~GraphByteRoleTests.AuthoredSourceControl' \
  --logger 'trx;LogFileName=resolution-byte-role.trx' --results-directory "$out/retained-consumers"
dotnet test tests/Scanner.Tests/Scanner.Tests.csproj -c Release \
  --filter 'FullyQualifiedName~DiagnosticArgumentsUseUnicodeText' \
  --logger 'trx;LogFileName=scanner-unicode.trx' --results-directory "$out/retained-consumers"
python3 tools/run-program-gate.py --check-managed

native_properties=()
if ! command -v clang >/dev/null && [[ -x "$program_root/tools/.cache/native/bin/cc" ]]; then
  native_properties+=("-p:CppCompilerAndLinker=$program_root/tools/.cache/native/bin/cc")
fi
dotnet publish validation/Program.Validation/Program.Validation.csproj \
  -c Release -r linux-x64 -p:PublishAot=true "${native_properties[@]}" -o "$out/native"
dotnet publish src/TypeScript.Check/TypeScript.Check.csproj \
  -c Release -r linux-x64 -p:PublishAot=true "${native_properties[@]}" -o "$out/cli-native"
python3 tools/run-program-gate.py --capture-native "$out/native-before-program.json"
python3 tools/run-program-gate.py --execute-native

# Exactly one existing cascade; no historical evidence recorder is invoked.
bash tools/verify-resolution.sh
python3 tools/run-program-gate.py --check-execution
