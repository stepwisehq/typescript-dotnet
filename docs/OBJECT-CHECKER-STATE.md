# Current project state

**Bounded anonymous-object checker milestone accepted; no active implementation work.**
The full Native AOT/regression gate passed on 2026-10-08. Stop here unless the user
provides a new bounded mandate; the resume instruction alone does not authorize
named types, libraries, calls or project checking.

## Saved checkpoints

- Scanner: `0a50e67`; [historical assessment](./ASSESSMENT.md).
- Parser/performance: `de25b66`; [historical state](./PARSER-STATE.md).
- Binder: `e94cc6f`; [historical assessment](./BINDER-ASSESSMENT.md).
- Scalar checker: committed as `7e692dd` before this phase;
  [assessment](./CHECKER-ASSESSMENT.md), [historical state](./SCALAR-CHECKER-STATE.md).
- Object checker: accepted, saved **uncommitted**; nothing pushed.
  [Assessment](./OBJECT-CHECKER-ASSESSMENT.md), [verification](./OBJECT-CHECKER-VERIFICATION.md),
  [durable evidence](../validation/results/object-checker-verified.json).

Upstream stays `fed0bf24149fb1ed36039212648bafdafc1ea10e`. Read the exact
[mandate](./OBJECT-CHECKER-PROMPT.md), completed [plan](./OBJECT-CHECKER-PLAN.md),
[source map](../source-map.json), [function inventory](./checker-functions.json)
and [departures](./OBJECT-CHECKER-DEPARTURES.md). All 85 library C# files map once;
112 mapping entries include 78 verified origin hashes. The 597-method inventory
tracks selected partial branches, not complete checker parity.

Final delivery proof was repeated successfully after removing one accidental extra
EOF newline in generated AST source. The original 70.14-second passed gate is
preserved in the evidence metadata; no semantic or verification-asset change occurred.

## Latest verified evidence

One Native AOT checker executable matches Go on 43 object cases (398 queries,
705 relationships, 27 file diagnostics, 344 global diagnostics) and all 45 scalar
cases. Native controls: 58 current admission controls, 20 actual Go utility
observations, 10 authored API checks and 258 actual relation-key observations.
Checker/binder/parser/scanner xUnit: 149/122/475/1,287, all 2,033 passed.
Native regressions: binder 118, parser 471, scanner 1,276 plus 178 helpers.

The exact-final-tree gate took 45.201 seconds and exited zero. All source/tool/corpus
writers acknowledged freeze before it began; source and assets remained unchanged.
Historical scalar corpus and all four earlier acceptance JSONs are byte-identical.

- Source SHA256: `1ce0c30edf2f470ee4a7c527f336980e5a80d8afc17108774c8764d99d6c15b9`.
- Asset SHA256: `d27278a4a82192fc3b13fc54f2f90c7914c068b3628828e31723527f3ac2e30d`.
- Native SHA256: `f66e0c539ef8b237fbcfa4974e183eaf36e3e0bf1d9b7b1661a71ec36062eab8`.
- Native executable: Linux x64 ELF, 5,682,440 bytes, libc/libm, no installed .NET runtime.

Reproduce with `./tools/verify-object-checker.sh`. See verification for prerequisites
and guarded recording commands. The historical scalar-only gate asserts two old
object-syntax rejections; use scalar commit `7e692dd` for that historical gate.
Current selection explicitly retires those two negatives, preserving their bytes
and all 45 semantic baselines. No repeated gate is needed absent source/assets changes,
a new environment or a concrete unresolved concern.

## Boundary and remaining gaps

Fixed single-file TS script, noLib/noResolve, strict null checking. Existing scalar
behavior plus anonymous object/type literals, nested optional/readonly data properties,
static names, shorthand, dot reads, contextual typing, freshness/widening and real
recursive structural relations. Unsupported input/branches throw explicitly.
Every case retains eight missing-global 2318 environment errors separately.

No named types/interfaces, standalone `object` annotation keyword, generics,
calls/signatures/indexes, arrays, explicit writes, branching flow, source unions/
intersections/keyof, libraries/modules/projects, general emit or services. Internal
finite key reductions and actual pseudochecker/context/emit-flag support are confined
to selected diagnostic and semantic dependencies. Malformed syntax is rejected by
semantic admission; parser recovery retains independent coverage.

Relevant identity evidence now includes 15 allocated-empty tables, 35 non-null
binder/structured-member aliases, 132 distinct table pairs, 122 property-target
links and 190 transient/declaration-symbol differences. There are 53 Widened cache
edges and 46 stable repeated widening probes. Regular-object cache, cross-object
shared member tables, object-target and write/name/containing-type edges remain
unhit; other private caches are only partially observed. Shared FlowList identities,
full position mapping and historical unobserved diagnostic codes remain explicit limits.
No unresolved observed discrepancy or independent-review blocker remains.

## Working arrangement and recovery

Three reused GPT-6.1 Sol agents implemented and cross-reviewed this phase. The main
session coordinated scope and acceptance; the established limitation on independently
confirming its own model through metadata remains. No extra agents or database were
needed. Approximately 65 minutes elapsed from scalar commit through the first passed
gate and 74 minutes through the final proof after documentation and EOF cleanup.
These are concurrent elapsed times, not labor; token/cost totals were unavailable. No additional user
intervention/packages or performance investigation occurred.

Read the completed [algorithm](./progress/object-checker.md),
[support](./progress/object-checker-support.md),
[verification](./progress/object-checker-verification.md),
[main review](./progress/review-object-checker.md) and
[support review](./progress/review-object-checker-support.md) checkpoints before changes.
All executable/tool/corpus work is finished and frozen. Do not regenerate hand ports
from stale temporary scripts or discard uncommitted work.

The next recommendation is named non-generic object types, starting with aliases
and an explicit recursion boundary before deciding on interfaces. That is a
recommendation, not authorization. Follow [START-HERE](../START-HERE.md) and
[RESUMING](./RESUMING.md); do not restart completed work.
