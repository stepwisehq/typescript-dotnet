# LSP protocol and transport investigation

Read-only investigation for the full language-services/LSP epic from `9b8fa74a`, pinned
upstream `fed0bf24149fb1ed36039212648bafdafc1ea10e`. No builds, workload runs or
implementation. Existing compiler, diagnostics, JSON byte adapters, bundled libraries,
content-mapper transforms and disk project support are dependencies to reuse; no C#
LSP/lsproto/JSON-RPC transport, live project Session or snapshot owner was found.
The current `Projects` directory is a compile/check wrapper, not the upstream live
`internal/project` server state.

## Source-defined boundary and order

| Complete source owner | Initial size / direct tests | Required consumer |
| --- | --- | --- |
| [jsonrpc](../../upstream/TypeScript/tsc/internal/jsonrpc/) | 2 files, 287 lines; framing tests live in lsproto | Content-Length byte framing, version, integer/string IDs and errors |
| [lsproto](../../upstream/TypeScript/tsc/internal/lsp/lsproto/) | 6 production files, 18,545 lines; 31 Test functions in 3 files | Typed messages, unions, required/nullability rules, method metadata and URI helpers |
| [lsp](../../upstream/TypeScript/tsc/internal/lsp/) | 5 direct production files, 3,205 lines; 33 Test functions in 15 files | Dynamic queues, progress, logger, stack sanitation and complete Server |
| [lspwatcher](../../upstream/TypeScript/tsc/internal/lsp/lspwatcher/) | 621 lines; 16 Test functions | Fallback file watching over actual fswatch backend |

These are direct `Test…` counts, not table rows or claimed execution. The generated
[protocol file](../../upstream/TypeScript/tsc/internal/lsp/lsproto/lsp_generated.go)
is 17,538 lines and comes from the existing [generator](../../upstream/TypeScript/tools/scripts/lsp/generate.mts).
Port complete protocol representation/codecs before semantic handlers: preserve
absent versus explicit null, object required fields, scalar/array/object union arms,
raw deferred response JSON and exact error classification. The pinned
[structcodec](../../upstream/TypeScript/tsc/internal/lsp/lsproto/structcodec.go)
uses cached reflection metadata for required and nullable tags. Native AOT needs a
complete explicit/generated equivalent or fully rooted closed metadata, not generic
System.Text.Json deserialization that changes these rules. JSON-RPC's current empty
string ID representation is also a source behavior to witness, not silently repair.

The [entrypoint](../../upstream/TypeScript/tsc/cmd/tsc/lsp.go) implements
`--lsp --stdio` only: pipe/socket flags are parsed but rejected without stdio.
Preserve this upstream gap; do not claim pipe/socket transport. SIGINT/SIGTERM,
client-process watchdog, stderr logging, global typings cache, npm installation,
process spawning and API-session IPC are explicit consumer dependencies. Profiling
commands exist upstream but require a separately stated .NET equivalent/boundary;
they must not cause a new profiler campaign during this port.

