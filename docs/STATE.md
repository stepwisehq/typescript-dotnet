# Paused for the day at the user's request (2026-10-13)

Latest results are recorded in [VS Code status](./VSCODE-CHECK-STATUS.md) and
[the investigation record](../validation/results/vscode-lsp-era-investigation.json).
Same-host best times: current Native/Go 9.61/8.52 s for check+emit (+12.8%),
6.96/6.15 s for check-only (+13.2%). Pre-LSP Native bests were 9.68 and 8.44 s.
All 30 runs passed correctness checks; the current emit median remains 8.5% slower
than pre-LSP, with broad variability and no established causal LSP regression.
Earlier target misses are retained. No compiler changes or running workloads.
Documentation and evidence are recorded in the accompanying commit. Do not resume
investigation or run additional benchmarks until the user asks to continue.

# Investigation checkpoint: LSP-era emit timing (2026-10-13)

Completed a source audit and same-host comparison of the accepted pre-LSP Native
executable, unchanged current Native executable, and pinned Go: five processes each
for check+emit and check-only. All 30 completed runs passed output/diagnostic checks;
all 20 Native runs qualified their full NoGC regions. Independent Sol review is clear.

| Mode | Pre-LSP best / median | Current best / median | Go best / median |
| --- | --- | --- | --- |
| Check+emit | 9.68 / 12.46 s | 9.61 / 13.52 s | 8.52 / 9.62 s |
| Check-only | 8.44 / 9.55 s | 6.96 / 9.49 s | 6.15 / 6.83 s |

Current best ratios to Go are 1.127934 and 1.131707, within 20% in these cohorts.
Retain both earlier emit target misses. Best emit is essentially unchanged from
pre-LSP, but its median is 8.5% slower with broad overlapping ranges. Check-only
medians are nearly equal. This does not establish a causal LSP regression or
improvement; performance variability and the smaller median difference remain open.
Emit allocations remain approximately 14.75–14.78 GB with similar peak RSS.
Source audit found ordinary JS emit scheduling/writer pooling unchanged; added
hover/declaration helpers have limited relevance to this JS-only workload. Larger
AOT image layout and parser guard changes are hypotheses, not established causes.

Evidence: artifacts/vscode-current-performance/baseline-emit-1 and baseline-noemit-2;
source audit lsp-era-source-audit.json; independent review lsp-impact-review.json.
The first noemit attempt stopped after one correct baseline process due to a missing
baseline label in the harness exit-code lookup. The corrected lookup preserves exit2
for all engines; the failed script/result remain in baseline-noemit-1 and are excluded
from the fresh cohort. No compiler changes, new publication or profiles were needed.

Documentation and durable result records are updated and independently reviewed.
This checkpoint includes the saved documentation and evidence. No running workloads.
The next informative investigation is fast versus
slow runs of the same binary, observing CPU work/scheduling before speculative edits;
do not automatically attribute the previous gap to LSP or revert accepted behavior.

# Completed: requested repeat of VS Code check+emit (2026-10-13)

Same current Native binary and pinned Go, unchanged settings and reference, five
alternating runs each. Best Native11.24s/Go8.37s =1.342891x (+34.3%, target missed).
Native samples15.15,14.74,11.24,15.29,11.98; Go9.22,8.67,8.37,10.27,10.92.
Medians14.74/9.22s. All 10,031 output files/150,466,543 bytes and ten comparable
diagnostics remain exact; all five Native full NoGC regions qualified. No build or
compiler change. Results are in artifacts/vscode-current-performance/emit-repeat-2.
Prior measurement-1 is retained separately below; check-only was not rerun.
Repeated emit target misses justify investigation, but do not establish the cause.
Sol review and documentation are complete; no further workload is pending from
this repeat. Its results are included in the current checkpoint.

# Completed: post-LSP VS Code refresh (2026-10-13)

The user requested fresh current-build VS Code measurements and documentation updates.
Root published the current source f709d076 as Linux x64 Native AOT, then ran a fresh
ordinary output comparison and five alternating processes per compiler per scenario.
All 10,031 emitted files (150,466,543 bytes) and ten comparable diagnostics match
pinned Go; every timed run passed diagnostics and all Native NoGC regions qualified.
Best check+emit: Native11.51s/Go8.09s =1.422744x, **misses the 20% target**.
Best check-only: Native6.77s/Go5.84s =1.159247x, meets the observed target.
All samples, medians/ranges, CPU/RSS and publication bindings are preserved in
[the current record](../validation/results/vscode-post-lsp-performance.json).
Do not rerun measurements or start optimizing merely because of this target miss:
this mandate is a refresh, and cross-date samples do not isolate its cause.

Current source HEAD f709d076; VS Code clean4038aefd; upstream
fed0bf24149fb1ed36039212648bafdafc1ea10e. Retained AVX2/min-worker128/checkers8,
Server GC, explicit NoGC16GiB emit/14GiB check-only and >=20GiB available preflight.
Artifacts: artifacts/vscode-current-performance/build-1 and measurement-1;
controllers/publication logs remain beside them. No compiler changes, new optimization
campaign, or broad regression rerun. Historical accepted results remain unchanged.
Sol independent result review is clear. README, current measurement/reproduction
documentation and the historical summary pointer have been updated; the pending
README runtime clarification is preserved. No further workloads are pending.
The user subsequently requested a commit; this refresh is included in the current checkpoint.

# Accepted: language services and LSP (2026-10-12)

The [whole contract](./LANGUAGE-SERVICES-CONTRACT.md) is accepted and delivered at
**21cdac7d4d5d28bf220703b62f3c66f2b94c6bfe**. Implementation, assertions, source mapping,
review records, reproduction and aggregate proof are committed; the working tree was
confirmed clean immediately after that commit. This documentation-only follow-up
records delivery for re-entry. No implementation, validation or review work remains.
Do not restart historical campaigns or rerun acceptance after compaction. Pre-commit
review/proof fields marking delivery as coordinator-pending are review-time snapshots;
this delivery record fulfills that action. Await the user's next direction.

Read [assessment](./LANGUAGE-SERVICES-ASSESSMENT.md),
[verification](./LANGUAGE-SERVICES-VERIFY.md),
[aggregate evidence](../validation/results/language-services-verified.json) and
[final independent review](./progress/lsp-final-contract-review.json).
Astra coordinates; GPT-6.1 Sol implements/reviews. No material blocker or user decision.
All Sol owners completed implementation and independent review; no queued agent work.
For future work, use followup_task to restart idle agents and coordinate builds centrally.

## Completed acceptance

- Pinned upstream fed0bf24149fb1ed36039212648bafdafc1ea10e, Linux x64,
  .NET10.0.401/runtime10.0.12 and Go1.27.1. No other platform acceptance claim.
- Managed language services508pass/1optional originalReplay skip; actual Native98
  all11 gates pass:406 whole original TypeScript functions and80 authored controls,
  separately adapted watcher/external/partial assertions. Original selection depth
  12000, eight decoder probes and fatal notification -6/exactmarker are included.
- Complete original editor corpus4571:4152pass/415originalskip/4samepinnedGo failures.
  Three missing-golden full outputs match as adapted evidence, not original passes.
  Fourth importType_js fails original VerifyNoErrors with the same two diagnostics;
  its subsequent intended reference assertions remain unexecuted in both compilers.
- Full inventory5006:4558whole original passes,416original skips (includingReplay),
  four reference failures,26platform exclusions, one Go-source generator, one adapted
  runtime test. Test functions, xUnit cases and partial channels are not interchangeable.
- Retained managed1730+496 allpass; full Parser18799 allpass. Fresh six Native
  publications/direct checks pass, including3153 foundation cases,87 shared controls,
  utilities20 and Emit152original/254authored.
- Full emitted-output registry15330:15285exactpublic outputs,13571originalassertions,
  39originalpreparation skips and six unchanged external incremental-metadata
  differences. No new differences. CLI6disk/3guard/3runtime controls pass.
- Full Native noemit15304 complete records exact, all120chunks success.
- Fresh actual public stdio and four-role signals/parent-watchdog comparisons pass.
  Complete responses/diagnostics, seven config/dependency phases,100queries/100unsaved
  edits, semantic tokens and source-admissible cancellation/stale results checked.
  Exact inherited terminals preserved: shutdown+exit/stdinopen gives1/context canceled;
  shutdown+EOF gives0/empty stderr. Long-lived server bypasses one-shot CLI NoGC.
- Corrected retained performance cohort2:75fresh processes, five runs each across
  five workloads and acceptedNative/currentNative/Go, matched actual runtime policy.
  Best whole-process current/baseline ratios1.0326/1.0501/1.0298/.9773/1.0608.
  Four modest2–6%slowdowns and one2.3%improvement remain explicit. Native allocation
  totals lower in allfive; distinct Go B/op units. No universal20% or external-project
  speed claim. Short public edit/request RSS increases are observations, not plateau
  or leak-freedom evidence. External projects and actual editor UI were not exercised.

## Evidence and resolved final findings

Artifacts under artifacts/language-services:
- integration-7: native-98/LanguageServices.Validation (SHA d16733b63b9fbb857ee75174fa654fb3fdae94db15fd743bab4e27e95ec16cbd),
  source-98/managed-inputs-98/native-inputs-98 and direct/corpus records.968 source
  hashes and179 actual ILC inputs bound. Actual editor run98.866s. Pinned Go CLI
  pinned-tsgo SHAfb9f349bdbeaad767473a3ab2893271bd106e978ed58a1c0b5ffcd5502450528.
- final-retained94 directory is staging name, actual current98 source. All7 phases
  completed with explicit frozen source and six actual ILC bindings. Fresh public CLI
  native/TypeScript.Check/tscheck SHAedd848db2d1595fd4cdb3173d291ed6212f67a1ae9bb550ea205ca4c4466aba9.
- integration-7/public-stdio-final and public-lifecycle-final: actual fresh paired
  public evidence. Tracked reproduction drivers in tools/language-services-oracle.
- final-performance/cohort-2/result.json; cohort1 retained. Python timedwait exit
  polling added up to50ms, creating shortworkload apparent1.780wall ratio. Runner-only
  correction uses blockingwait with60s watchdog; oldsource/data retained in
  wall-observer-transition. No production change/republication; correctedwall1.033.
- source-review: independent direct98/corpus98, retained managed/native/corpus-command/
  noemit/performance-cohort2 and public-final reviews CLEAR. Final aggregate review
  tracked in docs/progress/lsp-final-contract-review.json.

Final corrections preserved: genuine supporting-owner omissions (logging/Stringer/
nil handling, hover NodeCopy, borrowed SourceFile text, ByteStringValue equality,
writerdepth, named-number codec, lazy canonical source identity) were fixed and reviewed.
Source inventories alone were never treated as correctness. Native mapper initialization
failures exposed CLR pool-dependent IPC waits; complete blocking reader/handler/response/
duplex paths now pass original32-file managed/Native mapper tests. Dedicated OS threads
are a documented resource adaptation, not goroutine performance parity. Linux raw entry
names and UTIME_OMIT fixed with real controls. WatchTimeout harness now drains genuine
background work after unchanged2s sleeps, preserving Go synctest quiescence/assertions.

Retained Checker firstattempt831pass/1fail exposed stale reflection processEnding(List)
test binding. Direct call current Slice worker through existing friend access preserves
both original assertions; only test helper changed. Rebuilt3test-consumer projects;
reused passedEmit proof and reran832Checker+four remaining suites, all1730passed.
Old Emit493/Native251 recipe counts predated three already-accepted authored mutex
controls: actual496/254, original152 unchanged. Preserved allfailed attempts, oldfreeze/
recipe and exact counter/test-only transition proofs; no production workaround.

Efficiency lessons: complete source owners before broad acceptance, coordinate one
frozen build/publication set, reuse unchanged evidence after narrow harness corrections,
check actual test identity sets before trusting historical totals, and distinguish
observer scheduling/timing defects from compiler defects. Root ran all workloads;
Sol workers owned implementation and independent review. Measured durations/sample
counts are recorded; model-token totals, monetary costs and complete human-effort
measurements are unavailable. No additional process framework or private-state census.

This fulfills the language-services/LSP mandate. External project/editor or further
performance evaluation needs a subsequent user direction.

## Latest accepted checkpoint: dependency74

[Result74](../validation/results/language-services-dependencies-74.json),
[assessment](./progress/lsp-dependencies-74.md), [reproduction](./LANGUAGE-SERVICES-VERIFY.md)
and [independent evidence review](./progress/lsp-dependencies-74-review.json) are clear.
Managed420/420, retained Parser18799/18799, Native323whole originals+1adapted+62authored;
79changed source paths/179actual ILC inputs bound without drift. Committed at **e3908447**;
clean working tree confirmed. Freeze released; promote reviewed API/constants,
complete Server/CLI and candidate original-editor backend and continue autonomous
integration. No whole-server or service corpus acceptance claim.

## Prior accepted checkpoint: dependency55

[Result55](../validation/results/language-services-dependencies-55.json),
[assessment](./progress/lsp-dependencies-55.md),
[reproduction](./LANGUAGE-SERVICES-VERIFY.md) and
[independent evidence review](./progress/lsp-dependencies-55-review.json) are clear.
Managed400/400, native316 whole original TypeScript functions +1adapted +60authored;
43 changed source hashes/179 actual ILC inputs bound with no drift. Committed at **bfb0c0e1**, with clean working tree confirmed. Immediately continue the queued Registry/fixer/completion/
read-only-query/server families. Full services and stdio remain unfinished/unadvertised.

## Prior accepted checkpoint: dependency48

Upstream `fed0bf24149fb1ed36039212648bafdafc1ea10e` under `upstream/TypeScript/tsc`;
bundled Go1.27.1 under `tools/.cache/go/bin/go`. Emit/performance9b8fa74a, summarye003f22f.
Previous LSP dependencies: f655a303, f5feaa6a,3f41c7c4. **Dependency48 is
committed at0bd3b6c3 with managed/Native gates and independent review clear;
clean worktree was confirmed. Implementation freeze is released for next packets.**
Do not repeat accepted runs just because of re-entry.

