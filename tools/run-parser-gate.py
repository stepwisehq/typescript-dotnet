#!/usr/bin/env python3
"""Run the final parser/scanner gates with durable command timing and a log."""
import datetime,hashlib,json,pathlib,subprocess,sys,time
root=pathlib.Path(__file__).resolve().parent.parent
artifacts=root/'artifacts/parser-validation';artifacts.mkdir(parents=True,exist_ok=True)
command=['bash','tools/verify-parser.sh']
source_hash=subprocess.check_output([sys.executable,'tools/parser-source-hash.py'],cwd=root,text=True).strip()
asset_files=sorted(set((root/'tools/parser-oracle').glob('*.go')) | set((root/'tools').glob('*parser*')) | set((root/'validation/parser-corpus').glob('*')))
asset_files=[p for p in asset_files if p.is_file()]
def asset_hash():
 h=hashlib.sha256()
 for p in asset_files:h.update(p.relative_to(root).as_posix().encode());h.update(b'\0');h.update(p.read_bytes());h.update(b'\0')
 return h.hexdigest()
assets=asset_hash()
started_at=datetime.datetime.now(datetime.timezone.utc).isoformat()
started=time.monotonic()
log=artifacts/'final-gate.log'
print('Final gate log: '+str(log),flush=True)
with log.open('w') as stream:result=subprocess.run(command,cwd=root,stdout=stream,stderr=subprocess.STDOUT)
exit_code=result.returncode
if assets!=asset_hash():print('Verification assets changed during final gate',file=sys.stderr);exit_code=1
report=dict(command=command,startedAtUTC=started_at,elapsedSeconds=round(time.monotonic()-started,3),exitCode=exit_code,sourceSha256=source_hash,verificationAssetsSha256=assets,verificationAssetFiles=[p.relative_to(root).as_posix() for p in asset_files],log=log.relative_to(root).as_posix())
(artifacts/'final-gate.json').write_text(json.dumps(report,indent=2)+'\n')
print(json.dumps(report),flush=True)
sys.exit(exit_code)
