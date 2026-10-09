# Source-file binding completion verification

Actual Linux x64 Native AOT and regression acceptance passed. The durable
[proof](../validation/results/binding-completion-verified.json) records exact
source, asset and executable hashes, all counts, and the retained failed observer
attempt. The fixed
[contract](./BINDING-COMPLETION-CONTRACT.md) uses foundation baseline `2886d47`
and unchanged TypeScript revision `fed0bf24149fb1ed36039212648bafdafc1ea10e`.

## Reproduce

Use the Linux x64 prerequisites in the repository [README](../README.md): .NET
SDK selected by [global.json](../global.json), native compiler/linker and headers,
Python 3, Git, curl and Node.js. Scripts bootstrap the pinned Go toolchain and
upstream checkout. Initial dependency restoration needs network access.

The campaign environment is Linux x86_64 with .NET SDK 10.0.401 and Go 1.27.1
linux/amd64. Native publication uses the existing local compiler wrapper at
`tools/.cache/native/bin/cc`, invoking GCC 15.2.0 (Ubuntu 15.2.0-16ubuntu1).
System clang is absent. The gate selects that fallback when available; no additional
platform or native compiler configuration is claimed as verified.

From the repository root:

```sh
python3 tools/run-binding-completion-gate.py
python3 tools/record-binding-completion-evidence.py
```

The first command guards source and verification assets, preserves historical
corpus/proof bytes from `2886d47`, and executes:

- Fresh independent Go support/helper and targeted-graph observations.
- One complete foundation and accepted semantic regression cascade, including
  all current xUnit suites, original Go subsystem assertions, shared native
  foundation assertions, finite parser fuzz seeds and broad syntax/scanner graphs.
- The full original numeric baseline sets (156 and 236 deduplicated seeds), in
  addition to the historical foundation gate's shorter smoke calls.
- Actual Linux x64 Native AOT publication and execution of the new binding runner:
  shared API/helper controls, targeted graphs and all 17,723 foundation input units.

The second command refuses failed, incomplete or drifted results, then records
durable hashes, counts, observations, native binary identity and platform details.
It does not run additional suites or alter historical acceptance proofs. Generated
logs, native executables and detailed mismatch packets stay under ignored artifacts.

For a focused managed check, after restoring/building dependencies:

```sh
dotnet test tests/Binder.Tests/Binder.Tests.csproj -c Release
dotnet test tests/Scanner.Tests/Scanner.Tests.csproj -c Release --filter FullyQualifiedName~UtilityTests
```

These focused checks are not substitutes for native or campaign acceptance.

## Comparison and provenance

The independent Go observer derives syntax and bound fields from original Go types;
the C# observer uses static metadata and source-generated JSON for AOT. Both retain
ordered declarations, diagnostics and antecedents, symbol/table/flow/list references,
cycles, flags, source metadata and null/empty distinctions. Unordered tables are
sorted by canonical raw names. Generated names are validated against their actual
lexical owners before process-global counters are replaced by graph references;
the Symbol.Parent relationship remains independently compared.

An independent parse supplies the unbound syntax snapshot. The primary parse is
bound before lazy JSDoc, facts or symbol/container observation helpers. A second
binding call must preserve concrete graph identities, values, metadata nullability
and complete diagnostic state. Explicit prewarmed/lazy controls have separately
recorded observation orders. Helpers run only after the repeat-binding checks.

The [test inventory](./binding-completion-tests.json) separates original assertions,
benchmarks, additional API observations and compiler-input reuse. The pinned binder
package contains only BenchmarkBind, with no correctness assertion to port. Existing
applicable foundation assertions are retained, not counted as new binder tests.
Compiler inputs establish binding comparisons, not full compiler test success.

Natural source cases exercise allocated-empty tables and shared FlowList tails.
Seeded table aliases/list sharing/cycles test observer sensitivity and public graph
support separately. Fresh binding does not alias distinct table owner slots, based
on independently reviewed allocation/write closure. Diagnostic codes 1250/1251/1252
occur only in an unused upstream helper; all 27 reachable binder codes remain required.

## Explicit limits

Only Linux x64 Native AOT is an acceptance target. Finite inputs and source review
do not prove exhaustive correctness, resource-limit equivalence or arbitrary thread
scheduling. BindOnce concurrency controls combine bounded waiting observations,
immediate publication/no-retry checks and review of the synchronization implementation.

Incremental diagnostic RepopulateInfo has no C# model field. Go must report actual
nil; any nonnil observation fails. This is an explicit later-stage omission, not a
fabricated compared default. Arbitrary synthetic/prebound AST repair, backing-array
capacity semantics, checker resolution/types, cross-file/global merging, module or
project resolution, emit, transformations and language services remain outside scope.
