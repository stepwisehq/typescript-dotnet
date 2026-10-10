# Remaining type-system checkpoint

**Accepted: shared TypeScript type-system contract T1–T6.** Baseline `09630d6`;
upstream `fed0bf24149fb1ed36039212648bafdafc1ea10e` unchanged. The delivery commit
contains this checkpoint and the [durable proof](../validation/results/typesystem-verified.json).
No new campaign is active. Read the [assessment](TYPESYSTEM-ASSESSMENT.md),
[contract](TYPESYSTEM-CONTRACT.md), [inventory](TYPESYSTEM-INVENTORY.md) and
[reproduction guide](TYPESYSTEM-VERIFY.md).

Source implementation and independent GPT-6.1 Sol review found no unresolved substantive
in-contract omission. Genuine Program/options/unchanged libraries are preserved. Shared
TS generics, advanced operators, inference, relationships, tuple/callable/predicate
consumers and demanded Unicode/display support are reconciled with pinned upstream.
Whole checker/printer files remain partial because external consumers remain.

Actual Linux x64 Native AOT and managed execution both pass all 77 complete Go records,
seven shared checks in four dispatcher groups, 22 order/repeat executions each and
both ordinary disk CLI cases. Valid exits 0; invalid exits 1 with all six diagnostics
and no missing globals. All 84 new xUnit cases and retained gates pass. Selected TRX
aggregate: 24,849 passed case executions, zero failures/skips; not unique upstream tests
or every inherited repeated run. The successful gate took 1,492.728 seconds. All five
drift flags are false; 256 protected historical files are unchanged.

The seven shared checks comprise six original bodies (one new casing body with all
26 table assertions; five retained checker/mapper bodies) and one authored control.
The 77 records comprise 43 authored source-family requests, two genuine disk projects
and 32 reused compiler inputs from a finite selected inventory of 35; three external
whole inputs remain excluded. Reused inputs are not full compiler baseline passes.
The inventory/assessment distinguish source-reviewed behavior from finite runtime coverage.

Four library and four CLI snapshot builds succeeded, totaling 38.96 seconds. First gate
stopped after 65.547 seconds on a report-reader field mismatch, before retained/native
stages; source, inputs and expectations were unchanged. The narrow independently
reviewed reader fix passed six controls before the successful second run. The assessment
records the genuine source/display repairs and the exact additive public-observer
provenance transition separately. No private instrumentation was added.

Recommended next: async, generators and runtime protocol checking, following the
[roadmap](CHECKER-COMPLETION-ROADMAP.md), then JS/JSX/JSDoc/decorator/module/options
consumers and whole-checker no-emit reconciliation. This recommendation does not start
another campaign. Large-project comparison, .NET upgrades and optimization remain
deferred. Token/cost measurements are unavailable; no additional human decision or
package installation was needed.

For historical implementation/review detail: [core](progress/typesystem-core.md),
[support](progress/typesystem-support.md), [verification](progress/typesystem-verification.md).
The frozen source-inventory JSON files record pre-runtime review state; current runtime
acceptance is the proof/catalog above. Resume from this checkpoint, not an intermediate
packet note; do not rerun accepted work without a concrete reason.
