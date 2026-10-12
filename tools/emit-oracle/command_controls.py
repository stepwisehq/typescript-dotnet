#!/usr/bin/env python3
"""Authored real disk invocation controls for an existing tscheck executable.
No builds, expected corpus rewriting, or oracle substitutes occur here.
"""
import argparse
import json
from pathlib import Path
import subprocess
import tempfile
import time


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--executable', required=True)
    parser.add_argument('--result', required=True)
    parser.add_argument('--reference', help='Compare every actual emitted byte sequence with retained pinned Go capture')
    parser.add_argument('--go-reference', action='store_true', help='Capture exact pinned Go invocation/files using the same six configurations')
    args = parser.parse_args()
    executable = str(Path(args.executable).resolve())
    reference = {} if not args.reference else {item['name']: item for item in json.loads(Path(args.reference).read_text())['cases']}
    records = []
    start = time.monotonic()
    with tempfile.TemporaryDirectory(prefix='tscheck-emit-command-') as directory:
        root = Path(directory)
        cases = [
            ('output-map-declaration', 'export const value: number = 1;\n',
             {'sourceMap': True, 'declaration': True, 'declarationMap': True, 'listEmittedFiles': True}, 0, True),
            ('diagnostics-outputs-generated', 'export const value: number = "wrong";\n', {}, 2, True),
            ('diagnostics-outputs-skipped', 'export const value: number = "wrong";\n', {'noEmitOnError': True}, 1, False),
            ('no-emit', 'export const value: number = 1;\n', {'noEmit': True}, 0, False),
            ('list-files-only', 'export const value: number = "wrong";\n', {}, 0, False),
            ('unicode-output-path', 'export const café: number = 1;\n', {'outDir': 'sortie-é'}, 0, True),
        ]
        for name, source, options, expected_status, expected_write in cases:
            current = root / name
            current.mkdir()
            (current / 'main.ts').write_text(source)
            config = {'compilerOptions': {'target': 'ESNext', 'module': 'ESNext', 'strict': True,
                                          'outDir': 'out', **options}, 'files': ['main.ts']}
            (current / 'tsconfig.json').write_text(json.dumps(config))
            command = ([executable, '-p', str(current / 'tsconfig.json'), '--pretty', 'false']
                       if args.go_reference else [executable, '--config', str(current / 'tsconfig.json'), '--json'])
            if name == 'list-files-only':
                command.append('--listFilesOnly')
            process = subprocess.run(command, text=True, capture_output=True)
            if process.returncode != expected_status:
                raise AssertionError(f'{name}: exit {process.returncode}, expected {expected_status}: {process.stdout}{process.stderr}')
            if args.go_reference:
                records.append({'name': name, 'exitCode': process.returncode, 'stdout': process.stdout,
                                'stderr': process.stderr, 'configuration': config, 'source': source,
                                'outputs': {str(path.relative_to(current)): path.read_bytes().decode('utf-8')
                                            for path in current.rglob('*') if path.is_file()
                                            and path.name not in ['main.ts', 'tsconfig.json']}})
                continue
            result = json.loads(process.stdout)
            if result['ExitCode'] != expected_status:
                raise AssertionError(f'{name}: JSON status disagrees with process')
            actual_outputs = {str(path.relative_to(current)): path.read_bytes().decode('utf-8') for path in current.rglob('*')
                              if path.is_file() and path.name not in ['main.ts', 'tsconfig.json']}
            if reference:
                expected = reference[name]
                if expected['exitCode'] != process.returncode or expected['outputs'] != actual_outputs:
                    raise AssertionError(f'{name}: actual exit/files differ from pinned Go capture')
            output = current / options.get('outDir', 'out')
            emitted_js = output / 'main.js'
            if emitted_js.exists() != expected_write:
                raise AssertionError(f'{name}: actual JS write disagrees with requested emission policy')
            if name == 'output-map-declaration':
                expected_js = 'export const value = 1;\n//# sourceMappingURL=main.js.map'
                if emitted_js.read_bytes().decode('utf-8').rstrip('\n') != expected_js:
                    raise AssertionError(f'{name}: unexpected actual JS bytes {emitted_js.read_bytes()!r}')
                if (output / 'main.d.ts').read_bytes().decode('utf-8').rstrip('\n') != 'export declare const value: number;\n//# sourceMappingURL=main.d.ts.map':
                    raise AssertionError(f'{name}: unexpected declaration bytes')
                for filename, generated in [('main.js.map', 'main.js'), ('main.d.ts.map', 'main.d.ts')]:
                    mapping = json.loads((output / filename).read_bytes().decode('utf-8'))
                    if mapping['version'] != 3 or mapping['file'] != generated or mapping['sources'] != ['../main.ts'] or not mapping['mappings']:
                        raise AssertionError(f'{name}: invalid actual source map {mapping}')
                listing = result.get('Listing', '')
                for filename in ['main.js', 'main.js.map', 'main.d.ts', 'main.d.ts.map']:
                    if 'TSFILE: ' + str(output / filename) not in listing:
                        raise AssertionError(f'{name}: emitted-file listing omitted {filename}')
            elif name.startswith('diagnostics-'):
                diagnostics = result['Diagnostics']
                if len(diagnostics) != 1 or diagnostics[0]['Code'] != 2322 or diagnostics[0]['Line'] != 1 or diagnostics[0]['Column'] != 14 or diagnostics[0]['Message'] != "Type 'string' is not assignable to type 'number'.":
                    raise AssertionError(f'{name}: diagnostic differs from pinned Go observed TS2322 at (1,14)')
                if result['EmitSkipped'] != (not expected_write):
                    raise AssertionError(f'{name}: diagnostics/skipped status missing')
            elif name == 'list-files-only':
                if result['Diagnostics'] or str(current / 'main.ts') not in result.get('Listing', ''):
                    raise AssertionError(f'{name}: semantic work was not suppressed or input listing missing')
            elif name == 'unicode-output-path':
                if emitted_js.read_bytes().decode('utf-8') != 'export const café = 1;\n':
                    raise AssertionError(f'{name}: Unicode source/output boundary changed')
            records.append({'name': name, 'exitCode': process.returncode, 'passed': True})
    destination = Path(args.result)
    destination.parent.mkdir(parents=True, exist_ok=True)
    destination.write_text(json.dumps({'authored': True, 'executable': executable,
                                     'goReference': args.go_reference, 'elapsedSeconds': time.monotonic() - start, 'cases': records}, indent=2) + '\n')
    print(f'PASS {len(records)} actual disk invocation controls')


if __name__ == '__main__':
    main()
