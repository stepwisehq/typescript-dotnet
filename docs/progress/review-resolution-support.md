# Resolution support: independent core review

Review is limited to the newly selected resolution dependencies and graph/host
integration. It does not claim completion or a new audit of the whole Compiler,
AST, binder, formatter or accepted filesystem/configuration foundation.

## Source review completed

[Module Util](../../src/TypeScript.Scanner/Module/Util.cs) was compared with all
13 functions in [the pinned unit](../../upstream/TypeScript/tsc/internal/module/util.go).
No source finding remains in that comparison. Reviewed responsibilities include
package root extraction using the actual resolver separator leaf, scoped package
parse/mangle/unmangle, versioned types keys, pattern priority, extension admission
messages including effective JavaScript/strict flags, and JS output extension mapping.
This is source review, not independent runtime acceptance.

The source API's typed results retain raw Go-byte paths; accepted legacy disk-facing
APIs have explicit Unicode conversions. The newly demanded filename role is explicit
in source parse options: the default retains legacy scalar filenames and graph parsing
marks byte filenames. The necessary binder external-module symbol producer and
NodeBuilder no-context module filename consumer use the role helper. FileName itself
is unchanged. This closes a concrete double-encoding source seam; Unicode graph and
retained legacy witnesses remain required for runtime qualification.

## Selected loader and graph source review

Current support source through the grouped snapshot 4 was compared with the selected
pinned `fileloader.go` and `filesparser.go` responsibilities. Reviewed root/reference
extension search, metadata package lookup, explicit path/type directives, source import
and string augmentation collection, synthetic tslib/JSX inputs, usage/default modes,
resolution-before-scheduling, JavaScript depth, task deduplication/reprocessing, package
identity redirect, casing explanations, source postorder and actual edge publication.
The module nil-result conversion to an allocated zero result is present in the original
loader; it is not a replacement for the resolver's cache nil identity. The three new
AST format producers, JSX/transform producers and external-module-indicator options
were compared with their exact original conditions; no remaining finding was established.

The graph facade uses the actual resolver and selected loader over read IFS, retains raw
typed paths and parser-owned nodes, and does not enter the checker. Its post-graph check
entry validates the unchanged options and actual loaded source bodies. Resolution-only
settings are cleared solely from the admission comparison copy; the resolver and checker
receive the actual parsed options. The legacy facade has a distinct admitted host policy,
with strict byte-to-scalar conversions at that boundary. The graph loader uses the byte
parser overload and marks the filename role; fixed libraries retain the legacy role.
Ownership checks and cached checker queries do not trigger later discovery.

No remaining source finding was established in these selected responsibilities. This
review does not claim the whole Compiler or AST subsystem. Fresh task storage and the
single-threaded LIFO WorkGroup are explicit adaptations. Pools, snapshot disposal,
project-reference construction/redirect execution, content-mapper execution, supplemental
transformed files, default/lib-reference construction, and automatic global type tasks
are outside the selected graph environment. Resolver APIs still accept redirected
reference data. Authored NoResolve decision edges identify collected syntax without
claiming that the upstream loader resolved those explicit path/type requests. Per-edge
returned traces are observation data; Program tracing phase/watch infrastructure is
external. None of these exclusions should be disguised as empty actual resolution.

Both newly reached PackageJson cache receiver methods were checked against
`cache.go:188–198`: Exists and GetContents are nil-safe; WithPackageDirectory is strict.
Support restored the two nullable operations after the direct data tests exposed the
old helper mismatch. Snapshot 4 compiles the repair, and the complete direct dispatcher
now passes all 38 original Module bodies plus two authored controls. The early snapshot
3 NativeAOT result and retained CLI14 are useful risk/regression evidence, not final
current-source acceptance.

## Still open

The finite package diagnostic-formatting branch remains a separately bounded selected
port; no whole ModuleSpecifiers completion is claimed. Independent resolver/graph corpus,
current native execution and retained final regressions must qualify the source review.
This document records selected source/caller correspondence rather than exhaustive
input coverage or per-function execution.

## R4 producer-to-publication accounting (post-snapshot 4)

The earlier selected scheduling review does not close all downstream diagnostic
publication. This explicit pass follows the contract output categories through their
original producers and final graph publication; the new diagnostic packet is source-reviewed below and awaits compilation/execution.

