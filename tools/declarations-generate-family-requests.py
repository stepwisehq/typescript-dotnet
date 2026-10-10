#!/usr/bin/env python3
"""Finite authored D1-D5 public-behavior inputs. Never generates expected output."""
import hashlib
import json
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parent.parent
OUT = ROOT / 'validation/declarations-corpus'
OPTIONS = dict(strict=True, noEmit=True, target='ES2022', module='ESNext',
               moduleResolution='Bundler', lib=['ES2022'], types=[])
rows = []
domains = {}

def case(name, domain, files, options=None, queries=(), roots=None, incremental=()):
    opts = OPTIONS | (options or {})
    if opts.get('noLib'):
        opts.pop('lib', None)
    roots = roots or list(files)
    inputs = [{'Path': '/tsconfig.json', 'Text': json.dumps({'compilerOptions': opts, 'files': roots}, separators=(',', ':'))}]
    inputs += [{'Path': '/' + path, 'Text': text} for path, text in files.items()]
    semantic = dict(PerFile=True, Repeat=True)
    if queries:
        semantic['Queries'] = [dict(File='/' + file, Identifier=identifier, Operation=operation, Occurrence=occurrence)
                               for file, identifier, operation, occurrence in queries]
    if incremental:
        semantic['IncrementalFiles'] = ['/' + path for path in incremental]
    rows.append(dict(Name=name, ConfigPath='/tsconfig.json', Files=inputs, Bind=True,
                     SingleThreaded=True, Semantic=semantic))
    domains[name] = domain

def q(identifier, operation='type', occurrence=0, file='main.ts'):
    return (file, identifier, operation, occurrence)

case('declaration-only-real-library', 'D1', {'main.d.ts': 'declare class PublicModel { readonly id: string; constructor(id: string); }\n'},
     queries=[q('PublicModel', 'construct', file='main.d.ts')])
case('no-lib-missing-real-globals', 'D1', {'main.ts': 'export class Model { value = 1; }\n'}, {'noLib': True})
case('invalid-global-array-arity', 'D1', {'main.d.ts': 'interface Array<T, U> { length: number; }\n'}, {'noLib': True})
case('global-augmentation-before-array-use', 'D1/D4', {
    'augment.ts': 'export {}; declare global { interface Array<T> { warehouse(): T; } }\n',
    'main.ts': 'const values: number[] = [1]; export const item: number = values.warehouse();\n'},
     queries=[q('item')])
merge_files = {'a.d.ts': 'interface RecordModel { left: number; }\n',
               'b.d.ts': 'interface RecordModel { right: string; }\n',
               'main.ts': 'export const record: RecordModel = { left: 1, right: "ok" };\n'}
for order, roots in [('forward', ['a.d.ts', 'b.d.ts', 'main.ts']), ('reverse', ['b.d.ts', 'a.d.ts', 'main.ts'])]:
    case('cross-file-merge-' + order, 'D1/D4', merge_files, roots=roots, queries=[q('record', 'properties')])
case('external-module-augmentation', 'D1/D4', {
    'model.ts': 'export interface Model { id: number; }\n',
    'augment.ts': 'import "./model"; declare module "./model" { interface Model { label: string; } }\n',
    'main.ts': 'import { Model } from "./model"; export const model: Model = { id: 1, label: "x" };\n'},
     queries=[q('model', 'properties')])
case('ambient-pattern-module', 'D1/D4', {
    'ambient.d.ts': 'declare module "warehouse-*" { export const value: number; }\n',
    'main.ts': 'import { value } from "warehouse-stock"; export const stock: number = value;\n'}, queries=[q('stock')])
case('incompatible-interface-merge', 'D4', {'a.d.ts': 'interface Model { value: number; }\n',
     'b.d.ts': 'interface Model { value: string; }\n', 'main.ts': 'export const model: Model = { value: 1 };\n'})
