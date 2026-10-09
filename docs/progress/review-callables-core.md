# Callable core review — accepted bounded completion

Pinned source: `fed0bf24149fb1ed36039212648bafdafc1ea10e`. Contract:
[CALLABLES-CONTRACT](../CALLABLES-CONTRACT.md). Final source and observer review is
closed with no unresolved blocker for the selected contract. This is bounded
source fidelity and verification, not whole-checker parity.

The [durable proof](../../validation/results/callables-verified.json) records the
512.357-second exact-source Native AOT gate: 89 callable cases, all 23,411 xUnit
cases (701 checker), 304 numeric assertions, 43 admission controls, 10 API
assertions, 21 request-order rows per runtime and all retained native regressions.
Source, asset and historical-proof drift were all false.

Final review covered signature/context/return/predicate/Flow/operator/receiver paths,
nested declarations, actual builtin lookup and identity/cache producers. The late
local-interface/anonymous-cache, unique-symbol, duplicate-index nil-read and direct
if-body grammar findings are source-fixed and verified. Anonymous key associations
and strict unique owner/name/cache observations were independently reviewed, including
rejection of unrooted post-fixedpoint references. No active review or implementation
work remains. Source, tools and corpus are frozen.

The first successful gate was deliberately withheld from acceptance after missed
source paths were found; its preserved snapshot remains regression-only evidence.
The following notes retain historical review chronology, including the premature
closure and reopened findings. Their pending statuses are superseded by this header.

## Historical review chronology

Reviewed the selected branches of [signature construction and return resolution](../../src/TypeScript.Scanner/Checker/Checker.cs),
against `checker.go` functions `newSignature`, `getSignaturesOfSymbol`,
`getSignatureFromDeclaration`, `getReturnTypeOfSignature`,
`getNonCircularReturnTypeOfSignature`, `getReturnTypeFromBody`,
`checkAndAggregateReturnExpressionTypes`, `cloneSignature`, `instantiateSignatureEx`,
`getSingleSignature`, and `combineUnionOrIntersectionMemberSignatures`/parameter/this
helpers. No unresolved finding in this batch. Ordered overload exclusion, explicit
`this` separation, minimum arity, deferred return publication, recursion diagnostics,
target/mapper/composite propagation, void/never/implicit-undefined inference and
transient parameter symbols retain the upstream decisions. Constructor/rest/generic/
async branches remain explicit guards under the fixed admission boundary.

The earlier broad `instantiateSignatures` guard was reported to core and replaced
with its original `instantiateList(..., instantiateSignature)` boundary. The natural
hidden-this inherited-call-signature fixture now exercises this path in the Go corpus;
C# runtime comparison is pending. Managed append copies preserve the source slice
header lengths when extending composite signature lists.

Inference mapper allocation audit: `mapper.go` inference/backreference mappers are
allocated by `newInferenceContextWorker` or inferred type-parameter defaults.
`checker.go` allocation sites are generic overload candidates, generic signature
instantiation and conditional-type inference; `relater.go` allocation is conditional
inference. These are guarded source-excluded branches. Ordinary nongeneric contextual
callbacks use the actual nil-context arm and hidden-this inheritance uses ordinary
simple/composite mappers. No successful inference placeholder is introduced.

Remaining review: stable overload applicability/error paths, parameter/contextual
assignment, source-file deferred work, and verifier-owned Flow/predicate/body integration.
Then review the runtime discrepancy fixes and final source delta. Build 13 compiled
the current implementation: four of five tiny semantic graphs and 611 of 612 affected
xUnit assertions passed, including all 304 new original/Node numeric assertions.
The two outstanding core findings are contextual callback cache presence and ambient
declaration admission; the full matrix and Native AOT gate are pending.

## Initial independent Flow review

Verification handed off a stable [Flow.cs](../../src/TypeScript.Scanner/Checker/Flow.cs)
for read-only review. Compared flow-state allocation/reset, shared-flow memoization,
start-container traversal, plain assignment/auto-variable reduction, branch-label
antecedent collection, call effects/never reachability, condition/predicate narrowing,
and effect-signature selection/publication against their original `flow.go` functions.
No unresolved finding in this batch. Managed pool reset preserves all zero-value
fields and retains only the private reduce-label capacity; complete relation/cache key
preimages preserve the selected identity inputs. Selected nil-state and declared-type
identity shortcuts are original source decisions, not inferred permissive fallback.

Loop/switch/evolving-array/compound-write/for-in/destructuring/optional-access guards
correspond to excluded source producers. Inferred identifier predicates and never calls
retain genuine effectsSignature cache behavior; written assertion predicates remain
outside admission. Runtime branch/capture/auto-variable/default/receiver observations,
remaining primitive operator integration, and final source changes still need review.

## Contextual and flow review delta

