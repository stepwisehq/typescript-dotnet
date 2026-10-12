# Shared editor dependencies: checkpoint33

The language-services epic continues. This checkpoint completes the reviewed
change-tracking, content-mapper process-host, diagnostic-localization, IPC and
auto-import source dependencies and verifies their currently executable original
assertions. It does **not** establish a working editor server or complete service
behavior. Five original auto-import registry tests still require the real Session.

The pinned upstream revision is unchanged. Direct source mappings are in
[source-map](../../source-map.json); the per-owner mapping packets preserve exact
scope, source hashes and necessary adaptations. Existing Program reuse, borrowed
UTF8 text, required Go copies and accepted compiler runtime policies are retained.

## Evidence

[Execution record](../../validation/results/language-services-dependencies-33.json):

- Release build:19.23s, zero warnings/errors;212 ordinary xUnit cases passed,
  zero failures/skips. Eight unchanged isolated decoder cases retain prior evidence.
- Actual Linux x64 Native AOT publication and execution passed137 original
  TypeScript functions,11 original external locale functions, retained regexp/NFD
  channels and44 authored controls. Functions, channels, corpus rows and xUnit
  records are distinct units; the proof and output retain those distinctions.
- The isolated native notification-panic control terminated with the required
  marker and nonzero status without timing out. Core dumps were disabled.
- Original Go reference suites passed under the same pin. The115 fswatch and16
  lspwatcher original functions establish reference evidence only: their C# owner
  is still being implemented and receives no candidate pass credit.

Independent source reviews cover the changed owners and their original assertions.
The current result does not rerun or replace final retained compiler/emit regression,
real-transport, changing-project or repeated-request responsiveness acceptance.
[Reproduction](../LANGUAGE-SERVICES-VERIFY.md) documents the shared managed/native gate.

## Issues resolved without weakening assertions

The previous combined managed run passed195 of197 cases. One locale test incorrectly
compared an internal script ID instead of the original public spelling; the source
lookup maps zero to `Zzzz`. The original spelling and confidence assertions now pass.
The other failure came from the adapted in-process pipe: cancellation of a task
proxy raced an already-consumed write acknowledgement. It now preserves that exact
acknowledged handoff as Go's pipe does. Production locale inference and AsyncConn
algorithms were unchanged. Both fixes received independent review and passed this
managed and native gate.

Compiler joins exposed namespace, borrowed-buffer and nullable-owner differences.
These were reconciled at the actual owners, with no empty registry, nullable list
fallback, rewritten expectation or fixture-specific production branch. Frozen source
and central integration prevented agents from invalidating publication in progress.
Future packets should continue reconciling shared API signatures before promotion;
this packet still needed multiple central compile attempts.

## Continuing work

The next concurrent owners are genuine Snapshot/Session lifecycle and original
registry/pool assertions; shared semantic utilities, documentation, hover and
signature help; and complete Linux watcher/server lifecycle dependencies. They are
being assembled outside compilation until each complete dependency packet is ready.
The original fourslash harness will run against the actual candidate server with a
separate validation host-control connection, preserving its filesystem and state
assertions rather than borrowing a Go Session as the candidate's state.

No new token, cost or whole-epic duration measurement is available. Build durations,
recorded gate results and source-backed dispositions are the available observations.
