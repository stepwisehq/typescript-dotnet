# Emit verification

The whole-emit contract is active. The first synthesis/source-map prerequisite
checkpoint is accepted; this is not yet a user-facing JavaScript/declaration
compiler. [Scope](EMIT-CONTRACT.md), [implementation inventory](EMIT-IMPLEMENTATION.md),
[test dispositions](EMIT-TEST-INVENTORY.md), and
[durable prerequisite proof](../validation/results/emit-foundation-verified.json)
separate implemented responsibilities from final output acceptance.

Run from the repository root with the existing .NET 10 SDK and Linux x64 Native
AOT prerequisites. Restore is implicit in the first build:

```sh
dotnet build tests/Emit.Tests/Emit.Tests.csproj -c Release --nologo
dotnet test tests/Emit.Tests/Emit.Tests.csproj -c Release --no-build --no-restore --nologo
dotnet publish validation/Emit.Validation/Emit.Validation.csproj \
  -c Release -r linux-x64 -p:PublishAot=true \
  -p:OptimizationPreference=Speed -p:UseSizeOptimizedLinq=false \
  -p:ServerGarbageCollection=true -o artifacts/emit/foundation/native
artifacts/emit/foundation/native/Emit.Validation
```

Accepted result: 90 managed cases; native exit0 with 78 original test functions
(42 source-map, 36 generated-name) and30 authored controls. Original functions,
data rows and assertion counts are distinct. Both runners execute the same shared
assertions. Original pinned-Go tests also passed; see the provenance inventory.
Independent review covered original assertions, source algorithms/adaptations,
actual native publication and the evidence manifest.

Retained regressions at this prerequisite checkpoint:

```sh
dotnet test tests/Checker.Tests/Checker.Tests.csproj -c Release --nologo
dotnet test tests/TypeSystem.Tests/TypeSystem.Tests.csproj -c Release --nologo
CHECKER_NOEMIT_ROOT="$PWD" \
CHECKER_NOEMIT_REQUESTS="$PWD/validation/checker-noemit-corpus/requests.jsonl" \
CHECKER_NOEMIT_REFERENCE="$PWD/validation/checker-noemit-corpus/expected.jsonl.gz" \
CHECKER_NOEMIT_PREPARATION_DISPOSITIONS="$PWD/validation/checker-noemit-corpus/preparation-dispositions.json" \
CHECKER_NOEMIT_ACTUAL="$PWD/artifacts/emit/foundation/noemit-actual.jsonl" \
dotnet test tests/Checker.NoEmit.Tests/Checker.NoEmit.Tests.csproj -c Release --nologo \
  --filter 'FullyQualifiedName!~OriginalNoEmitDiagnosticsQueriesAndInvariants'
```

Accepted retained results: 832 checker,84 type-system,101 selected no-emit cases;
combined with emit,1,107 passed with no failures/skips. The filter excludes the
complete original compiler corpus at this prerequisite checkpoint. It does not
remove that later integrated acceptance obligation. Existing corpus regeneration
instructions remain in the no-emit documentation; do not regenerate expected
behavior to accommodate a failure.

Three central managed build attempts resolved namespace/accessibility/overload
integration errors; the third passed with zero warnings. Native publication
succeeded on its first attempt. Existing nullable warnings in retained observation
harnesses are separate from the production build. Token/cost measurements are not
available. No external-project or performance run was performed.

Full printer tests, actual JS/DTS/map outputs, original compiler configurations,
final Native AOT corpus comparison and comprehensive retained gates remain ahead.
The prepared reference registry and one successful original Go smoke establish
oracle readiness only. Linux x64 is the only verified native platform.
