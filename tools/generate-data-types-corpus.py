#!/usr/bin/env python3
"""Independent contract specimens, never filtered through managed admission.

Tiny base observations are integration evidence only. Rich observations are the
acceptance contract and are generated separately without replacing history.
"""
import argparse, hashlib, json, pathlib, subprocess

ROOT = pathlib.Path(__file__).resolve().parent.parent
REVISION = 'fed0bf24149fb1ed36039212648bafdafc1ea10e'

SPECIMENS = [
    ('D1', 'alias-scalar-mismatch', 'type N=number;let x:N="s";'),
    ('D1', 'alias-forward-object', 'let x:Point={x:1};type Point={x:number;y?:string};const y=x.x;'),
    ('D1', 'alias-value-confusion', 'type Point={x:number};const p=Point;'),
    ('D1', 'alias-scoped-shadow', 'type N=number;{type N=string;let x:N=1;}let y:N=1;'),
    ('D2', 'interface-basic', 'interface Point{x:number;y?:string}let x:Point={x:1};const y=x.x;'),
    ('D2', 'interface-merge-conflict', 'interface I{x:number}interface I{x:string}let x:I={x:1};'),
    ('D2', 'interface-alias-base', 'type A={a:number};interface I extends A{b:string}let x:I={a:1,b:"s"};const y=x.a;'),
    ('D3', 'recursive-property-alias', 'type N={value:number;next?:N};let n:N={value:1};const x=n.next;'),
    ('D3', 'naked-alias-cycle', 'type A=B;type B=A;let x:A=1;'),
    ('D4', 'union-common-property', 'type U={kind:"a";x:number}|{kind:"b";x:string};let u:U={kind:"a",x:1};const x=u.x;'),
    ('D5', 'intersection-conflict', 'type I={a:number}&{a:string};let x:I={a:1};'),
    ('D6', 'any-alias-heritage', 'type A=any;interface I extends A{x:number}let x:I={x:1};const y=x.x;'),
    ('D7', 'source-global-object', 'interface Object{}let x:object={a:1};'),
    ('D7', 'source-global-array-shape', 'interface Array{}let x:Array={};'),
]

