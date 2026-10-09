# Callable checking verification checkpoint

Current state: final postclosure build 24 is ready under the fixed
[contract](../CALLABLES-CONTRACT.md). All
[89 exact semantic comparisons](../../artifacts/callables-validation/managed24/report.json),
[701 checker xUnit cases](../../artifacts/callables-validation/callables-build24.trx),
and [21 original-Go and 21 managed order observations](../../artifacts/callables-validation/order24/report.json)
pass with zero failures or skips. Current source is
`32e043af7ec8419dbee4ce132be7cb72df84ef56a148d564c1a081dc989302df`.

The final [durable proof](../../validation/results/callables-verified.json) records
89/89 exact Native AOT comparisons, all 23,411 xUnit cases (701 checker), 304 original
numeric assertions, 43 admission controls, 10 API assertions, and 21 original-Go
plus 21 native order observations across eight sources. The full historical
foundation/binding/semantic cascade passes. The gate took 512.357 seconds with no
source, asset or historical-fixture drift. Assets are
`3af53ddfe83af1841091aa73608fb2fe1460adfdc8b8ef5f08628ba668e06a95`.

All source, tools and corpora remain frozen. Verification work is complete; no
further investigation, run or executable edit is pending. The earlier successful
80-case gate remains preserved as preclosure regression evidence only. Root owns
final assessment/state/commit. The authoritative acceptance is the actual final
durable proof linked above.

Baseline `de855ca31132bc95f84787f2a3a0b5ed3119c9c9`; pin
`fed0bf24149fb1ed36039212648bafdafc1ea10e`.

## Historical investigation and integration chronology

The sections below preserve the investigation and successive integration snapshots.
Their planning instructions, pending blockers and intermediate counts are historical;
the current-state header above is authoritative for resumption.

## Baseline and retained evidence

Clean working tree before this note. Read-only recomputation matched accepted
binding-completion source `1a5912ce84ce0908bb5d06365de4742fb74d0ae27335572eb55ff04e6011ee87`,
assets `4083b40e72903cded2a321ba26738b5ce256bd3ed7a39297934174b1e41cc080`
and all six native binaries in the [durable proof](../../validation/results/binding-completion-verified.json).
Historical scalar/object/data-type expected bytes stay unchanged.

## Baseline infrastructure and concrete omissions (before implementation)

