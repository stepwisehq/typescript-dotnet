# Callable core investigation checkpoint

**Current status: accepted fixed callable contract.** Final build 24 and independent
review closed all known findings. The corrected Linux x64 Native AOT gate passed in
512.357 seconds: 89 callable cases, all 23,411 xUnit cases (701 checker) and retained
regressions. See the [durable proof](../../validation/results/callables-verified.json)
and [assessment](../CALLABLES-ASSESSMENT.md). No active work remains. Source/assets
were unchanged during acceptance. The sections below are historical review and
implementation records; prior pending/reopened instructions are superseded here.

## Historical record

Owner: GPT-6.1 Sol checker algorithm agent. Implementation approved under
[fixed contract](../CALLABLES-CONTRACT.md); core owns shared-library builds.
C1–C5 selected source batches retain signature/parameter/return/context/body/call
algorithms and their actual data, flow, receiver, operator and diagnostic closure.
The first full gate succeeded at source `470449302c...`, but a final source-based
cache question exposed mandatory completion gaps. That gate and binaries are
preserved as regression evidence, not accepted callable completion.

Root authorized the bounded thaw. Core now restores local-interface outer-scope
collection and original duplicate-index delegation for merged interface/value or
function declarations. Caller-supplied ambient Symbol/Symbol.for retains the real
unique-symbol model/cache/unsigned owner-name preimage and scalar widening/order
behavior. Direct if-body let/const uses original 1156 error behavior. Verification
adds real nonempty anonymous-cache arguments/alias/target/key associations and
strict unique-name/cache ownership observations. No source generics, computed keys,
unique annotations or injected libraries were added.

Central Release build 24 passes zero warnings/errors in 3.56 seconds
(`/tmp/callables-build24.log`), including all source and observer repairs. Exact
source `32e043af7ec8419dbee4ce132be7cb72df84ef56a148d564c1a081dc989302df`.
Snapshot 24 matches all 89 expanded callable cases and passes all 701 checker
xUnit rows with zero failed/skipped. Eight order cases in two sequences match
21/21 results independently in Go and managed execution. The previous index-member
lookup failure is resolved without changing prior goldens.
Owned files are LF syntax-formatted, `git diff --check` is clean, the 1,474-record
function inventory is refreshed, and no owned semantic edit remains pending. Final
source/tool/corpus freeze is acknowledged; the repeated exact-source Native AOT/
regression gate remains required.
Campaign baseline `de855ca`, foundation `2886d47`; upstream remains
`fed0bf24149fb1ed36039212648bafdafc1ea10e`.
Ownership: core Checker/GrammarChecks/Utilities/Exports and shared library builds;
support Types/Links/Relater/Mapper and formatter; verifier exclusively Flow plus
Admission/profile/oracle/tests. Adapter runs use regular CopyLocal dependencies, so
library builds do not replace the runtime snapshot while it is under comparison.


## Recommended coherent capability

Preserve the accepted single-file data-type checker. Add ordinary non-generic
function declarations, function expressions and arrows; function-type aliases,
interface/type-literal call signatures and static object/interface methods.
Identifier parameters support required/optional/default values. Include overload
sets and failed-overload diagnostics, callable unions/intersections, higher-order
callbacks, contextual parameter types, inferred/annotated returns, recursion and
captured lexical variables. Include ordinary function blocks, local declarations,
nested functions, if/else, multiple returns, throw/never and basic numeric/string/
comparison/logical/conditional expressions. Simple identifier/static-dot assignment is selected. Loops/try-finally are explicitly
excluded by the fixed contract.

Explicit receiver parameters/object-method this/arrow capture fit non-library
callable use if selected. Preserve legacy options; an explicitly selected profile
may set strictFunctionTypes and noImplicitThis without claiming strict-all.
NoImplicitAny must remain a separately stated choice. No standard library is
silently synthesized, and exact noLib global diagnostics remain observable.

Prominent recommended exclusions: source generics/type arguments, async/generators/
Promise/iterators, classes/super, source new/construct signatures, arrays/tuples/rest/
spread/destructuring, decorators/JSX/JS, source modules/projects, emit/services and
library-method availability. A guard must reject only actual unsupported syntax/
resolved features, never valid in-contract errors, ordinary function names or
inferred callable machinery.

## Original investigation: exact source families and initial omissions

The following table preserves the pre-implementation investigation; its “current”
omissions describe that initial checkpoint, not the implemented source above.
All locations below refer to pinned [checker.go](../../upstream/TypeScript/tsc/internal/checker/checker.go)
except named counterparts. These are required families, not a claim that every
branch in each source interval must become public capability.

