#!/usr/bin/env python3
"""Prepare an emit request registry from accepted decoded original configuration identity.
No Go compiler run, emit evaluation, or expectation generation occurs here.
"""
import argparse, gzip, hashlib, json, pathlib, re

ROOT = pathlib.Path(__file__).resolve().parents[1]
parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('destination', type=pathlib.Path)
args = parser.parse_args()
destination = args.destination.resolve()
destination.mkdir(parents=True, exist_ok=True)
upstream = ROOT / 'upstream/TypeScript/tsc'
inventory = ROOT / 'validation/checker-noemit-decoded-inventory/fixtures.jsonl.gz'
sha = lambda path: hashlib.sha256(path.read_bytes()).hexdigest()
requests, dispositions, configurations = [], [], []
with gzip.open(inventory, 'rt') as stream:
    for row in map(json.loads, stream):
        source = ROOT / row['source']
        if sha(source) != row['sourceSha256']:
            raise ValueError(f'Original source changed: {source}')
        fixture = {'source': row['source'], 'sourceSha256': row['sourceSha256'],
                   'originalPreConfigurationSkip': row['originalPreConfigurationSkip'],
                   'originalConfigurationStatus': row['status'],
                   'originalExpansionFailureText': row.get('originalExpansionFailureText')}
        dispositions.append(fixture)
        for config in row.get('configurations') or []:
            name = source.name + (' ' + config['Name'] if config['Name'] else '')
            requests.append({'Name': name, 'Source': str(source),
                             'SourceSha256': row['sourceSha256'], 'Configuration': config['Name']})
            configured = (source.stem + '(' + config['Name'] + ')' + source.suffix) if config['Name'] else source.name
            configurations.append({'name': name, 'source': row['source'], 'configuration': config['Name'],
                                   'configuredBaselineName': configured, 'classification': 'inventoried-not-executed'})
if len({row['Name'] for row in requests}) != len(requests):
    raise ValueError('Duplicate original configuration identity')
if len({row['configuredBaselineName'] for row in configurations}) != len(configurations):
    raise ValueError('Repeated original baseline names require per-record tracking adaptation')
(destination / 'requests.jsonl').write_text(''.join(json.dumps(row, separators=(',', ':')) + '\n' for row in requests))
(destination / 'fixture-dispositions.json').write_text(json.dumps(dispositions, indent=2) + '\n')
(destination / 'configuration-dispositions.json').write_text(json.dumps(configurations, indent=2) + '\n')
functions = []
for family in ('printer', 'sourcemap', 'transformers', 'compiler', 'execute/tsc'):
    for file in sorted((upstream / 'internal' / family).rglob('*test.go')):
        text = file.read_text()
        for match in re.finditer(r'^func (Test\w+|BenchmarkEmit\w+)\(', text, re.M):
            functions.append({'source': str(file.relative_to(ROOT)), 'sourceSha256': sha(file),
                              'function': match[1], 'line': text[:match.start()].count('\n') + 1,
                              'disposition': 'inventoried-not-executed'})
(destination / 'function-inventory.json').write_text(json.dumps(functions, indent=2) + '\n')
sources = [upstream / 'internal' / name for name in (
    'testrunner/compiler_runner.go', 'testrunner/test_case_parser.go',
    'testutil/harnessutil/harnessutil.go', 'testutil/baseline/baseline.go',
    'testutil/baseline/testmain.go', 'testutil/tsbaseline/js_emit_baseline.go',
    'testutil/tsbaseline/sourcemap_baseline.go', 'testutil/tsbaseline/sourcemap_record_baseline.go')]
sources += [ROOT / 'tools/emit-oracle/runner_test.go', ROOT / 'tools/emit-oracle/baseline_capture.go',
            ROOT / 'tools/checker-noemit-oracle/diagnostics.go']
manifest = {'upstream': json.loads((ROOT / 'upstream.lock.json').read_text())['revision'],
            'baseline': 'e3131a5b', 'classification': 'prepared-original-full-emit-registry-not-executed',
            'decodedInventorySha256': sha(inventory), 'fixtureCount': len(dispositions),
            'configurationCount': len(requests), 'originalFunctionCount': len(functions),
            'sourceFiles': {str(path.relative_to(ROOT)): sha(path) for path in sources},
            'outputs': {name: sha(destination / name) for name in ('requests.jsonl', 'fixture-dispositions.json', 'configuration-dispositions.json', 'function-inventory.json')}}
(destination / 'manifest.json').write_text(json.dumps(manifest, indent=2) + '\n')
print(f'Prepared {len(dispositions)} fixtures and {len(requests)} original configurations; no execution.')
