#!/usr/bin/env python3
"""Record the complete frozen type-system gate, never overwrite historical proof."""
import datetime, importlib.util, json, os, subprocess
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
if os.environ.get('DECLARATIONS_CURRENT_CHECKING')!='1':raise RuntimeError('Explicit current checking context required')
spec=importlib.util.spec_from_file_location('typesystem_gate',ROOT/'tools/run-typesystem-gate.py');gate_module=importlib.util.module_from_spec(spec);spec.loader.exec_module(gate_module)

def main():
    g=gate_module;gate=g.read('artifacts/typesystem-validation/final-gate.json')
    drift=('sourceDrift','assetDrift','historicalAssetDrift','upstreamDrift','obligationDrift')
    if gate['exitCode'] or any(gate[k] for k in drift):raise RuntimeError('Failed/drifted gate')
    if gate['sourceSha256']!=g.sources() or gate['verificationAssetsSha256']!=g.fingerprint(g.assets()) or gate['reviewedAssertionObligations']!=g.obligations() or gate['protectedHistoricalAssetFiles']!=g.historical():raise RuntimeError('Frozen evidence differs')
    execution=g.check_execution();m=g.manifest_groups();binaries=g.native_snapshot()
    for path,row in binaries.items():
        row['file']=subprocess.check_output(['file',str(ROOT/path)],text=True).strip()
        if 'ELF 64-bit' not in row['file'] or 'x86-64' not in row['file']:raise RuntimeError('Expected actual Linux x64 ELF')
        result=subprocess.run(['ldd',str(ROOT/path)],stdout=subprocess.PIPE,stderr=subprocess.STDOUT,text=True);row['dependencies']=result.stdout.strip()
        if 'libcoreclr' in row['dependencies'].lower():raise RuntimeError('Managed runtime dependency')
    expressions=execution['retainedExecution'];declarations=expressions['retainedExecution'];program=declarations['retainedExecution'];resolution=program['retainedExecution'];configuration=resolution['retainedExecution'];projects=configuration['retainedExecution']
    selected=[execution['typesystemXUnit'],expressions['expressionsXUnit'],declarations['declarationsXUnit'],program['programXUnit'],resolution['resolutionXUnit'],configuration['configurationXUnit'],*projects['xUnit'].values()]
    result={'stage':'typescript-shared-type-system-completion','baseline':g.BASELINE,'upstreamRevision':m['upstream'],'recordedAtUTC':datetime.datetime.now(datetime.timezone.utc).isoformat(),'command':gate['command'],'exitCode':0,'elapsedSeconds':gate['elapsedSeconds'],'sourceSha256':gate['sourceSha256'],'verificationAssetsSha256':gate['verificationAssetsSha256'],**{k:False for k in drift},'reviewedAssertionObligations':gate['reviewedAssertionObligations'],'log':gate['log'],'logSha256':g.sha(gate['log']),'nativeBinaries':binaries,'managedBinaries':execution['managedBinaries'],'originalOracleBinaries':execution['originalOracleBinaries'],'typesystemManaged':execution['managed'],'typesystemNative':execution['native'],'typesystemXUnit':execution['typesystemXUnit'],'sharedChecks':m['sharedChecks'],'originalDirectTests':execution['original']['directTests'],'originalGoCorpus':execution['original']['corpus'],'jsonErrorProjection':execution['original']['jsonErrorProjection'],'retainedExpressionsExecution':expressions,'xUnitCases':sum(v['counts']['total'] for v in selected),'selectedXUnitRecords':[{'trx':v['trx'],'cases':v['counts']['total']} for v in selected],'currentCheckingEnvironment':m['currentCheckingEnvironment'],'currentCheckingReplacement':m['currentCheckingReplacement'],'authoredControlRetirement':m['authoredControlRetirement'],'corpusManifest':g.MANIFEST,'corpusManifestSha256':g.sha(g.MANIFEST),'semanticObservation':m['semanticObservation'],'contract':'docs/TYPESYSTEM-CONTRACT.md','limits':[
      'T1–T6 source responsibilities and genuine unchanged Program options/libraries; fixture inventories and authored behavioral evidence do not define the contract.',
      'One new complete TestJSCasing body preserves all26 assertions; checker2 and restored mapper3 are retained full original bodies, not newly ported. Compiler input reuse does not claim complete compiler baseline assertions or emit checks.',
      'Complete bulk/per-file/repeated diagnostics and finite public type/property/signature/symbol displays are compared. Relations uses direct public IsTypeAssignableTo with selected owned inputs/types; no private graph or incidental allocation identity census.',
      'Property display names are actual SymbolToString, not escaped storage keys; display names do not establish arbitrary unique/private property identity.',
      'The new corpus does not request incremental/update/reuse; those obligations remain in retained Program and semantic evidence. Generator/async/checkedJS/JSX/decorator-specific and other contract-external consumers remain explicit.',
      'Typed Go SemanticError producer-only modal rendering adaptation is retained with raw stderr and six actual controls; compiler/user diagnostic text is not normalized.',
      'Exactly one retained expression cascade runs. No extra new full consumer-suite pass or modification/reuse hook is added to historical gate scripts; repeated suites inside the accepted predecessor cascade remain actual executions.',
      'xUnitCases sums selected TRX execution records. It is not a distinct test inventory or every repeated execution; selected records are explicitly listed.',
      'Current retained authored CLI replacements require exactly DECLARATIONS_CURRENT_CHECKING=1 with fixed migration provenance; historical corpora/proofs remain unchanged. Unsupported boundaries are not Go semantic success.'
    ]}
    path=ROOT/'validation/results/typesystem-verified.json'
    if path.exists():raise RuntimeError('Preserve existing proof')
    path.write_text(json.dumps(result,indent=2)+'\n');print(json.dumps({'evidence':str(path.relative_to(ROOT)),'xUnitCases':result['xUnitCases'],'elapsedSeconds':result['elapsedSeconds']}))
if __name__=='__main__':main()
