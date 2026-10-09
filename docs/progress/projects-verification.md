# Bounded project verification checkpoint

**Accepted bounded campaign.** Baseline
`2598ceb5ce339fdc03558c1f13724f90ff10ee1e`; unchanged upstream
`fed0bf24149fb1ed36039212648bafdafc1ea10e`. The
[durable proof](../../validation/results/projects-verified.json) records the final
repaired-source gate: exit 0 in 780.934 seconds, with no source, asset, historical
fixture or upstream drift. This satisfies [the fixed contract](../PROJECTS-CONTRACT.md),
not whole Program/module checker parity.

All 48 canonical projects reproduce exactly in original Go and actual Linux x64
Native AOT; the [native comparison](../../artifacts/projects-validation/native/canonical/report.json)
took 41.528 seconds. The native dispatcher passes all 27 shared groups. Both runtimes
match all 30 order executions over 14 selected projects, including nonadjacent repeats
([order report](../../artifacts/projects-validation/native-order/report.json)).
The [ordinary native CLI](../../artifacts/projects-validation/native-cli/report.json)
passes 10 cases and 14 lexical controls with complete shipped-demo diagnostic equality.
All 23,542 xUnit cases pass without skips, including 832 checker rows: 48 new project
corpus theories and 27 new shared checks. All retained regression layers pass, and
nine fresh native binary hashes remain unchanged during execution.

Accepted source SHA-256:
`355b4842fa619586687a2de72293e259e9d993b35609f4cd2ae367b9cf83f953`.
Verification asset SHA-256:
`117226e06b6ef7181042b90512420614c07198d480c9dea705e7109d149cbcd7`.
[Coverage](../projects-coverage.json), [test provenance](../projects-tests.json) and
the separate [support catalog](../projects-support-tests.json) record exact scopes.
Shared provenance is 11 full assertion-preserving upstream functions, four adapted
groups and 12 authored groups. All 48 differential source inputs are authored;
no newly reused whole upstream compiler input units are claimed.

The readable [valid example](../../examples/projects/grouped-records) succeeds across
five implementation files; its [same-base invalid variant](../../examples/projects/grouped-records-invalid)
retains eight original file-attributed diagnostics. Canonical request config and source
bytes match shipped examples. Standard pinned Program and the default resolver produce
the independent oracle: no manual import symbols or metadata instrumentation.
Written-form inventory includes specifier-level type-only imports, mutually recursive
cross-file function bodies, reversed roots, string exports, named/anonymous default
functions and named default interfaces. Anonymous default interface is original parser
1003 diagnostic-only coverage, not a semantic positive.

A late probe exposed false acceptance before the first gate completed: replacement
UTF-8 decoding mapped lone-surrogate import bytes to a real U+FFFD filename. That run
was stopped and never accepted. The final source strictly decodes only at disk module-
path boundaries, preserving scanner/string semantics. Both lone-surrogate forms reject
with authored host status 2; original Go retains missing-module 2307/status 1. This is
an explicit representability boundary, not a diagnostic-equivalence claim. Valid
supplementary pairs and real U+FFFD filenames resolve exact targets and preserve cached
identity. [Original provenance](../../artifacts/projects-investigation/surrogate-specifier/review-original-summary.json)
and [independent repair review](../../artifacts/projects-investigation/surrogate-specifier/independent-repair-review.json)
preserve this distinction. Canonical 48 expectations were unchanged; final native
and xUnit controls run the repaired source.

Observations include whole passive binding graphs, file-qualified ownership, actual
resolution edges, reached module/export/alias identities, selected type/signature
fields, repeated queries and complete applicable diagnostic channels. Actual alias,
module and export-type store memberships retain null states and whole counts. Reached
export clones retain genuine identity, target and originating import. Private unreachable
duplicate clones are validated structural multisets with multiplicity, not a private
identity census or cloned-table topology claim. Accessibility/package/formatter private
topology is not asserted equal; demanded behavior and finite source APIs are compared.
The accepted collection private-state corpus remains a separate retained regression.
Unknown reached producers fail.