[Result48](../validation/results/language-services-dependencies-48.json),
[assessment](./progress/lsp-dependencies-48.md),
[reproduction](./LANGUAGE-SERVICES-VERIFY.md) and final
[independent review](./progress/lsp-dependencies-48-review.json) bind current evidence.
Build48 clean21.50s; focused Session/package3passes; fullmanaged392/392,0failed/skipped,
69s, no embedded ORIGINAL_SKIP. Native48 exit0:312 whole TypeScript-repository originals,
1 separately adapted original runtime observer,56 authored controls plus retained
external/partial channels. Whole functions, rows, cases and adapted channels differ.
All115 Linux/shared fswatch originals now accounted:114 whole assertions and1 adapted
CLR thread observer;26 Darwin backend functions remain outside explicit Linux boundary.
Complete TestSession24topgroups and Close original assertions now pass. No whole-server claim.

Native ELF `artifacts/language-services/integration-4/native-48/LanguageServices.Validation`,
SHA256 `d706dc4f52a0476c7af3823585c3311664115454506073263df43397ba6ca401`.
source-48.json binds50 changed compiled paths versus3f41c7c4, no drift through publication
and execution; native-inputs-48.json binds179 actual ILC inputs and saved response file.
Fatal IPC child -6, required marker, no timeout, no core. Eight unchanged decoder-depth
probes retain Native23 evidence, not added to392. Program/Emit compatibility46 builds
passed; comprehensive retained compiler/emit execution remains final epic work.

Original failures remain in integration-4: watcher event equality/prefix fixture bugs,
Session source-content accessor bugs, source-review lease/capacity corrections, and
real background crashes from nil autoimport watch map and byte-owned ATA package names.
All are fixed with original expected behavior intact. Both owner forms now exercise
all original package-name expectations. Managed45 raced one capacity edit and remains
intermediate; final48 source binding is clean. Nine central builds40–48 reported136.29s
combined tool time, not elapsed/model effort. No available token/cost measurements.
After Session failures, focused original runs avoided repeating69s watcher gate after
each fix; final full managed+Native gate passed. Details and review qualifications in48.

## Next service integration after bfb0c0e1

Latest74: all integrated service source families compile clean7.42s. Full managed
420/420,0fail/skip,70s (eight retained decoder-depth probes included); retained
Parser18799/18799 pass2s (compatibility build3.85s/11existing observer warnings).
Actual Linux x64 Native74 publish and full execution pass exit0:323 whole original
TypeScript functions +1adapted runtime observation +62authored, separate external
TrimFunc1/SlicePredicate2 plus inherited channels. Exact original selection-depth
passes separately; fatal IPC child-6/exactmarker/no timeout. ELF95116c200af392092865ed4ee0effd4dc08a4b7b6776663d77aff26c22b1f9a2.
79changed source paths/179actual ILC inputs bound with no drift. Full Completion
independent source review CLEAR; other current owners reviewed. Committed e3908447 with clean tree; source freeze released for next integrated packet.
No active workload.
Result/assessment74 and source-map saved; Inventory evidence review CLEAR. Foundation
independently reviews whole ignored fourslash backend. Protocol finishes actual
Server/entrypoint/handler closure and direct originals ignored. After74 commit release
freeze and promote complete API3/constants2 +Server/backend reviewed packets, compile
promptly, resolve whole binding batches and proceed original editor corpus/actual
Native stdio. Public --lsp is still unfinished/unadvertised. Foundation next closes
remaining direct-original assertion gaps, not accepted infrastructure re-audit.
Do not stop at checkpoint/compaction. Whole epic still incomplete.

Latest72 failed11bindings14.98s (main8,String3), down291. Foundation/Protocol correct
disjoint batches for73; other current service owners now report no binding errors.
Full independent Completion review also corrected its earlier successful-set claim:
Go final NewSetWithSizeHint always allocates even empty; restore sized HashSet success
producer, retain actual nullable error result. Do not retain incorrect lazy-success
attribution or claim an allocation gain. Reviewer must inspect full producer and called
constructor before ownership findings. TestTrimFunc and SlicePredicate originals
promoted; Native dispatcher includes separate external1+2counts. Fourslash backend
coherent drafts now include all three actual nonwire joins, independent FS/process
transport and real session-state projection, still gated on actual Server promotion.
No active workload; freeze released only for current correction batches.

72 readiness: Foundation main/JSDoc batch and Protocol String batch are saved/stable;
Foundation narrow String/Utilities review clear. CodeActions annotation-only header
conversion uses direct edits! (no Unsafe.As). Actual connected Utilities SourceFile
token-accessor defects are corrected with borrowed TextView, original algorithm
unchanged. Remaining optional SlicePredicate external originals are ignored pending
Protocol readiness ACK; do not block body compilation for test wrapper work. Root
still must establish freeze with all owners before72. Original Go slices IndexFunc/
ContainsFunc pass0.002s package, log slice-predicate-original-go.log. No active workload.

Latest71: failed291 actual body/API bindings13.91s. Freeze released; split correction
by disjoint files to reduce critical path: Foundation Completions200+JSDoc4 and shared
completion model/helper signatures; Protocol StringCompletions83+SemanticTokens2,
coordinates signatures directly; Inventory remaining edit2 then resumes complete
candidate fourslash backend (do not stop at its first message draft). Protocol's full
independent Completion review continues; Foundation narrowly reviews Protocol string
binding deltas. Other read-only owner bodies now have no reported compile errors;
that is not behavioral verification. Root next compile72 after whole batch readiness.

Latest70: full Completions/string/JSDoc288workers promoted (three direct counterparts,
shared leaves rehomed) plus original directory-prefix assertion. Full69 corrections
and external Go TestTrimFunc9rows/27comparisons promoted; root wired original Native
Completion+1 and separate external TrimFunc+1.70 fails13 early completion declaration/
type/collision bindings2.04s, Foundation fixes entire batch; current source freeze
released for it. Protocol independent whole Completion source review ongoing;
remaining service families source-reviewed. Do not misclassify Materialize as UTF8
decoding: ByteStringValue.Materialize returns raw Go char-per-byte strings. Proposed
comparator ownership change is allocation preservation, not a demonstrated Unicode
correctness defect (reviewer withdrew that attribution). Pinned Go original completion
prefix test passes0.004s package; TestTrimFunc also passes, logs in integration-6.
Next root compiles71 when Foundation ready, then resolves concrete binding failures;
actual service/fourslash/server verification remains future acceptance, not source credit.

Latest69 compilation failed86 mechanical/current API bindings13.21s across complete
editing and read-only families; Inventory/Protocol correcting their disjoint owners
as a batch, no workloads by agents.68 failed2 pre-existing duplicate raw/scanner
helpers2.04s; those are removed in favor of actual accepted producers. Read-only84
source review CLEAR, including sole nil-map predicate wrapper and lazily cached CLR
visitor (language allocation adaptation, not a missing Go cached-closure algorithm).
Root next freezes after both owners ready and compiles70. Original TrimFunc27
comparisons are drafted for next shared xUnit/Native integration. Fourslash backend
draft temporarily paused for Inventory to fix its69 binding joins. Full Completions
continues in ignored drafts. No accepted new commit or whole service claim yet.

Next68 integration prepared: complete edit six-owner111worker packet promoted
with five actual shared helper joins; Foundation independent source review CLEAR in
lsp-edit-services-review.md. Complete read-only84workers SemanticTokens/InlayHints/
CallHierarchy/CodeLens +actual byte/codec/helper dependencies promoted; Inventory
reviews, resolving duplicate exact checker predicate wrapper to one owner before
compile. IPC original deadline correction saved and independently reviewed CLEAR.
Root must freeze source/manifests when packets stable, then compile68 promptly.
No behavioral credit for these new owners from source review alone. Inventory has
resumed original candidate-backed fourslash backend work in ignored drafts, coordinating
actual Server APIs with Protocol; no shadow Go Session. Foundation resumes full
Completions after its single-pass edit review. Native/full managed acceptance remains
pending for this new source; latest accepted commit remains bfb0c0e1.

Latest67: clean22.51s; focused Selection original12000+Registry5+Expando2 all8pass3s.
Actual Native67 publication succeeded; same exact original selection depth passed
standalone exit0. ELF66b8105b5a093f49a2973490239f8afe4bea629349c0bc36f1f46f8d7e4c64f9;
source-67.json binds39 changed compiled paths, native-inputs-67.json binds179 actual
ILC inputs, no drift. Native full dependency execution passed exit0:322 whole original
TypeScript functions +1adapted runtime observer +62authored controls; inherited
external/partial channels unchanged, no ORIGINAL_SKIP. Fullmanaged67 ran
416cases (including eight unchanged decoder-depth probes unlike prior400):415pass,
one failed TestAsyncConnRunWaitsForHandlers,71s. This is not accepted full gate.
Protocol traced that timeout to shared fixture Await imposing1s where the Go original
waits without a per-read deadline. Root approved precise fixture correction after
active Native gate:30s deadlock-safety default, explicit1s for actually timed upstream
waits; preserve original context1s/negative100ms and all assertions. No production
scheduler change. Inventory independently reviews this narrow provenance correction.
Native gate finished, freeze released for this precise fixture correction and next
complete editing owners rather than separate fixture-only
publication. Keep67 failed log intact, no substituted pass claim. Next ignored edit
packet includes all six CodeActions/ImportFixes/MissingTypeAnnotation/ClassImplements/
OrganizeImports/FileRename counterparts111workers plus required actual helper joins.
Foundation continues full Completions, Protocol remaining read-only owners+Server.

Current integration66: clean build1.83s, focused6/7pass including both complete
hierarchical/flat Go expando responses. RegistryLifecycle fails three groups because
the new retained-directory fixture input is not normalized; Inventory fixing that
actual host representation with expected behavior unchanged. Freeze released for
this correction and independently reviewed parser continuation packet promotion.
Build61 had24 production bindings12.27s;62 production clean/test raw-display join22.79s;
63 nullable annotation5.79s;64 missing Xunit import6.94s;65 root misplaced import1.12s.
These are integration/process failures, not semantic test evidence. Future test
wrappers must reuse actual adjacent imports to avoid repeated compiler handoffs.
Selection depth test must not run again until the production stack remedy is ready.
Pinned Go unchanged SelectionDepth/Implementation
Worklist originals pass (integration-6/selection-worklist-original-go.log).
Registry failure was fixture directory lifetime: ProjectTestFileSystem.Remove rebuilt
from leaves and lost empty ancestors, unlike Go MapFS.remove. Reviewed correction
retains existing ancestors, without changing production Registry or expectations.
Go authored expando full responses captured in document-symbols-alias-go.log; that
historical filename does not imply shared-header evidence. Actual Go newDocumentSymbol
always creates nonnull Children, making proposed alias branch unreachable from the
service producer. Root and both reviewers withdrew that suspected defect; unaccepted
cell/model/codec/generator changes removed. Ordinary nullable Slice representation
and meaningful full public-response comparisons retained (DocumentSymbolExpandoChecks).
Foundation implements a reviewed production low-stack continuation design: guarded
recursive parser hubs continue synchronously on a fresh CLR stack only when needed,
retaining exclusive parser state and exception/context semantics. Protocol reviews;
no test-only stack, changed depth or skip. Runtime remedy remains unverified work.

Root central builds56–60 under artifacts/language-services/integration-6.60 is clean
(7.76s); no gate currently running.56:three Locale alias errors3.33s;57:14 actual
owner/span bindings14.41s;58:RawJsonValue test alias18.78s;59:existing GoStrings.Map
owner qualification6.18s. No new dependency or expectations changed for those joins.

Concrete execution defects, ordinary work to resolve:
- Focused registry+selection60 ABORTED in actual Parser on unchanged original12000
  parenthesis nesting: CLR StackOverflow in expression/parenthesized recursion. No
  pass credit. Foundation pauses Completion briefly to investigate proportionate
  production runtime stack adaptation (Go growable stack versus CLR); explain technique
  before edits, preserve parser algorithm and ordinary performance plus thread-local/
  arena ownership, error/cancellation semantics. No test-only larger stack, lower depth,
  skips or fixture shortcuts. Protocol independently reviews remedy. No debugger/heap work.
- Separate Registry60:4/5functions pass,3s; single failed LifecycleDeletedPackageDirectory
  group expected1bucket got0 at Invalidation.cs53. All15 lifecycle siblings did execute.
  Inventory diagnoses real original fixture/environment versus algorithm difference,
  with expected behavior unchanged. Logs/TRX in integration-6. No broader reruns yet.

Protocol may now promote coherent Symbols/AutoInsert plus genuine raw helper/snippet
closure, rehoming prior Symbols.SharedLeaves without duplication. Subsequent query
families remain ignored. Next build waits these complete owners and concrete defect fixes.

Promoted coherent next packet:
- Full MissingMemberFixer18workers/514Go lines plus unchanged-body checker helper
  visibility. Foundation source review clear, including raw localized-text owner and
  actual autoimport producer. Manifest lsp-missingmemberfixer-promotion.json.
- All5 Registry original functions/all20 subtest bodies, actual Session/component mapper
  fixture closure. Protocol review clear;15 Lifecycle/4 Entrypoint siblings aggregate
  failures preserving Go t.Run continuation. Original expectations unchanged. Manifest
  lsp-autoimport-registry-tests-promotion.json. Existing ProjectSessionFixture only gains
  optional trailing injected ISpawner, preserving existing defaults.
- Full Folding/SelectionRanges/LinkedEditing37workers and two exact completion line
  helpers (rehome coordinated, no duplicates). Whole original SelectionRange depth12000/
  chain1000/range/actual response serialization assertion. Inventory source review clear,
  after lazy zero-set and actual span/owner corrections. Manifest lsp-syntax-query-promotion.json.
Root wired +5 Registry/+1 Selection Native originals. Candidate execution still pending;
source review and a clean production build alone are not service behavior acceptance.
Both focused60 executions are recorded above; no separate Native gate just for tests.
Combine meaningful connected service behavior before next broader Native checkpoint.
Foundation continues full Completions, Inventory broader edit/codeactions/filerename,
Protocol remaining read-only services and actual Server. All next bodies stay ignored
during central gates. No blocker requires user input; whole epic continues autonomously.

## Ownership and current integration

Three reused GPT-6.1 Sol lanes remain active. Root alone runs workloads; next bodies
stay outside compile globs while an integration gate runs.

