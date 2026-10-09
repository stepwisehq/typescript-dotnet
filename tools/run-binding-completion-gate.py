#!/usr/bin/env python3
"""Guard complete binding/foundation/semantic verification without changing prior proof."""
import datetime,hashlib,json,pathlib,subprocess,sys,time
ROOT=pathlib.Path(__file__).resolve().parent.parent
OUT=ROOT/'artifacts/binding-completion-validation'
def sources():return subprocess.check_output([sys.executable,'tools/foundation-source-hash.py'],cwd=ROOT,text=True).strip()
def assets():
    files=[ROOT/'global.json',ROOT/'upstream.lock.json']
    for base in [ROOT/'tools',ROOT/'validation/corpus',*(ROOT/'validation').glob('*-corpus')]:
        files.extend(p for p in base.rglob('*') if p.is_file() and p.suffix in ('.go','.py','.sh','.json','.jsonl','.gz','.patch','.cs','.csproj') and not ({'.cache','bin','obj','__pycache__'}&set(p.relative_to(ROOT).parts)))
    return sorted(set(p for p in files if p.name not in ('source-requests.jsonl','source-units.jsonl')))
def fingerprint(files):
    h=hashlib.sha256()
    for p in files:h.update(p.relative_to(ROOT).as_posix().encode()+b'\0'+p.read_bytes()+b'\0')
    return h.hexdigest()
def main():
    OUT.mkdir(parents=True,exist_ok=True);source=sources();files=assets();asset=fingerprint(files);start=time.monotonic();utc=datetime.datetime.now(datetime.timezone.utc).isoformat();command=['bash','tools/verify-binding-completion.sh']
    # Historical fixture/proof bytes remain accepted artifacts, not new output destinations.
    historical_names=subprocess.check_output(['git','ls-tree','-r','--name-only','2886d47','validation/corpus','validation/parser-corpus','validation/binder-corpus','validation/checker-corpus','validation/object-checker-corpus','validation/data-types-corpus','validation/foundation-corpus','validation/results'],cwd=ROOT,text=True).splitlines()
    protected=[ROOT/name for name in historical_names]
    old={p:p.read_bytes() for p in protected}
    for p,value in old.items():
        pinned=subprocess.check_output(['git','show','2886d47:'+p.relative_to(ROOT).as_posix()],cwd=ROOT)
        if value!=pinned:raise RuntimeError('Historical asset changed before gate: '+str(p))
    print('Final binding completion log: '+str(OUT/'final-gate.log'),flush=True)
    with (OUT/'final-gate.log').open('w') as log:result=subprocess.run(command,cwd=ROOT,stdout=log,stderr=subprocess.STDOUT)
    source_drift=source!=sources();asset_drift=files!=assets() or asset!=fingerprint(files);historical_drift=any(p.read_bytes()!=value for p,value in old.items());status=result.returncode or int(source_drift or asset_drift or historical_drift)
    report=dict(command=command,startedAtUTC=utc,elapsedSeconds=round(time.monotonic()-start,3),exitCode=status,sourceSha256=source,verificationAssetsSha256=asset,sourceDrift=source_drift,assetDrift=asset_drift,historicalAssetDrift=historical_drift,verificationAssetFiles=[p.relative_to(ROOT).as_posix() for p in files],log='artifacts/binding-completion-validation/final-gate.log')
    (OUT/'final-gate.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps({k:v for k,v in report.items() if k!='verificationAssetFiles'}),flush=True);raise SystemExit(status)
if __name__=='__main__':main()
