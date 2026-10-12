#!/usr/bin/env python3
"""Authored disk checks of the explicit excluded incremental command boundary.
These are not Go-equal command controls or original incremental output assertions.
No compiler builds or baseline changes occur here.
"""
import argparse
import json
from pathlib import Path
import subprocess
import tempfile


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--executable', required=True)
    parser.add_argument('--result', required=True)
    args = parser.parse_args()
    executable = str(Path(args.executable).resolve())
    expected_error = ('Incremental or composite compilation requires the upstream incremental '
                      'snapshot/build-info owner, which is outside the supported command execution scope.')
    cases = [
        ('incremental-owner-rejected', {'incremental': True}, 2),
        ('composite-owner-rejected', {'composite': True}, 2),
        ('composite-option-diagnostic-preserved', {'composite': True, 'declaration': False}, 1),
    ]
    observations = []
    with tempfile.TemporaryDirectory(prefix='tscheck-command-boundary-') as directory:
        for name, options, expected_exit in cases:
            current = Path(directory) / name
            current.mkdir()
            (current / 'main.ts').write_bytes(b'export const value: number = 1;\n')
            config = {'compilerOptions': {'target': 'ESNext', 'module': 'ESNext',
                                          'skipLibCheck': True, 'outDir': 'out', **options},
                      'files': ['main.ts']}
            config_file = current / 'tsconfig.json'
            config_file.write_text(json.dumps(config))
            process = subprocess.run([executable, '--config', str(config_file), '--json'],
                                     capture_output=True, text=True)
            if process.returncode != expected_exit:
                raise AssertionError(f'{name}: unexpected status {process.returncode}: {process.stdout}{process.stderr}')
            result = json.loads(process.stdout)
            if result['ExitCode'] != expected_exit:
                raise AssertionError(f'{name}: JSON/process status differs')
            if expected_exit == 2:
                if result.get('Error') != expected_error or result.get('Diagnostics'):
                    raise AssertionError(f'{name}: excluded owner did not report its explicit host boundary: {result}')
            else:
                diagnostics = result['Diagnostics']
                if (result.get('Error') or not result['EmitSkipped'] or len(diagnostics) != 1
                        or diagnostics[0]['Code'] != 6304
                        or diagnostics[0]['Message'] != 'Composite projects may not disable declaration emit.'):
                    raise AssertionError(f'{name}: genuine option diagnostic was replaced: {result}')
            unexpected = [str(path.relative_to(current)) for path in current.rglob('*')
                          if path.is_file() and path.name not in ['main.ts', 'tsconfig.json']]
            if unexpected:
                raise AssertionError(f'{name}: rejected execution wrote outputs: {unexpected}')
            observations.append({'name': name, 'configuration': config, 'exitCode': process.returncode,
                                 'publicResult': result, 'stderr': process.stderr, 'passed': True})
    destination = Path(args.result)
    destination.parent.mkdir(parents=True, exist_ok=True)
    destination.write_text(json.dumps({'authored': True, 'classification': 'explicit-excluded-owner-command-boundary',
                                     'goEqualityClaim': False, 'executable': executable,
                                     'cases': observations}, indent=2) + '\n')
    print(f'PASS {len(observations)} authored disk command boundary controls')


if __name__ == '__main__':
    main()