| Promised category | Original producer/publication | Counterpart and current state | External-only omission |
| --- | --- | --- | --- |
| Source nodes, text, names and metadata | `parseTask.load`, `loadSourceFileMetaData`, `parseSourceFile`, `getProcessedFiles` | [FileLoader](../../src/TypeScript.Scanner/Compiler/FileLoader.cs), [FilesParser](../../src/TypeScript.Scanner/Compiler/FilesParser.cs), [Ast metadata](../../src/TypeScript.Scanner/Ast/Ast.cs), explicit filename role; source4 review complete | Pool/cache acquisition and disposal; transformed/supplemental content-mapper execution |
| Imports, synthetic nodes, string augmentations, usage modes and results | `resolveImportsAndModuleAugmentations`, `getModeForUsageLocation`, collection into per-file resolutions | Loader retains parser nodes, synthetic parent links, actual resolver results and authored per-edge traces; source4 review complete | Executing reference-project redirects or Program symlink cache |
| Scheduled/skipped dependency reasons | Original `shouldAddFile`, `start` depth/elision, package-ID redirect before child collection | Loader/FilesParser preserve decisions and publish authored scheduling enum, actual target identity and package redirects; source4 review complete | Global automatic/default-library environments |
| Explicit path and type references | `resolveTripleslashPathReference`, `resolveTypeReferenceDirectives`, `parseTask.load`, per-file type resolution publication | Actual explicit requests and same task scheduler; NoResolve collected decision edges are authored metadata, not actual resolution claims | Lib-reference/global type environment construction |
| Parse diagnostics and owners | Source parser diagnostics on loaded SourceFiles, Program parse projection | Graph publishes each actual parser-owned diagnostic and raw-role source owner | Compiler semantic diagnostics; transformed/canonical-source maps |
| Processing/include diagnostics | `processingDiagnostic.toDiagnostic`, `createDiagnosticExplainingFile`, `includeProcessor.getDiagnostics` | **Source repaired/reviewed; runtime pending**: UnknownReference direct diagnostic versus explaining-file branch, synthetic-location exclusion and exact chain assembly | Unknown library suggestions when the external lib-reference stage is absent |
| Stored resolution diagnostics | `getProcessedFiles` module/type maps → `includeProcessor.getDiagnostics` → DiagnosticsCollection | **Source repaired/reviewed; runtime pending**: publish final actual stored per-file mode/name module/type ResolutionDiagnostics through DiagnosticsCollection; source4 lacked this publication | General Program option/global/semantic channels |
| Root/reference/package/casing explanation chains | `FileIncludeReason.getReferencedLocation`, `computeReferenceFileDiagnostic`, `explainRedirectAndImpliedFormat` | Root/import/path/casing source4 selected review; **source repaired/reviewed; runtime pending** for implied package format and real package-dedup redirect explanations. Explicit type location has zero PackageId upstream; do not infer it from resolution result | Reference-output execution explanations, content-mapper supplemental and automatic/library include families |
| Final inspection and check separation | Actual loader processed files/results/metadata/diagnostics, authored facade | [ResolvedProjectGraph](../../src/TypeScript.Scanner/Projects/ResolvedProjectGraph.cs) projects complete selected channels; [Project](../../src/TypeScript.Scanner/Projects/Project.cs) admits actual graph/options separately | Whole Program/checker/emitter/environment completion |

The processing/type/synthetic conditions were sent to support for source confirmation
and batching. No production edits or expected-output changes were made by this review.
The complete source4 original38 dispatcher pass does not substitute for these newly
reached graph publication obligations.


## Grouped snapshot 5 source packet reviewed

Compared the updated include-processor and processing-diagnostic bodies with the
exact pinned producer/publication branches. The UnknownReference kind now produces
the direct explicit-type diagnostic at its reference range; synthetic locations do
not become the preferred primary location. Explicit type include locations retain
the original zero PackageId while task scheduling retains the actual resolution ID.
Package redirect and implied-format chains use already-published targets/metadata,
not new resolution calls. Final retained per-file mode/name result associations
publish their actual module/type ResolutionDiagnostics into DiagnosticsCollection.
No additional selected source finding remains in this packet; fresh graph/callback
and retained diagnostic comparisons remain required.

The finite formatter packet was also compared with original `specifiers.go:250–398`,
its selected relative branch and `util.go:30` comparator. Imported filenames plus
actual package-dedup redirects retain source order, ignored-path filtering and package
flags with reference-project/symlink caches explicitly absent. Existing-import reuse
canonicalizes the target using actual host case mode before the broader generator.
Path sorting and relative generation use the source typed case-sensitive operations.
Graph byte filenames and legacy Unicode filenames convert exactly once. Broader
package/path/rootDirs reverse generation remains explicit `semanticUnsupported`
after source existing-import reuse, with truthful graph results retained. No whole
formatter completion or matching runtime result is claimed by this source verdict.

