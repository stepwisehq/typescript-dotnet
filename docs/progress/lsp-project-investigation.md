# Language-service project state investigation

2026-10-12. Investigation only; no implementation, builds or acceptance runs. The new comprehensive LS/LSP mandate supersedes older no-active-work text. Baseline is accepted emit/performance `9b8fa74a`; upstream remains `fed0bf24149fb1ed36039212648bafdafc1ea10e`. Next action is root's dependency/ownership assignment, not a simplified project-update adapter.

## Existing foundation and missing owner

The accepted [compiler Program](../../src/TypeScript.Scanner/Compiler/Program.cs) already implements `ReuseProgram`/`UpdateProgram`, replacement eligibility, copy-on-replacement and full-rebuild fallback. Preserve it; project incremental state is not a reason to invent another compiler. Existing parser, binder, module resolver, config parser, cached VFS, content-mapper models and hash implementation are reusable dependencies. This was a source-presence assessment, not a renewed acceptance audit.

The current [disk Project](../../src/TypeScript.Scanner/Projects/Project.cs) explicitly describes itself as an authored disk API adapter. Its one loaded configuration/Program is not upstream `internal/project.Project`, and it has no editor overlay/session/snapshot owner. No counterparts were found for the upstream project dirty maps, parse refcounts, snapshot file system, registry builders, session or project checker pool. Keep the disk API intact and introduce the genuine package in a distinct namespace; avoid merging two different lifetime contracts merely because both types are named Project.

## Dependency order and concrete scope

Counts below are pinned `^func ` declarations, including constructors/receivers, excluding tests and generated stringers. They describe complete source families, not acceptance counts.

| Order | Complete source family | Files / functions | Responsibility |
| --- | --- | --- | --- |
| 1 | `project/dirty/*.go`; `refcountcache.go`, `ownercache.go`, `parsecache.go`, `extendedconfigcache.go` | 12 / 86 | Clone-on-first-mutation maps and boxes; serialized publication; acquire/ref/deref; snapshot owner sets; exact parse/config hash keys |
| 2 | `filechange.go`, `overlayfs.go`, `snapshotfs.go` | 3 / 117 | Open/change/save/close versions; immutable text and line caches; snapshot disk/directory caches; dirty, missing-parent and symlink invalidation |
| 3 | `configfileregistry{,builder}.go`, `project.go`, `projectcollection{,builder}.go`, `compilerhost.go`, `checkerpool.go`, `programcounter.go` | 8 / 179 | Config discovery/references; configured/inferred/synthetic projects; frozen compiler host; selective program replacement; service checker leases |
| 4 | `snapshot.go`, `snapshothost.go`, `session.go`, `watch.go`, `autoimport.go`, `api.go`, `client.go`, `background/queue.go`, `logging/*.go` | 11 / 208 | Retained immutable requests; coordinated snapshot transitions; pending changes, watcher/ATA diffs, debounce, cancellation and shutdown |

First proposed implementation ownership: complete order-1 cache/dirty family, with matching original tests, followed by the whole overlay/snapshot-FS family. An initial cache packet can be complete without fabricated Session/LS objects. `ParseCache` requires real file-handle and parse-key contracts; coordinate those with the document owner before implementation rather than adding placeholder factories. The document layer also requires actual `lsconv/linemap.go`/`converters.go`, protocol URI/position/change/watch types and encoding policy. Overlay partial changes are applied using the negotiated position encoding, not by interpreting LSP UTF-16 positions as Go byte offsets.

Snapshot and project owners have genuine cross-package joins: LS host/preferences/converters, autoimport registry, content-mapper runtime host, diagnostic locale, logging and client capabilities. Define those source contracts alongside their owners, then complete consumers. Full session cannot truthfully be marked complete with autoimport/ATA callbacks that do nothing.

Important upstream invariants: parse publication binds before exposing the AST; identity includes parse options, hash and script kind. Content-mapped canonical/supplemental files share one bundle reference. Extended configs track owner IDs because multiple acquires by one snapshot must count only once. A failed replacement releases its speculative parse acquire; successful replacement refs unchanged files. Snapshot readers retain old immutable state while builders lazily clone mutations. Dirty changes distinguish missing parents, node_modules changes, realpath aliases, reopen and excessive watch batches.

