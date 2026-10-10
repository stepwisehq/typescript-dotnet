# Remaining language implementation packet

Preparation only, while the protocol acceptance gate is frozen. Pin remains
`fed0bf24149fb1ed36039212648bafdafc1ea10e`; no production edits, builds, protocol
inventory edits, or acceptance claims accompany this note. This uses the existing
[core index](checker-remaining-core.md) and [support index](checker-remaining-support.md),
then reads the remaining original producer chains. Accepted TS declarations,
expressions, shared types, and the independently reviewed protocol packet remain
inputs. The proposed campaign closes remaining language responsibilities together,
through coherent source packets and one campaign acceptance gate.
The prepared [language contract](../LANGUAGE-CONTRACT.md) matches this responsibility
boundary; no omitted core family or material contract mismatch was found in this
preparation. Its whole-checker handoff follows language acceptance.

## Source boundary and actual gaps

The existing index tags 199 distinct functions in pinned
[checker.go](../../upstream/TypeScript/tsc/internal/checker/checker.go) with remaining
language joins: JS/JSDoc/CommonJS 94, decorators 61, modules/attributes/JSON 50,
JSX 33. These overlap and include already implemented bodies with missing arms;
they are reconciliation worklists, not counts of absent methods or proof of
completion. The separate [jsx.go](../../upstream/TypeScript/tsc/internal/checker/jsx.go)
has 59 functions / 1,488 lines and lacks its direct semantic counterpart.
The two functions in [jsdoc.go](../../upstream/TypeScript/tsc/internal/checker/jsdoc.go)
are already represented; implementing that small file alone cannot complete JS.

| Responsibility | Original entrypoints and locations | Required closure |
| --- | --- | --- |
| JS/JSDoc checking and annotations | `checkSourceFile` 2240; comment/type checks 2585–2630; `getIntendedTypeFromJSDocTypeReference` 23544; mixed `checkSignatureDeclaration` 2769, `getSignatureFromDeclaration` 20240, type-node dispatch 23299 | Replace genuine JS rejection with original lifecycle. Restore JSDoc nullable/nonnullable/optional/variadic/function/namepath and typedef/callback/enum/template/import annotation resolution, effective this/return/parameter types, syntax diagnostics, suggestions and reference tracking. Preserve actual checkJs/noCheck/directive policy; unchecked-file queries still use real source semantics. |
| JS constructors, assignments and CommonJS | `checkConstructorDeclaredProperties` 4325; assignment type families 18466–18645; alias targets 15284; `isCommonJSRequire` 16060; synthetic default type 16032 | Complete constructor/method `this` inference, function/class expandos and property descriptors, exports/module.exports assignment aliases, require discrimination and module merging. Reuse actual binder declarations and flow nodes. Preserve source cache/cycle/error behavior rather than resolving everything as any. |
| Module forms and options | import types 3375/3386 and 25109–25207; attributes 5559–5626; dynamic import 8469; source-phase import target 14797; const-enum access 7775 | Restore import/typeof-import qualified lookup, attribute object typing/validation/context, dynamic/source-phase imports, JSON type production, CommonJS/ESM aliases and synthetic defaults. Reconcile Node formats, resolution overrides, verbatim/isolated modes, source/defer phases, ambient module attributes, and const-enum use-site/project-reference policy. Existing resolution success does not implement these checker consumers. |
| Both decorator systems | `checkDecorators` 6223; `checkDecorator` 6262; resolution 8960–9020; argument count 9408/9418; signatures/context constructors 30691–31105; metadata references 29215–29465 | Port original legacy and ES call signatures, synthetic arguments, applicability/inference/context/arity/return diagnostics, private/static/name overrides, helper availability, and metadata reference/isolated diagnostic policy. Do not execute transformations or invent decorator effects absent upstream. |
| JSX core callers | expression/deferred/signature dispatch 2547/7933/8660; call/arity/applicability/inference 9061/9332/9481/9615; alias/context families 29031/29891/30591/31235/31446 | Integrate full source JSX semantic unit with genuine namespace/runtime metadata, call and construct component selection, contextual attributes/children and relation/inference. No React environment injection or alternate JSX checker. |
| Public query and diagnostic baseline | `GetTypeOfSymbolAtLocation` 16840; `getSymbolAtLocation` 32147; `getIndexSignaturesAtLocation` 32317; `getTypeOfNode` 32483; `GetAmbientModules` 15922 | Reconcile full checking-oriented type/symbol/node/location/module queries and their lazy consumers, not only identifier fixtures. Include JSDoc links, import attributes/literals, JSX names, this/assignment narrowing and write access. Separate actual service-only operations by caller, never by whole-file exclusion. |

## Implementable sequence and owners

1. **Shared inputs and JS annotations.** Core restores JS/JSDoc dispatch and
   effective annotation/signature/name-resolution chains in
   [Checker](../../src/TypeScript.Scanner/Checker/Checker.cs), extending
   [Types](../../src/TypeScript.Scanner/Checker/Types.cs) and
   [Links](../../src/TypeScript.Scanner/Checker/Links.cs) only for actual source
   state. Support supplies demanded AST/Utilities/JSDoc grammar and display leaves
   in their existing direct counterparts. First coherent build should exercise
   actual JS annotated declarations and public queries before all language bodies
   are finished; unfinished consumers must continue explicit rejection until ported.
