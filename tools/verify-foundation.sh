#!/usr/bin/env bash
set -euo pipefail
repo_root="$(cd "$(dirname "$0")/.." && pwd)"
cd "$repo_root"
mkdir -p artifacts/foundation-validation
command -v node >/dev/null || { echo "Original JS-number assertions require Node" >&2; exit 1; }
node --version > artifacts/foundation-validation/node-version.txt
./tools/build-parser-oracle.sh
./tools/build-oracle.sh
bash tools/foundation-syntax-probes/generate.sh artifacts/foundation-validation/syntax-services-current.json
cmp validation/foundation-corpus/syntax-service-fixtures.json artifacts/foundation-validation/syntax-services-current.json
# Reproduce selected clone graphs from the original source, never bless differences.
tools/.cache/parser-oracle --foundation-clones > artifacts/foundation-validation/clone-current.json
cmp validation/foundation-corpus/clone-fixtures.json artifacts/foundation-validation/clone-current.json
tools/.cache/parser-oracle < validation/foundation-corpus/diagnostic-gaps-requests.jsonl > artifacts/foundation-validation/diagnostic-gaps-current.jsonl
cmp validation/foundation-corpus/diagnostic-gaps-expected.jsonl artifacts/foundation-validation/diagnostic-gaps-current.jsonl
tools/.cache/parser-oracle < validation/foundation-corpus/targeted-requests.jsonl > artifacts/foundation-validation/targeted-current.jsonl
cmp validation/foundation-corpus/targeted-expected.jsonl artifacts/foundation-validation/targeted-current.jsonl
# Original direct packages include finite original parser seeds (no unbounded fuzz engine).
go_bin="$repo_root/tools/.cache/go/bin/go"
"$go_bin" -C upstream/TypeScript/tsc test ./internal/scanner ./internal/parser ./internal/ast -count=1
"$go_bin" -C upstream/TypeScript/tsc test ./internal/diagnostics -run '^(TestLocalize|TestLocalize_ByKey)$' -count=1
"$go_bin" -C upstream/TypeScript/tsc test ./internal/jsnum -run '^(TestString|TestFromString|TestStringRoundtrip|TestStringJS|TestParsePseudoBigInt)$' -count=1
"$go_bin" -C upstream/TypeScript/tsc test ./internal/jsnum -run '^$' -fuzz '^FuzzStringJS$' -fuzztime=1x -parallel=1
"$go_bin" -C upstream/TypeScript/tsc test ./internal/jsnum -run '^$' -fuzz '^FuzzFromStringJS$' -fuzztime=1x -parallel=1
"$go_bin" -C upstream/TypeScript/tsc test ./internal/testrunner -run '^TestMakeUnitsFromTest$' -count=1
"$go_bin" -C upstream/TypeScript/tsc test ./internal/tspath -run '^(TestGetRootLength|TestIsRootedDiskPath|TestPathIsRelative|TestNormalizeSlashes)$' -count=1
# This chain runs all checker/binder/parser/scanner xUnit tests, fresh historical
# original-Go expectations and actual Native AOT executables at each public layer.
./tools/verify-data-types.sh
native_properties=()
if ! command -v clang >/dev/null && [[ -x "$repo_root/tools/.cache/native/bin/cc" ]]; then
  native_properties+=("-p:CppCompilerAndLinker=$repo_root/tools/.cache/native/bin/cc")
