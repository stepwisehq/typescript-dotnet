# Whole-checker no-emit verification plan

This unbound plan began during protocol acceptance. Protocol commit `c5e96e1` released
the implementation freeze; the additive harness and faithful managed walker are now being
implemented without changing accepted corpus, proofs or the complete inventory. The authoritative input registry is
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
The three additive original Go preparation/baseline bridges are now applied under
[the new observer](../../tools/checker-noemit-oracle/) and compile cleanly through an
additive overlay (`go test -run '^$'`: no test cases executed). The
[managed full walker](../../validation/Program.Checks/CompilerBaselineWalker.cs) is a direct
source traversal/renderer port; its isolated consumer now compiles against candidate 1 using the joined core/support
NodeBuilder/Printer API packet. No display fallback or stub is introduced. Remaining
scratch is preserved in ignored `artifacts/language-staging/verification`, not only /tmp.
Actual original option-harness fatal/skip first decisions will be recorded as
source-qualified metadata while unchanged-option no-emit checking still executes; those
dispositions are distinct from the 42 original pre-configuration skips and target failures.

## Live first producer milestone

[The additive driver](../../tools/checker-noemit-oracle/runner_test.go) compiles and
uses the original configuration expansion, original preparation and full renderer. One
unchanged `asyncFunctionReturnNonPromiseThenable` configuration produced its complete
checking channels (two semantic diagnostics), 404-byte type baseline and 700-byte symbol
baseline. This is original reference production, not a complete historical compiler
assertion pass or a managed/native claim. The
[direct provenance](../../tools/checker-noemit-oracle/provenance.json) records source
hashes and excluded output operations. The managed source walker and full preparation consumer compile against the immutable
first production snapshot; the first complete record matches Go exactly.

The [locked declaration assets](../../validation/checker-noemit-environment/typescript-6.0.3/)
now include byte-exact public declarations, package metadata, license and notices.
[Preparation](../../tools/prepare-checker-api-environment.py) and its `--check` both
verify upstream lock version/URL/SHA512 integrity plus member hashes. No API fixture
semantic execution is claimed; the two internal declaration roles remain absent.

## First managed original batches

Raw-byte fixture extraction compares all 12,824 accepted inventory rows exactly (unit names,
byte hashes, file/global options, symlinks and current directory). This qualifies the
raw parser only: the original compiler disk-read producer additionally decodes BOMs.
The corrected decoded preparation inventory and independent managed comparison below
are authoritative for current configuration obligations; neither is checking success. The full isolated consumer references only
[immutable candidate 1](../../artifacts/language-validation/build1/snapshot.json),
whose Scanner SHA256 is `4771108a974b32dad902ed965499892c837a2f2b544cf0b0a1dd14125d8eafd7`.
No production C# build was performed by verification.

The first end-to-end `asyncFunctionReturnNonPromiseThenable` record compares exactly
across the separate pre/actual checking channels, correlated diagnostic owners, full
404-byte type/700-byte symbol baselines, and trace. The first responsibility batch
contains 19 actual original configurations covering JS/JSDoc, JSX, decorators and
modules; 9 complete records match. All 10 failures remain captured in
[the candidate-1 report](../../artifacts/checker-noemit-validation/language-batch1/report.json)
and its full request/Go/managed streams. Distinct causes: five rows dereference a nil
external-module exports map; two JS signature displays reject the original empty-this
fallback; three rows omit intrinsic-JSX/import-type/import-equals public symbols.
These are concrete source-consumer omissions, not incidental private graph differences.
Core/support are repairing them for the next coherent snapshot; no expectations changed.

The [managed preparation](../../validation/Program.Checks/CompilerNoEmitHarness.cs)
and [observation](../../validation/Program.Checks/CompilerNoEmitRunner.cs) preserve
actual harness option parsing/defaults, embedded config/root selection, authentic
`/.lib` bytes, shared source AST cache and original channel order. Parent review caught
and corrected trace capture timing (after actual checking, before baseline queries)
and `libFiles` parsing (actual ParseListTypeOption/Elements metadata). Trace formatting
now uses the actual diagnostic stringifier/byte-argument formatter before encoding once,
matching the original Message.Localize default-English path. Mapper test-host callbacks
remain explicitly pending, rather than returning a fake successful transformation.
The 15 compiler mapper inputs require only their authentic in-process test handlers;
the production subprocess/IPC implementation remains a separate operation.

## Decoded preparation reconciliation and original invariants

[The supplemental inventory](../../validation/checker-noemit-decoded-inventory/)
leaves the accepted raw inventory/provenance untouched. It calls actual
`getCompilerFileBasedTest`/`osvfs.ReadFile` before original configuration expansion
for all 12,824 source files, retaining their original file-byte SHA256. It records
15,330 successfully expanded configurations, including 39 configurations across
35 fixtures from the original pre-configuration skip list; 15,291 belong to the
12,782 ordinarily enumerated fixtures. Seven original expansion failures are
explicit removed `module: none` values, all within the original 32 removed-option
skips. These failures and original skips are dispositions, never checking passes.

