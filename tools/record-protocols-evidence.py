#!/usr/bin/env python3
"""Record the complete frozen type-system gate, never overwrite historical proof."""
import datetime, importlib.util, json, os, subprocess
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
if os.environ.get('DECLARATIONS_CURRENT_CHECKING')!='1':raise RuntimeError('Explicit current checking context required')
spec=importlib.util.spec_from_file_location('protocols_gate',ROOT/'tools/run-protocols-gate.py');gate_module=importlib.util.module_from_spec(spec);spec.loader.exec_module(gate_module)

def main():
    g=gate_module;gate=g.read('artifacts/protocols-validation/final-gate.json')
    drift=('sourceDrift','assetDrift','historicalAssetDrift','upstreamDrift','obligationDrift')
    if gate['exitCode'] or any(gate[k] for k in drift):raise RuntimeError('Failed/drifted gate')
    if gate['sourceSha256']!=g.sources() or gate['verificationAssetsSha256']!=g.fingerprint(g.assets()) or gate['reviewedAssertionObligations']!=g.obligations() or gate['protectedHistoricalAssetFiles']!=g.historical():raise RuntimeError('Frozen evidence differs')
    execution=g.check_execution();m=g.manifest_groups();binaries=g.native_snapshot()
    for path,row in binaries.items():
        row['file']=subprocess.check_output(['file',str(ROOT/path)],text=True).strip()
        if 'ELF 64-bit' not in row['file'] or 'x86-64' not in row['file']:raise RuntimeError('Expected actual Linux x64 ELF')
        result=subprocess.run(['ldd',str(ROOT/path)],stdout=subprocess.PIPE,stderr=subprocess.STDOUT,text=True);row['dependencies']=result.stdout.strip()
        if 'libcoreclr' in row['dependencies'].lower():raise RuntimeError('Managed runtime dependency')
    typesystem=execution['retainedExecution'];expressions=typesystem['retainedExecution'];declarations=expressions['retainedExecution'];program=declarations['retainedExecution'];resolution=program['retainedExecution'];configuration=resolution['retainedExecution'];projects=configuration['retainedExecution']
    selected=[execution['protocolsXUnit'],typesystem['typesystemXUnit'],expressions['expressionsXUnit'],declarations['declarationsXUnit'],program['programXUnit'],resolution['resolutionXUnit'],configuration['configurationXUnit'],*projects['xUnit'].values()]
    result={'stage':'typescript-async-generator-runtime-protocols','baseline':g.BASELINE,'upstreamRevision':m['upstream'],'recordedAtUTC':datetime.datetime.now(datetime.timezone.utc).isoformat(),'command':gate['command'],'exitCode':0,'elapsedSeconds':gate['elapsedSeconds'],'sourceSha256':gate['sourceSha256'],'verificationAssetsSha256':gate['verificationAssetsSha256'],**{k:False for k in drift},'reviewedAssertionObligations':gate['reviewedAssertionObligations'],'log':gate['log'],'logSha256':g.sha(gate['log']),'nativeBinaries':binaries,'managedBinaries':execution['managedBinaries'],'originalOracleBinaries':execution['originalOracleBinaries'],'protocolsManaged':execution['managed'],'protocolsNative':execution['native'],'protocolsXUnit':execution['protocolsXUnit'],'sharedChecks':m['sharedChecks'],'originalDirectTests':execution['original']['directTests'],'originalGoCorpus':execution['original']['corpus'],'jsonErrorProjection':execution['original']['jsonErrorProjection'],'retainedTypeSystemExecution':typesystem,'xUnitCases':sum(v['counts']['total'] for v in selected),'selectedXUnitRecords':[{'trx':v['trx'],'cases':v['counts']['total']} for v in selected],'currentCheckingEnvironment':m['currentCheckingEnvironment'],'currentCheckingReplacement':m['currentCheckingReplacement'],'authoredControlRetirement':m['authoredControlRetirement'],'corpusManifest':g.MANIFEST,'corpusManifestSha256':g.sha(g.MANIFEST),'semanticObservation':m['semanticObservation'],'contract':'docs/PROTOCOLS-CONTRACT.md','wholeCheckerInventory':m['wholeCheckerInventory'],'mapperGeneratorRestoration':m['mapperGeneratorRestoration'],'limits':[
      'P1–P6 source responsibilities use genuine unchanged Program options/libraries. Finite records and fixture selection do not define the contract.',
      'One complete original mapper generator body is restored, including both policies, bulk globals and incremental file/global diagnostics. Checker2, earlier mapper3 and casing1 are retained original bodies. One authored external consumer check remains separate.',
      'Complete bulk/per-file/incremental/repeated diagnostics and finite public types, symbols, signatures and properties are compared. Property display names use actual SymbolToString, not allocation keys. No private graph census.',
      'The complete compiler/conformance inventory records source/configuration and original assertion obligations only; it is not whole-checker runtime acceptance. Original skips remain dispositions, not passes or automatic final exclusions.',
      'Compiler input reuse compares complete genuine semantic records with actual options. It does not claim original complete errors/type/symbol/output baseline assertions.',
      'Declaration-output and Emit-induced diagnostics/pre-post count invariants are external operations. Applicable pre-emit config/Program/syntactic/semantic/global/suggestion obligations remain recorded unchanged.',
      'Exactly one retained type-system cascade runs; predecessor repeated tests remain actual executions. xUnitCases sums explicitly selected TRX execution records, not distinct cases or every repeated execution.',
      'Actual Linux x64 NativeAOT binaries and ordinary disk CLI exits/complete diagnostics are verified. No emitted program execution or external JS/JSX/decorator/module-consumer completion is claimed.',
      'Current retained authored controls use fixed DECLARATIONS_CURRENT_CHECKING=1 provenance. Historical corpus/proofs remain immutable; reviewed additive mapper restoration does not change earlier expectations.'
    ]}
    path=ROOT/'validation/results/protocols-verified.json'
    if path.exists():raise RuntimeError('Preserve existing proof')
    path.write_text(json.dumps(result,indent=2)+'\n');print(json.dumps({'evidence':str(path.relative_to(ROOT)),'xUnitCases':result['xUnitCases'],'elapsedSeconds':result['elapsedSeconds']}))
if __name__=='__main__':main()
