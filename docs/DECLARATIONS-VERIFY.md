# Reproducing declaration verification

Current acceptance status and qualified results are in
[the assessment](DECLARATIONS-ASSESSMENT.md) and the successful gate’s durable proof.
The source reference corpus contains 71 complete
Program records: 47 authored declaration-family inputs, three cross-file witnesses,
and 21 variants from 20 applicable pinned compiler inputs. These are distinct from
the two complete original checker assertion bodies and three restored original mapper
semantic channels. One authored shared group verifies explicit external semantic
consumer boundaries. All six shared groups and all 71 records also run through xUnit
and the actual Linux x64 Native AOT executable.

Run from the repository root after the production packet and reviewed assets are stable:

```sh
DECLARATIONS_CURRENT_CHECKING=1 python3 tools/run-declarations-gate.py --preflight
DECLARATIONS_CURRENT_CHECKING=1 python3 tools/run-declarations-gate.py
DECLARATIONS_CURRENT_CHECKING=1 python3 tools/record-declarations-evidence.py
```

The gate publishes the ordinary native CLI at
[artifacts/declarations-validation/cli-native/tscheck](../artifacts/declarations-validation/cli-native/).
Use the same genuine Program checking path directly:

```sh
artifacts/declarations-validation/cli-native/tscheck --config examples/declarations/warehouse/tsconfig.json --json
artifacts/declarations-validation/cli-native/tscheck --config examples/declarations/warehouse-invalid/tsconfig.json --json
```

The valid warehouse exits 0; the invalid warehouse exits 1 and reports the complete
checking diagnostics. Both use their actual parsed strict ES2022/Bundler options and
unchanged bundled ES2022 libraries. Neither config enables `skipLibCheck`; its default
is false, so library declaration diagnostics participate. No fixed library profile or
hidden root injection is used. The gate performs the native publish; a separate build
is unnecessary to use these commands after verification.

The runner executes unchanged pinned Go tests and authentic Program observations,
managed declaration tests/corpus, three affected retained managed suites (Binder, Checker and Scanner), and the
migrated configuration integration control before expensive native stages. Native
verification repeats complete records, shared assertions and selected forward/reverse
repeat sequences, then checks the actual disk warehouse valid/invalid CLI programs
against complete genuine Go diagnostics and exit codes. Exactly one retained Program
cascade follows, including the retained Projects native checks. Project assertion tests
already run inside Checker.Tests; no separate Projects.Tests project exists. No checker, library or compiler diagnostic text is filtered to make
a comparison pass.

[The manifest](../validation/declarations-corpus/manifest.json) binds committed request,
expected, provenance and fixture bytes. Expected streams are preserved original Go
output; the gate reproduces and compares them rather than generating target-derived
expectations. The Program semantic observer uses unchanged parsed options and authentic
bundled libraries, including actual bulk/per-file/incremental/repeated diagnostics and
selected public symbol/type/property/base/construct-signature queries.
Property `displayNameBase64` uses actual public `SymbolToString` on each returned
property, with its ordered type association retained. The explicit
[observer provenance](../validation/declarations-corpus/semantic-observation-provenance.json)
preserves earlier raw escaped-name evidence and records why allocation keys are not
portable display names; identical display names do not prove property identity.

`DECLARATIONS_CURRENT_CHECKING=1` selects only the reviewed current configuration and
resolution CLI replacements. The new checking base explicitly requests ES5 libraries;
historical noLib inputs, profile-rejection responses and accepted proofs remain
historical. Canonical interpretation/resolution records and original assertions are
unchanged. The new proof reports these intentional authored control migrations, including
two explicit external JSON checking failures that do not count as source semantic parity.

Reports and failed-attempt evidence are under
[declarations-validation](../artifacts/declarations-validation/). A successful frozen gate
can produce [declarations-verified.json](../validation/results/declarations-verified.json)
once; existing accepted evidence is never replaced. Successful acceptance requires every stage and all source/asset/historical drift
checks to pass. Failed-attempt reports remain separate from a successful proof.

The [explicit authored-control retirement](../validation/declarations-corpus/authored-control-retirement-provenance.json) replaces the historical globalThis-read capability ban with the same source checked through genuine Program APIs. Its noLib ES2022 environment retains all ten real missing-global diagnostics; no globals are supplied to hide them. The historical negative JSON and proofs remain unchanged.

The retained native data-boundary tool also selects this same reviewed retirement and the exact 14 current public API assertions only under `DECLARATIONS_CURRENT_CHECKING=1`. Standalone current runs of [check-data-types-boundary.py](../tools/check-data-types-boundary.py) require that context too; without it, the historical authored control selection remains the default. All other admission inputs and utility expectations remain unchanged.
