# Functions and callable-object campaign assessment

**Accepted: the fixed C1–C6 callable capability contract is satisfied.** Baselines are source-file binding
`de855ca` and foundation `2886d47`; upstream remains
`fed0bf24149fb1ed36039212648bafdafc1ea10e`. The fixed
[completion contract](./CALLABLES-CONTRACT.md) defines this capability, not an entire
upstream checker subsystem. The verified source fingerprint is
`32e043af7ec8419dbee4ce132be7cb72df84ef56a148d564c1a081dc989302df`.

## Implemented capability

The explicit Callable profile runs actual upstream checking algorithms from parsed,
bound source through semantic diagnostics. It supports ordinary non-generic functions,
arrows, methods, call signatures and callable objects; required/optional/default and
explicit-this parameters; overload ordering/resolution; contextual callbacks; inferred,
annotated and recursive returns; inferred type predicates; directed callable structural
relationships; and interactions with aliases, merged/inherited interfaces, recursive
shapes, unions and intersections. Function variance and method bivariance remain distinct.

Bodies include nested/local declarations, lexical capture, if/else, return, throw,
plain identifier/static-property assignment and the enumerated scalar operators.
Actual branch/effect/reachability and auto-variable inference are retained. This supports
single-file typed domain calculations, branching validation, callback composition and
object methods, including meaningful invalid-program diagnostics. It is semantic checking,
not execution or JavaScript emission. Legacy remains the default accepted data-only profile.

No standard library is injected. Ordinary promise-like diagnostic advice and lexical
arguments fallback follow real upstream behavior. Global diagnostics stay separate and
exact: the 89 reference cases contain 109 checker diagnostic roots and 711 global roots.
One synchronous callback error needs missing Promise (nine globals); caller-written
Object/Function interfaces leave six; the other 87 cases have eight. This is not a
clean-environment or complete-application checking claim.

## Contract evidence

| Row | Managed and source-review evidence | Final acceptance |
| --- | --- | --- |
| C1 declarations/signatures | Full signature fields/identities, parameters/receivers, overload/grammar failures | Passed: managed and Linux x64 native |
| C2 calls | Resolved/recovery signatures, ordered overload consequences, arity/type/receiver failures | Passed: managed and Linux x64 native |
| C3 inference | Contextual callbacks, returns/cycles/captures/defaults, predicates and repeat identities | Passed: managed and Linux x64 native |
| C4 relationships | Variance, callable objects, aliases/interfaces, recursive/composite types, hidden-this mappers | Passed: managed and Linux x64 native |
| C5 bodies/effects | Selected operators, assignments, branches, fallthrough/never, arguments/this and narrowing | Passed: managed and Linux x64 native |
| C6 explicit boundaries | Independent review, original assertions, admission/API/order controls; late findings closed; corrected-source full gate passed | Passed; delivery commit contains this report |

The frozen managed build matches all 89 Go cases and passes all 701 checker xUnit
cases with no skips. Actual observations include 178 signature records, 105 calls,
1,208 repeated queries, two composites, two inferred predicates and three genuine
signature target/mapper associations. Counts describe observations, not completeness.
The [coverage inventory](./callables-coverage.json) and
[verification guide](./CALLABLES-VERIFICATION.md) explain per-case behavior and ordering.

The final comprehensive gate passed in **512.357 seconds**, with **23,411 xUnit
cases** (3,838 scanner, 18,709 parser, 163 binder, 701 checker), zero failures/skips,
and actual execution of freshly published Linux x64 native binaries. All 89 callable
cases, 43 boundary controls, ten API assertions, 304 numeric assertions and 21 order
observations per runtime passed. Retained native scanner/parser/binding broad streams
all matched 17,723 inputs; 26 targeted binding cases, original foundation/shared/fuzz
checks and earlier scalar/object/named-data regressions also passed. Six native binary
fingerprints and source/asset/historical-proof guards showed no drift. See the
[durable proof](../validation/results/callables-verified.json) for exact component counts,
hashes and commands. Other platforms have not been verified.

Ten newly applicable original numeric test functions contribute 152 literal and 152
live Node assertions, shared by xUnit and native execution. Two numeric benchmarks are
excluded. There are no direct evaluator tests at this pin; evaluator coverage uses
original arithmetic assertions, source review and authored semantic comparisons.
The 89 callable cases are authored Go differentials, not full upstream compiler tests.
There are 43 authored admission controls (35 reject, eight admit), ten shared API
assertions and eight request-order control cases replayed in two sequences. See the
[test dispositions](./callables-tests.json).

