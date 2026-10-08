# Binder AST/support boundary checkpoint

Owner: AST/support agent. Root approved this boundary after 20 native/Go parser
readiness probes closed 14 diagnostic gaps. Parser/benchmark commit `de25b66`. Upstream remains
`fed0bf24149fb1ed36039212648bafdafc1ea10e`. Bounded source-file binding is accepted; no active work remains. Scanner owned central builds.

## Implemented minimum model and shared contract

- [binder.go](../../upstream/TypeScript/tsc/internal/binder/binder.go) imports AST,
  collections, core, debug, diagnostics and scanner; no checker/compiler resolver.
  Source-file binding needs neither sibling name nor reference resolver APIs.
- Keep Node wrapper and typed payload identity. Expand upstream embedded bases as
  flattened interfaces: DeclarationBase(Symbol), ExportableBase(LocalSymbol),
  LocalsContainerBase(Locals/NextContainer), FlowNodeBase(FlowNode). Concrete
  payloads expose state only where upstream embedding allows it. Add exact Node
  data/accessor forwards. Keep compiler fields out of parser SyntaxFields.
- BodyBase gains EndFlowNode; corresponding concrete payloads gain ReturnFlowNode
  and CaseOrDefaultClause gains FallthroughFlowNode. Preserve clone/update state
  rules from original Go methods; do not automatically copy new bind state.
- [symbol.go](../../upstream/TypeScript/tsc/internal/ast/symbol.go): Symbol fields
  Flags/Name/Declarations/ValueDeclaration/Members/Exports/Parent/ExportSymbol and
  atomic identity. Checker-only CheckFlags excluded explicitly. Complete numeric
  SymbolFlags counterpart. All names retain raw bytes, including invalid FE sentinel.
- SymbolTable uses ordinal byte-string keys and missing-read returns null, unlike
  ordinary Dictionary indexing. Preserve nil versus empty table. Lazy table
  helpers use managed references/GoRef for Go pointer-to-table field mutation.
- [flow.go](../../upstream/TypeScript/tsc/internal/ast/flow.go): full FlowFlags,
  FlowNode and ordered linked FlowList; FlowLabel alias remains FlowNode. Add both
  synthetic KindUnknown payloads/factories for switch clauses and reduce labels.
- SourceFile gains SymbolCount, BindDiagnostics, PatternAmbientModules, GlobalExports
  and IsBound/BindOnce. Publication must preserve once-only synchronized completion.
- Expand [utilities.go](../../upstream/TypeScript/tsc/internal/ast/utilities.go)
  only for binder closure: atomic IDs, lazy tables, module instance-state/ancestor
  analysis, declaration/assignment/name/strictness predicates. Include narrow core
  Pattern parsing/AppendIfUnique/OrElse and scanner declaration-name/error ranges.
  NodeId/SymbolId represented as ulong; process-global allocation uses atomic CAS.

## Risks / explicit omissions

Symbol identity, merge ordering and declaration arrays must remain shared; table
serialization should sort raw keys, since Go map iteration is unspecified. IDs
appear in private-identifier and ambient-pattern symbol names: oracle comparison
must canonicalize identity-derived portions rather than assume process counters
match. Flow graph serialization must handle cycles, linked antecedent order and
synthetic payloads separately from syntax traversal. Go once marks invocation done
on panic but publishes IsBound only on success; managed synchronization must not
silently introduce retries or generic Lazy exception behavior.

Binding mutates existing node flags/CommonJS metadata and consumes parser-produced JSDoc state;
accepted parser structure/diagnostics must retain regression coverage. Binder
emitFlags are observable NodeFlags writes, not authorization to implement emit.
No general transform facts, checker types/transient machinery, cross-file merges,
name/reference resolver, module resolution, service caches or emitter implementation.

Symbols/flags/raw tables/graphs/two synthetic payloads, embedded bases,
BoundFields metadata and SourceFile bind state compile with the full binder.
The 73-function generated syntax helper closure, plus explicit handwritten atomic/table/name adapters, is emitted by persistent
[binder-ast-port](../../tools/binder-ast-port/main.go); systematic enum-prefix
mapping corrections are retained there. Required diagnostics now contain 198
exact pinned records; narrow Core OrElse/AppendIfUnique helpers integrate. No placeholders.

Generator ownership: tools/generate-ast.py now retains binder fields separately
from SyntaxFields; run tools/generate-jsx-projection.go after schema output.
New tools/generate-binder-enums.py writes exact flags and preserves manual suffixes.
Binder utility emitter replaces only its closure marker; parser/manual helpers
remain authoritative. LF format with tools/CSharpFormat before central builds.
BindOnce uses a non-reentrant managed gate, records invocation before callback,
and publishes bound status only on success, preserving panic/no-retry behavior.

Final gate: [binder-verified.json](../../validation/results/binder-verified.json)
records 118/118 actual Native AOT binder graph comparisons (5,182 nodes, 858 symbols,
430 tables, 700 flows, 150 FlowLists and 41 diagnostics) and 122/122 binder xUnit
checks. Concurrent publication, failed-callback no-retry, default-options names
and repeated binding identities passed. Parser regression: 471 native/475 xUnit;
scanner regression: 1,276 native/1,287 xUnit plus 178 helper results. Exit code zero.
Accepted source SHA256: `0386f000d85382e8331fa97dee83e50dd469bdca4b90bead795075a18436bf18`.

Scratch schema + JSX projection + binder utility emission + LF formatting matched
reviewed Ast.cs/AstGenerated.cs, KindGenerated.cs/KindStringerGenerated.cs and
Utilities.cs byte for byte. Ast.cs handwritten portion is copied as the
source input, not claimed generated. Support sources are frozen.

Regeneration order: generate-ast.py scratch-directory; copy authoritative Ast.cs;
generate-jsx-projection.go scratch-directory; binder-ast-port/main.go scratch
Utilities.cs; CSharpFormat explicit scratch files; compare before replacement.
generate-binder-enums.py preserves manual model suffixes. Do not blindly run the
older parser utility emitter over manual/binder utility sections.

No active work. Independent algorithm review recorded
in [review-binder.md](./review-binder.md); support/model review is
[review-binder-support.md](./review-binder-support.md). Bounded acceptance does not include resolver or
transform scope expansion. Final source mapping is [source-map.json](../../source-map.json).
