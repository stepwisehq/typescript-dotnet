# Whole-checker no-emit verification plan

This is an unbound planning note written during the frozen protocol gate. It changes no
current contract, source, corpus, expectation or tool. The authoritative input registry is
[the complete original inventory](../../validation/checker-noemit-inventory/provenance.json):
12,824 compiler/conformance files, 15,190 enumerated configurations, 42 original skips.
It records obligations, not passes. The final gate must account for every configuration
and applicable assertion channel, rather than infer completeness from selected campaigns.

## Preserve the actual compiler harness

Use [CompilerBaselineRunner](../../upstream/TypeScript/tsc/internal/testrunner/compiler_runner.go:59)
for file enumeration and its setting/configuration expansion. Preserve original unit
bytes, variants, symlinks, casing, current directory, root selection, embedded config and
post-discovery option merge. [newCompilerTest](../../upstream/TypeScript/tsc/internal/testrunner/compiler_runner.go:225)
selects roots through the original last-unit require/reference heuristic or actual config
roots. [CompileFilesEx](../../upstream/TypeScript/tsc/internal/testutil/harnessutil/harnessutil.go:115)
adds genuine test lib files, creates the original map FS, wraps bundled libraries, and
constructs authentic mapped-source hosts. Replacing this with each fixture's invented
JSON config would lose original compiler-option and harness-option behavior.

The minimal Go path is additive bridge files in the existing testrunner, harnessutil and
tsbaseline packages, built through a source-hashed Go overlay. They can call unexported
original enumeration, parser, option and walker producers without replacing pinned
source. The unavoidable no-emit adaptation should copy only the source-defined harness
preparation/construction branch before Emit, with exact pinned function/body provenance
and equivalent C# preparation. Do not call full CompileFiles and merely discard emitted
files: it triggers output/declaration diagnostics and changes diagnostic state. No new
general observer or process framework is needed; the existing NDJSON comparator can
compare one complete fixture/configuration/channel record.

## Preserve no-emit assertion channels

The original [compileFilesWithHost](../../upstream/TypeScript/tsc/internal/testutil/harnessutil/harnessutil.go:603)
constructs a pre-check Program (its original TraceResolution=false clone) and a second
Program with actual options, then Emit. It collects config, Program, syntactic, semantic,
global and conditional CaptureSuggestions diagnostics; declaration diagnostics and
post-Emit count invariants are additional operations. For the whole no-emit comparison,
capture these genuine checking channels separately and retain their original collection
order, sort/dedup behavior, file owners, byte spans, arguments, chains, related information
and option policy. Where the original pre-check changes tracing, represent that explicit
source operation; also preserve the actual trace-enabled checking Program observation.
Never force NoEmit, skipLibCheck, noCheck or library flags to make inputs pass.

Original declaration-output diagnostics, actual emitted-output/source-map assertions and
Emit-induced diagnostic/count invariants remain explicitly external output producers;
they cannot silently be required by a combined .errors baseline, or silently be described
as preserved semantic assertions. Original .errors baseline bytes stay bound. A fresh
Go per-channel reference is an assertion-preserving no-emit adaptation when its exact
original checking domain/options and excluded output producers are documented. Mapper
transformed-source policy and diagnostic directives, suggestions, trace-resolution
assertions, and original applicable checker invariants each need their own disposition.
Private cross-runtime storage equality is not a completion condition.

## Reuse the full original public type/symbol walker

[verifyTypesAndSymbols](../../upstream/TypeScript/tsc/internal/testrunner/compiler_runner.go:479)
selects all actually loaded original input/other files and honors NoTypesAndSymbols.
[the original walker](../../upstream/TypeScript/tsc/internal/testutil/tsbaseline/type_symbol_baseline.go:304)
is the required domain, not the current finite identifier query selection. An additive
Go bridge can call newTypeWriterWalker/getTypes/getSymbols and return their actual ordered
records or baseline strings. Port that small traversal and rendering logic faithfully to
C# with source-qualified provenance:

- Actual AST preorder, reparsed-node exclusions, expressions/declaration names/heritage
  qualified-name selection and actual type-node/omitted-expression exclusions. Import/export-name and intrinsic-JSX predicates only choose the original Any shortcut versus NodeBuilder rendering; they do not omit observations.
- GetTypeAtLocation (including heritage parent substitution), original Any handling,
  TypeToTypeNode with NoTruncation/unique-symbol/shadowed-parameter flags, alias expansion,
  genuine printer context and text. The hasErrorBaseline input must follow the no-emit
  checking-channel boundary explicitly rather than accidentally use output errors.
- GetSymbolAtLocation, SymbolToStringEx with AllowAnyNodeKind, original declaration order,
  first-five display limit and remaining count, actual declaration basename/line/UTF16
  location and source text, including original default-library Decl(--,--) markers. Preserve mapped source text when the original loader transforms.
- Original file order and baseline header/CRLF/blank-line assembly/path-prefix fixups. Do not sort observations or modernize quirky source rendering. Fresh ordered walker records may supplement that source formatting, not replace its assertion domain.

These are public compiler behavior/assertion domains; they need source API/display/AST
helper dependencies, not an allocation-ID or private graph census. The C# public wrappers
and NodeBuilder/Printer flags must be checked for exact source coverage before full runs.
Keep original baseline hashes and each existing baseline fixup classified separately.
Do not claim full original compiler assertions from input-only semantic comparisons.

## Execute and account without another infrastructure layer