case('generic-inherited-default-constructor', 'D2', {'main.ts': '''export class Base<T> { constructor(public value: T) {} }
export class Derived extends Base<string> { extra = 1; }
export const instance = new Derived("ok");
'''}, queries=[q('Derived', 'base'), q('Derived', 'construct'), q('instance', 'properties')])
case('overloaded-constructors', 'D2', {'main.ts': '''export class Value {
 constructor(value: number);
 constructor(value: string);
 constructor(public value: number | string) {}
}
export const good = new Value(1); export const bad = new Value(true);
'''}, queries=[q('Value', 'construct')])
case('protected-default-constructor', 'D2/D3', {'main.ts': '''class Base { protected constructor(public value: number) {} }
export class Derived extends Base {}
export const denied = new Derived(1);
'''}, queries=[q('Derived', 'construct')])
case('abstract-and-implements', 'D2/D3', {'main.ts': '''interface Shape { area(): number; }
abstract class Base implements Shape { abstract area(): number; }
export class Incomplete extends Base {}
export const invalid = new Base();
'''})
case('constructor-super-flow', 'D3', {'main.ts': '''class Base { value = 1; }
export class Derived extends Base {
 field: number;
 constructor(flag: boolean) {
  if (flag) { super(); this.field = 1; } else { super(); this.field = 2; }
 }
}
export class Missing extends Base { constructor() { this.value = 2; super(); } }
'''})
case('strict-property-initialization-paths', 'D3', {'main.ts': '''export class Complete { value: number; constructor(flag: boolean) { if (flag) this.value = 1; else this.value = 2; } }
export class Incomplete { value: number; constructor(flag: boolean) { if (flag) this.value = 1; } }
export class EarlyReturn { value: number; constructor(flag: boolean) { if (flag) return; this.value = 1; } }
'''})
case('private-and-protected-origins', 'D3', {'main.ts': '''class Left { private key = 1; protected value = 2; }
class Right { private key = 1; protected value = 2; }
export let left: Left = new Right();
export const inaccessible = new Left().value;
'''})
case('lexical-private-members', 'D3', {'main.ts': '''class Base { #value = 1; read(): number { return this.#value; } }
export class Derived extends Base { #value = "own"; readOwn(): string { return this.#value; } }
export const instance = new Derived();
export const invalid = instance.#value;
'''}, queries=[q('instance', 'properties')])
case('accessors-and-auto-accessors', 'D3', {'main.ts': '''export class Model {
 accessor count: number = 1;
 get value(): number { return this.count; }
 set value(value: number) { this.count = value; }
}
export const model = new Model(); model.value = "bad";
'''}, queries=[q('model', 'properties')])
case('override-and-static-instance-mismatch', 'D3', {'main.ts': '''class Base { value = 1; static flag = true; }
export class Derived extends Base { override missing = 2; override value = "bad"; static flag = 1; }
'''}, {'noImplicitOverride': True})
case('static-block-and-static-this', 'D3', {'main.ts': '''export class Counter {
 static value: number;
 static { this.value = 1; }
 static read(): number { return this.value; }
}
export const count = Counter.read();
'''}, queries=[q('Counter', 'properties')])
case('anonymous-contextual-class-expression', 'D2/D5', {'main.ts': '''interface Constructor { new (value: number): { value: number }; }
export const Model: Constructor = class { constructor(public value: number) {} };
export const Anonymous = class { static label = "anonymous"; value = 1; };
export const instance = new Anonymous();
'''}, queries=[q('Model', 'construct'), q('Anonymous', 'construct'), q('instance', 'properties')])
case('generic-mixin-class', 'D2', {'main.ts': '''type Constructor = new (...args: any[]) => {};
function WithId<TBase extends Constructor>(Base: TBase) {
 return class extends Base { id = "item"; };
}
class Base { count = 1; }
export const Mixed = WithId(Base);
export const instance = new Mixed();
'''}, queries=[q('Mixed', 'construct'), q('instance', 'properties')])
case('extends-null-and-heritage-cycle', 'D2', {'main.ts': '''export class NullBase extends null { value = 1; }
export class Left extends Right {}
export class Right extends Left {}
'''}, queries=[q('NullBase', 'construct')])
case('enum-constants-member-unions', 'D4/D5', {'main.ts': '''export enum State { Empty, First = 1 << 2, Next = First + 1, Text = "ready" }
export const selected: State = State.Next;
export const key = State.Text;
export const invalid: State = 99;
'''}, queries=[q('State', 'declared'), q('selected'), q('key')])
case('enum-cycles-and-invalid-constants', 'D4', {'main.ts': '''export enum Cycle { A = B, B = A }
export const enum Computed { A = Math.random() }
export enum Missing { A = Math.random(), B }
'''})
case('ambient-enum-and-namespace-merge', 'D4', {'main.ts': '''declare enum Remote { A = 1, B }
export class Item { value = 1; }
export namespace Item { export const label = "item"; }
export enum Kind { A }
export namespace Kind { export const label = "kind"; }
export const item = new Item(); export const remote: Remote = Remote.B;
'''}, queries=[q('Item', 'properties'), q('Kind', 'properties'), q('remote')])
case('namespace-internal-alias-export-equals', 'D4', {'model.ts': '''namespace Warehouse { export class Item { value = 1; } }
import Alias = Warehouse.Item;
export = Alias;
''', 'main.ts': '''import Item = require("./model"); export const instance = new Item();
'''}, {'module': 'CommonJS', 'moduleResolution': 'Node10'}, queries=[q('instance', 'properties')])
case('invalid-namespace-export-merge', 'D4', {'main.ts': '''export class Model { value = 1; }
namespace Model { export const extra = 1; }
export interface Conflict { value: number; }
export type Conflict = string;
'''})
case('unique-symbol-and-well-known-computed-members', 'D5', {'main.ts': '''export const key: unique symbol = Symbol("warehouse");
export class Model { [key] = 1; [Symbol.toStringTag] = "Model"; }
export const model = new Model(); export const value = model[key];
'''}, queries=[q('model', 'properties'), q('value')])
case('computed-name-invalid-and-late-bound', 'D5', {'main.ts': '''declare const dynamic: string;
export interface Model { [dynamic]: number; }
export const key = "value";
export class Late { [key] = 1; }
export const instance = new Late();
'''}, queries=[q('instance', 'properties')])
case('malformed-selected-declarations', 'D2/D3/D4', {'main.ts': '''export class Broken { constructor(value:) {} get count(): { return 1; } }
export enum Bad { A = , B }
'''})
case('actual-expect-error-and-unused-directive', 'D1/D5', {'main.ts': '''class Model { private value = 1; }
// @ts-expect-error actual private access
export const hidden = new Model().value;
// @ts-expect-error unused source directive
export const valid: number = 1;
'''}, incremental=['main.ts'])
case('no-unused-private-parameters-generics-and-import', 'D5', {'dep.ts': 'export class Dependency {}\n', 'main.ts': '''import { Dependency } from "./dep";
export class Model<T> { private unused = 1; constructor(private parameter: number) {} }
function helper(unused: number): number { return 1; }
export const value = 1;
'''}, {'noUnusedLocals': True, 'noUnusedParameters': True}, incremental=['main.ts'])
for skip in [False, True]:
    case('skip-lib-check-' + str(skip).lower(), 'D1/D4', {'bad.d.ts': 'declare const invalid: MissingGlobal;\n', 'main.ts': 'export const value = 1;\n'}, {'skipLibCheck': skip})
