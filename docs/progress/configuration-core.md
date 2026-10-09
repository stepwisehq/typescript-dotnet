# Configuration subsystem: core assessment

Baseline `2c850db`, pinned original `fed0bf24149fb1ed36039212648bafdafc1ea10e`.
Initial lightweight source assessment completed without production edits or builds.
Coordinator subsequently authorized complete TsOptions interpretation/discovery;
the written contract and agreed dependency seams govern implementation.

## Complete boundary

Recommend all 17 production files in [original tsoptions](../../upstream/TypeScript/tsc/internal/tsoptions/):
210 named functions, 7,631 physical lines including 3,224 generated lines. These are
inventory facts, not effort estimates. Completion means the original entrypoints and
their complete input behavior, not an expanded whitelist around the existing strict
project configuration adapter.

| Original family | Responsibilities / required result |
| --- | --- |
| commandlineoption, namemap, declarations_generated, enummaps, diagnostics, errors | Full ordered option declarations, aliases, enum/list/path kinds, validation and spelling diagnostics; library/target maps. |
| options_generated, parsinghelpers, rawcompileroptions | Every declared compiler/build/type-acquisition option; original defaults, conversion, merge, `${configDir}`, affecting-option iteration, serialization and raw API finalization. |
| commandlineparser, parsedbuildcommandline | Compiler/build argument parsing, response files/cycles, alternate-mode diagnostics, project paths and build-option combinations. Parsing build flags does not perform a build. |
| tsconfigparsing | JSON source and object APIs; actual JSON-mode parser diagnostics, option/source locations, local and package `extends`, arrays/order/cycles/diamond inheritance, config cache, references, mapper descriptors and files/include/exclude discovery. |
| parsedoptions, parsedcommandline, wildcarddirectories | Original equality/nil distinctions; matched specs, ordered roots, reload, extended sources, lazy wildcard/path/reference maps, locale and source/output metadata. |
| showconfig | Complete serialization with relative paths, filtered file lists, implied options and original insertion order. |
| contentmappers | Resolve and validate genuine package manifests without executing packages. |

Parsing options which the current checker cannot execute is still required. Keep this
public configuration API separate from the accepted Projects fixed checker policy;
do not silently replace [ProjectConfiguration](../../src/TypeScript.Scanner/Projects/ProjectConfiguration.cs)
or claim that arbitrary parsed options imply checker/emit parity.

## Existing state and dependencies

There is no TsOptions production counterpart in the current source map.
[OptionsGenerated](../../src/TypeScript.Scanner/Core/OptionsGenerated.cs) has selected
fields/enums only; upstream CompilerOptions has 132 exported fields (plus internal state),
with full Clone/Equals, TypeAcquisition, BuildOptions and enum methods.
[CompilerOptions](../../src/TypeScript.Scanner/Core/CompilerOptions.cs) has selected
effective getters. Full option models/accessors and generated static parse/merge/
serialize access are necessary; reflection would obscure source boundaries and risk AOT.

Accepted scanner/JSON-mode parser, diagnostics and spelling helpers are reusable.
Configuration must retain the original AST/object conversion paths, comments/trailing
comma and recovery behavior, raw property order and diagnostic locations. A strict
System.Text.Json replacement would change those semantics. Preserve Go byte strings
through parsing; scalar-path representability is a disk-adapter boundary, not a global
string normalization.

Concrete missing dependencies:

- Read filesystem operations and typed path kinds: case sensitivity, ReadFile, FileExists,
  DirectoryExists, accessible entries/symlink metadata and Realpath. The root-contained
  Projects disk adapter is not a general config filesystem. Virtual case-sensitive and
  case-insensitive hosts allow exact algorithms without claiming all OS adapters.
- Complete vfsmatch discovery/SpecMatcher and wildcard directory rules, plus glob.Parse/
  Match used by PossiblyMatchesFileName. Extension precedence includes TS/JS/declarations,
  explicit JSON patterns and mapper extensions. Literal files survive exclude patterns.
- Package config resolution: getExtendsConfigPath calls module.ResolveConfig, which uses
  fixed NodeNext JSON/CJS resolution. Support confirmed package maps/typesVersions and
  realpath closure cannot be excluded by a config-input whitelist.
- Mapper metadata: ResolvePackageDirectory, complete needed package fields, Definition/
  Manifest/Mapper equality and raw JSON options. RunExternalCode controls the original
  diagnostic/drop behavior, but this subsystem never invokes mapper Host/IPC/transform.
