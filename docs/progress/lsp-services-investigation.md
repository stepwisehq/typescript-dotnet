# Language-service dependency investigation

Read-only investigation from `9b8fa74a`, against pinned upstream
`fed0bf24149fb1ed36039212648bafdafc1ea10e`. No builds, oracle runs or production
edits. Earlier emit/checker acceptance remains intact; its language-service exclusions
are prerequisites for this new authorized epic, not new exclusions.

## Actual pinned scope

The complete [ls package](../../upstream/TypeScript/tsc/internal/ls/) contains 64
production Go files, 42,296 lines and 1,440 function declarations, including its
subpackages. Counts use `*.go`, exclude `_test.go`, and count `^func `; they are source
inventory, not acceptance coverage. Breakdown:

| Package | Production files | Lines | Functions | Test declarations |
| --- | ---: | ---: | ---: | ---: |
| ls root | 39 | 31,145 | 1,038 | 9 |
| lsutil | 9 | 3,340 | 105 | 13 |
| lsconv | 2 | 700 | 39 | 6 |
| change | 3 | 1,604 | 68 | 1 |
| autoimport | 11 | 5,507 | 190 | 13 |

The 42 test declarations include one autoimport `TestMain`: 41 test entry functions
plus package lifecycle. Preserve individual subcases/assertions and skips separately.

Real service methods cover diagnostics; hover with verbosity expansion; completion
and item resolution (including string/module/JSX and autoimports); definition, type
and source definition; references, implementations and VS references; rename,
prepare-rename information and file-rename edits; document highlights; signature
help; document symbols; semantic tokens (full/range); inlay hints; folding and
selection ranges; linked editing; formatting (document/range/on-type); automatic
insertion/JSDoc snippets; organize imports; code actions/fix-all; call hierarchy; code
lenses and resolution. Cross-project references/rename/implementations use the genuine
[CrossProjectOrchestrator](../../upstream/TypeScript/tsc/internal/ls/crossproject.go),
not merely a single Program search.

Do not equate this pin with all TypeScript JS historical features. Its
[code-action registry](../../upstream/TypeScript/tsc/internal/ls/codeactions.go)
registers import fixes, isolated-declaration fixes and incorrectly implemented
interface fixes, plus organize-import handling. A general refactor suite is not
registered here. Original TODOs (e.g. computed-name completion and code-lens type
aliases) remain upstream limitations, not permission to omit implemented branches.

## Existing C# state

No C# LanguageService, LSUtil/LSConv/change/autoimport package, AST-navigation or
formatting counterparts were found in the current source tree. These service families
are missing, not implemented because related compiler APIs exist. No current
fourslash/service test harness was found. Existing compiler Program/checker, module
resolution/specifier generation, AST/scanner, printer/transform factories, source-map
mapper, collections, VFS and diagnostic formatting are genuine reusable dependencies.
They are accepted within prior scopes; integration must add real service-facing
contracts without substituting diagnostic-only behavior.

[Checker exports](../../upstream/TypeScript/tsc/internal/checker/exports.go) has
95 functions/412 lines, of which 89 are Checker methods. A name-only scan of current
Checker counterparts finds 19 corresponding method names and 70 absent. This is a
public-surface gap, not proof that 70 underlying checker algorithms are missing:
private `getContextualType`, `getMergedSymbol`, `getSignatureFromDeclaration` and
other real workers already exist. Port the complete exports/visibility contracts and
inspect each worker dependency. Names alone do not prove parity. Important consumers
include contextual/property/scope queries, merged/root/export symbols, aliases,
call/signature resolution, JS/JSX contextual completion, accessibility checks and
literal/union/index types.

The actual [checker services counterpart](../../src/TypeScript.Scanner/Checker/Services.cs) currently contains only GetExportsOfModule, while pinned [services.go](../../upstream/TypeScript/tsc/internal/checker/services.go) has 1,149 lines of scope/property/signature/service algorithms. These are substantive missing producer bodies in addition to exports wrappers. Complete this dependency before its service consumers.

