# Independent checker support and harness review

Reviewer: algorithm owner. Pin `fed0bf24149fb1ed36039212648bafdafc1ea10e`.
Bounded to the approved scalar single-file contract; no general checker acceptance.

- Compared [Types.cs](../../src/TypeScript.Scanner/Checker/Types.cs) wrapper flags,
  payload casts, literal fresh/regular back references, `Distributed`/`Types`,
  union shared state and selected links with upstream `checker/types.go`.
  One stable wrapper per payload preserves identity; omitted signatures/mappers
  are explicit field/model exclusions. Public alias access uses .NET null checks
  rather than Go nil-receiver calls. No selected-path discrepancy found.
- Compared [Links.cs](../../src/TypeScript.Scanner/Checker/Links.cs) and
  [Linkstore.cs](../../src/TypeScript.Scanner/Core/Linkstore.cs) with upstream
  `checker/links.go` and `core/linkstore.go`. Page-index split, 256-slot boundaries,
  slot identity, allocated-page node zero values and nil symbol pointers retain
  distinct Go semantics. Managed cells/objects replace slot addresses and arenas.
  Twenty independently derived Go/Native AOT utility observations match in the final stable gate.
- Reviewed full [Nameresolver.cs](../../src/TypeScript.Scanner/Binder/Nameresolver.cs)
  against upstream traversal, nil locals/exports reads, global-script skip,
  GlobalLookup addition, self-reference callback order, optional callbacks and
  named break/fallthrough translations. Function boundaries and dispatch retained;
  generated helper scratch equality reported by owner. Parameter/class/module
  arms exist, but first checker admission does not claim checking those constructs.
- Reviewed [Diagnostic.cs](../../src/TypeScript.Scanner/Ast/Diagnostic.cs)
  collection Add/dedup/collisions, lookup, cloned results, per-file/global stable
  sort, global combined sort and exact comparison priority. Raw byte arguments
  remain distinct from display text. Comparator paths retain filename adaptation
  from public .NET filename to UTF-8 ordinal byte order. The latest formatter fix
  restores Go `strings.ToValidUTF8` grouping of consecutive invalid bytes,
  especially a long Unicode type string cut at byte 317. The final native differential corpus confirms this display boundary; stored raw
  args are not modified.
- Reviewed selected options, generic spelling selection/tie breaking and
  feature-map extraction. Candidate byte lengths versus name rune lengths follow
  the pinned source even where surprising. Pinned Go casing is reused. Generic
  type tests and constrained factories are statically rooted, not reflection-based
  library serialization. General options/pools are excluded/documented.

Reviewed the authored [Admission.cs](../../src/TypeScript.Scanner/Checker/Admission.cs)
separately: fixed TS script syntax, parser diagnostics, directives/references and
JSDoc tags reject explicitly; ordinary documentation and local scalar globalThis
shadowing admit. Built-in globalThis rejection occurs after real object resolution.
Owned-node guards use reference identity and terminate on cyclic parent chains.
An input tree is assumed immutable during checker use; mutable-tree/concurrent
checker APIs are not claimed.

Reviewed [checker oracle](../../tools/checker-oracle/main.go), its parser discovery
reuse, [typed runner](../../validation/Checker.Validation/Runner.cs), contracts,
utility probes and comparer. The Go path creates a real noLib/noResolve Program,
asserts a single file, invokes actual checker APIs and keeps eight environment
errors separate. It does not reproduce the selected C# checker algorithms.
Type/symbol/table canonical IDs are assigned before recursive edge discovery,
preserving sharing/cycles; declaration IDs must belong to the source graph.
Tables sort raw keys. Public type text, raw intrinsic/literal values and arguments,
flags, fresh/regular literal links, union constituents, alias edges, related/chained
ordered diagnostics and Globals/globalThis table identity are compared exactly.

One harness omission reported and fixed by owner: typed diagnostic serialization
must encode actual `SkippedOnNoEmit()` rather than its false default. Type graph
scope is selected observable payloads and literal identity, not every mutable
internal cache field (union regular/origin caches are not all encoded). Neither
absolute allocation IDs nor excluded signature models are claimed equal.
Serialization uses static typed JSON metadata for Native AOT; Go reflection is
independent of the C# payload inventory. Admission rejection controls are authored
boundary assertions, distinct from Go semantic comparisons. No duplicate shared
builds/tests or edits to support/harness ownership were made during this review.

Status: independent support/admission/harness review complete, no unresolved
blocker or active work. The final stable combined gate (`bash tools/verify-checker.sh`)
passed at exit 0; [accepted evidence](../../validation/results/checker-verified.json)
records source SHA256
`ac38adb32995090a01777c010452f0405aed99e768a370c0178febc196cb940e`.

Final evidence includes 45 exact actual-Go/Native-AOT semantic graph comparisons,
121 relationships, 274 queries and all 82 checker xUnit tests. All 35 admission
controls, 10 public API assertions and 20 actual Go link/table observations pass
natively. Earlier-stage regressions also pass: binder 118 native/122 xUnit,
parser 471 native/475 xUnit and scanner 1276 native/1287 xUnit. The checker artifact
is an x86-64 ELF Native AOT executable; its hash/dependencies are recorded.

The first combined run passed its runtime checks but was rejected as acceptance
proof because the inventory-tool indentation metadata edit changed a guarded
verification asset during the run. This was an asset-stability rejection, not a
runtime failure. Verification repeated the entire combined gate on stable assets;
the [rejected proof](../../artifacts/checker-validation/asset-drift-gate.json) and
[accepted repeat](../../artifacts/checker-validation/final-gate.json) remain available.
No executable/tool/corpus changes were made for this final documentation update.
