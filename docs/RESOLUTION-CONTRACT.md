# Source-import module-resolution completion contract

**Fixed contract satisfied on Linux x64 Native AOT; see the [assessment](RESOLUTION-ASSESSMENT.md) and [proof](../validation/results/resolution-verified.json).** Baseline `37bd57d`;
upstream `fed0bf24149fb1ed36039212648bafdafc1ea10e`. The later `639c918` commit contains
only deferred .NET 11 notes and is retained. See the [mandate](RESOLUTION-PROMPT.md).

## Selection and completeness definition

Complete the pinned upstream `internal/module` production subsystem: five files,
3,272 physical lines, 149 functions/methods. At contract selection, the existing port had selected counterparts
for resolver/types/util; cache and staticresolver needed counterparts. A name in the
[function inventory](resolution-functions.json) does not establish implementation,
review or runtime coverage. Those statuses are separate.

The supported resolution kinds are the ones actually implemented upstream: **Node16,
NodeNext and Bundler**, with their original per-request resolution modes, conditions
and defaults. This contract does not invent Classic/Node10 support absent from the
pinned implementation. Unsupported/invalid API values retain explicit source behavior.

Completion means all responsibilities in these source files and the supporting
dependencies necessary to exercise them are implemented and independently reviewed,
with meaningful original-test and differential evidence. It does not mean complete
Program construction, arbitrary semantic checking or exhaustive input/branch coverage.

## Required capability

| Row | Required completion |
| --- | --- |
| R1: production APIs | All five source counterparts and 149 function boundaries: DefaultResolver, ResolverOptions, source/directory/config/package/type-reference resolution, automatic type-directive names, package entrypoints, static resolution/fallback, result models, utility APIs and trace/error behavior. |
| R2: search and metadata | Actual configuration-driven paths/rootDirs/module-suffix behavior; file/directory and extension substitution/precedence; TS/JS/declaration/JSON/extra-extension forms; package types/main/typesVersions, conditional exports/imports/self references, arrays/null targets, patterns, scoped packages, peer identities, type roots/typings location, redirected-reference inputs, symlinks and alternate results. Conditions and stopping behavior follow source, including failed/blocked candidates. |
| R3: identity and state | Original result and package identities, canonical/original paths, mode/directory/redirect-sensitive cache keys, negative entries, module LoadOrStore versus type-directive Store, parsed-pattern map identity, shared ResolutionData and shallow Clone behavior, cache enumeration and concurrent/racing host behavior exercised by original tests. |
| R4: configuration/graph integration | A usable disk graph-inspection entry consumes genuine TsOptions options/roots and actual resolver results independently of Checker admission. Port necessary upstream source-loader/import-collection/module-format/scheduling behavior. Preserve loaded node identities, cycles, every observed edge/result/mode, parse/inclusion diagnostics and explicit reasons for upstream non-scheduling. Include static/reexport/import-equals/require/import-type forms admitted by original collectors, original synthetic tslib/JSX dependencies, explicit path references, file forms and noResolve/allowJs/external-JS-depth/augmentation behavior. Observe and resolve explicit type directives without claiming construction of the global type environment. |
| R5: capability separation | Remove obsolete restrictions inside resolution and graph loading. A real dependency can resolve and enter the graph while remaining unsupported for semantic checking. The ordinary checking entry applies explicit semantic admission separately; do not falsify resolution, skip files or coerce options for the checker. Preserve the named legacy checking adapter and its accepted behavior. |
| R6: acceptance/delivery | Applicable upstream assertions systematically ported into xUnit/shared native tests, independent resolver and graph comparisons, independent source/omission/host/provenance review, actual Linux x64 Native AOT, retained configuration/project/semantic/foundation regressions, immutable historical proofs/fixtures, reproducible guide, source mapping, assessment and committed clean delivery. |

Every function receives an implementation disposition. Complete Module does not make
all of Compiler, AST semantic metadata, VFS, Project or checker complete. Preserve
one upstream source file to one counterpart where practical; supporting selected
functions and authored hosts/observers have separate mapping and adaptation records.

## Proportionate dependencies and external boundaries

The original ResolutionHost requires filesystem access and a typed current directory.
Reuse the accepted options, package-JSON, semver, typed paths and read-side VFS. Add
missing original path/AST/cache helpers only where reached. Use .NET collections,
locks and synchronization facilities while preserving source identity/publication
semantics; do not implement a separate resolution algorithm around the old narrow one.

