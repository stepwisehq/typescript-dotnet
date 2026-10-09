# Independent callable support review

**Current status: accepted fixed callable contract.** Final build 24 and independent
review closed all known findings. The corrected Linux x64 Native AOT gate passed in
512.357 seconds: 89 callable cases, all 23,411 xUnit cases (701 checker) and retained
regressions. See the [durable proof](../../validation/results/callables-verified.json)
and [assessment](../CALLABLES-ASSESSMENT.md). No active work remains. Source/assets
were unchanged during acceptance. The sections below are historical review and
implementation records; prior pending/reopened instructions are superseded here.

## Historical record

Core algorithm owner (GPT-6.1 Sol), bounded read-only review during callable integration.
The [contract](../CALLABLES-CONTRACT.md) remains the acceptance boundary. No final
Native AOT or campaign acceptance is recorded here yet.

Reviewed selected additions against pinned Go `fed0bf24149fb1ed36039212648bafdafc1ea10e`:

- [Types.cs](../../src/TypeScript.Scanner/Checker/Types.cs): all fourteen Signature
  fields, flags, ordered composite constituents, predicate payload/sentinel links,
  and three SignatureLinks slots match their original structures. Target, mapper,
  composite and isolated-signature type remain separate identities. Null parameter
  declarations are valid for composite parameters; the public authored guard was
  repaired at actual allocation helpers rather than requiring fabricated declarations.
- [Relater.cs](../../src/TypeScript.Scanner/Checker/Relater.cs): signature comparison
  retains arity, receiver direction, strict callback variance versus method
  bivariance, predicate compatibility, return-void/any exceptions and ternary
  composition. Minimum argument count retains the cache and trailing-void reduction.
  Predicate resolution retains target/composite/body paths and the genuine shared
  no-predicate sentinel. The synchronous arrow diagnostic path invokes genuine
  promise construction/then inspection; it does not manufacture an async type.
- [Mapper.cs](../../src/TypeScript.Scanner/Checker/Mapper.cs): existing simple,
  array, deferred, merged and composite mapping preserves hidden-this signature
  instantiation. Source-generic candidate/signature and conditional inference
  allocations remain explicitly outside the contract; this review does not claim
  complete inference mapper support.
- [NodeBuilderImpl.cs](../../src/TypeScript.Scanner/Checker/NodeBuilderImpl.cs) and
  [PseudoTypeNodeBuilder.cs](../../src/TypeScript.Scanner/Checker/PseudoTypeNodeBuilder.cs):
  parameter serialization retains real declaration reuse, optional/default handling,
  raw names, synthetic names and explicit receiver parameters. Return serialization
  retains nonnil enclosing contexts, mapper application, pseudo equivalence and
  inferred predicate rendering. Callable diagnostic printing remains a selected
  internal dependency, not a public emit facility.

Additional independent review covered:

- [Flow.cs](../../src/TypeScript.Scanner/Checker/Flow.cs): selected assignment,
  call/effect, branch/reduce, shared-flow and pooled-state lifecycle, captured
  reference/last-assignment boundaries, predicate/truthiness/equality narrowing
  and reachability match their pinned counterparts. The empty-array predicate's
  original nullable-initializer and binding-element preconditions were restored
  by the flow owner after review; no admitted runtime failure was claimed for
  that latent delta. Narrowing keys preserve actual type identities and boolean
  direction inputs. Excluded array/loop/switch/source-operator arms remain guarded.
- [Admission.cs](../../src/TypeScript.Scanner/Checker/Admission.cs): explicit
  Callable profile enables the selected syntax/options on the same checker;
  Legacy stays separate at admission. Recursive grammar checks retain exclusions
  for generics/rest/arrays/classes/async/modules, static-dot write constraints and
  documentation/external-reference boundaries. Parse-valid invalid method and
  parameter modifiers are admitted for actual grammar diagnostics, not successful
  class/accessibility support.
- [CallableGraph.cs](../../validation/Checker.Validation/CallableGraph.cs),
  [callable_graph.go](../../tools/checker-oracle/callable_graph.go) and their actual
  cache bridges: records preregister before recursive discovery; return, predicate
  and parameter-type observations occur in matching order. Signature/predicate/
  composite sharing, actual logical-link presence, ordered calls/deferred nodes,
  all eleven SourceFileLinks fields and nullable diagnostic chains remain explicit.
  Known special cache keys are read without population and unknown families fail;
  the final total-entry check closes a possible undiscovered-owner omission.
