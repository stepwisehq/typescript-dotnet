# Language-services verification

The [completion contract](./LANGUAGE-SERVICES-CONTRACT.md) is still in progress.
The current [dependency result](../validation/results/language-services-dependencies-74.json)
records a passing managed and Linux x64 Native AOT dependency run, its source hashes,
and its limitations. This does not establish an operational language server.
Subsequent source changes require their own integration verification.

## Shared dependency checks

From the repository root, with the pinned .NET SDK and Node available:

```sh
dotnet test tests/LanguageServices.Tests/LanguageServices.Tests.csproj -c Release
dotnet publish validation/LanguageServices.Validation/LanguageServices.Validation.csproj \
  -c Release -r linux-x64 -p:PublishAot=true -p:StripSymbols=true \
  -o artifacts/language-services/native
artifacts/language-services/native/LanguageServices.Validation
```

The xUnit wrappers and native executable call the same shared assertion bodies.
Original test functions, table rows, partial comparison channels and authored controls
retain separate inventory identities. The regexp executable labels its mixed output
as assertion groups; do not count a selected channel as an entire original function. Original converter tests retain their Node reference process.
Run from the repository so source fixtures and unchanged upstream baselines are found.
Publishing alone is insufficient: execute the produced native binary.

The current dependency74 packet passes **420 xUnit cases**, zero failures/skips,
including eight retained decoder-depth probes. Actual Linux x64 Native AOT executes
323 whole original TypeScript-repository functions, one separately adapted original
watcher runtime observation and62 authored controls. External dependency and partial
channels remain separately reported; see the
[assessment](./progress/lsp-dependencies-74.md),
[independent review](./progress/lsp-dependencies-74-review.json) and
[execution record](../validation/results/language-services-dependencies-74.json).
The retained parser suite additionally passes18,799 data-driven cases:

```sh
dotnet test tests/Parser.Tests/Parser.Tests.csproj -c Release
artifacts/language-services/native/LanguageServices.Validation --selection-depth
```

The second command runs the exact original12,000-parenthesis selection assertion
separately. Production synchronous stack continuation is exercised under Native AOT;
no test-only enlarged stack or reduced depth substitutes for the original input.

The isolated `--ipc-fatal-notification` native control must run as a child with a
bounded timeout and core dumps disabled. Success requires nonzero termination,
the exact `authored fatal IPC notification` marker, and no timeout. Native74 returned
-6 with that marker. This verifies the original notification boundary, not actual
server transport.

All five Registry originals and full Session/Snapshot/refcount/pool assertions now
pass. New complete semantic-service source counterparts are reviewed and compile;
the original editor corpus and actual public stdio server are still pending. A
Native dependency executable can trim uncalled services, so its passing run is not
whole-server acceptance. Comprehensive retained compiler/emit gates remain final
epic work. Earlier dependency records retain their exact historical scopes.

The four authored diagnostics/Emit comparisons use the tracked Go witness with the
same bundled library environment. To reproduce the reference without changing upstream:

```sh
python3 - <<'PYREF'
import json
from pathlib import Path
root = Path.cwd()
out = root / 'artifacts/language-services/context-emit-overlay.json'
out.parent.mkdir(parents=True, exist_ok=True)
out.write_text(json.dumps({'Replace': {
    str(root / 'upstream/TypeScript/tsc/internal/project/context_emit_control_test.go'):
    str(root / 'tools/language-services-oracle/context_emit_test.go')
}}))
PYREF
(cd upstream/TypeScript/tsc && ../../../tools/.cache/go/bin/go test \
  -overlay ../../../artifacts/language-services/context-emit-overlay.json \
  ./internal/project -run '^TestAuthoredSessionDiagnosticsEmitContext$' -count=1 -v)
dotnet test tests/LanguageServices.Tests/LanguageServices.Tests.csproj -c Release \
  --filter 'FullyQualifiedName~SessionDiagnosticsEmitTests'
dotnet test tests/Emit.Tests/Emit.Tests.csproj -c Release \
  --filter 'FullyQualifiedName~EmitModeTests|FullyQualifiedName~CheckerMutexTests|FullyQualifiedName~EmitWriteFailureTests'
```

