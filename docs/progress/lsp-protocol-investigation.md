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

Protocol implementation in progress: the reproducible [generator](../../tools/lsp/generate-protocol.py) now extracts all728 pinned Go type declarations and their field/tag/source-line metadata. Alias targets resolve to their actual type rather than inventing runtime owners. Position/Range and resolved capability snapshots retain value layout; recursive protocol DTOs use managed reference owners, with required embedded zero owners initialized explicitly. The complete codec/union/capability method family remains in progress; declaration presence is not codec acceptance. Raw deferred JSON owns copied immutable bytes, never retained JsonElement or pooled-buffer views. Framing original4 function bodies are shared in xUnit-compatible checks; no build or execution yet.

Generated implementation now also includes all79 nil-aware capability resolvers,
all71 document/location/comparison projections, and all30 numeric enum name
functions. The generator rejects unrecognized source shapes rather than emitting
fallback bodies. URI conversion uses the already accepted complete dynamic-path
owner; no TsPath replacement was needed. The selected file-URI dependency carries
raw Host/Path bytes and preserves Go escape/authority validation, using standard
IPAddress only for the required IPv6 validation boundary. Codec generation and
original protocol/converter assertion completion remain active; no compile or
Native acceptance has been claimed.

The complete converter production owner is now source-written in
[Converters.cs](../../src/TypeScript.Scanner/LanguageServices/Lsconv/Converters.cs) and
[Diagnostics.cs](../../src/TypeScript.Scanner/LanguageServices/Lsconv/Diagnostics.cs).
It includes all projection, original/virtual mapping, URI, byte/UTF16 coordinate and
pull/push diagnostic functions from converters.go. The existing span-map List result
owners are consumed directly; source projections and final outputs retain the exact
Go Slice capacities. Editor coordinates retain the upstream uint32-to-int32 conversion before clamping
against managed buffer positions. Original assertion translation and independent
review remain pending. Root's second diagnostic compile had no protocol declaration
errors; the complete tree still had query joins and has not passed acceptance.

Retained codec design is durable at
[codec-design.md](../../artifacts/language-services/drafts/codec-design.md); the actual
source generator remains under tools/lsp. No codec draft exists only in /tmp.
Generation stages all source analysis before atomically replacing each output,
with integration builds coordinated around the complete output pass. Complete codecs
remain active work after this project-consumer dependency; no fallback codec stubs
or temporarily excluded production files have been introduced.

All6 original converter test functions are now source-written in
[ConverterChecks.cs](../../validation/LanguageServices.Checks/ConverterChecks.cs),
including the original real Node reference script, all14 valid-UTF8 inputs and
binary length-prefixed transport. The original invalid-UTF8 roundtrip remains a
separate test, as upstream. URI source tables and supplemental projection identity
assertions are retained. Tests and Native registration remain unexecuted until the
coordinated integration gate. The focused accepted JSON quote dependency now
accepts ByteStringValue directly with its original rune/escape worker, removing the
protocol Marshal UTF16 boundary and preserving literal Unicode such as U+2028.

LSUtil's concrete JSON dependencies now use direct Dictionary object assembly in
both the fast parser and the existing source-defined partial/error decoder, without
copying an OrderedMap object tree. ParseDictionary admits only object/null roots,
matching json/v2's actual typed map token admission; ordinary Parse stays unchanged.
MarshalStringDeterministic sorts actual map fields by raw Go string names and leaves
declared struct field order alone. Six separately authored adapter controls cover
Unicode/invalid-byte quotation, strict duplicate/UTF8/surrogate decoding, owned
decoded/deferred payloads, partial mutable map ownership and nested deterministic
ordering. They are unexecuted and are not original test functions.

Closed codec recovery files are now durable under artifacts/language-services/drafts:
ProtocolDecoder.cs, codec-methods.json, generate-codecs.py and plain-readers.cs. The
source inventory distinguishes390 plain object readers,110 custom readers and7
streaming discriminator readers; there are97 pointer-union writers plus Registration
and19 value literal writers (117 total). All507 original readers remain required.
The390 generated plain readers are only staged draft bodies: runtime field/error,
closed typed dispatch, custom union and writer materialization is incomplete.
Do not claim codec completion from this staged source count. Preserve lazy per-type
specification ownership and avoid a runtime global dispatcher that forces every
unused params codec into the Native binary; deferred params are an actual upstream
linkage/ownership behavior, not merely a wire representation choice.

Project checker-pool request affinity requires the complete four-function
[Core context counterpart](../../src/TypeScript.Scanner/Core/Context.cs), mapped to
internal/core/context.go. The explicit immutable protocol context now retains a
borrowed RequestID byte header and the Temporary/Diagnostics/API lifetime enum.
All cancellation, locale and capability derivations preserve these unrelated values;
request-ID and lifetime derivations share the immutable capability owner. Defaults
remain empty ID and Temporary. This is the actual project consumer dependency,
not ambient request state or a compiler-pool ownership change. Source-written;
independent review and integration execution remain pending.

