# Source-file binding completion assessment

Status: **B1–B6 accepted.** The fixed source-file binding contract is complete, with
actual Linux x64 Native AOT execution and all retained regressions. This is not
exhaustive correctness or full compiler parity. See the durable
[verification proof](../validation/results/binding-completion-verified.json) and
[reproduction instructions](./BINDING-COMPLETION-VERIFICATION.md).

| Contract | Acceptance evidence |
| --- | --- |
| B1 source/dependency closure | All 167 functions accounted for; body/dependency and independent review; direct source mapping. |
| B2 declarations/symbols/scopes | 17,723 exact native graphs, 416,118 symbols and 147,932 tables; within-file merges, ordering, ownership and full metadata compared. |
| B3 control flow | 286,218 flow nodes and 46,565 list cells; all 13 flag bits, cycles, synthetic snapshots and natural shared tails compared. |
| B4 diagnostics/lifecycle | 1,423 broad binder diagnostics across all 27 reachable codes; complete state, repeat identities, isolated files and shared concurrency/failure controls. |
| B5 provenance/gaps | Original-test inventory, unchanged foundation inputs, natural identity witnesses, per-cell limitations and reviewed producer exclusions. |
| B6 native/review/delivery | 17,723 broad + 26 targeted native graphs; 15 shared native groups; all 22,972 xUnit cases and retained foundation/semantic native regressions; no source/asset/historical drift. |

The native broad graph contains 2,601,667 nodes. Modes are TS 15,084, JS 1,480,
TSX 486, JSX 16 and JSON 657. Twenty-eight allocated-empty tables and four shared
FlowList references occur in the broad corpus; targeted witnesses provide two and
one respectively. These are observations, not independent original test-function counts.

## Selected capability

The [fixed contract](./BINDING-COMPLETION-CONTRACT.md) completes initial binding of
fresh parsed source files against pinned TypeScript
`fed0bf24149fb1ed36039212648bafdafc1ea10e`, building on foundation commit `2886d47`.
JS, JSX, TS, TSX and JSON, declaration files, source module options and malformed
syntax are included. Declaration/symbol relationships, within-file merges and
conflicts, scopes/container chains, JS/CommonJS/expando and reparsed JSDoc behavior,
all binder-produced flags, complete diagnostics and cyclic/shared flow structures
are in scope. Imports are bound declarations; resolving their targets is excluded.

All 167 upstream binder functions are accounted for: 163 emitted counterparts,
two manually adapted accessors, fresh managed allocation and an omitted pool-return
routine. Body-family and independent source reviews found no missing binder
algorithm. No binder algorithm was changed in this campaign. All upstream symbol
fields and eight functions are present, so its historical partial-file mapping is
corrected to ported. Broader AST files retain explicit later-stage exclusions.

Existing adaptations remain small and explicit: managed reference objects preserve
pointer graphs, ordinal dictionaries preserve raw-byte symbol keys, atomic long/ulong
storage preserves IDs, and a non-reentrant SemaphoreSlim plus publication state
implements sync.Once success/failure behavior. Fresh allocation replaces arenas/pools.
Internal text retains the established UTF8-byte representation and byte positions;
public text adapters remain distinct. No resolver, checker or project dependency had
to be added. The new AOT harness uses static metadata and source-generated JSON.

## Changes and findings

- A real scanner dependency defect was corrected: absent comment directives now
  remain nil through construction, reset, text reuse, save and rewind, matching Go.
  The public getter is nullable; historical JSON alone continues projecting absence
  to an empty array. Actual-Go lifecycle observations and scanner utility assertions
  protect this distinction.
- New cold observers compare full graphs before lazy helper effects, repeated-binding
  identities and values, complete recursive diagnostic state, twelve source metadata
  null channels, symbol ownership/name/flag helpers and container flags.
- Malformed private-name destructuring exposed an observer assumption: a generated
  name's lexical class owner can differ from Symbol.Parent. Both relationships are
  now retained. A later xUnit failure exposed the same opaque ID in a helper display
  result; strict owner descriptors now cover that returned string too. Ordinary
  display bytes remain exact. No permissive digit stripping was introduced.
- Independent review strengthened concurrent waiting/publication observations and
  repeat-diagnostic snapshots. An initially proposed order regression did not trigger
  the actual bug; testing against the preserved broken observer caught that weakness.
  The corrected original and regression inputs fail the old observer and pass the new.

## Test provenance and coverage

The [binder/support inventory](./binding-completion-tests.json) contains 21 original
functions: 11 applicable assertion functions retained from foundation, eight excluded
performance benchmarks and two outside the required dependency closure. There are
zero original binder correctness functions: its only direct test-file entry is
BenchmarkBind. No original applicable assertion was skipped or weakened.

