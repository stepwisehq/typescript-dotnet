# Program environments: use and verification

**Verified Program construction and environment assembly.** The fixed
[contract](PROGRAM-CONTRACT.md) is complete at revision
`fed0bf24149fb1ed36039212648bafdafc1ea10e`, from checkpoint `1d6bd61`.
See the [assessment](PROGRAM-ASSESSMENT.md), [durable proof](../validation/results/program-verified.json)
and [resumption state](PROGRAM-STATUS.md).

## Environment construction and semantic checking

The Program environment follows actual configuration, discovery and resolution:
source files, genuine bundled default or selected libraries, transitive library and
type references, package declarations, project-reference mappings and Program-owned
diagnostics. Loading declarations does not establish that the current checker can
check their bodies or every API they declare.

The ordinary `--inspect-program <tsconfig>` entry exposes this
environment without acquiring an unsupported semantic checker. It includes configuration,
syntax, binding, Program and inclusion diagnostics. The [disk example](../examples/program/README.md)
uses genuine ES2022 libraries, application types, a path source and project-reference
metadata; an invalid variant supplies a missing-path case. Both complete outputs and
exit codes agree with pinned Go under actual Native AOT. Existing
`--inspect-config`, `--inspect-project`, `--config` and named `--project` commands retain
their documented policies and regression protection; inspection is not a replacement
for actual checking.

No declarations may be simplified, source dependencies dropped or diagnostic categories
suppressed to fit the supported semantic profiles. Environment failures, reported
construction diagnostics and unsupported semantic operations must remain distinct.
The [checker roadmap](CHECKER-COMPLETION-ROADMAP.md) describes the remaining semantic
work and its eventual no-emit completion gate.

## Supporting scope

Program's local update/reuse, lazy checker-pool construction, reference configuration
loading, source/output redirects and output-path collision validation are included.
No referenced project is built and no JavaScript or declaration output is written.
In-process content-mapper callback/result validation and span mapping are included;
external mapper processes/IPC and enabled performance tracing are outside this contract.
Read-side filesystem caching is included; new writable filesystem infrastructure is not.

Verification is required on actual Linux x64 Native AOT. Virtual Windows/UNC or
case-insensitive hosts exercise path algorithms but do not prove another native platform.
The [test inventory](program-tests.json) distinguishes preserved original assertions,
adapted subsets, authored differential cases and external tests. A compiler input reused
for environment observation is not a passing full compiler test.

## Verification sequence

Use the original direct assertions and affected managed consumers before the costly
native acceptance and retained regression cascade. New API signatures require normal
incremental dependency builds so adapters do not keep stale assemblies. Final evidence
must identify exact source/assets, native executables, upstream reference output,
independent review and preserved historical expectations. The accepted gate passes
24,598 xUnit cases with zero failures/skips, 186 paired cases, 78 shared groups and 76
repeated-order executions per runtime, plus both ordinary disk examples.

The ordinary command is:

```sh
dotnet publish src/TypeScript.Check/TypeScript.Check.csproj -c Release \
  -r linux-x64 -p:PublishAot=true -o artifacts/tscheck-native
artifacts/tscheck-native/tscheck --inspect-program examples/program/tsconfig.json
artifacts/tscheck-native/tscheck --inspect-program examples/program/tsconfig-invalid.json
```

The exits are 0 for an environment without observed in-scope diagnostics,
1 for configuration/syntax/bind/Program/inclusion diagnostics and 2 for host or
construction failures or invalid invocation. The example returns 0;
the invalid variant returns 1 with TS6053. Both are verified in managed and native execution.
JSON reports `semanticCheckingRequested: false` and keeps the diagnostic channels
separate. Inclusion diagnostics follow upstream public `GetIncludeProcessorDiagnostics`
policy, including its option/source-dependent skipping; global construction diagnostics
come from `GetProgramDiagnostics`. Loaded files and resolution results are reported
independently. No global or semantic checker pass is implied. Display paths are Unicode; Base64 fields preserve original path/text bytes.

The [build prerequisites](../README.md#build-and-verify) still apply. For a host without
Clang, append `-p:CppCompilerAndLinker="$PWD/tools/.cache/native/bin/cc"` to the publish
command when using the established local native compiler fallback.

The complete campaign gate is:

```sh
python3 tools/run-program-gate.py
```

It replays original Go assertions and corpus results, runs xUnit and managed controls,
publishes and executes both Linux x64 native targets, and runs the retained resolution
cascade once. It preserves earlier expected fixtures and proofs. Its report is
`artifacts/program-validation/final-gate.json`; a successful gate is required before
recording a new proof with `python3 tools/record-program-evidence.py`. The recorder
refuses to overwrite an existing proof. Reproduction should run the gate without
re-recording an already committed result.

The gate requires the pinned upstream checkout, established Go/.NET toolchains and
native prerequisites. Ordinary use of the published executable requires neither Go
nor an upstream checkout. Exact original library bytes are embedded resources.

Large-project correctness/performance comparisons against Go and JavaScript tsc remain
deferred until checker and required infrastructure completion.
