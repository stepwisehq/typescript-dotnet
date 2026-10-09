# Independent project host/support source review

Core owner review, baseline `2598ceb`, pinned upstream
`fed0bf24149fb1ed36039212648bafdafc1ea10e`. Implementation is authorized under the
[fixed contract](../PROJECTS-CONTRACT.md). This is a finite review of support's newly
selected host, resolver, diagnostic accessibility and specifier families; it does not
re-audit accepted scanner/parser/binder foundations or claim complete Program parity.
Native acceptance and independent review are closed for the bounded contract;
the [durable proof](../../validation/results/projects-verified.json) records the
fresh successful run. Historical intermediate notes below retain their actual scope.

## Reviewed source families

| Family | Source comparison / selected invariants | Disposition |
| --- | --- | --- |
| [FileLoader](../../src/TypeScript.Scanner/Compiler/FileLoader.cs), [FilesParser](../../src/TypeScript.Scanner/Compiler/FilesParser.cs) | Original fileloader/filesparser task boundaries; LIFO single-threaded WorkGroup scheduling, canonical case-sensitive task identity, loaded-before-child scheduling, lower-depth propagation, visited-before-recursion and dependency postorder. Include reasons are retained before the visited short circuit. Libraries are explicit roots, not default-library tasks. | No material finding in these selected paths. The abandoned initial authored DFS was replaced by mapped boundaries before the first accepted runtime vertical. Parallel locks/pools, redirects, package/JS depth and content-mapper branches are outside this host. |
| [Resolver](../../src/TypeScript.Scanner/Module/Resolver.cs) and [DiskHost](../../src/TypeScript.Scanner/Projects/DiskHost.cs) | Original LoadModuleFromFile/LoadModuleFromFileNoImplicitExtensions boundary, known-extension substitution, unknown suffix declaration probe before implicit append, directory/index selection, bounded relative resolution and winning-candidate admission. | Dotted implementation basename precedence was corrected before the 4b runtime checkpoint; Unicode and dotted-basename cases then matched Go. Unsupported winning declarations/file forms are rejected, not skipped. No package resolution claim. |
| [Configuration](../../src/TypeScript.Scanner/Projects/ProjectConfiguration.cs), [Project](../../src/TypeScript.Scanner/Projects/Project.cs) | Authored strict JSON gate: duplicate/unknown properties, types/options, nonempty roots, root containment and symlink rejection. Normalize/deduplicate configuration roots before original mapped root scheduling. One lazy shared checker, fresh actual library ASTs, immutable public fixed options and source-file ownership. | Actual config-parser duplicate-root behavior was independently checked; root include reasons retain the first matching source literal. Project.Options was made internal rather than allowing post-load public mutation. This is not an upstream full tsconfig implementation. |
| [Program](../../src/TypeScript.Scanner/Compiler/Program.cs), [FileInclude](../../src/TypeScript.Scanner/Compiler/FileInclude.cs) | Per-file bind/check composition on the same checker, original SkippedOnNoEmit marker filtering, diagnostic sort/dedup/related merge, and include/casing diagnostics alongside otherwise valid user checking. | Project.Check's earlier early-return-on-include-error was removed. Original 1409 argument ordering/absolute filename roles were restored. Malformed source and zero existing user sources intentionally prevent semantic entry; parse/include failure is not a semantic success. |
| [SymbolAccessibility](../../src/TypeScript.Scanner/Checker/SymbolAccessibility.cs) | Original alias candidates and transitive identity, visited table removal, stable shortest-chain selection, qualification/scope tables, namespace re-export exclusion, alternative-container pointer memoization, actual external-file ordering/index and reentry scan. | No material finding in the reviewed selected branches. Full ContainingSymbolLinks distinguishes an absent pointer from a pointer to nil. Private emitted accessibility topology is not newly serialized; required display/diagnostics/repeat behavior is still tested. |
| [ModuleSpecifiers](../../src/TypeScript.Scanner/ModuleSpecifiers/Specifiers.cs), [preferences](../../src/TypeScript.Scanner/ModuleSpecifiers/Preferences.cs), [ending helpers](../../src/TypeScript.Scanner/ModuleSpecifiers/Util.cs) | Original existing-import early return, mode/path equivalence, allowed-ending order, ProjectRelative path choice, index-shadow probes and Bundler ancestor imports lookup. Actual namespace/accessibility helpers feed this family rather than an approximate relative formatter. | No material algorithm finding in the reviewed finite branches. Original source APIs test no-map behavior; reached imports mappings remain explicit unsupported under the contract. |
| Readonly root index-shadow adaptation | Original processEnding probes Root + each of 13 supported suffixes before dropping /index. New ModuleSpecifierFileExists permits exactly those existence-only root-sibling candidates, checks symlinks, and delegates all other paths to the original bounded dependency FileExists. | Direct Go witness returns `..` without sibling and `../index` with sibling. Independently reviewed finite allowed set: no arbitrary outside source loading or Read operation was added. Dependency containment is unchanged. |