The authoritative capability boundary is
[initialize](../../upstream/TypeScript/tsc/internal/lsp/server.go#L1537) and the
[handler registry](../../upstream/TypeScript/tsc/internal/lsp/server.go#L1266), not
every method in the generated LSP schema. Registered semantic families include
completion/resolve, hover, definition/source/type definition, references and VS
references, implementation, signature help, rename/file rename, formatting,
diagnostics, symbols/highlights, folding/selection/linked editing, inlay hints,
code actions/lenses, call hierarchy and semantic tokens full/range. Semantic-token
delta is not advertised. Workspace diagnostics are not advertised as a provider.
Preserve custom project-info/API-session/content-mapper/log/debug methods and VS
extensions rather than trimming the server to standard editor requests. Position
encoding defaults to UTF16 and selects UTF8 when offered; actual converters must
retain byte/rune/UTF16 distinctions end to end.

## State, cancellation and client integration

[Read/dispatch/write loops](../../upstream/TypeScript/tsc/internal/lsp/server.go#L895)
separate framing, initialization, response matching, cancellation and ordered
session mutation. Requests before initialization receive ServerNotInitialized.
Cancellation is intercepted by the reader, while queued requests acquire cancellation
state at dispatch; retain that timing rather than promise cancellation of every
queued ID. Requests latch genuine project snapshots before asynchronous semantic
work, with separate pending client/server request maps and correlated responses.
Notifications do not become fabricated request responses. Preserve EOF/shutdown,
queue wakeup/drain and background work disposal contracts.

[Code-lens resolution](../../upstream/TypeScript/tsc/internal/lsp/server.go#L2276)
explicitly guards missing/shortened stale files with ContentModified; the source
also says this does not reject every change. Do not invent blanket stale-result
suppression. Project sessions own document versions/open/change/save/close,
configuration changes, project references and client diagnostics/progress/refresh.
Client dynamic watcher registration has a genuine
[fallback](../../upstream/TypeScript/tsc/internal/lsp/lspwatcher/lspwatcher.go)
with a 75ms batching window and platform fswatch dependencies. Actual Linux watching,
client registration responses and cancellation need acceptance; editor UI, real npm
network installation and external content-mapper binaries are distinct integration
environments to classify explicitly, not replace with silent successful mocks.

## Differential acceptance proposal

Reuse original [LSP client](../../upstream/TypeScript/tsc/internal/testutil/lsptestutil/lspclient.go)
and original test bodies/fixture options. Many server tests skip without embedded
libraries: build the genuine bundled-library Go reference so those do not disappear.
[Replay](../../upstream/TypeScript/tsc/internal/lsp/replay_test.go) is optional unless
an actual replay file/project is supplied; retain its original skip disposition.
Stack sanitation has three checked-in markdown baselines; .NET runtime stack format
needs a qualified adapter rather than an assumed byte-identical Go stack.

Compile the Go transport observer once and publish one Linux x64 Native server;
run bounded full-session transcripts through real redirected stdin/stdout pipes.
First preserve original codec/framing assertions (partial reads, concatenated frames,
malformed headers/JSON, IDs, unknown methods and required/null fields). Then execute
original full server sessions with deterministic VFS fixtures and genuine project/LS
owners. Compare complete correlated responses and client requests/notifications,
including error code/message/data, null/absent fields, versions, UTF8/UTF16 positions,
edits and semantic-token arrays. JSON object member ordering may be structurally
compared; ordered arrays and message dependency order must remain exact. Async
independent responses can match by request ID, but no blanket sorting or dropping
telemetry/progress/watch/cancellation messages. Answer server configuration and watch
registration requests with the same scripted client behavior in both runtimes.

Add few meaningful disk/process transcripts for stdio shutdown/EOF/cancellation,
client watchdog and actual Linux watcher changes; retain original test assertions in
shared xUnit/Native checks. External client/process/network cases need source-grounded
explicit dispositions before acceptance. Root should freeze complete protocol and
live-project ownership paths before integration; no per-feature stub handlers or
repeated per-small-change builds are needed.

## Authorized implementation handoff

Root subsequently authorized this lane to implement complete jsonrpc + lsproto model,
generated structure/codecs and complete lsconv as the coherent low-level dependency
family. Protocol namespace is `TypeScript.Lsp.Protocol`; generic JSON-RPC is
`TypeScript.JsonRpc`; converters/line map are `TypeScript.LanguageServices.Lsconv`.
Foundation owns genuine live project state and is consuming these APIs. The complete
linemap.go counterpart is now source-written at
[LineMap.cs](../../src/TypeScript.Scanner/LanguageServices/Lsconv/LineMap.cs), retaining
its Count(LF)+1 capacity, CR/LF-only line rules, raw-byte decoding and binary search.
No integrated build or acceptance yet. Actual editor features/custom mapping/project
methods stay in scope; root classified custom initializeAPISession embedding IPC and
Go-specific pprof tooling as explicit external boundaries, with unsupported responses
rather than advertised implementations, subject to independent review.
