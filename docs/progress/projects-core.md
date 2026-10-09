# Bounded relative ES-module projects: core investigation

Status: bounded [project contract](../PROJECTS-CONTRACT.md) accepted after independent
review and the final frozen Linux x64 Native AOT gate. The
[proof](../../validation/results/projects-verified.json) records source `355b4842…f953`,
48 exact native projects, shared 27, order 30 per runtime, ordinary CLI 10 and 23,542
passing xUnit tests with zero skips. Baseline `2598ceb` fixtures/proofs are preserved.
All actual compiler invocations and historical intermediate findings are recorded
below; their pending statements describe those earlier snapshots, not current status.

## Recommended boundary

Use one checker over the three exact ES5/decorator roots plus all discovered user
`.ts` files, with Bundler module resolution and ESNext modules/target. Keep Collections
strict flags and skipLibCheck. A bounded authored `tscheck.json` can specify roots and
fixed options without claiming full tsconfig/package resolution. The host owns actual
relative resolution, deterministic source order and root containment; module-local
symbols must never be merged into globals. Support notes that the pinned Program
rejects the removed Classic/Node10 modes, making Bundler preferable.

Include named/default/namespace and side-effect imports; declaration exports, local
and remote named exports, default expressions and named/anonymous default functions
and interfaces; type-only import/export forms; star and namespace reexports; string
import/export names; cyclic modules, alias cycles and star ambiguities. Cross-file
functions, generic interfaces/aliases, contextual callbacks and collection calls
reuse the real accepted algorithms after alias resolution. ESNext permits string
export names through checkModuleExportName, without a separate expression capability.

Retain source exclusions: import-equals/export-equals/CommonJS, dynamic import,
import.meta, import-type/type-query syntax, attributes/assertions, ambient modules and
augmentations, packages/node_modules/Node resolution, JS/JSX/JSON/declaration inputs,
classes/new, and other accepted body/operator exclusions. Internal printer-generated
import types and module names remain required when original diagnostics/display reach
them. Anonymous default declarations must use original binder/module symbols and
checker null-name handling, not a fabricated local identifier.

## Concrete existing limits

[Checker initialization](../../src/TypeScript.Scanner/Checker/Checker.cs) already
binds an ordered file list but rejects every external module and merges every accepted
script's locals into globals. `GetDiagnostics` and checkSourceFile are still tied to
one sourceFile. Public node-query guards likewise authorize only that AST; symbol
ownership already accepts declarations in the actual file set. The resolved-rest
capability guard specifically tests the single user sourceFile and must cover every
project user file without treating libraries as source-admitted code.

Alias paths deliberately throw in getSymbol, getTypeOfSymbol, resolveAlias,
resolveSymbolEx, resolveEntityName/resolveQualifiedName, type-only value-use callbacks,
getDeclarationOfAliasSymbol, module exports and spelling suggestions. Source-element
dispatch lacks import/export forms. ModuleSymbolLinks already has resolvedExports,
typeOnlyExportStarMap and exportsChecked; a real logical AliasSymbolLinks store and
its aliasTarget/immediateTarget/typeOnlyDeclaration/referenced state are still needed.
AliasTarget exists in the resolution-property enum but its active cycle branch is
not implemented. Do not replace unknown/circular targets with successful any aliases.

The selected [Program helper](../../src/TypeScript.Scanner/Compiler/Program.cs)
currently composes bind/check diagnostics only by calling no-argument GetDiagnostics.
A project must invoke per-file checking on the same checker, retain global/options/
resolution diagnostics separately, and sort/deduplicate each genuine Program channel.
It must check all discovered user modules, including an invalid dependency that is
not queried from the entry file. Replacing one checker with independent per-file
checkers would lose module and cross-file generic identity.

## Required original algorithm families

All anchors below refer to the pinned
[checker.go](../../upstream/TypeScript/tsc/internal/checker/checker.go).
This is a finite dependency-family plan, not a claim that every branch is required.

