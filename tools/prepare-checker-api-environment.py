#!/usr/bin/env python3
"""Prepare byte-exact public API declaration roles from the upstream locked package."""
import argparse, base64, hashlib, io, json, tarfile, urllib.request
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
DEST = ROOT / 'validation/checker-noemit-environment/typescript-6.0.3'
CACHE = ROOT / 'tools/.cache/checker-api-environment/typescript-6.0.3.tgz'
LOCK = ROOT / 'upstream/TypeScript/package-lock.json'
EXPECTED_URL = 'https://registry.npmjs.org/typescript/-/typescript-6.0.3.tgz'
EXPECTED_INTEGRITY = 'sha512-y2TvuxSZPDyQakkFRPZHKFm+KKVqIisdg9/CZwm9ftvKXLP8NRWj38/ODjNbr43SsoXqNuAisEf1GdCxqWcdBw=='
MEMBERS = ('lib/typescript.d.ts', 'lib/tsserverlibrary.d.ts', 'LICENSE.txt', 'ThirdPartyNoticeText.txt', 'package.json')

def sha(data): return hashlib.sha256(data).hexdigest()

def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--check',action='store_true')
    parser.add_argument('--archive',type=Path,help='Existing locked tarball; its integrity is still mandatory')
    args=parser.parse_args()
    entry=json.loads(LOCK.read_text())['packages']['node_modules/typescript']
    if entry['version']!='6.0.3' or entry['resolved']!=EXPECTED_URL or entry['integrity']!=EXPECTED_INTEGRITY:
        raise RuntimeError('Pinned original dependency changed')
    archive=args.archive or CACHE
    if not archive.exists():
        archive.parent.mkdir(parents=True,exist_ok=True)
        with urllib.request.urlopen(EXPECTED_URL,timeout=60) as response: data=response.read()
        if 'sha512-'+base64.b64encode(hashlib.sha512(data).digest()).decode()!=EXPECTED_INTEGRITY:
            raise RuntimeError('Locked package integrity mismatch')
        archive.write_bytes(data)
    data=archive.read_bytes()
    if 'sha512-'+base64.b64encode(hashlib.sha512(data).digest()).decode()!=EXPECTED_INTEGRITY:
        raise RuntimeError('Locked package integrity mismatch')
    payloads={}
    with tarfile.open(fileobj=io.BytesIO(data)) as tar:
        names=set(tar.getnames())
        for name in MEMBERS:
            member=tar.extractfile('package/'+name)
            if member is None: raise RuntimeError('Authentic required package member missing: '+name)
            payloads[name]=member.read()
        absent=[name for name in ('lib/typescript.internal.d.ts','lib/tsserverlibrary.internal.d.ts') if 'package/'+name not in names]
    if len(absent)!=2: raise RuntimeError('Internal role disposition changed')
    provenance={'classification':'authentic-upstream-locked-public-api-environment-adaptation',
        'upstream':'fed0bf24149fb1ed36039212648bafdafc1ea10e','version':'6.0.3',
        'lockSource':str(LOCK.relative_to(ROOT)),'lockSourceSha256':sha(LOCK.read_bytes()),
        'generator':'tools/prepare-checker-api-environment.py','generatorSha256':sha(Path(__file__).read_bytes()),
        'url':EXPECTED_URL,'integrity':EXPECTED_INTEGRITY,'archiveSha256':sha(data),'archiveBytes':len(data),
        'files':{name:{'sha256':sha(value),'bytes':len(value),'originalMember':'package/'+name} for name,value in payloads.items()},
        'virtualRoles':{'/.ts/typescript.d.ts':'lib/typescript.d.ts','/.ts/tsserverlibrary.d.ts':'lib/tsserverlibrary.d.ts'},
        'unavailableOriginalRoles':['/.ts/typescript.internal.d.ts','/.ts/tsserverlibrary.internal.d.ts'],
        'observationBoundary':'Nine original public API fixtures have authentic locked declarations available for fresh pinned-Go no-emit references. Historical built-version baselines remain immutable and version differences explicit. APILibCheck internal inputs remain unavailable; no fabricated aliases or successful checking claim.'}
    payloads['provenance.json']=(json.dumps(provenance,indent=2)+'\n').encode()
    if args.check:
        for name,value in payloads.items():
            if (DEST/name).read_bytes()!=value: raise RuntimeError('Locked environment reproduction differs: '+name)
    else:
        for name,value in payloads.items():
            target=DEST/name;target.parent.mkdir(parents=True,exist_ok=True);target.write_bytes(value)
    print(json.dumps({'files':len(MEMBERS),'archiveIntegrity':'exact','unavailableInternalRoles':2,'semanticExecution':'not performed'}))

if __name__=='__main__':main()
