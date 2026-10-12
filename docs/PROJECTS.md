# Checking a bounded ES-module project

**Accepted for the bounded [project contract](./PROJECTS-CONTRACT.md).** The final
Linux x64 Native AOT gate passed in 780.934 seconds: 48 exact project comparisons,
27 shared controls, 30 order executions per runtime, ten ordinary CLI cases and
23,542 xUnit tests with zero skips. The [durable proof](../validation/results/projects-verified.json)
records the exact source, assets and nine native binary fingerprints. This is not
general TypeScript project or library parity.

The historical `--project <tscheck.json>` path loads the bounded check-only
configuration described below. The genuine `--config <tsconfig.json>` path now
checks and emits JavaScript, declarations and source maps according to compiler
options; whole emit acceptance is tracked in [STATE](./STATE.md). It does not run
JavaScript, install packages or invoke Go. Use `--noEmit` for genuine checking alone.

```bash
artifacts/tscheck/tscheck --config path/to/tsconfig.json --json
artifacts/tscheck/tscheck --config path/to/tsconfig.json --noEmit --json
artifacts/tscheck/tscheck --config path/to/tsconfig.json --listFilesOnly
```

For genuine compilation, success exits **0**, diagnostics with the emitter
reporting `EmitSkipped` exit **1**, and other diagnostic results exit **2**. The
result flag determines status even when no output files are written. Unsupported host
execution also exits **2** with an explicit error. JSON exposes diagnostics,
`EmitSkipped` and listing text; ordinary listings use upstream LF lines. Compiler
options `listFiles`, `listEmittedFiles` and `explainFiles` request their normal
listings. `--listFilesOnly` is command-only: putting it in tsconfig retains the
upstream unknown-option diagnostic. Incremental or composite compilation requires
the excluded snapshot/build-info execution owner and is explicitly rejected after
genuine configuration/program option diagnostics. Existing `Project.Check()` is
unchanged; `Project.LoadConfiguration(path).Compile()` invokes real Program output.

 Its genuine pinned ES5,
decorators and decorators.legacy declarations are embedded in the executable.

## Build and ordinary invocation

From the repository root, publish with the .NET 10 SDK and a Linux x64 Native AOT
C/linker toolchain. The compiler fallback below is available in the study environment;
an ordinary installed clang toolchain can be used directly.

```bash
repo_root=$(pwd)
native_properties=()
if ! command -v clang >/dev/null && [[ -x "$repo_root/tools/.cache/native/bin/cc" ]]; then
  native_properties+=("-p:CppCompilerAndLinker=$repo_root/tools/.cache/native/bin/cc")
fi
dotnet publish src/TypeScript.Check/TypeScript.Check.csproj \
  -c Release -r linux-x64 -p:PublishAot=true "${native_properties[@]}" \
  -o artifacts/tscheck

artifacts/tscheck/tscheck --project examples/projects/grouped-records/tscheck.json
artifacts/tscheck/tscheck --project examples/projects/grouped-records-invalid/tscheck.json
```

Exit statuses: **0** for successful checking, **1** for
compiler diagnostics, **2** for unsupported input/configuration/host failure. The
invalid command intentionally returns a nonzero status. Diagnostics go to stderr
in ordinary mode. Add `--json` after the project path for structured stdout containing
the exit code, diagnostics or failure message. JSON diagnostics include paths, codes,
categories, messages, byte ranges, one-based line/UTF-16 column positions, message
chains and related information. The process exit status remains authoritative.

Once published, the executable needs neither the repository nor the upstream checkout
for ordinary use. Copy it to the desired location and pass the project's configuration
path. Only actual Linux x64 execution is an acceptance target; no other platform is
verified by this campaign.

The public API uses the same loaded project and shared checker:

```csharp
using TypeScript.Compiler;

Project project = Project.Load("/path/to/tscheck.json");
var diagnostics = project.Check();
var checker = project.Checker;
var entryFile = project.RootFiles[0];
var rawCheckerDiagnostics = checker.GetDiagnostics(entryFile);
```

`Project.Check()` composes applicable parse/include, bind/check and global diagnostics
with Program's noEmit filtering and sorting. The per-file Checker method returns raw
checker diagnostics; it is not the complete Program result. Query nodes must belong
to that project's actual user ASTs, and related symbols can belong to its embedded
libraries. Malformed syntax or a project with no existing user files prevents semantic
checker entry. `SourceFiles`, `RootFiles`, `UserFiles`, `LibraryFiles` and
`ResolutionEdges` expose the loaded snapshot; fixed options are not publicly mutable.

