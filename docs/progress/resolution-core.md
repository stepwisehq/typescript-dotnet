# Module resolution: core checkpoint

Baseline `37bd57d`; current HEAD `639c918` adds deferred .NET 11 notes only.
Upstream remains `fed0bf24149fb1ed36039212648bafdafc1ea10e`. Initial read-only
assessment found five production files, 3,272 physical lines and 149 function
boundaries: resolver112, staticresolver9, util13, cache9, types6. The
[function inventory](../resolution-functions.json) distinguishes responsibility,
translation, independent review and later execution evidence.

## Complete boundary and existing gaps

Complete the original Module package rather than widening the authored relative
resolver. Public resolution supports Node16, NodeNext and Bundler, with actual
resolution modes, conditions, extension selection, rootDirs/paths, package
self-name/imports/exports/typesVersions, optional typings locations, type-reference
directives, automatic type names and package entrypoint discovery. Preserve config
and package-directory resolution already used by the accepted configuration API.
Unsupported/removed resolution kinds retain the original panic behavior; module
resolution does not execute Program, checker, emitter, build or watch workflows.

The baseline [Resolver](../../src/TypeScript.Scanner/Module/Resolver.cs) combines
an authored relative-only Projects path with a selected configuration/package-directory
port. It lacks general source resolution, type-directive/entrypoint families,
full tracer/state modes and the original cache/static-resolution units. The authored
path must be replaced by the default resolver; checker file/option admission belongs
to the host after genuine resolution. [Types](../../src/TypeScript.Scanner/Module/Types.cs)
also lacked interfaces and type/entrypoint models and stored result paths
as ordinary strings. Source typed paths must preserve raw byte identity; old scalar
disk AST/checker paths need explicit adapters rather than implicit replacement decoding.

## API and dependency seams

Use `TypeScript.Modules` with `IResolutionHost` exposing existing read `IFS` and
the typed current directory, `IResolvedProjectReference` exposing config identity
and compiler options, and `IResolver` preserving the original module/directory/type
resolution return tuples, trace arrays and error channel. `ResolverOptions` carries
all six source fields; `ResolutionData` retains options, typings location, project
name, extra extensions and the package cache. Public models use `RootedFilePath`,
`PathKey`, `ModuleSpecifier`, source flags and value-like PackageId state.

Existing complete CompilerOptions, PackageJson/Semver, read VFS/matcher, typed paths,
diagnostics and ordered storage cover most dependencies. Source resolution adds
selected typed-path operations for logical/dynamic candidates, input/output remapping
and entrypoint wildcard enumeration. Trace diagnostics are real Message/argument
records, separate from resolution diagnostics; there is no invented failed-lookup
result array. Static resolution preserves directory/mode precedence, explicit nil
results, duplicate-key errors and fallback type-directive behavior.

Cache fidelity is an early obligation: module result publication uses LoadOrStore,
type-directive publication uses Store, and parsed paths cache keys use actual map
identity. Each rebound resolver has fresh request caches while ResolutionData shares
its package cache; Clone copies that cache table while retaining entries. Use standard
.NET synchronization with these distinct operations, retaining cached null presence.
Do not lock filesystem calls across races or substitute a sequential-only test host.

## Ownership and sequence

Core owns Resolver/Types/Cache/StaticResolver, the central build ledger and this
checkpoint. Support owns Util, selected path/package/VFS dependencies and host/project
integration. Verification owns all 38 upstream test bodies, shared/xUnit/native
assertions, oracle/protocol/corpus and assertion-level applicability inventory.

Stage models/cache/static seams first, then complete request/state/source/type/config
families, then entrypoints and precise dynamic paths. Pair the source families with
their original tests before dependent host integration: cache clone/identity,
directory intent/extension replacement, dynamic rootDirs/package/entrypoints,
type/condition diagnostics, static precedence and the three deterministic original
package-cache races. Run cheap affected legacy/configuration controls before any
comprehensive gate. No build or acceptance is claimed at this initial checkpoint.

## Current implementation checkpoint

The four core units are now translated and syntax-formatted: complete default resolver
request/state/search/config/type/entrypoint families, complete typed result models,
request and parsed-pattern caches, and static resolution. The authored relative-only
algorithm has been replaced. Translation is separate from source review and execution;
the first coherent central compiler batch has completed.

Cache operations retain source distinctions: trace-on bypasses request cache reads,
module publication keeps the first result, type-directive publication replaces it,
and parsed paths are keyed by actual map identity including nil. PackageId default
strings expose empty source values. Static optional modes retain StrongBox pointer
identity; internal source state/helper visibility enables unchanged original tests.

Necessary typed-result consumer changes preserve accepted configuration algorithms.
Checker diagnostic arguments now consume existing raw path bytes once, and the
obsolete external-library flag restriction is removed for admitted TS implementations.
Semantic file admission remains distinct from pure resolution and graph inspection.
Independent review and final runtime acceptance remain separate from this translation checkpoint.