Path relocation is field-role and ownership grounded. Lexical import-token recognition
preserves quoted/escaped strings and Unicode identifier-prefix lookalikes; reviewed
filename argument roles preserve full diagnostic chains and related config ownership.
Malformed source and projects without existing user files receive diagnostic/CLI controls,
not complete semantic graphs. Configuration is the documented authored strict subset,
not the full upstream tsconfig parser. Raw-byte Unix filenames, packages, incremental
state and emit remain outside the contract.

The investigation chronology below is historical. Early partial graphs, failures and
pending instructions do not supersede the accepted manifest and proof.

## Original Program investigation

[Initial scratch source](../../artifacts/projects-investigation/probe/main-initial.go.txt),
[requests](../../artifacts/projects-investigation/probe/requests.json),
[results](../../artifacts/projects-investigation/probe/results.jsonl), and
[hash/options manifest](../../artifacts/projects-investigation/probe/investigation.json)
preserve seven successful executions of the pinned, uninstrumented Go Program
with its default resolver. This is investigation evidence, not a managed/native
acceptance result. The scratch module/build is outside accepted source, tools,
corpus and oracle assets.

The fixed roots are the same complete ES5/decorators/decorators.legacy libraries,
then the entry script. Dependency discovery is genuine Program behavior:
models, generic grouping, barrel and application are distinct ASTs, in upstream
postorder. No aliases, export tables or resolution maps are manually constructed.
ESNext modules/target and Bundler resolution work with `noResolve=false`,
`noEmit=true`, `allowImportingTsExtensions=true`, `noUncheckedSideEffectImports=true`
and the accepted strict flags. `skipLibCheck=true` remains genuine Program
policy; all three libraries are explicitly loaded and are not default libraries.
[The refreshed nine-case probe](../../artifacts/projects-investigation/probe/fixed-options-investigation.json)
sets `types: []`, `allowJs=false` and `resolveJsonModule=false` explicitly. Its
ordinary outputs remain unchanged. Ancestor package.json `type: module` versus
`type: commonjs` yields identical observed files, resolutions, module exports,
query identities and diagnostics. Private package-scope metadata is not compared;
this establishes those ordinary cases only, not a general unused-metadata claim or raw field parity. Later source review found transitive module-specifier formatting may read ancestor imports mapping. Support is retaining genuine bounded ancestor reads; absent/type-only package metadata is admitted, while a demanded package imports mapping is explicitly unsupported. New transitive controls are pending.

The modular grouping application has zero semantic, syntactic, bind, global,
Program and config diagnostics. It infers `Groups<Row>` across named/re-exported
and namespace calls, default `Summary`, `Readonly<Row>` and `Partial<Row>`. Named
aliases resolve to the original generic declaration; namespace aliases target
the actual source module. A recursive A/B interface relationship combined with
cyclic export-star barrels yields shared A/B/value exports without diagnostics.
Explicit `.ts` and `.js` specifiers both resolve to their actual `.ts` files.
All sampled repeated type, symbol, alias-target and resolution identities hold.

Invalid source retains actual file-attributed diagnostics: 2614 missing export
with default-import suggestion, 2307 missing module, 2322 imported record field,
2345 cross-file generic callback mismatch, 2693 type-only name used as a value,
and 2882 missing side-effect dependency. Explicit `downlevelIteration:false`
produces Program diagnostic 5102 against the config JSON source, even though its
effective false behavior remains appropriate. The new strict config subset must
omit/reject that removed spelling. Classic and Node10 are also removed options at
this pin. Configuration diagnostic ownership must support a config source file
outside Program.SourceFiles; it cannot fabricate a user AST ordinal.

## Protocol and comparison proposal

