#!/usr/bin/env python3
"""Persist callsite-informed parser closures required for binding readiness."""
import json,pathlib
root=pathlib.Path(__file__).resolve().parent.parent
specimens=[
 ('jsdoc-typedef-multiple','/** @typedef T\n * @type {number}\n * @type {string}\n */ var x;',1),
 ('jsdoc-callback-template','/** @callback F\n * @template T\n * @returns {T}\n */ function f(){}',1),
 ('reserved-function','function class(){}',3),
 ('private-binding','var x, #y;',3),
 ('recovery-declarations','class C{ m() @bad } enum E{ @bad } function f<T,@bad>(){} type T=A<number,@bad>;',3),
 ('recovery-array','const [a,@bad]=x;',3),
 ('invalid-interface','interface 1 {}',3),
 ('invalid-typealias','type 1 = number;',3),
 ('js-assertion','x as T;',1),
 ('jsx-mismatch','const x=<A></B>;',4),
 ('import-attributes','import x from "m" with { @bad };',3),
]
path=root/'validation/parser-corpus/readiness-requests.jsonl'
path.write_text(''.join(json.dumps(dict(name='binder-readiness-'+name,text=text,scriptKind=kind,resolveJSDoc=True),separators=(',',':'))+'\n' for name,text,kind in specimens))
print(json.dumps(dict(cases=len(specimens))))
