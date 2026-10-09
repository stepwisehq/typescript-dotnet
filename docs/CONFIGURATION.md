# Configuration interpretation and project discovery

The [fixed contract](CONFIGURATION-CONTRACT.md) is accepted on **Linux x64 Native AOT**.
See the [assessment](CONFIGURATION-ASSESSMENT.md),
[durable evidence](../validation/results/configuration-verified.json) and
[resumption state](CONFIGURATION-STATUS.md).

`tscheck` can interpret an upstream-style TypeScript configuration, including JSONC,
inheritance and file discovery, independently of whether the checker supports the
resulting project. Configuration inspection preserves the original interpreted
settings and discovered files. It does not certify Program-owned option-combination
validation or semantic support for those settings and files.

## Build and inspect

From the repository root, use the .NET 10 SDK and a Linux x64 Native AOT C/linker
toolchain. The fallback below uses the compiler wrapper available in this environment:

```bash
repo_root=$(pwd)
native_properties=()
if ! command -v clang >/dev/null && [[ -x "$repo_root/tools/.cache/native/bin/cc" ]]; then
  native_properties+=("-p:CppCompilerAndLinker=$repo_root/tools/.cache/native/bin/cc")
fi
dotnet publish src/TypeScript.Check/TypeScript.Check.csproj \
  -c Release -r linux-x64 -p:PublishAot=true "${native_properties[@]}" \
  -o artifacts/tscheck

artifacts/tscheck/tscheck --inspect-config examples/projects/grouped-records/tsconfig.json
```

Inspection writes JSON to stdout containing the upstream show-config projection,
the discovered file list, exact filename bytes in `FileNamesBase64`, diagnostics
and the exit code. The display strings are for reading; the base64 values preserve
the interpreter's original byte values. Show-config is a resolved projection, not
a lossless reproduction of the input text.

Exit status **0** means inspection completed without configuration diagnostics,
**1** means configuration diagnostics, and **2** means an unsupported host operation
or invocation failure. An inspection exit of zero does not mean the project has
been type-checked. The published command needs no Go executable or upstream checkout.

## Check a supported project

```bash
artifacts/tscheck/tscheck --config examples/projects/grouped-records/tsconfig.json
artifacts/tscheck/tscheck --config examples/projects/grouped-records-invalid/tsconfig.json --json
```

The examples inherit [checking-base.json](../examples/configuration/checking-base.json)
and discover their source files with `include`/`exclude`. Their existing TypeScript
sources are unchanged. The invalid example deliberately returns failure.

Checking reports configuration diagnostics before semantic admission. It then
uses the actual interpreted options and file roots. It never rewrites them to
match a fixed profile or drops unsupported discovered source forms. Exit status
**0** means successful checking, **1** means compiler diagnostics and **2** means
unsupported settings, source behavior or host failure. Ordinary diagnostics go
to stderr; `--json` writes structured output to stdout while retaining the process
exit status.

The semantic profile remains the accepted [bounded project capability](PROJECTS.md).
It requires ESNext target/module, Bundler resolution, the explicit strict checking
settings in the shared base, `noEmit`, `noLib`, `skipLibCheck` and `types: []`.
The three genuine pinned ES5/decorators/decorators.legacy declarations are still
loaded explicitly. `skipLibCheck` does not establish whole-library checking or
support for every API in those files. Additional settings are rejected explicitly.
Use the shared base rather than relying on default values: upstream defaults can
enable behavior that the current checker does not implement.

Checking still supports bounded `.ts` implementations with the accepted relative
ES-module behavior. Package imports, user declarations, other source forms,
reference execution, content-mapper execution and source paths escaping the project
remain outside its boundary. Inherited language limitations include classes/new,
destructuring, async/generators, optional chaining, object spread and custom iterators.
Configuration inspection can correctly describe projects using these features;
that does not make them supported for checking. No JavaScript or declarations are emitted.

