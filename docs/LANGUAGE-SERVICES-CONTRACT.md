# Language services and LSP completion contract

Active epic authorized 2026-10-12. Implementation baseline: accepted emit and
performance `9b8fa74a`; its human-readable summary is preserved at `e003f22f`.
Reference: `fed0bf24149fb1ed36039212648bafdafc1ea10e` in
[pinned TypeScript](../upstream/TypeScript/tsc/). Completion means the implemented
upstream editor subsystem described here, not every feature in the LSP schema or
every historical JavaScript TypeScript service. The first shared-dependency execution gate has passed; complete subsystem and epic acceptance remain pending.

## Capability and ownership

Complete these upstream responsibilities, including their supporting algorithms:

1. **Protocol and text/query foundations.** JSON-RPC framing and errors, generated
   protocol types/codecs, URI and negotiated UTF-8/UTF-16 positions, AST navigation,
   preferences, checker service/export queries and configured hover serialization.
   Preserve required/absent/null and ordered union behavior under Native AOT.
2. **Project and document lifetime.** Genuine editor project/session owners,
   immutable snapshots, overlays, unsaved and incremental text changes, parse/config
   cache ownership, configured/inferred/synthetic projects, project references,
   selective Program reuse and replacement, checker leases, file watches, debounce,
   background cancellation, snapshot release and shutdown. Use accepted Program
   mechanisms, not repeated full compilation to imitate missing updates.
3. **Shared semantic/edit dependencies.** Complete formatting, change tracking,
   auto-import indexing/preparation/invalidation, module-specifier and source-map
   consumers, symbol display and documentation. Preserve genuine libraries and
   configuration, including supplemental/content-mapped source relationships.
4. **Language-service consumers.** Diagnostics and refresh; completion/resolve;
   hover and verbosity; definition/source/type definition; references and
   implementations across projects; rename/prepare/file rename; signature help;
   symbols/highlights; folding/selection/linked editing; full/range semantic tokens;
   inlay hints; formatting/on-type; automatic insertion and JSDoc snippets; call
   hierarchy; code lenses/resolve; organize imports and the upstream registered
   code-action/fix-all families. Preserve both successful and failure behavior.
5. **Actual server.** Planned public launch is `tscheck --lsp --stdio`, dispatched
   before the existing one-shot compilation policy. This command is not implemented
   yet. Initialization and accurate capability negotiation, ordered
   document mutations, concurrent snapshot-backed requests, cancellation, correlated
   client requests, registration/progress/logging, stale-result rules, shutdown/EOF,
   signal and parent-process lifecycle, and real stdio transport. Editor-facing
   custom project-info, source-definition, VS and content-mapper operations belong
   to this surface. Generated but unregistered LSP methods do not become advertised.

Source boundaries are [project](../upstream/TypeScript/tsc/internal/project/),
[ls](../upstream/TypeScript/tsc/internal/ls/),
[lsp](../upstream/TypeScript/tsc/internal/lsp/),
[jsonrpc](../upstream/TypeScript/tsc/internal/jsonrpc/),
[astnav](../upstream/TypeScript/tsc/internal/astnav/),
[format](../upstream/TypeScript/tsc/internal/format/), the service-facing checker
files, required host/dependency extensions and the
[server entrypoint](../upstream/TypeScript/tsc/cmd/tsc/lsp.go).
Direct Go-to-C# mappings and meaningful adaptations belong in
[source-map.json](../source-map.json). A source inventory is not a port or test claim.

## External boundaries and upstream limitations

- Linux x64 Native AOT is the required verified platform. Other runtime/platform
  support is unverified unless exercised. Internal Go byte positions and malformed
  UTF-8 behavior remain distinct from client UTF-16 coordinates.
- Upstream implements stdio; its parsed socket/pipe flags do not implement alternate
  LSP transports. Do not invent transport parity or advertise nonexistent methods.
