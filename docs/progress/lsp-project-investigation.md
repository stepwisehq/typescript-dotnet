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

The project tree has 28 test files / 109 top-level `Test*` functions (including dirty/background/logging/ATA subpackages). Early evidence comes from `dirty/syncmap_test.go`, `refcountcache_test.go`, `extendedconfigcache_test.go`, `overlayfs_test.go`, `snapshotfs_test.go` and bulk-cache tests. Follow with snapshot/session lifetime, config-file changes, default-project selection, references, untitled files, watchers/timeouts and all 39 checker-pool test functions. Preserve table rows and concurrency assertions separately from function counts; reuse `projecttestutil`'s genuine virtual FS/client/npm mocks as explicitly authored host adapters where needed.

ATA is concrete dependency pressure: four source files / 27 functions plus generated types-map data, package validation/discovery, semver and injected `NpmExecutor`. Port discovery/installation/state algorithms; decide real npm/network host and workspace-trust policy explicitly. Tests can use the original injected executor without claiming live package installation. Similarly content-mapper process spawning is gated by `RunExternalCode` and host capability; existing compiler models alone do not implement the external session process host.

Project watcher registration is client-driven via `Client.WatchFiles`/`UnwatchFiles`, so session support does not inherently require command-line build/watch orchestration. The separate `execute/incremental` build-info/solution-builder owners are not substitutes for service snapshots and should not silently expand into this first project-state dependency packet. Root should fix their final epic boundary explicitly, along with actual transport/host support, while keeping the comprehensive language-service contract intact.
