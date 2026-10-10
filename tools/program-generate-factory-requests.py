#!/usr/bin/env python3
"""Explicit public resolver callback values; not ordinary resolver diagnostics."""
import argparse
import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]


def generate():
    rows = []
    def add(name, options, source, decorator, root='main.ts'):
        files = {
            '/src/tsconfig.json': json.dumps({'compilerOptions': {
                'target': 'es2022', 'module': 'esnext', 'moduleResolution': 'bundler',
                'noLib': True, 'types': [], **options}, 'files': [root]}, separators=(',', ':')),
            '/src/' + root: source,
            '/src/dep.ts': 'export const dep=1;\n',
            '/src/node_modules/@types/env/index.d.ts': 'interface Environment {}\n'}
        rows.append({'Name': 'factory-' + name, 'ConfigPath': '/src/tsconfig.json',
                     'Files': [{'Path': path, 'Text': text} for path, text in files.items()],
                     'SingleThreaded': True, 'Bind': True, 'ResolverDecorator': decorator})
    add('module-publication', {}, 'import "./dep";\n', {'ModuleDiagnostics': True})
    add('module-nocheck', {'noCheck': True}, 'import "./dep";\n', {'ModuleDiagnostics': True})
    add('type-publication', {}, '/// <reference types="env" />\nexport {};\n', {'TypeDiagnostics': True})
    add('type-nocheck', {'noCheck': True}, '/// <reference types="env" />\nexport {};\n', {'TypeDiagnostics': True})
    add('type-declaration-skiplib', {'skipLibCheck': True},
        '/// <reference types="env" />\ninterface Root {}\n', {'TypeDiagnostics': True}, root='main.d.ts')
    add('automatic-type-global', {'types': ['env']}, 'export {};\n', {'TypeDiagnostics': True})
    add('module-failure', {}, 'import "./dep";\n', {'Error': True})
    add('module-type-combined', {}, '/// <reference types="env" />\nimport "./dep";\n',
        {'ModuleDiagnostics': True, 'TypeDiagnostics': True})
    return rows


def digest(data):
    return hashlib.sha256(data).hexdigest()


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--check', action='store_true')
    args = parser.parse_args()
    rows = generate()
    data = ''.join(json.dumps(row, separators=(',', ':')) + '\n' for row in rows).encode()
    sources = ['upstream/TypeScript/tsc/internal/compiler/program.go',
               'upstream/TypeScript/tsc/internal/compiler/includeprocessor.go']
    provenance = {'kind': 'authored-public-resolver-factory-callback-values', 'cases': len(rows),
                  'requestsSha256': digest(data),
                  'sourceFiles': {path: digest((ROOT / path).read_bytes()) for path in sources},
                  'limits': ['Default resolver performs all lookups. The callback supplies actual AST diagnostics.',
                             'Nonempty type slot tests its real Program consumer; no default resolver producer claim.',
                             'Captured source pointers are actual host GetSourceFile results; automatic inferred owner is absent.',
                             'These constructors are separate from disk configuration equivalence.'],
                  'expectedSource': 'pristine original Program with identical public factory callbacks'}
    outputs = {'validation/program-corpus/requests-factory.jsonl': data,
               'validation/program-corpus/requests-factory-provenance.json': (json.dumps(provenance, indent=2) + '\n').encode()}
    for path, content in outputs.items():
        target = ROOT / path
        if args.check:
            if target.read_bytes() != content:
                raise SystemExit('Factory input drift: ' + path)
        else:
            target.write_bytes(content)
    print(json.dumps({'cases': len(rows), 'requestsSha256': digest(data)}))


if __name__ == '__main__':
    main()
