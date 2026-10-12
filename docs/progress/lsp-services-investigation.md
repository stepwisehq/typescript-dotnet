# Language-service dependency investigation

## Current dependency checkpoint

The current selected external packet passed both managed and actual Linux x64
Native AOT execution in [dependency23](../../validation/results/language-services-dependencies-23.json).
The combined packet has142 ordinary managed cases and8 isolated depth cases, all
passed. Its original functions, selected/adapted channels and authored controls are
separate evidence units, not interchangeable with xUnit totals. This remains a
dependency checkpoint; complete services, server transport and retained full compiler
regressions are pending.

The regexp packet has22 assertion groups:17 whole original functions and five
selected or adapted channels. One module-filter group is authored. Seven additional
NFD channels preserve canonical properties, span, insertion/decomposition, official
Unicode17 conformance and cross-form bootstrap assertions with explicit setup/API
adaptations. The [86-function disposition inventory](./lsp-standard-library-tests.json)
records consumed/matched/skipped corpus rows and excludes unused APIs explicitly.
The complete selected parser/compiler and NFA/one-pass/backtracking matching engines
are production counterparts; no approximate CLR regex substitution is used.

The earlier [initial dependency gate](../../validation/results/language-services-dependencies-initial.json)
passed73 managed cases and its own narrower Native packet. Preference serializer
inversion was a production correction; AstNav projection and public NFD stream-safe
expectation were harness corrections. Later generation/importer fixes and attempted
integration results remain in the retained logs.

Current implementation proceeds with the complete autoimport family and genuine
language-service core/host, described in [the owner note](./lsp-autoimport-core.md).
Drafts remain outside the compile glob until coherent promotion. The nine LSUtil
counterparts and focused checker/string-comparison dependencies are mapped in
[the services packet](./lsp-services-mapping.json). Upstream library API closure
expands only when actual service/transport consumers require it; unselected APIs
are not credited by the matching-only checkpoint.

## Original investigation

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


## Semantic prerequisites: source-written, integration pending

The current packet includes all pinned services.go workers, the remaining exports.go
surface alongside accepted existing declarations, all AstNav token workers and the
actual binary-search dependency. Configured hover now includes its context owner,
18 declaration-expansion workers, type-stack/cycle lifetime, structural alias/reference/
class expansion, truncation outputs and cache gating. Checker display consumers save
and restore the shared verbosity owner; ordinary emit callers retain disabled expansion.
This is source-written work, not accepted implementation or executed test evidence.

The signature-query cache suppression follows services.go:366. Go pointer-to-link
keys are adapted to the actual node/symbol reference owners of the managed inline
link slots; restoration reacquires those same slots in finally. Required query-order
and repetition assertions are being prepared for integrated verification. The original
AstNav unit function has both unchanged table rows in shared xUnit/Native checks;
its four original Go JSON baseline channels now consume the unchanged fixture and
reference files, while original JavaScript comparison channels remain pending.
Original TestMain is harness setup, not a sixth assertion function.

No local compiler build or workload has run. Roslyn syntax validation and formatting
completed for the packet; root owns the first coordinated diagnostic compilation.


AstNav retains the original per-call NodeVisitor creation and conditional wrapping
callbacks. Managed closure/delegate allocations remain in these wrappers; there is
no claim that Go function values are represented as stack-only objects, nor a new
per-query visitor reuse algorithm. Concrete borrowed symbol/property names now stay
in byte headers through SymbolTable/checker/factory consumers. Dictionary traversals
use direct value enumerators with explicit nil-map guards wherever Go permits nil
range. The original rightmost-node traversal retains its capacity-one slice owner.

## LSUtil owner continuation

The complete pinned nine-file LSUtil family is now the active implementation owner.
Preference models retain embedded value snapshots, tri-state defaults and open
string-derived preference values. Configuration dispatch preserves raw-name fields,
`unstable` overrides, original nested-path priority and editor < JavaScript <
TypeScript < js/ts precedence. Generated parsing/serialization comes from pinned
`userpreferences_generated.go` and its schema, with explicit AOT-safe dispatch.
These bodies are still being assembled; source presence is not build or acceptance.

ASI, child/token completeness, symbol-display classification, module identifier/quote
utilities and organize-import detection/sorting preserve their original algorithms.
Borrowed names remain UTF8 headers; required rune arrays and output slices remain
owned allocations. Scanner navigation uses the real source file and token cache.

