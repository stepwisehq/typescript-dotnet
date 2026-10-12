# TypeScript compiler port

The pinned upstream **whole-checker no-emit and ordinary Program emit contracts are
complete**, verified under actual **Linux x64 Native AOT**. The compiler checks
configured projects and produces JavaScript, declarations and source maps through
ported upstream factory, transformation, printing and output infrastructure.

Final emit acceptance passed 2,223 xUnit records and the complete 15,330-configuration
upstream inventory with explicit dispositions: 15,285 exact public-output matches,
39 original preparation skips and six external incremental build-info metadata
differences. All 13,571 applicable original assertion sets agree; original option
policies and channel skips remain separately accounted for. All 15,304 retained
checker records also agree with pinned Go on the final native build. These are
separate counting units, not a combined number of distinct upstream tests.

See the [emit assessment and limitations](./docs/EMIT-ASSESSMENT.md),
[compilation and verification instructions](./docs/EMIT-VERIFY.md),
[test inventory](./docs/EMIT-TEST-INVENTORY.md),
[acceptance evidence](./validation/results/emit-verified.json),
[source mapping](./source-map.json) and [current state](./docs/STATE.md).
The [whole-checker assessment](./docs/CHECKER-NOEMIT-ASSESSMENT.md) retains earlier
semantic/environment limitations. This establishes the agreed pinned-upstream
contracts, not exhaustive correctness or parity with every historical tsc feature.
Only Linux x64 has been qualified. Incremental/build/watch orchestration and language
services remain outside scope.

The VS Code checking and full-emit performance campaigns met their scoped targets.
The [checking summary](./docs/PERFORMANCE-EPIC-SUMMARY.md) covers the original
allocation rollout; the [emit summary](./docs/EMIT-PERFORMANCE-SUMMARY.md) explains
the additional allocation and runtime work that reached **10.18s Native / 9.37s Go**.
These are best-of-five results with documented tuning and memory requirements;
see the [detailed measurements and reproduction](./docs/VSCODE-CHECK-STATUS.md).

Ordinary `tscheck --config <tsconfig> --json` uses genuine Program options and unchanged
upstream libraries, checks the project and emits JavaScript, declarations and maps
as requested by its compiler options. Use `--noEmit` for checking alone. Compiler
errors exit 1 when the emitter reports a skipped result and 2 otherwise; success
exits 0. The result flag, rather than the number of written files, determines status. `--listFilesOnly` lists inputs without emitting and is a command flag,
not a tsconfig option. `listFiles`, `listEmittedFiles` and `explainFiles` retain their
upstream listing behavior. Incremental or composite command execution reports an
explicit unsupported snapshot/build-info boundary; the ordinary Program emitter
does not fabricate build information. Existing `--project <tscheck.json>` remains
its accepted check-only compatibility path.

```sh
artifacts/tscheck/tscheck --config path/to/tsconfig.json --json
artifacts/tscheck/tscheck --config path/to/tsconfig.json --noEmit --json
artifacts/tscheck/tscheck --config path/to/tsconfig.json --listFilesOnly
```

The accepted emit checkpoint is recorded in [STATE](./docs/STATE.md). The strict ES2022 [job-stream project](./examples/protocols/job-stream/)
combines promises, synchronous/asynchronous generators, for-await and disposable
resources across files. Its [invalid variant](./examples/protocols/job-stream-invalid/)
reports six complete diagnostics; the valid project exits 0. The protocol checkpoint
used check-only status 1 for its invalid case; genuine compilation follows the emit
result statuses above. See the
[project usage guide](./examples/protocols/README.md). No Go process is needed to use
the C# checker or run tests against committed reference records.

