# Bounded object checker assessment

**Accepted as a bounded object-checking milestone.** The actual Native AOT
executable matches pinned Go on all 43 object cases and all 45 historical scalar
cases. All 2,033 xUnit tests and native scanner/parser/binder regressions passed.
The final 45.201-second gate had no source or verification-asset drift.
Independent reviews have no unresolved blocker. See
[verification](./OBJECT-CHECKER-VERIFICATION.md) and
[durable evidence](../validation/results/object-checker-verified.json).

Scalar work was committed as `7e692dd` before this phase. New object work is saved
uncommitted; nothing was pushed. Upstream remains
`fed0bf24149fb1ed36039212648bafdafc1ea10e`.

## Supported behavior

The existing single-file scalar checker now handles anonymous object literals and
inline object type annotations, nested data properties, optional/readonly members,
static identifier/string/numeric names, shorthand properties and ordinary dot reads.
It uses upstream contextual typing, freshness/widening, member resolution and
recursive structural relationships. Coverage combines fresh versus indirect excess
properties, required/optional compatibility, weak types, nested mismatches, missing
and misspelled members, duplicate properties/modifiers, unusual names, cycles through
initializers and definite-assignment interactions. Readonly-to-mutable compatibility
follows upstream rather than imposing a different immutability rule.

The 43 object cases compare 398 type/symbol queries and 705 directed assignability
results (485 true, 220 false), with 27 file diagnostics across 15 top-level codes.
Nested chains and related declarations are included. The no-library environment
still reports eight actual missing-global diagnostics per case, separately from file
errors: 344 object-case global diagnostics. These are incomplete environment checks,
not successful checking of complete standard-library programs.

Unsupported syntax and unselected runtime branches fail explicitly. Named types,
interfaces, the standalone `object` annotation keyword, generics, calls/signatures,
index signatures, arrays, element/optional-chain access, spreads/computed expressions,
explicit writes, branching flow, source unions/intersections/keyof, libraries,
modules, projects, emit and services remain excluded. Malformed syntax is rejected
by semantic admission; parser recovery retains its separate regression coverage.

## Fidelity and dependency cost

The [source map](../source-map.json), [function inventory](./checker-functions.json)
and [departures](./OBJECT-CHECKER-DEPARTURES.md) distinguish partial upstream ports
from authored admission/observation support. The inventory has 597 selected methods;
it is not a claim of 597 complete or exhaustively tested upstream functions.
Real recursive relation caches, ternary results, recursion/maybe stacks, deferred
diagnostics, contextual object typing and type-resolution cycles were needed.

The dependency boundary grew within necessary support, not new source-language
features. Best-match errors require finite property-key union/intersection reductions.
Object diagnostic formatting also requires actual enclosing-declaration context,
pseudochecker source-type reuse, cloning, selected tracker callbacks/state and
per-node emission flags.
These are selected original components; the pseudochecker is upstream diagnostic
support, not a replacement semantic checker. General emission remains unavailable.

Managed keys retain the complete selected relation/intersection key inputs instead
of upstream opaque XXH3 hashes. Six actual-key equality probes per case verify
direction, identity normalization and state distinctions. A standard .NET ordered
dictionary preserves upstream widening-property order. Existing raw UTF-8 byte
strings/positions, reference identities and managed allocation adaptations remain.
No optimization or performance comparison was performed.

## Binder identities and remaining limits

New observations cover 15 allocated-empty tables, 35 non-null aliases between bound
symbol member tables and structured member tables, 132 distinct such table pairs,
122 property-target links and 190 transient/declaration-symbol differences. All 43
globals tables retain their globalThis exports alias. This closes relevant observation
gaps for the selected object paths without reopening unrelated binder work.

The corpus observes 46 stable repeated widening results with distinct widened type
identities and 53 populated widening-cache entries. Regular-object cache entries,
cross-object shared member tables, ObjectType target edges and property write/name/
containing-type edges remain unhit. Other private union/context/intersection caches
are not fully serialized. Shared linked FlowLists and full source-position mapping
remain outside this straight-line path; historical unobserved diagnostic codes remain.
Passing cases establish the documented observation contract, not full object/checker
parity. Other operating systems and architectures remain unverified.

## Work process and next uncertainty

Three existing GPT-6.1 Sol agents were reused. The coordinator selected scope and
adaptations; owners implemented algorithms, support and independent verification,
then cross-reviewed each other's work. The established limitation on independently
confirming the coordinator's model through available metadata remains documented.

Workload was rebalanced when formatter/model work initially became ready: the
support owner took the untouched relation file while the main algorithm owner
continued construction/context/flow. One shared build owner, compact discrepancy
packets and independent Go corpus preparation reduced duplicated work. Eight initial
cases exposed integration defects before the broader matrix. Review added one
focused definite-assignment case; the corpus was not enlarged simply to inflate counts.

Several review findings restored actual upstream boundaries: assignment traversal,
shorthand check-mode propagation, separate signature-kind checks, source-context
reuse and literal widening. Differential comparison caught nil collection handling,
dot-name query dispatch, diagnostic selection, escaping flags and union resolution
side effects. Expected behavior was not adjusted to fit C#. An authored JSDoc fixture
was corrected using Go attachment evidence. A provisional diagnosis of builtin
globalThis acceptance was corrected: the value was rejected, but exception metadata
had changed. Future failure packets should state the observed assertion before a cause.

The initial assumption that diagnostic formatting could remain context-free caused
additional work. Earlier inspection of actual diagnostic callers would have exposed
that closure sooner. Per-owner files and checkpoints remained sufficient for recovery;
no database or additional agents were needed. Token totals and monetary cost are
unavailable, and elapsed concurrent work must not be described as labor or subscription
cost. From the scalar commit at 18:43:44 UTC to the first passed gate ending about
19:48:57 UTC was approximately 65 minutes. Documentation and removal of an accidental
extra EOF newline led to a full repeat for the exact delivered source tree, ending
about 19:57:46 UTC, approximately 74 minutes after the commit. Both frozen gates
passed without asset drift (70.14 and 45.201 seconds). These are measured concurrent
elapsed times, not measured labor. Run the whitespace check before freezing sources
next time to avoid this unnecessary repeat. No package installation or additional
user intervention was required.

The next informative slice is **named non-generic object types**, beginning with
type aliases and an explicit decision about recursive aliases before interfaces.
This tests declaration/type identity, named diagnostic serialization and recursion
against the anonymous-object foundation. Interfaces should have a separate initial
dependency check because their thisType/base/merge machinery was the reason for
excluding them here. Calls/signatures remain another substantial slice, not an
automatic addition. No next phase is started by this recommendation.

The result strengthens the route toward real-project checking, but ordinary
projects still require named types, functions, libraries and module/project behavior.
The unexpectedly substantial diagnostic closure is evidence against extrapolating
whole-checker cost from scalar or anonymous-object success.
