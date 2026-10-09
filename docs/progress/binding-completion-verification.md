# Binding completion verification checkpoint

Baseline `2886d47`; upstream `fed0bf24149fb1ed36039212648bafdafc1ea10e`.
Final guarded native/regression gate passed and durable proof recorded. All executable source, tools and corpus assets are frozen; no remaining verification work.
No unchanged-baseline rerun. Historical fixture/proof bytes remain unchanged.

- Independent cold Go oracle builds using retained reflection observers inside the
  pinned module; no tracked upstream source edits. New C# static serializer uses
  the same graph protocol without reflection serialization.
- Actual observation order: separate unbound parse baseline; untouched primary parse;
  bind; syntax/bound graph snapshot (including syntax getter observations); repeat bind
  and concrete identity/full-state byte equality; symbol/container API observations;
  optional explicit lazy JSDoc. Warm controls intentionally run original parser helpers
  before binding and are labeled separately.
- One broad managed triage: 17,722/17,723 exact, 253.866 s. Case10003 retained as a
  harness failure: malformed private destructuring uses lexical class as generating
  name owner, while Symbol.Parent is the object literal. Completion-only strict
  lexical-owner/signed preimage validation repaired; Parent still independently compared.
- Final native broad totals: 416,118 symbols, 147,932 tables, 286,218 flows, 46,565 FlowLists;
  28 allocated-empty tables, four shared tails, zero distinct shared table slots.
  All27 reachable binder diagnostic codes and all13 atomic FlowFlags observed.
- Full metadata null-state extension exposed real Scanner.CommentDirectives absent
  slice incorrectly represented as allocated empty. Scanner owner repaired nullable
  backing/getter/SetText/Reset/Mark/Rewind. Legacy Scanner.Validation preserves []
  projection with ?? []; parser forwards actual nil. No expected behavior relaxed.
- Final focused richer26/26 exact; shared eight support plus seven helper/lifecycle/
  observer-sensitivity groups all pass current managed Release snapshot. Counts include
  four exported binder APIs, 15 raw signed-ID rows, 12 strict-prologue rows, nine
  declaration sequences, three mode/container DFS sources, 16 directive lifecycle stages.
- New observer sensitivity graph intentionally seeds shared table slots, shared linked
  tails and a cycle; it is authored comparator/encoder evidence, not natural production.
  Natural empty Locals is class {}; natural tail is IIFE try/finally/return plus trailing use.
- Repeat snapshots include actual nullable diagnostic channel, complete recursive states,
  and twelve metadata nil channels, as well as canonical fields/flags/refs.
- RepopulateInfo incremental metadata is excluded from managed model; independent Go
  asserts actual nil and binder write closure never creates it. No fabricated default.
- Original binder package has only BenchmarkBind, no correctness tests. Catalog retains
  foundation support assertions and distinguishes new observations/input reuse.

Stable owned files: [oracle](../../tools/binding-completion-oracle),
[completion adapter](../../validation/Binder.Validation/Completion.cs),
[checks](../../validation/BindingCompletion.Checks),
[native dispatch](../../validation/BindingCompletion.Validation),
[corpus](../../validation/binding-completion-corpus),
[comparison](../../tools/compare-binding-completion.py),
[gate](../../tools/run-binding-completion-gate.py),
[recorder](../../tools/record-binding-completion-evidence.py),
[catalog](../binding-completion-tests.json).

Final proof: [binding-completion-verified.json](../../validation/results/binding-completion-verified.json).
The guarded gate passed in **480.539 s** with zero source, asset or historical-proof drift.
All **22,972 xUnit cases** passed without skips: scanner3,838; parser18,709;
binder163 (118 historical +26 cold graph +8 support +7 helpers +4 BindOnce); checker262.
Actual Native AOT passed all15 shared binding groups,26 targeted graphs and17,723
cold broad graphs. The same gate passed foundation3,153 shared bodies,17,633 original
finite parser seeds,17,723 full syntax graphs and17,723 ordinary scanner streams,
plus historical118 binder and92 data-type/43 object/45 scalar semantic comparisons.
Full original numeric156/236 finite Go argument baselines passed; no unbounded fuzz claim.

Source SHA256 `1a5912ce84ce0908bb5d06365de4742fb74d0ae27335572eb55ff04e6011ee87`;
verification-assets SHA256 `4083b40e72903cded2a321ba26738b5ce256bd3ed7a39297934174b1e41cc080`.
Completion native SHA256 `f782f76aa60dae3ab51d4dffae2a76fa48ad9f977392ed27a5cce631a758808a`,
6,639,056 bytes, Linux x86-64 ELF, dependencies libm/libc and the ELF loader.
Actual toolchain: .NET SDK10.0.401, Go1.27.1 linux/amd64, local extracted
GCC15.2.0 (Ubuntu15.2.0-16ubuntu1) via the local cc wrapper; system clang is absent.

All27 reachable binder codes and13 atomic FlowFlags were observed. Three unreachable
strict-helper codes1250/1251/1252 have no binder callers, independently reviewed.
Generated bound payload coverage81/85 includes172/178 static fields; it does not
establish both null/reference states for every field. Exact saved per-cell states and
raw attached JSDoc/synthetic payload exclusions are recorded in
[field reconciliation](../binding-completion-field-coverage.json) and
[source dispositions](../binding-completion-payload-coverage.md).
Shared distinct table slots are a seeded API/observer sensitivity obligation;
reviewed fresh-binding producer closure prevents natural distinct-slot aliases.
Naturally allocated-empty tables and shared FlowList tails are verified.
Physical process-ID magnitudes, incremental RepopulateInfo and later-stage compiler
behavior remain outside the comparison claim, with explicit strict Go nil/owner guards.

First frozen gate stopped34.647s with no source/asset/historical drift,161/162 binder
xUnit passed. Failed generated private SymbolName display leaked a process-local ID
for malformed assignment destructuring. Actual helper return now uses the same strictly
validated BoundName descriptor as bound names; ordinary #foo remains exact rawbytes.
Failed gate preserved as first-failed-*; scanner originalGo before/after order proof
and shared OrderSensitivity require old-binary failure/new-binary stability. No library
repair or original semantic expectation change. The final proof embeds the failed-gate summary and before/after order-sensitivity evidence.
Preserved old observer fails and repaired observer passes both exactcase10003 and the
shared malformed assignment witness, before/after8 private-class prefixes.
No further source/tool/corpus or documentation writes are planned by verification.
