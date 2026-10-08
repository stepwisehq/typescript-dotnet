# Parser/runtime fidelity review

Reviewed against Microsoft/TypeScript fed0bf24149fb1ed36039212648bafdafc1ea10e.
This is a bounded independent source review; final xUnit and Native AOT gates are
owned by verification. No optimization or profiling was performed.

## Findings and fixes

- **Fixed, nil slice fidelity:** [GoRuntime.Append](../../src/TypeScript.Scanner/Runtime/ParserCompat.cs)
  initially allocated an empty list for `append(nil, empty...)`. Both overloads
  now preserve the source for zero additions. Existing structural cases observe
  nil, empty and missing lists; no isolated public parser reproducer was found.
- **Fixed, lazy JSDoc:** [SourceFile.JSDoc](../../src/TypeScript.Scanner/Ast/Ast.cs)
  initially treated a registered callback's valid nil result as an absent
  registry. Registry validation now precedes invocation and nil is cached.
  [Named regression](../../tests/Parser.Tests/LazyJSDocTests.cs) checks repeated
  lazy and eager access on a node with no actual JSDoc.
- **Fixed, consolidation/formatting:** a duplicate spelling-helper block and
  second file-scoped namespace in [Core](../../src/TypeScript.Scanner/Core/Core.cs)
  were removed. The [formatter](../../tools/CSharpFormat/Program.cs) now explicitly
  writes LF; owned changed AST/Core/Diagnostics outputs were formatted.

## Checked boundaries

[Parser mark/rewind/lookAhead](../../src/TypeScript.Scanner/Parser/Parser.cs)
restore the same scanner, context, diagnostics lengths, JSDoc/reparse lengths and
error/await state as Go. Factory counters are intentionally not rewound, matching
upstream. Raw token accessors and source initialization preserve byte strings;
public decoding occurs at explicit boundaries rather than inside parser comparisons.

The five function-bearing parser counterparts account for 434 parser, 54 JSDoc,
21 reparser, six utilities and two references functions. Only pool return
`putParser` is omitted; `init` becomes static JSDoc callback registration. Managed
parser creation replaces pooling. The [Go AST emitter](../../tools/parser-port/main.go)
fails for unsupported syntax, preserves named results and receiver/local scope,
and translates switch fallthrough and labeled breaks explicitly. There are no
other upstream defer statements requiring cleanup or labeled continues in this slice.

[Slice adapters](../../src/TypeScript.Scanner/Runtime/ParserCompat.cs) preserve
nil/empty results and missing-list identity. Arena zero allocation/clone behavior
matches [upstream](../../upstream/TypeScript/tsc/internal/core/arena.go). Managed
lists copy slice storage instead of reproducing Go capacity/backing-array aliasing;
reviewed parser loops mutate node payloads or separate output lists, not the
structure of their ranged input. This adaptation is not a general Go slice API.

AST field serialization uses explicit generated syntax metadata and traversal;
no reflection-only serialization or dynamic code generation is required. Compiler
state, general transform facts and checker-only SyntheticExpression remain excluded.
Private JSX projection retains ambient/type erasure pruning, with targeted module
indicator comparisons. Latest parent report: 458 bounded managed structural cases
pass, all 186 parser-reachable factory payloads exercised; this is coverage evidence,
not proof of arbitrary-program equivalence. No additional blocking defect found.

Executable support edits frozen after this review; verification must bind final
Native AOT results to the final source hash.
