# Program construction and environment assembly assessment

**The defined Program/environment contract is complete.** Actual Linux x64 Native AOT
and the full retained regression cascade passed; independent review recommends
acceptance within the stated boundaries. See the [durable proof](../validation/results/program-verified.json).
Baseline `1d6bd61`; upstream `fed0bf24149fb1ed36039212648bafdafc1ea10e` unchanged.
The [fixed contract](PROGRAM-CONTRACT.md) controls acceptance.

## Implemented capability

The port now has genuine upstream Program construction rather than a selected project
graph standing in for a compiler environment. Configuration/discovery, source loading,
module and type resolution, default/explicit/transitive/replacement libraries,
automatic type packages, references, include reasons and Program diagnostics are
assembled through their original producers. The 115 bundled declaration files are
byte-identical to the pin. No declarations or dependencies are removed to accommodate
checker limitations.

Program's local Update/Reuse lifecycle and lazy CheckerPool policies are included,
along with source/output reference mappings, read-side declaration-faking host,
option/reference/output-collision diagnostics and construction queries. Output-path
validation never implies that output was emitted. In-process mapper interfaces,
transform/result validation, supplemental sources and complete SpanMap algorithms are
included; external mapper execution is not.

The [function record](program-functions.json) separately accounts for selected and
external compiler declarations, finite supporting dependency scopes, adaptations,
source review and aggregate runtime evidence. Source-file mapping remains direct;
the two emit-path leaves required for output diagnostics remain explicitly mapped to
Program, and the mapped AST helper exception stays with its containing managed AST.
The record accounts for 303 declarations across 12 compiler source files (one is a
package-only file): 291 selected and 12 external. The 21 finite supporting source
scopes account for 173 selected and 13 external declarations, separately from generated
resources and small language adaptations. These totals describe responsibilities,
including retained earlier code; they are not counts of newly written functions or
per-function execution coverage.

## Verification results

| Evidence | Result |
| --- | --- |
| xUnit, including all retained suites | **24,598 passed; zero failures/skips**. Program contributes 264 cases; three cheap retained consumer executions are separate repeats. |
| Program/dependency differential corpus | **186/186 exact** under pristine Go, managed .NET and actual Linux x64 Native AOT. |
| Shared checks | **78 passed** in managed and native execution; provenance below. |
| Repeated-order checks | **76 executions per runtime**, matching Go. |
| Ordinary disk inspection | **2/2** complete outputs and exits agree with Go, under managed and Native AOT execution. |
| Original Go assertions | **76 bodies passed**, including the four whose managed counterparts are explicitly adapted. |
| Retained acceptance | Resolution, configuration, project, collection, callable, earlier checker, binding, parser and scanner gates passed. |
| Native qualification | **15 executable paths** inspected as Linux x64 ELF without CoreCLR dependency. No other native platform is claimed. |
| Frozen gate | **982.578 seconds**, exit 0; all five drift checks clear and 187 historical fixture/proof files preserved. |

Use the [guide](PROGRAM.md) to reproduce the complete gate or ordinary inspection.
The example constructs 60 source files, including 57 genuine ES2022 library files,
application types and source/reference relationships. Its missing-path variant preserves
TS6053 and exit 1. `semanticCheckingRequested` remains false: this demonstrates real
environment assembly, not general semantic checking.

## Language and library adaptations

Managed locks, `SemaphoreSlim`, lazy publication and task scheduling replace Go runtime
primitives while preserving the required publication, checkout/release and recursive
queue behavior. Callback execution is kept outside collection locks. These are behavioral
adaptations, not attempts to reproduce internal `sync.Map` or goroutine storage.

`System.IO.Hashing` 10.0.8 supplies genuine XXH3-128 for mapper identities, paired against
Go's actual hash and declared-option JSON bytes. Static option descriptors avoid runtime
reflection for that serialization. The selected typed JSON matrix decoder preserves
Go's partial-value/error behavior; exact Go error-struct metadata is retained only by
the oracle as a diagnostic aid. The oracle canonicalizes only the source-sanctioned
randomized modal verb on an actual JSON SemanticError; compiler diagnostics and user
text are not normalized. Static embedded resource streams preserve genuine library
bytes. Explicit raw-byte filename roles retain upstream text semantics at public
Unicode display boundaries.

## Acceptance observations

The ordinary command is `tscheck --inspect-program <tsconfig>`. It reports the loaded
environment and configuration, syntax, binder, Program and inclusion diagnostics.
It does not acquire a checker or imply semantic success. The public upstream inclusion
query retains its actual option/source-dependent skipping; all loaded files and
resolution results are observed independently. This policy was fixed before acceptance,
without weakening an accepted expectation or adding a private Go observer.

Paired observations use pristine pinned Go NewProgram and equivalent hosts/options.
They compare file and library closure/order, source metadata, reference relationships,
resolution outcomes, complete observed diagnostics and locations, ExplainFiles output,
local-generation behavior and public mapper results. Original assertions additionally
exercise required identity, pool and lifetime invariants. Private storage topology is
not an equivalence requirement. The [test inventory](program-tests.json) distinguishes
full assertions, packaging adaptations, changed diagnostic channels and semantic-blocked
subsets; none count as full compiler baseline passes.

The outstanding nonempty type-reference diagnostic-publication path has a genuine
resolver-factory witness: the actual default resolver is decorated with actual AST
diagnostics and Program publishes them through its original consumer. This establishes
the publication behavior, including filtering and automatic-type handling. It does not
claim that the default type resolver naturally generates every nonempty diagnostic.

The original test inventory contains 106 bodies across 11 files:

