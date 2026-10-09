"""Inventory locked JSON/quoting dependency assertions by selected operations.

This is separate from the TsOptions original-body registry. No generated oracle
result is represented as a preserved source assertion or whole-library test pass.
"""
from pathlib import Path
import hashlib,json,re,subprocess
ROOT=Path(__file__).resolve().parents[1]
BASE=ROOT/'tools/.cache/go/src'
selected={
'TestConsumeWhitespace':'All JSON whitespace grammar rows; adapt consumed-prefix assertions at the finite grammar helper if exposed, otherwise document input/output/error differential adaptation.',
'TestConsumeLiteral':'All null/true/false grammar and malformed-prefix rows. Specialized fast-consumer results are separate from full parse results.',
'TestConsumeString':'Strict UTF8, raw bytes, escapes, surrogate pairs, consumed offsets and exact errors. Unquote replacement flags outside selected strict decode remain separate.',
'TestConsumeNumber':'Full number grammar rows and consumed/error terms; overflow typed conversion is a distinct selected semantic error domain.',
'TestParseHexUint16':'Selected four-hex escape parser domain; consumed index/error assertions need corresponding finite source helper.',
'TestParseUint':'Selected decimal bounds used by numeric decoding; bit-width assertion domains preserved where actual long/uint helpers are exposed.',
'TestAppendQuote':'Default JSON escaping and AllowInvalidUTF8 marshal subset; HTML/JS custom flags and buffer prefix are not automatically selected.',
'TestAppendNumber':'Float64 default marshal rows incl negative zero/extreme/NaN/Inf/error. Float32 and custom flags are not selected generated option values.',
'TestQuoteRune':'Exact invalid-character diagnostic quoting rows; interface is finite decoder error formatter, not strconv.Quote.',
'TestTruncatePointer':'Long JSON diagnostic pointer formatting rows if the mapped formatter truncates; full pointer structure API is not selected.',
'TestDecoder':'Value grammar and exact input bytes selected; streaming tokens/pointers/interleaving API omitted.',
'TestDecoderErrors':'Grammar/duplicate/UTF8/offset/pointer error rows selected; I/O and streaming operations omitted.',
'TestEncoder':'Marshal bytes/number/string output subset selected; streaming tokens/interleaving omitted.',
'TestEncoderErrors':'Selected serialization syntax/invalid bytes/float errors; caller-supplied stream token/state misuse omitted.',
'TestPointer':'Diagnostic JSON-pointer escaping subset selected; public Pointer traversal API omitted.',
'TestObjectNamespace':'Duplicate JSON property name semantics selected; private namespace storage topology not selected.',
'TestCoderMaxDepth':'Full raw parse/marshal depth-limit domain selected; mixed token stream state remains outside APIs.',
'TestValueMethods':'All strict whole-value IsValid literal predicates at existing ValidateValue; other methods not selected.',
'TestSemanticError':'Selected type/range error formatting, byte offsets and JSON pointers; arbitrary Go reflect type/method error cases omitted.',
'TestMarshal':'Only actual raw/ordered map/list/float64/options/static package field value domains; arbitrary Go type reflection/method/interface encoders excluded.',
'TestUnmarshal':'Only actual raw/ordered map/list/float64/options/static package field/Expected wrapper value domains; arbitrary Go reflection/custom methods excluded.',
'TestUnmarshalReuse':'Successive RawCompilerOptions map/header/partial-field state selected; arbitrary reflected Go structs not selected.',
'TestUnmarshalEOF':'Single complete raw value EOF/trailing bytes selected; decoder streaming API excluded.'}
rows=[]
paths=[BASE/'strconv/quote_test.go']
paths+=sorted((BASE/'encoding/json/internal/jsonwire').glob('*test.go'))
paths+=sorted((BASE/'encoding/json/jsontext').glob('*test.go'))
paths += [BASE/'encoding/json/v2/errors_test.go',BASE/'encoding/json/v2/arshal_test.go']
for file in paths:
 text=file.read_text();sha=hashlib.sha256(file.read_bytes()).hexdigest()
 for m in re.finditer(r'^func ((?:Test|Fuzz|Benchmark|Example)\w*)\(',text,re.M):
  name=m[1];kind='unit' if name.startswith('Test') else 'fuzz' if name.startswith('Fuzz') else 'benchmark' if name.startswith('Benchmark') else 'example'
  row={'package':str(file.parent.relative_to(BASE)),'name':name,'source':str(file.relative_to(ROOT)),'sourceSha256':sha,'line':text[:m.start()].count('\n')+1,'kind':kind,'managed':'not-run','native':'pending','xUnit':'pending'}
  if file.parent.name=='strconv':
   if name=='TestIsPrint':row.update(disposition='full-assertion-preserving-port',shared='validation/Configuration.Checks/QuoteChecks.cs',domain='0..unicode.MaxRune inclusive',managed='passed-isolated-central5b',evidence='artifacts/configuration-validation/build5b/quote-checks/report.json')
   elif name=='TestQuote':row.update(disposition='adapted-selected-operation-assertions',selectedAssertions='All eight Quote literal rows',omittedAssertions='Eight AppendQuote assertions; unused append API not implemented',shared='validation/Configuration.Checks/QuoteChecks.cs',managed='passed-isolated-central5b',evidence='artifacts/configuration-validation/build5b/quote-checks/report.json')
   else:row.update(disposition='unselected-API-or-performance',reason='Selected dependency implements Quote/IsPrint only; graphic/ASCII/rune/append/backquote/unquote APIs are outside this closure.')
  elif kind=='benchmark':row.update(disposition='performance-excluded')
  elif kind in ['fuzz','example']:row.update(disposition='seed-or-example-applicability-review-pending',reason='Selected raw codec grammar/marshal assertion domains must be separated from streaming/token/library API domains; no full fuzz/example claim.')
  elif name in selected:row.update(disposition='selected-assertion-subset-port-pending',selectedAssertions=selected[name],originalAssertionsPreserved=False)
  else:row.update(disposition='unselected-operation',reason='Streaming IO/token/reset/buffer/peek/state, arbitrary reflection or JSON canonicalization/API not exposed by selected standalone configuration codec; no whole-library claim.')
  rows.append(row)