Compared `contextuallyCheckFunctionExpressionOrObjectLiteralMethod`,
`assignContextualParameterTypes`, deferred expression-body checking, and the actual
`getTypePredicateFromBody` / parameter-refinement families against their pinned
counterparts. The recursive ContextChecked recheck, contextual `this` synthesis,
initializer widening, cached return publication, single-return eligibility, assignment
filter and true/false flow construction retain upstream control flow. Generic inferential
mode remains an explicit excluded branch; ordinary contextual callbacks use the real
nil inference-context arm.

The newly restored `isEvolvingArrayOperationTarget` retains the upstream static
`length` / called `push` / `unshift` recognition, including its short-circuit order.
Its element-assignment arm and actual evolving-array finalization remain guarded
because their source producers are excluded. This helper's ordinary false arm is
required by selected auto-variable flow; no array success is manufactured.

No new source-review blocker in this delta. Runtime fixes and the final consolidated
snapshot still require review; this note is not an acceptance claim.

## Overload integration review

Compared `resolveCall`, `reorderCandidates` and `chooseOverload` to their pinned
counterparts. The selected path preserves specialized-signature insertion order,
subtype-before-assignability passes, recursive resolution stack push/pop, cache
publication before failure diagnostics, and the two-stage contextual-sensitive
argument check. Managed slice copies during insertion preserve the resulting ordered
list; source generic inference and optional-chain signatures remain explicit guards.
No new finding in this batch. Build 16 compiled the bigint formatting and core ambient
packet without warnings; a subsequently identified source-element reachability/cache
omission is being repaired by core before refreshed runtime comparison.

## Build 18 core delta

Compared the restored quick-call path, source-element state/reachability pre-dispatch,
coalesced unreachable diagnostics and lazy global `Symbol` value lookup against their
original functions. The non-null callee check, single-signature return lookup,
current-node/instantiation reset and unreachable-state restoration retain source
ordering. Parenthesized type children now receive the genuine default childwalk.
Authored symbol/signature ownership sets are confined to public guards and do not
replace algorithm links or graph identities. No new source-review finding here.

The earlier contextual-cache, ambient and reachable-effect omissions are source-fixed
in the core snapshot, but refreshed runtime evidence is still pending. Review closure
and Native AOT acceptance are not claimed.

## Final omission audit findings (pending consolidated verification)

1. `getTypeFactsWorker` still guarded all bigint literals, including the actual internal
   zero produced by admitted bigint truthiness flow. Core is restoring the original
   zero/nonzero facts; written bigint literal syntax remains excluded.
2. `isTypeParameterPossiblyReferenced` omitted its original symbol/declaration/scope
   wrapper and guarded admitted method payloads. Core is restoring the wrapper,
   selected method traversal and anonymous-instantiation cache closure needed when
   actual hidden-this parameters are conservatively retained. Source generics and
   conditional-type inference remain guarded.
3. `reportNonexistentProperty` guarded every nonnil promise-like value. An ordinary
   admitted structural `then` object can reach this path: the source checks whether
   the promised value owns the requested property, then emits 2339 with related 2773;
   otherwise it continues ordinary missing-property diagnostics. Core is restoring
   that original selected arm, with a focused independent-Go observation planned.

Build 19 independently passed all 76 semantic cases and all 688 checker xUnit cases
before these final omission fixes. That agreement does not close the source findings.
Support source is currently stable; the root freeze and full native gate remain pending.

## Final source dispositions

All three omission findings above are source-fixed in clean build 21 and independently
reviewed against the pinned originals. The reference-filter wrapper now preserves
single/merged declaration and lexical-block decisions; anonymous-instantiation support
retains outer-parameter order, filtering across declarations, complete cache preimages,
self-cache seeding, alias/mapper/target identity, deferred reference creation and computed
flag publication. No successful generic-inference substitute was introduced.

The bigint facts arm now handles actual zero/nonzero payloads. Promise-like missing
properties now retain both the actual promised-value property suggestion and ordinary
fallthrough. Focused Go specimens extend the corpus to 79; their consolidated C#
result is pending. No unobserved anonymous cache state is claimed exercised merely
because this source closure is implemented.

The selected core signature/call/context/return/deferred/operator/receiver, inference
boundary, reachability and Flow families have no unresolved source-review blocker.
Remaining guards correspond to the fixed source exclusions or actual generic inference/
class/library payloads rather than successful permissive hooks. This is bounded source
review, not whole-checker parity; final runtime delta and exact-source Native AOT evidence
remain required. Support executable source and owned gate tools are freeze-ready.

## Review reopened after the first frozen gate

The earlier closure verdict above was premature. Coordinator source review found
that `getOuterTypeParametersOfClassOrInterface` still rejects interfaces nested in
admitted function, arrow and method bodies. Its SourceFile/Block-only ancestor
assumption does not match the original outer-parameter walk. A merged hidden-this
interface also naturally reaches a nonempty anonymous-instantiation cache; this is
an in-contract observation, not an excluded generic case. These paths need source
repair, independent review and actual Go/C# observations before acceptance.

