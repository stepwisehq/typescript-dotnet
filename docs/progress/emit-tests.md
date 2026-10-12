# Emit tests/reference and independent review checkpoint

Baseline `e3131a5b`, pinned Go `fed0bf24149fb1ed36039212648bafdafc1ea10e`.
Whole-emit acceptance remains pending; do not infer it from this dependency packet.

The [inventory](../EMIT-TEST-INVENTORY.md) records printer106, source-map42 and
transform2 original top-level functions, original compiler configuration identity,
all full-output assertion channels and explicit product/harness dispositions.
[prepare-emit-registry.py](../../tools/prepare-emit-registry.py) reuses the accepted
decoded inventory and source hashes: 12,824 fixtures, 15,330 configurations, seven
original expansion failures retained with failure text. Configured baseline names
are unique; noCheck/declaration recompilation happens inside the unchanged original
JS baseline helper and does not record competing baseline paths.

The additive [original runner](../../tools/emit-oracle/runner_test.go) calls original
full compiler preparation and every original assertion method. It captures raw
JS/DTS/maps/EmitResult and original baseline contents through the existing tracking
facility, keeping original assertion failures. Preparation/policy/channel skips
remain separate. Original suite cleanup prevents stale local deletion markers.
Overlay preparation reuses the existing accepted diagnostic projection; it does not
replace upstream production or test sources. Build/run are explicit later actions.

One Go compile and representative full-harness smoke passed; evidence is under
[reference infrastructure](../../artifacts/emit/reference-infrastructure/result.json).
This establishes machinery only, not emit-product parity. The final incremental compile/smoke also passed with original cleanup and
preparation-skip disposition capture; final source hashes are bound in that record. No full oracle export has run.
Go cache is shared at `/home/greg/.cache/go-build`; one reusable original executable
is approximately59MiB. The prepared registry is approximately15MiB, without emitted
whole-corpus copies or per-fixture binaries.

Shared [NameGeneratorChecks](../../validation/Emit.Checks/NameGeneratorChecks.cs)
ports all36 original functions and54 active expectations unchanged. Genuine source
parsing/binding, identities, callbacks and scope behavior remain. Independent test
source review is clear in
[name-generator review](../../artifacts/emit/foundation/namegenerator-test-independent-review.json).
The [shared project](../../validation/Emit.Checks/Emit.Checks.csproj),
[xUnit project](../../tests/Emit.Tests/Emit.Tests.csproj) and
[Native executable](../../validation/Emit.Validation/Program.cs) execute the same
bodies. Original function and authored control counts remain separate.

Independent [source-map review](../../artifacts/emit/source-review/sourcemap.json)
covers all six production files, the exact borrowed scanner overload, original42
functions/49 leaf rows and19 separate authored controls. Two concrete findings were
fixed: unknown huge JSON numbers must be skipped by typed parsing; binary-search
subtraction must not wrap at a minimum query position.
Independent [synthesis review](../../artifacts/emit/source-review/synthesis.json)
covers context/factory/names/helpers/flags, transformer/modifier foundation and exact
Core/AST/diagnostic-printer joins, plus11 authored controls. Maximum-delete Splice
arithmetic and default empty literal text findings were fixed. Constant helper text
bytes match pinned Go; immutable snippet values preserve the actual read-only emit
consumer and independent copy boundaries. Review records bind final frozen hashes.

Pinned Go direct references passed: source-map42 functions/49 leaf cases, and only
the selected36 original name-generator functions from printer. These are not a
full-printer result. Root coordinates managed builds, actual Native AOT and retained
regressions. Continue with full resolver/printer/transform/Program dependencies,
then full original compiler output comparison; external projects remain deferred.

First dependency packet managed xUnit passed90 cases with0 skipped. Actual Linux
x64 Native AOT executed78 original functions and30 authored controls using the same
shared bodies. Retained checker832 and type-system84 regressions passed; selected no-emit101 regressions passed. The
[independent checkpoint evidence review](../../artifacts/emit/foundation/checkpoint-independent-review.json)
binds the observed logs, TRX results and native executable. Root owns the dependency
checkpoint commit.
These gates establish this dependency packet, not whole-emission acceptance.