The first clean complete-family library snapshot is **3**, zero warnings/errors in
6.94 seconds. The previous binding passes are retained in the
[central build ledger](resolution-builds.json). Verification received immutable copies. The early Linux x64 NativeAOT run completed
35 original bodies plus two authored controls, including all three concurrency bodies;
the data family stopped at a nil-cache helper discrepancy. Both original nil-safe
InfoCacheEntry methods are now restored, and the retained typed helper API is preserved.

Grouped snapshot **4** compiled cleanly: library 6.25 seconds and CLI 1.21 seconds,
zero warnings/errors. Its direct dispatcher completed all **38 original bodies plus
two authored controls**. All 14 retained configuration CLI cases had already passed
on copied snapshot 3. Snapshot 4 differential/graph/current NativeAOT and final
regressions remain pending. Compilation and grouped tests do not imply per-function
execution coverage. The ledger contains seven actual central invocations, three failed
and four clean, totaling 30.26 seconds; adapter/oracle/AOT runs are separate.

The selected support loader's final diagnostic publication was then reviewed as its
own producer-to-caller boundary. Source4 lacked stored module/type resolution-diagnostic
publication and package redirect/implied-format explanations; explicit unknown-type
and synthetic-primary/type-location-ID conditions were also corrected in the same
batch. These findings, exact external omissions and the finite existing-import
formatter closure are recorded in [the independent support review](review-resolution-support.md).
Grouped snapshot **5** compiled library/CLI cleanly in 2.96/0.66 seconds. It is now
copied for the verifier's new graph and retained diagnostic comparisons. The central
ledger has nine actual invocations, three failed and six clean, totaling 33.88 seconds.

The reached PackageDirectory trace argument also required an explicit ordinary CLI
DTO branch. CLI-only snapshot **5b** compiled that source role cleanly in 0.60 seconds
against the unchanged library5 hash; references were not rebuilt. The ledger now has
ten actual invocations, three failed and seven clean, totaling 34.48 seconds. Its
ordinary trace fixture remains verification-owned.

Core independently reviewed the remaining 32 original assertion bodies; support
reviewed the three data and three concurrency bodies. Exact source/counterpart hashes,
retained input/assertion domains and setup adaptations are captured in
[the assertion review](review-resolution-assertions.json). No test expectation was
produced from the managed compiler, and no per-function coverage is inferred.

The dedicated authored default-resolver input packet is
[requests-core.jsonl](../../validation/resolution-corpus/requests-core.jsonl), reproducible
by [its request-only generator](../../tools/resolution-generate-core-requests.py). It
currently contains 69 independent environments and 310 actual API queries: all
supported modes, conditions/packages, files/directory intent, paths/rootDirs/suffixes,
types/automatic names, config/package-directory APIs, symlinks/case/raw virtual bytes,
extra extensions, redirects, static precedence/errors, entrypoints, typings cache and
input/output remapping. RawCompilerOptions is a separately declared source factory
input for internal options and the unseeded-config ambiguous-root diagnostic branch;
it never masks parsed config options. Its three raw-option rows use source stored
numeric enums (ESNext99/Bundler100), distinct from config-file names. The first run's
incorrect string inputs and original panic remain preserved investigation evidence.
The corrected original packet completed all 62 requests; comparison then exposed an
observer omission for reached PackageDirectory trace arguments. Verification is
adding an explicit source AsDirectoryPath projection to both observers and rerunning
the original producer before comparison. Expected outputs remain exclusively actual
pinned-Go execution, with paired results pending. Seven additional independent-review
cases now exercise conditional type modes, imports-root feature defaults/disabled
imports, and static type delegation with module fallback disabled. Their addition
preserves every byte of the prior 62 requests. This finite matrix supplements the
original tests; it is not exhaustive
branch or whole-compiler coverage.

Grouped snapshots **6** and **7** compiled library/CLI cleanly in 3.00/0.55 and
3.02/0.58 seconds. The latest immutable library is snapshot7. The central ledger
records **14 actual invocations, three failed and eleven clean, 41.63 seconds**;
consumer/oracle/native builds are separate. Snapshot7 closes the authored package
admission role, original builtin/direct-@types diagnostics and deferred triple-slash
edge diagnostic publication. Its 16 graph environments and 11 ordinary CLI controls
now match the actual original observations. The core69 resolver environments had
already matched source5 after explicit observer host-role corrections. The compiler
input reuse packet preserves original harness extraction but does not claim original
compiler assertions or omitted post-config option overlays. Final current native,
xUnit, complete retained checks and acceptance remain pending.

