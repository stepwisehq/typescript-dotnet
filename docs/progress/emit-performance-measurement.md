# Full-emit measurement lane

Verified delivery: the coherent packet passed independent source review, retained
managed/Native AOT correctness gates and the scoped emit performance target. See
[final evidence](../../validation/results/vscode-emit-performance.json) and
[assessment](../VSCODE-CHECK-STATUS.md). Notes below preserve investigation chronology;
intermediate pending statements are historical, not outstanding tasks.

Current: root first tuned pair produced Go10.19s / Native30.72s with exactoutputs.
The no-GC region started but ended unsuccessfully: allocationdelta19,915,560,712bytes
and collectiondelta25/11/2 show that14GiB is insufficient for fullEmit. This is
qualificationfailure, not a five-run target observation. Root completed one
immutable-baseline CoreCLR allocationcapture with exact10031outputs/10diagnostics.
Both existing decoderpasses completed, no lostevents/truncation, all176500ticks
typed/stacked/resolved and jointweights agree at20,075,515,816sampledbytes.
[Attribution](../../artifacts/emit-performance/baseline/managed-profile/attribution-summary.json)
identifies commentiterator4.004GB as dominantconcreteowner; maps fullvalue-sequence
packet source independentlyclear. Predicateparams385MB/ancestorresults288MB are
profile-grounded; inventory owns exactpredicatecache and parentvisitorjoin, preserving
requiredresults/cloneownership. InclusivePrinter5.944GB/transform2.027GB overlap;
no additionaldominantmissingupstreamreuse found. Root owns nextintegratedbuild/cohort.
Rawtrace/analysis/joint/ETLX retained; reproducible673MiB unsolicitedfoldedexport
removed with originalhash/size manifest, no SVG.

The new user target is Linux x64 Native AOT VS Code full emit at no more than
1.20 times pinned Go best-of-five wall time, with all output bytes and complete
comparable diagnostic messages preserved. Root runs all builds and compilers.
The previous single ordinary pair (Native25.38s/Go12.82s) remains a separate
control, not the tuned baseline or target claim.

[benchmark-vscode-emit.py](../../tools/benchmark-vscode-emit.py) is source-ready,
syntax-checked, and frozen for root's first pair. It reuses the accepted ordinary
CLI and current exact VS Code reference; the same-directory wrapper overrides
only output directory and matched eight checkers. Existing `--no-gc-region`
spans config/Program/check/emit and result serialization. Its existing public
policy sidecar records allocation delta, collection counts, start/end/mode and
14GiB total/2GiB LOH/20GiB available-memory qualification. Full emit may exceed
this region; fallback/failure is retained and cannot qualify a tuned target.

The controller supports one qualification pair or five reversing alternating
fresh pairs, GNU time wall/CPU/RSS, exact path/raw-byte/reference comparison and
all ten complete comparable pretty-false diagnostics per run. Hashing is outside
compiler timing; input discovery/hash warms files and this is not cold-cache
measurement. A single output directory is reused, with per-run stream/result
records retained. Workload text inventory is verified before and after the cohort;
small compiler/config/reference bindings are guarded every run. Timeout kills the
whole process group and retains evidence. Ordinary mode is separate and omits
both checker override and no-GC request. No debugger/heap capture or new compiler
instrumentation is introduced.

Root published the immutable edc53e02 snapshot with `PublishAot=true` and
`IlcInstructionSet=avx2` under `artifacts/emit-performance/baseline/native-avx2`;
actual ILC response contains `--instruction-set:avx2`. Next root action is:

```sh
python3 tools/benchmark-vscode-emit.py artifacts/emit-performance/baseline/first --runs 1
```

After coherent implementation/review, root can run `--runs 5 --native <candidate>`
in a fresh directory. No measurement has been executed by this lane. Other Sol
lanes own runtime/string/emitter callbacks and complete factory/transform paths;
this lane owns only the controller initially, then independent review.


