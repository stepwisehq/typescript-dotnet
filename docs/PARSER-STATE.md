# Current project state

**Accepted bounded parser checkpoint, 2026-10-08. Basic parser benchmark complete.**
Scanner checkpoint commit: `0a50e67`. Parser changes and evidence form this
checkpoint; nothing has been pushed. Do not restart completed work or begin
compiler stages without a new bounded mandate.

## Mandate and boundary

Read [START-HERE.md](../START-HERE.md), [INITIAL-PROMPT.md](./INITIAL-PROMPT.md) and
[PARSER-PROMPT.md](./PARSER-PROMPT.md). The upstream pin remains
`fed0bf24149fb1ed36039212648bafdafc1ea10e` from maintained `microsoft/TypeScript/tsc`.
The supported contract is initial JS/JSX/TS/TSX/JSON source-file parsing, required
syntax AST support, syntax diagnostics, JSDoc and required reparsing. Binding,
checking, module resolution and code generation remain excluded. Performance
investigation and optimization remain deferred.

The user prioritizes efficient, deliberate verified work over elapsed speed.
The main session coordinates; substantive work and independent reviews use
explicitly selected GPT-6.1 Sol agents. Three agents were reused by domain:

- `scanner`: parser/scanner integration; [checkpoint](./progress/parser.md).
- `support`: AST and syntax dependencies; [checkpoint](./progress/ast.md).
- `verification`: independent oracle/tests/native gates; [checkpoint](./progress/parser-verification.md).

## Final evidence

The combined gate passed on Linux x64 at 16:30 UTC, taking 24.001 seconds:

- Parser xUnit: 464 passed, zero failures.
- Actual Native AOT parser: 460 matching cases, 19,268 nodes, 318 diagnostics.
- Scanner regressions: 1,287 xUnit tests, 1,276 native cases and 178 helper outputs.
- Fresh unchanged-Go expectations, selected upstream tests and frozen source/asset
  hash guards passed. The native parser is 4,088,712 bytes, linked to libc/libm.
- Coverage: all186 parser-factory payload types,247 kinds,78 diagnostic codes;
  68/97 directly referenced parser codes observed,29 still unobserved.

Source SHA256: `d441e371cf28045777b3c8041d51e524ce96904ddecf6ff7e4f105edaf2edf8d`.
See [verification and reproduction](./PARSER-VERIFICATION.md),
[durable results](../validation/results/parser-verified.json),
[assessment](./PARSER-ASSESSMENT.md), and [source map](../source-map.json).
Independent [parser review](./progress/review-parser.md) and
[AST/harness review](./progress/review-parser-support.md) found no remaining blocker.
Five parser algorithm files reproduced byte-for-byte in scratch after documented
adapters/formatting. Handwritten support remains authoritative.

Historical scanner evidence remains in [SCANNER-STATE.md](./SCANNER-STATE.md),
[VERIFICATION.md](./VERIFICATION.md) and its original machine-readable result.
It must not be confused with the new parser/shared-library source snapshot.

## Limits and next action

The authorized basic Go versus C# Native AOT parser benchmark is complete.
Two existing Sol agents implemented and independently reviewed it. Forty samples
across four workloads passed all observer/drift checks, with34canonical preflight
cases. Real TypeScript medians:Go71.0MiB/s,C#60.4MiB/s (1.17x elapsed time);
TSX microfixtures were close with overlapping ranges; JSDoc/recovery were1.40x/
1.26x elapsed time. C# heap allocation volume was higher in allgroups.
See [performance report](./PARSER-PERFORMANCE.md),
[raw results](../benchmarks/parser-results.json), and
[benchmark checkpoint](./progress/parser-benchmark.md). Accepted source hash and
historical scanner results are unchanged. No optimization, profiling or deeper
investigation was performed or authorized. No active work remains.

This is a completed bounded milestone, not full parser/API/branch parity. Remaining
limits include unobserved diagnostics/recovery paths, isolated entity-name API
coverage, source position maps, general transform facts, compiler/service state,
incremental edits and platforms other than Linux x64. Scanner diagnostic1513 is
still unobserved. Exclusions and partial files remain explicit in the source map.

No additional compiler implementation is authorized at this stopping point. A useful next
bounded phase would close targeted parser correctness/API gaps and separately
inventory binder dependencies before selecting an implementation boundary. Keep
performance ideas deferred. Resume by reading these records and inspecting the
working tree; do not rerun every investigation merely to recover context.
