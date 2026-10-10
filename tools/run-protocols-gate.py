#!/usr/bin/env python3
"""Verify async/generator/runtime protocols and one unchanged retained type-system cascade."""
import argparse, collections, datetime, gzip, hashlib, importlib.util, json, os
from pathlib import Path
import re, subprocess, sys, time, xml.etree.ElementTree as ET
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'artifacts/protocols-validation'
MANIFEST='validation/protocols-corpus/manifest.json'
BASELINE='bb035b3'
if os.environ.get('DECLARATIONS_CURRENT_CHECKING')!='1':
    raise RuntimeError('Requires the reviewed DECLARATIONS_CURRENT_CHECKING=1 context')
spec=importlib.util.spec_from_file_location('retained_typesystem',ROOT/'tools/run-typesystem-gate.py')
retained=importlib.util.module_from_spec(spec);spec.loader.exec_module(retained)
read,sha,sources,fingerprint=retained.read,retained.sha,retained.sources,retained.fingerprint
GO,SEMANTIC_GO,GO_TOOL=retained.GO,retained.SEMANTIC_GO,retained.GO_TOOL
NATIVE,CLI=OUT/'native/Protocols.Validation',OUT/'cli-native/tscheck'
MANAGED,MANAGED_CLI=OUT/'managed/Protocols.Validation.dll',OUT/'cli-managed/tscheck.dll'
CURRENT_NATIVE_PATHS=[p.relative_to(ROOT).as_posix() for p in (NATIVE,CLI)]
NATIVE_PATHS=[*retained.NATIVE_PATHS,*CURRENT_NATIVE_PATHS]
checked_path=retained.checked_path
LIBRARY_GUARD,UPSTREAM_GUARD=retained.retained.LIBRARY_GUARD,retained.retained.UPSTREAM_GUARD

def write(path,value):
    path.parent.mkdir(parents=True,exist_ok=True);path.write_text(json.dumps(value,indent=2)+'\n')

def stable(value):
    if isinstance(value,dict):return {k:stable(v) for k,v in value.items() if k!='execution'}
    if isinstance(value,list):return [stable(v) for v in value]
    return value

def obligations():
    return {'contractSha256':sha('docs/PROTOCOLS-CONTRACT.md'),
            'testSelection':stable(read('docs/protocols-tests.json')),'retainedTypeSystem':retained.obligations()}

def assets():
    files=set(retained.assets())
    for folder in ('validation/Protocols.Checks','validation/Protocols.Validation','validation/protocols-corpus','tests/Protocols.Tests','examples/protocols'):
        files.update(p for p in (ROOT/folder).rglob('*') if p.is_file() and not {'bin','obj','__pycache__'}.intersection(p.parts))
    files.update(ROOT/p for p in ('docs/PROTOCOLS-CONTRACT.md','docs/PROTOCOLS-VERIFY.md',
       'docs/progress/protocols-core-inventory.json','docs/progress/protocols-support.md'))
    files.update(p for p in (ROOT/'validation/checker-noemit-inventory').rglob('*') if p.is_file())
    files.update(p for p in (ROOT/'tools/compiler-noemit-inventory').rglob('*') if p.is_file())
    files.add(ROOT/'tools/generate-checker-noemit-inventory.py')
    return sorted(files)

def historical():
    result={}
    names=subprocess.check_output(['git','ls-tree','-r','--name-only',BASELINE,'validation'],cwd=ROOT,text=True).splitlines()
    for name in names:
        folder=Path(name).parts[1]
        if folder not in ('corpus','results') and not folder.endswith('-corpus'):continue
        original=subprocess.check_output(['git','show',BASELINE+':'+name],cwd=ROOT)
        if (ROOT/name).read_bytes()!=original:raise RuntimeError('Historical evidence changed: '+name)
        result[name]=sha(name)
    return result

