#!/usr/bin/env python3
"""Exact ordinary config CLI comparison plus explicit unsupported boundaries."""
import argparse,base64,hashlib,importlib.util,json,pathlib,subprocess,tempfile,time
ROOT=pathlib.Path(__file__).resolve().parents[1]
def module(name,file):
 spec=importlib.util.spec_from_file_location(name,ROOT/'tools'/file);loaded=importlib.util.module_from_spec(spec);spec.loader.exec_module(loaded);return loaded
compare=module('configuration_compare','compare-configuration.py')
project=module('project_cli_roles','check-projects-cli.py')
def sha(path):return hashlib.sha256(path.read_bytes()).hexdigest()
def normalize(result,paths):
 result=dict(result)
 def owned(path):
  if path not in paths:raise ValueError('Unowned CLI filesystem path: '+path)
  return paths[path]
 def diagnostic(row):
  row=dict(row);project.diagnostic(row)
  if row['File'] is not None:row['File']=owned(row['File'])
  # Genuine rendered import-type tokens only; quoted literals and prefixes stay exact.
  module_paths={str(pathlib.PurePosixPath(k).with_suffix('')):str(pathlib.PurePosixPath(v).with_suffix(''))for k,v in paths.items()if k.endswith(('.ts','.tsx'))}
  row['Message']=project.relocate_type_imports(row['Message'],module_paths)
  # These fixed config-read/missing-file diagnostic templates contain actual filename arguments.
  if row['Code'] in (5083,6053):
   for actual,virtual in paths.items():row['Message']=row['Message'].replace("'"+actual+"'","'"+virtual+"'")
  for field in ('Chain','Related'):
   if row[field] is not None:row[field]=[diagnostic(value)for value in row[field]]
  return row
 result['Diagnostics']=[diagnostic(row)for row in result['Diagnostics']]
 if 'Files' in result:result['Files']=[owned(path)for path in result['Files']]
 if 'FileNamesBase64' in result:result['FileNamesBase64']=[base64.b64encode(owned(base64.b64decode(path,validate=True).decode()).encode()).decode()for path in result['FileNamesBase64']]
 return result
def main():
 ap=argparse.ArgumentParser(description=__doc__);ap.add_argument('--manifest',type=pathlib.Path,required=True);ap.add_argument('--executable',type=pathlib.Path,required=True);ap.add_argument('--managed',action='store_true');ap.add_argument('--output',type=pathlib.Path,required=True);a=ap.parse_args();manifest=compare.parse(a.manifest.read_bytes());block=manifest['cliControls'];a.output.mkdir(parents=True,exist_ok=True)
 for key in ('requests','expected','provenance'):
  if sha(ROOT/block[key])!=block[key+'Sha256']:raise ValueError('CLI asset hash differs: '+key)
 requests=[compare.parse(x)for x in (ROOT/block['requests']).read_bytes().splitlines()];expected={row['name']:row['result']for row in [compare.parse(x)for x in (ROOT/block['expected']).read_bytes().splitlines()]}
 if len(requests)!=block['cases'] or len({row['name']for row in requests})!=len(requests):raise ValueError('CLI request inventory differs')
 names={r['name']for r in requests if r['exitCode']!=2}
 if expected.keys()!=names:raise ValueError('CLI Go expected registry differs from semantic cases')
 before=sha(a.executable);failures=[];records=[];exact=0;start=time.monotonic()
 for row in requests:
  with tempfile.TemporaryDirectory(prefix='configuration-cli-proof-')as directory:
   root=pathlib.Path(directory);logical=row['logicalRoot'];paths={}
   def destination(path):
    if not path.startswith(logical+'/'):raise ValueError('Fixture path outside logical root')
    relative=path[len(logical)+1:]
    if '..' in pathlib.PurePosixPath(relative).parts:raise ValueError('Unnormalized fixture path')
    actual=root/relative;paths[str(actual)]=path;return actual
   config=destination(row['configPath'])
   for file in row['files']:
    path=destination(file['path']);path.parent.mkdir(parents=True,exist_ok=True);path.write_text(file['text'])
   for raw in row.get('rawEntries',[]):
    relative=base64.b64decode(raw['pathBase64'],validate=True)
    if relative.startswith(b'/')or b'..'in relative.split(b'/'):raise ValueError('Unsafe raw fixture path')
    native=str(root).encode()+b'/'+relative
    with open(native,'wb')as stream:stream.write(raw['text'].encode())
   args=(['dotnet']if a.managed else[])+[str(a.executable.resolve()),'--inspect-config'if row['cliAction']=='inspect'else'--config',str(config)]
   if row['cliAction']=='check':args.append('--json')
   process=subprocess.run(args,capture_output=True,cwd=ROOT,timeout=120)
   stem=a.output/row['name'];stem.with_suffix('.stdout.json').write_bytes(process.stdout);stem.with_suffix('.stderr.txt').write_bytes(process.stderr)
   try:
    actual=compare.parse(process.stdout)
    if process.returncode!=row['exitCode']or actual['ExitCode']!=row['exitCode']:raise ValueError('CLI exit differs')
    if row['exitCode']==2:
     if actual.keys()!={'ExitCode','Error','Diagnostics'}or actual['Diagnostics']!=[]or not isinstance(actual['Error'],str)or actual['Error']!=row['unsupportedError']:raise ValueError('Unsupported boundary response differs')
     normalized=None
    else:
     normalized=normalize(actual,paths);mismatch=compare.difference(expected[row['name']],normalized)
     if mismatch:raise ValueError(json.dumps(mismatch))
    exact+=1
   except (ValueError,KeyError,TypeError)as error:failures.append({'name':row['name'],'reason':str(error)})
   records.append({'name':row['name'],'exitCode':process.returncode,'expectedExitCode':row['exitCode'],'actual':normalized if 'normalized'in locals()else None,'stdoutSha256':hashlib.sha256(process.stdout).hexdigest(),'stderrSha256':hashlib.sha256(process.stderr).hexdigest(),'originalGo':row['name']in expected})
 drift=before!=sha(a.executable)
 if drift:failures.append({'reason':'Executable drift'})
 # Reuse independently literal/prefix-tested lexical projection, not blanket replacement.
 lexical=project.lexical_controls()
 report={'cases':len(requests),'exact':exact,'failures':failures,'managed':a.managed,'seconds':round(time.monotonic()-start,3),'manifestSha256':sha(a.manifest),'executableSha256':before,'executableDrift':drift,'originalGoCases':len(expected),'authoredBoundaryCases':len(requests)-len(expected),'lexicalProjectionControls':lexical,'records':records}
 (a.output/'report.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps({k:report[k]for k in ('cases','exact','failures','seconds')}));return bool(failures)
if __name__=='__main__':raise SystemExit(main())