fi
dotnet publish validation/Foundation.Validation/Foundation.Validation.csproj -c Release -r linux-x64 -p:PublishAot=true "${native_properties[@]}" -o artifacts/foundation-validation/native
python3 - <<'PYHASH'
import hashlib,json,pathlib
paths=['artifacts/'+p for p in ['foundation-validation/native/Foundation.Validation','parser-validation/native/Parser.Validation','validation/native/Scanner.Validation','binder-validation/native/Binder.Validation','data-types-validation/native/Checker.Validation']]
pathlib.Path('artifacts/foundation-validation/native-hashes-before.json').write_text(json.dumps({p:hashlib.sha256(pathlib.Path(p).read_bytes()).hexdigest() for p in paths},indent=2)+'\n')
PYHASH
artifacts/foundation-validation/native/Foundation.Validation > artifacts/foundation-validation/native-direct.json
artifacts/foundation-validation/native/Foundation.Validation --counts > artifacts/foundation-validation/native-counts.json
artifacts/foundation-validation/native/Foundation.Validation --fuzz > artifacts/foundation-validation/native-fuzz.json
python3 tools/compare-parser.py --corpus validation/foundation-corpus --group targeted --executable "$repo_root/artifacts/parser-validation/native/Parser.Validation" --report artifacts/foundation-validation/targeted-native-result.json
python3 tools/compare-foundation.py --executable "$repo_root/artifacts/parser-validation/native/Parser.Validation" --output artifacts/foundation-validation/broad-parser-native
python3 tools/compare-foundation.py --scanner --executable "$repo_root/artifacts/validation/native/Scanner.Validation" --output artifacts/foundation-validation/broad-scanner-native
python3 - <<'PY'
import collections,hashlib,json,pathlib,xml.etree.ElementTree as ET
root=pathlib.Path('artifacts/foundation-validation');manifest=json.loads(pathlib.Path('validation/foundation-corpus/source-manifest.json').read_text())
direct=json.loads((root/'native-direct.json').read_text());counts=json.loads((root/'native-counts.json').read_text())
expected={'cases':3153,'scanner':2552,'parser-ast':563,'syntax-service':14,'clone-graph':21,'diagnostic-gap':3}
if counts!=expected:raise RuntimeError('Shared enumeration differs from frozen case inventory: '+str(counts))
mapping={'scanner':'scannerCases','parser-ast':'parserAstCases','syntax-service':'syntaxServiceCases','clone-graph':'cloneGraphCases','diagnostic-gap':'diagnosticGapCases'}
if direct['cases']!=counts['cases'] or any(direct[field]!=counts[component] for component,field in mapping.items()):raise RuntimeError('Shared Native AOT cases missing or incomplete')
for stage in ['broad-parser-native','broad-scanner-native']:
    result=json.loads((root/stage/'report.json').read_text())
    if result['limited'] or result['counts']['requests']!=manifest['requests'] or result['counts']['exact']!=manifest['requests'] or result['failures']:raise RuntimeError('Incomplete/failed broad foundation '+stage)
ns='{http://microsoft.com/schemas/VisualStudio/TeamTest/2010}'
def methods(path):
    tree=ET.parse(path);results=collections.Counter()
    for row in tree.findall('.//'+ns+'UnitTest'):
        method=row.find(ns+'TestMethod')
        if method is not None:results[method.attrib['className'].split('.')[-1]]+=1
    return tree,results
scanner_tree,scanner_methods=methods('artifacts/validation/scanner.trx')
if scanner_methods['FoundationScannerTests']+2!=counts['scanner']:raise RuntimeError('Scanner shared xUnit cases incomplete')
for name in ['ScanStringPreservesLoneSurrogatesLikeUpstream','ScanSourceKeywordLikeUpstream']:
    if sum(row.attrib.get('testName')=='TypeScript.Scanner.Tests.OracleTests.'+name for row in scanner_tree.findall('.//'+ns+'UnitTestResult'))!=1:raise RuntimeError('Missing legacy forwarding assertion '+name)
_,parser_methods=methods('artifacts/parser-validation/parser.trx')
for cls,group in [('FoundationDirectTests','parser-ast'),('FoundationSyntaxServiceTests','syntax-service'),('FoundationCloneGraphTests','clone-graph'),('FoundationDiagnosticGapTests','diagnostic-gap')]:
    if parser_methods[cls]!=counts[group]:raise RuntimeError('Shared xUnit/native count differs: '+cls)
if parser_methods['FoundationFuzzTests']!=manifest['counts']['originalFuzzSeeds']:raise RuntimeError('Finite xUnit seeds incomplete')
for path,want in json.loads((root/'native-hashes-before.json').read_text()).items():
    if hashlib.sha256(pathlib.Path(path).read_bytes()).hexdigest()!=want:raise RuntimeError('Native executable changed during execution: '+path)
targeted=json.loads((root/'targeted-native-result.json').read_text())
if targeted['cases']!=8 or targeted['failures']:raise RuntimeError('Targeted native graphs incomplete')
fuzz=json.loads((root/'native-fuzz.json').read_text())
if fuzz['originalFiniteFuzzSeeds']!=manifest['counts']['originalFuzzSeeds'] or fuzz['skipped']:raise RuntimeError('Original finite parser seeds incomplete/skipped')
PY

