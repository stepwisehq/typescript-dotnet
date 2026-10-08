#!/usr/bin/env python3
"""Record named/recursive/composite acceptance without changing historical proofs."""
import hashlib, json, pathlib, subprocess, sys, xml.etree.ElementTree as ET
root = pathlib.Path(__file__).resolve().parent.parent
folder = 'artifacts/data-types-validation'
def read(path): return json.loads((root/path).read_text())
def digest(path): return hashlib.sha256((root/path).read_bytes()).hexdigest()
def run(*command): return subprocess.check_output(command, cwd=root, text=True).strip()
def counters(path):
    value = ET.parse(root/path).find('t:ResultSummary/t:Counters', {'t':'http://microsoft.com/schemas/VisualStudio/TeamTest/2010'})
    counts = {key:int(value.attrib[key]) for key in ['total','executed','passed','failed','error','timeout','aborted']}
    if counts['passed'] != counts['total'] or any(counts[key] for key in ['failed','error','timeout','aborted']): raise RuntimeError('Failed xUnit checks')
    return counts
gate = read(folder+'/final-gate.json')
source = run(sys.executable, 'tools/checker-source-hash.py')
if gate['exitCode'] or source != gate['sourceSha256']: raise RuntimeError('Failed gate or executable source drift')
assets = hashlib.sha256()
for name in gate['verificationAssetFiles']:
    assets.update(name.encode()); assets.update(b'\0'); assets.update((root/name).read_bytes()); assets.update(b'\0')
if assets.hexdigest() != gate['verificationAssetsSha256']: raise RuntimeError('Verification assets changed after gate')
def native_evidence(directory, executable, trx):
    value = read(directory+'/native-result.json')
    if value['failures']: raise RuntimeError('Failed native comparison')
    binary = directory+'/native/'+executable
    value.update(nativeSha256=digest(binary), nativeBytes=(root/binary).stat().st_size,
        nativeFile=run('file', str(root/binary)), nativeDependencies=run('ldd', str(root/binary)), xUnit=counters(directory+'/'+trx))
    return value
data = native_evidence(folder, 'Checker.Validation', 'checker.trx')
objects = read(folder+'/object-native-result.json')
if data['nativeSha256'] != (root/(folder+'/native-sha256-before-execution.txt')).read_text().strip(): raise RuntimeError('Native binary changed during comparisons')
scalar = read(folder+'/scalar-native-result.json')
boundary = read(folder+'/native-boundary.json')
if scalar['failures'] or scalar['cases'] != 45: raise RuntimeError('Scalar regression incomplete')
coverage = read('validation/data-types-corpus/coverage.json')
controls = sum(len(read('validation/'+corpus+'/admission-controls.json')) for corpus in ['checker-corpus','object-checker-corpus','data-types-corpus'])-len(read('validation/data-types-corpus/retired-admission-controls.json'))
if boundary['admissionControls'] != controls or len(boundary['goUtilityObservations']) != 20 or len(boundary['authoredPublicApiAssertions']) != 10: raise RuntimeError('Current boundary evidence incomplete')
if data['cases'] != coverage['cases'] or data['failures'] or objects['cases'] != 43 or objects['failures']: raise RuntimeError('Data/object evidence differs from coverage')
historical = ['validation/results/verified.json','validation/results/parser-verified.json','validation/results/binder-verified.json','validation/results/checker-verified.json','validation/results/object-checker-verified.json']
preserved = []
for path in historical:
    if (root/path).read_bytes() != subprocess.check_output(['git','show','1f7c953:'+path], cwd=root): raise RuntimeError('Historical evidence changed: '+path)
    preserved.append(dict(path=path, sha256=digest(path)))
result = dict(upstreamRevision=read('upstream.lock.json')['revision'], sourceSha256=source,
    sourceHashAlgorithm='SHA256 sorted UTF8 relative path + NUL + raw bytes + NUL for .cs/.csproj under src,tests and all four validation adapters, excluding bin/obj',
    dataTypes=data, anonymousObjectRegression=objects, scalarRegression=scalar, boundary=boundary,
    binderRegression=native_evidence('artifacts/binder-validation','Binder.Validation','binder.trx'),
    parserRegression=native_evidence('artifacts/parser-validation','Parser.Validation','parser.trx'),
    scannerRegression=native_evidence('artifacts/validation','Scanner.Validation','scanner.trx'),
    gate=gate, coverage=dict(path='validation/data-types-corpus/coverage.json', sha256=digest('validation/data-types-corpus/coverage.json'), inventory=coverage),
    corpusManifest=dict(path='validation/data-types-corpus/manifest.json', sha256=digest('validation/data-types-corpus/manifest.json')),
    historicalEvidencePreserved=preserved, intentionalBoundaryChanges=read('validation/data-types-corpus/retired-admission-controls.json'),
    goVersion=run('tools/.cache/go/bin/go','version'), dotnetSdkVersion=run('dotnet','--version'))
path = root/'validation/results/data-types-verified.json'
path.write_text(json.dumps(result, indent=2)+'\n')
print(json.dumps(dict(path=path.relative_to(root).as_posix(), dataTypeCases=data['cases'], checkerTests=data['xUnit']['passed'], scalarCases=scalar['cases'], sourceSha256=source, nativeBytes=data['nativeBytes'])))
