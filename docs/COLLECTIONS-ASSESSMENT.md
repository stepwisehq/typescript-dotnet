# Generic-collection campaign assessment

**Fixed G1–G6 contract accepted in the delivery commit containing this report.**
Baseline `cfa6d2f`; upstream `fed0bf24149fb1ed36039212648bafdafc1ea10e` unchanged.
The [completion contract](./COLLECTIONS-CONTRACT.md) is satisfied under actual
Linux x64 Native AOT. This is a bounded generic-collection capability, not whole
checker, standard-library, project or compiler parity. The
[durable proof](../validation/results/collections-verified.json) records the complete
658.892-second gate, seven native binaries and zero source/asset/historical drift.

## Capability and environment

The selected capability combines generic aliases/interfaces/functions and inference,
recursive structural relationships, mutable/readonly/evolving arrays, concrete tuples,
array/tuple rest and spreads, keyed/mapped/conditional utility types, contextual
callbacks and ordinary loops. It extends the actual checker; unsupported paths raise
explicit errors rather than becoming successful checks.

The fixed environment embeds the complete, unmodified pinned `lib.es5.d.ts`,
`lib.decorators.d.ts` and `lib.decorators.legacy.d.ts`, in that order, followed by one
user TypeScript script. Every file is parsed and bound; demanded library declarations
retain their actual types, overloads and constraints. `skipLibCheck=true` skips
whole-library declaration-body checking, not demanded type resolution. All four roots
have the original Program's non-default-library classification. Exact options, resource
hashes and loading instructions are in [the guide](./COLLECTIONS.md).

The selected API surface is the 22 named Array and 15 named ReadonlyArray instance
members, ordinary Array call overloads and Array.isArray, plus the actual utility
aliases required by the contract. Complete embedded declaration files do not imply
semantic support for every declared API. ES2015 Iterable/Map/Set libraries are absent;
synchronous iteration uses the pinned checker's genuine array-like fallback.

The [valid grouped-record example](../examples/collections/grouped-records.ts)
combines constrained generic selection/grouping, recursive records, readonly inputs,
Record buckets, contextual collection callbacks, mapped utilities, tuple/rest/spread
and loop accumulation. Its [invalid counterpart](../examples/collections/grouped-records-invalid.ts)
retains all eight original diagnostics. These examples are type-checked, not emitted or
executed as JavaScript.

## Acceptance evidence

| Contract row | Accepted evidence |
| --- | --- |
| G1 authentic environment | Exact complete three-library bytes/order; real global merging, ownership/isolation and diagnostic policy; valid demo has no applicable or missing-global diagnostics |
| G2 generic behavior | Constraints/defaults, explicit/inferred/contextual/recursive/overload success and failure; actual models, inference/mappers and selected cache relationships in all 44 rich comparisons |
| G3 collection behavior | All 37 named members and all 50 named method overload declarations witnessed, all 3 Array call declarations, predicates/isArray, mutable/readonly/evolving arrays, tuples/rest/spread and failures |
| G4 keyed/utility behavior | Actual indexed/mapped/conditional/infer/reverse-mapped behavior and utilities, including required internal representations and selected cache associations |
| G5 bodies/demonstration | Valid grouped app, complete eight-diagnostic invalid counterpart, loop/capture/assignment/narrowing state and independent targeted interactions |
| G6 verification/delivery | Independent source/omission/tool review, actual Linux x64 Native AOT, all retained regressions and committed source/mapping/reproduction assets |

All 44 authored canonical cases match pinned Go under both managed execution and the
fresh native executable, including complete applicable diagnostics and rich semantic
state. Native execution also passes all 12 shared bodies and two request sequences over
eight cases (18 exact executions per runtime, repeating after intervening environments).
All 44 ordinary original outputs are byte-exact with and without observation metadata.
The native comparison took 11.789 seconds; this is an observation-harness measurement,
not a compiler performance comparison. No other platform is verified.

All 23, 467 xUnit cases pass with zero failures/skips: scanner 3, 838, parser 18, 709,
binder 163 and checker 757. The latter comprise 701 retained cases, 12 shared bodies and
44 new oracle cases. Native regression evidence includes 17, 723 scanner, parser and
binder inputs each; 26 targeted and 118 historical binder cases; 3, 153 foundation direct
checks and 17, 633 finite fuzz seeds; 45 scalar, 43 object, 92 named-data and 89 callable
comparisons, plus their retained controls. Historical expected/proof bytes are unchanged.