| Family | Original entry points | Required behavior |
| --- | --- | --- |
| File scope and checking | initializeChecker 1309; checkSourceFile 2240; getDiagnostics 14230 | Bind once for the complete actual source set; merge scripts only; module export checking and source unused-registration state; per-file grammar/deferred work and diagnostics. |
| Import/export checking | checkImportDeclaration 5420; checkExternalImportOrExportDeclaration 5489; checkImportBinding 5527; checkModuleExportName 5539; checkExportDeclaration 5675; checkExportSpecifier 5738; checkExportAssignment 5770; checkExternalModuleExports 5880; checkAliasSymbol 6937 | Real binding conflicts, missing/default/nonexported-member errors, module placement grammar, default expression checks and duplicate exports; keep off-profile emit/helper branches visibly bounded. |
| Alias target selection | getTargetOfImportClause 14797; getTargetOfModuleDefault 14816; resolveExportByName 14904; getTargetOfNamespaceImport/Export 14917/14925; getTargetOfImportSpecifier 14936; getExternalModuleMember 14956; getExportOfModule 15083; getTargetOfExportSpecifier 15245; getTargetOfExportAssignment 15270; getTargetOfAliasDeclaration 16122 | Distinguish local, immediate and final targets; resolve default and namespace meanings; preserve source declaration and alias identities. |
| Host-mediated module lookup | resolveExternalModuleName/Worker 15395/15416; resolveExternalModule 15453; resolveExternalModuleSymbol 15942; resolveESModuleSymbol 15954 | Consume real host resolved records/files/modes. Missing modules and script-as-module errors remain diagnostics; no manual export tables or spelling-specific fallback. |
| Export closure | getExportsOfModule 16517; getExportsOfModuleWorker 16534; extendExportSymbols 16621 | Original visited-symbol DFS permits export-star cycles, preserves original exports and type-only propagation, skips default during star extension, reports ambiguous names unless explicitly overridden. |
| Alias cycles/type/value/reference state | getTypeOnlyAliasDeclaration/Ex 2177/2187; getImmediateAliasedSymbol 2199; resolveAlias 16652; resolveIndirectionAlias 16679; tryResolveAlias 16689; getSymbolFlagsEx; getDeclarationOfAliasSymbol 16783; getTypeOfAlias 19005; getDeclaredTypeOfAlias 24628; markAliasReferenced/markAliasSymbolAsReferenced/markExportAsReferenced 29331/29353/29374 | Resolution-stack cycle detection, unknown sentinel, type-only source propagation, value-versus-declared-type distinction, and real reference flags even without emit. |
| Exported declarations and local resolution | checkExportsOnMergedDeclarations 7113; getDeclarationSpaces 7163; onSuccessfullyResolvedSymbol; getSymbol/getSymbolFlags/resolveEntityName | Local/export merged symbols, alias meanings, imported-binding assignment diagnostics, qualified namespace reads/types, misspelling suggestions and type-only value errors. |
| Diagnostic type display | symbolaccessibility.go; nodebuilderimpl.go symbolToTypeNode 650 onward, getSpecifierForModuleSymbol and symbol-chain helpers; nodecopy.go; printer.go | Original accessible alias chains, qualified names and generated relative import-type display; demanded type formatting must not throw merely because its declaration is external. |

Grammar closure is small and explicit: grammarchecks.go checkGrammarExportDeclaration
199, checkGrammarModuleElementContext 206, checkGrammarImportClause 2100 and
checkGrammarTypeOnlyNamedImportsOrExports 2155, plus existing modifier/source checks.
Source attributes/dynamic imports do not authorize their unrelated grammar families.

## Genuine cache and host pressure

Module links and alias links are newly reachable persistent logical stores. Capture
actual local/export/immediate/final symbol relationships, nil/allocated-empty tables,
resolved export ordering, type-only star source nodes, reference flags and repeat/cycle
state. Existing generic signature/reference/alias/type caches remain shared across files;
file-qualified declaration owners and diagnostic related-information paths are mandatory.

