"""Scoped reproducible original graph observer overlay; no pinned file mutation."""
from pathlib import Path
import hashlib,json
ROOT=Path(__file__).resolve().parents[2]
SOURCE=ROOT/'upstream/TypeScript/tsc/internal/compiler'
def generate(output):
 output.mkdir(parents=True,exist_ok=True)
 replacements={};proof=[]
 def apply(name,changes):
  path=SOURCE/name;original=path.read_text();value=original
  if 'oracleGraph' in value or 'oracleCapture' in value:raise ValueError('Already instrumented '+name)
  for old,new in changes:
   if value.count(old)!=1:raise ValueError('Original site count '+name+': '+repr(old))
   value=value.replace(old,new)
  target=output/name;target.write_text(value);replacements[str(path)]=str(target)
  proof.append({'source':str(path.relative_to(ROOT)),'sourceSha256':hashlib.sha256(path.read_bytes()).hexdigest(),'replacementSha256':hashlib.sha256(target.read_bytes()).hexdigest(),'exactSites':len(changes)})
 apply('fileloader.go',[
  ('type fileLoader struct {','type fileLoader struct {\n\toracleGraph *OracleResolutionGraph // authored contract-scoped observer only'),
  ('\t\t\tresolutionsTrace = append(resolutionsTrace, trace...)','\t\t\tresolutionsTrace = append(resolutionsTrace, trace...)\n\t\t\toracleEdge := p.oracleCaptureModule(t, entry, index-importsStart, moduleName, mode, resolvedModule, trace, err)'),
  ('\t\t\tshouldAddFile := moduleName !=', '\t\t\toracleResolutionDiagnostic := module.GetResolutionDiagnostic(optionsForFile, resolvedModule, file)\n\t\t\tshouldAddFile := moduleName !='),
  ('module.GetResolutionDiagnostic(optionsForFile, resolvedModule, file) == nil &&', 'oracleResolutionDiagnostic == nil &&'),
  ('\n\t\t\tif shouldAddFile {','\n\t\t\tp.oracleCaptureModuleDecision(oracleEdge, shouldAddFile, isJsFile, oracleResolutionDiagnostic)\n\t\t\tif shouldAddFile {'),
  ('\t\tresolved, trace := p.resolver.ResolveTypeReferenceDirective(ref.FileName, fileName, resolutionMode, redirect)','\t\tresolved, trace := p.resolver.ResolveTypeReferenceDirective(ref.FileName, fileName, resolutionMode, redirect)\n\t\tp.oracleCaptureType(t, ref, resolutionMode, resolved, trace)'),
 ])
 apply('filesparser.go',[
  ('\t\t\tresolvedRef, processingDiagnostic := loader.resolveTripleslashPathReference(ref.FileName, file.FileName(), index)','\t\t\tresolvedRef, processingDiagnostic := loader.resolveTripleslashPathReference(ref.FileName, file.FileName(), index)\n\t\t\tloader.oracleCapturePath(t, ref, resolvedRef, processingDiagnostic)'),
  ('if compilerOptions.NoLib != core.TSTrue && !loader.opts.SkipModuleResolution {','if compilerOptions.NoLib != core.TSTrue && !loader.opts.SkipModuleResolution && loader.oracleGraph == nil {'),
 ])
 target=output/'oracle_resolution_graph.go';target.write_bytes((ROOT/'tools/resolution-oracle/graph_access.go.inc').read_bytes());replacements[str(SOURCE/'oracle_resolution_graph.go')]=str(target)
 (output/'overlay.json').write_text(json.dumps({'Replace':replacements},indent=2)+'\n')
 (output/'provenance.json').write_text(json.dumps({'authored':True,'inputs':proof,'hookScope':'Passive original module/path/type publication; approved external lib-reference scheduling boundary only','diagnosticMetadataView':['Program.opts actual loader opts','Program.hosts.Host actual compiler host','Program.caseSensitivity actual FS','Program.processedFiles actual filesParser output','Program.resolutionData actual resolver data'],'noProductionSourceMutation':True},indent=2)+'\n')
 return output/'overlay.json'
