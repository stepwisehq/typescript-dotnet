#!/usr/bin/env python3
"""Authored Program option/output/reference witnesses; expected values are Go-owned."""
import argparse
import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
PIN = 'fed0bf24149fb1ed36039212648bafdafc1ea10e'
SOURCE = 'upstream/TypeScript/tsc/internal/compiler/program.go'
SOURCE_SHA256 = '78427ea44a00ba5175d886b77a9b32b11d6fd917c26a3d2e53c5ac04c3d0b81c'
BASE = {'target': 'es2022', 'module': 'esnext', 'moduleResolution': 'bundler', 'noLib': True, 'types': []}
ROWS = []
OBLIGATIONS = []


def add(name, options=None, *, source_files=None, roots=None, references=None, extra=None, config_path='/src/tsconfig.json', flags=None, branch='verifyCompilerOptions'):
    sources = source_files or {'/src/main.ts': 'export const value = 1;\n'}
    opts = dict(BASE)
    opts.update(options or {})
    config = {'compilerOptions': opts, 'files': roots if roots is not None else [path.removeprefix('/src/') for path in sources]}
    if references is not None:
        config['references'] = references
    files = [{'Path': config_path, 'Text': json.dumps(config, ensure_ascii=False, separators=(',', ':'))}]
    files.extend({'Path': path, 'Text': text} for path, text in sources.items())
    files.extend({'Path': path, 'Text': text} for path, text in (extra or {}).items())
    request = {'Name': 'core-' + name, 'ConfigPath': config_path, 'Files': files, 'SingleThreaded': True}
    request.update(flags or {})
    ROWS.append(request)
    OBLIGATIONS.append({'name': request['Name'], 'producer': branch, 'kind': 'authored-source-branch-input', 'requestSha256': digest((json.dumps(request, ensure_ascii=False, separators=(',', ':')) + '\n').encode())})


def digest(data):
    return hashlib.sha256(data).hexdigest()