New authored observations comprise eight shared support groups, four exported-binder
helper groups and three runtime/observer controls; xUnit and Native AOT use the same
bodies. There are 26 targeted graph inputs and 17,723 reused foundation input units.
These are data-driven cases and compiler-input comparisons, not thousands of newly
ported original test functions or successful full compiler tests. Existing extraction
dispositions remain: 13,017 physical source inputs, 25 excluded non-source units and
four documented ancillary-mode adaptations. No corpus re-extraction was needed.

All 27 reachable binder diagnostic codes and 13 atomic flow flags were observed.
Codes 1250/1251/1252 occur solely in an unused private upstream helper; independent
callsite review explains their absence. Natural inputs produce allocated-empty tables
and shared FlowList tails. Distinct fresh table owner slots cannot alias under the
reviewed allocation/write closure; seeded aliases test APIs and observer sensitivity,
not natural binder production.

The [payload reconciliation](./binding-completion-payload-coverage.md) distinguishes
85 generated payload classes/178 fields from 81 cold-observed classes/172 field cells.
Two absent classes are transform-only markers; two are original attached JSDoc forms
outside binder child traversal. Their reparsed ordinary counterparts are compared.
Historical warm evidence covers three JSDocSignature cells as nil; the raw
JSDocTypeLiteral.Symbol cell remains source-reviewed, not runtime-observed. Per-role
aggregate counts do not establish every null/non-null state for every payload field.
The [per-cell record](./binding-completion-field-coverage.json) separately records
states from saved full fixtures: 50 cells with both states, 40 null-only, 71 reference-only,
and 17 without a saved full-fixture state. Broad streaming comparisons still check
actual values; their aggregate reports are not repurposed into per-cell state counts.

## Remaining boundaries and downstream use

Checker resolution/type inference, cross-file/global merging, module/project
infrastructure, transformations, emit, incremental AST repair and language services
remain outside the contract. Binder-produced information for those consumers is
retained. Incremental diagnostic RepopulateInfo has no managed field: the independent
Go observer asserts actual nil and rejects any nonnil result. Process-global counter
magnitudes, allocation capacities and arbitrary thread schedules are not equivalence
claims. Only Linux x64 Native AOT is an acceptance platform.

Downstream checker work may now rely on file-local
symbols, ordered declarations/value declarations, scopes, exports/member tables,
container links, diagnostics and flow relationships produced by fresh parsing and
binding. Rebinding an already-bound instance is a no-op; shallow clones with shared
bound descendants are not an incremental rebuild API. Raw attached JSDoc nodes are
not a substitute for the binder-facing reparsed nodes.

The recommended next campaign is callable-type checking: signatures, parameter/return
types, function expressions/declarations, calls and method interactions, with independent
semantic diagnostics and explicit library/resolution boundaries. This binding work
removes a foundational uncertainty; it does not establish callable or whole-checker
parity, nor readiness to check typical projects.

## Observed effort and process

Three existing explicitly selected GPT-6.1 Sol agents were reused for core/support/
verification work and cross-review. Root coordinated scope, decisions, evidence and
integration; the existing main-session model-metadata limitation remains disclosed.
No new package installation or additional user decision was required.

One broad managed triage took 253.866 seconds, yielding 17,722 exact graphs and one
observer rejection. Focused replays handled repairs instead of rerunning that full
managed corpus. The first final gate stopped after 34.647 seconds on the additional
helper-ID observer defect, with no source/asset drift. Its evidence is preserved.
The successful final gate took 480.539 seconds, including 247.522 seconds for the
broad native Go/C# binding comparison. It passed 3,838 scanner, 18,709 parser,
163 binder and 262 checker xUnit cases with no skips. Retained native evidence
includes 3,153 shared foundation cases, 17,633 finite parser seeds, 17,723 full
syntax comparisons and 17,723 scanner streams, plus 118 historical binder,
45 scalar, 43 object and 92 named-data checker cases. Complete original Go numeric
baseline sets of 156 and 236 seeds also passed. These elapsed values measure
verification work, not a comparative binder performance benchmark.

The new native executable is 6,639,056 bytes, Linux x86_64 ELF, with libc/libm
dependencies and no CLR dependency. The environment uses .NET SDK 10.0.401,
Go 1.27.1 and the existing GCC 15.2.0 native compiler fallback; system clang is absent.

Reuse of the foundation corpus, disjoint edit ownership, a sole coordinated library
build owner and source-aware independent review avoided duplicated extraction and
algorithm work. Negative controls against the broken observer proved more useful
than superficially similar regression inputs. Future campaigns should run affected
xUnit tests before the full gate when process-global IDs or test order matter.
Token totals and monetary cost are unavailable; elapsed validation time is not a
proxy for either. No performance optimization or new benchmark study was undertaken.
