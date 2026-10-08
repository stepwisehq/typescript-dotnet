#!/usr/bin/env python3
"""Stream fresh pinned-Go/C# syntax graphs; retain exact mismatches, store success digests."""
import argparse,collections,gzip,hashlib,json,pathlib,subprocess,tempfile,time
ROOT=pathlib.Path(__file__).resolve().parent.parent

def difference(want,got,path='$'):
    if type(want)!=type(got):return f'{path}: type {type(want).__name__} / {type(got).__name__}'
    if isinstance(want,dict):
        for key in want:
            if key not in got:return f'{path}.{key}: missing'
            if diff:=difference(want[key],got[key],path+'.'+key):return diff
        for key in got:
            if key not in want:return f'{path}.{key}: unexpected'
    elif isinstance(want,list):
        if len(want)!=len(got):return f'{path}: length {len(want)} / {len(got)}'
        for index,(a,b) in enumerate(zip(want,got)):
            if diff:=difference(a,b,f'{path}[{index}]'):return diff
    elif want!=got:return f'{path}: expected {str(want)[:160]!r}, actual {str(got)[:160]!r}'
    return None

def diagnostic_codes(rows):
    for row in rows:
        yield str(row['code'])
        yield from diagnostic_codes(row.get('related', []))
        yield from diagnostic_codes(row.get('chain', []))

def canonical_hash(value):return hashlib.sha256(json.dumps(value,sort_keys=True,ensure_ascii=True,separators=(',',':')).encode()).hexdigest()
class Process:
    def __init__(self,command):
        self.command=command;self.stderr=tempfile.TemporaryFile(mode='w+');self.start()
    def start(self):self.process=subprocess.Popen(self.command,cwd=ROOT,stdin=subprocess.PIPE,stdout=subprocess.PIPE,stderr=self.stderr,text=True,bufsize=1)
    def request(self,line):
        self.process.stdin.write(line);self.process.stdin.flush();output=self.process.stdout.readline()
        if not output:
            self.process.wait(timeout=30);self.stderr.seek(0);error=self.stderr.read();self.stderr.seek(0);self.stderr.truncate();self.start()
            return None,error
        return json.loads(output),None
    def finish(self):
        self.process.stdin.close()
        if self.process.stdout.read():raise RuntimeError('Extra graph rows')
        status=self.process.wait(timeout=30);self.stderr.seek(0);errors=self.stderr.read();self.stderr.close()
        if status:raise RuntimeError(errors)

def main():
    p=argparse.ArgumentParser();p.add_argument('--executable',required=True);p.add_argument('--requests',default='validation/foundation-corpus/source-requests.jsonl.gz');p.add_argument('--output',default='artifacts/foundation-validation/broad-managed');p.add_argument('--limit',type=int);p.add_argument('--scanner',action='store_true');args=p.parse_args()
    output=ROOT/args.output;output.mkdir(parents=True,exist_ok=True);mismatches=output/'mismatches';mismatches.mkdir(exist_ok=True)
    started=time.monotonic();oracle=Process([str(ROOT/'tools/.cache/scanner-oracle' if args.scanner else ROOT/'tools/.cache/parser-oracle')]);managed=Process([args.executable]);counts=collections.Counter();kinds=collections.Counter();codes=collections.Counter();modes=collections.Counter();failures=[]
    requests_path=ROOT/args.requests;opener=gzip.open if requests_path.suffix=='.gz' else open
    with opener(requests_path,'rt') as requests,(output/'digests.jsonl').open('w') as digests:
        for index,line in enumerate(requests):
            if args.limit is not None and index>=args.limit:break
            request=json.loads(line);modes[str(request['scriptKind'])]+=1
            if args.scanner:
                request=dict(name=request['name'],textBytesBase64=request['textBytesBase64'],skipTrivia=False,scriptTarget=99,languageVariant=int(request['scriptKind'] in (2,4)))
                line=json.dumps(request,separators=(',',':'))+'\n'
            want,upstream_error=oracle.request(line);got,port_error=managed.request(line);counts['requests']+=1
            entry=dict(index=index,name=request['name'])
            if upstream_error or port_error:
                failure=dict(**entry,upstreamError=upstream_error,portError=port_error);failures.append(failure)
                # Upstream failures are retained and never silently removed from scope.
                (mismatches/f'{index:05d}-request.json').write_text(line)
                (mismatches/f'{index:05d}-errors.json').write_text(json.dumps(failure,indent=2)+'\n')
                counts['upstreamFailures']+=int(upstream_error is not None);counts['portFailures']+=int(port_error is not None)
                continue
            diff=difference(want,got)
            if diff:
                failures.append(dict(**entry,difference=diff));counts['mismatches']+=1
                for label,data in [('request',request),('go',want),('csharp',got)]: (mismatches/f'{index:05d}-{label}.json').write_text(json.dumps(data,separators=(',',':'))+'\n')
                if len(failures)<=20:print(request['name']+': '+diff,flush=True)
            else:counts['exact']+=1
            if args.scanner:
                counts['tokens']+=len(want['tokens']);kinds.update(str(n['kind']) for n in want['tokens'])
                counts['diagnostics']+=len(want['diagnostics']);codes.update(str(d['code']) for d in want['diagnostics'])
            else:
                counts['nodes']+=len(want['nodes']);kinds.update(n['dataType'] for n in want['nodes'])
                for channel in ['parseDiagnostics','jsDiagnostics','jsDocDiagnostics']:
                    counts[channel]+=len(want[channel]);codes.update(diagnostic_codes(want[channel]))
            entry.update(goSha256=canonical_hash(want),csharpSha256=canonical_hash(got),exact=not bool(diff));digests.write(json.dumps(entry,separators=(',',':'))+'\n')
            if (index+1)%1000==0:print(json.dumps(dict(completed=index+1,counts=dict(counts),elapsedSeconds=round(time.monotonic()-started,2))),flush=True)
    oracle.finish();managed.finish()
    report=dict(stage='scanner' if args.scanner else 'parser',limited=args.limit is not None,requestsFile=str(requests_path.relative_to(ROOT)),requestsSha256=hashlib.sha256(requests_path.read_bytes()).hexdigest(),executable=args.executable,counts=dict(counts),modeCounts=dict(modes),payloadCounts=dict(sorted(kinds.items())),diagnosticCounts=dict(sorted(codes.items())),diagnosticCountScope='Parser counts include related information and message chains; channel totals count root diagnostics.',failures=failures,elapsedSeconds=round(time.monotonic()-started,3),digestsSha256=hashlib.sha256((output/'digests.jsonl').read_bytes()).hexdigest())
    (output/'report.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(dict(counts=dict(counts),failures=len(failures),elapsedSeconds=report['elapsedSeconds']),separators=(',',':')))
    raise SystemExit(bool(failures))
if __name__=='__main__':main()