The old `--project <tscheck.json>` entry remains a named strict compatibility adapter.
Its authored restrictions are documented in the [previous project guide](PROJECTS.md).
Use `--config` for genuine configuration interpretation and discovery.

## Public API and subsystem scope

```csharp
using TypeScript.Compiler;

ConfigurationInspection inspection = Project.InspectConfiguration(configPath);
var diagnostics = inspection.Diagnostics;
var parsed = inspection.Parsed;
byte[] showConfigJson = inspection.ShowConfigurationJSON();

// Reports configuration diagnostics, then enforces supported checking behavior.
Project project = Project.LoadConfiguration(configPath);
var checkingDiagnostics = project.Check();
```

The mapped [TsOptions sources](../src/TypeScript.Scanner/TsOptions/) provide the
underlying configuration, raw-object, command-line/response-file, build-argument,
inheritance, discovery, reload, query and serialization APIs. The ordinary command
is deliberately smaller than a full compiler CLI. Build arguments, references and
output filenames are interpreted as metadata; this does not execute builds or emit.
Package lookup for configuration inheritance and mapper manifests is separate from
the checker’s still-excluded source package imports.

The [source map](../source-map.json) and [function inventory](configuration-functions.json)
record the 17 upstream TsOptions counterparts, generated material and supporting
dependency boundaries. The selected Go JSON and x/text compatibility paths preserve
behavior that standard .NET APIs alone do not match. They are not complete ports
of those external libraries.

Actual platform acceptance is Linux x64 only. Virtual Windows paths and case modes
are testable without claiming Windows execution. Encountered Linux directory names or successfully resolved symlink targets
that cannot be represented as scalar UTF8 are rejected explicitly before replacement
decoding can alias another file. A missing target retains the original failed-lookup
behavior; no target bytes are returned or decoded. Pure configuration values and virtual paths retain
original Go bytes. OS metadata has documented representation limits; see the
[contract](CONFIGURATION-CONTRACT.md).

## Verification

From the repository root, reproduce the complete guarded gate:

```bash
bash tools/bootstrap-upstream.sh
bash tools/bootstrap-go.sh
python3 tools/run-configuration-gate.py
```

The gate reproduces the pinned Go observations and selected original tests, runs
configuration xUnit tests, publishes and executes the configuration validator and
ordinary command with Linux x64 Native AOT, then runs the retained project/foundation/
semantic regression cascade once. Source, test obligations, assets, original sources,
native executables and historical proof files are guarded against drift. The ordinary
command itself does not require any of this verification infrastructure.

Only after a successful gate and closed independent review, record the evidence:

```bash
python3 tools/record-configuration-evidence.py
```

The [corpus manifest](../validation/configuration-corpus/manifest.json) identifies
exact requests, original expectations and helper provenance. A focused replay against
an already published native validator can use the same comparator, for example:

```bash
python3 tools/compare-configuration.py \
  --requests validation/configuration-corpus/subsystem-requests.jsonl \
  --expected validation/configuration-corpus/subsystem-expected.jsonl.gz \
  --executable artifacts/configuration-validation/native/Configuration.Validation \
  --output artifacts/configuration-validation/focused
```

Original upstream assertions, adapted dependency assertions and authored differential
cases are accounted for separately in the [test inventory](configuration-tests.json)
and its linked dependency catalogs. Public raw JSON error observations canonicalize
only the original library's deliberately variable modal phrase on actual typed
SemanticError instances; raw Go traces and negative controls are preserved. Compiler
and ordinary CLI diagnostics are compared exactly. See the
[adaptations](CONFIGURATION-DEPARTURES.md) for the precise limits.

The successful gate passes 24,078 cumulative xUnit cases with no failures/skips,
319 new native comparisons, 217 native shared groups, 38 order executions per runtime
and 14 ordinary native CLI cases plus lexical controls. All retained native gates pass.
See the [assessment](CONFIGURATION-ASSESSMENT.md) for original-test dispositions and
observation limits. This completes the configuration contract, not full compiler parity.