- OutputPaths/common source directory helpers, ProjectReference path helpers and locale
  parsing. GetOutputFileNames/source-to-dts maps return metadata without emitting files
  or recursively building referenced projects.

## Seams, ownership and sequence

Retain original ParseCommandLine/ParseBuildCommandLine, ParseConfigFileTextToJson,
ParseJsonConfigFileContent/ParseJsonSourceFileConfigFileContent,
GetParsedCommandLineOfConfigFile(Path), ParseExtendedConfig, RawCompilerOptions.Finalize
and ConvertToTSConfig boundaries. Host and ExtendedConfigCache are explicit interfaces;
ParsedCommandLine owns original source/spec/cache state. Do not substitute per-file
checker creation or hand-authored option/output tables.

Coordinator ownership: core owns TsOptions/*, complete Core options/helpers,
ContentMapper metadata, OutputPaths and Glob. Support owns typed paths, read FS,
vfsmatch/walkdir, Module config/package-directory closure, PackageJson and Semver.
Verification owns independent original fixtures/protocol/direct assertion inventory
and integration controls. One central build owner remains core.

Stage coherent units: (1) full option/path/FS/descriptor models and generated declarations;
(2) CLI/raw option conversion and JSON config source/object paths; (3) inheritance/cache
and package resolution plus complete discovery; (4) showconfig, reload/matching/reference/
output metadata and mapper manifests; (5) all public families, direct upstream assertions,
independent diagnostics/order/cache identities and native gate. A simple config is an
early integration witness, never the acceptance boundary.

Highest risks are interface width and path-kind conversion, ordered/nil JSON and options,
cache lifetime/cycle behavior, extension precedence, and package resolver closure.
Avoid the prior late-cache pattern: inventory lazy ParsedCommandLine/extended-config
state, raw option identities and query/reload effects before freezing observer schema.
Claiming complete TsOptions excludes external process execution, watch/build scheduling,
actual emit and checker implementation for arbitrary option combinations; it does not
exclude declared input forms or demanded package config/mapper metadata resolution.


## First implementation batch (historical unbuilt checkpoint)

The complete 132-exported-field CompilerOptions model, TypeAcquisition/BuildOptions, enums,
shallow Clone, field-specific Equals and stored-field descriptors are generated from
locked source by `tools/configuration-generate-options.py`. All declaration data and
all 13 generated option routines have source-hash-checked generators. Generated
merge/build-info/serialization subsets follow upstream selection, rather than assuming
every stored field participates in every operation. These routines are staged with
supporting conversion, ordered storage, metadata/equality and diagnostic helpers;
configuration parser/discovery/public command-line entry points remain in progress.
No central build or runtime acceptance is recorded for this batch.

Go `*int` fields use standard `StrongBox<long>`: full 64-bit values, shallow pointer
sharing in Clone, value equality in Equals, nil distinct from a box containing zero.
`CompileOnSave` will retain the analogous mutable bool pointer when its model lands.
Paths/raw content retain Go bytes and invalid virtual identity; only the OS adapter
strictly decodes scalar UTF8. Configuration AST filenames explicitly retain raw bytes,
while legacy project AST filenames retain their admitted Unicode role.

The shared raw JSON adapter is an authored AOT-safe token/ordered-tree boundary;
JSONC is still parsed by the original TypeScript AST/parser. Input Unmarshal is strict
UTF8/escaped-surrogate, duplicate-name policy is explicit; Marshal permits replacement
of invalid string bytes. Exact observable adapter errors and byte serialization remain
subject to differential review. Full `internal/json` implementation is not claimed.

Locale is a real dependency. The selected x/text v0.42.0 grammar/lookup/canonicalization
closure includes complete generated registered-subtag/alias tables. Root authorized
this proportionate expansion; CultureInfo, language matching and collators are not
substitutes. The value type is staged for the independently owned diagnostic writer;
actual Parse and registry port remain pending.

Implementation/function status is tracked in [the compact inventory](../configuration-functions.json).
It records source translation separately from compilation/test/acceptance evidence.


## Current integration checkpoint

Four actual shared-library compiler invocations have run: declaration pass 1
(1.56 seconds, four dependency naming errors), pass 1b (1.31 seconds, one core
missing import), pass 1c (5.06 seconds, ten body errors), and pass 2 (4.90 seconds,
three remaining diagnostic-writer Tristate receiver errors). All other current core
and dependency bodies compile; no successful binary/runtime acceptance is claimed.
The writer owner has the compact remaining packet. The locale development declaration
explicitly throws until the genuine separately owned x/text closure is installed;
that declaration cannot pass campaign acceptance and is not a success substitute.

The actual source option descriptor list contains 132 exported CompilerOptions fields
(not the earlier investigation count of 140), four TypeAcquisition and six BuildOptions
fields. Original clone pointer and value-equality semantics are preserved separately.
Command-line worker/response/list/enum parsing is staged; public command/build wrappers
await complete parsed metadata. JSONC AST text conversion now retains source diagnostics,
ordered raw values and explicit byte-string filename ownership. Output filename/common
source metadata and the complete original Glob algorithm are staged, with no emit execution.


## Complete translation checkpoint (build 5b)

All 210 named functions in the 17 TsOptions production files now have translated
counterparts. This is implementation progress, not complete test or acceptance
evidence. The consolidated static observation adapter exposes the original 11
config-spec slots and extended-config payload without invoking lazy getters.

Eight actual shared-library compiler invocations have run. In addition to the
historical four error passes above, build 3 passed in 5.98 seconds, build 4 passed
in 6.42 seconds, build 5 failed in 6.03 seconds with seven core local-name, string
marshal, TextRange and nullable-flow errors, and build 5b passed in 5.01 seconds
with zero warnings/errors. The immutable build 5b library was handed to verification.
Owned handwritten production was subsequently formatted without algorithm changes.
The real separately owned locale parser has replaced the temporary throwing
declaration; its grammar and generated registries still need independent acceptance.

Executed independent assertions: all six complete compiler-option groups (132
fields), eleven semver groups, seven packageJSON groups and four command-line
groups pass supplied snapshots. Config-text comparisons are 11/11 exact, semver
boundaries 26/26, and writer comparisons 22/22. Full parser/discovery/public-metadata
coverage, comprehensive regressions and NativeAOT acceptance remain pending.
Dependency source review is recorded in [the independent review](review-configuration-support.md).

## JSON and inspection integration checkpoint (build 7)

Eleven actual compiler invocations have run. Build 6 compiled the library but failed
its CLI dependency with three inaccessible runtime-helper references (7.15 seconds);
6b repaired the explicit CLI byte/display adapter and passed library plus CLI in
1.90 seconds. Build 7 passed both in 7.63 seconds with zero warnings/errors. Immutable
snapshots and hashes are retained under `artifacts/configuration-validation/build7`.
The production inventory now records translation separately from its compiled
snapshot and pending final independent review; all 210 translations remain distinct
from acceptance.

The JSON adapter's observable error contract required a selected Go standard-library
closure. [The source/hash mapping](../configuration-json-provenance.json) identifies
token grammar, strict UTF8/escaped-surrogate checks, delimiter state, pointer/offset
errors and supported static container traversal. General reflection, arbitrary custom
marshalers, streaming I/O and canonical/reformat options remain external. Ordered raw
field occurrences and token source offsets/pointers let the typed package decoder skip
unknown numeric fields lexically and retain its actual duplicate/partial-state rules.
No expected-message rewriting or fixture lookup is used.

A helper-only probe found a real managed stack overflow in the prior recursive writer
on a legal 10,000-container input. Explicit container work replaces that recursion;
the original depth limit, empty-container fast paths, cycle threshold, traversal order,
partial output and error pointers remain observable. Helper comparisons currently
match 53 malformed first-token/error probes, the same 53 generic partial-tree/error/
marshal records, ten depth/cycle serialization cases, six decoding depth cases and
nine nonfinite-number cases. These are focused development evidence; the latest
nonfinite and generic failed-read refinements follow build 7 and await the next shared
snapshot plus independent public API and native replay.

The public nil-list `GetNameMapFromList` boundary now returns allocated empty name
maps, as original `len`/`range` operations do. The matcher/walker handoff preserves all
20/8 original functions and passed all 28 shared groups on an isolated build 5b
reference. Independent assertion review and NativeAOT execution remain separate.

## Immutable build 8 and early native risk check

Build 8 is the twelfth actual compiler invocation: library plus CLI succeeded in
7.79 seconds, with zero warnings/errors. The immutable library SHA256 is
`84946fdcc651d2e6cecf432b2f2281fe5955085d10cc1b9f2ba29396e8afda4d`.
Verifier's supplied-build comparisons close all 25 JSON depth/cycle/nonfinite
records, both name-map controls and the package 35/duplicate 25 packets. Package
error precedence is 9/10; the remaining completed numeric token before a missing
object close requires a post-8 raw-field retention repair, with unchanged expected
output.

The one focused early NativeAOT publish used captured assertion/adapter source and
that immutable library. Two failed publish attempts exposed 21 then two generic
JSON-node overload sites (42 then four IL2026/IL3050 errors). Explicit typed-node
calls corrected the adapter without changing its schema or adding suppressions.
The third attempt succeeded. Actual Linux x64 native execution passes the five OS
assertion groups and all 25 JSON records; the same known package-precedence case
remains 9/10. Captured inputs and executable hashes show no drift. The executable
is an ELF x86-64 binary without a CoreCLR dependency. Durable development reports
are under `artifacts/configuration-validation/build8/early-native`, including
`focused-report.json` and each exact comparison's `report.json`.

This is a risk check, not final acceptance. A later OS raw-name collision and the
parsed absolute-root integration failure require the reviewed post-8 source packet.
Live assertion-source holds ended after capture; unrelated test work continues
without another early-publish freeze.

## Build 9 and metadata assertion handoff

The thirteenth shared compiler invocation passed library plus CLI with zero
warnings/errors in 3.58 seconds. Immutable build 9 contains the package pending
scalar-token retention repair, parsed absolute-root admission and raw Linux
directory/link boundary plus mapped nativepath branch closure. Its library SHA256
is `d2956e46e8fac8d134d3e68e31ae9b243f0c875f4a79e1a085377d7093e0680a`.
Verification owns the repaired package-priority and raw OS assertion replay;
support owns integration smoke. Subsequent source cleanup removes the extra
validation walk before the nativepath fast path; it is pending the next natural
coherent snapshot, rather than a standalone rebuild.

The additional full original assertion families are handed to verification:
[ParsedOptionChecks](../../validation/Configuration.Checks/ParsedOptionChecks.cs)
preserves three functions, [OutputPathChecks](../../validation/Configuration.Checks/OutputPathChecks.cs)
five, and [WildcardDirectoryChecks](../../validation/Configuration.Checks/WildcardDirectoryChecks.cs)
three. All eleven groups passed an isolated immutable build-8 reference.
[The source inventory guard](../../tools/configuration-inventory-metadata-tests.py)
checks all fifteen parsed/flattened mapper fields, twenty-five explicit mutations,
seven nil/empty slice slots and original function names/hashes independently of
production equality descriptors. Thin xUnit wrappers reuse the shared bodies;
independent assertion review and native/xUnit execution remain separate.

[Independent dependency source review](review-configuration-support.md) now covers
the remaining selected Locale, typed paths, Semver and PackageJson closure, with
explicit omissions and the current Linux nativepath cleanup disposition. No new
source finding remains from that review; comprehensive acceptance is still pending.

## Build 10 and assertion peer review

Fourteenth actual shared compiler invocation: library plus CLI passed with zero
warnings/errors in 3.34 seconds. Immutable [snapshot](../../artifacts/configuration-validation/build10/snapshot.json)
contains exact static configuration descriptor tag fixes, the typed StartsWithDirectory
nonempty-relative predicate and source NativePath fast-path cleanup. Library SHA256:
`cc8e6e7fb2413ba9a7206ee495474c1e66eaf4758288ba603d718df15b25ced4`.

[Independent assertion review](review-configuration-assertions.md) compares 50 verifier
TsOptions original functions and 27 selected/adapted Locale groups against actual source
bodies. The five skipped M49 zero-return checks and complete Go JSON tag suffix guard
were restored in test/source-domain code; no compiler defect was claimed for either.
Source-table generator checks pass. Core-owned metadata11/matcher28 still require the
verifier's independent assertion verdict; final native/xUnit acceptance remains separate.

Core owns the final configuration gate/recorder/shell scaffolding exclusively; verifier
owns canonical corpus/manifest and comparison/order/ordinary-CLI observation tools.
The new configuration/native/CLI/shared/order portion precedes exactly one retained
projects cascade. No historical evidence recorder will be invoked or overwritten.


### Raw JSON producer closure after snapshot 11b

The selected JSON20 source tests passed 18/20 on copied11b. The two failures
were a test-operation mismatch: generic Unmarshal reports different object-name
and numeric-EOF errors than raw Decoder.ReadValue and Encoder.WriteValue.
The actual original API trace is preserved under
[json-original-api-boundary](../../artifacts/configuration-validation/json-original-api-boundary/).
Generic decoding remains unchanged. Package Expected callbacks genuinely demand
ReadValue prevalidation; public JsonValue/Definition.Options marshaling genuinely
demands whole-buffer WriteValue validation and compaction. The producer split
and source functions/conditions are recorded in
[JSON provenance](../configuration-json-provenance.json).

Independent precompile review with support accounted for all142 generated option
fields and the supported static metadata models. Nil versus allocated-empty
TSConfig slices and JSONv2 false/omitempty ProjectReference behavior were
restored in the same batch. Raw writer compaction preserves number spelling,
source default string formatting and member order; invalid fragments commit
no value/delimiter, retaining prior output and original semantic wrappers.
Raw nil versus allocated empty and Definition.Options successful-empty omission
are explicit conditions. Shared source visibility remains finite; no streaming,
reflection or custom-option framework was introduced. This is implementation
and source-review evidence, with fresh JSON20/raw/static-model/corpus runtime
qualification still pending the coherent snapshot12.

Snapshot12 compiled the shared library and ordinary CLI with zero warnings/errors
in 3.258 seconds. The immutable
[snapshot](../../artifacts/configuration-validation/build12/snapshot.json) records
the actual library hash. The central
[compiler ledger](configuration-builds.json) now contains17 invocations totaling
76.558 seconds (10 clean,7 failed iterations), excluding adapter/oracle/native
publishes. This remains compilation evidence; fresh source-current runtime
qualification is owned by verification.

The final gate tooling now checks the exact110 main plus42 full TypeScript
dependency original assertion obligations, with missing/duplicate/source-drift
negative controls recorded separately. JSON typed-error controls execute before
reproducing the corpus and retain raw original stderr. All new subsystem checks
precede exactly one retained Projects cascade. At that historical snapshot no
comprehensive gate had started.

## Final source and gate checkpoint

The final shared snapshot is [14](../../artifacts/configuration-validation/build14/snapshot.json).
All 210 functions in the 17 TsOptions files remain translated and compiled.
The [central compiler ledger](configuration-builds.json) records 19 actual
library/CLI compiler invocations: 12 clean and seven failed development passes,
85.811 seconds total. Adapter/oracle builds, NativeAOT publishes and comprehensive
gate invocations are excluded from that total.

Three unsuccessful gate attempts are preserved rather than relabeled as acceptance:

- [Attempt 1](../../artifacts/configuration-validation/gate-attempt1/final-gate.json)
  was intentionally stopped after a JSON test-catalog provenance collision.
  Source-qualified identities now distinguish the selected jsonwire QuoteRune
  assertions from unselected strconv rune APIs; no test or expected byte changed.
- [Attempt 2](../../artifacts/configuration-validation/gate-attempt2/final-gate.json)
  failed original-Go replay because two authored ordinary-map fixtures assumed
  deterministic multi-key traversal. Their prior-prefix assertions now invoke the
  actual OrderedMap source API, with two added single-key ordinary-map controls.
  The prior 317 assets and failure remain archived; all prior 265 cases are unchanged.
- [Attempt 3](../../artifacts/configuration-validation/gate-attempt3/final-gate.json)
  passed the new configuration portion but exposed a retained legacy empty-config
  null-list regression. The compatibility adapter now iterates the existing nullable
  AST property list safely, allowing its original missing-files rejection. No AST
  allocation, historical assertion or expected output changed. All 27 retained
  project shared groups, the new integration control and CLI14 passed snapshot14.

The fresh [comprehensive gate](../../artifacts/configuration-validation/final-gate.json)
then passed in 860.209 seconds with source, asset, historical, upstream and assertion
obligation drift all false. Actual Linux x64 NativeAOT execution matched all 319
configuration records in 1.190 seconds across groups, all 217 shared assertion groups,
38 order executions per runtime and 14 ordinary CLI cases plus 14 lexical controls.
All 152 complete original TypeScript functions replayed without failure or skip.
The new configuration xUnit suite passed 536 cases; the single retained Projects
cascade passed 23,542, giving 24,078 total with zero failures and zero skips.

These are actual whole-subsystem and dependency observations, not a claim that every
individual function was exercised. The raw fixture correction does not establish
deterministic traversal of ordinary multi-key Go maps or their correlated partial
output/error offsets. The selected JSON/Locale and Linux adapter limits remain
explicit in their provenance and the contract. Independent final evidence review
approved the current hashes, native executables and complete assertion inventory.
The [durable proof](../../validation/results/configuration-verified.json) is now
recorded without overwriting any earlier campaign proof. The complete configuration
contract is accepted at this source snapshot.

The next coherent subsystem candidate is the remaining Module/Program resolution
and loading boundary. Configuration completion already required genuine package
exports/imports/typesVersions and native path behavior, while downstream checking
still explicitly rejects unsupported effective options, package source resolution
and excluded implementation syntax. Complete that subsystem and its original tests
before dependent checker integration; do not treat the newly complete configuration
parser as evidence for arbitrary project checking, emit or build execution.
