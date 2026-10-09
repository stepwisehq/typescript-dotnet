#!/usr/bin/env python3
"""Independent authored admission controls, separate from actual Go agreement."""
import argparse,json,pathlib,subprocess,time
ROOT=pathlib.Path(__file__).resolve().parents[1]
p=argparse.ArgumentParser();p.add_argument('--executable',required=True);p.add_argument('--managed',action='store_true');p.add_argument('--output',required=True);a=p.parse_args();start=time.monotonic();command=(['dotnet'] if a.managed else [])+[a.executable]
controls=json.loads((ROOT/'validation/callables-corpus/admission-controls.json').read_text());rows=[]
run=subprocess.run(command,input=''.join(json.dumps({**r,'admissionOnly':True})+'\n' for r in controls),text=True,stdout=subprocess.PIPE,stderr=subprocess.PIPE)
if run.returncode:raise RuntimeError(run.stderr[-12000:])
actual=[json.loads(line) for line in run.stdout.splitlines()]
if len(actual)!=len(controls):raise RuntimeError('Incomplete authored admission responses')
for request,result in zip(controls,actual):
 feature=request['expectedFeature'];admitted=feature is None
 if result['admitted']!=admitted or result['feature']!=feature:raise RuntimeError(request['name']+': '+repr(result))
 if not admitted and not(0<=result['pos']<=result['end']<=len(request['text'].encode())):raise RuntimeError('Invalid rejection span')
 rows.append(dict(name=request['name'],admitted=admitted,feature=feature))
api=subprocess.run(command+['--callable-self-tests'],text=True,stdout=subprocess.PIPE,stderr=subprocess.PIPE)
if api.returncode:raise RuntimeError(api.stderr[-12000:])
checks=json.loads(api.stdout)
if len(checks)!=10 or any(not c['boolValue'] for c in checks):raise RuntimeError('Incomplete callable API controls')
report=dict(command=command,admissionControls=len(rows),admitted=sum(r['admitted'] for r in rows),rejected=sum(not r['admitted'] for r in rows),apiControls=len(checks),controls=rows,api=checks,elapsedSeconds=round(time.monotonic()-start,3))
pathlib.Path(a.output).write_text(json.dumps(report,indent=2)+'\n');print(json.dumps({k:v for k,v in report.items() if k not in ['controls','api']},separators=(',',':')))