# Final operation selection derives from actual finite counterpart/test APIs.
provenance_files=['validation/Configuration.Checks/json-original-provenance.json','validation/Configuration.Checks/json-original-text-provenance.json','validation/Configuration.Checks/json-original-arshal-provenance.json']
counts={}
qualified_counts={}
for file in provenance_files:
 provenance=json.loads((ROOT/file).read_text())
 for name,count in provenance['sourceTests'].items():
  matching=[source for source in provenance['sourceFiles'] if re.search(r'^func '+re.escape(name)+r'\(', (ROOT/source).read_text(), re.M)]
  if len(matching)!=1:raise RuntimeError('Missing/ambiguous original assertion source: '+name)
  key=(matching[0],name)
  if key in qualified_counts:raise RuntimeError('Duplicate qualified JSON assertion: '+str(key))
  qualified_counts[key]=count
  counts[name]=count
semantic_source='tools/.cache/go/src/encoding/json/v2/errors_test.go'
qualified_counts[(semantic_source,'TestSemanticError')]=3
counts['TestSemanticError']=3
unused={
 'TestParseUint':'The source decimal uint64 integer helper is not called or exposed by the selected codec. Float64 grammar/conversion and CLI int64 parsing use different producer operations; adding a uint parser only for this test would expand the dependency.',
 'TestUnmarshalReuse':'Every original assertion concerns backing reuse of an existing []byte, []int, map[string]string or pointer chain under reflected typed-target unmarshal. Generic Parse has no destination argument; RawCompilerOptions sequential field publication is a different operation, not a substitute.',
 'TestUnmarshalEOF':'Every original row invokes a custom unmarshaler/WithUnmarshalers callback returning io.EOF. No ordinary whole-buffer EOF assertion exists in this body; stream/custom hooks remain external.'}
