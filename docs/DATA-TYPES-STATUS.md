# Data-type campaign capability inventory

**Accepted: D1–D7 implementation, differential Native AOT execution and independent review complete.**
Fixed [contract](./DATA-TYPES-CONTRACT.md), original [mandate](./DATA-TYPES-PROMPT.md).
Baseline `1f7c953`, committed cleanly at 2026-10-08 20:13:35 UTC.
Upstream remains `fed0bf24149fb1ed36039212648bafdafc1ea10e`.

| Row | Implementation and independent review | Latest evidence | Final disposition |
| --- | --- | --- | --- |
| D1 aliases/names | Selected paths complete; scoped/forward/qualified lookup and invalid meanings reviewed | Rich Go graph/diagnostics exact, including source/binder errors | Accepted |
| D2 interfaces/bases/merges | Multiple bases, merges, hidden references and synthetic any-base indexes complete | Exact member/base/table/target/constraint/index identities and diagnostics | Accepted |
| D3 recursion | Legal property recursion, relations and invalid alias/base cycles complete | Exact cycles, directed relations, repeated identities and diagnostics | Accepted |
| D4 unions | Construction/common members/context/discriminants complete | Deferred fields, real ten-arm map and Cartesian relationship cases exact | Accepted |
| D5 intersections | Distribution, member combinations/reductions/never diagnostics complete | Exact constituents/origins/flags/unique-literal cache and missing-property chains | Accepted |
| D6 integrated use | Named/contextual construction, shorthand/dot reads and structural checks complete | Exact freshness/widening/readability/type relationships and finite-key cache | Accepted |
| D7 boundary/regression | Reviewed explicit admission/retirements and original result channels | 78 controls, 20 Go utility observations, 10 API checks pass native; all earlier regressions pass | Accepted |

## Current snapshot

Build19 library clean, zero warnings/errors. Persistent final managed validation
matches all **92 rich cases** (14 tiny + 78 matrix); recorded comparison durations
0.281 and 0.508 seconds. These describe harness execution, not compiler throughput.
All 78 authored controls pass (7 admitted / 71 rejected), plus 20 actual Go utility
observations and 10 authored API assertions. Source/tools/corpus writers explicitly
froze; source SHA256 is `ac9763058844d0ce1daa50fd8c6ba22748c083713592ebd7d2aabf81cfe7b928`.
All three reviews have no unresolved selected-path blocker:
[core](./progress/review-data-types-core.md),
[support](./progress/review-data-types-support.md),
[verification](./progress/review-data-types-verification.md).

The frozen comprehensive gate passed in **82.979 seconds**, exit 0, with no source
or verification-asset drift. The actual native executable matches all 92 rich cases,
43 anonymous-object cases and 45 scalar cases. All **2,146 xUnit tests** pass
(262 checker, 122 binder, 475 parser, 1,287 scanner). Earlier native comparisons pass:
118 binder, 471 parser, 1,276 scanner and 178 scanner helper outputs.
The native executable is 6,377,064 bytes. See the
[durable proof](../validation/results/data-types-verified.json) for fingerprints.
No further implementation or verification work remains in this contract.

## Observed coverage, beyond totals

The rich graph compares 1,601 queries, 1,138 directed relationships (823 true /315
false), 61 checker diagnostic records, four binder diagnostics and 732 separately
retained global errors. Eight globals appear in 88 cases, seven in four source-builtin
cases. No constant-count/category suppression. Actual graph: 2,859 AST nodes,
1,273 types, 1,277 symbols, 833 tables, 65 alias identities and 152 stable repeated
declared-type queries. Relevant edges include 10 hidden type parameters, two
IndexInfo identities, four IndexType payloads, nine deferred symbol links, one
module export cache and ten existing property-key cache associations.

All logical selected caches distinguish absence/allocated-empty/content. Opaque
hash bits and untouched physical paged-slot placement are excluded explicitly;
actual key-to-value/equality and resolved-node symbols remain compared. Retained
mapper, mapped-symbol and merged-symbol entries remain zero, not claimed covered.
See [verification](./DATA-TYPES-VERIFICATION.md) and
[coverage](../validation/data-types-corpus/coverage.json) for the full observed
field/link inventory and limits. Historical corpus/evidence bytes remain preserved.

## Work observations and recovery

Three existing GPT-6.1 Sol owners were reused, with explicit file ownership and
cross-review. One core build owner; verifier adapter CopyLocal outputs isolate
running snapshots from later library-only builds. Source and fixture hashes guard
comparison snapshots. No extra agents/database/process framework or new user setup.
Token and monetary measurements are unavailable. Baseline commit through final
acceptance took about 85 minutes 15 seconds of concurrent wall time, excluding final
documentation/commit. This is not labor time. No optimization or performance work occurred.

Progress was measured at meaningful boundaries: build1 had 35 integration compile
errors; build5 was clean; build9 ran all initial cases; build10 matched13/14;
build11 matched14/14 and61/75 matrix; build16 matched14/14 and67/77;
build18 matched all92; final build19 retains all92 managed comparisons.
Rich cache observation caught incorrect node-store kinds, omitted reduction effects,
extra empty symbol-cache allocation and alias-error flags. Independent review caught
in-contract guards, publication/caller-flag/state-reset differences, conditional
property-write handling and nil-context behavior. All were repaired through actual
upstream paths, not expected-output edits. Two index-derived triage labels were
corrected; packets now carry explicit field/store names with paths before hypotheses.

Current owner notes: [core](./progress/data-types-core.md),
[support](./progress/data-types-support.md),
[verification](./progress/data-types-verification.md). Hand ports are authoritative;
never rerun stale temporary translators over them. This campaign is complete.
If interrupted before its delivery commit, inspect Git and finish only that commit
and reporting. Do not start the next campaign without a new mandate.
