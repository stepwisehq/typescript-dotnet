# Generic collection verification checkpoint

Implementation authorized under [the fixed contract](../COLLECTIONS-CONTRACT.md).
Baseline `cfa6d2f`; upstream `fed0bf24149fb1ed36039212648bafdafc1ea10e`.
Source, tools, corpus and examples are frozen after clean central Debug build 30.
The complete Linux x64 Native AOT gate passed, with all retained regressions.
No accepted historical input, expected output or proof bytes were changed.

[The final recorded proof](../../validation/results/collections-verified.json)
records gate exit 0 in 658.892 seconds, with no source, asset or historical asset
drift. Native comparison is 44 of 44 exact in 11.789 seconds; all 12 shared
checks and 18 order executions per runtime pass. The full cascade passes all
23,467 xUnit cases without skips, including 757 checker cases and all 44 final
collection oracle theory rows. Seven freshly published native binaries and both
original oracle binaries are fingerprinted before and after execution. Current
whole-overlay behavioral parity is again 44 of 44 byte-exact (16.768 seconds),
and actual-Go support operations reproduce their unchanged expected bytes.

Final source SHA256 is
`4b72bcbcae5b18e9322d2d45ecee624bc0d4b2f4d4b0ca109209fcf78f3fc9c3`;
verification asset SHA256 is
`5bfa508a7115d8b175af41cea303ba10c792656d7c025b53353b865a6655dda6`.
The observed limits below remain part of this acceptance disposition.

[The final canonical manifest](../../validation/collections-corpus/manifest.json)
registers 44 unique authored cases, all requesting rich stored observations,
plus 12 shared checks. [Managed comparison](../../artifacts/collections-validation/build30/canonical-managed/report.json)
is 44 of 44 exact in 12.560 seconds. [Complete current-overlay behavioral parity](../../artifacts/collections-validation/build30/metadata-parity-canonical/report.json)
is 44 of 44 uninstrumented/instrumented response bytes exact in 16.592 seconds.
[Request-order controls](../../artifacts/collections-validation/build30/order/report.json)
pass all 18 executions in each runtime, across eight cases and two fresh process
sequences, with unchanged binaries (13.750 seconds). The isolated build 30
shared dispatcher passes all 12 checks, with no skips.

The corpus covers fixed-environment merges and collisions, generic constraints,
defaults and inference failures, recursive and merged callable relations,
Array/ReadonlyArray APIs, tuple/rest/spread, mapped and conditional utilities,
reverse inference, loop and evolving-array flows, and practical grouped-record
applications with eight invalid checks. The exact observed totals are 260
queries, 17,932 semantic records and 12,385 stored state cells. [The coverage
inventory](../collections-coverage.json) reconciles all 37 named instance members,
all 50 named method overload declarations and all three Array call declarations
against actual retained resolved signature/declaration identities. These totals
are evidence inventory, not a claim that every combination was exercised.

Whole passive binding identity covers the three complete libraries and user
file. Semantic roots are user/reached state; unused library type resolution is
not forced. Selected cache associations preserve full key membership, identity,
nullable state and cardinality. Reached union/intersection intern membership is
not a whole-map census. No retained synthetic types were rooted in these cases;
transient synthetic argument production is implemented and source-reviewed,
and unknown retained synthetic store owners still fail. Unchanged private
formatter/cache backing topology and full collection APIs remain explicitly
unclaimed in [the retained-cache ledger](collections-retained-caches.md).

Finite authored producer metadata closes two genuine identity ambiguities:
applicability versus error-reporting checks, and subtype versus assignable
choose-overload passes. Frames retain only original already-computed inputs,
restore prior state on every exit and nest correctly. There are no allocation
ordinals, process-ID ordering terms or semantic re-queries. [The original
applicability witness](../../artifacts/collections-validation/build26/applicability-phase-witness.json)
and subsequent failed reports remain preserved. Kind 14 was temporarily
misclassified as Awaited rather than RegularObjectLiteral; unnecessary metadata
was removed before build 28. This was an investigation error, not a compiler
defect. The actual existing source-to-regular edge supplies the census input.