def manifest_groups():
    m=read(MANIFEST);prior=retained.manifest_groups()
    if m['baseline']!=BASELINE or m['upstream']!=read('upstream.lock.json')['revision']:raise RuntimeError('Campaign pin differs')
    for key in ('currentCheckingEnvironment','currentCheckingReplacement','authoredControlRetirement','jsonErrorProjection'):
        if m[key]!=prior[key]:raise RuntimeError('Unreviewed retained context: '+key)
    retained.retained.validate_project_paths()
    for p in ('tests/Protocols.Tests/Protocols.Tests.csproj','validation/Protocols.Validation/Protocols.Validation.csproj','validation/Protocols.Checks/Protocols.Checks.csproj'):checked_path(p)
    names={};groups=m['groups']
    if not groups or len({g['name'] for g in groups})!=len(groups):raise RuntimeError('Duplicate/empty groups')
    for g in groups:
        if not re.fullmatch('[a-zA-Z0-9_-]+',g['name']) or type(g['cases'])!=int or g['cases']<=0:raise RuntimeError('Invalid corpus group')
        for key in ('requests','expected','provenance'):
            if Path(g[key]).parts[:2]!=('validation','protocols-corpus'):raise RuntimeError('Noncanonical corpus path')
            checked_path(g[key],g[key+'Sha256'])
        pr=read(g['provenance'])
        if pr['baseline']!=BASELINE or pr['upstream']!=m['upstream'] or pr['requestsSha256']!=g['requestsSha256'] or pr.get('cases',pr.get('requests'))!=g['cases']:raise RuntimeError('Corpus provenance differs')
        if not pr['sourceFiles']:raise RuntimeError('Missing source provenance')
        for path,digest in pr['sourceFiles'].items():checked_path(path,digest)
        rows=(ROOT/g['requests']).read_bytes().splitlines(keepends=True)
        raw=(ROOT/g['expected']).read_bytes();raw=gzip.decompress(raw) if g['expected'].endswith('.gz') else raw
        expected=[json.loads(x) for x in raw.splitlines()]
        if len(rows)!=g['cases'] or len(expected)!=len(rows) or hashlib.sha256(raw).hexdigest()!=g['originalOutputSha256']:raise RuntimeError('Corpus cardinality/output provenance differs')
        for r,e in zip(rows,expected):
            item=json.loads(r);name=item['Name']
            if name in names or e['name']!=name:raise RuntimeError('Corpus identity differs')
            names[name]=r
        if 'domains' in pr and set(pr['domains'])!={json.loads(r)['Name'] for r in rows}:raise RuntimeError('Family coverage provenance differs')
        if 'inputs' in pr:
            variants=[]
            for item in pr['inputs']:
                checked_path(item['source'],item['sourceSha256'])
                for path,digest in item.get('baselineSourceFiles',{}).items():checked_path(path,digest)
                if item['disposition']=='reused-compiler-input-semantic-differential':variants.extend(item['variants'])
            if len(variants)!=len(rows) or any(v['name']!=json.loads(r)['Name'] or v['requestSha256']!=hashlib.sha256(r).hexdigest() for v,r in zip(variants,rows)):raise RuntimeError('Compiler variants differ')
    if len(names)!=m['cases']:raise RuntimeError('Complete corpus count differs')
    original={(g['package'],f) for g in m['originalTests'] for f in g['functions']}
    required={(r['package'],r['function']) for r in read('docs/protocols-tests.json')['directOriginalFunctions']}
    if original!=required or len(original)!=7 or sum(len(g['functions']) for g in m['originalTests'])!=7:raise RuntimeError('Original assertion registry differs')
    for g in m['originalTests']:
        source=''
        for path,digest in g['sourceFiles'].items():source+=checked_path(path,digest).read_text()
        for name in g['functions']:
            if not re.search(r'^func '+re.escape(name)+r'\(',source,re.M):raise RuntimeError('Missing original body')
    relation=m['publicRelationsObservation']
    if relation!={'provenance':'validation/typesystem-corpus/public-relations-provenance.json','provenanceSha256':'f6f8ec93e3a615d99391dff85da517457511965b9217636b65e777ff0bf6a5c9'}:raise RuntimeError('Unexpected public relation extension path/hash')
    checked_path(relation['provenance'],relation['provenanceSha256'])
    extension=read(relation['provenance'])
    for path,digest in extension['sourceFiles'].items():checked_path(path,digest)
    if extension['classification']!='authored-additive-public-relations-observer-extension':raise RuntimeError('Unreviewed relation observation')
    casing=read('validation/TypeSystem.Checks/js-casing-test-provenance.json')
    checked_path(casing['source'],casing['sourceSha256']);checked_path(casing['generated'],casing['generatedSha256'])
    if casing['originalFunctions']!=['TestJSCasing'] or casing['originalTableAssertions']!=26:raise RuntimeError('Casing original domain differs')
    shared=m['sharedChecks'];flat=[n for g in shared['groups'] for n in g['names']]
    if shared['count']!=8 or len(flat)!=8 or set(flat)!={f for p,f in original}|{'Authored.ExternalSemanticConsumers'}:raise RuntimeError('Shared assertion inventory differs')
    if any(len(g['classNames'])!=1 for g in shared['groups']):raise RuntimeError('Shared class domain differs')
    if shared['xUnitClassCounts']!={g['classNames'][0]:len(g['names']) for g in shared['groups']} or any(g['count']!=len(g['names']) for g in shared['groups']):raise RuntimeError('Shared xUnit registry differs')
    order=m['orderControls'];checked_path(order['requests'],order['requestsSha256']);orderrows=(ROOT/order['requests']).read_bytes().splitlines(keepends=True)
    if len(orderrows)!=order['cases'] or len(set(orderrows))!=len(orderrows) or any(names.get(json.loads(r)['Name'])!=r for r in orderrows) or order['sequencesPerRuntime']!=2 or order['executionsPerRuntime']!=2*(len(orderrows)+1):raise RuntimeError('Order requests differ')
    cli=m['cliControls']
    if cli['runner']!='tools/check-protocols-cli.py' or cli['semanticObserver']!='tools/resolution-cli-oracle/main.go':raise RuntimeError('Unexpected CLI producer')
    for key in ('requests','provenance','runner','semanticObserver'):checked_path(cli[key],cli[key+'Sha256'])
    shipped={p.relative_to(ROOT).as_posix():sha(p) for p in (ROOT/'examples/protocols').rglob('*') if p.is_file() and p.suffix in ('.ts','.json')}
    if cli['sourceFiles']!=shipped or cli['expectedExits']!={'job-stream-valid':0,'job-stream-invalid':1} or cli['cases']!=2:raise RuntimeError('Disk CLI fixtures differ')
    pr=read(cli['provenance'])
    if pr['sourceFiles']!=shipped or pr['requestsSha256']!=cli['requestsSha256']:raise RuntimeError('CLI provenance differs')
    restoration=m['mapperGeneratorRestoration'];checked_path(restoration['provenance'],restoration['provenanceSha256'])
    rp=read(restoration['provenance'])
    if rp['classification']!='original-generator-full-assertion-restoration' or rp['originalFunctions']!=['TestContentMapperDirectivesPreserveIncrementalGlobals']:raise RuntimeError('Mapper full original restoration differs')
    for path,digest in rp['sourceFiles'].items():checked_path(path,digest)
    inventory=m['wholeCheckerInventory'];checked_path(inventory['provenance'],inventory['provenanceSha256'])
    ip=read(inventory['provenance']);checked_path(ip['inventory'],ip['inventorySha256'])
    if ip['baseline']!=BASELINE or ip['upstream']!=m['upstream'] or ip['classification']!='complete-original-compiler-harness-inventory-not-runtime-coverage':raise RuntimeError('Whole checker inventory differs')
    for path,digest in ip['sourceFiles'].items():checked_path(path,digest)
    raw=gzip.decompress((ROOT/ip['inventory']).read_bytes());rows=[json.loads(r) for r in raw.splitlines()]
    if hashlib.sha256(raw).hexdigest()!=ip['uncompressedSha256'] or len(rows)!=ip['inputs']:raise RuntimeError('Whole checker inventory bytes/count differ')
    obligations();return m

