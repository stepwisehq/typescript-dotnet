#!/usr/bin/env python3
"""Exact existing callable expectations under changed process-global allocation order."""
import argparse, hashlib, json, pathlib, subprocess, time

ROOT = pathlib.Path(__file__).resolve().parents[1]
NAMES = ['C3-predicate-typeof', 'C3-self-recursive-return',
         'C4-hidden-this-instantiated-call', 'C4-union-different-params',
         'C4-merged-hidden-this-cache', 'C4-merged-hidden-this-cache-reversed',
         'C5-global-symbol-identity-widening', 'C5-global-symbol-for-identity']


def canonical(value):
    # Object member serialization order is irrelevant; JSON scalar types and every
    # array order remain exact, including null versus allocated-empty arrays.
    return json.dumps(value, sort_keys=True, separators=(',', ':'))


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--executable', required=True)
    parser.add_argument('--managed', action='store_true')
    parser.add_argument('--output', required=True)
    args = parser.parse_args()
    out = pathlib.Path(args.output); out.mkdir(parents=True, exist_ok=True)
    corpus = ROOT / 'validation/callables-corpus'
    manifest = json.loads((corpus / 'manifest.json').read_text())
    rows = {}; tiny = []
    for group in manifest['groups']:
        paths = [(corpus / (group['group'] + '-requests.jsonl'), 'requestsSha256'),
                 (corpus / (group['group'] + '-expected.jsonl'), 'expectedSha256')]
        for path, key in paths:
            if hashlib.sha256(path.read_bytes()).hexdigest() != group[key]:
                raise RuntimeError('Corpus digest mismatch: ' + str(path))
        requests = paths[0][0].read_text().splitlines()
        expected = paths[1][0].read_text().splitlines()
        if len(requests) != group['cases'] or len(expected) != group['cases']:
            raise RuntimeError('Corpus count mismatch')
        for request, want in zip(requests, expected):
            name = json.loads(request)['name']
            if name in rows: raise RuntimeError('Duplicate callable name')
            rows[name] = (request, canonical(json.loads(want)))
            if group['group'] == 'tiny': tiny.append(name)
    if len(rows) != manifest['cases']: raise RuntimeError('Manifest total mismatch')
    sequences = [('initial', NAMES), ('after-prefix-reversed', tiny + list(reversed(NAMES)))]
    commands = [('originalGo', [str(ROOT / 'tools/.cache/checker-oracle')]),
                ('target', (['dotnet'] if args.managed else []) + [args.executable])]
    started = time.monotonic(); records = []
    for runtime, command in commands:
        for label, names in sequences:
            raw = '\n'.join(rows[name][0] for name in names) + '\n'
            process = subprocess.run(command, input=raw, text=True, capture_output=True)
            path = out / (runtime + '-' + label + '.jsonl')
            path.write_text(process.stdout)
            (out / (runtime + '-' + label + '.stderr')).write_text(process.stderr)
            if process.returncode: raise RuntimeError(f'{runtime}/{label} exited {process.returncode}')
            lines = process.stdout.splitlines()
            if len(lines) != len(names): raise RuntimeError(f'{runtime}/{label} incomplete output')
            for name, line in zip(names, lines):
                if canonical(json.loads(line)) != rows[name][1]:
                    raise RuntimeError(f'{runtime}/{label} differs from original expectation: {name}; retained {path}')
            records.append(dict(runtime=runtime, order=label, names=names, exact=len(names),
                                output=str(path), sha256=hashlib.sha256(path.read_bytes()).hexdigest()))
    report = dict(orderControlCases=len(NAMES), sequencesPerRuntime=len(sequences),
                  originalGoExact=sum(r['exact'] for r in records if r['runtime']=='originalGo'),
                  targetExact=sum(r['exact'] for r in records if r['runtime']=='target'),
                  commands=dict(commands), records=records,
                  elapsedSeconds=round(time.monotonic()-started, 3),
                  comparison='Exact existing full graph expectations in original and reversed-after-prefix request orders; no expected regeneration or field normalization.')
    (out / 'report.json').write_text(json.dumps(report, indent=2) + '\n')
    print(json.dumps({k:v for k,v in report.items() if k not in ('commands','records')}, separators=(',', ':')))


if __name__ == '__main__':
    main()