- [CallableNumericChecks.cs](../../validation/Checker.Validation/CallableNumericChecks.cs)
  and [fixture extraction](../../tools/generate-callable-numeric-fixtures.py): the
  ten original tables are evaluated from pinned Go literal expressions, not C#
  outputs. Literal equality, NaN handling, equal signed zeros and Node's original
  exponent-only one-ULP allowance match the upstream assertion helpers. The
  [Evaluator](../../src/TypeScript.Scanner/Evaluator/Evaluator.cs) preserves numeric/
  string coercion, caller-selected outer-expression stripping, entity callbacks
  and original result flags; excluded element/template syntax throws. Node is an
  explicit prerequisite, so missing Node does not count as passing assertions.

No unresolved material source finding remains in these reviewed families. Snapshot
19 matched 76 callable cases and passed all 688 checker xUnit rows. Subsequent
source review exposed three admitted old guards (bigint facts, nested hidden-this
methods and thenable property diagnostics); independent Go witnesses establish
these were real failures. Core restored them and support independently reviewed
that delta. Build 21 compiles clean and the expanded 79-case comparison and 691 checker
xUnit rows passed. Final formatted build 22 is clean with zero warnings/errors,
source `470449302cdad708100ef6d82e12b005a1f31fce7f14b1065980808b84c25d2b`;
all owned executable source/tools are frozen. The final 80-case isolated refresh
and actual native/regression gate are pending. Runtime agreement remains additional evidence,
not a replacement for this source review.

Remaining evidence limits: signature special caches are genuinely empty in the
saved non-generic/ordinary-call corpus; isolatedSignatureType remains null and its
source callers require excluded generic/decorator/JSX/accessor machinery. The anonymous-instantiation cache branch is retained but its nonempty map was
unobserved in the saved 80 cases. This is an **in-contract untested path**, not a
source-excluded producer: a merged interface with implicit-this inheritance has a
hidden parameter whose symbol has multiple interface declarations; the original
reference filter then conservatively returns true rather than removing it.

## Anonymous-instantiation disposition clarification

The actual saved `C4-hidden-this-nested-method` input has a single declaration of
`B`. Its inherited `member` TypeLiteral enters
[getObjectTypeInstantiation](../../src/TypeScript.Scanner/Checker/Checker.cs),
collects the outer `B.thisType`, then checks its explicit scalar method parameter
and return annotations. They contain no `ThisType` or other hidden-parameter
reference. The saved original-Go node 11 TypeNodeLinks has allocated-empty
`outerTypeParameters` (not null), proving entry and filtering rather than merely
an inferred absence. Its target anonymous `instantiations` stays nil.

The original [reference filter](../../upstream/TypeScript/tsc/internal/checker/checker.go)
at 22891 performs that scope/reference scan only when the parameter symbol has
exactly one declaration. If a second ordinary `interface B {other:number}` merges
with the existing hidden-this `B extends A`, it returns conservative true. The
nested member retains the hidden parameter, and the original target/self seed,
composed argument mapping and cached anonymous result become reachable. Interface
merging and nested methods are selected capability; this was reported to root as
concrete missing cache evidence before acceptance, with no runtime/source changes
or experiments during the frozen gate.

The same read-only follow-up also found a separate residual old guard in
[getOuterTypeParametersOfClassOrInterface](../../src/TypeScript.Scanner/Checker/Checker.cs):
it restricts all ancestors to SourceFile/Block, while current admission permits
nongeneric local interfaces inside function bodies. Original source 24261 delegates
to `getOuterTypeParameters(..., false)`; admitted nongeneric function ancestors
supply no source type parameters and need not reject. This contract-critical guard
was reported to root before any repair. This note is not a new successful probe or
acceptance claim. Rest/tuple, source generics, async/generator,
classes/constructors, injected libraries and general emit are excluded. No complete
checker or exhaustive branch parity is claimed. Exact-source Native AOT/regression
acceptance still requires the final frozen gate.

