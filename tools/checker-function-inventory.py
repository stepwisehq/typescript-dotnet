#!/usr/bin/env python3
"""Record selected hand-port function locations and explicit branch rejection.

This is an inspection inventory, not a C# generator or a proof of equivalence.
The reviewed C# is authoritative; the branch contracts live in DATA-TYPES-CONTRACT, CHECKER-PLAN and OBJECT-CHECKER-PLAN.
"""
import json,pathlib,re
root=pathlib.Path(__file__).resolve().parents[1]
pin='fed0bf24149fb1ed36039212648bafdafc1ea10e'
upstream=root/'upstream/TypeScript/tsc/internal'
files=['Checker','Relater','Flow','Utilities','GrammarChecks','NodeBuilder','NodeBuilderImpl','Printer','Exports','PseudoTypeNodeBuilder','NodeCopy','NodeBuilderScopes','SymbolTracker','EmitSupport','Mapper','SymbolAccessibility','EmitResolver']
sources={}
for package in ['checker','printer','pseudochecker']:
 for path in (upstream/package).glob('*.go'):
  for m in re.finditer(r'^func\s+(?:\([^\n]*?\)\s+)?(\w+)\(',path.read_text(),re.M):
   sources.setdefault(m.group(1),[]).append({'path':str(path.relative_to(root)),'line':path.read_text().count('\n',0,m.start())+1})

sources['getFeatureMap']=[{'path':'upstream/TypeScript/tsc/internal/checker/utilities.go','line':1336,'kind':'lazy initializer'}]

# Upstream initialization stores closures in fields rather than declaring these
# as named functions. Preserve those source locations in the inspection inventory.
for name, line in {'isPrimitiveOrObjectOrEmptyType':1260,'containsMissingType':1263,'couldContainTypeVariables':1266}.items():
 sources[name]=[{'path':'upstream/TypeScript/tsc/internal/checker/checker.go','line':line,'kind':'initialized function field'}]
for name in ['getGlobalAsyncIterableIteratorType','getGlobalAsyncIterableType','getGlobalBigIntType','getGlobalESSymbolType','getGlobalIterableIteratorType','getGlobalIterableType','getGlobalPromiseType','getGlobalNonNullableTypeAliasOrNil']:
 text=(upstream/'checker/checker.go').read_text()
 match=re.search(r'^\s*c\.'+name+r' = ',text,re.M)
 if not match:raise ValueError('Missing upstream resolver field '+name)
 sources[name]=[{'path':'upstream/TypeScript/tsc/internal/checker/checker.go','line':text.count('\n',0,match.start())+1,'kind':'initialized lazy global resolver'}]

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
 'Checker':'Fixed no-library single-script initialization; scalar, named aliases/interfaces and recursive data-object checking; union/intersection reduction, genuine member/cache identity, contextual typing, deferred work, widening/property reads and internal finite-key/index/implicit-this support. Written generic/call/index/write/library/module paths reject.',
 'Relater':'Primitive, named/recursive data-object, union/intersection relationships with actual ternary/cache/maybe stacks, recursive properties, excess/missing/weak checks, inherited synthetic any-base indexes and diagnostic elaboration. Written signatures/generics/private data routes reject when nonempty/reached.',
 'Flow':'Straight-line variable/property reference flow and actual AST assignment-marking traversal. Conditional/loop/call/evolving-array flow remains excluded.',
 'Utilities':'Selected deterministic scalar/named/composite type and symbol ordering, static property names, feature map, scope/identity and diagnostic helpers. Excluded source forms guard.',
 'GrammarChecks':'Fixed ESNext script declarations, aliases/interfaces/merges/heritage and static data-property grammar. Function/class/ambient/destructuring/write modes remain excluded.',
 'NodeBuilder':'Selected diagnostic type/symbol wrappers with genuine enclosing-declaration context and caches. General emit/verbosity/external alias/source generic tracking excluded.',
 'NodeBuilderImpl':'Primitive, named/recursive data-object, union/intersection, implicit-this and synthetic index type nodes; actual cycle/truncation guards, symbol tracking and declaration reuse via PseudoChecker. Written signatures/generics/remapped/general emit serialization rejects.',
 'Printer':'Selected diagnostic wrapper boundaries, explicit caller flags and ordered primitive/named/object/union/intersection/index text with synthesized-comment policy. UTF16 public conversion separated from byte-string algorithm; general emit unsupported.',
 'PseudoTypeNodeBuilder':'Selected declaration reuse, primitive/static object equivalence, original nongeneric reference/composite type-node cloning and contextual fallback. General source generic/function reuse excludes.',
 'NodeCopy':'Selected original/source clone and existing primitive/nongeneric reference/composite/data-property syntax reuse for diagnostic nodes; general emit unsupported.',
 'NodeBuilderScopes':'Selected enclosing symbol-type scope and restore semantics for diagnostic reuse; general scopes excluded.',
 'SymbolTracker':'Selected diagnostic tracking callbacks and reported-diagnostic cache effects; general emit trackers excluded.',
 'EmitSupport':'Selected implicit undefined annotation predicate required by diagnostic declaration serialization; general emit unsupported.',
 'Mapper':'Original selected simple/array/deferred/function/merged/composite mapper models and factories; source inference mapper construction remains excluded.',
 'SymbolAccessibility':'Single-file scope/meaning/qualification/visited-table/cache and declaration accessibility algorithms for named data types. External alias/module/class scopes reject.',
 'EmitResolver':'Selected declaration visibility/link state for diagnostic reuse and symbol serialization; external emit/resolution API excluded.'}
