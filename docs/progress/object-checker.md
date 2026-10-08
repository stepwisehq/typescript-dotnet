# Object checker checkpoint

Status: complete and accepted for the bounded object-checking scope below. Final verification-owned full gate exited0; no unresolved blocker or active source work. All43 object cases and45 scalar regressions match Go under Native AOT, and149 checker xUnit tests pass. No expected-output edits were used to resolve runtime differences.

Pinned source: `fed0bf24149fb1ed36039212648bafdafc1ea10e`; scalar checkpoint commit `7e692dd`.

Scope: existing scalar TS script slice plus anonymous finite object literals/type literals, nested required/optional/readonly data properties, static identifier/string/numeric keys, shorthand properties, dot reads, contextual initializer checking, freshness/excess/missing/wrong/weak checks. Named types/interfaces, source unions, calls/signatures/index signatures, spreads/computed expressions/methods, writes, libraries/modules remain explicit unsupported routes.

Ownership:
- Algorithm owner: Checker/{Checker,Flow,GrammarChecks,Utilities,Exports}.cs, required scanner identifier helper, function/branch inventory; single shared library build owner.
- Support: Types/Links, Relater.cs (transferred before edits), AST/diagnostic helpers, diagnostic-only NodeBuilder/Printer and narrow PseudoChecker counterparts.
- Verification: Admission, object introspection friend helper, independent Go oracle/native harness/corpus and gates.
- Root: scope decisions, assessment/state/source mapping coordination.

Implemented selected paths: anonymous member resolution/cache, lazy type literals, transient object/shorthand property identities, contextual stack and nullable-union discrimination, union synthetic property caches, deferred-node/diagnostic queues, widening/regular-object caches, property/non-null facts and flow, selected grammar. Actual relation maps/ternary/maybe stacks/recursion/error elaboration are integrated from support. Internal finite property-key extraction and primitive literal-union intersection reduction serve original diagnostic best-match logic; retained structural intersection payloads explicitly reject.

Adaptations to retain in final inventory: managed identity collections and Go nil-range/length handling; complete relation-key preimage record instead of XXH3 opaque hash; selected intersection cache keys retain ordered IDs/flags; managed allocation and parser/binder adapters unchanged. No performance investigation or optimization is authorized in this phase.

Completed implementation/review: real enclosing-declaration formatter/PseudoChecker reuse is integrated and independently reviewed. Main owner restored source signature query boundaries after optional-union cache review: the actual getSignaturesOfType call resolves MembersResolved even with empty signature slots. Other fixed discrepancies: FE canonical empty-type name, shorthand18004, duplicate-property2717, synthesized Unicode-name escape flags. Support/model/formatter sources are stable.

Final verification: `bash tools/verify-object-checker.sh` exited0. Durable [evidence](../../validation/results/object-checker-verified.json) and [gate report](../../artifacts/object-checker-validation/final-gate.json) record exact source SHA256 `1ce0c30edf2f470ee4a7c527f336980e5a80d8afc17108774c8764d99d6c15b9` with source/asset guards unchanged. Native object comparisons cover885 AST nodes,475 symbols,292 tables,363 types,398 queries and27 file diagnostics, plus344 real no-library global errors.58 authored admission controls,10 public API controls and20 independent Go utility observations pass.

Historical regression gates also pass: binder118 Native AOT cases/122 xUnit, parser471 Native AOT cases/475 xUnit, scanner1276 Native AOT cases/1287 xUnit. Final Debug library build10 had zero warnings/errors; Native AOT executables and their hashes are recorded in durable evidence.

Function inventory now includes all checker/printer/PseudoChecker counterparts (597 selected methods after final formatting); source/package/file preference distinguishes duplicate names. It is an inspection/guard inventory, not a generator or proof of full-source port. Reviewed C# is authoritative.

Next action: none in this phase. Source, tools, corpus and function inventory remain frozen at the verified checkpoint. Future work requires a newly selected bounded scope; do not infer complete checker/library/module/emit support from this gate. Only documentation changed after acceptance. Token/cost metrics are unavailable; no performance measurements or optimization were performed.
