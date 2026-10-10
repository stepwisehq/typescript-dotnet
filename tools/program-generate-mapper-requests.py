#!/usr/bin/env python3
"""Explicit public in-process Project fixtures; expected only from pinned Go."""
import argparse
import base64
import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]


def generate():
    rows = []
    def add(name, response, options=None, source='<template />'):
        config = {'compilerOptions': {'noLib': True, 'types': [], 'target': 'es2022',
                  'module': 'esnext', 'moduleResolution': 'bundler', **(options or {})},
                  'files': ['Component.vue']}
        files = {'/src/tsconfig.json': json.dumps(config, separators=(',', ':')),
                 '/src/Component.vue': source}
        rows.append({'Name': 'mapper-' + name, 'ConfigPath': '/src/tsconfig.json',
                     'Files': [{'Path': path, 'Text': text} for path, text in files.items()],
                     'SingleThreaded': True, 'Bind': True, 'MapperProject': response})
    add('synthesized-success', {'Text': 'export const value = 1;', 'VirtualExtension': '.ts'})
    add('virtual-mts', {'Text': 'export {};', 'VirtualExtension': '.mts'},
        {'module': 'nodenext', 'moduleResolution': 'nodenext'})
    add('virtual-cts', {'Text': 'export {};', 'VirtualExtension': '.cts'},
        {'module': 'nodenext', 'moduleResolution': 'nodenext'})
    add('supplemental-success', {'Text': 'export {};', 'VirtualExtension': '.ts', 'Supplemental': [
        {'Text': 'export {};', 'VirtualExtension': '.mts'}, {'Text': 'export {};', 'VirtualExtension': '.cts'}]})
    text = 'export const value = 1;'
    mapping = json.dumps([[0, len(text), 0, len(text), 0, 1048575]], separators=(',', ':')).encode()
    add('verbatim-mapping', {'Text': text, 'VirtualExtension': '.ts',
        'MappingJSONBase64': base64.b64encode(mapping).decode()}, source=text)
    add('nil-map-failure', {'Text': 'export {};', 'VirtualExtension': '.ts', 'NilMappings': True})
    add('invalid-extension', {'Text': 'export {};', 'VirtualExtension': '.vue'})
    add('invalid-range', {'Text': 'export {};', 'VirtualExtension': '.ts',
        'MappingJSONBase64': base64.b64encode(b'[[0,100,0,100,0,1048575]]').decode()})
    add('parse-diagnostic', {'Text': 'export const bad = ;', 'VirtualExtension': '.ts'})
    add('option-diagnostic', {'Text': 'export {};', 'VirtualExtension': '.ts', 'OptionDiagnostic': True})
    for kind in range(6):
        add('transform-error-' + str(kind), {'ErrorKind': kind, 'ErrorText': 'authored callback failure'})
    for kind in range(5):
        add('project-error-' + str(kind), {'ProjectErrorKind': kind})
    return rows


def digest(data):
    return hashlib.sha256(data).hexdigest()


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--check', action='store_true')
    args = parser.parse_args()
    rows = generate()
    data = ''.join(json.dumps(row, separators=(',', ':')) + '\n' for row in rows).encode()
    sources = ['upstream/TypeScript/tsc/internal/contentmapper/transform.go',
               'upstream/TypeScript/tsc/internal/compiler/contentmapper_test.go',
               'upstream/TypeScript/tsc/internal/compiler/fileInclude.go']
    provenance = {'kind': 'authored-public-in-process-mapper-callback-values', 'cases': len(rows),
                  'requestsSha256': digest(data),
                  'sourceFiles': {path: digest((ROOT / path).read_bytes()) for path in sources},
                  'limits': ['ParsedConfig.ContentMappers is explicitly supplied through the original public source API.',
                             'Callback values pass through actual CompilerHost TransformAndParse and Program consumers.',
                             'No mapper process or unsupported semantic/global checker acquisition is performed.'],
                  'obligations': ['Canonical/supplemental public associations and original text/mapping/identity',
                                  'Failure stub transform identity and real typed failure diagnostics',
                                  'Virtual extension formats, actual parse/bind diagnostics, option diagnostics',
                                  'Supplemental include reasons and exact ExplainFiles bytes'],
                  'expectedSource': 'pristine original Program with identical public Project callbacks'}
    outputs = {'validation/program-corpus/requests-mapper.jsonl': data,
               'validation/program-corpus/requests-mapper-provenance.json': (json.dumps(provenance, indent=2) + '\n').encode()}
    for path, content in outputs.items():
        target = ROOT / path
        if args.check:
            if target.read_bytes() != content:
                raise SystemExit('Mapper fixture drift: ' + path)
        else:
            target.write_bytes(content)
    print(json.dumps({'cases': len(rows), 'requestsSha256': digest(data)}))


if __name__ == '__main__':
    main()
