#!/usr/bin/env python3
"""Complete original no-emit records, exact dispositions, and one retained cascade."""
import argparse,collections,gzip,hashlib,importlib.util,json,os,pathlib,subprocess,sys,time,xml.etree.ElementTree as ET
ROOT=pathlib.Path(__file__).resolve().parents[1]
OUT=ROOT/'artifacts/checker-noemit-validation/final'
BASELINE='c5e96e1'
MANIFEST='validation/checker-noemit-corpus/manifest.json'
if os.environ.get('DECLARATIONS_CURRENT_CHECKING')!='1' or os.environ.get('CHECKER_LANGUAGE_CURRENT')!='1':raise RuntimeError('Both explicit reviewed current checking and language capability contexts are required')
spec=importlib.util.spec_from_file_location('retained_protocols',ROOT/'tools/run-protocols-gate.py')
retained=importlib.util.module_from_spec(spec);spec.loader.exec_module(retained)
sha,read,sources,fingerprint=retained.sha,retained.read,retained.sources,retained.fingerprint

def write(path,value):
 path.parent.mkdir(parents=True,exist_ok=True);path.write_text(json.dumps(value,indent=2)+'\n')
def rows(path):
 with (gzip.open(path,'rt') if str(path).endswith('.gz') else open(path)) as f:return [json.loads(line) for line in f if line.strip()]
def source_snapshot(path):
 data=read(path)
 for p,digest in data['sourceFiles'].items():
  if sha(p)!=digest:raise RuntimeError('Production snapshot source differs: '+p)
 library=path.parent/data['library']
 if sha(library)!=data['files']['TypeScript.Scanner.dll']:raise RuntimeError('Immutable DLL differs')
 return data,library

def obligations():
 catalog=read('docs/checker-noemit-tests.json');catalog.pop('execution',None)
 return {'contractSha256':sha('docs/LANGUAGE-CONTRACT.md'),'selection':catalog,'retained':retained.obligations()}
def assets():
 paths=set(retained.assets())
 for folder in ('validation/checker-noemit-corpus','validation/checker-noemit-decoded-inventory','validation/checker-noemit-environment','validation/Checker.NoEmit.Validation','tests/Checker.NoEmit.Tests','tools/checker-noemit-oracle'):
  paths.update(p for p in (ROOT/folder).rglob('*') if p.is_file() and not {'bin','obj','__pycache__'}.intersection(p.parts))
 paths.update(ROOT/p for p in ('docs/LANGUAGE-CONTRACT.md','docs/CHECKER-NOEMIT-VERIFY.md'))
 return sorted(paths)
def historical():
 result={}
 for name in subprocess.check_output(['git','ls-tree','-r','--name-only',BASELINE,'validation'],cwd=ROOT,text=True).splitlines():
  folder=pathlib.Path(name).parts[1]
  if folder not in ('corpus','results') and not folder.endswith('-corpus'):continue
  if (ROOT/name).read_bytes()!=subprocess.check_output(['git','show',BASELINE+':'+name],cwd=ROOT):raise RuntimeError('Historical evidence changed: '+name)
  result[name]=sha(name)
 return result