Next packet source preparation: remaining70 printer functions comprise66 printing
functions and4 utility functions. Shared originals now contain65 printing functions
and4 utilities, including553 active parsed input rows,15 binary precedence rows,
8 nullish mixing rows and70 utility leaf rows. The single
`TestPartiallyEmittedExpression` original body remains verbatim in a pending text
source until genuine TypeEraserTransformer exists; xUnit explicitly reports that
one dependency skip and the Native runner reports the pending function. Trailing
comma transforms use the original genuine EmitContext node visitor and are runnable.
The first integrated managed test run passed all65 original printer functions; three utility functions failed through the pre-existing decimal-only FormatUint adapter and one authored reference-resolution control failed. One genuine TypeEraser function remains explicitly pending. Owners are resolving these concrete failures; integrated packet acceptance remains pending.
Pinned compiler emit workload inventory corrected to three benchmarks, including
`BenchmarkEmitLongLinesWithLineBreaks`; none is a direct assertion test.

Complete pinned Go printer package reference passed106 functions/745 leaf cases,
0skip/fail, including the genuine TypeEraser-dependent function; individual outcomes
are in [printer-original-result.json](../../artifacts/emit/reference-infrastructure/printer-original-result.json).
Semantic resolver review is source-clear in [resolver.json](../../artifacts/emit/source-review/resolver.json),
with concrete NaN/borrowed-text/empty-string findings fixed and the demanded checker
reference-cache leaf reviewed. Underlying NodeBuilder and integrated behavior remain
separate. Shared50 transform prerequisites received independent foundation source
review; root central build identified two declaration-only compiler joins, now fixed.
Printer production review is active; semicolon indentation forwarding must commit
pending punctuation exactly as pinned source. No new packet acceptance yet.

Independent printer source review progress is recorded in [printer.json](../../artifacts/emit/source-review/printer.json). Writers, utility algorithms, Core line-start value enumeration and SourceFileSource are reviewed; main printer review continues beyond the type/binding dispatch. Concrete semicolon indentation, NUL escape and borrowed template-text findings were corrected by the owner. Comment trimming must use Go Unicode WhiteSpace rather than the JS predicate; the owner is correcting this and authoring a separate witness. Complete demanded FormatUint radix2..36 source extension reviewed clear. Resolver controls now include five authored functions, including a genuine Program declaration-projection fixture; final expectations and integrated execution remain under review.

Full printer production/writers/utilities/source-file/synthetic-file review is now source-clear in [printer.json](../../artifacts/emit/source-review/printer.json), including final borrowed numeric and JSX consumer fixes. Comment and source-map save/restore state, list identity/trailing commas, helper clone/sort boundaries and exact original unsupported JSDoc panic were checked. Root integrated managed/Native acceptance remains pending; the accepted source-map/synthesis packet was not re-reviewed.

TypeEraser completion is active: complete original transformer body and69 original direct TestTypeEraser table rows are source-written, plus the original TestPartiallyEmittedExpression now calls that genuine transform. No build or execution claim yet; independent foundation review is required before integrated acceptance. The previous explicit pending body remains preserved as provenance until final metadata reconciliation.

Pinned Go tstransforms package reference passed once:2 original functions and89 leaf rows (TypeEraser69, ImportElision20),0skip/fail. [Individual reference dispositions](../../artifacts/emit/reference-infrastructure/tstransforms-original-result.json) bind the command and log. C# ImportElision remains unimplemented; this original reference is reusable for that later family. Owned source/tests remain frozen for the next central batch.

Central test4:169 xUnit cases executed,168pass/1authored comment-control failure/0skip. All149 original emit functions passed, including all70 printer originals and TestTypeEraser69 direct rows. Actual Native AOT pending; original expectations unchanged. Removed superseded TypeEraser pending text after exact activation review. Updated individual original dispositions in printer-test-provenance.json and prepared seven root mapping entries in /tmp/emit-shared-source-map-entries.json (Utilities22, Destructuring28, TypeEraser5, RuntimeSyntax1 partial leaf, three original test mappings); root source-map unchanged.

Final printer/resolver/TypeEraser integrated gate passed:169 emit xUnit records,832 checker,84 type-system and101 selected no-emit records (1,186 total), zero failures/skips. Actual Linux x64 Native AOT executed149 original functions and46 separately authored logical controls through the same shared bodies. The authored comment expectation was corrected only after exact pinned Go/C# byte agreement established the original indentation space; production and original assertions were unchanged. Earlier test1 failures and test2/test3 compile joins remain recorded in the [source-bound proof](../../validation/results/emit-printer-resolver-verified.json). Full compiler-output corpus and remaining transforms/Program emission are subsequent obligations; this is a subsystem gate.
