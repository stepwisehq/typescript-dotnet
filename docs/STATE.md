# Current project state

**First bounded semantic checker milestone accepted; no active implementation work.**
The final stable source/assets gate passed on 2026-10-08. Stop here unless the user
provides a new bounded mandate. Do not begin general checking, libraries or projects
from the resume instruction alone.

## Saved checkpoints

- Scanner: `0a50e67`; [historical assessment](./ASSESSMENT.md).
- Parser/performance: `de25b66`; [parser state](./PARSER-STATE.md).
- Binder: committed as `e94cc6f` before this phase; [binder assessment](./BINDER-ASSESSMENT.md).
- First checker slice: accepted, saved **uncommitted**; nothing pushed.
  [Assessment](./CHECKER-ASSESSMENT.md), [verification](./CHECKER-VERIFICATION.md),
  [durable evidence](../validation/results/checker-verified.json).

Upstream stays `fed0bf24149fb1ed36039212648bafdafc1ea10e`. Read
[START-HERE](../START-HERE.md), the exact [mandate](./CHECKER-PROMPT.md),
[source map](../source-map.json), [function inventory](./checker-functions.json)
and [departures](./checker-departures.md).

## Latest verified boundary

Actual Native AOT checker: 45 semantic cases, 274 queries and 121 assignability
comparisons, zero differences. Separate native checks: 35 admission controls
(33 reject/two admit), 20 actual Go utility observations and ten authored API checks.
Checker/binder/parser/scanner xUnit: 82/122/475/1,287, all passed. Native regressions:
binder 118, parser 471, scanner 1,276 cases plus 178 scanner helpers.

Executable source SHA256:
`ac38adb32995090a01777c010452f0405aed99e768a370c0178febc196cb940e`.
Verification assets SHA256:
`c9f669c200dab74c04f908780cdff2ad95ff44ff655bcef034f24d8a52897824`.
Native checker SHA256:
`d886b2e58750043164efa856c984e1e79451967626b4bf669a1a2ee348ecca62`.

The final gate took 47.701 seconds and exited zero. The first run passed runtime
checks but was rejected because an inventory-tool edit changed guarded assets;
that 51.463-second attempt is recorded, not treated as acceptance. All writers
stopped before the full successful repeat. Prior acceptance records are unchanged.

Reproduce with `./tools/verify-checker.sh`; see verification for prerequisites and
recording commands. No repeated gate is needed absent changes, a new environment
or a concrete unresolved concern. Freeze all source/tool/corpus writers before
future evidence runs, including inventory generators.

## Scope and limits

Supported: fixed one-file TS script, scalar variable declarations/nested blocks,
literal/identifier initializers, primitive/literal annotations, real name resolution,
literal freshness/widening, selected scalar relationships, forward/self-reference
flow and semantic diagnostics. The exact admission and runtime boundaries are in
the verification/departures documents. Unsupported input throws explicitly.

This is **noLib/noResolve**, with no supplied standard declarations. Every accepted
case preserves eight actual missing-global 2318 environment diagnostics separately
from file errors. There is no claim of a complete standard-library program or of
full checker parity. No object/function/call, generic/alias, branching flow, module,
project, emit, incremental or service semantics are accepted. Malformed syntax is
rejected by this semantic slice; parser recovery remains independently covered.

Shared globals/globalThis table identity is now compared; allocated empty tables
and page-store semantics have direct Go/native probes. Shared FlowList identities,
full position mapping, unobserved historical diagnostic codes and unrepresented
mutable type caches remain explicit limits. There is no unresolved observed
checker discrepancy. No performance measurement or optimization was performed.

## Recovery and next decision

Three reused, explicitly selected GPT-6.1 Sol agents implemented and cross-reviewed
this phase. The main session coordinated decisions/acceptance; its own model could
not be independently verified through available metadata. All assigned work is
complete. Read [algorithm](./progress/checker.md), [support](./progress/checker-support.md),
[verification](./progress/checker-verification.md), [algorithm review](./progress/review-checker.md)
and [support/harness review](./progress/review-checker-support.md) before changes.

The assessment recommends investigating simple object/interface relationships and
non-generic signatures/calls, with an explicit standard-library/dependency boundary.
That recommendation is not authorization to start. Uncommitted source is saved work;
do not discard it or regenerate hand-ported algorithms from old temporary scripts.