## Fidelity, dependencies and limits

The [source map](../source-map.json) preserves direct file counterparts; the
[function inventory](./checker-functions.json) records 1,474 selected methods across
this cumulative checker/support port. It is not a count of complete upstream functions
or proof of whole-file parity. Algorithms remain partial where the contract excludes
source forms. New admission, observation and ownership guards are explicitly authored.

Important additional dependencies were real flow/capture analysis, diagnostic rendering,
scoped declaration reuse, promise-like error advice, a constant evaluator and exact numeric
operations. CLR binary64/bit conversion and BigInteger replace Go primitives; the original
256-bit intermediate exponent rounding is retained. Fake name scopes clone eagerly rather
than on first write. Public identity registries admit real synthesized parameters while
rejecting foreign checker objects. See [adaptations](./CALLABLES-DEPARTURES.md).

Prominent limitations remain: arrays/tuples/rest/spreads, source generics, classes/new,
async/generators, loops/switch/try, modules/projects, libraries, emit and language services.
The inherited scalar literal/admission restrictions remain explicit in the contract.
Caller-written declarations describe external values; they do not resolve dependencies.

Observed caches are snapshots at the documented query/visit order, not a simultaneous
whole-checker snapshot. Ephemeral overload candidate arrays are not serialized separately;
their ordered selection and diagnostic consequences are compared. Special signature caches
are empty on the selected sources; isolated-signature/decorator state stays null. The
original scalar projection omits the union regular-type cache, but this campaign’s rich
graph records its actual field. Physical backing allocation/capacity remains omitted.
Late source review exposed and closed local-interface ancestor handling, genuine
merged-interface anonymous instantiations, caller-supplied Symbol/Symbol.for internal
unique types, a merged interface/value declaration walk, and direct-if-body declaration
diagnostic 1156. Nine permanent cases retain the original 80 expected outputs unchanged.
Two merged-interface orders each observe three actual anonymous cache entries; each
of two unique-symbol inputs observes two actual owners. Complete cache key preimages,
map membership, target/mapper/argument/alias identities and whole-map cardinality are
validated. Unknown/unrooted preimages fail; observations do not seed compiler caches.
These repairs close known omissions, not all possible undiscovered defects.

## Process and next work

Three GPT-6.1 Sol owners were reused for implementation and cross review. File ownership
kept concurrent edits separate; moving Flow to the verification owner after its initial
harness checkpoint reduced the core bottleneck. One owner performed 24 central library
builds; isolated adapter snapshots allowed comparison without build races. Baseline hashes
were checked instead of rerunning unchanged suites at the start. No package installation
or user clarification was required during this campaign. Token totals and subscription
costs are unavailable and are not estimated here.

The largest cost was dependency closure: declarations/calls reach flow, contextual and
return caches, diagnostic builders and numeric/promise helpers. Early runtime tests exposed
old guards; source review then found three real gaps after 76 examples already passed.
New witnesses reproduced those failures and drove upstream-grounded repairs. One synthetic-
parameter test premise was corrected from actual Go evidence; expected semantic goldens
were not changed to conceal implementation failures. Waiting for a first coherent compile
made early runtime feedback slower; later grouped failures and isolated snapshots worked
better. The first comprehensive gate cost 529.436 seconds and passed all 23,402 xUnit
and native checks, but late source/cache reachability challenges revealed missing admitted
paths. That run is preserved as pre-closure evidence; the corrected final gate took another 512.357 seconds.
The repeated acceptance work was avoidable: source reachability and cache-producing
witnesses should have been settled before freezing. No performance optimization or new
throughput benchmark was attempted.

The next substantial capability campaign should address generic collection checking:
type parameters/constraints/inference, arrays/tuples and a bounded real standard-library
surface, with iteration/parameter-form dependencies assessed before fixing its contract.
Those exclusions now prevent more ordinary single-file application code than another
small callable feature. Module/project infrastructure and broader libraries would still
remain separate obstacles. This campaign is evidence for a route forward, not an estimate
of remaining whole-checker effort or proof that full application checking is equally easy.
