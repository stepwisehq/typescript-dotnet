# Independent binding support/assertion review

Baseline `2886d47`; pinned TypeScript
`fed0bf24149fb1ed36039212648bafdafc1ea10e`. Independent review by binder algorithm owner. A later reviewed scanner dependency
nil-state repair is noted below; no shared-library builds by this owner. Runtime
acceptance pending.

Reviewed [shared support assertions](../../validation/BindingCompletion.Checks/BindingSupportChecks.cs),
[thin xUnit wrapper](../../tests/Binder.Tests/BindingSupportTests.cs),
[AST source state](../../src/TypeScript.Scanner/Ast/Ast.cs),
[symbol helpers](../../src/TypeScript.Scanner/Ast/Symbol.cs),
[flow models](../../src/TypeScript.Scanner/Ast/Flow.cs), and
[support inventory](../binding-completion-support-inventory.json).

## Findings and current dispositions

1. **Concurrent waiter observation gap, source repair reviewed.** Original success/failure
   groups launched competitors then released immediately, and sampled state only after
   joining the first callback. A no-wait implementation could pass these observations.
   Support added dedicated started callers, a bounded any-return-before-release
   observation and bounded completion. Immediate per-caller publication reads (success) and callback-completion-marker
   reads (failure) are now present in both source bodies. The corrected body was
   reread; expected fixtures and actual execution remain pending. Finite scheduling cannot prove universal blocking; preserve that
   limitation and retain the source synchronization proof. No underlying library defect
   identified: the non-reentrant gate already blocks until callback exit.
2. **Expected-fixture provenance pending.** Run requires an embedded independently
   generated Go fixture and fails explicitly if missing. At this review boundary the
   fixture generation/dispatch is verifier-owned work in progress, so the eight groups are
   authored observations, not executed/passed assertions or original upstream tests.
   The coordinator subsequently delegated the Go support producer to this reviewer.
   [support.go](../../tools/binding-completion-oracle/support.go) invokes actual upstream
   APIs; support owner independently reviews it. Recheck fixture payload equality and
   source hashes before acceptance; this reviewer cannot self-sign producer independence.

## Source and observation agreement

- Pinned ast.go:2897 wraps the callback in sync.Once.Do and stores IsBound only after
  success. Go once.go defer marks completion even on panic; no retry and retained
  partial writes follow from this boundary. Managed attempted state is protected by
  the non-reentrant gate and IsBound is volatile-published only after success. Failure
  group catches the precise exception, then calls both BindOnce and Binder again and
  checks no retry/no rollback; it does not treat failure as bound success.
- Tables compares nil to allocated-empty, repeated getter identity and independent
  lazy slots. Seeded cross-owner sharing is explicitly an API/sensitivity control.
  The natural empty-locals class witness and nil empty-type-member witness are separate.
- Symbols samples owning files, prototype parent recovery, transient non-ownership,
  local/export flag links, static/private/raw names and zero binder CheckFlags. The
  transient symbol with borrowed declarations is a deliberately excluded checker-style
  ownership control, not a purported binder-created transient.
- Atomic IDs compares stable/nonzero same-object identity under concurrent reads and
  distinct-object identity, avoiding equality of process-global counter values.
- Synthetic flow factories compare actual kind/span/flags, indices, targets and linked
  identity. Seeded sharing is labeled separately from the natural IIFE tail producer.
- Source clone/changed update observes fresh binding state and shared syntax/metadata;
  unchanged update observes retained identity. It does not promise rebinding an arbitrary
  shallow clone whose shared descendants already contain binding state.
- File isolation compares separately parsed equal-name files and repeat binding without
  sorting away declaration/flow identity. Shared assertion bodies serve both xUnit and
  native; no xUnit or dynamic reflection dependency exists in the bodies. Native execution
  still must establish AOT compatibility.

## Producer absence/presence challenge

Distinct published table-owner slots cannot alias on fresh parsed inputs: binder.go
whole Members assignments (:1003, :1638) allocate new maps; ast/utilities.go:46 creates
one new map for one nil slot; remaining binder writes mutate entries of that slot.
There is no whole-table copy. CommonJS :1050 shares Symbol values in a separately
allocated export-equals table. Multiple declarations/nodes pointing to one Symbol have
one table owner, not distinct aliased owners. This depends on initial parser binding
slots being nil and excludes preseeded, checker and already-bound AST mutation.
Support independently confirms the source closure. Absence remains source-derived,
not naturally exercised alias production.

Allocated-empty source Locals is witnessed by original Go `class {}`. The IIFE
`(function(){try{if(a)return 1;x=2;}finally{x=3;}return 4;})();z;` has a natural
shared FlowList tail because combineFlowLists retains its tail and the return reduce
snapshot publishes it too. Original Go reports seven cells, one with two incoming
references, and two reduce payloads. Without the trailing use that flow is not rooted.

