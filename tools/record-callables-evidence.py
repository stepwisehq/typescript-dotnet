#!/usr/bin/env python3
"""Persist successful current-source callable evidence without touching prior proofs."""
import collections, datetime, hashlib, importlib.util, json, pathlib, subprocess
import xml.etree.ElementTree as ET

ROOT = pathlib.Path(__file__).resolve().parent.parent
OUT = ROOT / 'artifacts/callables-validation'

def read(name):
    return json.loads((ROOT / name).read_text())

def sha(name):
    return hashlib.sha256((ROOT / name).read_bytes()).hexdigest()

def main():
    spec = importlib.util.spec_from_file_location('callables_gate', ROOT / 'tools/run-callables-gate.py')
    gate_module = importlib.util.module_from_spec(spec); spec.loader.exec_module(gate_module)
    gate = read('artifacts/callables-validation/final-gate.json')
    if gate['exitCode'] or any(gate[key] for key in ('sourceDrift', 'assetDrift', 'historicalAssetDrift')):
        raise RuntimeError('Callable gate failed or drifted')
    if gate['sourceSha256'] != gate_module.sources() or gate['verificationAssetsSha256'] != gate_module.fingerprint(gate_module.assets()):
        raise RuntimeError('Current source/assets differ from successful gate')
    gate_module.check_execution()
    historical = gate_module.historical()
    if historical != gate['protectedHistoricalAssetFiles']:
        raise RuntimeError('Historical fixture/proof set differs from gate')
    tests = {}; namespace = '{http://microsoft.com/schemas/VisualStudio/TeamTest/2010}'
    for name, path in [('scanner', 'artifacts/validation/scanner.trx'), ('parser', 'artifacts/parser-validation/parser.trx'), ('binder', 'artifacts/binder-validation/binder.trx'), ('checker', 'artifacts/data-types-validation/checker.trx')]:
        tree = ET.parse(ROOT / path)
        counts = {key: int(value) for key, value in tree.find('.//' + namespace + 'Counters').attrib.items()}
        if counts['failed'] or counts['passed'] != counts['total']:
            raise RuntimeError('Failed/skipped xUnit: ' + name)
        classes = collections.Counter()
        for row in tree.findall('.//' + namespace + 'UnitTest'):
            method = row.find(namespace + 'TestMethod')
            if method is not None:
                classes[method.attrib['className'].split('.')[-1]] += 1
        tests[name] = dict(counts=counts, classCaseCounts=dict(classes), trx=path, trxSha256=sha(path))
    binaries = gate_module.native_snapshot()
    for path, data in binaries.items():
        data['file'] = subprocess.check_output(['file', str(ROOT / path)], text=True).strip()
        if 'ELF 64-bit' not in data['file'] or 'x86-64' not in data['file']:
            raise RuntimeError('Expected actual Linux x64 native executable: ' + path)
        linked = subprocess.run(['ldd', str(ROOT / path)], text=True, stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
        data['dependencies'] = linked.stdout.strip()
        if 'libcoreclr' in data['dependencies']:
            raise RuntimeError('Unexpected managed runtime dependency: ' + path)
    regressions = {key: read(path) for key, path in {
        'bindingTargeted': 'artifacts/binding-completion-validation/targeted-native/report.json',
        'bindingBroad': 'artifacts/binding-completion-validation/broad-native/report.json',
        'bindingSharedCases': 'artifacts/binding-completion-validation/native-counts.json',
        'originalGoNumericFinite': 'artifacts/binding-completion-validation/original-go-numeric-finite.json',
        'foundationDirect': 'artifacts/foundation-validation/native-direct.json',
        'foundationFiniteFuzz': 'artifacts/foundation-validation/native-fuzz.json',
        'foundationBroadSyntax': 'artifacts/foundation-validation/broad-parser-native/report.json',
        'foundationBroadScanner': 'artifacts/foundation-validation/broad-scanner-native/report.json',
        'historicalBinder': 'artifacts/binder-validation/native-result.json',
        'dataTypes': 'artifacts/data-types-validation/native-result.json',
        'objects': 'artifacts/data-types-validation/object-native-result.json',
        'scalars': 'artifacts/data-types-validation/scalar-native-result.json',
        'legacyControls': 'artifacts/data-types-validation/native-boundary.json',
    }.items()}
    manifest = read('validation/callables-corpus/manifest.json')
    oracle = {}
    for group in manifest['groups']:
        name = group['group']; current = 'artifacts/callables-validation/' + name + '-original-go-current.jsonl'
        expected = 'validation/callables-corpus/' + name + '-expected.jsonl'
        if (ROOT / current).read_bytes() != (ROOT / expected).read_bytes():
            raise RuntimeError('Current original-Go callable outputs differ: ' + name)
        oracle[name] = dict(cases=group['cases'], current=current, currentSha256=sha(current), expectedSha256=sha(expected))
    result = dict(stage='non-generic-callables', baseline=gate_module.BASELINE,
        upstreamRevision=read('upstream.lock.json')['revision'],
        recordedAtUTC=datetime.datetime.now(datetime.timezone.utc).isoformat(),
        command=gate['command'], exitCode=0, elapsedSeconds=gate['elapsedSeconds'],
        sourceSha256=gate['sourceSha256'], verificationAssetsSha256=gate['verificationAssetsSha256'],
        sourceDrift=False, assetDrift=False, historicalAssetDrift=False,
        log=gate['log'], logSha256=sha(gate['log']),
        nativeBinaries=binaries, nativeFingerprintScope='All six freshly published cascade binaries were captured after the historical cascade and before callable execution, then checked afterward. Earlier cascade executions retain their own pre/post guards.',
        xUnit=tests, xUnitCases=sum(row['counts']['total'] for row in tests.values()),
        callableNative=read('artifacts/callables-validation/native/report.json'),
        callableControls=read('artifacts/callables-validation/native-boundary.json'),
        callableOrderControls=read('artifacts/callables-validation/native-order/report.json'),
        originalGoCallable=oracle,
        originalArithmetic=dict(provenance=read('validation/callables-corpus/numeric-provenance.json'), nativeAssertions=read('artifacts/callables-validation/native-numeric.json'), originalGoLog='artifacts/callables-validation/original-go-arithmetic.log', originalGoLogSha256=sha('artifacts/callables-validation/original-go-arithmetic.log')),
        regressions=regressions,
        corpusManifest='validation/callables-corpus/manifest.json', corpusManifestSha256=sha('validation/callables-corpus/manifest.json'),
        contract='docs/CALLABLES-CONTRACT.md',
        limits=['Single-file, non-generic TS script capability under the explicit Callable option profile; this is not whole-checker parity.', 'Legacy admission/options are retained and separately regressed; Callable is strictNullChecks, strictFunctionTypes, noImplicitAny and noImplicitThis, not strict-all.', 'Rest/arrays/tuples, source generics, construct/class/async/module/library/emit families remain outside admission.', 'Callable corpora are authored independent-Go differentials and API/admission assertions, not full compiler-test assertion ports.'])
    destination = ROOT / 'validation/results/callables-verified.json'
    destination.write_text(json.dumps(result, indent=2) + '\n')
    print(json.dumps(dict(evidence=str(destination.relative_to(ROOT)), sourceSha256=result['sourceSha256'], xUnitCases=result['xUnitCases'], elapsedSeconds=result['elapsedSeconds'])))

if __name__ == '__main__':
    main()