def preflight(snapshot):
 m=read(MANIFEST)
 if read('docs/checker-noemit-tests.json')['manifestSha256']!=sha(MANIFEST):raise RuntimeError('Catalog manifest correlation differs')
 if m['baseline']!=BASELINE or m['upstream']!=read('upstream.lock.json')['revision']:raise RuntimeError('Wrong pin')
 for key in ('requests','expected','preparation','provenance'):
  retained.checked_path(m[key],m[key+'Sha256'])
 pr=read(m['provenance'])
 promotion=m['authoredCapabilityPromotion']
 if promotion!={'provenance':'validation/checker-noemit-corpus/authored-capability-migration.json','provenanceSha256':'a66b9e07699c34ba705ecb4e7a64671c5f6f8dbb0f8c42c7e4515dd485e92bd2','retainedSharedCheck':'Authored.ExternalSemanticConsumers','completeSameInputProgramRecords':4,'sameInputResolutionSemanticComparisons':2}:raise RuntimeError('Unreviewed capability promotion')
 retained.checked_path(promotion['provenance'],promotion['provenanceSha256'])
 migration=read(promotion['provenance'])
 for p,digest in {**migration['sourceFiles'],**migration['assets']}.items():retained.checked_path(p,digest)
 for p,digest in pr['originalSourceEnvironmentFiles'].items():retained.checked_path(p,digest)
 for p,digest in pr['generationInputs'].items():retained.checked_path(p,digest)
 original=ROOT/m['originalBinary']
 if sha(original)!=pr['originalBinarySha256']:raise RuntimeError('Original reference executable differs')
 request=rows(ROOT/m['requests']);expected=rows(ROOT/m['expected']);prep=read(m['preparation'])
 if len(request)!=15304 or len(expected)!=15304 or len(prep)!=33 or m['checkingCases']!=15304 or m['preparationCases']!=33 or m['authoredHarnessCases']!=3:raise RuntimeError('Registry cardinality differs')
 if len({r['Name'] for r in request})!=15304 or len({r['name'] for r in expected})!=15304:raise RuntimeError('Duplicate checking identities')
 for q,e in zip(request,expected):
  if set(q)!=set(('Name','Source','SourceSha256','Configuration')) or e['name']!=q['Name'] or e['status']!='observed' or e['sourceSha256']!=q['SourceSha256'] or e['configuration']!=q['Configuration']:raise RuntimeError('Reference identity differs')
  if sha(q['Source'])!=q['SourceSha256'] or e['originalAssertions']!={'unionOrdering':True,'parentPointers':True}:raise RuntimeError('Original input/assertion differs')
 if len({p['Name'] for p in prep})!=33 or collections.Counter(p['stage'] for p in prep)!={'configuration-expansion':7,'option-preparation':26}:raise RuntimeError('Original preparation domain differs')
 for p in prep:
  if sha(p['Source'])!=p['SourceSha256']:raise RuntimeError('Preparation source differs')
 for p in ('tests/Checker.NoEmit.Tests/Checker.NoEmit.Tests.csproj','validation/Checker.NoEmit.Validation/Checker.NoEmit.Validation.csproj','tools/verify-protocols.sh'):retained.checked_path(p)
 source_snapshot(snapshot);retained.manifest_groups();historical();retained.LIBRARY_GUARD.validate_libraries();retained.UPSTREAM_GUARD.validate_upstream();obligations()
 return m

def command(argv,log,environment=None):
 started=time.monotonic()
 with log.open('w') as stream:r=subprocess.run(argv,cwd=ROOT,env=environment,stdout=stream,stderr=subprocess.STDOUT)
 record={'command':argv,'exitCode':r.returncode,'elapsedSeconds':round(time.monotonic()-started,3),'log':log.relative_to(ROOT).as_posix(),'logSha256':sha(log)}
 write(log.with_suffix('.json'),record)
 print(json.dumps(record),flush=True)
 if r.returncode:raise RuntimeError('Stage failed: '+str(log))
 return record

def test_project(consumer):
 target=ROOT/consumer['target'];project=OUT/'tests.csproj'
 tree=ET.parse(ROOT/'tests/Checker.NoEmit.Tests/Checker.NoEmit.Tests.csproj');group=tree.getroot().find('ItemGroup')
 for item in list(group):
  if item.tag=='ProjectReference':group.remove(item)
 for name,path in [('Program.Checks',target),('TypeScript.Scanner',target.parent/'TypeScript.Scanner.dll')]:
  ref=ET.SubElement(group,'Reference',{'Include':name});ET.SubElement(ref,'HintPath').text=str(path)
 ET.SubElement(group,'PackageReference',{'Include':'System.IO.Hashing','Version':'10.0.8'})
 for p in sorted((ROOT/'tests/Checker.NoEmit.Tests').glob('*.cs')):ET.SubElement(group,'Compile',{'Include':str(p)})
 ET.SubElement(tree.getroot().find('PropertyGroup'),'EnableDefaultCompileItems').text='false'
 tree.write(project);return project

