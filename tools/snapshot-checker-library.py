#!/usr/bin/env python3
"""Copy an already built production library; never build production from verification."""
import argparse,hashlib,json,pathlib,shutil
ROOT=pathlib.Path(__file__).resolve().parents[1]
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
p=argparse.ArgumentParser();p.add_argument('--library',type=pathlib.Path,default=ROOT/'src/TypeScript.Scanner/bin/Release/net10.0/TypeScript.Scanner.dll');p.add_argument('--output',type=pathlib.Path,required=True);a=p.parse_args()
source=a.library.resolve();out=a.output.resolve();(out/'snapshot').mkdir(parents=True,exist_ok=True)
files={}
for name in ('TypeScript.Scanner.dll','TypeScript.Scanner.pdb','TypeScript.Scanner.deps.json'):
 path=source.parent/name;shutil.copy2(path,out/'snapshot'/name);files[name]=sha(path)
sources={str(p.relative_to(ROOT)):sha(p) for p in sorted((ROOT/'src').rglob('*')) if p.is_file() and p.suffix in ('.cs','.csproj') and not {'bin','obj'}.intersection(p.parts)}
(out/'snapshot.json').write_text(json.dumps({'configuration':'Release','library':'snapshot/TypeScript.Scanner.dll','files':files,'sourceFiles':sources,'qualification':'Copied existing build; acceptance still requires source/hash/final runtime qualification.'},indent=2)+'\n')
print(str(out/'snapshot.json'))
