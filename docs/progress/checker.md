# Bounded semantic checker checkpoint

Status: bounded checker phase complete; final stable Native AOT/xUnit/regression
combined gate passed. No active work or unresolved review blocker. Executable
sources/tools/corpus are frozen; this checkpoint update is documentation only.
Upstream `fed0bf24149fb1ed36039212648bafdafc1ea10e`; accepted binder is baseline.

- Algorithm owner: partial Checker/{Checker,Relater,Flow,Utilities,GrammarChecks,
  NodeBuilder,NodeBuilderImpl,Printer,Exports}.cs and diagnostic-only
  Printer/{Printer,Utilities,Textwriter}.cs. Reviewed hand ports are authoritative.
- Support owner: Types/Links, real paged core link store, CheckFlags/Symbol,
  selected options, full NameResolver plus AST predicates, diagnostic collection,
  raw argument display boundary, generic spelling and required diagnostics.
- Verification owner: authored Admission/VerificationAccess, real Program oracle,
  typed native adapter, corpus, utility/public API controls, xUnit/final gates.
  Algorithm owner is the sole shared library build coordinator.
- API: namespace TypeScript.Checking, Checker.Create(SourceFile); file/global
  diagnostics, GetTypeAtLocation/GetSymbolAtLocation, TypeToString,
  IsTypeAssignableTo. Fixed single-file no-library TS script: ESNext,
  strictNullChecks true, noImplicitAny/strictBindCallApply false, noResolve.
  Real eight missing-library 2318 diagnostics remain separately visible.
- Syntax: scalar var/let/const declarations, nested blocks, scalar/literal/
  parenthesized annotations, literal/identifier/parenthesized initializers.
  Source admission rejects all other syntax/features explicitly; runtime guards
  reject unselected model/flow/relationship/serialization paths. No full checker,
  project/module resolution, library loading or general emit API is implied.
- Inventory: docs/checker-functions.json currently 223 selected/adapted methods,
  exact source/destination locations, retained conditions/cases and rejection
  features. docs/checker-departures.md records scoped initialization and language
  adaptations. Refresh inventory after formatting using
  `python3 tools/checker-function-inventory.py`; this does not regenerate source.
- Reviewed corrections: nullable-target normalization, union singleton recursion,
  GlobalLookup suggestion extras, parenthesized initializer quick fallback,
  named-member nil/sort semantics, genuine query default arms, owned-node guards,
  raw Unicode diagnostic formatting/invalid-run grouping and scalar SymbolToNode.
  Restored original cachedTypes/CachedTypeKey helper boundaries.
- Final command: `bash tools/verify-checker.sh`, guarded full repeat, exit 0.
  [Accepted evidence](../../validation/results/checker-verified.json) records
  source SHA256 `ac38adb32995090a01777c010452f0405aed99e768a370c0178febc196cb940e`
  and verification-asset SHA256
  `c9f669c200dab74c04f908780cdff2ad95ff44ff655bcef034f24d8a52897824`.
- Native checker: all 45 cases match actual Go exactly; 713 AST nodes, 205 symbols,
  88 tables, 215 types, 274 queries, 31 file diagnostics, 360 global diagnostics
  and 121 relations. All 82 checker xUnit tests pass. Native boundary checks pass:
  35 admission controls, 10 authored API assertions and 20 actual Go cache/table
  observations. Native executable is ELF x86-64 with libc/libm dependencies;
  binary identity and byte size are retained in the accepted evidence.
- Earlier-stage regressions on the final source: binder 118 native/122 xUnit,
  parser 471 native/475 xUnit, scanner 1276 native/1287 xUnit, all pass.
- First combined run completed its runtime checks but its proof was rejected by
  the verification-asset hash guard: an inventory-tool indentation metadata edit
  occurred during the gate. This was not a runtime/semantic failure. The entire
  combined gate was repeated with stable final assets and accepted at exit 0.
  [Rejected proof](../../artifacts/checker-validation/asset-drift-gate.json) and
  [accepted repeat](../../artifacts/checker-validation/final-gate.json) are retained.
- Independent reviews: docs/progress/review-checker.md (support reviews algorithms)
  and review-checker-support.md (algorithm reviews support/admission/harness).
- Re-entry: no next implementation task. Preserve this accepted boundary and
  evidence; any future semantic expansion requires a separately authorized scope.
