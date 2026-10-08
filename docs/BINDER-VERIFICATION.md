# Source-file binder verification

The bounded source-file binding checkpoint **passed** on 2026-10-08, using
TypeScript revision `fed0bf24149fb1ed36039212648bafdafc1ea10e`. The durable
[evidence record](../validation/results/binder-verified.json) ties the source,
verification assets, requests, Go expectations and actual native executables to
hashes. This establishes agreement on the contract and cases below, not full
binder parity or exhaustive branch coverage.

## Results

| Gate | Passed |
| --- | ---: |
| Binder xUnit | 122 |
| Native AOT binder differential cases | 118 |
| Go/native concurrency, failure and API self-tests | 4 |
| Parser regression xUnit | 475 |
| Native AOT parser cases | 471 |
| Scanner regression xUnit | 1,287 |
| Native AOT scanner cases / helper outputs | 1,276 / 178 |

There were zero observed differences. Binder comparisons covered 5,182 syntax or
synthetic nodes, 858 symbols, 430 tables, 700 flow nodes, 150 linked flow records
and 41 top-level binder diagnostics. Parser regressions covered 19,424 nodes and
347 diagnostics; scanner regressions covered 17,977 tokens and 1,547 diagnostics.
These counts describe coverage; acceptance compares complete canonical records.

The final combined gate started at 17:22:18 UTC and took 37.26 seconds. Its timing
is operational evidence, **not a Go/C# performance benchmark**. No binder
performance conclusion follows from it.

## Reproduction

Use the Linux x64 prerequisites in [README](../README.md), including SDK 10.0.401
and native compiler/linker dependencies. From the repository root:

```sh
./tools/verify-binder.sh
```

This builds the pinned Go oracle, regenerates and compares every stored expectation,
runs xUnit, publishes the binder with Native AOT, compares the native output and
self-tests, then runs parser and scanner gates including their native executables.
It rejects source changes during verification. The local GCC fallback is used
when clang is absent and the prepared compiler exists; a fresh system can install
clang and zlib development packages as documented in README.

To also collect a durable gate record after deliberate changes:

```sh
python3 tools/run-binder-gate.py
python3 tools/record-binder-evidence.py
```

For a managed development loop only:

```sh
dotnet test tests/Binder.Tests/Binder.Tests.csproj -c Release
```

Managed success alone does not satisfy acceptance. Expected results originate in
pinned Go; do not edit them to accommodate the port. Native executables and logs
are rebuildable, ignored artifacts. Historical scanner/parser acceptance records
remain unchanged; current regressions are recorded with the binder evidence.

## What is compared

The [independent Go oracle](../tools/binder-oracle/main.go) discovers fields from
actual Go payloads and embedded bases. It does not read the C# generator schema.
The [C# harness](../validation/Binder.Validation/Runner.cs) uses static typed field
dispatch and source-generated JSON, with reflection serialization disabled.

- Pre-bind and post-bind syntax graphs: exact byte spans, raw scalar values,
  kinds, flags, parents, field roles, child order, null/empty/missing lists,
  source metadata and ordered syntax diagnostic channels.
- Declaration-to-symbol and local/export-symbol links; complete symbol flags,
  raw names, ordered declarations, value declaration, parent and export symbol.
- Locals/member/export tables and scope/container chains, preserving distinct
  identities and null versus allocated tables. Only unordered map enumeration
  is sorted; declaration and edge order remains exact.
- Flow attachment, return/end/fallthrough state, complete flags, syntax references,
  single and linked antecedents, cycles and shared identities. Synthetic switch
  clause data and reduce-label targets/temporary antecedents are included.
- Bound source-file status, symbol count, global exports and pattern ambient
  module metadata; ordered binder diagnostics, including code/category/key,
  raw arguments, formatted text, byte spans and related information.

Canonical graph identities replace allocation addresses and process-global IDs.
Ordinary names remain exact bytes. Only two known generated name forms replace
an embedded counter with its canonical owner: private names and attributed ambient
patterns. The adapter first asserts the numeric counter belongs to the actual
owner and preserves every other byte. Table sorting uses that canonical identity
so a process counter crossing a decimal digit boundary cannot reorder the graph.
Prior-input-order cases check this normalization.

A second bind must leave both output and concrete graph identities unchanged.
Four Go/native self-tests check concurrent BindOnce publication, failed callback
no-retry behavior, default parse-option module names, and concurrent binder
idempotence. Diagnostics fields omitted by the inherited syntax protocol are
asserted to have their expected defaults; checker-only Symbol.CheckFlags is
asserted zero in Go and remains absent in C#, rather than fabricated.

## Corpus and limits

The [coverage inventory](../validation/binder-corpus/coverage.json) contains 100 TS,
12 JS, 2 TSX, 1 JSX and 3 JSON cases. Cases exercise declaration merging/conflicts,
local/export splits, private/computed names, CommonJS/expando assignments, JSDoc,
ambient namespaces/patterns, malformed recovery/raw bytes, and control flow through
branches, loops, labels, switch and try/finally. Three real upstream AST source
files supplement focused specimens. This is selected coverage, particularly
small for JSX/TSX/JSON, not a complete conformance suite.

All 13 atomic FlowFlags bits and all binder-produced SymbolFlags bits were observed.
The corpus includes six cases with flow cycles, 509 shared flow identities,
11 merged symbols, 44 local/export links and 261 next-container links. It does
**not** exercise shared table identities, shared FlowList identities or allocated
empty tables; the protocol represents them, but this checkpoint does not establish
their behavioral fidelity through differential cases.

27 of 30 diagnostics referenced by binder.go were observed. Codes 1250, 1251 and
1252 occur only in an upstream helper with no callsites in the pinned binder.
They remain unexecuted; this is not proof of full branch parity. The readiness
work added 11 parser fixtures exercising 14 of the previously missing 29 codes.
The remaining parser gaps are 1129, 1140, 1142, 1179, 1206, 1228, 1389, 1439,
1441, 1443, 1486, 8011, 8038, 17021 and 18016. Scanner diagnostic 1513 remains
unobserved. See [readiness assessment](./BINDER-PARSER-READINESS.md).

NameResolver, ReferenceResolver, checker/transient state, cross-file/global
semantic merges, module resolution, emit, incremental rebinding and language
services are excluded. Numeric global ID values and allocation strategy are not
cross-process equivalence targets; their relationships are. Upstream binder tests
contain only a benchmark, so passing `go test` alone would add no behavioral proof.
No unresolved observed discrepancy remains after independent
[binder review](./progress/review-binder.md) and
[support/oracle review](./progress/review-binder-support.md).

## Frozen evidence

- Executable source SHA256: `0386f000d85382e8331fa97dee83e50dd469bdca4b90bead795075a18436bf18`.
- Verification assets SHA256: `96b0d68823559d3fdc9b88792c690537159fcd8631a4fe681d2e171546b40549`.
- Binder native SHA256: `11350baa2fcf008fd24faa93c288d4e886d9c02fc2d1cc5b77f07b386db64d31`.

The actual binder executable is a stripped Linux x64 ELF, 4,923,336 bytes, depending
on libc, libm and the system loader, with no installed .NET runtime dependency.
Source and verification assets were unchanged throughout the final gate. The
[evidence record](../validation/results/binder-verified.json) specifies hash inputs
and per-corpus/native regression hashes. Other operating systems/architectures
have not been verified.
