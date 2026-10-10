#!/usr/bin/env python3
"""Source-family TypeScript inputs and public queries, never expected output."""
import hashlib
import json
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'validation/typesystem-corpus'
PIN = 'fed0bf24149fb1ed36039212648bafdafc1ea10e'
OPTIONS = dict(strict=True, noEmit=True, target='ES2022', module='ESNext',
               moduleResolution='Bundler', lib=['ES2022'], types=[])
rows=[]
domains={}

def q(identifier, operation='type', occurrence=0, file='main.ts'):
    return dict(File='/'+file, Identifier=identifier, Operation=operation, Occurrence=occurrence)

def pair(source, target):
    return dict(Source=q(source) if isinstance(source,str) else source,
                Target=q(target) if isinstance(target,str) else target)

def case(name, domain, files, options=None, queries=(), relations=()):
    if isinstance(files,str):files={'main.ts':files}
    semantic=dict(PerFile=True,Repeat=True)
    if queries:semantic['Queries']=list(queries)
    if relations:semantic['Relations']=list(relations)
    opts=OPTIONS | (options or {})
    inputs=[dict(Path='/tsconfig.json',Text=json.dumps(dict(compilerOptions=opts,files=list(files)),separators=(',',':')))]
    inputs += [dict(Path='/'+path,Text=text) for path,text in files.items()]
    rows.append(dict(Name=name,ConfigPath='/tsconfig.json',Files=inputs,Bind=True,SingleThreaded=True,Semantic=semantic))
    domains[name]=domain

