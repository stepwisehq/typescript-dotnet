#!/usr/bin/env python3
"""Generate bounded parser fixtures, preserving modes, grammar and recovery intent."""
import base64,hashlib,json,pathlib,subprocess
root=pathlib.Path(__file__).resolve().parent.parent
rows=[]
def add(name,text='',**options):rows.append(dict(name=name,text=text,**options))
cases={
 'empty':'',
 'variables':'let x: number = 1, y = 2; const {a:b,...rest} = obj; let [first,,...tail] = arr;',
 'control':'if (x) y(); else z(); for(let i=0;i<3;i++) x+=i; for(const k in o){} for(const v of xs){} while(x--){} do{}while(false); switch(x){case 1:break;default:throw x;} try{}catch(e){}finally{}',
 'functions':'async function* f<T extends string>(x?:T,...rest: T[]): Promise<T> {yield await x; return x!;} const g = <T,>(x:T)=>x; function overload(x:string):void;',
 'classes':'@decorator export abstract class C<T> extends Base<T> implements I<T> { #p=1; static {this.x=1;} constructor(public readonly x:T){} get value():T{return this.x;} set value(v:T){} async *method<U>(x:U){yield x;} }',
 'types':'type T<X> = X extends infer U ? keyof U : never; type F=(x:number)=>string; type M<T>={readonly [K in keyof T as `get${K & string}`]?: T[K]}; type A = [name:string,...rest:number[]]; type Q = import("m").X; type R=typeof x; type B = A[0] | (string & {});',
 'interface-enum':'interface I<T> extends Base<T> { readonly p?:T; (x:T):void; new(x:T):I<T>; [k:string]:T; m<U>(x:U):T; } const enum E {A,B=2,C="c"} namespace N { export let x=1; } declare module "m" { export interface X{} }',
 'expressions':'const x = a?.b?.(1)?.[k] ?? (new C<T>(1)); const y = /[a-z]+/giu; const z=`head${x}tail`; const q={a,...b, get x(){return 1}, method(){}}; x satisfies T; const v=<T>x; const t=x as T; import.meta; import("m");',
 'imports':'import type {X as Y} from "m"; import d, * as ns from "n"; import eq = require("m"); export {x as y}; export type * from "m"; export default function(){}; export = x;',
 'source-phase-upstream':'import source a from "./a.wasm"; import source from from "./module.js"; import source from "./module.js"; import.source("./a.wasm", {with:{type:"webassembly"}});',
 'heritage-upstream':'class C extends Base<number> implements Contract<string> {} interface I extends Parent<boolean> {} interface Invalid implements Recovery {} interface MissingExtends extends A. {} class MissingImplements implements B. {}',
 'escaped-phase-upstream':r'import.s\u006furce("./a.wasm"); import.d\u0065fer("./a.js");',
 'using':'using resource = open(); await using asyncResource = openAsync();',
 'regex-diagnostics':r'const r=/(?<name>a)\k<missing>/u; const bad=/a{3,2}/u;',
 'unicode-literals':'const α="🦀\\ud800\\uD83E\\uDD80"; const 中文 = α + 0xffffffffffffffff; const x = `Ω\\n`; const 𐐀 = 1;',
 'directives':'/// <reference path="./a.ts"/>\n/// <reference types="node" resolution-mode="import"/>\n/// <reference lib="es2025"/>\n// @ts-ignore\nconst x=1;',
}
for name,text in cases.items():add(name,text)
malformed=['const = ;','function f( {','class C extends {}','interface I { p: ; m( }','const x = a?.;','type T = { [K in ]: };','import { from "m";','if (x { y();','const r=/unterminated\nnext();','const x = `head${;','export default;','let [a,...] = ;','for (let i=;i<;i++) {','try { throw } catch {','const x = f<T(1);','@dec class {','namespace N { export','type T = [a?:string,b:number];']
for i,text in enumerate(malformed):add(f'recovery-{i}',text)
for name,text in [('function','function f<T>(a:T):T {return a;}'),('class','class C { x=1; m(){return this.x;} }'),('template','const x=`a${b}c`;')]:
 for cutoff in sorted(set([1,len(text)//3,2*len(text)//3,len(text)-1])):add(f'truncated-{name}-{cutoff}',text[:cutoff])
add('tsx-elements','const x=<><A prop="α" {...rest}>text{value}<B /></A></>;',scriptKind=4)
add('tsx-recovery','const x=<A prop={}><B></A>;',scriptKind=4)
add('jsx-elements','const x=<div data-id="1">text {x}</div>;',scriptKind=2,externalModuleJSX=True)
add('js-syntax-diagnostics','interface I{} const x:number=1; function f<T>(x:T):T{return x;}',scriptKind=1)
add('js-commonjs','exports.x = 1; const x=require("m"); module.exports=x;',scriptKind=1)
add('json-valid','{"x":[1,true,null],"y":-1}',scriptKind=6)
add('json-recovery',"{'x':undefined, bad: NaN, // comment\n y: [1,,] } trailing",scriptKind=6)
add('declaration-file','declare const x: number; export interface I { x:number; }',fileName='/index.d.ts')
add('forced-module','const x=1;',externalModuleForce=True)
for kind in [1,3]:
 add('jsdoc-types-'+str(kind),'/** @template T\n * @param {T} x input\n * @returns {T}\n */ function f(x){return x;}',scriptKind=kind,resolveJSDoc=True)
 add('jsdoc-recovery-'+str(kind),'/** @param {Array.<} x\n * @param {number} x\n * @returns { */ function f(x){}',scriptKind=kind,resolveJSDoc=True)
add('jsdoc-reparse-upstream','/**\n * @typedef {(\n * "a" |\n * "b"\n * )[]} T\n */ const value=0;',scriptKind=1,resolveJSDoc=True)
add('jsdoc-import-parent-upstream','test("", async function(){ ;(/** @type {typeof import("a")} */ ({})); ;(/** @type {typeof import("a")} */ a); });',scriptKind=1,resolveJSDoc=True)
for payload in [b'const x="\xff";',b'const \xed\xa0\x80=1;',b'\xfffunction f(){}']:
 add('malformed-utf8-'+hashlib.sha256(payload).hexdigest()[:8],textBytesBase64=base64.b64encode(payload).decode())
module_cases={
 'runtime-function':'function f(){return <div/>;}',
 'ambient-function':'declare function f(){return <div/>;}',
 'runtime-class':'class C { m(){return <div/>;} }',
 'ambient-class':'declare class C { m(){return <div/>;} }',
 'ambient-namespace':'declare namespace N { const x=<div/>; }',
 'declare-const':'declare const x=<div/>;',
 'typequery-recovery':'type T = typeof (<div/>);',
 'typealias-recovery':'type T = (<div/>);',
 'call-typeargs-recovery':'f<(<div/>)>();',
 'jsx-typeargs-recovery':'const x=<A<(<div/>)> />;',
 'extends-typeargs-recovery':'class C extends A<(<div/>)> {}',
 'fragment-runtime':'const x=<><div/></>;',
}
for name,text in module_cases.items():
 for enabled in [False,True]:add('module-jsx-'+name+'-'+str(enabled),text,scriptKind=4,externalModuleJSX=enabled)
output=root/'validation/parser-corpus/grammar-requests.jsonl';output.write_text(''.join(json.dumps(row,ensure_ascii=True,separators=(',',':'))+'\n' for row in rows))
manifest=dict(upstreamCommit='fed0bf24149fb1ed36039212648bafdafc1ea10e',scope='Grammar/fuzz/helper submanifest; coverage.json inventories the complete corpus',grammarCases=len(rows),requestSha256=hashlib.sha256(output.read_bytes()).hexdigest(),modeCounts={str(k):sum(r.get('scriptKind',3)==k for r in rows) for k in [1,2,3,4,6]})
(root/'validation/parser-corpus/manifest.json').write_text(json.dumps(manifest,indent=2)+'\n');print(json.dumps(manifest))

fuzz_root=root/'upstream/TypeScript/tsc/internal/parser/testdata/fuzz/FuzzParser'
fuzz_output=root/'validation/parser-corpus/fuzz-requests.jsonl'
with fuzz_output.open('w') as output:
 subprocess.run([str(root/'tools/.cache/parser-oracle'),'--extract-fuzz',str(fuzz_root)],stdout=output,check=True)
manifest['fuzzCases']=len(fuzz_output.read_text().splitlines())
manifest['fuzzSources']=[dict(path=p.relative_to(root/'upstream/TypeScript').as_posix(),sha256=hashlib.sha256(p.read_bytes()).hexdigest(),license='Apache-2.0') for p in sorted(fuzz_root.iterdir()) if p.is_file()]
(root/'validation/parser-corpus/manifest.json').write_text(json.dumps(manifest,indent=2)+'\n')

# These named upstream tests probe normalized source text on original/reparsed
# JSDoc types. Select canonical IDs from independently discovered Go role fields.
helper_rows=[
 dict(name='JSDocTypeSourceSurvivesReparse',text='/**\n * @typedef {(\n *   "a" |\n *   "b"\n * )[]} T\n */\nconst value = 0;',scriptKind=1,resolveJSDoc=True),
 dict(name='JSDocTypeSourcePropagatesToConstructedReparse',text='/**\n * @param {{\n *   value: string\n * }} options\n */\nfunction foo(options) {}',scriptKind=1,resolveJSDoc=True),
]
baseline=subprocess.run([str(root/'tools/.cache/parser-oracle')],input=''.join(json.dumps(r)+'\n' for r in helper_rows),text=True,capture_output=True,check=True)
for row,line in zip(helper_rows,baseline.stdout.splitlines(),strict=True):
 nodes=json.loads(line)['nodes'];ids=[]
 for node in nodes:
  wanted=(node['dataType']=='JSDocTypeExpression' or node['kind']==346) if row['name']=='JSDocTypeSourceSurvivesReparse' else node['dataType']=='ParameterDeclaration'
  if wanted:
   ids.extend(field['nodeId'] for field in node['fields'] if field['name']=='Type' and not field['isNull'])
 if len(ids)!=(2 if row['name']=='JSDocTypeSourceSurvivesReparse' else 1):raise RuntimeError('Upstream JSDoc probe selection changed')
 row['textProbeIds']=ids
for included in [False,True]:helper_rows.append(dict(name='JSDocTokenPositionIncludesComment-'+str(included),text='/**doc*/ const x=1;',scriptKind=3,textProbeIds=[1],textProbeIncludeJSDoc=included))
helpers=root/'validation/parser-corpus/helpers-requests.jsonl'
helpers.write_text(''.join(json.dumps(r,separators=(',',':'))+'\n' for r in helper_rows))
manifest['helperCases']=len(helper_rows)
manifest['helperReference']='tsc/internal/parser/parser_test.go named JSDoc source-survival tests'
(root/'validation/parser-corpus/manifest.json').write_text(json.dumps(manifest,indent=2)+'\n')
