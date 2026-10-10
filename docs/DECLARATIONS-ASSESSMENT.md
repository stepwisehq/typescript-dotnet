# Declaration and object-model campaign assessment

**D1–D6 implementation, review and acceptance gates passed.** Baseline `e7b81a2`, upstream
`fed0bf24149fb1ed36039212648bafdafc1ea10e`. The
[completion contract](DECLARATIONS-CONTRACT.md) controls scope; the
[checkpoint](DECLARATIONS-STATUS.md) and [test inventory](declarations-tests.json)
distinguish implementation responsibilities, behavioral evidence and external work.
The delivery commit contains this assessment and the durable proof; this is not a
whole-checker completion claim.

## Implementation direction

The ordinary configuration invocation now uses genuine Program construction and
checker initialization over its actual options and unchanged bundled declarations.
The exact-three-library gate and hidden library injection are removed. Historical
named test adapters still supply their explicit environments to the same checker;
they are not an alternative production type system.

The selected source closure covers class instance/static/constructor models,
inheritance and access rules, constructor/static initialization flow, members and
accessors, enums, namespaces, augmentation/merging, declaration diagnostics and the
supporting relation, grammar and diagnostic-display paths. Independent source review
is separate from selected behavioral comparisons. Whole checker files remain partial.

Actual library consumers also required declaration deprecation metadata, intrinsic
alias validation, typed ambient binding elements and synchronous iterator type-model
helpers. These are upstream algorithms, not replacement library declarations or
special cases for the demonstration. Operational string mappings, runtime
destructuring, generator bodies and async execution remain distinct unfinished
consumers. Checking intrinsic alias declarations alone does not implement their
operational types.

The disk [warehouse example](../examples/declarations/warehouse/tsconfig.json)
combines an abstract base/derived model, private/protected parameter properties,
accessors, an enum, class/namespace merging, a cross-file ambient interface merge
and relative module imports. Its [invalid companion](../examples/declarations/warehouse-invalid/tsconfig.json)
checks constructor arguments, visibility, accessor writes, incomplete assignments
and abstract construction. Managed and actual Linux x64 native complete diagnostics
and exits agree: the valid project has no errors (exit 0), while the invalid project
has six errors, codes 2345/2341/2445/2322/2741/2511 (exit 1). Both use unchanged ES2022
libraries with `skipLibCheck` left at its false default. This is a concrete usable model within the contract, not an
ordinary-application or complete-standard-library claim.

## Deliberate adaptations and removed scaffolding

- C# atomic checker IDs use `Interlocked` and an unchecked unsigned conversion to
  preserve source counter behavior. Enabled type tracing remains explicitly external.
- Managed pool synchronization implements the actual lazy checker lifecycle.
  Source options, initialization order and semantic acquisition no longer depend on
  a fixture profile.
- Signed integer range compatibility returns an empty sequence for negative counts,
  matching Go. `Enumerable.Range` otherwise throws. Genuine library tuple checking
  exposed this shared adaptation defect; the fix is not tuple-specific.
- Ordinary projects use real source libraries and parsed options. The configuration
  demonstration now requests ES5 explicitly instead of relying on the removed
  injection despite `noLib: true`. Historical captured fixtures and proofs remain
  unchanged; current expectations use fresh genuine upstream behavior.
- Original mapper semantic diagnostic channels have been restored in three direct
  assertion bodies. The generator mapper's remaining protocol assertions are still
  a later responsibility, not a passing full test.

The [source mapping](../source-map.json) retains original counterpart relationships
and the removed authored admission gate's retirement record. Full-file completion
must not be inferred from a selected responsibility's completion.

Property-list comparisons now use the actual public `SymbolToString` result as
`displayNameBase64`. Raw escaped property names included incidental allocation IDs;
prior mismatches and fresh pinned-Go provenance are retained. Property order, associated
types, nil shape and full diagnostics still compare; direct raw symbol observations
are unchanged. Display-name equality does not establish arbitrary identity, so original
required identity and nominal-behavior assertions remain separate. No diagnostic or
name regex normalization was introduced.

