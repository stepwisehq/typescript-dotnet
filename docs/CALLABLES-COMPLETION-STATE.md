# Current project state

**No active implementation campaign. The fixed functions/callable-object contract is
accepted and delivered in the commit containing this state.** No push or subsequent
campaign is authorized. Start here after session loss; do not resume historical failed
builds or regenerate hand-ported code from translation scaffolds.

## Accepted checkpoint

Read the [exact mandate](./CALLABLES-PROMPT.md), [contract](./CALLABLES-CONTRACT.md),
[assessment](./CALLABLES-ASSESSMENT.md), [verification](./CALLABLES-VERIFICATION.md),
[adaptations](./CALLABLES-DEPARTURES.md), [coverage](./callables-coverage.json) and
[durable evidence](../validation/results/callables-verified.json).
Baseline binder `de855ca`, foundation `2886d47`; upstream remains
`fed0bf24149fb1ed36039212648bafdafc1ea10e`. Earlier binding state is preserved in
[BINDING-COMPLETION-STATE.md](./BINDING-COMPLETION-STATE.md).

Source SHA: `32e043af7ec8419dbee4ce132be7cb72df84ef56a148d564c1a081dc989302df`.
Verification-assets SHA: `3af53ddfe83af1841091aa73608fb2fe1460adfdc8b8ef5f08628ba668e06a95`.
Final Native AOT gate: Linux x64, 512.357 seconds, all drift flags false.
All 23,411 xUnit cases pass (3,838 scanner, 18,709 parser, 163 binder, 701 checker).
All 89 actual-Go callable cases, 304 original numeric assertions, 43 admission controls,
ten API assertions and 21 order observations per runtime pass. All retained foundation,
binding and earlier semantic regressions pass; six native binary fingerprints match.
Only Linux x64 is verified. Reproduction: `python3 tools/run-callables-gate.py`, then
`python3 tools/record-callables-evidence.py` only after a complete successful run.

## Scope and preserved limitations

Callable uses the real upstream checker with fixed noLib/noResolve ESNext,
strictNullChecks/strictFunctionTypes/noImplicitAny/noImplicitThis. Legacy retains its
accepted default behavior. Ordinary non-generic functions/arrows/methods/callable objects,
overloads, contextual/inferred callbacks/returns, predicates, receivers and selected
bodies/flow/operators are implemented. Arrays/rest/tuples/generics/async/classes/broader
statements/libraries/modules/projects/emit remain excluded. Missing-library diagnostics
remain exact and separate. Read the contract before interpreting successful checks.

All late review findings are closed: local interfaces, actual nonempty merged-interface
anonymous instantiation caches, unique-symbol dependencies from caller declarations,
interface/value merge handling and direct-if-body diagnostic 1156. Independent strict
key/owner/cache observations and nine new cases preserve the original 80 expectations.
The earlier 529.436-second pass is pre-closure evidence only, with six binaries and an
87-file manifest under `artifacts/callables-validation/before-cache-closure-470449302c/`.
Do not substitute its proof for the final corrected-source evidence.

## Resumption and process

The same three explicitly selected GPT-6.1 Sol agents implemented and cross-reviewed:
core owned Checker/GrammarChecks/Utilities/Exports and 24 central builds; verification
owned Flow/admission/oracles/tests; support owned models/relations/formatters/numerics
and final gates. Root owned scope, mapping, assessment and delivery. Main-session model
metadata limitations remain disclosed; no model substitution was authorized. Token and
cost telemetry are unavailable. No user clarification or package installation was needed.

For a new authorized phase, use these state/contract/mapping records and file ownership
instead of reconstructing old chat. Do not repeat broad foundation/binder audits without
a concrete discrepancy. Source-reachability/cache-producing witnesses should be settled
before expensive final gates. The recommended next substantial capability is generic
collection checking with a bounded real library surface; it is a recommendation, not
permission to start. Ordinary full application checking remains unavailable.
