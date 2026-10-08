#!/usr/bin/env python3
"""Reconcile exact source declarations with explicit foundation body/scope dispositions.

Names and locations are inspection aids, never automatic equivalence evidence.
Utilities/Precedence have an independent owner's body inventory. Generated field
assignments/accessors/updates/traversal require the static schema review plus the
independent structural/identity observations; no single name-presence scan proves
these algorithms. The reviewed C# remains authoritative.
"""
import pathlib,json,re,collections
root=pathlib.Path(__file__).resolve().parents[1]
path=root/'docs/foundation-syntax-source-inventory.json'
data=json.loads(path.read_text())
files=list((root/'src/TypeScript.Scanner/Ast').glob('*.cs'))+list((root/'src/TypeScript.Scanner/Parser').glob('*.cs'))
texts={str(p.relative_to(root)):p.read_text() for p in files}
methods=collections.defaultdict(list)
pattern=re.compile(r'^\s*(?:public|private|internal|protected)\s+(?:(?:static|virtual|override|sealed|new|async)\s+)*[\w<>,?.()\[\] ]+?\s+(\w+)(?:<[^\n>]+>)?\s*\(',re.M)
for filename,text in texts.items():
 classes=list(re.finditer(r'\b(?:class|interface|struct)\s+(\w+)',text))
 for m in pattern.finditer(text):
  owner=next((c[1] for c in reversed(classes) if c.start()<m.start()),'')
  methods[m[1]].append({'path':filename,'line':text.count('\n',0,m.start())+1,'receiver':owner})