The retained codec draft now has closed field-shape readers in shape-readers.py /
shape-readers.cs, used directly by all390 plain and43 simple-kind union readers.
They preserve reused slice capacity, partial failing element headers, mutable map
updates, pointer admission and exact fixed-array length checks without a global
runtime type registry. Raw ReadValue now borrows the decoder input; its explicit
Clone operation creates the required retained deferred owner. This is still draft
source: remaining custom unions, semantic-error routing and complete writers are
not yet materialized in production, and no codec completion is claimed.

The closed draft generator now accounts for all507 source readers:390 strict plain
objects,110 custom readers (43 simple-kind,8 raw-key,12 bool/raw-key,4 direct trial,
23 additional kind-switch,19 literal,1 registration) and7 streaming discriminators.
It also emits92 default Go struct readers, ordinary typed struct writers,97 exact
marshalUnion writers, registration and19 literal writers. These remain durable
ignored drafts, not compiled implementation or accepted evidence. Discriminator
fields before the kind retain explicit value.Clone owners; the kind itself and
ordinary raw classification borrow the immutable decoder frame. Trial arms retain
source order and assign only after full success. Remaining work is complete semantic
error routing, closed generic request/message entry points, assembly/materialization,
original assertion ports and independent/integrated verification.

Generated nullable scalar/value headers preserve presence, zero values and partial
read state. They are value snapshots rather than a public mutable Go-pointer alias
API. The actual protocol consumers and original reused-union assertions must be
checked before this adaptation is accepted; object pointers retain their class
identity and Slice/map headers preserve backing owners. Do not replace these owners
based only on an unobserved arbitrary private alias mutation, or claim full pointer
identity from nullable headers.

Root's source review identified an observable coordinate arithmetic omission:
`core.TextPos` is signed int32, so the pinned converter casts protocol uint32
coordinates before its range test and performs int32 addition before clamping.
The port now retains those casts and unchecked additions, including invalid negative
line indexing rather than introducing a new clamp. Four separately authored shared
controls cover ASCII overflow and non-ASCII UTF8/UTF16 negative signed characters;
the durable Go witness is
[converter_boundaries_test.go](../../tools/language-services-oracle/converter_boundaries_test.go).
Root's additive pinned-Go witness passes all four cases (converter-boundaries-go.log);
foundation's narrow source review is clear. Integrated managed/Native execution of
these new controls remains pending. The six original converter functions remain intact.


The durable decoder draft now retains Go numeric syntax/range distinctions via the
actual jsonwire.ParseUint algorithm, complete-value consumption before numeric/string
wrong-kind errors, token-only consumption for boolean wrong-kind errors, and the
float64 partial-result write before range failure. These are draft semantics, not
accepted codec evidence: streaming SemanticError type/offset/pointer enrichment and
closed public read/write entrypoints still need completion before materialization.

The initial root-managed and actual Linux x64 Native dependency gate now passes the
six original converter functions and all four authored signed-coordinate boundaries;
this evidence covers the saved converter arithmetic correction. Full converter owner
review remains separate from that focused correction. Closed codec work continues in
the ignored durable draft directory, with complete599 struct reader/writer inventory
assembly, explicit root-shape descriptors and streaming semantic error metadata.
These drafts are not production codec completion or server transport acceptance.

The closed entrypoint draft now includes all104 lazy method-info descriptors, typed
request/notification construction, all three generic JSON-RPC message owners, and
lsproto Message/RequestMessage/ResponseMessage. Source-specific behavior is retained:
custom request decode is transactional except absent Params retains an old payload;
generic decoding mutates existing fields; result failures discard partial values,
while parameter failures return partial values. Deferred JSON clones survive decoder
lifetime, and nil jsontext.Value boxed in a response result remains present JSON null.
Generated model/scalar read facades preserve preset-any types without reflection.
Recovery entrypoints remain `artifacts/language-services/drafts/codec-design.md`,
`root-descriptors.py`, `generate-codecs.py`, `assemble-codecs.py`, and `model-facades.py`.
Before atomic production promotion, finish typed map/slice cycle identities and
byte-method error closure, reconcile complete root error type strings, then port the
remaining27 protocol/URI test functions. The original31 package total includes the
four framing functions already covered; do not count those twice. No codec builds
or behavioral runs have occurred for these drafts.

## Closed codec promotion (current; acceptance pending)

The complete codec family is now promoted under `src/TypeScript.Scanner/Lsp/Protocol`
with the genuine generic JSON-RPC owners under `src/TypeScript.Scanner/JsonRpc`.
The preceding draft-only paragraphs are historical. All599 generated readers and
599 writers,749 distinct lazy root descriptors and104 method-info owners are now
present. Pointer/value roots remain distinct even when both use one CLR class.
Default-any mutation, transactional custom messages, typed nil payloads, deferred
owned raw JSON, duplicate decoded names, strict UTF8 decoding and replacement
encoding, depth admission and source slice/map cycle identities are retained.
The JsonRpc ID byte-method adapter adds/collapses semantic offsets and pointers;
non-semantic failures retain the original outer typed error context.

