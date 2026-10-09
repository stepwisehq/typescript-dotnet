#!/usr/bin/env python3
"""Strict callable differential comparison; failed requests remain retained failures."""
import argparse,hashlib,json,pathlib,subprocess,sys,tempfile,time
ROOT=pathlib.Path(__file__).resolve().parents[1]
p=argparse.ArgumentParser();p.add_argument('--executable',required=True);p.add_argument('--managed',action='store_true');p.add_argument('--group',choices=['tiny','matrix']);p.add_argument('--output',required=True);args=p.parse_args()
OUT=pathlib.Path(args.output);OUT.mkdir(parents=True,exist_ok=True)
CORPUS=ROOT/'validation/callables-corpus';manifest=json.loads((CORPUS/'manifest.json').read_text());groups=[g for g in manifest['groups'] if args.group is None or g['group']==args.group]
command=(['dotnet'] if args.managed else [])+[args.executable];start=time.monotonic();failures=[];counts=dict(requests=0,exact=0,signatures=0,calls=0,queries=0,composites=0)

def difference(want,got,path='$',field=None):
 if type(want)!=type(got):return dict(path=path,field=field,expectedType=type(want).__name__,actualType=type(got).__name__)
 if isinstance(want,dict):
  nearest=want.get('name',field)
  for key in want:
   if key not in got:return dict(path=path+'.'+key,field=nearest,expected='present',actual='missing')
   if d:=difference(want[key],got[key],path+'.'+key,nearest):return d
  for key in got:
   if key not in want:return dict(path=path+'.'+key,field=nearest,expected='absent',actual='present')
 elif isinstance(want,list):
  if len(want)!=len(got):return dict(path=path,field=field,expectedLength=len(want),actualLength=len(got))
  for i,(a,b) in enumerate(zip(want,got)):
   if d:=difference(a,b,f'{path}[{i}]',field):return d
 elif want!=got:return dict(path=path,field=field,expected=want,actual=got)
 return None

stderr=tempfile.TemporaryFile(mode='w+')
def spawn():return subprocess.Popen(command,stdin=subprocess.PIPE,stdout=subprocess.PIPE,stderr=stderr,text=True,bufsize=1)
process=spawn()
for group in groups:
 requests=CORPUS/(group['group']+'-requests.jsonl');expected=CORPUS/(group['group']+'-expected.jsonl')
 for path,key in [(requests,'requestsSha256'),(expected,'expectedSha256')]:
  if hashlib.sha256(path.read_bytes()).hexdigest()!=group[key]:raise RuntimeError('Manifest digest mismatch: '+str(path))
 count=0
 with requests.open() as r,expected.open() as e:
  for request in r:
   line=e.readline()
   if not line:raise RuntimeError('Incomplete expected requests')
   data=json.loads(request);want=json.loads(line);got=None;d=None
   try:
    process.stdin.write(request);process.stdin.flush();response=process.stdout.readline()
    if not response:raise RuntimeError('Checker process ended without response')
    got=json.loads(response);d=difference(want,got)
   except (BrokenPipeError,RuntimeError,json.JSONDecodeError) as error:
    if process.poll() is None:process.kill()
    process.wait(timeout=10);stderr.flush();stderr.seek(0);message=stderr.read();stderr.seek(0);stderr.truncate()
    d=dict(path='$',exception=str(error),stderr=message[-10000:]);process=spawn()
   counts['requests']+=1;count+=1
   graph=want['callableGraph']
   for key in ['signatures','calls','queries','composites']:counts[key]+=len(graph[key])
   if d:
    failure=dict(name=data['name'],difference=d);failures.append(failure)
    name=str(counts['requests']).zfill(3)
    (OUT/(name+'-request.json')).write_text(request)
    (OUT/(name+'-expected.json')).write_text(json.dumps(want,separators=(',',':'))+'\n')
    if got is not None:(OUT/(name+'-actual.json')).write_text(json.dumps(got,separators=(',',':'))+'\n')
    print(json.dumps(failure,separators=(',',':')),file=sys.stderr,flush=True)
   else:counts['exact']+=1
  if e.readline():raise RuntimeError('Extra expected row')
 if count!=group['cases']:raise RuntimeError('Manifest request count mismatch')
process.stdin.close()
extra=process.stdout.read();status=process.wait(timeout=30)
if extra or status:raise RuntimeError('Unexpected extra output/final checker exit')
report=dict(command=command,groups=[g['group'] for g in groups],expectedRequests=sum(g['cases'] for g in groups),counts=counts,failures=failures,elapsedSeconds=round(time.monotonic()-start,3))
(OUT/'report.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps({k:v for k,v in report.items() if k!='failures'},separators=(',',':')),flush=True)
raise SystemExit(bool(failures))