def validate_actual(path,m):
 expected={r['name']:r for r in rows(ROOT/m['expected'])};actual=rows(path)
 if len(actual)!=len(expected) or len({r['name'] for r in actual})!=len(actual):raise RuntimeError('Actual complete row set differs')
 for r in actual:
  if expected.get(r['name'])!=r:raise RuntimeError('Actual complete record differs: '+r['name'])
 return {'cases':len(actual),'requestsSha256':m['requestsSha256'],'expectedSha256':m['expectedSha256'],'actual':path.relative_to(ROOT).as_posix(),'actualSha256':sha(path)}

def check_managed(m):
 ns='{http://microsoft.com/schemas/VisualStudio/TeamTest/2010}';path=OUT/'checker-noemit.trx';tree=ET.parse(path)
 counts={k:int(v) for k,v in tree.find('.//'+ns+'Counters').attrib.items()}
 if counts['passed']!=15340 or counts['total']!=15340 or counts['failed']:raise RuntimeError('Whole xUnit failed/skipped/count differs')
 definitions={u.attrib['id']:u.find(ns+'TestMethod').attrib['className'] for u in tree.findall('.//'+ns+'UnitTest')}
 result=tree.findall('.//'+ns+'UnitTestResult');classes=collections.Counter(definitions[r.attrib['testId']] for r in result)
 if dict(classes)!={'Checker.NoEmit.Tests.CompilerNoEmitTests':15304,'Checker.NoEmit.Tests.CompilerPreparationDispositionTests':33,'Checker.NoEmit.Tests.CompilerHarnessTests':3}:raise RuntimeError('Actual xUnit class domain differs')
 for r in result:
  if r.attrib['outcome']!='Passed':raise RuntimeError('Unsuccessful actual row')
  if definitions[r.attrib['testId']]=='Checker.NoEmit.Tests.CompilerNoEmitTests':
   # xUnit truncates long string arguments; complete saved records bind exact identities.
   if not r.attrib['testName'].startswith('Checker.NoEmit.Tests.CompilerNoEmitTests.OriginalNoEmitDiagnosticsQueriesAndInvariants('):raise RuntimeError('Unexpected xUnit method')
 binding=read(OUT/'managed-input-bindings.json');consumer=read(OUT/'consumer/build-report.json')
 if any(binding[k+'Sha256']!=m[k+'Sha256'] for k in ('requests','expected','preparation')) or binding['consumerBuildSha256']!=sha(OUT/'consumer/build-report.json') or binding['snapshotSha256']!=sha(consumer['snapshot']):raise RuntimeError('Managed input/build binding differs')
 test_sources={str(p.relative_to(ROOT)):sha(p) for p in sorted((ROOT/'tests/Checker.NoEmit.Tests').glob('*')) if p.is_file()}
 if binding['testSources']!=test_sources or sha(consumer['target'])!=consumer['targetSha256'] or sha((ROOT/consumer['target']).parent/'TypeScript.Scanner.dll')!=consumer['scannerSha256']:raise RuntimeError('Managed source/executable binding differs')
 for p,digest in consumer['compiledSourceFiles'].items():
  if sha(p)!=digest:raise RuntimeError('Compiled consumer source differs: '+p)
 return {'counts':counts,'classCaseCounts':dict(classes),'trx':path.relative_to(ROOT).as_posix(),'trxSha256':sha(path),'completeRecords':validate_actual(OUT/'managed-actual.jsonl',m),'binding':binding,'displayNameBoundary':'xUnit display arguments truncate; complete persisted records bind every exact original identity.'}

