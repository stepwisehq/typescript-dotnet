#!/usr/bin/env python3
"""Inventory tracked inputs and aggregate direct-test provenance, without executing tests."""
import collections
import hashlib
import json
import pathlib
import re
import subprocess

ROOT = pathlib.Path(__file__).resolve().parent.parent
UPSTREAM = ROOT / 'upstream/TypeScript'
PIN = 'fed0bf24149fb1ed36039212648bafdafc1ea10e'
if subprocess.check_output(['git', 'rev-parse', 'HEAD'], cwd=UPSTREAM, text=True).strip() != PIN:
    raise RuntimeError('Wrong upstream pin')
tracked = subprocess.check_output(['git', 'ls-files'], cwd=UPSTREAM, text=True).splitlines()
manifest = ROOT / 'validation/foundation-corpus/source-inputs.jsonl'
manifest.parent.mkdir(parents=True, exist_ok=True)
counts = collections.Counter()
extensions = collections.Counter()
source_bytes = 0
fuzz_fixtures = []
for path in tracked:
    if path.startswith('tsc/testdata/fixtures/'):
        content = (UPSTREAM/path).read_bytes()
        fuzz_fixtures.append(dict(path=path, bytes=len(content), sha256=hashlib.sha256(content).hexdigest(),
            disposition='additional-differential', originalInvariant='FuzzParser finite fixture seed must not panic for supported extensions',
            extraction='Raw fixture source (original fixture seeding does not use the multi-file splitter)',
            executionStatus='pending'))
with manifest.open('w') as stream:
    for path in tracked:
        match = re.match(r'tsc/testdata/tests/cases/(compiler|conformance|transpile)/', path)
        if not match:
            continue
        content = (UPSTREAM / path).read_bytes()
        counts[match[1]] += 1
        extensions[pathlib.Path(path).suffix] += 1
        source_bytes += len(content)
        stream.write(json.dumps(dict(path=path, suite=match[1], bytes=len(content),
            sha256=hashlib.sha256(content).hexdigest(),
            disposition='input-only-reuse', executionStatus='pending-extraction'), separators=(',', ':')) + '\n')

framework = []
def add(path, name, assertion, disposition, reason, table=None):
    text = (UPSTREAM / path).read_text()
    line = text[:text.index('func ' + name + '(')].count('\n') + 1
    framework.append(dict(path=path, name=name, line=line, kind='test', tableCases=table,
        tableCaseCountBasis='Literal table records' if table is not None else 'Dynamic input/configuration count, not inferred from physical files',
        assertionSummary=assertion, disposition=disposition, reason=reason, evidence=[], executionStatus='not-run'))
add('tsc/internal/testrunner/compiler_runner_test.go', 'TestLocal',
    ['Unique test base filenames; all-program diagnostics/content mapper/JavaScript/declaration/source-map/type-symbol/module-resolution baselines; union ordering; parent pointers'],
    'later-stage-blocked', 'Original compiler assertion suite requires compiler/project/checker/emit beyond foundations. Its physical source inputs are separately reused; parent-pointer invariant is retained by canonical syntax comparison.')
add('tsc/internal/testrunner/transpile_runner_test.go', 'TestTranspile',
    ['Transpile and declaration output plus diagnostic baselines across original configurations'],
    'later-stage-blocked', 'Original output/emit assertions are outside foundations; its 25 physical inputs are separately reused.')
add('tsc/internal/testrunner/test_case_parser_test.go', 'TestMakeUnitsFromTest',
    ['Exact two unit filenames and contents after option stripping; nil config/config-file and allocated-empty symlink map'],
    'excluded', 'Go-only oracle infrastructure, not a required C# foundation API. The original helper itself is used for input extraction; its original assertions will run in Go as an infrastructure check.')
add('tsc/internal/testrunner/compiler_test_filter_test.go', 'TestCompilerTestFileFilter',
    ['28 regex/prefix/configuration/whitespace filter results equal expected inclusion'],
    'excluded', 'Go test-selection CLI implementation, not scanner/parser/syntax behavior.', 28)

support_exclusions = []
for path in tracked:
    if not path.endswith('_test.go') or not (path.startswith('tsc/internal/collections/') or path.startswith('tsc/internal/tspath/')):
        continue
    text = (UPSTREAM / path).read_text()
    for match in re.finditer(r'^func ((Test|Fuzz|Benchmark)\w+)\(', text, re.M):
        name, prefix = match.group(1, 2)
        if name in ('TestGetRootLength','TestIsRootedDiskPath','TestPathIsRelative','TestNormalizeSlashes'):
            continue # Required subset owned by scanner's direct-test inventory.
        support_exclusions.append(dict(path=path, name=name, line=text[:match.start()].count('\n')+1,
            kind=dict(Test='test', Fuzz='fuzz', Benchmark='benchmark')[prefix], tableCases=None,
            tableCaseCountBasis='Not counted: excluded full API; no inferred table coverage',
            disposition='excluded', evidence=[],
            reason=('Ordered-map/set/SyncMap full public APIs are not used by syntax foundations. Parser Set and diagnostic Set use managed HashSet with required state covered by direct assertions and exact syntax/diagnostic comparisons.'
                if '/collections/' in path else
                'Full filesystem/URL/project path algebra is outside source-text foundations. Logical filename/path-key inputs are caller supplied; required declaration-extension/relative-module tests are inventoried in source helpers and differential inputs, not a port of this path package.')))