case('intrinsic-literal-union-casing','T2/T6',r'''declare const upper: Uppercase<"ßfoo" | "ﬁoo">;
declare const lower: Lowercase<"İSPANYOL" | "ΟΣ">;
declare const capital: Capitalize<"ßfoo">;
declare const uncapital: Uncapitalize<"İfoo">;
''',queries=[q('upper'),q('lower'),q('capital'),q('uncapital')])
case('intrinsic-surrogate-and-context-casing','T2/T6',r'''declare const high: Uppercase<"\uD800x">;
declare const low: Lowercase<"A\uDC00B">;
declare const context: Lowercase<"ʕΣ" | "ʰΣ" | "ΣͅA">;
''',queries=[q('high'),q('low'),q('context')])
case('intrinsic-generic-template-instantiation','T1/T2/T3',r'''type Route<T extends string> = Uppercase<`api:${T}`>;
type Initial<T extends string> = Capitalize<`${T}Name`>;
declare function route<T extends string>(name:T):Route<T>;
export const result = route("users");
declare const initial: Initial<"account">;
declare const pattern: Uppercase<`key:${string}`>;
declare const accepted: "KEY:ABC";
declare const rejected: "key:abc";
''',queries=[q('result'),q('initial'),q('pattern')],relations=[pair('accepted','pattern'),pair('rejected','pattern')])
case('intrinsic-constraint-and-idempotence','T1/T2/T4',r'''type Stable<S extends string> = Uppercase<Uppercase<S>>;
declare const stable: Stable<"ready">;
declare const single: Uppercase<"ready">;
type Invalid = Uppercase<42>;
declare function onlyUpper<T extends Uppercase<string>>(value:T):T;
export const good = onlyUpper("READY");
export const bad = onlyUpper("ready");
''',queries=[q('stable'),q('single'),q('good')],relations=[pair('stable','single'),pair('single','stable')])
case('template-numeric-bigint-boolean-inference','T2/T3',r'''type NumberOf<S extends string> = S extends `${infer N extends number}` ? N : never;
type BigOf<S extends string> = S extends `${infer N extends bigint}` ? N : never;
type BoolOf<S extends string> = S extends `${infer B extends boolean}` ? B : never;
declare const integer: NumberOf<"42">;
declare const fractional: NumberOf<"1.5">;
declare const nonRoundtrip: NumberOf<"01">;
declare const big: BigOf<"123">;
declare const yes: BoolOf<"true">;
declare const invalid: BoolOf<"TRUE">;
''',queries=[q(x) for x in ['integer','fractional','nonRoundtrip','big','yes','invalid']])
case('template-prefix-suffix-and-empty-inference','T2/T3',r'''type Split<S extends string> = S extends `${infer A}:${infer B}` ? [A,B] : never;
type Head<S extends string> = S extends `${infer A}${infer Rest}` ? [A,Rest] : never;
declare const split: Split<"left:right:tail">;
declare const empty: Split<":">;
declare const supplementary: Head<"\u{1F600}abc">;
declare const missing: Split<"none">;
''',queries=[q('split'),q('empty'),q('supplementary'),q('missing')])
case('template-union-intersection-relationship','T2/T4',r'''type Route = `/users/${number}` | `/orders/${string}`;
declare const exact: "/users/12";
declare const route: Route;
declare const bad: "/users/no";
declare const narrowed: `/users/${number}` & `${string}/12`;
''',queries=[q('route'),q('narrowed')],relations=[pair('exact','route'),pair('bad','route'),pair('narrowed','route')])
case('template-source-complexity-diagnostic','T1/T2',r'''type Digit = "0"|"1"|"2"|"3"|"4"|"5"|"6"|"7"|"8"|"9";
type TooMany = `${Digit}${Digit}${Digit}${Digit}${Digit}${Digit}`;
''')
case('mapped-remapping-filter-and-modifiers','T2/T3',r'''interface Model { readonly id:number; title?:string; active:boolean; count:number; }
type Numeric<T> = { -readonly [K in keyof T as T[K] extends number ? `get${Capitalize<K & string>}` : never]-?: T[K] };
declare const selected: Numeric<Model>;
type Mutable<T> = { -readonly [K in keyof T]-?: T[K] };
declare const mutable: Mutable<Model>;
''',queries=[q('selected','properties'),q('mutable','properties')])
case('mapped-colliding-keys-value-union','T2/T4',r'''type Merge<T> = { [K in keyof T as Uppercase<K & string>]:T[K] };
declare const result: Merge<{left:number; LEFT:string; right:boolean}>;
declare const accepted: {LEFT:number|string; RIGHT:boolean};
declare const rejected: {LEFT:number; RIGHT:boolean};
''',queries=[q('result','properties')],relations=[pair('result','accepted'),pair('result','rejected')])
case('mapped-numeric-unique-symbol-keys','T2/T4/T5',r'''declare const token:unique symbol;
type Keys = { 0:string; name:number; [token]:boolean };
type Preserve<T> = {[K in keyof T as K]:T[K]};
type Strings<T> = {[K in keyof T as K extends string ? K : never]:T[K]};
declare const preserved:Preserve<Keys>;
declare const strings:Strings<Keys>;
declare const key:keyof Preserve<Keys>;
''',queries=[q('preserved','properties'),q('strings','properties'),q('key')])
case('mapped-homomorphic-readonly-tuple','T1/T2/T5',r'''type Box<T> = {[K in keyof T]:{value:T[K]}};
type Mutable<T> = {-readonly [K in keyof T]:T[K]};
declare const boxed:Box<readonly [label:string,count?:number,...flags:boolean[]]>;
declare const mutable:Mutable<typeof boxed>;
declare const array:Box<readonly number[]>;
''',queries=[q('boxed'),q('mutable'),q('array')])
case('mapped-generic-indexed-access-optionality','T1/T2/T4',r'''type Optional<T> = {[K in keyof T]?:T[K]};
declare function read<T,K extends keyof T>(object:Optional<T>,key:K):Optional<T>[K];
export const value=read({count:1},"count");
type PickKeys<T,K extends keyof T> = {[P in K]:T[P]};
declare const picked:PickKeys<{count:number;label:string},"count">;
''',queries=[q('value'),q('picked','properties')],options=dict(exactOptionalPropertyTypes=True,noUncheckedIndexedAccess=True))
case('reverse-mapped-inference-callbacks','T2/T3',r'''type Box<T> = {[K in keyof T]:{value:T[K]}};
declare function unbox<T>(value:Box<T>):T;
export const result=unbox({name:{value:"ready"},count:{value:3}});
declare function apply<T>(value:{[K in keyof T]:{value:T[K];read:(v:T[K])=>void}}):T;
export const contextual=apply({name:{value:"ready",read:v=>{v.toUpperCase();}},count:{value:1,read:v=>{v.toFixed();}}});
''',queries=[q('result','properties'),q('contextual','properties')])
case('generic-mapped-to-string-index-relation','T2/T4',r'''declare function compare<T extends string>(mapped:{[K in T]:number},index:{[key:string]:number}):void;
function test<T extends string>(mapped:{[K in T]:number},wrong:{[key:string]:string}) {
 const index:{[key:string]:number}=mapped;
 const bad:{[key:string]:string}=mapped;
 return index;
}
declare const mapped:{[K in "a"|"b"]:number};
declare const index:{[key:string]:number};
''',queries=[q('test')],relations=[pair('mapped','index'),pair('index','mapped')])
case('conditional-distribution-any-never','T2/T3',r'''type Choose<T> = T extends string ? "text" : "other";
type Wrapped<T> = [T] extends [string] ? "text" : "other";
declare const distributed:Choose<string|number>;
declare const wrapped:Wrapped<string|number>;
declare const anyResult:Choose<any>;
declare const neverResult:Choose<never>;
''',queries=[q(x) for x in ['distributed','wrapped','anyResult','neverResult']])
case('conditional-constrained-infer-and-deferred','T1/T2/T3',r'''type First<T> = T extends [infer H extends string,...unknown[]] ? H : never;
type Deferred<T,U> = T extends U ? T : never;
declare const first:First<["ready",number]>;
declare const rejected:First<[number,string]>;
declare function filter<T,U>(value:T,other:U):Deferred<T,U>;
export const result=filter("ready" as const,"other" as string);
''',queries=[q('first'),q('rejected'),q('result')])
case('conditional-recursion-and-alias-instantiation','T1/T2/T3',r'''type Flatten<T> = T extends readonly (infer U)[] ? Flatten<U> : T;
type Values<T> = T extends object ? {[K in keyof T]:Values<T[K]>}[keyof T] : T;
declare const flattened:Flatten<readonly (readonly (1|2)[])[]>;
declare const values:Values<{name:"ready";nested:{count:3}}>;
''',queries=[q('flattened'),q('values')])
case('conditional-instantiation-depth-diagnostic','T1/T2',r'''type Endless<T> = T extends unknown ? Endless<T> : never;
type TooDeep = Endless<"ready">;
''')
case('generic-default-constraints-and-errors','T1/T3',r'''interface Box<T extends {id:number} = {id:number}> {value:T}
declare function box<T extends {id:number} = {id:number}>(value?:T):Box<T>;
export const defaulted=box();
export const inferred=box({id:1,name:"ready"});
export const rejected=box({id:"bad"});
type InvalidDefault<T extends string = number> = T;
''',queries=[q('defaulted','properties'),q('inferred','properties')])
case('generic-dependent-and-circular-constraints','T1/T3',r'''declare function pick<T,K extends keyof T>(value:T,key:K):T[K];
export const inferred=pick({count:1,label:"ready"},"label");
export const rejected=pick({count:1},"missing");
type Circular<T extends T> = T;
''',queries=[q('inferred')])
case('variance-annotations-and-relationship','T1/T4',r'''interface Producer<out T> {get:()=>T}
interface Consumer<in T> {put:(value:T)=>void}
interface Bad<out T> {put:(value:T)=>void}
declare const narrow:Producer<"ready">;
declare const wide:Producer<string>;
declare const broadConsumer:Consumer<string>;
declare const narrowConsumer:Consumer<"ready">;
''',relations=[pair('narrow','wide'),pair('wide','narrow'),pair('broadConsumer','narrowConsumer'),pair('narrowConsumer','broadConsumer')])
case('inference-priorities-contravariance-and-noinfer','T1/T3/T4',r'''declare function choose<T>(values:readonly T[],fallback:NoInfer<T>):T;
export const good=choose(["red","blue"] as const,"red");
export const bad=choose(["red","blue"] as const,"green");
declare function consume<T>(first:(value:T)=>void,second:(value:T)=>void):T;
export const narrowed=consume((value:{id:number})=>{},(value:{id:number;name:string})=>{});
''',queries=[q('good'),q('narrowed','properties')])
case('generic-callback-context-and-return-inference','T3/T5',r'''declare function map<T,U>(values:T[],callback:(value:T)=>U):U[];
export const result=map([{count:1}],item=>({text:item.count.toFixed(),ready:true as const}));
declare function wrap<T>(callback:()=>T):T;
export const returned=wrap(()=>({id:1,label:"ready"}));
''',queries=[q('result'),q('returned','properties')])
case('generic-signature-canonical-contextual-relation','T3/T4/T5',r'''type Left = <T>(value:T)=>T;
type Right = <U>(value:U)=>U;
declare const left:Left;
declare const right:Right;
declare const bad:<T>(value:T)=>string;
declare function use<T>(callback:(value:T)=>T,value:T):T;
export const selected=use(left,{id:1});
''',queries=[q('selected','properties')],relations=[pair('left','right'),pair('right','left'),pair('bad','left')])
case('overload-generic-inference-and-recovery','T3/T4/T5',r'''declare function choose<T extends string>(value:T):{kind:"text";value:T};
declare function choose<T extends number>(value:T):{kind:"number";value:T};
export const text=choose("ready");
export const number=choose(42);
export const rejected=choose(true);
''',queries=[q('text','properties'),q('number','properties')])
case('variadic-generic-call-and-normalization','T1/T3/T5',r'''declare function concat<A extends unknown[],B extends unknown[]>(left:[...A],right:[...B]):[...A,...B];
export const result=concat([1,"ready"],[true]);
type Joined<T extends unknown[]>=[head:string,...T,tail:boolean];
declare const joined:Joined<[count:number,label?:string]>;
''',queries=[q('result'),q('joined')])
case('variadic-implied-arity-empty-slice','T1/T2/T3/T5',r'''type LastTwo<T> = T extends [...(infer Rest)[],...infer Pair extends [any,any]] ? Pair : never;
type FirstTwo<T> = T extends [...infer Pair extends [any,any],...(infer Rest)[]] ? Pair : never;
declare const exact:LastTwo<[1,2]>;
declare const longer:LastTwo<[1,2,3,4]>;
declare const first:FirstTwo<[1,2]>;
declare const empty:LastTwo<[]>;
''',queries=[q(x) for x in ['exact','longer','first','empty']])
case('variadic-readonly-rest-relation-and-errors','T1/T4/T5',r'''declare const mutable:[label:string,...counts:number[]];
declare const readonlyTuple:readonly [label:string,...counts:number[]];
declare const fixed:[string,number];
declare const short:[string];
function take(...args:[label:string,count:number,...flags:boolean[]]) {}
take("ready",1,true);
take("ready");
take("ready","bad");
''',relations=[pair('mutable','readonlyTuple'),pair('readonlyTuple','mutable'),pair('fixed','mutable'),pair('short','fixed')])
case('variadic-rest-function-inference','T3/T4/T5',r'''declare function bind<T extends unknown[],R>(callback:(head:string,...rest:T)=>R,head:string):(...rest:T)=>R;
export const bound=bind((head:string,count:number,ready:boolean)=>({head,count,ready}),"prefix");
export const result=bound(1,true);
bound("bad",true);
''',queries=[q('bound'),q('result','properties')])
case('intersection-private-origin-conflict-diagnostic','T2/T4',r'''class Left {private value=1;left=true;}
class Right {private value=2;right=true;}
declare const impossible:Left & Right;
export const rejected=impossible.value;
declare const left:Left;
declare const right:Right;
''',queries=[q('impossible')],relations=[pair('left','right'),pair('right','left')])
case('union-intersection-optionality-and-index-relation','T2/T4',r'''declare const branch:{kind:"left";left:number}|{kind:"right";right:string};
declare const wider:{kind:string};
declare const combined:{id:number}&{name:string};
declare const optional:{id?:number};
declare const required:{id:number};
declare const dictionary:{[key:string]:number};
declare const record:{left:number;right:number};
''',options=dict(exactOptionalPropertyTypes=True),relations=[pair('branch','wider'),pair('wider','branch'),pair('combined','required'),pair('optional','required'),pair('required','optional'),pair('record','dictionary')])
case('object-literal-union-arraylike-target-selection','T3/T4',r'''type Result={id:number;name:string}|readonly number[];
export const selected:Result={id:1,name:"ready"};
export const rejected:Result={id:"bad",name:"ready"};
declare function choose<T>(value:T|readonly T[]):T;
export const inferred=choose({id:1});
''',queries=[q('selected','properties'),q('inferred','properties')])
case('predicate-structural-signature-relationships','T4/T5',r'''interface Animal {kind:string}
interface Dog extends Animal {bark():void}
declare const dogGuard:(value:Animal)=>value is Dog;
declare const animalGuard:(value:Animal)=>value is Animal;
declare const booleanGuard:(value:Animal)=>boolean;
''',relations=[pair('dogGuard','animalGuard'),pair('animalGuard','dogGuard'),pair('dogGuard','booleanGuard'),pair('booleanGuard','dogGuard')])
case('inferred-predicates-and-flow','T3/T5',r'''export function isNumber(value:unknown) {return typeof value==="number";}
export function notNull<T>(value:T) {return value!==null;}
export function use(value:string|number|null) {
 if(isNumber(value)) { const numeric=value; return numeric.toFixed(); }
 if(notNull(value)) { const present=value; return present.toString(); }
 return "missing";
}
''',queries=[q('isNumber'),q('notNull'),q('numeric'),q('present')])
case('assertion-predicate-import-and-diagnostics','T4/T5',{'guard.ts':'''export function assertText(value:unknown):asserts value is string {
 if(typeof value!=="string") throw new Error("text required");
}
''','main.ts':'''import {assertText} from "./guard";
export function use(value:unknown) {assertText(value);const narrowed=value;return narrowed.toUpperCase();}
function wrong(value:string):value is number {return true;}
''',},queries=[q('narrowed'),q('use')])
case('type-predicate-this-and-generic-constraint','T1/T4/T5',r'''class Box<T> {
 value:T|undefined;
 has():this is this & {value:T} {return this.value!==undefined;}
}
export function use(box:Box<string>) {if(box.has()){const present=box.value;return present.toUpperCase();}return "missing";}
function invalid<T extends string>(value:T):value is number {return false;}
''',queries=[q('present'),q('use')])

