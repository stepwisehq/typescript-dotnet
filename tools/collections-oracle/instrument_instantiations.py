"""Generate an exact metadata-only Go overlay, never edit pinned source.
Each hook follows an original store. Seed keys are assigned once to a local so
capture reuses the already-computed key rather than reevaluating its helper.
"""
from pathlib import Path
import difflib,hashlib,json
ROOT=Path(__file__).resolve().parents[2]
def generate(output):
 original=ROOT/'upstream/TypeScript/tsc/internal/checker/checker.go'
 source=original.read_text();modified=source
 patches=[
 ('\t\t\t\t\t\t\t\tconstraint := c.instantiateType(declaredConstraint, mapper)', '\t\t\t\t\t\t\t\toracleCollectionsCaptureDeferred(c, mapper, parent, typeParameters)\n\t\t\t\t\t\t\t\tconstraint := c.instantiateType(declaredConstraint, mapper)'),
 ('key := getTypeInstantiationKey(typeArguments, newAlias, t.objectFlags&ObjectFlagsSingleSignatureType != 0)', 'oracleCollectionsSingleSignature := t.objectFlags&ObjectFlagsSingleSignatureType != 0\n\tkey := getTypeInstantiationKey(typeArguments, newAlias, oracleCollectionsSingleSignature)'),
 ('data.instantiations[getTypeInstantiationKey(typeParameters, target.alias, false)] = target', 'oracleCollectionsSeedKey := getTypeInstantiationKey(typeParameters, target.alias, false)\n\t\tdata.instantiations[oracleCollectionsSeedKey] = target\n\t\toracleCollectionsCaptureObjectInstantiation(c, target, data.instantiations, oracleCollectionsSeedKey, typeParameters, target.alias, false, target)'),
 ('data.instantiations[key] = result', 'data.instantiations[key] = result\n\t\toracleCollectionsCaptureObjectInstantiation(c, target, data.instantiations, key, typeArguments, newAlias, oracleCollectionsSingleSignature, result)'),
 ('links.instantiations[key] = instantiation','links.instantiations[key] = instantiation\n\t\toracleCollectionsCaptureInstantiation(c, links, links.instantiations, key, typeArguments, alias, "alias", false, instantiation)'),
 ('links.instantiations[getTypeListKey(typeParameters)] = t','oracleCollectionsSeedKey := getTypeListKey(typeParameters)\n\t\t\t\tlinks.instantiations[oracleCollectionsSeedKey] = t\n\t\t\t\toracleCollectionsCaptureInstantiation(c, links, links.instantiations, oracleCollectionsSeedKey, typeParameters, nil, "alias-seed", false, t)'),
 ('root.instantiations[key] = result','root.instantiations[key] = result\n\t\t\toracleCollectionsCaptureInstantiation(c, root, root.instantiations, key, typeArguments, alias, "conditional", forConstraint, result)'),
 ('root.instantiations[getConditionalTypeKey(outerTypeParameters, nil /*alias*/, false /*forConstraint*/)] = links.resolvedType','oracleCollectionsSeedKey := getConditionalTypeKey(outerTypeParameters, nil /*alias*/, false /*forConstraint*/)\n\t\t\troot.instantiations[oracleCollectionsSeedKey] = links.resolvedType\n\t\t\toracleCollectionsCaptureInstantiation(c, root, root.instantiations, oracleCollectionsSeedKey, outerTypeParameters, nil, "conditional", false, links.resolvedType)'),
 ]
 for old,new in patches:
  if modified.count(old)!=1:raise RuntimeError('Producer site changed: '+old)
  modified=modified.replace(old,new)
 # Finite provenance of already-computed fresh object inputs and the actual
 # regularization source. Scope replacements to original function boundaries.
 for signature,needle,replacement in [
  ('func (c *Checker) chooseOverload(', '{\n', '{\n\tdefer oracleCollectionsEnterOverload(c, s.node, relation, s.argCheckMode)()\n'),
  ('func (c *Checker) isSignatureApplicable(', '{\n', '{\n\tdefer oracleCollectionsEnterApplicability(c, node, signature, relation, checkMode, reportErrors)()\n'),
  ('func (c *Checker) checkObjectLiteral(', '\t\treturn result\n\t}', '\t\toracleCollectionsCaptureObject(c, result, node, checkMode, contextualType)\n\t\treturn result\n\t}'),
  ('func (c *Checker) getRegularTypeOfObjectLiteral(', '\treturn regular', '\toracleCollectionsCaptureRegularObject(c, regular, t)\n\treturn regular')]:
  start=modified.index(signature);end=modified.index('\nfunc ',start+5)
  function=modified[start:end]
  if needle=='{\n':
   headerEnd=function.index('{\n');function=function[:headerEnd]+function[headerEnd:].replace(needle,replacement,1)
  else:
   if function.count(needle)!=1:raise RuntimeError('Object provenance producer changed: '+signature)
   function=function.replace(needle,replacement)
  modified=modified[:start]+function+modified[end:]
 output.mkdir(parents=True,exist_ok=True);target=output/'checker.go';target.write_text(modified)
 diff=''.join(difflib.unified_diff(source.splitlines(True),modified.splitlines(True),fromfile='pinned/checker.go',tofile='metadata-overlay/checker.go'))
 (output/'instrumentation.diff').write_text(diff)
 replacements={str(original.resolve()):str(target.resolve())}
 extra_files=[]
 extra_patches={
 'inference.go':[
 ('compareTypes = c.compareTypesAssignable','compareTypes = c.compareTypesAssignable\n\t\toracleCollectionsCaptureComparer(c, compareTypes, c, "checker-assignable", 0)'),
 ('n.nonFixingMapper = c.newInferenceTypeMapper(n, false /*fixing*/)','n.nonFixingMapper = c.newInferenceTypeMapper(n, false /*fixing*/)\n\toracleCollectionsCaptureInferenceComparer(c, n, compareTypes)'),
 ],
 'relater.go':[
 ('nil /*errorReporter*/, c.compareTypesAssignable, nil /*reportUnreliableMarkers*/','nil /*errorReporter*/, oracleCollectionsCaptureComparer(c, c.compareTypesAssignable, c, "checker-assignable", 0), nil /*reportUnreliableMarkers*/'),
 ('r.c.newInferenceContext(sourceParams, nil /*signature*/, InferenceFlagsNone, r.isRelatedToWorker)','r.c.newInferenceContext(sourceParams, nil /*signature*/, InferenceFlagsNone, oracleCollectionsCaptureComparer(r.c, r.isRelatedToWorker, r, "relater-worker", 0))'),
 ('return r.c.compareSignaturesRelated(source, target, checkMode, reportErrors, r.reportError, isRelatedToWorker, r.c.reportUnreliableMapper)','oracleCollectionsCaptureComparer(r.c, isRelatedToWorker, r, "signature-related", uint32(intersectionState))\n\treturn r.c.compareSignaturesRelated(source, target, checkMode, reportErrors, r.reportError, isRelatedToWorker, r.c.reportUnreliableMapper)'),
 ]}
 for name,changes in extra_patches.items():
  original_extra=original.with_name(name);before=original_extra.read_text();after=before
  for old,new in changes:
   if after.count(old)!=1:raise RuntimeError('Comparer producer site changed: '+old)
   after=after.replace(old,new)
  saved=output/name;saved.write_text(after);replacements[str(original_extra.resolve())]=str(saved.resolve())
  diff+=''.join(difflib.unified_diff(before.splitlines(True),after.splitlines(True),fromfile='pinned/'+name,tofile='metadata-overlay/'+name))
  extra_files.append({'file':name,'originalSha256':hashlib.sha256(before.encode()).hexdigest(),'overlaySha256':hashlib.sha256(after.encode()).hexdigest()})
 (output/'instrumentation.diff').write_text(diff)
 (output/'overlay.json').write_text(json.dumps({'Replace':replacements},indent=2)+'\n')
 (output/'manifest.json').write_text(json.dumps({'upstream':'fed0bf24149fb1ed36039212648bafdafc1ea10e','originalSha256':hashlib.sha256(source.encode()).hexdigest(),'overlaySha256':hashlib.sha256(modified.encode()).hexdigest(),'diffSha256':hashlib.sha256(diff.encode()).hexdigest(),'producerHooks':16,'additionalFiles':extra_files,'semanticPolicy':'Existing keys and computed values captured after original store; no additional checker/getter/mapper or ID evaluation. Three seed expressions evaluated once at original position into authored locals. Applicability and choose-overload frames capture only original entry inputs and defer restoration on every exit; candidate/reportErrors are absent for choose-overload.'},indent=2)+'\n')
if __name__=='__main__':
 import argparse
 parser=argparse.ArgumentParser();parser.add_argument('--output',type=Path,default=ROOT/'artifacts/collections-validation/instantiation-provenance');args=parser.parse_args();generate(args.output)
