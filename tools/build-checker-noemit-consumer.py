#!/usr/bin/env python3
"""Build only the authored no-emit consumer against an immutable central DLL."""
import argparse, glob, hashlib, json, pathlib, subprocess, xml.etree.ElementTree as ET
ROOT=pathlib.Path(__file__).resolve().parents[1]
def sha(p): return hashlib.sha256(p.read_bytes()).hexdigest()
def main():
 p=argparse.ArgumentParser();p.add_argument('--snapshot',type=pathlib.Path,required=True);p.add_argument('--output',type=pathlib.Path,required=True);a=p.parse_args()
 snapshot=a.snapshot.resolve();out=a.output.resolve();data=json.loads(snapshot.read_text());library=snapshot.parent/data['library']
 if sha(library)!=data['files']['TypeScript.Scanner.dll']:raise SystemExit('Central immutable library hash mismatch')
 before=sha(library);out.mkdir(parents=True,exist_ok=True)
 source=ROOT/'validation/Checker.NoEmit.Validation/Checker.NoEmit.Validation.csproj';tree=ET.parse(source)
 for group in tree.getroot().findall('ItemGroup'):
  for child in list(group):
   if child.tag=='ProjectReference':
    group.remove(child);reference=ET.SubElement(group,'Reference',{'Include':'TypeScript.Scanner'});ET.SubElement(reference,'HintPath').text=str(library)
   elif child.tag=='Compile':child.set('Include',str((source.parent/child.get('Include')).resolve()))
 ET.SubElement(tree.getroot().find('ItemGroup'),'Compile',{'Include':str(source.parent/'Program.cs')})
 compiled_sources=sorted({pathlib.Path(p) for group in tree.getroot().findall('ItemGroup') for child in group if child.tag=='Compile' for p in glob.glob(child.get('Include'))})
 source_hashes={str(p.relative_to(ROOT)):sha(p) for p in compiled_sources}
 project=out/'consumer.csproj';tree.write(project)
 command=['dotnet','build',str(project),'-v:q'];result=subprocess.run(command,cwd=ROOT,capture_output=True,text=True)
 (out/'build.log').write_text(result.stdout);(out/'build.stderr').write_text(result.stderr)
 target=out/'bin/Debug/net10.0/Program.Checks.dll'
 report={'command':command,'exitCode':result.returncode,'snapshot':str(snapshot.relative_to(ROOT)),'snapshotSha256':sha(snapshot),'scannerSha256':before,'sourceProjectSha256':sha(source),'sourceEntrySha256':sha(source.parent/'Program.cs'),'compiledSourceFiles':source_hashes}
 if result.returncode==0:
  report['target']=str(target.relative_to(ROOT));report['targetSha256']=sha(target)
  if sha(target.parent/'TypeScript.Scanner.dll')!=before:raise SystemExit('Consumer output Scanner differs from central immutable library')
 if any(sha(ROOT/p)!=h for p,h in source_hashes.items()):raise SystemExit('Consumer source drift while building')
 if sha(library)!=before:raise SystemExit('Central immutable library drift')
 (out/'build-report.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report));return result.returncode
if __name__=='__main__':raise SystemExit(main())