Frozen scratch confirmation (no source/tools/corpus writes or builds):
[report](../../artifacts/callables-validation/cache-review-frozen/report.json)
retains exact requests and binary fingerprints. All three local-interface sources
(function, arrow and object method) succeed in pinned Go without file diagnostics
and fail the managed/native ancestor guard. The merged hidden-this source reaches
nonempty anonymous instantiations in every runtime; all three strict rich observers
fail `Unknown instantiated-object key family`. This is a concrete required observer
closure, not a binder/checker crash or a normalized success. Root will thaw only the
needed repair and witness work after the current gate ends.

Support also identified an admitted ambient `SymbolConstructor` call. Read-only
source comparison reveals the current call-specialization predicate differs from
pinned 8557 (`isSymbolOrSymbolForCall(node)`): restoring that predicate is required.
An actual caller-supplied global `Symbol` returning symbol may additionally require
internal unique-symbol creation; this remains a pending bounded dependency question,
not an excluded-by-name library assertion. No frozen source was changed.

## Bounded post-gate closure review

Root authorized the mandatory repair/witness thaw after preserving the initial
successful regression gate. The local-interface wrapper now retains original
`getClassOrInterfaceLikeDeclaration` and recursive outer-parameter collection;
`includeThisTypes=false` preserves nil nongeneric outer parameters. The original
Symbol-call predicate and real unique-symbol cache/allocation/raw unsigned name
preimage are restored. Source generics, unique-symbol annotations, computed names
and injected libraries are not admitted.

Independent review of the new observer checks finds the original preimages intact:
anonymous self-seed uses stored outer parameters/owner alias/false; instantiated
values use actual target, ordered stored mapper arguments, alias and retained
single-signature flag. Simple/Array source identity is checked, and the extra
SingleSignature composite mapper is peeled only at its original first-map boundary.
Every actual cache key is recomputed and checked for same-value lookup, with unknown
producer families failing explicitly. Unique raw name is validated exactly as
`FE@owner.Name@unsignedSymbolId`, then represented by actual owner identity and raw
owner name. Its entire real cache count and per-owner identity are checked. No
cache-producing getter or observer-assigned flag is introduced.

Support's unique model/formatter now preserves actual `name`, TypeBase identity,
accessible-symbol vs unique operator rendering and original tracker callback.
Core scalar helper review retains original unique widening, contextual literal
handling, union ordering and property-name fields. A final direct-if let/const
source guard was also restored: pinned Go confirms 1156 without parser errors;
original `error` side effect and false-return behavior are retained.

Central build 23 passes zero warnings/errors. Mandatory rich closure cases and
all prior cases are awaiting the verifier's isolated differential snapshot; no
final acceptance or refreeze is claimed by this note yet.

The post-thaw actual-Go and managed closure establishes genuinely nonempty
anonymous caches: merged hidden-this owners hold three entries (self seed plus two
distinct argument mappings), all exact key associations/repeated lookups true.
Unique Symbol and Symbol.for cases preserve nominal a/b inequality and assignment
to ordinary symbol with reverse rejection, including const/let/var widening.
Late final-phase argument/alias registration was tightened to reject unrooted
values, matching the original-Go observer rather than adding records after the
snapshot fixed point. Snapshot 23 matched 88/89; the remaining merged-value
variable walk failure is repaired by the original index-symbol duplicate-check
boundary (not spelling or declaration-kind special casing). Build 24 compiles
clean; final comparison and repeated frozen gate remain pending.

Final managed snapshot 24: **89/89 exact**, **701/701 checker xUnit** with zero
failures/skips, and **21/21** Go and managed order observations (eight cases, two
sequences). All owned executable source/tool/corpus writes are frozen. Source
`32e043af7ec8419dbee4ce132be7cb72df84ef56a148d564c1a081dc989302df`.
No unresolved material source/observer finding remains. The mandatory merged
anonymous-cache and actual unique-symbol branches now have direct independent Go
identity/key evidence; they are no longer described as unobserved. Complete checker,
source-generic/library/emit parity and exhaustive branch coverage remain unclaimed.
The repeated exact-source Native AOT/regression gate is still pending.