The independent [Go oracle](../../tools/checker-oracle/main.go) uses actual
compiler.NewProgram, noLib/noResolve/SkipModuleResolution, ESNext,
strictNullChecks=true, strictBindCallApply=false, noImplicitAny=false for Legacy.
Callable now uses strictFunctionTypes/noImplicitThis/noImplicitAny=true as selected.
Source TS scripts only.
File diagnostics and global environment diagnostics are separate and ordered;
actual global count is compared, not forced to eight. Full flags, raw args,
message chains, related declarations, source/file identity are retained.
The [C# runner](../../validation/Checker.Validation/Runner.cs) shares this order:
parse/syntax observation, create/check, type+symbol queries, relation queries,
globals/diagnostics, selected graph observations. Graph traversal and TypeToString
can resolve caches. The new order must be explicit and mirrored exactly.

Both data graph encoders currently reject nonempty signature lists. The existing
[Signature model](../../src/TypeScript.Scanner/Checker/Types.cs) only supports
absent storage; mapper, composite and type-predicate fields are omitted. No
callable checking was preserved behind admission. Core guards include source
functions, parameter initialization, function flow, contextual signatures,
callable relations and function formatting. These require upstream algorithms,
not observer defaults or normalized-away lists.

## Proposed observation extension

Add opt-in callableObservations, new corpus/evidence and static AOT DTOs; leave
historical request/output bytes unchanged. Register signature IDs before recursion.
Compare complete Signature fields discovered independently in Go: flags,
minArgumentCount/resolvedMinArgumentCount, declaration, ordered parameters and
null state, typeParameters, thisParameter, resolvedReturnType/typePredicate,
target, mapper, isolatedSignatureType, composite kind+ordered signatures.
Compare parameter symbols/types, declaration ownership, call/construct signature
order and split, overload selection via actual GetResolvedSignature, return type,
resolved SignatureLinks(node) including effects/decorator slots, and contextual
parameter/context-checked/deferred state for selected function paths. Excluded
predicate/decorator/generic source forms do not authorize invented nulls.
Observe existing slots first; explicitly queried return/call APIs can then resolve
slots. Record repeated query identity and state after identical operations, not a
fictional simultaneous graph snapshot. Physical ID/page placement remains excluded;
logical cache presence/identity/order remains meaningful.

## Candidate interaction matrix for coordinator scope decision

1. Function types/declarations/expressions/arrows: typed and inferred returns,
   optional/default scalar parameters, missing/wrong argument diagnostics, lexical
   capture and forward/self-reference. Empty/no-return vs explicit void/nonvoid.
2. Contextual arrows/functions: identifier/scalar/object parameters and returns;
   higher-order callback argument context, wrong contextual property/return,
   nested callback context and optional parameter undefined behavior.
3. Callable data objects: function-valued properties, method/call signatures,
   named aliases/interfaces and inheritance/merges; valid and invalid data+
   signature relationships, preserving method-vs-function variance behavior.
4. Overloads: ordered specialized signatures, successful best candidate,
   too-few/too-many/wrong argument failures and related overload declarations;
   implementation compatibility and reversed source declaration order.
5. Composite/recursive interactions: callable union/intersection call selection,
   differing parameter/return shapes, recursive callable-property relationships,
   repeated relations/query identities and circular return-inference diagnostics.
6. Boundaries: independently authored controls for source generics, constructors/
   classes, rest/spread/destructuring, async/generators, explicit this/written predicates,
   arbitrary writes/branches, standard-library .call/.apply/.bind and other modes.
   Retire only controls actually admitted by selected contract, with positive and
   negative semantic counterparts. Invalid admitted calls must reach Go diagnostics.

Parameters/body restrictions need explicit coordinator selection: non-generic
identifier parameters and ordinary scalar/object/call expressions are coherent;
array/tuple rest and spread imply noLib array machinery, async implies Promise,
constructors imply separate signature/class paths, while .call/.apply/.bind rely
on library callable augmentation. Do not silently add fake libraries. Deferred
body checking/capture/return paths are still necessary for ordinary functions.

## Dependency audit refinements

Core recommends ordinary function declarations/expressions/arrows, non-generic
callable types/data methods, overload/composite signatures, identifier optional/
default parameters, nested closures and if/else/multiple return/throw bodies.
Rest/arrays/tuples/destructuring are proposed exclusions due real Array/ReadonlyArray
and iterator/library closure. Root still decides exact body/operator/options scope.
Ordinary boolean-return functions can infer actual TypePredicate payloads even if
written predicates are excluded; kind/index/raw name/type must remain observed.
Signature has14 upstream fields including numeric id, represented by canonical
identity rather than raw allocator counter. Parameter symbols generated for union
signatures can have nil declarations and synthesized arg names; do not demand an
original parameter declaration or coerce them into bound parameters.

Original checker direct tests are GetSymbolAtLocation (bundled-library host) and
GetTypeAtLocationOfTypeOnlyImportClause (module/import behavior), plus tracer state
mutation. None is an original callable-signature/call assertion. New callable
comparisons are authored actual-Go differential evidence, not full compiler tests.
Untracked oracle_semantic_utilities_test.go is authored prior instrumentation,
not an upstream original test. Existing semantic related/chain lists normalize
nil/empty; additional callable diagnostic-state observation should retain actual
nil states without retroactively altering historical outputs.

## Current executable milestones

Authored public CheckerProfile Legacy/Callable and admission are on disk; default
Legacy syntax/flags/rejection metadata remain preserved. Core initializes the
profile and owns the sole shared build. Callable TS grammar follows the contract;
35 independently authored negative controls and eight positive controls are stored.
New xUnit wrappers request full original graphs; none has run against C# yet.

Independent Go oracle compiled and ran **72 cases** (five tiny +67 C1–C6 matrix).
Actual public relation pairs are discovered from original syntax names and re-run
in both directions plus identity, without C#-derived expectations. The cases have
153 signature records, 2 composites and83 calls plus1003 repeated type/symbol query identities.
Full13 nonallocator Signature fields are reflected in original declaration order;
allocator identity becomes canonical record IDs. Real shared no-predicate sentinel
is an explicit reference. Two further predicate records show real inferred
predicates. Selected cachedSignatures has zero retained entries; that is not cache
branch proof. GetReturnType/GetTypePredicate and parameter type calls intentionally
resolve caches before each record snapshot; signatureLinks are captured before
structured type/member work. Work queue captures new cache-linked type identities.

Original sources include wrong-call2345, overload2769 with related declarations,
implementation2394, strict function2322 vs method bivariance, receiver2684,
recursive return7023, context/implicit-any7006, auto capture7034/7005, plus actual
'did you mean to call' related6212 and object-method property related6500.
Global errors are exact original results (normally eight2318; source builtin names
can change them). Nullable file/global/bind diagnostic channels and recursive
chain/related nil states are added opt-in; Legacy JSON schemas/fixtures stay intact.

Owned implementation paths:
[admission/profile](../../src/TypeScript.Scanner/Checker/Admission.cs),
[original callable observer](../../tools/checker-oracle/callable_graph.go),
[original private access](../../tools/checker-oracle/callable_access.go),
[static mirror](../../validation/Checker.Validation/CallableGraph.cs),
[corpus generator](../../tools/generate-callables-corpus.py),
[new corpus](../../validation/callables-corpus),
[new oracle tests](../../tests/Checker.Tests/CallableOracleTests.cs),
[new boundary tests](../../tests/Checker.Tests/CallableBoundaryTests.cs).

Next: compile cache/query/repeat bridges and independent schema/provenance review;
wait for sole-owner first library snapshot, then adapter-only five tiny comparison
and affected boundary/Legacy xUnit. Route compact first-difference packets before
full matrix. Counter-order negative controls and exact cache associations remain
planned. Final one frozen Linux x64 Native AOT/full regression cascade, no acceptance
until C1–C6, reviews and source/assets/native fingerprints are all closed.


## Flow ownership handoff

Root authorized exclusive [Flow.cs](../../src/TypeScript.Scanner/Checker/Flow.cs)
implementation to verification after the oracle checkpoint. Core retains
Checker-origin predicate creation/body refinement and sole shared builds.
Restored real pooled FlowState, shared-flow caches, branching/reduce-label/start
analysis, assignment/auto inference/capture marking, truthiness/equality/typeof/
discriminant/predicate-call narrowing, effects signatures and reachability.
Source loops/switch/arrays stay explicit excluded branches; no successful
placeholder. First source packet is waiting on the core coordinated compile.
No C# callable verdict yet. Latest Go 72 expected assets include opt-in repeated
query identity observations and exact no-predicate sentinel links.

Static [callable private access](../../src/TypeScript.Scanner/Checker/CallableVerificationAccess.cs) now mirrors actual logical signatureLinks, no-predicate sentinel and per-owner special cached-signature key associations, rejecting any unknown retained key family.

## Source closure and concrete original witnesses (current)

The hidden-this inherited signature specimen gives three actual nonnil target/mapper
pairs: `type A={}; interface B extends A{(x:number):number} interface C extends B{}
declare let f:C; const y=f(1);`. The earlier anonymous callable heritage case did
not instantiate signatures. Both remain distinct cases.

Ordinary then-callable objects passed to number parameters give 2345 with actual
related2773 (await suggestion); the same object returned from an arrow annotated
`()=>number` gives2322 with related6502. Recursive thenable input gives2345 without
the suggestion. Each original case reports eight2318 globals separately. No
async or standard library is injected. Parser-valid readonly method modifiers and
readonly/public identifier parameter modifiers give 1024/1042/2369; Callable
admits these invalid combinations to actual grammar checking.

SourceFileLinks now observes all eleven original fields. Initial attempted
two-field-only observation failed on real identifierCheckNodes registration even
with noUnused flags false; no zero assertion or worklist omission remains. Original
Go observation uses actual logical store and iterator order.

[Flow source closure](callables-flow.md) records explicit excluded branch guards
and remaining core dependencies. Source compilation is integration evidence only;
no C# callable semantic verdict or C5 acceptance yet.

## First managed integration packets

Build7 adapter compiled cleanly. Five tiny cases stopped at actual remaining
checkCollisionsForDeclarationName parameter guard; preserved all failures in
[report](../../artifacts/callables-validation/tiny-build7/report.json). Affected
xUnit ran 308 cases:295pass13 fail. Two Legacy globalThis failures were exception
Feature metadata regression; new positives exposed method/signature guards. The
authored async control exposed duplicate modifier routing in admission, repaired
without changing its expectation.

Build8 tiny still0/5: three nonempty-signature guards, one declaration-without-body
function flag guard, one pending binary dispatch. Source collision path progressed.
[Retained report](../../artifacts/callables-validation/tiny-build8/report.json)
contains request+expected outputs and failure dispositions. No expected values
were weakened; these are integration failures, not acceptance.

[Numeric provenance](../../validation/callables-corpus/numeric-provenance.json)
now lists ten newly required original jsnum functions,152literal and152Node
assertions (304 shared xUnit/native cases). Methods are being supplied by support;
these assertion bodies have not yet run. Native flag `--callable-numeric-tests`;
public authored APIs use `--callable-self-tests` (eight assertions). Support owns
composed final gate/recorder; it invokes one retained binding/foundation/semantic
cascade and uses its freshly published Checker.Validation binary.

## Build13 integration and expanded original support assertions

Isolated Release13 adapter ran five tiny cases:4exact, one actual typeNodeLinks
presence difference on contextual arrow body (not normalized away). Full first
73-case matrix gave28exact/45 fail;
[report](../../artifacts/callables-validation/matrix-build13/report.json) retains
each request/expected and actual output or exception. Required remaining core
guards include ambient/default/capture/augmentation paths; full source worklist
observation found missing interface/block registrations. Core has compact packets.

Affected xUnit filter `FullyQualifiedName!~CallableOracleTests` ran 612 cases:611 pass,
1 fail (caller declare variable grammar guard). This excludes all new semantic
OracleTests; CLI semantic evidence remains separate. All304 original newly-applicable
numeric literal+Node assertions and allLegacy cases passed.

Current corpus is76 (five tiny+71 matrix), adding original TS/noLib defineProperty
constant/callback descriptors (2304Object missing and2339function property missing),
function truthiness2774, and actual internal0n from bigint-typed logical truthiness.
The latter also retains mixed numeric2365 and unary2736; written bigint literals
still rejected. Callable leaf observer now compares actual PseudoBigInt Negative
and raw Base10Value, with managed default-null backing mapped to Go's empty string.
Tiny expected bytes stay unchanged.

Next: core consolidated snapshot; adapter-only targeted/full comparison when it
closes relevant failure classes. Support restores actual BigIntLiteral formatter
branch needed by the internal0n dependency. No new broad foundation runs, no native
acceptance yet.

## Build19 milestone and final source-review closures

Build19 reached76/76 exact semantic comparisons and688/688 all checker xUnit tests
(no skips/warnings), including every callable oracle row and304 shared original numeric
assertions. Reports: [managed graph](../../artifacts/callables-validation/matrix-build19/report.json)
and [xUnit](../../artifacts/callables-validation/callables-build19.trx).
The genuine synthetic nil-declaration parameter control now uses number/string arms;
number/number reuses an original parameter in actual Go, so that prior fixture premise
was corrected without changing semantic expectations. API assertions are now 10.

Independent source review adds three proven selected regressions, total 79 cases:
internal bigint-zero union truthiness, hidden-this nested MethodSignature filtering,
and missing thenable properties (2339with/without2773). Build19 explicitly guards these;
core repairs genuine original paths before freeze. All prior76expected rows remain
byte-identical after removing the three new rows; tiny hashes unchanged. Current Go
coverage 166 signatures/2 composites/88 calls/1085 repeat queries; cachedSignatures0 is honest.
[Per-field coverage](../callables-coverage.json) and [independent review](review-callables-verification.md)
record observable states and unverified caches.

Four existing identity-sensitive cases pass exact original/reversed-after-prefix order
controls: actualGo13outputs and managed 13outputs. Support integrated mandatory native
order execution into its gate/recorder. Flow's source-review initializer-null precondition
has been restored via actual isEmptyArrayAssignment; excluded array source stays guarded.
Next: consolidated source snapshot closes three review regressions, then final format/
source/asset freeze, one complete native cascade and durable evidence. No acceptance yet.

## Final managed snapshot and writer freeze

Final formatted build 22 sourcehash:
`470449302cdad708100ef6d82e12b005a1f31fce7f14b1065980808b84c25d2b`.
80/80 exact comparisons,692/692 all checker xUnit,43controls(8admit/35reject),10API
assertions and actualGo13+managed 13 order outputs passed. Reports:
[matrix](../../artifacts/callables-validation/matrix-build22/report.json),
[xUnit](../../artifacts/callables-validation/callables-build22.trx),
[order](../../artifacts/callables-validation/order-build22/report.json).
Root's lexical arguments capture witness follows genuine empty-object IArguments
fallback and eight separate globals. Final cache observer asserts discovered entries
match actual whole-map cardinality, so unobserved owners cannot be silently omitted.
No JSON schema change or previous semantic expectation rewrite follows that assertion.

All verification executable/tool/corpus files are now frozen; documentation-only
updates may continue. Source review has no remaining blocker. Support owns composed
native gate tools; root authorizes final execution. Reproduction and precise limits:
[verification report](../CALLABLES-VERIFICATION.md). Next action is exactly one guarded
full native/regression gate and success-only recorder, then docs/provenance refresh.

## Late independent cache/scope challenge: acceptance blocked

The frozen 80-case gate passed 529.436 seconds with no source/asset/historical drift,
but root's final source challenge found two additional required branches. Do not
emit accepted durable proof for that snapshot. Preserve gate/log/native reports as
an intermediate successful regression run before edits and repeat final acceptance
only after these source-grounded omissions close.

[Core frozen probes](../../artifacts/callables-validation/cache-review-frozen/report.json):
local interfaces inside ordinary function/arrow/object-method bodies are accepted
by actual Go but frozen managed/native throw the old outer-declaration guard.
A merged hidden-this interface with nested method type retains a real nonempty
anonymous-object instantiation cache; all current rich observers fail deliberately
at Unknown instantiated-object key family. This is an observer boundary gap, not
an original-Go checker panic or permission to discard key associations.

Core owns original outer-scope helper restoration. Verification must extend both
cache observers with full actual type-instantiation preimages and owner/mapper/
argument/alias identity, without cache-producing getters. Add exact sources and
repeated/order identity controls, then targeted managed, independent review,
refreeze and one full native/regression rerun. Until post-gate thaw is coordinated,
all source/tool/corpus files remain frozen; documentation alone may change.

## Post-gate closure integration (build 23)

The preclosure successful gate is preserved under
[before-cache-closure-470449302c](../../artifacts/callables-validation/before-cache-closure-470449302c/)
and remains regression evidence, not phase acceptance. Root authorized the targeted
source/asset thaw after preservation.

The corpus is now 89 cases: the original 80 plus local-interface function/arrow/method,
merged-hidden-this anonymous caches in both declaration orders, ordinary
SymbolConstructor interface/value merge, actual global Symbol and Symbol.for const
identity/let-var widening, and admitted if-let grammar1156. Both original tiny files
and the original 75 matrix request/expected prefixes retain their exact SHA256 bytes.

Actual Go exposes three anonymous cache entries in each merged-interface case:
the owner self-seed and two mapped values. Every actual key is reconstructed from
its full original preimage (ordered outer arguments, actual alias, single-signature
flag), checked against the actual stored key and lookup/value identity; anonymous
association output records these relationships. Both observers reject unrooted
arguments/aliases after the graph fixedpoint rather than register late values.
Unique symbol names validate their complete raw owner/name/unsigned-ID preimage,
then encode actual owner identity. The entire actual unique cache must be rooted,
and every symbol-key lookup/repeat must return the observed value.

[Managed23](../../artifacts/callables-validation/managed23-unique-fixed/report.json)
is88/89 exact. The sole remaining core failure is the legal SymbolConstructor
interface/value merge: the old duplicate-index checker called Members on a merged
VariableDeclaration. Core is restoring the original member-table/index-symbol
boundary. Two authored observer switches initially omitted UniqueESSymbolType and
were corrected without changing Go expectations. The new
[request-order control](../../artifacts/callables-validation/order23/report.json)
compares8 control sources in two sequences: actual Go 21/21 and managed 21/21 exact,
including both anonymous-cache and both unique-symbol sources. New const unique
symbols are mutually unassignable; each assigns to ordinary symbol while the
reverse does not. All separate global diagnostics remain unfiltered.

Next: core build 24; adapter-only refresh, all 89 exact comparisons and every
Checker.Tests row; final independent review/format/freeze before the support-owned
full Native AOT and historical regression gate. No further corpus growth without
an actual contract gap.

## Final post-closure managed snapshot and verification freeze

Core build 24 is clean. [All89 exact comparisons](../../artifacts/callables-validation/managed24/report.json)
pass, [changed-order Go and managed observations](../../artifacts/callables-validation/order24/report.json)
are21/21 each across8 controls/two sequences, and
[all 701 Checker.Tests](../../artifacts/callables-validation/callables-build24.trx)
pass with zero failures/skips/build warnings. This includes every89 CallableOracleTests
row,304 original numeric rows, all Legacy semantic rows,43 boundary controls and10
public API assertions. Legal SymbolConstructor now follows the original index-symbol
lookup rather than iterating incompatible merged declarations. Both genuine Unique
cases and both nonempty anonymous caches compare exact graph/name/preimage/map identity.

All owned executable/source/tools/corpora are now frozen. No further writes there
are planned. Only documentation status/evidence updates may follow. Root coordinates
all-writer freeze and authorizes the support-owned final full Native AOT gate; the
preclosure successful gate remains preserved and must not become acceptance proof.