The incompatible mutable/readonly filter predicate control now matches original
Go, including both 2769 diagnostics and narrowed array results. Exact original
formatter visibility and parameter-symbol fast paths were restored; no observer
cache waiver or predicate visitor skip was introduced. [The prior observer
failure](../../artifacts/collections-validation/build29/filter-cache-diagnostic/report.json)
remains distinct from [the corrected complete result](../../artifacts/collections-validation/build30/canonical-managed/report.json).

Build 21 previously passed all 713 checker xUnit cases: 701 historical cases and
12 shared collection checks. The final complete cascade now passes 757 checker
cases, including those 44 registered final corpus theory rows. The older 713
total remains a historical integration milestone.
The original checker symbol test is explicitly adapted to the fixed host; the
ordered-set test preserves its three Has assertions before unsupported
Delete/Clear/Clone. Seven additional helper groups have independent actual-Go
fixtures. General OrderedMap/SyncMap APIs and private backing topology are not
claimed. See [the test inventory](../collections-tests.json).

The same-checker original Program projection composes bind/check diagnostics and
applies original sorting and related-info merging. All files are parsed and bound;
`skipLibCheck` skips declaration-file checking only. Explicit root order is ES5,
decorators, decorators.legacy, then `/main.ts`, with actual default-library status
false for all four files. Library syntax has 10,890 nodes before user input.
The complete per-request graph is gzip-compressed without dropping mutable
library merge state. [Readable valid and invalid applications](../../examples/collections/)
match canonical request bytes. Earlier bootstrap/vertical streams were archived
as investigation-only, not counted as acceptance.

Owned implementation includes [the fixed environment](../../src/TypeScript.Scanner/Checker/FixedLibraryEnvironment.cs),
[admission](../../src/TypeScript.Scanner/Checker/CollectionAdmission.cs),
[Flow](../../src/TypeScript.Scanner/Checker/Flow.cs),
[the independent Go oracle](../../tools/collections-oracle/), and
[the shared validation adapter](../../validation/Collections.Validation/).
Core owns shared builds and acceptance tooling; support owns static model/cache
mirrors and independent review. The root-authorized comprehensive native gate and proof recording are complete;
source, tools, corpus and examples remain frozen.

## Historical investigation chronology

The following sections preserve prior investigation and integration states. Their
pending statements do not override the current checkpoint above.

## Actual original-Go investigation

[Scratch producer](../../artifacts/collections-investigation/explicit-roots-probe.go),
[results](../../artifacts/collections-investigation/explicit-roots-results.jsonl) and
[manifest](../../artifacts/collections-investigation/investigation.json) preserve eight
original-Go runs outside guarded source/tool/corpus assets. These are investigation
observations, not canonical acceptance or a compiler-test assertion pass.

The demo uses recursive `Row`, generic constrained selection of readonly record
arrays, actual Array.filter/map/reduce, tuple returns, generic identifier rest,
array spread, a concrete tuple-spread call and ordinary array for-of. Actual Go
infers `string[]`, `number`, `[string[], number]`, `Row | undefined`, a `Row` loop
variable and numeric loop total, with no user/library/global diagnostics. Invalid
variants produce 2344 constraint, 2339 property, 2322 tuple shape, 2322 generic
reduce callback return, and 2495 nonarray for-of diagnostics. With the complete
ES2015 iterable/symbol extension, the same valid types hold and the last invalid
code becomes 2488. This does not justify an ES2015 iteration claim for the chosen
ES5 environment.

Both false/true skipLibCheck options were probed. Healthy original libraries yield
no errors either way, so those outputs alone do not demonstrate suppression. Raw
Checker.GetDiagnostics is separate from Program.GetSemanticDiagnostics: the latter
honors actual [SkipTypeChecking](../../upstream/TypeScript/tsc/internal/compiler/program.go:872),
including declaration-file skip policy. The new adapter must retain that policy
explicitly; do not call raw library diagnostics and describe them as skipLibCheck
results. Library syntax/binding/identity and demanded library member semantics
remain real even when declaration-file semantic checking is skipped.

Exact selected virtual roots are `/main.ts`, `/lib/lib.es5.d.ts`,
`/lib/lib.decorators.d.ts`, `/lib/lib.decorators.legacy.d.ts`; all library bytes
are unmodified original files. `noLib: true` disables implicit default loading,
`noResolve: true` plus SkipModuleResolution limits projects; explicit roots load
this fixed environment. Initial lib-option-only probing exposed that noResolve
suppresses referenced lib loading. Do not silently omit decorators or symbols
when claiming a complete closure.