Truthful graph integration needs more of the original source loader than the prior
TS-only/ES-module demonstration. The finite expansion includes source-file metadata,
package format, dependency collection, scheduling and existing FilesParser identity/
cycle handling. The coordinator has reported this expansion before implementation.
It does not authorize a replacement authored graph search or general Program port.

Resolver APIs accept actual redirected-reference options/config identity and return
their original results. Constructing/executing reference projects, default libraries,
automatic global type environments, broad Program option-combination validation,
watch/incremental invalidation, package installation, emit and language services remain
external. Standalone automatic type-name discovery itself is included in R1. Graph
inspection reports this boundary and is not presented as complete compiler diagnostics.

The existing genuine ES5/decorators/decorators.legacy checking environment and its
skipLibCheck meaning remain unchanged. Graph inspection may load library/declaration
files as actual dependencies without claiming that their bodies or all their APIs can
be checked. New language features or global semantic merging are not authorized here.

The ordinary entry will be `tscheck --inspect-project <tsconfig>`; exact output and
exit status are documented before delivery. `--config` continues to mean semantic
checking, with truthful resolution followed by supported semantic admission. Purely
resolver-owned admission bans may be removed; semantic-only limitations remain explicit.

## Platform and representation

Actual native acceptance is **Linux x64**. Preserve the accepted explicit scalar
boundary for real OS filenames and symlink targets; do not replacement-decode raw
names into a different file. Virtual hosts retain original Go byte paths, including
encoded dynamic paths, case modes and Windows/UNC forms. Passing those virtual cases
does not establish Windows/macOS execution. Explain host adaptations and differences
in error representation rather than silently changing resolution outcomes.

## Verification and observations

Inventory all three original Module test files (38 test bodies) and applicable
dependency tests/fixtures. Preserve complete assertions where the corresponding API
is in scope, including static resolver, private helper, shared-data and concurrency
tests. Adapt thread/signal mechanics when needed without dropping the race assertions.
Record original, adapted, reused-input and authored evidence separately; compiler
fixture inputs used only for resolution are not full compiler-test successes.

Use real upstream result schemas: filenames/path keys, original paths, extensions,
flags, complete package IDs, alternate results and resolution diagnostics. Preserve
trace DiagAndArgs, errors, host lookup behavior and cache/identity relationships where
observable. Do not invent failed-lookup arrays absent from the original result type.
Observation metadata is authored support, not a ported compiler API.

Establish equivalent source options, package bytes, filesystem data, current directory,
case sensitivity and redirect/static overrides. Cover successes, missing/blocked/
ambiguous candidates, extension precedence, malformed metadata, conditions, interactions,
dynamic paths, state reuse, cache sharing and input order. Distinguish unordered map
enumeration from ordered search/trace behavior before authoring expectations; compare
complete correlated states without erasing meaningful offsets, errors or identities.

Choose a realistic config-driven package graph early, including paths, exports/imports,
multiple source forms, cycles and failing variants. Compare actual loaded closure and
edges independently of Checker. Separately retain a semantically supported project
to establish successful checking and cross-file failures through the new resolver.

Publish and execute the resolver/shared validator and ordinary graph/check commands
with Native AOT. Run the retained configuration cascade once on the final coherent
source/assets. A passing JIT suite, source review or partial native probe is not final
acceptance. Preserve historical proofs and expected fixtures unchanged.

## Execution and stopping

Reuse three GPT-6.1 Sol owners with exclusive source ownership: core owns Resolver,
Types, Cache and StaticResolver; support owns Util and required host/loader integration;
verification owns tests/oracle. Pair applicable original assertions with implementation
families immediately. Core is sole central builder and supplies immutable snapshots.
Cross-owner review challenges source fidelity and omissions.

Internal gates are typed API/cache foundations, complete resolution families with
original assertions, truthful graph integration, then independent comprehensive
acceptance. Move directly between them. Run cheap retained tests for touched adapters
before the expensive cascade; do not repeatedly replay broad tests without a concrete
change/risk. The coordinator owns contract, mapping, decisions and final acceptance.

Do not narrow this contract to declare success. Report a concrete material dependency
beyond these boundaries before broadening scope. Ordinary implementation difficulty,
test volume or a completed internal milestone is not a stopping condition. .NET 11
investigation and optimizations remain deferred. No next subsystem is authorized by
completion of this one.
