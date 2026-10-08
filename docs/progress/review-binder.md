# Independent binder algorithm/runtime review

Owner: AST/support agent. Pinned upstream `fed0bf24149fb1ed36039212648bafdafc1ea10e`;
bounded source-file binding review, independent of the binder implementation owner.

## Finding

- **Resolved, identity fidelity:** Go `int(GetSymbolId(...))` is signed 64-bit on
  the pinned platform; a C# `int` cast truncated private-name IDs above 32 bits.
  [Binder.cs](../../src/TypeScript.Scanner/Binder/Binder.cs) now formats an
  unchecked `long` conversion with invariant decimal `Strconv.Itoa(long)`;
  [patch.py](../../tools/binder-port/patch.py) preserves the fix on regeneration.
  No ordinary corpus failure triggered this finding. The fix follows the source
  integer-width contract; a counter-exhaustion experiment was not performed.

## Checked paths and conclusions

- [declareSymbolEx / declareModuleMember](../../src/TypeScript.Scanner/Binder/Binder.cs)
  retain merge/exclusion tests, replacement behavior, distinct local/export symbol
  links, declaration insertion order, and ordered duplicate diagnostics/related info.
  Symbol and Node equality remains reference identity.
- Nil table reads in `lookupName` and CommonJS export promotion use nullable
  indexing; missing keys use [SymbolTable](../../src/TypeScript.Scanner/Ast/Symbol.cs)'s
  null-returning indexer. The sole map-value traversal promotes into the export-equals
  symbol's separate export table; the checked binder paths do not mutate the table
  being enumerated. Dictionary order is not a claimed Go semantic; canonical output
  sorts raw keys. Deferred expando traversal does not append further expando work.
- Container dispatch/save/restore, IIFE/constructor flow handling, unreachable
  for/for-in early exits, labeled break/continue and try/catch/finally reduction
  follow the upstream branches. Flow labels preserve ordered, identity-deduplicated
  linked antecedents and Referenced/Shared mutations.
- [scanner source-file spans](../../src/TypeScript.Scanner/Scanner/Scanner.cs) match
  the upstream cases, including reparsed declarations, missing nodes and JSDoc
  satisfies tags. Binder calls raw byte-string text/name helpers, and encodes the
  public Unicode filename once when constructing its raw module symbol name.
- [binder emitter](../../tools/binder-port/main.go) preserves Go lexical-object
  scopes, receiver callbacks, multiple results, switch fallthrough and labeled
  breaks; unsupported syntax fails rather than emitting placeholder handlers.
  The only omitted defer returns the binder to Go's pool: fresh managed binders
  and [object arenas](../../src/TypeScript.Scanner/Runtime/BinderCompat.cs) preserve
  per-bind state/identity and use AOT-safe constrained construction.
- [SourceFile copyFrom/BindOnce](../../src/TypeScript.Scanner/Ast/Ast.cs) intentionally
  leave symbol/local/flow/bound publication state out of clones, as upstream does.
  BindOnce serializes callbacks with a non-reentrant gate, never retries a failed
  callback, and publishes IsBound only on success. Later calls after failure return
  without replaying the exception. The [named tests](../../tests/Binder.Tests/BindOnceTests.cs)
  cover concurrent publication and failure/no-retry; execution belongs to the
  verification owner's final gates.

No outstanding blocking defect found in this bounded review. Final evidence:
[binder-verified.json](../../validation/results/binder-verified.json) records 118/118
Go/actual Native AOT bound graphs and 122/122 binder xUnit checks, including
concurrent publication, failed Once no-retry, default-options names and repeated
binding identity. Parser/scanner regression gates also passed. No active review work.
This is bounded source review and differential evidence, not proof of every binder
input or of excluded checker, resolver, module-resolution or emit stages. No source
edits or independent builds were performed during this review.
