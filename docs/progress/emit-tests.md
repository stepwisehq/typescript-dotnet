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
