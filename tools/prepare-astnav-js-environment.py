#!/usr/bin/env python3
"""Prepare only the exact locked TypeScript runtime required by original AstNav JS assertions.
Uses the existing checker API archive/cache and lock, without installing npm dependencies.
"""
import argparse, base64, hashlib, importlib.util, io, json, tarfile
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
spec = importlib.util.spec_from_file_location('checker_api_environment', ROOT/'tools/prepare-checker-api-environment.py')
api = importlib.util.module_from_spec(spec)
spec.loader.exec_module(api)
MEMBERS = ('package.json', 'lib/typescript.js', 'LICENSE.txt', 'ThirdPartyNoticeText.txt')
DEST = ROOT/'upstream/TypeScript/node_modules/typescript'

def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--check', action='store_true')
    parser.add_argument('--archive', type=Path, default=api.CACHE)
    args = parser.parse_args()
    entry = json.loads(api.LOCK.read_text())['packages']['node_modules/typescript']
    if (entry['version'], entry['resolved'], entry['integrity']) != ('6.0.3', api.EXPECTED_URL, api.EXPECTED_INTEGRITY):
        raise RuntimeError('Original pinned runtime dependency changed')
    # Reuse the existing verified archive; its preparer can fetch it if absent.
    data = args.archive.read_bytes()
    if 'sha512-'+base64.b64encode(hashlib.sha512(data).digest()).decode() != api.EXPECTED_INTEGRITY:
        raise RuntimeError('Locked runtime archive integrity mismatch')
    hashes = {}
    with tarfile.open(fileobj=io.BytesIO(data)) as archive:
        for name in MEMBERS:
            member = archive.extractfile('package/'+name)
            if member is None: raise RuntimeError('Required authentic runtime member missing: '+name)
            value = member.read()
            target = DEST/name
            if args.check:
                if target.read_bytes() != value: raise RuntimeError('Locked runtime reproduction differs: '+name)
            else:
                target.parent.mkdir(parents=True, exist_ok=True)
                target.write_bytes(value)
            hashes[name] = {'sha256': hashlib.sha256(value).hexdigest(), 'bytes': len(value), 'originalMember': 'package/'+name}
    print(json.dumps({'version':'6.0.3', 'archiveSha256': hashlib.sha256(data).hexdigest(),
        'integrity':api.EXPECTED_INTEGRITY, 'files':hashes, 'runtimeTests':'not performed'}, indent=2))

if __name__ == '__main__': main()
