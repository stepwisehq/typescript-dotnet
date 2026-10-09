#!/usr/bin/env python3
"""Authored contract cases; expected results come only from the actual pinned Go program."""
import base64,hashlib,json,pathlib,subprocess,time
ROOT=pathlib.Path(__file__).resolve().parents[1]
OUT=ROOT/'validation/callables-corpus'
TINY=[
 ('typed-call','function f(x:number):number {return x;} const y=f(1);'),
 ('wrong-call','function f(x:number):number {return x;} const y=f("bad");'),
 ('context-callback','declare function apply(f:(x:number)=>number):number; const result=apply(x=>x);'),
 ('branch-return','function f(x:boolean){if(x)return 1;else return "no";} const y=f(true);'),
 ('function-relation','let f:(x:{a:number})=>number; let g:(x:{a:number;b:string})=>number; f=g;')
]
MATRIX=[
 ('C1-optional-default','function f(x:number,y:string="yes",z?:boolean):string{return y;}const a=f(1);const b=f(1,2);'),
 ('C1-missing-extra-arity','function f(x:number,y:string):number{return x;}f();f(1);f(1,"a",true);'),
 ('C1-overload-selection','function f(x:"a"):number;function f(x:"b"):string;function f(x:string):number|string{return x==="a"?1:"yes";}const a=f("a");const b=f("b");const c=f("other");'),
 ('C1-overload-reversed','function f(x:"b"):string;function f(x:"a"):number;function f(x:string):number|string{return x==="a"?1:"yes";}const a=f("a");const b=f("b");'),
 ('C1-overload-incompatible','function f(x:string):string;function f(x:number):number;function f(x:number):number{return x;}'),
 ('C1-default-forward-shadow','const x=1;function f(a:number=b,b:number=1){return a;}function g(x:string,y=x){return y;}const y=g("ok");'),
 ('C1-declare-external','declare function f(x:{a:number}):{b:string};declare let value:{a:number};const out=f(value);'),
 ('C1-this-receiver','interface Box{value:number;m(this:Box,x:number):number}const b:Box={value:1,m(x){return this.value+x;}};const a=b.m(2);const detached=b.m;const bad=detached(2);'),
 ('C1-invalid-this-positions','function f(x:number,this:{a:number}){return this.a;}const arrow=(this:{a:number})=>this.a;'),
 ('C2-not-callable-null','declare let n:number;declare let maybe:((x:number)=>number)|undefined;const a=n(1);const b=maybe(1);'),
 ('C2-overload-callback','declare function choose(f:(x:number)=>number):number;declare function choose(f:(x:string)=>string):string;const a=choose(x=>x);'),
 ('C2-call-fresh-excess','function f(x:{a:number}):number{return x.a;}const a=f({a:1,b:2});const obj={a:1,b:2};const b=f(obj);'),
 ('C2-call-nested-wrong','function f(x:{item:{name:string}}){return x.item.name;}f({item:{name:1}});'),
 ('C2-unknown-name','const value=missing(1);function f(x:number){return missingInner(x);}'),
 ('C3-context-nested','declare function outer(f:(x:{a:number})=>(y:string)=>number):number;const n=outer(x=>y=>x.a);'),
 ('C3-context-wrong-return','const f:(x:{a:number})=>number=x=>x.a;const bad:(x:string)=>number=x=>x;'),
 ('C3-inferred-property-return','function f(){return {a:1,b:"s"};}const out=f();const x:number=out.b;'),
 ('C3-self-recursive-return','function f(){return f();}const x=f();'),
 ('C3-mutual-return','function a(){return b();}function b(){return a();}const x=a();'),
 ('C3-recursive-annotated','function f(x:boolean):number{if(x)return f(false);return 1;}const out=f(true);'),
 ('C3-capture-default','function outer(x:number){function inner(y=x){return y;}return inner;}const f=outer(1);const y=f();'),
 ('C3-implicit-any','function f(x){return x;}const value=f(1);const g=(y)=>y;'),
 ('C3-predicate-typeof','function isString(value:string|number){return typeof value==="string";}function use(value:string|number){if(isString(value)){const s:string=value;return s;}return value;}'),
 ('C3-predicate-null','function present(value:{a:number}|undefined){return value!==undefined;}function use(value:{a:number}|undefined){if(present(value))return value.a;return 0;}'),
 ('C3-predicate-assigned-parameter','function isString(value:string|number){value=1;return typeof value==="string";}function use(value:string|number){if(isString(value)){const s:string=value;}return value;}'),
 ('C3-predicate-annotated-boolean','function isString(value:string|number):boolean{return typeof value==="string";}function use(value:string|number){if(isString(value)){const s:string=value;}return value;}'),
 ('C3-predicate-multiple-return','function isString(value:string|number,flag:boolean){if(flag)return typeof value==="string";return false;}function use(value:string|number){if(isString(value,true)){const s:string=value;}return value;}'),
 ('C3-predicate-conditional-return','function isString(value:string|number,flag:boolean){return flag?typeof value==="string":false;}function use(value:string|number){if(isString(value,true)){const s:string=value;}return value;}'),
 ('C1-parameter-order-self-default','function order(a?:number,b:number){return b;}function self(a:number=a){return a;}function wrong(a:number="bad"){return a;}'),
 ('C4-method-vs-function-variance','interface Base{a:number}interface Derived extends Base{b:string}declare let narrow:(x:Derived)=>number;declare let wide:(x:Base)=>number;wide=narrow;narrow=wide;declare let mn:{m(x:Derived):number};declare let mw:{m(x:Base):number};mw=mn;mn=mw;'),
 ('C4-return-void-special','declare let f:()=>number;declare let v:()=>void;v=f;f=v;const ignored:()=>void=()=>1;'),
 ('C4-callable-property-merge','interface F{(x:number):number;tag:string}interface F{(x:string):string}declare let f:F;const n=f(1);const s=f("a");f.tag="ok";f.tag=1;'),
 ('C4-method-inherit-conflict','interface A{m(x:number):number}interface B extends A{m(x:string):number}'),
 ('C4-hidden-this-instantiated-call','type A={};interface B extends A{(x:number):number}interface C extends B{}declare let f:C;const y=f(1);'),
 ('C4-callable-alias-heritage','type F={ (x:number):number;tag:string };interface G extends F{label:boolean}declare let g:G;const x=g(1);'),
 ('C4-recursive-callable-property','interface A{next:()=>A;value:number}interface B{next:()=>B;value:number}declare let a:A;declare let b:B;a=b;b=a;const c=a.next().next();'),
 ('C4-recursive-callable-mismatch','interface A{next:()=>A;value:number}interface B{next:()=>B;value:string}declare let a:A;declare let b:B;a=b;'),
 ('C4-union-call','type F=((x:"a")=>number)|((y:"a"|"b")=>string);declare let f:F;const a=f("a");const bad=f("b");'),
 ('C4-union-different-params','type F=((x:number)=>number)|((y:string)=>string);declare let f:F;const out=f(1);'),
 ('C4-intersection-call','type F=((x:number)=>number)&((x:string)=>string);declare let f:F;const n=f(1);const s=f("a");const bad=f(true);'),
 ('C4-function-property-expando','function f(x:number){return x;}f.tag="label";const s=f.tag;const n:number=f.tag;const y=f(1);'),
 ('C5-flow-auto-variable','let x;if(true)x=1;else x="s";const y=x;function f(){let value;value={a:1};return value.a;}'),
 ('C5-return-fallthrough','function f(x:boolean){if(x)return 1;}function g(x:boolean):number{if(x)return 1;}function empty(){}const a=f(false);const b=empty();'),
 ('C5-throw-never','function fail(){throw "error";}function f(x:boolean):number{if(x)return 1;return fail();}const y=fail();'),
 ('C5-this-arrow-capture','const obj={value:1,m(){const read=()=>this.value;return read();}};const x=obj.m();function missing(){return this.foo;}'),
 ('C5-assignment-capture','let x:number|undefined;function f(){x=1;return x;}const before:number=x;const after=f();'),
 ('C5-operators-invalid','function f(x:number,y:string){const a=x+y;const b=x-y;const c=x&y;const d=x===y;return !x?void 0:typeof y;}'),
 ('C5-nullish-logical','function f(x:{a:number}|undefined){const value=x??{a:1};if(x&&x.a)return x.a;return value.a;}'),
 ('C5-function-local-type-shadow','type T=number;function f(){type T=string;const x:T=1;return x;}const value=f();'),
 ('C5-auto-implicit-capture','let value;function use(){return value;}const out=use();'),
 ('C4-did-you-mean-call','function create(){return {a:1};}const value:{a:number}=create;function scalar(){return 1;}const numberValue:number=scalar;'),
 ('C4-object-method-bad-context','const target:{handler:(x:number)=>number}={handler(x){return "bad";}};const data:{handler:number}={handler(x:number){return x;}};'),
 ('C1-invalid-method-parameter-modifiers','interface A{readonly m(x:number):number}const obj={readonly m(x:number){return x;}};function f(readonly x:number){return x;}function g(public x:number){return x;}'),
 ('C2-thenable-argument','const value={then(resolve:(value:number)=>void){resolve(1);}};function take(x:number){}take(value);'),
 ('C3-thenable-arrow-return','const value={then(resolve:(value:number)=>void){resolve(1);}};const f:()=>number=()=>value;'),
 ('C3-recursive-thenable','interface T{then(resolve:(value:T)=>void):void}declare let value:T;function take(x:number){}take(value);'),
 ('C6-source-builtins','interface Object{}interface Function{}function f(x:number){return x;}const out=f(1);'),
 ('C6-unicode-signatures','function é(𝒙:{名:string}):string{return 𝒙.名;}const y=é({名:"🌟"});const bad=é({名:1});'),
]
# Compact operand families cover every admitted operator, with independently
# authoritative original diagnostics rather than hand-authored result types.
for family,operators,valid,invalid in [
 ('arithmetic',['+','-','*','**','/','%'],('a','b'),('s','obj')),
 ('bitwise',['&','|','^','<<','>>','>>>'],('a','b'),('s','obj')),
 ('relational',['<','<=','>','>='],('a','b'),('s','obj')),
 ('equality',['==','===','!=','!=='],('a','b'),('s','obj')),
 ('logical',['&&','||','??'],('maybe','b'),('s','obj')),
]:
 declarations='declare let a:number;declare let b:number;declare let s:string;declare let obj:{x:number};declare let maybe:number|undefined;'
 expressions=''.join('const valid'+str(i)+'='+valid[0]+op+valid[1]+';const invalid'+str(i)+'='+invalid[0]+op+invalid[1]+';' for i,op in enumerate(operators))
 MATRIX.append(('C5-operator-family-'+family,declarations+expressions))