Namespace imports enter resolveESModuleSymbol even for pure TS ESM. If a module has a
default member, the original getTypeWithSyntheticDefaultImportType 16032 can publish
SyntheticType with value equal to its original type: canHaveSyntheticDefault 15112
returns false for ordinary TS without export-equals. That cache side effect is real;
an unconditional synthetic-module guard or fabricated wrapper would be incorrect.
No CommonJS spread/default-wrapper machinery is justified merely by this path.

The original checker Program interface (554) expects resolved-module lookup, source
lookup, usage/default format, default-library classification and module-specifier
host services. The bounded implementation can provide fixed absent project/package/
JSX/helper state, but must preserve the selected actual resolved records and relative
specifier generation. Diagnostic printers may synthesize import("./model").Row when
no accessible alias exists; source import-type exclusion cannot suppress that internal
path. No new class/library environment is presently required by the proposed module
boundary. Host/path/options/diagnostic correctness is material work of its own.

## Ownership and integration milestones

- Core: Checker.cs, Exports.cs, GrammarChecks.cs, Utilities.cs; logical alias model/
  stores in Types.cs; narrow demanded NameResolver, SymbolAccessibility, EmitResolver,
  EmitSupport, NodeBuilder/NodeCopy and Printer changes. Preserve Inference/Relater/
  Mapper unless an actual module dependency requires a source repair. Core alone
  performs shared-library builds.
- Support: project public API/config/disk host, relative resolver/path closure,
  Compiler/Program.cs and Core compiler options. Agree actual host interface and
  resolved record before integrating checker lookups.
- Verification: admission/profile/public creation handshake, project original oracle,
  file-qualified protocol/observations/corpus/tests and independent source review;
  narrow Flow changes only for a concrete module producer.

1. Agree host/options/file ownership APIs, then integrate complete initialization,
   lookup and named alias family together. First runnable vertical is two user files:
   exported generic identity/record type, inferred and explicit imported calls,
   dependency diagnostics and module-local name isolation on one checker.
2. Close default/namespace/string-name/type-only forms, imported binding errors,
   per-file deferred work and genuine module/type display, then run focused positives
   and exact invalid controls.
3. Close reexport/star/namespace chains, alias and export-star cycles, duplicate/ambiguous
   export precedence, cross-file generic/recursive/contextual interactions and wrong
   value-use diagnostics. Port the newly applicable original alias tests honestly.
4. Inventory new retained module/alias/accessibility key producers before source freeze;
   reuse accepted collection readers without forcing whole-library member types.
   Perform independent host/admission/core/formatter review, then one comprehensive
   retained regression plus actual project Native AOT gate.

The preceding section records the completed read-only assessment. Implementation
was subsequently authorized under the fixed contract; current work is recorded below.


## First integration checkpoint

The host handshake is now fixed: support supplies
[ICheckerProgramHost](../../src/TypeScript.Scanner/Compiler/CheckerHost.cs), exact
[ResolvedModule](../../src/TypeScript.Scanner/Module/Types.cs) records and public
Project.Load/Check. Lookup keeps the original importing-file/reference-string/mode
shape. Core's CreateForProgram constructs one checker over the supplied complete file
list and uses the host's real options; prior Create/Collections behavior is retained.

The first source batch established: exact AliasSymbolLinks and logical
store, original alias/type-only/immediate/reference resolution methods, AliasTarget
stack detection, per-file GetDiagnostics/checkSourceFile, project user node ownership,
source module locals excluded from globals, actual module value typing, and resolved
rest capability checks over every project user implementation. The required original
Core.FindLast helper has captured backward slice length and Go zero-value behavior.
The private sourceFile remains the old no-argument API's primary user file, not a
mutable active-file substitution. Grammar source-file checks receive the actual file.
The next family is now integrated and LF/Roslyn formatted: named/default/namespace
import targets; local/string/reexport targets; module value wrapping and synthetic-type
cache publication; original alias/cycle/type-only propagation; complete export-star DFS
with collision/type-only bookkeeping; source import/export checks and declaration-space
merging; reference marks; module-name host lookup and real missing/script diagnostics.
Core.Find now retains captured forward length and Go zero values. Resolution calls the
actual host record and ModuleUtilities.GetResolutionDiagnostic. Prior collection
metadata scopes remain Collections-only; Projects has distinct capability state.