case('non-strict-nullable-union-inference','T1/T2/T3',r'''declare const nullable:string|null|undefined;
declare const plain:string;
declare function identity<T>(value:T):T;
export const inferred=identity(Math.random()>0.5 ? null : {id:1});
export const widened=Math.random()>0.5 ? "ready" : undefined;
''',options=dict(strict=False,strictNullChecks=False),queries=[q('nullable'),q('inferred','properties'),q('widened')],relations=[pair('nullable','plain'),pair('plain','nullable')])
case('pattern-template-intersection-empty-object','T2/T4',r'''declare const pattern:`pre:${string}` & {};
declare const exact:"pre:ready";
declare const rejected:"other";
declare function preserve<T extends string>(value:T & {}):T;
export const preserved=preserve("pre:ready");
''',queries=[q('pattern'),q('preserved')],relations=[pair('exact','pattern'),pair('rejected','pattern')])
case('generic-variadic-base-constraint','T1/T3/T5',r'''export function copy<T extends [number,string]>(value:T):[...T] {return [...value];}
export const copied=copy([1,"ready"]);
export function tail<T extends unknown[]>(value:[number,...T]):T {const [first,...rest]=value;return rest;}
export const remaining=tail([1,"ready",true]);
''',queries=[q('copy'),q('copied'),q('tail'),q('remaining')])