def snapshot(paths):return {p.relative_to(ROOT).as_posix():{'sha256':sha(p),'sizeBytes':p.stat().st_size} for p in paths}
def managed_snapshot():return snapshot(sorted(p for folder in (MANAGED.parent,MANAGED_CLI.parent) for p in folder.iterdir() if p.is_file()))
def current_native_snapshot():return snapshot((NATIVE,CLI))
def native_snapshot():return {**retained.native_snapshot(),**current_native_snapshot()}
def oracle_snapshot():return retained.oracle_snapshot()

def compare_group(g,executable,stage,managed=False):
    subprocess.run([sys.executable,'tools/compare-programs.py','--requests',g['requests'],'--expected',g['expected'],'--executable',str(executable),'--output',str(OUT/stage/g['name'])]+(['--managed'] if managed else []),cwd=ROOT,check=True)

def reproduce_original():
    m=manifest_groups();write(OUT/'oracle-before.json',oracle_snapshot());direct=[]
    result=subprocess.run([str(GO),'--json-error-controls'],cwd=ROOT,capture_output=True)
    (OUT/'json-error-controls.stdout').write_bytes(result.stdout);(OUT/'json-error-controls.stderr').write_bytes(result.stderr)
    if result.returncode or json.loads(result.stdout)!=read(m['jsonErrorProjection']['expected']):raise RuntimeError('Typed source error controls failed')
    write(OUT/'json-error-controls-report.json',{'command':[str(GO),'--json-error-controls'],'exitCode':result.returncode,'executableSha256':sha(GO),'stdoutSha256':sha(OUT/'json-error-controls.stdout'),'stderrSha256':sha(OUT/'json-error-controls.stderr')})
    for g in m['groups']:compare_group(g,GO,'original-go')
    for i,g in enumerate(m['originalTests']):
        command=[str(GO_TOOL),'test','-json',g['package'],'-run','^('+'|'.join(g['functions'])+')$','-count=1'];r=subprocess.run(command,cwd=ROOT/'upstream/TypeScript/tsc',capture_output=True)
        path=OUT/f'original-tests-{i}.jsonl';path.write_bytes(r.stdout);path.with_suffix('.stderr').write_bytes(r.stderr)
        events=[json.loads(line) for line in r.stdout.splitlines()];passed=[e['Test'] for e in events if e['Action']=='pass' and 'Test' in e and '/' not in e['Test']]
        if r.returncode or sorted(passed)!=sorted(g['functions']) or any(e['Action'] in ('skip','fail') for e in events):raise RuntimeError('Original assertions failed/skipped')
        direct.append({'command':command,'exitCode':r.returncode,'functions':passed,'sourceFiles':g['sourceFiles'],'log':path.relative_to(ROOT).as_posix(),'logSha256':sha(path),'stderrSha256':sha(path.with_suffix('.stderr'))})
    write(OUT/'original-tests.json',direct)