The first grouped Debug library compiler pass is running over this family plus
support's real FileLoader/FilesParser/resolver/Program/options and verification's
admission/static alias/module bridge. This is a dependency integration checkpoint,
not runtime acceptance. Remaining selected printer/accessibility and contextual module
query guards will be closed from concrete source/runtime findings; off-profile
CommonJS, source-phase, ambient/augmentation, JS, isolated/verbatim-module and emit
branches stay explicit. Current selected markLinkedReferences hints are real; its
speculative emitter Unspecified walk has no admitted semantic/public producer.

Next action: group compiler findings by owner, close the first runnable two-file
generic vertical, then independently review host/resolver original precedence,
missing-versus-unsupported winners, source-task identity/order and noEmit diagnostics.
The old no-argument checker API's primary file remains fixed; all project checking
uses actual per-file state in one shared checker.


## First runnable compiler snapshot

All compiler invocations are recorded separately: central 1 failed in 2.02 seconds
with two duplicate-declaration errors and zero warnings; central 1b failed in 4.91
seconds with 29 core-only body-dependency errors and zero warnings. Those errors
were source enum/API/nil-map adaptations, the real export-origin target field, and
explicit closure of excluded emit/CommonJS branches. No support/admission errors
were reported. Central 2 Debug completed in 5.73 seconds with zero warnings/errors.
Its immutable library copy is under artifacts/projects-validation/build2/library.

Source review also exposed the prior primary-file collision shortcut. It is now
replaced with original per-node collision worker/name/ambient/target predicates
and actual host format queries; NoEmit markers retain their real diagnostic flag.
Existing Core.Find was upgraded in place, not duplicated, and support authored a
narrow original-Go/managed control for captured range length and Go-zero string
behavior. Ast.GetSourceFileOfModule/GetNonAugmentationDeclaration are tiny demanded
original utilities helpers, preserving actual declarations rather than file names.

This is compile readiness only. Verification next runs the isolated genuine
two-file generic import vertical, then family/runtime closure; core independently
reviews the now-stable host/resolver/Program/config source and selected guard
producers. No comprehensive gate or runtime acceptance has occurred.


## Module service/query snapshot

Central 3 Debug completed in 6.00 seconds with zero warnings/errors. It includes
the source-mapped Services.GetExportsOfModule list projection (services.go:131),
original declaration/module/import-property symbol query branches and alias spelling
suggestions, verification's full export-origin target/import fields, and support's
restored LoadModuleFromFileNoImplicitExtensions/dotted-candidate source boundaries.
The subsequent central 3b adds the separately requested real GetAliasedSymbol
(checker.go:32720) wrapper: getTargetSymbol is instantiated-symbol lookup and must
never stand in for alias ultimate resolution. Central 3b completed in 5.54 seconds with zero warnings/errors, with immutable
copy artifacts/projects-validation/build3b/library. This is a separate compiler run;
no runtime result is claimed in this checkpoint.

Root explicitly transferred SymbolAccessibility.cs exclusively to support for real
alias-candidate/qualification/module-container closure. Core retains remaining
NodeBuilder/NodeCopy/Printer families. Host review confirms selected WorkGroup LIFO
execution and visited-before-child postorder, actual per-file noEmit marker filtering,
and identity-based host lookups. The duplicate-root question is configuration-source
dependent: original config parsing deduplicates normalized roots before file loading;
support/verifier will compare the actual strict raw config input before treating
pre-task normalized-root reduction as a defect. No root behavior was changed on an
unsupported assumption.


### First actual runtime and combined formatter closure

The immutable central 3b adapter compared 11 initial requests with genuine original
Program outputs: 1 exact (normalized duplicate roots), 10 failed. Nine failures share
`checkExternalModuleExports` indexing a nil module export map, including the first
2-file generic named-import vertical. The selected original read now uses a nullable
lookup; absent `Exports` remains absent. The invalid grouped request additionally
reached nameless module symbol display through `errorNoModuleMemberSymbol`.

