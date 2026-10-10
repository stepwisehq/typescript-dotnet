#!/usr/bin/env python3
"""Relocate only authored source-path metadata for an unchanged original registry."""
import gzip,hashlib,json,pathlib
ROOT=pathlib.Path(__file__).resolve().parents[1];folder=ROOT/'validation/checker-noemit-corpus'
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
with gzip.open(ROOT/'validation/checker-noemit-decoded-inventory/fixtures.jsonl.gz','rt') as f:inventory={row['source'].split('/tests/cases/')[1]:row for row in map(json.loads,f)}
paths={name:str(ROOT/row['source']) for name,row in inventory.items()}
requests=[json.loads(line) for line in (folder/'requests.jsonl').read_text().splitlines()];prep=json.loads((folder/'preparation-dispositions.json').read_text())
for row in requests+prep:
 # Configuration suffixes are original parenthesized names; derive source from
 # immutable raw hash plus the exact fixture path, never select another input.
 old=pathlib.PurePath(row['Source']).as_posix();name=old.split('/tests/cases/',1)[1]
 assert name in inventory and row['SourceSha256']==inventory[name]['sourceSha256'] and sha(pathlib.Path(paths[name]))==row['SourceSha256']
 row['Source']=paths[name]
(folder/'requests.jsonl').write_text(''.join(json.dumps(row,separators=(',',':'))+'\n' for row in requests))
(folder/'preparation-dispositions.json').write_text(json.dumps(prep,indent=2)+'\n')
provenance=json.loads((folder/'provenance.json').read_text());provenance['requestsSha256']=sha(folder/'requests.jsonl');provenance['preparationDispositionsSha256']=sha(folder/'preparation-dispositions.json')
(folder/'provenance.json').write_text(json.dumps(provenance,indent=2)+'\n')
m=json.loads((folder/'manifest.json').read_text())
for k in ('requests','preparation','provenance'):m[k+'Sha256']=sha(ROOT/m[k])
(folder/'manifest.json').write_text(json.dumps(m,indent=2)+'\n')
catalog=ROOT/'docs/checker-noemit-tests.json';selection=json.loads(catalog.read_text());selection['manifestSha256']=sha(folder/'manifest.json');catalog.write_text(json.dumps(selection,indent=2)+'\n')
print('Original source bytes/options/references unchanged; authored source-path metadata rebound.')