| Family | Original function boundaries | Required behavior/current pressure |
| --- | --- | --- |
| Declaration/signature grammar | checkParameter:2700; checkSignatureDeclaration:2769; checkFunctionDeclaration:3434; checkFunctionOrConstructorSymbol:3492; worker:3500; checkAllCodePathsInNonVoidFunctionReturnOrThrow:3759; grammarchecks.go checkGrammarParameterList:687/checkGrammarFunctionLikeDeclaration:758 | Parameter initializer/order/type checking, overload consistency, real body and return completeness; current declaration dispatcher excludes functions/parameters |
| Calls and overload selection | checkCallExpression:8529; getResolvedSignature:8614; resolveSignature:8660; resolveCallExpression:8678; resolveCall:9061; reorderCandidates:9178; chooseOverload:9246; hasCorrectArity:9332; isSignatureApplicable:9481; overload failure helpers:9723–10145 | Ordered candidates, subtype/assignability passes, contextual callback rechecks, arity/argument diagnostics, cached resolving sentinel; no current call implementation |
| Function expressions/context | checkFunctionExpressionOrObjectLiteralMethod:10340; contextual worker:10381; deferred worker:10437; getContextualSignature:10495; assignContextualParameterTypes:10580; assignNonContextualParameterTypes:10626; getContextuallyTypedParameterType:30006; getContextualReturnType:30214; getContextualSignatureForFunctionLikeDeclaration:30260; getContextualTypeForArgumentAtIndex:30321 | Actual deferred body queue, ContextChecked links, IIFE parameter inference, optional/default context, nil versus real inference contexts; current context/parameter guards are partial |
| Signature identity and return resolution | getTypeOfFuncClassEnumModule:17291; getSignaturesOfSymbol:20210; getSignatureFromDeclaration:20240; getReturnTypeOfSignature:20405; getReturnTypeFromBody:20530; checkAndAggregateReturnExpressionTypes:20663; instantiateSignature:21019; resolveAnonymousTypeMembers:21054; newSignature:25781 | Real signature IDs/caches/targets/mappers and return cycles, body inferred unions/widening/never; current nonempty signature creation/resolution/instantiation guards reject these paths |
| Composite callables | resolveUnionTypeMembers:21456; getUnionSignatures:21516; appendSignatures:21739 combineUnionOrIntersectionMemberSignatures:21587; combineUnionOrIntersectionParameters:21626; combineUnionOrIntersectionThisParam:21692 | Ordered overload/callable unions and intersections, synthetic signatures and return composites; no custom property-loop replacement |
| Function bodies/expressions | checkBlock:3816; checkIfStatement:3833; checkReturnStatement:4117; checkReturnExpression:4162; checkThrowStatement:4256; checkTryStatement:4266; checkCatchClause:4278; checkPrefixUnaryExpression:11091; checkPostfixUnaryExpression:11145; checkConditionalExpression:11170; checkBinaryLikeExpression:12575; checkAssignmentOperator:13001 | Real return/closure/definite-assignment diagnostics and operator typing; current bodies accept only data declarations/blocks |
| Flow | [flow.go](../../upstream/TypeScript/tsc/internal/checker/flow.go) getTypeAtFlowNode:117; getTypeAtFlowCall:288; getTypeAtFlowCondition:354; narrowTypeByBinaryExpression:469; branch labels:1253; loop labels:1325; getEffectsSignature:2047; reachability:2513/2522; assignment tracking:2655–2749 | Current Flow.cs explicitly rejects call/condition/label/reduce and containing-function flow. Preserve real snapshots/shared flows/cycles/captured references; if/return cannot bypass this |
| Callable relations | [relater.go](../../upstream/TypeScript/tsc/internal/checker/relater.go) compareSignaturesRelated:1491; parameter/minimum/rest helpers:1726–1860; signaturesRelatedTo:4473; signaturesIdenticalTo:4593 | Parameter direction, callback variance, method bivariance, void/never returns, overload correspondence, real ternary/cache/error chains; support owner investigates |
| Public observations | [exports.go](../../upstream/TypeScript/tsc/internal/checker/exports.go) GetSignaturesOfType:187; GetTypePredicateOfSignature:227; GetReturnTypeOfSignature:247; GetResolvedSignature:271 | Authentic narrow wrappers plus owned-node/type guards, signature identity and cache observations; no signature-help/service API implied |

The relevant original-source clusters span thousands of lines (roughly 5,000+
checker lines before flow/relations/formatter dependencies). This is substantial
algorithm restoration, not a few switch arms; interval sizes are source scale,
not an optimistic finished-port estimate or complete dependency count.

## Required internal dependencies and boundary pressure

- Ordinary non-generic boolean-returning functions may **infer a type predicate**:
  getTypePredicateFromBody:20939 and checkIfExpressionRefinesAnyParameter/Parameter:
  20969/20989 construct real conditional flows. Excluding authored predicate syntax
  does not justify returning noTypePredicate for these functions. Signature/flow/
  formatter observations must retain the actual inferred predicate.