The in-progress source batch restores the original declared/default/name-type display,
alias target serialization, qualified symbol/expression chains, per-symbol/file/mode
specifier cache and internal import-type output. `ContainingSymbolLinks` now retains
both cross-file cache fields, using a `LinkCell<List<Symbol>>` to preserve absent pointer
versus pointer to nil slice. Module import-attribute type lookup retains its genuine
Symbol-to-Type cache and empty-object result for SourceFile modules. Selected pure
`ValueToString`, `EscapeString` and `UnquoteString` counterparts provide the original
string/numeric name behavior. Source attributes, user import-type syntax and broad
emit remain excluded.

Support exclusively owns the approved `ModuleSpecifiers` counterpart plus
`SymbolAccessibility`; core owns builder/printer callsites. Its generated names must
use the original relative/ending/package-lookup decisions. Ancestor package metadata
is a genuine selected diagnostic-host dependency; encountered mappings requiring
excluded package resolution stay explicit unsupported paths, without changing
Bundler defaults. A combined compiler/runtime snapshot is pending this family.

Independent accessibility review compared candidate/transitive alias resolution,
visited-symbol-table removal, stable shortest-chain sorting, qualification and scope
tables, alternative-container pointer memoization, external module order/index and
reentry scan with the pinned original. No material finding in those reviewed paths.
Private accessibility topology is explicitly unclaimed by the new observer; demanded
diagnostic and rendered-type behavior remains required exact comparison.


### Combined formatter compiler packet

Central 4 is a distinct actual Debug invocation: failed with 7 errors, 0 warnings,
4.99 seconds (The log is an investigation artifact,
not a reproduction dependency). The support accessibility/host/specifier family had
no body compiler errors; one missing exact `CountPathComponents` leaf accounts for
two references. Core repairs cover the lowercase alias warning, expression type-argument
header access, original NoIndentation bit and inherited unique-symbol payload access.
No runtime outcome is attributed to this failed invocation. A corrected grouped
snapshot follows once the exact leaf is ready.


Central 4b is a separate corrected Debug invocation: clean, 0 warnings, 0 errors,
5.36 seconds. Immutable library copy:
`artifacts/projects-validation/build4b/library`. The complete first module formatter/
accessibility/specifier source batch now compiles. Exact initial module comparisons
are pending this copied snapshot; no build success is reported as semantic parity.


### First actual semantic vertical

The copied central 4b library passed the original/managed 2-file generic named-import
vertical exactly (1/1) and the shared dispatcher passed 22/22 (18 support +4 profile/
ownership verification groups). Reports: [vertical](../../artifacts/projects-validation/build4b/vertical/report.json)
and [shared controls](../../artifacts/projects-validation/build4b/shared-controls.json).
The 11-case broader initial packet is still running; no native/whole-campaign claim.

Support's independent builder/core source review identified four finite translation
repairs, restored in the next source batch without expected changes: UTF8 encode the
no-context filename fallback and resolved disk-path diagnostic arguments; pass the
original Js ending preference for computed ESM resolution mode; and use Program's
real default resolution mode (None here) when no string context specifier exists.
These are source-grounded repairs even where direct existing import spellings could
hide a runtime difference. The reviewed ModuleSpecifiers preference/endings,
existing-import early return/mode tests, case-sensitive single-target relative path,
index-shadow probes and actual ancestor package imports lookup have no material
finding so far. Package mappings remain explicit unsupported at the original
reverse-matching boundary; no default-option changes or absent metadata fabrication.


### Central 5 coherent review/runtime repair snapshot

Central 5 Debug compiler invocation: clean, 0 warnings/errors, 6.08 seconds;
immutable `artifacts/projects-validation/build5/library`. This includes the four
independent byte/mode/ending source repairs, support's internal option ownership,
and original root expression/entity-name printer dispatch. The latter closes the
actual initial invalid-demo StringLiteral failure: generated module names must not
enter type-node precedence. Initial 4b broader comparison was 4/11 exact; six positives
have an authored observer registration-order defect under repair, and the invalid demo
had the concrete root printer blocker. Expected semantic outputs remain original.
Final paired initial/demo and affected retained checks are running on the new snapshot.

