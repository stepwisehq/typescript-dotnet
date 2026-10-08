# Foundation syntax support review

Read-only family review against TypeScript pin
`fed0bf24149fb1ed36039212648bafdafc1ea10e`; final bounded foundation gate passed; no unresolved blocker.
Scope: support-owned AST/factory/visitor/position/cache/fact/English-message changes,
including retained implementations that new foundation APIs expose.

- **Concrete repair implemented:** [Ast.VisitNodes and SameMapNodes](../../src/TypeScript.Scanner/Ast/Ast.cs),
  and [NodeVisitor.VisitSlice](../../src/TypeScript.Scanner/Ast/Visitor.cs), previously read mutable
  List lengths or use fail-fast List enumeration. Original Go callback traversal
  captures a slice header/length. Append-only callbacks must not visit appended
  nodes or throw. Support restored captured-length handling; verification owns actual
  Go append observations for unchanged/changed/deleted and SyntaxList mapping paths.
  This parallels the independently repaired [Core.Filter](../../src/TypeScript.Scanner/Core/Core.cs).
  Managed prefix copies preserve captured length; Go backing-array aliasing is not
  claimed by the List adaptation.
- **Position mapping:** [PositionMap](../../src/TypeScript.Scanner/Ast/PositionMap.cs)
  retains end-of-rune entries, delta accumulation, both <= binary searches and
  JS-string surrogate decoding. Public strings encode once. ASCII maps have an
  allocated empty private entries list instead of a nil slice; public results match.
  SourceFile's Lazy factory retains identity/publication. A factory failure is
  rethrown on later reads rather than Go Once returning its zero result; only
  excluded resource exhaustion can fail ordinary raw-text computation.
- **Source caches:** [SourceFile](../../src/TypeScript.Scanner/Ast/Ast.cs) name-table
  publication and completed-on-failure marker match Once; declaration-map locking
  and nil-based retry match its separate mutex algorithm. Token cache uses parent
  identity plus span, validates cached kind before reparsed-parent rejection, then
  preserves token-kind factories, raw substrings and parent/location assignment.
  GetOrComputeData uses typed cells with wait/publication and completed-on-failure;
  later reads return default after a throwing callback, matching Go Once. Default
  keys reject; allocation replaces Go type erasure without reflection/AOT discovery.
- **Facts and iterators:** [Subtreefacts](../../src/TypeScript.Scanner/Ast/Subtreefacts.cs)
  enum/masks, eraseable child presence, object-rest promotion and cached FactsComputed
  masking match source. Concrete compute/propagation dispatch follows flattened
  embedded bases; unsupported CompositeBase default throws. Lazy generated child
  iterators preserve original ForEachChild field order, JSDoc name-first branches,
  list capture at traversal start and fixed lengths. Authored mutation observations
  are separately owned by verification; source family review is not exhaustive
  branch coverage. Compiler-created SyntheticExpression remains explicitly excluded.
- **Factories/updates/clones:** creation, update and clone hooks retain source order;
  update restores flags/span only when identity changes. Node/ModifierList constructors
  preserve nil contents and synthesized spans. ReleaseArenas has no managed arena to
  release; counters/hooks remain. Generated shallow clones preserve child identity;
  full graph/deep clone and visitor lifting/deletion assertions supplement direct
  original tests. New generic storage and interfaces have static AOT-safe dispatch.
- **English records:** independent extraction compared all **2222** generated messages
  against pinned Go: exact names, codes, categories, keys, text and unnecessary,
  deprecated and compatibility-pyramid flags. The generator emits the entire catalog,
  not merely currently observed diagnostics. Localization remains default English;
  external/serialized/ad-hoc diagnostic constructors retain metadata. Source-file
  path comparison converts public filenames to raw bytes before ordinal comparison.

## Required Go sorting dependency

[Sort](../../src/TypeScript.Scanner/Runtime/Slices/Sort.cs) and
[Zsortanyfunc](../../src/TypeScript.Scanner/Runtime/Slices/Zsortanyfunc.cs) retain
SortFunc plus all 13 reachable PDQ helper bodies from **Go 1.27.1**. Comparison
signs/order, equal partition, recursion boundaries, imbalance limit, heap fallback,
pivot reversal, median/ninther swap counters, partial insertion's original `j>=1`
and uint64 xorshift match source. BitOperations bit length matches bits.Len for
managed List lengths. No stable-sort or unrelated slices API is imported.

