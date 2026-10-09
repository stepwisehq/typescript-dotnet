"""Strict standalone configuration comparison; never generate expected output."""
import argparse
import hashlib
import gzip
import json
from pathlib import Path
import subprocess
import time


def object_value(pairs):
    result = {}
    for key, value in pairs:
        if key in result:
            raise ValueError(f'Duplicate JSON key: {key}')
        result[key] = value
    return result


def parse(line):
    def invalid(value):
        raise ValueError(f'Non-JSON numeric value: {value}')
    return json.loads(line, object_pairs_hook=object_value, parse_constant=invalid)


def difference(expected, actual, path='$'):
    if type(expected) is not type(actual):
        return {'path': path, 'reason': 'JSON type differs', 'expected': expected, 'actual': actual}
    if isinstance(expected, dict):
        if expected.keys() != actual.keys():
            return {'path': path, 'reason': 'Object fields differ', 'expected': list(expected), 'actual': list(actual)}
        for key in expected:
            result = difference(expected[key], actual[key], path + '.' + key)
            if result:
                return result
    elif isinstance(expected, list):
        if len(expected) != len(actual):
            return {'path': path, 'reason': 'Array length differs', 'expected': len(expected), 'actual': len(actual)}
        for index, (left, right) in enumerate(zip(expected, actual)):
            result = difference(left, right, f'{path}[{index}]')
            if result:
                return result
    elif expected != actual:
        return {'path': path, 'reason': 'Value differs', 'expected': expected, 'actual': actual}
    return None


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--requests', required=True, type=Path)
    parser.add_argument('--expected', required=True, type=Path)
    parser.add_argument('--executable', required=True, type=Path)
    parser.add_argument('--managed', action='store_true')
    parser.add_argument('--output', required=True, type=Path)
    args = parser.parse_args()
    request_bytes = args.requests.read_bytes()
    expected_bytes = args.expected.read_bytes()
    requests = [parse(line) for line in request_bytes.splitlines()]
    decoded_expected = gzip.decompress(expected_bytes) if args.expected.suffix == ".gz" else expected_bytes
    expected = [parse(line) for line in decoded_expected.splitlines()]
    if not requests or len(expected) != len(requests):
        raise ValueError('Nonempty request and expected streams must have equal lengths')
    names = [row['name'] for row in requests]
    if len(set(names)) != len(names):
        raise ValueError('Request names must be unique')
    for request, row in zip(requests, expected):
        if row['name'] != request['name'] or row['mode'] != request['mode']:
            raise ValueError('Expected response identity does not match request')
    args.output.mkdir(parents=True, exist_ok=True)
    command = (['dotnet'] if args.managed else []) + [str(args.executable.resolve())]
    binary_before = hashlib.sha256(args.executable.read_bytes()).hexdigest()
    start = time.monotonic()
    result = subprocess.run(command, input=request_bytes, capture_output=True)
    elapsed = time.monotonic() - start
    (args.output / 'stdout.jsonl').write_bytes(result.stdout)
    (args.output / 'stderr.txt').write_bytes(result.stderr)
    failures = []
    actual_lines = result.stdout.splitlines()
    exact = 0
    if result.returncode != 0:
        failures.append({'reason': 'Process failed', 'exitCode': result.returncode})
    if len(actual_lines) != len(requests):
        failures.append({'reason': 'Response count differs', 'expected': len(requests), 'actual': len(actual_lines)})
    for index, line in enumerate(actual_lines[:len(requests)]):
        try:
            actual = parse(line)
            mismatch = difference(expected[index], actual)
            if mismatch:
                failures.append({'name': names[index], **mismatch})
            else:
                exact += 1
        except (ValueError, TypeError) as error:
            failures.append({'name': names[index], 'reason': 'Invalid response JSON', 'error': str(error)})
    binary_after = hashlib.sha256(args.executable.read_bytes()).hexdigest()
    if binary_before != binary_after:
        failures.append({'reason': 'Executable drift'})
    report = {'total': len(requests), 'exact': exact, 'failures': failures,
              'seconds': round(elapsed, 3), 'exitCode': result.returncode, 'command': command,
              'requestsSha256': hashlib.sha256(request_bytes).hexdigest(),
              'expectedSha256': hashlib.sha256(expected_bytes).hexdigest(),
              'executableSha256': binary_before, 'executableDrift': binary_before != binary_after}
    (args.output / 'report.json').write_text(json.dumps(report, indent=2) + '\n')
    print(json.dumps({'total': len(requests), 'exact': exact, 'failures': len(failures), 'seconds': report['seconds']}))
    return 0 if not failures else 1


if __name__ == '__main__':
    raise SystemExit(main())