hand={'CreateCore':'Scoped newChecker adaptation: fixed single-file/no-library Program inputs; omitted unused initialization state documented in departures.',
 'unsupported':'Authored explicit unsupported exception factory.',
 'initializeScalarOptions':'Authored fixed-option initialization separated from newChecker.',
 'compareTypeLists':'Selected CompareTypes recursive ordered scalar/named/composite type-list comparison with excluded source-generic payload guards.',
 'typeToStringByteString':'Authored raw-byte wrapper; public TypeToString decodes once.',
 'compareSymbols':'Forwarding boundary to original compareSymbolsWorker; memoization omitted as documented.',
 'cloneSymbolTable':'Managed clone of the original maps.Clone symbol-table operation.',
 'guardFiniteObject':'Authored excluded-payload guard before selected original relation algorithms.',
 'guardAbsentSignaturesAndIndexes':'Authored nonempty-callable guard; synthetic inherited indexes are selected.',
 'validateOwnedType':'Authored public checker-type ownership guard.',
 'aliasToTypeReferenceNode':'Authored factoring of original alias symbol/type-argument node construction.',
 'mostOriginal':'Forwarding boundary to actual printer EmitContext.MostOriginal.',
 'getLinks':'Managed link-cell retrieval; each counterpart retains its documented source store.',
 'emitTypeNodeWorker':'Authored factoring under original emitTypeNode precedence boundary.',
 'symbolToStringWithoutSymbolChain':'Forwarding boundary to symbolToStringEx with original DoNotIncludeSymbolChain|AllowAnyNodeKind flags.',
 'typeToStringNoTypeReduction':'Forwarding boundary to typeToStringEx with original NoTypeReduction-only flags.',
 'typeToStringWriteArrayAsGenericType':'Forwarding boundary to typeToStringEx with original WriteArrayAsGenericType-only flags.',
 'IsTypeAssignableTo':'Authored public adapter over upstream isTypeAssignableTo with checker ownership guard.'}
records=[]
paths=[root/'src/TypeScript.Scanner/Checker'/f'{name}.cs' for name in files]+list((root/'src/TypeScript.Scanner/Printer').glob('*.cs'))+list((root/'src/TypeScript.Scanner/Pseudochecker').glob('*.cs'))
pattern=re.compile(r'^[ \t]{4,8}(?:private|public|internal|protected)\s+(?:(?:static|override|virtual|sealed|async)\s+)*[\w<>?,.() ]+?\s+(\w+)\s*\([^;{=]*\)\s*(?:=>|\{)',re.M)
for path in paths:
 text=path.read_text();package=path.parent.name.lower()
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
   'includedBranches':hand.get(name,contracts.get(path.stem, 'Selected diagnostic-only pseudo type/declaration lookup or emit-state support; excluded grammar/options/general emit routes guard or are inaccessible from fixed entry.')),
   'retainedConditions':re.findall(r'\bif\s*\(([^\n]*)\)',body),
   'retainedCases':re.findall(r'case\s+([^:]+):',body),
   'explicitUnsupportedGuards':guards,
   'omittedByContract':'Other semantic branches excluded by authored admission/fixed options and explicit runtime guards; see DATA-TYPES-CONTRACT.md, OBJECT-CHECKER-PLAN.md, CHECKER-PLAN.md and checker-departures.md. This per-file scope is an inspection aid, not evidence that every upstream branch is ported.'})
result={'pin':pin,'status':'Selected partial function inventory; no full checker/emit claim','methodCount':len(records),'authoritative':'Reviewed C# hand ports; generator is not claimed for these algorithm files','functions':records}
output=root/'docs/checker-functions.json';output.write_text(json.dumps(result,indent=2)+'\n')
print(f'{len(records)} functions inventoried; unmapped: '+', '.join(sorted({r['name'] for r in records if r['upstream'] is None and r['name'] not in hand})))
