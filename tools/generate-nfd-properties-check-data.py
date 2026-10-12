#!/usr/bin/env python3
"""Pinned Unicode17 original canonical property expected data, not production tables."""
from pathlib import Path
import re,struct,gzip,json,hashlib
scope={};prefix=Path('tools/generate-regex-checks.py').read_text().split('alltext=')[0]
exec(prefix.replace('root=Path(__file__).resolve().parents[1]','root=Path.cwd()'),scope)
raw=scope['raw'];src=Path('/home/greg/go/pkg/mod/golang.org/x/text@v0.42.0/unicode/norm/data17.0.0_test.go');rows=[]
for line in src.read_text().splitlines():
 m=re.match(r'\s*\{(0x[0-9a-f]+), (\d+), (\d+), (\d+), ([fg])\((.*)\)\},',line)
 if not m:continue
 rune,ccc,lead,trail,fn,args=m.groups();tokens=list(re.finditer(scope['strpat'],args));qc={'Yes':0,'No':1,'Maybe':2}[args.split(',',1)[0]]
 canonical=raw(tokens[0].group())
 rows.append((int(rune,16),int(ccc),int(lead),int(trail),qc,canonical))
assert len(rows)>7000
out=Path('validation/LanguageServices.Checks/RegexData/nfd-canonical-properties.bin.gz')
with out.open('wb') as f,gzip.GzipFile(filename='',fileobj=f,mode='wb',mtime=0) as writer:
 writer.write(struct.pack('<i',len(rows)))
 for rune,ccc,lead,trail,qc,canonical in rows:
  writer.write(struct.pack('<iBBBBi',rune,ccc,lead,trail,qc,len(canonical)));writer.write(canonical)
meta={'source':str(src),'sourceSha256':hashlib.sha256(src.read_bytes()).hexdigest(),'resource':str(out),'resourceSha256':hashlib.sha256(out.read_bytes()).hexdigest(),'rows':len(rows),'scope':'original TestProperties canonical CCC/combinesBackward/nonstarter counts/decomposition assertion channel; compatibility/composing observations excluded','transport':'exact little-endian primitive fields and original UTF8 canonical decomposition bytes, gzip mtime0'}
Path('validation/LanguageServices.Checks/RegexData/nfd-canonical-properties-provenance.json').write_text(json.dumps(meta,indent=2)+'\n');print(meta)
