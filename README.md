# TypeScript scanner port feasibility study

A C# port of the Go TypeScript scanner, pinned to the maintained
[microsoft/TypeScript source](./UPSTREAM.md). This repository is a scanner study,
not a compiler implementation. The bounded checkpoint is accepted; see the
[feasibility assessment](./docs/ASSESSMENT.md) and
[verification evidence](./docs/VERIFICATION.md) for results and limitations.

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

A basic Go versus Native AOT throughput/allocation comparison is recorded in
[PERFORMANCE.md](./docs/PERFORMANCE.md), with raw samples and explicit measurement
limits. Reproduce it with `./tools/benchmark.sh`. It measures the current scanner
API, including input conversion, and does not benchmark the full compiler.

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
validation, token flags, diagnostic callbacks, and comment directives. Supporting
AST definitions are restricted to token kinds and scanner records. Compiler-node
and source-file utilities are explicitly excluded; no parser/checker/emitter is
implemented. See [source-map.json](./source-map.json) and the
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