The new gate/recorder/shell are exclusively core-owned; verification owns their
manifest/corpus/oracle and independently reviews tooling. The gate will execute the
new Resolution corpus/shared/native/ordinary CLI first, then one retained Configuration
cascade. Every ordinary original API request is also replayed by an observer companion
without a compiler overlay; graph observation retains its separately described
publication overlay and explicit external default-lib-reference scheduling boundary.
No source-function execution coverage is inferred from these grouped environments.

Snapshot **8** compiled cleanly in **3.04 seconds** for the library and **0.58 seconds**
for the CLI. The ledger now records **16 actual invocations, three failed/thirteen
clean, 45.25 seconds**. This batch contains only the explicit named legacy host
admission repair after preserved compatibility tests exposed changed winner/category
behavior. Default resolver/graph hosts do not implement the optional interface;
shared ResolutionData contains no admission reference. Source correspondence and
compatibility scope are recorded in the support review. Focused retained Projects27
and CLI11 run before the final comprehensive gate; native acceptance remains pending.

The first comprehensive attempt completed the new original/native resolution
checks, then stopped at the retained authored Configuration integration Unicode
RootFiles filename assertion. No acceptance was recorded. Logs, TRX and binary/input
fingerprints are preserved under artifacts/resolution-validation/gate-attempt1.
The independently reviewed repair converts only the private checking graph's public
filename role before binding; raw path keys, AST owners and standalone graph output
remain unchanged. Original Unicode assertions remain, and the former full-config
outside-root restriction is explicitly retired under R5 while legacy admission stays.

Grouped snapshot **9** compiled library/CLI with zero warnings/errors in **3.09/0.46
seconds**. The central ledger now records **18 invocations, 3 failed and 15 clean,
48.80 seconds** total. Focused integration/CLI runtime is support-owned; a fresh
comprehensive run is still required. No per-function runtime coverage is inferred.

Attempt2 stopped automatically at the retained Binder default-filename assertion:
162 passed/1 failed. Source/asset/history/upstream/obligation drift remained false;
new resolution and configuration portions passed but no acceptance was recorded.
The failed evidence is preserved under artifacts/resolution-validation/gate-attempt2.
The independently reviewed byte-role accessor now maps the managed null default
filename to the Go empty zero string locally, preserving public representation.

Grouped **snapshot10** Release library/CLI compiled cleanly in **2.84/0.61 seconds**
for retained-suite references. Ledger **20 invocations, 3 failed/17 clean, 52.25
seconds**. Default Binder focus and raw/scalar/null/empty/Unicode shared focus pass.
Four retained managed suites and the comprehensive final gate remain required.

The snapshot10 cheap retained managed run initially reused a stale Foundation.Checks
assembly with the old three-argument parse-options constructor. Its MissingMethod
failures were harness dependency state, preserved separately, and only the adapter
was rebuilt before repeating affected suites. Prefer normal incremental dependency
builds after API/constructor changes; BuildProjectReferences=false is appropriate
only when the complete dependency graph is known current. An up-to-date MSBuild
visit is cheaper than stale-adapter diagnosis. No production or expectation change
was made for this build-state issue.

Snapshot10 readiness is now closed: unchanged default Binder focus and expanded
GraphByteRole group pass; all four retained managed suites pass **23,542 tests**,
zero failures/skips (Scanner3,838, Parser18,709, Binder163, Checker832). Reports
under artifacts/resolution-validation/build10 bind the actual production DLL SHA.
A fresh frozen mandatory full gate has started; this readiness is not acceptance.

## Final recorded campaign evidence

The source10 frozen comprehensive run passed in **912.603 seconds**, all source,
asset, historical, upstream and assertion-obligation drift guards false. The
[durable proof](../../validation/results/resolution-verified.json) records original
Module38, native shared40, canonical216, pristine ordinary95, order30 per runtime
and ordinary CLI11 (6 actual Program, 3 graph, 2 authored unsupported boundaries),
plus the complete retained cascade. Aggregate selected xUnit is **24,334 passed**,
zero failures/skips: new256 +Configuration536 +retained23,542. Prior failed attempts
remain preserved; no historical golden/proof was rewritten.

All five Module units/149 functions are translated and independently source-reviewed.
The function inventory now refers to campaign runtime evidence without inferring
per-function execution, exhaustive branch equivalence or every private cache state.
The central ledger closes at **20 invocations, 3 failed/17 clean, 52.25 seconds**;
full gates, adapter builds and NativeAOT publishes are separate measured work.

The next useful assessment is complete Program responsibilities: authentic library
selection/scheduling and project-reference lifecycle are now visible external
boundaries of truthful resolution/graph loading. Ordinary package .d.ts checking
remains a separate concrete checker-admission/type-support barrier. Assess those
source units before integration; avoid treating further fixture additions as
completion of Program or Checker. No investigation of that next scope was done here.
