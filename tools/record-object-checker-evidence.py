#!/usr/bin/env python3
"""Record current object acceptance separately from every historical checkpoint."""
import hashlib, json, pathlib, subprocess, sys, xml.etree.ElementTree as ET
root = pathlib.Path(__file__).resolve().parent.parent
folder = 'artifacts/object-checker-validation'
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
objects = native_evidence(folder, 'Checker.Validation', 'checker.trx')
if objects['nativeSha256'] != (root/(folder+'/native-sha256-before-execution.txt')).read_text().strip(): raise RuntimeError('Native binary changed during comparisons')
scalar = read(folder+'/scalar-native-result.json')
boundary = read(folder+'/native-boundary.json')
if scalar['failures'] or scalar['cases'] != 45: raise RuntimeError('Scalar regression incomplete')
if boundary['admissionControls'] != 58 or boundary['retiredHistoricalAdmissionControls'] != ['object','object-type'] or len(boundary['goUtilityObservations']) != 20 or len(boundary['authoredPublicApiAssertions']) != 10: raise RuntimeError('Current boundary evidence incomplete')
coverage = read('validation/object-checker-corpus/coverage.json')
if objects['cases'] != coverage['cases'] or objects['cases'] != 43: raise RuntimeError('Object coverage differs from execution')
historical = ['validation/results/verified.json','validation/results/parser-verified.json','validation/results/binder-verified.json','validation/results/checker-verified.json']
preserved = []
for path in historical:
    if (root/path).read_bytes() != subprocess.check_output(['git','show','7e692dd:'+path], cwd=root): raise RuntimeError('Historical evidence changed: '+path)
    preserved.append(dict(path=path, sha256=digest(path)))
result = dict(upstreamRevision=read('upstream.lock.json')['revision'], sourceSha256=source,
    sourceHashAlgorithm='SHA256 sorted UTF8 relative path + NUL + raw bytes + NUL for .cs/.csproj under src,tests and all four validation adapters, excluding bin/obj',
    objectChecker=objects, scalarRegression=scalar, boundary=boundary,
    binderRegression=native_evidence('artifacts/binder-validation','Binder.Validation','binder.trx'),
    parserRegression=native_evidence('artifacts/parser-validation','Parser.Validation','parser.trx'),
    scannerRegression=native_evidence('artifacts/validation','Scanner.Validation','scanner.trx'),
    gate=gate, coverage=dict(path='validation/object-checker-corpus/coverage.json', sha256=digest('validation/object-checker-corpus/coverage.json'), inventory=coverage),
    corpusManifest=dict(path='validation/object-checker-corpus/manifest.json', sha256=digest('validation/object-checker-corpus/manifest.json')),
    historicalEvidencePreserved=preserved, intentionalBoundaryChanges=[dict(control='object', reason='Anonymous object literals supported; new empty/basic cases replace historical negative'),dict(control='object-type', reason='Anonymous type literals supported; new empty/basic cases replace historical negative')],
    goVersion=run('tools/.cache/go/bin/go','version'), dotnetSdkVersion=run('dotnet','--version'))
path = root/'validation/results/object-checker-verified.json'
path.write_text(json.dumps(result, indent=2)+'\n')
print(json.dumps(dict(path=path.relative_to(root).as_posix(), objectCases=objects['cases'], checkerTests=objects['xUnit']['passed'], scalarCases=scalar['cases'], sourceSha256=source, nativeBytes=objects['nativeBytes'])))
