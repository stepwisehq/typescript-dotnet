#!/usr/bin/env python3
"""Bounded original/compiler-consumer feedback; full public records, no private census."""
import argparse, base64, gzip, hashlib, json, os, pathlib, subprocess, time
ROOT=pathlib.Path(__file__).resolve().parents[1]
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def rows(p):return [json.loads(line) for line in p.read_text().splitlines() if line.strip()] if p.exists() else []
def differences(a,b,path='$'):
 if type(a)!=type(b):return [{'path':path,'original':a,'managed':b}]
 if isinstance(a,dict):
  out=[]
  for key in sorted(a.keys()|b.keys()):
   if key not in a or key not in b:out.append({'path':path+'.'+key,'missing':'original' if key not in a else 'managed'})
   elif a[key]!=b[key]:out.extend(differences(a[key],b[key],path+'.'+key))
  return out
 if isinstance(a,list):
  out=[] if len(a)==len(b) else [{'path':path,'originalCount':len(a),'managedCount':len(b)}]
  for i,(x,y) in enumerate(zip(a,b)):
   if x!=y:out.extend(differences(x,y,path+f'[{i}]'))
  return out
 return [{'path':path,'original':a,'managed':b}]
def main():
 p=argparse.ArgumentParser();p.add_argument('--requests',type=pathlib.Path,required=True);p.add_argument('--original',type=pathlib.Path,required=True);p.add_argument('--consumer-build',type=pathlib.Path,required=True);p.add_argument('--output',type=pathlib.Path,required=True);p.add_argument('--batch-size',type=int,default=32);p.add_argument('--timeout',type=int,default=120);p.add_argument('--original-reference-report',type=pathlib.Path);a=p.parse_args()
 if a.batch_size<1 or a.timeout<1:raise SystemExit('Positive batch/timeout required')
 requests=a.requests.resolve();original=a.original.resolve();build_path=a.consumer_build.resolve();out=a.output.resolve();out.mkdir(parents=True,exist_ok=True)
 build=json.loads(build_path.read_text());target=ROOT/build['target'];snapshot=ROOT/build['snapshot'];scanner=target.parent/'TypeScript.Scanner.dll'
 if sha(target)!=build['targetSha256'] or sha(scanner)!=build['scannerSha256'] or sha(snapshot)!=build['snapshotSha256']:raise SystemExit('Consumer/snapshot hash mismatch')
 before={'original':sha(original),'consumer':sha(target),'scanner':sha(scanner),'snapshot':sha(snapshot),'requests':sha(requests),'consumerBuild':sha(build_path)}
 source=rows(requests);names=[r['Name'] for r in source]
 if not source or len(names)!=len(set(names)):raise SystemExit('Empty or duplicate named requests')
 reference_rows=None;reference_binding=None
 if a.original_reference_report:
  reference_report=a.original_reference_report.resolve();reference=json.loads(reference_report.read_text());reference_file=reference_report.parent/reference['reference'];reference_requests=ROOT/reference['requests']
  if reference.get('classification')!='fresh-original-noemit-reference-production-not-target-or-emit-test-pass' or reference.get('drift') is not False or reference.get('failedRows')!=0 or reference.get('processFailures') or reference.get('originalObserved')!=reference.get('cases'):raise SystemExit('Incomplete original reference report')
  if reference['hashesBefore']!=reference['hashesAfter'] or reference['hashesAfter']['original']!=sha(original) or reference['requestsSha256']!=sha(reference_requests) or reference['referenceSha256']!=sha(reference_file):raise SystemExit('Original reference input/binary/output hash mismatch')
  for path,digest in reference['sourceFiles'].items():
   if sha(ROOT/path)!=digest:raise SystemExit('Original reference source/environment hash mismatch: '+path)
  with gzip.open(reference_file,'rt') as stream:cached=[json.loads(line) for line in stream if line.strip()]
  reference_rows={r['name']:r for r in cached};bound_requests={r['Name']:r for r in rows(reference_requests)}
  if len(reference_rows)!=len(cached) or len(cached)!=reference['cases'] or len(bound_requests)!=reference['cases']:raise SystemExit('Original reference count/identity mismatch')
  for request in source:
   record=reference_rows.get(request['Name'])
   if bound_requests.get(request['Name'])!=request or not record or record.get('status')!='observed' or record.get('sourceSha256')!=request['SourceSha256'] or record.get('configuration')!=request['Configuration']:raise SystemExit('Original reference request/observation mismatch: '+request['Name'])
  reference_binding={'report':str(reference_report),'reportSha256':sha(reference_report),'reference':str(reference_file),'referenceSha256':sha(reference_file),'requestsSha256':sha(reference_requests),'sourceFiles':reference['sourceFiles']}
  before.update(originalReferenceReport=sha(reference_report),originalReference=sha(reference_file),originalReferenceRequests=sha(reference_requests))
 observations=[];processes=[];failures=[];started=time.monotonic()
 for offset in range(0,len(source),a.batch_size):
  batch=source[offset:offset+a.batch_size];folder=out/f'batch-{offset//a.batch_size:04d}';folder.mkdir(exist_ok=True)
  input=folder/'requests.jsonl';input.write_text(''.join(json.dumps(r,separators=(',',':'))+'\n' for r in batch))
  for kind,command,cwd in [('original',[str(original),'-test.run','^TestAuthoredWholeNoEmit$','-test.v'],ROOT/'upstream/TypeScript/tsc'),('managed',['dotnet',str(target),str(ROOT),str(input),str(folder/'managed.jsonl')],ROOT)]:
   if kind=='original' and reference_rows is not None:
    output=folder/'original.jsonl';output.write_text(''.join(json.dumps(reference_rows[r['Name']],separators=(',',':'))+'\n' for r in batch))
    processes.append({'kind':'original','processExecuted':False,'classification':'reused-complete-pinned-Go-reference','referenceReportSha256':reference_binding['reportSha256'],'requestsSha256':sha(input),'outputSha256':sha(output),'observed':len(batch),'exitCode':0,'timeout':False})
    continue
   environment=os.environ.copy();environment.update(CHECKER_NOEMIT_REQUESTS=str(input),CHECKER_NOEMIT_OUTPUT=str(folder/'original.jsonl'))
   start=time.monotonic()
   with (folder/(kind+'.log')).open('wb') as log,(folder/(kind+'.stderr')).open('wb') as error:
    try:result=subprocess.run(command,cwd=cwd,env=environment,stdout=log,stderr=error,timeout=a.timeout);exit_code=result.returncode;timeout=False
    except subprocess.TimeoutExpired:exit_code=None;timeout=True
   output=folder/(kind+'.jsonl');observed=rows(output)
   record={'kind':kind,'command':command,'cwd':str(cwd),'exitCode':exit_code,'timeout':timeout,'elapsedSeconds':round(time.monotonic()-start,3),'requestsSha256':sha(input),'outputSha256':sha(output) if output.exists() else None,'stdoutSha256':sha(folder/(kind+'.log')),'stderrSha256':sha(folder/(kind+'.stderr')),'observed':len(observed)}
   processes.append(record)
   if timeout or exit_code!=0:failures.append({'kind':kind,'batch':offset//a.batch_size,'process':record})
  expected=rows(folder/'original.jsonl');actual=rows(folder/'managed.jsonl')
  if len(expected)!=len(batch) or len(actual)!=len(batch):failures.append({'batch':offset//a.batch_size,'requested':len(batch),'originalRows':len(expected),'managedRows':len(actual),'classification':'missing-observation-not-pass'})
  e={r['name']:r for r in expected};m={r['name']:r for r in actual}
  if len(e)!=len(expected) or len(m)!=len(actual) or any(name not in [r['Name'] for r in batch] for name in e.keys()|m.keys()):raise SystemExit('Duplicate/unrequested response')
  for request in batch:
   name=request['Name'];x=e.get(name);y=m.get(name)
   exact=x is not None and y is not None and x.get('status')=='observed' and y.get('status')=='observed' and x==y
   row={'name':name,'exact':exact,'originalStatus':x.get('status') if x else 'missing','managedStatus':y.get('status') if y else 'missing','batch':offset//a.batch_size}
   if not exact:row['differences']=differences(x,y)
   observations.append(row)
 after={'original':sha(original),'consumer':sha(target),'scanner':sha(scanner),'snapshot':sha(snapshot),'requests':sha(requests),'consumerBuild':sha(build_path)}
 if reference_binding:
  after.update(originalReferenceReport=sha(reference_report),originalReference=sha(reference_file),originalReferenceRequests=sha(reference_requests))
  if any(sha(ROOT/path)!=digest for path,digest in reference_binding['sourceFiles'].items()):failures.append({'classification':'original-reference-source-environment-drift'})
 report={'originalReference':reference_binding,'classification':'actual-original-config-noemit-diagnostics-and-full-type-symbol-baselines','cases':len(source),'exact':sum(r['exact'] for r in observations),'failed':sum(not r['exact'] for r in observations),'sourceSnapshot':build['snapshot'],'sourceSnapshotSha256':build['snapshotSha256'],'scannerSha256':build['scannerSha256'],'inputAndExecutableHashesBefore':before,'inputAndExecutableHashesAfter':after,'drift':before!=after,'elapsedSeconds':round(time.monotonic()-started,3),'processes':processes,'processFailures':failures,'observations':observations,'limits':['Fresh pinned-Go original public no-emit assertion domains, not complete historical emit/compiler-test passes.','Bounded batch scheduling preserves original options and per-row checking/walker order; incremental cache/builder scheduling remains explicit external operation.','Missing/crashed/timed-out observations are failures, never successful exclusions.']}
 (out/'report.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps({k:report[k] for k in ['cases','exact','failed','drift','elapsedSeconds']}));return 0 if report['failed']==0 and not failures and not report['drift'] else 1
if __name__=='__main__':raise SystemExit(main())