Configured NodeBuilder hover is explicitly partial: the current
[NodeBuilder](../../src/TypeScript.Scanner/Checker/NodeBuilder.cs) admits ordinary
emit requests, while [NodeBuilderImpl](../../src/TypeScript.Scanner/Checker/NodeBuilderImpl.cs)
keeps maxExpansionDepth=-1 and throws for configured `shouldExpandType`. The genuine
[nodebuilder_hover.go](../../upstream/TypeScript/tsc/internal/checker/nodebuilder_hover.go)
has 605 lines/18 functions; complete its context/options and class/interface/enum/
union expansion owners and display consumers together. Existing ordinary serialization
must remain intact. Hover symbol-display/JSDoc documentation is additional real work,
not just TypeToString plus an LSP wrapper.

LS diagnostics combine syntactic, semantic, suggestion and optional declaration
channels over main and supplemental source files, then convert ranges/related data.
Existing compiler diagnostics are inputs, not a complete service implementation.
Converters handle negotiated position encoding, URI normalization and content-mapped
original coordinates. Preserve byte text/Go offsets internally, actual UTF8/UTF16
conversion at the protocol boundary and invalid UTF8 behavior; no blanket UTF16
character-index approximation.

## Dependency-first order

1. Complete actual protocol/position interfaces in coordination with the LSP lane,
   AST navigation (1 production file/794 lines/18 functions), LS preferences/generated
   fields, token/child utilities and LS converters. Define real snapshot host contracts.
2. Complete Checker exports and service-only NodeBuilder/hover display requests;
   preserve shared checker leases, transferable release, cancellation and pooled
   context ownership. Services require genuine per-file Program metadata.
3. Complete formatting (10 production files/4,257 lines/235 functions), change tracker
   and autoimport owners (registry/index/alias resolver/view/specifiers/import adder).
   Host snapshots and autoimport preparation/index invalidation are required for
   later completion/code-action correctness. Generated preference schema and
   export-kind string material must retain provenance.
4. Integrate LanguageService plus independent structural services and diagnostics,
   then hover/signatures/navigation and references/rename/cross-project traversal;
   integrate completion+resolve and edit-producing services once their complete
   shared dependencies are ready. Parallel ownership is reasonable across these
   families; this is dependency order, not an authorization to accept partial scope.
5. Integrate the genuine project/session host and LSP dispatch with all implemented
   pinned capabilities. Dynamic edits, project membership, source-map redirects,
   supplemental files, cancellation and autoimport retry semantics must be exercised.

Material scope pressure: LS alone is ~42k production lines; formatting/AST navigation
add ~5k, before project/session/protocol owners and missing checker-facing contracts.
A fixture-limited server is not whole upstream completion. Root must retain the full
capability inventory and explicitly disposition upstream gaps, not silently omit
expensive families or cross-project behavior.

## Original test and generated-material approach

Reuse the pinned [fourslash harness](../../upstream/TypeScript/tsc/internal/fourslash/)
and generated [tests](../../upstream/TypeScript/tsc/internal/fourslash/tests/) identities.
That directory has 4,372 `_test.go` files with 4,572 `Test*` declarations including
its `TestMain` lifecycle (4,571 other declarations); these syntactic totals are not
executed/pass totals. Preserve original source inputs, marker/range parsing, options,
edit sequences, result ordering, baselines, auxiliary files, project references,
preferences and individual assertion channels. Inventory disabled/skipped tests and
harness TODOs individually; missing bundled embedding is a harness prerequisite,
not an unsupported product feature. The original formatter/ASTnav packages add seven
and six direct Test declarations respectively. Original autoimport tests include
crash/lifetime guards and registry/index ordering, not just final completion lists.

Original direct tests plus full applicable fourslash observations should share actual
assertion bodies between xUnit and Native AOT. Keep managed/AOT dispositions explicit;
protocol JSON unions/generated serializers must use genuine AOT-safe machinery.
Validate generated userpreferences against `tools/userPreferences.schema.json`
(generator header) and export stringer output against pinned enum values. Preserve
full result/null distinctions, source edits and applied edit outcomes; requests that
only return canned expected fixtures cannot qualify. No tests have been run in this
investigation, and no service acceptance is claimed.
