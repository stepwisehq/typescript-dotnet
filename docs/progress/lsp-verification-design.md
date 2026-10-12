# Language-service verification joins

Coordinator notes, 2026-10-12. This is a design to implement and review, not a
verification result. The complete initial test identity inventory is
[language-services-upstream-tests.json](../language-services-upstream-tests.json).
Query that file by family; it is not necessary to load it into agent context.

## Preserve original assertions without hand-copying the entire harness

The pinned [test client](../../upstream/TypeScript/tsc/internal/testutil/lsptestutil/lspclient.go)
already sends actual Content-Length-framed JSON through pipes. The
[fourslash harness](../../upstream/TypeScript/tsc/internal/fourslash/fourslash.go)
runs original assertions/baselines over these results. This suggests using an additive
Go observer overlay to retain full session inputs and outputs while the original
tests still execute their unchanged assertions. A shared xUnit/Native replay target
can compare full candidate results, not merely counts or selected fields. Preserve
all original expected baselines; recording a currently failing reference test does
not turn it into a passing original test.

Do not choose this route without accounting for the non-wire operations below.
Captured request traffic alone is not a complete reproduction. An alternative is
an external backend adapter for the original harness, but keeping a shadow Go Server
for state observations would falsely attribute its state to C#. Avoid that shortcut.

Before implementing the corpus backend, compare that approach with a direct port of
the original harness and generated test wrappers. The pinned fourslash and
lsptestutil Go files together contain8,358 physical lines; wire capture may not save
effort once its non-wire joins are included. This is an implementation choice still
to resolve after the codecs/transport dependencies, not authorization to build two
harnesses. A candidate-backed original Go harness is also viable only if all actual
state observations come from the C# server rather than a shadow reference session.

Concrete original joins found in source (the direct Server accesses in the
fourslash/client harness are limited to inferred options, InitComplete and the
state-baseline entrypoint; filesystem and process setup remain separate joins):

- Before initialization, the harness calls `SetCompilerOptionsForInferredProjects`
  directly, and later waits on `Server.InitComplete`. Preserve this ordering and
  configuration via the validation host's real server APIs.
- Fixtures use a case-aware in-memory FS, genuine bundled declarations, symlinks,
  dynamic document names and sometimes build-prepared dependencies. Preserve these
  host inputs; do not flatten projects or omit missing files. Build preparation is
  reference fixture preparation, not a claim that our build orchestrator exists.
- Save, file rename and related operations mutate the fixture filesystem in addition
  to sending protocol notifications. Capture/replay those mutations in sequence.
- State-baseline fixtures inspect actual Session snapshots, projects, open files and
  config registries. Preserve their original assertions through a bounded projection
  of those same observations or a direct port; public response agreement alone does
  not satisfy those tests. No extra private-state census is required.
- Content-mapper fixtures inject local test processes/spawners. Their setup is part
  of the case, not a reason to fabricate successful output.
- Server-initiated requests need the same scripted configuration/registration replies.
  Retain notification/error/null distinctions and causal ordering. Normalize only
  specific source-justified runtime values, with explicit accounting; do not drop
  asynchronous traffic wholesale.

Classify C# replay rows as adapted upstream verification with the exact preserved
assertion channels. Direct foundation/project/codec tests belong in shared C# checks
called by xUnit and Native AOT, with original names, cases and provenance. Keep
upstream reference failures/skips, unported assertion channels and absent fixtures
visible per entry. Original function counts, data rows and authored controls differ.

Final transport acceptance additionally runs the actual public `tscheck --lsp --stdio`
process on local disk workspaces: initialization, changing documents/configuration,
watchers, cancellation, refresh, stale-request handling, shutdown/EOF and repeated
edits/requests. A validation-only memory-FS server host may exercise the same production
Server with deterministic inputs, but does not replace the public launch gate.

## Execution and resource policy

Build one pinned bundled-library reference for each coherent observer revision;
reuse it across cases. Capture only required fixture state/results, avoid copying
bundled libraries into every case, and retain compact complete dispositions. Keep
compiler/emit regression acceptance separate from service fixture success.

Use the normal long-lived runtime for server workloads, not the optional compiler
CLI no-GC region. Record cold initialization separately from repeated request/edit
latency, allocation and retained memory. Investigate concrete retention/reuse defects
with focused observations. No external project or exhaustive cache equivalence is
required by this epic.

## Selected corpus backend, pending implementation review

The coordinator selects an additive candidate backend for the original Go fourslash
harness. Preserve its test bodies, assertion workers and expected baselines. Replace
the test-client construction with the real C# production Server over framed stdio,
plus a validation-only host-control connection. No reference Server or Session may
supply candidate observations. This is adapted harness evidence with preserved
assertions, not a claim that all Go test bodies were ported to xUnit.

The concrete source assessment finds three non-wire server joins: inferred options
before initialize, InitComplete, and printStateDiff. Preserve the first two through
the actual candidate APIs. Port the original state-diff projection against the real
C# snapshot: in particular, program/source identity changes drive existing
`*modified*` assertions and may not be replaced by content-only comparisons. Return
its text to the unchanged original baseline comparison. This is a required original
assertion, not an invitation to broaden private-state observation.

Keep the original fixture filesystem authoritative. A validation host forwards its
actual filesystem API (including case behavior, symlinks/realpath, stat, reads and
writes), so fixture mutations and fsDiffer observe the same storage. Preserve the
injected content-mapper Spawner through owned streams and lifecycle/error forwarding.
The host-control connection must remain independent of the LSP reader so synchronous
host calls cannot deadlock request dispatch. It is validation infrastructure, never
an advertised editor capability. Keep exact bundled declarations and prebuilt fixture
inputs; do not flatten paths or replace the host with a shadow project.

This choice avoids translating thousands of test bodies and the entire 8,358-line
Go harness while retaining their intended observations. It still requires independent
review of every adapted join and full test dispositions. Direct subsystem assertions
continue in shared C# checks called by xUnit and Native AOT. The actual public
disk-backed server gate remains separate: remote-host corpus timings do not establish
ordinary server responsiveness. No backend has been implemented or executed yet.
