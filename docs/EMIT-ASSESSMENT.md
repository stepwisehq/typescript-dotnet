# Whole emit assessment — accepted contract scope

## Accepted whole-emit record

The first in-repository ManyFiles benchmark exposed CLR ThreadPool starvation: the
first emit worker's lazy checker initialization queued another work group, while many
other emit workers occupied pool threads waiting for that same lazy owner. Go parks
those waiting goroutines without occupying scheduler workers. The workload was not
weakened and its stalled observation remains recorded.

The reviewed general WorkGroup adaptation retains immediate queue scheduling and
adds a standard ready queue that callbacks and the waiting caller claim exactly once.
The caller helps its group's ready work before blocking, including nested groups.
Enqueue/wakeup/wait transitions share the gate; recursive pending accounting, ordered
completion and exception propagation remain intact. No eager checker initialization,
thread-limit increase, per-job Task/thread allocation or benchmark fixture change was
introduced. Compiler work state is explicit; action producer ExecutionContext identity
is not promised when another callback or waiter claims that action.

The existing WorkGroupScheduling assertion case now exercises 200 lazy waiters and
four nested initialization jobs. Registration counts remain unchanged. Independent
[source review](../artifacts/emit/source-review/workgroup-nested-liveness.json) is clear.
The fresh build, all 2,223 managed records and six Native publications passed
under [the new candidate gate](../artifacts/emit/final-acceptance-workgroup-fix/).
The actual Native [ManyFiles reproduction](../artifacts/emit/final-acceptance-workgroup-fix/manyfiles-reproduction/result.json)
completed with exit 0 in 0.221 seconds, versus the prior stalled process stopped after
103.7 seconds. This establishes liveness; overlapping publication makes the duration
unsuitable as performance evidence. The subsequent isolated 50-run workload cohort
completed successfully. Fresh Native corpus, command and all 15,304 retained noEmit
records passed. All required execution and independent final reviews are complete; the coordinator
accepted the contract-qualified scope. The final [source-bound proof](../validation/results/emit-verified.json)
records the accepted execution. Source/tools/tests remained frozen through execution.

This document separates completed implementation and development evidence from final
acceptance. The pinned reference is `fed0bf24149fb1ed36039212648bafdafc1ea10e`.
The [contract](EMIT-CONTRACT.md), [implementation inventory](EMIT-IMPLEMENTATION.md)
and [test inventory](EMIT-TEST-INVENTORY.md) define the applicable scope. External
project evaluation and performance measurements are not part of this correctness gate.

## Implementation

The coherent dependency families are implemented: synthesis/metadata, complete printer
and source-map machinery, genuine reference/emit resolver and non-hover NodeBuilder,
shared transform foundations, TS/ES/JSX/module/constant-enum/declaration transforms,
compiler EmitHost/Emitter/Program orchestration, demanded writable filesystem owners,
and configured command emission. Real Program/checker/library owners drive output;
there are no fixture-only substitute resolvers or declaration transforms.

Ownership adaptations retain raw UTF8 byte headers, Go slices and required clones,
explicit capacities, generated-name/emit metadata ownership, pooled writers, concurrent
per-source work and final input-ordered reduction. Linux output errors retain original
operation/path/errno wording, including first failing parent, rather than hiding the
exception behind a successful write callback. Independent source reviews are recorded
under [source-review](../artifacts/emit/source-review/).

Original upstream TODO behavior remains explicitly pinned. Hover-only serialization,
language services, unrelated build/watch and incremental snapshot/builder ownership
are outside the contract. Ordinary Program emission does not manufacture build-info.
The command rejects unsupported incremental operational execution; check-only APIs
retain their established behavior.

## Original reference and development results

The complete original reference has 15,330 configurations: 13,571 full-harness passes,
1,720 original option-policy dispositions and 39 preconfiguration skips. Eight
output-channel-only skips and seven fixture-expansion failures remain separate.
Eleven source-map-record filename collisions were recovered in one fresh process,
with exact source/configuration identities; the main original reference is unchanged.

The managed consumer preserves all nine original channels, public diagnostics/results,
ordered JS/DTS/maps, real declaration recompilation and noCheck repeat invariants,
formatted baselines and AST parent/union assertions. The full managed development run
covered all15,330 records. Initial raw defects exposed nil symbol exports, missing
method comment ranges and an omitted context-sensitive-parameter cache guard; all
were repaired against actual upstream bodies without changing expected outputs.
Six formatted content-mapper discrepancies were then repaired. The final six-case
repair passed all original channels/public observations in 1.540s after consumer-build7
passed with zero warnings/errors in 10.961s. These durations describe execution effort,
not performance results.

Six remaining raw discrepancies are exclusively incremental-owned build-info emitted
file metadata. They remain visible blocked public Program comparisons. Their original nine-channel
assertion sets match; that does not make their complete public output comparison pass.
All other fields and channels are compared. All seven wrapper-required configurations
are counted separately, including the one with matching ordinary public output.

## Accepted execution evidence

The prepared [gate recipe](../artifacts/emit/final-acceptance-workgroup-fix/run.py) separates fresh
managed builds/tests, fresh RID dependency compilation and actual ILC input binding,
Native direct controls, full emit corpus, disk/runtime controls and retained noEmit.
The [selected xUnit corpus](../artifacts/emit/final-managed/selection.json) contains 122
qualified original full-harness rows, including repaired cases and representative
categories; it supplements the full standalone corpus rather than replacing it.

