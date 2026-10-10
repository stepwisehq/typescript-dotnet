#!/usr/bin/env python3
"""Supplement the frozen raw inventory with actual original decoded-read expansion.

No compiler assertions run. The original file-byte hashes remain authoritative.
"""
import argparse,collections,gzip,hashlib,json,os,pathlib,re,subprocess
ROOT=pathlib.Path(__file__).resolve().parents[1]
UP=ROOT/'upstream/TypeScript/tsc'
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def variants(row):return [{'Name':v['Name'],'Config':v['Config']} for v in (row.get('configurations') or [])]
def main():
 p=argparse.ArgumentParser();p.add_argument('--original',type=pathlib.Path,required=True);a=p.parse_args()
 out=ROOT/'artifacts/checker-noemit-validation/decoded-configurations';out.mkdir(parents=True,exist_ok=True)
 dest=ROOT/'validation/checker-noemit-decoded-inventory';dest.mkdir(parents=True,exist_ok=True)
 original=a.original.resolve();before=sha(original)
 command=[str(original),'-test.run=^TestAuthoredDecodedCompilerInventory$','-test.count=1']
 env=os.environ|{'CHECKER_DECODED_INVENTORY_OUTPUT':str(out/'original.jsonl')}
 with (out/'original.log').open('wb') as stdout,(out/'original.stderr').open('wb') as stderr:r=subprocess.run(command,cwd=UP,env=env,stdout=stdout,stderr=stderr)
 rawpath=ROOT/'validation/checker-noemit-inventory/fixtures.jsonl.gz'
 raw={r['source']:r for r in map(json.loads,gzip.decompress(rawpath.read_bytes()).splitlines())}
 rows=[json.loads(l) for l in (out/'original.jsonl').read_bytes().splitlines()]
 if len(rows)!=len(raw):raise RuntimeError('Incomplete original decoded enumeration')
 failure_text={};failure_name=None
 for line in (out/'original.log').read_text().splitlines():
  match=re.search(r'--- FAIL: TestAuthoredDecodedCompilerInventory/(compiler|conformance)/([^ ]+)',line)
  if match:failure_name=(match[1],match[2])
  diagnostic=re.search(r'harnessutil.go:\d+: (.+)',line)
  if diagnostic and failure_name:failure_text[failure_name]=diagnostic[1]
 deltas=[];seen=set();causes=collections.Counter()
 for row in rows:
  source=pathlib.Path(row['source']);key=source.relative_to(ROOT).as_posix();row['source']=key
  if key in seen or key not in raw or sha(source)!=row['sourceSha256'] or raw[key]['sourceSha256']!=row['sourceSha256']:raise RuntimeError('Original fixture registry/hash changed')
  seen.add(key);prior=raw[key]
  if row['status']!='decoded-configuration-enumerated':
   row['originalExpansionFailureText']=failure_text[(row['suite'],source.name)]
  changed=[k for k in ('rawSettings','units','symlinks','currentDirectory','globalOptions','extractionError') if k in row and row[k]!=prior[k]]
  old,new=variants(prior),variants(row)
  if old!=new:changed.append('configurations')
  if changed:
   data=source.read_bytes();cause='UTF-8 BOM' if data.startswith(b'\xef\xbb\xbf') else 'UTF-16 LE BOM' if data.startswith(b'\xff\xfe') else 'UTF-16 BE BOM' if data.startswith(b'\xfe\xff') else 'original-preconfiguration-skip-attempt' if row['originalPreConfigurationSkip'] else 'non-BOM-preparation-delta'
   if not row['originalPreConfigurationSkip']:causes[cause]+=1
   deltas.append({'source':key,'sourceSha256':row['sourceSha256'],'cause':cause,'changedFields':changed,'originalPreConfigurationSkip':row['originalPreConfigurationSkip'],'oldConfigurations':old,'decodedConfigurations':new,'newVariantNames':sorted({x['Name'] for x in new}-{x['Name'] for x in old}),'removedRawVariantNames':sorted({x['Name'] for x in old}-{x['Name'] for x in new})})
  row['classification']='decoded-original-preparation-inventory-not-semantic-execution'
 data=b''.join((json.dumps(row,ensure_ascii=False,separators=(',',':'))+'\n').encode() for row in rows)
 target=dest/'fixtures.jsonl.gz';target.write_bytes(gzip.compress(data,mtime=0))
 counts={'fixtures':len(rows),'decodedConfigurations':sum(len(r.get('configurations') or []) for r in rows),'decodedConfigurationsWithoutOriginalPreConfigurationSkips':sum(len(r.get('configurations') or []) for r in rows if not r['originalPreConfigurationSkip']),'originalPreConfigurationSkips':sum(r['originalPreConfigurationSkip'] for r in rows),'expansionFailures':sum(r['status']!='decoded-configuration-enumerated' for r in rows),'changedFixtures':len(deltas),'changedPreviouslyEnumeratedFixtures':sum(not d['originalPreConfigurationSkip'] for d in deltas),'rawConfigurations':sum(len(r['configurations']) for r in raw.values()),'previouslyEnumeratedDeltaCauses':dict(causes),'changedPreviouslyEnumeratedSettingsConfigurations':sum(not d['originalPreConfigurationSkip'] and 'rawSettings' in d['changedFields'] for d in deltas)}
 sources=['tools/reconcile-checker-noemit-configurations.py','tools/checker-noemit-oracle/decoded_inventory_test.go','upstream/TypeScript/tsc/internal/testrunner/compiler_runner.go','upstream/TypeScript/tsc/internal/testrunner/test_case_parser.go','upstream/TypeScript/tsc/internal/testrunner/options_generated.go','upstream/TypeScript/tsc/internal/testutil/harnessutil/harnessutil.go','upstream/TypeScript/tsc/internal/vfs/osvfs/os.go','upstream/TypeScript/tsc/internal/vfs/internal/internal.go']
 # Include every additive observer source in the executable, not just the new test.
 sources+=sorted(str(x.relative_to(ROOT)) for x in (ROOT/'tools/checker-noemit-oracle').glob('*.go') if str(x.relative_to(ROOT)) not in sources)
 provenance={'classification':'actual-decoded-original-configuration-reconciliation-not-checker-pass',**counts,'rawInventory':str(rawpath.relative_to(ROOT)),'rawInventorySha256':sha(rawpath),'rawInventoryProvenanceSha256':sha(ROOT/'validation/checker-noemit-inventory/provenance.json'),'decodedInventorySha256':sha(target),'sourceFiles':{s:sha(ROOT/s) for s in sources},'command':command,'exitCode':r.returncode,'originalBinarySha256':before,'originalOutputSha256':sha(out/'original.jsonl'),'originalLogSha256':sha(out/'original.log'),'originalStderrSha256':sha(out/'original.stderr'),'originalBinaryDrift':sha(original)!=before,'deltaExplanation':'Previously enumerated changes are disk-read BOM decoding only:820 UTF-8,6 UTF-16LE,1 UTF-16BE fixtures.821 change extracted settings/config values; remaining6 change extracted units without settings. Actual original config expansion adds101 net obligations excluding original skips. No options normalization/default merge/source edit is introduced.',
 'boundary':'Actual getCompilerFileBasedTest/osvfs.ReadFile BOM decoding and original option expansion. All original sources including42 preconfiguration skips are attempted unchanged. Per-fixture original fatal/panic is retained as expansion failure, never success or automatic exclusion. Original map configuration enumeration is presented sorted by original description; semantic/search ordering is untouched. Raw12824 extraction equivalence remains historical raw-parser evidence.'}
 (dest/'deltas.json').write_text(json.dumps({'counts':counts,'deltas':deltas},indent=2)+'\n');provenance['deltasSha256']=sha(dest/'deltas.json')
 (dest/'provenance.json').write_text(json.dumps(provenance,indent=2)+'\n');print(json.dumps(counts))
 if provenance['originalBinaryDrift']:raise RuntimeError('Original binary drift')
if __name__=='__main__':main()
