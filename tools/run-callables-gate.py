#!/usr/bin/env python3
"""Guard one composed callable/native/regression gate; never replace historical proof."""
import argparse, collections, datetime, hashlib, json, pathlib, subprocess, sys, time
import xml.etree.ElementTree as ET

ROOT = pathlib.Path(__file__).resolve().parent.parent
OUT = ROOT / 'artifacts/callables-validation'
BASELINE = 'de855ca'
NATIVE_PATHS = [
    'artifacts/data-types-validation/native/Checker.Validation',
    'artifacts/binding-completion-validation/native/BindingCompletion.Validation',
    'artifacts/foundation-validation/native/Foundation.Validation',
    'artifacts/binder-validation/native/Binder.Validation',
    'artifacts/parser-validation/native/Parser.Validation',
    'artifacts/validation/native/Scanner.Validation',
]

def sources():
    return subprocess.check_output([sys.executable, 'tools/foundation-source-hash.py'], cwd=ROOT, text=True).strip()

def assets():
    files = [ROOT / 'global.json', ROOT / 'upstream.lock.json']
    for base in [ROOT / 'tools', ROOT / 'validation/corpus', *(ROOT / 'validation').glob('*-corpus')]:
        files.extend(p for p in base.rglob('*') if p.is_file() and
            not ({'.cache', 'bin', 'obj', '__pycache__'} & set(p.relative_to(ROOT).parts)) and
            p.name not in ('source-requests.jsonl', 'source-units.jsonl'))
    return sorted(set(files))

def fingerprint(files):
    digest = hashlib.sha256()
    for path in files:
        digest.update(path.relative_to(ROOT).as_posix().encode() + b'\0' + path.read_bytes() + b'\0')
    return digest.hexdigest()

def native_snapshot():
    return {name: {'sha256': hashlib.sha256((ROOT / name).read_bytes()).hexdigest(),
                   'sizeBytes': (ROOT / name).stat().st_size} for name in NATIVE_PATHS}

def historical():
    names = subprocess.check_output(['git', 'ls-tree', '-r', '--name-only', BASELINE, 'validation'], cwd=ROOT, text=True).splitlines()
    selected = [name for name in names if pathlib.PurePosixPath(name).parts[1] in ('corpus', 'results') or pathlib.PurePosixPath(name).parts[1].endswith('-corpus')]
    result = {}
    for name in selected:
        pinned = subprocess.check_output(['git', 'show', BASELINE + ':' + name], cwd=ROOT)
        if (ROOT / name).read_bytes() != pinned:
            raise RuntimeError('Historical fixture/proof differs from baseline: ' + name)
        result[name] = hashlib.sha256(pinned).hexdigest()
    return result

def read(name):
    return json.loads((ROOT / name).read_text())

