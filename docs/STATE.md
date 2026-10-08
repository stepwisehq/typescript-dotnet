# Current project state

**Accepted: data-type campaign D1–D7; no active implementation work.**
Actual Linux x64 Native AOT and all earlier regressions passed on 2026-10-08.
Read the [assessment](./DATA-TYPES-ASSESSMENT.md), [fixed contract](./DATA-TYPES-CONTRACT.md),
[capability inventory](./DATA-TYPES-STATUS.md), [verification](./DATA-TYPES-VERIFICATION.md)
and [durable proof](../validation/results/data-types-verified.json).
The [exact mandate](./DATA-TYPES-PROMPT.md) is fulfilled by the campaign delivery commit.
If interrupted before that commit, inspect Git and finish only documentation/commit
and reporting. After delivery, wait for a new mandate; do not start the recommended
next campaign automatically. Preserve saved work; never regenerate hand ports.

## Accepted scope and limits

Standalone single-file TS data shapes: scalar/object aliases, forward/scoped names,
data-only interfaces with multiple inheritance/merging, recursive shapes, unions,
intersections, construction/shorthand/dot reads and structural relationships.
Internal implicit-this/reference/mapper, relation-key, alias and diagnostic support
required by these paths is included. No source generics, functions/signatures/indexes,
arrays/classes, libraries, branching/writes, modules/projects, emit or services.
Exact noLib globals stay visible (normally eight; source builtin declarations can
change upstream results). No typical full application or full checker parity claim.
Historical observation limits remain explicit in the assessment.

## Evidence and reproducibility

Run `./tools/verify-data-types.sh` from the delivery commit. Final frozen gate:
82.979 seconds, exit 0, no source/assets drift. Native exact comparisons: 92 campaign,
43 anonymous-object and 45 scalar cases; 78 admission controls, 20 Go utility
observations and 10 authored API checks pass. All 2,146 xUnit tests pass (262 checker,
122 binder, 475 parser, 1,287 scanner). Earlier native regressions pass: 118 binder,
471 parser, 1,276 scanner and 178 scanner helper outputs. All three independent
reviews have no remaining selected-boundary blocker.

- Source SHA256: `ac9763058844d0ce1daa50fd8c6ba22748c083713592ebd7d2aabf81cfe7b928`.
- Assets SHA256: `80052551e9d70e28315750ee041d4774e4ab48c31994cbbed8b2eb2aaacf47ce`.
- Native SHA256: `cef51515a21d5f087fb1bc356a33dcf25747833fb5e4b3f109b650c1a26135e1`.
- Native size: 6,377,064 bytes, Linux x64 ELF without a .NET runtime dependency.

No source/tool/corpus changes followed this proof. Documentation-only finalization
requires no repeated behavioral gate. Changed executable source/assets would require
new evidence. Pinned upstream: `fed0bf24149fb1ed36039212648bafdafc1ea10e`.

## History and working arrangement

Anonymous-object milestone committed cleanly as `1f7c953` at 2026-10-08 20:13:35 UTC,
without unrelated changes or push. Historical [state](./OBJECT-CHECKER-STATE.md),
[assessment](./OBJECT-CHECKER-ASSESSMENT.md), [evidence](../validation/results/object-checker-verified.json).
Earlier scalar `7e692dd`, binder `e94cc6f`, parser `de25b66`, scanner `0a50e67`.
The current campaign delivery commit is the commit containing this accepted state;
inspect Git history rather than embedding a self-referential hash.

Three existing GPT-6.1 Sol agents handled core, support and verification with disjoint
ownership and cross-review. Coordinator owned scope, decisions, assessment and commit.
Main-model metadata cannot independently confirm Astra; established limitation retained.
Owner records: [core](./progress/data-types-core.md), [support](./progress/data-types-support.md),
[verification](./progress/data-types-verification.md). No active owner tasks remain.
Nineteen numbered integration builds, about 85 minutes 15 seconds from baseline
commit to proof, then documentation/commit. No further user intervention/package
installation, new agents or process framework. Token/cost measurements unavailable.

Recommended next substantial campaign: functions and callable object shapes, after
selecting its inference/library and statement/flow boundary. This recommendation is
not authorization. Optimization and deeper performance investigation remain deferred.