- **emit_foundation:** complete Snapshot/API/refcount originals promoted and reviewed;
  independent References and six-worker Diagnostics reviews clear. Now porting complete
  Completions, string completions and JSDoc snippet closure (9,768 pinned Go lines),
  using genuine existing indexing/change/formatting/display/checker producers.
- **emit_inventory:** full References/ImportTracker/CrossProject/Rename/Highlights
  closure promoted: five counterparts,183 workers,5,228 Go lines. Source-reviewed
  and build54 clean; original Worklist assertions pass. Now five actual autoimport
  Registry originals, followed by complete edit-producing services (organize imports,
  file rename, registered code-action/fix-all families). Service response evidence
  remains pending; Worklist alone is not References acceptance.
- **emit_print_maps:** explicit request-context Emit and full six-worker LS Diagnostics
  promoted/reviewed; one actual Emit algorithm preserves checkout/lifetime through
  semantic/declaration collectors. Four authored real Session diagnostics→Emit→diagnostics
  controls are being promoted with exact pinned-Go observations and unchanged accepted
  diagnostic projector. Then continue genuine Server lifecycle/dispatch/context/transport
  and required client capability descriptor. No empty handlers or public advertisement.

SnapshotLifetime checks preserve accepted SnapshotFS classes/wrappers unchanged:
TestSnapshot all10 groups, TestProjectIDNarrowing, TestRefCountingCaches all11 groups.
The local genuine Session.API update/adoption workers are a required shared dependency;
embedding API transport remains excluded. Native dispatcher adds3 original functions.
Focused50 passed3/3; focused54 passed these3 plus original References Worklist (4/4,2s).
Full managed/Native acceptance of the new packet is still pending.

Build54 clean22.51s after prior integration corrections. Build49 production compiled,
16 test binding errors;50 clean7.31s.51 failed26 namespace ambiguities (3.26s);52 failed134
References binding errors (13.36s), mostly Go Kind.KindFoo versus C# Kind.Foo;53 left2
actual helper-owner names (14.40s).54 resolved those to existing PrinterUtilities and
NameResolver producers, without adding foundation algorithms. Source review preserved
Go fallthrough, borrowed PathKey and SourceFile content ownership. All logs retained
under artifacts/language-services/integration-5; intermediate builds are not acceptance.

Four authored Go controls passed in context-emit-go-2.log (0.211s package): genuine
bundled libraries, strict/declaration/skipLibCheck, valid/invalid source × noEmitOnError.
Complete LSP diagnostics before/after, full EmitResult diagnostics and exact JS/d.ts
outputs captured. Invalid/noEmitOnError produces7006+2322 and skips outputs; other
cases emit as upstream. These are reference expectations, not candidate pass credit.
The earlier two-row noLib capture is superseded for acceptance, retained as history.
Protocol may promote the coherent captured-control packet after focused54 release;
root next builds55, diagnoses controls, builds/runs applicable retained Emit controls,
then combines full managed and Linux x64 Native acceptance before a new checkpoint.
No separate Native publication per focused original test. Program/Emit compatibility
also required because five existing default-token call sites gained explicit types.

Process correction from51/52: compile promptly after complete connected bodies and
syntax/target-collision checks. Prolonged manual API binding preflight duplicated the
compiler poorly. Independent review focuses on algorithm/ownership/omission/assertion
fidelity. Still no stubs, per-small-change validation loop, overlapping edits or
agent-run workloads. No current blocker requires a user decision.

Build55 clean22.78s. Context controls4/4 and retained Emit mode/mutex/write-failure10/10
pass; Program+Emit compatibility build clean3.92s. Full managed400/400,0fail/skip,70s.
Actual Linux x64 Native55 published and executed successfully:316 whole original TS
functions,1adapted original runtime observation,60authored controls; retained external/
partial channels unchanged. Fatal child-6 with required marker/no timeout/no core.
ELF SHA c0f4f939bb1371aabdb3de7fe096e8f622862cb15b3ca6cd9ac301d1919b43b1;
179 actual ILC inputs and43 changed compiled source paths bound, no drift.
Result validation/results/language-services-dependencies-55.json, assessment/reproduction
and mapping/test inventory updated. Independent final evidence/claim review is clear; committed at bfb0c0e1, clean tree confirmed. Source freeze released for next coherent packets. Do not repeat passed gates or count service behavior as verified merely
because it compiled. Whole server/epic remains incomplete and work continues directly.
Registry five whole original functions/all20 subtest bodies assembled; independent review found sibling failure aggregation required to preserve Go t.Run behavior. Fix then promote stable packet. MissingMemberFixer is Inventory's next full owner,
a concrete dependency of Foundation's genuine class-member completion snippets.
Protocol ports full read-only query families before their genuine Server consumers.

## Remaining epic acceptance and reusable references

Session/Snapshot/project-autoimport production counterparts are source-reviewed;
original Snapshot/API/refcount assertions now pass focused managed checks; Registry and combined Native evidence remain pending as above. Ten semantic
counterparts/268workers compile and are source-reviewed (utilities/source maps/display/
JSDoc/hover/icon/signature/formatting/definition/source-definition). Only direct service
formatting originals have behavioral evidence so far. Actual service/fourslash results,
other service consumers and real public Server transport remain unfinished/unadvertised.
Source-reviewed bodies do not imply verified editor capabilities.

[Verification design](./progress/lsp-verification-design.md) selects original Go
fourslash bodies/baselines with actual candidate framed stdio and independent validation
host control; no shadow Go Session. Genuine C# snapshots/FS/same Program and SourceFile
identity must provide existing non-wire state assertions. This is adapted harness
evidence, not thousands of original bodies ported to xUnit. Backend still requires
implementation/review. Public disk launch, changing documents/config, cancellation,
stale results/refresh, repeated-edit resources and retained compiler/emit/performance
regressions remain required. No external project evaluation.

Reusable original Go logs under integration-2: project99functions/250subtests,
watcher LSP16/2 and fswatch115/233, autoimport12/36, contentmapper31/30, diagnostics4/28,
IPC13/3, locale public6/275+internal6/200. Reference passes alone grant no C# credit.
Integration-3 format4/8, whole TestReplacer(reference only; candidate49selectedrows),
authored MarshalIndent1/10 remain unchanged. Go json/v2 randomly chooses cannot/unable
once per process; canonical C# cannot qualified with exact type/path/cause/partialbytes.
Earlier accepted39/33/23 proofs retain their original source bindings; don't rewrite.
The updated test inventory reports312whole passes,1adapted original,4partial channels,
4662unexecuted,26Darwin exclusions,1external generator; six TestMain lifecycle entries
are separate. Missing implementation and unexercised behavior remain distinct.

## Historical dependency verification (build11 and later managed work)

The following records preserve earlier intermediate evidence. Statements of pending
work in this historical section are superseded by current build23 evidence above.

[Source-bound execution record](../validation/results/language-services-dependencies-initial.json)
records178 changed source/check/tool hashes, native executable hash and limitations.
Build11 is clean (11.80s); managed **73/73 cases**,0 skipped,366ms test duration.
Actual Linux x64 Native AOT publication and ELF execution passed:
- 51 original TypeScript dependency test functions;
- one separate original x/text NFD function with20 original rows;
- four partial AstNav Go JSON baseline channels and three original token subtests;
- 26 authored controls, including eight public NFD observations and four signed
  coordinate cases. These units differ from xUnit cases and assertion counts.

Raw evidence: artifacts/language-services/integration-1, including native-publish.log,
native-run.log, tests/dependencies-3.trx. Native SHA256:
`bc5a6a6d22e7be430cab1da44e07bff5e64b459f82cc8885fc6b206412ef8446`.
Pinned Go additionally admits the authored protocol-depth witness: model chains
16/256/2048/4998 and default-any arrays16/256/2048/10000,0.041s; see integration-1
protocol-depth-go.log/overlay. C# depth behavior is still unverified.
Pinned Go also passed all31 original protocol Test functions and122 subtests
(`protocol-original-go.jsonl`), with no failures/skips. Seven authored JSON boundary
observations passed (`protocol-json-go.log`): invalid UTF8 admission/replacement,
duplicate keys including escaped spellings, retained raw payload ownership, request
reuse with absent params, and typed-nil result serialization. These are reference
observations; the closed C# codecs and corresponding Native boundaries are pending.
Complete Go package reference runs also passed44 regexp tests plus31 examples,
12 syntax tests, and26 norm tests plus2 examples. Three norm originals skipped
to avoid downloading Unicode data;23,622 norm subtests passed. Raw JSONL is in
integration-1/regexp-original-go.jsonl and nfd-original-go.jsonl. Examples are not
Test-function inventory entries. C# selected assertion channels remain a separate
claim; the exact official Unicode17 fixture is embedded for its NFD channels.

No full service/server transport, remaining original corpus, comprehensive retained
compiler/emit gates or final completion review has run for this epic.
New BFS source/test was added after this frozen result and is not covered by it.
Native runner now wires52 TypeScript functions (BFS pending),1 external NFD,4+3
partial channels/subtests and27 authored controls (new graph diagnostic change
control pending).

Observed corrections are retained rather than concealed: first70-case run had63 passes;
preference generation omitted two upstream negations (production defect, fixed and
reviewed); AstNav harness used CLR enum names instead of Go token names (harness
fix, expected baselines unchanged); NFD authored count was stale6 vs8. Next73-case
run had71 passes: ATA log projections were missing (complete actual consumer-family
fix, then all73 passed). Earlier NFD authored full-string expectation omitted the31st
mark after CGJ; actual pinned Go established the correction. Original20 rows unchanged.
Core.TextPos is int32: converter unsigned/widened arithmetic differed from upstream;
signed casts/overflow restored and four actual Go/managed/Native observations passed.

## Review, inventory and next gates

Independent source reviews are scoped and hash-bound, not blanket acceptance:
[cache/dirty](./progress/lsp-project-owner-review.md),
[queries/AstNav](./progress/lsp-checker-query-review.md),
[preferences](./progress/lsp-preferences-review.md),
[NFD](./progress/lsp-nfd-review.md),
[selected regexp](./progress/lsp-regexp-review.md). The latter covers all selected
parser/engine workers and219 newly generated Unicode range owners; Native and
complete applicable assertions remain separate gates. Preference review records its initial
miss and the corrected negation re-review. Configured hover, complete protocol codecs,
new logging/VFS/project owners and broader completion still require independent review.

[TypeScript test inventory](./language-services-upstream-tests.json): initial4,442files/
4,820 Test entries plus one newly required core BFS file/function.51 original functions
have managed/Native assertion passes; four AstNav functions retain partial passing Go
channels with original JavaScript channels pending. New BFS is unexecuted. TestMain
lifecycle, empty original logging bodies and scheduling adaptations stay explicit.
[External dependency tests](./progress/lsp-standard-library-tests.json):30 norm,
12 regexp/syntax and44 regexp Test entries; selected/mixed/external dispositions
are still being reconciled. Do not equate written tests or totals with completeness.

[Verification design](./progress/lsp-verification-design.md) accounts for real fixture
FS/config/state/non-wire joins. Never use a shadow Go Session to satisfy C# assertions.
Assess direct original harness port versus capture once transport is coherent; no new
framework now. Actual Native public transport, edits/invalidation/cancellation, complete
applicable assertions, independent review and retained regressions remain mandatory.

## Operating lessons and fixed adaptations

- Preserve Go UTF8/Slice ownership, actual copies and accepted performance adaptations.
  Direct foreach over Slice<T>, no Values/ToList bridge. C# tuple Rest is reserved;
  use Remainder. Resolve real namespaces/overload order across caller and callee.
- Existing immutable snapshots, speculative acquire rollback and genuine pool/Program
  reuse remain required. Go package-shared mutable owners must not become readonly
  unless their complete consumer behavior justifies it.
- NFD uses x/text v0.42.0/Unicode17; regex selected closure uses Go1.27.1. Exact source
  pins, BSD notices and mappings are required. No approximate Regex/FormD fallback.
  Nonselected replacement/find-all and normalization forms are explicitly external.
- Source reviews follow a coherent compile where practical. Build shared Checks before
  wrappers when using BuildProjectReferences=false; saved library permits diagnostic
  tests while agents continue source. Freeze source and metadata for Native publication;
  refresh actual dependency references, and do not commit during publication.
  Wait for explicit owner freeze ACK before compiling: crossing promotions caused
  avoidable checks/API diagnostics while an earlier DLL was intentionally reused.
- Twenty-two central production diagnostic builds started so far; build16 is fully clean
  (production/Checks/xUnit,11.50s).
  Shared checks/wrapper then built in1.86s/1.65s using saved production DLLs.
  Managed run4 passed82/83 (0skip,691ms), with TestMinInputLen `(aa){3,5}` failing;
  independently traced to importer reading5 inside quantifier instead of original6.
  Generator fixed to read the numeric field after the literal; production unchanged.
  New BFS and graph diagnostic-change controls passed. After corrected fixtures and
  eight additional syntax functions, managed regex run1 passed16/16 groups (0skip,
  720ms):15complete original functions +one selected TestMatch channel. Remaining
  external-test dispositions and Native behavior are pending. Raw logs/TRX retained.
  Build17 stopped at two formatter alias/accessibility errors (2.04s); those were
  corrected. Build18 reached the complete new family and reported49 errors (12.25s),
  assigned by owner. No assertions have run against the new combined family yet.
  Build19 stopped on two redundant Debug.Assert methods (3.29s): accepted overloads
  already existed in other partial declarations. Redundant additions removed;
  original assertions unchanged. Use repository-wide symbol searches across partial
  owners before declaring an API missing. Build20 reached two still-unfixed generated
  value-union allocation sites (12.49s); generator read/write paths must both be
  checked in final generated output before claiming their fix.
  Production21 passed (15.48s including subsequent Checks alias failure). Checks21
  exposed absent generated nested equality workers; generator now closes over1248
  shapes for749 public roots and verifies every helper reference. Checks21b exposed
  reused C# pattern local names; all649 RHS names are now unique. Checks21c passed
  in5.35s; wrapper21 passed in1.67s. Original field-by-field comparisons preserved.
  Known review defects surfaced before execution, so their batch was fixed before
  running the combined assertions rather than knowingly rerunning bad inputs.
- Declarations/body/API joins were
  corrected without stubs, source exclusions or warning suppression. Whole gates are
  deferred to coherent completion; current focused reruns addressed actual failures.
