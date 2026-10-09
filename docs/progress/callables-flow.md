# Callable flow source closure

Verification owns [Flow.cs](../../src/TypeScript.Scanner/Checker/Flow.cs); core owns
Checker-origin predicate creation/body refinement and all shared-library builds.
This source-closure checklist is reviewed at final build 24: all 89 managed
comparisons and all 701 checker xUnit cases pass. The final [Native AOT/regression proof](../../validation/results/callables-verified.json)
passes all 89 semantic cases and all 23,411 xUnit cases, including 701 checker cases,
with no source or asset drift. Original:
[flow.go](../../upstream/TypeScript/tsc/internal/checker/flow.go), pinned revision
`fed0bf24149fb1ed36039212648bafdafc1ea10e`.

Selected implementations now retain pooled flow-state lifecycle, shared-node
memoization, depth-limit diagnostic, function/capture start traversal, assignment
and auto-variable reduction, branch/reduce antecedents, truthiness/equality/typeof/
discriminant/predicate narrowing, effect signatures, reachability, and assignment
marking/last-assignment positions. Narrowing and assignment-reduction cache keys
retain complete identity/direction inputs rather than the upstream hash bits.

| Remaining explicit guard family | Source boundary that excludes it |
| --- | --- |
| Switch/loop/array flow, switch bypass | Source switch/loops/array mutations are rejected; no synthetic loop producer is claimed. |
| Compound assignment, for-in/of | Only ordinary `=` writes are admitted; loops excluded. |
| Auto-array/evolving-array finalization | Array literals/rest/tuples and injected Array libraries are excluded. Scalar/object auto-variable assignments take actual original non-array arms. |
| Binding elements, destructuring | Only identifier declaration/parameter names are admitted. |
| Element/optional access, optional calls | Only static dot access and ordinary calls are admitted. |
| `in`/`instanceof`, delete, super | These written operators/classes are excluded. Ordinary `.constructor` equality remains an implemented original narrowing path. |
| Pattern/template literal types | Written template/mapped types are excluded; ordinary strings and source unions retain actual scalar facts. |

Every successful selected branch uses upstream-derived flow semantics. Excluded
arms throw explicit Unsupported metadata rather than return a fabricated type.
An internal path reaching a guard must reopen this source-boundary assessment.

Required checker-origin closure findings sent to core (verification did not edit
its sources): ordinary function identifier typing; parameter-safe declaration
access; auto implicit-any diagnostics; parameter/non-variable-symbol narrowing;
simple dot assignment targets and expando property flow; real initializer/undefined
property initial type; registration of unused-identifier worklists even with
unused options false. Those original helper boundaries and all integration findings are now source-reviewed
and compiled in build 24. All 89 managed cases pass; native C5 acceptance is established by the actual final gate proof. Earlier build/triage discussion in the
[verification chronology](callables-verification.md) is historical.

The fixed Go corpus has concrete branch assignment/auto inference, captured locals,
predicate positive and non-inference controls, never-return calls, method/arrow this,
function data writes, and thenable diagnostic paths. Runtime exact comparison passes build 24, including the final internal bigint-zero truthiness, nested hidden-this method and arguments-capture review witnesses. Internal flow-loop state is not modeled
as a dummy field: admitted sources have no loop producer, and call cache publication
uses the original zero-loop-stack arm with an explicit boundary note.

## Retained signature cache coverage limits

Original `getErasedSignature`, `getCanonicalSignature`, `getBaseSignature` insert
only for nonempty signature type parameters; inner/outer signature cache keys are
optional-call-chain paths. Generic signature-instantiation type-list entries need
excluded source generic/inference paths. All 89 current cases retain zero entries, checked against global map cardinality;
observer rejects an unknown retained key family rather than flattening it.

Natural hidden-this interface inheritance nevertheless yields three target/mapper
signature pairs without inserting this signature cache. Merged hidden-this cases
also retain actual nonempty anonymous-object instantiation maps, whose complete
key/value/argument/alias associations are now observed separately. The observer retains these actual
mapping edges and repeated-call identity.

`isolatedSignatureType` remains null in the saved corpus. Its original callers are
generic function inference/instantiated generic returns, decorators/JSX, abstract
construct-signature printing, and synthesized getter/setter/decorator functions.
These are excluded source features. No fake isolated type is allocated to increase
coverage; all actual fields remain serialized, so an encountered value is compared.