## Confirmed integration repairs and evidence

Support's independent core review identified four source-boundary defects, all restored
in central 5: UTF8 encode the no-context module filename and disk filename diagnostic
arguments; retain original Js ending preference for computed ESM mode; use Program's
real default resolution mode None when no context specifier exists. These are core
integration repairs, not changes to support's resolver algorithms.

Core separately confirmed and repaired the newly ported nil Exports lookup in
reportNonExportedMember and restored the original namespace invocation recovery
signature test. A narrow repeated-pattern scan of new Exports/type-only map reads
found no further missing Go-nil adaptation; original nil tables remain nil.

The copied central 4b snapshot matched the two-file generic named-import vertical
and passed the then-current shared 22 groups. Central 5's initial 11 project cases
matched original Go exactly in 7.895 seconds, including Unicode/dotted names, duplicate
roots and a recursive star cycle. Its retained Callable tiny 5 and 126 scalar/Callable
boundary assertions passed. Central 6 compiles cleanly (0 warnings/errors, 5.74 seconds)
and its ordinary managed CLI passes 8 checks, including complete shipped-demo
diagnostic equivalence to Go and Unicode byte/UTF16 positions. The final 48-case
managed corpus and both 30-execution process-order sequences are exact. Central 7
adds narrow strict-configuration decode/path guards, independently reviewed at the
actual decoding boundaries; its malformed Unicode CLI check returns structured
exit 2, without a broad catch. See the [core checkpoint](./projects-core.md)
for reports and every actual compiler invocation. No native result is inferred.

## Reviewed omissions and remaining boundary

The selected host omits project references, redirects, automatic type acquisition,
default-library discovery, JS depth/Node format, package resolution, emit and incremental
watcher/content-mapper state. Exact explicit libraries plus fixed options justify those
omissions; a demanded excluded operation throws instead of producing fabricated any.
Loaded declaration files retain real demanded semantics despite skipLibCheck.

Package metadata generation is distinct from dependency loading. Required ancestor
package reads and no-map metadata work normally; an actual imports mapping that needs
excluded package resolution rejects explicitly at the original reverse-match boundary.
No option is disabled to bypass this path. Metadata/cache/watcher topology and private
formatter syntax caches are not claimed as whole-state parity by the project observer.

The late byte-to-disk review found a real false acceptance: lone-surrogate import bytes
could decode with replacement and load another filename. The first gate was stopped
and preserved. The authored disk adapter now uses strict scalar-UTF8 decoding at import
loading and both raw module lookups; non-path scanner literals are unchanged. Independent
source/control review and final native controls prove high/low rejection plus valid paired
and actual replacement-character path identities. This is explicit host rejection,
distinct from original Go's missing-module diagnostic, not invented diagnostic parity.

No unresolved finding remains in the reviewed families. Final 48 original/native
projects, shared 27, order 30 per runtime, CLI 10 plus lexical 14, applicable original
11 tests and the complete 23,542-test regression suite passed; all nine native binaries
and source/assets/historical/upstream guards passed. The bounded contract is accepted;
the omissions listed above remain explicit. Original assertion source bytes and final
manifest registries stay guarded while documentation records execution status.
