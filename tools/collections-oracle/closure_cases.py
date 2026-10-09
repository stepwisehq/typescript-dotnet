"""Concrete producer/contract omissions identified by independent source review.
Additional original-Go differential sources, never compiler test-pass claims.
"""
from pathlib import Path
import json
ROOT=Path(__file__).resolve().parents[2]
def cases():
 rows=[
 ('G2-merged-local-generic-inheritance','interface Base<T>{value:T} interface Base<T>{map<U>(f:(value:T)=>U):U} interface Derived<U> extends Base<U>{extra:U[]} declare const derived:Derived<number>;const mapped=derived.map(x=>x+1);function local<T>(value:T){interface Box<U>{outer:T;inner:U}const box:Box<number>={outer:value,inner:1};return box;}const result=local("a");interface Defaults<T=number>{value:T}interface Defaults<T=string>{tag?:string}'),
 ('G4-symbol-number-index-keys','interface Dictionary{[key:string]:string;[key:number]:string;[key:symbol]:number}declare const data:Dictionary;declare const token:symbol;const symbolValue=data[token];const numberValue=data[1];data[token]="bad";type Keys=keyof Dictionary;let symbolKey:Keys=token;let numberKey:Keys=1;type Common=keyof ({a:number;shared:string}|{b:boolean;shared:string});let common:Common="shared";let invalid:Common="a";'),
 ('G2-distinct-recursive-relations','interface Left<T>{value:T;next?:Left<T>;visit:(value:T)=>void}interface Right<U>{value:U;next?:Right<U>;visit:(value:U)=>void}declare let left:Left<string>;const good:Right<string>=left;const bad:Right<number>=left;declare let broad:Left<string|number>;const narrow:Right<string>=broad;'),
 ('G2-overload-inference-failure-retry','function run<T extends {name:string}>(value:T,f:(value:T)=>number):number;function run<T extends {id:number}>(value:T,f:(value:T)=>string):string;function run(value:any,f:any):any{return f(value);}const bad=run({name:"a"},value=>value.name);const named=run({name:"a"},value=>value.name.length);const identified=run({id:1},value=>"ok");'),
 ('G2-noinfer-callback-default','declare function select<T>(values:T[],fallback:NoInfer<T>):T;const good=select([1,2],3);const bad=select([1,2],"x");declare function empty<T=number>():T;const defaulted=empty();declare function apply<T,U>(value:T,f:(value:T)=>U):U;const mapped=apply({score:1},value=>value.score+1);const invalid=apply({score:1},(value:string)=>value.length);'),
 ('G2-generic-constraint-suggestion','function requireName<T>(value:T){const named:{name:string}=value;return named;}function bounded<T extends {name:string}>(value:T){const named:{name:string}=value;return named;}'),
 ('G3-array-constructor-generic-intersection','declare function tagged<T>(value:T):T&{tag:string};const factory=tagged(Array);const values=factory(1,2);const label=factory.tag;declare function constrained<T extends ArrayConstructor>(value:T):T&{tag:string};const constrainedFactory=constrained(Array);const more=constrainedFactory(3);'),
 ('G3-array-rest-alias-and-infer','type Args<T>=T[];function length<T>(...args:Args<T>):number{return args.length;}const count=length(1,2);type First<T>=T extends [infer H,...infer R]?H:never;const value:First<[string,number]>="a";'),
 ('G3-shadowed-array-alias-rest','function outer<T>(){{type T=number[];function inner(...args:T){return args.length;}return inner(1,2);}}const count=outer<string>();'),
 ]
 a='|'.join('{a:"a'+str(i)+'"}' for i in range(317));b='|'.join('{b:"b'+str(i)+'"}' for i in range(317))
 rows.append(('G2-intersection-size-error','type A='+a+';type B='+b+';type Product=A&B;declare const product:Product;'))
 return [dict(name=name,profile='Collections',fileName='/main.ts',text=text,storedObservations=False) for name,text in rows]
if __name__=='__main__':
 (ROOT/'validation/collections-corpus/closure-requests.jsonl').write_text(''.join(json.dumps(r,separators=(',',':'))+'\n' for r in cases()))
