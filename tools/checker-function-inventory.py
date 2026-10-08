#!/usr/bin/env python3
"""Record selected hand-port function locations and explicit branch rejection.

This is an inspection inventory, not a C# generator or a proof of equivalence.
The reviewed C# is authoritative; the branch contract also lives in CHECKER-PLAN.
"""
import json,pathlib,re
root=pathlib.Path(__file__).resolve().parents[1]
pin='fed0bf24149fb1ed36039212648bafdafc1ea10e'
upstream=root/'upstream/TypeScript/tsc/internal'
files=['Checker','Relater','Flow','Utilities','GrammarChecks','NodeBuilder','NodeBuilderImpl','Printer','Exports']
sources={}
for package in ['checker','printer']:
 for path in (upstream/package).glob('*.go'):
  for m in re.finditer(r'^func\s+(?:\([^\n]*?\)\s+)?(\w+)\(',path.read_text(),re.M):
   sources.setdefault(m.group(1),[]).append({'path':str(path.relative_to(root)),'line':path.read_text().count('\n',0,m.start())+1})

sources['getFeatureMap']=[{'path':'upstream/TypeScript/tsc/internal/checker/utilities.go','line':1336,'kind':'lazy initializer'}]

def end_of_method(s,start):
 # Mask ordinary/verbatim strings, characters and comments before balancing braces.
 masked=re.sub(r'//[^\n]*|/\*.*?\*/|@"(?:""|[^"])*"|"(?:\\.|[^"\\])*"|\'(?:\\.|[^\'\\])*\'',lambda m:' '*len(m.group()),s,flags=re.S)
 brace=masked.find('{',start);arrow=masked.find('=>',start)
 if arrow>=0 and (brace<0 or arrow<brace):return masked.index(';',arrow)+1
 if brace<0:raise ValueError('Missing method body')
 depth=1;i=brace+1
 while depth:
  if masked[i]=='{':depth+=1
  if masked[i]=='}':depth-=1
  i+=1
 return i

contracts={
 'Checker': 'Selected script scalar initialization, variable checks, caches, literal/primitive/boolean unions, name callbacks and node queries. No libraries/modules/structured types/general declarations.',
 'Relater':'Primitive/fresh-literal fast relationships, scalar union distribution/nullable normalization, scalar diagnostics. Recursive structured relation cache, signatures, constraints and object elaboration excluded.',
 'Flow':'Start/declaration-assignment/shared/unreachable flow, assignment reduction, exact straight-line variable initialization. Conditional/loop/call/reduce/evolving-array flow excluded.',
 'Utilities':'Scalar deterministic type/symbol comparison, complete feature map and primitive-name library selection, mutable-local predicate and node diagnostics. Named/structured type comparison excluded.',
 'GrammarChecks':'Fixed ESNext script variable grammar; no modifiers/ambient/destructuring/definite-assertion/function/loop forms. Numeric suggestions kept separately from errors.',
 'NodeBuilder':'Default nil-context scalar diagnostic type/symbol wrappers. General declaration serialization/trackers/verbosity/options excluded.',
 'NodeBuilderImpl':'Intrinsic/scalar literal/boolean type nodes and single-part variable/synthetic-builtin symbol expressions. Alias/structured/remapped/computed/qualified/type-argument serialization excluded.',
 'Printer':'Default-only primitive diagnostic text, byte cutoff and selected symbol names. General verbosity/flags/enclosing declaration/signature format excluded.'}
hand={'CreateCore':'Scoped newChecker adaptation: fixed single-file/no-library Program inputs; omitted unused initialization state documented in departures.',
 'unsupported':'Authored explicit unsupported exception factory.',
 'initializeScalarOptions':'Authored fixed-option initialization separated from newChecker.',
 'compareTypeLists':'Adaptation of CompareTypes scalar union recursive lexicographic comparison.',
 'typeToStringByteString':'Authored raw-byte wrapper; public TypeToString decodes once.',
 'IsTypeAssignableTo':'Authored public adapter over upstream isTypeAssignableTo with checker ownership guard.'}
records=[]
paths=[root/'src/TypeScript.Scanner/Checker'/f'{name}.cs' for name in files]+list((root/'src/TypeScript.Scanner/Printer').glob('*.cs'))
pattern=re.compile(r'^[ \t]{4,8}(?:private|public|internal|protected)\s+(?:(?:static|override|virtual|sealed|async)\s+)*[\w<>?,.() ]+?\s+(\w+)\s*\([^;{=]*\)\s*(?:=>|\{)',re.M)
for path in paths:
 text=path.read_text();package='printer' if path.parent.name=='Printer' else 'checker'
 for m in pattern.finditer(text):
  name=m.group(1);end=end_of_method(text,m.start());body=text[m.start():end]
  candidates=sources.get(name,sources.get(name[0].lower()+name[1:],[]))
  preferred=[c for c in candidates if c['path'].endswith('/'+package+'/'+path.stem.lower()+'.go')]
  candidate=(preferred or candidates)
  guards=[]
  for guard in re.finditer(r'throw\s+(?:\w+\.)*unsupported\("([^"\n]*)"',body):
   line=body.count('\n',0,guard.start())+text.count('\n',0,m.start())+1
   guards.append({'line':line,'featurePrefix':guard.group(1)})
  records.append({'name':name,'destination':str(path.relative_to(root)),'line':text.count('\n',0,m.start())+1,
   'upstream':candidate[0] if candidate else None,'mapping': 'authored/adapted' if name in hand else 'selected upstream function' if candidate else 'authored or adapted helper',
   'includedBranches':hand.get(name,contracts.get(path.stem,'Synthesized scalar/identifier printer branch; no source/comment/maps/general emit.')),
   'retainedConditions':re.findall(r'\bif\s*\(([^\n]*)\)',body),
   'retainedCases':re.findall(r'case\s+([^:]+):',body),
   'explicitUnsupportedGuards':guards,
   'omittedByContract':'Other semantic branches excluded by source admission/fixed options; see CHECKER-PLAN.md and checker-departures.md.'})
result={'pin':pin,'status':'Selected partial function inventory; no full checker/emit claim','methodCount':len(records),'authoritative':'Reviewed C# hand ports; generator is not claimed for these algorithm files','functions':records}
output=root/'docs/checker-functions.json';output.write_text(json.dumps(result,indent=2)+'\n')
print(f'{len(records)} functions inventoried; unmapped: '+', '.join(sorted({r['name'] for r in records if r['upstream'] is None and r['name'] not in hand})))
