# Whole-checker no-emit assessment

**The whole-checker no-emit contract is complete and accepted on Linux x64.**
The [durable proof](../validation/results/checker-noemit-verified.json) records
complete managed and actual Native AOT agreement, retained regressions and unchanged
historical evidence. This establishes the agreed checking contract, not exhaustive
correctness, complete historical emit-test success or full compiler parity.
Use the [saved-reference reproduction guide](CHECKER-NOEMIT-REPRODUCTION.md).

## Scope and implementation

The [whole-checker mandate](CHECKER-COMPLETION-PROMPT.md) continues from `bb035b3`;
the protocol checkpoint `c5e96e1` is the immediate predecessor of the final
[language contract](LANGUAGE-CONTRACT.md). The upstream revision remains
`fed0bf24149fb1ed36039212648bafdafc1ea10e`.

Accepted initialization/declaration, expression/statement/flow, shared type-system
and async/generator/protocol work supplies the foundation. This last campaign
completes JS/JSDoc, JSX, module interoperability, decorators, option-dependent
checking and the diagnostic/type/symbol display dependencies required by genuine
Program checking. The original compiler/conformance inputs exercise these together
under their actual configurations and library environments.

The current [core review](progress/language-core.md),
[support review](progress/language-support.md) and [source map](../source-map.json)
are the current overlay to the preserved pre-language inventories. Both owners
report no known unfinished in-scope no-emit consumer after independent source review
and repair of whole-run findings. That is an implementation/review disposition;
it does not mean that every branch or combination has been executed.

Go-to-C# correspondence remains direct. The new JSX unit has its own counterpart;
existing counterparts carry the mixed-language and display work. Meaningful
adaptations retain Go byte-string, nil, identity and slice-publication behavior
where .NET differs. The related-diagnostic append repair explicitly preserves Go
slice-header publication. Local template-product lists use owned List.Add rather
than cloning on every append; original guards, ordering and publication remain.
No alternative checker or fixture-specific semantic implementation was introduced.

## Original test inventory and observations

The [test catalog](checker-noemit-tests.json) records individual provenance and
dispositions. Totals describe different units and must not be added indiscriminately.

| Inventory | Disposition |
| --- | --- |
| 12,824 original compiler/conformance files | All accounted for; compiler inputs run through an assertion-preserving no-emit adaptation, not complete emit tests. |
| 12,782 originally non-skipped files | 15,291 checking configurations after actual original BOM/UTF-16 decoding. |
| 42 originally skipped files | Ten API-environment fixtures and 32 removed-option fixtures are individually reconciled below. |
| Ten API-environment fixtures | Ten fresh checking records using authentic locked public TypeScript 6.0.3 declarations; one retains two unavailable internal declaration dependencies. |
| 32 removed-option fixtures | Three checking records; 22 files produce 26 original option-preparation failures; seven produce original configuration-expansion failures. |
| 15,304 complete checking records | 15,291 standard plus 13 supplemental records; complete managed and actual Linux x64 Native AOT comparisons passed. |
| 33 preparation-failure cases | Exact original failure stage/text checked separately; not semantic successes. |
| Three additional harness cases | Authored decoding, filesystem-identity and removed-option controls. |
| Two direct upstream checker test functions | Complete original assertions retained through the earlier regression cascade. |
| Benchmark and enabled-tracer test | External performance/instrumentation operations, not silently passed or skipped checking assertions. |

The whole managed xUnit run passed 15,340 cases with zero failures or skips. Every
one of the 15,304 saved actual checking records equals its pinned-Go reference.
Those records compare complete pre-checking/checking diagnostics with ownership,
locations, chains, related information and ordering; original type/symbol baselines
with their traversal and formatting; and resolution traces where enabled. Original
within-runtime union-order and AST-parent assertions also execute. No exhaustive
cross-runtime private-storage comparison is required.

The historical raw inventory remains preserved. Actual original disk decoding added
101 net standard configuration variants; that correction did not change fixture
bytes or expected behavior. Missing, crashing or timed-out records were failures to
resolve, not exclusions. The first full feedback run's 127 failed obligations were
closed before the subsequent complete managed run passed.

## Acceptance and limitations

