#!/usr/bin/env python3
"""Authored source-family protocol witnesses; expected output is pinned Go only."""
import hashlib
import json
from pathlib import Path
import sys

ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'validation/protocols-corpus'
OPTIONS=dict(strict=True,noEmit=True,target='ES2022',module='ESNext',moduleResolution='Bundler',lib=['ES2022','ESNext.Disposable'],types=[])
rows=[];domains={}

def q(name,operation='type',occurrence=0,file='main.ts'):
    return dict(File='/'+file,Identifier=name,Operation=operation,Occurrence=occurrence)

def case(name,domain,text,options=None,queries=()):
    files={'main.ts':text} if isinstance(text,str) else text
    opts=OPTIONS|(options or {})
    config=json.dumps({'compilerOptions':opts,'files':list(files)},separators=(',',':'))
    semantic=dict(PerFile=True,Repeat=True,IncrementalFiles=['/'+next(iter(files))])
    if queries:semantic['Queries']=list(queries)
    rows.append(dict(Name=name,ConfigPath='/tsconfig.json',Files=[dict(Path='/tsconfig.json',Text=config)]+[dict(Path='/'+n,Text=t) for n,t in files.items()],Bind=True,SingleThreaded=True,Semantic=semantic))
    domains[name]=domain

case('await-primitive-promise-union','P1', '''declare const mixed: number | Promise<string>;
export async function consume() { const resolved = await mixed; return resolved; }
export const result = consume();
''',queries=[q('resolved'),q('result')])
case('await-generic-recursive-assimilation','P1/T1', '''export async function unwrap<T>(value: T): Promise<Awaited<T>> { return await value; }
declare const nested: PromiseLike<PromiseLike<{ id: number }>>;
export const result = unwrap(nested);
''',queries=[q('result')])
case('await-custom-thenable-valid','P1', '''interface Thenable { then(callback: (value: string) => unknown): void; }
declare const value: Thenable;
export async function consume() { const resolved = await value; return resolved; }
export const result = consume();
''',queries=[q('resolved'),q('result')])
case('await-invalid-then-callback','P1', '''declare const value: { then(callback: number): void };
export async function consume() { const resolved = await value; return resolved; }
''',queries=[q('resolved')])
case('await-incompatible-then-this','P1', '''interface Other { other: string; }
interface Thenable { then(this: Other, callback: (value: number) => unknown): void; }
declare const value: Thenable;
export async function consume() { return await value; }
''')
case('await-recursive-thenable-cycle','P1', '''interface Recursive { then(callback: (value: Recursive) => unknown): unknown; }
declare const value: Recursive;
export async function consume() { return await value; }
''')
case('await-nullish-then-union','P1', '''declare const value: Promise<number> | null | undefined;
export async function consume() { const resolved = await value; return resolved; }
''',queries=[q('resolved')])
case('async-contextual-return-inference','P1/T3', '''declare function visit<T>(fn: () => Promise<T>): T;
export const result = visit(async () => ({ id: 1, label: "job" }));
export const callback: () => Promise<number> = async () => 1;
''',queries=[q('result'),q('callback')])
case('async-return-annotation-errors','P1', '''export async function wrong(): number { return 1; }
export async function mismatch(): Promise<string> { return 1; }
export async function rejected(): PromiseLike<number> { return 1; }
''')
case('async-es5-promise-annotation','P1', '''export async function consume(): Promise<number> { return await 1; }
export const result = consume();
''',options=dict(target='ES5',lib=['ES5','ES2015.Promise']),queries=[q('result')])
case('async-es5-missing-promise-value','P1/P5', '''export async function consume(): Promise<number> { return 1; }
''',options=dict(target='ES5',lib=['ES5']))
case('await-context-and-module-errors','P1/P5', '''export {};
function ordinary() { return await Promise.resolve(1); }
const value = await Promise.resolve(1);
''',options=dict(target='ES2015',module='CommonJS',moduleResolution='Node16'))
case('await-node-commonjs-top-level','P1/P5',{'main.cts':'''export const value = await Promise.resolve(1);
'''},options=dict(module='NodeNext',moduleResolution='NodeNext'))
case('generator-yield-return-next','P2', '''export function* generate(): Generator<number, string, boolean> {
  const resumed = yield 1;
  if (resumed) yield 2;
  return "done";
}
export const iterator = generate();
''',queries=[q('resumed'),q('iterator')])
case('generator-inferred-return-yield','P2/T3', '''export function* generate() { yield 1; yield "job"; return true; }
export const iterator = generate();
''',queries=[q('iterator')])
case('generator-contextual-signature','P2/T3', '''export const factory: () => Generator<number, string, boolean> = function* () {
  const resumed = yield 1;
  return resumed ? "yes" : "no";
};
''',queries=[q('resumed'),q('factory')])
case('generator-invalid-next-return','P2', '''export function* generate(): Generator<number, string, boolean> {
  const resumed: string = yield 1;
  yield "wrong";
  return 3;
}
''')
case('generator-yield-star-return','P2/P3', '''function* child(): Generator<number, string, void> { yield 1; return "done"; }
export function* parent() { const returned = yield* child(); return returned; }
export const iterator = parent();
''',queries=[q('returned'),q('iterator')])
case('generator-yield-star-sent-mismatch','P2/P3', '''declare function child(): Generator<number, void, number>;
export function* parent(): Generator<number, void, string> { yield* child(); }
''')
case('generator-generic-callback','P2/T3', '''declare function run<T>(fn: () => Generator<T, void, unknown>): T;
export const result = run(function* () { yield { id: 1 }; });
''',queries=[q('result')])
case('async-generator-awaited-yield-return','P1/P2', '''export async function* generate(): AsyncGenerator<number, string, boolean> {
  const resumed = yield Promise.resolve(1);
  if (resumed) yield 2;
  return Promise.resolve("done");
}
export const iterator = generate();
''',queries=[q('resumed'),q('iterator')])
case('async-generator-inferred-protocol','P1/P2', '''export async function* generate() { yield Promise.resolve(1); yield "job"; return true; }
export const iterator = generate();
''',queries=[q('iterator')])
case('async-generator-invalid-annotation','P2', '''export async function* generate(): Generator<number, void, unknown> { yield 1; }
''')
case('async-generator-delegation-sync-fallback','P2/P3', '''function* child(): Generator<number, string, void> { yield 1; return "done"; }
export async function* parent() { const returned = yield* child(); return returned; }
export const iterator = parent();
''',queries=[q('returned'),q('iterator')])
case('for-await-custom-slow-protocol','P3', '''declare const values: { [Symbol.asyncIterator](): { next(): Promise<{ value: number; done: false } | { value: string; done: true }> } };
export async function consume() { for await (const current of values) { const copied = current; } }
''',queries=[q('current'),q('copied')])
case('for-await-sync-promises-fallback','P1/P3', '''declare const values: Iterable<Promise<number>>;
export async function consume() { for await (const current of values) { const copied = current; } }
''',queries=[q('current'),q('copied')])
case('for-await-union-protocol','P3', '''declare const values: Iterable<string> | AsyncIterable<number>;
export async function consume() { for await (const current of values) { const copied = current; } }
''',queries=[q('current'),q('copied')])
case('for-await-malformed-next-result','P3', '''declare const values: { [Symbol.asyncIterator](): { next(): number } };
export async function consume() { for await (const current of values) {} }
''')
case('for-await-malformed-method-parameters','P3', '''declare const values: { [Symbol.asyncIterator](required: string): AsyncIterator<number> };
export async function consume() { for await (const current of values) {} }
''')
case('for-await-invalid-sync-fallback','P3', '''declare const values: { [Symbol.iterator](): { next(): { done: number; value: string } } };
export async function consume() { for await (const current of values) {} }
''')
case('for-await-explicit-binding-types','P3/P5', '''declare const values: AsyncIterable<number>;
export async function consume() { for await (const current: string of values) {} }
''')
case('using-nullish-and-valid-protocol','P4', '''export {};
declare const value: Disposable | null | undefined;
using resource = value;
using empty = null;
''',queries=[q('resource')])
case('await-using-async-and-sync-fallback','P4', '''declare const asyncValue: AsyncDisposable;
declare const syncValue: Disposable;
export async function consume() { await using first = asyncValue; await using second = syncValue; }
''')
case('using-async-only-suggestion','P4', '''declare const value: AsyncDisposable;
export async function consume() { using resource = value; }
''')
case('using-invalid-dispose-signatures','P4', '''export {};
using missing = { close(): void {} };
using required = { [Symbol.dispose](value: number): void {} };
''')
case('await-using-invalid-result','P4', '''export async function consume() { await using resource = { [Symbol.asyncDispose](): number { return 1; } }; }
''')
case('using-invalid-grammar','P4/P5', '''export {};
using { value } = { value: 1 };
using absent;
using list = null, extra = null;
''')
case('using-loop-context-grammar','P4/P5', '''export {};
for (using value in {}) {}
for (using value of [null]) {}
for (await using value of [null]) {}
''')
case('using-missing-disposable-library','P4/P5', '''export {};
using resource = null;
''',options=dict(lib=['ES2022']))
case('protocol-import-helpers-missing','P1/P2/P4/P5', '''export async function* generate() { await using resource = null; yield 1; }
''',options=dict(target='ES5',importHelpers=True))

