# Checker support checkpoint

Owner: AST/support agent. Completed; no active implementation or verification work.
Upstream pin: `fed0bf24149fb1ed36039212648bafdafc1ea10e`.

The [final gate](../../validation/results/checker-verified.json) passed with exit 0
for source SHA256 `ac38adb32995090a01777c010452f0405aed99e768a370c0178febc196cb940e`.
This accepts only the fixed noLib/noResolve single-file TS scalar slice. It records
45 Native AOT semantic comparisons, 35 admission controls, 20 independent Go
page/table observations, 10 authored API assertions and 82 checker xUnit tests.
Shared binder/parser/scanner regressions also passed: 122/475/1287 xUnit tests.
Executable sources, tools and corpora are frozen; this checkpoint update is documentation-only.

## Completed ownership

- [Types.cs](../../src/TypeScript.Scanner/Checker/Types.cs): full flags, stable Type
  wrapper, intrinsic/literal fresh-regular identities, actual boolean unions,
  partial ObjectType initialization markers and selected cache records. Managed
  classes flatten Go payload bases; no invented Signature/Mapper state.
- [Links.cs](../../src/TypeScript.Scanner/Checker/Links.cs) and
  [Linkstore.cs](../../src/TypeScript.Scanner/Core/Linkstore.cs): all original store
  methods with stable managed cells, page-level TryGet/Has and indirect nil slots.
- [Nameresolver.cs](../../src/TypeScript.Scanner/Binder/Nameresolver.cs): all thirteen
  original functions and callback fields, raw-byte names, nil maps and lexical/global
  lookup. Selected checker hooks reject unsupported semantic paths explicitly.
- Actual [CheckFlags](../../src/TypeScript.Scanner/Ast/CheckFlags.cs) and
  [Symbol](../../src/TypeScript.Scanner/Ast/Symbol.cs) field, bounded options,
  required [AST helpers](../../src/TypeScript.Scanner/Ast/Utilities.cs), generic
  [spelling selection](../../src/TypeScript.Scanner/Core/Core.cs), diagnostic
  collection/comparison and diagnostic-only invalid UTF8 run conversion.
- [Source map](../../source-map.json) finalized: 74 library C# files each mapped
  once, 92 entries and 68 checked upstream hashes. Historical scanner/parser/binder
  scopes and evidence remain distinct; expanded counterparts keep partial status.

## Reproduction and limits

[Enum extraction](../../tools/generate-checker-support-enums.py) accepts an optional
scratch output root and preserves the manual Type suffix. The
[Go AST emitter](../../tools/checker-support-port/main.go),
[patcher](../../tools/checker-support-port/patch.py) and LF formatter regenerated the
thirteen-function NameResolver byte-for-byte. Eight contextual query predicates
were emitted from the selected function list and integrated by
[query-ast.py](../../tools/checker-support-port/query-ast.py); scratch comparison matched.
[Feature-map extraction](../../tools/generate-checker-feature-map.py) preserves
57 keys / 107 ordered entries. Handwritten models, cache/options/diagnostic/core
adaptations remain authoritative; there is no complete checker regeneration claim.

ReferenceResolver, general objects/calls/functions/aliases/generics, source assignment
syntax, branching flow, libraries, cross-file resolution and emit remain excluded.
Selected graph observables do not cover every mutable cache; union regularType and
post-check lazy documentation caches are omitted. No diagnostic chains occur in this
corpus; allocated-empty tables are observed separately by actual Go utility probes.
Any expanded semantic boundary requires a new scoped implementation and verification phase.
