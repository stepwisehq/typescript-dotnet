#!/usr/bin/env python3
"""Strict per-request collection comparison; preserve every failure and graph.
No mismatch normalization, assertion filtering, or expected-output rewriting.
"""
import argparse,gzip,hashlib,json,pathlib,subprocess,tempfile,time
p=argparse.ArgumentParser();p.add_argument('--requests',required=True);p.add_argument('--expected',required=True);p.add_argument('--executable',required=True);p.add_argument('--managed',action='store_true');p.add_argument('--output',required=True);args=p.parse_args()
out=pathlib.Path(args.output);out.mkdir(parents=True,exist_ok=True)
requests=pathlib.Path(args.requests);expected=pathlib.Path(args.expected)
def difference(a,b,path='$',field=None):
 if type(a)!=type(b):return dict(path=path,field=field,expectedType=type(a).__name__,actualType=type(b).__name__)
 if isinstance(a,dict):
  field=a.get('name',field)
  for key,value in a.items():
   if key not in b:return dict(path=path+'.'+key,field=field,expected='present',actual='missing')
   if d:=difference(value,b[key],path+'.'+key,field):return d
  for key in b:
   if key not in a:return dict(path=path+'.'+key,field=field,expected='absent',actual='present')
 elif isinstance(a,list):
  if len(a)!=len(b):return dict(path=path,field=field,expectedLength=len(a),actualLength=len(b))
  for i,(x,y) in enumerate(zip(a,b)):
   if d:=difference(x,y,f'{path}[{i}]',field):return d
 elif a!=b:return dict(path=path,field=field,expected=a,actual=b)
 return None
cmd=(['dotnet'] if args.managed else [])+[args.executable]
start=time.monotonic();failures=[];total=exact=0
with tempfile.TemporaryFile(mode='w+') as err:
 def spawn():return subprocess.Popen(cmd,stdin=subprocess.PIPE,stdout=subprocess.PIPE,stderr=err,text=True,bufsize=1)
 process=spawn()
 opener=gzip.open if expected.suffix=='.gz' else open
 with requests.open() as rows,opener(expected,'rt') as goldens:
  for request in rows:
   total+=1;want_line=goldens.readline()
   if not want_line:raise RuntimeError('Incomplete expected file')
   row=json.loads(request);want=json.loads(want_line);got=None
   try:
    process.stdin.write(request);process.stdin.flush();reply=process.stdout.readline()
    if not reply:raise RuntimeError('Process ended without response')
    got=json.loads(reply);d=difference(want,got)
   except (BrokenPipeError,RuntimeError,json.JSONDecodeError) as e:
    if process.poll() is None:process.kill()
    status=process.wait(timeout=15);err.flush();err.seek(0);message=err.read();err.seek(0);err.truncate()
    d=dict(path='$',exception=str(e),exitCode=status,stderr=message);process=spawn()
   if d:
    failure=dict(name=row['name'],difference=d);failures.append(failure)
    stem=str(total).zfill(3)
    (out/(stem+'-request.json')).write_text(request)
    (out/(stem+'-expected.json.gz')).write_bytes(gzip.compress(want_line.encode(),mtime=0))
    if got is not None:(out/(stem+'-actual.json.gz')).write_bytes(gzip.compress(json.dumps(got,separators=(',',':')).encode(),mtime=0))
    print(json.dumps(failure),flush=True)
   else:exact+=1
  if goldens.readline():raise RuntimeError('Extra expected rows')
 process.stdin.close();extra=process.stdout.read();status=process.wait(timeout=30)
 if status or extra:raise RuntimeError('Unexpected final status/output')
report=dict(command=cmd,requestsSha256=hashlib.sha256(requests.read_bytes()).hexdigest(),expectedSha256=hashlib.sha256(expected.read_bytes()).hexdigest(),total=total,exact=exact,failures=failures,elapsedSeconds=round(time.monotonic()-start,3))
(out/'report.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps({k:v for k,v in report.items() if k!='failures'}))
raise SystemExit(bool(failures))
