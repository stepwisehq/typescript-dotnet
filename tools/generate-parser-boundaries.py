#!/usr/bin/env python3
"""Target speculative state, top-level-await reparse, Unicode, and metadata."""
import base64,json,pathlib
root=pathlib.Path(__file__).resolve().parent.parent
rows=[]
def add(name,text,**options):rows.append(dict(name=name,text=text,**options))
for i,text in enumerate(['import fs from "fs";','import fs from "node:fs";','import x from "unknown-lib";','import a from "fs"; import b from "node:fs";','import a from "node:fs"; import b from "fs";','import a from "node:test"; import b from "test";','export * from "node:crypto"; import("fs");']):add('builtin-import-style-'+str(i),text)
for name,text in {
 'top-level-await':'export {}; await f(); const x=await g(); for await(const y of xs){}',
 'await-recovery':'export {}; await\nusing r = open(); const f=(await)=>await;',
 'speculative-directive':'const f = <T /*before*/>(x:T /*after*/)=>x;\n// @ts-expect-error\nf<// @ts-ignore\nstring>(1);',
 'unicode-byte-boundaries':'const 𐐀="🦀";\r\nconst café = /Ω/u;\u2028const s=`汉${𐐀}`;',
 'jsdoc-constructed-tuple':'/** @param {[string, number]} arg */ function f(arg){}',
 'jsdoc-constructed-function':'/** @param {function(this: Object, ...number): string} f */ function g(f){}',
 'jsdoc-import-return':'/** @returns {import("node:fs").Stats} */ function f(){}',
}.items():add(name,text,scriptKind=1 if name.startswith('jsdoc') else 3,resolveJSDoc=name.startswith('jsdoc'))
for name,text,kind in [('await','export {}; const x=await f();',3),('jsx','const x=<A a={x}>汉{value}</A>;',4),('jsdoc','/** @param {{value: string}} p */ function f(p){}',1)]:
 # Token/Unicode boundaries and every fifth byte exercise recovery, rather than
 # a large arbitrary random corpus. Preserve prefixes as raw bytes.
 payload=text.encode()
 cuts=sorted(set(range(0,len(payload),5))|{len(payload)-1})
 for cut in cuts:rows.append(dict(name=f'boundary-truncation-{name}-{cut}',textBytesBase64=base64.b64encode(payload[:cut]).decode(),scriptKind=kind,resolveJSDoc=kind==1))
for name,text in {
 'scalar-string':'"🦀"', 'scalar-number':'-123.5e2', 'scalar-null':'null',
 'array':'[1,{"α":true},null]', 'object':'{"escaped":"\\ud800","nested":{}}',
 'truncated-object':'{"x":', 'truncated-array':'[1,', 'missing-property':'{":1}',
 'unquoted-property':'{x:1}', 'trailing-input':'{} []', 'trailing-comma':'{"x":1,}',
 'empty':'', 'invalid-literal':'undefined',
}.items():add('json-mode-'+name,text,scriptKind=6)
for name,text in {
 'syntax-diagnostics':'interface I{} const x:number=<div/>;',
 'mismatched-tag':'const x=<A><B></A>;', 'attribute-recovery':'const x=<A p= q={}/>;',
 'comments':'const x=<>{/* comment */}<A/> text</>;',
 'jsdoc':'/** @param {string} x */ function f(x){return <A>{x}</A>;}',
}.items():add('jsx-mode-'+name,text,scriptKind=2,resolveJSDoc=name=='jsdoc')
for enabled in [False,True]:add('jsx-mode-module-'+str(enabled),'const x=<A/>;',scriptKind=2,externalModuleJSX=enabled)
(root/'validation/parser-corpus/boundaries-requests.jsonl').write_text(''.join(json.dumps(r,separators=(',',':'))+'\n' for r in rows))
print(json.dumps(dict(cases=len(rows))))