case('generic-constructor-callback-normal-arity','T3/T4/T5',r'''declare class Factory<T> { constructor(executor:(accept:(value:T)=>void)=>void); }
export const factory=new Factory<number>(accept=>{accept();accept("wrong");accept(1);});
''',queries=[q('factory')])
case('promise-constructor-synchronous-resolve-arity','T4/T5',r'''export const promise=new Promise<number>(resolve=>{resolve();resolve("wrong");resolve(1);});
''',queries=[q('promise')])

case('intrinsic-mixed-nested-mapping-membership','T2/T4',r'''declare const upperLower:Uppercase<Lowercase<`pre:${string}`>>;
declare const capitalLower:Capitalize<Lowercase<string>>;
declare const upperMatch:"PRE:READY";
declare const upperMiss:"PRE:Ready";
declare const capitalMatch:"Ready";
declare const capitalMiss:"READY";
declare function nested<T extends string>(value:T):Uppercase<Lowercase<T>>;
export const inferred=nested("MiXeD");
''',queries=[q('upperLower'),q('capitalLower'),q('inferred')],relations=[pair('upperMatch','upperLower'),pair('upperMiss','upperLower'),pair('capitalMatch','capitalLower'),pair('capitalMiss','capitalLower')])

SOURCE_FILES=['checker/checker.go','checker/types.go','checker/inference.go','checker/relater.go',
              'checker/grammarchecks.go','checker/exports.go','checker/printer.go','stringutil/js_case.go','stringutil/js_case_generated.go']
