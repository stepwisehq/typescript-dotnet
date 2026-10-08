#!/usr/bin/env python3
"""Time the complete binder/parser/scanner gate and reject source/asset drift."""
import datetime, hashlib, json, pathlib, subprocess, sys, time
root = pathlib.Path(__file__).resolve().parent.parent
artifacts = root/'artifacts/binder-validation'; artifacts.mkdir(parents=True, exist_ok=True)
command = ['bash', 'tools/verify-binder.sh']
source_hash = subprocess.check_output([sys.executable, 'tools/binder-source-hash.py'], cwd=root, text=True).strip()
asset_files = sorted(p for base in ['tools', 'validation/corpus', 'validation/parser-corpus', 'validation/binder-corpus'] for p in (root/base).rglob('*') if p.is_file() and p.suffix in ['.go', '.py', '.sh', '.json', '.jsonl', '.patch', '.cs', '.csproj'] and not ({'.cache', 'bin', 'obj', '__pycache__'} & set(p.relative_to(root).parts)))
asset_files += [root/'upstream.lock.json']
def asset_hash():
    digest = hashlib.sha256()
    for path in sorted(asset_files):
        digest.update(path.relative_to(root).as_posix().encode()); digest.update(b'\0')
        digest.update(path.read_bytes()); digest.update(b'\0')
    return digest.hexdigest()
assets = asset_hash(); started_at = datetime.datetime.now(datetime.timezone.utc).isoformat(); started = time.monotonic()
log = artifacts/'final-gate.log'; print('Final gate log: '+str(log), flush=True)
with log.open('w') as stream:
    result = subprocess.run(command, cwd=root, stdout=stream, stderr=subprocess.STDOUT)
exit_code = result.returncode
if assets != asset_hash(): print('Verification assets changed during final gate', file=sys.stderr); exit_code = 1
if source_hash != subprocess.check_output([sys.executable, 'tools/binder-source-hash.py'], cwd=root, text=True).strip(): print('Executable source changed during final gate', file=sys.stderr); exit_code = 1
report = dict(command=command, startedAtUTC=started_at, elapsedSeconds=round(time.monotonic()-started, 3), exitCode=exit_code, sourceSha256=source_hash, verificationAssetsSha256=assets, verificationAssetFiles=[p.relative_to(root).as_posix() for p in sorted(asset_files)], log=log.relative_to(root).as_posix())
(artifacts/'final-gate.json').write_text(json.dumps(report, indent=2)+'\n')
print(json.dumps({k:report[k] for k in ['exitCode', 'elapsedSeconds', 'sourceSha256', 'verificationAssetsSha256', 'log']}), flush=True)
sys.exit(exit_code)