Two actual service dependencies must be completed rather than substituted:

- Go 1.27.1 `regexp.Compile`/`MatchString`: pinned syntax parser, compiler/program,
  simplification and the matching-engine paths chosen by the original implementation.
  The old module-specifier exclusion helper was an explicitly scoped no-emit stub.
  Its real slash/flag parser, synchronized pattern cache, cached invalid patterns and
  Unicode/rune matching are now required. Replacement, FindAll and unrelated capture
  APIs are not required consumers; shared matching workers still must be genuine.
- `golang.org/x/text` v0.42.0 `norm.NFD.String`, including its worker/table closure and
  Unicode 17 generated material. Removing Mn after NFD does not make every difference
  unobservable: Mc musical marks with CCC216/226 survive filtering, so stream-safe
  boundaries can affect retained order. Invalid UTF8 and pinned decompositions also
  remain public behavior. Host FormD alone is not claimed equivalent.

Root authorized these proportional managed upstream dependencies. BSD notices and
per-file provenance will accompany them; no approximate regex/normalization fallback
or input exclusion will stand in for completion. Existing Unicode case tables can be
reused where their pinned semantics match. Current prerequisite query packet remains
source-reviewed with the narrow Memory link, scanner/accessibility and explicit byte
content-equality integration joins; configured-hover review and execution are pending.

The thirteen original LSUtil test functions are now source-written and unexecuted.
Preference tests preserve every original assertion and table row, with an explicit
AOT field-fill/equality helper replacing reflection. They execute sequentially in
the shared runner; Go `t.Parallel` subtest scheduling is not reproduced. These tests
assert values rather than concurrent behavior. Utility tests retain all three,
seven and five original rows, including raw UTF8 accent bytes; normalization remains
a genuine pending engine dependency.

The required NFD dependency is now being ported from x/text v0.42.0: exact
Unicode17 canonical decomposition/NFC-trie tables regenerate through
[generate-nfd-tables.py](../../tools/generate-nfd-tables.py), with source SHA retained
in generated output. Trie and packed-property owners are source-written; the actual
quick-span, reorder-buffer and stream-safe normalization workers remain in progress.
This is not a completed normalization implementation or executed comparison.

The NFD.String selected worker closure is now source-written, including exact
quick-span, segment traversal, packed properties, stream-safe CGJ insertion, fixed
embedded reorder arrays and Hangul decomposition. Nonselected NFC/NFKC composition,
Transform/Append/iterator/public-normality APIs are excluded explicitly; NFD uses
the original canonical trie and append flusher. Original byte-to-string ownership
requires the final immutable copy, which is retained. This narrows the earlier
rough whole-file estimate to the actual String call closure; source review and
original/differential assertions are still pending, and no compile/run has occurred.

Build4 owner joins are corrected as a single source packet: direct Slice enumeration,
actual ancestor/check flags and retained query/writer owners, generated module preference
constant qualification, and readonly configuration serialization. The numeric collation
private workers retain borrowed byte headers across rune advancement, zero trimming and
comparison; no substring or List conversion was added. Regex inline enumeration exposes
the standard value-enumerator pattern. Central compilation and behavior remain pending.

The pinned public NFD observer now records eight exact observations. Its whole-string
stream-safe result retains the final combining mark after CGJ; the earlier authored
expectation incorrectly reused the inner-segment prefix. That expectation is corrected,
and quick-prefix/multiple-segment plus canonical reorder observations are added. Original
20 segment assertions and production normalization algorithms are unchanged.

Central build7 compiled the accumulated production family successfully. Shared-check
feedback is being reconciled against that frozen library; no behavior has executed yet.
The next regex-only source continuation adds exact Go Unicode17 category/script/fold
and Perl/POSIX group tables with reproducible generators and raw-byte dictionary keys.
Original Mn/Lu table owners are reused. Complete parser and selected execution-engine
workers remain in draft, not replaced with CLR regular expressions.

The first managed dependency run reached70 cases:63 passed and7 failed. The two
preference failures exposed a generated serializer defect: both original inverted
inlay suppression booleans were emitted without negation. The generator and owner
are corrected without changing original assertions; narrow review/retest are pending.
All20 original NFD segment rows and8 public observations passed internally; the
wrapper expected the old count6 and is corrected to8. Four AstNav baseline failures
are under independent harness diagnosis; current service query controls passed.