Independent SHA256 verification matches the source-map records:
`sort.go` e6cd273b637e7dd2f547596c22e6236b92ae9b376f31ceb7b3c3269dd60a38df;
`zsortanyfunc.go` 50398783e085ac9cc1d7c6249ebe75a5cc4425dcdc6a4cc448ea6e7179126492.
[Go license](../../licenses/Go-LICENSE.txt) exactly matches the toolchain's BSD license,
SHA256 911f8f5782931320f5b8d1160a76365b83aea6447ee6c04fa6d5591467db9dad.
Headers retain Go authors and port notices. Five actual broad reparse-clone tie-order
failures motivated this dependency; identity-permutation probes/replay and final
native evidence now pass in the exact-source foundation gate. Comparator mutation/concurrent mutation is not an
additional guarantee.

Central11 source/checks build passed0warnings/errors. The owned focused rerun passed
114/114 checks:94 original path rows and20 additional helper groups, including six
actual-Go Filter callback rows and16,705 numeric Go/Node comparisons. Syntax callback
probes and reparse tie replay pass in the verifier-owned final native proof.
[Final gate](../../validation/results/foundation-verified.json) exit0 in231.23s: all3153
shared native bodies,22930 xUnit and17723 broad requests per parser/scanner pass
without source/asset drift. Source `d14cb48750c8ce35f0767f97885b01f9b0d63c853390f6cfb4f9b3f4cb50fd67`.
No active executable work or unresolved review blocker. Support independently challenged
and accepted the1513 public-unreachability rationale; this is source reasoning,
not observed diagnostic coverage. No other concrete blocker found in
this bounded source review; no exhaustive AST or compiler-baseline parity claim.

## Independent diagnostic-gap challenge

Accepted the three proofs in [defensive parser diagnostic inventory](../foundation-parser-unreachable-diagnostics.md)
after inspecting actual pinned callers and recovery. This closes source dispositions,
not observed-code coverage; no private state injection or absence-of-witness inference.

- **1140:** [isListElement](../../upstream/TypeScript/tsc/internal/parser/parser.go:891)
  accepts comma; [isListTerminator](../../upstream/TypeScript/tsc/internal/parser/parser.go:948)
  accepts every noncomma for PCTypeArguments. [parseDelimitedList](../../upstream/TypeScript/tsc/internal/parser/parser.go:694)
  cannot reach abort with any token because the required non-element/non-terminator
  conjunction is false. Challenged both bracketed type-reference arguments and
  expression speculative arguments at line5343; both use this exact context/loop.
  JSDoc reuses parseTypeArguments and does not add a separate error caller.
- **1179:** [outer heritage list](../../upstream/TypeScript/tsc/internal/parser/parser.go:1823)
  starts only on extends/implements. The inner heritage-element list's normal end
  tokens are EOF/open brace/extends/implements, all accepted by the outer list as
  terminators/elements. A return at any other token requires an inner comma/context
  error at that token. Challenged the no-progress advance, parent-context recovery,
  scanner errors, keyword-escape checking, type-heritage conversion and lookAhead
  rollback: a later bad returned token is diagnosed again by the inner loop; lookAhead
  restores its saved length after that error, preserving it. Neither finishNode nor
  checkJSSyntax removes parse diagnostics or moves the scanner. checkJSSyntax appends
  to the separate JS list. The subsequent outer error uses the unchanged TokenRange
  and [same-start suppression](../../upstream/TypeScript/tsc/internal/parser/parser.go:329).
  The1179 arm may execute but cannot append a public diagnostic.
- **1439:** [sole blank-name selection](../../upstream/TypeScript/tsc/internal/parser/parser.go:2092)
  requires Identifier text type plus unconsumed Equals. All three callers inspected:
  expression statement1534, throw1466, property-semicolon2053. The first two consume
  Equals in [assignment parsing](../../upstream/TypeScript/tsc/internal/parser/parser.go:4219)
  and return BinaryExpression, losing the required Identifier. A line-break throw
  creates an empty identifier and accepts ASI. Property parsing always calls
  [parseInitializer](../../upstream/TypeScript/tsc/internal/parser/parser.go:1700),
  which consumes Equals without a line-break exception; a nonnil initializer also
  exits through the generic-semicolon branch before the name helper. Challenged
  escaped type identifiers, declaration lookahead and class/property recovery:
  they do not create another caller or preserve the required Equals state.

Defensive C# arms remain ported. Actual witnesses1129,1486(related information),18016
are separate verifier-owned evidence; these three source proofs do not replace those.