def build():
    add('baseline-options')
    for name, opts in [
        ('removed-baseurl', {'baseUrl': './mapped'}),
        ('removed-outfile', {'outFile': './bundle.js'}),
        ('removed-target-es5', {'target': 'es5'}),
        ('removed-module-amd', {'module': 'amd'}),
        ('removed-module-system', {'module': 'system'}),
        ('removed-module-umd', {'module': 'umd'}),
        ('removed-resolution-classic', {'moduleResolution': 'classic'}),
        ('removed-resolution-node10', {'moduleResolution': 'node10'}),
        ('removed-false-options', {'alwaysStrict': False, 'esModuleInterop': False, 'allowSyntheticDefaultImports': False, 'downlevelIteration': False}),
        ('strict-required', {'strictPropertyInitialization': True, 'exactOptionalPropertyTypes': True, 'strictNullChecks': False}),
        ('strict-inherited', {'strict': True, 'strictPropertyInitialization': True, 'exactOptionalPropertyTypes': True}),
        ('isolated-declarations-invalid', {'isolatedDeclarations': True, 'allowJs': True}),
        ('isolated-declarations-valid', {'isolatedDeclarations': True, 'declaration': True}),
        ('inline-map-conflicts', {'inlineSourceMap': True, 'sourceMap': True, 'mapRoot': './maps'}),
        ('composite-flags-disabled', {'composite': True, 'declaration': False, 'incremental': False}),
        ('incremental-known-config', {'incremental': True}),
        ('paths-key-and-substitution', {'paths': {'two*stars*': ['./target/*/*'], 'relative': ['target'], 'empty': [], 'null': None}}),
        ('paths-valid-relative-absolute', {'paths': {'valid/*': ['./target/*', '/other/*']}}),
        ('sources-and-maproots-without-maps', {'inlineSources': True, 'sourceRoot': './original', 'mapRoot': './maps'}),
        ('sources-and-maproots-with-maps', {'sourceMap': True, 'inlineSources': True, 'sourceRoot': './original', 'mapRoot': './maps'}),
        ('declaration-paths-without-declarations', {'declarationDir': './decl', 'declarationMap': True}),
        ('declaration-paths-with-declarations', {'declaration': True, 'declarationDir': './decl', 'declarationMap': True}),
        ('nolib-with-lib', {'lib': []}),
        ('preserve-const-enums-isolated', {'isolatedModules': True, 'preserveConstEnums': False}),
        ('preserve-const-enums-verbatim', {'verbatimModuleSyntax': True, 'preserveConstEnums': False}),
        ('checkjs-disabled-allowjs', {'checkJs': True, 'allowJs': False}),
        ('declaration-only-without-declaration', {'emitDeclarationOnly': True}),
        ('decorator-metadata-without-experimental', {'emitDecoratorMetadata': True}),
        ('jsx-factory-invalid-qualified', {'jsxFactory': 'A..factory', 'jsxFragmentFactory': 'A..fragment'}),
        ('jsx-fragment-without-factory', {'jsxFragmentFactory': 'Fragment'}),
        ('jsx-automatic-conflicts', {'jsx': 'react-jsx', 'jsxFactory': 'React.createElement', 'jsxFragmentFactory': 'React.Fragment', 'reactNamespace': 'React'}),
        ('jsx-classic-source-conflict', {'jsx': 'react', 'jsxImportSource': 'runtime', 'reactNamespace': 'not-a-name'}),
        ('jsx-factory-and-namespace', {'jsx': 'react', 'jsxFactory': 'React.createElement', 'reactNamespace': 'React'}),
        ('import-ts-extension-with-emission', {'allowImportingTsExtensions': True}),
        ('import-ts-extension-noemit', {'allowImportingTsExtensions': True, 'noEmit': True}),
        ('import-ts-extension-rewrite', {'allowImportingTsExtensions': True, 'rewriteRelativeImportExtensions': True}),
        ('package-conditions-wrong-resolution', {'moduleResolution': 'node10', 'resolvePackageJsonExports': True, 'resolvePackageJsonImports': True, 'customConditions': []}),
        ('bundler-wrong-module', {'module': 'amd'}),
        ('node-module-wrong-resolution', {'module': 'node16', 'moduleResolution': 'bundler'}),
        ('node-resolution-wrong-module', {'module': 'esnext', 'moduleResolution': 'nodenext'}),
        ('node-compatible-mode', {'module': 'nodenext', 'moduleResolution': 'nodenext'}),
    ]:
        add(name, opts)

    collision_sources = {'/src/main.ts': 'export const value = 1;\n', '/src/main.js': 'export const old = 2;\n'}
    add('js-overwrites-input', {'allowJs': True}, source_files=collision_sources, branch='verifyCompilerOptions/output-path collision')
    add('noemit-does-not-block-output', {'allowJs': True, 'noEmit': True}, source_files=collision_sources, branch='verifyCompilerOptions/output-path noEmit guard')
    add('tsx-ts-duplicate-js-output', {'jsx': 'react'}, source_files={'/src/item.ts': 'export const value = 1;\n', '/src/item.tsx': 'export const other = 2;\n'}, branch='verifyCompilerOptions/output-path multiple inputs')
    add('declarations-overwrite-input', {'declaration': True}, source_files={'/src/main.ts': 'export const value = 1;\n', '/src/main.d.ts': 'export declare const original: number;\n'}, branch='verifyCompilerOptions/declaration output-path collision')
    add('buildinfo-overwrites-input', {'incremental': True, 'tsBuildInfoFile': './main.ts'}, branch='verifyCompilerOptions/build-info collision')
    add('outdir-migration-layout', {'outDir': './dist'}, source_files={'/src/nested/main.ts': 'export const value = 1;\n'}, branch='verifyCompilerOptions/common directory migration')
    add('explicit-rootdir-layout', {'outDir': './dist', 'rootDir': './nested'}, source_files={'/src/nested/main.ts': 'export const value = 1;\n'}, branch='verifyCompilerOptions/common directory explicit root')
    add('outside-rootdir', {'rootDir': './nested', 'outDir': './dist'}, branch='checkSourceFilesBelongToPath')
    add('composite-import-not-listed', {'composite': True}, source_files={'/src/main.ts': 'import { dependency } from "./dep"; export const value = dependency;\n', '/src/dep.ts': 'export const dependency = 1;\n'}, roots=['main.ts'], branch='verifyCompilerOptions/composite file-list inclusion')
    add('composite-import-listed', {'composite': True}, source_files={'/src/main.ts': 'import { dependency } from "./dep"; export const value = dependency;\n', '/src/dep.ts': 'export const dependency = 1;\n'}, branch='verifyCompilerOptions/composite file-list inclusion')
    add('reference-missing', references=[{'path': '../missing'}], branch='verifyProjectReferences/missing config')
    child = lambda opts: json.dumps({'compilerOptions': {**BASE, **opts}, 'files': ['dep.ts']}, separators=(',', ':'))
    add('reference-not-composite', references=[{'path': '../child'}], extra={'/child/tsconfig.json': child({}), '/child/dep.ts': 'export const dependency = 1;\n'}, branch='verifyProjectReferences/composite requirement')
    add('reference-noemit', references=[{'path': '../child'}], extra={'/child/tsconfig.json': child({'composite': True, 'noEmit': True}), '/child/dep.ts': 'export const dependency = 1;\n'}, branch='verifyProjectReferences/noEmit prohibition')
    add('reference-buildinfo-collision', {'composite': True, 'tsBuildInfoFile': '/shared/build.tsbuildinfo'}, references=[{'path': '../child'}], extra={'/child/tsconfig.json': child({'composite': True, 'tsBuildInfoFile': '/shared/build.tsbuildinfo'}), '/child/dep.ts': 'export const dependency = 1;\n'}, branch='verifyProjectReferences/shared build-info output')
    add('reference-empty-parent', references=[{'path': '../child'}], roots=[], extra={'/child/tsconfig.json': child({'noEmit': True}), '/child/dep.ts': 'export const dependency = 1;\n'}, branch='verifyProjectReferences/empty parent suppresses option requirement')
    add('reference-source-output', {'composite': True}, source_files={'/src/main.ts': 'import { dependency } from "../child/dep"; export const value = dependency;\n'}, references=[{'path': '../child'}], extra={'/child/tsconfig.json': child({'composite': True, 'outDir': './dist'}), '/child/dep.ts': 'export const dependency = 1;\n'}, flags={'UseSourceOfProjectReference': True}, branch='project-reference source/output construction and verification')
    add('reference-output-missing', {'composite': True}, source_files={'/src/main.ts': 'import { dependency } from "../child/dep"; export const value = dependency;\n'}, references=[{'path': '../child'}], extra={'/child/tsconfig.json': child({'composite': True, 'outDir': './dist'}), '/child/dep.ts': 'export const dependency = 1;\n'}, branch='project-reference missing declaration output diagnostics')
    # Unicode owner/range comparison exercises the explicit raw filename role;
    # it does not ask an unsupported semantic checker to run.
    add('unicode-option-and-output-owner', {'rootDir': './nested', 'outDir': './dist', 'strictPropertyInitialization': True, 'strictNullChecks': False}, source_files={'/src/雪.ts': 'export const value = 1;\n'}, config_path='/src/配置.json', branch='verifyCompilerOptions/Unicode config and source diagnostic ownership')


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--check', action='store_true')
    args = parser.parse_args()
    if digest((ROOT / SOURCE).read_bytes()) != SOURCE_SHA256:
        raise SystemExit('Pinned Program source drift')
    build()
    names = [row['Name'] for row in ROWS]
    assert len(names) == len(set(names))
    raw = ''.join(json.dumps(row, ensure_ascii=False, separators=(',', ':')) + '\n' for row in ROWS).encode()
    provenance = {'schema': 1, 'pin': PIN, 'kind': 'authored source-grounded Program construction differential inputs, not original test bodies', 'cases': len(ROWS), 'requestsSha256': digest(raw), 'generator': 'tools/program-generate-core-requests.py', 'source': SOURCE, 'sourceSha256': digest((ROOT / SOURCE).read_bytes()), 'obligations': OBLIGATIONS, 'limits': ['Expected outputs are produced exclusively by the unmodified pinned Go NewProgram observer.', 'All cases use genuine parsed config and Bundled host; options and file content are not overridden by observation.', 'Semantic execution and output writing are not requested.', 'Incremental-without-config and internal SuppressOutputPathCheck factory values are not expressible in this config-based protocol; no unsupported input field or observer override is invented.']}
    generated = {'validation/program-corpus/requests-core.jsonl': raw, 'validation/program-corpus/requests-core-provenance.json': (json.dumps(provenance, indent=2, ensure_ascii=False) + '\n').encode()}
    for path, contents in generated.items():
        target = ROOT / path
        if args.check:
            if not target.exists() or target.read_bytes() != contents:
                raise SystemExit('Generated asset drift: ' + path)
        else:
            target.write_bytes(contents)
    print(f'{len(ROWS)} Program core input rows; expected outputs untouched')


if __name__ == '__main__':
    main()