The service checker pool is a new owner, not a replacement for the accepted compiler pool. Upstream separates one diagnostics checker, request/file-affine temporary query checkers and a persistent API checker. Cancellation, idle disposal and discard semantics preserve held old-snapshot checkers. Retain existing transferable checker mutex ownership; do not restore the old compiler's lifetime behavior by treating every query as a full fresh compile.

## Original tests and external boundaries

The project tree has 28 test files / 107 test functions plus two `TestMain` lifecycle functions (including dirty/background/logging/ATA subpackages). Early evidence comes from `dirty/syncmap_test.go`, `refcountcache_test.go`, `extendedconfigcache_test.go`, `overlayfs_test.go`, `snapshotfs_test.go` and bulk-cache tests. Follow with snapshot/session lifetime, config-file changes, default-project selection, references, untitled files, watchers/timeouts and all 39 checker-pool test functions. Preserve table rows and concurrency assertions separately from function counts; reuse `projecttestutil`'s genuine virtual FS/client/npm mocks as explicitly authored host adapters where needed.

ATA is concrete dependency pressure: four source files / 27 functions plus generated types-map data, package validation/discovery, semver and injected `NpmExecutor`. Port discovery/installation/state algorithms; decide real npm/network host and workspace-trust policy explicitly. Tests can use the original injected executor without claiming live package installation. Similarly content-mapper process spawning is gated by `RunExternalCode` and host capability; existing compiler models alone do not implement the external session process host.

Project watcher registration is client-driven via `Client.WatchFiles`/`UnwatchFiles`, so session support does not inherently require command-line build/watch orchestration. The separate `execute/incremental` build-info/solution-builder owners are not substitutes for service snapshots and should not silently expand into this first project-state dependency packet. Root should fix their final epic boundary explicitly, along with actual transport/host support, while keeping the comprehensive language-service contract intact.


## First dependency packet written (not accepted)

Dirty eight-file algorithms, RefCountCache/OwnerCache/ParseCache/ExtendedConfigCache and Background.Queue are now complete source counterparts. Collections.SyncMap has the approved optional comparer constructor and explicit-state Range overload. The genuine overlay file-content/handle models and SnapshotFS source interfaces are present because parse/config caches consume them; OverlayFS event/filesystem operations and SnapshotFS implementation remain unfinished and are not represented as complete files. Logging awaits its focused actual format boundary; no service or session placeholder was introduced.

Shared ProjectCacheChecks contains six original standalone cache functions, ProjectDirtyChecks all four subtests of original TestSyncMapProxyFor, and ProjectBackgroundChecks all five subtests of original TestQueue: eight original functions total. TestRefCountingCaches and snapshot/session assertions await their actual owners. Thin xUnit wrappers added; no managed/native execution yet. Private non-reentrant SemaphoreSlim entry mutexes retain upstream callback lock boundaries; the accepted checker mutex/pool remains unchanged. File handles retain immutable borrowed byte owners; legacy Go byte-character inputs acquire one byte owner at the document boundary. Captured map traversal is the same permitted mutation-tolerant managed adaptation as accepted Collections.SyncMap; clone comparers and nil-map headers remain explicit.

Next: central initial dependency compile/review coordinated by root, then finish complete overlay/filechange/snapshotFS and logging owners. Do not infer acceptance from API readiness or formatting.


Logging Logger/LogCollector/LogTree (34 original functions) and focused raw-byte format adapter are now source-written, not executed/reviewed. Original logging entries are one interface check plus one empty test; both preserved as weak original evidence. Two authored controls cover raw/quote/duration/nop behavior and tree/concurrent publication. Total current shared entries: ten original functions + two authored controls, twelve xUnit records. Logging formatter is scoped to observed Logf verbs; structured `%v` owners require explicit Go projections and server's separate `%T/%+v` consumer remains future work, not silently guessed CLR reflection.

Root assigned reciprocal independent review of stable checker query/AST navigation counterparts before further overlay operations. Initial owner packet is stable for inventory review. No builds by lane; root central compile remains pending protocol syntax readiness.

Comparer-copy review correction: Collections.SyncMap Clone/ToMap now retain the optional comparer in each copied owner. One separately authored shared/xUnit control covers case-insensitive identity and copy isolation (ten originals + three authored, thirteen records). FileChange complete source counterpart is written; full OverlayFS/SnapshotFS operations continue next, without a simplified document update substitute.