Remaining: final independent fixture review, repaired concurrent observations, campaign
managed/native outcomes and final delta. No runtime-source blocker currently identified.

## Original-Go producer handoff

Eight observation groups are independently authored in the isolated Go producer.
Go map slots are non-comparable: actual reflect.Value.UnsafePointer identity equality
(the canonical binder oracle's existing mechanism) establishes the same claims as
managed ReferenceEquals without extra table mutations. No raw map addresses or
process-global counter values are serialized. Source support review challenged a
weaker first-draft marker-read witness which could not distinguish copied maps;
that draft was replaced before execution. This is an observer repair, not a binder
runtime defect. All producer output expectations still require original-Go execution.

Support independently reread the corrected Go producer and confirmed actual map
identity, all eight source inputs/output keys and concurrent immediate-read ordering;
no producer-source blocker identified. Fixture build/execution remains verifier-owned
and pending. Next review is new cold graph/protocol and public helper fixtures; no
unnecessary rereview of unchanged core is required.

## Cold observer review and concrete broad finding

Reviewed new completion dispatcher/transform, static native contracts and streamed
comparer. Primary binding uses a separate parse untouched by observations; the unbound
baseline is a different parse. Repeated binding compares concrete node/symbol/table/
flow/list identities and snapshot bytes before ownership/name/container and explicit
lazy-JSDoc observations. Nullable binding-diagnostic channel and real metadata states
are added without changing historical graph schema; full flags/ordered arrays remain
compared. Detailed observer review continues while broad packets are retained.

**Actual shared observer failure (broad case10003).** In private-name destructuring,
a PropertyAssignment inside an object literal has symbol.Parent equal to the object
literal symbol, while binder.go:320 generates its raw private name from lexical
GetContainingClass(declaration).Symbol. Legacy Name/SortKey assumed Parent was always
the generating class and rejected both real Go/C# results. Requested correction is
strict validation/canonicalization against the lexical generating owner, preserving
the separate actual Parent edge and exact raw name preimage. No binder algorithm
change, assertion removal or reference failure disposition is justified. Observer
owner repair and actual replay remain pending.

Original Go has now generated both support/helper fixtures. Verifier reports first
24 cold/prewarmed cases, all 8 support groups and all 4 helper groups managed-exact.
These are integration results, not native/final acceptance. Support independently
reviewed both Go producers (15 private rows, 12 prologue rows, 9 value sequences and
3 container inputs), with no unresolved producer-source defect.

Cold snapshot nuance: legacy syntax serialization calls SourceFile.Imports after
binding and may populate syntax caches. It does not touch the primary file before
binding or alter the binder output under comparison. Document this post-bind
observation instead of promising every AST cache remains untouched. Go explicitly
asserts nil RepopulateInfo; that checker-only field is omitted from the managed
model, and source binding never sets it. This absence needs a precise source
disposition, not a claim that the protocol encodes a nonexistent field.

## Completion repair and nullable metadata delta

The completion-only lexical private owner repair was reread in both composed Go
and static native observers. It matches pinned binder.go:320/:326, validates the
complete raw prefix/actual-owner-ID/suffix, sorts by that same owner's syntax
identity, and retains the independent actual Symbol.Parent reference. Legacy Go
observer source/default native mode remain unchanged. No relaxed equality or
binder algorithm modification was needed; affected replay remains pending.

MetadataNulls compares real collection presence after the same snapshot stage in
both runtimes; Imports is deliberately observed before this nullable sample. It
exposed a scanner dependency defect: the sole upstream comment-directive mutation
is append (scanner.go:1007), while defaultScanner, Reset (:238), SetText (:397) and
ScannerState Mark/Rewind (:293/:297) preserve nil until that append. C# formerly
allocated an empty list at default/reset/text assignment and lost nil in state
snapshots. Nullable backing/getter and nullable saved snapshots restore this state;
parser's existing ?.ToList forwarding needs no substitute normalization. Public
absence assertions now require null; legacy normalized JSON projection remains []
through an explicit observer loop. Independent lifecycle fixtures/shared assertions
and final execution are verifier-owned. The initial scanner emitter is a documented
temporary scaffold, so the reviewed source remains authoritative.

Four exported helper shared C# bodies were compared against the independently
reviewed original-Go producer: legitimate parsed node selection, exact raw name
bytes and signed64 IDs, prologue stopping/escape semantics, ordered value priority,
and DFS container kind/parent flags agree. Inputs are fixture metadata; observed
outputs are actual APIs, not expected values echoed from fixtures. Native AOT uses
friend atomic access and static JSON metadata, with no reflection-based encoder.
No additional source-review blocker found in these helper paths.

## Final consumer/source review and writer freeze

The final DirectiveLifecycle consumer has the same 16 actual-scanner operation
stages as the independent Go producer, including absent new/text/reset state,
present and absent saved states, replay and reuse. Null is a separate observed bit;
normalizing the contents array does not erase presence. The seeded sensitivity
consumer encodes a full binding graph with three distinct table-owner slots sharing
one empty table, a shared linked tail and cyclic flow. It uses static source-
generated JSON metadata and matches the independently authored Go producer's
operations. SeededOnly is explicit; this is observer sensitivity, not a natural
binder-production claim.

Completion private-name preimage formatting now also preserves the signed native
int cast (unchecked managed long); historical default formatting is unchanged.
All shared checks/library/test projects are in the source guard, and tools/fixtures
are in the asset guard. No source-review blocker remains. This owner's source/tool/
corpus writes are frozen; documentation may record later evidence. The consolidated Release library/checks build passes with zero warnings/errors.
Five authored test nullable warnings were resolved with explicit array-presence
assertions before refreezing; no runtime change was needed. Affected metadata replay
and final Native AOT gate remain pending.


## Gate-discovered helper counter leak

The first final gate failed on the targeted private-destructuring row, not a
binder or SymbolName algorithm discrepancy. Pinned ast/symbol.go:95 returns the
literal source private name only for private class-element ValueDeclaration;
otherwise it returns symbol.Name. The malformed object-literal PropertyAssignment
therefore correctly returns raw FE#ownerID@#foo. The new helper projection encoded
that process-dependent raw ID directly instead of retaining its validated owner
relationship. Completion graph names already normalize that relationship.

An independent original-Go order experiment ran the witness alone and after
`class CounterPrefix { #p=1; }` in separate processes. Every graph/metadata field
matched except symbolHelpers[7].displayNameBase64: FE#1@#foo versus FE#2@#foo.
The [pre-fix sensitivity record](../../artifacts/binding-completion-validation/review-display-order-before.json)
retains exact requests, differences and oracle binary hash. This demonstrates the
old observer defect without changing runtime code or inferring a source mismatch.

Approved repair: pass the actual returned Ast.SymbolName string through strict
BoundName validation. Ordinary display bytes, including class-private #foo, remain
raw; genuine raw private/pattern names require the complete owner preimage and
canonical owner identity. Actual Symbol.Parent stays independently encoded. New
schema/source review and order-varying post-fix evidence remain pending; no runtime
or owned executable file is reopened by this review.


Independent review caught a too-weak first OrderSensitivity regression: it used
only binding-pattern `const { #foo:x }=this`, not the assignment object-literal
PropertyAssignment which produces the raw helper name. The preserved broken oracle
also passed that proposed before/after-eight-prefixes test; its five helper names
were all ordinary. Verifier was asked to add the actual assignment destructuring
and retain both a genuine private descriptor and raw #foo display assertions before
regenerating/re-freezing. The schema implementation itself was reread and correctly
passes the actual API string through strict owner validation, rather than replacing
SymbolName or dropping raw-name equality. No runtime source change is justified.


The repaired actual-API DisplayName BoundName descriptor and corrected shared
OrderSensitivity source were reread. Assignment destructuring is now included in
both producers/consumers, with explicit generated-private and ordinary-#foo display
presence bits. The [post-fix negative-control record](../../artifacts/binding-completion-validation/review-display-order-after.json)
runs both exact original case10003 and the exact shared source alone versus after
eight private-class prefix requests: the preserved broken observer fails equality
in each case; the repaired original-Go observer passes whole canonical equality in
each case. Each repaired result has one genuine private descriptor and one raw
ordinary private display. Raw preimages still validate lexical owner, and actual
Parent remains a separate graph edge. No runtime algorithm change or assertion
normalization beyond process-counter owner canonicalization occurred. Own executable
files remain frozen; the corrected gate must execute again before acceptance.

Four unobserved payload dispositions were independently source-challenged without
new experiment: NotEmittedStatement and NotEmittedTypeElement have no parser factory
caller and are transformer/decoder-only on fresh-source input. Attached raw JSDoc
is not a ForEachChild binding edge; the binder's implicitly-exported JSDoc predicate
only recognizes reparsed declarations/modules. Raw JSDocSignature callback/overload
nodes are replaced by ordinary declaration/function-type nodes (reparser.go:146).
Raw JSDocTypeLiteral property/typedef routes become ordinary TypeLiteral/property
nodes (reparser.go:244/:274). Historical bound evidence observes two raw
JSDocSignature nodes with null bound fields. JSDocTypeLiteral.Symbol remains
runtime-unobserved/source-reviewed; syntax observation does not establish a bound
runtime observation. No counterexample producer path was identified.
