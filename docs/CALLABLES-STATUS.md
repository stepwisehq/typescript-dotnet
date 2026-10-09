# Callable campaign status

**Accepted: fixed C1–C6 contract satisfied.** Baseline `de855ca`, foundation
`2886d47`, upstream `fed0bf24149fb1ed36039212648bafdafc1ea10e` unchanged.
See the [contract](./CALLABLES-CONTRACT.md), [assessment](./CALLABLES-ASSESSMENT.md),
[verification instructions](./CALLABLES-VERIFICATION.md) and
[durable proof](../validation/results/callables-verified.json).

| Row | Disposition |
| --- | --- |
| C1 signatures/declarations/parameters | Implemented, independently reviewed, exact managed/native graph and diagnostic comparison |
| C2 calls/overloads | Implemented, including ordered selection/recovery and argument/receiver failures |
| C3 contextual/return inference | Implemented, including callbacks, cycles, captures, inferred predicates and identities |
| C4 relationships/objects | Implemented, including variance, named/recursive/composite types and nonempty hidden-this caches |
| C5 bodies/effects | Implemented for the explicit statement/operator boundary, including flow and builtin diagnostic dependencies |
| C6 acceptance | Actual Linux x64 Native AOT, all regressions, strict observations and independent reviews pass; committed delivery contains this status |

Final build 24 matches all 89 actual-Go cases; all 23,411 xUnit cases pass (701 checker),
zero failures/skips. The native gate took 512.357 seconds and passed 43 boundary controls,
ten API assertions, 304 original numeric assertions and eight order controls yielding
21 exact observations per runtime. All retained scanner/parser/binder/checker regressions
pass. Source/assets/historical proof and six native binary checks show no drift.
Other platforms are unverified. This is contract completion, not whole-checker parity.

Nine late regression cases close local-interface ancestry, nonempty merged-interface
anonymous caches, actual Symbol/Symbol.for unique types, interface/value merging and
original diagnostic 1156. All original 80 expectations remain byte-identical. A prior
529.436-second full pass predates these repairs and is preserved only as pre-closure
evidence under `artifacts/callables-validation/before-cache-closure-470449302c/`.

The same three explicitly selected Sol owners implemented and cross-reviewed the work:
[core](./progress/callables-core.md), [verification](./progress/callables-verification.md),
[support](./progress/callables-support.md). Their lower historical notes explain earlier
failures; this status and [STATE](./STATE.md) govern resumption. No next campaign is
authorized. Token/cost telemetry is unavailable; reported counts and times are measured.