The optional `--no-gc-region` flag applies only to one-shot `--config` checking;
ordinary execution stays unchanged. It uses the standard .NET API with a 14 GiB
total budget including 2 GiB LOH, after a Linux MemAvailable preflight of 20 GiB.
Insufficient or unavailable headroom falls back to ordinary GC. The region may
also end automatically; neither budget nor preflight is a process memory limit.
Set `TSC_GC_POLICY_REPORT` to a separate file to record start/end modes, fallback,
allocation and collection counters without changing diagnostic stdout or exit codes.
This is an authored runtime adaptation, not an upstream compiler option. AVX2
publication and a matched checker-count configuration remain separate explicit
choices; portable/default execution remains available.

To publish the explicit AVX2 variant from the repository root:

```sh
dotnet publish src/TypeScript.Check/TypeScript.Check.csproj -c Release -r linux-x64 -p:PublishAot=true -p:IlcInstructionSet=avx2 -o artifacts/production-avx2
```

This requires an AVX2-capable CPU with OS vector-state support and the Native AOT
build prerequisites. Omitting `IlcInstructionSet=avx2` retains the portable default;
Server GC and the compiler algorithms are unchanged.

For a matched eight-checker comparison, use `compilerOptions.checkers: 8` in the
same genuine project configuration for both compilers. A separate config can extend
the original, but place its entry **beside the original config** so implicit type
roots and package lookup keep the original directory. For example, beside VSCode's
`src/tsconfig.json`, a `src/tsconfig.checkers8.json` can contain:

```json
{"extends":"./tsconfig.json","compilerOptions":{"checkers":8}}
```

Do not relocate that entry to a different artifact directory without preserving and
verifying resolution equivalence. Invoke both compilers with this same config:

```sh
TSC_GC_POLICY_REPORT=/tmp/tscheck-policy.json artifacts/production-avx2/tscheck --config /home/greg/projects/vscode/src/tsconfig.checkers8.json --noEmit --json --no-gc-region
artifacts/vscode-diagnostic-fix/pinned-cli/tsc --project /home/greg/projects/vscode/src/tsconfig.checkers8.json --noEmit --skipLibCheck --pretty false
```

The report is optional; omit the environment assignment to disable it. Successful
start alone does not establish a complete region: check `noGcStarted`,
`noGcEndOutcome` and `modeBeforeEnd`. Ordinary diagnostics may still produce exit 1
(Native) or exit 2 (Go). These are one-shot instructions; no watch-mode guarantee or
hard RSS limit is implied.

All 115 pinned library files remain embedded unchanged. `--inspect-config`,
`--inspect-project` and `--inspect-program` inspect configuration, resolved graphs and
genuine Program environments independently of semantic checking. See their
[configuration](./docs/CONFIGURATION.md), [resolution](./docs/RESOLUTION.md) and
[Program](./docs/PROGRAM.md) guides. Build/watch orchestration and language services remain external;
ordinary Program emit is described in the [emit guide](./docs/EMIT-VERIFY.md).

A C# port of the Go TypeScript scanner, initial syntax parser/AST, source-file binder
and bounded scalar/object/data-type, callable and generic-collection checking, pinned to maintained
[microsoft/TypeScript source](./UPSTREAM.md). The defined scanner/parser/syntax-AST
foundation contract, source-file binding completion contract and six earlier bounded
checkpoints, plus the fixed callable and generic-collection contracts, passed actual **Linux x64 Native AOT** verification. See the
[current state](./docs/STATE.md), [binding assessment](./docs/BINDING-COMPLETION-ASSESSMENT.md),
[foundation assessment](./docs/FOUNDATION-ASSESSMENT.md),
[completion contract](./docs/FOUNDATION-CONTRACT.md) and
[verification](./docs/FOUNDATION-VERIFICATION.md) for scope and evidence.

The following historical named profiles are explicit compatibility/test adapters,
not the ordinary `--config` environment.

The default `Legacy` profile retains standalone scalar/object data checking. The
explicit `Callable` profile adds ordinary functions/arrows/methods, overloads,
contextual callbacks and inferred returns, callable objects and relationships,
receivers, recursive shapes, selected `if`/return/throw bodies, simple assignments
and scalar operators. Select it with `Checker.Create(sourceFile, CheckerProfile.Callable)`
or `"profile":"Callable"` in a checker validation request.