case('no-check-actual-policy', 'D1', {'main.ts': 'export class Model { private value = 1; }\nexport const hidden = new Model().value;\n'}, {'noCheck': True})

# Authored actual package declaration boundaries, not replacement globals/libs.
helper_source = '''export class Model {
 #value = 1;
 get value(): number { return this.#value; }
 set value(value: number) { this.#value = value; }
}
export const model = new Model();
'''
helper_versions = {
    'missing-module': None,
    'missing-exports': 'export {};\n',
    'old-arity': 'export declare function __classPrivateFieldGet(receiver: any, state: any): any;\nexport declare function __classPrivateFieldSet(receiver: any, state: any, value: any): any;\n',
    'compatible-arity': 'export declare function __classPrivateFieldGet(receiver: any, state: any, kind: any, accessor: any): any;\nexport declare function __classPrivateFieldSet(receiver: any, state: any, value: any, kind: any, accessor: any): any;\n',
}
for version, declarations in helper_versions.items():
    files = {'main.ts': helper_source}
    if declarations is not None:
        files['node_modules/tslib/package.json'] = '{"name":"tslib","types":"index.d.ts"}'
        files['node_modules/tslib/index.d.ts'] = declarations
    case('downlevel-private-imported-helpers-' + version, 'D3/D5 imported helper module/export/arity diagnostics',
         files, {'target': 'ES2015', 'importHelpers': True}, roots=['main.ts'])