- Include client and fallback filesystem watching needed by sessions. The separate
  command-line solution builder, incremental build-info orchestration and build/watch
  command remain outside scope; they are not the editor snapshot implementation.
  Complete the shared `lspwatcher`/`fswatch` algorithms and required Linux inotify,
  fanotify and filesystem-specific fallback transport. The pinned Linux server disables
  fallback initialization when the client lacks dynamic registration because Linux
  backends do not report fast recursive support; preserve that rule without omitting
  the separately exercised watcher implementation. Native Windows/macOS/BSD backends
  are outside this Linux implementation boundary, not merely claimed unverified.
- Include ATA package discovery, installation/state algorithms and the injected npm
  process host. Verify deterministic upstream executor cases and local host behavior;
  live registry availability and the contents of arbitrary downloaded packages are
  external environments, not reasons for silent successful stubs.
- Include the workspace-trust-gated content-mapper host and session integration.
  Exercise deterministic local processes; arbitrary third-party mapper correctness
  is external. Preserve rejection/failure when execution is unavailable or untrusted.
- The custom separate embedding-API session/IPC subsystem and Go pprof debug commands
  are external to editor services. Do not advertise them or fake success. Explicit
  unsupported-method behavior and the original tests affected by this boundary must
  be visible. This boundary does not exclude editor project-info or mapping requests.
- Preserve concrete upstream gaps, including limited registered code actions, no
  semantic-token delta provider, and documented reference/symlink TODOs. Inventory
  affected tests/paths; do not convert an upstream limitation into a new port omission.

No empty responses, permissive placeholders, simplified library declarations or
fixture-specific algorithms qualify. Necessary changes to established compiler
foundations require a concrete dependency or defect and retained regression coverage.
Preserve UTF-8 borrowing, Go slice ownership/copies, callback reuse and accepted
runtime adaptations. The optional one-shot CLI no-GC policy is not a server default.

Concrete supporting dependencies identified during implementation: auto-import
exclusion patterns require Go `regexp.Compile`/`MatchString` semantics, including
syntax rejection and the selected matching workers; import sorting requires the
pinned `x/text` NFD normalization workers/tables. The actual caller removes Mn
characters after NFD, but stream-safe boundaries can still change the order of
retained Mc characters. A plain .NET Regex/FormD substitution is therefore not
assumed equivalent. Port these required dependency closures with exact source pins,
licenses, direct mappings and applicable original assertions. The server stack
sanitizer additionally requires original capture replacement and
index observations; complete that concrete closure before its consumer. Unused
standard-library find-all APIs remain outside the caller closure. This dependency decision
does not authorize an unrelated general-purpose library project. Source-grounded
compatibility findings still require behavioral verification.

Locale-aware server diagnostics also require the genuine diagnostic lookup/cache,
13 pinned message catalogs, and the missing pinned `x/text` language matcher and
likely-subtag/canonicalization workers. Reuse the existing locale parser; do not
substitute .NET culture selection or silently return English for supported locales.
The original Go-source localization generator is a tooling-test disposition, distinct
from runtime catalog assertions and actual Native AOT resource-loading verification.

## Acceptance

Implementation completeness and verification completeness are separate gates:

- Account for production files/functions, generated structures and required helpers
  across every responsibility above. Explicitly track missing and partial bodies,
  remaining adaptations and implemented-but-unexercised behavior. Independent Sol
  review challenges omissions, original algorithm fidelity and completion claims.
- Inventory applicable direct tests and the complete pinned fourslash suite. Port
  executable original assertions to xUnit/shared Native checks; preserve fixture
  options, markers, changes, order, baselines, negative cases and original skips.
  Distinguish original assertions, documented adaptations, reused inputs and new
  comparisons. Count test entries and data rows separately. A fixture loaded or
  request completed is not its original test passing.
- Compare meaningful results against bundled-library pinned Go with equivalent
  workspace, preferences, source and client state. Include complete diagnostics,
  source edits and their applied outcomes, response/error/null distinctions,
  UTF-8/UTF-16 ranges and semantic relationships. Object property order may be
  normalized; array order and causally ordered messages may not be discarded.
