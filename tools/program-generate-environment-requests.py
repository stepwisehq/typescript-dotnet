#!/usr/bin/env python3
"""Source-derived Program environment inputs; all expectations come from Go."""
import argparse
import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
BASE = {'target': 'es2022', 'module': 'esnext', 'moduleResolution': 'bundler'}
ROWS = []


def add(name, options=None, source='export const value = 1;\n', extra=None,
        roots=None, references=None, flags=None, producer='library/type environment'):
    config = {'compilerOptions': {**BASE, **(options or {})},
              'files': roots if roots is not None else ['main.ts']}
    if references is not None:
        config['references'] = references
    files = {'/src/tsconfig.json': json.dumps(config, separators=(',', ':')),
             '/src/main.ts': source, **(extra or {})}
    ROWS.append({'Name': 'environment-' + name, 'ConfigPath': '/src/tsconfig.json',
                 'Files': [{'Path': path, 'Text': text} for path, text in files.items()],
                 'SingleThreaded': True, 'Bind': True, **(flags or {})})
    OBLIGATIONS.append({'name': ROWS[-1]['Name'], 'producer': producer})


OBLIGATIONS = []


def build():
    add('default-es2022', {'types': []})
    add('default-esnext', {'target': 'esnext', 'types': []})
    add('nolib', {'noLib': True, 'types': []})
    add('allocated-empty-lib', {'lib': [], 'types': []})
    add('explicit-es2022-dom', {'lib': ['es2022', 'dom'], 'types': []})
    add('triple-lib', {'noLib': True, 'types': []},
        '/// <reference lib="es2022" />\nexport const value = 1;\n')
    add('triple-lib-unknown', {'noLib': True, 'types': []},
        '/// <reference lib="not-real" />\nexport const value = 1;\n')
    add('no-default-lib-directive', {'types': []},
        '/// <reference no-default-lib="true" />\nexport const value = 1;\n')
    add('library-replacement', {'lib': ['es2022'], 'types': []}, extra={
        '/src/node_modules/@typescript/lib-es2022/package.json': '{"name":"@typescript/lib-es2022","version":"1.0.0","types":"index.d.ts"}',
        '/src/node_modules/@typescript/lib-es2022/index.d.ts': 'interface ReplacementMarker {}\n'})
    types = {
        '/src/node_modules/@types/env/package.json': '{"name":"@types/env","version":"1.0.0","types":"index.d.ts"}',
        '/src/node_modules/@types/env/index.d.ts': 'interface EnvironmentMarker {}\n'}
    add('automatic-types', {'noLib': True}, extra=types)
    add('automatic-types-disabled', {'noLib': True, 'types': []}, extra=types)
    add('automatic-types-explicit', {'noLib': True, 'types': ['env']}, extra=types)
    add('automatic-types-missing', {'noLib': True, 'types': ['missing']}, extra=types)
    add('automatic-types-ancestor', {'noLib': True}, extra={
        path.replace('/src/node_modules', '/node_modules'): text for path, text in types.items()})
    add('automatic-types-custom-root', {'noLib': True, 'typeRoots': ['./custom']}, extra={
        '/src/custom/env/index.d.ts': 'interface CustomRootMarker {}\n'})
    add('automatic-types-typings-null', {'noLib': True}, extra={
        '/src/node_modules/@types/env/package.json': '{"typings":null}',
        '/src/node_modules/@types/env/index.d.ts': 'interface HiddenMarker {}\n'})
    add('explicit-type', {'noLib': True, 'types': []},
        '/// <reference types="env" />\nexport const value = 1;\n', types)
    add('explicit-type-missing', {'noLib': True, 'types': []},
        '/// <reference types="missing" />\nexport const value = 1;\n')
    add('path-reference-chain', {'noLib': True, 'types': []},
        '/// <reference path="./first.d.ts" />\nexport const value = 1;\n', {
            '/src/first.d.ts': '/// <reference path="./second.d.ts" />\ninterface First {}\n',
            '/src/second.d.ts': 'interface Second {}\n'})
    add('noresolve-references', {'noLib': True, 'types': [], 'noResolve': True},
        '/// <reference path="./first.d.ts" />\n/// <reference types="env" />\nimport "./dep";\n',
        {**types, '/src/first.d.ts': 'interface First {}\n', '/src/dep.ts': 'export {};\n'})
    add('skip-module-resolution', {'noLib': True}, 'import "./dep";\n',
        {**types, '/src/dep.ts': 'export {};\n'}, flags={'SkipModuleResolution': True})
    for depth in [0, 1, 2]:
        add('javascript-depth-' + str(depth), {'noLib': True, 'types': [], 'allowJs': True,
            'maxNodeModuleJsDepth': depth}, 'import "external";\n', {
            '/src/node_modules/external/package.json': '{"name":"external","version":"1.0.0","main":"index.js"}',
            '/src/node_modules/external/index.js': 'import "nested"; export const value = 1;\n',
            '/src/node_modules/external/node_modules/nested/package.json': '{"main":"index.js"}',
            '/src/node_modules/external/node_modules/nested/index.js': 'export const nested = 1;\n'})
    parallel_files = {'/src/a.ts': 'import "./common"; export const a = 1;\n',
                      '/src/b.ts': 'import "./common"; export const b = 1;\n',
                      '/src/common.ts': 'export const common = 1;\n'}
    add('serial-shared-dependency', {'noLib': True, 'types': []}, extra=parallel_files,
        roots=['a.ts', 'b.ts'], producer='WorkGroup/collectFiles shared dependency')
    add('parallel-shared-dependency', {'noLib': True, 'types': []}, extra=parallel_files,
        roots=['a.ts', 'b.ts'], flags={'SingleThreaded': False},
        producer='WorkGroup/collectFiles shared dependency')
    child = json.dumps({'compilerOptions': {**BASE, 'noLib': True, 'types': [],
        'composite': True, 'outDir': './dist'}, 'files': ['dep.ts', 'nested.ts']}, separators=(',', ':'))
    add('reference-redirect-children', {'noLib': True, 'types': []},
        'import { dependency } from "../child/dep"; export const value = dependency;\n', {
            '/child/tsconfig.json': child,
            '/child/dep.ts': 'import { nested } from "./nested"; export const dependency = nested;\n',
            '/child/nested.ts': 'export const nested = 1;\n'}, references=[{'path': '../child'}],
        flags={'UseSourceOfProjectReference': True}, producer='redirected task child traversal/include reasons')
    add('repeated-casing-acquisition', {'noLib': True, 'types': []},
        'import "./FILE"; import "./FILE"; import "./file";\n',
        {'/src/file.ts': 'export {};\n'}, flags={'CaseInsensitive': True},
        producer='duplicate acquired casing/public DuplicateSourceFiles')
    add('parse-bind-diagnostics', {'noLib': True, 'types': []},
        'export const bad = ;\nexport const duplicate = 1; export const duplicate = 2;\n')