- Two Sol lane turns ended with temporary selected-model capacity errors; restarted
  the same GPT-6.1 Sol lanes from durable work, with no model substitution/re-audit.
- The next external-dependency test packet includes applicable RE2/Fowler matching
  and NFD conformance channels. Preserve exact original data in compressed resources;
  matching-only channels do not establish capture-result APIs or whole original
  functions. Root will build once the coherent packet is ready, then overlap its
  execution with independent review of the new project graph by the services lane.
- No debugger/heap-capture work, SVG flamegraphs, speculative process framework or
  unrelated cleanup. Keep source/test/implementation gaps explicit and continue work.

# Emit performance verified; final review and delivery (2026-10-12)

Implementation is frozen; all seven correctness gates passed. Actual Linux x64 Native
AOT best10.18s/Go9.37s=1.086446 in five matched alternating full VSCode emit runs.
Every run matched10,031 paths/150,466,543bytes and all ten comparable diagnostics.
Scope: same pinned Go and VSCode main src project, AVX2, eight checkers, standard
ThreadPoolMinThreads128 publication setting and explicit NoGC16/2GiB with20GiB
available-memory preflight. Portable project defaults unchanged; no universal timing claim.

Passed2226 managed tests; Native152original+254authored controls;15330emit configurations
retain15285exact public matches,39preparation skips and the same6external incremental
metadata differences, with13571original assertion sets. All15304retained noemit records
match exactly; CLI6+guards3+runtime3 passed. Acceptance CLI ELF/ILC response identical
to measured integration6. Independent source, gate and measurement reviews are clear.

Noemit protection completed five each with exact complete diagnostics/current Compile
fields and all14/2GiB regions: Native8.55,10.37,9.48,10.64,9.70; Go7.13,7.96,8.12,8.55,8.55.
Best1.199158 is nominally within20%, but0.01s timer rounding can cross1.20. This is not
a robust margin or an unchanged historical6.95s promise. Original failed harness FIRST
is retained: obsolete Check API status1/assumed EmitSkippedtrue corrected ONLY to pinned
Compile status2/EmitSkippedfalse/emptyListing; Diagnostics/Error exact, no compiler edit
or rerun. Both source traces and offline qualification independently reviewed.

Delivery evidence is finalized at validation/results/vscode-emit-performance.json;
aggregate review at validation/results/vscode-emit-performance-review.json must be
clear before committing. Assessment/reproduction: docs/VSCODE-CHECK-STATUS.md.
Raw evidence and seven phase markers: artifacts/emit-performance/acceptance.
No workload is running and no more builds/profiles/benchmarks are needed absent a
concrete review finding. If the verified files remain uncommitted, finish the review,
commit this coherent epic and verify clean working trees. The commit containing the
result is the delivery checkpoint; no self-referential commit hash is needed here.
Do not restart accepted implementation or verification after compaction. Once clean,
this completes the authorized emit performance target in its documented scope;
further work needs a new user mandate. Root owns delivery and the Astra coordination;
GPT-6.1 Sol lanes supplied implementation, source review and independent acceptance.

# Implementation and measurement history: Go-faithful emit performance (2026-10-12)

Authorized from `edc53e02` exact VS Code emit comparison. Target is Linux x64 Native
AOT full main-project compile/check/emit wall time <=1.20x pinned Go, using best of
five fresh processes each, alternating order, equivalent genuine source/options/libs.
Every qualified run must retain identical 10,031 output files/raw bytes and all ten
complete comparable diagnostics. Report every sample, variability and memory, even
though best-to-best is primary. Source maps/declarations are off in this real workload;
retain comprehensive applicable emit/xUnit/no-emit correctness gates before acceptance.
Do not silently equate this workload target with all emit modes or all platforms.

Reuse prior scoped AVX2/eight-checker/explicit NoGC policy as an eligible measured
configuration, clearly separate from portable defaults. Checkers=8 applies to both;
NoGC lifecycle/budget failures remain visible. Ordinary latest observations were
25.38 Native /12.82 Go; they are not an equivalent tuned baseline. No debugger/heap
capture, SVG flamegraphs, unrelated redesign or broad foundation reassessment.

Priority: coherent owner-to-consumer allocation completion grounded in pinned Go,
not isolated tweaks followed by repeated tests/benchmarks. Preserve algorithms, control
flow, function boundaries, required copies and identity. Apply UTF8 borrowing/byte
comparison, Slice reuse, static stateful callbacks, embedded records and genuine
upstream pooling where needed. Document departures and independently review changes.
Root owns centralized builds/benchmarks and decisions; no agent workload runs.

Current integration: first coherent allocation packet is source-reviewed and Native
published. Integration1 exact VS Code outputs/diagnostics passed, allocations fell
19.9156GB→14.7550GB (~26%), but14/2GiB NoGC region failed (GC9/3/2), Native32.21s/Go8.59s.
Existing baseline trace partition decode shows93.11% sampled SOH/6.89%LOH; old
14/2 budget reserves only12GiB SOH with heap balance constraints. No Native per-heap
failure cause is claimed from the managed trace. A reviewed authored runtime update
uses16/2GiB for opt-in configuration emit, preserving explicit --noEmit/check-only14/2,
20GiB MemAvailable preflight/fallback/defaultGC. Config-declaredNoEmit conservatively
uses16 because region precedes configuration parse. Benchmark expectation change is
explicit --no-gc-budget-gib16, oldfailedruns preserved.

Integration2 CLI-only publish consumed unchanged frozen integration1 Scanner IL.
First run matched all outputs/diagnostics; the completed alternating five-run cohort
also matched every run and every 16/2GiB NoGC region ended successfully. Best Native
12.96s / Go8.41s = 1.541 (target NOT met); Native range12.96–17.04, Go8.41–8.95.
Native allocation14.7654GB. Best-wall CPU Native71.05s / Go74.95s suggests examining
utilization/waiting rather than assuming more compute. Full results: integration2/cohort.

Actual Native perf and matched Go pprof captures completed under integration2/native-cpu;
both retain exact outputs and all ten diagnostics. Go appends two profile-path status
lines, separately verified and recorded after initial raw-text assertion failure.
Native 6,202 samples/lost0 show busy checking followed by less utilization late in emit;
worker startup was prompt. All-thread caller unwind mostly empty; root is recovering
per-TID stacks offline (no new capture). CPU-only samples do not prove off-CPU causes.
Go alloc profile is delayed by GC publication, not an exact total allocation census.
No allocation ratio can be inferred from it. Required checker locking and VFS write
semaphores currently match Go; do not redesign them on speculation. Sol maps owns a
concrete narrow VFS correction: decode directory only on first-open failure, as Go does.
Foundation owns trace attribution; no workload runs or builds by agents. Continue to
coherent source-grounded improvements, integrated Native VS Code qualification and
finally comprehensive acceptance. No final-gate run or acceptance claim yet.

Integration3: recovered per-TID stacks conserve 2,269 samples; selected late waits
resolve checker GetConstantValue/GetReferencedValueDeclaration. They do not quantify
blocked wall time. Keep SemaphoreSlim: checker resolver and transferable public leases
share the same mutex; thread-affine Lock/Monitor would narrow upstream ownership.
Controlled runtime-only experiment publishes standard ThreadPoolMinThreads=128,
verified actual ILC --runtimeknob (no compiler scheduling/lock-scope changes). Fresh
build11.99s/publish12.66s succeeded, actual Scanner reference and binary hashes bound in
integration3/binary-binding.json. Includes reviewed failure-only VFS directory decode.
First and five-each cohort completed: all exact outputs/diagnostics/full regions,
best13.55Native/8.69Go=1.559. No improvement demonstrated; min128 remains experiment,
NOT chosen production default. No workload currently running. Resolver recursive callback
owner/static-state predicate correction is now independently reviewed/frozen (EmitResolver.cs).
Inventory checks remaining profile-grounded emit callback families; foundation prepares
proportionate off-CPU/wall attribution because CPU-only profiles cannot explain waiting.
No bespoke mutex, thread-affine lock substitution or compiler scheduling redesign authorized.
Next integrated publication should omit experimental min128 unless new evidence warrants it.

Integration4 diagnostic NativeAOT published12.91s after freshbuild11.58s, EventSourceSupport
true, AVX2, no minworker override; actual ILC/binary/source binding retained. Callback
claim corrected: old recursive lambda already reused its delegate per-node. New resolver
owner cache saves only per-invocation setup (no asserted measurable speedup). Remaining
callback audit found no further profile-grounded per-node omission; stop speculative cleanup.
Native wait-duration capture completed exit2 under integration4/native-waits: actual
10031bytes/10fulltexts match, trace11.96MB/lost0. Instrumented16.416s is NOT benchmark.
Initial launch lacked DOTNET_ROOT; retained launch-failure then restored installed root.
Initial diagnostic renderer used wrong cwd; qualification.json records offline correction
to actual repository cwd (unchanged captured DTO/expectations, no rerun). No debugger/heap/SVG.
Existing analyzer compiled/decoded and added focused wait object IDs from sameETLX;
104202MonitorWait intervals,122.984thread-seconds. Eight identities own99.60% of that
wait time, beginning~7.5s and ending~15.1s. Prior nativeCPU wait callers resolve checker
GetConstantValue. NativeEventPipe stacks absent, so identity naming remains an inference.
Startup GC suspend only19.936ms; late mean waitingthreads11–21 across multiple seconds.
GC keyword also producedallocationticks, explicitly accounted, no heapdump.

Current implementation decision: root authorized a small standard Linux POSIX unnamed
semaphore adapter for checker mutex only after two Sol feasibility assessments. NativeAOT
UnixWaitSubsystem has a process-wide signal-state lock; libc semaphores use per-object
atomic/futex state. This concrete runtime difference justifies measurement, not a promised
gain. Preserve all compiler algorithms, lock boundaries, shared checker/pool/resolver identity,
nonreentrancy and transferable Done. Linuxx64/glibc32-byte ABI only, managed SemaphoreSlim
fallback elsewhere. No custom waitqueue/scheduler or copied Go-runtime mutex state machine.
Foundation owns runtime support +Checker.mu/EmitResolver/CheckerPool joins +shared authored
mutual exclusion/cross-thread release/overrelease/fallback controls; inventory independent
review. Use finalizer-owned stable native storage/no publicDispose, KeepAlive through calls,
EINTR retries, loud native failure, held guard clear before post. This preserves ownership;
Go goroutine parking/fairness are not reproduced or claimed. No agent builds/workloads.
After coherent source review, root builds, runs focused synchronization correctness controls
because native lifetime/concurrency changed, then actualNativeVSCode before broader gates.
Integration5 coherent POSIX packet source review clear (checker-mutex-independent-review.json).
Three shared authored xUnit records passed (native/default plus forced managed fallback),
14.25s build/test; productionNativeAOT publish12.93s passed. ActualILC/binary/source hashes
bound, no experimental minworker override, EventSourceSupport=false. Full VSCode firstpair
completed exact10031outputs/tenfulltexts/fullNoGC: Native15.53s/Go9.46s; nottarget.
Five-each alternating cohort completed: Native15.68,16.67,12.95,11.61,12.37;
Go9.57,11.41,9.07,8.80,8.66. Every output/diagnostic/full region passed.
Best11.61Native/8.66Go=1.34065, target NOT met. Fresh Native Emit.Validation
also passed152original+254authored controls, including both mutex backends;
logs under integration5/native-mutex-controls. Managed total now496emit +1730retained.
Prepared finalgate count delta preserves all original assertions/dispositions.
Next controlled runtime interaction: same frozen POSIX source with standard minworkers128.
Earlier min128 was with the old CLR global wait subsystem, so its negative result does
not establish this combination. Root centrally publishes/qualifies; agents do no builds.
Integration6 same-source POSIX+standard minworkers128 publication passed (11.66s).
Five fresh interleaved runs qualified: best10.18Native/9.37Go=1.086446, all outputs,
full diagnostics and all five regions passed. Native median10.39 range10.18–14.76;
Go median10.06 range9.37–12.00. Target observed in this cohort, not final acceptance.
Now run retained correctness gates and no-emit performance protection. Root changes
only finalgate CLI publication flags to measured AVX2/min128/EventSourcefalse; validator
publication unchanged. Portable csproj defaults unchanged. Foundation prepares accepted
no-emit controller rebind; inventory independently reviews evidence. No new compiler edits.
Acceptance build, all2226 managed tests, six actual Native publications and direct
Native controls passed. Full15330configuration emit corpus preserved15285exact public
matches/39preparation skips/same6external incremental metadata differences, with all
13571original assertions retained. CLI6+guards3+runtime3 passed. Root running15304
noemit corpus (25856); no parallel workload/builds. Acceptance CLI ELF and actual ILC
response are byte-identical to measured integration6 candidate (foundation binding).
All seven correctness phases passed, including all15304 noemit records exact.
Noemit VSCode protection first pair completed Go7.42/Native9.22 with full14/2 region
and unchanged exact Diagnostics/Error. Harness initially failed on obsolete check-only
API fields (historical ExitCode1, assumed EmitSkippedtrue). Two Sol source traces confirm
pinned genuine Compile noEmit nil-target/no-buildinfo returns empty EmitResult, so
EmitSkippedfalse and diagnostic ExitCode2 are correct (Goalso2). Foundation narrowly
corrects those API expectations and qualifies captured pair offline, preserving original
failed row; no compiler edit/rerun. Inventory delta review passed; five-cohort now running76768, same integration6 binary. Source remains
frozen; only docs/artifact summaries may change during gates. Final acceptance pending.

Independent source reviews clear: scanner-printer, transform-factory including
FileLoader null→default compile join, optional emit NoGC policy+fallbackcontrols and
benchmark expectations. Artifacts under artifacts/emit-performance. Full retained
correctness gate prepared (NOT RUN) artifacts/emit-performance/acceptance/run.py phases
build managed publish native corpus command noemit; reuse references/unchanged expected
counts and six external build-info metadata differences. Root executes after useful
performance target, not now. Continue autonomously; no acceptance claim yet.

Current GPT-6.1 Sol lanes:
- emit_inventory: Transformers and AST factory/synthesis ownership/allocation paths.
- emit_print_maps: Printer, text writers, source maps and output encoding/writing.
- emit_foundation: reusable measurement controller and runtime/parallelism comparison;
  independent review after implementation ownership freezes.