MATRIX = [
    ('D1','alias-chain','type A={x:number};type B=A;type C=B;let c:C={x:1};const x=c.x;'),
    ('D1','type-value-legal-sharing','type Value={x:number};const Value={x:1};let x:Value=Value;'),
    ('D1','value-as-type','const Value=1;let x:Value=1;'),
    ('D1','unknown-type','let x:Absent=1;'),
    ('D1','alias-typo','type Coordinate={x:number};let x:Coordinat={x:1};'),
    ('D1','interface-typo','interface Coordinate{x:number}let x:Coordinat={x:1};'),
    ('D1','qualified-first-missing','let x:Absent.Member=1;'),
    ('D1','qualified-second-missing','interface Scope{x:number}let x:Scope.Member=1;'),
    ('D1','qualified-value-namespace','const Scope={Member:1};let x:Scope.Member=1;'),
    ('D1','qualified-globalThis-missing-member','type T=globalThis.Missing;let x:T=1;'),
    ('D1','duplicate-alias','type A=number;type A=string;let x:A=1;'),
    ('D1','alias-interface-collision','type A={x:number};interface A{y:string}let x:A={x:1};'),
    ('D1','nested-rendering-shadow','interface Item{x:number}{type Item={x:string};let value:Item={x:1};}let outer:Item={x:1};'),
    ('D1','escaped-unicode-alias','type \\u00e9={"🦀":number};let x:é={"🦀":"s"};'),
    ('D2','interface-forward-base','interface Derived extends Base{y:string}interface Base{x:number}let x:Derived={x:1,y:"s"};'),
    ('D2','multiple-compatible-bases','interface A{x:number}interface B{x:number;y:string}interface C extends A,B{z?:boolean}let c:C={x:1,y:"s"};'),
    ('D2','multiple-incompatible-bases','interface A{x:number}interface B{x:string}interface C extends A,B{}'),
    ('D2','own-override-base-conflict','interface A{x:number}interface B{x:string}interface C extends A,B{x:number}'),
    ('D2','own-override-compatible','interface A{x:number}interface B{x:number}interface C extends A,B{x:number;y:string}let c:C={x:1,y:"s"};'),
    ('D2','inherited-optional-required','interface A{x?:number}interface B{x:number}interface C extends A,B{}'),
    ('D2','own-optional-required','interface A{x:number}interface C extends A{x?:number}'),
    ('D2','merged-fields','interface I{x:number}interface I{y:string}let i:I={x:1,y:"s"};'),
    ('D2','merged-fields-reversed','interface I{y:string}interface I{x:number}let i:I={x:1,y:"s"};'),
    ('D2','merged-readonly-conflict','interface I{readonly x:number}interface I{x:number}'),
    ('D2','merged-optional-conflict','interface I{x?:number}interface I{x:number}'),
    ('D2','merged-heritage','interface A{x:number}interface B{y:string}interface I extends A{}interface I extends B{}let i:I={x:1,y:"s"};'),
    ('D2','alias-intersection-base','type A={a:number}&{b:string};interface I extends A{c:boolean}let i:I={a:1,b:"s",c:true};'),
    ('D2','alias-base-derived-hidden-this','type A={x:number};interface I extends A{self?:I}interface J extends I{y:string}let j:J={x:1,y:"s"};const self=j.self;'),
    ('D2','any-base-extra-property-read','type A=any;interface I extends A{x:number}let i:I={x:1,extra:"s"};const y=i.extra;'),
    ('D2','alias-union-base-invalid','type A={a:number}|{b:string};interface I extends A{x:number}'),
    ('D2','alias-primitive-base-invalid','type A=number;interface I extends A{x:number}'),
    ('D2','missing-base','interface I extends Missing{x:number}'),
    ('D3','direct-recursive-interface','interface N{value:number;next?:N}let n:N={value:1};const x=n.next;'),
    ('D3','mutual-recursive-alias','type A={b?:B;value:number};type B={a?:A};let a:A={value:1,b:{a:{value:2}}};'),
    ('D3','recursive-interface-compatible','interface A{x:number;next?:A}interface B{x:number;next?:B}let a:A={x:1};let b:B=a;'),
    ('D3','recursive-interface-mismatch','interface A{x:number;next?:A}interface B{x:string;next?:B}let a:A={x:1};let b:B=a;'),
    ('D3','recursive-alias-compatible','type A={x:number;next?:A};type B={x:number;next?:B};let a:A={x:1};let b:B=a;'),
    ('D3','recursive-nested-mismatch','type A={child?:A;payload:{x:number}};type B={child?:B;payload:{x:string}};let a:A={payload:{x:1}};let b:B=a;'),
    ('D3','self-heritage-cycle','interface A extends A{x:number}'),
    ('D3','mutual-heritage-cycle','interface A extends B{x:number}interface B extends A{y:string}'),
    ('D3','cycle-through-alias-base','type A=B;interface B extends A{x:number}'),
    ('D4','nullable-read','type U={x:number}|undefined;let u:U;const x=u.x;'),
    ('D4','union-common-read','type U={x:number;a:string}|{x:string;b:boolean};let u:U={x:1,a:"s"};const x=u.x;'),
    ('D4','union-noncommon-read','type U={a:number}|{b:string};let u:U={a:1};const x=u.b;'),
    ('D4','union-optional-common','type U={x?:number}|{x:string};let u:U={};const x=u.x;'),
    ('D4','union-discriminant-context','type A={kind:"a";value:number};type B={kind:"b";value:string};type U=A|B;let u:U={kind:"b",value:1};'),
    ('D4','named-union-best-overlap-failure','interface A{common:string;a:number}interface B{common:string;b:boolean}let v:A|B={common:1,a:"wrong"};'),
    ('D4','union-fresh-excess','type U={kind:"a";a:number}|{kind:"b";b:string};let u:U={kind:"a",a:1,b:"s"};'),
    ('D4','union-indirect-width','type U={kind:"a";a:number}|{kind:"b";b:string};const source:{kind:"a";a:number;b:string}={kind:"a",a:1,b:"s"};let u:U=source;'),
    ('D4','union-alias-cache-separation','type A={x:number}|{x:string};type B={x:number}|{x:string};let a:A={x:1};let b:B=a;'),
    ('D4','primitive-union','type U=number|string;let x:U=true;'),
    ('D5','intersection-compatible','type I={a:number}&{b:string};let i:I={a:1,b:"s"};const a=i.a;const b=i.b;'),
    ('D5','intersection-optional-required','type I={a?:number}&{a:number};let i:I={};'),
    ('D5','intersection-discriminant-never','type I={kind:"a";x:number}&{kind:"b";y:string};let i:I={kind:"a",x:1,y:"s"};'),
    ('D5','intersection-missing-member','type I={a:number}&{b:string};let i:I={a:1,b:"s"};const x=i.missing;'),
    ('D5','never-intersection-missing-member','type I={kind:"a"}&{kind:"b"};let i:I;const x=i.missing;'),
    ('D5','intersection-nested-mismatch','type I={child:{x:number}}&{child:{y:string}};let i:I={child:{x:1,y:2}};'),
    ('D5','intersection-distributed-union','type U=({kind:"a";x:number}|{kind:"b";y:string})&{common:boolean};let u:U={kind:"a",x:1,common:true};const x=u.common;'),
    ('D5','intersection-alias-cache-separation','type A={x:number}&{y:string};type B={x:number}&{y:string};let a:A={x:1,y:"s"};let b:B=a;'),
    ('D5','primitive-intersection-never','type I=number&string;let i:I=1;'),
    ('D6','named-contextual-shorthand','interface I{value:1}const value=1;let i:I={value};'),
    ('D6','named-fresh-vs-indirect','interface I{x:number}let fresh:I={x:1,y:2};const source={x:1,y:2};let indirect:I=source;'),
    ('D6','named-readonly-mutability','interface A{readonly x:number}interface B{x:number}let a:A={x:1};let b:B=a;'),
    ('D6','named-weak-target','interface I{x?:number}const source={y:1};let i:I=source;'),
    ('D6','named-object-keyword','interface I{x:number}let objectValue:object={x:1};let target:I=objectValue;'),
    ('D6','nested-optional-dot','interface I{child?:{value:number}}let i:I={};const x=i.child.value;'),
    ('D6','local-globalThis-shadow-object','{interface I{x:number}const globalThis:I={x:1};const value=globalThis.x;}'),
    ('D7','source-builtin-wrong-shape','type Array=number;let x:Array=1;'),
    ('D7','source-builtin-function-empty','interface Function{}interface CallableFunction{}interface NewableFunction{}let x:number=1;'),
    ('D4','three-arm-deferred-property','type U={value:number;a:string}|{value:string;b:boolean}|{value:boolean;c:number};let u:U={value:1,a:"s"};const value=u.value;'),
    ('D5','three-arm-intersection-property','type I={value:number|string}&{value:string|boolean}&{value:string|undefined};let i:I={value:"s"};const value=i.value;'),
    ('D4','ten-arm-discriminant-cache','type U={kind:"a";a:number}|{kind:"b";b:number}|{kind:"c";c:number}|{kind:"d";d:number}|{kind:"e";e:number}|{kind:"f";f:number}|{kind:"g";g:number}|{kind:"h";h:number}|{kind:"i";i:number}|{kind:"j";j:number};let u:U={kind:"j",j:1};const kind=u.kind;'),
    ('D4','union-valued-discriminant-related','type S={kind:"a"|"b";value:number};type T={kind:"a";value:number}|{kind:"b";value:number};let s:S={kind:"a",value:1};let t:T=s;'),
    ('D4','union-valued-discriminant-payload-mismatch','type S={kind:"a"|"b";value:number};type T={kind:"a";value:number}|{kind:"b";value:string};let s:S={kind:"a",value:1};let t:T=s;'),
    ('D4','discriminant-cartesian-covered','type S={kind:"a"|"b";phase:0|1;value:number};type T={kind:"a";phase:0;value:number}|{kind:"a";phase:1;value:number}|{kind:"b";phase:0;value:number}|{kind:"b";phase:1;value:number};let s:S={kind:"a",phase:0,value:1};let t:T=s;'),
    ('D4','discriminant-cartesian-missing-combination','type S={kind:"a"|"b";phase:0|1;value:number};type T={kind:"a";phase:0;value:number}|{kind:"a";phase:1;value:number}|{kind:"b";phase:0;value:number};let s:S={kind:"a",phase:0,value:1};let t:T=s;'),
    ('D4','scalar-union-reductions','type A=any|{x:number};type U=unknown|{x:number};type N=never|{x:number};type O=object|{x:number};type L=number|null;type D="a"|"a"|string|never;let a:A=1;let u:U=1;let n:N={x:1};let o:O={x:1};let l:L=null;let d:D="s";'),
    ('D5','scalar-intersection-reductions','type A=any&{x:number};type U=unknown&{x:number};type N=never&{x:number};type O=object&{x:number};type L=null&{x:number};type D=number&number&unknown;let a:A=1;let u:U={x:1};let n:N={x:1};let o:O={x:1};let l:L=null;let d:D=1;'),
]


