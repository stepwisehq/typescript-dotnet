#!/usr/bin/env python3
"""Source-file binding specimens selected for symbols, scopes, flow and errors."""
import base64,hashlib,json,pathlib,subprocess
root=pathlib.Path(__file__).resolve().parent.parent
corpus=root/'validation/binder-corpus';rows=[]
def add(name,text,**options):rows.append(dict(name=name,text=text,**options))
cases={
 'scope-shadowing':'var x=0; {let x=1; const y=x;} function f(x){var y=x; {let z=y;} return y;}',
 'binding-patterns':'const {a:x,b=1,...rest}=obj; let [y,,...tail]=arr; function f({p,q=1},[r,...xs]){return p;}',
 'hoisted-functions':'f(); function f(){} function f(x:string):void; function f(x:any){}',
 'exports-local-split':'export function f(){} export class C{} export enum E{A} export namespace N{export const x=1;} export interface I{} export type T=string;',
 'merges':'class C{} interface C{x:number} namespace C{export const y=1;} function f(){} namespace f{export const p=1;} enum E{A} namespace E{export const z=1;}',
 'namespace-instantiation':'namespace N{export interface I{}} namespace N{export const enum E{A}} namespace N{export const x=1;}',
 'const-enum-only':'namespace N{export const enum E{A}} namespace N{export const enum F{B}}',
 'signatures-infer':'interface I<T>{(x:T):T;new(x:T):I<T>;[x:string]:T;m<U>(x:U):T;} type T<X>=X extends infer U?U:never; type M<T>={[K in keyof T]:T[K]};',
 'class-members':'class C<T>{#p=1;static x=2; constructor(public x:T){this.y=1;} get p(){return this.#p;} set p(v){this.#p=v;} optional?:T; method<U>(x:U){return this;}}',
 'computed-properties':'class C{["x"](){}[-1]=1;[Symbol.iterator](){return this;}[dynamic]=0;} const o={a:1,["x"]:2,[dynamic]:3,get p(){return 1;},set p(v){},m(){}};',
 'imports-exports':'import d,{x as y} from "m";import * as ns from "n";import eq=require("q"); export {y};export * from "m";export * as group from "n";',
 'ambient':'declare namespace N{interface I{} class C{} const x:number;} declare module "m"{export interface I{}} declare global{interface Window{x:number}}',
 'pattern-ambient':'declare module "*.json" {const v:any;export default v;} declare module "bad**"{}',
 'attributed-patterns':'declare module "*.json" with {type:"json"}{} declare module "*.json" with {type:"text"}{}',
 'empty-scopes':'function f(){} function g(){ {} try{}catch(e){} } const a=()=>{}; class C{static{}}',
 'loops-labels':'let x=0; outer:for(let i=0;i<3;i++){while(x<3){x++;if(x===2)continue outer;break;}} do{x--;}while(x);for(const k in o){x++;}for(const v of xs){x=v;}',
 'branches':'function f(x:any){if(typeof x==="string"&&x.length){x=1;}else{x=2;}const y=x?x:0;return y;}',
 'logical-optional':'let x:any; x&&=f(); x||=g(); x??=h(); const y=x?.a?.[k]?.(1)??0; x!.a;',
 'arrays-calls':'const a=[]; a.push(1); a.unshift(2); a[0]=3; delete a[0]; assert(a); returner();',
 'destructuring-flow':'let a,b; [a,b]=xs; ({a,b}=obj); for([a,b] of xs){} function f(x=1){return x;}',
 'try-catch-finally':'function f(x){try{if(x)return 1;throw x;}catch(e){x=e;}finally{if(x)x=0;}return x;}',
 'unreachable':'function f(x){if(x)throw x;return 1; x=3;} while(true){break;} function g(){throw 1;}',
 'iife':'let x=0; (function(){x=1;return;})(); (()=>{x=2;})(); async function f(){await g();} function* g(){yield x;}',
 'class-flow':'class C{#x; constructor(x){if(x)this.#x=x;else return;} static{let x=1; if(x)x=2;} m(){return this.#x;}}',
 'await-using':'export {}; await f(); await using x=open(); using y=open(); for await(const z of xs){}',
 'duplicate-types':'type T=string; type T=number; interface I{} type I=number;',
 'enum-conflict':'enum E{A} class E{} const enum F{A} enum F{B}',
 'default-conflicts':'export default function f(){} export default class C{} export default 1;',
 'export-type-suggestion':'import {T} from "m"; export type T;',
 'prototype-conflict':'class C{static prototype=1;}',
 'private-constructor':'class C{#constructor=1;}',
 'strict-module':'export {}; var eval=1; var arguments=1; delete eval; with(x){}; let implements=1;',
 'strict-class':'class C{m(eval){let arguments=1;let implements=1; {function f(){}}}}',
 'strict-prologue':'"use strict";var eval=1;let implements=1;delete x;with(x){}; label:let x=1;{function f(){}}',
 'escaped-prologue':r'"use \u0073trict";with(x){};var eval=1;',
 'reserved-module':'export {}; var await=1;',
 'namespace-export-global':'export as namespace Lib; export const x=1;',
 'namespace-export-nested':'export {}; namespace N{export as namespace Lib;}',
 'namespace-export-script':'export as namespace Lib;',
 'ambient-export-error':'export declare module "m"{}',
 'nonpattern-attributes':'declare module "m" with {type:"json"}{}',
 'namespace-export-modifier':'declare export as namespace Lib; export {};',
 'async-contextual-identifier':r'async function f(){let aw\u0061it=1;}',
 'export-type-related-suggestion':'type T=number; export type T;',
}
for name,text in cases.items():add(name,text)
add('global-exports-declaration','export as namespace Lib; export const x:number;',fileName='/lib.d.ts')
add('unicode-filename-module','export const α="🦀";',fileName='/d/é.ts')
add('forced-module','const x=1;',externalModuleForce=True)
add('tsx-attributes','export const x=<A prop="α" other={value} {...rest}/>;',scriptKind=4)
add('jsx-attributes','const x=<A prop="α" other={value}/>;',scriptKind=2,externalModuleJSX=True)
for name,text in {
 'commonjs':'exports.x=1;module.exports=function f(){}; const x=require("m");',
 'commonjs-type-export':'/** @typedef {number} T */ exports.x=1;',
 'expando':'function f(){} f.x=1;f.prototype.y=function(){};Object.defineProperty(f,"z",{value:1});',
 'this-assignment':'function C(){this.x=1;this.m=function(){};} C.prototype.p=1;',
 'replaceable-method':'class C{constructor(){this.m=function(){};}m(){}}',
 'jsdoc-generics':'/** @template T\n * @param {T} x\n * @returns {T}\n */ function f(x){return x;}',
 'jsdoc-overloads':'/** @overload\n * @param {number} x\n * @returns {string}\n */\n/** @param {number|string} x */ function f(x){return x;}',
 'jsdoc-namespace':'/** @typedef {{x:number}} N.T */ const x=1;',
 'jsdoc-import':'/** @import {A as B} from "m" */ const x=1;',
}.items():add(name,text,scriptKind=1,resolveJSDoc=True)
for name,text in [('object','{"α":[1,true,null]}'),('scalar','"🦀"'),('malformed','{x:[1,,]}')]:add('json-'+name,text,scriptKind=6)
for i,payload in enumerate([b'let \xce\xb1=1;',b'const x="\xff";',b'var x, #y;']):add('raw-bytes-'+str(i),'',textBytesBase64=base64.b64encode(payload).decode())
# Reuse the targeted readiness shapes, not the entire prior grammar suite.
rows.extend(dict(json.loads(line),name='binding-'+json.loads(line)['name']) for line in (root/'validation/parser-corpus/readiness-requests.jsonl').open())
# Real TS source units already split using the independent upstream test splitter.
upstream=[]
for line in (root/'validation/parser-corpus/upstream-requests.jsonl').open():
 request=json.loads(line)
 if 'packages/typescript/src/' in request['name']:upstream.append(request)
