# Independent checker fidelity review

Reviewer: AST/support owner. Upstream pin `fed0bf24149fb1ed36039212648bafdafc1ea10e`.
Completed with no unresolved blocking finding and no active review work.
The [final guarded Native AOT gate](../../validation/results/checker-verified.json)
passed for source SHA256 `ac38adb32995090a01777c010452f0405aed99e768a370c0178febc196cb940e`.
Acceptance is confined to the selected scalar slice; this review does not expand admission.

## Findings and final dispositions

- [Relater.cs](../../src/TypeScript.Scanner/Checker/Relater.cs): restored the actual
  definitely-nonnullable source / nullable-target union normalization and recursive
  union constituent handling in typeCouldHaveTopLevelSingletonTypes. Selected
  boolean/undefined union relationships retain upstream branches.
- [Checker.cs](../../src/TypeScript.Scanner/Checker/Checker.cs): restored the global
  spelling primitive-alias candidate iterator, parenthesized literal quick-type nil
  fallback and genuine getNamedMembers nil/filter/sort closure. No assumed-empty or
  assumed-false substitute remains for these retained branches.
- Internal diagnostic naming uses raw byte strings; public TypeToString decodes
  once. [Diagnostic formatting](../../src/TypeScript.Scanner/Diagnostics/Diagnostics.cs)
  now matches strings.ToValidUTF8 invalid-run collapse. Four long-Unicode boundary
  specimens originally had matching raw arguments but wrong display replacement
  counts; final comparisons pass. Stored args and scanner JS/surrogate semantics
  remain unchanged. The verifier's nil-list assertion fix was a separate harness defect.
- [Exports.cs](../../src/TypeScript.Scanner/Checker/Exports.cs) forwards through the
  actual private isTypeAssignableTo boundary; authored ownership checks reject
  foreign checker types/nodes, orphan nodes and cyclic foreign parent chains.
- Withdrawn suspicion: the special missing-name string `"ast.Symbol"` is verbatim
  in pinned checker.go:14206. It was preserved rather than corrected speculatively.

[Flow.cs](../../src/TypeScript.Scanner/Checker/Flow.cs),
[NameResolver](../../src/TypeScript.Scanner/Binder/Nameresolver.cs) and
[Diagnostic.cs](../../src/TypeScript.Scanner/Ast/Diagnostic.cs) were reviewed for
straight-line assignment/start/cache behavior, nil-map/global lookup hooks,
collection identity/order/result-copy and raw argument boundaries. Selected scalar
NodeBuilder/Printer paths retain string/control/surrogate escaping and byte cutoff.
Unsupported flow/structured/serialization branches remain explicit or excluded.

The authored admission/harness/oracle/gate review found no blocker. Entity registration
precedes cyclic edges; declaration/constituent order and raw-byte table-key sorting
are preserved. Actual globalThis exports alias and CheckFlags are observed. Admission
rejects unsupported directives, external/module/tagged-JSDoc environments and parse
errors, while allowing ordinary scalar names, including local globalThis shadowing.

## Final evidence and limits

The gate records 45 native semantic comparisons, 35 admission controls, 20 actual
Go page/table observations, 10 authored API checks and 82 checker xUnit passes.
Binder/parser/scanner native comparisons and 122/475/1287 xUnit regressions also
passed. Source/assets drift guards and native binary metadata tie this evidence to
the frozen tree. The [algorithm owner's support review](review-checker-support.md)
has no unresolved blocker. No independent project builds or algorithm-owner edits
were made during this review.

This is bounded evidence, not general checker equivalence. Graphs omit some mutable
cache state, union regularType and post-check lazy documentation caches. No diagnostic
chains occur in accepted scalar cases; allocated-empty tables are covered by separate
Go utility probes, not scalar graph specimens. General library/project/module,
structured types, branching flow and emit remain outside acceptance.
