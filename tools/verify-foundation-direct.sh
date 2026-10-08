#!/usr/bin/env bash
# First milestone: original direct assertion bodies in xUnit and actual Native AOT.
set -euo pipefail
repo_root="$(cd "$(dirname "$0")/.." && pwd)"
cd "$repo_root"
command -v node >/dev/null || { echo "Node is required by the original JS-number reference assertions" >&2; exit 1; }
mkdir -p artifacts/foundation-direct
node --version > artifacts/foundation-direct/node-version.txt
"$repo_root/tools/bootstrap-go.sh"
"$repo_root/tools/bootstrap-upstream.sh"
# This is the unchanged original Go test for the oracle's input splitter,
# not a claim of a C# runner infrastructure port.
"$repo_root/tools/.cache/go/bin/go" -C "$repo_root/upstream/TypeScript/tsc" test ./internal/testrunner -run '^TestMakeUnitsFromTest$' -count=1
dotnet test tests/Scanner.Tests/Scanner.Tests.csproj -c Release --filter 'FullyQualifiedName~Foundation|FullyQualifiedName=TypeScript.Scanner.Tests.OracleTests.ScanStringPreservesLoneSurrogatesLikeUpstream|FullyQualifiedName=TypeScript.Scanner.Tests.OracleTests.ScanSourceKeywordLikeUpstream' --logger 'trx;LogFileName=scanner-direct.trx' --results-directory artifacts/foundation-direct
dotnet test tests/Parser.Tests/Parser.Tests.csproj -c Release --filter 'FullyQualifiedName~FoundationDirectTests' --logger 'trx;LogFileName=parser-ast-direct.trx' --results-directory artifacts/foundation-direct
native_properties=()
if ! command -v clang >/dev/null && [[ -x "$repo_root/tools/.cache/native/bin/cc" ]]; then
  native_properties+=("-p:CppCompilerAndLinker=$repo_root/tools/.cache/native/bin/cc")
fi
dotnet publish validation/Foundation.Validation/Foundation.Validation.csproj -c Release -r linux-x64 -p:PublishAot=true "${native_properties[@]}" -o artifacts/foundation-direct/native
artifacts/foundation-direct/native/Foundation.Validation > artifacts/foundation-direct/native-result.json
python3 - <<'PY'
import json,pathlib,xml.etree.ElementTree as ET
root=pathlib.Path('artifacts/foundation-direct')
counts=[]
for name in ['scanner-direct.trx','parser-ast-direct.trx']:
    tree=ET.parse(root/name)
    row=tree.find('.//{http://microsoft.com/schemas/VisualStudio/TeamTest/2010}Counters')
    if row is None or int(row.attrib['total'])==0 or row.attrib['total']!=row.attrib['passed']:
        raise RuntimeError('Original direct xUnit assertions missing/failed: '+name)
    counts.append(int(row.attrib['total']))
native=json.loads((root/'native-result.json').read_text())
if counts != [native['scannerCases'],native['parserAstCases']]:
    raise RuntimeError('Native and xUnit shared assertion case counts differ')
print(json.dumps(dict(xUnitCases=sum(counts),native=native)))
PY
