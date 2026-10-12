#!/usr/bin/env python3
"""Bounded fresh-process VS Code full-emit timing with exact retained output checks."""
import argparse
import hashlib
import json
import os
import shutil
import signal
import statistics
import subprocess
import tempfile
import time
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]

def sha(path):
    with Path(path).open('rb') as stream:
        return hashlib.file_digest(stream, 'sha256').hexdigest()

def save(path, value):
    Path(path).write_text(json.dumps(value, indent=2, ensure_ascii=False) + '\n')

def render_native(document, cwd):
    # Same complete pretty-false public projection as compare-vscode-emit.py.
    def chain(diagnostic, depth=0):
        result = diagnostic['Message']
        for child in diagnostic.get('Chain') or []:
            result += '\n' + '  ' * (depth + 1) + chain(child, depth + 1)
        return result
    result = ''
    for diagnostic in document['Diagnostics']:
        location = ''
        if diagnostic.get('File'):
            path = os.path.relpath(diagnostic['File'], cwd).replace(os.sep, '/')
            location = f"{path}({diagnostic['Line']},{diagnostic['Column']}): "
        result += location + diagnostic['Category'].lower() + ' TS' + str(diagnostic['Code']) + ': ' + chain(diagnostic) + '\n'
    return result + (document.get('Listing') or '')

def no_gc_qualified(policy, budget_gib):
    return (policy['noGcStarted'] and not policy['fallbackAtStart'] and
            policy['modeBeforeEnd'] == 'NoGCRegion' and
            policy['noGcEndOutcome'] == 'endedSuccessfully' and policy['restoredExact'] and
            policy['serverGC'] and policy['totalBudgetBytes'] == budget_gib * 1024**3 and
            policy['lohBudgetBytes'] == 2 * 1024**3 and
            policy['minimumAvailableBytes'] == 20 * 1024**3 and
            policy['preflightAvailableBytes'] >= 20 * 1024**3)

