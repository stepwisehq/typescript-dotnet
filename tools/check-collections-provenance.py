#!/usr/bin/env python3
"""Compare uninstrumented behavior with metadata-only overlays, without storage reads."""
import argparse
import gzip
import hashlib
import json
from pathlib import Path
import subprocess
import time

ROOT = Path(__file__).resolve().parents[1]
p = argparse.ArgumentParser(description=__doc__)
source = p.add_mutually_exclusive_group(required=True)
source.add_argument('--manifest', type=Path)
source.add_argument('--requests', type=Path)
p.add_argument('--uninstrumented', required=True)
p.add_argument('--instrumented', required=True)
p.add_argument('--output', type=Path, required=True)
args = p.parse_args()
block = json.loads(args.manifest.read_text())['instrumentationParity'] if args.manifest else None
requests = ROOT / block['requests'] if block else args.requests
raw = requests.read_bytes()
digest = hashlib.sha256(raw).hexdigest()
rows = [json.loads(line) for line in raw.decode().splitlines()]
if not rows or len({r['name'] for r in rows}) != len(rows):
    raise RuntimeError('Empty or duplicate parity requests')
if block and (block['cases'] != len(rows) or block['requestsSha256'] != digest):
    raise RuntimeError('Parity manifest count/hash mismatch')
# The same semantic operations run in both originals. Authored graph reads which
# require producer metadata are deliberately disabled, rather than swallowed.
for row in rows:
    row['storedObservations'] = False
payload = ''.join(json.dumps(r, separators=(',', ':')) + '\n' for r in rows)
args.output.mkdir(parents=True, exist_ok=True)
start = time.monotonic()
results = []
for label, executable in [('uninstrumented', args.uninstrumented), ('instrumented', args.instrumented)]:
    run = subprocess.run([executable], input=payload, text=True, capture_output=True, timeout=600)
    (args.output / (label + '.jsonl.gz')).write_bytes(gzip.compress(run.stdout.encode(), mtime=0))
    (args.output / (label + '.stderr')).write_text(run.stderr)
    if run.returncode:
        raise RuntimeError(f'{label} exited {run.returncode}; evidence retained')
    lines = run.stdout.splitlines()
    if len(lines) != len(rows):
        raise RuntimeError(f'{label} incomplete/extra replies')
    results.append(lines)
failures = []
for row, left, right in zip(rows, *results):
    # Byte equality also establishes exact scalar types, nulls and ordering.
    if left != right:
        failures.append({'name': row['name'], 'uninstrumentedSha256': hashlib.sha256(left.encode()).hexdigest(),
                         'instrumentedSha256': hashlib.sha256(right.encode()).hexdigest()})
report = {'cases': len(rows), 'exact': len(rows) - len(failures), 'requestsSha256': digest,
          'failures': failures, 'elapsedSeconds': round(time.monotonic() - start, 3),
          'comparison': 'Complete uninstrumented/instrumented behavior bytes; storedObservations=false',
          'binarySha256': {label: hashlib.sha256(Path(path).read_bytes()).hexdigest()
                           for label, path in [('uninstrumented', args.uninstrumented), ('instrumented', args.instrumented)]}}
(args.output / 'report.json').write_text(json.dumps(report, indent=2) + '\n')
print(json.dumps(report))
raise SystemExit(bool(failures))
