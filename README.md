# TypeScript port feasibility study

A C# port of the Go TypeScript scanner and required syntax parser/AST support,
pinned to the maintained [microsoft/TypeScript source](./UPSTREAM.md).
See [current state](./docs/STATE.md) and [parser plan](./docs/PARSER-PLAN.md).
Binding, checking, module resolution and code generation remain outside scope.
Both bounded checkpoints passed actual Native AOT verification. See the
[parser assessment](./docs/PARSER-ASSESSMENT.md) and
[parser verification](./docs/PARSER-VERIFICATION.md) for current results and limits;
the [scanner assessment](./docs/ASSESSMENT.md) preserves the earlier checkpoint.

The [scanner library](./src/TypeScript.Scanner) targets .NET 10. The
[validation executable](./validation/Scanner.Validation) supports Native AOT;
[xUnit tests](./tests/Scanner.Tests) compare against stored results from the pinned
Go scanner. No Go process is needed to use the C# scanner.

## Build and verify

The reproducible toolchain targets **Linux x64**. Install .NET SDK 10.0.401
(or a compatible patch selected by [global.json](./global.json)), Git, Python 3,
curl, and the native build prerequisites. On Ubuntu:

```sh
sudo apt-get update
sudo apt-get install -y clang zlib1g-dev
./tools/verify.sh
```

The script fetches the exact upstream revision, bootstraps checksum-pinned Go
1.27.1 locally, builds the Go oracle, runs upstream scanner tests, checks that the
stored expected results still match Go, runs xUnit, publishes Native AOT, and
compares the actual native executable's output with the same oracle results.
Initial setup requires network access for upstream source, Go, and NuGet packages.
It refuses a mismatched or locally modified upstream checkout.

For the faster managed test loop, using the checked-in oracle corpus:

```sh
dotnet test tests/Scanner.Tests/Scanner.Tests.csproj -c Release
```

The parser phase has its own gate, including scanner regressions:

```sh
./tools/verify-parser.sh
```

For its managed test loop, run
`dotnet test tests/Parser.Tests/Parser.Tests.csproj -c Release`.
See the [parser plan](./docs/PARSER-PLAN.md) for the canonical tree/diagnostic
contract and the [parser assessment](./docs/PARSER-ASSESSMENT.md) for current limits.

A basic Go versus Native AOT throughput/allocation comparison is recorded in
[PERFORMANCE.md](./docs/PERFORMANCE.md), with raw samples and explicit measurement
limits. Reproduce it with `./tools/benchmark.sh`. It measures the current scanner
API, including input conversion, and does not benchmark the full compiler.

The parser has a separate [basic performance report](./docs/PARSER-PERFORMANCE.md)
and [raw samples](./benchmarks/parser-results.json). Reproduce with
`./tools/parser-benchmark.sh` after the parser verification gate has created the
accepted native validator. It measures parsing and a small result observer;
optimization and deeper investigation remain deferred.

To publish and check Native AOT explicitly:

```sh
dotnet publish validation/Scanner.Validation/Scanner.Validation.csproj \
  -c Release -r linux-x64 -p:PublishAot=true -o artifacts/validation/native
python3 tools/compare.py \
  --executable "$PWD/artifacts/validation/native/Scanner.Validation"
```

Native publish success alone is insufficient: the comparison must pass. Detailed
test logs and generated binaries go under ignored `artifacts/`; durable result
summaries belong in the project documentation.

## Representation and scope

Positions are **UTF-8 byte offsets**, matching Go. `SetText(string)` encodes a .NET
string as UTF-8; byte-array/span overloads accept arbitrary source bytes.
`TokenTextBytes()` and `TokenValueBytes()` preserve exact upstream bytes, including
malformed input and JavaScript lone-surrogate value encoding. `TokenValue()` gives
the corresponding .NET UTF-16 value. Raw bytes are authoritative for differential
verification.

The port covers lexical scanning, contextual rescans, JSX/JSDoc modes, regex syntax
validation, token flags, diagnostic callbacks, and comment directives. The completed
parser phase adds syntax trees, syntax diagnostics and required AST support;
its acceptance is tracked separately in [STATE.md](./docs/STATE.md). Binding,
checking, module resolution and code generation remain excluded.
See [source-map.json](./source-map.json) and the
[mapping convention](./docs/SOURCE-MAPPING.md) for per-file completeness and
exceptions. Ported source and newly authored compatibility/harness code are marked
separately. Applicable notices are preserved under [licenses/](./licenses/).

## Verification data and recovery

[validation/corpus/](./validation/corpus) contains requests, pinned-Go expected
results, a source manifest, and a coverage inventory. JSONL requests can ask for
ordinary scanning or an explicit operation sequence. The protocol's source is
[Contracts.cs](./validation/Scanner.Validation/Contracts.cs) and its independent
Go adapter is [tools/oracle/main.go](./tools/oracle/main.go).

When deliberately adding cases, run `python3 tools/generate-corpus.py` and regenerate
expected results with the freshly built Go oracle. Never edit an expected result
to accommodate C# behavior. Normal verification does not rewrite the baseline.

For interrupted sessions, start at [START-HERE.md](./START-HERE.md), which preserves
the original user mandate and later directions, then follow
[docs/RESUMING.md](./docs/RESUMING.md).
Subsystem checkpoints record the last verified milestone and exact next action.
