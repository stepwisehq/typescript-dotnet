# Source-import module-resolution assessment

**The fixed [R1–R6 contract](RESOLUTION-CONTRACT.md) is satisfied.** The final gate
passed in **912.603 seconds**, with **24,334 xUnit cases, zero failures/skips**, actual
Linux x64 Native AOT execution and all retained regressions. The
[durable proof](../validation/results/resolution-verified.json) records 13 native
executable paths and exact source/assets/binary evidence. All five drift checks are
false; 171 historical fixture/proof files are unchanged. This is subsystem-contract
completion, not exhaustive correctness or full compiler parity.

Baseline `37bd57d`; upstream remains
`fed0bf24149fb1ed36039212648bafdafc1ea10e`. Deferred .NET 11 notes at `639c918` are
preserved; no runtime migration or optimization was undertaken.

## Implementation versus acceptance

All five upstream Module production files and 149 functions/methods are translated and
independently source-reviewed, with direct counterparts and original license notices.
The [function inventory](resolution-functions.json) and [source map](../source-map.json)
distinguish implemented functions from runtime witnesses. Required Compiler/AST/host
and formatter additions are selected dependencies, not claims of completing those
subsystems. The [guide](RESOLUTION.md) documents ordinary invocation and reproduction.

| Contract | Implemented capability | Acceptance status |
| --- | --- | --- |
| R1 | Complete Module APIs, result types, static resolver, caches, utility and factory entry points | All 38 original bodies replayed in Go and shared xUnit/native checks; independent source/assertion review complete |
| R2 | Source search, extension priority, packages/maps/conditions, config options, types, redirects, symlinks | 216 compound comparisons pass actual native and fresh Go |
| R3 | Required cache keys/publication, sharing/clone identities, negative entries and original concurrency assertions | Original assertions and 30 order executions per runtime pass; not exhaustive concurrency/private storage equivalence |
| R4 | Genuine configuration-driven source loading, collectors/scheduling, metadata, references and diagnostics | Original-loader graph comparisons pass; explicit external stages below |
| R5 | Genuine resolution followed by separate checker admission, ordinary inspection/check commands, named legacy policy | All 11 new CLI cases and retained legacy/configuration cases pass; Unicode/default regressions repaired |
| R6 | Provenance, independent review, native execution, retained regressions and committed delivery | Complete gate and durable evidence pass; delivery commit contains this assessment |

The first frozen gate stopped after 63.847 seconds at a retained configuration
integration assertion: the public project SourceFile filename exposed Go byte-string
storage instead of its previously promised Unicode disk identity. New resolution
xUnit256 and native216/shared40/order30/CLI11 had passed. This is a concrete API
adaptation regression; those partial results do not accept the campaign.

The second frozen gate stopped after 267.967 seconds at the unchanged retained Binder
default-filename assertion. The new byte accessor passed a null managed default to UTF-8
encoding rather than preserving Go's empty-string value. The local repair and added
witness in the existing shared filename control pass focused snapshot10 checks; no
expected result changes.
The new resolver and retained configuration native stages passed before this failure.
All four retained managed suites subsequently passed on snapshot10: 23,542 cases,
zero failures/skips, before retrying the comprehensive gate.

## Evidence inventory and limits

- **38 original direct Module test bodies**, from three upstream test files, ported with
  their assertions and replayed against pristine Go. Managed/native shared execution
  includes these plus **two authored controls** (Deduplicate and Unicode filename roles).
  The three original concurrency tests use actual Tasks and synchronization gates.
- **216 compound differential requests**, not 216 original tests: 69 core requests with
  310 API queries, 22 utility requests with 368 queries, 105 adapted compiler-input graph
  requests, 16 targeted graph requests and four example API requests.
- **76 selected upstream compiler inputs**: 72 reused for the 105 graph requests; four
  removed-mode-only inputs excluded. All extracted input payloads were compared with
  the original test parser. Original full compiler assertions are not claimed. Omitted
  post-discovery option overrides for embedded configurations are documented; the
  independent core matrix covers actual supported resolution modes/options.
- **14 order groups /30 executions per runtime**, including source-required public cache
  and identity relationships. Private lazy package-cache topology is unobserved.
- **95 ordinary API observations** match the pristine companion exactly. The graph oracle
  invokes original loader and diagnostic producers through a scoped overlay. Its omitted
  stages are stated below; it is not an independent reimplementation of graph traversal.
- **11 ordinary CLI controls**: six compared with genuine Go Program semantic diagnostics,
  three with the upstream loader graph, and two authored explicit-unsupported boundaries. Successful and failing package projects,
  Unicode paths, traces, builtin diagnostic2591 and direct-@types diagnostic6137 are included.

Public results include filenames, canonical/original paths, extensions/flags, complete
package IDs, alternate results, resolution diagnostics and ordered per-query search and
trace observations. Graphs include actual nodes/edges, modes, package metadata, reasons
for scheduling/non-scheduling and parse/inclusion diagnostics. Observed casing, package
format/deduplication, deferred path errors, malformed Unicode source, cycles and external
JS depth are compared. Counts alone are never the comparator.

