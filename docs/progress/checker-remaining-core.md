# Remaining core checker responsibility index

Baseline `bb035b3`; unchanged pin `fed0bf24149fb1ed36039212648bafdafc1ea10e`.
This read-only index complements the [support index](checker-remaining-support.md).
It covers the three core-owned pinned production units. Their complete lexical
function, named-model and top-level declaration-group index is in
[checker-remaining-core-inventory.json](checker-remaining-core-inventory.json).
Every entry has a represented, adapter, accepted-family input, or explicitly unresolved
disposition. Lexical name correspondence is triage, not branch completion; unresolved
entries can be folded wrappers or genuine omissions. No private runtime census,
production change, acceptance claim or historical inventory rewrite follows from it.

The original [checker](../../upstream/TypeScript/tsc/internal/checker/checker.go) has
1,514 lexical function declarations; [types](../../upstream/TypeScript/tsc/internal/checker/types.go)
has 117; [links](../../upstream/TypeScript/tsc/internal/checker/links.go) has six.
Receiver overloads are distinct source entries. These counts establish file coverage
only. The original Git file set excludes authored oracle source. Prior declaration,
expression and shared type-system reviews remain inputs, with the current protocol
packet independently reviewed and compiled separately from pending runtime acceptance.

## Remaining semantic families

| Family | Concrete original producers / present partial callers | Disposition |
| --- | --- | --- |
| JS/JSDoc | `checkJSDocComments`, `checkJSDocComment`, `resolveJSDocMemberName`, `checkJSDocType`, `getIntendedTypeFromJSDocTypeReference`; mixed signature/annotation/this/constructor/expando producers and CommonJS assignment alias paths | Remaining semantic responsibility. Accepted TS library metadata helpers are reused; real JS checking must replace the current JS consumer rejection and shortcuts. |
| Module and options | `checkImportType`, `getTypeFromImportTypeNode`, `resolveImportSymbolType`, `getIdentifierChain`, `getResolutionModeOverride`, `checkImportAttributes`, `checkImportAttributesExpression`, `getContextualImportAttributeType`, `checkImportCallExpression`; current source-phase, attributes, JSON, require and synthetic-default branches | Remaining semantic responsibility. Genuine Program/resolver metadata supplies inputs; no fixture option overrides or successful empty callbacks. |
| Decorators | `checkDecorator`, `resolveDecorator`, argument counting, legacy/ES decorator signatures, decorator context/result constructors and alias/metadata references | Remaining semantic/resolution/diagnostic responsibility. Executable emission and enabled tracing remain separate boundaries; a helper's emit-related name alone is insufficient exclusion. |
| JSX joins | Contextual expressions/attributes, apparent contextual types, signature/arity/effective arguments, alias references and shared inference callers in core | Remaining caller responsibility linked to support's absent original JSX semantic unit. No injected React environment or enabling old admission lists. |
| Diagnostic/query closure | Primitive-export error helper, const-enum access policy, module-literal resolution/public ambient-module queries, exact public type/symbol forwarding and demanded index queries | Explicit unresolved source/caller classification. Some missing names are direct inline wrappers, others are real option/diagnostic omissions. Complete diagnostics and public queries, rather than method-name counts, decide the needed implementation. |

`checkConstEnumAccess` is a concrete absent source checking family: use-site restrictions,
isolated/verbatim-module policy and project-reference preserve-const-enum options are
not accounted for merely by existing const-enum value/type computation. The original
primitive-export error helper is likewise absent by name and requires caller-led
review. Conversely, `checkParenthesizedExpression` is already a direct dispatch
forwarder; generic `instantiateList<T>` and modeled cache-key zero operations are
represented adaptations. These distinctions must survive the final reconciliation.

## Models, globals and authored boundaries

Shared Type/Signature/predicate/index/tuple/mapped/conditional/string payloads, links,
flags, options and caches largely have current counterparts. The source feature-target
table was restored in the protocol packet. TypeId/SignatureId are represented by uint;
Go payload embedding/pointer arenas use documented managed payload/reference adapters.
The original six node/symbol Get/Has/TryGet operations are present in
[Links](../../src/TypeScript.Scanner/Checker/Links.cs); allocated node struct slots and
symbol arena allocation remain distinct. Final review should inspect demanded model
consumers, not require private allocation or storage topology equality.

The current complete source exception-site list is included in the static inventory.
It separates named historical profile rest/ownership adapters from genuine Program
consumers. Existing local construction/profile helpers, passive verification hooks,
managed identity ownership checks, integer/slice/byte compatibility, nil-tracer
initialization and arena adapters are C# boundaries, not replacement source solvers.
`containerSeemsToBeEmptyDomElement`, collapsed wrappers and source-name diagnostics
are explicitly future source-reconciliation points where matching a name is insufficient.
Legacy compatibility remains only where needed for intended historical assertions;
no genuine-core shortcut is accepted merely because an old profile used it.

No future production edits were made while preparing this index. Current protocol
behavioral findings take priority; its accepted checkpoint precedes subsequent campaigns.