- Execute real redirected-stdio sessions through the Linux x64 Native AOT server,
  including unsaved changes, repeated edits, project/config/dependency changes,
  cancellation, registration, refresh, stale-code-lens behavior and shutdown.
  Preserve upstream cancellation timing; do not invent stronger guarantees.
- Measure repeated-edit/request latency and retained memory on reproducible local
  workspaces; distinguish warm reuse from first load and account for snapshot
  release. Use focused ownership observation only for concrete correctness/resource
  risks, not exhaustive private-state equality.
- Retain applicable compiler, emit and performance protection, with comprehensive
  epic acceptance after coherent integration. External projects are not required;
  their evaluations remain deferred by the new mandate. Preserve historical scoped
  performance evidence without implying an unmeasured universal/server speed claim.

At completion deliver reviewed commits, clean worktree, server launch/editor
connection instructions, reproducible acceptance commands, complete test dispositions,
updated mappings, implementation/evidence limitations and observed effort. Do not
narrow this contract to claim success or stop at internal milestones.

## Initial test inventory (not execution)

A source scan separates actual `Test*` entries from `TestMain` package lifecycle:

| Source family (recursive) | Test entries | Lifecycle functions |
| --- | ---: | ---: |
| project | 107 | 2 |
| ls | 41 | 1 |
| lsp | 79 | 1 |
| format | 7 | 0 |
| astnav | 5 | 1 |
| modulespecifiers | 10 | 0 |
| fourslash | 4,571 | 1 |

The [per-file/function inventory](./language-services-upstream-tests.json) records
4,442 source test files, their hashes and 4,820 test entrypoints. One concrete core
BFS dependency was added afterward. The inventory records226 original TypeScript-repository functions passed at the
accepted dependency39 gate; remaining execution and detailed
dispositions are still work.
These are function identities, not table-row, assertion or pass counts. The original
investigations also report raw `Test`-prefixed declaration counts; those include
lifecycle functions and must not be used as executed-test totals. Dependency tests
outside these packages will be added when the corresponding concrete joins are
identified. Per-test dispositions and original assertions remain acceptance work.

## Dependency order and current status

| Internal milestone | Complete owners before consumers | Status |
| --- | --- | --- |
| Shared foundations | Protocol/codecs/converters; checker queries/hover; AST navigation; project ownership utilities | In progress |
| Reusable project environment | Overlays, snapshots, caches, graph/pool, session lifecycle, preferences, hosts | Complete production counterparts promoted and source-reviewed through Session/Snapshot; original Session/pool assertions and integrated execution in progress |
| Shared editing and indexing | Formatting, change tracking, autoimport, display/source maps | Formatting/change/autoimport dependencies scoped-verified at f5feaa6a; display/source-map/semantic consumers promoted, verification pending |
| Semantic services | All listed upstream request algorithms and cross-project interactions | In progress: full hover/signature/definition/source-definition counterparts compile and are source-reviewed; direct formatting assertions pass managed/Native, service comparisons remain pending; other consumers unfinished |
| Server and full acceptance | Real dispatch/transport, direct/fourslash comparisons, Native AOT, review and regressions | Missing |

Concrete implementation dependencies refine this sequence: formatting precedes
change tracking, which precedes full autoimport support. Real autoimport and the
LanguageService core must be available before Snapshot/Session consumers can finish.
Their work therefore proceeds alongside the reusable project environment rather
than waiting for a falsely complete project milestone.

The [project investigation](./progress/lsp-project-investigation.md),
[service investigation](./progress/lsp-services-investigation.md) and
[protocol investigation](./progress/lsp-protocol-investigation.md) record initial
source-grounded dependencies. They are investigation evidence, not completion.
Root coordinates integration builds and shared APIs; GPT-6.1 Sol owns implementation
and independent review. Reuse agents across coherent source owners, avoid overlapping
edits and per-small-change comprehensive rebuilds. Keep resumable state in
[STATE.md](./STATE.md); a checkpoint does not require renewed authorization.
