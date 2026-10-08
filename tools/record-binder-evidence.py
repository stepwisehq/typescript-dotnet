#!/usr/bin/env python3
"""Record verified binding evidence without overwriting earlier checkpoints."""
import hashlib, json, pathlib, subprocess, sys, xml.etree.ElementTree as ET
root = pathlib.Path(__file__).resolve().parent.parent

def read(path): return json.loads((root/path).read_text())
def digest(path): return hashlib.sha256((root/path).read_bytes()).hexdigest()
def run(*command): return subprocess.check_output(command, cwd=root, text=True).strip()
def counters(path):
    result = ET.parse(root/path).find('t:ResultSummary/t:Counters', {'t':'http://microsoft.com/schemas/VisualStudio/TeamTest/2010'})
    counts = {key:int(result.attrib[key]) for key in ['total','executed','passed','failed','error','timeout','aborted']}
    if counts['passed'] != counts['total'] or any(counts[key] for key in ['failed','error','timeout','aborted']): raise RuntimeError('Tests did not all pass: '+str(counts))
    return counts

gate = read('artifacts/binder-validation/final-gate.json')
current = run(sys.executable, 'tools/binder-source-hash.py')
if gate['exitCode'] or current != gate['sourceSha256']: raise RuntimeError('Failed gate or executable source drift')
assets = hashlib.sha256()
for name in gate['verificationAssetFiles']:
    assets.update(name.encode()); assets.update(b'\0'); assets.update((root/name).read_bytes()); assets.update(b'\0')
if assets.hexdigest() != gate['verificationAssetsSha256']: raise RuntimeError('Verification assets changed after gate')

def evidence(folder, executable, trx):
    value = read(folder+'/native-result.json')
    if value['failures']: raise RuntimeError('Native comparisons failed')
    binary = folder+'/native/'+executable
    value.update(nativeSha256=digest(binary), nativeBytes=(root/binary).stat().st_size,
        nativeFile=run('file', str(root/binary)), nativeDependencies=run('ldd', str(root/binary)),
        xUnit=counters(folder+'/'+trx))
    return value
binder = evidence('artifacts/binder-validation','Binder.Validation','binder.trx')
parser = evidence('artifacts/parser-validation','Parser.Validation','parser.trx')
scanner = evidence('artifacts/validation','Scanner.Validation','scanner.trx')
go_checks = (root/'artifacts/binder-validation/go-self-tests.txt').read_text()
if go_checks != (root/'artifacts/binder-validation/native-self-tests.txt').read_text(): raise RuntimeError('Native semantic self-tests differ from Go')
if len(go_checks.splitlines()) != 4: raise RuntimeError('Missing semantic self-tests')
scanner['utilityResults'] = sum(len(json.loads(line).get('utilityResults') or []) for line in (root/'validation/corpus/expected.jsonl').open())
coverage = read('validation/binder-corpus/coverage.json')
if coverage['cases'] != binder['cases']: raise RuntimeError('Coverage differs from tested corpus')
result = dict(upstreamRevision=read('upstream.lock.json')['revision'], sourceSha256=current,
    sourceHashAlgorithm='SHA256 of sorted UTF8 relative path + NUL + raw bytes + NUL for .cs/.csproj under src,tests,validation/Scanner.Validation,validation/Parser.Validation,validation/Binder.Validation excluding bin/obj',
    binder=binder, parserRegression=parser, scannerRegression=scanner, semanticSelfTests=go_checks.splitlines(),
    gate=gate, coverage=dict(path='validation/binder-corpus/coverage.json',sha256=digest('validation/binder-corpus/coverage.json'),inventory=coverage),
    corpusManifest=dict(path='validation/binder-corpus/manifest.json',sha256=digest('validation/binder-corpus/manifest.json')),
    parserReadiness=dict(path='validation/results/binder-parser-readiness.json',sha256=digest('validation/results/binder-parser-readiness.json')),
    historicalEvidencePreserved=['validation/results/verified.json','validation/results/parser-verified.json'],
    goVersion=run('tools/.cache/go/bin/go','version'),dotnetSdkVersion=run('dotnet','--version'))
path=root/'validation/results/binder-verified.json'; path.write_text(json.dumps(result,indent=2)+'\n')
print(json.dumps(dict(path=path.relative_to(root).as_posix(),binderCases=binder['cases'],binderTests=binder['xUnit']['passed'],parserCases=parser['cases'],parserTests=parser['xUnit']['passed'],scannerTests=scanner['xUnit']['passed'],sourceSha256=current,nativeBytes=binder['nativeBytes'])))
