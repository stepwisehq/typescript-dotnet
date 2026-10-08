#!/usr/bin/env python3
"""Compare native/managed JSONL scanner outputs with the pinned Go oracle."""
import argparse, hashlib, json, pathlib, subprocess, sys, time
root=pathlib.Path(__file__).resolve().parent.parent
parser=argparse.ArgumentParser()
parser.add_argument('--executable', required=True)
parser.add_argument('--requests',default=str(root/'validation/corpus/requests.jsonl'))
parser.add_argument('--expected',default=str(root/'validation/corpus/expected.jsonl'))
parser.add_argument('--report')
args=parser.parse_args()
requests=pathlib.Path(args.requests).read_bytes()
expected=[json.loads(x) for x in pathlib.Path(args.expected).read_text().splitlines()]
start=time.monotonic()
process=subprocess.run([args.executable],input=requests,stdout=subprocess.PIPE,stderr=subprocess.PIPE)
if process.returncode:
 sys.stderr.write(process.stderr.decode(errors='replace')); sys.exit(process.returncode)
actual=[json.loads(x) for x in process.stdout.splitlines()]
names=[json.loads(x)['name'] for x in requests.splitlines()]
failures=[]
for i,(want,got) in enumerate(zip(expected,actual)):
 if want!=got:
  failures.append(names[i])
  if len(failures)<=10:
   print('MISMATCH:',names[i],file=sys.stderr)
   for field in want:
    if want[field]!=got.get(field):
     if isinstance(want[field],list):
      for j,(a,b) in enumerate(zip(want[field],got.get(field,[]))):
       if a!=b: print(f'  {field}[{j}] expected={a} actual={b}',file=sys.stderr); break
      if len(want[field])!=len(got.get(field,[])): print(f'  {field} lengths {len(want[field])}/{len(got.get(field,[]))}',file=sys.stderr)
     else: print(' ',field,want[field],got.get(field),file=sys.stderr)
if len(actual)!=len(expected): failures.append('row-count')
report=dict(executable=args.executable,cases=len(expected),tokens=sum(len(x['tokens']) for x in expected),diagnostics=sum(len(x['diagnostics']) for x in expected),failures=failures,elapsedSeconds=round(time.monotonic()-start,3),requestsSha256=hashlib.sha256(requests).hexdigest(),expectedSha256=hashlib.sha256(pathlib.Path(args.expected).read_bytes()).hexdigest())
if args.report: pathlib.Path(args.report).write_text(json.dumps(report,indent=2)+'\n')
print(json.dumps(report,separators=(',',':')))
sys.exit(bool(failures))
