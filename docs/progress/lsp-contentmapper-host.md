# Content mapper process host and Session dependency

This lane now owns complete pinned `internal/contentmapper/hostimpl.go` (1374 lines)
as a genuine trust-gated Session dependency. Existing transform/span-map/hash/result
owners remain reused. The complete source and all28 original shared assertions are now promoted for
independent review and central integration. They have not compiled or executed;
this is not yet a host acceptance claim. Ignored construction drafts are retained
only as historical staging, superseded by the consolidated production counterpart. Snapshot/Session drafts remain preserved while
this necessary dependency is assembled.

The protocol lane owns the actual IPC asynchronous connection, JSONRPC protocol and
timing closure. Agreed interfaces use duplex `Stream`, explicit `CancellationToken`,
borrowed method names, retained raw JSON response owners and closed typed payloads.
Spawner reads child stdout, writes child stdin and tears down the process when
closed; stderr has its own raw-byte writer. Optional process-exit state and original
five-second initialize timeout remain distinct error paths. No embedding endpoint
or unrelated named-pipe transport is included.

Outgoing initialize/openProject/closeProject/transform params are Go value roots.
They will cross actual IPC any slots through existing `ProtocolPayload<T>` with
closed content-mapper descriptors, retaining pointer/value distinction where tests
actually use pointers. No reflection or default .NET JSON substitute is planned.
Timing workers preserve inline atomic operation counters, required detached map
snapshots, cumulative active-request wait accounting and signed nanoseconds.

[Exact original test identities](./lsp-contentmapper-host-mapping.json) list all28
host test functions, pending this real owner and IPC closure. Accepted transform
coverage remains separate. Root centralizes the eventual original Go reference,
managed integration and actual Native execution. No builds or workloads have been
run by this lane for this new family.

The current outside-compilation assembly also contains closed codecs for the
actual wire DTO fields and compact diagnostic-directive tuples; raw stderr buffering,
logging protocol and once-only process-close owners; host cancellation/locale/reset
lifecycle and identity retain/release; production spawn/initialize handshake; and
UTF8/UTF16 mapping/directive/diagnostic normalization workers. The genuine IPC
closure is now promoted and independently source-reviewed, with combined managed
build25 assertions passed. All58 original host function bodies are now drafted,
including real project leases, handle refresh/close, dynamic identity and watched-file
validation. All28 original host assertions and their shared in-process mapper fixtures are
source-written and promoted, with managed/Native execution still pending. Existing span-map decoding can consume the retained raw bytes
directly, so no additional decode overload is needed.

Logging wraps an `io.ReadWriteCloser` interface in the source, so optional process
exit-state methods are not promoted through the logging wrapper. The draft preserves
that distinction rather than silently improving its initialization-error classification.

Closed DTO admission was reconciled with the pinned JSON/v2 struct and raw-value
workers: JSON null resets a value-root struct; pointer roots remain null; raw
`omitempty` observes encoded null, empty strings, arrays and objects. The raw-value
formatter reuses the accepted JSON compatibility owner and its invalid-UTF8
admission. The protocol lane supplied a focused internal read-only-use projection
of retained `RawJsonValue` bytes, allowing the same owner to feed formatting without
an invented clone. This small dependency belongs to the eventual host review packet.

The focused raw-owner review confirms that `FromRetainedBytes` shares only an
already-owned outgoing buffer and marks allocated-empty as non-nil, whereas the
existing span constructor still performs the decoder-required copy.
`BorrowOwnedBytes` is an internal read-only-use projection; the actual formatting
consumer reads the input and creates a distinct output owner. Both adapters are
part of the forthcoming host packet, not a claim that the new host has executed.
The host draft also disposes linked cancellation registrations after cancellation
while retaining its token for repeated close and existing lease error paths.

Build26 stopped before method bodies on wire/normalized diagnostic namespace collisions.
The new wire structs now use distinct `WireDiagnostic` and
`WireMappedDiagnosticDirective` names. Their original JSON descriptor names and
normalized Scanner result APIs are unchanged; existing compiler/options consumers
need no extra aliases. This declaration-only correction is saved for the next
central integration build, with no candidate execution claim.