### Central 5 runtime closure and next source batch

The copied central 5 library compared all initial 11 requests exactly in 7.895 seconds:
[report](../../artifacts/projects-validation/build5/initial/report.json). The six prior
positive differences were authored observer registration order; their adapter repair
did not change original semantic expectations. Retained Callable tiny 5 and the scalar/
Callable boundary subset (126 assertions) passed on that snapshot.

Focused source review found two remaining admitted diagnostic branches, queued for the
next integrated snapshot. `reportNonExportedMember` now uses a nil-safe original export=
lookup while retaining the module's nil Exports table. `invocationErrorRecovery` now
checks the actual originating target signatures, returns normally when none exist and
attaches original 7038 related information when applicable; it no longer rejects every
namespace import. A scan limited to newly ported direct module/export/type-only map
operations found no additional missing Go-nil adaptation; other operations have source
allocation/guard invariants or existing nil-safe operations.

Core now also owns the new project gate/recorder and ordinary CLI checks. The draft
composes exactly one retained collection/regression cascade, preserves all baseline
`2598ceb` fixtures/proofs, and guards nine native targets (retained seven, project
validation and ordinary disk CLI). Manifest-driven original/helper/project comparison,
shared/xUnit registries, process order, demo byte equivalence and CLI status/location
checks must close before freeze. No comprehensive gate/native result is claimed.
The practical [guide](../PROJECTS.md) documents the distinct Program and raw Checker
channels and remains acceptance-pending. Await verifier's wider family packet and
support's exact readonly index-shadow host repair, then perform the next coherent build.

Central 6 is a separate Debug compiler invocation: clean, 0 warnings/errors,
5.74 seconds. Immutable library copy:
[build6/library](../../artifacts/projects-validation/build6/library). It includes
the two module diagnostic source repairs and support's original 1409 argument and
readonly index-shadow host changes. Paired family/demo/shared results are pending;
the build itself is not evidence of semantic/native parity.

The ordinary CLI target then compiled separately with BuildProjectReferences=false:
clean 0 warnings/errors, 1.03 seconds, with an immutable output copy in
[build6/cli](../../artifacts/projects-validation/build6/cli). Its eight managed checks
passed in 1.500 seconds, including exact shipped valid/invalid diagnostic projection
from actual original rich outputs (codes, messages, file/ranges, derived UTF16 display
positions, chain/related fields and duplicate multiplicity). The top-level diagnostic
multiset combines original Program channels; recursive child order stays exact.
[Report](../../artifacts/projects-validation/build6/managed-cli-canonical/report.json).
Native CLI execution and final manifest-bound replay remain pending.

Central 7 is a separate Debug library invocation: clean, 0 warnings/errors,
6.14 seconds, immutable [build7/library](../../artifacts/projects-validation/build7/library).
Only support's authored strict configuration/path decoding guards and owner formatting
changed production since central 6; no checker algorithm changed. The actual lone-
surrogate configuration abort is now converted at its decoding boundary to the
documented configuration failure. Per coordinator, run targeted malformed CLI and
shared controls here, while the widened original/managed corpus finishes on copied 6;
the final frozen native/xUnit gate will validate final source together.

The ordinary CLI target compiled separately against central 7 without rebuilding
project references: clean 0 warnings/errors, 0.69 seconds, immutable
[build7/cli](../../artifacts/projects-validation/build7/cli). The targeted escaped
lone-surrogate configuration check passed (1/1) with structured exit 2; the same
tool execution passed 14 finite lexical/JavaScript escape projection controls.
[Report](../../artifacts/projects-validation/build7/managed-cli-malformed/report.json).
This is targeted managed evidence; the final nine-case native CLI gate remains pending.