owners = []
for name in ['foundation-scanner-tests.json', 'foundation-parser-ast-tests.json']:
    path = ROOT / 'docs' / name
    if not path.exists():
        raise RuntimeError('Missing owner inventory: ' + str(path))
    data = json.loads(path.read_text())
    if data['upstreamRevision'] != PIN:
        raise RuntimeError('Wrong owner inventory pin')
    for file in data['files']:
        original_path = file['path'].removeprefix('upstream/TypeScript/')
        if original_path not in tracked:
            raise RuntimeError('Claimed original test source is not tracked: ' + original_path)
        file['path'] = original_path
        file['sha256'] = hashlib.sha256((UPSTREAM/original_path).read_bytes()).hexdigest()
    owners.append(dict(path='docs/' + name, sha256=hashlib.sha256(path.read_bytes()).hexdigest(),
        subsystem=data['subsystem'], files=data['files']))

direct_rows=[dict(path=file['path'],**row) for owner in owners for file in owner['files'] for row in file['testFunctions']]
all_rows=direct_rows+framework+support_exclusions
identities=[(row['path'],row['name']) for row in all_rows]
if len(identities)!=len(set(identities)):
    raise RuntimeError('Duplicate original test function disposition')
function_summary=dict(total=len(all_rows),directSubsystemFunctions=len(direct_rows),
    byDisposition=dict(collections.Counter(row['disposition'] for row in all_rows)),
    byKind=dict(collections.Counter(row['kind'] for row in all_rows)),
    directByDisposition=dict(collections.Counter(row['disposition'] for row in direct_rows)))
source_manifest=ROOT/'validation/foundation-corpus/source-manifest.json'
extracted=json.loads(source_manifest.read_text()) if source_manifest.exists() else None

catalog = dict(upstreamRevision=PIN, baseline='b6b22b9', executionStatus='direct-ports-in-progress',
    countingRules=['Count original Go test/fuzz/benchmark functions separately from table rows and extracted source units.',
        'Assertion-preserving ports include equivalent byte-string/byte-array and API representation translations.',
        'Adapted assertions change what is established, e.g. subset assertions or excluded original cases; adaptations are explicit.',
        'Input reuse and additional exact differential assertions do not establish original compiler baselines.',
        'Pending dispositions/implementations are not passing evidence.'],
    dispositions={
        'assertion-preserving-port':'All original success assertions for selected original cases remain equivalent, with representation notes.',
        'adapted-assertions':'Original assertion or case scope changed; exact difference recorded.',
        'input-only-reuse':'Original input, new syntax/scanner assertions; no original later-stage baseline claim.',
        'additional-differential':'Authored inputs/observations independently compared against the pin.',
        'excluded':'Outside fixed foundation surface, with concrete dependency reason.',
        'later-stage-blocked':'Original assertions depend on uncompleted compiler/checker/project/emit behavior.',
        'unresolved':'Not yet classified; blocks final acceptance.'},
    originalFunctionSummary=function_summary,
    directSubsystemInventories=owners, frameworkTests=framework, sharedPackageExclusions=support_exclusions,
    setupFunctions=[dict(path='tsc/internal/testrunner/testmain_test.go', name='TestMain', reason='Test package setup, not a correctness test function.')],
    broaderInputs=dict(physicalFiles=dict(counts), physicalBytes=source_bytes, physicalExtensions=dict(extensions),
        manifest=manifest.relative_to(ROOT).as_posix(), manifestSha256=hashlib.sha256(manifest.read_bytes()).hexdigest(),
        sourceUnits=extracted['counts']['extractedUnits'] if extracted else None, sourceUnitCountBasis='Actual pinned multi-file/options splitter plus raw fixture inputs; physical file/marker counts are not unit counts.',
        extractionManifest='validation/foundation-corpus/source-manifest.json' if extracted else None,
        originalAssertionsDisposition='later-stage-blocked', syntaxInputDisposition='input-only-reuse',
        nativeExecutionStatus='pending'),
    otherSuites=[dict(path='tsc/internal/fourslash/tests/', trackedGoTestFiles=sum(path.startswith('tsc/internal/fourslash/tests/') and path.endswith('_test.go') for path in tracked),
        disposition='later-stage-blocked', reason='Language-service editor assertions; outside foundation surface. Not included in selected compiler/conformance/transpile input campaign.'),
        dict(path='tsc/testdata/baselines/reference/', disposition='later-stage-blocked', reason='Mixed full compiler/emit/type/symbol/module baselines, not canonical syntax-tree expected output.')],
    finiteFuzzFixtureSeeds=fuzz_fixtures)
(ROOT / 'docs/foundation-test-catalog.json').write_text(json.dumps(catalog, indent=2) + '\n')
print(json.dumps(dict(physicalFiles=dict(counts), physicalBytes=source_bytes,
    ownerFunctions=sum(len(file['testFunctions']) for owner in owners for file in owner['files']),
    frameworkFunctions=len(framework), excludedSharedFunctions=len(support_exclusions))))
