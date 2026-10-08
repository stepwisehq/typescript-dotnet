#!/usr/bin/env python3
"""Record successful exact-source native foundation and retained regression evidence."""
import collections,hashlib,json,pathlib,subprocess,sys,xml.etree.ElementTree as ET
ROOT=pathlib.Path(__file__).resolve().parent.parent
OUT=ROOT/'artifacts/foundation-validation'
def read(path):return json.loads((ROOT/path).read_text())
def sha(path):return hashlib.sha256(path.read_bytes()).hexdigest()
run=read('artifacts/foundation-validation/final-gate.json')
if run['exitCode'] or run['sourceDrift'] or run['assetDrift']:raise RuntimeError('Final foundation gate failed or drifted')
if subprocess.check_output([sys.executable,'tools/foundation-source-hash.py'],cwd=ROOT,text=True).strip()!=run['sourceSha256']:raise RuntimeError('Sources changed after successful gate')
digest=hashlib.sha256()
for name in run['verificationAssetFiles']:
 p=ROOT/name;digest.update(name.encode()+b'\0'+p.read_bytes()+b'\0')
if digest.hexdigest()!=run['verificationAssetsSha256']:raise RuntimeError('Assets changed after successful gate')
manifest=read('validation/foundation-corpus/source-manifest.json');catalog=read('docs/foundation-test-catalog.json')
parser=read('artifacts/foundation-validation/broad-parser-native/report.json');scanner=read('artifacts/foundation-validation/broad-scanner-native/report.json')
for report in (parser,scanner):
 if report['limited'] or report['failures'] or report['counts']['requests']!=manifest['requests'] or report['counts']['exact']!=manifest['requests']:raise RuntimeError('Incomplete broad evidence')
def trx(name):
 p=ROOT/name;tree=ET.parse(p);ns='{http://microsoft.com/schemas/VisualStudio/TeamTest/2010}'
 row=tree.find('.//'+ns+'Counters');counts={k:int(v) for k,v in row.attrib.items()}
 if not counts['total'] or counts['total']!=counts['passed']:raise RuntimeError('Incomplete xUnit evidence '+name)
 groups=collections.Counter()
 for test in tree.findall('.//'+ns+'UnitTest'):
  method=test.find(ns+'TestMethod')
  if method is not None:groups[method.attrib['className']]+=1
 return dict(path=name,sha256=sha(p),counts=counts,testCasesByClass=dict(groups))
xunit={k:trx(path) for k,path in {'scanner':'artifacts/validation/scanner.trx','parser':'artifacts/parser-validation/parser.trx','binder':'artifacts/binder-validation/binder.trx','checker':'artifacts/data-types-validation/checker.trx'}.items()}
def native(name):
 p=ROOT/name;description=subprocess.check_output(['file',str(p)],text=True).strip()
 if 'ELF' not in description:raise RuntimeError('Expected native ELF '+name)
 return dict(path=name,sha256=sha(p),bytes=p.stat().st_size,file=description,dependencies=subprocess.check_output(['ldd',str(p)],text=True).strip())
native_files={label:native(path) for label,path in {'foundation':'artifacts/foundation-validation/native/Foundation.Validation','scanner':'artifacts/validation/native/Scanner.Validation','parser':'artifacts/parser-validation/native/Parser.Validation','binder':'artifacts/binder-validation/native/Binder.Validation','checker':'artifacts/data-types-validation/native/Checker.Validation'}.items()}
before=read('artifacts/foundation-validation/native-hashes-before.json')
for item in native_files.values():
 if before[item['path']]!=item['sha256']:raise RuntimeError('Native binary differs from pre-execution fingerprint: '+item['path'])
proof=dict(upstreamRevision=manifest['upstreamRevision'],baselineCommit='b6b22b9',gate=run,
 sourceSha256=run['sourceSha256'],verificationAssetsSha256=run['verificationAssetsSha256'],
 nativeBinaries=native_files,xUnit=xunit,sharedAssertionCategories=dict(scannerOriginalCases=2532,scannerAdditionalGroups=20,parserOriginalSyntaxCases=561,defaultEnglishDiagnosticGroups=2,additionalSyntaxServiceGroups=14,additionalCloneGraphs=21,additionalDiagnosticWitnessGraphs=3),nativeFingerprintsBeforeExecution=before,targetedFoundationNative=read('artifacts/foundation-validation/targeted-native-result.json'),sharedNativeDirect=read('artifacts/foundation-validation/native-direct.json'),sharedNativeCaseCounts=read('artifacts/foundation-validation/native-counts.json'),originalFiniteNativeFuzz=read('artifacts/foundation-validation/native-fuzz.json'),
 broadParser=parser,broadScanner=scanner,inputManifest=manifest,originalTestFunctionSummary=catalog['originalFunctionSummary'],
 additionalDiagnosticDispositions=read('validation/foundation-corpus/coverage-dispositions.json'),
 retainedNativeRegressions={label:read(path) for label,path in {'scanner':'artifacts/validation/native-result.json','parser':'artifacts/parser-validation/native-result.json','binder':'artifacts/binder-validation/native-result.json','dataTypes':'artifacts/data-types-validation/native-result.json','objects':'artifacts/data-types-validation/object-native-result.json','scalar':'artifacts/data-types-validation/scalar-native-result.json','boundary':'artifacts/data-types-validation/native-boundary.json'}.items()},
 defectDiscovery=dict(parserInitialManagedReport=read('artifacts/foundation-validation/broad-managed/report.json'),scannerInitialManagedReport=read('artifacts/foundation-validation/broad-scanner-managed/report.json'),repairReplay=read('artifacts/foundation-validation/repair-managed/report.json'),assetAdaptationReplay=read('artifacts/foundation-validation/asset-adaptations-managed/report.json')),
 runtime=dict(dotnetVersion=subprocess.check_output(['dotnet','--version'],text=True).strip(),goVersion=subprocess.check_output([str(ROOT/'tools/.cache/go/bin/go'),'version'],text=True).strip(),nodeVersion=(OUT/'node-version.txt').read_text().strip()),
 limits=['Input-only compiler/transpile reuse does not establish original compiler/emit/project/checker baselines.','Finite original fuzz seeds run; no unbounded fuzz engine claim.','Three parser and one regexp diagnostic branches are source-unreachable as individually documented, not observed.','Default English diagnostics only; localization files/generation, content-mapper interpretation, incremental diagnostic repopulation and projects remain excluded.','Managed callback prefix-copy preserves captured Go slice contents/order under append; arbitrary backing-array alias/capacity mutation is not claimed.'])
p=ROOT/'validation/results/foundation-verified.json';p.write_text(json.dumps(proof,indent=2)+'\n');print(json.dumps(dict(path=str(p.relative_to(ROOT)),sourceSha256=proof['sourceSha256'],verificationAssetsSha256=proof['verificationAssetsSha256'],nativeFoundationSha256=native_files['foundation']['sha256'],parserRequests=parser['counts']['requests'],scannerRequests=scanner['counts']['requests'],xUnitCases=sum(r['counts']['total'] for r in xunit.values())),separators=(',',':')))
