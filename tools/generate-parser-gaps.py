#!/usr/bin/env python3
"""Deliberate specimens for gaps observed in the initial coverage inventory."""
import json,pathlib
root=pathlib.Path(__file__).resolve().parent.parent
rows=[]
def add(name,text,**options):rows.append(dict(name=name,text=text,**options))
add('remaining-expressions','const C=class extends B{}; const big=123n; const a=[...xs]; tag`x${a}`; delete a.x; void a; debugger; with(a){};')
add('remaining-types','type F=abstract new<T>(x:T)=>T; class C { self():this{return this;} } type T=[string?,...number[]];')
add('namespace-exports','export * as ns from "m"; export as namespace N;')
add('import-attributes','import x from "m" with { type:"json", "mode":"test" }; export {x} from "m" with {type:"json"};')
add('jsx-namespace','const x=<svg:path xml:lang="en"/>;',scriptKind=4)
tags={
 'augments':'@augments {Base<T>}', 'implements':'@implements {I}', 'deprecated':'@deprecated old',
 'callback':'@callback F\n * @param {number} x\n * @returns {string}',
 'overload':'@overload\n * @param {string} x\n * @returns {number}',
 'override':'@override', 'private':'@private', 'protected':'@protected', 'public':'@public',
 'readonly':'@readonly', 'satisfies':'@satisfies {T}', 'see':'@see Foo.bar', 'this':'@this {Window}',
 'throws':'@throws {Error} bad', 'import':'@import { A as B } from "m"',
 'links':'Text {@link Foo.bar label} {@linkcode Foo} {@linkplain Foo text}',
 'rest-optional':'@param {...number} xs\n * @param {string=} name',
}
for name,tag in tags.items():add('jsdoc-gap-'+name,'/**\n * '+tag+'\n */ function f(x){}',scriptKind=3,resolveJSDoc=True)
errors={
 'element-access':'a[];', 'super':'class C extends B {m(){super;}}',
 'reference':'/// <reference bad="x" />\nconst x=1;', 'resolution-mode':'/// <reference types="a" resolution-mode="bad" />',
 'statement':'else {}', 'switch':'switch(x){ x(); }', 'property':'interface I { = }', 'enum':'enum E { 1 }',
 'typeparam':'function f<>(){}', 'typearg':'type T=A<,>;', 'linebreak':'function f(){throw\nx;}',
 'jsx-empty':'const x=<>;', 'jsx-parents':'const x=<A/><B/>;', 'jsx-unclosed':'const x=<A>text;', 'jsx-fragment-close':'const x=<><A/></A>;',
 'destructuring-object':'const {,} = obj;', 'destructuring-array':'const [...,] = arr;',
 'decorator-position':'function f(){@dec x();}', 'decorator-this':'function f(@dec this:T){}', 'decorator-property':'class C { public @dec x=1; }',
 'decorator-export':'@a export @b class C{}', 'optional-new':'new a?.b();',
 'predicate':'let x: a is string;', 'enum-separator':'enum E { A B }',
 'reserved-name':'class const {}', 'reserved-variable':'let if=1;', 'reserved-parameter':'function f(if){}',
 'union-function':'type T=string | ()=>void;', 'union-constructor':'type T=string | new()=>T;',
 'intersection-function':'type T=string & ()=>void;', 'intersection-constructor':'type T=string & new()=>T;',
 'typo-keyword':'intrface I{}', 'namespace-name':'namespace {}', 'interface-name':'interface {}', 'typealias-name':'type = number;',
 'variable-location':'if(true) let x=1;', 'call-type':'let x: A(1);', 'module-name':'module 1 {}',
 'try-finally':'try {} x();', 'instantiation-member':'f<T>.x;', 'import-name':'import {1} from "m";',
 'invalid-interface-name':'interface string {}', 'invalid-type-name':'type string=1;', 'invalid-namespace-name':'namespace string {}',
 'super-typeargs':'class C extends B { constructor(){super<T>();} }', 'block-destructuring':'{} = x;',
 'old-import-assert':'import x from "m" assert {type:"json"};',
 'unary-exponent':'const x=-a ** b;', 'assertion-exponent':'const x=<T>a ** b;',
 'private-parameter':'function f(#x){}', 'private-outside':'x.#y;', 'private-var':'let #x=1;', 'private-chain':'class C{#x; m(){return this?.#x;}}',
}
for name,text in errors.items():add('diagnostic-gap-'+name,text,scriptKind=4 if name.startswith('jsx') else 3)
js_errors=['import x=require("m");','export = x;','class C implements I{}','type T=number;','abstract class C{}','f<T>();','class C{constructor(public x){}}','x!;','const x=<T>y;','x satisfies T;']
for i,text in enumerate(js_errors):add('js-diagnostic-gap-'+str(i),text,scriptKind=1)
for name,tag in {'duplicate':'@returns {number}\n * @returns {string}','typedef-types':'@typedef {number} T\n * @type {string}','template-order':'@typedef {number} T\n * @template X','template-curly':'@template {T} {}'}.items():
 add('jsdoc-diagnostic-gap-'+name,'/**\n * '+tag+'\n */ const x=1;',scriptKind=1,resolveJSDoc=True)
(root/'validation/parser-corpus/gaps-requests.jsonl').write_text(''.join(json.dumps(r,separators=(',',':'))+'\n' for r in rows))
print(json.dumps(dict(cases=len(rows))))
