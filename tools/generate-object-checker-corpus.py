#!/usr/bin/env python3
"""Actual Go object graph fixtures; no admission whitelist is applied to Go."""
import argparse,hashlib,json,pathlib,subprocess
root=pathlib.Path(__file__).resolve().parent.parent
parser=argparse.ArgumentParser();parser.add_argument('--group',choices=['tiny','matrix'],default='tiny');args=parser.parse_args()
rows=[]
def add(name,text): rows.append(dict(name=name,text=text,objectObservations=True))
for name,text in {
 'basic':'const source={a:1};let target:{a:number}=source;const value=source.a;',
 'fresh-excess':'let target:{a:number}={a:1,b:2};',
 'indirect-width':'const source={a:1,b:2};let target:{a:number}=source;',
 'missing-required':'let target:{a:number}={};',
 'nested-chain':'const source={nested:{a:"s"}};let target:{nested:{a:number}}=source;',
 'optional-read':'let source:{a?:number}={};const value=source.a;',
 'shorthand':'const value=1;const source={value};const result=source.value;',
 'empty-tables':'const source={};let target:{}=source;',
}.items():add(name,text)
if args.group=='matrix':
 rows=[]
 for name,text in {
  'insertion-order':'const a={x:1,y:"s"};const b={y:"s",x:1};let t:{x:number,y:string}=a;let u:{y:string,x:number}=b;',
  'required-to-optional':'let source:{a:number}={a:1};let target:{a?:number}=source;',
  'optional-to-required':'let source:{a?:number}={};let target:{a:number}=source;',
  'weak-no-common':'const source={b:1};let target:{a?:number}=source;',
  'weak-with-common':'const source={a:1,b:2};let target:{a?:number}=source;',
  'weak-empty':'let target:{a?:number}={};',
  'readonly-to-mutable':'let source:{readonly a:number}={a:1};let target:{a:number}=source;',
  'mutable-to-readonly':'let source:{a:number}={a:1};let target:{readonly a:number}=source;',
  'nested-optional-read':'let source:{nested?:{a:number}}={};const value=source.nested.a;',
  'missing-property':'const source={a:1};const value=source.b;',
  'typo-property':'const source={count:1};const value=source.coun;',
  'nested-typo':'const source={nested:{count:1}};const value=source.nested.coun;',
  'fresh-nested-excess':'let target:{nested:{a:number}}={nested:{a:1,b:2}};',
  'indirect-missing-multiple':'const source={x:1};let target:{a:number,b:string}=source;',
  'indirect-nested-width':'const source={nested:{a:1,b:2}};let target:{nested:{a:number}}=source;',
  'contextual-literal':'let target:{a:1,b:"yes",c:true}={a:1,b:"yes",c:true};',
  'contextual-literal-mismatch':'let target:{a:1,b:"yes",c:true}={a:2,b:"no",c:false};',
  'contextual-shorthand':'const value=1;let target:{value:1}={value};',
  'shorthand-missing':'const target={missing};',
  'self-reference':'const source={a:source};const value=source.a;',
  'mutual-reference':'const a={b};const b={a};',
  'shadowed-shorthand':'const value=1;{const value="s";const source={value};const result=source.value;}',
  'duplicate-object-key':'const source={a:1,a:"s"};const value=source.a;',
  'duplicate-readonly':'let source:{readonly readonly a:number}={a:1};',
  'duplicate-type-property':'let source:{a:number,a:string}={a:1};',
  'static-names':r'const source={"é":1,"a-b":"s",1:true,"01":null,"__proto__":2,"constructor":3};const value=source.é;',
  'escaped-names':r'const source={"\u0061":1,"\uD800":"\uDC00","🦀":true};const value=source.a;',
  'numeric-property-rounding':'const source={9007199254740993:1,1e3:2,0x10:3};',
  'optional-read-assign-failure':'let source:{a?:number}={};let value:number=source.a;',
  'primitive-empty-target':'let numberTarget:{}=1;let stringTarget:{}="s";let boolTarget:{}=true;',
  'null-object-failure':'let target:{a:number}=null;',
  'scalar-to-object-failure':'let target:{a:number}=1;',
  'object-to-scalar-failure':'let target:number={a:1};',
  'reserved-source-name':'const source={"__call":1,"__index":2,"__new":3,"__x":4};',
  'uninitialized-with-object-sibling':'let x:number;let obj={a:1};let y=x;',
 }.items():add(name,text)
# Independent syntax discovery selects expression/type roots and declaration names
# alongside the old default scalar query IDs. Preserve deterministic AST DFS order.
inputs=''.join(json.dumps(row)+'\n' for row in rows)
probes=subprocess.run([str(root/'tools/.cache/checker-oracle')],input=inputs,capture_output=True,text=True,check=True)
for row,line in zip(rows,probes.stdout.splitlines(),strict=True):
 value=json.loads(line);ids={query['nodeId'] for query in value['queries']};wide=[];relation_ids=set()
 for node in value['parser']['nodes']:
  if node['dataType'] in ('ObjectLiteralExpression','TypeLiteralNode','PropertyAccessExpression'):
   ids.add(node['id'])
   if node['dataType'] in ('ObjectLiteralExpression','TypeLiteralNode'):relation_ids.add(node['id'])
  if node['dataType']=='ObjectLiteralExpression':wide.append(node['id'])
  if node['dataType']=='VariableDeclaration':
   name=next(field for field in node['fields'] if field['name']=='Name')['nodeId'];relation_ids.add(name)
 row['queryNodeIds']=sorted(ids);row['widenNodeIds']=wide
 row['relationPairs']=[[a,b] for a in sorted(relation_ids) for b in sorted(relation_ids)]
path=root/('validation/object-checker-corpus/'+args.group+'-requests.jsonl');path.write_text(''.join(json.dumps(row,separators=(',',':'))+'\n' for row in rows))
expected=subprocess.run([str(root/'tools/.cache/checker-oracle')],input=path.read_text(),capture_output=True,text=True,check=True)
(root/('validation/object-checker-corpus/'+args.group+'-expected.jsonl')).write_text(expected.stdout)
manifest=dict(upstreamRevision='fed0bf24149fb1ed36039212648bafdafc1ea10e',cases=len(rows),requestsSha256=hashlib.sha256(path.read_bytes()).hexdigest(),expectedSha256=hashlib.sha256(expected.stdout.encode()).hexdigest(),selection='Object '+args.group+': fresh/indirect/weak/missing/nested chains/optional/readonly/shorthand/static names/empty tables',historicalScalarEvidence='validation/results/checker-verified.json',retiredHistoricalAdmissionControls=['object','object-type'],instrumentation='tools/checker-oracle/object_access.go copied as untracked original-module read-only private-store observer; tracked upstream and pin unchanged',observationOrder='GetDiagnostics; query types/symbols and scalar graph formatting; assignability pairs; globals/diagnostics; relation-key equality; repeated widening; ascending canonical-type public property discovery and raw members/signature/index storage; property link work queue; selected existing cache reads; final flags. Queues expand only by index, no live collection foreach mutation. Regular cache is never forced.')
(root/('validation/object-checker-corpus/'+args.group+'-manifest.json')).write_text(json.dumps(manifest,indent=2)+'\n');print(json.dumps(manifest))
