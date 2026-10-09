"""Precise metadata-only overlay of existing flow key writes and hash return."""
from pathlib import Path
import argparse, difflib, hashlib, json
ROOT=Path(__file__).resolve().parents[2]
def generate(output,replacements=None):
 original=ROOT/'upstream/TypeScript/tsc/internal/checker/flow.go'
 before=Path((replacements or {}).get(str(original),original)).read_text();after=before
 start=after.index('func (c *Checker) getFlowReferenceKey(');end=after.index('\nfunc ',start+5)
 function=after[start:end]
 if 'oracleCollectionsFlow' in function:raise RuntimeError('Flow return already instrumented')
 for needle in ['var b keyBuilder','return b.hash()','return nonDottedNameCacheKey']:
  if function.count(needle)!=1:raise RuntimeError('Original flow return site changed: '+needle)
 function=function.replace('var b keyBuilder','var b keyBuilder\n\toracleCollectionsFlowStart(c, &b)').replace('return b.hash()','key := b.hash()\n\t\toracleCollectionsFlowEnd(c, &b, key, true)\n\t\treturn key').replace('return nonDottedNameCacheKey','oracleCollectionsFlowEnd(c, &b, nonDottedNameCacheKey, false)\n\treturn nonDottedNameCacheKey')
 after=after[:start]+function+after[end:]
 start=after.index('func (c *Checker) writeFlowCacheKey(');end=after.index('\nfunc ',start+5)
 function=after[start:end]
 if 'oracleCollectionsFlow' in function:raise RuntimeError('Flow writes already instrumented')
 for call,kind,owner,width in [('b.writeSymbol(symbol)','symbol','symbol',8),('b.writeType(declaredType)','type','declaredType',4),('b.writeType(initialType)','type','initialType',4),('b.writeNode(flowContainer)','node','flowContainer',8),('b.writeNode(node)','node','node',8)]:
  expected=2 if call in ['b.writeSymbol(symbol)','b.writeType(declaredType)'] else 1
  if function.count(call)!=expected:raise RuntimeError('Original flow writer count changed: '+call)
  function=function.replace(call,call+f'\n\t\toracleCollectionsFlowSlotCapture(c, b, "{kind}", {owner}, {width})')
 after=after[:start]+function+after[end:]
 output.mkdir(parents=True,exist_ok=True);target=output/'flow.go';target.write_text(after)
 diff=''.join(difflib.unified_diff(before.splitlines(True),after.splitlines(True),fromfile='pinned/flow.go',tofile='metadata-overlay/flow.go'))
 (output/'flow.diff').write_text(diff)
 result=dict(replacements or {});result[str(original)]=str(target.resolve())
 (output/'overlay.json').write_text(json.dumps({'Replace':result},indent=2)+'\n')
 (output/'flow-manifest.json').write_text(json.dumps({'upstream':'fed0bf24149fb1ed36039212648bafdafc1ea10e','originalSha256':hashlib.sha256(before.encode()).hexdigest(),'overlaySha256':hashlib.sha256(after.encode()).hexdigest(),'diffSha256':hashlib.sha256(diff.encode()).hexdigest(),'semanticPolicy':'Entity slots recorded after original writes; existing completed hash evaluated once. Raw gaps retained; no getter/ID/mapper work in metadata.'},indent=2)+'\n')
 return result
if __name__=='__main__':
 p=argparse.ArgumentParser();p.add_argument('--output',type=Path,required=True);p.add_argument('--base-overlay',type=Path);args=p.parse_args()
 generate(args.output,json.loads(args.base_overlay.read_text())['Replace'] if args.base_overlay else None)
