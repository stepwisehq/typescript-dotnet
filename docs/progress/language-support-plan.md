# Next language campaign: JSX support plan

Prepared 2026-10-10 while the protocol final gate runs. Source authority is pinned
`fed0bf24149fb1ed36039212648bafdafc1ea10e`. This is a documentation-only ownership and
dependency proposal; no production edit, build, runtime replay or change to frozen
protocol assets follows from it. The [remaining source index](checker-remaining-support.md)
and accepted reviews are reused rather than audited again.

## Complete source boundary

Port the whole [jsx.go](../../upstream/TypeScript/tsc/internal/checker/jsx.go) unit:
59 function declarations, 1,488 lines, including its source models and constants.
The boundary is all JSX checking/public-query responsibilities, with actual options,
libraries, local/global namespaces and Program module metadata. It is not defined by
React fixtures or a selected list of tags.

| Source family | Required behavior |
| --- | --- |
| Element, self-closing, fragment, attributes and expression checks | Original immediate/deferred scheduling, closing-tag resolution, grammar, missing JSX option/namespace errors, deprecated signatures, component/return bounds and `JSX.ElementType` validation. |
| Context, inference and overload selection | Contextual attributes/child positions, union discrimination, generic component inference, call/construct preference and composite signatures, explicit type arguments, intrinsic synthetic signatures, factory arity diagnostics and related declaration locations. |
| Attribute/children construction | Boolean shorthand, expression/string values, fresh JSX object types, ordered spread/any/invalid intersections, duplicate spread diagnostics, intra-expression inference, explicit children conflicts, tuple/array contextual children and exact original synthetic property ownership. |
| Relation elaboration | Attribute locations and suggestions, hyphenated names, semantic children versus trivia/empty expressions, single/multiple child arity, tuple/array/Iterable children, exact optional-property diagnostics, text-specific messages, chains and diagnostic-output ordering. |
| Namespace/factory/runtime lookup | Intrinsic named/indexed/template keys, class props and intrinsic class attributes, managed alias/interface defaults, local pragmas/config factories/fragments, `null` fragment factory, automatic runtime import containers, first-tag diagnostic location and global JSX fallback. |
| Public semantic queries | Source type/symbol/contextual query arms for tags/attributes/expressions and actual exported namespace/fragment-factory getters; imported factory reference/unused tracking remains genuine. |

All source branches in this unit remain part of the proposal, including `IsInJSFile`
generic-default decisions. Checked `.jsx` bodies require the parallel JS/JSDoc closure;
they are not permanently excluded from whole-checker completion. JSX transform options
still affect semantic errors and runtime-import selection even though emitting JavaScript
and calling a UI runtime remain separate operations.

## Proposed disjoint ownership

Support owns a new direct counterpart `Checker/Jsx.cs` for the complete source unit,
including `JsxFlags`, `JsxReferenceKind`, `JsxElementLinks`, `JsxElaborationElement`,
`JsxNames` and `ReactNames`. This is a source-file counterpart, not a new alternate
checker or generated production fragment. Support also owns demanded branches in
existing [GrammarChecks](../../src/TypeScript.Scanner/Checker/GrammarChecks.cs),
[Relater](../../src/TypeScript.Scanner/Checker/Relater.cs),
[Utilities](../../src/TypeScript.Scanner/Checker/Utilities.cs), and finite public
JSX wrappers in [Exports](../../src/TypeScript.Scanner/Checker/Exports.cs), subject
to the core handshake. Required AST/display leaves stay caller-led and are assigned
before editing shared files.

Core remains sole writer of [Checker](../../src/TypeScript.Scanner/Checker/Checker.cs),
[Types](../../src/TypeScript.Scanner/Checker/Types.cs) and
[Links](../../src/TypeScript.Scanner/Checker/Links.cs), and owns JS/JSDoc, decorators,
modules and options. JSX joins in those files should be integrated as bounded original
method/branch packets, with support providing API names and independently reviewing
the saved joins. Core remains the sole production build owner.

The first support packet can translate the unit's models, namespace/intrinsic lookup,
attributes and contextual helpers. The next completes signature/element/deferred checks
and relation elaboration. These are staging packets, not narrower acceptance boundaries;
the full source unit and all core consumers must close together.

## Exact joins and existing dependencies

Core must add the source `jsxElementLinks` store and `_jsxNamespace`/
`_jsxFactoryEntity` state. Existing [SourceFileLinks](../../src/TypeScript.Scanner/Checker/Types.cs)
already stores local factory/fragment namespaces, nodes and fragment type; Checker
already creates its empty JSX object intrinsics. Retain actual cache/sentinel semantics,
including `unknownSymbol` negative namespace/module observations and per-file first-tag
ownership, without new private-state probes.

Restore original JSX branches in `checkExpressionWorker`, `checkDeferredNode`,
`resolveSignature`, `resolveCall`, `hasCorrectArity`, `isSignatureApplicable`,
`inferTypeArguments`, `getEffectiveCallArguments`, `resolveUntypedCall` and overload
error reporting. Contextual joins include `getContextNode`, `getContextualType`,
`getContextualTypeForArgumentAtIndex`, `getApparentTypeOfContextualType`,
`ObjectLiteralDiscriminator.matches`, `isPossiblyDiscriminantValue` and
`isContextSensitive`. Variable-like `JsxAttribute`, type/symbol query, truthiness/
side-effect and JSX alias/reference arms must preserve source behavior, not merely
remove unsupported guards. Port the actual `markJsxAliasReferenced` source family and
its `markLinkedReferences` caller, retaining factory scope diagnostics and unused imports.

