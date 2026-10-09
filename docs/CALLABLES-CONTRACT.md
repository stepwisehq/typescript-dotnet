# Functions and callable-object completion contract

Status: fixed contract satisfied; Linux x64 Native AOT verified. [Mandate](./CALLABLES-PROMPT.md).
Baselines: source-file binding `de855ca`, foundation `2886d47`, accepted scalar/object/
named-data semantics. Upstream remains `fed0bf24149fb1ed36039212648bafdafc1ea10e`.
Do not narrow this contract to declare completion. This is a substantial useful
single-file callable capability, not all upstream functions or whole-checker parity.

## Capability and options

The new explicit `CheckerProfile.Callable` uses the same ported upstream checker,
with fixed noLib/noResolve, ESNext, strictNullChecks, strictFunctionTypes,
noImplicitAny and noImplicitThis. It does not enable strictBindCallApply,
noImplicitReturns, exactOptionalPropertyTypes or claim all strict options. The
existing default `Legacy` profile retains its accepted admission and option behavior.
The named profiles and admission are authored API boundaries, not substitute checkers.
No broad compiler-option configuration/project system is introduced.

Input is one syntactically valid TS script. Preserve existing admitted scalar,
named/recursive/interface/object/union/intersection behavior under the new profile,
with actual upstream option-dependent diagnostics. In-contract semantic errors are
checked and diagnosed, not classified as unsupported merely because they are invalid.
Malformed syntax, JS/JSX/TSX/JSON, declaration files and source modules remain outside
semantic admission and retain their accepted parser/binder regression protection.
The inherited scalar literal set is string/number/boolean/null. Bigint and symbol
type annotations remain admitted, but written bigint literals were not part of the
accepted scalar entry and are not added here. This does not exclude internal zero-
bigint literals or operations/narrowing on bigint-typed parameters and variables.

Required source capability:

- Ordinary named function declarations, named/anonymous function expressions,
  expression/block arrows and nested functions with lexical captures.
- Function types, interface/type-literal call signatures, static-name method
  signatures and object methods, function-valued properties and callable objects
  with data properties. Include interface merging/inheritance, aliases, recursive
  shapes, unions and intersections of admitted types and their callable members.
- Non-generic overload declarations with implementations, overload sets and original
  candidate ordering/selection/failure behavior; callable union/intersection signature
  combination, not simply choosing the first signature.
- Required, optional and defaulted simple identifier parameters, and an explicit first
  `this` parameter. Preserve parameter/return type annotations, contextual parameter
  and return inference, default-initializer checking, arity, return compatibility,
  receiver applicability, method `this` and lexical arrow capture.
- Calls without spread/type arguments, higher-order functions/callbacks, inferred and
  annotated returns, recursive return resolution and actual cycle diagnostics/fallbacks.
  Infer predicates from ordinary boolean-return functions where upstream does so,
  and preserve their signature payload and call-based narrowing effects.
- Function-body and top-level blocks, local variables, nested admitted functions/types,
  `if`/`else`, `return` in functions, `throw`, expression statements and simple `=`
  assignment to identifiers/static dot properties, including function data properties.
  Cover multiple returns, fallthrough/void, never/throw paths, captured variables,
  assignment flow and implicit-any auto-variable inference required by this profile.
- Scalar arithmetic/bitwise/equality/relational/logical/nullish operators, conditional
  expressions, unary numeric/not/bitwise-not, `typeof` and `void`; preserve errors for
  invalid admitted operand types. No special-casing literal fixtures.
- Source-written `declare function` and `declare` variable declarations may describe
  caller-supplied external values with admitted types. These are ordinary source
  declarations, not harness-injected library stand-ins or actual dependency resolution.

Prominent parameter/body exclusions: rest parameters, array/tuple types or values,
destructuring, spreads, generators/async/await/yield, constructors/new/classes/super,
loops/switch/try/catch/finally/labels/break/continue, compound assignments/increments,
accessors, computed names, optional/element access, `in`/`instanceof`/`delete` and
comma expressions. Source generics/type arguments, explicit type predicates/assertion
signatures, type assertions/type queries, polymorphic source `this` types/ThisType<T>,
written index/mapped/conditional/keyof/indexed-access types, enums and namespaces
remain excluded. Admit invalid combinations within supported forms to produce actual
upstream diagnostics; do not turn arbitrary checker failures into unsupported success.

No standard-library declarations are added. Existing missing-global diagnostics stay
separate and exact; source declarations can alter their number or add builtin shape
errors. Preserve actual upstream missing-global error/fallback identities on internal
paths. A real upstream any/error fallback is distinct from a permissive port placeholder.
Broad libraries, generics, modules/resolution, projects, emit, incremental compilation
and language services require a material scope decision before expansion.

## Necessary dependency closure and fidelity