def invoke(rows):
    process = subprocess.run([str(ROOT/'tools/.cache/checker-oracle')],
        input=''.join(json.dumps(row)+'\n' for row in rows), text=True,
        capture_output=True, check=True)
    values = [json.loads(line) for line in process.stdout.splitlines()]
    if len(values) != len(rows):
        raise RuntimeError('Oracle response count differs')
    return values, process.stdout


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--rich', action='store_true')
    parser.add_argument('--group', choices=['tiny','matrix'], default='tiny')
    args = parser.parse_args()
    specimens = MATRIX if args.group=='matrix' else SPECIMENS
    rows = [dict(name=name,text=text) for _,name,text in specimens]
    preliminary, _ = invoke(rows)
    for row, result in zip(rows, preliminary, strict=True):
        ids = {q['nodeId'] for q in result['queries']}
        declared = []
        relations = set()
        widening = []
        for node in result['parser']['nodes']:
            payload = node['dataType']
            if payload in ('TypeAliasDeclaration','InterfaceDeclaration','TypeReferenceNode',
                    'TypeLiteralNode','UnionTypeNode','IntersectionTypeNode','PropertyAccessExpression',
                    'ObjectLiteralExpression'):
                ids.add(node['id'])
            if payload in ('TypeAliasDeclaration','InterfaceDeclaration'):
                declared.append(node['id'])
            if payload in ('VariableDeclaration','TypeAliasDeclaration','InterfaceDeclaration'):
                name = next(f['nodeId'] for f in node['fields'] if f['name']=='Name')
                ids.add(name);relations.add(name)
            if payload == 'ObjectLiteralExpression':
                widening.append(node['id'])
        row['queryNodeIds'] = sorted(ids)
        row['relationPairs'] = [[a,b] for a in sorted(relations) for b in sorted(relations)]
        if args.rich:
            row.update(dataObservations=True,declaredNodeIds=declared,widenNodeIds=widening)
    group = args.group if args.rich else 'integration'
    folder = ROOT/'validation/data-types-corpus'
    requests = folder/(group+'-requests.jsonl')
    requests.write_text(''.join(json.dumps(row,separators=(',',':'))+'\n' for row in rows))
    results, expected = invoke(rows)
    if args.rich and any('dataGraph' not in result for result in results):
        raise RuntimeError('Rich request ignored: build current oracle before generating acceptance fixtures')
    (folder/(group+'-expected.jsonl')).write_text(expected)
    manifest = dict(upstreamRevision=REVISION,cases=len(rows),
        observations='rich data graph' if args.rich else 'public base graph: integration only, not acceptance',
        requestsSha256=hashlib.sha256(requests.read_bytes()).hexdigest(),
        expectedSha256=hashlib.sha256(expected.encode()).hexdigest(),
        groups=[dict(contract=contract,name=name,checkerCodes=[d['code'] for d in result['checkerDiagnostics']],
            binderCodes=[d['code'] for d in result['bindDiagnostics']],globalCodes=[d['code'] for d in result['globalDiagnostics']])
            for (contract,name,_),result in zip(specimens,results,strict=True)],
        globals='Exact per-case original Program globals; source builtin declarations may change them; no filtering or fixed count')
    (folder/(group+'-manifest.json')).write_text(json.dumps(manifest,indent=2)+'\n')
    print(json.dumps(dict(group=group,cases=len(rows),requestsSha256=manifest['requestsSha256'],expectedSha256=manifest['expectedSha256'])))


if __name__ == '__main__':
    main()