omitted={
 'TestConsumeWhitespace':'None of the seven grammar/consumed-prefix assertions; Go t.Run mechanics adapted.',
 'TestConsumeLiteral':'Specialized ConsumeNull/True/False fast consumers absent; generic ConsumeLiteral count/error assertions retained for all25 rows.',
 'TestConsumeString':'Nonstrict string flags/count/error pass, ConsumeSimpleString and AppendUnquote replacement/partial-output assertions absent. Strict ConsumeString count/error assertions retained for all66 rows.',
 'TestConsumeNumber':'ConsumeSimpleNumber fast path absent; generic ConsumeNumber consumed-prefix/error assertions retained for all67 rows.',
 'TestParseHexUint16':'None of ten value/validity literal rows; existing source helper exposed internally only.',
 'TestAppendQuote':'HTML/JS flag rows and strict-invalidUTF8 output-prefix pass absent; all23 flags=0 AllowInvalidUTF8 output/error expectations retained through default scalar-string marshal.',
 'TestAppendNumber':'Float32 formatting assertions absent; all49 float64 raw-bit/input expected byte rows retained through default scalar-number marshal.',
 'TestQuoteRune':'None of eight character quoting rows; direct selected error helper.',
 'TestTruncatePointer':'None of12 truncation rows with original limit10.',
 'TestDecoder':'Token/TokenDelims/PeekKind/StackPointer and returned raw ReadValue preservation omitted. All25 default whole-value grammar success predicates retained through ValidateValue; no consumed-prefix or stream byte assertion claimed.',
 'TestDecoderErrors':'All52 first-call default ReadValue exact errors through lexical raw ReadValue validator required before selected Expected callbacks; generic Unmarshal remains separate. Later calls, stream EOF, options, PeekKind, InputOffset, StackPointer and unread-buffer assertions omitted; ValidateValue has no corresponding state/output API.',
 'TestEncoder':'Only6 default primitive null/bool/string compact byte assertions retained via raw parse+typed scalar marshal. Raw number lexeme preservation and raw object/array compaction, token streams, indentation and pointer/buffer state omitted.',
 'TestEncoderErrors':'All47 first-call default WriteValue errors retained through actual strict whole-buffer FormatRawValue producer; generic decode/ReadValue are separate. OutputOffset, unflushed partial output, token/state misuse, later stream calls and custom options omitted.',
 'TestPointer':'Four valid Parent+AppendToken roundtrip expectations retained with original parent/token literals. Parent/LastToken/Tokens/Contains/IsValid public pointer APIs and invalid UTF8 replacement roundtrip omitted.',
 'TestObjectNamespace':'49 original insert/remove operations applied to whole-object duplicate validation; insertion predicates asserted, removal reflected in next original input, sequence repeated in fresh scopes. Private namespace storage/list/map threshold, reset topology and one nonstrict invalidUTF8 replacement row omitted.',
 'TestCoderMaxDepth':'Four original SingleValue array/object grammar acceptance and exact depth-error expectations retained at depth10000/10001. ReadValue length/raw output and mixed token state/output assertions omitted; separate authored partial-tree/native depth evidence retains its distinct provenance.',
 'TestValueMethods':'All33 source IsValid literal predicate rows retained at ValidateValue. Compact/Indent/Canonicalize values, errors and mutable output operations omitted.',
 'TestSemanticError':'Three exact existing helper-compatible marshal int/unmarshal uint/float64 object+offset formatting rows retained. Default handle action/no-type/arbitrary reflect type/unknown-name relocation/nested syntactic-error unwrapping APIs absent or unselected. Typed-int1e1000 not relabeled float64.',
 'TestMarshal':'All18 default standard raw-value literal rows selected by generator: no custom options, named Go types, structs/methods, writer mode, canonicalization, multi-key unordered marshal byte assertions. Full/partial output and original error strings retained; typed nil slice adaptation is output equality only.',
 'TestUnmarshal':'Initially nil *any standard-domain rows only; original full/partial raw tree +error assertions retained. Prepopulated interfaces/typed destination merge and all reflection/custom unmarshalers/options omitted.'}
