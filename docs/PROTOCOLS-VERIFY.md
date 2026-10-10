# Verify asynchronous and runtime protocols

The source-defined [P1–P6 contract](PROTOCOLS-CONTRACT.md) governs this campaign.
Current acceptance status belongs to [the assessment](PROTOCOLS-ASSESSMENT.md) and,
when recorded, [the durable proof](../validation/results/protocols-verified.json).
This guide is a frozen acceptance input; it does not assert a run has completed.

From the repository root, run:

```sh
DECLARATIONS_CURRENT_CHECKING=1 python3 tools/run-protocols-gate.py
DECLARATIONS_CURRENT_CHECKING=1 python3 tools/record-protocols-evidence.py
```

The recorder requires the entire successful frozen gate and refuses to overwrite an
existing proof. The gate validates real project paths, source/provenance/request/output
hashes, pinned original sources, historical evidence and reviewed assertion obligations.
It executes original Go tests and complete records, new xUnit/managed checks, one retained
[type-system cascade](TYPESYSTEM-VERIFY.md), then actual Linux x64 NativeAOT checks and
ordinary CLI. There is no additional full retained consumer-suite pass. Selected TRX sums
are aggregate execution records, not distinct case totals or all repeated executions.

Use the ordinary native checker produced by that gate:

```sh
artifacts/protocols-validation/cli-native/tscheck --config examples/protocols/job-stream/tsconfig.json --json
# exit 0; no diagnostics
artifacts/protocols-validation/cli-native/tscheck --config examples/protocols/job-stream-invalid/tsconfig.json --json
# exit 1; six complete diagnostics
```

The shipped configuration loads authentic ES2022 and ESNext.Disposable declarations,
strict checking and default `skipLibCheck: false`. It checks async/await, sync/async
Generators, for-await, and using/await-using across actual source files. No emitted
JavaScript or resource cleanup execution is claimed. Native publication is performed
once for the new validator and CLI by [the shell](../tools/verify-protocols.sh):

```sh
dotnet publish validation/Protocols.Validation/Protocols.Validation.csproj -c Release -r linux-x64 -p:PublishAot=true -o artifacts/protocols-validation/native
dotnet publish src/TypeScript.Check/TypeScript.Check.csproj -c Release -r linux-x64 -p:PublishAot=true -o artifacts/protocols-validation/cli-native
```

The [manifest](../validation/protocols-corpus/manifest.json) contains 94 records:
40 authored source-family cases, two disk integration cases, and 52 directive variants
from 42 reused whole compiler inputs (46 inventoried, four explicit whole-input
dispositions). Shared execution has eight checks: seven complete original bodies
(checker2, mapper4, casing1) and one retained authored external-consumer check. The mapper
generator body is newly restored with both original policies and all actual bulk/global/
incremental assertions; earlier original bodies are retained, not new ports. New xUnit
execution has 102 cases. Order controls execute two fresh sequences, 22 observations per
runtime. Complete diagnostics retain filenames, spans, arguments, chains, related
information, and all genuine options/libraries; inferred type queries use existing public
APIs. Neither emitted baselines nor incidental private graphs define acceptance.

Historical mapper/corpus/proof bytes remain unchanged. The retained declaration and
expression guards accept only their exact original source set or the fixed reviewed
[full-generator restoration](../validation/protocols-corpus/mapper-generator-restoration-provenance.json).
Current authored retained CLI controls still require the exact reviewed
`DECLARATIONS_CURRENT_CHECKING=1` context documented in [the preceding guide](TYPESYSTEM-VERIFY.md).

The [complete future no-emit inventory](../validation/checker-noemit-inventory/provenance.json)
records 12,824 original inputs, 15,190 configurations, and 42 original pre-configuration
skips. It is bound as an obligation inventory, not a protocol semantic pass. Reproduce it
with `python3 tools/generate-checker-noemit-inventory.py --check`. Applicable original
checking and full type/symbol walker assertions remain required before whole-checker
acceptance; output/declaration-emit and emit-induced diagnostic invariants are explicitly
separate operations. Original skips are not automatic whole-checker exclusions.