## Project configuration

Create a strict JSON file, conventionally named `tscheck.json`:

```json
{
  "files": ["src/main.ts"]
}
```

The configuration directory bounds the project. Entries are relative to it; relative
imports are resolved from the importing file. Dependencies are loaded automatically.
Multiple explicit roots are allowed. Normalized paths share file identity; cycles
are supported by genuine module and checker algorithms. Paths escaping the project,
symlink-dependent paths and node_modules dependencies are unsupported. Module paths
must decode as scalar UTF8 at the disk boundary; preserved lone-surrogate literal
bytes are rejected explicitly rather than resolving a replacement-character filename.
Original diagnostic formatting can perform read-only ancestor package and index-shadow
existence probes outside that directory; those probes do not load outside source files.

This is an authored configuration subset, **not full tsconfig support**. Comments,
trailing commas, duplicate/unknown properties, empty roots, extends/include/exclude,
references and unsupported options fail explicitly. `compilerOptions` may restate only
these exact fixed values; it does not select arbitrary compiler behavior:

| Value | Accepted option names |
| --- | --- |
| `"ESNext"` | target, module |
| `"Bundler"` | moduleResolution |
| `true` | noEmit, allowImportingTsExtensions, noUncheckedSideEffectImports, noLib, skipLibCheck, strictNullChecks, strictFunctionTypes, noImplicitAny, noImplicitThis, forceConsistentCasingInFileNames |
| `false` | noResolve, allowJs, resolveJsonModule, strictBindCallApply, noImplicitReturns, exactOptionalPropertyTypes, noUncheckedIndexedAccess |
| `[]` | types |

String values use the spelling shown. `downlevelIteration` is omitted: explicitly
setting even false is a removed-option error at the pinned upstream revision. Its
effective behavior is false. Source constructs governed by class initialization,
built-in iterators and catch-variable options remain excluded; the fixed option record
sets StrictPropertyInitialization, StrictBuiltinIteratorReturn and
UseUnknownInCatchVariables false. Their configuration spellings are not accepted.

`noLib=true` disables automatic library discovery; it does not remove the three explicit
embedded declarations. `types=[]` disables automatic external type discovery. The
complete library bytes and hashes are inherited from the
[accepted collection environment](./COLLECTIONS.md). With `skipLibCheck=true`, the
declarations are loaded and bound, and demanded types resolve normally. Whole-library
declaration bodies and every loaded library API are not claimed checked or supported.

## Supported projects and prominent limits

User source forms are `.ts` implementations. Relative Bundler resolution includes
extensionless/dotted basenames, explicit `.ts`, `.js` to `.ts` substitution and
directory index lookup, with upstream candidate precedence. An unsupported candidate
or package.json selected by that search fails explicitly instead of being skipped.
Unrelated ancestor package metadata does not enable package resolution.

Supported module forms include named/default/namespace/side-effect imports; type-only
imports and exports; exported variables, functions, aliases and interfaces; named
exports/renames, default expressions, named or anonymous functions and named interfaces; named/star/namespace
re-exports and string export names. Imported types and callable values compose the
accepted generics, collection APIs, recursive records, unions/intersections, mapped
utilities, contextual callbacks, tuple/rest/spread and loop behavior across files.
An anonymous default interface is parser-invalid at the pinned revision and reports
diagnostic 1003; it does not count as successful semantic checking.

The [valid record project](../examples/projects/grouped-records/) has five modules
with a recursive Row/RecordLink type relationship, grouping/select helpers, a barrel
and an application: 100 physical lines (86 nonblank), 2,930 source bytes. Its
[invalid variant](../examples/projects/grouped-records-invalid/) has five modules,
110 physical lines (95 nonblank), 3,354 source bytes, and
adds cross-file missing exports/dependencies, incompatible values/callbacks/property
access and a type-only value use. This is type-checking evidence, not executed runtime
behavior or a scalability claim. These sizes exclude the three embedded libraries.

