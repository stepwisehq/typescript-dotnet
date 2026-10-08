#!/usr/bin/env python3
"""Record verified checking evidence without overwriting earlier checkpoints."""
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

gate = read('artifacts/checker-validation/final-gate.json')
current = run(sys.executable, 'tools/checker-source-hash.py')
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
checker = evidence('artifacts/checker-validation','Checker.Validation','checker.trx')
binder = evidence('artifacts/binder-validation','Binder.Validation','binder.trx')
parser = evidence('artifacts/parser-validation','Parser.Validation','parser.trx')
scanner = evidence('artifacts/validation','Scanner.Validation','scanner.trx')
boundary = read('artifacts/checker-validation/native-boundary.json')
if boundary['admissionControls'] != 35 or len(boundary['goUtilityObservations']) != 20 or len(boundary['authoredPublicApiAssertions']) != 10: raise RuntimeError('Incomplete boundary evidence')
scanner['utilityResults'] = sum(len(json.loads(line).get('utilityResults') or []) for line in (root/'validation/corpus/expected.jsonl').open())
coverage = read('validation/checker-corpus/coverage.json')
if coverage['cases'] != checker['cases']: raise RuntimeError('Coverage differs from tested corpus')
result = dict(upstreamRevision=read('upstream.lock.json')['revision'], sourceSha256=current,
    sourceHashAlgorithm='SHA256 of sorted UTF8 relative path + NUL + raw bytes + NUL for .cs/.csproj under src,tests,validation/Scanner.Validation,validation/Parser.Validation,validation/Binder.Validation,validation/Checker.Validation excluding bin/obj',
    checker=checker, binderRegression=binder, parserRegression=parser, scannerRegression=scanner, boundary=boundary,
    gate=gate, coverage=dict(path='validation/checker-corpus/coverage.json',sha256=digest('validation/checker-corpus/coverage.json'),inventory=coverage),
    corpusManifest=dict(path='validation/checker-corpus/manifest.json',sha256=digest('validation/checker-corpus/manifest.json')),
    parserReadiness=dict(path='validation/results/binder-parser-readiness.json',sha256=digest('validation/results/binder-parser-readiness.json')),
    historicalEvidencePreserved=['validation/results/verified.json','validation/results/parser-verified.json','validation/results/binder-verified.json'],
    goVersion=run('tools/.cache/go/bin/go','version'),dotnetSdkVersion=run('dotnet','--version'))
path=root/'validation/results/checker-verified.json'; path.write_text(json.dumps(result,indent=2)+'\n')
print(json.dumps(dict(path=path.relative_to(root).as_posix(),checkerCases=checker['cases'],checkerTests=checker['xUnit']['passed'],binderCases=binder['cases'],binderTests=binder['xUnit']['passed'],parserCases=parser['cases'],parserTests=parser['xUnit']['passed'],scannerTests=scanner['xUnit']['passed'],sourceSha256=current,nativeBytes=checker['nativeBytes'])))