| Required gate | Accepted final record |
| --- | --- |
| All Emit xUnit and 122 selected original cases | Fresh repair candidate PASS: 493 records, including 122 original configurations |
| Retained managed checks | Fresh repair candidate PASS: 1,730 records |
| Actual Native emit direct assertions | Fresh PASS: 152 original functions and 251 authored controls |
| Actual Native original emit corpus | Fresh qualified PASS: 15,330 observed; 15,285 public matches; 13,571 assertion sets; only six external metadata differences |
| Native configured disk command / guard / runtime controls | Fresh first-attempt PASS: 6 / 3 / 3 |
| Native retained Foundation / utilities / Program controls | Fresh PASS: 3,153 + 87 / 20 / 9 |
| Native retained original noEmit | Fresh PASS: all 15,304 complete public records exact |
| Actual ILC roots/dependencies and binary/source/reference hashes | Six fresh repair-candidate publications verified; current freeze retained |
| Independent final execution review | CLEAR: execution evidence and completion claim |

The [final proof](../validation/results/emit-verified.json) binds the independently reviewed execution. The [fresh Native corpus comparison](../artifacts/emit/final-acceptance-workgroup-fix/native-emit-corpus/comparison.json)
observed every configuration with no missing rows or in-scope failures. Comparator
exit 1 retains exactly six incremental-owned build-info metadata differences; their
nine original assertion sets match. The gate accepts only that reviewed contract
boundary, not an unconditional zero-difference result. Fresh command execution passed
all six disk, three guard and three runtime cases on its first attempt. All completed
repair-candidate phases use freeze hash
`a111b465d715c41d88daa85e49ccd3a12c8ea9a41612112d4e2255fa2689f752`.

Observed fresh execution effort: build 63.297 seconds, managed tests 148.318 seconds
and the six-publication phase 96.676 seconds. These are build/correctness phase elapsed
times, not measured Emit durations or a performance comparison. Full emit corpus took
445.1744 seconds (bounded runner 444.1730 seconds), command 4.7516 seconds and full
retained noEmit 365.0557 seconds. Failed earlier build
joins, the stopped prior ManyFiles workload and the observer-only quoting correction
remain preserved; none are rewritten as successful attempts.

## Prior candidate observer freeze transition

Runtime attempt 1 failed in the Go-side Node observer because embedded quotation marks
were not transported correctly. The correction changed only the authored runtime tool's
JSON string transport; inputs, expressions, expectations and all compiler binaries stayed
unchanged. Earlier build, managed, publication, direct and corpus phase records retain
freeze hash `ca3a8a541f2c9795412ef3ee0464062cdda8e5e007ce1393bab9502116cf47a9`.
The [one-file transition](../artifacts/emit/final-acceptance/command/attempt-1/harness-only-transition.json)
records the tool hash change. Command attempt 2 uses the later freeze hash
`d00e7738cd6294826ea8a2a821877cd3eee98c31bf41dc8d41a1a4477196fea8`.
No earlier completion record was rewritten and no corpus or build was repeated for
this observer correction.

## Integration lesson

The high-concurrency original 200-file workload exposed a CLR scheduling adaptation
gap despite broad correctness coverage over smaller fixtures. Future integrations that
introduce parallel consumers should run this unchanged in-repository workload early
as a liveness check, before broad corpus gates. The observation does not authorize
thread-limit tuning, workload reduction, external project timing or a performance
conclusion. Both the stopped original candidate and successful repair reproduction
remain evidence.

## In-repository workload observations

The [complete cohort](../artifacts/emit/final-benchmarks-workgroup-fix/result.json)
retains all 50 successful fresh-process runs: five original workload variants, five
equal repeats for each runtime. These are measured Emit durations; source/Program
preparation is outside the measured loop and write output is discarded, matching the
original benchmark. Best, median and maximum are in milliseconds. The ratios compare
best Native with best pinned Go in this cohort.

| Variant | Go best / median / max ms | Native best / median / max ms | Best Native / Go |
| --- | ---: | ---: | ---: |
| LongLines1000 | 17.710 / 18.709 / 20.303 | 28.844 / 32.202 / 39.316 | 1.629× |
| LongLines5000 | 238.972 / 250.187 / 255.773 | 335.954 / 358.481 / 376.589 | 1.406× |
| LongLines10000 | 930.676 / 947.579 / 975.813 | 1306.074 / 1324.546 / 1374.380 | 1.403× |
| ManyFiles | 53.714 / 54.937 / 57.995 | 120.855 / 128.263 / 130.689 | 2.250× |
| LongLinesWithLineBreaks | 18.360 / 18.636 / 20.604 | 38.774 / 54.667 / 59.596 | 2.112× |

| Variant | Go allocation-byte range | Native allocation-byte range |
| --- | ---: | ---: |
| LongLines1000 | 8,167,392–8,177,568 | 10,568,016–10,590,896 |
| LongLines5000 | 10,537,552–10,558,576 | 17,320,072–17,341,688 |
| LongLines10000 | 13,354,496–13,373,416 | 25,786,184–25,879,208 |
| ManyFiles | 73,258,136–73,320,600 | 183,465,168–183,472,816 |
| LongLinesWithLineBreaks | 13,660,936–13,673,304 | 26,267,416–26,283,616 |

Go reports original testing-benchmark allocation counters; Native reports whole-runtime
`GC.GetTotalAllocatedBytes` differences over the measured loop. Runtime allocation
categories, stacks and accounting differ; these are retained observations rather than
identical counter semantics or an allocation-parity assertion. This is neither a
before/after speedup nor a new VS Code comparison, and it does not establish universal
performance parity or a new emit threshold. The prior ManyFiles 103.7-second stall is
failure/liveness evidence, not a baseline for a speedup ratio.

Final independent [completion-claim review](../artifacts/emit/source-review/final-claim-review.json)
and [execution-evidence review](../artifacts/emit/final-acceptance-workgroup-fix/final-acceptance-independent-review.json)
are clear. Acceptance covers the stated emit contract and preserves all external
owner dispositions; it does not claim incremental builder or external project parity.