def native(m):
 consumer=read(OUT/'consumer/build-report.json');project=OUT/'consumer/consumer.csproj';target=OUT/'native/Program.Checks'
 args=['dotnet','publish',str(project),'-c','Release','-r','linux-x64','-p:PublishAot=true','-o',str(target.parent)]
 if not __import__('shutil').which('clang') and (ROOT/'tools/.cache/native/bin/cc').is_file():args.append('-p:CppCompilerAndLinker='+str(ROOT/'tools/.cache/native/bin/cc'))
 publish=command(args,OUT/'native-publish.log');before=sha(target)
 checking=rows(ROOT/m['requests']);expected={r['name']:r for r in rows(ROOT/m['expected'])};records=[];allactual=OUT/'native-actual.jsonl'
 with allactual.open('w') as aggregate:
  for offset in range(0,len(checking),128):
   folder=OUT/'native-chunks'/f'{offset//128:04d}';folder.mkdir(parents=True,exist_ok=True)
   batch=checking[offset:offset+128];request=folder/'requests.jsonl';actual=folder/'actual.jsonl'
   request.write_text(''.join(json.dumps(r,separators=(',',':'))+'\n' for r in batch))
   started=time.monotonic();argv=[str(target),str(ROOT),str(request),str(actual)]
   with (folder/'stdout').open('w') as log,(folder/'stderr').open('w') as error:
    r=subprocess.run(argv,cwd=ROOT,stdout=log,stderr=error,timeout=180)
   values=rows(actual)
   record={'command':argv,'exitCode':r.returncode,'cases':len(batch),'elapsedSeconds':round(time.monotonic()-started,3),'requestsSha256':sha(request),'actualSha256':sha(actual),'stdoutSha256':sha(folder/'stdout'),'stderrSha256':sha(folder/'stderr')};records.append(record)
   write(OUT/'native-progress.json',{'completed':offset+len(values),'total':len(checking),'records':records})
   if r.returncode or len(values)!=len(batch) or [v['name'] for v in values]!=[q['Name'] for q in batch] or any(expected.get(v['name'])!=v for v in values):raise RuntimeError('Native record/chunk failed: '+str(folder))
   for v in values:aggregate.write(json.dumps(v,separators=(',',':'))+'\n')
   if offset%1024==0:print('Native exact '+str(offset+len(values))+'/'+str(len(checking)),flush=True)
 if sha(target)!=before:raise RuntimeError('Native executable drift')
 result={'publish':publish,'executable':target.relative_to(ROOT).as_posix(),'executableSha256':before,'chunks':records,'completeRecords':validate_actual(allactual,m)}
 write(OUT/'native-report.json',result);return result

def execution():
 m=read(MANIFEST);managed=check_managed(m);v=read(OUT/'native-report.json')
 if v['executableSha256']!=sha(v['executable']):raise RuntimeError('Native binary drift')
 checking=rows(ROOT/m['requests']);offset=0
 for record in v['chunks']:
  argv=record['command'];request=pathlib.Path(argv[2]);actual=pathlib.Path(argv[3]);folder=actual.parent
  if argv[:2]!=[str(ROOT/v['executable']),str(ROOT)] or record['exitCode'] or record['requestsSha256']!=sha(request) or record['actualSha256']!=sha(actual) or record['stdoutSha256']!=sha(folder/'stdout') or record['stderrSha256']!=sha(folder/'stderr'):raise RuntimeError('Native command/stream proof differs')
  if rows(request)!=checking[offset:offset+record['cases']]:raise RuntimeError('Native chunk selected different rows')
  offset+=record['cases']
 if offset!=len(checking):raise RuntimeError('Native chunk domain differs')
 v['completeRecords']=validate_actual(OUT/'native-actual.jsonl',m)
 return {'wholeCheckerXUnit':managed,'native':v,'retainedExecution':retained.check_execution()}