Root approved complete editor-required VFS AppendFile/Remove/Chtimes owners and server-used wrapper forwarding; they are genuinely absent in accepted read/write-only FS. This lane owns the focused optional mutation contract/OsVfs leaves, preserving SnapshotFS deliberate mutation panics. Core.TextChange/ApplyBulkEdits two-function direct byte-owner counterpart is now source-written; full VFS and overlay operations are next. No builds or acceptance runs.

Current first central diagnostic compile freeze: focused VFS Append/Remove/Chtimes and real bundled/cached forwarding are saved, source-written but not independently accepted; Core edits and FileChange are complete source counterparts. Two authored ProjectFileMutationChecks rows cover ordered raw-byte edits and actual append/timestamps/remove/symlink boundaries, bringing current project checks to ten originals + five authored (fifteen xUnit records). No direct Core textchange tests exist upstream; cachedvfs TestRemove is a separate original function still awaiting its shared fixture. Linux transport preserves original bounded1024 directory reads/reopen and fd-relative no-follow/error order; other platforms use managed filesystem fallback and are unverified. Whole OverlayFS/SnapshotFS remains pending after build feedback.

OverlayFS now has the complete original filesystem/immutable directory merge/event reduction/processChanges bodies saved, including negotiated encoding consumers, each required map/slice clone, and original save/watch/reopen ordering. Actual Converters/LanguageKind conversion owners remain protocol-lane dependencies not yet on disk; no replacement was introduced. Source counterpart requires API reconciliation/review and original two overlay tests before acceptance. SnapshotFS whole59-function owner is next.


SnapshotFS continuation: complete immutable cache owner, builder finalization, directory
pruning, realpath alias publication, reload/invalidation, watch filtering, ordered
open/close conversion and source tracking bodies are now written. File reads occur
outside entry locks and reload mutates only dirty-cloned cached owners. Both cached
line-info owners reset on changed content; an existing authored control now observes
precomputed old/new owners without increasing the control count. Overlay's genuine
converter dependency has landed. These sources remain integration/review pending;
no LS behavioral or Native acceptance has run. Original SnapshotFS/Overlay assertion
porting and coherent owner review are the next validation responsibilities.

Both original OverlayFS functions and six directly executable SnapshotFS functions
now retain their original assertions in shared checks and thin xUnit wrappers.
The original fifty concurrent directory reads remain concurrent.
TestAutoImportBuilderFS is explicitly pending its genuine auto-import owner.
The linked accepted compiler virtual filesystem provides read/symlink behavior; a
finite mutation adapter retains test-map state and rebuilds that test backing host
after actual mutations (not a production/session update algorithm). Provenance is
in lsp-project-files-tests.json; none of these new tests has executed. Mapping
now marks whole OverlayFS/SnapshotFS source-written/integration-pending.

Watch dependency now source-written: all22 original functions and the complete
project Client contract, with required GetCommonParentDirectories/common-parent
worker added to the existing path owner. Six original watcher test functions are
retained in shared checks/xUnit. Byte glob/component headers, required concat copies,
watcher slice sharing, ignored-set clones and first registration IDs are preserved.
Nullable Go watcher receivers use explicit C# helper calls. Request APIs use the
protocol request context and the actual generated telemetry union alias.
No build/behavioral acceptance yet; next owner is config registry/project graph.

Config registry15 functions and project identifier14 functions are now written.
The immutable SessionOptions/CheckerPoolOptions inputs and full ProgramCounter3
identity/refcount methods are also source-written. Genuine project checker pool
(17 functions) is next: it requires request ID/lifetime derivation on the explicit
request context, preserving the ordinary compiler CancellationToken interface.
Project/collection/compiler-host/config-builder remain missing, not placeholders.
No additional compilation or behavioral verification has run.

The complete project checker pool17 methods are now source-written. Dedicated
diagnostics, temporary query and persistent API checker identities, category-aware
request/file affinity, transferable once-release leases, cancellation association
cleanup, canceled-checker replacement, global diagnostic accumulation and discard
behavior retain the upstream control flow. Standard SemaphoreSlim slot counting
reverses channel occupied-slot representation only; timers use monotonic elapsed
time and preserve in-flight callbacks/discard guards. The explicit protocol context
request ID/lifetime dependency is being completed by the protocol lane. Go int64
MaxCheckers remains long until the checked CLR array/semaphore storage boundary.
All original pool tests depend on genuine Session setup; no miniature replacement
fixture was introduced. Project/compiler-host/collection/config-builder remain next.

