#!/usr/bin/env python3
"""Run the existing original emit consumer in bounded fresh processes, then compare once."""
import argparse
import gzip
import hashlib
import json
import subprocess
import sys
import time
from pathlib import Path


def sha(path):
    with path.open('rb') as stream:
        return hashlib.file_digest(stream, 'sha256').hexdigest()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('executable', type=Path)
    parser.add_argument('requests', type=Path)
    parser.add_argument('root', type=Path)
    parser.add_argument('result_directory', type=Path)
    parser.add_argument('--batch-size', type=int, default=100)
    args = parser.parse_args()
    if args.batch_size < 1:
        parser.error('batch-size must be positive')
    executable, requests, root, destination = (path.resolve() for path in
        (args.executable, args.requests, args.root, args.result_directory))
    destination.mkdir(parents=True, exist_ok=False)
    comparator = root / 'tools/emit-oracle/compare-program-output.py'
    originals = root / 'artifacts/emit/whole-reference/original.jsonl.gz'
    supplemental = root / 'artifacts/emit/whole-reference-record-supplement/original.jsonl'
    guarded = [executable, Path(__file__).resolve(), comparator, requests, originals, supplemental]
    if executable.suffix == '.dll':
        guarded += sorted(executable.parent.glob('*.dll'))
        guarded += sorted(executable.parent.glob('*.deps.json'))
        guarded += sorted(executable.parent.glob('*.runtimeconfig.json'))
    frozen = {str(path): sha(path) for path in dict.fromkeys(guarded)}
    (destination / 'frozen-inputs.json').write_text(json.dumps(frozen, indent=2) + '\n')
    def guard():
        if any(sha(Path(path)) != digest for path, digest in frozen.items()):
            raise RuntimeError('Frozen emit executable, tool, requests or reference changed during corpus execution.')
    command = ['dotnet', str(executable)] if executable.suffix == '.dll' else [str(executable)]
    started = time.monotonic()
    batches = []
    combined = destination / 'actual.jsonl.gz'
    request_lines = requests.read_text(encoding='utf-8').splitlines()
    names = [json.loads(line)['Name'] for line in request_lines]
    if len(set(names)) != len(names):
        raise ValueError('Duplicate original request identity.')
    with (destination / 'progress.jsonl').open('w') as progress:
        for offset in range(0, len(request_lines), args.batch_size):
            guard()
            directory = destination / f'batch-{len(batches):04d}'
            directory.mkdir(exist_ok=True)
            batch_requests, output = directory / 'requests.jsonl', directory / 'actual.jsonl'
            lines = request_lines[offset:offset + args.batch_size]
            batch_requests.write_text('\n'.join(lines) + '\n', encoding='utf-8')
            invocation = command + ['--emit', str(root), str(batch_requests), str(output)]
            batch_started = time.monotonic()
            with (directory / 'run.log').open('w') as log:
                result = subprocess.run(invocation, cwd=root, stdout=log, stderr=subprocess.STDOUT)
            guard()
            observation = {'command': invocation, 'exitCode': result.returncode,
                'elapsedSeconds': time.monotonic() - batch_started, 'requests': len(lines),
                'observed': 0, 'outputSha256': None, 'rawOutputBytes': 0}
            if output.exists():
                observation.update(outputSha256=sha(output), rawOutputBytes=output.stat().st_size)
                with gzip.open(combined, 'ab', compresslevel=6) as aggregate, output.open('rb') as stream:
                    for line in stream:
                        observation['observed'] += 1
                        aggregate.write(line)
                output.unlink()  # Closed gzip member retains the complete batch bytes.
            (directory / 'result.json').write_text(json.dumps(observation, indent=2) + '\n')
            batches.append(observation)
            progress.write(json.dumps({'batch': len(batches), **observation}) + '\n')
            progress.flush()
            print(json.dumps({'batch': len(batches), 'requestsCompleted': offset + len(lines),
                'exitCode': result.returncode, 'observed': observation['observed']}), flush=True)
    comparison_command = [sys.executable, str(comparator), str(originals), str(combined),
        str(destination / 'comparison.json'), '--requests', str(requests)]
    with (destination / 'comparison.log').open('w') as log:
        comparison = subprocess.run(comparison_command, cwd=root, stdout=log, stderr=subprocess.STDOUT)
    guard()
    summary = {'requests': len(request_lines), 'observed': sum(batch['observed'] for batch in batches),
        'batchSize': args.batch_size, 'processes': len(batches), 'failedProcesses': sum(batch['exitCode'] != 0 for batch in batches),
        'elapsedSeconds': time.monotonic() - started, 'comparisonCommand': comparison_command,
        'comparisonExitCode': comparison.returncode, 'frozenInputs': frozen,
        'requestSha256': sha(requests), 'actualSha256': sha(combined), 'actualCompressedBytes': combined.stat().st_size}
    (destination / 'result.json').write_text(json.dumps(summary, indent=2) + '\n')
    print(json.dumps(summary), flush=True)
    return 1 if summary['failedProcesses'] or comparison.returncode else 0


if __name__ == '__main__':
    raise SystemExit(main())