The unchanged copied central 6 checker completed the final canonical corpus:
48/48 exact in 28.377 seconds, including mutual function recursion, explicit root
permutations and inline type-only imports. Both process-order sequences passed
30/30 executions on each runtime in 26.252 seconds.
[Comparison](../../artifacts/projects-validation/build6/canonical-final/report.json),
[order](../../artifacts/projects-validation/build6/order/report.json).
The final configuration delta remains covered by central 7's targeted controls;
comprehensive Native AOT acceptance is still pending.

The first frozen comprehensive gate was stopped with exit 143 after independent
support review confirmed false acceptance at the byte-string-to-disk boundary:
a preserved lone-surrogate import resolved a replacement-character filename, whereas
original Go reported 2307 and loaded only main. No acceptance proof was emitted.
[Stopped status](../../artifacts/projects-validation/stopped-surrogate-gate/status.json),
[partial log](../../artifacts/projects-validation/stopped-surrogate-gate/partial-gate.log).
The completed checker 832 and binder 163 assertions passed without skips before
the stop; this partial run is not a successful comprehensive gate.

Coordinator authorized only a strict scalar-UTF8 disk-path decoder repair and
corresponding focused controls. Support owns that host repair; core adds one ordinary
CLI check with actual replacement-character candidates present (suite 9→10), expecting
explicit host rejection rather than resolution to another file. Global scanner/string
semantics and the accepted collection algorithms remain unchanged. Await the narrow
repair's source review and targeted results before a fresh frozen gate.

Central 8 is a separate Debug library invocation: clean 0 warnings/errors,
7.25 seconds, immutable [build8/library](../../artifacts/projects-validation/build8/library).
The only repair is strict scalar-UTF8 decoding at the disk boundary, shared by import
loading and both raw module lookup adapters. Independent source/controls review found
no issue; no global scanner/string change was made. Separate no-reference-rebuild
checks/CLI/adapter compilations were clean in 2.12/2.13/4.26 seconds respectively.

The copied snapshot passed all 27 shared bodies in 0.343 seconds, including high/low
surrogate rejection with replacement candidates present and valid paired/replacement
character target identities. Its ordinary managed CLI passed all 10 checks in 3.747
seconds, preserving full canonical demo diagnostic equivalence and all 14 lexical
controls. [Shared proof](../../artifacts/projects-validation/build8/shared-controls.json),
[CLI report](../../artifacts/projects-validation/build8/managed-cli/report.json).
Original 48 expected rows remain unchanged. No full managed corpus was repeated;
a fresh frozen comprehensive gate is required for final current-source acceptance.

## Final accepted checkpoint

The fresh comprehensive gate passed exit 0 in 780.934 seconds. Source
`355b4842fa619586687a2de72293e259e9d993b35609f4cd2ae367b9cf83f953` and assets
`117226e06b6ef7181042b90512420614c07198d480c9dea705e7109d149cbcd7` remained unchanged,
as did all 115 protected historical assets and tracked pinned upstream source.
Nine actual ELF64 x86-64 binaries have current/pre/post hashes and no CoreCLR dependency.
[Gate](../../artifacts/projects-validation/final-gate.json),
[durable proof](../../validation/results/projects-verified.json).

Actual native projects: 48/48 exact in 41.528 seconds; shared bodies 27/27;
order 30/30 executions per runtime; ordinary CLI 10/10 in 2.197 seconds plus 14
lexical controls and complete shipped-demo diagnostics. All 11 newly applicable
original assertion-preserving test functions passed. Four xUnit projects passed
23,542 assertions (scanner 3,838; parser 18,709; binder 163; checker 832), zero skipped.
The retained collection/regression cascade ran once in this successful gate; the
earlier interrupted cascade is separate incomplete evidence, not reused acceptance.
No earlier evidence recorder was called.

The concrete next language campaign should address classes/new and ordinary
destructuring, alongside useful source import-type/typeof type queries, on this shared
project host. Packages, Node formats and broad tsconfig/project references are a
separate host/resolution campaign; neither is implied by this bounded acceptance.
