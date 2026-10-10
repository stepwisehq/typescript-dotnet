#!/usr/bin/env python3
"""Inventory all pinned compiler configurations using the original Go producers.

This enumerates obligations; it does not execute or claim compiler assertions.
"""
import argparse
import collections
import gzip
import hashlib
import json
import os
from pathlib import Path
import subprocess

ROOT = Path(__file__).resolve().parents[1]
UPSTREAM = ROOT / 'upstream/TypeScript/tsc'
OUT = ROOT / 'artifacts/protocols-validation/compiler-inventory'
DEST = ROOT / 'validation/checker-noemit-inventory'
PIN = 'fed0bf24149fb1ed36039212648bafdafc1ea10e'

def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()

def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--check', action='store_true')
    args = parser.parse_args()
    if subprocess.check_output(['git', '-C', str(ROOT/'upstream/TypeScript'), 'rev-parse', 'HEAD'], text=True).strip() != PIN:
        raise RuntimeError('Upstream pin changed')
    OUT.mkdir(parents=True, exist_ok=True)
    DEST.mkdir(parents=True, exist_ok=True)
    observer = ROOT/'tools/compiler-noemit-inventory/inventory_test.go'
    # An additive test file only; no existing production or test file is replaced.
    overlay = {'Replace': {str(UPSTREAM/'internal/testrunner/authored_noemit_inventory_test.go'): str(observer)}}
    overlay_path = OUT/'overlay.json'
    overlay_path.write_text(json.dumps(overlay, indent=2)+'\n')
    command = [str(ROOT/'tools/.cache/go/bin/go'), 'test', '-overlay='+str(overlay_path), '-json',
               './internal/testrunner', '-run', '^TestAuthoredCompilerNoEmitInventory$', '-count=1']
    env = os.environ | {'COMPILER_NOEMIT_INVENTORY_OUTPUT': str(OUT/'original.jsonl')}
    with (OUT/'original-test.jsonl').open('wb') as stdout, (OUT/'original-test.stderr').open('wb') as stderr:
        result = subprocess.run(command, cwd=UPSTREAM, env=env, stdout=stdout, stderr=stderr)
    if result.returncode:
        raise RuntimeError('Original inventory producer failed; preserve partial output and test log')
    rows = [json.loads(line) for line in (OUT/'original.jsonl').read_bytes().splitlines()]
    baselines = {}
    for suite in ('compiler','conformance'):
        for path in sorted((UPSTREAM/'testdata/baselines/reference'/suite).glob('*')):
            suffix = next((s for s in ('.errors.txt','.types','.symbols') if path.name.endswith(s)), None)
            if suffix:
                baselines.setdefault((suite,path.name[:-len(suffix)]), []).append(path)
    for row in rows:
        source = Path(row['source'])
        row['source'] = source.relative_to(ROOT).as_posix()
        if row['sourceSha256'] != sha(source):
            raise RuntimeError('Input changed during inventory')
        stem = source.stem
        variants = row.pop('configurations')
        row['configurations'] = [] if variants is None else variants
        names = [stem]
        for config in row['configurations']:
            configured = stem+('('+config['Name']+')' if config['Name'] else '')
            names.append(configured)
            values = config['Config'] or {}
            # Original emit/declaration flags remain inputs. Operations are scoped,
            # never disguised by setting noEmit or changing libraries/options.
            config['noEmitObligations'] = {
                'diagnostics': 'required-unexecuted: config/Program/syntactic/semantic/global; original diagnostic ordering and complete ownership/text',
                'suggestions': 'required-when-original-captureSuggestions: unexecuted',
                'declarationDiagnostics': 'conditional GetEmitDeclarations invokes Program.GetDeclarationDiagnostics declaration-output producer: external emit/declaration diagnostic operation; ordinary checker diagnostics under original declaration options remain required',
                'typesAndSymbols': 'original-harness-disabled' if values.get('notypesandsymbols','').lower()=='true' else 'required-unexecuted: original public type/symbol baseline producer',
                'contentMapper': 'required-when-original-mapped-files: unexecuted; process fixture dependency separately classified',
                'moduleResolution': 'required-when-original-traceResolution: unexecuted',
                'emitAndSourceMaps': 'external output operation; original options retained and pre-emit checker assertions required separately',
                'postEmitDiagnosticInvariant': 'external original Emit-induced diagnostics and pre/post-emit count invariant; no claim these execute in a no-emit comparison',
                'unionOrderingAndParentPointers': 'original invariant assertions inventoried; private representation parity is not a completion requirement',
            }
        row['baselineSourceFiles'] = {path.relative_to(ROOT).as_posix():sha(path)
            for name in sorted(set(names)) for path in baselines.get((row['suite'],name),[])}
        if row['upstreamDisposition']=='skipped-before-configurations':
            row['noEmitDisposition']='original-upstream-skip-not-a-pass; final checker applicability remains explicit'
            row['checkerObligation']='API sample source checking depends on absent original built /.ts declarations; API runtime/watch/emit calls are external, not a reason to erase source checking' if row['upstreamReason'].startswith('requires') else 'original removed-option pin disposition; no successful checking/configuration claim'
        else:
            row['noEmitDisposition']='inventoried-unexecuted; final whole-checker gate must close every applicable configuration'
    data=b''.join((json.dumps(row,ensure_ascii=False,separators=(',',':'))+'\n').encode() for row in rows)
    compressed=gzip.compress(data,mtime=0)
    files=['tools/generate-checker-noemit-inventory.py','tools/compiler-noemit-inventory/inventory_test.go','upstream/TypeScript/tsc/internal/testrunner/compiler_runner.go',
           'upstream/TypeScript/tsc/internal/testrunner/compiler_runner_test.go','upstream/TypeScript/tsc/internal/testrunner/test_case_parser.go',
           'upstream/TypeScript/tsc/internal/testrunner/options_generated.go','upstream/TypeScript/tsc/internal/testutil/harnessutil/harnessutil.go',
           'upstream/TypeScript/tsc/internal/testutil/tsbaseline/type_symbol_baseline.go','upstream/TypeScript/tsc/internal/testutil/tsbaseline/error_baseline.go']
    counts={'inputs':len(rows),'suites':dict(collections.Counter(r['suite'] for r in rows)),
            'configurations':sum(len(r['configurations']) for r in rows),
            'originalPreConfigurationSkips':sum(r['upstreamDisposition']=='skipped-before-configurations' for r in rows),
            'extractionErrors':sum(r['extractionError'] is not None for r in rows)}
    provenance={'baseline':'bb035b3','upstream':PIN,'classification':'complete-original-compiler-harness-inventory-not-runtime-coverage',
        **counts,'inventory':'validation/checker-noemit-inventory/fixtures.jsonl.gz','inventorySha256':hashlib.sha256(compressed).hexdigest(),
        'uncompressedSha256':hashlib.sha256(data).hexdigest(),'sourceFiles':{name:sha(ROOT/name) for name in files},
        'observationBoundary':'Original compiler/conformance enumeration, settings extraction, file/configuration expansion and unit extraction. Canonical configuration presentation sorts names only; no search/diagnostic/type ordering changes. No original source replaced; one authored Go test file overlay.',
        'optionPolicy':'Do not force noEmit/skip flags/library substitutions. Final semantic and type/symbol assertions remain pending; output writing is a separate operation.',
        'originalSkipPolicy':'42 original pre-configuration skips retained as dispositions, not passes or automatic whole-checker exclusions. Built-API fixture source obligations and removed options require separate final accounting.'}
    payload=(json.dumps(provenance,indent=2)+'\n').encode()
    if args.check:
        if (DEST/'fixtures.jsonl.gz').read_bytes()!=compressed or (DEST/'provenance.json').read_bytes()!=payload:
            raise RuntimeError('Inventory reproduction differs')
    else:
        (DEST/'fixtures.jsonl.gz').write_bytes(compressed)
        (DEST/'provenance.json').write_bytes(payload)
    (OUT/'report.json').write_text(json.dumps({'command':command,'exitCode':0,**counts,
        'originalOutputSha256':sha(OUT/'original.jsonl'),'testLogSha256':sha(OUT/'original-test.jsonl'),
        'testStderrSha256':sha(OUT/'original-test.stderr'),'inventorySha256':provenance['inventorySha256']},indent=2)+'\n')
    print(json.dumps(counts))

if __name__=='__main__':main()