The special `Symbol` call condition is also under review. The pinned call site uses
`isSymbolOrSymbolForCall(node)` rather than the current constructor-name predicate.
An ambient caller-supplied `Symbol` value can require the genuine unique-symbol
model even without libraries; no successful substitute or spelling-based exclusion
is authorized.

The first comprehensive frozen gate passed in 529.436 seconds without source,
asset or historical-proof drift: 80 callable native cases, 304 numeric assertions,
43 admission controls, 10 API assertions, 13 exact order rows per runtime and all
23,402 xUnit tests passed, with retained native regressions exact. Its 87-file
[snapshot](../../artifacts/callables-validation/before-cache-closure-470449302c/preservation.json)
preserves all six native binaries and reports as regression-only evidence. No
accepted callable proof was written. This run is pre-closure verification expense;
the required full exact-source gate must run again after the reopened findings close.
The coordinator has now authorized source/assets thaw for those repairs.

## Reopened family audit and support handoff

Compared the restored class/interface declaration selector and outer-parameter
wrapper against `checker.go:24261–24334`. The walk now includes admitted function,
arrow and object-method ancestors rather than rejecting them by container kind.
Source-generic contextual signatures, mapped types and conditional inference still
require distinct producers rejected by admission; hidden interface `thisType`
belongs to the interface's parameter list rather than a source-generic signature.

Rechecked the full anonymous-instantiation algorithm (`checker.go:22792–22889`):
declaration/target selection, nullable alias arguments, filtering across merged
declarations, nonnil-empty outer-parameter publication, original self-cache key,
mapped argument key, combined mapper, deferred-reference versus anonymous payload,
result publication and computed flags retain source ordering. Runtime verification
must now observe the naturally nonempty cache and its actual key/value associations.

The residual guard review found one more admitted branch: a direct `if` body can
contain a parsed `let`/`const` variable statement. The grammar counterpart threw
instead of publishing original diagnostic 1156. Core has restored the exact keyword
selection and error call; a pinned-Go parsed witness is pending. Other reviewed
container guards require source classes, loops, binding patterns or decorators,
which are separately rejected at admission.

Caller-provided builtin names are not excluded by spelling. The global resolver
retains actual source symbol lookup and class/interface arity checks; absent or
wrong-arity generic library globals return original empty fallback types. No selected
source signature declares type parameters, so the explicit generic-inference guards
cannot be justified merely from `noLib` but do have the admission/producer boundary.

Support added actual `UniqueESSymbolType.name` storage and wrapper cast/payload
identity, the original diagnostic accessible-reference versus `unique symbol` arm,
tracker/recovery forwarding and selected printer operator. Core owns the real global
`Symbol`/`Symbol.for` predicate, per-symbol cache and unsigned-ID raw-name construction.
Written unique-symbol syntax remains excluded. Source formatting and owned whitespace
checks pass; runtime cache/name controls and the final source delta remain pending.

The remaining Promise arity guard was challenged by its caller rather than its name:
the original helper first requires a parameter owned by a function/arrow immediately
inside a `new` expression. Admission rejects that producer, so ordinary ambient
`Promise.resolve` or a local function named `resolve` cannot reach the guarded tail.
`createPromiseType` requires a genuine arity-one global class/interface; every admitted
nongeneric caller-supplied `Promise` fails the original arity check and returns its
empty-generic fallback before that guard. The same producer reasoning applies to
generic `NonNullable`/Iterable alias/global paths; nongeneric builtin names themselves
remain allowed and use genuine lookup/fallback behavior.

The observer review confirms that unique-symbol names validate the exact raw
`FE@owner.Name@unsignedID` preimage before emitting a canonical owner/name descriptor.
Per-rooted-type membership plus whole-cache cardinality prevents a hidden cache entry
from being balanced by an unrelated visible type. Anonymous associations reconstruct
arguments, alias and single-signature state, compare every actual map key/value and
reject duplicate reconstructed keys or unknown mapper families. A C# late-registration
asymmetry found during review was repaired: both observers now reject unrooted argument
or alias references after the graph fixedpoint and use existing canonical IDs.

The final interface/value duplicate-index repair was compared to the original
`checker.go:5003/5011/20206` boundaries. It publishes the declared-symbol check flag,
calls the real index helper and preserves nil member-map lookup. Synthetic inherited
any-base `IndexInfo` belongs to the type's index-info list, not a source index-declaration
symbol; written index declarations are independently rejected by admission. Empty
interface/value merged symbols therefore take the original nil-return arm. No further
source-review blocker remains in these repaired families; the expanded 89-case runtime
packet and final exact-source native gate remain pending.