Immutable baseline AVX2 publish succeeded from `git archive edc53e02` under
`artifacts/emit-performance/baseline/source`, output native-avx2 (23.12s publication).
First matched pair completed: Go10.19s, Native30.72s; exact outputs/diagnostics.
Native allocated19.9156GB and fell out of14GiB NoGC region, collections25/11/2;
qualification exited1 as intended. Evidence baseline/first/comparison.json. No five-run
cohort until integrated work qualifies. Baseline managed allocation capture and joint decode
completed under baseline/managed-profile:20.0755GB sampled,0lost,not truncated,
176500typed resolved allocation ticks. Top4.004GB iterator type is exactly scanner
comment worker now being converted across printer/declaration consumers. Profile
outputs match all10031 files. Native counter19.9156GB is separate exact runtime evidence.
Current additions: embedded visitor/factory hooks+cached context callbacks, stateful
SameMap including generator source, scanner value comment traversal, byte-span line
and comment fast-skip, direct indentation fill. Further profile-justified AST parent
setter and five-predicate ancestor caller avoid transient closure/array allocations.
No claims of measured candidate gain yet; integration build after coherent packet.
 Sol foundation is preparing one baseline
managed allocation trace using existing tools to attribute full-emit extra allocations.
This isolates baseline from active implementation edits. Benchmark artifacts reuse
bounded output directories; retain hashes/results rather than five giant output copies.
Config wrappers MUST reside beside VS Code src/tsconfig.json to preserve default
source/type roots. Source config and existing VS Code output must remain unchanged.

Completion includes reviewed implementation, full matched Native target cohort,
unchanged output/diagnostics, applicable retained regressions, source mapping and
reproducible evidence, committed checkpoint and clean working tree. Continue beyond
internal milestones; do not stop merely after initial measurements or a plan.

# Completed: VS Code emit comparison (2026-10-12)

The user authorized comparing real VS Code emit after accepted checkpoint `7542f694`.
Completed the main `src/tsconfig.json` project at VS Code `4038aefd2c4aa51457420ef764c4c2358ae0ddfa`:
10,030 JS + one JSON file, 150,466,543 bytes per compiler; all relative paths and raw bytes
match. Both exited 2 with ten identical complete pretty-false diagnostics (including
chains/locations/category/code). Byte-end/related-information DTO fields are not exposed
by Go's nonpretty CLI and are not claimed by this text comparison. Source maps and
declarations were not requested; this is not a full product build/runtime test.

Ordinary accepted Linux x64 Native AOT: 25.384587s; pinned Go: 12.815099s. These are single
observations with ordinary defaults, not the prior tuned no-emit performance cohort.
Real config skipLibCheck=true retained. Temporary wrappers beside the real config
change only outDir. An initial relocated-wrapper attempt changed default roots and
was excluded; its 10,030 generated source-adjacent JS files were removed via a recorded
untracked/stem/timestamp manifest. VS Code working tree is clean.

Evidence: `artifacts/vscode-emit-comparison/2026-10-12-corrected`, including runs,
provenance, all file hashes and full diagnostics. Reproduce with
`python3 tools/compare-vscode-emit.py artifacts/vscode-emit-comparison/fresh-run`.
Sol inventory/config/cleanup/documentation and Sol comparator with independent review;
root independently confirmed every output byte and full rendered diagnostics. See
[VSCODE-CHECK-STATUS.md](VSCODE-CHECK-STATUS.md). No new implementation campaign is active.

# Accepted: whole emit contract (2026-10-12)

**No active campaign.** The checkpoint containing
[emit-verified.json](../validation/results/emit-verified.json) completes the
[whole-emit contract](EMIT-CONTRACT.md). Implementation, source mapping, independent
reviews, genuine pinned-Go comparisons and actual Linux x64 Native AOT acceptance
are recorded in [EMIT-ASSESSMENT.md](EMIT-ASSESSMENT.md),
[EMIT-TEST-INVENTORY.md](EMIT-TEST-INVENTORY.md) and
[EMIT-VERIFY.md](EMIT-VERIFY.md). Upstream remains
`fed0bf24149fb1ed36039212648bafdafc1ea10e`.

The final candidate includes the general WorkGroup scheduling repair: waiting callers
can execute queued work, avoiding CLR thread starvation during nested lazy checker
construction. The unchanged original 200-file emit workload reproduced the defect
and verified its repair. Required ownership/allocation adaptations remain in place.

Final evidence under `artifacts/emit/final-acceptance-workgroup-fix`:

- 2,223 managed xUnit records passed; six actual Native AOT publications verified.
- Native direct emit: 152 original functions and 251 authored controls passed.
- All 15,330 original configurations accounted for: 15,285 exact public matches,
  13,571 matching original assertion sets, 39 preparation skips, 1,720 original
  option-policy records and six visible incremental metadata differences. Counting
  categories overlap as documented; the six blocked complete comparisons are not passes.
- Disk comparisons 6, incremental boundary guards 3 and actual Node runtime comparisons
  3 passed. Retained Foundation 3,153 + 87 controls, utilities 20 and Program 9 passed.
- All 15,304 complete retained no-emit records match the original reference.
- Five original emit workloads, five fresh-process runs per runtime, completed.
  Best Native/Go ratios are 1.40–2.25×; this is an initial emitter observation, not a
  before/after speedup or a new VS Code performance result.

Incremental snapshot/build-info/build/watch ownership and language services remain
external. The CLI rejects incremental/composite execution rather than inventing build
information. Pinned upstream TODOs, original test dispositions and Linux-only platform
evidence remain explicit. No external project was evaluated during emit acceptance.

The latest user mandate ends at emit acceptance. Wait for authorization before a new
external-project evaluation or campaign. The performance summary was replaced with the
requested conversational account and its earliest sampled cumulative allocation figure
corrected to 52.02 GB in earlier dedicated commits.

## Historical emit execution notes

The material below preserves implementation and failed-attempt history. Its live session
IDs, pending labels and active-work instructions are superseded by the accepted status
above; do not restart those sessions or rerun accepted work on re-entry.

# Active epic: complete upstream emit (2026-10-11)

The user authorized autonomous whole-emit completion after performance `e3131a5b`.
Follow [EMIT-CONTRACT.md](EMIT-CONTRACT.md), [EMIT-PROMPT.md](EMIT-PROMPT.md),
[EMIT-IMPLEMENTATION.md](EMIT-IMPLEMENTATION.md), [EMIT-TEST-INVENTORY.md](EMIT-TEST-INVENTORY.md)
and [EMIT-VERIFY.md](EMIT-VERIFY.md). Complete coherent upstream dependency subsystems,
then whole compiler emission. Do not stop at milestones/commits/compaction or seek
renewed authorization. Astra coordinates; existing GPT-6.1 Sol agents implement/review.
No external project or VS Code evaluation until whole-emit acceptance. Preserve
accepted allocation/runtime adaptations. Language services and unrelated build/watch
are excluded. Upstream remains fed0bf24149fb1ed36039212648bafdafc1ea10e.

## Accepted checkpoints

- `3e78aede`: synthesis, transform base and source maps.
- `34ade775`: full printer, reference/emit resolvers, non-hover declaration serialization,
  shared transform dependencies. 1,186 managed passes; actual Linux x64 Native AOT.
- `b0cf6af4`: seven TypeScript transform files/142 functions, MultiMap11. 1,222 managed;
  Native150 original functions+87 authored controls. Full diagnostic projector retained.
- `13d29ca3`: complete17-file ES family/407 functions, OrderedSet8. 1,304 managed;
  Native151 originals+168 controls. NamedEvaluation20 count corrected, no missing body.
- **`e8b11bcf` latest accepted emit checkpoint**: JSX48, ConstEnum3, CommonJS83,
  shared modules41 (seven files/175 functions); genuine Program HasFileName format
  join, OrderedMap entry enumerator. 1,379 managed passes; Native publish15.785s,
  run6.574s,151 original functions+243 authored controls. Evidence
  validation/results/emit-script-transforms-verified.json and independent ACK under
  artifacts/emit/script-transforms.75 new exact-Go full-chain/public-diagnostic cases.
  Two build attempts, three Emit test attempts: harness defaults corrected; actual
  borrowed-string const-enum consumer defect fixed with unchanged references.

These are dependency checkpoints, not whole emit acceptance. Original module utility
nil/empty TODOs and declaration JSDoc TODOs remain explicitly pinned upstream limitations.
Three hover-only NodeBuilder methods remain outside scope. No blanket private-state parity.

## Current implementation and ownership

**Complete declaration and genuine Program output implementation is source-written,
uncommitted, undergoing integrated verification.** All181 declaration functions
(Transform110/Util15/Diagnostics32/Tracker21/Supplemental3), actual contracts,
EmitHost26/Emitter23, Program emit/declaration diagnostics/noEmit modes, demanded
HasFileName joins, pooled writers/parallel input-ordered result collection, real filesystem
writes, and CLI/Project compile orchestration are present. No permissive stubs.

Existing Sol lanes:
- emit_foundation: declaration Transform/Util and narrow borrowed scanner/resolver/Core
  joins; independently reviewed support, compiler and CLI. Authored actual write-failure/
  raw-byte controls have exact pinned-Go witnesses; current work is pre/post diagnostic
  loss, then independent whole-output assertion review.
- emit_print_maps: declaration support/contracts; independently reviewed transform and VFS.
  Owns command integration/disk controls, original portable logger test, and original
  source-map baseline/record/recorder assertion ports. These recorder sources are landed,
  source-reviewed; consumer wiring is inventory-owned. Runtime fixture controls follow.
- emit_inventory: compiler EmitHost/Emitter/Program, required VFS ownership path and whole
  original-corpus consumer using existing Program.Checks/Checker.NoEmit.Validation.
  Owns JS baseline/declaration recompile/noCheck assertions and full comparator integration.
Root owns central builds, gates, global mapping, final evidence/docs/commits. Use
followup_task for actionable assignments to completed agents; send_message doesn't wake them.

Source reviews under artifacts/emit/source-review are clear for declaration-transform,
declaration-support, vfs-write, compiler-output, command-output and compiler-sourcemap-baselines.
Review closed actual dictionary missing-key semantics, nested deferred state restoration,
JSDoc raw Unicode boundary, public zero-emitter nil result, and all-three-BOM recorder
handling. Source clear is separate from behavioral acceptance.

Required VFS support preserves raw Go bytes through OS/Bundled/Cached owners; write→parent
creation→retry, bounded write concurrency and errors. Append/remove/clocks unneeded here.
Original syscall diagnostic fidelity is being checked against Go, not automatically
excused as a .NET boundary. External subprocess content-mapper host remains an existing
external environment boundary; portable logger assertions were still ported, not excluded.

## Current execution — resume here

**Current live gate: root exec session50661** under
artifacts/emit/final-acceptance-workgroup-fix. Fresh build63.3s, all2223managed148.3s,
six Native publications96.7s, direct152+251 and retainedFoundation/utilities/Program
passed. Original ManyFiles liveness reproduced fixed (0.221sprocess, concurrentpub,
notperformancemeasure). All50 isolated originalbenchmarkobservations completed,
independentlyreviewed: bestNative/Go1.40–2.25x, noemitthresholdclaim. Full15330emit
corpus RUNNING, followed automatically by command then15304retainednoemit. Poll50661;
progress native-emit-corpus/progress.jsonl then noemit/attempt-1/progress.json.
No source/fixture/tool changes permitted during gates. Newproof writerprepared in
samegatefolder; execute afterremainingreviews. Final docs/proof/source-map/commit
remain. Do not stop at this checkpoint or restart prior failedbenchmark cohort.

### Prior candidate and repair history (no longer live sessions)

**Final correctness gates completed, but emit acceptance remains OPEN.** All final
2,223 managed tests, six Native publications, direct Emit152+251, Foundation3153+87,
utilities20, Program9, full15330 emit corpus (only6 external incremental metadata
differences) and all15304 retained no-emit records passed their qualified gates.
Command6+guards3+runtime3 passed after an observer-only quoting correction. Reviews
are clear; earlier phase freeze hashes and transition are preserved.

**New concrete issue: original ManyFiles emit benchmark hangs under Native.** Root
cohort session59661 stopped exit1 after root terminated exact PID687278 after103.7s
without progress (~zero CPU). Same Go200-file fixture completed process0.116s /58.6ms
measured Emit. Three preceding long-line paired observations completed; partial data
is artifacts/emit/final-benchmarks/result.json. native-manyfiles-timeout.json and logs
preserve failure. This is not a usable speed measurement or accepted benchmark.
Shared scheduling fix is now source-written and independently clear. Root session
23728 is running fresh build→managed→six Native publications under
artifacts/emit/final-acceptance-workgroup-fix; build passed63.3s. Sources frozen.
Run actual ManyFiles immediately after publication before the remaining broad gates.
The new benchmark cohort script reuses the unchanged Go binary and has a60s timeout.

Sol emit_inventory implemented ConcurrentQueue caller-participation in WorkGroup
and extended the existing construction-boundary nested-lazy control; emit_foundation
reviewed exactly-once, recursive completion and lost-wakeup behavior clear.
No debugger or heap capture. Root coordinates narrow rebuild/reproduction then
proportionate revalidation of affected gates; previous passed candidate evidence stays
honest. Candidate proof writer is prepared under final-acceptance but not executed.
Docs/source mapping closeout pending this defect and final benchmark observations.

### Earlier final execution detail (superseded run status)



**Final acceptance is RUNNING, root exec session51104.** Poll write_stdin51104.
All code/build outputs are frozen. Build attempt3, managed2223, six Native publications,
direct Native152original+251authored Emit and retainedFoundation3153+87/util20/Program9
passed with independent review. Full Native15330 corpus completed457.382s:15285 public
matches,13571 complete original assertion sets,39prep skips,1720originalpolicy records,
ONLY6 reviewed incremental build-info metadata failures. Comparator exit1 remains visible;
qualified contract gate independently accepted. Corpus/binaries/reference hashes stable.

CLI6 and boundaryguards3 passed. Runtime attempt1 failed in the Go-side Node observer
because embedded quotes were not escaped; fixed JSON string transport preserves exactly
the same source/expression/expectations. ONLY tools/emit-oracle/runtime_controls.py changed;
prior freeze and explicit one-file transition saved command/attempt-1. All earlier phase
completion records retain their original freeze hash (do not rewrite them). No build or
corpus repeated. Command attempt2 now PASSED6+3+3 runtime cases; full15304 retained
noemit comparisons RUNNING, progress final-acceptance/noemit/attempt-1/progress.json.

