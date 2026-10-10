#!/usr/bin/env python3
"""Record only a complete frozen whole-checker gate; never overwrite prior proof."""
import argparse,datetime,importlib.util,json,pathlib,subprocess
ROOT=pathlib.Path(__file__).resolve().parents[1]
spec=importlib.util.spec_from_file_location('whole_gate',ROOT/'tools/run-checker-noemit-gate.py');g=importlib.util.module_from_spec(spec);spec.loader.exec_module(g)
p=argparse.ArgumentParser();p.add_argument('--snapshot',type=pathlib.Path,required=True);a=p.parse_args();m=g.preflight(a.snapshot.resolve());gate=g.read(g.OUT/'final-gate.json')
drift=('sourceDrift','assetDrift','historicalAssetDrift','upstreamDrift','obligationDrift')
if gate['exitCode'] or any(gate[k] for k in drift):raise RuntimeError('Failed/drifted gate')
if gate['snapshot']!=a.snapshot.resolve().relative_to(ROOT).as_posix() or gate['snapshotSha256']!=g.sha(a.snapshot.resolve()):raise RuntimeError('Recorder snapshot differs from actual gate')
if gate['sourceSha256']!=g.sources() or gate['verificationAssetsSha256']!=g.fingerprint(g.assets()) or gate['reviewedAssertionObligations']!=g.obligations() or gate['protectedHistoricalAssetFiles']!=g.historical():raise RuntimeError('Frozen evidence differs')
execution=g.execution();binaries=g.retained.native_snapshot();native=execution['native'];binaries[native['executable']]={'sha256':native['executableSha256'],'sizeBytes':(ROOT/native['executable']).stat().st_size}
for path,row in binaries.items():
 row['file']=subprocess.check_output(['file',str(ROOT/path)],text=True).strip()
 if 'ELF 64-bit' not in row['file'] or 'x86-64' not in row['file']:raise RuntimeError('Actual Linux x64 ELF required')
 result=subprocess.run(['ldd',str(ROOT/path)],stdout=subprocess.PIPE,stderr=subprocess.STDOUT,text=True);row['dependencies']=result.stdout.strip()
 if 'libcoreclr' in row['dependencies'].lower():raise RuntimeError('Managed runtime dependency')
selected=[execution['wholeCheckerXUnit']];current=execution['retainedExecution']
# Match the predecessor record's explicit selected aggregate, not every duplicate TRX.
for key in ('protocolsXUnit','typesystemXUnit','expressionsXUnit','declarationsXUnit','programXUnit','resolutionXUnit','configurationXUnit'):
 selected.append(current[key]);current=current['retainedExecution']
selected.extend(current['xUnit'].values())
proof={**gate,'stage':'whole-original-checker-noemit-language','recordedAtUTC':datetime.datetime.now(datetime.timezone.utc).isoformat(),'upstreamRevision':m['upstream'],'checkingCases':15304,'preparationDispositionCases':33,'authoredHarnessCases':3,'wholeCheckerXUnit':execution['wholeCheckerXUnit'],'wholeCheckerNative':native,'retainedProtocolsExecution':execution['retainedExecution'],'nativeBinaries':binaries,'xUnitCases':sum(v['counts']['total'] for v in selected),'selectedXUnitRecords':[{'trx':v['trx'],'cases':v['counts']['total'],'sha256':v['trxSha256']} for v in selected],'manifest':g.MANIFEST,'manifestSha256':g.sha(g.MANIFEST),'catalog':'docs/checker-noemit-tests.json','limits':['Aggregate selected xUnit cases are executed cases, not distinct original tests; predecessor duplicates remain visible.','Whole original compiler checking/full public type-symbol/trace assertions are qualified under unchanged options/bytes, with explicit no-emit and harness scheduling/environment adaptations. Not complete historical emit-output/compiler-test passes.','Original APILibCheck internal built declaration roles remain unavailable; its fresh missing-import/noemit record and runtime invariants are compared, not a historical built-environment success.','Original preparation failures and original preconfiguration policy are dispositions, never semantic passes.','No cross-runtime private-state census or performance acceptance.']}
path=ROOT/'validation/results/checker-noemit-verified.json'
if path.exists():raise RuntimeError('Preserve existing proof')
path.write_text(json.dumps(proof,indent=2)+'\n');print(json.dumps({'evidence':str(path.relative_to(ROOT)),'xUnitCases':proof['xUnitCases'],'elapsedSeconds':proof['elapsedSeconds']}))