## Verification and limitations

The [durable proof](../validation/results/declarations-verified.json) records the
successful frozen gate: **1,163.022 seconds**, exit 0, all five drift checks clear,
and all **218 historical fixture/proof files unchanged**. Actual native acceptance
is **Linux x64 only**. Reproduce it and use the ordinary `--config` CLI through the
[verification guide](DECLARATIONS-VERIFY.md).

| Contract domain | Accepted implementation and evidence |
| --- | --- |
| D1 initialization | Actual Program files/options, ordered global/ambient merges, lazy factories and diagnostic lifecycle; genuine noLib, ES2022 and default-library environments, invalid globals and repeated acquisition. |
| D2 class/object producers | Instance/static/constructor/base/this models, inheritance and mixins, default/overloaded/private/abstract constructors and new; complete diagnostics and selected types/base/signature/relationship queries. |
| D3 members/initialization | Fields, methods, accessors, static blocks, visibility/override and actual constructor/static/post-super flow; invalid visibility, inheritance and path-dependent initialization cases. |
| D4 declaration relationships | Enums, namespaces, ambient/global/external augmentation and merges, required TS aliases/export targets; valid/invalid cross-file declaration and diagnostic comparisons. |
| D5 shared closure | Computed/unique names, declaration relations/grammar/unused/display, demanded genuine-library dependencies; independent source-family review plus interacting success/failure cases. |
| D6 integration | Genuine disk warehouse CLI, original assertions, complete Go comparisons, managed/native execution, order controls and all retained foundation/binder/semantic/configuration/resolution/Program gates. |

New evidence comprises **77 xUnit cases**, **71 complete pinned-Go comparisons** in
each managed/native runtime, **six shared assertion groups**, **26 order/repeat
executions per runtime**, and **two ordinary native disk CLI cases**. The 71 records
are **47 authored source-family cases + 3 environment/demo witnesses + 21 variants
from 20 upstream compiler inputs**. Of 23 selected compiler fixtures, 20 are reused
and three are explicitly excluded for later destructuring/async/decorator consumers.
This is a selected semantic corpus, not an exhaustive upstream compiler-test inventory.

The six shared groups preserve **two complete original checker bodies**, **three
restored original mapper semantic assertion bodies**, and **one authored external
consumer control**. All five original bodies also execute unchanged in Go. Tracer
instrumentation and the upstream benchmark are separately inventoried; neither is
claimed as a new declaration correctness test. See the [test inventory](declarations-tests.json)
for provenance, exclusions and execution records.

All retained regression stages passed. The new affected managed checks also passed
before native publication (Binder 163, Checker 831, Scanner 3,838 and one configuration
integration control). These deliberately repeat some later suite executions; aggregate
TRX totals are execution evidence, not a distinct upstream-test count. The selected-suite TRX aggregate is 24,674 passed case executions, with zero failures
or skips; cheap repeats are recorded separately. The proof retains stage-level counts
and verifies 17 native executable paths as ELF x86-64 without a CoreCLR dependency. Independent cross-owner GPT-6.1 Sol source and assertion review
has no unresolved substantive in-contract finding.

Evidence distinguishes original checker assertions, restored mapper assertions,
upstream compiler inputs reused for semantic comparison, authored source-family
inputs, and explicit unsupported-consumer controls. Compiler-input reuse is not a
claim that an upstream full compiler/emit test passed. A port-specific unsupported
exception is not semantic parity with Go.

The current CLI migration explicitly replaces obsolete authored environment/profile
expectations. It preserves the configuration and resolver algorithm corpora and
original assertions. Complete diagnostics, file attribution, locations and exit
status remain compared for genuinely checked cases. Unsupported JSON/JSX/JS or
decorator consumers are reported separately, subject to actual upstream skip policy.