Nonempty **type-reference ResolutionDiagnostics publication** is implemented and
source-reviewed but remains unexercised: proportionate investigation found no natural
public rooted-host witness through the reached producer. This is not a claim of
unreachability. Nil/missing type results, diagnostic2688 and complete field forwarding
are exercised. Exhaustive inputs, every branch and all possible thread schedules are
not established by this evidence.

Three authored casing format witnesses (module/commonjs/absent) initially depended on unspecified Go map acquisition order,
producing two valid complete observation streams. A source-supported root order now
loads the canonical root first; the same casing/package-format assertions remain, with
fresh repeated-Go verification. No diagnostic sorting, blanket filtering or changed
compiler algorithm was used. Earlier divergent outputs remain in intermediate evidence.

## Adaptations and boundaries

Go byte strings and typed path roles remain explicit internally. The disk host accepts
scalar UTF-8 OS paths and rejects unsupported encodings without replacement-decoding a
different file. The public checking Project API preserves its documented Unicode filename role;
standalone graph SourceFile names remain original byte strings.
Ordinal collections, explicit locks, nullable extension operations for actual Go
nil-receiver methods, and Lazy for the once-combined Node builtin table replace runtime
facilities without changing the selected algorithms. A fresh managed LIFO task list
replaces source task pools/parallel infrastructure for this loader invocation.

Source-to-publication review found and closed real adjacent omissions: nil-safe package
cache access, module/type diagnostic publication, package-format/dedup explanation chains,
synthetic preferred-location rules and deferred path-reference locations. Selected
original module-name formatting now reuses actual imported package names and dedup
relationships. Broader reverse package/path generation remains explicitly unsupported.

The retained configuration integration test's outside-root rejection is explicitly
retired under R5 and replaced by actual successful checking of that discovered source.
The original Unicode public filename assertion remains, with added raw-graph and
diagnostic-owner checks. This is an intentional supported-capability expansion,
separate from repairing the filename regression; historical expected corpora and
proofs remain unchanged.

A named legacy-host callback is an authored compatibility departure: only after failed
file probes and an eligible existing non-ESM directory does it enforce the older
`--project` package-directory policy. Ordinary/full graph hosts do not implement it;
it cannot reject an earlier actual file winner or change their original search order.
Legacy policy is not presented as upstream resolver behavior.

External stages remain **complete Program construction and option validation, default
libraries and automatic global type environments, library-reference scheduling,
reference-project execution, content mappers, incremental/watch, emit and services**.
The graph observer constructor omits the external Program/default-library/automatic/
reference root stages. Its source overlay disables only the selected lib-reference
scheduling/diagnostic block and adds passive observations; options and AST reference
metadata remain intact. Standalone
automatic type-name discovery and explicit type-reference resolution are implemented.

Graph resolution supports source/declaration/JS/JSON forms that the current checker
cannot consume semantically. Checking retains explicit existing TS implementation and
scalar/object/callable/collection boundaries and its fixed genuine three-library
skipLibCheck environment. No missing-global categories are suppressed to obtain
agreement. No additional platform or broad third-party library compatibility is claimed.

## Review and execution efficiency

Three existing explicitly selected GPT-6.1 Sol agents own core resolution, support and
verification. Astra coordinates scope, decisions and acceptance. Source/assertion
reviews are disjoint: [core review](progress/review-resolution-core.md),
[support review](progress/review-resolution-support.md) and
[assertion review](progress/review-resolution-assertions.json). Independent gate review
also used 12 rejection controls; a passing review is not a substitute for execution.

Pairing original tests with source families, disjoint corpus ownership, central coherent
snapshots and affected-test checks helped. Remaining integration surprises show a real
process cost: retained friend-test API compilation, final diagnostic publication and
Unicode public adapters should be checked at their first producer/consumer seam.
Further private-state observation would not have prevented those integration issues.
Before the successful retry, retained managed suites were checked before costly native stages.
It also exposed a stale test adapter after reference builds were suppressed: normal
incremental dependency builds are preferable when public constructor signatures change.
Future work should prioritize executable upstream entry points and cheap consumer tests.

The [ledger](progress/resolution-builds.json) records **20 central library/CLI compiler
invocations, 52.25 seconds: three failed and 17 clean**. These exclude adapter/oracle/
native/gate work and are not total effort. The two unsuccessful gate attempts took
63.847 and 267.967 seconds; the successful full gate took 912.603 seconds. These are
measured execution times, not total campaign elapsed time or a performance comparison. There is no
available token or monetary telemetry. No package installation or user unblock was
required. The user supplied prospective efficiency guidance during this campaign;
no campaign restart, broad cleanup or new tracking framework followed from it.

## Downstream implications and next subsystem

Subsequent work can rely on the complete selected Module
APIs and configuration-driven source graph across package metadata and supported
resolution modes, without rebuilding a demonstration-specific resolver. Relative-only,
package-map, outside-directory and file-form restrictions are removed from resolution;
semantic admission remains explicit.

The next high-value completion target is **Program construction and environment
assembly**: full file inclusion/global and library environments, option-combination
validation and integration with project references at a separately assessed boundary.
These are now concrete external stages rather than unresolved source-import algorithms.
Checker language expansion (notably classes/new, destructuring and broader declarations)
remains necessary for ordinary applications. Completing resolution does not establish
whole-compiler feasibility, full checker parity or arbitrary package compatibility.
