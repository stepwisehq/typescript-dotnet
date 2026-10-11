# Active: cumulative allocation comparison after accepted integration19

The user explicitly directed continuing after827f131. Go performance parity is
unmet; do not stop at another checkpoint/report/commit. Follow START-HERE.md's
comprehensive ownership-path SOP; Astra coordination/GPT-6.1 Sol implementation and
independent review remain. Do not redo accepted families or local benchmark loops.

Integration19 is accepted; this checkpoint includes source/doc/proof changes and
the completed cumulative Native/Go allocation comparison. No tracked production changes after
freeze; all source/control/profile/final evidence independent ACKs closed.
- Frozen compiler bd28c9ddd6a18c402dc60890afd031775e16058c09d69a0495cdb4d5bb83260f.
  Production10.07s zero warnings/errors; NativeCLI11.05s exact frozen compiler.
- VSCode FIRST17.22wall/71.48CPU/9,672,180KiB,10completeDTO/Gochains exact.
  One observation versus prior17.72s; CPU/RSS did not decline, no speedup claim.
- One capture BEFORE tests:10,818,807,776sampledB/10.075800GiB,−3.2024%vs18,
  −71.3493%originalServerGC.92,791typed ticks, one missingstack,zero loss/truncation.
  Offlineanalysis only overlapped correctness tests. Profile independentACK closed.
- Managed1712pass:Checker832/TypeSystem84/selectedParser695/additionalNoEmit101.
  NativeFoundation3153+87shared controls/utilities20/full15304originals exact.
  FullNative120chunks340.08s. Actual compiler/applicationILCinputs,587/242guards exact.
- InitialNativeFoundation MissingMethodException was stale RID Parser.Validation;
  fresh already-built dependency restored, onlyChecker/Foundation republished.
  Initialfailure retained. Finalcontroller aggregate-only NameError occurred AFTER
  allNative outputs/result persisted; aggregate independently reconstructed from
  unchangedTRX/stage/chunk records. No semantic repair/production rebuild/retests.
  Twelve cold nullable warnings match18, separate from productionzero.
- Changes: AST composite-only cache, Node/Type/Mapper runtime typed selfviews,
  PseudoType singleobjectembedding, source missinglistsentinel; known-count hints,
  intersectionorderedSet threshold17, lazy leasedRelater reporter reuse, one-pass
  authored genuineProgram libraryclassification. Preserve Go copies/retainedcallbacks.
  Source mapping/doc/proof updated; no unresolved sourcefindings.

NEXT WORK IS RUNNING, not awaiting user assignment:
- support owns artifact-only cumulative allocation measurement under
  artifacts/vscode-allocation-rollout/integration-19/cumulative-allocation.
  Source-ready0650da84/scanner source and result ACKs closed; Native+Go runs passed.
  Actual Native ILC references frozen19 compiler directly; Go -overlay changes only
  CLI main measurement wrapper/imports. Tracked production/upstream source untouched.
- Native public GC.GetTotalAllocatedBytes(precise:true) reports before103,648B /
  after10,675,045,432B /delta10,674,941,784B. ServerGCtrue,CPUcount16 is NOT GCheapcount.
  Gen0/1/2 collection deltas36/17/9. Full10diagnostics/guards exact. Go completeddelta8,992,138,304B (before1,014,928/end8,993,153,232); ratio1.1871416,
  gap1,682,803,480B. Bothcomplete10diagnostics/status/guards exact.
- Go runtime.ReadMemStats.TotalAlloc measured around normalrunMain, before os.Exit;
  no forcedGC/profiling, sidecarconstruction outside finalcounter. Absolute+delta
  boundaries differ somewhat byfrontend/startup; not exactpurechecker/RSS/native
  memory/objectcounts. Instrumentedelapsed is NOT benchmark. This resolves actual
  sampled/lastGC-lag ambiguity; do not revive prior exact5x allocation claims.
- scanner independently verified measurement results; resultACKb427ca2f closed.
- verification final docs/proof consistency ACK closed; all19regressionexecution
  finished. Root owns the checkpoint; support/verification own next GC design/review.
- Next task assigned: support designs bounded standard ServerGC policy/heapbudget
  comparison on UNCHANGED19Nativebinary; verification independentlyreviews. Four
  checkerworkers/16CPU and prior37.57%SVRsampledcost motivate this now that cumulative
  allocationgap is18.7%, notseveral-times. No productionGCpolicychange yet; wait
  designreview/rootrelease, smallcontrolledcompleteVSCodecomparison, no hugegrid.
  Continue from measured gap+existing17Native/GoCPU evidence to next substantial
  source-grounded allocation/runtimeaction. No current evidence of another dominant
  missed Go family. Naive publicSymbol Memory handles and ConcurrentDictionary swap
  remain reviewed unfavorable designs; don't redo them without new evidence.
  Runtime GC tuning is being assessed; no production configchange chosen yet.

No dependent publication remains active. Preserve this accepted checkpoint and
continue autonomously with the assigned runtime investigation. No commits during
any future dependent publication. No new
process framework, SVGs, custommaps/globalhandles/unsafeGCpointers/speculativepooling.

# Allocation layout and current CPU follow-through accepted (2026-10-11)

**Resume here:** the user authorized current Native AOT/pinned-Go CPU investigation
and executing on identified storage/parallel-efficiency items from `82873ea`.
Integration18 completes the reviewed compact internal-location family and its
acceptance. The standing comprehensive Go-faithful optimization mandate and SOP in
[START-HERE.md](../START-HERE.md) remain; do not restart accepted families or revert
to per-local-change tests/profiles. **Go performance parity is not achieved.**
This checkpoint closes these identified changes, not every possible runtime cost.

All 26 map-backed internal link stores and ValueSymbolLinks sparse pages now retain
8-byte private ordinals into stable chunks rather than 16-byte Memory locations.
Caller live refs/escaping Memory, zero/presence/identity and source 2-to-256 growth
remain. The new standard List chunk directory adds indirection/storage; no global
handle registry, unsafe pointers, new checker algorithm or public identity change.
Independent Sol source, control and allocation reviews closed without findings.
Source-ready and review artifacts: artifacts/vscode-link-local-ordinals/.

Accepted integration18 evidence:

- Compiler ec1fd52aa36dd43847ba74cc503ffa7ad6cabfa9e695560a1da2101a57d52245.
  Production build 9.43s, zero warnings/errors. CLI Native publication 9.83s consumed
  exact frozen compiler. Actual VSCode FIRST: **17.72s wall /70.46s CPU /9,126,904KiB**,
  expected exit 1, all 10 complete diagnostics/available pinned chains exact, no drift.
  Previous single observation 19.83s; saved Go 9.61s. No repeatable speedup claim.