The first command captures complete reference observations; it does not overwrite
expected values. The shared native dependency runner includes all four candidate
controls. These are authored comparisons, not original Go test functions.

The newly added protocol depth probes have a separate native entrypoint so runtime
stack admission can be tested without terminating the ordinary assertion packet:

```sh
for depth in 16 256 2048 4998; do
  artifacts/language-services/native/LanguageServices.Validation --protocol-depth model "$depth" || exit
done
for depth in 16 256 2048 10000; do
  artifacts/language-services/native/LanguageServices.Validation --protocol-depth any "$depth" || exit
done
```

These probes are authored boundary observations, not original test functions. All eight passed
as separate processes under Linux x64 Native AOT and in xUnit; pinned Go admits the
same inputs. These exercise decoding, not encoder-depth or real server worker stacks.

## Reference and scope

Use revision `fed0bf24149fb1ed36039212648bafdafc1ea10e` in the
[upstream checkout](../upstream/TypeScript/tsc/). The
[TypeScript test inventory](./language-services-upstream-tests.json) and
[external dependency inventory](./progress/lsp-standard-library-tests.json) distinguish
original assertions, partial channels, unexecuted tests and later dependencies.

The small authored normalization reference can be regenerated without changing
upstream sources:

```sh
cd upstream/TypeScript/tsc
../../../tools/.cache/go/bin/go run ../../../tools/language-services-oracle/nfd_public.go
```

Original reference suites can be reproduced from the same upstream directory:

```sh
../../../tools/.cache/go/bin/go test ./internal/lsp/lsproto -count=1 -json
../../../tools/.cache/go/bin/go test ./internal/format -count=1 -json
../../../tools/.cache/go/bin/go test regexp regexp/syntax -count=1 -json
../../../tools/.cache/go/bin/go test golang.org/x/text/unicode/norm -count=1 -json
```

The recorded runs passed31 protocol test functions/122 subtests,7 formatting functions,44 regexp test
functions/31 examples,12 syntax test functions, and26 norm test functions/2 examples/
23,622 subtests. Three original norm functions skipped to avoid downloading Unicode
data; these default runs do not establish their conformance assertions. C# selected
NFD channels use the exact official Unicode17 fixture recorded in their provenance.
Do not count example functions or source inventory entries as executed Test functions.

The [verification design](./progress/lsp-verification-design.md) records the remaining
workspace, original-harness and real-transport requirements. Public server launch and
editor-connection instructions will be added when the actual server is implemented
and verified; `tscheck --lsp --stdio` is currently only the planned command.

## New original and authored comparisons in dependency74

The completion-prefix and deep-selection checks preserve the original Go assertions.
Run the upstream controls with the bundled toolchain:

```sh
(cd upstream/TypeScript/tsc && ../../../tools/.cache/go/bin/go test ./internal/ls \
  -run '^(TestSelectionRangeDepthIsLimited|TestTryRemoveDirectoryPrefixCaseFoldingShrinksPrefix)$' -count=1)
tools/.cache/go/bin/go test strings -run '^TestTrimFunc$' -count=1
tools/.cache/go/bin/go test slices -run '^(TestIndexFunc|TestContainsFunc)$' -count=1
```

The authored document-symbol witness is
[document_symbols_alias_test.go](../tools/language-services-oracle/document_symbols_alias_test.go).
Its historical name does not imply private-header alias parity. Overlay it at
`internal/project/document_symbols_alias_control_test.go` using the same Go overlay
pattern above, then run `TestAuthoredDocumentSymbolChildrenAliases`. Candidate
`DocumentSymbolExpandoTests` compares both complete hierarchical/flat responses and
is included in the shared Native runner. The retained capture log and exact overlay
are linked from the dependency74 execution record.
