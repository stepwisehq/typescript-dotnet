#!/usr/bin/env python3
"""Callsite-informed scalar relationships; never filter actual Go diagnostics."""
import hashlib,json,pathlib,subprocess
root=pathlib.Path(__file__).resolve().parent.parent
rows=[]
def add(name,text):rows.append(dict(name=name,text=text))
for name,text in {
 'empty':'',
 'multiple-declarations':'let n=1,s="a",b=true; const x=n,y=s,z=b;',
 'repeated-literal-caches':'const a=1,b=1,c="é",d="é",e=true,f=true;let n=a,s=c,t=e;',
 'literal-annotations':'let s:"a"="a";let n:1=1;let b:true=true;',
 'literal-annotation-mismatches':'let s:"a"="b";let n:1=2;let b:true=false;',
 'parenthesized-literals':'const n=(1);const s=(("a"));const b=(true);',
 'parenthesized-types':'let n:(number)=1;let s:("a")="a";',
 'unicode-names':'let α=1; {let α="é";let β=α;}let γ=α;',
 'escaped-names':r'let \u0061=1;let x=a;let y=\u0061;',
 'unicode-typo':'let café=1;let x=cafè;',
 'out-of-scope':'{let x=1;} let y=x;',
 'var-scope':'{var x=1;}let y=x;',
 'forward-var':'var y=x;var x=1;',
 'self-var':'var x=x;',
 'forward-const':'const y=x;const x=1;',
 'bare-primitives':'let n:number;let s:string;let b:boolean;let u:unknown;let a:any;let v:void;let q:never;let e:bigint;let y:symbol;',
 'any-unknown-never':'let a:any=1;let u:unknown=1;let n:number=a;let m:number=u;let q:never=1;',
 'undefined-void':'let u:undefined=undefined;let v:void=undefined;let n:number=undefined;',
 'global-this-declaration':'let globalThis=1;',
 'undefined-declaration':'let undefined=1;',
 'var-undefined-declaration':'var undefined=1;',
 'local-global-this-shadow':'{const globalThis=1;const x=globalThis;}',
 'primitive-type-as-value':'let x=number;let y=string;let z=boolean;',
 'missing-symbol':'const x=Symbol;',
 'duplicate-let':'let x=1;let x=2;',
 'var-conflicting-types':'var x:number;var x:string;',
 'uninitialized-const':'const x;',
 'plain-comments':'/** ordinary documentation */ let x=1; // simple comment\nlet y=x;',
 'escaped-literals':r'const s="a\n\t\"\\é";const t="\u0061\u{1F980}";',
 'astral-lone-surrogates':r'const s="🦀";const a="\uD800";const b="\uDC00";const c="\uD800\uDC00";',
 'numeric-boundaries':'const a=9007199254740993;const b=1e-323;const c=1e309;const d=0xabcdef123456789;',
}.items():add(name,text)
for size in [159,160,161,1000]:add('literal-format-length-'+str(size),'let s:"'+('é🦀'*(size//2)+'a'*(size%2))+'"="different";')
add('scalar-relations','let n:number=1;let s:string="a";let b:boolean=true;let a:any=1;let u:unknown=1;let q:never;let z:null=null;let e:undefined=undefined;let v:void;const one=1;const yes=true;')
# Select actual top-level declaration-name node IDs independently from Go parser output.
last=rows[-1];probe=subprocess.run([str(root/'tools/.cache/checker-oracle')],input=json.dumps(last)+'\n',capture_output=True,text=True,check=True)
value=json.loads(probe.stdout);ids=[]
for node in value['parser']['nodes']:
 if node['dataType']=='VariableDeclaration':
  field=next(field for field in node['fields'] if field['name']=='Name');ids.append(field['nodeId'])
last['relationPairs']=[[a,b] for a in ids for b in ids]
# Exercise explicit public type-node queries too, using independent Go syntax
# structure and existing default query IDs rather than C# admission predicates.
probes=subprocess.run([str(root/'tools/.cache/checker-oracle')],input=''.join(json.dumps(row)+'\n' for row in rows),capture_output=True,text=True,check=True)
for row,line in zip(rows,probes.stdout.splitlines(),strict=True):
 value=json.loads(line);query_ids={q['nodeId'] for q in value['queries']}
 for node in value['parser']['nodes']:
  if node['dataType']=='VariableDeclaration':
   annotation=next(field for field in node['fields'] if field['name']=='Type')
   if annotation.get('nodeId') is not None:query_ids.add(annotation['nodeId'])
 row['queryNodeIds']=sorted(query_ids)
path=root/'validation/checker-corpus/selected-requests.jsonl';path.write_text(''.join(json.dumps(row,separators=(',',':'))+'\n' for row in rows))
manifest=dict(upstreamRevision='fed0bf24149fb1ed36039212648bafdafc1ea10e',selectedCases=len(rows),initialTinyCases=9,selection='Primitive/name/flow/formatting/cache relationships and independent unsupported controls; no standard library; all actual environment diagnostics retained',selectedRequestsSha256=hashlib.sha256(path.read_bytes()).hexdigest())
(root/'validation/checker-corpus/manifest.json').write_text(json.dumps(manifest,indent=2)+'\n');print(json.dumps(manifest))