MATRIX.extend([
 ('C5-unary-operand-families','declare let n:number;declare let s:string;declare let obj:{x:number};const p=+n;const m=-n;const b=~n;const l=!n;const t=typeof n;const v=void n;const sp=+s;const sm=-s;const sb=~s;const op=+obj;const om=-obj;const ob=~obj;const ol=!obj;const ot=typeof obj;const ov=void obj;'),
 ('C3-named-function-expression-iife','const recurse=function named(x:number):number{if(x>0)return named(x-1);return x;};const a=recurse(2);const b=(function named(x:number){return x+1;})(1);const c=((x:number)=>x+1)(2);'),
 ('C2-optional-callable-member','interface Box{f?:(x:number)=>number}declare let box:Box;const bad=box.f(1);function use(value:Box){if(value.f)return value.f(1);return 0;}'),
 ('C5-define-property-value','function f(){}Object.defineProperty(f,"x",{value:1});const y=f.x;'),
 ('C5-define-property-callback','function f(){}Object.defineProperty(f,"callback",{value:(x:number)=>x+1});const y=f.callback(1);'),
 ('C5-arguments-arrow-capture','function outer(){const inner=()=>arguments;return inner();}const result=outer();'),
 ('C2-thenable-missing-property','declare const p:{then(resolve:(value:{x:number})=>void):void};const x=p.x;const missing=p.absent;'),
 ('C5-bigint-union-truthiness','function f(x:bigint){const z=x&&"";if(!z)return typeof z;return z;}'),
 ('C4-hidden-this-nested-method','type A={};interface B extends A{member:{m(x:number):number}}interface C extends B{}declare let c:C;const y=c.member.m(1);'),
 ('C5-bigint-zero-narrowing','function zero(value:bigint){const out=value&&"yes";const mixed=value+1;const positive=+value;const negative=-value;if(!value)return out;return value;}'),
 ('C5-function-truthiness','function f(){return 1;}if(f){const n=1;}function use(callback:()=>number){if(callback)return callback();return 0;}'),
 ('C3-return-shape-fallthrough','function none(){}function explicitVoid():void{return;}function fallthrough(x:boolean){if(x)return 1;}function multiple(x:boolean){if(x)return 1;return "no";}const a=none();const b=explicitVoid();const c=fallthrough(true);const d=multiple(true);'),
])
MATRIX.extend([
 ('C3-local-interface-function','function f(){interface Local{x:number} const x:Local={x:1};return x.x;}const value=f();'),
 ('C3-local-interface-arrow','const f=()=>{interface Local{x:number} const x:Local={x:1};return x.x;};const value=f();'),
 ('C3-local-interface-method','const obj={f(){interface Local{x:number} const x:Local={x:1};return x.x;}};const value=obj.f();'),
 ('C4-merged-hidden-this-cache','type A={};interface B extends A{member:{m(x:number):number}}interface B{other:number}interface C extends B{}declare let c:C;const y=c.member.m(1);const z=c.member.m(2);'),
 ('C4-merged-hidden-this-cache-reversed','type A={};interface B{other:number}interface B extends A{member:{m(x:number):number}}interface C extends B{}declare let c:C;const y=c.member.m(1);const z=c.member.m(2);'),
 ('C5-symbol-constructor-ordinary','interface SymbolConstructor{():symbol;}declare const SymbolConstructor:SymbolConstructor;const value=SymbolConstructor();'),
 ('C5-global-symbol-identity-widening','declare function Symbol():symbol;const a=Symbol();const b=Symbol();let widened=Symbol();var broad=Symbol();declare let s:symbol;const same=a;'),
 ('C5-global-symbol-for-identity','declare const Symbol:{():symbol;for(key:string):symbol};const a=Symbol.for("x");const b=Symbol.for("x");let widened=Symbol.for("x");declare let s:symbol;'),
 ('C5-if-let-grammar','function f(flag:boolean){if(flag)let value=1;}'),
])
RELATION_NAMES={
 'C5-global-symbol-identity-widening': [('a','b'),('a','widened'),('a','broad'),('a','s')],
 'C5-global-symbol-for-identity': [('a','b'),('a','widened'),('a','s')],
 'function-relation': [('f','g')],
 'C4-method-vs-function-variance': [('wide','narrow'),('mw','mn')],
 'C4-return-void-special': [('f','v')],
 'C4-recursive-callable-property': [('a','b')],
 'C4-recursive-callable-mismatch': [('a','b')],
}
def relations(request,value):
 pairs=[]
 for left,right in RELATION_NAMES.get(request['name'],[]):
  found={}
  for node in value['parser']['nodes']:
   if node['dataType']!='VariableDeclaration':continue
   field=next(f for f in node['fields'] if f['name']=='Name')
   if field['isNull']:continue
   nameNode=value['parser']['nodes'][field['nodeId']]
   if nameNode['dataType']!='Identifier':continue
   text=base64.b64decode(nameNode['fields'][0]['bytesBase64']).decode()
   if text in (left,right):found[text]=nameNode['id']
  if set(found)!={left,right}:raise RuntimeError('Independent original syntax relation endpoints missing')
  pairs.extend([[found[left],found[right]],[found[right],found[left]],[found[left],found[left]]])
 return pairs