| Original library | SHA256 |
| --- | --- |
| [lib.es5.d.ts](../../upstream/TypeScript/tsc/internal/bundled/libs/lib.es5.d.ts) | `45cc92f8dd3df7cb07b7d19e2b2ca34e78bc005c910c1e53ee1cf9bf37910215` |
| [lib.decorators.d.ts](../../upstream/TypeScript/tsc/internal/bundled/libs/lib.decorators.d.ts) | `1ce14b81c5cc821994aa8ec1d42b220dd41b27fcc06373bce3958af7421b77d4` |
| [lib.decorators.legacy.d.ts](../../upstream/TypeScript/tsc/internal/bundled/libs/lib.decorators.legacy.d.ts) | `b3a048b3e9302ef9a34ef4ebb9aecfb28b66abb3bce577206a79fee559c230da` |

Options: ESNext, noLib/noResolve/SkipModuleResolution, strictNullChecks,
strictFunctionTypes, noImplicitAny and noImplicitThis; genuine skipLibCheck true.
strictBindCallApply, exactOptionalPropertyTypes and noUncheckedIndexedAccess stay
false/default unless the fixed contract says otherwise. No fake Array/interface
or filtered library excerpt is used. The [grouped demo producer](../../artifacts/collections-investigation/grouped-probe.go)
and [actual results](../../artifacts/collections-investigation/grouped-results.jsonl)
now add `Groups<T> = Record<string, T[]>`, indexed group writes, bucket push/for-of,
Readonly/Partial values and generic reverse-mapped inference, plus concrete tuple
rest/spread. The chosen ES5 environment has no user/library/global diagnostics.
Actual types are `Groups<Row>`, `string[]`, `Readonly<Row>`, `Partial<Row>`, inferred
`{ name: string; score: number; }` from Readonly and `{ score: number; }` from Partial,
and string tuple-rest output. Invalid readonly writes produce 2540; bad Partial
and indexed Record element values produce 2322. These are original-Go scratch
controls, not a production or native claim.

## Observation design and omissions to close before acceptance

1. Replace the single-file node lookup with stable file-qualified declaration and
   syntax identities across user and every pinned library file. Preserve source
   filename/raw bytes, flags, declared ownership, synthetic/detached identities and
   null declaration lists. Do not index a library declaration into the user AST.
2. Keep a complete passive global/library binding graph separately from explicit
   user/reached semantic roots. The old rich observer follows stored globalThis
   type links, then GetPropertiesOfType/GetTypeOfSymbol for every discovered
   property; this can demand every global value type. Explicit declared queries
   call GetDeclaredTypeOfSymbol only for their requested declarations. Avoid that
   accidental all-library semantic traversal. Do not omit a type actually demanded
   by user checking, diagnostics, requested APIs or genuine retained cache links.
3. Even user array-type traversal can force all unused Array member types through
   the old property walk. New library observation must record actual stored member
   identities/links without invoking unused member APIs merely to serialize them.
   Explicitly requested/reached map/filter/reduce signatures, parameters, returns,
   predicates and target/mapper/type arguments must still be complete.
4. Compare complete tuple payloads: element flags/labels, min/fixed lengths,
   combined flags, readonly, target and ordered resolved arguments, rest/variadic
   producer differences and array base/index relationships. Include actual new
   IndexedAccessType/mapped/conditional/intrinsic payloads when demanded by the
   documented ES5 utility types; source syntax admission is a separate boundary.
5. Extend strict key associations beyond the formerly empty generic caches:
   generic signature special and type-list keys, reference/alias/anonymous
   instantiations, tuple interning, mapper/target/isolatedSignatureType, alias-aware
   argument order, relation direction/state. Every populated observed map needs
   actual reconstructed preimages plus actual lookup/value identity, not only
   cardinality or flattened values. Unknown/unrooted key families must fail.