def execute_stage(managed=False):
    m=manifest_groups();prefix='managed' if managed else 'native';exe,cli=(MANAGED,MANAGED_CLI) if managed else (NATIVE,CLI);command=(['dotnet'] if managed else [])+[str(exe)]
    for g in m['groups']:compare_group(g,exe,prefix+'-corpus',managed)
    registry=subprocess.run(command+['--registry'],cwd=ROOT,capture_output=True,check=True);write(OUT/(prefix+'-registry.json'),json.loads(registry.stdout))
    if json.loads(registry.stdout)!=m['sharedChecks']:raise RuntimeError('Actual registry differs')
    controls=subprocess.run(command+['--controls'],cwd=ROOT,capture_output=True);(OUT/(prefix+'-controls.stdout')).write_bytes(controls.stdout);(OUT/(prefix+'-controls.stderr')).write_bytes(controls.stderr)
    write(OUT/(prefix+'-controls-report.json'),{'command':command+['--controls'],'exitCode':controls.returncode,'executableSha256':sha(exe),'count':m['sharedChecks']['count'],'stdoutSha256':sha(OUT/(prefix+'-controls.stdout')),'stderrSha256':sha(OUT/(prefix+'-controls.stderr'))})
    if controls.returncode or controls.stdout.strip()!=str(m['sharedChecks']['count']).encode():raise RuntimeError('Shared controls failed')
    subprocess.run([sys.executable,'tools/check-program-order.py','--manifest',MANIFEST,'--go',str(GO),'--target',str(exe),'--output',str(OUT/(prefix+'-order'))]+(['--managed'] if managed else []),cwd=ROOT,check=True)
    block=m['cliControls'];subprocess.run([sys.executable,block['runner'],'--manifest',MANIFEST,'--original',str(SEMANTIC_GO),'--executable',str(cli),'--output',str(OUT/(prefix+'-cli'))]+(['--managed'] if managed else []),cwd=ROOT,check=True)