fuzz={
 'FuzzCompareUTF16':'All seeds/assertions target unselected UTF16 canonical ordering/CompareUTF16, not JSON grammar.',
 'Example_stringReplace':'Token-stream ReadToken/WriteToken/StackPointer and Multiline transform/stdout are outside the standalone codec.',
 'ExampleEscapeForHTML':'HTML/JS escaping flags, Multiline and struct reflection stdout are unselected.',
 'ExampleMultiline':'Multiline struct reflection stdout is unselected.',
 'FuzzCoder':'Random token/value stream recoding, Clone/Peek/end-delimiter/error state assertions are unselected. Seed inputs are not represented as preserved whole-buffer assertions.',
 'FuzzResumableDecoder':'FaultyBuffer/resumable I/O equivalence is unselected.',
 'FuzzValueFormat':'Finite33 strict-validity seed inputs are covered by the genuine TestValueMethods.IsValid source literal assertions; no extra fuzz body or execution claim. Fuzz reference-decoder comparison, random option choices, RFC7159 invalidUTF8 and Compact/Indent/Canonicalize remain unselected.'}
for row in rows:
 name=row['name']
 if name in unused:
  row.update(disposition='unselected-actual-source-operation',reason=unused[name],originalAssertionsPreserved=False)
  row.pop('selectedAssertions',None)
 elif (row['source'],name) in qualified_counts:
  row.update(disposition='adapted-selected-operation-assertions',selectedAssertionRows=qualified_counts[(row['source'],name)],omittedAssertions=omitted.get(name,fuzz.get(name,'')),shared='validation/Configuration.Checks/JsonOriginalChecks.cs',sharedCase=name,provenance=provenance_files,originalAssertionsPreserved=False,managed='source-ready-not-run')
 elif name in fuzz:
  row.update(disposition='unselected-actual-source-operation',reason=fuzz[name],originalAssertionsPreserved=False)
selected_rows={(row['source'],row['name']) for row in rows if row.get('shared')=='validation/Configuration.Checks/JsonOriginalChecks.cs'}
if selected_rows!=set(qualified_counts):raise RuntimeError('Qualified original JSON obligation inventory differs')
if any(row.get('shared')=='validation/Configuration.Checks/JsonOriginalChecks.cs' for row in rows if row['package']=='strconv'):raise RuntimeError('Unselected strconv API attributed to JSON source assertion')
report={'goVersion':subprocess.check_output([str(ROOT/'tools/.cache/go/bin/go'),'version']).decode().strip(),'scope':'Locked toolchain JSONv2 grammar/error/marshal and strconv Quote/IsPrint dependencies of configuration interpretation','contract':'docs/CONFIGURATION-CONTRACT.md','status':'source-assertion-selection-complete; managed/native/xUnit and independent review pending','dependencyWrapper':'upstream/TypeScript/tsc/internal/json/json.go','rule':'Preserve reachable original assertions at faithful helper or subsystem API; adapting call shape does not establish same consumed offsets, partial state or stream API assertions. Complete original body counts remain distinct from selected operation assertions.','currentProbeEvidence':['artifacts/configuration-validation/build5b/raw-json-paired/report.json','artifacts/configuration-validation/build5b/package-json-paired/report.json'],'probesAreOriginalAssertions':False,'entries':rows}
(ROOT/'docs/configuration-json-tests.json').write_text(json.dumps(report,indent=2)+'\n')
print(json.dumps({'inventoryEntries':len(rows),'fullOriginalBodies':1,'adaptedQuoteGroups':1,'pendingSelectedJsonFunctions':sum(r.get('disposition')=='selected-assertion-subset-port-pending' for r in rows),'jsonSharedGroups':len(counts)}))
