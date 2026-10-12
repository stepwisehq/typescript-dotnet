#!/usr/bin/env python3
"""Three authored executable observations over unchanged original emit fixtures.
Uses existing compiler executables and the same Node process command for each output.
This is neither an original runtime suite nor a replacement for corpus assertions.
"""
import argparse
import hashlib
import json
from pathlib import Path
import shutil
import subprocess
import tempfile


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--go-executable', required=True)
    parser.add_argument('--executable', required=True)
    parser.add_argument('--node', default='node')
    parser.add_argument('--result', required=True)
    args = parser.parse_args()
    repository = Path(__file__).resolve().parents[2]
    cases = repository / 'upstream/TypeScript/tsc/testdata/tests/cases'
    # Compiler settings are the actual original fixture directives. The observer
    # reads resulting values without editing the fixture's TypeScript input.
    selected = [
        ('const-enum-computed-access', cases / 'conformance/constEnums/constEnumPropertyAccess1.ts',
         {'target': 'ES2015', 'declaration': True},
         '({a,a1,g,computedGetter:new C()[2],computedMethod:typeof C.prototype[1]})'),
        ('private-field-constructor', cases / 'conformance/classes/members/privateNames/privateNameField.ts',
         {'target': 'ES2015', 'strict': True},
         '({instance:new A("café") instanceof A,publicKeys:Object.keys(new A("second"))})'),
        ('destructuring-default-assignment', cases / 'compiler/destructuringAssignmentWithDefault.ts',
         {'target': 'ES2015', 'strictNullChecks': True},
         '({x,calls:[f1(),f2(),f3({color:"blue",width:2}),f4(["red",4])].map(value=>value===undefined)})'),
    ]
    node = shutil.which(args.node) or args.node
    node_version = subprocess.check_output([node, '--version'], text=True).strip()
    records = []
    with tempfile.TemporaryDirectory(prefix='tscheck-emit-runtime-') as directory:
        for name, fixture, settings, expression in selected:
            observed = {}
            for implementation, executable in [('go', args.go_executable), ('managed', args.executable)]:
                current = Path(directory) / name / implementation
                current.mkdir(parents=True)
                source = fixture.read_bytes()
                (current / fixture.name).write_bytes(source)
                configuration = {'compilerOptions': {'outDir': 'out', **settings}, 'files': [fixture.name]}
                config_path = current / 'tsconfig.json'
                config_path.write_text(json.dumps(configuration))
                command = ([str(Path(executable).resolve()), '-p', str(config_path), '--pretty', 'false']
                           if implementation == 'go' else [str(Path(executable).resolve()), '--config', str(config_path), '--json'])
                process = subprocess.run(command, text=True, capture_output=True)
                if process.returncode not in [0, 2]:
                    raise AssertionError(f'{name}/{implementation}: no generated output: {process.returncode} {process.stdout}{process.stderr}')
                js = current / 'out' / fixture.with_suffix('.js').name
                outputs = {str(path.relative_to(current / 'out')): path.read_bytes().hex()
                           for path in (current / 'out').rglob('*') if path.is_file()}
                suffix = json.dumps("\nconsole.log(JSON.stringify(" + expression + "));")
                script = 'const fs=require("fs"),vm=require("vm");vm.runInNewContext(fs.readFileSync(process.argv[1],"utf8")+' + suffix + ',{console,exports:{}});'
                execution = subprocess.run([node, '-e', script, str(js)], text=True, capture_output=True)
                if execution.returncode != 0:
                    raise AssertionError(f'{name}/{implementation}: actual Node execution failed: {execution.stderr}')
                observed[implementation] = {'compileExitCode': process.returncode, 'files': outputs,
                                            'nodeExitCode': execution.returncode, 'stdout': execution.stdout,
                                            'stderr': execution.stderr}
            if observed['go'] != observed['managed']:
                raise AssertionError(f'{name}: compiler output or actual Node observation differs: {observed}')
            records.append({'name': name, 'fixture': str(fixture.relative_to(repository)),
                            'sourceSha256': hashlib.sha256(source).hexdigest(), 'options': settings,
                            'authoredObserver': expression, 'observations': observed, 'passed': True})
    destination = Path(args.result)
    destination.parent.mkdir(parents=True, exist_ok=True)
    destination.write_text(json.dumps({'authored': True, 'node': node, 'nodeVersion': node_version,
                                     'cases': records}, indent=2) + '\n')
    print(f'PASS {len(records)} original-fixture actual Node comparisons')


if __name__ == '__main__':
    main()