- **16,321 managed passes**: Checker 832, TypeSystem 84, NoEmit 15,405; zero failures/
  skips. Full 15,304 original records exact in managed and actual Linux x64 Native.
  Native utilities 20 original observations plus added shared growth/live-alias
  assertions pass. Actual ILC inputs match compiler; 587 source/harness and 242
  production/generator inputs unchanged. Earlier 16/17 broad foundations retained.
- ONE allocation capture: **11,176,731,432B/10.409142GiB sampled**, −3.9768% versus 16,
  −70.4014% versus original matched Server GC. Includes the 17 guard, not isolated ordinal result.
  All 96,056 ticks have type/stack with at least one resolved frame; no loss/truncation/
  overflow. Complete value/map families including common integer locations and
  directories fell 442,612,432 sampled bytes. Source-classified UInt64 arrays have
  98.64% bounded caller coverage, not exhaustive physical event-owner proof.
- Some dependency preparation/initial test work overlapped capture due crossed
  scheduling messages; pause/resume evidence retained. No profiled time comparison.
  Full managed NoEmit had started before duplicate-corpus reduction arrived, so it
  finished once without abort/repeat. Fix sequencing before launching heavy work.

Current CPU baseline 17: SVR runtime methods: 37.57% of sampled user CPU, mainly marking/compaction
but including allocation helpers; dedicated GC threads: 24.14 scheduled CPU seconds. Go builtin
GC-bearing stacks: 5.55 CPU seconds use a different classification/domain; no exact
subtraction. Native worker DWARF unwinding failed, leaves resolve, zero sample loss.
Four workers carry 87.87% of worker samples and match pinned checker grouping; no worker-budget fix.
No current proof cache locks dominate; Monitor.Enter only 5 samples. Raw profiles retained,
no SVG. Report: artifacts/vscode-allocation-rollout/integration-17/cpu-comparison.json.

Design decisions: naive Memory-backed public Symbol widens every reference from 8 to 16 bytes
through tables/arrays/AST/types and parent/export fields; a facade retains N objects.
Retain public class identity, without claiming no future packing design can help.
Authored token guards actual cross-checker API ownership, not just observation. Standard
ConcurrentDictionary cache conversion deferred: extra per-entry nodes, no demonstrated
contention bottleneck. Immutable Program metadata dictionaries and source four-checker
budget stay. No additional concrete missing dominant Go ownership family was found;
remaining public record/standard collection/runtime costs require source-grounded design
and measured evidence, not arbitrary copy removal or a custom global handle system.