def comparison_reports(m,stage,exe,managed=False):
    reports={}
    for g in m['groups']:
        path=OUT/stage/g['name']/'report.json';v=read(path);command=(['dotnet'] if managed else [])+[str(exe)]
        if v['command']!=command or v['total']!=g['cases'] or v['exact']!=g['cases'] or v['failures'] or v['exitCode'] or v['executableDrift'] or v['executableSha256']!=sha(exe) or any(v[k]!=g[k] for k in ('requestsSha256','expectedSha256')):raise RuntimeError('Invalid comparison proof')
        raw=(ROOT/g['expected']).read_bytes();raw=gzip.decompress(raw) if g['expected'].endswith('.gz') else raw
        actual=(path.parent/'stdout.jsonl').read_bytes().splitlines();expected=raw.splitlines()
        if len(actual)!=len(expected) or any(retained.retained.canonical_json(json.loads(a))!=retained.retained.canonical_json(json.loads(e)) for a,e in zip(actual,expected)):raise RuntimeError('Saved full response differs')
        reports[g['name']]={'report':path.relative_to(ROOT).as_posix(),'reportSha256':sha(path),'stdoutSha256':sha(path.parent/'stdout.jsonl'),'stderrSha256':sha(path.parent/'stderr.txt'),**v}
    return reports

def validate_order_report(m,order,exe,managed=False):
    command=(['dotnet'] if managed else [])+[str(exe)]
    expected_executions=m['orderControls']['executionsPerRuntime']
    if order['failures'] or any(order[k]!=m['orderControls'][k] for k in ('cases','sequencesPerRuntime')) or order['originalGoExact']!=expected_executions or order['targetExact']!=expected_executions or order['requestsSha256']!=m['orderControls']['requestsSha256']:raise RuntimeError('Order proof differs')
    if order['commands']!={'originalGo':[str(GO)],'target':command} or any(r['exitCode'] for r in order['records']):raise RuntimeError('Order commands failed')
    for label,path in (('originalGo',GO),('target',exe)):
        if order['binarySha256Before'][label]!=sha(path) or order['binarySha256After'][label]!=sha(path):raise RuntimeError('Order executable drift')

