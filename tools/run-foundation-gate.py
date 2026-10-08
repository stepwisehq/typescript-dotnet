#!/usr/bin/env python3
"""Guard the full foundation Native AOT/regression gate against source or asset drift."""
import datetime,hashlib,json,pathlib,subprocess,sys,time
ROOT=pathlib.Path(__file__).resolve().parent.parent
OUT=ROOT/'artifacts/foundation-validation';OUT.mkdir(parents=True,exist_ok=True)
def sources():return subprocess.check_output([sys.executable,'tools/foundation-source-hash.py'],cwd=ROOT,text=True).strip()
def assets():
    paths=[ROOT/'upstream.lock.json',ROOT/'global.json']
    for folder in [ROOT/'tools',ROOT/'validation/corpus',*(ROOT/'validation').glob('*-corpus')]:
        paths.extend(p for p in folder.rglob('*') if p.is_file() and p.suffix in ('.go','.py','.sh','.json','.jsonl','.gz','.patch','.cs','.csproj') and not ({'.cache','bin','obj','__pycache__'}&set(p.relative_to(ROOT).parts)))
    # Expanded gzip corpus is an ignored convenience cache, not another asset.
    paths=[p for p in paths if p.name not in ('source-requests.jsonl','source-units.jsonl')]
    return sorted(set(paths))
def fingerprint(paths):
    result=hashlib.sha256()
    for p in paths:result.update(p.relative_to(ROOT).as_posix().encode()+b'\0'+p.read_bytes()+b'\0')
    return result.hexdigest()
files=assets();source=sources();asset=fingerprint(files);start=time.monotonic();utc=datetime.datetime.now(datetime.timezone.utc).isoformat();command=['bash','tools/verify-foundation.sh'];log=OUT/'final-gate.log'
print('Final foundation gate log: '+str(log),flush=True)
with log.open('w') as stream:result=subprocess.run(command,cwd=ROOT,stdout=stream,stderr=subprocess.STDOUT)
source_drift=source!=sources();asset_drift=files!=assets() or asset!=fingerprint(files);status=result.returncode or int(source_drift or asset_drift)
report=dict(command=command,startedAtUTC=utc,elapsedSeconds=round(time.monotonic()-start,3),exitCode=status,sourceSha256=source,verificationAssetsSha256=asset,sourceDrift=source_drift,assetDrift=asset_drift,verificationAssetFiles=[p.relative_to(ROOT).as_posix() for p in files],log=log.relative_to(ROOT).as_posix())
(OUT/'final-gate.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps({k:v for k,v in report.items() if k!='verificationAssetFiles'}),flush=True);raise SystemExit(status)