## Resolver observation channel challenge

The initial R1–R3 observation review compared the original result declarations with
both observers, rather than inferring completeness from matching output. All eleven
ResolvedModule fields and seven ResolvedTypeReferenceDirective fields are represented,
including diagnostics, extension flags, PackageId, OriginalPath and AlternateResult.
Returned nonnil objects retain identities independently of equal values. Trace order,
argument nil/empty state and host lookup/read order are preserved. Package cache keys
are deliberately sorted only to root entry/content aliases; complete exported package
field state is selected, while private lazy version-path topology is not observed.
The original concurrency/clone/cache tests remain separate evidence for that behavior.

Static, redirected-reference and raw-options requests invoke distinct original source
factories. RawCompilerOptions uses stored numeric enum values; ordinary config input
uses names. The first original request run exposed three authored raw inputs using
config strings and stopped in Finalize. Those inputs alone were corrected to actual
ESNext99/Bundler100 values; the partial output and panic remain investigation evidence,
not a compiler discrepancy. The corrected request packet contains 62 cases and 279
queries; original execution completed all 62, but paired evidence remains pending.
The managed observer then rejected a reached PackageDirectory trace argument. The
original reflection path exposed an empty object for that private-field type, hiding
its path. An explicit source AsDirectoryPath argument-role projection on both sides
was requested and acknowledged; fresh original output must precede comparison. This
is an observer repair, not a resolver discrepancy.

Two protocol/documentation follow-ups were sent to verification: reject unsupported
request fields or validate the exact published schema instead of silently ignoring
them, and update the early observer README now that static/redirect/entrypoint/graph
modes exist. Original diagnostic repopulation state, private lazy cache topology and
OS timestamps/Sys objects are not implied by this selected projection. Final paired
records and the R4 graph witnesses must still qualify this channel review.

## Compiler input reuse review

Independently compared [the fixture generator](../../tools/resolution-generate-compiler-inputs.py)
with the pinned regular `ParseTestFilesAndSymlinks` input parser, compiler-runner root
selection, and `harnessutil.CompileFilesEx` root filtering. The generator's directive
removal, line joining, filename splitting and link direction match the selected
fixture domain. Existing independent Go extraction evidence reports all 76 inputs
exact. The last-file-only heuristic retains source require/reference/noimplicitreferences
conditions, and JSON/tsbuildinfo root filtering is the actual later harness operation,
not an invented reduction. All virtual source/package/config bytes are retained after
that original extraction. Platform/wildcard directives outside the inventoried domain
reject rather than silently approximating additional cases.

The catalog has 72 reused inputs and four removed-mode-only exclusions, yielding 105
graph requests. Source hashes and exact request-name membership were independently
checked. Removed explicit variants remain excluded instead of receiving replacement
modes. No-config requests use authored JSON configuration from retained directive
variants and the original root heuristic. Embedded-config requests deliberately use
the first original config without the compiler harness's subsequent option overlay;
every unapplied compiler directive is listed separately. In particular, those reused
inputs do not establish their originally intended trace/module/target variants.

The catalog correctly claims zero original compiler assertions. It proves neither
the compiler harness's original baselines nor all original mode variants. The separate
core matrix supplies explicit Node16/NodeNext/Bundler modes, conditional types/imports,
paths/rootDirs/suffixes, symlink options, internal raw options and cache/static behavior;
the reused graph inputs supplement that API matrix. Actual parsed options and graph
records must still be compared against Go for each adapted request. No duplicate
generator or runtime run was performed by this review, and no scope finding remains
in the inspected input adaptation/provenance packet.

## Bounded graph-checking path restrictions (post-snapshot 6)

Followed the actual `LoadConfiguration` → graph → semantic admission →
`CreateForProgram` → external-module lookup/diagnostic formatting path, and searched
those counterparts for remaining relative/node_modules/root/symlink rejection sites.
This is a call-path restriction inventory, not a broad checker audit.

