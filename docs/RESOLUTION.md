# Source-import resolution: use and verification

The [fixed contract](RESOLUTION-CONTRACT.md) covers the pinned upstream Module
subsystem and its necessary source-graph integration. The complete gate passed on Linux x64 with actual Native AOT execution. See the
[durable proof](../validation/results/resolution-verified.json),
[status](RESOLUTION-STATUS.md) and [assessment](RESOLUTION-ASSESSMENT.md) for exact scope
and remaining evidence limits.

## Inspect a real project graph

From the repository root, publish the ordinary command:

```sh
dotnet publish src/TypeScript.Check/TypeScript.Check.csproj -c Release \
  -r linux-x64 -p:PublishAot=true -o artifacts/tscheck-native
artifacts/tscheck-native/tscheck --inspect-project examples/resolution/package-graph/tsconfig.json
artifacts/tscheck-native/tscheck --inspect-project examples/resolution/package-graph-invalid/tsconfig.json
```

The first example resolves a mixed-form package graph; the second deliberately has
missing/blocked dependencies. Inspection emits JSON containing files, dependency edges,
resolution modes and results, path/type references, scheduling reasons, parse and
inclusion diagnostics. Display paths are Unicode; Base64 path fields preserve the
exact original byte representation. Exit status is **0** for a graph without observed
failures, **1** for configuration/parse/inclusion/unresolved-dependency failures, and
**2** for unsupported operations, invalid invocation or host failures.

Inspection uses real tsconfig interpretation and Node16, NodeNext or Bundler resolution,
as implemented at the pinned revision. It includes paths/rootDirs/module suffixes,
package exports/imports/typesVersions, conditions and metadata, TS/JS/declaration/JSON
source forms, cycles, symlinks, explicit path/type references and synthetic dependencies.
Graph inspection does **not** perform type checking or complete Program option validation.
Default-library and automatic-global environment construction, library-reference
scheduling, reference-project execution and content mappers remain external. Explicit
observations distinguish dependencies that resolve from those scheduled into this graph.

The shipped mixed graph intentionally includes source forms beyond the checker. Its
small JSX package declaration is an authored resolution fixture, not a claim of React
or broad standard-library support. The graph examples and API examples are integration
evidence; they do not define the subsystem's implementation boundary.

## Check a supported package project

```sh
artifacts/tscheck-native/tscheck --config examples/resolution/package-checking/tsconfig.json --json
artifacts/tscheck-native/tscheck --config examples/resolution/package-checking-invalid/tsconfig.json --json
```

The valid project should exit 0 without diagnostics. The invalid variant should exit 1
with TS2322. Full diagnostics include source file, byte span and one-based UTF-16
line/column. Ordinary checking needs neither Go nor the upstream checkout.

Checking first loads the genuine graph, then applies explicit semantic admission.
The supported environment remains ESNext target/module, Bundler resolution, noEmit,
noLib, skipLibCheck and types: [], with three complete embedded upstream libraries:
ES5, decorators and decorators.legacy. Their presence does not imply checking their
bodies or supporting every declared API. Use the
[checking configuration](../examples/configuration/checking-base.json) for the exact
required strictness settings. Unknown or unsupported settings are rejected rather than
silently ignored. Resolver-only paths, package imports, rootDirs, suffixes, custom
conditions, package-map controls and symlink settings are no longer blanket-rejected.

Supported semantic sources remain the prior TS implementation and collection/callable
capabilities. Resolving JavaScript, declarations, JSON or other source forms does not
make those forms semantically supported. Classes/new, destructuring, broader libraries,
full module interop, general Program construction, emit and services remain separate.
Broader diagnostic module-name generation may explicitly reject unsupported formatting.
The named `--project <tscheck.json>` adapter preserves its older strict host policy.
`--inspect-config <tsconfig>` remains available for configuration-only inspection.

## Reproduce acceptance

Prerequisites are the existing .NET 10 SDK, Git, Python 3, curl, Node.js for inherited
numeric assertions, and Linux Native AOT prerequisites (Ubuntu: clang and zlib1g-dev).
See the [repository build instructions](../README.md#build-and-verify). The scripts
fetch/check the pinned upstream revision and checksum-pinned Go toolchain; initial
setup and NuGet restoration require network access. Existing recorded GCC fallback
support is used only where the repository's executable compiler wrapper is present.

```sh
python3 tools/run-resolution-gate.py
# Only after a successful complete gate, and only if no proof already exists:
python3 tools/record-resolution-evidence.py
```

The gate rebuilds Go and compares its fresh output with unchanged committed expectations,
replays all 38 direct Module tests,
checks a pristine companion for ordinary API parity, runs the Resolution xUnit suite,
publishes and executes both the Resolution validator and ordinary tscheck with Linux
x64 Native AOT, and performs the retained configuration/project/semantic/foundation
cascade once. It verifies source/assets/obligations/upstream/historical preservation.
It does not rewrite prior accepted proofs. A failed or partial run is not acceptance.
The recorder deliberately refuses to overwrite an existing resolution proof.

For a focused managed developer check using the stored Go expectations:

```sh
dotnet test tests/Resolution.Tests/Resolution.Tests.csproj -c Release
```

Focused checks do not replace final native acceptance. Full logs and intermediate
snapshots live in ignored artifacts; the durable proof is recorded only after success.
Verification is actual **Linux x64**. Virtual case-sensitive/insensitive, Windows/UNC
and raw-byte host cases do not establish execution on another operating system.

## Provenance and comparisons

The [test inventory](resolution-tests.json) separates 38 original Module bodies,
two authored shared controls, authored differential cases, and compiler inputs reused
only for graph observations. The [compiler-input dispositions](resolution-compiler-inputs.json)
account for all 76 selected upstream inputs: 72 reused and four exclusively removed-mode
inputs excluded. None is reported as a passing full compiler test. Embedded-config
post-discovery harness overrides that are not applied are listed explicitly.

The 216 compound comparisons cover complete public resolver results, diagnostics,
ordered traces/lookups, source-graph relationships and selected required cache/identity
invariants. Fourteen order groups produce 30 executions per runtime. Detailed private
lazy-cache topology is not an equivalence requirement. The original graph observer
uses upstream loader/diagnostic producers through the documented scoped overlay;
95 ordinary API observations additionally match a pristine companion byte-for-byte.
See the [assessment](RESOLUTION-ASSESSMENT.md) for implementation/evidence limitations.
