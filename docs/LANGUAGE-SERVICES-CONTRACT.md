# Language services and LSP completion contract

Active epic authorized 2026-10-12. Implementation baseline: accepted emit and
performance `9b8fa74a`; its human-readable summary is preserved at `e003f22f`.
Reference: `fed0bf24149fb1ed36039212648bafdafc1ea10e` in
[pinned TypeScript](../upstream/TypeScript/tsc/). Completion means the implemented
upstream editor subsystem described here, not every feature in the LSP schema or
every historical JavaScript TypeScript service. No implementation is accepted yet.

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
5. **Actual server.** Initialization and accurate capability negotiation, ordered
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

## Dependency order and current status

| Internal milestone | Complete owners before consumers | Status |
| --- | --- | --- |
| Shared foundations | Protocol/codecs/converters; checker queries/hover; AST navigation; project ownership utilities | In progress |
| Reusable project environment | Overlays, snapshots, caches, graph/pool, session lifecycle, preferences, hosts | Missing |
| Shared editing and indexing | Formatting, change tracking, autoimport, display/source maps | Missing |
| Semantic services | All listed upstream request algorithms and cross-project interactions | Missing |
| Server and full acceptance | Real dispatch/transport, direct/fourslash comparisons, Native AOT, review and regressions | Missing |

The [project investigation](./progress/lsp-project-investigation.md),
[service investigation](./progress/lsp-services-investigation.md) and
[protocol investigation](./progress/lsp-protocol-investigation.md) record initial
source-grounded dependencies. They are investigation evidence, not completion.
Root coordinates integration builds and shared APIs; GPT-6.1 Sol owns implementation
and independent review. Reuse agents across coherent source owners, avoid overlapping
edits and per-small-change comprehensive rebuilds. Keep resumable state in
[STATE.md](./STATE.md); a checkpoint does not require renewed authorization.