def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('directory', type=Path)
    parser.add_argument('--native', type=Path, default=ROOT / 'artifacts/emit-performance/baseline/native-avx2/tscheck')
    parser.add_argument('--go', type=Path, default=ROOT / 'artifacts/emit/command-reference/tsgo')
    parser.add_argument('--project', type=Path, default=Path('/home/greg/projects/vscode/src/tsconfig.json'))
    parser.add_argument('--reference', type=Path, default=ROOT / 'artifacts/vscode-emit-comparison/2026-10-12-corrected')
    parser.add_argument('--runs', type=int, choices=[1, 5], default=1)
    parser.add_argument('--checkers', type=int, default=8)
    parser.add_argument('--no-gc-budget-gib', type=int, choices=[14, 16], default=14, help='Expected reported region budget; does not configure compiler')
    parser.add_argument('--ordinary', action='store_true', help='Separate no-region control; omit checkers override to preserve project default')
    parser.add_argument('--timeout', type=int, default=900)
    args = parser.parse_args()
    directory = args.directory.resolve()
    directory.mkdir(parents=True, exist_ok=False)
    reference = args.reference.resolve()
    expected = json.loads((reference / 'comparison.json').read_text())
    assert expected['allOutputPathsAndBytesEqual'] and expected['diagnostics']['fullComparableTextEqual']
    expected_files = {row['path']: row['files']['go'] for row in expected['outputs']}
    expected_text = (reference / 'go.stdout.txt').read_bytes()
    binaries = {'go': args.go.resolve(), 'native': args.native.resolve()}
    frozen = {str(p): sha(p) for p in [Path(__file__), reference / 'comparison.json', reference / 'go.stdout.txt', *binaries.values()]}
    for path, digest in expected['provenance']['files'].items():
        assert sha(path) == digest, path
        frozen[path] = digest
    # Complete conservative workload text inventory is checked outside timing,
    # once before and once after the cohort; per-run guards cover small inputs.
    vscode = args.project.resolve().parent.parent
    def workload_inputs(exclude=None):
        return {str(p): sha(p) for base in [vscode / 'src', vscode / 'node_modules']
                for p in base.rglob('*') if p != exclude and p.is_file() and p.suffix.lower() in ['.ts', '.tsx', '.mts', '.cts', '.js', '.json']}
    workload = workload_inputs()
    save(directory / 'workload-inputs.json', workload)
    output = directory / 'output'
    config = {'extends': str(args.project.resolve()), 'compilerOptions': {'outDir': str(output)}}
    if not args.ordinary:
        config['compilerOptions']['checkers'] = args.checkers
    config_bytes = (json.dumps(config, indent=2) + '\n').encode()
    with tempfile.NamedTemporaryFile(prefix='.tscheck-emit-perf-', suffix='.json', dir=args.project.resolve().parent, delete=False) as stream:
        stream.write(config_bytes)
        actual_config = Path(stream.name)
    (directory / 'config.json').write_bytes(config_bytes)
    frozen[str(actual_config)] = sha(actual_config)
    env = os.environ.copy()
    removed = []
    for key in list(env):
        if key.startswith(('DOTNET_', 'COMPlus_', 'CORECLR_', 'COR_', 'GODEBUG', 'GOGC', 'GOMEMLIMIT', 'GOMAXPROCS', 'GOEXPERIMENT', 'TSC_GC_')) or key in ['TSC_ALLOCATION_REPORT', 'TSC_NO_GC_REGION']:
            removed.append(key)
            env.pop(key)
    commands = {'go': [str(binaries['go']), '-p', str(actual_config), '--pretty', 'false'],
                'native': [str(binaries['native']), '--config', str(actual_config), '--json'] + ([] if args.ordinary else ['--no-gc-region'])}
    order = [runtime for index in range(args.runs) for runtime in (['go', 'native'] if index % 2 == 0 else ['native', 'go'])]
    save(directory / 'inputs.json', {'frozenInputs': frozen, 'commands': commands, 'order': order, 'removedEnvironmentVariables': removed,
                                    'configuration': config, 'expectedNoGcTotalBudgetGiB': args.no_gc_budget_gib, 'mode': 'ordinary-control' if args.ordinary else 'matched-eight-checker-AVX2-noGC',
                                    'limits': 'Repeated fresh processes with warm inputs. Ordinary control is separate. No-GC qualification required for tuned target. Related diagnostic DTO/byte-end spans not exposed by Go text formatter.'})
    rows = []
    def guard():
        for path, digest in frozen.items():
            assert sha(path) == digest, path
    try:
        for index, runtime in enumerate(order):
            guard()
            shutil.rmtree(output, ignore_errors=True)
            folder = directory / f'{index + 1:02d}-{runtime}'
            folder.mkdir()
            command = ['/usr/bin/time', '-q', '-f', '%e %U %S %M %F %R %W', '-o', str(folder / 'time.txt'), *commands[runtime]]
            run_env = env.copy()
            if runtime == 'native' and not args.ordinary:
                run_env['TSC_GC_POLICY_REPORT'] = str(folder / 'policy.json')
            started = time.monotonic()
            with (folder / 'stdout').open('wb') as out, (folder / 'stderr').open('wb') as err:
                process = subprocess.Popen(command, cwd=ROOT, env=run_env, stdout=out, stderr=err, start_new_session=True)
                try:
                    process.wait(timeout=args.timeout)
                except subprocess.TimeoutExpired:
                    os.killpg(process.pid, signal.SIGKILL)
                    process.wait()
                    save(folder / 'timeout.json', {'command': command, 'timeoutSeconds': args.timeout, 'exitCode': process.returncode, 'classification': 'Timed process group terminated; stdout/stderr retained, no timing acceptance'})
                    raise
            elapsed = time.monotonic() - started
            values = (folder / 'time.txt').read_text().splitlines()[-1].split()
            stdout = (folder / 'stdout').read_bytes()
            document = json.loads(stdout) if runtime == 'native' else None
            text = render_native(document, ROOT).encode() if document else stdout
            files = {p.relative_to(output).as_posix(): p for p in output.rglob('*') if p.is_file()}
            differences = []
            for name in sorted(set(files) | set(expected_files)):
                path = files.get(name)
                if path is None or name not in expected_files or path.stat().st_size != expected_files[name]['bytes'] or sha(path) != expected_files[name]['sha256'] or path.read_bytes() != (reference / 'go-output' / name).read_bytes():
                    differences.append(name)
            policy = json.loads((folder / 'policy.json').read_text()) if runtime == 'native' and not args.ordinary else None
            qualified = no_gc_qualified(policy, args.no_gc_budget_gib) if policy else args.ordinary or runtime == 'go'
            correct = (not differences and len(files) == len(expected_files) and text == expected_text and process.returncode == 2 and
                       not (folder / 'stderr').read_bytes() and (document is None or document['ExitCode'] == 2 and document['EmitSkipped'] is False and not document.get('Error')))
            row = {'runtime': runtime, 'round': index // 2 + 1, 'command': command, 'exitCode': process.returncode,
                   'wallSeconds': float(values[0]), 'controllerWallSeconds': elapsed, 'cpuSeconds': float(values[1]) + float(values[2]),
                   'peakResidentKiB': int(values[3]), 'majorPageFaults': int(values[4]), 'minorPageFaults': int(values[5]), 'swaps': int(values[6]),
                   'outputFiles': len(files), 'outputBytes': sum(p.stat().st_size for p in files.values()), 'outputDifferences': differences,
                   'diagnosticTextEqual': text == expected_text, 'correctnessPassed': correct, 'noGcFullRegionQualified': qualified,
                   'policy': policy, 'stdoutSha256': sha(folder / 'stdout'), 'stderrSha256': sha(folder / 'stderr')}
            save(folder / 'result.json', row)
            rows.append(row)
            save(directory / 'progress.json', rows)
            guard()
            assert correct, 'Exact output/diagnostic/status guard failed; retained run evidence'
            # A single qualification pair stops before expensive five-each work.
            if args.runs == 1 and not qualified:
                break
        assert workload_inputs(actual_config) == workload, 'Workload changed during cohort'
        summary = {runtime: {field: {'best': min(v), 'median': statistics.median(v), 'range': [min(v), max(v)]}
                             for field in ['wallSeconds', 'cpuSeconds', 'peakResidentKiB']
                             for v in [[x[field] for x in rows if x['runtime'] == runtime]]}
                   for runtime in commands if any(x['runtime'] == runtime for x in rows)}
        ratio = summary['native']['wallSeconds']['best'] / summary['go']['wallSeconds']['best']
        qualified = all(x['noGcFullRegionQualified'] for x in rows)
        save(directory / 'comparison.json', {'runs': rows, 'summary': summary, 'bestNativeToGoRatio': ratio,
             'target': 1.2, 'allNoGcRunsFullRegionQualified': qualified,
             'observedTargetMet': args.runs == 5 and not args.ordinary and qualified and ratio <= 1.2,
             'limits': 'One pair is qualification only; five each best-to-best target. All samples retained. Initial hashes warm input cache. Output verification/hashing outside timed compiler process; output directory reused, final output retained. Runtime budgets are not memory limits.'})
        return 0 if qualified else 1
    finally:
        actual_config.unlink()

if __name__ == '__main__':
    raise SystemExit(main())
