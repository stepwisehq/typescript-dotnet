# Remaining non-core checker responsibility index

Baseline `bb035b3`; source authority is `git ls-tree -r` at pinned
`fed0bf24149fb1ed36039212648bafdafc1ea10e`, restricted to `tsc/internal/checker`.
The pin contains 26 production Go files and two test files. No authored `oracle_*`
files enter this inventory. Core owns `checker.go`, `types.go` and `links.go`; the
23 other source files below are a dependency/gap index, not a new completeness audit.
No future JS/JSX implementation, runtime, build or historical inventory edit follows
from this note. The current [protocol contract](../PROTOCOLS-CONTRACT.md) is implemented
first; accepted source-family reviews remain evidence rather than work to repeat.

This index covers the 23 non-core production units. The other five pinned files are
core-owned checker.go/types.go/links.go and checker_test.go/tracer_test.go. The two
checker assertion bodies are already retained; enabled-tracer test domains remain
external. Final whole-checker accounting must join all 28 entries, including applicable
test assertions and demanded semantics in display/emit-support files. A helper folded
into an existing adapter can be represented without a same-name method; a present
method can still have unimplemented branches. This index does not decide either from
name matching.

Accepted source authority is recorded in the [declaration assessment](../DECLARATIONS-ASSESSMENT.md),
[expression assessment](../EXPRESSIONS-ASSESSMENT.md) and
[type-system inventory](../TYPESYSTEM-INVENTORY.md), including their linked independent
reviews. Those source responsibility reviews are reused rather than repeated here.

Counts below are lexical top-level function/method declarations, including generic
helpers and generated compile checks. Counts do not measure branch completion. The
historical relation inventory lists 188 definitions; including the generic
`asRecursionId` wrapper yields 189 declarations. Its C# record construction already
implements the wrapper adaptation. This accounting difference identifies no semantic
omission and does not alter frozen type-system evidence.