6. Inference contexts may be ephemeral and popped after checking. Retain actual
   stored contexts/mappers when reachable; compare candidate and contravariant
   candidate order, inferred type, priority/fixed/top-level/implied-arity state,
   return mapper and intra-expression sites. Do not manufacture a retained context
   after its producer has released it. If a selected branch has no retained root,
   use a separately identified actual-Go/private-helper observation or source
   closure evidence plus inferred resolved-call signatures, not a fake graph edge.
7. Compare syntax/bind/user semantic/library-policy/global/config/program channels
   separately with complete raw args/chains/related flags/file ownership/order and
   null/empty states. Do not force the old eight missing-global count with libraries.
   Record operations/roots before and after observation to detect cache-producing
   getters; repeat/reverse requests must preserve identities under global-ID shifts.

## Candidate valid/invalid interaction matrix

| Capability | Actual observables and failures |
| --- | --- |
| Generic names/constraints/defaults | Explicit versus inferred arguments, dependent constraints/defaults, shadowing, recursion, wrong arity/constraint and unresolved type names; actual instantiation target/args/alias/mapper identity. |
| Inference/context callbacks | Covariant/contravariant candidates, contextual callback parameter/return inference, repeated call caching, overload candidate failures and generic return inference; no implicit-any success masking. |
| Record arrays and utilities | Map/filter/reduce on mutable/readonly typed records, predicate narrowing of nullable elements, grouped Record values, Readonly/Partial optional properties and nested recursive records; wrong callback returns/property keys/accumulator types. |
| Arrays/tuples/index access | Empty and evolving arrays, contextual literals, readonly relationships, exact/out-of-bounds tuple indexes, optional/required/rest elements, tuple spread arity and labels; genuine array/tuple target/element/index cache states. |
| Rest/spread relationships | Identifier array and concrete-tuple rest, fixed tuple call spread and ordinary array spread, wrong rest type/position/arity, repeated generic instantiation; variadic generic tuple source boundary still root/core decision. |
| Loops/body flow | Ordinary array for-of element typing/assignment/capture/returns, invalid nonarray iteration, evolving-array assignment and loop merge caches; no ES2015 general iterable claim from ES5. |
| Identity/diagnostics/admission | Reordered declarations/requests, same/different generic argument identities, directional relations, exact full diagnostics, unknown foreign node/type/symbol rejection; authored negatives independent from Go semantic comparisons. |

## Upstream assertion provenance

Tracked [checker tests](../../upstream/TypeScript/tsc/internal/checker/checker_test.go)
contain two functions, not an inference suite. TestGetSymbolAtLocation has three
nonnull symbol assertions plus configuration preconditions and bundled libraries;
its body/setup should become applicable with the real environment. Its original
empty compiler options differ from the new fixed strict profile; preserve the
original options through an internal original-test path or label that setup
adaptation honestly. TestGetTypeAtLocationOfTypeOnlyImportClause uses two source
files and module resolution and remains outside a single-user-script contract.
BenchmarkNewChecker is performance, not a correctness assertion.
[Tracer test](../../upstream/TypeScript/tsc/internal/checker/tracer_test.go) is tracing
state, outside semantic checking. The local oracle_semantic_utilities_test.go is
authored instrumentation, not tracked original upstream test evidence. There is
no tracked inference_test.go.

[Collections direct tests](../../upstream/TypeScript/tsc/internal/collections/)
contain nine functions: six behavioral map/set/sync-map bodies potentially required
by actual selected support adapters, one JSON unmarshal body (only if that adapter
is required), and two allocation-count tests. OrderedMapEqualFunc has ten table
rows with both directional assertions; OrderedMap exercises 1,000 entries and
repeat/delete/early-stop iteration; clone checks independent identity and ordered
keys/values. Allocation thresholds are Go-runtime-specific performance assertions,
not portable semantic checks. Scope each behavioral body against actual used
support APIs; do not claim whole collection-package parity from dictionary counts.

Existing [compiler/conformance inputs](../../upstream/TypeScript/tsc/testdata/tests/cases/)
are reusable source inputs, not direct original assertion ports. Candidates include
genericCallTypeArgumentInference, typeInferenceWithTupleType, genericRestParameters1
and constrained/contextual generic-call families. Whole files often include
classes/new, assertions, mutation/loops or nonmatching directives, so distinguish
unchanged eligible whole units from explicitly adapted focused excerpts. The old
foundation splitter inventory can supply source bytes; no repeat full extraction
or compiler-baseline claim is needed. New exact expectations must come from the
actual pinned Go program under the selected authentic environment.