def check_execution():
    manifest = read('validation/callables-corpus/manifest.json')
    report = read('artifacts/callables-validation/native/report.json')
    if report['groups'] != [g['group'] for g in manifest['groups']] or report['expectedRequests'] != manifest['cases'] or report['counts']['requests'] != manifest['cases'] or report['counts']['exact'] != manifest['cases'] or report['failures']:
        raise RuntimeError('Incomplete/failed native callable comparison')
    boundary = read('artifacts/callables-validation/native-boundary.json')
    controls = read('validation/callables-corpus/admission-controls.json')
    if boundary['admissionControls'] != len(controls) or boundary['admitted'] != sum(c['expectedFeature'] is None for c in controls) or boundary['rejected'] != sum(c['expectedFeature'] is not None for c in controls) or boundary['apiControls'] != 10:
        raise RuntimeError('Incomplete callable admission/API assertions')
    order = read('artifacts/callables-validation/native-order/report.json')
    if order['orderControlCases'] != 8 or order['sequencesPerRuntime'] != 2 or order['originalGoExact'] != 21 or order['targetExact'] != 21:
        raise RuntimeError('Incomplete callable process/order controls')
    numeric = read('artifacts/callables-validation/native-numeric.json')
    provenance = read('validation/callables-corpus/numeric-provenance.json')
    fixture = ROOT / 'validation/callables-corpus/numeric-fixtures.json'
    source = ROOT / 'upstream/TypeScript/tsc' / provenance['source']
    if hashlib.sha256(fixture.read_bytes()).hexdigest() != provenance['fixtureSha256'] or hashlib.sha256(source.read_bytes()).hexdigest() != provenance['sourceSha256']:
        raise RuntimeError('Original numeric assertion provenance mismatch')
    numeric_count = provenance['tableRows'] + provenance['nodeAssertions']
    if provenance['originalFunctions'] != 10 or len(numeric) != numeric_count or any(item['boolValue'] is not True for item in numeric):
        raise RuntimeError('Incomplete original/Node native numeric assertions')
    namespace = '{http://microsoft.com/schemas/VisualStudio/TeamTest/2010}'
    tree = ET.parse(ROOT / 'artifacts/data-types-validation/checker.trx')
    counter = {key: int(value) for key, value in tree.find('.//' + namespace + 'Counters').attrib.items()}
    if counter['failed'] or counter['passed'] != counter['total']:
        raise RuntimeError('Failed/skipped checker xUnit')
    classes = collections.Counter()
    for row in tree.findall('.//' + namespace + 'UnitTest'):
        method = row.find(namespace + 'TestMethod')
        if method is not None:
            classes[method.attrib['className'].split('.')[-1]] += 1
    if classes['CallableOracleTests'] != manifest['cases'] or classes['CallableBoundaryTests'] != len(controls) + 3:
        raise RuntimeError('Incomplete callable xUnit registry')
    if classes['CallableNumericTests'] != numeric_count:
        raise RuntimeError('Incomplete original/Node xUnit numeric registry')
    before = read('artifacts/callables-validation/native-before-callables.json')
    if before != native_snapshot():
        raise RuntimeError('Native binary changed during callable execution')
    (OUT / 'native-after-callables.json').write_text(json.dumps(native_snapshot(), indent=2) + '\n')

def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--capture-native')
    parser.add_argument('--check-execution', action='store_true')
    args = parser.parse_args()
    if args.capture_native:
        (ROOT / args.capture_native).write_text(json.dumps(native_snapshot(), indent=2) + '\n')
        return
    if args.check_execution:
        check_execution()
        return
    OUT.mkdir(parents=True, exist_ok=True)
    source = sources(); files = assets(); asset = fingerprint(files); protected = historical()
    command = ['bash', 'tools/verify-callables.sh']
    start = time.monotonic(); utc = datetime.datetime.now(datetime.timezone.utc).isoformat()
    print('Final callable gate log: ' + str(OUT / 'final-gate.log'), flush=True)
    with (OUT / 'final-gate.log').open('w') as log:
        result = subprocess.run(command, cwd=ROOT, stdout=log, stderr=subprocess.STDOUT)
    source_drift = source != sources()
    asset_drift = files != assets() or asset != fingerprint(files)
    historical_drift = any(not (ROOT / name).is_file() or hashlib.sha256((ROOT / name).read_bytes()).hexdigest() != digest for name, digest in protected.items())
    status = result.returncode or int(source_drift or asset_drift or historical_drift)
    report = dict(command=command, baseline=BASELINE, startedAtUTC=utc,
        elapsedSeconds=round(time.monotonic()-start, 3), exitCode=status,
        sourceSha256=source, verificationAssetsSha256=asset, sourceDrift=source_drift,
        assetDrift=asset_drift, historicalAssetDrift=historical_drift,
        verificationAssetFiles=[p.relative_to(ROOT).as_posix() for p in files],
        protectedHistoricalAssetFiles=protected, log='artifacts/callables-validation/final-gate.log')
    (OUT / 'final-gate.json').write_text(json.dumps(report, indent=2) + '\n')
    print(json.dumps({k: v for k, v in report.items() if k not in ('verificationAssetFiles', 'protectedHistoricalAssetFiles')}), flush=True)
    raise SystemExit(status)

if __name__ == '__main__':
    main()
