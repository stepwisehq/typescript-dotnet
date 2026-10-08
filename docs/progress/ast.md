# AST and parser support checkpoint

Pin: Microsoft/TypeScript fed0bf24149fb1ed36039212648bafdafc1ea10e,
tsc/internal/ast. Scanner checkpoint0a50e67. Owner: support agent.

## Current implementation and verification

All353 kinds/aliases and canonical stringer names preserve token IDs. Generated
AstGenerated has190 syntax payloads,192 New factories, Update/Clone methods,
ordered traversal and typed SyntaxFields. NodeFlags/ModifierFlags are complete.
Ast.cs supplies tagged Node, flattened base interfaces, NodeList/ModifierList,
source-file parse metadata/diagnostics/JSDoc caches, hooks and generic accessors.
Utilities is the exact parser-required syntax closure; Precedence ports binary
precedence. Visitor/Deepclone support reparsing. ParseOptions module detection
preserves Go JSX pruning through a private single-bit fact projection.
Diagnostics metadata includes parser messages/report flags. Core helpers and
ECMAScript line starts now reside in their original Core.cs/Text.cs counterparts.

Central build passed; tiny valid/recovery structure matches. Verifier reports
458 managed comparisons pass (latest root report), including JSX module
options, JSDoc/reparse, upstream files and parser boundary cases; all186
parser-reachable factory payloads are exercised.
Fixed nil list construction, zero NodeList literals vs undefined factory spans,
and Go shallowest-depth method promotion in the JSX projector. Added exact
IsJSDoc/Contains/null-file JSDoc behavior and nullable lazy callback caching.
Final LF formatting complete; duplicated Core consolidation block removed.

## Explicit boundaries / regeneration

No symbols/flow/binder/checker/content-mapper/service state. SyntheticExpression
(any checker Type) is excluded entirely. Public general SubtreeFacts excluded.
Private JSX projection keeps all AST-shape/ambient conditions and original
Composite caching; all non-JSX fact bits project to zero. The discarded
BinaryExpression branch solely adds object-rest facts, so cannot change JSX.
Go embedded bases become interfaces; managed new replaces arenas. Internal
text remains Latin1 byte strings; NodeList null/empty/missing remain distinct.

Regenerate schema with python3 tools/generate-ast.py; build/run
 tools/generate-ast-functions.go then python3 tools/fix-ast-functions.py;
 build/run tools/generate-ast-accessors.go (replaces only Ast.cs marker suffix);
 build/run tools/generate-jsx-projection.go LAST (appends mapped source markers).
Then format owned outputs with tools/CSharpFormat. Do not overwrite manual AST
or diagnostics work. NodeFlags/ModifierFlags currently have manual numeric ports.

Independent parser/runtime/emitter review complete in review-parser.md.
Support edits frozen; final gates passed:464parser xUnit,460actual Native AOT
structural cases, scanner regressions. Source hash and acceptance limits are in
../STATE.md and ../../validation/results/parser-verified.json. No pending work.
