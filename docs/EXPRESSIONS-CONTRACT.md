# TypeScript expressions, statements and checker-flow completion contract

Baseline `5efa4e5`; pinned upstream `fed0bf24149fb1ed36039212648bafdafc1ea10e`.
The [mandate](EXPRESSIONS-PROMPT.md), [roadmap](CHECKER-COMPLETION-ROADMAP.md) and
[accepted declarations](DECLARATIONS-ASSESSMENT.md) govern this campaign. This completes
selected upstream synchronous TS responsibilities, not the entire checker. Current
implementation/evidence status is in [the checkpoint](EXPRESSIONS-STATUS.md).

## Completion responsibilities

| Domain | Required upstream closure and acceptance |
| --- | --- |
| E1 Runtime binding/assignment | Array/object binding and assignment patterns, nested/default/rest elements, parameter/local/catch/loop binding, contextual and inferred types, assignment-target validation and diagnostics. Reuse declaration binding, tuples, generic type and synchronous iterable models; preserve their source algorithms and constraints. |
| E2 Composite/optional expressions | Object/array spread and rest type construction, computed/shorthand properties and overwrites, optional property/element/call chains, contextual and overload/callable interactions. Preserve optionality markers, short-circuit result types, evaluation/flow consequences and errors. |
| E3 Synchronous expression dispatch | Remaining TS literal/expression forms: bigint, regex, no-substitution/template/tagged templates, omitted elements, non-null/satisfies/assertion consumers, delete, in/instanceof/private-in, operators/assignments/comma, meta-properties and demanded helpers. Include upstream syntax/target/option diagnostics and inferred types; source recovery is distinct from an unsupported consumer. |
| E4 Synchronous statements | Complete source for-in, ordinary for-of/for/while/do, switch/cases/default, labels/break/continue, try/catch/finally/throw, with diagnostics, return/if/block/expression/empty/debugger statements and ordinary binding/assignment interactions. Preserve source traversal, error recovery, noImplicitReturns/fallthrough/unreachable/label/catch-variable policies where applicable. |
| E5 Checker flow | Complete synchronous TS responsibilities in flow.go: assignment/destructuring/delete transfers, branch/loop/reduce-label reachability, switch discriminants/exhaustiveness and bypass, optional-chain containment, in/instanceof/private-brand/property presence, predicates/assertion effects, closures and constant references. Retain source cache/reentrancy and invalidation algorithms. Assert observed narrowed/inferred types and diagnostics, not just binder graph equality. |
| E6 Integration | Genuine Program/library environments and useful disk example across prior declaration/callable/collection/module capabilities; valid and invalid variants. Applicable original assertions, source-derived differential cases and inferred-type observations, independent algorithm/omission review, actual Linux x64 Native AOT and all retained regression gates, updated mapping/assessment/roadmap and committed clean delivery. |

The source authorities are the current counterparts and pinned
[checker.go](../upstream/TypeScript/tsc/internal/checker/checker.go) source-element and
expression dispatch/producer chains,
[flow.go](../upstream/TypeScript/tsc/internal/checker/flow.go), and required
[grammar/relation/inference helpers](../upstream/TypeScript/tsc/internal/checker/).
Initial family locations and implementation gaps are recorded concisely in
[core](progress/expressions-core.md), [support](progress/expressions-support.md) and
[verification](progress/expressions-verification.md) notes. The historical callable-era
function inventory is not current completeness evidence. No contract narrowing merely
to satisfy a corpus, fixture-specific algorithm, permissive fallback or suppressed
failure is permitted. Port direct source responsibilities and remove obsolete selected
vertical-slice restrictions as their real consumers become supported.

## External boundaries

Async/await, generator/yield, for-await, dynamic-import Promise execution and resource
management/disposal remain later protocol responsibilities. General checked JS/JSDoc,
JSX, decorators and broader runtime module interoperability remain separate. Operational
intrinsic string mappings, advanced type-system completion/key-remapping and other
unfinished type families stay explicit; existing type algorithms are reused, not newly
claimed complete. If a selected synchronous caller requires a proportionate supporting
source algorithm, include it and record why. Report material expansion before undertaking
it; never weaken genuine libraries or silently skip an unsupported dependency.

TS metadata already supported by declaration checking remains supported. Existing module
resolution, Program options, unchanged standard libraries and diagnostic publication
remain genuine. Source skip/check/noLib rules apply as upstream; missing globals and
unsupported semantics must be separately observable, not broadly filtered. Emit,
services, scheduler/build/watch and enabled tracing remain external. No foundation
re-audit without a concrete dependency or discrepancy. Platforms other than Linux x64
remain unverified natively. Large-project correctness/performance comparisons and
optimization remain deferred until whole-checker completion.

## Execution and evidence

Astra coordinates; three reused GPT-6.1 Sol agents implement/review/verify. Core owns
Checker/Types and central builds. Support owns Flow/grammar/relation/inference/display
and supplies bounded Checker patches to core. Cross-owner review challenges both source
omissions and behavioral claims. Verifier owns corpus/oracle/tests/CLI and the final gate.
No overlapping main-file writers; use coherent build snapshots and failure representatives
before broad replay. Internal milestones: binding/statements; composites/optional/flow;
remaining dispatch and source-family closure; integrated independent acceptance. Progress
between them is automatic, not a request for another assignment.

Reuse original assertion bodies where applicable; keep preserved/adapted assertions,
compiler-input reuse and authored comparisons distinct. Preserve complete config/Program/
syntax/bind/global/semantic diagnostics, file attribution/spans/related chains/directives,
and actual inferred/narrowed type observations on both valid and invalid inputs. Include
cross-feature and order/repeat controls where an observable correctness risk exists.
Private-state observation remains a diagnostic tool rather than an exhaustive gate.

Run cheap affected managed consumers before publishing, validate real executable project
paths, and perform one frozen full retained declarations cascade after independent
readiness review. Native xUnit runner publication is unnecessary, but the actual native
checking executable must pass equivalent behavior. Record measured effort and failures
honestly; no token or monetary estimate without telemetry. Successful selected comparisons
are evidence, not proof of exhaustive correctness or whole-file completion.
