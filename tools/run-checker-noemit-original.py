#!/usr/bin/env python3
"""Produce complete original no-emit references in bounded process batches.

This is reference production, not a managed pass or historical emit-test pass.
"""
import argparse,gzip,hashlib,json,os,pathlib,subprocess,time
ROOT=pathlib.Path(__file__).resolve().parents[1]
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def main():
 p=argparse.ArgumentParser();p.add_argument('--requests',type=pathlib.Path,required=True);p.add_argument('--original',type=pathlib.Path,required=True);p.add_argument('--output',type=pathlib.Path,required=True);p.add_argument('--batch-size',type=int,default=32);p.add_argument('--timeout',type=int,default=120);a=p.parse_args()
 if a.batch_size<1 or a.timeout<1:raise RuntimeError('Positive batch size/timeout required')
 requests=a.requests.resolve();original=a.original.resolve();out=a.output.resolve();out.mkdir(parents=True,exist_ok=True)
 rows=[json.loads(l) for l in requests.read_text().splitlines()];names=[r['Name'] for r in rows]
 if not rows or len(names)!=len(set(names)):raise RuntimeError('Empty/duplicate requests')
 sourcepaths=list((ROOT/'tools/checker-noemit-oracle').glob('*.go'))+[pathlib.Path(__file__).resolve()]
 sourcepaths+=list((ROOT/'validation/checker-noemit-environment/typescript-6.0.3').rglob('*'))
 sourcepaths+=[ROOT/'validation/checker-noemit-decoded-inventory/provenance.json',ROOT/'validation/checker-noemit-decoded-inventory/fixtures.jsonl.gz']
 sources={str(p.relative_to(ROOT)):sha(p) for p in sorted(sourcepaths) if p.is_file()}
 before={'requests':sha(requests),'original':sha(original)};records=[];failures=[];observations=[];start=time.monotonic()
 with (out/'original.jsonl.gz').open('wb') as raw,gzip.GzipFile(fileobj=raw,mode='wb',mtime=0) as compressed:
  for offset in range(0,len(rows),a.batch_size):
   batch=rows[offset:offset+a.batch_size];folder=out/f'batch-{offset//a.batch_size:04d}';folder.mkdir(exist_ok=True)
   input=folder/'requests.jsonl';output=folder/'original.jsonl';input.write_text(''.join(json.dumps(r,separators=(',',':'))+'\n' for r in batch))
   command=[str(original),'-test.run=^TestAuthoredWholeNoEmit$','-test.count=1','-test.v'];env=os.environ|{'CHECKER_NOEMIT_REQUESTS':str(input),'CHECKER_NOEMIT_OUTPUT':str(output)};started=time.monotonic()
   with (folder/'original.log').open('wb') as log,(folder/'original.stderr').open('wb') as stderr:
    try:r=subprocess.run(command,cwd=ROOT/'upstream/TypeScript/tsc',env=env,stdout=log,stderr=stderr,timeout=a.timeout);code=r.returncode;timeout=False
    except subprocess.TimeoutExpired:code=None;timeout=True
   values={};parseErrors=[]
   if output.exists():
    for number,line in enumerate(output.read_bytes().splitlines(),1):
     try:row=json.loads(line)
     except json.JSONDecodeError as error:parseErrors.append({'line':number,'error':str(error)});continue
     name=row.get('name')
     if name in values or name not in [r['Name'] for r in batch]:raise RuntimeError('Duplicate/unrequested original response')
     values[name]=row;compressed.write(line+b'\n')
   record={'batch':offset//a.batch_size,'command':command,'exitCode':code,'timeout':timeout,'elapsedSeconds':round(time.monotonic()-started,3),'requestsSha256':sha(input),'outputSha256':sha(output) if output.exists() else None,'stdoutSha256':sha(folder/'original.log'),'stderrSha256':sha(folder/'original.stderr'),'observed':len(values),'parseErrors':parseErrors};records.append(record)
   if code!=0 or timeout or parseErrors:failures.append(record)
   for request in batch:
    row=values.get(request['Name']);valid=row is not None and row.get('sourceSha256')==request['SourceSha256'] and row.get('configuration')==request['Configuration'];observed=valid and row.get('status')=='observed'
    observations.append({'name':request['Name'],'batch':offset//a.batch_size,'status':row.get('status') if row else 'missing','originalObserved':observed})
   if offset//a.batch_size%20==0:
    print(json.dumps({'completed':min(offset+len(batch),len(rows)),'total':len(rows),'failedRows':sum(not r['originalObserved'] for r in observations),'elapsedSeconds':round(time.monotonic()-start,3)}),flush=True)
 after={'requests':sha(requests),'original':sha(original)};drift=before!=after or any(sha(ROOT/p)!=h for p,h in sources.items())
 report={'classification':'fresh-original-noemit-reference-production-not-target-or-emit-test-pass','requests':str(requests.relative_to(ROOT)),'requestsSha256':before['requests'],'cases':len(rows),'originalObserved':sum(r['originalObserved'] for r in observations),'failedRows':sum(not r['originalObserved'] for r in observations),'elapsedSeconds':round(time.monotonic()-start,3),'reference':'original.jsonl.gz','referenceSha256':sha(out/'original.jsonl.gz'),'sourceFiles':sources,'hashesBefore':before,'hashesAfter':after,'drift':drift,'processes':records,'processFailures':failures,'observations':observations,'boundary':'Original unchanged options, genuine libraries, all no-emit diagnostics, full original type/symbol walker, trace, original union and parent-pointer assertions. Missing/crashed/timed-out/failed observations are not passes. Output/declaration/incremental-cache assertion domains remain separate.'}
 (out/'report.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps({k:report[k] for k in ('cases','originalObserved','failedRows','elapsedSeconds','drift')}));return 0 if not report['failedRows'] and not failures and not drift else 1
if __name__=='__main__':raise SystemExit(main())
