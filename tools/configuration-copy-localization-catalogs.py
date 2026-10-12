#!/usr/bin/env python3
"""Copy exact pinned diagnostic gzip archives; do not regenerate translations."""
from pathlib import Path
import argparse,hashlib,json
ROOT=Path(__file__).resolve().parents[1]
parser=argparse.ArgumentParser(description=__doc__)
parser.add_argument('--check',action='store_true')
args=parser.parse_args()
manifest=json.loads((ROOT/'docs/progress/lsp-localization-catalogs.json').read_text())
out=ROOT/'src/TypeScript.Scanner/Diagnostics/Locales'
out.mkdir(exist_ok=True)
for name,item in manifest['resources'].items():
    data=(ROOT/item['source']).read_bytes()
    if hashlib.sha256(data).hexdigest()!=item['sha256']: raise SystemExit('Pinned catalog drift: '+name)
    output=out/(name+'.json.gz')
    if args.check:
        if output.read_bytes()!=data: raise SystemExit('Copied catalog drift: '+name)
    else: output.write_bytes(data)