Direct-test inventory: 12 upstream unit-test functions (3 checker and 9 collection-support)
plus 1 benchmark. Two function projections are explicitly adapted, preserving 3 original
assertions each; 10 unit functions and the benchmark are excluded with reasons. No new
whole upstream test function is claimed assertion-preserving. The 12 shared bodies
comprise those 2 adaptations and 10 authored controls; the 44 semantic cases are authored
differentials. Zero new whole compiler inputs were reused here; the accepted foundation
compiler-input corpus remains in the regression gate. None is reported as a full
compiler-test pass.

Exact witnesses, counts and limitations are in the [test provenance](./collections-tests.json),
[API coverage](./collections-coverage.json) and
[retained-cache ledger](./progress/collections-retained-caches.md). Independent reviews
cover [source fidelity](./progress/review-collections-core.md) and
[verification completeness](./progress/review-collections-verification.md).

Reproduce from the repository root with `python3 tools/run-collections-gate.py`;
record current successful evidence using `python3 tools/record-collections-evidence.py`.
Prerequisites, fixed options, native publication and ordinary demonstration commands
are in [COLLECTIONS](./COLLECTIONS.md). Ordinary native use requires neither Go nor an
upstream checkout. Verification intentionally uses the pinned Go reference.

The comparison distinguishes complete applicable Program diagnostics from raw checker
diagnostics and missing-library diagnostics. It observes the four-file passive binding
relationships and reached semantic models, inference/mapper state, selected cache
associations, identities, nullable entries and loop state. Repeated queries and
intervening-environment process ordering address different reuse risks.

Whole private intern-map census, persistent formatter-cache topology and untouched
allocator backing layout remain unclaimed. Actual reached intern membership and
selected retained-cache associations are distinct evidence. Metadata-only original-Go
overlays retain erased producer inputs; their ordinary outputs must also match the
uninstrumented oracle. This is not a performance or memory-equivalence claim.

## Porting and execution lessons

The direct source mapping is retained in [source-map.json](../source-map.json).
Selected original algorithms remain in their Go-file counterparts. The fixed host,
embedded resource loader, validation protocol and observation metadata are explicitly
authored support. The partial Program port supplies original diagnostic composition;
selected declaration-visibility helpers support diagnostic formatting, not emit.

.NET facilities provide exact resource streams, SHA-256, JSON document reading and
ordered dictionaries where their semantics fit. Managed list adaptations preserve the
selected Go header/identity behavior and retain their documented shared-view limits.
No general Go runtime or collection framework was added.

Three GPT-6.1 Sol owners were reused for implementation and independent review, with
one shared compiler-build owner and isolated verification snapshots. Focused evidence
found real omissions in automatic-array handling, TypeAlias wrapper identity, receiver
predicates, index signatures, diagnostic visibility and function-parameter name tracking.
Those findings were kept separate from observer defects.

Observation work became a serial bottleneck as additional retained-state families were
closed. A concrete mistaken cache-enum classification also caused unnecessary metadata
and a build; those additions were removed, and the existing regularization relationship
was reused. Future work should check named producer/key definitions first and establish
the finite observation inventory against the full selected corpus earlier. Inventorying
actual overload declarations from the start would also avoid late gaps hidden by
method-name coverage. Splitting final corpus assembly and order controls from oracle investigation reduced that
bottleneck without adding agents. Repeating cases after intervening environments made
the order check stronger with the same execution count.

There are 30 numbered central library-build logs, with 142.31 seconds of cumulative
MSBuild-reported elapsed time (range 1.29–6.83 seconds per build). These omit other
builds, oracle/test work and model time; they are not total campaign time or a
productivity comparison with the preceding campaign. One comprehensive acceptance
gate ran and passed in 658.892 seconds; no second complete gate was required. Token use,
monetary cost and total model effort are unavailable; no estimates or subscription-cost assumptions
are used. No user package installation or scope decision has been required during this
campaign.

## Remaining boundaries and next campaign

Prominent unsupported source forms include destructuring, object rest/spread, generic
variadic tuple composition, tuple-constrained rest parameters, assertions, optional
chains, classes/new, modules, async/generators/custom iterators and several control-flow
forms. Source template expressions/types, mapped key remapping and intrinsic string
transforms remain excluded. Required internal library representations are treated
separately. The exact inherited restrictions are in the contract and guide.

A small multi-file project still needs a real user-file host, import/export and alias
resolution, relative module resolution, cycles and cross-file diagnostic/query
ownership. Binding three fixed declaration roots plus one script does not supply those
semantics. The next substantial campaign should address a bounded ES-module environment
with relative imports, named/default exports, re-exports, cycles and cross-file generic
calls, retaining these libraries and excluding package/Node resolution initially.

That would remove a concrete project boundary. Common syntax and broader libraries
would still require further campaigns; selected generic-collection success cannot
establish whole-checker feasibility or ordinary application parity.