Next: root fixed contract, durable exact demo/corpus ownership handoff, then small
original-Go/managed verticals before broad coverage. No implementation or unchanged
baseline suite rerun is authorized by this investigation note alone.

## Retained mapper producer audit (current)

Pinned `mapper.go` represents fixing/nonfixing inference with a concrete
`InferenceTypeMapper`: actual checker, `InferenceContext` and fixing boolean fields.
Its retained context can therefore be compared after the inference stack clears,
without evaluating a closure or fabricating a graph root. Compare full inference
candidate/contra-candidate order, inferred type, priority/topLevel/isFixed/impliedArity,
context signature/flags, fixing/nonfixing/return/outer-return mappers and intra-expression
sites; preregister contexts before cyclic mapper links. A comparer callback needs
source-producer classification rather than only its function pointer.

`FunctionTypeMapper` has exactly five checker initialization producers: unique literal,
unreliable reporting, unmeasurable reporting, restrictive and permissive. Classify
by exact identity against those original checker-owned mapper fields, preserve mapper
identity, and reject any unknown producer. This does not compare arbitrary captures.

`DeferredTypeMapper` has exactly one producer, inferred type-parameter constraint
resolution at `checker.go:17530`. Targets capture the checker, parent TypeReference,
ordered type-parameter list and individual index. Function pointers cannot prove those
captures. Root approved source-grounded constructor-provenance instrumentation if
needed, labelled authored observation metadata rather than original payload fields.
Validate actual sources/target arity/order and preserve evaluation order; do not call
targets during serialization. Separate source-level conditional/infer queries must
exercise actual target realization. Instrumentation/retained-witness closure is pending.

## Current integration boundaries

[The test inventory](../collections-tests.json) lists all tracked direct checker and
collections tests, with original versus authored instrumentation distinguished. No
original test has been rerun or claimed passed in this campaign. The checker symbol
query test has three applicable nonnil assertions; fixed-library/profile setup differs
from its bundled/default-config host and must be labelled honestly. Collection adapter
mutation/order applicability is being reviewed with support before executable ports.

[Four environment requests](../../validation/collections-corpus/environment-requests.jsonl)
are now independently produced: Array augmentation succeeds with optional property type;
conflicting generic Array merging emits 2428; incompatible Array variable redeclaration
emits 2403 with actual library ownership; Object augmentation succeeds. Full user/library
API ownership, fresh AST isolation and current skip-policy helper runtime remain pending.
Actual Go classifies all four explicit roots as non-default libraries; declaration files
separately skip semantic checking. The observer now reports this bit explicitly.

The first selected Flow source batch restores real evolving-array element/final caches,
loop reference cache and in-process stack, compound assignment, element-reference matching
and for-of initial types. Sixteen source methods were restored; central compilation and
independent review/runtime remain pending. Old Legacy/Callable producers are unchanged.
Remaining Flow closure includes for-of explicit-symbol typing, element-reference key
paths and exclusion proofs for switch/for-in/destructuring/optional-chain guards. Core
owns ordinary array/index/loop checker helpers and CachedTypeKind.EvolvingArrayType.

## Current owned implementation checkpoint

Flow array evolution, loop state, complete reference-key preimages, static element
name lookup and known Symbol-name lookup are restored from the pinned source.
Central build 7 compiles these paths. Actual loop/array behavior remains to be
compared; compilation is not semantic evidence. The new stored-state observer is
opt-in, separate from bootstrap goldens, and performs no semantic getter calls.
Its independent Go discovery is on disk; support owns the disjoint reflection-free
managed payload accessor. Unknown nonempty cache families and unclassified closure
provenance fail rather than serialize opaque success. Cache association closure and
explicit reached roots remain next blockers.

There are no original direct core tests for SameMapIndex, SameMap, Same, Concatenate,
MapIndex or ReplaceElement: every tracked core test file contains only the accepted
BFS and pattern tests. New helper mutation/identity controls will therefore be
classified as authored original-Go differential observations, not assertion ports.