Both profiles use fixed single-file no-library options. Missing-library errors stay
separately visible: usually eight, with real source declarations and diagnostic paths
changing the exact result. Unsupported inputs fail explicitly. **Arrays/tuples/rest,
source generics, classes, async, loops/switch/try, standard libraries, modules and
project checking remain outside this callable contract.** This is not typical complete
application checking or full checker parity. See the full
[callable boundary](./docs/CALLABLES-CONTRACT.md).

The [library](./src/TypeScript.Scanner) targets .NET 10. Separate executable harnesses
for [scanner](./validation/Scanner.Validation), [parser](./validation/Parser.Validation),
[binder](./validation/Binder.Validation) and [checker](./validation/Checker.Validation)
support Native AOT; xUnit compares stored pinned-Go results. No Go process is
needed to use the C# implementation.

## Generic-collection capability and verification

The explicit `Collections` profile adds generic inference, arrays/readonly arrays,
concrete tuples, rest/spread, mapped/conditional utilities and loops using complete,
unmodified pinned ES5/decorators/decorators.legacy declarations. It checks one user TS
script with fixed options and genuine `skipLibCheck` policy; no fake library signatures.
The native [grouped-record example](./examples/collections/grouped-records.ts) has zero
applicable or missing-global diagnostics, and its invalid variant preserves all 8 errors.

The fixed [contract](./docs/COLLECTIONS-CONTRACT.md) passes 44 rich original-Go/native
comparisons,12 shared checks and 18 order executions per runtime. All 23,467 xUnit tests
and retained native regressions pass. See [usage and reproduction](./docs/COLLECTIONS.md),
[assessment/limitations](./docs/COLLECTIONS-ASSESSMENT.md),
[source mapping](./source-map.json) and [durable proof](./validation/results/collections-verified.json).
Classes/new, destructuring, generic variadic tuples, custom iterators/ES2015 libraries,
modules and multi-file project checking remain prominent exclusions.

```sh
python3 tools/run-collections-gate.py
# Only after a successful complete gate:
python3 tools/record-collections-evidence.py
```

The gate performs the retained callable/binding/foundation cascade once, reproduces
pinned-Go expectations, freshly publishes the collection Linux x64 Native AOT target,
and checks complete observations, order, binary/source/assets and historical preservation.
Ordinary use loads embedded libraries and needs no Go process or upstream checkout.

## Callable campaign verification

The [callable contract](./docs/CALLABLES-CONTRACT.md),
[verification instructions](./docs/CALLABLES-VERIFICATION.md),
[coverage inventory](./docs/callables-coverage.json) and
[adaptations](./docs/CALLABLES-DEPARTURES.md) define this capability.
The accepted callable contract matches 89 authored pinned-Go semantic graphs under
actual Linux x64 Native AOT. All 23,411 xUnit cases pass, including 701 checker cases;
all retained regressions pass. See the [assessment](./docs/CALLABLES-ASSESSMENT.md) and
[durable proof](./validation/results/callables-verified.json) for limitations and evidence.

```sh
python3 tools/run-callables-gate.py
# Only after a successful complete gate:
python3 tools/record-callables-evidence.py
```

The gate performs one complete binding/foundation/earlier-semantic regression cascade,
fresh Go comparisons, Native AOT callable/numeric/API/boundary/order checks and source/
asset/binary drift checks. Ten newly applicable original numeric test functions provide
152 literal plus 152 live Node assertions. The 89 semantic cases are authored differential
comparisons, not original full compiler tests. No Node or Go process is needed for
ordinary use of the C# checker; Node is required by the original numeric verification.

## Source-file binding completion verification

The [binding contract](./docs/BINDING-COMPLETION-CONTRACT.md),
[assessment](./docs/BINDING-COMPLETION-ASSESSMENT.md) and
[test inventory](./docs/binding-completion-tests.json) define the accepted source-file
binding surface. All 17,723 broad and 26 targeted binding graphs agree with pinned Go
under actual Linux x64 Native AOT; all 22,972 xUnit cases pass. Compiler-input reuse
does not establish full compiler-test success or whole-checker parity.