The prior raw registry changed on 827 ordinarily enumerated files exclusively
because of BOM decoding: 820 UTF-8 BOM, six UTF-16LE and one UTF-16BE. Of these,
821 change settings/configuration values; six change units without changing
settings. The decoded expansion adds 101 net ordinarily enumerated variant
obligations. [The delta/provenance](../../validation/checker-noemit-decoded-inventory/deltas.json)
records every old/new configuration without changing any original fixture.
The independent managed parser/expander matches all 12,817 successful expansions
plus all seven explicit original expansion failures. No default/options rewrite
was used; a null original configuration is an empty settings map at preparation.

Two additional preparation defects were repaired: the original config VFS receives
a last-wins map of repeated virtual filenames, and the managed driver accepts
original nil configuration maps. [The authored filesystem](../../validation/Program.Checks/CompilerBaselineFileSystem.cs)
preserves source stored spelling, component ordering, symlink precedence and
explicit canonical collision rejection independently of accepted ConfigurationFS.

[The corrected candidate-3 batch](../../artifacts/checker-noemit-validation/responsibilities-decoded/candidate3/report.json)
contains the same 337 whole original fixtures with their genuine decoded variants:
510 configurations, 508 exact complete records, two public JSX diagnostic
differences, 33.661 seconds, executable/input drift false. The previous raw509
report remains intact; its original failed row was an absent raw-registry variant,
not an original checker failure. Both corrected runtimes execute the exact original
union-ordering and parent-pointer assertion domains after type/symbol/trace
observations. [The C# assertion adapter](../../validation/Program.Checks/CompilerOriginalInvariantChecks.cs)
retains deterministic PCG shuffle/sort checks and within-runtime reference identity;
no private graph or cross-runtime cache census is published.

The authentic public API environment helper is integrated on both runtimes and
independently checked against the exact locked archive. Nine public API fixtures
have complete-record parity. The absent internal declaration environment remains
explicitly unavailable for its historical assertions; fresh checking still reports
its actual missing-import diagnostics and executes both original invariants.

The mapper helper directly ports seven original in-process fixture callback
algorithms and their decode/identity boundaries. Candidate 5 matches all 15 mapper
records after the source diagnostic alias-display repair. The original subprocess
implementation is not claimed. The JSX typeargument delimiter diagnostic also
matches; the ReactNode qualifier and extra API heritage diagnostics remain genuine
public differences assigned to source owners.

## Whole reference and candidate-5 comparison

[The complete original reference](../../artifacts/checker-noemit-validation/whole-standard/original/report.json)
contains all 15,291 ordinarily enumerated configurations: 15,291 observed, zero
failed rows, 126.226 seconds, source/input/executable drift false. It preserves
complete no-emit diagnostics, original type/symbol baselines and both original
within-runtime assertions. It is reference production, not a managed or historical
emit-test pass.

[The isolated candidate-5 build](../../artifacts/checker-noemit-validation/consumer5-whole/build-report.json)
binds the immutable production source snapshot and compiled consumer sources.
The complete candidate-5 managed comparison finished in bounded chunks against
those saved Go records: 15,164 exact of 15,291, 127 failed, 894.700 seconds,
drift false. Failures separate 95 observed public-record differences, ten caught
source/query/printer exceptions and 22 missing rows after a single 120-second
timeout. The timeout starts with the original excessive-template regression;
its later 21 rows are not automatically classified as source failures. The optional reference reuse seam verifies all reference source,
environment, request, binary and output hashes plus exact named row selection;
C# source corrections do not rerun the original reference. All chunk failures and
full records are retained. Original skipped configurations and seven genuine
removed-option expansion failures remain separate from the standard run.

The tightened independent configuration reconciliation has now passed on candidate
5: all 12,817 successful decoded expansions and the exact text of seven original
expansion failures, including symlink/global-option/current-directory metadata.
No original options or fixture bytes changed.

[Candidate-6 targeted replay](../../artifacts/checker-noemit-validation/whole-standard/observed-failures-candidate6/report.json)
resolves 75 of the 105 observed failures, with 30 remaining; the separately
[isolated timeout tail](../../artifacts/checker-noemit-validation/whole-standard/timeout-tail-candidate6/report.json)
resolves all 21 later rows and retains a genuine 30-second no-output timeout for
`templateLiteralTypeExcessiveLength.ts`. The unchanged original expects bounded
2589 diagnostics for that regression. Remaining causes are grouped by source
responsibility; no comprehensive managed repeat is scheduled before convergence.
The candidate-6 authentic [API environment replay](../../artifacts/checker-noemit-validation/api-environments/candidate6/report.json)
now matches all ten complete fresh-Go records and original within-runtime invariants.

[The in-process xUnit assembly](../../tests/Checker.NoEmit.Tests/) has been staged
with complete-record theories invoking the same original walker and union/parent
assertions. It starts no process/build per configuration. Only the original-shaped
AST cache is shared (complete parse options/text/script-kind key); each row creates
fresh Program/checker/options/mapper/trace state. A final managed xUnit pass will
replace a redundant managed observer pass; actual peak RSS can be captured with
ordinary OS tools. No test parallelism setting was changed. Three authored
preparation controls cover BOM/directive timing, stored-case/canonical collisions
and original removed-value failure; current isolated compilation is clean.

## Final candidate and execution organization

The complete source-5 feedback run's 127 failures are preserved. Candidate 6
resolved the API invariant/public-diagnostic causes and most observed failures;
candidate 7 matched 30 of the remaining 31, including the excessive-template
regression. [Candidate 8](../../artifacts/checker-noemit-validation/whole-standard/remaining-candidate8/report.json)
closes the sole remaining tuple-discriminant public diagnostic: one complete record
exact, zero failures, 0.830 seconds, drift false. There is no additional full managed
observer repeat before final xUnit qualification.

[The final registry](../../validation/checker-noemit-corpus/manifest.json) binds
15,304 genuine checking records, 33 separately labeled original preparation-error
dispositions and three authored controls. The isolated readiness xUnit run passes
37/37 (all 33 dispositions, three controls and one complete-record theory), zero
skips, 675 ms. xUnit truncates long string arguments in display labels; final
identity qualification uses complete fresh persisted records with duplicate/missing
rejection, plus strict actual TRX class/method/count/pass checks. The final pass
is 15,340 xUnit executions, followed by one retained protocols cascade and the
same 15,304 checking records through actual Linux x64 NativeAOT. No private
representation census or second comprehensive managed observer pass is added.

Four authored former JS/JSX/decorator restrictions and two Resolution CLI rejection
controls are promoted to unchanged-input complete pinned-Go observations.
[The exact migration](../../validation/checker-noemit-corpus/authored-capability-migration.json)
preserves old corpus/proof bytes and named legacy profile controls. The four
Program observations execute inside the existing retained shared check rather
than increasing the new checking-registry row count. The CLI pair now compares
complete genuine source diagnostics/exits, with all nine earlier Go comparisons
retained. Source-owner whole-function and independent harness reviews precede
the coordinated final freeze.

## Recorded qualification

[The durable proof](../../validation/results/checker-noemit-verified.json) records
15,340 passing new xUnit executions: 15,304 complete original no-emit checking
records, 33 original preparation dispositions and three authored controls, with
zero failures or skips. The complete actual row set is checked independently of
truncated xUnit display names. The managed process peaked at 6,499,196 kB RSS.
Actual Linux x64 NativeAOT matches all 15,304 records across 120 bounded chunks;
all original union-ordering and parent-pointer assertions execute. Native full
configuration reconciliation matches the prior managed 12,824-fixture output
byte-for-byte, including seven original expansion-failure dispositions.

The first gate's passing managed evidence remains bound to its actual original
DLL and inputs. Its failed retained test-adapter compilation and two actual
metadata-only AOT publish failures are preserved. The independently reviewed
[fixed composition](../../artifacts/checker-noemit-validation/continuation-attempt1/fixed-composition.py)
admits only the exact typed rooted-helper adapter and metadata serialization
source transitions; checking entry code, production source 8, checking inputs
and references remain unchanged. The repaired rooted-helper original assertions
execute under actual AOT as part of the 27 passing Projects controls. All failed
and remaining retained command suffixes then pass. There is no general resume
or source-mismatch bypass.

The selected aggregate is 40,291 executed xUnit cases, including repeated retained
cases; it is not a count of distinct upstream tests. The proof binds 24 Linux x64
ELF paths, 287 unchanged historical files and five clear drift checks. Its
2,428.190 seconds is the sum of qualified execution phases: original gate
547.552, retained suffix 1,458.479, successful native publish 11.490 and native
checking chunks 410.669. Native checking and retained continuation overlapped;
this is not a single-process wall-clock duration. The two failed publishes,
3.064 and 2.917 seconds, are recorded separately.

Original historical emit/declaration-output and incremental-builder/cache
assertions remain external to this no-emit adaptation. APILibCheck's two original
internal built declaration roles remain unavailable; its fresh missing-import
record and runtime invariants are compared, without claiming historical built
environment success. [Cached-reference reproduction](../CHECKER-NOEMIT-REPRODUCTION.md)
is separate from the comprehensive original-origin audit and does not require
regenerating Go expectations for ordinary C# tests.

The cached-reference documentation smoke builds the real Release xUnit project
and selects exactly one existing `compiler/2dArrays.ts` row: one execution,
one pass, complete saved-record equality. The real native validation project
also publishes successfully; direct execution matches that same complete record
with explicit exit 0. The selected native command ran twice while capturing its
process status; no full corpus or retained suite was repeated. Registry, original
reference and proof bytes were not changed. The ignored
[smoke report](../../artifacts/checker-noemit-cached/smoke-report.json) preserves
the actual TRX, output and new smoke-binary hashes.