Material exclusions remain: package/bare specifiers and Node-format resolution;
user `.d.ts` files; JS/JSX/TSX/JSON/.mts/.cts source; ambient modules/augmentation;
import attributes, import-equals/export-equals, dynamic import/import.meta; source
import-type nodes and typeof type queries (ordinary runtime typeof remains supported);
classes/new, destructuring, object rest/spread,
optional chaining, async/generators, custom iterators, broader libraries and the other
[collection syntax limits](./COLLECTIONS-CONTRACT.md). Malformed syntax reports parse
failure without claiming complete semantic diagnostics. Project references, incremental
builds, emit and language services remain separate work.

## Verification

The campaign uses the pinned original Go Program and default module resolver under
equivalent files/options/libraries. It compares complete applicable diagnostic
channels and file locations plus selected module/export/alias/type/signature
relationships. Disk-root relocation is handled only for known source/module paths;
unrelated user text must not be rewritten. Private unrooted clone identity omissions
must be explicit in the final assessment. Original tests, adapted assertions and
authored comparisons are separately inventoried.

For a focused exact comparison, build the independent reference oracle and point the
project comparator at an already built managed adapter or native validation executable:

```bash
bash tools/bootstrap-upstream.sh
bash tools/bootstrap-go.sh
bash tools/build-projects-oracle.sh
python3 tools/compare-projects.py \
  --requests validation/projects-corpus/canonical-requests.jsonl \
  --expected validation/projects-corpus/canonical-expected.jsonl.gz \
  --executable artifacts/projects-validation/native/Projects.Validation \
  --output artifacts/projects-validation/focused
```

The final corpus and helper assets must exist before this command can run. This is
verification work; ordinary `tscheck` invocation never builds or runs the Go oracle.
The validation executable uses newline-delimited project requests containing a name,
relative file-to-source map, configuration text and optional selected query locations.
It compares the actual shared Program file set, diagnostics and semantic relationships.
It is separate from the ordinary disk CLI.

The final managed snapshot also passed all ten CLI cases in 3.747 seconds,
including canonical demo diagnostics and the 14 lexical controls:
[current managed report](../artifacts/projects-validation/build8/managed-cli/report.json).
The host explicitly rejects non-scalar module path bytes; valid escaped pairs and
replacement-character names retain actual resolution identities in the shared tests.

To reproduce the comprehensive frozen acceptance run:

```bash
bash tools/bootstrap-upstream.sh
bash tools/bootstrap-go.sh
python3 tools/run-projects-gate.py
```

It runs one retained collection/regression cascade, replays original project and helper
outputs, publishes both project validation and ordinary `tscheck` with Native AOT, and
executes project comparisons, shared assertions, process order and ordinary CLI exit/
location checks. It guards current source, assets, all nine native binaries, oracle
executables and historical accepted fixtures/proofs. The recorder is a separate final
step after successful evidence and review:

```bash
python3 tools/record-projects-evidence.py
```

The [final gate](../artifacts/projects-validation/final-gate.json) and
[durable proof](../validation/results/projects-verified.json) record actual execution.
The native validator compared all 48 projects exactly in 41.528 seconds;
the [order checks](../artifacts/projects-validation/native-order/report.json) passed
30 executions per runtime, and the [ordinary native CLI](../artifacts/projects-validation/native-cli/report.json)
passed all ten cases in 2.197 seconds, preserving complete original diagnostics for
both shipped examples. All 11 newly applicable original test functions passed.
Source, assets, protected historical proofs and upstream tracked source did not drift.
Earlier accepted evidence is preserved rather than rewritten by this campaign.

The ordinary native commands used by the gate can be run directly:

```bash
artifacts/projects-validation/cli-native/tscheck --project examples/projects/grouped-records/tscheck.json --json
artifacts/projects-validation/cli-native/tscheck --project examples/projects/grouped-records-invalid/tscheck.json --json
```

Their measured statuses are 0 and 1 respectively. This invocation needs no Go oracle.

The first comprehensive run was stopped after a confirmed disk-path encoding defect;
its [partial status](../artifacts/projects-validation/stopped-surrogate-gate/status.json)
is not acceptance evidence. The narrow host repair and focused controls closed
before the fresh successful run recorded above; preserved scanner bytes were unchanged.

Ordinary project work beyond this boundary still encounters inherited language limits
such as classes/new, destructuring and source import-type/typeof type queries, separately
from package resolution and general project-host support. A substantial next language
campaign should address those common source forms on the existing shared project host;
package/Node configuration and resolution require their own coherent host campaign.
