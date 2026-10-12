# Checker query and token dependency review

Independent source review against pinned `fed0bf24149fb1ed36039212648bafdafc1ea10e`.
Scope is [Services](../../src/TypeScript.Scanner/Checker/Services.cs),
[ServiceExports](../../src/TypeScript.Scanner/Checker/ServiceExports.cs),
[Tokens](../../src/TypeScript.Scanner/AstNav/Tokens.cs),
[BinarySearch](../../src/TypeScript.Scanner/Core/BinarySearch.cs), the existing checker
worker visibility changes and the constant-value leaf relocation from EmitApi.
Configured-hover NodeBuilder/Printer changes are assigned separately and excluded here.
No build, Go execution, xUnit or Native AOT claim accompanies this source review.

The service algorithms, signature/type cache save-and-restore, symbol-root traversal,
reference scanning, constraint unions/intersections and token scanner/JSDoc ordering
were compared with their direct Go bodies. The cache maps use stable Node/Symbol owners
instead of Go link pointers because accepted link stores contain value records; restoring
through those owners preserves the same slots. The managed restoration finally block
also prevents abandoned cache state on exceptions. Existing public project-symbol
ownership checks remain explicit accepted boundary adaptations. Binary search and the
constant-value move preserve their original branches. Visibility joins change access only.

Concrete findings sent to the author:

- Newly traversed symbol maps require null guards: Go ranges a nil map safely;
  direct dictionary enumeration cannot. Scope-copy functions were corrected. Module
  export/property enumeration and module member lookup were also corrected with owner-null handling;
  the SymbolTable indexer already handles absent keys correctly.
- Property/symbol-name producers must retain their ByteStringValue headers through
  the actual checker and SymbolTable lookups. The author corrected the reported
  augmented-property, union-property, global-name and contextual/destructuring paths.
- The token rightmost-worker slice has original capacity one; the initial zero-capacity
  translation was corrected. No other explicit make capacity or clone exists in that Go file.
- Token visitor creation is present in upstream too. CLR wrapper closures are an
  allocation adaptation to assess across the whole callback owner, not grounds to
  invent a different upstream visitor lifetime. Dictionary interface enumeration can
  be replaced by direct null-guarded traversal to avoid boxing while retaining key headers.

Original assertion provenance: the two rows of TestUnitFindPrecedingToken are retained.
Four mapCode Go JSON baseline channels retain the original per-byte-position traversal,
run coalescing and JSON data comparisons, including original panic recovery for the
next-token channel. They cover only the Go-baseline portions of four larger original
functions. Their TypeScript differential portions and additional JSDoc/pointer-identity
subtests remain unported; lifecycle TestMain is not an executed assertion. The two
service query-order controls are authored observations, not upstream function passes;
configured-hover control semantics belong to its separate review.

Status: source review CLEAR after the nil-owner/capacity/borrowed-name corrections.
The author explicitly retains original per-call visitor ownership and documents CLR
wrapper delegate/closure allocation as an adaptation limitation; no stack-only allocation
claim or invented query-wide reuse was accepted. Central compilation and behavioral
acceptance remain pending.

Reviewed snapshots (SHA256):

- `src/TypeScript.Scanner/Checker/Services.cs`: `52a75bc425d97a08799c61f8a6b60a52c65e0bcaeee2e109273869ae3ea33196`
- `src/TypeScript.Scanner/Checker/ServiceExports.cs`: `b05a48b3664afc8e0df1ee6d46ed94863a1319504bcedb351e94cf8dce3f7b14`
- `src/TypeScript.Scanner/AstNav/Tokens.cs`: `88ee1c600d96ced9bf35043696ae382ce54979fbede12f7bbf76c8703d2c8085`
- `src/TypeScript.Scanner/Core/BinarySearch.cs`: `220d66df275c64a41c71d51012edd639ac11e4420d00dc7eb657f3529d4163d5`
- `src/TypeScript.Scanner/Checker/EmitApi.cs`: `74ce725a3b72cae7584a38f483034ea13a3acf63835e4b9bcc3e6857ffa47c2d`

Narrow final declaration joins reviewed: AstNav comparison constants are exact -1/0/1; synthetic-symbol flag uses the actual CheckFlags enum. No branch change.

Build-2 join review: Memory<ValueSymbolLinks> reads use !IsEmpty/Span[0], ref property-name headers and actual source worker/visitor aliases match. Two reference-query comparisons initially used inherited ValueType.Equals (owner/offset identity), which differs from Go string content equality; corrected to EqualsValue and independently verified. Core partial accessibility now matches the existing internal owner. No tests executed by reviewer.

The existing authored query-order control now retains a declaration and both actual
call identifier references (three total) across separate byte-owner offsets. This
narrow assertion delta and EqualsValue correction are reviewed clear; test count
is unchanged and execution remains pending.