rows.extend(upstream)
path=corpus/'selected-requests.jsonl';path.write_text(''.join(json.dumps(row,separators=(',',':'))+'\n' for row in rows))
opaque_text='class A{#x=1;m(){return this.#x;}} class B{#x=2;} declare module "*.json" with {type:"json"}{} declare module "*.json" with {type:"text"}{}'
opaque=[]
for round in range(3):
 for i in range(11):opaque.append(dict(name=f'opaque-prior-{round}-{i}',text=f'class Prior{i}{{#x=1;}} declare module "*.prior{i}" with {{type:"{i}"}}{{}}'))
 opaque.append(dict(name='opaque-target-'+str(round),text=opaque_text))
op=corpus/'opaque-requests.jsonl';op.write_text(''.join(json.dumps(row,separators=(',',':'))+'\n' for row in opaque))
manifest=dict(upstreamRevision='fed0bf24149fb1ed36039212648bafdafc1ea10e',selectedCases=len(rows),opaqueCases=len(opaque),parserReadinessCases=11,realSourceUnits=len(upstream),requestsSha256=hashlib.sha256(path.read_bytes()).hexdigest(),opaqueRequestsSha256=hashlib.sha256(op.read_bytes()).hexdigest(),selection='Authored symbol/scope/diagnostic/flow cases plus three pinned real TS source units; opaque ID counters shifted between identical targets')
manifest['upstreamFiles']=[entry for entry in json.loads((root/'validation/parser-corpus/upstream-manifest.json').read_text())['files'] if entry['path'].startswith('packages/typescript/src/')]
manifest['upstreamPanics']=[]
(corpus/'manifest.json').write_text(json.dumps(manifest,indent=2)+'\n');print(json.dumps(manifest))
