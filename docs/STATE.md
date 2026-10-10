# Active performance work (2026-10-10)

**Resume here:** the internal `Slice<Signature>` rollout after checkpoint `2e69b54`
is qualified for an intermediate checkpoint. Native AOT VS Code and focused checks
pass; matched allocation is down2.435%. See the evidence below. Next selection is
inference candidate buffers versus the source-shaped markNodeAssignmentsWorker
visitor allocation path, using the saved joint allocation profile.
Sol verifier owns heavy jobs; source-only implementation may proceed against a
frozen7bab baseline. No broad final regression run is scheduled.

**Latest authorization: continue allocation and UTF8 work regardless of GC results.**
User explicitly directed ongoing allocation/string-copy reduction and byte-backed
ReadOnlySpan<byte> work. Sol scanner owns the approved owned-byte loader/source/view/parser/scanner pipeline
and JSDoc text-fragment Slice headers; support owns reusable tests and independent
review. Public byte[] inputs copy; trusted internal buffers transfer ownership;
public text projection is explicit, internal source consumers keep views. Preserve
BOM/invalid/WTF8/position/alias semantics and existing identifiers/string-key boundaries.
The owned-byte packet is implemented and behavior-qualified; controlled performance
measurements are complete on frozen binaries; the span-helper follow-up has an early native workload result. Contract and current findings: [VS Code
status](VSCODE-CHECK-STATUS.md#owned-utf8-source-pipeline-active-completion-contract).
Heavy workloads remain serialized.

**Current follow-ups:** byte-span helpers (native VS Code 33.25s) and scanner/template
intermediate-copy removal (37.66s) preserve all ten complete diagnostics; single-run
timings are descriptive, not speedup evidence. Parser list scratch buffers now use
`Slice<Node>` with upstream capacity 16 and an intentional final detached public-list
clone; clean build `622cd762`, [first native workload](../artifacts/vscode-parser-slices/first-vscode/result.json)
28.45s /118.47 CPU seconds /12,000,996KiB peak, preserving all ten complete records
and pinned chains. The [token-projection run](../artifacts/vscode-token-projection/first-vscode/result.json)
was37.66s /143.87 CPU seconds /12,511,004KiB peak. These are single observations,
not matched speedup claims. Source and independent review artifacts are under
`artifacts/vscode-{byte-span,token-projection,parser-slices}`.
Upstream keyBuilder's 192-byte buffer/spill and XXH3-128 key now replace the
List/array/Base64 construction. Snapshot `4110af9b`, ten genuine Go hash witnesses
pass; native VS Code 28.24s /122.88 CPU seconds /11,496,168KiB, ten records exact.
Existing named Collections observations retain a lazy per-checker preimage sidecar;
ordinary checking passes null and retains no preimages or observation identities.
The subsequent combined lazy page-cell and final hex/regexp comparison packet
(`5b6220a8`, `artifacts/vscode-lazy-link-cells`) passes native VS Code in 27.24s
/116.63 CPU seconds /10,564,812KiB, ten records exact. Its matched CoreCLR profile
is complete: Server CoreCLR estimated sampled allocation weight34.106→28.335GiB
(−16.92%). This is one profile each across combined byte-span/token/parser/key/cell
changes, not per-packet causal isolation or exact allocated-byte totals. No lost
allocation events or truncated trace; inclusive groups overlap and are not summed.
[Profile comparison](../artifacts/vscode-lazy-link-cells/allocation-comparison.json).
The measured `Range<Type>` iterator remains about0.766GiB in the combined profile.
Support's independently reviewed List Range value enumerable compiled cleanly in
one9.54s build, frozen DLL `e9fac1c6`; only the existing runtime adapter changed.
Its [first native workload](../artifacts/vscode-list-range/first-vscode/result.json)
reports31.95s /120.62 CPU seconds /11,480,348KiB peak, all ten complete diagnostic
records and available pinned chains exact. These native timings are single
observations, not matched speedup claims. The [focused checks](../artifacts/vscode-list-range/focused/report.json)
passed in82.232s against frozen `e9fac1c6`: existing Collections44/DataTypes92/page1
comparisons137/137, plus26 shared managed checks and the same26 under Native AOT
(key10/Range3/token5/parser2/span6). Inputs/expectations unchanged; project-reference
builds disabled, both test outputs consumed the frozen DLL. This is targeted
regression evidence, not a new broad acceptance gate. Direct foreach now uses a
value enumerator; interface routes remain supported. Prior managed live-Count
semantics remain unchanged, distinct from Go's captured slice-header length.
No identifier/symbol/path byte-view migration has begun: user agrees to wait for
profiles before prioritizing that larger change.

**Last qualified intermediate checkpoint:** source was frozen and
qualified as Scanner DLL `7bab3765`, native CLI `3b524d18`, under
`artifacts/vscode-range-pathkey`. Native VS Code:23.33s /95.87 CPU seconds
/11,411,500KiB peak, all ten complete expected diagnostics and available pinned
chains exact. Focused10managed (seven range/three PathKey) and seven actual Native
AOT range controls passed. All source changes independently reviewed; full
regressions remain deferred by user direction. This is performance work in
progress, not final acceptance or a speed-parity claim.

The preceding `fe6cb0db` batch also passed five genuine-Go complete relation records
and four scanner snapshot controls. Its relation fix restores first call/construct
signature comparison using the existing Relater, correcting the earlier all-
overloads diagnostic policy. Existing expected records were not weakened.
The package-scoped Go `-m=2` escape-report attempt was stopped after135.672s at
observed16.04GiB Go-process RSS with no escape-analysis conclusion; no retry.
Evidence: `artifacts/vscode-generic-signature-state/go-escape`.

Progress commit is `2e69b54` (clean tree at creation). The next authorized packet
is an end-to-end internal `Slice<Signature>` rollout through structured/declared/
composite storage, resolution, signature inference, and owned overload candidates.
Public List-return APIs retain explicit nil-aware detached snapshots. Other Type,
Symbol, IndexInfo, and Node storage stays unchanged; outer lists of signature
headers may remain owned `List<Slice<Signature>>` collections.

Contract: preserve Go function structure, nil/empty producer normalization,
clipped public-type versus unclipped checker views, shallow element identities,
Core.Same owner/start/length rules (capacity ignored; all empties same), clone-on-
first-change maps, and independently owned candidate mutation. No dual persistent
List/Slice storage or temporary per-call projection internally. Restore exact
zero/one/two signature-family producer branches. Shared helpers are only those
required by this signature chain. Inline header sizes grow, so allocation/RSS/speed
benefit must be measured. This is signature storage completion within ongoing
allocation work, not compiler-wide Slice or optimization completion.

Ownership: Sol scanner owns Checker.cs/Types.cs/Exports.cs; support owns shared
Slice/Core/runtime helpers and downstream signature consumers; verification owns
independent review, targeted Go-backed controls and the sole build window. Plan
one integration build after static readiness, then actual native VS Code first.
Afterward use existing Callable89/Collections44 plus relevant generic-callable
rows and finite header/behavior controls, then one allocation comparison against
frozen7bab or the documented olderfe6 baseline. No comprehensive regression run.

**Signature rollout first workload:** central integration needed one grouped repair
(nine mechanical binding errors), then built cleanly in5.867s. Frozen library
`cd2b47bb`, Native AOT CLI `c7a34224`, under `artifacts/vscode-signature-slices`.
Actual Linux x64 Native AOT VS Code:22.73s /97.14 CPU seconds /11,323,472KiB peak;
Server GC true/16, exit1, all ten complete diagnostic records and available pinned
chains exact. This is one observation versus7bab23.33s, not a causal speed claim.
Focused checks pass: eight new managed controls, four native shared boundaries,
four native complete Go Program records, Callable89, Collections44 and TypeSystem77.
Named observer adapters preserve schemas/expectations; an older Slice<Node> observer
cast failed first and was corrected only in the harness. Independent source review
found no material issue after grouped binding repairs. No full regression gate is
scheduled.
Matched sampled interval allocation22,490,726,952→21,942,971,296bytes (20.946→20.436GiB,
−2.435%). Signature Lists451,909,624→0; signature arrays234,309,112→68,657,256bytes.
Candidate zero loss/truncation,192899ticks with stacks; baseline one missing stack.
One sampled trace per version, not exact census or native allocation totals. Final
comparison and joint type/caller analysis are saved under
`artifacts/vscode-signature-slices/{allocation-comparison.json,candidate-allocation/joint.json}`.
No heavy job is running. Existing Sol support investigates inference candidate ownership;
Sol verification investigates the visitor path; both are read-only until selection.

**Pre-signature allocation baseline:** exact frozen7bab managed CLI, Server GC,
`artifacts/vscode-signature-slices/baseline-allocation`:22,490,726,952 sampled
interval bytes (20.945GiB),198091ticks, zero lost/truncated events; one tick has no
stack. Ten diagnostic DTOs exact. CLI refsfalse build2.242s, no library rebuild;
profiled wall25.73s is not a benchmark. This supersedes fe6 as the baseline for
signature migration. Earlier fe6→7bab accumulated Append/range changes reduce
23.289→20.945GiB; no signature benefit is included yet.

**Latest profiles:** matched Server CoreCLR captures show sampled allocation weight
28.3346→23.2893GiB (−17.81%), `5b6220a8`→`fe6cb0db`; ten diagnostics exact,
zero lost events/truncation. This covers accumulated range/snapshot/copy/callback
changes, not isolated effects or exact Native AOT allocation totals. It predates
the qualified `f9ffa800` and `7bab3765` follow-ups. Comparison:
`artifacts/vscode-relation-inference/allocation-comparison.json`.

Actual native CPU capture on `f9ffa800`/CLI `165e1011`:9018samples, zero lost,
55.66% of sampled user CPU in Server-GC/allocator runtime leaf functions. Worker
caller chains did not unwind; do not claim precise source callers, wall-time
shares or exhaustive exact GC CPU. The graph is deliberately a flat leaf view:
`artifacts/vscode-append-capacities/native-cpu/native-cpu-flamegraph.svg`.
Full findings: `artifacts/vscode-append-capacities/findings.json`.
This supports continued allocation reduction, not a claim that it explains every
remaining performance gap. No global List→Slice or name-storage rewrite has begun.

**Allocation attribution:** the existing 5b trace was reprocessed once (4.10s), not
recaptured, to correlate allocated type and full stack/nearest compiler caller.
`artifacts/vscode-lazy-link-cells/candidate-managed/allocation-type-callers-ranked.json`
conserves all 272,130 allocation ticks and 30,424,041,664 sampled interval bytes;
zero loss/truncation/group overflow, originals unchanged. Disjoint type groups:
arrays9.967GiB, closures3.054, Lists2.477, Func/Action2.286, iterator state machines
1.185, strings1.016, other8.350. These are sampled type attributions, not guaranteed
removable allocations or Native AOT totals. Type+caller pairs identified signature
checking Node[]0.842GiB, named-members Symbol[]0.336, inference predicate0.303,
and generic-call-signature capture0.260. Prefer those actual pairs to inferences
from unrelated type and inclusive-stack totals. Go baseline alloc_space5.176GiB
versus managed28.335GiB is roughly5.5x, with different profiler/runtime limitations.
User reiterates continued progress only with close upstream fidelity; flag material
architectural departures. Do not call allocation work complete or schedule a final
broad regression merely because another small packet passed.

User explicitly corrected premature batch finalization: performance remains far
from the target. Cache keys are another step, not the last larger packet. Continue
build → native VS Code → profile/targeted investigation into the next measured
bottlenecks. No final broad regression run is planned now; reserve that for actual
finalization or a concrete correctness risk. These changes do not complete
compiler-wide slice/storage adaptation. Saved allocation leaf rankings
under `artifacts/vscode-byte-source/candidate-managed/visible-allocation-leaves.json`
separate method partitions from overlapping type/group observations.


**Owned-byte integration: behavior-qualified; mixed timing, lower sampled allocation.**
Snapshot1 compiled cleanly on invocation2 (8.88s MSBuild), Scanner SHA `4d79859d`;
35 source files changed. The [snapshot](../artifacts/vscode-byte-source/build/snapshot-1/snapshot.json),
[focused gate](../artifacts/vscode-byte-source/focused/report.json) (61/61, 33.973s),
and independent core/VFS reviews bind the implementation.

All functional stages of the comprehensive run passed in 1238.374s: 38,868 managed
assertions, including the current 15,395 no-emit cases and 15,304 complete original
checking records; native Foundation 3153 + view17 + fragment13 + byte12; fresh
Go/native parser 17,723 and scanner 17,723; native no-emit 15,304 exact across 120 chunks;
Program78/regex3/arrow4/filename11; Projects 27 controls and 48 complete records.

The original [gate report](../artifacts/vscode-byte-source/final-regressions/report.json)
remains failed at its final source/input guard. The only production-source delta was
one removed trailing ASCII space in a scanner comment. The separately approved
[qualification](../artifacts/vscode-byte-source/final-regressions/postgate-qualification/review.json)
preserves both source hashes and the exact diff. Managed test copies and earlier
native builds used frozen `4d79859d`; later Program/Projects native publishes used
`bcfc6f7c`, rebuilt after that comment-only change. Six actual native executable
hashes are recorded. Current source is not claimed byte-identical to the snapshot;
no broad test rerun or source restoration was needed.

The [first full native VS Code run](../artifacts/vscode-byte-source/first-vscode/result.json)
completed in 37.96s /154.56 CPU seconds /12,096,368KiB peak, exit 1 with empty stderr.
All ten complete diagnostic records and available pinned-Go chains match. This is
one correctness/timing observation. The completed [C,B,B,C comparison](../artifacts/vscode-byte-source/timing/comparison.json)
has baseline mean31.095s versus candidate33.205s (+6.79%); CPU+5.48%, peak+0.28%.
Ranges overlap; no native speedup is established. Matched Server CoreCLR sampled
allocation weight35.168→34.106GiB (−3.02%), strings2.594→1.907GiB (−26.46%).
Byte arrays and JSDoc descriptor arrays also decline; overlapping frame weights are
not summed. [Compact findings](../artifacts/vscode-byte-source/final-findings.json) bind the evidence.

The subsequent byte-span helper candidate preserves all ten diagnostics in its
[first native VS Code run](../artifacts/vscode-byte-span/first-vscode/result.json):33.25s,
143.07CPU seconds,11,564,804KiB peak. One observation is descriptive, not a matched win.

**User-directed sequencing for subsequent packets:** build, then actual Native AOT
VS Code feedback first, then targeted issues/controls as needed. Run comprehensive
regressions only before finalization; do not put the twenty-minute broad gate ahead
of initial workload feedback. Preserve frozen binaries/references and serialize heavy jobs.

Four pinned-Go arrow witnesses retain U+2028/U+2029/LF TS1200 and valid-space control;
the rune-based source check repairs a prior char-per-byte grammar omission. Existing
expected records were not rewritten. The matched Server GC allocation baseline is
[recorded separately](../artifacts/vscode-utf8-ingress/server-baseline/qualified-summary.json).

**Server GC accepted; subsequent source work is active.** CLI defaulttrue; actual
native false/true controls and managed defaulttrue report16processors. Four matched
native runs preserve all10diagnostics. MeansWKS60.565→Server28.49s(−52.96%),
CPU134.79→123.76(−8.18%),peakRSS10,963,370→12,178,528KiB(+11.08%).
No heap tuning, no swapping. Frozen accepted serverCLI SHA722d03c2, Scanner7ac3e4f2.
Evidence `artifacts/vscode-server-gc/{performance-build/binding.json,timing/comparison.json}`.
Support final result review passed. The matched Server baseline and owned-byte
integration are complete; the current measurement owner and gate qualification are
recorded above. Build/test/profile jobs remain serialized.

**Completed selected JSDoc text-sharing work; performance result is mixed.**
The initial JSDoc source views/regex raw handoff substantially reduced copying:
mean native65.72→54.76s, sampled allocations46.334→36.928GiB. Filename byte-role
sharing then reduced sampled allocations36.928→35.181GiB and its isolated native
means62.77→60.715s (small/noisy). Both accepted with complete diagnostic agreement.
Evidence: `artifacts/vscode-jsdoc-view/final-findings.json` and
`artifacts/vscode-text-followup/filename-final-findings.json`.

Latest comprehensive JSDoc packet implements all seven upstream text slices plus
associated scanner-token, trim/indent/pragma/normalization helpers and four AST
comment/link payloads. Public mutable string lists project once and remain shared
across clones. Detached unprojected nodes intentionally retain source backing like
Go. Source-level public access is preserved; field→property bridges do not claim
binary/reflection ABI equivalence. Narrow generator reproduction covers all affected
families; historical unrelated full-file generator drift is separate.

Final correctness gate PASS378.387s:23,496 managed assertions, native Foundation
3,153+17+13, fresh complete Go/native parser 17,723/scanner 17,723, noemit21,
Program78+filename11+regex3, Projects27+48whole records. No source/input drift.
All four native VSCode runs and the profiled managed run preserve ten complete
diagnostics and available pinned-Go rendered chains/locations. All use skipLibCheck.
Compiled Scanner SHA7ac3e4f2; frozen WKS CLI5bb8edd5. Evidence:
`artifacts/vscode-jsdoc-fragments/{source-reconciliation.json,final-findings.json,
final-regressions/report.json,build/snapshot-1/snapshot.json}`.

**No incremental speed/net-allocation win from the final fragment packet:** native
means61.77→62.42s(+1.05%, overlappingranges), CPU162.405→161.78s, peak−0.68%;
sampledtotal35.181→35.284GiB(+0.29%). Strings fall2.715→2.609GiB but new view arrays
account for0.283GiB; JSDoc inclusive weight grows0.230→0.359GiB. Source-sharing
completion is accepted separately from performance. Profiles are sampled CoreCLR,
not exact NativeAOT allocations; no complete GC-pause claim from this trace.

**Boundaries and next direction.** Identifier strings/symbol keys, public outputs and
joined text still materialize. The owned-byte source path and JSDoc fragment Slice
headers are implemented; this does not establish allocation-complete Go parity.
Existing List/arena adaptations outside that packet remain separate. Invalid/WTF8,
escape and byte/UTF16-position semantics were retained and tested. Further append/list
and cache-key work remains a measured candidate, not an implemented optimization.

**Read-only next candidate (not implemented):** core assessed keyBuilder's existing
class/List/ToArray/Base64 path (~1.2GiB overlapping sampled group). Pinned Go uses
192inline bytes + spill and XXH3-128. Existing System.IO.Hashing can support a value/
stack builder and128-bit key, with ref recursion and exact byte order/sentinels.
Existing named Collections preimage observers are a concrete compatibility boundary;
ordinary Program must not retain preimages just for them. No new observer framework
or unconditional private-state equivalence requirement. Validate Go vectors/spill/
recursivewriters and actual semantics before measuring. Defer code until current
owned-byte gate/performance finish; this is a proposal, not a performance claim.

**Preserve work.** All performance changes remain uncommitted; no new commit was
requested. Preserve older Slice/inference work (correctness-qualified but earlier
native timing+8.77%, not a speed success). No expected diagnostics were weakened.
HEAD9629210; Go pinfed0bf24149fb1ed36039212648bafdafc1ea10e. Three existing GPT-6.1
Sol agents reused under Astra coordination. No new packages/human intervention needed;
token/cost measurements unavailable. Details/reproduction in [VSCode status](VSCODE-CHECK-STATUS.md).

# Historical checkpoints (read newest performance state above first)

# Current project state

**Completed investigation: baseline VS Code CPU, GC and allocation profiles.**
User authorized profiling, flame graphs and a pinned-Go allocation comparison.
No compiler changes were made. The unchanged `9629210` Native AOT CPU capture has
12,865 samples, zero reported loss: identified workstation-GC functions account
for 51.74% of sampled user CPU, including 17.69% in a GC spin-lock helper. A separate
native GC trace has 2,614 complete collections and 36.739s GC suspension intervals
out of 61.950s (59.30%, including handshake/restart overhead). Managed CoreCLR
allocation weights estimate 48.443 GiB; Go alloc_space estimates 5.176 GiB, with
sampling/runtime/final-GC-cycle caveats. This is not an exact native allocation ratio.
All full captures retain the previously verified ten diagnostics. Four interactive
graphs and reproduction instructions are linked from the
[reviewed findings](VSCODE-CHECK-STATUS.md#measured-profiling-follow-up-2026-10-10).
Native worker call chains could not be unwound; its graph is explicitly flat.
Three existing Sol agents handled capture, analysis and independent review; four
full workloads ran sequentially. Evidence lives under `artifacts/vscode-profiling/`.
Recommended next controlled experiment: Server GC on the same baseline, measuring
uninstrumented time, CPU, peak memory and diagnostics. It has not been performed.
Candidate slowdown causality remains unresolved; no candidate profile, optimization,
broader slice migration or commit was made. Preserve the uncommitted candidate below.

**Evaluated, uncommitted: Go-style slices and inference stacks from `9629210`.**
Sol implemented array-backed value descriptors and migrated only the two inference
stacks; independent cross-review found no correctness issues. Shared-storage,
header-copy, nil/empty, bounds, overlap, range and reset behavior pass 23 xUnit and
actual Linux x64 Native AOT checks. Retained managed coverage passes 84 type-system,
14 prior fixes and 12 original inference rows: 133 managed cases total, zero failures
or skips. The genuine Go witness separately validates 16 public slice observations.
Both native VS Code candidate runs match all ten baseline/pinned-Go diagnostics.

Performance did not improve: balanced B,C,C,B means are baseline 62.17s / 142.10 CPU
seconds versus candidate 67.625s / 158.515 CPU seconds (+8.77% wall, +11.55% CPU).
Mean peak RSS is 3.10% lower but baseline variation overlaps candidates. This is a
correctness-qualified candidate, not a performance acceptance or broad-migration
recommendation. The user asks about actual allocation profiling; investigate that
before selecting further optimizations. No cache-key/GC/pooling changes were made.
Exact Go allocator-rounded capacity sequences remain excluded; these first stacks
do not depend on them. See [scope, evidence and reproduction](VSCODE-CHECK-STATUS.md).

**Post-acceptance VS Code follow-up:** the nil-distribution crash and ordinary
CLI raw-binding diagnostic policy are repaired; 14 focused xUnit cases and actual
Linux x64 Native AOT policy controls pass. The corrected full native check completes
in 62.37s with ten diagnostics matching the exact pinned Go (9.61s), including
owners, codes, locations and full rendered chains. Installed Go 7.0.2 reports zero;
this is revision divergence, not grounds to alter pinned relation behavior. No
optimization was made. This checkpoint preserves the fixes and tests; see
[findings and reproduction](VSCODE-CHECK-STATUS.md).
Historical acceptance proof and corpus remain unchanged. This follow-up completes
the authorized diagnostic repair and bounded performance assessment; no broader
campaign or speculative performance tuning is underway. A subsequent read-only
Sol investigation strengthens the performance hypothesis: allocating cache keys,
copying slice adapters, and larger object graphs are primary candidates, with
confirmed workstation GC a possible amplifier. See the findings' follow-up section;
no new compiler run or production changes were made for that investigation.

**Accepted: whole-checker no-emit completion from `bb035b3`.** Read the
[mandate](CHECKER-COMPLETION-PROMPT.md) and [live checkpoint](CHECKER-COMPLETION-STATUS.md).
The [L1–L6 contract](LANGUAGE-CONTRACT.md) and whole-checker gate pass actual Linux x64
Native AOT and retained regressions. See the [assessment](CHECKER-NOEMIT-ASSESSMENT.md),
[saved-reference commands](CHECKER-NOEMIT-REPRODUCTION.md) and
[proof](../validation/results/checker-noemit-verified.json).
All 15,304 complete managed/native records agree with Go; 15,340 new xUnit cases and
40,291 selected aggregate executions pass, zero failures/skips. All five drift guards
are clear; 287 historical files remain unchanged. The original 12,824-file inventory
is accounted for by checking, preparation-failure and explicit environment dispositions.
The historical internal API declaration gap is a verification limit, not suppressed.
**Stop here as authorized.** Large-project no-emit correctness/performance comparisons
need a separate assignment. Emit and services remain external. The sections below
are historical checkpoints, not active instructions to restart completed work.

## Accepted protocol checkpoint `c5e96e1`

[Assessment](PROTOCOLS-ASSESSMENT.md), [reproduction](PROTOCOLS-VERIFY.md),
[contract](PROTOCOLS-CONTRACT.md) and [proof](../validation/results/protocols-verified.json).
All 94 managed/native Go records, eight shared checks, 102 new xUnit cases, 22 order
observations per runtime and both native disk CLI examples pass. Seven original test
bodies are complete, including the restored generator mapper assertions. Selected TRX
aggregate: 24,951 passed case executions, zero failures/skips; not a unique-test count.
Gate: 1,518.981 seconds, five drift guards clear, 271 historical files unchanged.

The historical raw inventory records 12,824 original inputs and 42 original skips.
The [decoded supplement](../validation/checker-noemit-decoded-inventory/) corrects
configuration obligations using the original BOM-aware loader: 15,291 standard
configurations, plus 39 supplemental configurations and seven original expansion
failures. Those obligations are now reconciled in the accepted whole-checker proof;
the 39 supplemental configurations comprise 13 checking records and 26 original
preparation failures. Read the live checkpoint for current evidence.

## Accepted type-system baseline

**Accepted historical baseline: shared TypeScript type-system contract T1–T6 (`bb035b3`).**
Baseline `09630d6`; delivery commit contains this state and the
[durable proof](../validation/results/typesystem-verified.json). Read the
[assessment](TYPESYSTEM-ASSESSMENT.md), [checkpoint](TYPESYSTEM-STATUS.md),
[contract](TYPESYSTEM-CONTRACT.md), [inventory](TYPESYSTEM-INVENTORY.md) and
[verification guide](TYPESYSTEM-VERIFY.md).

All 77 complete pinned-Go records match managed and actual Linux x64 Native AOT,
including finite public inferred-type and 42 assignability observations with complete
diagnostics. Seven shared checks in four groups preserve six original bodies and one
authored control; 22 order/repeat executions per runtime and both ordinary disk CLI
cases pass (valid0, invalid1 with all six diagnostics, no missing globals).
All 84 new xUnit cases and retained gates pass. Selected-suite TRX aggregate: 24,849
passed case executions, zero failures/skips; not a unique-test inventory. Gate elapsed
1,492.728 seconds, all five drift guards clear, 256 historical files unchanged.

Source reconciliation and independent Sol review close the shared TS generic/inference/
relationship/advanced-operator and required tuple/callable/predicate responsibilities.
Original casing and demanded diagnostic-display support are included. Source review
and finite behavioral evidence remain separate; whole checker/printer files are partial.
The assessment records real source repairs, one harness-only failed gate and a narrow
exact-version observer provenance adaptation without changed expectations.

Recommended next: async, generators and runtime protocol checking, then remaining
JS/JSDoc/JSX/decorator/module/options consumers and whole-checker reconciliation under
the [roadmap](CHECKER-COMPLETION-ROADMAP.md). The current whole-checker mandate above now authorizes this sequence autonomously. Large-project comparisons, .NET upgrades and optimization remain
deferred. No new private-state instrumentation or token/cost estimates were added.

## Prior expression checkpoint

**Accepted: TypeScript synchronous expressions, statements and checker-flow E1–E6.**
Baseline `5efa4e5`; delivery commit contains this state and the
[durable proof](../validation/results/expressions-verified.json). This historical campaign was committed as `09630d6`; the shared type-system checkpoint above supersedes its next-step recommendation. Read the [assessment](EXPRESSIONS-ASSESSMENT.md),
[contract](EXPRESSIONS-CONTRACT.md), [checkpoint](EXPRESSIONS-STATUS.md) and
[verification guide](EXPRESSIONS-VERIFY.md).

Actual Linux x64 Native AOT and managed execution pass 85 complete pinned-Go records,
six shared groups, 26 order/repeat executions per runtime and both ordinary disk
projects (valid exit 0, invalid exit 1 with five complete diagnostics). All 91 new
xUnit cases and retained gates pass; selected-suite TRX aggregate 24,765 passed case
executions, zero failures/skips. This aggregate is not a unique-test inventory.
The successful frozen gate took 1,398.790 seconds; five drift checks clear and
242 historical fixture/proof files unchanged. Independent Sol review has no unresolved
substantive in-contract finding. The assessment records the first failed gate and
source/public-query versus observer-bookkeeping repairs without changed expectations.

Historical recommendation: remaining type-system closure under the [checker roadmap](CHECKER-COMPLETION-ROADMAP.md).
That campaign is now accepted above. Async/protocol and JS-specific consumers
remain unfinished; whole-checker completion is not claimed. Private-state observations
remain diagnostic aids, not automatic acceptance obligations. No new instrumentation
was added. Large-project comparison and .NET 11/optimization remain deferred.

## Historical accepted campaigns

The scopes and superseded recommendations below are historical; the current checkpoint
above controls resumption.

**Accepted: genuine checker initialization and declaration/object-model completion,
D1–D6.** Baseline `e7b81a2`; delivery commit contains this state and
[durable proof](../validation/results/declarations-verified.json). The expression/statement/flow contract above supersedes this historical checkpoint. Follow the [assessment](DECLARATIONS-ASSESSMENT.md),
[contract](DECLARATIONS-CONTRACT.md), [checkpoint](DECLARATIONS-STATUS.md) and
[reproduction guide](DECLARATIONS-VERIFY.md).

Actual Linux x64 Native AOT and managed execution pass 71 complete Go comparisons,
six shared groups, 26 order/repeat executions per runtime and both ordinary disk
checking cases (valid exit 0, invalid exit 1 with all six diagnostics). New 77 xUnit
cases and the full retained cascade pass. The selected-suite TRX aggregate is 24,674
passed case executions with zero failures/skips, not a distinct upstream-test inventory;
cheap affected-suite repeats are recorded separately. The frozen gate took 1,163.022
seconds, all five drift checks clear and 218 historical fixture/proof files unchanged.
Independent GPT-6.1 Sol source/assertion/evidence reviews report no unresolved
substantive in-contract finding.

Ordinary `--config` now checks genuine Program options and unchanged source libraries;
the exact-library admission gate and hidden library injection are removed. Historical
profiles remain named test adapters. Whole checker files are still partial. Runtime
body/flow, operational string mappings, async/generator and JS/JSX/JSDoc/decorator work
remain separate; see the [refreshed roadmap](CHECKER-COMPLETION-ROADMAP.md).
Recommended next: complete TypeScript expressions/statements and control flow over
these accepted declaration models. That campaign is now accepted above; reuse accepted
evidence rather than repeating audits without a concrete regression. Finish checker/infrastructure
before large-project comparisons; .NET 11/optimization remains deferred.

**Accepted: Program construction and environment assembly, P1–P6.** Baseline
`1d6bd61`; pin `fed0bf24149fb1ed36039212648bafdafc1ea10e` unchanged.
See the [guide](PROGRAM.md), [assessment](PROGRAM-ASSESSMENT.md),
[concise checkpoint](PROGRAM-STATUS.md), [source mapping](../source-map.json)
and [durable proof](../validation/results/program-verified.json).

Actual Linux x64 Native AOT passes 186 paired comparisons, 78 shared groups,
76 repeated-order executions per runtime and both ordinary disk inspections.
All 24,598 xUnit cases pass with zero failures/skips; all prior native gates pass.
The single frozen gate took 982.578 seconds; five drift checks clear, 187 historical
fixture/proof files unchanged. A later one-line recorder bookkeeping correction is
explicitly bound in the proof; no compiler/tests/runtime evidence changed.

Use `tscheck --inspect-program <tsconfig>` for real source/library/type/reference
construction and its diagnostics. It does not imply successful semantic checking.
All 115 genuine pinned libraries are embedded; current checker profile restrictions
remain explicit. Local Program reuse and in-process mapper/SpanMap are included;
emit/build/watch/services, mapper processes and broader checker computations remain
separate. Native platforms other than Linux x64 are unverified.

Program delivery was committed as `e7b81a2`. The accepted declaration campaign above
supersedes this historical stopping point.
Next recommendation: genuine checker initialization and declaration/object-model
completion, following the [checker roadmap](CHECKER-COMPLETION-ROADMAP.md).
Finish checker/required infrastructure before large-project Go/JavaScript-tsc no-emit
correctness or performance comparisons. The .NET 11/optimization investigation stays
deferred. Reuse accepted evidence; do not reconstruct or rerun completed campaigns.

**Prospective efficiency guidance (current and future campaigns):** prioritize
implementation and efficient verification. Private cache/identity/closure/storage
observations are diagnostic aids, added only for concrete correctness risks, required
upstream invariants or meaningful resource concerns. Distinguish observer limitations
and incidental representation from consequential defects. Retain existing machinery,
useful regressions, applicable upstream tests, meaningful Go comparisons, independent
review and actual Native AOT acceptance. No restart, accepted-work audit, broad cleanup
or new process framework; simplify only an active bottleneck. Make substantive changes
to observations explicit, never change expectations to conceal defects, and disable
observation-only overhead before future performance comparisons. Let an underway final
acceptance run finish.

**Accepted: source-import module-resolution completion, R1–R6.**
Baseline `37bd57d`; upstream remains `fed0bf24149fb1ed36039212648bafdafc1ea10e`.
All five Module files /149 functions are translated and independently reviewed.
Actual Linux x64 Native AOT passes 216 comparisons, 40 shared checks, 30 order
executions per runtime and 11 CLI cases. All 24,334 xUnit cases pass with zero
failures/skips; retained configuration/project/semantic/foundation gates pass.
The successful final gate took 912.603 seconds with all five drift checks false,
13 native executable paths and 171 historical fixture/proof files preserved.
Read the [guide](RESOLUTION.md), [assessment](RESOLUTION-ASSESSMENT.md),
[resumption checkpoint](RESOLUTION-STATUS.md) and
[durable proof](../validation/results/resolution-verified.json).

Resolution and graph inspection exceed current checker capabilities. Full Program/
library-environment construction, reference execution, broader declarations/checker
support and emit remain separate. No next campaign is authorized. If delivery was
interrupted, finish the authorized commit without altering proven source/assets;
otherwise await a new mandate. Recommended next assessment: Program construction
and environment assembly. The .NET 11/optimization investigation remains deferred.

**Accepted: configuration interpretation and file discovery, S1–S6.**
Baseline `2c850db`; upstream `fed0bf24149fb1ed36039212648bafdafc1ea10e` unchanged.
Read the [mandate](CONFIGURATION-PROMPT.md), [contract](CONFIGURATION-CONTRACT.md),
[assessment](CONFIGURATION-ASSESSMENT.md), [guide](CONFIGURATION.md),
[concise checkpoint](CONFIGURATION-STATUS.md) and
[durable proof](../validation/results/configuration-verified.json).
The delivery commit containing this entry records the accepted campaign.

All 17 TsOptions files /210 functions are implemented and independently reviewed,
with required dependency scopes mapped separately. The successful final gate took
860.209 seconds: 24,078 xUnit cases pass with zero failures/skips; actual Linux x64
Native AOT passes 319 configuration comparisons, 217 shared groups, 38 order executions
per runtime and 14 ordinary CLI cases plus lexical controls. All retained regressions
pass. Source/assets/upstream/obligations and 126 historical fixture/proof files are unchanged.

Use `tscheck --inspect-config <tsconfig>` for genuine interpretation/discovery and
`tscheck --config <tsconfig>` for supported checking with explicit admission. The
legacy strict `--project` adapter remains named compatibility behavior. Configuration
completion does not imply full Program validation/execution or checker support.
Source package imports, reference/watch/mapper execution, emit and services remain external.

No next campaign is authorized. If delivery was interrupted, inspect git status and
finish the authorized commit, preserving the proven source and assets. Otherwise await
the next mandate. Recommended next subsystem: remaining source-import module resolution,
followed by full Program construction/validation. Do not restart completed investigations.
Three GPT-6.1 Sol workers were reused; root owned coordination and acceptance.
See the assessment for measured build/gate effort, limitations and process corrections.

## Deferred side investigation

The user requested a Sol investigation of .NET 11 Unicode/text APIs, then explicitly
deferred it until after initial implementation. The worker was stopped. Preserve the
[preliminary notes](DOTNET11-INVESTIGATION.md) without treating them as an adoption
review or active task. No runtime/SDK/source changes or benchmarks were performed;
the accepted checkpoint remains on .NET 10. Do not resume optimization work without
new authorization.

The following project and earlier records are historical. Their superseded next-step
recommendations and configuration exclusions do not override the accepted contract above.

**Accepted: bounded multi-file ES-module project checking, P1–P5.**
Baseline `2598ceb`; upstream `fed0bf24149fb1ed36039212648bafdafc1ea10e` unchanged.
The delivery commit containing this entry records the completed campaign.
Read the [mandate](./PROJECTS-PROMPT.md), fixed [contract](./PROJECTS-CONTRACT.md),
[assessment](./PROJECTS-ASSESSMENT.md), [practical guide](./PROJECTS.md), and
[durable proof](../validation/results/projects-verified.json).
No next campaign is authorized. Do not restart completed investigations or benchmarks.

## Current acceptance and resumption

The repaired-source full gate passed in **780.934 seconds**, with independent Sol
acceptance review and no source, asset, historical-asset or upstream drift:

- **23,542 xUnit cases**, zero failures/skips: scanner 3,838, parser 18,709,
  binder 163, checker 832.
- Actual **Linux x64 Native AOT**: 48 exact project comparisons, 27 shared groups,
  30 order executions per runtime, and 10 ordinary CLI cases with 14 lexical controls.
- Eleven new full original Go test functions replayed successfully; all retained
  foundation, binding and semantic native gates passed. Nine native binaries and
  115 protected historical fixture/proof files were verified.
- Source SHA: `355b4842fa619586687a2de72293e259e9d993b35609f4cd2ae367b9cf83f953`.
  Asset SHA: `117226e06b6ef7181042b90512420614c07198d480c9dea705e7109d149cbcd7`.

The user command is `tscheck --project <tscheck.json> [--json]`. The fixed environment
uses Bundler/ESNext, a strict authored JSON subset with explicit files, relative
ES modules, and genuine ES5/decorators/decorators.legacy declarations with skipLibCheck.
It retains accepted collection-language restrictions. Packages, general tsconfig,
classes/new, destructuring, broader libraries, emit and services are not accepted.
See the contract for exact boundaries and observation omissions.

The first gate was stopped (exit 143, not accepted) after a lone-surrogate module
specifier was replacement-decoded to a different existing filename. Strict decoding
at three disk lookup boundaries now rejects such non-scalar paths with host exit 2;
original Go reports 2307 for these witnesses, so this is explicit unsupported behavior,
not diagnostic parity. Ordinary escaped Unicode paths remain supported. The fresh
complete gate above verifies the repair; canonical 48 expectations were unchanged.

All three explicit GPT-6.1 Sol owners were reused: [core](./progress/projects-core.md),
[support](./progress/projects-support.md), [verification](./progress/projects-verification.md).
Independent reviews and test provenance remain linked in the assessment. Root owns
contract, source mapping, acceptance and delivery. Eleven central Debug library builds
reported 59.76 seconds in aggregate (three failed); this is not total effort or a
performance benchmark. No installation or user unblock was required; token/cost
telemetry is unavailable.

The [source map](../source-map.json) records selected ports as partial and authored
host/harness code separately. Source or verification-asset changes require affected
verification and fresh native evidence; never overwrite earlier campaign proofs.
Current executable/assets were frozen before the successful gate; final documentation
and proof recording do not alter those fingerprints. Check git status on re-entry;
if delivery was interrupted before commit, finish the authorized commit, preserving
all reviewed work. Otherwise await a new bounded mandate.

Recommended next project-workflow campaign: genuine upstream tsconfig files/include/
exclude discovery and configuration diagnostics. Classes/new and destructuring remain
separate important language barriers. No package/Node or emit expansion is implied.

The following collection records are historical evidence; their project exclusions
and next-step recommendation are superseded only by the accepted project contract above.

## Historical collection baseline (2598ceb)

Read the [exact mandate](./COLLECTIONS-PROMPT.md), fixed
[contract](./COLLECTIONS-CONTRACT.md), [assessment](./COLLECTIONS-ASSESSMENT.md),
[guide](./COLLECTIONS.md) and [durable proof](../validation/results/collections-verified.json).
The explicit Collections profile checks one valid TS script with complete genuine
ES5/decorators/decorators.legacy declarations, fixed options and authentic skipLibCheck
policy. It supports the agreed generic inference, arrays/readonly/concrete tuples,
selected collection APIs, rest/spread, keyed/mapped/conditional utilities and loops.
Libraries are parsed/bound and demanded declarations remain real; whole-library body
checking, full checker parity and project checking are not claimed.

Final gate: `python3 tools/run-collections-gate.py`, exit 0, **658.892 seconds**.
Evidence recorded with `python3 tools/record-collections-evidence.py` after final
independent review. No source, verification-asset or historical-asset drift.

- All **23,467 xUnit cases** pass, zero failures/skips: scanner 3,838, parser 18,709,
  binder 163, checker 757 (701 retained +12 shared +44 collection oracle cases).
- Actual **Linux x64 Native AOT**: all 44 rich collection comparisons, 12 shared checks,
  and 18 order executions per runtime pass. Valid demo has zero applicable/missing-global
  diagnostics; invalid demo retains all eight original diagnostics.
- All 44 original ordinary outputs are byte-exact with/without observation metadata.
  All 37 named members, 50 method overload declarations and three Array call declarations
  have actual witnesses. All earlier scanner/parser/binder/checker native gates pass.
- Seven native binaries, two Go oracles and 102 protected historical assets were checked.
  Source SHA: `4b72bcbcae5b18e9322d2d45ecee624bc0d4b2f4d4b0ca109209fcf78f3fc9c3`.
  Asset SHA: `5bfa508a7115d8b175af41cea303ba10c792656d7c025b53353b865a6655dda6`.

The final corpus is [manifest-driven](../validation/collections-corpus/manifest.json).
Early snapshots are historical investigation, not extra acceptance cases. Test origin
and exclusions are in [collections-tests.json](./collections-tests.json); API witnesses
and observation limits are in [collections-coverage.json](./collections-coverage.json)
and the [cache ledger](./progress/collections-retained-caches.md).

## Historical collection limitations and working lessons

Three explicit GPT-6.1 Sol owners were reused: [core](./progress/collections-core.md),
[support](./progress/collections-support.md), [verification](./progress/collections-verification.md).
Independent [source](./progress/review-collections-core.md) and
[verification](./progress/review-collections-verification.md) reviews are closed for this
contract. All source/tool/corpus owners stopped edits before the successful gate;
subsequent changes were documentation and the recorded proof only.

Direct Go-to-C# mapping and partial/authored distinctions remain in
[source-map.json](../source-map.json). Code changes require affected verification and
fresh native evidence as appropriate; do not reuse the proof for changed code/assets.
Do not repeat completed broad audits without a new discrepancy or dependency.

Source destructuring, generic variadic tuples, classes/new, custom iterators/ES2015
libraries, modules/projects/emit and other explicit contract exclusions remain.
Whole private intern/formatter cache topology, untouched allocator backing and synthetic
types absent from the selected observations are not inferred complete. Required internal
library representations are distinct from excluded user syntax.

Thirty numbered central build logs total 142.31 seconds of MSBuild-reported time;
this excludes other builds, model/oracle/test effort and total campaign wall time.
One comprehensive gate ran successfully. Token/cost telemetry is unavailable. No user
installation or scope decision was needed beyond the mandate. A late cache-enum
misclassification caused unnecessary metadata/build work; those hooks were removed.
Detailed history and limitations remain in the assessment rather than being hidden.

Recommended next campaign: bounded multi-file ES-module hosting and relative imports,
exports/re-exports, cycles and cross-file generic calls/diagnostics. It needs a new
mandate; package/Node resolution, broader libraries and common excluded syntax are
separate limits. Prior callable state is in
[CALLABLES-COMPLETION-STATE.md](./CALLABLES-COMPLETION-STATE.md).