payload=''.join(json.dumps(r,separators=(',',':'),ensure_ascii=False)+'\n' for r in rows).encode()
sourceFiles=['tools/protocols-generate-family-requests.py','docs/PROTOCOLS-CONTRACT.md',
 'upstream/TypeScript/tsc/internal/checker/checker.go','upstream/TypeScript/tsc/internal/checker/grammarchecks.go',
 'upstream/TypeScript/tsc/internal/checker/flow.go','upstream/TypeScript/tsc/internal/checker/emitsupport.go']
pr=dict(baseline='bb035b3',upstream='fed0bf24149fb1ed36039212648bafdafc1ea10e',classification='authored-source-family-genuine-Program-protocol-inputs',
        cases=len(rows),requestsSha256=hashlib.sha256(payload).hexdigest(),domains=domains,
        sourceFiles={n:hashlib.sha256((ROOT/n).read_bytes()).hexdigest() for n in sourceFiles},
        observation='Complete real Program/global/bulk/per-file/incremental/repeat diagnostics and finite owned identifier public types; no private protocol cache or emitted execution claim.')
rendered=(json.dumps(pr,indent=2)+'\n').encode()
if '--check' in sys.argv:
    assert (OUT/'requests-families.jsonl').read_bytes()==payload
    assert (OUT/'requests-families-provenance.json').read_bytes()==rendered
else:
    OUT.mkdir(parents=True,exist_ok=True);(OUT/'requests-families.jsonl').write_bytes(payload);(OUT/'requests-families-provenance.json').write_bytes(rendered)
print(json.dumps({'cases':len(rows),'requestsSha256':pr['requestsSha256']}))