def generate(name,cases):
 requests=[dict(name=case,text=text,scriptKind=3,fileName='/main.ts',profile='Callable',callableObservations=True,dataObservations=True) for case,text in cases]
 raw=''.join(json.dumps(r,ensure_ascii=True)+'\n' for r in requests).encode()
 started=time.monotonic();run=subprocess.run([str(ROOT/'tools/.cache/checker-oracle')],input=raw,stdout=subprocess.PIPE,stderr=subprocess.PIPE)
 if run.returncode:raise RuntimeError(run.stderr.decode(errors='replace'))
 values=[json.loads(x) for x in run.stdout.splitlines()]
 if len(values)!=len(requests):raise RuntimeError('Oracle incomplete')
 for request,value in zip(requests,values):
  if 'callableGraph' not in value:raise RuntimeError('Missing callable observations')
  if pairs:=relations(request,value):request['relationPairs']=pairs
 # Request endpoints are discovered from independent original syntax, then the
 # authoritative checker is invoked again with those directed public API queries.
 if any('relationPairs' in r for r in requests):
  raw=''.join(json.dumps(r,ensure_ascii=True)+'\n' for r in requests).encode()
  run=subprocess.run([str(ROOT/'tools/.cache/checker-oracle')],input=raw,stdout=subprocess.PIPE,stderr=subprocess.PIPE)
  if run.returncode:raise RuntimeError(run.stderr.decode(errors='replace'))
  values=[json.loads(x) for x in run.stdout.splitlines()]
  if len(values)!=len(requests):raise RuntimeError('Relation oracle incomplete')
 OUT.mkdir(exist_ok=True);(OUT/(name+'-requests.jsonl')).write_bytes(raw);(OUT/(name+'-expected.jsonl')).write_bytes(run.stdout)
 return dict(group=name,cases=len(cases),elapsedSeconds=round(time.monotonic()-started,3),requestsSha256=hashlib.sha256(raw).hexdigest(),expectedSha256=hashlib.sha256(run.stdout).hexdigest())
if __name__=='__main__':
 summaries=[generate('tiny',TINY),generate('matrix',MATRIX)]
 (OUT/'manifest.json').write_text(json.dumps(dict(upstreamRevision=json.loads((ROOT/'upstream.lock.json').read_text())['revision'],profile='Callable',disposition='authored-independent-Go-differential',groups=summaries,cases=sum(x['cases'] for x in summaries)),indent=2)+'\n');print(json.dumps(summaries))
