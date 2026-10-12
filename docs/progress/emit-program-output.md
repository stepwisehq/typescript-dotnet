# Program output and filesystem dependencies

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
[source review](../../artifacts/emit/source-review/workgroup-nested-liveness.json) is clear.
Fresh build and all 2,223 managed records passed. Actual Native ManyFiles completed
with exit 0 in 0.221 seconds, versus the prior stopped 103.7-second stalled process.
This is liveness evidence only because publication overlapped. All six fresh publications passed; direct Native gates passed. Fresh Native corpus, command and full retained noEmit revalidation passed. See the
[reproduction](../../artifacts/emit/final-acceptance-workgroup-fix/manyfiles-reproduction/result.json). Results
below describe the prior candidate until the new gate finishes; they are not acceptance
of the scheduling repair. Source/tools/tests remain frozen during execution.

Active after accepted script-transform checkpoint `e8b11bcf`. This lane owns genuine
compiler EmitHost26/emitter23, Program emission and declaration diagnostics, demanded
HasFileName host joins, filesystem write owners/wrappers and existing corpus consumer
integration. The coordinator owns builds and acceptance; this lane performs no independent builds.
Final integrated correctness execution passed within the qualified contract scope. Declaration interfaces/transform/support owned other lanes.

Write dependency source-written first: public IFS.WriteFile has no default behavior.
OS owns bounded32-write/128-parent-operation semaphores, raw-byte stream writes,
create/truncate modes, first attempt then parent creation/retry, ignored deferred-close
error as upstream. Borrowed output transport follows accepted source-byte transport:
OS/Bundled/Cached forwards preserve ByteStringValue until real external string boundary.
Bundled and project-reference resolver owners retain genuine original write panics.
Authored read-only mocks explicitly return errors; forwarding mocks delegate actual
owners. Existing baseline filesystem gains synchronized real write state and observed
read paths, parent creation/retry and symlink/case owners, retaining raw content.
Metadata/clock behavior remains the authored fixture's existing bounded contract;
no whole MapFS/mutating-VFS implementation claim. Append/remove/clocks unneeded by
compiler output and outside this demanded dependency; no silent fallback writes.

Writer pool clear/reuse and per-file workgroup/input-ordered result reduction preserve
pinned Program ownership and concurrency. Nil versus empty targets, ForceEmit/builder
signature bypass and precise noEmit result modes are covered by actual public controls.
Command integration follows pinned diagnostic/emit/list-only and exit-code handling,
preserving existing --noEmit/performance and explicit no-GC policy.
No general command-parser or build/watch expansion.

Full original Go reference capture is coordinated by maps using one prebuilt original
runner in 100-configuration processes; this lane does not duplicate it. The managed corpus
consumer reuses CompilerNoEmitHarness/Runner preparation narrowly and retain all
original diagnostics, ordered emitted results, JS/DTS/maps, declaration recompiles,
noCheck and source-map observations. No large replacement authored showcase family.

VFS write dependency independently reviewed clear in
[the source review](../../artifacts/emit/source-review/vfs-write.json); compiler behavior now passed the final integrated correctness gates. The production
packet is independently reviewed: genuine EmitHost26, all emitter23 counterparts
(including eligibility retained in Program), Program parallel emission/noemit/public result
contracts and declaration diagnostic cache. The cache uses the existing genuine SyncMap
owner lazily; the per-invocation ConcurrentBag writer pool clears and reuses TextWriter
owners. Explicit per-job capture owners retain the original closure state, while ordered
result projection keeps the original map allocation before reduction.

Four generic HasFileName host methods retain borrowed SourceFile fast paths and actual
metadata/project options for other original file owners. Project's explicit wrappers are
owned by the command lane. OutputFilePaths implements the declaration-output interface;
the two genuine declaration transformers implement the shared original consumer interface.
Four demanded stringutil functions preserve URI byte escaping/BOM recognition. Writer
TextValue and generator Value carry actual immutable output headers to filesystem writes;
external WriteFile callbacks still receive the original raw string. RawSourceMap's existing
exported fields become public for the real Program result, with no serialization algorithm
change. Formatter and whitespace checks passed before the source freeze. No local build.