Build attempt1 failed at3 JSON Add generic/AOT overloads; explicit JsonNode casts fixed.
Attempt2 failed benchmark setup Slice/List API join; List capacity/order outside timing
fixed. Both independent reviews clear, failed attempts retained. Recipe now collects
compile failures across dependency graph before failing, behavioral gates unchanged.
Managed narrow repair6 and final493Emit cases passed. No more source edits planned.

After noemit: run reviewed in-repo benchmark plan final-acceptance/benchmark-cohort-plan.json
(5variants×5runs perruntime, no external project). Root executes; foundation review clear.
Then final proof/docs/map reconciliation, independent completion/evidence review, commit
and clean tree. No VSCode/profiling work. Reproduction from fresh checkout is documented
in docs/EMIT-REFERENCE.md and independently reviewed, no regenerations during gate.

**Full development run is complete**: artifacts/emit/program-output/dev-full-2,
15,330 observations in154 processes,671.071s, zero runtime failures. Frozen original,
supplement, comparator and154request hashes rechecked unchanged.15,285 exact public
matches +39 prep skips +6 external incremental differences. All nine original channels
additionally revealed six contentMapper baseline discrepancies (13565 matched assertion
sets). Foundation owns exact DiagnosticWriter canonical/source-map location adapter
completion plus supplemental-file consumer predicate; maps independently reviews.
No expectations changed. Full Native acceptance has not started.

Inventory prepared122 qualified original xUnit rows (artifacts/emit/final-managed),
including all eligible repaired cases and representative categories. It now prepares
a thin final gate script reusing accepted retained commands, with fresh managed/RID
builds and actual ILC input verification. Root alone runs central builds/gates. Maps
updates EMIT-VERIFY reproduction and reviews the final recipe. No external-project runs.

Content-mapper repair now passed: consumer-build7 10.961s zero warnings/errors; exact6
original public outputs and all9 original assertion channels match in1.540s, evidence
dev-repair-3. Independent maps review clear. No more code changes planned; final
source freeze and central gates follow reviewed artifacts/emit/final-acceptance/run.py.

First full raw run dev-full-1 had15,208 exact public matches,39 prep skips,83discrepancies:
63 nil Exports lookups and8 missing method comment ranges are fixed. Remaining6 diagnostic
failures were the omitted checker.go16942 context-sensitive parameter cache guard;
exact upstream guard restored, independently reviewed, no harness reordering. Temporary
public-stage probe isolated the defect and was removed; its evidence remains preserved.
Build5 passed10.945s; repair83 run4.476s matches all79 original assertion sets and77raw
outputs. Six raw differences ONLY buildinfo metadata require the separate incremental
builder/snapshot owner (reviewed existing contract boundary, not passing tests).
Command explicitly rejects unsupported incremental/composite execution while retaining
actual configuration diagnostics; ordinary Program and check-only behavior remain.

Complete original9channel consumer and comparator independently reviewed clear, including
real declaration recompile/noCheck repeats, exact formatted baselines and the11-record
Go source-map-record collision supplement. Last narrow review fixes: explicit CaseSensitive
JSON PathKey and full source/config/policy identity checks. Consumer-only build6 passed1.840s.
All original expectations are unchanged. Source reviews bind current sources; actual
Native/full retained correctness and final committed checkpoint remain outstanding.

Production build1 stopped on one NodeFactory alias (1.826s), build2 exposed15 API/type
joins (8.679s), build3 passed10.962s zero warnings/errors. Consumerbuild1 exposed three
nullable adapter joins; consumerbuild2 passed2.008s. Logs artifacts/emit/program-output.
First100 original configurations matched90 full public outputs/diagnostics and retained
10 original prep skips (13 option-policy rows included),2.074s. First actual managedCLI
six disk controls passed5.933s against actual pinnedGo: JS/dts/maps, Unicode paths,
output-generated/skipped failures, noEmit/listFilesOnly. Native command/full corpus pending.
Latest logger-only source change was not in that earlier CLI binary; final gate rebuilds it.

Go reference execution already COMPLETE:15,330 records,13,571 original full-harness passes,
1,720 option-policy dispositions,39 original preconfiguration skips. Eight output-only
skips;13,563 output assertions and13,571 each other eight channels passed. Zero original
assertion failures. Seven fixture-expansion failures stay separately inventoried.
Same prebuilt binary154 sequential100-request batches,150.417s measured. Raw236,001,291
observation bytes compressed27,141,120 (logs2,384,030); no duplicate raw batches retained.
Evidence artifacts/emit/whole-reference/{result.json,original.jsonl.gz}. No observer edits.
Actual Go CLI six reference cases: artifacts/emit/command-reference/go-controls.json.
Raw byte reading preserves CRLF/BOM; listFilesOnly is command-only, JSON TS6266 retained.

The nil-map fix preserves Go lookup semantics: only nullable Exports receiver is guarded;
Symbol remains required. Original GetErrorBaseline and source-map recorders are now
source-written; original JS baseline/recompile/noCheck joins are in progress. Maps ports
the demanded pinned patience DiffText helper for exact noCheck failure-baseline assertions.
Pinned write-failure witness is exact `mkdir <root>/blocker: not a directory`,5033
full DTO, nil emitted files, EmitSkipped=false. Inventory owns genuine MkdirAll/path
error adaptation, maps reviews. These are unresolved defects, not accepted platform
or observation differences. Raw arbitrary-byte/BOM witness also captured successfully.

Next: inspect full development discrepancies; fix ordinary defects with original expectations
unchanged. Finish original JS/declaration recompile/noCheck/map assertions, independently
review them; then integrated xUnit, actual Linux x64 Native AOT full corpus and CLI/runtime
output modes, retained foundation/binder/checker/noEmit correctness and applicable in-repo
performance gates. No external-project runs. Finish source map/evidence/docs and coherent
commits; continue to the fixed whole-emit gate, not merely this internal checkpoint.
Freeze source AND build metadata during acceptance publication (no intervening commits).
Check actual ILC dependency assemblies rather than assuming top-level build freshness.

User tangents already handled: Go file/line anchors remain in new ports; no historical
comment cleanup. Preferred chat performance summary replaced repo text at13a89a0b;
52,015,179,472 sampled bytes (52.02GB/48.44GiB) initial ReleaseCoreCLR cumulative allocation
clarification committed1eecad58, distinctly from live memory or exact Native bytes.
Do not redo profiling or stop after answering tangents.

# Accepted final delivery checkpoint (2026-10-11)

Integration31 is accepted: complete integrated correctness gates and the delivered
CLI performance target passed independent final review. The delivery commit containing
this entry records the accepted implementation and evidence. Do not restart builds,
gates, profiling or configuration trials on re-entry; this performance target is met
under the explicit configuration below. No more builds are pending.
The previous accepted21 baseline remains preserved separately.

The durable production CLI opt-in is `--no-gc-region` for one-shot `--config` checks.
It uses standard GC.TryStartNoGCRegion with 14 GiB total including 2 GiB LOH,
20 GiB Linux MemAvailable preflight, and honest fallback/end outcomes. Default
portable Server GC remains unchanged. Source adaptations include complete byte-name
producer/consumer boundaries, standard XXH3 integer map hashing (semantic hashes and
content equality unchanged), exact Node/Symbol pointer-identity LinkStore comparers
(other key types retain default equality), and complete parser temporary spill
ArrayPool ownership with required arena clones preserved. No isolated pooling or
hash/comparer speedup is claimed. Batch was a single artifact observation, not adopted.

CLI-only build passed 2.222 s with 244 sources/no drift; frozen29 Scanner IL remains
0d6730e7 and fresh CLI IL is 4c0d07af. Actual production ILC root/Scanner/Hashing
inputs and both portable/AVX2 binaries are bound. Portable default-four FIRST was
17.95 s (one observation, no performance target claim); AVX2 default-four FIRST
16.27 s. Matched-eight opt-in FIRST had a successful full region and exact original
outputs. Independent FIRST and correctness review passed.

The existing gates ran once: 1,730 managed cases (original 1,721 plus 9 CLI policy
cases), Native foundation 3,153 original assertions plus 87 shared controls,
utilities 20, Program 9, and all 15,304 original Native rows exact. No source,
expectation or Scanner rebuild was needed during validation.
[Correctness result](../artifacts/vscode-allocation-rollout/integration-31-production-delivery/build-1/regressions/selected-result.json)
and [independent correctness ACK](../artifacts/vscode-allocation-rollout/integration-31-production-delivery/correctness-independent-review.json)
retain the full gate evidence.

The final fresh five-each delivered cohort used Linux x64 AVX2, matched checkers=8
in pinned Go and Native, explicit Native --no-gc-region, unchanged source/library
inputs and no wrapper runtime flags. Native best 6.95 s versus Go 6.06 s gives
1.1468647x, meeting the user's primary best-to-best <=1.2x target on this workload.
Secondary medians were 9.25/6.78 s; ranges 6.95–10.42/6.06–8.33 s; median RSS
12,021,304/8,814,056 KiB. All five full regions and complete original diagnostics
qualified. A brief roughly three-second read/review may overlap the opening run;
all observations remain retained, and the two best runs were adjacent in round 3,
not the opening pair. Five observations and host variability do not establish a
universal runtime guarantee or portable-default target.
[Delivered cohort](../artifacts/vscode-allocation-rollout/integration-31-production-delivery/timing/comparison.json)
is distinct from artifact30 and earlier default-four/failed-eight cohorts.

[Final independent acceptance ACK](../artifacts/vscode-allocation-rollout/integration-31-production-delivery/result-independent-review.json)
is clear. Implementation, review and acceptance are complete in this delivery checkpoint.
[README reproduction](../README.md) describes portable and explicit AVX2 publication,
matched-eight config and opt-in/fallback qualifications. The abandoned debugger,
runtime-crash and heap-capture investigation stays closed. No further workload or
source audit is required to finish this accepted checkpoint.

# Latest user direction: abandon the diagnostic investigation (2026-10-11)

The user reported repeated platform classifier errors and directed abandoning the
debugger/runtime-crash investigation. Stop this line of work, including further heap
captures or attempts to work around the platform restriction. Preserve existing
evidence; do not resume this investigation after compaction or a restart. This
supersedes diagnostic assignments and pending actions in the historical record below.
Ordinary Go-faithful allocation and performance work remains authorized.

Integration25 finished: all15 diagnostic comparisons and guards passed independent
review. Reject adopting AVX2+C3: median wall was9.51% slower and median RSS7.99%
higher than AVX2 defaults; median CPU was essentially unchanged. Best/median seconds:
Go11.39/12.24, AVX2 defaults18.07/18.83, AVX2+C3 15.62/20.62. The1.2x target remains
unmet. Production source and portable runtime defaults remain unchanged. No build,
benchmark or diagnostic workload remains active; source/metadata freeze is released.
Integration23 remains a promising CPU-specific artifact, not the production default.

# Prior performance continuation: accepted21 restored (2026-10-11)

Latest decision supersedes provisional POH entries in the chronological record below.
Integration22 POH placement was **rejected**, and the exact original Linkstore source
and accepted21 central/native build outputs were restored. Candidate source, binaries,
all measurements and the unexplained9.735s GC pause remain preserved under
`artifacts/vscode-allocation-rollout/integration-22-poh/`; rejection.json records restoration.
The repeat did not reproduce9.735s (baseline max0.830s/candidate1.586s), but neither
capture established consistently lower GC cost. Five uninstrumented runs showed only
modest wall/RSS improvement and effectively flat medianCPU. No broader arena extension.
No new comprehensive regression acceptance is claimed for the rejected experiment.
Accepted production compiler remains integration21/077578ed; 1.2x Go target unmet.

