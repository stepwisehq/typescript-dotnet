# Checker verification checkpoint

Status: accepted exact-source combined gate completed; verification sources/assets remain frozen.
Durable machine-readable proof: [checker-verified.json](../../validation/results/checker-verified.json).

- Pin: maintained TypeScript `fed0bf24149fb1ed36039212648bafdafc1ea10e`, Go1.27.1, .NET10.0.401. Binder checkpoint `e94cc6f` was hash-confirmed before this phase without needless rerun.
- [Independent oracle](../../tools/checker-oracle/main.go) uses actual `compiler.NewProgram` and `checker.NewChecker`, VFS one-file host, ESNext/noLib/noResolve/SkipModuleResolution, strictNullChecks=true, strictBindCallApply=false, noImplicitAny=false. No replacement checker algorithm/library declarations supplied.
- [45 semantic cases](../../validation/checker-corpus/manifest.json):713 AST nodes,205 symbols,88 tables,215 types,274 queries,121 directed assignability pairs (50 true),31 file diagnostics,360 separately retained global diagnostics. Every case preserves the actual eight2318 missing-library errors; fully valid standard-library programs are not claimed.
- [Coverage](../../validation/checker-corpus/coverage.json) includes scalar/type-node queries, literal fresh/regular cycles, boolean union order, raw Unicode/escaped/lone-surrogate values, exact numeric bits, forward/self/out-of-scope/shadowed names, typo-related info, global conflicts and UTF8 formatter truncation.
- [35 authored admission controls](../../validation/checker-corpus/admission-controls.json):33 rejected,2 admitted; kept distinct from Go semantic expectations. Go oracle never applies the C# whitelist.
- [20 actual Go utility observations](../../validation/checker-corpus/utility-expected.json) match native for paged low/high store boundaries, untouched allocated-page slot behavior, symbol pointer-slot nil behavior, lazy allocated-empty maps and identity.
- [10 authored public API checks](../../validation/Checker.Validation/SelfTests.cs) pass native:foreign/orphan/cyclic nodes, repeated queries/diagnostics, Globals/globalThis shared table, Readonly, visible environment errors and explicit built-in object-read rejection.
- Native checker:5,234,456-byte x64 ELF PIE; runtime dependencies libc/libm/loader, no dotnet host. SHA256 `d886b2e58750043164efa856c984e1e79451967626b4bf669a1a2ee348ecca62`.
- Exact source SHA256 `ac38adb32995090a01777c010452f0405aed99e768a370c0178febc196cb940e`; verification assets `c9f669c200dab74c04f908780cdff2ad95ff44ff655bcef034f24d8a52897824`.
- Final command: `python3 tools/run-checker-gate.py`,47.701s,exit0; then `python3 tools/record-checker-evidence.py`. Log `artifacts/checker-validation/final-gate.log`.
- Checker82 xUnit pass. Current regressions:binder118 native comparisons/122 xUnit;parser471 comparisons/475 xUnit;scanner1276 comparisons/1287 xUnit. Prior committed scanner/parser/binder evidence preserved unchanged.
- First complete runtime gate rejected an inventory-tool metadata edit during its asset scope. Executable hash stayed fixed. The whole gate was repeated on the settled snapshot; rejection reason/report retained in durable checker proof and `artifacts/checker-validation/asset-drift-gate.{json,log}`.
- Integration found actual public query fallback gaps and diagnostic invalid-UTF8 run replacement grouping; owners repaired original behavior. Go expectations were not changed to mask a discrepancy. New annotation query IDs expanded observations on unchanged case inputs.
- Independent algorithm/support and harness/admission reviews reported no remaining blockers. No more executable edits or gates pending.

Limits:single-file scalar TS admission only; other modes/modules/library/project/object/call/function/controlflow/general expression semantics reject explicitly. Parse-error recovery is not admitted. Type graph records selected public observations, not every mutable cache:union.regularType was never serialized in either oracle and remains unverified; literal fresh/regular cycles and union constituents are recorded. No diagnostic chains occur in this scalar corpus; related diagnostics do. Parser snapshot precedes checking; postchecking lazy documentation cache is not separately serialized. Binder flow graphs remain covered by the current full binder regression.

Next action: root finalize assessment/source-map/commit. Reentry can verify recorded source/assets/native hashes without rerunning this accepted gate unless sources or evidence change.
