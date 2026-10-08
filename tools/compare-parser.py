#!/usr/bin/env python3
"""Stream canonical parser comparisons and report first differing paths."""
import argparse,hashlib,json,pathlib,subprocess,sys,time,tempfile
root=pathlib.Path(__file__).resolve().parent.parent
parser=argparse.ArgumentParser();parser.add_argument('--executable',required=True);parser.add_argument('--corpus',default=str(root/'validation/parser-corpus'));parser.add_argument('--report');parser.add_argument('--group',help='Compare one named fixture group during staged integration')
args=parser.parse_args();started=time.monotonic();failures=[];cases=nodes=diagnostics=0;groups=[]
def difference(want,got,path='$'):
 if type(want)!=type(got):return f'{path}: expected type {type(want).__name__}, actual {type(got).__name__}'
 if isinstance(want,dict):
  for key in want:
   if key not in got:return f'{path}.{key}: missing'
   if d:=difference(want[key],got[key],f'{path}.{key}'):return d
  for key in got:
   if key not in want:return f'{path}.{key}: unexpected'
 elif isinstance(want,list):
  if len(want)!=len(got):return f'{path}: length {len(want)} expected / {len(got)} actual'
  for i,(a,b) in enumerate(zip(want,got)):
   if d:=difference(a,b,f'{path}[{i}]'):return d
 elif want!=got:return f'{path}: expected {str(want)[:200]!r}, actual {str(got)[:200]!r}'
 return None
stderr_file=tempfile.TemporaryFile(mode='w+')
process=subprocess.Popen([args.executable],stdin=subprocess.PIPE,stdout=subprocess.PIPE,stderr=stderr_file,text=True,bufsize=1)
for requests_path in sorted(pathlib.Path(args.corpus).glob((args.group+'-requests.jsonl') if args.group else '*-requests.jsonl')):
 expected_path=requests_path.with_name(requests_path.name.replace('-requests.jsonl','-expected.jsonl'))
 # Feed one request and consume one response at a time; the persistent process
 # also checks request isolation while keeping memory bounded.
 count=0
 with requests_path.open() as requests,expected_path.open() as expected:
  for request in requests:
   line=expected.readline()
   if not line:raise RuntimeError('Incomplete expected rows')
   data=json.loads(request);want=json.loads(line)
   process.stdin.write(request);process.stdin.flush();response=process.stdout.readline()
   if not response:
    stderr_file.seek(0);raise RuntimeError(f'{data["name"]}: parser process failed: {stderr_file.read(4000)}')
   got=json.loads(response);d=difference(want,got)
   if d:
    failures.append(dict(name=data['name'],difference=d))
    if len(failures)<=20:print(data['name']+': '+d,file=sys.stderr)
   cases+=1;count+=1;nodes+=len(want['nodes']);diagnostics+=sum(len(want[name]) for name in ['parseDiagnostics','jsDiagnostics','jsDocDiagnostics'])
  if expected.readline():raise RuntimeError('Extra expected rows')
 groups.append(dict(name=requests_path.stem,cases=count,requestsSha256=hashlib.sha256(requests_path.read_bytes()).hexdigest(),expectedSha256=hashlib.sha256(expected_path.read_bytes()).hexdigest()))
if cases==0:raise RuntimeError('No parser fixtures selected')
process.stdin.close()
if process.stdout.read():raise RuntimeError('Extra parser output rows')
if process.wait(timeout=30):stderr_file.seek(0);raise RuntimeError(stderr_file.read(4000))
report=dict(executable=args.executable,cases=cases,nodes=nodes,diagnostics=diagnostics,failures=failures,elapsedSeconds=round(time.monotonic()-started,3),groups=groups)
if args.report:pathlib.Path(args.report).write_text(json.dumps(report,indent=2)+'\n')
print(json.dumps(report,separators=(',',':')));sys.exit(bool(failures))
