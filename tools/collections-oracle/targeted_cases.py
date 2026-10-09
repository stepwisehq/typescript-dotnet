"""Authored contract interactions; original Go supplies every result.
These sources are differential cases, not upstream compiler assertion ports.
"""
from pathlib import Path
import json
ROOT=Path(__file__).resolve().parents[2]
def cases():
 return [
 ('G1-bind-only-redeclaration','let value=1; let value=2;'),
 ('G1-bind-and-check-errors','let item=1; let item="x"; let wrong:number="bad";'),
 ('G2-default-constraint-success','interface Box<T,U=T[]>{value:T;others:U} function first<T extends {name:string}>(x:T):string{return x.name;} const box:Box<number>={value:1,others:[2]};const value=first({name:"ok",extra:1});'),
 ('G2-default-and-constraint-errors','type Broken<T=U,U=number>=T; type Self<T=T>=T; type Order<T=string,U>=T; function only<T extends {name:string}>(x:T):T{return x;} const bad=only({name:1});'),
 ('G2-legal-recursive-instantiation','interface Link<T>{value:T;next?:Link<T>} const link:Link<number>={value:1,next:{value:2}};let wrong:Link<string>=link;'),
 ('G2-instantiation-depth-error','type Recursive<T>=T extends string?Recursive<T>:T;const value:Recursive<"x">="x";'),
 ('G2-circular-generic-constraint','type Bad<T extends T>=T; type Pair<T extends U,U extends T>=[T,U]; declare let value:Bad<number>;'),
 ('G2-contextual-generic-overloads','function choose<T>(x:T[]):T|undefined;function choose<T>(x:T):T;function choose(x:any):any{return x;}const one=choose([1,2]);const record=choose({x:1});const callback:<T>(x:T)=>T=x=>x;const named=callback("a");'),
 ('G3-union-array-synthetic-method','declare let xs:number[]|string[];const fn=xs.map;const values=xs.map(x=>x);'),
 ('G3-tuple-optional-rest-and-readonly','type Labeled=[name:string,count?:number,...tags:string[]];const tuple:Labeled=["a",2,"b"];const tail=tuple[2];const fixed:readonly [string,number]=["a",1];const beyond=fixed[2];fixed[0]="x";'),
 ('G3-array-relationships-and-bounds','const values:number[]=[1,2];const readonlyValues:readonly number[]=values;const invalid:number[]=readonlyValues;values[0]="wrong";const length=values.length;'),
 ('G3-receiver-predicate-and-this-return','const values:(string|number)[]=[1,"a"];const sorted=values.sort((a,b)=>0);if(values.every((value):value is number=>typeof value==="number")){const first=values[0];}declare let readonlyValues:readonly (string|number)[];if(readonlyValues.every((value):value is string=>typeof value==="string")){const first=readonlyValues[0];}'),
 ('G4-utility-array-tuple-preservation','type Row={name:string;score:number;active?:boolean};const required:Required<Row>={name:"a",score:1,active:true};const readonlyTuple:Readonly<[string,number]>=["a",1];const partial:Partial<Row>={score:2};const picked:Pick<Row,"name"|"score">={name:"b",score:3};const omitted:Omit<Row,"active">=picked;let excluded:Exclude<string|number,string>=1;let extracted:Extract<string|number,string>="a";let nonnull:NonNullable<string|null|undefined>="a";'),
 ('G4-conditional-infer-index-and-reverse','type Element<T>=T extends (infer U)[]?U:never;type Values<T>=T[keyof T];type Mutable<T>={-readonly [K in keyof T]-?:T[K]};declare function infer<T>(x:Readonly<T>):T;const result=infer({name:"x",score:1});const element:Element<string[]>="a";const value:Values<{a:number;b:string}>=1;const mutable:Mutable<{readonly name?:string}>={name:"a"};'),
 ('G4-index-keyof-invalid','interface Dictionary{[key:string]:number}const data:Dictionary={a:1};const number=data["a"];data["b"]="x";type Keys=keyof {a:number;b:string};let key:Keys="missing";type Invalid={a:number}["bad"];'),
 ('G5-loop-evolving-array-and-capture','const out=[];for(let i=0;i<3;i++){if(i===1)continue;out.push(i);}let n=0;while(n<2){out.push(n++);}do{n--;if(n===0)break;}while(n>0);const callbacks=[];for(const value of out){callbacks.push(()=>value);}const first=callbacks[0]();'),
 ('G5-loop-narrowing-and-writes','function sum(xs:number[]):number{let total=0;for(const x of xs){total+=x;}return total;}function narrow(value:string|number){while(typeof value==="string"){value=value.length;}return value;}const sumValue=sum([1,2]);const narrowed=narrow("a");'),
 ('G5-invalid-loop-iteration-and-control','for(const value of 1){}let n:number=0;while(n<2){n+="wrong";break;}continue;'),
 ]
def write():
 rows=[dict(name=name,profile='Collections',fileName='/main.ts',text=text,storedObservations=False) for name,text in cases()]
 # Explicit roots from the actual Go parsed graph: declaration identifiers,
 # both union-property accesses and their member-name symbols.
 rows[8]['queryNodeIds']=[10893,10902,10903,10905,10909,10911,10913]
 dest=ROOT/'validation/collections-corpus/targeted-requests.jsonl'
 dest.write_text(''.join(json.dumps(row,separators=(',',':'))+'\n' for row in rows))
if __name__=='__main__':write()