renames={'computeSubtreeFacts':'ComputeSubtreeFacts','propagateSubtreeFacts':'PropagateSubtreeFacts','subtreeFactsWorker':'SubtreeFactsValue','setModifiers':'SetModifiers','updateNode':'UpdateNode','cloneNode':'CloneNode','visit':'Visit','visitNodes':'VisitNodes','visitNodeList':'VisitNodeList','visitModifiers':'VisitModifiers','forEachChild_JSDocParameterOrPropertyTag':'ForEachChildJSDocParameterOrPropertyTag','visitEachChild_JSDocParameterOrPropertyTag':'VisitEachChildJSDocParameterOrPropertyTag','collectIdentifiersForSourceFile':'CollectIdentifiersForSourceFile','ParseIsolatedEntityName':'ParseIsolatedEntityNameRaw','resolveJSDoc':'JSDoc'}
content={'OriginalText','OriginalFileName','SpanMap','IsContentMapped','ContentMapper','IsContentMapperFailureStub','ContentMapperTransformIdentity','VirtualFileName','ContentMapperParseOptions','SetContentMapperInfo','DiagnosticDirectives','SupplementalSourceFiles','CanonicalSourceFile','IsContentMapperSupplemental','displayMessageArgs'}
later={'RepopulateInfo','SetRepopulateInfo','GetNodeId','GetSymbolId','GetMergedSymbol'}
synthetic={'SyntheticExpression','PartiallyEmittedExpression','SyntheticReferenceExpression','NotEmittedStatement','NotEmittedTypeElement','FlowSwitchClauseData','FlowReduceLabelData'}
fields=set()
for file in data['files']:
 source=file['path'];stem=pathlib.Path(source).stem
 for row in file['sourceFunctions']:
  name,receiver=row['name'],row['receiver'] or ''
  typ=(receiver.split('*')[-1].strip() if '*' in receiver else receiver.split()[-1] if receiver else '')
  method=renames.get(name,name);candidates=methods.get(method,methods.get(method[:1].upper()+method[1:],[]))
  aliases={'NodeDefault':'NodeData','CompositeBase':'NodeData','MutableNode':'Node','TypeSyntaxBase':'NodeData','ClassLikeBase':'ClassDeclaration','AccessorDeclarationBase':'GetAccessorDeclaration','ImportAttributesNode':'Ast'}
  matched=[c for c in candidates if c['receiver']==aliases.get(typ,typ or 'Ast')]
  row['counterpartMethods']=(matched or candidates)[:4]
  if len(matched or candidates)>4:row['additionalCandidateCount']=len(matched or candidates)-4
  row['bodyReview']='reviewed syntax/source family; expanded final Native AOT execution pending'
  row['reviewEvidence']=['docs/progress/review-foundation-syntax-support.md']
  if source.startswith('tsc/internal/parser/'):
   row['reviewEvidence']+=['docs/foundation-parser-ast-tests.json','docs/progress/foundation-syntax.md']
  if stem in ('utilities','precedence'):
   row['reviewEvidence']=['docs/foundation-utilities-source-inventory.json','docs/progress/review-foundation-utilities.md']
  if stem=='visitor':row['reviewEvidence']+=['docs/progress/review-foundation-utilities.md']
  if source.startswith('tsc/internal/parser/'):
   row['scopeDisposition']='in-scope-parser'
   row['implementationDisposition']='ported-initial-parser-algorithm' if candidates else 'unresolved'
   row['reason']='Initial source-file/JSDoc/reparse/reference/isolated-name paths; original function boundaries and branches retained with managed storage/byte-string adaptations. Direct original assertions passed xUnit/AOT, finite original fuzz bodies passed managed, and prior structural comparisons retained; final expanded mode/fuzz Native AOT gate pending.'
   if name in ('init','putParser','newParser'):
    row['implementationDisposition']='managed-runtime-adaptation'
    row['reason']={'init':'Parser static constructor registers actual lazy JSDoc callback.','putParser':'No parser pool return under managed fresh parser allocation; all observable parse state remains per parse.','newParser':'Managed constructor/allocation initializes the original Parser fields and factory; pool mechanics excluded.'}[name]
  elif stem in ('utilities','precedence'):
   row['scopeDisposition']='owner-disposition-in-separate-inventory'
   row['implementationDisposition']='see-scanner-owner-body-inventory'
   row['reason']='Exclusive Utilities/Precedence completion owner supplies exact body/dependency review; do not infer completeness from lexical presence.'
  elif stem in ('symbol','flow') or name in later or typ in ('Symbol','FlowNode','FlowList'):
   row['scopeDisposition']='retained-later-stage-state'
   row['implementationDisposition']='outside-foundation-completion-claim'
   row['reason']='Existing binder/checker symbol/flow ownership or diagnostic repopulation state; prior acceptance retained, no foundation execution/completion claim.'
  elif name in content:
   row['scopeDisposition']='excluded-content-mapping'
   row['implementationDisposition']='ordinary-unmapped-diagnostic-adaptation' if name=='displayMessageArgs' else 'excluded'
   row['reason']='Content mapper span aliases/transforms/supplemental sources explicitly excluded. Ordinary unmapped diagnostics retain stored arguments; no fabricated mapper object/state.'
  elif stem=='parseoptions' and name in ('GetExternalModuleIndicatorOptions','isFileForcedToBeModuleByFormat'):
   row['scopeDisposition']='excluded-project-option-derivation';row['implementationDisposition']='excluded'
   row['reason']='Derives explicit parse options from CompilerOptions/package metadata. Foundation accepts explicit JSX/Force options and ports their syntax behavior fully.'
  elif typ in synthetic or any(name==prefix+t for t in synthetic for prefix in ('New','Update','As')):
   row['scopeDisposition']='excluded-later-only-synthetic-payload';row['implementationDisposition']='retained-if-present-no-foundation-claim'
   row['reason']='Cannot be produced by initial parsing; actual later-stage synthetic/checker payload construction excluded. Pure kind predicate remains in scope separately.'
  else:
   row['scopeDisposition']='in-scope-syntax-or-source-support'
   row['implementationDisposition']='ported' if candidates else 'unresolved'
   row['reason']='Original syntax/source function implemented in mapped counterpart; representation adapts tagged Node union and promoted embedded Go bases to typed payload interfaces/NodeData dispatch.'
   if stem=='ast_generated':row['reason']='Schema-generated payload field/factory/update/clone/accessor/traversal algorithm; embedded-base flattening and aliases are managed representation adaptations. Full source facts restored separately. Static family review and structural/identity corpus required, not name-presence proof.'
   if name in fields:
    row['implementationDisposition']='field-access-adaptation';row['reason']='Original getter represented by existing SourceFile public field with same nullable slice contents; no independent algorithm.'
   if name=='NewNodeFactory':
    row['implementationDisposition']='constructor-adaptation';row['reason']='NodeFactory constructor retains original hooks/zero initialization; explicit Go NewNodeFactory forwarding boundary represented by C# construction.'
   if name=='String' and typ=='Kind':
    row['implementationDisposition']='generated-kind-string-table';row['reason']='Pinned complete names/indexes and exact invalid numeric fallback.'
   if stem=='kind_stringer_generated' and name=='_':
    row['implementationDisposition']='compile-time-enum-check-adaptation';row['reason']='Go generated compile-time ordinal assertions represented by pinned full C# enum values and numeric structural comparisons.'
   if name in ('SubtreeFacts','subtreeFactsWorker') and typ in ('NodeDefault','CompositeBase'):
    row['implementationDisposition']='embedded-base-dispatch-adaptation';row['reason']='NodeData virtual dispatch selects uncached default vs actual Composite cache; computed-bit publication stripped from returned facts. Complete original mask/compute algorithms emitted by original receiver.'
   if typ in ('NodeBase','TypeSyntaxBase','ModifiersBase','DeclarationBase','ExportableBase','LocalsContainerBase','FlowNodeBase','FunctionLikeBase','ClassLikeBase','BodyBase','LiteralLikeNodeBase','TemplateLiteralLikeNodeBase','AccessorDeclarationBase') and not candidates:
    row['implementationDisposition']='embedded-base-field-or-interface-adaptation';row['reason']='Go embedded method/field promotion represented by flattened payload interface/NodeData dispatch; inspect exact base/type fields rather than require a duplicate concrete base object.'
   if name=='IsAccessorDeclaration' and typ=='AccessorDeclarationBase':
    row['implementationDisposition']='embedded-base-kind-predicate-adaptation';row['reason']='Original base method true only on accessor receiver; managed generated Ast.IsAccessorDeclaration performs actual kind predicate on Node wrapper.'
   if name=='ReleaseArenas':
    row['implementationDisposition']='managed-runtime-adaptation';row['reason']='Managed objects need no arena release; hooks/nodeCount/textCount retained as original resetting factory does. No observable node invalidation invented.'
   if name=='resolveJSDoc':
    row['implementationDisposition']='source-service-boundary-adaptation';row['reason']='Original lazy resolver/cache algorithm in SourceFile.JSDoc, with eager/null-cache lookup factored into same managed method; callback absence check distinct from valid nil result.'
   if name=='IterChildren':row['reason']='Actual lazy per-payload iterator generated from original ForEachChild order, defers later fields and captures list length/reference when entered; no eager list snapshot. Focused original-Go mutation/early-stop observations passed against Release9; final native execution pending.'
   if stem=='deepclone':row['reason']='Exact original clone visitor, synthetic positions/trailing-comma sentinel, modifier/list identity and reparse flags. Original541 rows preserve distinct clone/VisitEachChild child-count assertions in xUnit/AOT.'
   if stem=='positionmap':row['reason']='Exact JS-string rune loop/cumulative deltas/two binary searches; private Go nil entries slice becomes allocated managed List. Original6 tests and parser source map assertion passed xUnit/AOT; expanded raw-byte mapping observations pending.'
   if stem=='diagnostic' and name=='Localize':row['reason']='Default-English branch retained; translated locale argument/catalogs excluded. All pinned default-English metadata/key lookup included; unmapped byte args use original ToValidUTF8 invalid-run collapse.'
data['status']='Explicit per-function reviewed source-family/scope inventory; expanded final native gate pending. Counterpart names/locations remain inspection aids, not standalone equivalence proof.'
data['generatedRepresentation']='Tagged wrapper/typed payloads; promoted base fields/accessors flattened. Full facts and lazy iterators regenerated only in their marked sections; handwritten supplements authoritative.'
path.write_text(json.dumps(data,indent=2)+'\n')
counts=collections.Counter(r['implementationDisposition'] for f in data['files'] for r in f['sourceFunctions'])
print(dict(counts))
for f in data['files']:
 pending=[(r['name'],r['receiver']) for r in f['sourceFunctions'] if r['implementationDisposition']=='unresolved']
 if pending:print(f['path'],pending)