| Disposition | Bodies | What is established |
| --- | ---: | --- |
| New full assertion-preserving ports | 71 | Original input domains and assertions retained. |
| Full assertions with packaging adaptation | 1 | TestTestingLibPath uses the managed test-host location, with all original assertions. |
| Retained full original | 1 | MapperDiagnosticName remains in the accepted configuration suite; not counted again among new groups. |
| Adapted diagnostic publication channel | 3 | Original expected construction diagnostics, observed through construction APIs instead of the unsupported general semantic entry. |
| Adapted construction subset | 1 | Original generator mapper input/environment retained; global/incremental generator semantic assertions are blocked. |
| External process protocol | 27 | Mapper process/IPC executor is outside the contract. |
| External writable filesystem | 2 | Read-side cached VFS does not claim write forwarding. |

The 78 new shared groups comprise the 72 full-assertion groups, four adapted groups
and two additional authored scheduling/semantic-boundary controls. Parameter rows and
differential requests are not additional upstream test bodies.

## Contract reconciliation

All six contract gates are satisfied within their declared boundaries. This does not
claim exhaustive correctness or complete Compiler/checker parity.

| Contract gate | Implementation and evidence |
| --- | --- |
| P1 — Program/host lifecycle | Original seven Program and seven CheckerPool assertion groups; genuine construction, local Update/Reuse, eight generation controls, repeated-order controls and explicit semantic-acquisition boundary. |
| P2 — source environment | Genuine 115 bundled resources; default/explicit/transitive/replacement libraries, automatic/explicit types, JS depth, resolution settings, redirects and casing covered by 29 environment cases plus full-environment cases. |
| P3 — project references | Three mapped reference files; source/output/DTS-faking read behavior, recursive configs, missing/cyclic states and validation, including original reuse assertions and the core matrix. No project build or declaration generation. |
| P4 — diagnostics/queries | 60 core option/output/reference comparisons, eight factory publication cases, lifecycle location checks and complete ordinary inspection outputs. General checker-owned computations remain explicit external consumers. |
| P5 — mapped sources | Complete SpanMap unit and 31 original bodies, three ParseResult bodies, 21 mapper paired observations and actual identity/JSON dependencies. Mapper semantic subsets and external process tests are separately classified. |
| P6 — verification/delivery | Pristine Go, xUnit, independent source/assertion/omission review, native Program and CLI, retained cascade, source mapping, reproduction and checker roadmap. Recorded proof and final delivery documents are included in this checkpoint. |

## Limits and downstream assumptions

An environment may construct correctly while its checker operations are unsupported.
Actual semantic acquisition retains loaded options/files and only admits the already
accepted checker environment. No options are rewritten and no libraries are silently
injected or omitted. General global/semantic/declaration/suggestion computation,
async/generators, broad declarations and arbitrary libraries still require checker work.
The original generator mapper test therefore remains semantically blocked; its
separately executable construction observations are explicitly adapted evidence.

Other external responsibilities are emitter/build/watch/services, mapper subprocess/IPC,
new writable filesystem forwarding, the nonembedded Go packaging variant,
ThrottleGroup, and enabled performance/type tracing. Host diagnostic tracing is real.
Virtual Windows/UNC/case hosts establish their path algorithms only; actual platform
acceptance is Linux x64. The disk host accepts scalar UTF-8 paths and rejects unsupported
encodings, retaining the [documented host boundary](RESOLUTION-ASSESSMENT.md#adaptations-and-boundaries).

Downstream work may now rely on the selected Program construction APIs,
real environments, local lifecycle, diagnostic/query policies and declared host
boundaries. It may not infer that all of the Compiler package, all of program.go, or
arbitrary semantic checking is complete. The next campaign should close genuine
checker initialization and the declaration/object model; the
[checker roadmap](CHECKER-COMPLETION-ROADMAP.md) defines substantial follow-on campaigns
and the eventual whole-checker no-emit gate.

## Execution and efficiency

Three existing GPT-6.1 Sol workers were reused with disjoint producer/test ownership
and cross-owner review. A finite dependency packet moved off the loader owner's critical
path; source-authored option cases also used the otherwise available core worker.
Central incremental builds fed immutable consumer snapshots; targeted comparisons
preceded the final complete gate. No broad accepted-foundation audit or extra private
state framework was introduced.

Source review and focused comparisons found real missing branches before acceptance:
current-generation diagnostic ownership, redirected-child scheduling, duplicate casing
ownership, nil module-cache handling, raw-byte configuration filename roles, and relative
mapped-file explanations. An empty observation representation mismatch was separately
classified as a harness defect. Expected upstream behavior remained unchanged.

Early mechanical integration failures and stale consumer assemblies cost avoidable
work. Normal incremental dependency builds, cheap consumer probes and one designated
build owner reduced that repetition. The central build ledger records 10 invocations (six clean, four mechanical failures)
and 47.08 seconds of invocation wall time. This excludes consumer builds, Go generation,
Native AOT publishing, test execution and coordination; it is not total engineering
effort. The single full final gate took 982.578 seconds, including original Go replay,
builds, publishing and all retained execution; this is not a compiler performance
benchmark. Its five drift checks were clear and 187 historical fixture/proof files were
unchanged.

The post-run recorder initially indexed the nested retained test summary one level too
deep. A one-line bookkeeping correction was independently reviewed; the successful
gate/log and all production/runtime evidence were preserved. The proof explicitly
records the pre/post recorder and asset hashes. The original fingerprint was
reconstructed with only the preserved recorder bytes substituted, proving there was
no unrelated change. No full runtime rerun or expectation change was needed. Token use,
model cost and complete person-time are unavailable. No additional human intervention
or package installation was required during this campaign.
