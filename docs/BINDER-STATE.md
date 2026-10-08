# Current project state

**Bounded source-file binder checkpoint accepted; no active implementation work.**
Final combined verification passed on 2026-10-08. Stop here unless the user gives
a new bounded mandate. Do not begin a type checker or repeat completed performance
investigations on re-entry.

## Saved checkpoints

- Scanner: commit `0a50e67`; historical [assessment](./ASSESSMENT.md).
- Parser and basic performance: committed before binding as `de25b66`; historical
  [parser state](./PARSER-STATE.md) and [performance report](./PARSER-PERFORMANCE.md).
- Binder: accepted, saved **uncommitted** in the working tree; nothing pushed.
  [Assessment](./BINDER-ASSESSMENT.md), [verification](./BINDER-VERIFICATION.md),
  [durable results](../validation/results/binder-verified.json).

Upstream remains `fed0bf24149fb1ed36039212648bafdafc1ea10e` in maintained
microsoft/TypeScript/tsc. Read [START-HERE](../START-HERE.md), the exact
[binder mandate](./BINDER-PROMPT.md), and [source map](../source-map.json).

## Latest verified boundary

Actual Native AOT binder: 118 exact canonical comparisons and four semantic
self-tests, zero differences. Graphs cover 5,182 syntax/synthetic nodes, 858 symbols,
430 tables, 700 flows, 150 linked flow records and 41 binder diagnostics. All
122 binder, 475 parser and 1,287 scanner xUnit tests passed. Native parser regressions
passed 471 cases; scanner regressions passed 1,276 cases plus 178 helper outputs.

Executable source SHA256:
`0386f000d85382e8331fa97dee83e50dd469bdca4b90bead795075a18436bf18`.
Native binder SHA256:
`11350baa2fcf008fd24faa93c288d4e886d9c02fc2d1cc5b77f07b386db64d31`.
Verification assets SHA256:
`96b0d68823559d3fdc9b88792c690537159fcd8631a4fe681d2e171546b40549`.
The durable result specifies inputs and all individual corpus/native hashes.
Historical scanner/parser result records remain unchanged.

Acceptance compares declarations/symbol relationships, scopes, ordered diagnostics
and complete represented flow graphs, with pre/post syntax snapshots. Counts alone
are not the contract. Reproduction: `./tools/verify-binder.sh`; see verification
for prerequisites and evidence recording. No repeated gate is needed absent source
or verification changes, a new environment, or a concrete unresolved concern.

## Limits and next decision

There is no unresolved observed discrepancy. This is bounded agreement, not full
binder parity. Shared table identities, shared FlowList identities and allocated
empty tables remain unexercised. Three unobserved binder diagnostic codes occur
in an unused helper. Readiness closed 14 of the previous 29 parser diagnostic gaps;
15 remain unobserved, as does scanner diagnostic 1513. Exact lists and coverage
limits are in the verification report.

NameResolver/ReferenceResolver, checker/transient state, cross-file/global semantic
merges, module resolution, emit, incremental rebinding and language services remain
excluded. Full transform facts and full position mapping are still partial.
No binder performance measurement or optimization was performed.

An initial single-file checker slice is justified as the next study, subject to a
new mandate and explicit dependency inventory. Do not infer authorization from
this recommendation or from the resume instruction.

## Coordination and recovery

The main session owned scope, contracts and acceptance; three explicitly selected
GPT-6.1 Sol agents handled implementation and independent review. Own coordinator
model selection could not be independently verified through available metadata.
Agents have no remaining assigned implementation work. Their persisted records:

- [Binder algorithms](./progress/binder.md).
- [AST/support](./progress/binder-support.md).
- [Verification](./progress/binder-verification.md).
- [Binder review](./progress/review-binder.md).
- [Support/oracle review](./progress/review-binder-support.md).

Uncommitted source is saved work, not disposable scratch. Inspect changes before
regeneration; generated and handwritten ownership is recorded in the source map
and subsystem notes. Do not reconstruct completed work from old conversation.
