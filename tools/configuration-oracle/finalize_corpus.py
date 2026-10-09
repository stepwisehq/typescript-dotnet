"""Publish checked configuration corpus metadata; never generate expectations."""
from pathlib import Path
import argparse,gzip,hashlib,json,subprocess
ROOT=Path(__file__).resolve().parents[2];BASE=ROOT/'validation/configuration-corpus'
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
def load(p):return [json.loads(row)for row in (gzip.decompress(p.read_bytes())if p.suffix=='.gz'else p.read_bytes()).splitlines()]
def main():
 parser=argparse.ArgumentParser(description=__doc__);parser.add_argument('--registry',required=True,type=Path);args=parser.parse_args();registry=json.loads(args.registry.read_text());groups=[];names={};counts={}
 for req in sorted(BASE.glob('*-requests.jsonl')):
  if req.name in ('cli-requests.jsonl','order-requests.jsonl'):continue
  expected=req.with_name(req.name.replace('-requests.jsonl','-expected.jsonl.gz'));requests=load(req);responses=load(expected)
  if not requests or len(requests)!=len(responses):raise ValueError('Corpus cardinality differs')
  for line,r,e in zip(req.read_bytes().splitlines(),requests,responses):
   if r['name']in names or r['name']!=e['name']or r['mode']!=e['mode']:raise ValueError('Corpus identity differs')
   names[r['name']]=line;counts[r['mode']]=counts.get(r['mode'],0)+1
  groups.append({'group':req.name.removesuffix('-requests.jsonl'),'requests':str(req.relative_to(ROOT)),'expected':str(expected.relative_to(ROOT)),'cases':len(requests),'requestsSha256':sha(req),'expectedSha256':sha(expected)})
 selected=['source-discovery-extends','source-inherited-error-cache','command-response-cycle','build-arguments','declared-nil-map','override-raw-file','inheritance-array-cache-isolation','inheritance-cycle','package-extends-tsconfig-field','package-self-config-priority','references-output-build-metadata','mapper-inherited-manifest','glob-reload-unicode-case','drive-root-discovery','unc-root-discovery','raw-byte-config-owner','read-failure-config','missing-config']
 order=BASE/'order-requests.jsonl';order.write_bytes(b'\n'.join(names[name]for name in selected)+b'\n')
 cli={key:str((BASE/('cli-'+key+('.jsonl'if key!='provenance'else'.json'))).relative_to(ROOT))for key in ('requests','expected','provenance')};cli['cases']=len(load(ROOT/cli['requests']));cli['originalCases']=len(load(ROOT/cli['expected']));cli['authoredBoundaryCases']=cli['cases']-cli['originalCases'];cli['lexicalProjectionControls']=14
 for key in ('requests','expected','provenance'):cli[key+'Sha256']=sha(ROOT/cli[key])
 packages={}
 inventory=json.loads((ROOT/'docs/configuration-tests.json').read_text())
 for row in inventory['entries']:
  if row.get('disposition')=='assertion-preserving-port'and row.get('shared'):
   package=packages.setdefault(row['package'],{'package':row['package'],'functions':[],'sourceFiles':{}});package['functions'].append(row['name']);package['sourceFiles'][row['source']]=row['sourceSha256']
 dependencyPackages={}
 dependencySelected={(row['package'],row['name']):row['source']for row in inventory['entries']if row.get('disposition')=='dependency-assertion-preserving-port'}
 import re
 for name in ('ordered-map-test-provenance.json','dependency-metadata-test-provenance.json','module-test-provenance.json'):
  provenance=json.loads((ROOT/'validation/Configuration.Checks'/name).read_text());functions=provenance.get('assertionPreservingFunctions',list(provenance.get('functions',{})))
  for function in functions:
   matches=[source for source,digest in provenance['sources'].items()if source.startswith('upstream/TypeScript/tsc/')and source.endswith('_test.go')and sha(ROOT/source)==digest and re.search(r'^func '+re.escape(function)+r'\(', (ROOT/source).read_text(),re.M)]
   if len(matches)!=1:raise ValueError('Dependency original source ambiguous: '+function)
   source=matches[0];package='./'+str(Path(source).parent.relative_to('upstream/TypeScript/tsc'));key=(package,function)
   if key in dependencySelected:raise ValueError('Duplicate dependency original: '+function)
   dependencySelected[key]=source
 for (package,function),source in sorted(dependencySelected.items()):
  group=dependencyPackages.setdefault(package,{'package':package,'functions':[],'sourceFiles':{}});group['functions'].append(function);group['sourceFiles'][source]=sha(ROOT/source)
 if len(dependencySelected)!=42:raise ValueError('Reviewed dependency original registry must contain42 functions')
 manifest={'schema':'configuration-v1','upstreamRevision':'fed0bf24149fb1ed36039212648bafdafc1ea10e','baseline':'2c850db','cases':sum(g['cases']for g in groups),'groups':groups,'modeCounts':counts,'sharedChecks':registry,'corpusXUnitClassCounts':{'Configuration.Tests.ConfigurationOracleTests':sum(g['cases']for g in groups)},'orderControls':{'requests':str(order.relative_to(ROOT)),'requestsSha256':sha(order),'cases':len(selected),'sequencesPerRuntime':2,'executionsPerRuntime':2*(len(selected)+1)},'cliControls':cli,'originalTests':list(packages.values()),'dependencyOriginalTests':list(dependencyPackages.values()),'demonstrations':[{'name':'checking-demo-valid','directory':'examples/projects/grouped-records','configuration':'tsconfig.json','exitCode':0},{'name':'checking-demo-invalid','directory':'examples/projects/grouped-records-invalid','configuration':'tsconfig.json','exitCode':1}],'observationLimits':['Complete stored option/spec/cache/query values are observed; shared reference topology is established only by preserved clone/merge/WithFileNames/cache mutation assertions, not a private graph census.','Ordinary multi-key map traversal and correlated error-prefix order are not compared; malformed default-map boundaries use single-key inputs, and prior-prefix assertions use actual OrderedMap insertion order. Forty source process variants remain investigation evidence.','Original json/v2 SemanticError own modal prefix is canonicalized only at actual typed error nodes; original raw variants are retained in oracle stderr. Plain or wrapped user errors and compiler/CLI diagnostics remain exact.','Typed-nil JSON input declarations are authored metadata where managed null erases interface type; nil ordered-map pointer is rejected invalid source API input.','UNC host path-role relocation only; virtual arbitrary-byte ReadFile equivalence with original fstest is unclaimed. Raw config owner bytes are observed through the source API.','Contentmapper processes, project builds/referenced-project execution, emit, watch, acquisition and full Program option-combination validation remain outside standalone interpretation.']}
 osExpected=BASE/'os-projections-expected.json';osProvenance=BASE/'os-projections-provenance.json'
 if osExpected.exists()and osProvenance.exists():manifest['osProjections']={'expected':str(osExpected.relative_to(ROOT)),'provenance':str(osProvenance.relative_to(ROOT)),'expectedSha256':sha(osExpected),'provenanceSha256':sha(osProvenance)}
 modalExpected=BASE/'json-error-controls-expected.json';modalProvenance=BASE/'json-error-controls-provenance.json'
 if modalExpected.exists()and modalProvenance.exists():manifest['jsonErrorProjection']={'expected':str(modalExpected.relative_to(ROOT)),'provenance':str(modalProvenance.relative_to(ROOT)),'expectedSha256':sha(modalExpected),'provenanceSha256':sha(modalProvenance),'controls':6}
 (BASE/'manifest.json').write_text(json.dumps(manifest,indent=2)+'\n');print(json.dumps({'cases':manifest['cases'],'shared':registry['count'],'orderCases':len(selected),'originalFunctions':sum(len(x['functions'])for x in packages.values())}))
if __name__=='__main__':main()
