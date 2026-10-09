#!/usr/bin/env python3
"""Authored binding interactions, retaining historical fixtures byte-for-byte."""
import gzip,hashlib,json,pathlib,subprocess
ROOT=pathlib.Path(__file__).resolve().parent.parent
OUT=ROOT/'validation/binding-completion-corpus'
rows=[]
def add(name,text,kind=3,filename=None,jsx=False,force=False,warm=False,resolve=False):
    rows.append(dict(parse=dict(name=name,text=text,scriptKind=kind,fileName=filename or '/'+name+{1:'.js',2:'.jsx',3:'.ts',4:'.tsx',6:'.json'}[kind],externalModuleJSX=jsx,externalModuleForce=force,resolveJSDoc=warm,foundationObservations=warm),warmBeforeBind=warm,resolveJSDocAfterBind=resolve))
add('cold-empty-table-missing-class','class {}')
add('cold-nil-empty-type','type T = {};')
add('cold-shared-finally-return-tail','(function(){try{if(a)return 1;x=2;}finally{x=3;}return 4;})();z;')
add('cold-unicode-module-name','export const é=1;',filename='/d/é.ts')
for name,text,kind in [('ts-module','export const x=1;',3),('js-commonjs','exports.x=1; module.exports.y=2;',1),('js-jsdoc','/** @typedef {{x:number}} T */\n/** @type {T} */ let value; value.x;',1),('ts-ambient','declare module "pattern*" { export const value:number; }',3),('jsx','const x=<div/>;',2),('tsx','const x=<div/>;',4),('json','{"x":[1,true,null]}',6)]:
    for warm in (False,True):add(name+('-prewarmed' if warm else '-cold'),text,kind,warm=warm,resolve=True)
for jsx in (False,True):
    for force in (False,True):add('module-options-'+str(int(jsx))+str(int(force)),'const x=<div/>;',4,jsx=jsx,force=force)
add('declaration-file-ambient','declare const x:number;',filename='/d/index.d.ts')
add('missing-binder-name','class {}; const = 1;')
add('metadata-import-augmentation-duplicate','import {x} from \"m\"; declare module \"m\" { export const y:number; } export let z=1; export let z=2;')
# Exact failed input retained as an observer regression; no source edits or relaxed names.
with gzip.open(ROOT/'validation/foundation-corpus/source-requests.jsonl.gz','rt') as source:
    for index,line in enumerate(source):
        if index==10003:
            original=json.loads(line)
            if 'privateNamesNotAllowedAsDestructuringPatterns.ts' not in original['name']:raise RuntimeError('Pinned source index changed')
            rows.append(dict(parse=original,warmBeforeBind=False,resolveJSDocAfterBind=True));break
requests=OUT/'targeted-requests.jsonl';requests.write_text(''.join(json.dumps(r,ensure_ascii=True,separators=(',',':'))+'\n' for r in rows))
expected=subprocess.check_output([str(ROOT/'tools/.cache/binding-completion-oracle')],input=requests.read_bytes(),cwd=ROOT)
if len(expected.splitlines())!=len(rows):raise RuntimeError('Incomplete independent Go outputs')
(OUT/'targeted-expected.jsonl').write_bytes(expected)
manifest=dict(upstreamPin=json.loads((ROOT/'upstream.lock.json').read_text()),targetedCases=len(rows),groups=['allocated-empty/nil table','naturally shared FlowList tail','Unicode filename/raw symbol names','cold/prewarmed TS modules, JS CommonJS/JSDoc, ambient, JSX/TSX/JSON','module options cross-product','declaration filename'],broadSource='validation/foundation-corpus/source-requests.jsonl.gz',broadSourceSha256=hashlib.sha256((ROOT/'validation/foundation-corpus/source-requests.jsonl.gz').read_bytes()).hexdigest(),broadCases=17723,legacyCorpusUnchanged='validation/binder-corpus',provenance='Additional original-Go differential observations, not upstream direct test ports.')
(OUT/'manifest.json').write_text(json.dumps(manifest,indent=2)+'\n')
print(json.dumps(dict(targetedCases=len(rows))))
