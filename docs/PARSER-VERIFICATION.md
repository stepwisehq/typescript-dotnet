# Parser verification

**Accepted bounded checkpoint, 2026-10-08.** All final gates passed against the
unchanged upstream pin on Linux x64. This establishes meaningful syntax agreement
for the supported modes and corpus, not full parser/API or branch parity.

| Gate | Result |
| --- | --- |
| Fresh pinned Go outputs equal stored expectations | Passed |
| Selected upstream parser tests | Passed |
| Parser xUnit | 464 passed, zero failures |
| Actual Native AOT parser comparisons | 460 passed, zero differences |
| Scanner xUnit regressions | 1,287 passed |
| Actual Native AOT scanner regressions | 1,276 cases and 178 helper results passed |
| Frozen source and verification assets | Hashes unchanged |

The parser comparison includes 19,268 nodes, 247 kinds, all 186 payload types
directly referenced by parser factory calls, and 318 diagnostics: 285 parse,
17 JS and 16 JSDoc. It observes 78 diagnostic codes overall and 68 of the 97 codes
directly referenced by parser source. The remaining 29 are explicit coverage gaps.
The scanner retains its previous unobserved lexical diagnostic 1513.

The published parser is a stripped 4,088,712-byte ELF64 executable linked to
libc/libm; it executes without an installed .NET runtime. Publication used SDK
10.0.401 and the existing local native compiler fallback. The combined frozen
gate took 24.001 seconds; its 0.291-second parser comparison is test execution
timing, **not** a Go/C# performance benchmark.

[Durable machine-readable evidence](../validation/results/parser-verified.json)
records toolchain, corpus hashes, test counts, native identity and gate details.
The verified source SHA256 is
`d441e371cf28045777b3c8041d51e524ce96904ddecf6ff7e4f105edaf2edf8d`;
native SHA256 is
`c0d12d543b3f3ae226dfa94ed080f8fc69183393855410c3635a9f920d762ffa`.

## Reproduction

Use the pinned upstream and Linux x64 prerequisites described in the
[README](../README.md), then run from the repository root:

```sh
./tools/verify-parser.sh
```

To also retain command timing and a durable evidence record:

```sh
python3 tools/run-parser-gate.py
python3 tools/record-parser-evidence.py
```

This builds the independent Go oracle, runs selected upstream parser tests,
recomputes and checks stored expected graphs, runs parser xUnit tests, publishes
and executes the Native AOT parser harness, and reruns the scanner's complete
verification gate. The parser source hash must remain unchanged throughout.
Logs and binaries are written under ignored `artifacts/`.

The faster managed loop uses the same checked-in expectations:

```sh
dotnet test tests/Parser.Tests/Parser.Tests.csproj -c Release
```

The [JSONL request contract](../validation/Parser.Validation/Contracts.cs) accepts
`text` or `textBytesBase64`, explicit numeric `scriptKind`, optional `fileName`,
external-module JSX/force options and `resolveJSDoc`. Normal .NET callers use:

```csharp
using TypeScript.Scanner;

SourceFile file = Parser.ParseSourceFile(
    new SourceFileParseOptions(FileName: "/example.ts"),
    "const answer: number = 42;",
    ScriptKind.TS);
Node root = file.AsNode();
var syntaxDiagnostics = file.Diagnostics();
```

The byte-array overload preserves arbitrary input bytes. Positions are UTF8 byte
offsets. Internal AST text fields retain Go byte strings; use `TextBytes()` or
`DecodedText()` at .NET boundaries. This is a faithful feasibility API, not a
finished idiomatic compiler SDK.

## What is compared

The [Go oracle](../tools/parser-oracle/main.go) independently reflects concrete
upstream syntax payloads. The C# encoder uses static typed metadata and no runtime
reflection requirement. Both produce a flat graph with kinds, flags, byte spans,
raw scalar values, named node/list fields, null/empty/missing lists, parent identity,
and actual child traversal order. Recovery and zero-width nodes are retained.
Source metadata and ordered parse, JS and JSDoc diagnostics are compared separately,
including message metadata, arguments and related information.

The [corpus manifest](../validation/parser-corpus/manifest.json),
[upstream provenance](../validation/parser-corpus/upstream-manifest.json) and
[coverage inventory](../validation/parser-corpus/coverage.json) distinguish authored
cases from pinned upstream sources. Expected output always comes from unchanged Go.
Cold and repeated warm JSDoc resolution also check nullness and node identity;
named JSDoc source-text regressions exercise normalization helpers.

## Supported modes and limits

Explicit JS(1), JSX(2), TS(3), TSX(4) and JSON(6) are in the supported contract,
including declaration filenames, external-module JSX/force options and eager or
explicit lazy JSDoc resolution. Unknown mode retains the upstream exception;
reserved/invalid numeric modes are outside the tested contract.

Exclusions: incremental edits, source position-map APIs, general public transform
facts, binder/checker/flow/service state, module resolution and emit. Required
JSDoc and top-level-await reparsing are included. The separate isolated-entity-name
entry point is implemented but is outside source-file corpus acceptance unless
explicitly verified. Full compiler tests are not run as syntax-only evidence.

Factory payload coverage is not branch coverage or proof of all recovery paths.
Unobserved diagnostics remain listed in the coverage inventory. Native proof is
limited to the tested Linux x64 environment; other operating systems and
architectures are unverified. No parser performance claim is made.