| Site | Classification and disposition |
| --- | --- |
| ProjectAdmission / CollectionAdmission relative-only specifier rule | Explicit full-resolution host capability now propagates through every recursive visit and both preflight/checker entry calls. Legacy entry defaults false; literal/module syntax exclusions remain. Snapshot6 compiled this change. |
| Checker missing-module message selector | Old blanket bare-name throw replaced with actual checker.go15404 builtin-name membership and original diagnostic arms. Required once-initialized Core.NodeCoreModules table/typing-cache leaf source-ported; support independently reviewed. |
| Direct `@types/` module error | Restored the newly reachable checker.go15459 prefix diagnostic before the existing host lookup, including original byte-string arguments. No resolver spelling restriction replaces it. |
| ProjectDiskHost outside-root/node_modules/symlink checks and legacy configuration symlink check | Reachable only through named legacy `Project.Load` compatibility path. Full configuration uses ConfigurationInspection/ResolvedProjectGraph/ResolutionDiskHost over actual OS read IFS; it does not instantiate that restricted host. |
| Checker external-library flag | Removed as a resolver restriction in the earlier typed-result packet. Non-.ts implementation, import-equals, module augmentation, source import attributes and excluded body forms remain genuine semantic capability limits. |
| NodeBuilder node_modules import-type guard / ModuleSpecifiers generated package/path/rootDirs branches | Explicit diagnostic-formatting limit after original existing-import reuse, separately approved. Actual source graph remains inspectable; no reverse-package/path generator completeness is claimed. |
| Checker module environment rejection | Applies to non-Projects semantic profiles; shared project checker keeps module locals separate from script globals. Ambient/pattern augmentations remain excluded semantic syntax. |

No additional graph-checking resolver-only path/root/symlink reject was identified in
these bounded callers. The shipped package-checking examples exposed both obsolete
admission/selector guards before final acceptance; snapshot6 reached the second guard,
and the current source packet closes it. Original package checking, builtin missing
module and affected legacy controls must qualify the next compiled packet.

The additional R4 path-reference edge repair was compared with fileloader.go721–760
and final FilesParser/include publication. Lookup failures and self-reference return
nil resolved-path data, while the edge retains its actual ProcessingDiagnostic until
processed source/include reasons exist. Final ToDiagnostic then supplies the real
reference owner/range and ResolutionDiagnostic scheduling, instead of publishing an
early global diagnostic. No expected-output rewrite or diagnostic constructor guess
is involved. Source review closed; current runtime evidence remains required.

### Current observer host and ordinary CLI review

The authored Resolution runner's vfstest role was rechecked against the source
`convertMapFS`, `comparePathsByParts` and `setEntry` boundaries. Prefix entries retain
their original prefix spelling without propagating a different stored parent into
a leaf; explicit leaves retain the exact input path. The part comparer switches to
the complete remaining suffix once either path lacks a slash. Strict UTF-8 lookups
and nil empty directory/file lists reproduce the reached normalized POSIX vfstest
backing host; this is not a claim that the authored host ports general VFS mutation.
The old accepted Configuration host was not altered. The current69 packet is exact
on snapshot7 after these observer-only corrections.

The ordinary CLI semantic observer uses the genuine parsed config/options and
Program, with the same three hash-checked declaration files as explicit roots. It
gathers config, program, syntactic, semantic and global diagnostics and applies the
original sort/deduplication. The ordinary wrapper compares complete scalar disk
filename/range/UTF16/message/chain/related diagnostics and graph payloads, preserves
unknown trace-shape rejection, and explicitly witnesses the PackageDirectory trace
role. No option masking or target-derived expectation was found. Current11 controls
pass on copied7; final current native execution remains pending.

The current nonempty type-resolution diagnostic slot was source-traced rather than
fabricated: its only appended diagnostic producer is input/output remapping of
exports/imports. Primary type-root lookup does not take that route; relative
secondary lookup takes file/directory search; nonrelative secondary exports lookup
has the lexical node_modules exclusion before realpath. No honest nonempty public
type-result witness was found for the reached ordinary rooted hosts. The stored slot
and final publication branch remain source-reviewed, with actual empty/missing-type
controls and explicit unexercised nonempty publication qualification.

Reviewed current counterpart fingerprints:

- `validation/Resolution.Checks/ResolutionRunner.cs`: `117bbd6de346c7500b6a79362b0adaa83ddb5995a045418b26be2fb7a1a90f99`
- `tools/resolution-cli-oracle/main.go`: `aa5ae11a9d334e6d2e4cb689a9786088a7a0b7fd4c6e01eeee15fd57b279a05e`
- `tools/check-resolution-cli.py`: `18c627aed51b468c132901722dcc1f8d0e9945a3419bb14c2a8cc863b9eb83de`

### Named legacy compatibility repair on snapshot8

The legacy-only authored admission uses the same `LegacyResolutionHost` instance as
the full resolver's IFS and the selected FileLoader host. Its scoped module call
validates the old relative/mode/containment policy, invokes the complete actual
resolver and restores the active flag in `finally`. Only a reached existing source
file probe can reject an unsupported winner; it does not reject an unused shadow
file. The optional directory callback runs after file probes fail, the candidate
directory exists, ESM directory lookup is disabled and package metadata is requested.
It checks only that reached directory's actual package.json. Ancestor package scope
reads are unchanged, and earlier file winners bypass the callback.

