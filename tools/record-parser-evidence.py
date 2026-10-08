#!/usr/bin/env python3
"""Store successful frozen-tree parser/scanner evidence; keep historical data."""
import hashlib,json,pathlib,subprocess,sys,xml.etree.ElementTree as ET
root=pathlib.Path(__file__).resolve().parent.parent
def read(path):return json.loads((root/path).read_text())
def digest(path):return hashlib.sha256((root/path).read_bytes()).hexdigest()
def run(*command):return subprocess.check_output(command,cwd=root,text=True).strip()
def counters(path):
 tree=ET.parse(root/path);ns={'t':'http://microsoft.com/schemas/VisualStudio/TeamTest/2010'}
 result=tree.find('t:ResultSummary/t:Counters',ns)
 values={k:int(result.attrib[k]) for k in ['total','executed','passed','failed','error','timeout','aborted']}
 if values['passed']!=values['total'] or any(values[k] for k in ['failed','error','timeout','aborted']):raise RuntimeError('Tests did not all pass: '+str(values))
 return values
gate=read('artifacts/parser-validation/final-gate.json')
native=read('artifacts/parser-validation/native-result.json')
scanner=read('artifacts/validation/native-result.json')
current=run(sys.executable,'tools/parser-source-hash.py')
if gate['exitCode'] or native['failures'] or scanner['failures']:raise RuntimeError('Failed final gate cannot become acceptance evidence')
if current!=gate['sourceSha256'] or current!=native['sourceSha256']:raise RuntimeError('Source drift after final gate')
assets=hashlib.sha256()
for name in gate['verificationAssetFiles']:
 assets.update(name.encode());assets.update(b'\0');assets.update((root/name).read_bytes());assets.update(b'\0')
if assets.hexdigest()!=gate['verificationAssetsSha256']:raise RuntimeError('Verification assets changed after final gate')
binary='artifacts/parser-validation/native/Parser.Validation'
if digest(binary)!=native['nativeSha256']:raise RuntimeError('Native binary changed after final gate')
oracle_sources=sorted((root/'tools/parser-oracle').glob('*.go'))
oracle_hash=hashlib.sha256()
for path in oracle_sources:
 oracle_hash.update(path.relative_to(root).as_posix().encode());oracle_hash.update(b'\0');oracle_hash.update(path.read_bytes());oracle_hash.update(b'\0')
coverage=read('validation/parser-corpus/coverage.json')
parser_tests=counters('artifacts/parser-validation/parser.trx');scanner_tests=counters('artifacts/validation/scanner.trx')
scanner['utilityResults']=sum(len(json.loads(line).get('utilityResults') or []) for line in (root/'validation/corpus/expected.jsonl').open())
scanner['nativeSha256']=digest('artifacts/validation/native/Scanner.Validation')
scanner['xUnit']=scanner_tests
result=dict(upstreamRevision=read('upstream.lock.json')['revision'],sourceSha256=current,sourceHashAlgorithm='SHA256 of sorted UTF8 relative path + NUL + raw bytes + NUL for .cs/.csproj under src,tests,validation/Scanner.Validation,validation/Parser.Validation excluding bin/obj',native=native,nativeBytes=(root/binary).stat().st_size,xUnit=parser_tests,scannerRegression=scanner,gate=gate,coverage=dict(path='validation/parser-corpus/coverage.json',sha256=digest('validation/parser-corpus/coverage.json'),cases=coverage['cases'],nodes=coverage['nodes'],kinds=len(coverage['kindCounts']),payloads=len(coverage['payloadCounts']),diagnosticChannels=coverage['diagnosticChannelCounts'],observedDiagnosticCodes=coverage['observedDiagnosticCodes'],unobservedParserReferencedDiagnosticCodes=coverage['unobservedParserReferencedCodes']),upstreamManifest=dict(path='validation/parser-corpus/upstream-manifest.json',sha256=digest('validation/parser-corpus/upstream-manifest.json')),oracleSourceSha256=oracle_hash.hexdigest(),goVersion=run('tools/.cache/go/bin/go','version'),dotnetSdkVersion=run('dotnet','--version'))
path=root/'validation/results/parser-verified.json';path.write_text(json.dumps(result,indent=2)+'\n')
print(json.dumps(dict(path=path.relative_to(root).as_posix(),cases=coverage['cases'],parserTests=parser_tests['passed'],scannerTests=scanner_tests['passed'],nativeBytes=result['nativeBytes'],sourceSha256=current)))