The initial audit identifies thousands of original checker lines across declaration,
signature construction/resolution, calls/overloads, contextual inference, returns,
assignments and body checking. Complete the selected original branches with their
function/file organization, names and ordering intact. Supporting models must include
real Signature/CompositeSignature/TypePredicate/SignatureLinks and required node/type/
symbol caches, including transient parameters with nil declarations. Internal hidden
this parameters, mappers and inference machinery required by non-generic source are
not excluded just because source generics are. Do not implement a miniature checker.

Real signature relationships must preserve original callback handling, strict function
variance, method bivariance, parameter/return checks, composite relationships and
recursive caches. Diagnostic rendering and pseudo-node builders must represent actual
function/signature/receiver types and recursive errors. Static AOT observations must
not replace or mutate compiler results to simplify comparison.

Arrays/rest are deferred for a concrete reason: upstream array creation depends on
Array/ReadonlyArray generic globals, while rest/spread relationships add tuple and
iteration machinery. Ordinary functions, selected assignments/branches and explicit
receivers have no identified material dependency beyond the chosen single-file path.
If implementation contradicts that finding, report the dependency before broadening.

Implementation refinement: ordinary call-error and truthiness diagnostics can inspect
awaited/promise-like types even without async source. Preserve actual missing-global
lookups/fallbacks and non-generic structural `then` behavior required by these paths.
An otherwise admitted object is not excluded because its property is named `then`.
This diagnostic dependency does not authorize source async, fake library declarations
or broad generic/conditional-type expansion; report concrete scope pressure first.

## Acceptance rows

| ID | Required behavior | Evidence |
| --- | --- | --- |
| C1 | Signature/declaration/parameter model and overloads | Full signature graphs, declaration/parameter/this identities, flags/minimum arity, overload order, grammar/merge/body diagnostics |
| C2 | Calls and overload resolution | Actual resolved signatures/candidates, argument and receiver types, return/predicate results, valid/invalid arity/type/overload interactions |
| C3 | Contextual and return inference | Typed higher-order callbacks, contextual parameter/return results, nested captures, defaults, recursive/circular inference, inferred predicates and cache identity |
| C4 | Callable relationships and object integration | Directed compatibility, strict function versus method variance, callable aliases/interfaces/merges/recursive shapes/unions/intersections, method/data property interactions |
| C5 | Function bodies and effects | Selected expressions/statements/assignments/branch returns, void/never/fallthrough, receiver/arrow capture, implicit-any and call-predicate narrowing behavior |
| C6 | Boundaries and complete acceptance | Independent unsupported controls/profile distinctions, exact global diagnostics, reviewed omissions, Linux x64 Native AOT, all established regressions, clean committed delivery |

Each row needs implementation, successful and failing actual-Go observations, xUnit,
actual Native AOT execution and independent correctness/omission review. Observe
signature order, parameters, type/predicate/composite/target/mapper links, resolved
calls and logical signature/node/symbol caches, not just counts. Exercise repeated
queries and newly reachable identities/cache associations. Canonicalize global IDs
only by validated relationships; test query/order sensitivity explicitly.

Define observation order. Keep historical fixtures unchanged; add an opt-in callable
schema including complete nullable diagnostic metadata and separately identified
missing globals. Unknown reachable Go payloads must fail observer discovery. List
unobserved fields/states and omitted caches explicitly. Test inputs reused from
compiler suites are not full compiler-test passes. Original applicable tests retain
assertion provenance; authored differentials remain labeled as such.

## Execution, resumption and stopping

Reuse the three explicitly selected GPT-6.1 Sol agents: core owns Checker.cs, GrammarChecks.cs, Utilities.cs and Exports.cs, and is sole
shared-library build owner;
support owns Types.cs, Relater.cs, Mapper.cs and required diagnostic-builder/printer
counterparts; verification owns Admission.cs/profile boundary, independent oracles,
corpora, observations, tests and gates, plus the selected Flow.cs implementation.
Flow ownership moved after the first reference/harness checkpoint to reduce the
core dependency bottleneck; core/support must independently review that code. Coordinate APIs/ownership before overlapping
edits. Root owns scope, decisions, mapping/state, assessment and commits. Existing
coordinator model-metadata limitations remain disclosed; no silent model substitution.

Internal progression: real signature/parameter/return model and typed calls; contextual/
body/inference closure; overload/composite/relationship interactions; independent
review/coverage closure; a frozen comprehensive native/regression acceptance.
These are internal milestones, not requests for another assignment. Build coherent
batches, validate small independent Go/JIT cases, run affected xUnit early after
admission or global-ID changes, and use isolated adapter snapshots for parallel work.
Do not rerun foundation/binder completeness audits without a concrete discrepancy.

Preserve historical proof/corpus bytes and run their complete regression protections
at final acceptance, including full original numeric finite baselines and broad
foundation/binder native comparisons. Review new files/whitespace before freeze.
Track actual builds/runs/elapsed time where available; no invented token or cost totals,
no optimization/profiling or workflow database. Maintain concise owner notes and a
capability table. Continue until C1–C6 are met and committed with a clean tree, or a
material boundary/access blocker requires a decision. Do not relabel partial work.