No other platform, exhaustive correctness, complete library API support or
whole-checker parity is established by this campaign. The
[checker roadmap](CHECKER-COMPLETION-ROADMAP.md) retains the wider no-emit gate.

Generic rest/variadic forms are not universally rejected on the genuine Program path:
existing upstream tuple normalization, rest-signature, inference and relation algorithms
can check them. The whole advanced generic-rest family has not received completion
review and evidence in this campaign. That evidence limitation differs from an
implemented consumer that explicitly reports unsupported behavior; there is no
successful placeholder behind the old named-profile restriction.

## Next dependency-aware campaign

Complete the upstream TypeScript expression/statement and control-flow responsibilities
next: binding/assignment destructuring, rest/spread, optional chains, switch/try/catch/
for-in/labels and the remaining related narrowing/flow transfers. These are direct
barriers to ordinary function bodies using the declaration models now implemented.
Reuse the typed declaration binding and synchronous iterator type-model helpers;
do not port another isolated variant for runtime binding.

Operational template/string mapping, advanced type-system closure, generator/async/
disposal protocols, JS/JSDoc/JSX/decorator/module-option closure and the whole-checker
reconciliation remain separate roadmap obligations. The next campaign's precise
dependency closure must come from the source; this recommendation does not pre-approve
silently omitting demanded algorithms. Large-project correctness/performance comparisons
remain deferred until the whole-checker no-emit gate.

## Observed execution lessons

Three reused GPT-6.1 Sol agents divide core implementation, supporting implementation
and independent review, and verification. A sole checker writer integrates shared
function changes; this avoids simultaneous edits to the largest upstream counterpart.
Source review found missing initializer factories and declaration/module callers
that selected fixtures alone would not establish.

Genuine library checking exposed successive previously excluded consumers. Repeating
the full comparison corpus for each common exception would add little evidence.
After the early full replays, verification uses a representative per distinct cause
and returns to the full corpus after shared blockers clear. Immutable compiled
snapshots allow consumer verification without competing production builds.

This still exposed integration churn: several old profile assumptions survived in
shared producers or dispatch even when their underlying type algorithms already
existed. The useful adjustment was to compare the surrounding upstream producer
family after a concrete failure, not remove a single failing guard and immediately
repeat every test. The first broad 70-record replay after genuine ES2022 checking
cleared produced 63 exact matches, with remaining implementation and observer causes
kept separate. That is interim triage evidence, not an acceptance result.

The first frozen acceptance attempt stopped at the cheap retained Checker suite,
before any native publish, after 171.332 seconds with all five drift checks clear.
It found two obsolete authored globalThis rejection assertions and a real legacy API
exception-category regression. The latter was repaired without changing its assertion.
The former are replaced explicitly by same-input source-backed positive behavior,
with historical files preserved. The new genuine `noLib` witness retains all ten
missing-global diagnostics; the legacy adapter's different eight-global environment
is not substituted for it. Focused corrected tests pass. This is a justified failed
attempt and rerun, not a successful gate or a reason to suppress diagnostics.

Attempt2 then passed the corrected managed checks but stopped on a harness path error:
a nonexistent standalone Projects test project had been listed even though its four
wrappers already run inside Checker.Tests. It took 188.997 seconds, all five drift
checks were clear, and native stages had not started. Removing that phantom duplicate
stage does not remove project assertions or the retained native project cascade.
This second attempt was avoidable verification overhead; checking executable project
paths during preflight addresses the concrete mistake without adding a process framework.

Core recorded **25 central library builds totaling 91.58 MSBuild seconds**; CLI builds
are recorded separately in the [core note](progress/declarations-core.md). Successful
acceptance took 1,163.022 seconds after the two failed attempts described above. These
are measured build/gate costs, not total model work, tokens, subscription cost or compiler
performance. Token/cost telemetry is unavailable. Three Sol agents were reused; no
new package installation or user technical decision was needed to finish this gate.
No optimization or large-project benchmark is part of this work.
