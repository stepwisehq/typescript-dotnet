# TypeScript port feasibility study

A C# port of the Go TypeScript scanner, syntax parser/AST, source-file binder and
bounded scalar/object checker slices, pinned to maintained
[microsoft/TypeScript source](./UPSTREAM.md). All five bounded checkpoints passed
actual Native AOT verification. See the [current state](./docs/STATE.md),
[object checker assessment](./docs/OBJECT-CHECKER-ASSESSMENT.md) and
[verification](./docs/OBJECT-CHECKER-VERIFICATION.md) for scope and evidence.

The semantic slice checks single-file scalar declarations, anonymous objects,
inline object types, dot reads and structural relationships under fixed no-library
options. It preserves the eight upstream missing-library errors
separately from file semantic diagnostics and rejects unsupported inputs explicitly.
Named types/interfaces, generics, calls, project checking, modules and code
generation are excluded. This is a bounded object milestone, not full checker parity.

The [library](./src/TypeScript.Scanner) targets .NET 10. Separate executable harnesses
for [scanner](./validation/Scanner.Validation), [parser](./validation/Parser.Validation),
[binder](./validation/Binder.Validation) and [checker](./validation/Checker.Validation)
support Native AOT; xUnit compares stored pinned-Go results. No Go process is
needed to use the C# implementation.

## Build and verify

The reproducible toolchain targets **Linux x64**. Install .NET SDK 10.0.401
(or a compatible patch selected by [global.json](./global.json)), Git, Python 3,
curl, and the native build prerequisites. On Ubuntu:

```sh
sudo apt-get update
sudo apt-get install -y clang zlib1g-dev
./tools/verify-object-checker.sh
```

The script fetches the exact upstream revision, bootstraps checksum-pinned Go
1.27.1 locally, builds the Go oracle, checks fresh scalar/object expectations, runs
xUnit, publishes Native AOT, and compares the actual native executable with Go.
It also runs the binder, parser and scanner regression gates, including their
native executables and upstream scanner tests.
Initial setup requires network access for upstream source, Go, and NuGet packages.
It refuses a mismatched or locally modified upstream checkout.

For the faster managed test loop, using the checked-in oracle corpus:

```sh
dotnet test tests/Scanner.Tests/Scanner.Tests.csproj -c Release
```

The current object checker gate includes scalar, binder, parser and scanner regressions:

```sh
./tools/verify-object-checker.sh
```

The historical scalar-only gate (`./tools/verify-checker.sh`) asserts the earlier
admission boundary; reproduce it at scalar commit `7e692dd`. Two old object-syntax
rejections deliberately became supported in this phase. Historical fixture bytes
and evidence remain preserved.

For a managed checker development loop:

```sh
dotnet test tests/Checker.Tests/Checker.Tests.csproj -c Release
```

The binder gate also remains available, including parser and scanner regressions:

```sh
./tools/verify-binder.sh
```

For a managed binder development loop:

```sh
dotnet test tests/Binder.Tests/Binder.Tests.csproj -c Release
```

The parser also retains its own gate, including scanner regressions:

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
`./tools/parser-benchmark.sh` from parser checkpoint `de25b66` after its verification gate has created the
accepted native validator. Its source guard intentionally rejects later binder changes;
see the report for historical reproduction. It measures parsing and a small result observer;
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

## Bounded semantic API

```csharp
using TypeScript.Scanner;
using TypeScript.Checking;

var file = Parser.ParseSourceFile(new("/main.ts", "/main.ts"),
    "const source = { count: \"text\" }; let target: { count: number } = source;",
    ScriptKind.TS);
var checker = Checker.Create(file); // Includes source-file binding.
var fileDiagnostics = checker.GetDiagnostics(); // Includes error 2322.
var environmentDiagnostics = checker.GetGlobalDiagnostics(); // Eight noLib errors.
```

Unsupported input raises `UnsupportedSemanticFeatureException`; it is not a
completed semantic check. Keep the parsed tree immutable while using its checker.

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
its acceptance is tracked separately in [STATE.md](./docs/STATE.md). Source-file
binding now adds declaration/symbol relationships, scope tables, binder diagnostics
and control-flow graphs. The checker adds selected scalar and anonymous-object
semantic checks; its [exact boundary](./docs/OBJECT-CHECKER-VERIFICATION.md) remains narrow. Broad
checking, cross-file semantic resolution, module resolution and code generation
remain excluded.
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