case('downlevel-private-imported-helpers-disabled', 'D3/D5 actual ImportHelpers false',
     {'main.ts': helper_source}, {'target': 'ES2015', 'importHelpers': False})

# These preserve source skipping policy; they do not claim JS/JSDoc checking.
js_source = '/** @type {number} */\nexport const value = "invalid when checked";\n'
case('external-js-skipped-by-no-check', 'D1 actual NoCheck skip policy for external source',
     {'main.js': js_source}, {'allowJs': True, 'checkJs': True, 'noCheck': True,
                             'lib': ['ES5'], 'skipLibCheck': True})
case('external-js-skipped-by-no-check-directive', 'D1 actual ts-nocheck skip policy',
     {'main.js': '// @ts-nocheck\n' + js_source},
     {'allowJs': True, 'checkJs': True, 'lib': ['ES5'], 'skipLibCheck': True})
case('external-js-skipped-by-check-js-false', 'D1 actual CheckJs false skip policy',
     {'main.js': js_source}, {'allowJs': True, 'checkJs': False,
                             'lib': ['ES5'], 'skipLibCheck': True})

# Required library declaration dependency, without generator/runtime bodies.
case('ambient-rest-binding-signature', 'D1/D2 typed ambient array-binding rest signature',
     {'main.ts': '''export interface Port<TNext> {
 next(...[value]: [] | [TNext]): { done: boolean; value: TNext };
}
declare const port: Port<number>;
export { port };
export declare function first([x, y]: [string, number]): typeof x;
export function read<T extends { value: number } | { value: string }, K extends keyof T>(obj: T, key: K): T[K] {
 return obj[key];
}
'''}, queries=[q('port', 'properties'), q('first'), q('read')])
case('ambient-rest-binding-grammar', 'D1/D2 actual malformed ambient parameter grammar',
     {'main.ts': '''export interface InvalidPort {
 next(...[value]: [number], other: number): void;
 reset([value = 0]: [number]): void;
 take([first, ...tail, last]: number[]): void;
 end([...tail,]: number[]): void;
}
'''})

# Replaces an authored historical capability ban, preserving its exact source.
# No libraries/globals are supplied: actual missing-global diagnostics remain.
case('global-this-read-current-capability', 'D1 source checkIdentifier globalThis value; authored ban retirement',
     {'main.ts': 'const x=globalThis;'}, {'noLib': True},
     queries=[q('x'), q('globalThis'), q('x', 'symbol'), q('globalThis', 'symbol')])

payload = ''.join(json.dumps(row, ensure_ascii=False, separators=(',', ':')) + '\n' for row in rows).encode()
source_names = ['internal/checker/checker.go', 'internal/checker/flow.go', 'internal/checker/grammarchecks.go',
                'internal/checker/relater.go', 'internal/checker/printer.go', 'internal/compiler/program.go']
provenance = dict(baseline='e7b81a2', upstream='fed0bf24149fb1ed36039212648bafdafc1ea10e',
    classification='authored-source-family-public-Program-semantic-inputs-not-original-test-bodies',
    cases=len(rows), requestsSha256=hashlib.sha256(payload).hexdigest(), domains=domains,
    sourceFiles={('upstream/TypeScript/tsc/' + name): hashlib.sha256((ROOT/'upstream/TypeScript/tsc'/name).read_bytes()).hexdigest() for name in source_names},
    observation='Complete existing Program construction records and actual global/bulk/per-file/repeated/incremental diagnostics; finite public type/property/base/constructor/symbol queries. No text filtering or source/options/library substitutions.',
    exclusions='This finite authored set is not a complete compiler-input inventory. Decorators and generator/async/JS/JSX/JSDoc checking bodies are external under D1-D6; explicit JS skip-policy rows do not check those bodies. Actual selected declaration errors remain observed.')
provenance_bytes = (json.dumps(provenance, indent=2) + '\n').encode()
if '--check' in sys.argv:
    assert (OUT/'requests-families.jsonl').read_bytes() == payload
    assert (OUT/'requests-families-provenance.json').read_bytes() == provenance_bytes
else:
    OUT.mkdir(parents=True, exist_ok=True)
    (OUT/'requests-families.jsonl').write_bytes(payload)
    (OUT/'requests-families-provenance.json').write_bytes(provenance_bytes)
print(json.dumps({'cases': len(rows), 'requestsSha256': provenance['requestsSha256']}))