Reproduce generated outputs with `python3 tools/lsp/generate-codecs.py`. Tracked
modules in `tools/lsp/codecs` extract the actual pinned methods afresh and assemble
complete outputs in ignored `artifacts/language-services/codec-generation`;
intermediate fragments never enter compilation. Final files are the assembled
ProtocolCodec, RootDescriptors, MethodInfos, ModelFacades, IntrinsicValues,
IntrinsicReaders and DynamicTypes `.Generated.cs` files. The shared generated
assertion tables/equality are also produced there. Format final C# with the existing
CSharpFormat tool. No retained ignored method inventory is required to regenerate.

The shared original corpus now exposes27 named functions, excluding the separately
covered four framing functions. Additional authored controls expose seven JSON
observations and eight admitted recursive depths. Root executed the full original
Go package (31 functions/122 subtests), the JSON witness and the depth witness.
Candidate codec build/managed/Native acceptance remains pending. Native depth
entrypoints are `ProtocolCodecChecks.RunModelDepthBoundary` (16/256/2048/4998) and
`RunAnyDepthBoundary` (16/256/2048/10000); isolate each actual process to classify a
stack failure without losing other evidence. No smaller admission limit is assumed.
Production and shared test sources are frozen for root integration build17.

The105 Method constants and104 MethodInfo descriptors are different original
inventories, not a missing codec: pinned `lsp_generated.go:11188` additionally
Declares `MethodTextDocumentSemanticTokens` (`textDocument/semanticTokens`) without
RequestInfo/NotificationInfo. The concrete full/delta/range requests have their own
infos; the generic semantic-token method constant remains available exactly as
upstream defines it. No descriptor is invented for the declaration-only method.

The first central codec body compilation exposed actual C# declaration joins. The
two value-union readers are emitted by `kind-unions.py`; their final generated
bodies now avoid reference-only `??=` initialization. New map-only object namespaces
preserve json/v2 duplicate-name rejection after invalid-UTF8 replacement. Generated
struct fields retain disabled namespaces; ordinary valid map keys keep borrowed
headers. Three separately authored map observations are source-written and require
pinned Go/candidate execution, separately from the original27 and prior JSON7/depth8.

## Current coherent codec gate and next server support (2026-10-12)

The complete closed codec family now compiles:599 readers/writers,749 root shapes
and104 method infos. All27 remaining original protocol function bodies and separate
JSON7/map3/zero5 authored controls passed the coordinator's managed run; the four
framing functions are counted separately. The full original pinned Go protocol
package passed31 functions/122 subtests, and separate Go depth/JSON/map/zero witnesses
retain their exact admissions/errors. Actual Native23 dependency publication passed, including all eight isolated
recursive-depth cases through model4998/default-any10000. No stack adaptation is
justified. This does not imply server/transport completion; full codec independent
source review is still closing.
Tracked generation is [generate-codecs.py](../../tools/lsp/generate-codecs.py) with
its [closed templates](../../tools/lsp/codecs/); historical ignored draft fragments
are superseded and must not overwrite those sources. Custom value-root writers now
initialize their Go zero owner while pointer-root null exits stay separate. The
original optional discriminator test owner's Title RejectNull flag was corrected
after that publication (true to false for its nonnilable Go string); initial
Native23 binds the prior test-only metadata and next gate will include the corrected
flag. Original non-null input/assertions and production behavior are unchanged.

Full independent formatter source/assertion review is clear; see
[lsp-formatting-review.md](./lsp-formatting-review.md). Lowest server support drafts
are durable under `artifacts/language-services/drafts/server`:DynamicQueue.cs and
DynamicQueueChecks.cs. They preserve the sole idle/ready state owner, original Slice
pop/zero/reset and all three original functions. A synchronous condition represents
channel-state admission without racing two reads that can orphan-consume state;
cancellation wakes admission and does not revoke an admitted owner. This is a draft
.NET adaptation awaiting independent review and integrated execution, not a new
queue algorithm or stronger cancellation guarantee. Progress retains a64-event
buffer, one ordered mutable worker, delayed timer and refcounted ordered messages;
its original tests use Go synctest, so their deterministic time/wait adapter must be
explicit when that next complete owner is materialized.

The direct upstream-to-destination mapping for this complete dependency owner is
[lsp-protocol-mapping.json](./lsp-protocol-mapping.json). It records one-to-many
handwritten/generated counterparts, original source hashes, explicit authored
closed-codec support and required partial-file dependency joins. This complements
the source-hash review packet and lets the coordinator merge discoverable entries
into the primary manifest without marking unrelated partial runtime files complete.