APIState required map clones/equality (2 methods), ModuleResolverFactory and exact
ls.Project interface are now written as real graph dependencies; remaining graph
classes are not stubbed. Pending VFS source reconciliation restored rootOpenDir
ENOTSUP→ENOTDIR and symlink-vs-real-ENOTDIR error classification, with the actual
readlinkat EINTR/ERANGE/full-buffer growth rules. Exact Go toolchain source/hash is
in the local mapping packet; this is only the demanded RemoveAll dependency closure.
No tests/builds executed; all new VFS/project owners still require independent review.

## ATA dependency closure source boundary

Complete ATA installer/model17 functions, discovery7 functions and the459-entry
active filename map are now source-written. Scoped package validation3 was retained
and now accepts both borrowed byte and legacy raw-string headers directly. Npm
execution is injected through the actual cancellation/cwd/raw-args/raw-output/error
contract; a real process host and Session consumer will follow, without live registry
correctness claims. Typed JSON destinations preserve map/struct/null shape through
closed decoders. Original upstream event and latest-version TODOs stay explicit.

The missing Core.ThrottleGroup3-function closure is now present, preserving shared
semaphore slots, unconditional acquisition, first returned error and wait-for-all.
The derived errgroup context is discarded upstream; callbacks retain their supplied
context. Accepted WorkGroup scheduling was not changed. Standard nonreentrant
SemaphoreSlim represents sync.Once serialization and occupied-slot channels.

Original TestInstallNpmPackages retains485 package strings and both subtests;
TestDiscoverTypings retains all nine subtests and exact expected nil paths, name sets
and watcher order. Both use shared xUnit/Native-compatible assertions; they are not
executed yet. ATA is API-reconciled/formatter-clean and awaits central compilation
and independent review. Genuine Project/compiler-host/collection/config-builder
continue next; no graph classes or npm-success placeholders were introduced.

Central build10 compiles the full ATA dependency packet. The second managed run
has73 xUnit records:71 passed and the two new ATA original functions failed only
at logging projection of pattern-enumerable Slice headers. The complete actual
ATA/project Slice logging roles now have closed, direct header iteration; nil and
allocated-empty both format as Go `[]`, with raw text/order retained. Existing
authored logging coverage was extended without changing its count. This repair
is saved but unexecuted. Installer sync.Once now also retains its atomic done fast
path and publishes done even on failure; no eager registry allocation is discarded.
The packet remains coherent for the next central dependency verification.

Next genuine graph boundary is Project plus compilerHost and the collection/config
builders. In particular, preserve UpdateProgram speculative dirty-file acquisition
and release, unchanged-file parse-cache refs, snapshot host freeze/tracking disable,
and source content-mapper collision failure derefs. No graph placeholder has been
added to the compilation graph while root verifies these completed dependencies.

The initial dependency gate is now complete: managed73/73 and actual Linux x64
Native dependency assertions passed, recorded in
[language-services-dependencies-initial.json](../../validation/results/language-services-dependencies-initial.json).
That evidence predates the new graph owner joins and does not verify Project or Session.

The full Project/compiler-host/collection/config-registry builder closure is now
source-written and promoted for central diagnostic compilation. It retains actual
UpdateProgram reuse/fallback, parse-cache acquire/deref, mutable SourceFS freeze,
dirty owner cloning, recursive parallel reference loading, configured/inferred/
synthetic root updates, watches and ATA invalidation. Request models are exact
required graph data, not a substitute Snapshot or Session. Established command-line
List storage requires detached removal owners to preserve prior snapshots; this is
an explicit C# representation copy, separate from upstream-required clones.
Diagnostic change detection follows its real callers: retained same-file config
owners or nil-file API diagnostics, full scalar/chain/related fields and Go nil
slice distinctions, without a generalized AST structural comparison. Project IDs
retain direct UTF8 JSON ownership and span numeric parsing at their real boundaries.
The separate complete Core BFS dependency and its original five-leaf function are
source-written, registered and unexecuted. Next: central graph API feedback, focused
change/ownership assertions, then genuine SnapshotHost/Snapshot/Session consumers.

Dependency ordering continuation: after the current complete graph/check boundary,
this lane owns the complete upstream format package followed by ls/change.
SnapshotHost/Snapshot/Session drafts are retained outside compilation while the
service lane completes genuine autoimport and LanguageService contracts. This
prevents invented registries or service stand-ins; it does not narrow the graph
or Session contract. The central build12 graph joins are corrected and saved.
