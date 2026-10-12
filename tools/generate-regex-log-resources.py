#!/usr/bin/env python3
"""Lossless literal decoding for the original selected RE2 MatchString channels."""
from pathlib import Path
import base64,bz2,gzip,hashlib,json
scope={};prefix=Path('tools/generate-regex-checks.py').read_text().split('alltext=')[0]
exec(prefix.replace('root=Path(__file__).resolve().parents[1]','root=Path.cwd()'),scope)
raw=scope['raw'];out=Path('validation/LanguageServices.Checks/RegexData');out.mkdir(exist_ok=True);records=[]
for name in ['re2-search.txt','re2-exhaustive.txt.bz2']:
 src=Path('tools/.cache/go/src/regexp/testdata')/name;dst=out/(name.removesuffix('.bz2')+'.gz');count=0
 with (bz2.open(src,'rb') if name.endswith('.bz2') else src.open('rb')) as reader,dst.open('wb') as backing,gzip.GzipFile(filename='',mode='wb',fileobj=backing,mtime=0) as writer:
  for line in reader:
   if line.startswith(b'"'):line=b'@'+base64.b64encode(raw(line.decode('utf8').strip()))+b'\n'
   writer.write(line);count+=1
 records.append({'source':str(src),'sourceSha256':hashlib.sha256(src.read_bytes()).hexdigest(),'resource':str(dst),'resourceSha256':hashlib.sha256(dst.read_bytes()).hexdigest(),'lines':count,'adaptation':'original quoted Go literals decoded exactly to raw bytes and base64 encoded; every nonliteral line retained byte-for-byte; no expected result changes'})
(out/'provenance.json').write_text(json.dumps(records,indent=2)+'\n');print(json.dumps(records))