| Pinned file | Function declarations | Current counterpart/responsibility and remaining boundary |
| --- | ---: | --- |
| flow.go | 130 | [Flow](../../src/TypeScript.Scanner/Checker/Flow.cs): accepted synchronous family; protocol packet restores actual for-await annotation lookup. Mixed JS-specific producer callers still require next-campaign reconciliation. |
| grammarchecks.go | 78 | [GrammarChecks](../../src/TypeScript.Scanner/Checker/GrammarChecks.cs): accepted TS grammar and current async/yield/using/for-await packet; absent JSX grammar and remaining import phase/options branches belong to later language closure. |
| inference.go | 79 | [Inference](../../src/TypeScript.Scanner/Checker/Inference.cs): accepted shared TS family; future JSX/JS consumers must reuse it and reconcile any mixed branches. |
| relater.go | 189 | [Relater](../../src/TypeScript.Scanner/Checker/Relater.cs): accepted shared TS family plus represented recursion-ID wrapper; two JSX helpers and JSX elaboration/property arms remain. Enabled tracing is external. |
| mapper.go | 37 | [Mapper](../../src/TypeScript.Scanner/Checker/Mapper.cs): accepted generic/type mapper dependency; reuse it. Method presence alone is not a new whole-unit claim. |
| jsdoc.go | 2 | No separate C# file. Both helpers already reside in [Checker](../../src/TypeScript.Scanner/Checker/Checker.cs) and [Utilities](../../src/TypeScript.Scanner/Checker/Utilities.cs), selected for genuine TS library metadata. Full JS/JSDoc checking additionally lives in checker.go and AST/parser consumers; it is not a missing 100-line file alone. |
| jsx.go | 59 | Entire semantic source unit absent as a direct counterpart: element/fragment/self-closing checks, contextual attributes/children, component signatures, intrinsic/managed attributes and JSX diagnostics. Actual AST/parser/resolver metadata exists; semantic checking is unfinished. |
| exports.go | 95 | [Exports](../../src/TypeScript.Scanner/Checker/Exports.cs) plus existing public partial methods: selected actual type/symbol/query/assignability wrappers. Missing forwarding APIs must be classified by actual checking/public-query versus external emit/service use. |
| emitsupport.go | 11 | [EmitSupport](../../src/TypeScript.Scanner/Checker/EmitSupport.cs): semantic visibility/accessor/property/display dependencies selected; JSDoc implicit-undefined and other demanded JS visibility branches remain. Actual declaration emission/alias-marking-only consumers external. |
| emitresolver.go | 55 | [EmitResolver](../../src/TypeScript.Scanner/Checker/EmitResolver.cs): selected source resolver interface helpers. Emitting JS/declarations is external; semantic helper/availability diagnostics cannot be excluded merely by this file's name. |
| utilities.go | 151 | [Utilities](../../src/TypeScript.Scanner/Checker/Utilities.cs): selected source helpers across accepted TS campaigns plus current yield traversal. Remaining JS/JSDoc/JSX/module/display leaves require caller-led completion; source names alone do not establish semantic implementation. |
| nodebuilder.go | 29 | [NodeBuilder](../../src/TypeScript.Scanner/Checker/NodeBuilder.cs): actual diagnostic/public type-symbol-signature serialization contexts. Full external tracker/verbosity/declaration emit entrypoints remain separate. |
| nodebuilderimpl.go | 116 | [NodeBuilderImpl](../../src/TypeScript.Scanner/Checker/NodeBuilderImpl.cs): accepted TS type/display families. JSDoc reuse/this-parameter, package-import diagnostic and mode/attribute paths remain to classify/complete by genuine diagnostic callers. Configured hover expansion is external. |
| nodebuilderscopes.go | 4 | [NodeBuilderScopes](../../src/TypeScript.Scanner/Checker/NodeBuilderScopes.cs): selected signature/type scopes. An existing binding-pattern parameter branch still rejects a demanded display scope; include in final diagnostic consumer reconciliation if reached, without concluding accepted checking algorithms are absent. |
| nodecopy.go | 28 | [NodeCopy](../../src/TypeScript.Scanner/Checker/NodeCopy.cs): selected AST reuse/recovery paths; remaining dynamic names, return annotations, reference/signature/index and JS module rewrites have both diagnostic and emit/service callers. Preserve source fallback semantics; do not blanket classify this unit external. |
| nodebuilder_hover.go | 18 | No separate counterpart. All functions implement configured hover/class/enum expansion; service-only source responsibility external to no-emit checking. Generic diagnostic serialization remains in scope elsewhere. |
| pseudotypenodebuilder.go | 9 | [PseudoTypeNodeBuilder](../../src/TypeScript.Scanner/Checker/PseudoTypeNodeBuilder.cs): selected source pseudo-type comparison/conversion fallback. Existing accessor/assertion/tuple/signature branches need actual caller classification; pseudo syntax is not an alternative semantic solver. |
| printer.go | 27 | [Printer](../../src/TypeScript.Scanner/Checker/Printer.cs): actual type/symbol/signature/predicate diagnostics and public display, including NoErrorTruncation. Wider source-node/declaration/verbosity exports remain external unless checking requires them. |
| symbolaccessibility.go | 41 | [SymbolAccessibility](../../src/TypeScript.Scanner/Checker/SymbolAccessibility.cs): selected diagnostic accessibility/module name serialization. CommonJS class-assignment container branch remains; broader aliases/module exports and diagnostic consumers belong to the next campaigns. Emit-only visibility marking separate. |
| symboltracker.go | 14 | [SymbolTracker](../../src/TypeScript.Scanner/Checker/SymbolTracker.cs): selected actual diagnostic tracking callbacks. External configured declaration/service trackers need not be implemented merely for private representation parity. |
| services.go | 66 | [Services](../../src/TypeScript.Scanner/Checker/Services.cs) supplies actual GetExportsOfModule. Public contextual/property/module helpers may be checking-query dependencies; signature help/completions/references/hover routines are external. Avoid a blanket filename exclusion. |
| tracer.go | 38 | No direct counterpart; genuine disabled/nil-tracer entry is explicit. Enabled checker tracing is external and must never be represented as recorded-success no-op. |
| stringer_generated.go | 2 | Generated SignatureKind string formatting and compile-time enum check, without a dedicated counterpart. Existing C# enum labels differ from source debug spelling; classify actual formatter callers before deciding no-emit relevance. No generated Unicode/library data gap is implied. |

## Next language and display completion pressures

JS/JSDoc is mainly mixed responsibility in checker.go, not a large missing jsdoc.go
unit: effective annotations, constructors/this assignments and expandos, CommonJS
assignment/module merging, contextual declaration/signature inference and JS-specific
errors/suggestions. Reuse the accepted AST/JSDoc parser; identify demanded absent AST
helper branches when genuine inputs expose them. JSX has a truly absent semantic unit
and requires its flags/link models, JSX namespace/runtime metadata, intrinsic element
and component call/construct selection, managed attributes, children/spread/context
inference, grammar and relation elaboration. None is implemented by enabling old
admission lists or injecting a React declaration environment.

Module/options closure crosses genuine checker external-module resolution, aliases,
CommonJS/import-equals/export-equals/UMD, JSON, source/defer imports and import attributes,
with diagnostic package/module-name rendering. Accepted full resolver/Program tables
supply inputs; they do not imply all checker consumers are complete. Source host
metadata/options must stay authentic. Utility/display readiness follows these actual
semantic callers; general emitter, language-service hover/completion and enabled tracing
remain external. Public no-emit diagnostics cannot use those exclusions to hide a
missing builder or recovery branch.

This index reports future completion pressure, not unresolved defects in accepted
source families. Current protocol producers, grammar and delayed global/mapped
assertions are the immediate work. Final whole-checker responsibility review must also
reconcile core checker/types/links, actual test/configuration variants and demand-driven
printer/utility branches; no private-state census is required.