def check_stage(m,managed=False):
    prefix='managed' if managed else 'native';exe,cli=(MANAGED,MANAGED_CLI) if managed else (NATIVE,CLI);command=(['dotnet'] if managed else [])+[str(exe)]
    corpus=comparison_reports(m,prefix+'-corpus',exe,managed);controls=read(OUT/(prefix+'-controls-report.json'))
    if read(OUT/(prefix+'-registry.json'))!=m['sharedChecks'] or controls['command']!=command+['--controls'] or controls['executableSha256']!=sha(exe) or controls['exitCode'] or controls['count']!=m['sharedChecks']['count']:raise RuntimeError('Shared proof differs')
    for stream in ('stdout','stderr'):
        if controls[stream+'Sha256']!=sha(OUT/(prefix+'-controls.'+stream)):raise RuntimeError('Shared streams changed')
    if (OUT/(prefix+'-controls.stdout')).read_text().strip()!=str(m['sharedChecks']['count']):raise RuntimeError('Incomplete controls')
    order=read(OUT/(prefix+'-order/report.json'))
    validate_order_report(m,order,exe,managed)
    cli_report=read(OUT/(prefix+'-cli/report.json'));block=m['cliControls'];hashes={'original':sha(SEMANTIC_GO),'target':sha(cli)}
    if cli_report['originalCommand']!=[str(SEMANTIC_GO)]:raise RuntimeError('Original CLI command differs')
    for field,suffix in (('originalStdout','.stdout'),('originalStderr','.stderr')):
        if cli_report[field+'Sha256']!=sha(OUT/(prefix+'-cli')/('original'+suffix)):raise RuntimeError('Original CLI stream changed')
    if cli_report['cases']!=2 or cli_report['exact']!=2 or cli_report['failures'] or cli_report['managed'] is not managed or cli_report['executableDrift'] or cli_report['executableHashesBefore']!=hashes or cli_report['executableHashesAfter']!=hashes or cli_report['manifestSha256']!=sha(MANIFEST) or cli_report['requestsSha256']!=block['requestsSha256'] or cli_report['runnerSha256']!=block['runnerSha256'] or cli_report['sourceFiles']!=block['sourceFiles'] or cli_report['originalExitCode']:raise RuntimeError('CLI proof differs')
    rows=[json.loads(r) for r in (ROOT/block['requests']).read_bytes().splitlines()]
    if len(cli_report['records'])!=len(rows):raise RuntimeError('CLI record inventory differs')
    for r,q in zip(cli_report['records'],rows):
        if r['name']!=q['Name'] or not r['passed'] or r['difference'] is not None or r['exitCode']!=block['expectedExits'][q['Name']] or r['command']!=(['dotnet'] if managed else [])+[str(cli),'--config',q['ConfigPath'],'--json']:raise RuntimeError('CLI command/result differs')
        for stream in ('stdout','stderr'):
            if r[stream+'Sha256']!=sha(OUT/(prefix+'-cli')/(r['name']+'.'+stream)):raise RuntimeError('CLI payload changed')
    return {'corpus':corpus,'sharedChecks':controls,'order':order,'cli':cli_report}

def check_managed():
    m=manifest_groups();original=comparison_reports(m,'original-go',GO);direct=read(OUT/'original-tests.json')
    if len(direct)!=len(m['originalTests']):raise RuntimeError('Original package inventory differs')
    for row,g in zip(direct,m['originalTests']):
        if row['exitCode'] or sorted(row['functions'])!=sorted(g['functions']) or row['sourceFiles']!=g['sourceFiles'] or sha(row['log'])!=row['logSha256']:raise RuntimeError('Original proof differs')
        events=[json.loads(r) for r in checked_path(row['log']).read_bytes().splitlines()]
        if any(e['Action'] in ('fail','skip') for e in events):raise RuntimeError('Original fail/skip event')
    modal=read(OUT/'json-error-controls-report.json')
    if modal['executableSha256']!=sha(GO) or modal['exitCode'] or modal['command']!=[str(GO),'--json-error-controls'] or json.loads((OUT/'json-error-controls.stdout').read_bytes())!=read(m['jsonErrorProjection']['expected']):raise RuntimeError('Typed error controls differ')
    for stream in ('stdout','stderr'):
        if modal[stream+'Sha256']!=sha(OUT/('json-error-controls.'+stream)):raise RuntimeError('Typed error raw stream changed')
    tests=retained.retained.check_trx('artifacts/protocols-validation/protocols.trx');tree=ET.parse(ROOT/tests['trx']);ns='{http://microsoft.com/schemas/VisualStudio/TeamTest/2010}';classes=collections.Counter(row.find(ns+'TestMethod').attrib['className'] for row in tree.findall('.//'+ns+'UnitTest'))
    wanted={**m['sharedChecks']['xUnitClassCounts'],m['corpusXUnitClass']:m['cases']}
    if dict(classes)!=wanted or tests['counts']['total']!=m['cases']+m['sharedChecks']['count']:raise RuntimeError('Actual xUnit class/count differs')
    tests['classCaseCounts']=dict(classes)
    if read(OUT/'managed-before.json')!=managed_snapshot() or read(OUT/'oracle-before.json')!=oracle_snapshot():raise RuntimeError('Managed/oracle binary drift')
    return {'original':{'corpus':original,'directTests':direct,'jsonErrorProjection':modal},'managed':check_stage(m,True),'protocolsXUnit':tests}