def digest(data):
    return hashlib.sha256(data).hexdigest()


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--check', action='store_true')
    args = parser.parse_args()
    build()
    data = ''.join(json.dumps(row, ensure_ascii=False, separators=(',', ':')) + '\n' for row in ROWS).encode()
    sources = ['upstream/TypeScript/tsc/internal/compiler/fileloader.go',
               'upstream/TypeScript/tsc/internal/compiler/filesparser.go']
    provenance = {'kind': 'authored-source-derived-environment-inputs',
                  'upstream': 'fed0bf24149fb1ed36039212648bafdafc1ea10e',
                  'requests': len(ROWS), 'requestsSha256': digest(data),
                  'sourceFiles': {path: digest((ROOT / path).read_bytes()) for path in sources},
                  'obligations': OBLIGATIONS, 'expectedSource': 'pristine original NewProgram only'}
    outputs = {'validation/program-corpus/requests-environment.jsonl': data,
               'validation/program-corpus/requests-environment-provenance.json':
                   (json.dumps(provenance, indent=2) + '\n').encode()}
    for path, content in outputs.items():
        target = ROOT / path
        if args.check:
            if target.read_bytes() != content:
                raise SystemExit('Generated environment input drift: ' + path)
        else:
            target.write_bytes(content)
    print(json.dumps({'requests': len(ROWS), 'requestsSha256': digest(data)}))


if __name__ == '__main__':
    main()