This is an explicit authored compatibility departure from the otherwise direct
resolver counterpart. Default and graph hosts do not implement the optional
interface, so the branch is inert there. No host/callback reference is introduced
into shared ResolutionData, no default-host search or result publication changes,
and no old relative resolver algorithm is retained. The typed retained adapter
keeps all prior fixture/assertion/identity/rejection-category obligations. Snapshot8
compiled library/CLI cleanly; focused retained and ordinary runtime is support-owned
and pending at this review checkpoint.

Current reviewed compatibility files:

- `src/TypeScript.Scanner/Projects/DiskHost.cs`: `9ea602b6d07a53148a581f8648df3f623aa3bb5902699157deb96304406673d5`
- `src/TypeScript.Scanner/Compiler/FileLoader.cs`: `f37a6ae0300dbe60edcde9b4671788a833518c90e49c686f2d0d8d6e609001f6`
- `src/TypeScript.Scanner/Projects/Project.cs`: `b141b05670a02bfae7f5bb4eb91f804b477d4d48c8c4de2232dab968b1db4bee`
- `validation/Projects.Checks/ProjectSupportChecks.cs`: `f28e88d31bfbc031d7daeab958a2d5e50a1bb41afe65450bd477d8d34b37b9d7`

### Post-gate checking filename-role repair

The first comprehensive run stopped at the retained authored configuration integration
assertion for a Unicode RootFiles filename. Its new resolution native checks passed,
but that is not acceptance. The failed reports and pre-repair inputs are preserved
under artifacts/resolution-validation/gate-attempt1.

Reviewed the narrowly scoped repair before snapshot9: LoadConfiguration creates a
private graph, then converts only each file's parse-options filename to scalar disk
Unicode and sets its explicit filename role before checker binding. It keeps the
same AST, text, parent/diagnostic ownership, raw PathKey and resolver/cache results.
Standalone InspectResolvedGraph never takes this constructor and retains byte-valued
names. Project metadata/mode/formatter callers use FileNameGoBytes; both formatter
filename consumers select the explicit host role. No global SourceFile contract or
resolver algorithm changes. The Preferences one-line change was independently
reviewed by support.

The original RootFiles Unicode assertion remains unchanged. Additional authored
assertions cover UserFiles, raw filename helper, standalone graph and diagnostic
owner. The former full-config outside-root rejection is intentionally replaced by
a positive capability assertion under R5; the named legacy containment policy and
its existing assertions remain intact. This group is authored, not one of Module38
or Configuration original bodies. No historical expected output/proof changed.
Focused runtime and final comprehensive acceptance remain pending here.

Reviewed scoped counterpart fingerprints:

- `src/TypeScript.Scanner/Projects/Project.cs`: `f3c9b59a282e8db25bed66dd119ac8dcaa64cdd8758edf093c65f738fe532314`
- `src/TypeScript.Scanner/ModuleSpecifiers/Specifiers.cs`: `ce9ce8522d1d0be516495b7342c9d0a3f40026a53cab85e7493bb3067da6b8cb`
- `src/TypeScript.Scanner/ModuleSpecifiers/Preferences.cs`: `84bdd144284d0006c93ec70e86ae30c12bcea64be101711c14537943ba47ab04`
- `validation/Configuration.Checks/IntegrationChecks.cs`: `f05e87c5b75da791f520596e50960f9ad6aeb18b19dabc3b7154616903134b79`

### Default managed filename zero value

Attempt2 completed automatically with a retained Binder default-options failure.
The new byte-role accessor passed the managed record struct's null default filename
to UTF8 encoding; the Go string zero value is empty. Independently reviewed the
accessor-only repair: normalize its local input to empty before selecting either
raw or scalar conversion. Public FileName/parse-options representation, raw strings,
path keys and global GoString remain unchanged. The unchanged retained default
Binder assertion and verifier's raw/scalar/null/empty/Unicode shared group pass
on copied10. Four retained managed suites and full final gate remain pending.

Reviewed Ast counterpart SHA: `c6371f174311bbf05cf65f22a738842683987feeb792a4d1f18e8b215bf4496b`.

Snapshot10 focused runtime qualification: retained unchanged default Binder1,
expanded GraphByteRole1 and full Scanner/Parser/Binder/Checker23,542 pass with
no failures/skips. Initial Scanner/Parser stale helper ABI attempts are preserved;
only Foundation.Checks was recompiled against the current constructor, and current
repeat reports bind the actual production SHA. Final comprehensive/native acceptance
remains pending.