Use the existing comparator/runner with bounded chunks of fixture/configuration records.
Start with responsibility-selected smoke batches while remaining JS/JSX/module consumers
are implemented, then expand to all applicable configurations only when those consumers
are coherent. A crash/timeout is a failed or blocked record, never a pass or a permanent
scope exclusion. Isolate a failed record with the same options for diagnosis; preserve
its original packet, stderr and exact cause. Do not retry until a run happens to pass.
Use existing process timeout mechanisms and stable input/configuration keys; keep each
record's channel disposition, source/expected/output hash and original/managed/native
execution status. No broad scheduler, versioning or reusable receipt framework is needed.

The final gate requires complete per-configuration/channel accounting, original applicable
full assertion tests, honest original skips/external operations, genuine diagnostics and
walker comparisons, NativeAOT, usable ordinary projects, source review and one retained
cascade. Aggregated TRX execution totals are separate from distinct inventory counts.
A source family is complete only after source implementation and behavioral obligations
are qualified; finite prior campaign passes do not fill unexecuted whole-inventory rows.

## Ten built-API environment inputs remain obligations

Nine samples point to /.ts/typescript.d.ts. APILibCheck additionally points to
/.ts/typescript.internal.d.ts, /.ts/tsserverlibrary.d.ts and
/.ts/tsserverlibrary.internal.d.ts. Their original source/option/baseline bytes remain
bound despite the upstream environment skip. Checking these source files is separate
from executing compiler/watch/transform APIs.

The pinned repository lacks those legacy built artifacts and their declaration-generation
source recipe. Its own package-lock fixes TypeScript6.0.3 at
https://registry.npmjs.org/typescript/-/typescript-6.0.3.tgz with exact SHA512 integrity.
That authentic locked dependency is a viable public API/tsserver declaration input to
prepare and verify; it is not an unpinned replacement. Internal declaration-role mapping
still needs original-source/version corroboration or an explicit coordinator decision.
Do not fabricate internal declarations, simplify samples, or convert missing inputs into
automatic exclusions. Thirty-two removed-option skips likewise remain original pinned
skip dispositions, not passes. Their original parser policy must be accounted separately.

### Verified locked package investigation

The authentic TypeScript6.0.3 package was downloaded only into
`/tmp/typescript-dotnet-locked-api-6.0.3`; no frozen source/assets changed. The 4,515,854-byte
archive matches the exact lock integrity
`sha512-y2TvuxSZPDyQakkFRPZHKFm+KKVqIisdg9/CZwm9ftvKXLP8NRWj38/ODjNbr43SsoXqNuAisEf1GdCxqWcdBw==`.
The local report records the archive/source-lock hashes and package members. Actual roles:

| Original role | Authentic locked artifact | SHA256 | Bytes |
| --- | --- | --- | --- |
| /.ts/typescript.d.ts | package/lib/typescript.d.ts | c967e03c8dbb4770f7e2f0b71b5d13593de679a41cc2b60266c4b69f1342a263 | 588452 |
| /.ts/tsserverlibrary.d.ts | package/lib/tsserverlibrary.d.ts | cee9e9bf9a8f67388ee878afd5e172189421bae6625076b774d68563d036fbfd | 866 |
| /.ts/typescript.internal.d.ts | absent | — | — |
| /.ts/tsserverlibrary.internal.d.ts | absent | — | — |

The nine public API samples now have an authentic locked environment input available
for future fresh pinned-Go no-emit references. Historical baseline/version differences
must be retained as explicit environment adaptations, not erased. APILibCheck still
requires two unavailable internal declaration inputs; classify that genuine environment
limitation separately. No alias/stub declarations or unpinned replacement is authorized,
and no legacy JS declaration build should be invented without its available pinned source.
No sample checking or whole-corpus semantic execution was performed in this investigation.

Independent support read review corrected the plan's import/export exclusion wording:
those predicates affect the Any display shortcut only. It also identified exact demanded
public SymbolToStringEx/SymbolFormatFlags, NewNodeBuilder/shared EmitContext, public/internal
TypeToTypeNode flags, dynamic computed NodeCopy and binding-parameter scope helpers as
future source dependencies. Core/support received them before any full-walker implementation.
No current source or protocol acceptance observations changed.

### Incremental-option checking is a scheduling adaptation

The original harness createProgram wraps actual NewProgram in incremental.Program when
Incremental is true. Config/syntactic/Program/global/suggestion methods forward directly;
semantic diagnostics instead cache/collect affected files through the actual Program's
GetSemanticDiagnosticsForIncremental and apply FilterNoEmitSemanticDiagnostics plus include
diagnostics. The inventory has one explicit buildinfo fixture,
`incrementalConcurrentSafeAliasFollowing.ts`, with outdated state and a chained-alias
concurrency regression. Original testBuildInfoReader deliberately replaces Version with
the current version rather than discarding it. This is not metadata-only behavior.

Coordinator-approved C# scheduling adaptation: recompute complete genuine Program
semantic/query obligations with the original Incremental options and buildinfo files
unchanged, compare against the real Go incremental wrapper, and qualify every incremental
configuration's complete diagnostics/full walker. Do not claim cache/affected-file/builder
parity. Original cache/update/emit assertions remain external operation domains. If a
real result difference demands more than fresh source checking, report the consequence
before expanding source scope; do not assume equivalence or clear the options.

Scratch-only additive Go baseline and pre-emit preparation bridge files plus provenance
are staged under `/tmp/typescript-dotnet-language-verification`. They reuse the original
full walker/renderers and copied original option/FS construction with explicit separate
pre-check/actual-option checking channels; createProgram retains the real Go wrapper.
They have not been formatted, compiled, executed or applied, and are not current gate
assets. No C# display fallback/stub is introduced while demanded source wrappers are pending.