SLL and R2R also rejected; production runtime defaults unchanged. All jobs finished;
no build/performance workload active, source/metadata freeze released. Root is recording
these reviewed results, not stopping the autonomous performance mandate.
Next active packet: integration23-avx2. Sol cache_audit confirmed accepted21 actual
XXH3 long accumulation/Span comparison SSE2, pinned ILC default excludes optimistic
AVX/VEX while Go dispatchesAVX2. Root authorized ONE explicitIlcInstructionSet=avx2
artifact-only candidate using unchanged21 wrapper/frozenIL, Default/telemetryOFF.
Authorcache_audit prepares coherentpublish+FIRST+5Go/5old21wrapper/5new23wrapper packet;
Solrecovery reviews packet once, root executes. No productioncsproj/algorithm change.
AVX2CPU requirement explicit; nativeflag/AVX512 expansion excluded. Packet0738d3a1 independently reviewed clear; root publication96863 completed. Native
FIRST61900 passed exactoriginaldiagnostics (19.218s preliminaryelapsed). Fiveeach
Go/accepted21wrapper/AVX2 telemetryOFF timing56801 completed all15exact +finalguards.
Best/median Go9.13/9.56, portable17.12/17.84, AVX2 13.70/16.58; targetunmet.
Recovery independently proved actualYMM vsbaselineXMM XXH/Span paths and reviewed
all15rawresults/guards clear; statusdoc updated. Retain promisingCPU-specificartifact;
portableproductiondefault unchanged.
AVX2 evidence committed3f98000c. Next integration24-retained-heap: exactruntime10.0.12
supports standardNativeAOTgcdump object/edge/typeID events (typenames omitted, debug
symbol attribution needed; root completeness limited). Solcache_audit prepares
artifact wrapper retaining actualProgram/checker owners afterCheck +release/KeepAlive,
modernlocalgcdump installation and ONE boundedcapture recipe; recovery reviews once.
Root owns publication/workload. Source-ready93be0362 independently reviewed clear:
actualproject->Program->Checker[] ownerhold+KeepAlive, explicitEventSourceSupporttrue,
collectoronly4GiBmanagedlimit/1GiBperfilelimit,120scollection/5minhold. Hostdiskfree
unknown (mountpointsnotmounted), userreportedfreeinghostspace; boundedexposure. Root
publication40528 completed with actualEventPipeenabled bindings independently clear.
Capture86350 FAILED EndOfStreamException openingheapsession aftertypeflush (~2s);
no dump/stdout/policy, targetexit notpersisted byoriginalfailurepath. NO validretained
composition/diagclaim. Authorcache_audit investigates exacttool/runtimeIPC; tinyexisting
project samebinary compatibilityprobe authorized, NO fullVSCode rerun yet. Recovery
reviews failure reasoning. Toolwarn.NET11Blockunsupported/fallback; causeunknown.
Originalfailedrecords retained; collect-with-outcomes.py fixesfuturebookkeeping only.
Tiny samebinary examples/program compatibilityprobe reproduced targetSIGSEGV(-11),
collector255 before release/noheap. Author nowauthorizedONE tiny samebinary gdb
backtrace withmatchingdbg ifinstalled; no largeVSCode retry orrebuild. Neither gdb
nor lldb present (PATH+cache+/usr libs checked); root requesteduser installgdb via
sudo apt-get update && sudo apt-get install -y gdb, awaitingreply. No debugger
reproduction executed. Recovery checks exactruntimeknownissues whilewaiting;
author records failedcapture statusdocs. Exacttoolsource
uses1024MiBeventbuffer, correcting earlier256MiBestimate. Blockfallback expected;
Strong knownruntimecause candidate: dotnet/runtime#131677, fix#131678; exactpinned
heapdump path uses ScanContext.thread_number=-1 asServerGCindex. Source chain
verified in known-runtime-defect-assessment.json; notbacktrace-confirmed. This is
separate from prior9.735spause. No storage/largeheapOOM inference fromfailedcaptures.
Continue with standardCoreCLRretainedcomposition PROXY: authorcache_audit prepares
companionproject same24hold/21compilerIL, pinned10.0.12+ServerGC, ONEboundedcapture;
proxyready328b4987 afterfailurebookkeepingdelta independentlyreviewedclear;
publication13214 passed. CoreCLRcapture98428 FAILED TOO: targetSIGABRT(-6),
stderr free(): invalid next size (normal), collector255/StartSessionEOF~1.7s,
noheap/nodiagqualification. Failure/targetexit nowproperlyrecorded.
USERinstalledgdb. Authorcache_audit nowexecutes twofocusedtiny gdbreproductions
of SAMEpublishedNative+CoreCLR binaries, noVSCode/no rebuilds; recoveryinterprets
stacksindependently. Nativeknownbug remainscandidate; CoreCLRfailurecannotbe
attributedtoNative-onlybug. Tinygdb nowCONFIRMSNative#131677: ServerGCheapindex-1
fromGcScanRootsForETW. CoreCLRtinycapture succeeds (collector0,2.3MBdump, targetexit1);
largeCoreCLRabort unexplained. RootauthorizesONE fullVSCodeCoreCLR undergdb existing
proxybinary tolocalizefault or captureheap. Minimalrecipee76dfc13 independently
reviewed; fullCoreCLRgdb44738 reproduced allocatorSIGABRT onServerGCthread6,
freeinvalidnextsize(size64480), privatecoreclrIPs unsymbolized. Collector255/noheap,
secondstopownedkill, no naturalexit/DTOqualification. Independentrawreview saved.
Authorcache_audit owns exactcoreclr symbolsretrieval/sourceassessment (buildID
79945f51fb2612f13b7667a10a8fd29122664791), recoveryreviews attribution. Exactcoreclrdebugsymbols nowresolved with independentlycorroboratedINFERREDloadbias
0x7ffff7000000 (originalmapmissing); faultfree isBulkStaticsLogger destructor,
64464Bbuffer/64480Bchunk. Byte/WCHARcapacitymismatch verifiedsource butactualwriter
notproved/no publishedfixfound. Authorread-onlychecks longstaticfieldnametrigger.
No furtherheapcaptures or runtimepatch. No runtimeupgrade.
Next integration25 runtimecombination: ONEexisting23AVX2default vs samebinaryC3 vs
freshGo5each, recipeea7ed494/ready569bb233 independentlyreviewed. Roottiming61890
ACTIVE observerOFF; no newbuild/knobgrid or productionchange. C3wasunadopted, not
rejected; individualbenefits cannotbeadded. Recovery reviewsfinalresults.
Keepcommitfreeze until15finalguards; metadatachanges only aftertimingcomplete.
No Native retry/rebuild/runtimechange.
No VSCodeheapcomposition claims yet. Supersedesgdbpending above.
No runtimeupgrade/patch/workstationGCworkaround.
Proxycan guide logicalstoragefamilies, NOT Nativephysicalheapcounts/performance.
Nativefailedartifacts retained, gdbrequest remainspending for confirmation.
ForcedGC capture is NOT timing; one toolinvocation may force multipleGCs. No new
census framework or production source changes. Publication metadatafreeze released.
Preserveproductionportabledefault until evidence. Publication complete; metadata freeze released after timing56801 finalguards.
Do not restart rejected Symbol arenas, NodeLinks conversion, GC knob grids or POH rollout.
Use established proof/commands; avoid another recipe-review cycle for incidental wording.
Storageagent removed only rejected22 stale snapshot/linux-x64 subtree137.61MiB;
receipt artifacts/storage-cleanup/2026-10-11-rejected22-stale-snapshot.json. Future-safe
centralbuild recipe copies top-level outputs only; originalrecipes/evidence unchanged.

# Crash recovery and storage cleanup (2026-10-11)

The WSL/server reboot interrupted integration21 full Native no-emit acceptance.
User reports host disk pressure, has freed host space, and explicitly authorized
cleaning our generated garbage then resuming. Do not infer crash cause from guest df.
All precrash jobs/agents died; session27376 is NOT active. Last progress8832/15304,
69chunks. Source changes, original review artifacts and exact binaries survived.

Cleanup complete. Project62→24GiB; artifacts56→18GiB. Two artifact passes reclaimed
38.331GiB (30.819GiB derived exports/ETLX and compressed JSONL;7.512GiB obsolete
historical validation binaries/runtime copies plus compressed TRX/logs). Receipts:
artifacts/storage-cleanup/2026-10-11-artifacts.json and historical-builds.json.
Standard pinned `go clean -cache` reclaimed27.29GiB; obsolete installers175.76MiB.
Current20/21, original traces, matching profile binaries/debug, source/metadata retained.

Integration21 continuation completed: root session59438 exit0, all15304 exact original
records; reused8832 and executed6472. Independent final acceptance ACK73497dc8 confirms
full rows,1721managed, NativeFoundation3153+87,utilities20,Program9,actualILC and all
source/binary guards.2048 prefix records explicitly reconstructed from preserved exact
actual rows and sequential controller evidence; no invented uninterrupted pass.
Accepted dispatch checkpoint077578ed committed. Source/metadata freeze released;
no compiler changes since. Runtime investigation continues autonomously.

Same-binary backgroundGC screen completed session8691: default20.34/18.95s,
batch18.30/18.54s; all full diagnostics exact. Default retained (small screen, higher
batchCPU and variableRSS; no large/repeatable benefit claim). Native21 immutable
binary/compiler and full evidence remain under artifacts/vscode-allocation-rollout/integration-21.
Fresh5Go/5defaultNative/5Conserve3 comparison completed root82882 exit0, all15
complete diagnostics and final input guards exact. Recipee1822d61, reviewedclear by
Sol recovery. Results fresh-go-comparison/timing/comparison.json. Best/medianwall:
Go12.03/12.98; defaultNative17.83/22.47; Conserve3 14.70/17.80. Conserve3 vsGo
1.221945xbest/1.371341xmedian:20%targetNOTmet. Conserve3 medianRSS9,495,232KiB vs
default8,805,920/Go7,784,448. No adoptedruntimepolicy yet. All5Go runs
13.03,13.50,12.56,12.98,12.03 (interleaved); warm-input timing, hostnoise unisolated.
Sol cache_audit independently accepted fresh15result comparison and updated statusdoc.
DATAS15comparison completed31334 exit0, independentreviewclear. SameC3 bothNative:
DATASon median19.86wall vs off20.56 (+3.52%); offmedianRSS+11.76%. RejectDATASoff
adoption; full15diagnostic/reference guards exact. No runtimepolicy adopted.
SourceNodeLinks recheck found accepted18 already uses stablechunkstructs+refreturns;
NO missing classperlink allocation. Do not redo that family or rejectedSymbolarena.

Latest user asks SustainedLowLatency. Root authorizes one coherent artifact-only
Native diagnostic/timing wrapper reuse on frozen21CLI/Scanner/Hashing. Solstorage_audit
implements optional public GCtelemetry (initial/final +100ms changedcompletedindex,
max512records; heap/promoted/pause/generation fields; missedindicesexplicit). Standard
GCSettings.LatencyMode selectsnormal/SLL; effective readback MUST match becauseNative
setter can silentlyignoreSLL withoutbackgroundGC. Solrecovery verifies exactNative
route and independentlyreviews. Productiondefaults in BOTHarms (noC3/batch/DATASoff).
Lifecycle sourceReadyf771b151 independentlyreviewed. ONE Nativewrapperpublish
13.3587s, actualILC frozenScanner/Hashing/Speed/LINQfalse/ServerGC exact, compiler
NOTrebuilt. Nativewrapper c70515c68ceb5d37a29c0276f12a8b397b7db266f05a2f9e77b43d0b7edc5684.
Default+SLL+Go captures79270 completedalloriginaldiag. Independentcaptureanalysis:
Nativecollections31/15/8 vs36/17/9, allocation10.601/10.592GB nearidentical; totalpause
2.676/3.891s; maxobservedpauseGEN1 662/864ms. SLLstilllateblockinggen2; occupiedheap
proxies7.206/7.237GB similar, fragmentation.939/1.396GB. Snapshotgaps5/3withinobserved
interval; publicmetrics notscannedbytes/perowner/exactGo-livephase equality.

SamewrappertelemetryOFF15timingcompleted69590exit0: normalbest16.64median16.67,
SLLbest17.14median19.05;Go best8.58median11.58. RejectSLLadoption; rootaskedSolrecovery
independentresultreview+statusdoc. Publicsmallmodeverification sidecar includedbothNative
arms; no lifecycleobserver. EarlierC3best14.70/Go12.03 gap22% is cohort-specific,
NOTstableprogressclaim; laterGo8.58/9.06 validates freshreference necessity.

User nowrequestsReadyToRun experiment (tieredJIT), and Speed. Speedalreadyactual--Ot
productionNative+wrapper; R2RCrossgenoptimization must separatelyverify. Solcache_audit
prepared r2r-comparison artifact-only same3wrapperCSsources, frozen21inputDLLs transformed
byCrossgen; NativeAOTfalse/R2Rtrue, explicitTieredCompilation+PGO, hostruntime10.0.12
RollForwardDisable. Allpacks cached, offline-only restore, no downloads authorized.
Solstorage_audit reviewCLEAR binds sourceReadyf6b15025. ONE R2Rpublish completed
root13057 exit0: offline restore0.816s/publication3.730s; actualCrossgen transforms
and R2Rheaders checked, productioncompiler not rebuilt. Root9197 completed15exact;
independentreviewclear alloutputs/guards. Go best8.93median11.08; Native17.31/19.67;
R2R19.11/22.32. RejectR2Radoption (median+13.47%vsNative); 1.2xtargetNOTmet. Nativeacceptance
and1.2xtarget remain; R2Rresults separate, no assumptionpropertySpeedcontrolsJIT.
Sourcecheckpoint077578ed unchanged, productionruntimepolicyunchanged; no moreGCknobs.
User asks direct LOH allocation for compile-lifetime data. Solrecovery investigates
exactLinuxNativeAOT APIs/threshold and existing structarena chunk sizing read-only;
no arena policy/source change yet. Ordinary reference arrays do not place pointees
onLOH; no blanketpadding or NodeLinks re-port. Worst662ms pause was noncompacting
Gen1 with1.626GB promoted, not proof of copying cost.
Exact NativeAOT+CoreCLR source does support standard GC.AllocateArray<T>(length,
pinned:true) for reference-containing arrays (POH, not LOH; no size threshold).
Root authorized candidate integration-22-poh: Solcache_audit prepares artifact-only
patch for complete compile-retained LinkStoreArena/PagedLinkStore backing arrays,
unchanged capacities/ordinals/managed refs. Solrecovery independentreview; no parser
arenas/padding/unsafe storage. No production edits/build until9197 timing finalguards.
Reuse existing centralbuild/publishVSCodeFIRST scripts after source-review; measure
before adoption. POH remainscollectible/cardtracked; not zero-GC or demonstratedgain.
Integration22 sourceReady0637b687 independentreviewCLEAR; rootappliedexactreviewed
patch (onlyCore/Linkstore.cs twoallocationfactories, candidatec1ec3b8f). Centralbuild
root46390 passed10.63s compiler214d7c92; existing21 publication+VSCodeFIRST root99989
completed actualNative21.28wall/76.93CPU/8,740,036KiBRSS, all10fulldiagnostics exact.
SourceANDmetadata freeze active, no commits. Storage_audit preparing minimalreviewed
15timingrecipe adaptation 5Go/5accepted21productionNative/5candidate22productionNative,
cache_audit independentreview. No observers/wrappers/policyknobs. Reuseworkloadguards.
No broadregression untilcandidateevidence; oneNativeobservationnotbenefitclaim.
Recipe307baa2d independentACK77a4e200 released; root69366 completedall15exact,
Solrecovery actualpublication+finalresultreviewCLEAR. BestwallGo9.37/base16.77/POH15.22;
medians11.58/22.16/20.97. POHmedianwall−5.37%,best−9.24%,RSS−5.46%,CPU−0.55%; wide
variance, no significance/targetclaim. Retainpromisingcandidatependingbroaderverification;
accepted21 remainslastfullgate. Storage_audit nowprepares minimalreuse21 GC-lifecycle
wrapper in22-poh/gc-lifecycle, same3CSsources frozen22IL/normalmode only; cache_audit
review. Rootwillpublishonce thennew22/default+existing21/default publictelemetrycapture
serial (no SLL/C3/newtrace/timingclaims) tointerpretPOHoccupancy/promoted/pause. Source
andmetadata freezeheld for thispublication. Wrapperreadyfabe6878 independentreview
CLEAR; buildroot16186 completed, capture root21058 nowactive serial22Default then
21Default. No competingworkloads/broadregression. Source-map adds explicitlyprovisional
POH note, prioracceptance unchanged. Oncecapture/reviewcomplete rootmaycheckpoint
sourcecandidate+docs asprovisional; do not implyfullgate or target achieved.
IMPORTANT latestcapture21058 completed but POHadoption/checkpoint nowHELD: original
22capture totalpause14.053s vsfresh21 2.757; candidateGen0index35 compactingpause
9734.763ms, POH1.178GB. Independentreviewconfirmsrawcounterunits/diagnostics/bindings,
no attribution toPOH/observer yet. User askswhethermistake/astronomicalpause. ExactGC
source timer includes suspension-rendezvous and timed-GC waits/descheduling, notCPU.
Rootauthorized focusedreversecaptures21then22 samefrozenwrappers +perfstat100ms
task-clock/context-switch/pagefaulttext (+time), no rebuild/newobserver. Storage_audit
prepares, cache_auditreviews, rootexecutes. Keeporiginaloutlier evidence; do notexpand
POH or commitcandidate before resolvingresource concern. Otheruserprocessesactive;
no killing/tuning; guestcounters cannotfullyestablishWSLhost scheduling behavior.
Followupreadye88c1c81 independentACK6139ef1c released; root10616 nowrunning
reverse21then22 captures with softwareintervalcounters/time. No competingownedjobs.
No newbuild/observer edits and original9.735s anomaly retained. Next reviewactual
interval data/GCsnapshots beforeanyadoption/checkpointdecision.
10616 failedharness afterbaseline21: installedperf7 -I returns0 forchild1; idle
intervalnot-counted markers expected, kernelsoftwarecounterpermissions restricted.
Tool-only probesreproduced; originalbaselineGC/output/time retained, intervaldata
NOTusedforattribution. No inheritedcounterfailureclaim fromsleepingparent. Simpler
reviewedGNUtimefallbackreadyedfe9cb8 root42067 NOWRUNNING direct21then22 samewrappers,
explicit%xexit1/fullDTO, CPU/wall/fault/switchtotals +publicGC. No rebuild/newobserver.
No furthercapturevariantframework; totalscannotlocalizeindividualpause. Finishthis
focusedriskcheck then decide; avoidrepeatedmetadata-review loops. Latestuser asksstructs/
unmanaged: inline managedstructrecordarrays+refreturns alreadyused; ulonglocations
referencefree. Node-reference arrays do not inlineNodeobjects; no unmanagedgraphrewrite. No candidate
adoption yet. R2Rexperimentdocs updated; baseline accepted077578ed remainsseparate.
cache_audit completed acceptance
review/proof/docs/map updates and made folded exports opt-in in artifact-only analyzer
source; compiled historical analyzer unchanged. No future hugeexports unless requested.
Root owns execution/release/STATE/commit; no competing performance workloads.
Conserve3 is a single source-grounded hypothesis about repeated GC scanning vs heap
headroom, NOT adopted policy or equivalentGoGOGC. No compiler algorithm changed.