- Full Signature fields/SignatureFlags/CompositeSignature/TypePredicate and
  SignatureLinks are required, including resolved-return cycle tags and synthetic
  target/mapper/composite identities. Inherited implicit-this references from the
  accepted interface capability can instantiate signatures without source generics.
- Non-generic call applicability explicitly supplies a nil InferenceContext to
  checkExpressionWithContextualType, but higher-order/deferred/contextual machinery
  must preserve original branches and report any reachable nonnil inference
  closure. Return/contextual parameter inference is distinct from generic type-
  argument inference; neither may be replaced by successful placeholders.
- Rest is materially larger: checkParameter:2742 compares anyReadonlyArrayType;
  createArrayType:25235/25239 creates generic global Array/ReadonlyArray references;
  checker initialization:1363–1382 creates array markers; relation rest-at-position
  helpers may construct tuples; spread applicability requires tuple/index/iteration
  helpers. NoLib fallbacks are real empty-object/error semantics, not a substitute
  for useful typed arrays. Keep this family prominent unless the coordinator
  explicitly chooses its library/model/tuple closure.
- Async/generator source bodies remain excluded. Synchronous call/arrow diagnostic
  suggestions nevertheless invoke real promise/awaited/thenable inspection; the
  selected cache/cycle/then-callback paths are required internal support. Genuine
  generic library environments remain guarded, rather than supplying fake types.
- Named/callable diagnostic rendering extends actual NodeBuilder/PseudoChecker/
  printer signature/parameter/predicate branches; current nonempty guards must be
  removed for selected paths, not bypassed with handcrafted function strings.

## Ownership and implementation sequence recommendation

Core owner: Checker.cs/Utilities.cs/GrammarChecks.cs/Exports.cs, retaining source
file/function boundaries. Flow.cs was transferred exclusively to verification
to balance the actual flow/body dependency work. Support owner:
Types/Links, Relater and signature/predicate formatting/pseudochecker/receiver
support. Verification owner: admission/profile/protocol, original-Go expected
outputs, static AOT signature graph and tests. Implementation is active under the fixed contract; Mapper.cs is exclusively support-owned.

Internal milestones: (1) actual signature/parameter/return caches + typed call;
(2) contextual callback/overload errors and callable relations; (3) body branches/
multiple returns/throw/closures/recursive inference + actual inferred predicates;
(4) callable-object/named/composite/receiver interactions, chosen profile and
negative controls; (5) independent source/guard review, frozen systematic native
verification and all accepted regressions. Milestones are not campaign acceptance.

Next: await root authorization and the repeated exact-source native gate.
All owned source/tool/corpus writes are frozen; only documentation evidence updates
remain. No owned executable changes remain pending. Selected-path cross-review is complete
with no unresolved material finding; observed and unhit paths remain distinguished
in the review and coverage inventories. Source/tools are frozen; docs-only
evidence updates remain.
Support owns Mapper and verification owns Flow; do not edit either from core recovery.

## Frozen-gate read-only clarification

Anonymous-instantiation nonempty-cache coverage is untested within the contract,
not impossible: multiple declarations of a hidden-this interface make the original
reference filter conservative true, retaining outer parameters on nested anonymous
members. The saved single-declaration nested-method case proves actual entry and
allocated-empty `outerTypeParameters` after filtering. Root was notified of the
merged-interface mandatory evidence candidate and a separate old local-interface
ancestor guard before edits; see [independent review](./review-callables-support.md).
Source/tool/corpus freeze remains in force pending root direction.

## Post-gate bounded repair checkpoint

Root authorized source/tool/corpus thaw after preserving the first successful
regression gate. The selected completion gaps remain active; that gate is not
campaign acceptance. Core now restores original local-interface outer-parameter
lookup via `getClassOrInterfaceLikeDeclaration` and `getOuterTypeParameters(...,
false)`, and the actual `isSymbolOrSymbolForCall` call predicate. Caller-supplied
ambient global Symbol/Symbol.for has actual pinned-Go unique-symbol results;
original symbol-identity cache, unsigned raw name preimage, allocation and scalar
widening/order/property-name helpers are implemented. Support owns the exact
UniqueESSymbolType model and diagnostic serialization. Written unique-symbol and
computed-key syntax remain excluded.

Support's final residual-guard review found direct if-body let/const declarations;
actual Go reports 1156 without parser diagnostics. Original checker error side
effect and false return are restored. Verification owns the mandatory new witness
rows and anonymous-cache/unique-name strict observation closure. All earlier
expected outputs remain unchanged. Next central build is 23 after its source
bridge is stable; no shared build is currently running.

Final managed closure: 89/89 exact, 701/701 checker xUnit, and original-Go/managed
21/21 order observations. The merged anonymous cache and actual unique identities
are now observed; only genuinely excluded/unhit signature special-cache and
isolated-signature families remain as noted in the review. No unresolved source or
observer review finding. This is managed evidence; repeated native acceptance is
still pending.
