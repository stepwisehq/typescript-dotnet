#!/usr/bin/env python3
"""Integration triage only: isolate failures, never an acceptance gate."""
import argparse,hashlib,json,pathlib,subprocess,time

def difference(want,got,path='$',context=''):
    if isinstance(want,dict):
        if 'ownerKind' in want and 'name' in want:
            context=f"store {want['name']} {want['ownerKind']} {want['owner']}"
        elif 'name' in want:context=f"{context} field {want['name']}".strip()
        elif 'dataType' in want:context=f"payload {want['dataType']}"
    suffix=f' ({context})' if context else ''
    if type(want)!=type(got):return f'{path}{suffix}: type {type(want).__name__}/{type(got).__name__}'
    if isinstance(want,dict):
        if set(want)!=set(got):return f'{path}{suffix}: keys {sorted(set(want)^set(got))}'
        for key in want:
            if d:=difference(want[key],got[key],path+'.'+key,context):return d
    elif isinstance(want,list):
        if len(want)!=len(got):return f'{path}{suffix}: length {len(want)}/{len(got)}'
        for i,(a,b) in enumerate(zip(want,got)):
            if d:=difference(a,b,f'{path}[{i}]',context):return d
    elif want!=got:return f'{path}{suffix}: expected {str(want)[:120]!r}, actual {str(got)[:120]!r}'

parser=argparse.ArgumentParser();parser.add_argument('--group',default='tiny');parser.add_argument('--executable',default='validation/Checker.Validation/bin/Debug/net10.0/Checker.Validation');args=parser.parse_args()
root=pathlib.Path(__file__).resolve().parent.parent
out=root/'artifacts/data-types-validation'/('staged-'+args.group);out.mkdir(parents=True,exist_ok=True)
adapter=pathlib.Path(args.executable).resolve().parent
snapshot={p.name:hashlib.sha256(p.read_bytes()).hexdigest() for p in [adapter/'TypeScript.Scanner.dll',adapter/'Checker.Validation.dll']}
fixture_paths=[root/'validation/data-types-corpus'/(args.group+suffix) for suffix in ['-requests.jsonl','-expected.jsonl']]
fixture_hashes={p.name:hashlib.sha256(p.read_bytes()).hexdigest() for p in fixture_paths}
requests=fixture_paths[0].read_text().splitlines()
expected=fixture_paths[1].read_text().splitlines()
results=[];started=time.monotonic()
for request,want in zip(requests,expected,strict=True):
    name=json.loads(request)['name'];process=subprocess.run([args.executable],cwd=root,input=request+'\n',text=True,capture_output=True)
    (out/(name+'.stdout.jsonl')).write_text(process.stdout)
    (out/(name+'.stderr.txt')).write_text(process.stderr)
    error=('exception: '+process.stderr.splitlines()[0]) if process.returncode else difference(json.loads(want),json.loads(process.stdout))
    results.append(dict(name=name,exitCode=process.returncode,difference=error))
    if error:print(name+': '+error)
if snapshot!={name:hashlib.sha256((adapter/name).read_bytes()).hexdigest() for name in snapshot}:raise RuntimeError('Adapter snapshot changed during comparison')
if fixture_hashes!={p.name:hashlib.sha256(p.read_bytes()).hexdigest() for p in fixture_paths}:raise RuntimeError('Oracle fixtures changed during comparison')
report=dict(integrationOnly=True,group=args.group,adapterSha256=snapshot,fixtureSha256=fixture_hashes,cases=len(results),exact=sum(x['difference'] is None for x in results),elapsedSeconds=round(time.monotonic()-started,3),results=results)
(out/'report.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps({k:v for k,v in report.items() if k!='results'}))