```sh
python3 tools/run-binding-completion-gate.py
python3 tools/record-binding-completion-evidence.py
```

This gate includes the complete foundation and semantic regression cascade plus the
full original numeric finite baseline sets. See
[verification instructions](./docs/BINDING-COMPLETION-VERIFICATION.md) for prerequisites,
observation order, provenance and limits. This historical binding campaign did not implement callable checking; the later
callable campaign above builds on its accepted contract.

## Foundation completion verification

The [foundation contract](./docs/FOUNDATION-CONTRACT.md),
[assessment](./docs/FOUNDATION-ASSESSMENT.md) and
[upstream test inventory](./docs/foundation-test-catalog.json) describe the accepted
scanner/parser/syntax-AST completion campaign. Compiler-input reuse establishes syntax comparisons,
not full compiler test success.

From the repository root, run the guarded native and regression gate:

```sh
python3 tools/run-foundation-gate.py
```

The gate includes original Go subsystem tests, all xUnit suites, Native AOT publication,
shared foundation assertions, finite parser fuzz seeds and full corpus comparisons.
For the complete original numeric fuzz baseline sets, also run:

```sh
tools/.cache/go/bin/go -C upstream/TypeScript/tsc test ./internal/jsnum -run '^$' -fuzz '^FuzzStringJS$' -fuzztime=156x -parallel=1
tools/.cache/go/bin/go -C upstream/TypeScript/tsc test ./internal/jsnum -run '^$' -fuzz '^FuzzFromStringJS$' -fuzztime=236x -parallel=1
```

These are bounded seed checks, not an open-ended fuzz campaign. The main gate's
short numeric fuzz invocations are smoke checks; the additional commands establish
complete Go baseline execution. See [verification](./docs/FOUNDATION-VERIFICATION.md)
for provenance, observation limits and detailed reproduction instructions.

## Build and verify

The reproducible toolchain targets **Linux x64**. Install .NET SDK 10.0.401
(or a compatible patch selected by [global.json](./global.json)), Git, Python 3,
curl, Node.js (for the original numeric reference assertions), and the native build prerequisites. On Ubuntu:

```sh
sudo apt-get update
sudo apt-get install -y clang zlib1g-dev
./tools/verify-data-types.sh
```

The script fetches the exact upstream revision, bootstraps checksum-pinned Go
1.27.1 locally, builds the Go oracle, checks fresh scalar/object/data-type expectations, runs
xUnit, publishes Native AOT, and compares the actual native executable with Go.
It also runs the binder, parser and scanner regression gates, including their
native executables and upstream scanner tests.
Initial setup requires network access for upstream source, Go, and NuGet packages.
It refuses a mismatched or locally modified upstream checkout.

For the faster managed test loop, using the checked-in oracle corpus:

```sh
dotnet test tests/Scanner.Tests/Scanner.Tests.csproj -c Release
```

The current data-type gate includes scalar/object, binder, parser and scanner regressions:

```sh
./tools/verify-data-types.sh
```

Historical boundary gates should be reproduced at their own commits:
`./tools/verify-checker.sh` at scalar commit `7e692dd`, and
`./tools/verify-object-checker.sh` at object commit `1f7c953`. Eight historical
admission rejections became supported in this campaign, in addition to two retired
in the anonymous-object phase, as recorded
in the [boundary manifest](./validation/data-types-corpus/retired-admission-controls.json).
Historical fixture bytes and evidence remain preserved.

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
    "type I = { child: { x: number } } & { child: { y: string } }; let i: I = { child: { x: 1, y: 2 } };",
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
and control-flow graphs. The checker adds scalar and named/recursive/composite object
checks; its [exact boundary](./docs/DATA-TYPES-CONTRACT.md) remains explicit. Broad
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