Original reference capture completed all 15,330 configurations: 13,571 full-harness passes,
1,720 original option policies, 39 preconfiguration skips, eight output-channel-only skips,
zero original assertion failures. Seven expansion failures remain separately recorded.
[Capture](../../artifacts/emit/whole-reference/result.json) reuses one prebuilt runner across
154 bounded processes; actual outputs/source maps/full original channel assertions are
retained compressed. The final Native full-corpus comparison now matches every in-scope observation and
all 13,571 original assertion sets.

Command/Project implementation ownership transferred to the maps lane; this lane retains
compiler Program/emitter and the original corpus consumer. No command source edits overlap.

The complete original nine-channel consumer is now source-reviewed and integrated. It
reuses original preparation, real declaration recompilation/noCheck repeats, formatted JS
and error baselines, source-map records, and original AST parent/union checks. The eleven
reference record-name collisions were recovered in one fresh original process, preserving
the main reference; the supplement verifies source/configuration identity.

Development repair83 matched 79 original assertion sets and 77 raw results, leaving
only six incremental-owned build-info metadata differences. The complete managed run
subsequently exposed six content-mapper formatted baselines; actual upstream repairs
passed their six-case retest. The temporary diagnostic probe was removed after the
exact checker cache guard correction; its observations remain evidence.

Final execution passed 2,223 managed records, including 493 Emit records and all 122
selected original rows. Six actual Native publications bind freshly rebuilt dependency
paths/hashes. Native Emit passed 152 original functions and 251 authored controls.
The full Native corpus observed all 15,330 configurations, with 15,285 exact public
matches, 13,571 original assertion sets, 39 prep skips and 1,720 original policies.
Only six reviewed incremental metadata comparisons remain blocked; their nine original
channels still match. Comparator exit 1 is preserved, not converted to an unconditional
pass. Native CLI/guards/runtime passed 6/3/3, retained Foundation/utilities/Program
passed 3,153 + 87 / 20 / 9, and all 15,304 retained Native noEmit public records match.

Runtime observer attempt 1 failed on Go-side Node quoting. A reviewed one-file JSON
transport correction preserved all source/expression/expected values and binaries.
Earlier phase freeze hashes remain unchanged; command attempt 2 and noEmit use the
explicit later freeze. The separate benchmark cohort and final proof/commit closeout
remain pending. See [assessment](../EMIT-ASSESSMENT.md) and
[verification](../EMIT-VERIFY.md) for current evidence links.

The reusable [bounded launcher](../../tools/run-emit-corpus.py) preserves frozen binary,
request and reference hashes before execution, rejects existing result directories, and
closes each gzip batch before removing raw output. It invokes the existing comparator once;
it does not resume or reinterpret original failures. Four separate genuine Program mode
controls retain exact Go callbacks/results, including force-DTS skipped-write mutation.
Linux write failures retain the exact first failing ancestor/op/errno text rather than
.NET exception wording; raw output bytes and both mkdir/open failures have Go witnesses.

The original high-concurrency workload caught this scheduler boundary despite the broad
small-fixture corpus. Run it early when introducing future parallel consumers, before
expensive broad gates; preserve original workload and ownership rules. No external
performance claim follows from this concurrent-publication reproduction.

The post-fix cohort completed all 50 observations across five unchanged original
workload variants, five repeats per runtime. Best Native/Go ratios are 1.629×, 1.406×,
1.403×, 2.250× and 2.112×. The [assessment](../EMIT-ASSESSMENT.md) records exact
best/median/max timings and both allocation ranges; counter semantics differ. No
before/after, universal parity or external project claim follows. Fresh Native corpus and command gates passed under the new candidate path;
full retained noEmit revalidation also passed.

Fresh corpus revalidation matches all 13,571 original assertion sets and 15,285
public output records, with only the six reviewed incremental metadata deltas. Fresh
command attempt 1 passed all 6/3/3 observations. Full retained noEmit matched all 15,304 records; independent final reviews are clear and the contract-qualified scope is accepted. Measured fresh phase effort is
63.297 seconds build, 148.318 seconds managed and 96.676 seconds publication, not
workload speed evidence or an inferred implementation-cost figure.