def run(snapshot):
 m=preflight(snapshot);OUT.mkdir(parents=True,exist_ok=True)
 before={'sources':sources(),'assets':fingerprint(assets()),'obligations':obligations(),'history':historical()};files=assets();started=time.monotonic();status=0
 try:
  command([sys.executable,'tools/build-checker-noemit-consumer.py','--snapshot',str(snapshot),'--output',str(OUT/'consumer')],OUT/'consumer-build.log')
  consumer=read(OUT/'consumer/build-report.json');project=test_project(consumer)
  env=os.environ|{'CHECKER_NOEMIT_ROOT':str(ROOT),'CHECKER_NOEMIT_REQUESTS':str(ROOT/m['requests']),'CHECKER_NOEMIT_REFERENCE':str(ROOT/m['expected']),'CHECKER_NOEMIT_PREPARATION_DISPOSITIONS':str(ROOT/m['preparation']),'CHECKER_NOEMIT_ACTUAL':str(OUT/'managed-actual.jsonl')}
  (OUT/'managed-actual.jsonl').write_text('')
  write(OUT/'managed-input-bindings.json',{'requestsSha256':m['requestsSha256'],'expectedSha256':m['expectedSha256'],'preparationSha256':m['preparationSha256'],'consumerBuildSha256':sha(OUT/'consumer/build-report.json'),'snapshotSha256':sha(snapshot),'testSources':{str(p.relative_to(ROOT)):sha(p) for p in sorted((ROOT/'tests/Checker.NoEmit.Tests').glob('*')) if p.is_file()}})
  command(['/usr/bin/time','-v','dotnet','test',str(project),'-c','Release','--logger','trx;LogFileName=checker-noemit.trx','--results-directory',str(OUT)],OUT/'whole-xunit.log',env)
  check_managed(m);print('Whole managed xUnit15340 PASS',flush=True)
  command(['bash','tools/verify-protocols.sh'],OUT/'retained-protocols.log');retained.check_execution()
  native(m);execution()
 except Exception as error:
  status=1;write(OUT/'failure.json',{'type':type(error).__name__,'error':str(error)});print(str(error),flush=True)
 try:retained.UPSTREAM_GUARD.validate_upstream();upstream=False
 except RuntimeError:upstream=True
 drifts={'sourceDrift':sources()!=before['sources'],'assetDrift':files!=assets() or fingerprint(assets())!=before['assets'],'historicalAssetDrift':any(sha(p)!=h for p,h in before['history'].items()),'upstreamDrift':upstream,'obligationDrift':obligations()!=before['obligations']}
 result={'baseline':BASELINE,'command':[sys.executable,'tools/run-checker-noemit-gate.py','--snapshot',str(snapshot)],'elapsedSeconds':round(time.monotonic()-started,3),'exitCode':status or int(any(drifts.values())),**drifts,'sourceSha256':before['sources'],'verificationAssetsSha256':before['assets'],'verificationAssetFiles':[p.relative_to(ROOT).as_posix() for p in files],'protectedHistoricalAssetFiles':before['history'],'reviewedAssertionObligations':before['obligations'],'snapshot':snapshot.relative_to(ROOT).as_posix(),'snapshotSha256':sha(snapshot)}
 write(OUT/'final-gate.json',result);print(json.dumps({k:v for k,v in result.items() if k not in ('verificationAssetFiles','protectedHistoricalAssetFiles','reviewedAssertionObligations')}));return result['exitCode']
def main():
 p=argparse.ArgumentParser();p.add_argument('--snapshot',type=pathlib.Path,required=True);p.add_argument('--preflight',action='store_true');p.add_argument('--check-execution',action='store_true');a=p.parse_args();snapshot=a.snapshot.resolve()
 if a.preflight:preflight(snapshot);print('Preflight PASS');return 0
 if a.check_execution:preflight(snapshot);execution();print('Execution PASS');return 0
 return run(snapshot)
if __name__=='__main__':raise SystemExit(main())