Actual Linux x64 Native AOT publication and all 15,304 complete checking comparisons
pass, including the original runtime assertions. The native metadata helper has
reproduced all 12,824 fixture-preparation records byte for byte, including seven
original expansion failures. All retained foundation, binder, semantic, configuration,
resolution, Program and project regressions pass. The selected aggregate is 40,291
passed xUnit case executions, not unique upstream tests; predecessor executions can
repeat. The proof identifies 24 actual Linux x64 ELF executable paths without a
CoreCLR dependency. All five drift guards are clear and 287 historical evidence
files remain unchanged. Other native platforms have not been qualified.

Acceptance explicitly composes the successful managed run from the first attempt
with the completed retained suffix and fresh native execution after two reviewed
test-harness repairs. The old managed binary and input bindings are preserved;
only a retained host adapter and metadata-only serialization changed. Every new
native checking record executes the corrected native consumer. The private helper
used by the authored host test also passes its original assertions under Native AOT.
Failed attempts remain recorded; no failed channel is counted as a pass.

The proof SHA-256 is
`1654872f04385a8df8471716c7f6213596a748a207cc4a000e4f081f3ef5c2cf`.
Its source fingerprint is
`7e893ae12cc3f6f57c79a016da8fd177cb954fafec802809d4150a90dee88f2c`;
verification assets are
`3361a73567a8e54ba61f2da46f09c323bdc59924f6e39c83d305c1a8365fa269`.

The unavailable historical internal API declaration artifacts remain a verification
limitation: unchanged input reports actual missing-import diagnostics under the
genuine public-package environment. Agreement with fresh Go there is not equivalence
to running the unavailable historical built environment. No declaration stand-ins
or diagnostic suppression compensate for that gap.

JavaScript/declaration/source-map output, diagnostics requiring actual declaration
output or Emit execution, incremental builder/cache scheduling, enabled tracing, and language
services remain outside the no-emit contract. Actual incremental options and inputs
remain present, with C# performing cold semantic recomputation. Mapper callbacks
execute in process; subprocess IPC is not claimed. Historical named-profile harness
restrictions do not constrain genuine Program checking.
Semantic metadata, visibility and display helpers remain included even when their
source files have emitter-related names.

## Execution lessons and observed effort

Three GPT-6.1 Sol workers were reused for implementation, verification and independent
review, with Astra coordination. Twelve central production-library build invocations
took 101.58 seconds and produced eight immutable candidates. Build times are not
total implementation effort. The full managed acceptance stage took 364.881 seconds;
its recorded peak RSS was 6,499,196 KiB. These are verification observations, not a
performance comparison against Go or JavaScript tsc. Token and monetary usage were
not available and are not estimated.
The 120 native checking chunks took 410.669 seconds of summed process time. This
differs from total elapsed campaign time and excludes publishing and other gates.
The recorded qualified phase sum is 2,428.190 seconds: initial attempt 547.552,
remaining retained sequence 1,458.479, successful native publish 11.490 and native
chunks 410.669. Native checking and retained execution overlapped, so this sum is
not wall-clock duration. Two failed AOT publishes took 3.064 and 2.917 seconds and
remain separately recorded. No package installation or scope intervention was
required for this final campaign; user feedback improved the review discipline.

The original full harness and whole-input comparisons found partial function bodies
that earlier name inventories and finite capability tests had missed. The corrective
review practice compares each repaired upstream function and directly involved
helpers, not only changed lines. Whole-subsystem assertions should be established
earlier in future porting work.

Central source snapshots, disjoint ownership and targeted failure replays reduced
rebuilds. Late authored test-host interface drift and metadata-only AOT serialization
errors still caused avoidable acceptance interruptions. Their fixes preserve original
assertions and unchanged compiler source; explicit evidence composition retains the
valid managed run while requiring new native execution. Future affected-harness
compile and AOT checks should precede the expensive combined regression run.

## Next step

The whole-checker gate has passed. Stop implementation and report readiness for
the separately authorized large-project correctness and performance evaluation with
emit disabled. Such evaluation should keep genuine project/library environments and
disable observation-only overhead. It is not authorized by this assessment and has
not been started. Emit and language services remain separate implementation work.