Use standard Program plus its default resolver as the independent oracle,
with exact fixed libraries and equivalent bounded project files/options.
Obtain complete Program semantic diagnostics over every loaded user file;
retain syntactic, binder, global, config parsing and Program/include-processing
channels separately, including exact chains, related files/ranges and all flags.
Apply original noEmit filtering. Malformed input is a parse failure, not complete
semantic checking. Ordinary native checking must be independent of Go/upstream.

Reuse the accepted passive binding encoder with file-qualified nodes and actual
file discovery order. Register every parsed source and config diagnostic owner
explicitly. Keep distinct module-local symbols out of globals. Observe actual
resolution edges (containing file/specifier/mode/resolved path/extension/status),
source module symbols and ordered exports, local export aliases, immediate and
ultimate alias targets, type-only declarations, unknown/circular targets and
repeated identities. Compare selected user/reached type records, signature and
resolved call relationships across files; display text alone is insufficient.
Use deterministic source/relationship-based identity, never process IDs or
allocation ordinals. Existing collection cache regression remains the full
private generic-cache census; do not recreate it without a new reached family.
Unknown new retained module payload/cache families must fail explicitly.

Target interactions include all authorized import/export forms, local/default
and string-named exports, type-only value misuse and imported-binding writes,
star collision versus local shadowing, cyclic function/type/alias relationships,
missing/default/non-module imports, same declaration shared through multiple
paths, normalized duplicate and parent paths, `.js` substitution and directory
precedence, root/query/process order and fresh-project isolation. Disk host
controls must distinguish genuine missing files from unsupported earlier search
candidates and reject symlinks/outside-root paths. Strict configuration controls
are authored host policy, not whole tsconfig parity.

## Demonstration and direct assertions

The readable final demonstration will split the accepted record example into
model types, generic collection functions, a barrel and an application, retaining
recursive cross-file records, grouping/map/filter/reduce, mapped views and
array/tuple/rest/loop work. Invalid variants preserve the same base and add
independent imported constraints/argument/return/property, export and resolution
failures. The current scratch application is a candidate, not shipped canonical
example bytes; format and preserve its useful logic before expected generation.

The original [type-only import clause test](../../upstream/TypeScript/tsc/internal/checker/checker_test.go:66)
is newly applicable. Preserve its two clause-type identity assertions against a
regular import and its default-clause versus type-reference identity assertion;
label the fixed library/host/options adaptation. The already accepted ordinary
GetSymbolAtLocation assertions remain regression coverage. Original Program
FileOrderingImports/FileOrderingCycles subcases provide useful order assertions,
but their Windows paths, full default library set, absolute cycle imports and
non-module console leaves require adaptations; they cannot be counted unchanged.
Package/JSON/rootDirs/source-phase/import-update/race/resolver tests remain outside
the bounded campaign unless the exact selected support operation becomes applicable.
The extension-replacement direct test uses excluded moduleSuffixes, so ordinary
`.js` substitution controls are authored comparisons, not a claimed unchanged
execution of that test. Benchmarks are excluded.

## Ownership and next milestone

Core owns checker algorithms, alias/model stores and formatter dependencies;
it is the sole central library builder. Support owns selected source-mapped
host/resolver/Program/options and authored config/disk/CLI adapters. Verification
owns Projects profile/admission, the independent oracle, semantic projection,
corpus/examples/shared xUnit/native controls and acceptance tooling. The agreed public entry is support-owned `TypeScript.Compiler.Project.Load`, with
one shared lazy Checker; standalone `Checker.Create(file, Projects)` explicitly
rejects the missing project host. ProjectAdmission reuses the collection source
boundary and adds authorized module forms. A consolidated AOT-safe verification
bridge reads actual alias/module/export-type stores without invoking resolvers.

First vertical: genuine two-file generic named import with exact resolution,
symbol target, inferred return and complete diagnostics. Then complete default,
namespace, type-only and star-cycle families. Use isolated central snapshots,
affected tests after substantive changes, and one frozen comprehensive native
cascade. Current status is an exact managed vertical, 22 executed shared groups and a finite remaining printer/adapter packet; no project acceptance.