2. **JS assignment/CommonJS plus module forms.** Core replaces partial assignment,
   require/alias/export/JSON/import-type/dynamic-import/attribute/source-phase
   producers together. Support owns their bounded Flow/Grammar/display/helper
   dependencies. This joins JS inference with actual module metadata rather than
   leaving a separate fixed-host path. Reconcile options in each original consumer,
   including diagnostic recovery and public queries.
3. **Decorators and JSX in parallel source ownership.** Core owns complete
   decorator families and all shared Checker joins. Support owns the new one-to-one
   `Checker/Jsx.cs` counterpart, including upstream JSX enums/link models/constants,
   plus existing Grammar JSX checks, Relater JSX elaboration/excess-property rules,
   Utilities/AST/display dependencies, and JSX public forwarding in Exports. Core
   adds the JSX LinkStore and namespace/factory state and integrates bounded shared
   call/context/alias/query packets. A new direct counterpart is justified by the
   original separate file; no extra Checker feature fragments are proposed.
4. **Whole-language consumer reconciliation.** Compare remaining mixed branches
   and required public wrappers across core, support, grammar, flow, diagnostics,
   serialization and genuine Program/options. Use source-family review and genuine
   original compiler-input variants, then the single integrated campaign gate.
   Present methods and passing fixtures alone do not establish responsibility closure.

Core remains the sole live writer of Checker/Types/Links and any required
[CheckerHost](../../src/TypeScript.Scanner/Compiler/CheckerHost.cs) or
[Program](../../src/TypeScript.Scanner/Compiler/Program.cs) seams, and the sole central
production build owner. Support must hand off source-bounded shared Checker patches
as scratch packets; it never edits those files live. Support owns its listed existing
partials and JSX counterpart. AST/Utilities/Exports/helper edits have one explicit
owner; ask for transfer before touching a peer's file. Verifier owns tests, original
references, corpus, comparison, gates and native builds. No production build is part
of this frozen preparation.

## Models, dependencies and scope pressures

The genuine host already supplies source files/options, resolution caches, module
usage/implied/emit formats, source/output project references, helper specifiers and
JSX runtime imports. Source phase currently needs original synthetic alias-target
publication, not a new host callback. Decorator global resolver factories and
`SignatureLinks.decoratorSignature` already exist; restore their consumers and
original synthetic property/parameter/signature constructors. Existing this-expando
maps and constructor/static return-flow support are reusable, but callers still
contain missing JS arms. JSX needs its source-specific links/state; reuse AST JSX
nodes/pragmas and existing runtime imports, not a bespoke module resolver.

Parser/JSDoc nodes and binder CommonJS/module.exports producers are present. Their
actual helper preconditions and declarations must be checked when consuming them;
missing AST/grammar/binder behavior is a concrete dependency to port, not permission
to approximate a checker result. JSDoc effective annotations, overloads/templates,
constructor/prototype typing and CommonJS assignment cycles are the largest mixed
source pressure. Public type serialization also demands JSDoc reuse, this parameters,
module names/attributes and JS visibility branches in support files. Emission,
configured language-service hover/completion, and enabled tracing remain separate;
no-emit helper availability, metadata references, declaration visibility and complete
diagnostics remain in scope.

## Legacy adapters versus genuine shortcuts

Keep named historical profile option setup, foreign-node exception compatibility,
hostless module-format fallback and existing passive verification hooks only where
they serve meaningful historical adapters. These must not affect genuine Program
type/diagnostic algorithms. No private graph census or new compatibility observer is
required for this campaign.

The actual public assertion domain also includes the original
[type/symbol baseline walker](../../upstream/TypeScript/tsc/internal/testutil/tsbaseline/type_symbol_baseline.go)
304–466. Its AST preorder/reparsed-node rules, heritage parent queries,
TypeToTypeNode flags (NoTruncation, AllowUniqueESSymbolType,
GenerateNamesForShadowedTypeParams; IgnoreErrors/AllowUnresolvedNames and
InTypeAlias recovery), SymbolToStringEx parent/AllowAnyNodeKind convention,
declaration ordering/first-five locations, and raw source/UTF-16 line rendering
must be preserved. The existing finite identifier/TypeToString observer is useful
feedback, not a replacement for this original public traversal. Actual missing
forwarding is concrete: SymbolFormatFlags and SymbolToStringEx, shared EmitContext
NodeBuilder entry/flag propagation, dynamic-name visitor recovery and binding-pattern
parameter display scope. Support owns these display/Exports leaves; core owns actual
public query wrappers and node consumers; verifier owns walker translation/comparison.

One confirmed genuine discrepancy is
`containerSeemsToBeEmptyDomElement`: original checker.go 11891 tests that the explicit
Lib list lacks `lib.dom.d.ts`, then uses `everyContainedType` and the common DOM-name
predicate. Current Checker omits the Lib condition and substitutes a union-only local
predicate. Replace it with the original helper chain during options/diagnostic closure.
Conversely, current `markAliasReferenced` already uses actual verbatim/isolated options
despite a stale fixed-option comment, and `isSourceFileDefaultLibrary` genuinely calls
Program despite stale explicit-root prose. Classify source behavior before changing
an algorithm merely because a historical comment names a profile.

No blanket JS/query/decorator/JSX file exclusions should survive completed language
consumers. Every remaining unsupported site must identify an actual responsibility
outside no-emit checking or a concrete unfinished source dependency. Historical
proofs and fixtures are immutable; newly completed authored negative controls require
explicit source-backed positive successors, not changed original assertions.