Integration1 first qualification retained exact outputs/diagnostics but failed the
no-GC lifecycle: Native32.21s/Go8.59s, allocation14,755,027,624 bytes, GC9/3/2.
Allocation fell about26% from baseline; neither pair is a qualified target result.
The existing14GiB total/2GiB LOH reservation gives SOH only12GiB. Actual ILC package
10.0.12 uses [runtime v10.0.12 GC reservation logic](https://github.com/dotnet/runtime/blob/v10.0.12/src/coreclr/gc/gc.cpp): separate SOH/LOH reserves,1.05 scaling and per-heap division/balancing allowance.
Global allocation below14GiB cannot establish which reserve exhausted. Baseline
CoreCLR kind/heap sampling is being decoded from the existing verified ETLX,
without another capture or heap dump; it cannot prove candidate Native heap skew.
The existing artifact analyzer now accepts `--allocation-partitions PATH` together
with `--reuse-etlx`; sampled interval totals and sampled object sizes are explicitly
distinguished. Root owns the small analyzer build/decode and policy decision.

The unchanged complete correctness recipe is prepared in
`artifacts/emit-performance/acceptance/run.py` and its retained command packet,
with only artifact-output paths redirected. No phase has run. It rebuilds managed
and RID dependency graphs and verifies actual ILC inputs against those fresh hashes;
run after the performance cohort to avoid overwriting shared compiler assemblies.

The existing trace partition decode completed: Small18,693,066,040 bytes (93.11%),
Large1,382,449,776 bytes (6.89%), exact same20,075,515,816 sampled interval total.
Fifteen observed heap indices0–14; small maximum1,908,832,632/minimum1,001,445,240
shows1.91x baseline skew. These are baseline CoreCLR samples, not candidate Native
reservation counters or proof of failure cause. Sampled ObjectSize totals are only
sampled objects and must not be substituted for interval weights.
Root authorized opt-in full-config-emit16GiB total/2GiB LOH, existing check-only14/2,
unchanged20GiB preflight/fallback/restoration and ordinary GC defaults. This provides
SOH14GiB instead of12GiB while retaining LOH headroom. Constructor default remains
check-only; config lifecycle begins before parsing, so config-declared NoEmit reserves
conservatively16GiB unless explicit CLI --noEmit/listFilesOnly. Legacy --project stays14.
Controller `--no-gc-budget-gib16` records the expected reservation, does not configure
compiler, and still demands successful full lifecycle. Default14 preserves old records.
Existing low-preflight fact exercises both modes without real reservation, count unchanged.
Independent inventory review precedes root qualification; no workload run by this lane.


Integration2 uses qualified16/2GiB full emit region. Five alternating samples each
preserve output/diagnostics and full-region completion, but best Native12.96/Go8.41
=1.541; target not met. Root captured native user CPU via existingperf and pinned Go
pprof aftercohort; profile timings are separate observations. Native6202samples,
zero reported loss, sampled userCPU62.646s over14.67s sample span vs17.12s profiled
wall. Timestamp/TID partition is retained in
`artifacts/emit-performance/integration2/native-cpu/time-thread-analysis.json`.
Workers sampled within0.175–0.224s of initialGCsample; seconds2–5 show8TIDs/~6CPU/s,
late7–14s moreTIDs/~1.4–4.1CPU/s. No multi-second scheduler-start starvation inferred.
User-only profile cannot establish kernel/offCPU duration; 6201/6202full-export
samples have no displayedcaller frames. Existing per-TID offline recovery requested,
not anothercapture. Nonexclusive emit checker leases and full-file linked-reference
locks match pinnedGo; no missing scheduling optimization established.
Pinned pprof.Stop writesallocs without forcingGC; localGo runtime documents profile
lag up totwoGCcycles. The4155MBalloc_space sample is not comparable to Native exact
14.755GBcounter and must not become anallocationratio. Gostdout two profilepathlines
are separately qualified after exact ten diagnosticprefix; original captureassertion
failure preserved, no rerun or weakened compiler output expectation.


Offline per-TID recovery completed for main751370 and workers751395/751394/751388.
`native-cpu/per-tid-attribution.json` conserves2269/6202 samples, exactorder/PID/TID/
period/leafIP and microsecond-vs-nanosecond timestamps within1.1us. Recovered late
wait/spin samples15 (0.1515sampledCPU,5.42%selectedlateCPU) resolve GetConstantValue10,
GetReferencedValueDeclaration4, IsReferencedAliasDeclaration1. This locates checker
mutex contention but cannot quantify blockedwall or extrapolate fourTIDs towholeprocess.
Caller recovery confirms lateEmit activity; no compiler lock-scope change authorized.
Shared checker.mu also owns transferable public Done leases, so thread-affine Monitor
would violate Go sync.Mutex cross-goroutine release/nonreentrancy. SemaphoreSlim stays.

Root is building integration3 as controlled runtime-only minimum-worker128 experiment,
with the reviewed VFS borrowed-directory decode correction and otherwise frozenpacket.
Prepared commands/provenance are in `integration3/prepared-command.json`. Installed
SDK10.0.401 maps ThreadPoolMinThreads to RuntimeHostConfigurationOption; NativeAOT
10.0.12 Native.targets266 embeds this as --runtimeknob:System.Threading.ThreadPool.MinThreads=128.
Actual postpublish rsp/ScannerIL/ELF binding must confirm appliedconfiguration; no new
observer/environment override. SameAVX2/eightcheckers/full16/2GiBregion and20GiBpreflight.
One qualification pair precedes fiveeach only iffullyqualified. No compiler pipeline,
mutex, ordering or ownership changes; no build/workload execution by this lane.

Diagnostic wait capture is prepared under integration4/native-waits (not executed by
this lane). Root published a separate EventSource-enabled Native binary with AVX2
and default minimum workers; rejected min128 is absent. Runtime keyword mask
`0x40040014001` requests GC, contention, threading, WaitHandle and stack events at
verbose level, without allocation, heap or CPU-sampling providers. Existing analyzer
now optionally pairs WaitHandle301/302 and contention81/91 by process/thread,
retaining start/stop/caller intervals, thread sums and union durations, unmatched and
lost-event counts, and startup GC/threadpool timelines. Native stack availability
must be validated. WaitHandle stop precedes possible Monitor reacquisition; intervals
can overlap and do not capture every filesystem wait. Diagnostic build/tracing timing
is excluded from the best-five performance comparison. Ordinary kernel scheduling
trace access is unavailable; no permission changes or custom synchronization proposed.

Root completed integration4 diagnostic capture:16.416s (observer build/timing only),
exact outputs and all ten comparable messages. Recorded waits show a sharp phase
change: MonitorWait≈0 blocked threads through second6, then mean7.07 at second7,
20.86/20.15 at seconds8/9,11–12.68 at seconds10–12,17.98/19.07 at seconds13/14.
Eight object identities account for104,189/104,202 MonitorWait pairs and122.498/
122.984 summed thread-seconds (99.60%); they begin7.465–7.762s and end14.928–15.120s.
This strongly supports the checker8 handoff/serialization interpretation alongside
previous resolved GetConstantValue stacks. It does not conclusively name objects:
all Native EventPipe caller stacks were absent, so object IDs are retained and
qualification is explicit. No heap/private object inspection is needed or proposed.
The sole startup induced GC suspend spans12.133–32.069ms (~19.936ms), with workers
starting~183ms; neither explains seconds-long wall gap. Zero events lost, zero stop
mismatches and one final unmatched wait start. GC verbose also records allocation
ticks; analysis excludes them from its bounded GC/threadpool timeline. No heap dump.
All relevant interval/object/per-second evidence is in integration4/native-waits;
thread sums and overlapping contention intervals are not claimed as wall savings.

Root authorized a standard POSIX semaphore representation for checker mutex only.
Source-ready [CheckerMutex](../../src/TypeScript.Scanner/Runtime/CheckerMutex.cs)
uses Linux x64/glibc-export detection, stable32-byte sem_t storage, sem_init(0,1),
EINTR-retrying sem_wait, sem_post, no custom queue/spin/handoff, and SemaphoreSlim
fallback before initialization elsewhere. Checker/pool/resolver share the same owner
and existing Wait/Release scopes unchanged. A minimal binary release guard clears
before posting; cross-thread release and nonreentrancy are retained. Initialization
publishes storage only after success; all failed construction frees storage. Native
calls keep their owner alive through exceptional paths; no Dispose is exposed and
finalizer never throws or frees state if destroy fails. Blocking calls use ordinary
P/Invoke GC transitions. This changes CLR representation, not Go fairness/scheduling.
Three authored shared/native controls cover contention, same-thread recursive waiting
with another-thread release, over-release recovery and forced managed fallback; on
supported actual Linux/glibc they explicitly require native backend. Independent
inventory review and root central compilation/actual Native behavior pending; no
performance improvement is claimed. Source-map packet is `/tmp/emit-checker-mutex-source-map-entries.json`.
Independent inventory source/control review is now CLEAR, bound in
`artifacts/emit-performance/checker-mutex-independent-review.json`. Source is frozen
for integration5 root build/three managed controls/actual Native VSCode qualification.
Counts increase by three authored cases: full Emit496 managed, Native152 original
functions +254 authored controls; retained coverage is unchanged. No local workload.

Integration5 actual targeted xUnit three controls passed; actual Native Emit.Validation
also executed152 original functions+254 authored controls successfully (9.626s).
Native source/179 actual ILC DLL inputs/rsp/ELF binding is captured under
integration5/native-mutex-controls/acceptance-independent-review.json, with explicit
post-run hash-capture timing. Frozen367 source inputs have no drift. Completed POSIX
cohort Native15.68,16.67,12.95,11.61,12.37s; Go9.57,11.41,9.07,8.80,8.66s. Every run
matches all outputs/diagnostics and full NoGC lifecycle. Best ratio1.34064665 remains
above1.20 target. Preserve earlier negative CLR min128 and stopped/failed NoGC runs.
Root next tests POSIX plus standard min128, isolating its interaction with the removed
CLR global wait layer; no compiler scope/scheduler changes. A read-only alternative
assessment confirms standard SemaphoreSlim.WaitAsync().GetAwaiter().GetResult()
activates PortableThreadPool cooperative blocking on worker threads; it preserves
sync acquisition/nonaffine release but adds contended Task/MRES allocations and still
uses CLR wait parking. Its exact bounded implementation would replace CheckerMutex
backend only, preserving all consumer types/scopes/Done lifetimes and meaningful
mutex controls. No alternative source implementation is authorized or made yet.

Integration6 POSIX+explicit minworker128 completed all five fresh pairs: Native10.18,
13.77,10.39,14.76,10.38s; Go10.06,10.65,9.99,12.00,9.37s. Best10.18/9.37=
1.08644610 meets1.20 scoped target, with every output byte/path/full comparable
message and every NoGC region qualified. Median10.39/10.06; ranges remain visible.
This is an opt-in AVX2/checkers8/min128/NoGC16 policy; portable defaults unchanged.
No managed cooperative alternative is implemented. Root final correctness recipe
publishes its CLI (sixth target) with the measured explicit overrides; validator
publications retain standard settings. Earlier advice that there were only five
publication roots was corrected against the current root-owned runner.

Accepted no-emit protection controller has been copied/rebound under
`artifacts/emit-performance/noemit-protection/run-paired.py`. It preserves original
complete diagnostic DTO/Go stdout expectations, alternating five-fresh timing,
14/2GiB region/20GiB preflight and guards. Current genuine Compile's added
EmitSkipped=true/Listing=empty fields are separately explicit, not substituted
original diagnostic expectations. Current integration6 source/IL/binary guards and
same-directory owned temporary checker8 config replace historical release/commit
bindings. FIRST then five-pair invocation prepared only; inventory independent narrow
review pending. Full correctness gates and no-emit protection remain pending parent
execution; this cohort target observation is not final acceptance.

Final correctness preparation is now executing centrally: build, managed2226
(496Emit+1730retained), publications and directNative152original+254authored plus
retained controls have completion markers. WholeEmit corpus is running; command,
retained15304noemit and noemit performance protection remain pending. Draft result
`artifacts/emit-performance/result-draft.json` records only completed facts and all
measured baseline/FIRST/cohort samples. Its effort ledger records actual command
seconds, not human/epic elapsed time. Final correctness CLI is byte-identical to the
measured integration6 candidate (ELF05fc470f..., rspb5f318..., Scannerdbf5ba...);
no implementation-equivalence-only qualification is needed. Same explicit AVX2,
min128 and EventSourcefalse publication policy; portable project defaults unchanged.
No final acceptance claim until remaining phases/protection and independent review.

All seven final correctness phases now completed:2226 managed, actual Native direct
and retained controls, full15330 Emit requests with15285 public matches/13571 original
nine-channel assertion matches/39prep/1720policy/6 visible external incremental
buildinfo metadata deltas only, six disk+three guards+three Node controls, and all
15304 exact retainednoemit records. Draft/effort ledger refreshed from actual markers.

Noemit protection FIRST captured Go7.42/Native9.22 with full14/2 region, but old
historical check-only wrapper expectations failed on current genuine Compile fields.
Pinned program.go1999–2031 NoEmit+nil targets/no buildinfo returns empty EmitResult
(EmitSkippedfalse); execute/tsc/emit.go66–70 with diagnostics therefore returns status2.
Current managed behavior is exact; the historical check-only wrapper's status1 differs.
The controller now separately asserts CompileExitCode2/EmitSkippedfalse/Listingempty
while preserving original full Diagnostics/Error/Go stdout. Failed result retained;
first pair qualified offline, no compiler change or rerun. Narrow inventory review
pending before five fresh pairs. Only this protection, final review/docs/commit remain.
