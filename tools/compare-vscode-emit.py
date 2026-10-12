#!/usr/bin/env python3
"""Run two existing compilers or compare their retained VS Code disk outputs exactly."""
import argparse
import difflib
import hashlib
import json
import re
import os
import subprocess
import time
import tempfile
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]

def sha(path):
    with path.open('rb') as stream:
        return hashlib.file_digest(stream, 'sha256').hexdigest()

def save(path, value):
    path.write_text(json.dumps(value, indent=2, ensure_ascii=False) + '\n')

def output_files(directory):
    return {p.relative_to(directory).as_posix(): p for p in directory.rglob('*') if p.is_file()}

def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('directory', type=Path)
    parser.add_argument('--compare-only', action='store_true')
    parser.add_argument('--project', type=Path, default=Path('/home/greg/projects/vscode/src/tsconfig.json'))
    parser.add_argument('--native', type=Path, default=ROOT / 'artifacts/emit/final-acceptance-workgroup-fix/native/TypeScript.Check/tscheck')
    parser.add_argument('--go', type=Path, default=ROOT / 'artifacts/emit/command-reference/tsgo')
    args = parser.parse_args()
    directory = args.directory.resolve()
    if not args.compare_only:
        directory.mkdir(parents=True, exist_ok=False)
        runs = []
        for name, binary in [('native', args.native.resolve()), ('go', args.go.resolve())]:
            config = directory / (name + '-config.json')
            save(config, {'extends': str(args.project.resolve()), 'compilerOptions': {'outDir': str(directory / (name + '-output'))}})
            # Config location affects default rootDir/typeRoots. Keep the actual
            # wrapper beside the original config, and retain its exact bytes here.
            with tempfile.NamedTemporaryFile(prefix='.tscheck-emit-compare-', suffix='.json', dir=args.project.resolve().parent, delete=False) as temporary:
                temporary.write(config.read_bytes())
                actual_config = Path(temporary.name)
            command = [str(binary), '--config', str(actual_config), '--json'] if name == 'native' else [str(binary), '-p', str(actual_config), '--pretty', 'false']
            stdout = directory / ('native.stdout.json' if name == 'native' else 'go.stdout.txt')
            stderr = directory / (name + '.stderr.txt')
            binary_hash, config_hash = sha(binary), sha(config)
            started = time.monotonic()
            try:
                with stdout.open('wb') as out, stderr.open('wb') as err:
                    result = subprocess.run(command, cwd=ROOT, stdout=out, stderr=err)
                assert sha(binary) == binary_hash and sha(actual_config) == config_hash, 'Input changed during run'
            finally:
                actual_config.unlink()
            runs.append({'runtime': name, 'command': command, 'cwd': str(ROOT), 'exitCode': result.returncode,
                         'wallSeconds': time.monotonic() - started, 'binary': str(binary), 'binarySha256': binary_hash,
                         'config': str(config), 'configSha256': config_hash})
            save(directory / 'runs.json', runs)
    runs = json.loads((directory / 'runs.json').read_text())
    for record in runs:
        runtime = record.get('runtime', record.get('name'))
        assert sha(directory / (runtime + '-config.json')) == record['configSha256'], 'Retained config identity changed'
        assert sha(Path(record['command'][0])) == record['binarySha256'], 'Compiler binary identity changed'
    configs = {n: json.loads((directory / (n + '-config.json')).read_text()) for n in ['native', 'go']}
    assert all(set(c) == {'extends', 'compilerOptions'} and set(c['compilerOptions']) == {'outDir'} for c in configs.values()), 'Wrappers must only override outDir'
    assert configs['native']['extends'] == configs['go']['extends'], 'Different projects'
    files = {n: output_files(directory / (n + '-output')) for n in ['native', 'go']}
    records, mismatches = [], []
    diff_directory = directory / 'diffs'
    diff_directory.mkdir(exist_ok=True)
    for index, name in enumerate(sorted(set(files['native']) | set(files['go']))):
        paths = {n: files[n].get(name) for n in files}
        record = {'path': name, 'files': {n: None if p is None else {'bytes': p.stat().st_size, 'sha256': sha(p)} for n, p in paths.items()}}
        equal = all(paths.values()) and paths['native'].read_bytes() == paths['go'].read_bytes()
        record['equalBytes'] = bool(equal)
        if not equal:
            mismatch = {'path': name, 'reason': 'missing-file' if not all(paths.values()) else 'bytes-differ'}
            content = {n: b'' if p is None else p.read_bytes() for n, p in paths.items()}
            try:
                text = {n: v.decode('utf-8') for n, v in content.items()}
                diff = ''.join(difflib.unified_diff(text['go'].splitlines(keepends=True), text['native'].splitlines(keepends=True), fromfile='go/' + name, tofile='native/' + name))
            except UnicodeDecodeError:
                diff = 'go bytes: ' + content['go'].hex() + '\nnative bytes: ' + content['native'].hex() + '\n'
            target = diff_directory / (str(index) + '.diff')
            target.write_text(diff, newline='')
            mismatch['diff'] = str(target)
            mismatches.append(mismatch)
        records.append(record)
    native_stdout = directory / 'native.stdout.json'
    native = json.loads(native_stdout.read_bytes())
    go_stdout = (directory / 'go.stdout.txt').read_bytes().decode('utf-8')
    go_stderr = (directory / 'go.stderr.txt').read_bytes().decode('utf-8')
    go_diagnostics = re.findall(r'^(.*?)(?:error|warning|message|suggestion) TS(\d+): (.*)$', go_stdout + go_stderr, re.M)
    # diagnosticwriter.go:364–383/576–587: pretty-false Go prints the
    # complete message chain with two spaces per level, not related information.
    def flattened(diagnostic, level=0):
        text = diagnostic['Message']
        for child in diagnostic.get('Chain') or []:
            text += '\n' + '  ' * (level + 1) + flattened(child, level + 1)
        return text

    native_run = next((r for r in runs if r.get('runtime', r.get('name')) == 'native'), {})
    cwd = native_run.get('cwd', str(ROOT))
    rendered = ''
    for diagnostic in native.get('Diagnostics', []):
        location = ''
        if diagnostic.get('File'):
            relative = os.path.relpath(diagnostic['File'], cwd).replace(os.sep, '/')
            location = f"{relative}({diagnostic['Line']},{diagnostic['Column']}): "
        rendered += location + diagnostic['Category'].lower() + ' TS' + str(diagnostic['Code']) + ': ' + flattened(diagnostic) + '\n'
    # Go listing is a separate channel after its diagnostic text. Preserve its
    # exact text; default VS Code configuration emits no listing.
    rendered += native.get('Listing') or ''
    (directory / 'native.pretty-false-projection.txt').write_text(rendered, newline='')
    diagnostic_text_equal = rendered == go_stdout and not go_stderr
    diagnostics = {'native': native.get('Diagnostics', []), 'goRawStdout': go_stdout, 'goRawStderr': go_stderr,
                   'nativePrettyFalseProjection': rendered, 'fullComparableTextEqual': diagnostic_text_equal,
                   'goDiagnosticHeaders': [{'locationPrefix': a, 'code': int(b), 'message': c} for a, b, c in go_diagnostics],
                   'qualification': 'Full comparable pretty-false text includes exact relative file/line/UTF16 column, category, TS code, complete message and recursively indented chains. Go nonpretty formatter does not expose byte-end spans or related-information DTOs; those native fields are retained but unverified by this text comparison. External diagnostic Source prefixes are not exposed in the native CLI DTO.'}
    diagnostics['bothEmpty'] = not diagnostics['native'] and not go_diagnostics
    if not diagnostic_text_equal:
        (diff_directory / 'diagnostics.diff').write_text(''.join(difflib.unified_diff(go_stdout.splitlines(keepends=True), rendered.splitlines(keepends=True), fromfile='go-diagnostics', tofile='native-diagnostics')), newline='')
    statuses = {r.get('runtime', r.get('name')): r['exitCode'] for r in runs}
    status_equal = statuses.get('native') == statuses.get('go') == native.get('ExitCode') and statuses.get('native') in [0, 1, 2]
    native_stderr_empty = not (directory / 'native.stderr.txt').read_bytes()
    actual_emit = bool(files['native']) and bool(files['go']) and native.get('EmitSkipped') is False
    result = {'statusCodesEqual': status_equal, 'nativeStderrEmpty': native_stderr_empty, 'nonemptyActualEmit': actual_emit, 'classification': 'actual-external-project-comparison', 'runs': runs, 'configurations': configs,
              'provenance': json.loads((directory / 'provenance.json').read_text()) if (directory / 'provenance.json').exists() else None,
              'outputCounts': {n: len(f) for n, f in files.items()}, 'outputs': records, 'mismatches': mismatches,
              'allOutputPathsAndBytesEqual': not mismatches, 'nativeExitCode': native.get('ExitCode'), 'diagnostics': diagnostics,
              'streamBindings': {p.name: sha(p) for p in [native_stdout, directory / 'native.stderr.txt', directory / 'go.stdout.txt', directory / 'go.stderr.txt']},
              'qualification': 'No output normalization, compiler builds, expected-output rewriting or performance threshold. OutDir differs intentionally; differing absolute map text remains an actual byte difference.'}
    save(directory / 'comparison.json', result)
    print(json.dumps({'outputs': result['outputCounts'], 'byteMismatches': len(mismatches), 'diagnosticsBothEmpty': diagnostics['bothEmpty']}))
    if mismatches or not status_equal or not diagnostic_text_equal or not native_stderr_empty or not actual_emit:
        return 1
    return 2 if native.get('Error') else 0

if __name__ == '__main__':
    raise SystemExit(main())