def check_execution():
    m=manifest_groups();current=check_managed();native=check_stage(m);old=retained.check_execution()
    if read(OUT/'native-before.json')!=current_native_snapshot():raise RuntimeError('Native executable drift')
    return {**current,'native':native,'retainedExecution':old,'managedBinaries':managed_snapshot(),'originalOracleBinaries':oracle_snapshot()}

def run_gate():
    manifest_groups();LIBRARY_GUARD.validate_libraries();UPSTREAM_GUARD.validate_upstream();OUT.mkdir(parents=True,exist_ok=True)
    before_source=sources();files=assets();before_assets=fingerprint(files);history=historical();before_obligations=obligations();command=['bash','tools/verify-protocols.sh'];start=time.monotonic()
    print('Protocol gate log: '+str(OUT/'final-gate.log'),flush=True)
    with (OUT/'final-gate.log').open('w') as log:r=subprocess.run(command,cwd=ROOT,stdout=log,stderr=subprocess.STDOUT)
    try:UPSTREAM_GUARD.validate_upstream();upstream_drift=False
    except RuntimeError:upstream_drift=True
    drifts={'sourceDrift':sources()!=before_source,'assetDrift':files!=assets() or fingerprint(assets())!=before_assets,'historicalAssetDrift':any(not (ROOT/p).is_file() or sha(p)!=s for p,s in history.items()),'upstreamDrift':upstream_drift,'obligationDrift':obligations()!=before_obligations}
    report={'command':command,'baseline':BASELINE,'elapsedSeconds':round(time.monotonic()-start,3),'exitCode':r.returncode or int(any(drifts.values())),**drifts,'sourceSha256':before_source,'verificationAssetsSha256':before_assets,'verificationAssetFiles':[p.relative_to(ROOT).as_posix() for p in files],'protectedHistoricalAssetFiles':history,'reviewedAssertionObligations':before_obligations,'log':'artifacts/protocols-validation/final-gate.log'}
    write(OUT/'final-gate.json',report);print(json.dumps({k:v for k,v in report.items() if k not in ('verificationAssetFiles','protectedHistoricalAssetFiles','reviewedAssertionObligations')}),flush=True);raise SystemExit(report['exitCode'])

def main():
    p=argparse.ArgumentParser(description=__doc__);g=p.add_mutually_exclusive_group()
    for flag in ('preflight','reproduce-original','capture-managed','execute-managed','check-managed','capture-native','execute-native','check-execution'):g.add_argument('--'+flag,action='store_true')
    a=p.parse_args();OUT.mkdir(parents=True,exist_ok=True)
    if a.preflight:manifest_groups();historical();LIBRARY_GUARD.validate_libraries();UPSTREAM_GUARD.validate_upstream()
    elif a.reproduce_original:reproduce_original()
    elif a.capture_managed:write(OUT/'managed-before.json',managed_snapshot())
    elif a.execute_managed:execute_stage(True)
    elif a.check_managed:check_managed()
    elif a.capture_native:write(OUT/'native-before.json',current_native_snapshot())
    elif a.execute_native:execute_stage()
    elif a.check_execution:check_execution()
    else:run_gate()
if __name__=='__main__':main()
