# Language-services verification

The [completion contract](./LANGUAGE-SERVICES-CONTRACT.md) is still in progress.
The current [dependency result](../validation/results/language-services-dependencies-23.json)
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

The current dependency23 packet passed **142 ordinary xUnit cases and eight separate
decoder-depth cases**. Actual Linux x64 Native AOT execution passed86 original
TypeScript dependency functions (including31 protocol and7 formatting functions),
one original external normalization function,17 complete original regexp functions
plus5 selected/adapted channels,7 additional NFD channels,4 partial AST-navigation Go
baseline channels,3 original token subtests and43 authored controls. These units are
not interchangeable. The result records original corpus skips and source/binary hashes.

[Independent review](./progress/lsp-dependencies-23-review.json) checked the result,
counts and hashes. [Codec source review](./progress/lsp-codec-review.md),
[formatting review](./progress/lsp-formatting-review.md) and
[project graph review](./progress/lsp-project-graph-review.md) are clear for their
stated scopes. One post-run test-adapter nullability flag was corrected to upstream;
the original non-null input/assertions are unchanged, no production changed, and the
record explicitly does not claim execution of that corrected flag. The original
AST-navigation JavaScript channels, full service corpus, actual server transport and
comprehensive retained compiler/emit regressions remain pending.

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