data=''.join(json.dumps(row,separators=(',',':'),ensure_ascii=False)+'\n' for row in rows).encode()
provenance=dict(baseline='09630d6',upstream=PIN,classification='authored-source-family-genuine-Program-comparisons; no-original-body-claim',
                cases=len(rows),domains=domains,requestsSha256=hashlib.sha256(data).hexdigest(),
                sourceFiles={'upstream/TypeScript/tsc/internal/'+name:hashlib.sha256((ROOT/'upstream/TypeScript/tsc/internal'/name).read_bytes()).hexdigest() for name in SOURCE_FILES},
                observation='Complete genuine Program diagnostic channels plus public inferred/property types and finite IsTypeAssignableTo pairs. No private IDs/graph or diagnostic normalization.',
                options='Actual config/Bundled unchanged strictES2022/defaultskipLibCheckfalse; explicit per-case options retained.')
assets={OUT/'requests-families.jsonl':data,OUT/'requests-families-provenance.json':(json.dumps(provenance,indent=2)+'\n').encode()}
for path,value in assets.items():
    if '--check' in sys.argv:
        assert path.read_bytes()==value,'Stale type-system inputs/provenance: '+str(path)
    else:
        path.parent.mkdir(parents=True,exist_ok=True);path.write_bytes(value)
print(json.dumps(dict(cases=len(rows),relations=sum(len(x['Semantic'].get('Relations',[])) for x in rows),requestsSha256=provenance['requestsSha256'])))