Support's grammar closure is the three original methods `checkGrammarJsxElement`,
`checkGrammarJsxName` and `checkGrammarJsxExpression`. Relater needs actual hyphenated/
ignored JSX property helpers, known/excess-property handling, JSX attribute relation
elaboration and intrinsic-attribute intersection error handling. Existing broad JSX
payload guards must be replaced with their original algorithm branches. Shared TS
inference, mapped/conditional/default types, spread, tuple and Iterable algorithms are
already present and should be reused; concrete missing callers are repaired, not forked.

The [Program](../../src/TypeScript.Scanner/Compiler/Program.cs) and
[checker host](../../src/TypeScript.Scanner/Compiler/CheckerHost.cs) already expose
`GetJSXRuntimeImportSpecifier`, from actual loader tables. Reuse it for `react-jsx`/
`react-jsxdev` and pragma/config import sources, then genuine external-module lookup.
Do not manufacture package symbols, inject implicit declarations or add a JSX resolver.
Existing option parsing/Program validation supplies actual JSX modes and factory options.

The [AST helpers](../../src/TypeScript.Scanner/Ast/Utilities.cs) already provide semantic
JSX children, pragmas, factory-root identifiers and implicit runtime metadata; the
[scanner helper](../../src/TypeScript.Scanner/Scanner/Utilities.cs) supplies intrinsic
tag classification. [ParseIsolatedEntityName](../../src/TypeScript.Scanner/Parser/Parser.cs)
has a byte-array entry: factories/pragmas contain Go-byte strings and must use that role
once. Preserve raw names, source spans and diagnostic arguments; normal .NET culture
comparison/casing or a second Unicode encoding is not a source adaptation.

## Evidence and adjacent scope pressure

Verifier should retain genuine original JSX fixture bytes/settings and their actual
declaration environments. Pinned [React declarations](../../upstream/TypeScript/tsc/testdata/tests/lib/react18/)
and [custom JSX fixtures](../../upstream/TypeScript/tsc/testdata/tests/cases/conformance/jsx/)
provide real alternatives; authored application-specific JSX namespaces may test a
custom factory, but are not replacements for React or bundled standard libraries.
Preserve missing namespace/runtime diagnostics when the actual environment lacks them.

Meaningful public comparisons cover named/indexed/namespaced tags; generic/overloaded
function and class/union components; managed/default attributes; spreads/children and
diagnostic locations; classic/preserve/automatic/dev modes; fragments/pragmas/module
failure locations; noUnused and strict/exact-optional/noImplicitAny policies. Full
diagnostics and existing public type/symbol/signature/property observations suffice;
no incidental cache/identity census is proposed.

Material joins to coordinate are checked JS/JSX annotation inference, automatic runtime
module/alias/format semantics, and diagnostic builder recovery for JSX error types.
These are actual source consumers in the next whole-language campaign. Emit-only factory
transformation/output baselines and language-service rename/hover are separate operations;
source checking still performs closing-tag resolution and demanded public getters even
when a comment mentions a service use. Any newly demanded parser/AST/display branch
should be assessed at its actual caller before widening work beyond this packet.

## Independent whole no-emit plan review

Compared the [verification plan](checker-whole-verification-plan.md) with original
`newCompilerTest`/`compileFilesWithHost` and the complete
[type/symbol walker](../../upstream/TypeScript/tsc/internal/testutil/tsbaseline/type_symbol_baseline.go).
The original preparation, actual pre-check trace clone, distinct checking channels,
conditional suggestions, full loaded input/other-file walk and explicitly external
declaration/Emit-induced operations are accounted for. The new
[L1–L6 contract](../LANGUAGE-CONTRACT.md) includes these demanded checking/query/display
dependencies without claiming whole-inventory execution from a finite language corpus.
No material contract omission was identified.

One plan wording correction was sent to verification: import/export-name and intrinsic
JSX conditions affect the `any` display shortcut; they do not exclude those nodes from
the original walker. Preserve the exact reparsed preorder/type-node/omitted-expression
filters and heritage parent substitution. Original declaration order/first-five limit,
default-library `Decl(--, --)` markers, actual byte source text and UTF16 locations,
headers/CRLF/path-prefix fixups and baseline quirks must remain source-qualified. The
no-emit `hasErrorBaseline` policy is an explicit adaptation, not output-error substitution.

Concrete next-campaign dependencies were handed directly to core and verification:

- Source public `SymbolToStringEx(symbol, enclosingDeclaration, meaning, flags)` and
  `SymbolFormatFlags` are absent. The walker uses the actual parent, `SymbolFlags.None`
  and `AllowAnyNodeKind`; today's default public `SymbolToString` cannot replace them.
- Source `NewNodeBuilder(checker, EmitContext)` and `TypeToTypeNode` with explicit flags,
  internal flags and nil tracker must share the printer's real context. The current
  internal wrapper cannot pass `AllowUnresolvedNames` or a supplied context. Existing
  NoTruncation/unique-symbol/shadowed-parameter/InTypeAlias flags can be reused, including
  the original alias retry; a `TypeToString` shortcut would change that source policy.
- Original `getExistingNodeTreeVisitor`'s dynamic computed-name branch consumes
  `AllowUnresolvedNames`; the current dynamic-name rejection lacks that branch.
  `enterNewScope` still rejects binding-pattern parameters. Both are actual full-walker
  display consumers, not automatically external declaration-output work.
- Program's per-file checker acquisition and type/symbol queries, AST walker selectors,
  internal-name escaping and scanner source/line/UTF16 helpers already exist. Preserve
  the raw-byte versus public Unicode roles, and reconcile the genuine JS public-query
  restriction under L1 rather than bypassing it in a test harness.

This was finite source review of the plan and its direct dependencies. It added no
observer, inventory, production edit, build or runtime replay, and changed no current
frozen protocol asset. Implementation ownership remains the next-campaign handshake.