[Durable proof](../validation/results/allocation-rollout-verified.json),
[assessment and reproduction](./VSCODE-CHECK-STATUS.md#current-cpu-and-storage-follow-through-2026-10-11-accepted)
and [source mapping](../source-map.json) record exact snapshot boundaries. Three existing
Sol agents reused; no token or cost measurements available. All implementation/profile/
validation tasks and final independent evidence consistency review finished without
findings. This record is delivered with the implementation commit. Do not rebuild
after commit merely to change SourceLink metadata in evidence.

# Comprehensive allocation rollout accepted (2026-10-11)

**Resume here:** the comprehensive Go-faithful allocation rollout and its final
profile-driven Latin1 guard continuation are complete, independently reviewed and
verified on actual Linux x64 Native AOT. Source implementation checkpoint `c1efeb9`
and broad acceptance checkpoint `4d058ea` precede the final guard delivery commit.
The [durable proof](../validation/results/allocation-rollout-verified.json),
[assessment/reproduction](./VSCODE-CHECK-STATUS.md#comprehensive-rollout-integrated-results-2026-10-11)
and [source mapping](../source-map.json) distinguish both accepted snapshots.
**Go performance parity is NOT achieved.** Completion means the established
allocation techniques were rolled out through connected owners and consumers,
not that every .NET allocation disappears or all future performance work is done.
No implementation/test/profile process remains active after final acceptance.

## Final accepted evidence

- Latest integration17 Native VS Code: **19.83s wall /72.27s CPU /9,441,460KiB
  peak RSS**, expected exit1, all ten complete diagnostics/available pinned chains
  exact, empty stderr and clean source/config/binary guards. One observation;
  saved pinned Go9.61s remains faster. No isolated speedup claim.
- Latest production SHA256:
  `d12c2ce2e799ae728a7597ffbabb21284732b82f5fde4e8f87a785cfe7808f16`.
  Native CLI `26003fc2f4f423d51c72c2771fb183a64b2ae6be84528ee3675769adc9304369`.
  Production zero warnings/errors,242 guarded inputs; final587 harness/production
  inputs and five Native binaries unchanged across verification.
- Integration17 affected managed **4,741pass** (Scanner3842,Program264,
  selectedParser635), actual Native foundation3153+55text/hash controls,
  Program78+11filename and Scanner1276complete rows pass. Full original
  **15,304 Native noemit records pass**, including ordering/parent relationships;
  complete aggregate output hash equals integration16. No failures/skips in selected
  managed cases. Eleven reviewed cold nullable warnings remain, separately from
  production zero warnings. Source/control independent reviews closed.
- Retained integration16 broader evidence: **40,451 unique passing managed outcomes**,
  composed of full12-suite40,434pass/17cold failures and reviewed264Program+11filename
  reruns. Original failures preserved; not one fictitious all-green run. Native
  original15304, fresh scanner/parser/binder17,723inputs each, all83semantic/control
  commands and direct/shared foundation/Slice controls passed. These were not all
  reexecuted under17. Nine supported disk CLI cases pass; command-line-only
  ListFilesOnly config transport correctly failed6266, with public override managed-only.
- ONE final comprehensive allocation capture belongs to **integration16/c20**,
  before the small17guard successor: **11,639,613,928B /10.840235GiB sampled**,
  −2.111860%vs14 and −69.175587%vs original matched ServerGC.99,239typed events,
  2missingstacks; zero lost/truncated/overflow. Exact diagnostics/guards. No new
  capture for the small successor. Sampled CoreCLR weights are not exact Native
  AOT/Go bytes, retained memory or throughput. Corrected14 map subtotal includes
  VarianceLinks543,176B; historical total is unchanged.

## What was closed and what remains

Borrowed UTF8/WTF8 source/text/key/path ownership, retained/scratch Slice headers,
stateful immediate callbacks, embedded records/link storage, actual Go parser/binder/
parse-task reuse, shared spelling buffers and fixed-concat allocation are implemented
through their identified consumers. Actual Clone/Clip/detach, owned changed outputs
and retained closures remain. All14 final Latin1 guards use standard cached
SearchValues with full U+0000..00FF membership; existing strict rejection and
legacyUnicode/surrogate behavior remain. One existing shared control body covers
raw-string boundaries/hashing/output lifetime with all original assertions retained.

Independent source-role review found no further concrete missing Go reuse/ownership
path among dominant measured roles. Significant residual costs are canonical Symbol/
AST/type records, actual arrays/source buffers, standard dictionary layout and wider
Memory locations. ConcurrentBag does not reproduce sync.Pool GC eviction; idle
parsers retain their scanner and previous source. Public symbol packing/custom
handles/tables were not introduced to chase allocation equality. Any subsequent
performance work should start from these measured remaining roles and preserved
source requirements, rather than restart local changes or undo required copies.

Follow the current SOP in [START-HERE.md](../START-HERE.md) after compaction.
Use GPT-6.1 Sol implementation/review with coordinator-owned integration. One source
family reviewer is enough; actionable child handoffs use followup_task. Keep build
metadata frozen during publication: SourceRevisionId alone does not freeze SourceLink;
use frozen artifacts with BuildProjectReferences=false after explicit dependency
compilation, or commit before publishing. Source/confidence evidence and pending work
must remain distinct. Older sections below are historical and do not restart accepted
work. No new emit or language-service campaign is authorized by this checkpoint.

## Recovery after VM interruption (2026-10-10)

VM reboot reported and current uptime confirmed restart. Working source, committed
records, immutable snapshots and peer reviews survived. Existing Sol agents resumed
through followup_task; no replacement implementation/re-audit needed. No live prior
build/check processes remained. Recovery verified242 integration8 guarded inputs:
exactly6 reviewed successor sources changed, no unexpected changes. Latest Evaluator
consumer ACK fa3af28e/ad195c4c and owner/helper hashes survived.
Integration9 source-hashes/build-result/logs were empty after interruption; preserve
as interrupted/unqualified, not passed. Replacement integration10 command passed,
zero warnings/errors/no drift;1.111s was incremental reuse, NOT fresh compilation time.
Frozen library667d06599ae9f3a66abd8a699b9ae6954840d4440c0280cfeb7bf759fac9f581.
Native publication10 passed9.324s consuming exact library, but actualVSCode FAILED
exit134 after5.01s: InvalidCastException in checkTemplateExpression still casting new
boxed evaluator descriptor to string. No diagnostics produced. Failure and full logs
saved/fsynced in integration-10; source/config/binary guards unchanged. This source
receiver defect is distinct from VM interruption; last successful workload remains8.
Support finite repair e10d5f42 passed scanner whole actual Evaluator-consumer review.
Integration11 PASSED:9.516s production command (library recompiled),0warnings/errors,
242inputs/no drift; Nativepublish9.187s consumed exact81548d13e08910c81b0ef66eea69328a868b4c9647acb50d9f2a252322221613.
Native c81f5d314103208e46b5e001a20c984c549aab4f38710d0e3775ca6083aeab0d;
actualVSCode22.03wall/92.84CPU/9,393,184KiBpeak, expectedexit1/all10completeDTO+
availablepinnedchains exact, stderrempty/allguards clean,0majorfault/swaps. Single run,
no causal speed claim. Summary saved/fsynced under integration-11/boundary-summary.json.
Integration12 subsequently passed for the complete link-storage family (below).
Integration11 remains the last pre-storage workload; allocation profile8 does not
measure subsequent receiver/storage changes. One matched integration12 capture completed; no full suites or repeated timing runs
are queued. Current allocation evidence is summarized below.
Internal-link arena implementation was authorized after independent feasibility review.
ValueSymbolLinks now uses struct payloads in existing GoArena chunks with standard
Memory locations and live ref-return consumers (75Get calls,32ref record locals).
Storage/runtime4, productionconsumer4 and coldobserver6 source families are complete
and independently reviewed; integration12 now covers these changes in production.
Wider Memory page entries and chunk-tail retention have measurable allocation costs,
recorded below; do not claim packing lowered allocated bytes for every store.
LateBoundLinks now uses the existing Core.LinkStore<Symbol,LateBoundLinks> map,
matching Go rather than the prior ID-indexed store; the completed map-family change
now packs its struct payload too. No public Symbol representation change was introduced.
The consolidated Core.LinkStore assessment initially found 25 internal map-backed
owners; followthrough found enumMemberLinks also belongs to Go's map store, making
26. Root authorized the whole connected family: existing Dictionary keys plus
Memory locations into GoArena struct chunks, live ref consumers, source zero-state
initialization, inline two-table member/export storage and existing cold observers.
The sole genuine nodeLinkStore payload, SymbolNodeLinks, is also included: inline
struct pages, ref Get and standard Memory TryGet preserving PAGE presence even for
untouched zero slots. No arena indirection is introduced for those Go inline pages.
This closes the known selected link storage ownership family before one integration.
All selected storage, production and cold-observer source changes are now implemented
and independently reviewed. Final owner3/consumer8/cold10 handoffs are saved under
artifacts/vscode-map-links-packed and artifacts/vscode-value-links-packed. Source is
paused after coordinated integration12. Production build passed10.264s,0warnings/errors,
242guardedinputs unchanged; library98679002c013ca793b96510104f6ef850cf6aabec181bd723ff5d1d5ec010fd9.
Nativepublish10.894s consumed that exact library; native741409c702fbc73fb43bed53e01fbb7128edb1ce708ed07bd2dfc987a0957391.
ActualVSCode21.23wall/83.18CPU/9,367,284KiBpeak, expectedexit1/all10completeDTO+
availablepinnedchains exact, empty stderr/guards clean,0majorfault/swaps. One observation,
not proven speedup; broader regression acceptance remains pending. Evidence saved/fsynced
under integration-12/boundary-summary.json. Existing cold observer adapters are reviewed
but not exercised by the production CLI workload.
Review found two actual lifetime joins: JSX's recursive visitor needs an escaping
live location, and deferred iteration must reread SourceFileLinks.deferredNodes on
each step so recursive field replacement is visible. Existing schemas stay unchanged.
Legacy GoArena List-returning helpers have no live callers; no cleanup is needed.
No public Symbol packing, custom handles or per-type build/test cycles. Verifier has
standing authorization for the integration build and actual Native AOT VS Code run,
including coordinated repair of concrete failures. Assess the integrated result
without stopping at the handoff; full regressions remain deferred at this boundary.
Host currently16CPUs,31GiB RAM with28GiB available,8GiB swap unused; no crash cause
established, do not claim OOM or attribute the reboot to this task without evidence.

## Integration12 allocation evidence and continuation

One matched CoreCLR capture:12,093,367,752B/11.262826GiB sampled allocation weight,
−3.23875% versus integration8 and−67.97394% versus original matched ServerGC profile.
103,631 ticks, all typed/stacked; zero loss/truncation/aggregation overflow; exact10
complete diagnostics and guards clean. Rawtrace/ETLX and comparison.json retained
under integration-12/managed-profile. No profile11 exists; this comparison includes
both receiver/callback followthrough and the current storage family, not isolated effects.
Disjoint named link-type buckets: value arena+locations1.080→1.276GB(+18.14%);
true node pages267.8→146.0MB(−45.48%); map links621.4→659.9MB(+6.19%). Combined
selected link weight increases112.6MB while overall weight decreases404.8MB.
Closures fall155.4MB and delegates146.5MB. Fewer record objects does not imply fewer
bytes: standard Memory widens sparse page/dictionary entries. Runtime/GC throughput
cannot be inferred from allocated-byte weights or one native timing observation.
Scanner assessed standard representation alternatives. StrongBox can restore pointer-
width sparse entries but also one object per record; no throughput advantage is proven.
Root retains the integrated representation, with its measured cost explicit, rather
than starting isolated rollback/benchmark cycles. The source-role assessment identifies
remaining retained typed-path substrings; the connected continuation below is authorized.
Broad xUnit/Go/cold-observer regression qualification remains pending, not waived.

## Current source and execution boundary

**Active continuation: complete typed-path borrowed-text ownership.** Go rooted path
Directory/WithoutRoot/RootAndRelativePath borrow headers; C# retained path structs still
use CLR string slices. Profile12 attributes50.8MB to Directory and42.7MB to WithoutRoot,
with27.0MB adjacent basename work. These are sampled roles, not guaranteed savings.
Complete Rooted/Relative/PathKey owners, transformations, content equality/hashing,
retained keys and internal resolver/Program/VFS/configuration consumers together.
Use existing immutable byte descriptors and standard .NET comparisons/copying. Keep
actual changed normalization/concatenation outputs, OS Unicode conversion and public
display/JSON boundaries; don't claim all normalized path allocation is removable.
Do not preserve conversion bridges merely to finish local edits. No per-file builds,
tests, profiles or performance claims; review and integrate the whole family first.
Implementation12 remains the frozen baseline while this continuation is unverified.
TsPath/runtime owner family is source-ready for independent review at
artifacts/vscode-typed-path-values/build/source-ready.json (17 live files). The former
BorrowedPaths overlay is removed after moving single descriptor algorithms into their
direct source counterparts. VFS/configuration production22 and required cold12 receiver joins are source-ready
for independent review under artifacts/vscode-typed-path-consumers/verification.
The semantic33 family is ready under artifacts/vscode-path-semantic-consumers/build/source-ready.json.
Final reciprocal reviews are closing; source is paused for coordinated integration.
Those initial reviews subsequently closed; integration13 compiler result is retained:
8.46s,4errors,0warnings,242guardedinputs (237production files+5generators), no drift.
No native run attempted. Three Glob string/descriptor bindings and one ResolvePath
descriptor overload are being closed as one actual owner/consumer repair, with source
review before the next coordinated build. Do not insert intermediate Materialize shims.
Review includes readable source structure, public null
versus empty adapters, content equality/hash and actual native-buffer lifetimes.
Include the changed configuration-generate-tsoptions.py in the next guarded generator
set alongside the previous four generators; source counts remain dynamic.
The same integration also closes the already identified immediate contextual-object,
awaited-type and declaration predicate callbacks from integration12/source-roles.json.
Support uses existing explicit-state cores in Checker.cs; preserve lazy reads,
recursion/cache publication and genuine retained callbacks. No separate small gate.

Three existing GPT-6.1 Sol agents, shared workspace with disjoint editing:
- scanner: all TsPath owners/helpers and required Runtime byte helper APIs; ensure
  content-based typed-path equality/hash, not backing-array reference equality.
- support: Compiler/Module/ModuleSpecifiers/Checker/Ast/Binder and other semantic
  path consumers, coordinating borrowed APIs with scanner.
- verification: Vfs/TsOptions/Projects path consumers and their actual external
  boundaries; sole coordinated build/publish/workload/profile executor after review.
Root owns scope, decisions, documents/source-map and commit/acceptance. An idle agent
needs followup_task to restart; send_message alone does not start its next turn.

**Latest workload and allocation profile: integration12 above/below. Prior integration8 evidence:**
- Production build10.381s, zero warnings/errors;238 production sources plus4 generators,
  no drift. Library a745e5467f78e768c48b26a948c4a5b9b633c01675dd404dc46c56c96cd0b5c3.
- Native AOT publish10.912s consumed that exact library; native
  1b587dbf03007e13ef84e7d77daf4b4ecf5ee1c216a939d1ff6a15b3d93997ba.
  Actual ServerGC=true, ProcessorCount16 (NOT a measured heap count).
- Actual VS Code24.54wall/91.59CPU/10,076,540KiB peak, expected exit1,
  all10 complete diagnostic DTOs and available pinned chains exact; empty stderr;
  no source/binary/config drift, swaps or major faults. Single observation, no
  wall-speed improvement claim versus earlier integration5 23.63s.
  Evidence: artifacts/vscode-allocation-rollout/integration-8/boundary-summary.json.
- Integration7 failed8.610s with one legacy null recursion-stack binding; exact
  reviewed null→default nil-header repair produced clean integration8. Failure saved.
- Integration6 remains distinct:26.13wall/112.18CPU/10,020,768KiB peak, full10 exact.
  Its cost increase is unresolved; not dismissed as proven variance or causal defect.
- ALL known ownership families below are implemented/reviewed and in integration8.
  ONE existing-recipe managed allocation+sampled-thread CPU capture completed on frozen
  integration8:12,498,151,248B/11.639810GiB,−10.9407% vs integration5 and−20.5078% vs
  saved b84. Original matched ServerGC35.167694GiB→11.639810 (−66.902%); older
  pre-ServerGC capture not pooled. Zero loss/truncation/overflow;108641ticks,2without
  stacks. Full10 diagnostics exact, guards clean. Closures906→504MB,delegates689→440MB,
  arrays5.047→4.648GB. These are sampled CoreCLR weights, NOT exact Native AOT totals.
  Rawtrace/ETLX/joint/comparison under integration-8/managed-profile. Managed CPU
  samples dominated by waits/PollGC, cannot establish native CPU shares. Source paused;
  residual classification identified actual smaller semantic receivers; next source
  family below is authorized. No new Native CPU capture, full suites or repeated runs
  authorized. No final acceptance claim.

**Previous integration5 workload and latest allocation/CPU profiles:**
- Production build9.544s, zero warnings/errors,238 source files, source drift NONE.
  Source and compiled output copies: artifacts/vscode-allocation-rollout/integration-5/.
  Library ceab9d4efd339e96c521ccd851cfc32f85c6a76f8846a8c4d226e3b49f754c94.
  Top-level managed output hashes are authoritative; copied old linux-x64 subtree is
  explicitly stale. Read snapshot-roles.json rather than assuming every copied file is new.
- Native AOT publish11.628s consumed exact ceab; native
  d4fdc64525860ced308595361688d6bc926e969a6e29b5f61ac85e748f84c966.
  Actual ServerGC=true, ProcessorCount16 (NOT a measured heap count).
- First actual VS Code run: expected exit1, all10 complete diagnostic DTOs and available
  pinned chains exact; stderr empty;23.63wall/94.93CPU/9,064,632KiB peak, no swaps or
  major faults, source/binary/config drift NONE. This is not a strong speedup claim.
  Exact evidence: integration-5/boundary-summary.json and first-vscode/result.json.
- One matched CoreCLR profile:15,722,496,824→14,033,517,160 sampled allocation interval
  bytes (14.642716→13.0697GiB,−10.74%). No lost events/truncation;123074 allocation
  ticks,123073 with stacks/one without. Full10 diagnostics exact. Current main types:
  Symbol1.356GB,byte-key/Symbol entries1.190GB,ValueSymbolLinks775MB,Type[]630MB,
  Identifier599MB,String474MB. Wider headers/key entries are real tradeoffs; object
  merges redistribute type buckets. These are not exact Native AOT allocation totals.
  Evidence: integration-5/managed-profile/comparison.json plus raw trace/ETLX/stacks.
- One native cpu-clock:u99Hz/DWARF8192 profile:7,986 samples/no lost lines, ServerGC
  helpers48.272% exclusive sampled user CPU, XxHash32Complete0.63%, alternate key lookup
  1.29%, Dictionary.FindValue3.06%. Worker4066/4066 samples lack caller frames; unknown
  ~0.7%. Header-only metadata warning retained; primary leaves/report clean. GC CPU
  cannot be subtracted from wall time. Evidence: integration-5/native-cpu/analysis.json,
  comparison.json and perf.data. No SVG or repeated timing run.
All captures completed. The continuation below is covered by integration8, but NOT
by integration5 allocation/CPU profiles. Integration8 single managed capture completed; no full suites queued.

**First continuation included in integration6 (also retained by integration8).**
- Binder descriptor construction and direct typed new at the three existing factory
  boundaries are complete/independently reviewed: artifacts/vscode-binder-borrowed-names/
  build/source-ready.json (c8ae0e93 Binder). Ambient/private/signed/external module names
  retain bytes until actual Go-owned concatenation. Empty GoObjectArena adapters never
  packed records; replacing new T/Activator with typed new preserves identity/counts.
- Parser parseList forwarding now uses static callback state and one original
  parseListIndex<TState> algorithm; obsolete empty runtime adapter removed. Complete/
  reviewed: artifacts/vscode-parser-list-state/build/. No separate execution gate.
- TypeMapperData:TypeMapper one-object representation matches mapper.go:102 embedding,
  preserving data=self, factories/dispatch and deferred captures. Complete/support-ACK'd:
  artifacts/vscode-mapper-single-object/build/ (Mapper77d703b9). Finite named observer
  guard handles bare wrapper versus concrete payload without recursive double counting;
  logical schema and expectations unchanged.
- Checker cache/accessibility8-file family complete/scanner-ACK'd:
  artifacts/vscode-checker-cache-lifetimes/build/. Active mapper/cache headers retain
  cleared dictionaries in spare slots exactly as checker.go22634 (previous port discarded
  them); current prior capture attributes71,764,640B Dictionary weight to that caller,
  not all dependent buffers. Deferred property read/write headers preserve true Clone;
  switch headers preserve cap-clamped ranges and nil/empty witness states; retained
  accessibility caches/NodeBuilder consumers preserve optional cached-nil distinction.
- Checker synchronous callback family complete/scanner-ACK'd:
  artifacts/vscode-checker-synchronous-state/build/. Source object/conditional type
  instantiation and class initializer ancestor predicates use explicit state with one
  mapTypeWithAlias algorithm (checker.go22792/22973/13977). ~200MB prior disjoint
  closure/delegate attribution is a lower bound, NOT promised savings.
- Verifier complete synchronous Relater predicate/template-map and supporting
  Core/TsPath state family is source-ready and both peer-ACK'd at
  artifacts/vscode-relater-state-predicates/build/ (relater.go765/2378/3224/3287/4610). ~232MB prior disjoint
  attribution is a lower bound. Preserve callback order, lazy target.types[i] and Quit.
- Scanner supplies shared FindAncestorOrQuit<TState>/FindAncestor<TState> with one
  original loop; artifacts/vscode-ancestor-state/build/. Completed return-statement
  explicit-state helper: artifacts/vscode-ast-return-state/build/; latest shared
  Ast Utilities066f03c5, peer-reviewed. One recursive Visitor at the existing virtual
  child API remains; no generated visitor framework was added.
- Final Checker readonly/context-sensitive/return/yield predicates completed and
  peer-reviewed: artifacts/vscode-checker-node-predicates/build/ (Checker26384ddd,
  Utilities5d5db24e). Keep original traversal order, early stop, function/type/decorator
  boundaries; yield also retains one recursive Visitor at the virtual child API.
- Inference immediate declaration predicates completed/peer-reviewed:
  artifacts/vscode-inference-declaration-state/build/ (Inference61c4dde4), pinned
  inference.go843/1630. Original lazy current skip-node-set lookup retained.
Additional known owner-path closure, now included in integration8:
- support: tuple element-info storage/normalization and retained variance metadata
  through Checker/Relater/Inference, reviewed6-file family at
  artifacts/vscode-tuple-variance-slices/build/. Source clone-on-transform and
  variance uncomputed/nonnil-empty cache distinctions remain.
- scanner: NodeKindIs variadic spans, parseDelimitedList explicit-state parameter
  callback and nil-map Range without dummy Dictionary. Reviewed3-file family at
  artifacts/vscode-syntax-invocation-state/build/. Source enum short representation
  uses standard MemoryMarshal.Cast/span Contains; nonnil map enumeration unchanged.
- verification: ParseTask include-reason nil owner and original diagnostic consumers,
  plus existing copy-on-change lowercasing/direct bytes in GetScriptKindFromFileName.
  Reviewed3+1 families: artifacts/vscode-parse-task-reasons/{build,core-script-kind}/.
- support connected Type scratch6-file family complete and independently reviewed:
  artifacts/vscode-checker-type-scratch/build/source-ready.json. Covers return/yield,
  iterator and retained mapped results; inference unmatched/indices; contextual/indexed
  properties; mapped/conditional/base constraints; enum/assignment/rest keys; relation
  discriminant matrix, JSX and pseudo unions.74 immediate Type literals use nonescaping spans. Preserve actual
  unique append, visitor captures, nil/empty, source capacities, Clone/Clip, canonical
  normalization and published buffers. Inference preference callbacks separately reviewed.
- Remaining in-place instantiation/relation stacks already retain/reuse capacity;
  external input/output adapters and genuine owned outputs remain explicit. Scanner
  and Program lanes report no established substantial remaining owner omissions from
  current findings. Two unmeasured JSDoc callback locals remain; no tiny tail patch selected.
Consolidated production owner families now independently reviewed and in integration8:
- support (artifacts/vscode-checker-symbol-node-scratch/build/source-ready.json): ambient-symbol/cache and export visited-symbol headers; renamed-binding,
  duplicate-export/index/import diagnostic node groups, missing-binding/index-location/
  identifier-chain node scratch; outer signature-group mapped headers.
- verifier (artifacts/vscode-nodebuilder-tracked-slices/build/source-ready.json):
  NodeBuilder tracked-symbol cache/recovery and deferred-report headers complete.
  Original scope/cache/rollback/replay semantics retained. Program/Binder/Core lane closed.
- scanner: independent source reviewer for these complete ownership paths.
Retain in-place reused stacks, ordered-set Add storage and actual formatter/public
List adapters. No blanket collection redesign or repeated per-generic-type gate.
Integration8 build, actual Native AOT VS Code, and one managed capture are frozen
and complete. Broad owner-path rollout is not final acceptance. The profile/source
review found required instantiated Symbol/ValueSymbolLinks records, canonical AST
identities, member/normalization arrays and source buffers in dominant buckets;
no large missing storage owner was established there.

**Historical integration8→11 receiver work and initial arena assessment:**
The receiver work below is covered by integration11. Its original feasibility-only
arena restrictions have been superseded by the implemented connected storage family
at the top of this file; do not treat them as the current assignment or stopping rule.
- support source-ready/paused: seven connected synchronous callback sites (~256.4MB disjoint sampled
  attribution, NOT predicted savings) and four internal Checker Text/Name projections.
  Existing explicit-state/static callback and descriptor APIs; preserve lazy guards,
  object construction mutable state and real output/error materialization.
- scanner source-ready/reviewed: Evaluator string/numeric/template Result byte-descriptor ownership through
  retained enum consumer; preserve real owned concatenation, coordinate Checker.cs
  edits through support. Residual3 ACK saved; Evaluator2 enum/cache/relation consumer
  successor source-ready, scanner reviewing. Source owner Result already value struct;
  single any channel retains boxed descriptors. Binary builder empty-operand copy is
  a documented unmeasured runtime difference; Go can reuse the other operand. No
  claim every concatenation must allocate.
- verifier: profile classification complete at integration-8/managed-profile/source-roles.json;
  independently reviews Evaluator handoff. After that, bounded read-only assessment
  of actual Go Symbol/ValueSymbolLinks arena storage versus CLR per-record objects.
  Distinguish required logical identity from a required separate CLR allocation;
  report at integration-8/managed-profile/arena-assessment.json confirms real Go
 2→256 chunked struct storage. Public Symbol class/ref packing needs material model
  change; excluded. Internal ValueSymbolLinks may admit struct chunks and ref-return
  mutation, Memory<T> only for escaping locations; no implementation authorized yet.
  Support after evaluator closure assesses connected mutation/escape/null/identity/cold
  observation dependencies read-only. Avoid custom handle framework and direct huge
  sparse struct pages (source intentionally uses indirect arenas). Wider stored Memory
  descriptors can offset savings; no promised net memory/CPU benefit. No whole AST audit.
Keep measured raw Project-leaf total separate from selected four-site subset and use
consistent decimal bytes/MiB. Do not claim the whole bucket removable. No microtests,
new instrumentation/SVG or per-family build/profile cycle. Integration9 interrupted; integration10 produced concrete template-evaluator cast
failure. See recovery section: repaired integration11 workload PASSED after independent
consumer source review. Preserve integration8 measurement independently; no new
profile or broad tests automatically. Internal-link arena assessment informs the next
substantial decision after the active semantic defect is resolved.

## Retained design decisions and boundaries

- Standard Dictionary + immutable ByteStringKey snapshots preserve byte/raw-string
  content equality and stable hashes; no custom table/interner. GoBuilder directly owns
  Slice<byte>, uses standard span copies/source growth; published prefixes survive
  append/reset (reset detaches). Public/nonbyte compatibility projections stay explicit.
- LiteralType remains pinned types.go:895 single any/object payload; StringLiteralType
  is only an alias, not a new subclass. Box borrowed descriptor where Go string escapes
  into any; preserve fresh/regular sharing and computed-enum nulls.
- AST and Type payload/header merges are in integration5; Mapper merge is later source.
  Go FlowNode/FlowList and link records are separate identities, not embedded wrappers.
  Do not merge them into symbols/nodes or invent handles/custom arenas. Removing stable
  class identity requires an assessed storage/lifetime redesign, not a helper swap.
- Symbol otherwise matches Go fields; borrowed name/declaration headers add24bytes versus
  old CLR string/List references. Its8-byte authored ownership token is a real extra
  field. A binder-only token subtype was declined: modest savings do not justify new
  guard/layout complexity. No whole observer cleanup or additional state instrumentation.
- JSDoc clones share an established mutable public List projection. Keep its shared
  owner/canonical CAS; redundant nested fragment record removed. No projection cache.
- Six standard SearchValues<byte> ASCII scan sets stop before every >=128 byte; original
  rune/EOF and malformed UTF8/WTF8 advancement remain. Native hashing evidence does not
  support changing algorithms or adding retained hash state.
- Genuine deferred mapper/inference callbacks survive return and retain checker/context/
  index identity. Only synchronous callbacks become explicit-state static calls.
- Preserve true Go Clip/Clone/Concat/append/nil rules. Source review caught cloneSymbol
  requiring Clip, indexInfos Clip, config diagnostic Clip, mapped Append, line-map
  capacity and extended-config nil/Append. Earlier tuple-spread regression remains
  preserved below; do not weaken expected behavior to hide sharing bugs.
- Real external JSON/OS/diagnostic output boundaries may project owned arrays/strings;
  internal signature convenience alone is not such a boundary. Genuine option DTO Lists
  do not need a converter framework just to replace their container type.

Resumption/evidence navigation: START-HERE.md persists the SOP; this active section
tracks current work. docs/VSCODE-CHECK-STATUS.md now has a compact optimization reference
index, measurements/limitations and rejected directions; source-map.json carries
upstream mappings and explicitly pending current adaptations. Documentation checkpoint
04af035 committed the index/procedure; later live facts are saved in these files.
Implementation is still uncommitted relative to accepted b84b436. Do not stage all or
confuse source-only snapshots with accepted work. Earlier integration1–4 grouped build
findings and immutable snapshots remain under artifacts/vscode-allocation-rollout/.

**Qualified checkpoint committed `5e960cd`:** `c8962c20` library, `ac486c4d` native at
`artifacts/vscode-ast-text-values/`. Exact17 production files + generator and three
qualified authored AST test/runner files were committed at executed versions.
First clean build9.10s/publish10.28s. Native VS Code exact10:24.73 wall/102.16 CPU,
10,290,800 KiB peak, no swaps/major faults. Focused managed4143/native723 records
and controls passed, including authored AST5 both modes. Sampled CoreCLR allocation
15.453668 ->15.300418 GiB (-0.992%), one trace each, no loss/truncation, one no-stack
tick explicit. String allocation increased78.09MB; no speedup or isolated causality
claim. See qualification.json. Comprehensive final regressions remain deferred.

**Qualified receiver checkpoint committed `faad6ec`:** e79300ff library/8e9e134c
native. Build9.29s/publish10.94s. Native VS Code exact10:23.74wall/103.97CPU/
9,956,328KiB. Focused287managed (42 original records+Binder163+TypeSystem77+AST5)
and actual nativeAST5 all256-key extension passed. No new profile or speedup claim.
Evidence: artifacts/vscode-checker-text-values/qualification.json.

**Qualified checkpoint committed `d39e05c` (+provenance `72565f3`):** NodeList arena/header snapshot2
`061cb294` was committed from exact saved sources (30 production files +two generators
+18 successful harness/test/resource files). All focused qualification passed:
906 managed; native162 parser DTOs +29 shared cases +one authored semantic spread
record. Sampled allocation15.300418 ->14.869364GiB (-2.8173%), zero event loss/
truncation, all141334 events have resolved frames. Receiver+NodeList cumulative,
not isolated savings. Flamegraph saved under candidate-allocation/. Native VS Code exact10:
25.13wall/101.88CPU/9,753,968KiB. No speedup established. First build had35 finite
representation bindings, grouped retry compiled clean. Failed snapshot1 `b80e63ee`
then produced16 diagnostics: confirmed migration defect, not input drift (old e793
same-input still exact10). getEffectiveCallArguments omitted upstream slices.Clip
before tuple-spread expansion, overwriting shared suffix. Exact source Clip restored;
neighboring new append boundaries reviewed. Failed evidence retained. Do not use its
24.03s as accepted performance evidence. Focused spread-suffix regression demonstrates oldb80 failure and corrected061c
full pinned-Go semantic agreement, including intentional2345/repeats/queries.
Full regressions and repeated native timing stay deferred to later milestones.

**Qualified constituent/parser checkpoint:** combined parser scratch +Type constituent batch
`67bd3eb5` library /`d90a3628` native is preserved from its16 frozen source files.
Eight initial finite compiler bindings repaired together; clean retry9.615s.
Native VS Code exact10 but slower single observation:33.64wall/112.84CPU/
10,042,164KiB. Subsequent selected gates and alternating runs below establish that
the one slower observation did not repeat; no strong speedup claim.
Support reviewed changed ownership/cache/copy/boxing paths with no defect found.
Six alternating native observations all retain exact10:061c->67bd means
24.4033->23.97wall,102.4067->99.00CPU,10,473,164->9,889,178.67KiB peak.
Wall ranges overlap; no strong speed claim and no reproduced regression.
Managed442 selected checks and native162 parser+scratch2/sort1 pass. Authored stable
sort extension passes in managed/native execution; prior sort controls exercised
pdqsort only. Sampled allocation14.869364->14.642716GiB (-1.5243%), all139,008 events
typed/stack-resolved, zero loss/truncation. No SVG generated. Final qualification and
exact seven executed harness copies are saved; those qualified versions join the
checkpoint, while next AST layout edits remain separate in the working tree.
Saved sources: artifacts/vscode-type-constituent-slices/.
Parser scratch source/review: artifacts/vscode-parser-scratch-slices/.

**AST representation implemented, intermediate verification only:** scanner and support independently found
normal C# inheritance NodeData:Node can model Go embedded Node while eliminating
separate wrapper allocation and back-reference. Keep concrete bare Node and
Node.data=self dispatch; explicit hiding must avoid recursion/warnings. Common node
fields remain, so full1.11GB sampled Node bucket is NOT removable savings. AST-only
candidate, no Type layout/pooling redesign. Repeated timing did not reproduce the
initial slowdown, so scanner owns implementation and support independent review.
Generator and named observer compatibility changes are required; source algorithms/
identity/hooks/clone behavior remain intact by independent review.
Six-file snapshot62389c81 built clean10.816s/native publish11.312s. Native b557cb84
matches exact10/available pinned chains:27.53wall/105.85CPU/9,645,988KiB peak.
Single observation, no speed claim. No allocation capture/pairs launched. Saved under
artifacts/vscode-ast-single-object/. TypeSystem77 passed before latest user steering;
verifier stopped queued checks and cancelled the remaining Collections44 process
and its suspended launcher. That run is user-interrupted/incomplete, not passed.
Keep partial evidence explicit; no final acceptance/commit yet. Latest
whole-path strategy above applies to remaining work, not more micro-packet validation.
User emphasized .NET copy facilities: Slice growth/append/arena use span CopyTo;
one identity-only Reparser loop changed to CopyTo, transformations remain loops.
Old List adapters still copy and are being removed from hot paths.
Root approved public Slice<Node> child-list headers, clipped arena allocation and
Go Clone header/slot sharing; provisional C# List APIs may change, but actual Go
behavior/expected outputs remain. User informed. Ownership follows the three lanes
above; coordinated integration builds remain serialized. Keep List input factory adapters
explicit; no per-node wrapper or hot list snapshots. Replace incidental List
ReferenceEquals observer with Go Core.Same storage test only where original intended
assertion is the Go shared-first-slot rule; preserve results.
Do not interpret any checkpoint/agent final message as authorization to stop work.

**Qualified path packet, committed `08d2294`:** Path/PathByteText/DiskHost/ParserCompat plus
existing ProjectSupport controls and a nil-aware signature-slice observer adapter.
Restores upstream normalization fast path, avoids ASCII encode/decode round trips,
and gives singleton arena compatibility lists exact capacity. Frozen library
`b81a0750`, native `694d4f24`; clean retry after annotation-only nullable repair.
Native VS Code: 22.64 wall / 91.50 CPU seconds, 10,459,804 KiB peak; exact ten
complete diagnostics and pinned available chains. Focused: 5 path + 21 managed
ProjectSupport, 27 native controls, 48 canonical Go Project records passed.
Sampled CoreCLR allocations 16.497538 -> 16.404452 GiB (-0.5642%). One capture per
variant, no lost/truncated/missing-type/stack events. No isolated speedup claim.
Evidence: `artifacts/vscode-path-normalization-fastpath/`.

**Observer limitation:** selected Project channels passed, not the entire validator.
A stale Collections.Validation dependency produced an ILC warning for unexercised
StoredGraph.SignatureSeed. Refresh that dependency before using that channel or
its final gate. Current source already adapts slice storage. Do not expand this
into a private-state equivalence project. The separate Projects Runner compile
projection was repaired without changing null/empty/order/expected DTO semantics.

**Rejected mapper frame experiment:** split union scratch into a private no-inline
worker; actual native dispatcher frame shrank 216 -> 16 bytes (tuple variants
232 -> 16/32), removing early bulk reference clearing. Original union body unchanged.
Native VS Code and 11 focused checks passed, but three alternating runs each gave
19.4333 -> 19.9267 seconds mean (+2.54%), CPU +0.58%, overlapping ranges. No speed
benefit established. Removed only our nine-line source experiment; evidence retained
at `artifacts/vscode-mapper-union-frame/`. No further repeats planned.

**Comprehensive rollout authorized:** user explicitly requests rolling out all
identified Go-faithful allocation patterns, not repeatedly deferring wider adoption.
- Scanner agent: borrowed byte views/span operations across scanner/parser/JSDoc;
  first ready packet removes isValidIdentifier suffix strings and per-rune builder
  temporaries. Identify persistent token/AST boundaries requiring propagation.
- Support agent: checker/relater/flow Type slice sharing, capacity, scratch reuse and
  explicit callback state. First coherent batch: Flow antecedent storage and union
  consumers; next assess inference object callback arrays/lists. Preserve source
  ownership, independent headers, nil/empty, order and cache behavior.
- Verification agent: sole heavy window; existing profile attribution and runtime
  slice/common support. Batch ready source, Native AOT VS Code first, focused checks
  next. Profile after substantive batches; no repeated captures for tiny edits.
Measured b81a Type[] ~1,046 MiB and List<Type> ~689 MiB; infer-object callback
Type[]257 MB/List143 MB and flow-branch List146 MB are allocation buckets, not
promised savings. Scope coverage is implementation work, not another audit framework.

**Implementation sequence now underway:** combined reviewed batch includes borrowed
scanner token views/cached projection, parser fixed-token byte comparisons,
shared flow branch/loop Type slices through borrowed union inputs, conditional
inference two-target span, JsNum span iteration, and initial runtime string fixes.
Verifier froze all235 source files at library `93159964`, native `b62aaf99`.
The exact eleven production files are committed in `146839d`; subsequent source
changes belong to the next batches. Clean retry
9.22s, native publish10.81s after four finite compile-binding fixes. Native VS Code
passes exact ten complete diagnostics and pinned chains: 24.34 wall /96.57 CPU
seconds, peak9,829,460 KiB, no swaps/major faults. This single observation does not
show a speedup. Focused controls passed: managed scanner3842 +13 original flow/conditional records
+5shared; native539 complete scanner records +5shared. Cumulative sampled allocation
capture vs b81a:16.404452 ->15.453668GiB (-5.7959%), zero loss/truncation; all147193
candidate ticks typed with named stacks. No native throughput win established. Evidence:
`artifacts/vscode-scanner-token-views/`. Source-only next work has resumed.

**Callback/rune checkpoint, committed `d6a3cb5`:** library `4bc27d0c`,
native build at `artifacts/vscode-instantiation-state/`; ten exact production files
and the reviewed existing token-lifetime assertions are committed. Native VS
Code passes exact10:23.33 wall /97.59 CPU seconds /10,030,632 KiB peak. Clean build
8.85s, publish10.85s. Focused checks passed:19 original whole compiler records, TypeSystem77, Scanner3842,
shared lifetime17/token5 managed;539 complete scanner records and17 shared lifetime
cases under Native AOT. No new allocation capture for this callback/rune batch. New AST, private stacks/inference candidates and JsNum edits are UNSTAGED and
unqualified. Root has not staged any of these next changes.

**Next source ownership:** support continues remaining checker/relater/inference
callback state; rune enumeration/count and exact owned rune output plus range/count consumers are
in frozen4bc27. Scanner is implementing the AST stage; support is implementing all
identified private checker slice stacks, and verifier owns JsNum span/narrow-integer
fastpath work. Standard long/ulong span parsers handle decimal/binary/hex with
signed64 limit; octal needs a bounded adapter. These changes remain unqualified. Next scanner
stage is AST/parser/binder text propagation with generated-source updates: 16-byte
dual-backed immutable raw-string/byte-array view, public Text projections and
setters retained, borrowed internal reads and standard ordinal SymbolTable alternate
lookup. No dictionary replacement/interner. Extra per-node header footprint must be
measured. CLR ReferenceEquals between separately materialized borrowed text is not
Go string behavior; supplied owned-string/null/set semantics remain. Inserted string
keys are an explicit intermediate boundary, not comprehensive rollout completion.

Current followthrough decisions: continue checker TextValue consumers after the
AST ingress stage; inserted symbol-table string keys remain explicitly intermediate.
Next assess NodeList/GoArena shared-slice storage using the existing arena findings,
not another broad audit. The provisional C# List field types are not an explicit
user API-freeze requirement, but any migration must state API/harness implications
and preserve pinned Go element/header/clone/append behavior. Do not introduce
per-node wrappers that erase the allocation benefit or silently lose mutations.

**Current timing context:** callback packet 7c0f reduced sampled allocations
17.107677 -> 16.497538 GiB (-3.57%), but its three-each native comparison against
282c had elapsed +5.19%, CPU -1.47%. No net speed win. Native profile has 52.373%
exclusive SVR runtime helper samples (allocation/GC, not exhaustive GC census).
Worker caller chains unavailable. CoreCLR traces all showed 1 -> 3 -> 9 -> 15 heap
progression; actual native heap count unverified. No GC-policy experiment underway.

**Measurement caution:** saved Go 5.176 GiB is sampled alloc_space, with up to two
GC cycles of lag and no exact TotalAlloc/final-GC data. Unknown missing tail; no
exact cross-runtime allocation ratio. Profiled Go RSS 7.567 GiB differs from the
separate unprofiled 7.374 GiB run. Do not infer allocated totals from RSS.

**Process:** reuse agents, frozen binaries and existing evidence; batch concise
notes/checkpoints. Root owns STATE; support owns status/mapping updates. Avoid
new report frameworks and repeated qualification of unchanged source. Persistent
AST/identifier storage migration needs coherent ownership/API changes, not a
blanket rename; no global interner or GC-policy change. Do not stop at checkpoints.

**Timing context:** six alternating frozen native runs compare signature baseline
`cd2b47bb` to direct-slot candidate `c385e71b`: mean 23.17→21.36s wall (−7.81%),
100.017→95.76 CPU (−4.26%), peak RSS mean +0.90%; all six exact diagnostics.
Three runs each, descriptive cumulative evidence, not per-packet attribution.
`artifacts/vscode-link-slots/paired-native/report.json`. Pinned Go saved run was
9.61s wall / 46.86CPU / 7.374GiB peak; sampled Go alloc_space 5.176GiB uses a
different profiler/runtime and is not an exact Native AOT byte comparison.
CoreCLR heap history reported 1→3→9→15 heaps, not automatically sixteen because
ProcessorCount is sixteen. Native heap count is unverified.

**Callback approach:** generic explicit state plus static callbacks already serve
mapTypeEx, inference parameter/return application, property relations and non-null
reporters. Receiver-only callbacks cache once per checker following upstream
initialization. Direct-only local delegates become local functions where this
preserves mutable state/order. Trivial predicates use ordered loops. Rollout is
incomplete; do not rely on JIT escape optimization or claim all callbacks fixed.
Keep captured value versus shared mutable state semantics faithful. Inference
candidate storage migration remains deferred by measured size and alias risk.

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
Inference and visitor triage is complete; see active selected batch above.
No broad inference-candidate or Type storage migration has begun.

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