Prevention: Go cache trims by age, not a size ceiling; reuse unchanged pinned oracle
binaries and clear build cache between accepted boundaries when disk pressure warrants.
Keep original profiling traces and compact summaries, avoid retaining huge derived
stack exports/ETLX indefinitely. No blanket cache disabling, new storage framework,
or repeated small-change builds/profiles. HostVHD free space differs from guest df;
no VHD compaction/shutdown or host settings changed.

NEW USER TARGET: Linuxx64 Native AOT VSCode wall <=1.2x pinnedGo with equivalent
files/options/libraries/environment and complete diagnostics. HistoricalGo9.61s
implies11.53s planning threshold only; final acceptance needs freshpaired Go/Native
runs, observation overhead off. Continue sane upstream-faithful improvements without
stopping at milestones; no semantic weakening or broad algorithm redesign. Target
persisted START-HERE. User requests several VSCode runs and best result: report
best plus median/range for equal-count fresh Go/Native, retain all runs. Stackalloc investigation found17existingexplicit sites plus3
inlinearrayfamilies; remaining spelling-rune32scratch potential is not a measuredhotspot.
No stackalloc edits authorized during frozen gate.

Recovery recipe97ada464 reviewed clear: all8832 raw original rows exact;
6784 have durablemetadata,2048 records chunks53–68 reconstruct from exactactualrows+
sequentialcontroller creating69 after exit/exactguards. Progress alone insufficient
because it precedes assertion. Preserve zero/lostmetadata, mark recovered provenance.
Remaining6472 startchunk69, separatecontinuation with fsync files/result/directory.

The standing autonomous performance mandate continues. Resume active compiler state
below once this recovery/cleanup is complete; do not treat reboot as a new campaign.

# Active: continuing Go-faithful performance and allocation work

Resume under [START-HERE.md](../START-HERE.md)'s comprehensive ownership SOP.
Astra coordinates; existing GPT-6.1 Sol scanner/support/verification agents implement
and independently review. Do not stop at a commit, family gate or compaction.
Active source root is /home/greg/projects/typescript-dotnet on main.

## Integration20: accepted in this checkpoint

Current accepted source/acceptance checkpoint3f466095 (integration20).
Predecessor source5951030; rejected-experiment documentation3714e565.
Integration20 is preserved; the next dispatch family is now authorized below.
All integration20/21 gates finished; current performance continuation is recorded above. Root session39453
finished exit0: INTEGRATION20 SELECTED VALIDATION PASSED, original15304/15304 exact.

Completed families:
- Flow.cs + Checker.cs: complete synchronous narrowing/map/filter explicit-state,
  cached-static callbacks. Preserve branch/evaluation order, origins/reduction/caches
  and genuinely retained/deferred closures. Source review338ac6f8; one CS0176 central
  build qualification repair independently reviewed4bc5b554. No expectation changes.
- WorkGroup.cs: standard stateful ThreadPool work item replaces unused Task/wrapper;
  normal ExecutionContext, pending reservation, recursive queue, failure/finally/wait
  semantics retained. CheckerPool.cs fixed-size Go Slice buffers and actual len(files)
  capacity hints through producer/consumer chain; worker count/FENNEL/cache unchanged.
  Production reviewb9a22d82; existing seven cold assertions migrated to actual Slice API
  without retaining test-only production adapters. Cold adaptation reviewed separately.
- CLI Native AOT standard throughput policy: OptimizationPreference=Speed and
  UseSizeOptimizedLinq=false; ServerGC/default DATAS/ISA unchanged. Actual --Ot,
  LINQfalse and VxsortEnabled runtime archive verified. Installed NativeAOT pack has
  no MIBC: NO consumed PGO or PGO benefit claim. Config reviewc6839618.

Evidence: artifacts/vscode-allocation-rollout/integration-20/.
Build-1 failed only static qualification; preserved. Build-2 clean10.95s,0warnings/errors.
Compiler8eacff5cb69d7a5cac6d9bf5c607b5087dd09c7f662a33c49461cae036f1d6db.
CLI24c60da50168d72f7eae6c2c9c989263bd68b4187f1ddc8e9ea76c6946546ea7.
Actual Linuxx64 Native VSCode FIRST16.82wall/71.11CPU/8,166,848KiBRSS;
all10 complete diagnostics/chains exact, expectedexit1; independentACK7153b8de.
One timing, not demonstrated repeatable speedup. Saved pinnedGo9.61s remains faster.
Cumulative Native10,594,962,224B; -79,979,560B(-0.7492%) vs19,
1.178247x(+17.8247%) savedpinnedGo8,992,138,304B. Collectiondelta36/16/9.
Counter recipe/result ACKba6e320d/de843168. Combined source+policy result, not isolated
causality. Heap-accounting/runtime differences explicit, not RSS or sampled byte ratio.

Acceptance completed: managed1721 zero failure/skip (mixed original/port/additional
provenance); NativeFoundation3153original+87shared controls, Utilities20original,
Program7CheckerPool+2ConstructionBoundary, fulloriginal15304Native noemit exact.
Fresh actual ILC compiler AND application dependency hashes, Speed/LINQfalse flags,
946source/harness+242production guards verified. Production0warnings;12known unchanged
cold nullable warnings separate. RecipeACK946486ae; final independent acceptanceACK64a3080f closed, including
independent comparison of all15304 complete rows. Durable proof, assessment and source-map updated. No duplicate run or postcommit rebuild.
This checkpoint records the implementation and acceptance; continue with current CPU work below.

## Next work already authorized and active

Scanner recovered saved17 Native CPU caller stacks by separate perf script --tid
process per worker. Installedperf7.0.14 uses libdw (not libunwind), with cached-thread
selection bug confirmed by installed code. Four-TID recovery keeps3441sample periods/
timestamps, recovers3318multi-frame stacks; covers87.87%worker and54.758%alluserCPU.
Analysis under integration-17/native-cpu/per-tid-recovery/. Support independently
reviews sample conservation/binary/debug binding, coverage and inclusive/exclusive
claims. No package installation, source rebuild, new recording or SVG was needed.
Old17 predates18/19/20 changes; recovered required semantic/map work alone does not
justify replacement algorithms or establish current20 bottlenecks.

Root captured ONE current20 CPU profile using exact frozen build-2 CLI after all gates,
no rebuild/competingwork: integration-20/native-cpu/perf.data (8f56316c),5177samples,
179MB,99Hz userCPU/32768B DWARF. Full10 diagnostics exact,242live/copy+binary/config
guards pass, child expectedexit1. Controller session7273 exited1 only because strict
stderr classifier encountered perf 'Failed to open /proc/schedstat'. Original report/
assertion/data preserved. Verifier confirmed warning is optional CPU_DOMAIN_INFO header
metadata, not CPU/switch recording:5177 SAMPLE,99404 SWITCH,0 LOST/THROTTLE, complete
record boundaries. HEADER_EVENT_DESC metadata warning also exists on17; fixed attrs
and leaves decode. Current CPU data usable with explicit limitations;
DO NOT rerecord or silently rewrite failure. Scanner authorized leaf/thread/loss pass
then separate dominantTID decoder processes using proven libdw workaround. Support
closed historical recoveryreview39b6cde2 and available for current independent review.
Current recovery complete:2981 selectedworker samples,2888multiframe,88.483%workerCPU.
Current leaf casts4.21%all sampleduserCPU, dictionary9.52%, GC-runtime34.98% (includes
allocationhelpers); dedicatedGC/BGC33.80% overlaps, do not sum. Fullsources/callers
under integration20/native-cpu/per-tid-recovery/analysis.json. No inferred wallgain.
Current capture result independentreview8a5a47c8; support reviewing attribution.

Root AUTHORIZED next complete concrete payload-dispatch family (scanner implementation,
verification independent review): AST/Type/PseudoType concrete AsX and matching generators.
Standard C# fastpath `this as Concrete ?? (Concrete)data` removes redundant intermediate
base-payload check on valid objects, retains original bare-header null and mismatched
payload exception via cold fallback. Go AsX uses checked type assertions, NOT unchecked
casts: no Unsafe.As. Preserve real nullable predicates, Node Base-named public interfaces
and Type embedded virtual overrides; don't flatten Mapper virtual dispatch blindly.
Concrete218accessors and full virtualowner8 implemented, independentsourceACK0210bc3b;
final2sharedcontrols independentlyreviewed933af5ff. Source and buildmetadata frozen.
Verifier added2existing shared control bodies, originalcasecounts/assertions unchanged.
Root expanded sameconnectedfamily after sourceassessment: six Type embedded accessors
can become base virtual originalforwarding +TypeData override originalnildefaults,
retaining all custom/concrete overrides while removing redundant intermediate RTTI.
Scanner also owns complete analogous Node/Mapper forwarding joins where equivalent;
coordinate independentreview of generatedvisibility/abstractmethods and cached
SubtreeFactsValue recursion. No cachealgorithm change. Node IterChildren internalpayload
vs publicwrapper needs explicit visibility adaptation or retained guard if incompatible.
Preserve original baseforwarding behavior for bareheaders. No direct-predicate bypass.
Complete all applicable manual/generated family sites with source mapping, not just
sampled hot methods. Integration21/build-1 passed10.27s zero warnings/errors, compiler2dca9be631dfb3051467a1e4ac41aaf29cb1591837aa6b0b38099b8c5a9042b9.
Actual Native FIRST passed16.48wall/61.68CPU/8,657,716KiBRSS, full10 exact/all242guards.
Reversepaired baseline20/candidate21 completed60316: baseline15.36/17.11wall,
candidate16.84/15.55; medians16.235/16.195wall,64.685/65.48CPU,
8,887,960/8,548,110KiBRSS. No meaningful demonstrated speedup;2observations each.
Actual firstpublication independentACK4eb3ad28, pairedACK0d8afb6e. Root decides
standard faithful virtual dispatch removes authoredredundanthop, qualifybehavior
without speedup claim. Original regression session27376 was interrupted by reboot;
reviewed durable continuation completed59438 with full15304 exact. Final ACK73497dc8
includes managed/Native controls and actual compiler/application dependency bindings.
No new allocation capture needed for dispatch-only family. Source/metadata freeze lifted.
Current runtime-policy investigation appears at top; no map algorithm rewrite or
rejectedSymbol arena revival from required lookup hotspots.

## Preserved rejected experiment and runtime decision

Symbol arena experiment remains isolated in /home/greg/projects/typescript-dotnet-symbol-arena,
branch perf/symbol-arena commit953de084. Do not adopt source or run its compiler as main.
Full owner/consumer migration reviewed, boxed TypeResolution identity defect fixed.
Candidate allocations11,545,083,560B (+8.15%vs19); paired wallmedians18.43vs16.81s,
CPU70.285vs68.425,RSS9,908,206vs8,701,422KiB;2observations each, no statistical claim.
Wider managed locations outweighed fewer separate objects. REJECTED ADOPTION; broad
acceptance deliberately not run, no correctness completion claim. Exact evidence and
review preserved in experiment artifacts/symbol-arena/assessment.json and main proof.
No blind arena packing variants or reopened accepted families.

Default adaptive ServerGC retained: prior fixed16 screen's6.93%medianwall benefit
cost16.90%RSS in2runs; fixedHeapCount also changes DATAS/conserve/affinity policies.
No new GC policy sweep without concrete reason. Latest cumulative allocation ratio is
~1.18xGo, NOT5x; old sampled managed vsGoheap totals were not comparable exact measures.

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
