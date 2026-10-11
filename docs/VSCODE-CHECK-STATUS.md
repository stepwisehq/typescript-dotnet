# VS Code checking: diagnostic repairs and performance evidence

The post-acceptance investigation fixed two demonstrated port defects: a null
iteration crash and the ordinary CLI's publication of raw binding diagnostics.
The corrected Linux x64 Native AOT executable completes the main VS Code project
check. Its remaining ten diagnostics also occur in the exact pinned Go compiler;
no relation algorithm was changed to match the different installed compiler.
This follow-up checkpoint records the fixes, focused tests, and comparison findings.

The historical whole-checker acceptance at `17e8c36` and its corpus/proof remain
unchanged. This focused investigation does not repeat that complete acceptance gate
or imply that finite acceptance established exhaustive large-project correctness.

## Record ownership and source fast paths (integration19, accepted)

From accepted `827f131`, the combined source changes close several related costs:

- AST, semantic Type, TypeMapper and PseudoType payload dispatch derives from the
  concrete CLR object instead of retaining redundant self-references. PseudoType
  also loses its separate wrapper. Scalar AST nodes no longer carry the cache that
  upstream embeds only in composite nodes. Composite volatile facts and propagation
  remain unchanged; missing-list state follows the source sentinel/header.
- Intersection construction now follows upstream orderedSet: linear membership
  through16 entries, then a standard identity HashSet, retaining insertion order
  and the original slice capacity. Known-count map hints omitted from globals,
  file indexing, diagnostic alias results and one-member binding tables are restored.
- Immediate Relater error reporters reuse a lazy bound delegate across the existing
  exclusive lease/free-list lifecycle. Captured comparers retained by inference
  remain. The authored RealProgram adapter classifies libraries/user files once
  using the genuine Program predicate, preserving source order and duplicates.

All source families and the added assertions in the existing two parser-list
controls have independent Sol reviews. Controls cover computed-zero facts,
propagation, bare-header behavior, missing-list clone/header replacement and growth;
they do not add a private-layout census or change original expected results.
The public authored IsMissing field becomes a getter derived from the current
slice header. Generator and parser regeneration inputs preserve the changes.

The frozen production build passed with zero warnings/errors. Actual Linux x64
Native AOT VSCode ran first and retained all10 complete diagnostic records/available
Go chains: **17.22s wall /71.48s CPU /9,672,180KiB peak RSS**. This single observation
versus18's17.72s does not establish repeatable speedup; CPU and RSS did not decline.
Compiler SHA256 is `bd28c9ddd6a18c402dc60890afd031775e16058c09d69a0495cdb4d5bb83260f`.

The one [allocation capture](../artifacts/vscode-allocation-rollout/integration-19/managed-profile/comparison.json)
reports **10,818,807,776 sampled bytes /10.075800GiB**, down **3.2024%** from18 and
**71.3493%** from the original matched ServerGC baseline. Source-bound AST payloads
account for much of the combined reduction; individual layout savings are not
isolated experiments. All92,791 ticks have types,92,790 have stacks with at least
one resolved frame; zero loss/truncation/overflow. Compiler-generated closure labels
can change between builds and must not be treated as stable semantic identities.
Partial GC histories and sampled CoreCLR intervals cannot establish exact Native/Go
allocation or GC-time ratios. Unlike18, capture completed before any regression
build/test; only offline analysis overlapped correctness tests.

Acceptance passed **1,712 selected managed cases** (Checker832, TypeSystem84,
Parser695 and additional NoEmit101), **15,304 complete original Native records**,
**3,153 original foundation assertions**,87 shared controls and20 original utility
observations. Independent source, profile and final evidence review found no
unresolved behavioral discrepancy. Actual compiler/application ILC inputs and
587 source/harness inputs remained bound to the frozen snapshot. Earlier16/17/18
gates remain retained rather than relabeled as executed under19.

A stale RID-specific Parser.Validation dependency caused the first NativeFoundation
run to fail with MissingMethodException despite correct compiler binding. The fresh
already-built dependency was restored; only Checker and Foundation validators were
republished. Initial failure and retries remain in the evidence. A controller-only
NameError after all Native checks passed prevented final aggregate emission; the
aggregate was reconstructed from original TRX/stage/chunk records and independently
reviewed, without rerunning the corpus. Twelve cold nullable warnings match18;
production had none. Actual ILC application dependencies now receive the same
binding checks as the compiler, avoiding this active publication pitfall.

One production build/CLI publication, one allocation capture and one selected
harness build were used; three validator publications required two binding retries.
No semantic repair or production rebuild. Full original managed corpus execution
was omitted here because the original Native corpus plus selected managed suites
cover this integration, while earlier managed-original evidence remains preserved.
Three Sol agents were reused; no token/cost measurements are available.

Reproduce via the existing [no-emit instructions](./CHECKER-NOEMIT-REPRODUCTION.md),
[shared foundation checks](../validation/Foundation.Checks/ParserListScratchChecks.cs)
and [exact selected commands/results](../artifacts/vscode-allocation-rollout/integration-19/regressions/selected-result.json).
The ordinary VSCode command remains unchanged; [durable proof](../validation/results/allocation-rollout-verified.json)
records this snapshot separately from previous acceptance.
The [cumulative allocation comparison](../artifacts/vscode-allocation-rollout/integration-19/cumulative-allocation/comparison.json)
now uses public runtime counters around normal CLI checking, before report formatting:

| Runtime | Before | After | Workload delta |
| --- | ---: | ---: | ---: |
| Native AOT10.0.12 | 103,648B | 10,675,045,432B | **10,674,941,784B** |
| Go1.27.1 | 1,014,928B | 8,993,153,232B | **8,992,138,304B** |

Native's cumulative heap-accounted delta is **1.18714× Go (+18.714%)**, a gap of
1,682,803,480B. Earlier comparisons between CoreCLR sampled intervals and Go's
potentially GC-lagging heap profile cannot support a several-times allocation claim.
This new measurement is the current CLI-wide volume comparison. It does not revise
or discard the earlier raw profiles, which remain useful for allocation attribution.

Artifact-only Native entry/finally blocks call GC.GetTotalAllocatedBytes(precise:true)
and reference the exact frozen compiler. A Go build overlay changes only the main
wrapper/imports, calling runtime.ReadMemStats before/after unchanged runMain and
writing before os.Exit. Removing the additions reproduces both original CLIs.
Both runs retained exact complete diagnostics, empty stderr and original exit1/2;
source/config/binary and actual Native ILC reference guards passed. No forcedGC,
profiling, production source modification or repeat run. Sidecar formatting is
outside the final counter; instrumented elapsed times are not speed measurements.
The [preparation/build/run recipe](../artifacts/vscode-allocation-rollout/integration-19/cumulative-allocation/source-ready.json)
records reproduction and bounds.

Runtime allocator rounding, object headers, stack allocation, frontend startup and
cleanup boundaries differ. These are cumulative heap-accounted bytes, not retained
memory, RSS, native allocations or equal logical allocation counts. Native's public
collection deltas36/17/9 and Go's19 collections describe different collectors and
cannot be compared as equivalent cycles. CPUcount/GOMAXPROCS16 is not observed GC
heap count. A subsequent [ServerGC policy screen](../artifacts/vscode-allocation-rollout/integration-19/gc-policy/comparison.json)
ran the unchanged accepted Native executable six times, in forward/reverse order:

| Policy | Wall observations | Median CPU | Median peak RSS |
| --- | --- | ---: | ---: |
| Default adaptive ServerGC | 17.44s /17.21s | 67.04s | 8.338GiB |
| Four fixed heaps | 17.56s /17.28s | 57.94s | 9.022GiB |
| Sixteen fixed heaps | 15.91s /16.34s | 61.295s | 9.747GiB |

All six complete diagnostic outputs, original exit status, source/config/binary
and runtime guards passed; independent review closed. Fixed16's median wall time
was6.93% lower with16.90% higher peak RSS. Fixed4 reduced CPU13.57% while median wall
was0.55% higher. **Keep the production adaptive default unchanged**: this finite
screen demonstrates a tradeoff on this host/workload, not a general better policy.

Explicit heap count disables DATAS; default-versus-fixed also changes implicit
memory conservation and default affinity policy. The separate NativeAOT10.0.12
public configuration probe verifies accepted settings, not the target's changing
heap history. Only the unchanged compiler is timed. Two artifact guard mistakes
(linker spelling and public configuration key names) stopped preparation before
any timed run; the successful probe build was reused. Finite corrections were
reviewed with final results rather than requiring more handoff cycles. Further
screens should derive those guards from actual captured output from the outset.
These two observations/policy do not establish statistical significance, isolate
GC costs or explain the whole Go gap. No additional policy sweep was performed.

## Current CPU and storage follow-through (2026-10-11, accepted)

The [current CPU comparison](../artifacts/vscode-allocation-rollout/integration-17/cpu-comparison.json)
profiles the frozen integration17 Native AOT executable and pinned Go against the
same VS Code configuration. Both retain the ten reference diagnostics. Native
Server GC/runtime methods account for **37.57% of sampled user CPU**, mainly marking
and compaction, but also including allocation helpers; dedicated GC threads account
for **24.14 scheduled CPU seconds**.
Go's built-in profile records **5.55 CPU seconds on GC-bearing stacks**. These
classifications and sampling domains differ: they support prioritizing allocation
and GC scanning, not subtracting percentages or predicting wall-time savings.

Four native workers carry 87.87% of worker samples, matching upstream's checker
grouping. Current samples do not establish cache locks as a major bottleneck:
Monitor.Enter appears in five samples, and off-CPU intervals do not identify why
threads waited. Native worker DWARF unwinding failed despite resolved leaf symbols
and zero sample loss; Go's built-in callchains remain usable. Raw profiles are
retained, with no new SVG. Profiled runs are not new uninstrumented benchmarks.

The implemented and independently reviewed storage change replaces retained 16-byte Memory
locations with store-local 8-byte ordinals across all 26 map-backed link stores and
symbol-arena pages. Stable arrays retain upstream's 2-to-256 growth; consumers still
receive actual live refs or Memory when required. The chunk directory and lookup
are explicit extra costs. The integrated Native build checks VS Code with all ten
complete diagnostics unchanged at **17.72s wall /70.46s CPU /9,126,904KiB peak RSS**;
this single observation does not establish a repeatable speedup over the preceding
19.83s observation. The retained regression gate passed. This is local
storage adaptation, not a new global handle system or
changed checker algorithm.

The [combined allocation capture](../artifacts/vscode-allocation-rollout/integration-18/managed-profile/comparison.json)
reports **11,176,731,432 sampled bytes /10.409142GiB**, down **3.9768%** from
integration16's 10.840235GiB. It includes the intervening integration17 text guard,
so it is not an isolated per-change experiment. All96,056 allocation ticks have
types and stacks with at least one resolved frame; no loss/truncation/aggregation
overflow was recorded. These are sampled CoreCLR intervals, not exact Native AOT
or Go allocations, retained memory, or object counts.

After including the common integer location arrays/map entries and new chunk
directories, the changed symbol-link and map-link families account for about
442.61MB of the 462.88MB total sampled reduction. Named payload types alone would
have exaggerated savings by overlooking the new common storage. Source-based
classification is explicit: bounded caller observations cover294.34MB of298.40MB
of the common UInt64 array bucket; the remaining4.05MB is not an exhaustive
event-owner proof. New typed directory allocations contribute1.857MB and are
included once. Some dependency compilation overlapped capture preparation due to
a crossed scheduling handoff; the target allocation events and all guards remain
valid, but no profiled elapsed-time comparison or full serialization is claimed.

The [public Symbol assessment](../artifacts/vscode-allocation-rollout/public-symbol-storage-assessment.json)
found that a naive Memory-backed value handle would widen every symbol reference
from 8 to 16 bytes, including symbol tables, arrays, AST/type fields and the parent
and export links. A class facade would retain the per-symbol objects. Neither is
adopted as an allocation improvement; this does not establish that every future
packing design is unhelpful. The authored ownership token enforces cross-checker
API ownership and cannot simply be removed as observation overhead.

A standard ConcurrentDictionary adaptation of Go SyncMap was also assessed.
It would add per-entry node allocations, with no current proof of substantial
lock contention. It is deferred; immutable Program metadata dictionaries and the
upstream-matching checker worker budget remain unchanged.

Acceptance passed **16,321 managed cases** (Checker832, TypeSystem84, NoEmit15,405),
with zero failures or skips. All **15,304 original Native AOT records** match the
unchanged pinned-Go references, including complete diagnostics, semantic observations,
union ordering and parent-pointer assertions. The native utility consumer also
passes all20 original observations and the added shared growth/lifetime assertions.
Actual ILC reference inputs consume the frozen production compiler;587 source/harness
inputs and242 production/generator inputs remained unchanged. Earlier foundation
coverage remains explicit in the16/17 evidence; it was not all rerun here. See the
[durable proof](../validation/results/allocation-rollout-verified.json) and
[execution results](../artifacts/vscode-allocation-rollout/integration-18/regressions/selected-result.json).

Three existing Sol agents handled implementation, profile work, controls and
independent review. One production build, one CLI publication, one selected
validation dependency refresh and two validator publications were used, with no
production repair/rebuild. The current Native/Go CPU captures preceded implementation;
one combined allocation capture followed it. A crossed handoff caused some overlap
and a test pause, and the full managed no-emit run had started before a proposed
reduction of duplicate corpus execution arrived. Both runs were preserved instead
of aborted/repeated. Fixing the execution sequence before launch will avoid that
coordination overhead. No token or monetary usage measurements are available.

Reproduce the storage controls with the existing checker xUnit suite; its utility
case retains the original Go observations and now also asserts live-location
behavior across arena growth. For actual Native execution, publish the same
utility consumer:

```bash
dotnet test tests/Checker.Tests/Checker.Tests.csproj -c Release
dotnet test tests/TypeSystem.Tests/TypeSystem.Tests.csproj -c Release
dotnet publish validation/Checker.Validation/Checker.Validation.csproj \
  -c Release -r linux-x64 -p:PublishAot=true \
  -p:CppCompilerAndLinker="$PWD/tools/.cache/native/bin/cc" \
  -o artifacts/compact-links/native
artifacts/compact-links/native/Checker.Validation --utilities
```

The complete original no-emit corpus uses the existing
[reproduction instructions](./CHECKER-NOEMIT-REPRODUCTION.md). The ordinary Native
VS Code invocation is in [Reproduction and records](#reproduction-and-records).
Raw profiler commands and exact binary/source hashes are retained in the linked
comparison artifacts. No generated graph or new profiling infrastructure is needed.

## Optimization reference index

This index separates identified techniques, implementation coverage and evidence.
Historical sections below preserve the measurements at their exact snapshots;
[STATE.md](./STATE.md) records the live rollout and
[the operating procedure](../START-HERE.md#current-operating-procedure-comprehensive-allocation-rollout)
controls how it continues after a restart. “Qualified” below means the recorded
finite verification passed, not exhaustive correctness or completion of the current
broader rollout. Combined changes cannot establish isolated per-technique gains.

| Identified allocation or runtime issue | Source-faithful technique and current coverage | Evidence / reference |
| --- | --- | --- |
| Workstation GC is unsuitable for this measured workload | Server GC enabled in the executable and verified under Native AOT. No new heap-count or GC-policy experiment is underway. | [Measured Server GC comparison](#server-gc-enabled-and-verified-under-native-aot) |
| Go substrings retain backing storage; ordinary C# substrings copy | Borrow immutable source through byte views, JSDoc fragments, tokens and AST text; project only actual string consumers. Owners, descriptor consumers and transformed-token byte builders are included in accepted integration16. | [JSDoc sharing](#comprehensive-jsdoc-commentlink-text-sharing-verified-implementation), [owned UTF8 pipeline](#owned-utf8-source-pipeline-active-completion-contract) |
| Repeated byte/string conversions during comparisons and scans | Span comparison/search, direct UTF8/WTF8 decoding, preserved byte offsets and invalid-byte advancement. Fixed ASCII SearchValues runs and rune/escape output closure are included in the combined native workload proof; broader regression acceptance passed in integration16. | [Borrowed token work](#borrowed-tokens-flow-slices-and-conditional-targets), [live state](./STATE.md) |
| Names are forced into managed strings just to enter internal maps | Borrowed immutable ByteStringKey with standard Dictionary and alternate lookup; symbol/literal/property keys compile in the combined snapshot. Resolver/cache/path continuation is included in the combined native workload proof; broader acceptance passed in integration16. No custom table or interner. | [Source mapping](../source-map.json), [key support](../src/TypeScript.Scanner/Runtime/ByteStringKey.cs), [live state](./STATE.md) |
| Managed List wrappers and copied subranges replace Go slice headers | Slice storage through owners, caches and consumers; standard Span.CopyTo, source Clip/Clone/detachment and captured range semantics. Inference/signature/node/constituent families qualified earlier; generic/member/program/diagnostic families are included in accepted integration16. | [Slice semantics](#implemented-follow-up-slice-support-and-inference-stacks), [AST slices](#ast-child-slices-and-arena-publication), [constituent results](#type-constituent-slices-and-parser-scratch) |
| Capturing delegates and LINQ introduce transient objects | Explicit state plus cached static callbacks, source-shaped loops and retained Go lazy/scratch boundaries. The established connected immediate-callback families are included in accepted integration16. Retained closures are not removed when upstream lifetime requires them. | [Checker/flow callbacks](#checker-and-flow-callback-state), [local mapper/parser scratch](#local-mapper-and-parser-scratch) |
| Go embedded records became separately allocated wrapper/payload pairs | Direct inheritance preserves one object and source identity/dispatch. AST and checker Type merges are in the integration5 workload proof; mapper header/payload merging is included in integration6. FlowNode/FlowList are genuinely separate Go records and remain separate. | [Live state and review boundaries](./STATE.md), [source mapping](../source-map.json) |
| Go packs records into stable arena chunks; C# currently allocates records separately | AST/Type/Mapper wrapper merging is implemented. Internal ValueSymbolLinks, 26 map-backed link stores and inline node pages are implemented/reviewed and exercised in Native AOT integration12. Wider location storage has measured costs; aggregate allocation reduction is not an isolated arena benefit. Public Symbol packing remains excluded. | [Arena assessment](../artifacts/vscode-allocation-rollout/integration-8/managed-profile/arena-assessment.json), [live state](./STATE.md) |
| Link/reference ownership adds per-record wrapper objects | Direct reference slots and opaque ownership tokens with preserved source lifetime/identity. Earlier qualified changes; no unrelated private-observer redesign. | [Link storage](#fixed-link-store-reference-slots), [ownership/reporters](#opaque-ownership-tokens-and-unbound-reporters) |
| Scratch copies, general path work and singleton growth add avoidable work | Retain source scratch/header reuse, ASCII/path fast paths and source initial capacities. Required persistent snapshots remain detached. Broader canonical resolver/path receivers are included in the combined workload proof. | [Path/capacity work](#path-fast-paths-and-singleton-capacity), [parser scratch](#type-constituent-slices-and-parser-scratch) |

Important retained findings:

- A missing upstream `Clip` during tuple-spread expansion caused extra diagnostics;
  the exact source ownership operation fixed it. Do not remove a required copy or
  capacity restriction merely because it allocates. The
  [AST slice record](#ast-child-slices-and-arena-publication) preserves the failed
  snapshot and the regression evidence. Current source review also corrected
  declaration cloning to the upstream `Clip` where sharing is required.
- Go escape analysis motivates inspecting actual lifetime and ownership. It does
  not prove that all Go temporary objects are stack allocated or that a C# struct
  conversion preserves identity. No blanket stack-allocation claim is made.
- Sampled CoreCLR allocation weights are not exact Native AOT totals; the saved Go
  heap sample is not an exact whole-run allocation census. RSS is not allocation
  volume. Later source changes have no measured savings until integrated execution.

The following integration5–16 chronology preserves earlier checkpoint status; the
[final integrated results](#comprehensive-rollout-integrated-results-2026-10-11)
supersede its pending-action statements. The earlier qualified baseline was `b84b436`. The frozen integration5
snapshot built with zero warnings/errors and published under Linux x64 Native
AOT; its allocation/CPU profiles do not cover the later integration6 continuation. Its first VS Code run matched all ten complete diagnostics and available pinned
chains: **23.63s wall, 94.93s CPU, 9,064,632 KiB peak RSS**, expected exit 1, empty
stderr and no source/config/binary drift. This single run is workload evidence, not
a strong throughput claim or the pending broader regression acceptance. The matched
CoreCLR profile reports **14.642716 → 13.0697 GiB sampled allocation weight (−10.74%)**,
zero lost events/truncation, and one allocation event without a stack. Strings fell
from about 813 to 474 MB; wider symbol and byte-key storage offset part of the gain.
These are sampled managed intervals, not exact Native AOT allocation totals.
See [exact execution evidence](../artifacts/vscode-allocation-rollout/integration-5/boundary-summary.json)
and [profile comparison](../artifacts/vscode-allocation-rollout/integration-5/managed-profile/comparison.json).
The integration6 continuation implements a missed upstream active-mapper cache
reuse rule and deferred/switch/accessibility storage owners end-to-end.
The corresponding [native CPU profile](../artifacts/vscode-allocation-rollout/integration-5/native-cpu/analysis.json)
attributes **48.272% of 7,986 sampled user-CPU observations** to Server GC helpers,
versus **0.63%** in XxHash32 completion. This supports continued allocation/lifetime
work, not a hash redesign. Worker-thread caller stacks were unavailable (4,066
samples), limiting caller attribution; these percentages cannot be subtracted from
wall time. Required link identities, the shared JSDoc projection owner, and the
existing Symbol ownership token remain explicit adaptations. A proposed token-only
subtype was declined because its modest binder-only savings do not justify added
layout/guard complexity. Generic binder factories and parser forwarding captures
are included in integration6, alongside immediate Checker/Relater/Inference callbacks. These are source-shaped follow-through changes, not runtime-policy tuning. Raw profiles and immutable source snapshots are retained under
[artifacts/](../artifacts/); no new SVG flamegraphs or per-local-change benchmark
cycle should be started from the historical reproduction commands below.

Latest combined workload proof: **integration8**, after tuple/variance metadata,
connected Type/Symbol/Node scratch, signature groups, NodeBuilder tracked caches,
parser argument spans and the source nil-task/nil-map corrections. Production build
**10.381s**, zero warnings/errors; Native AOT publish **10.912s**; VS Code matched all
ten diagnostics and available pinned chains: **24.54s wall, 91.59s CPU,
10,076,540 KiB peak RSS**. Source/config/binary guards passed. These are single runs:
integration6 was **26.13s/112.18s/10,020,768 KiB**, and integration5 was
**23.63s/94.93s/9,064,632 KiB**. No wall-speed improvement or causal regression is
claimed. Integration7's single recursion-header binding error and its reviewed fix
are retained. Broader regression acceptance remains pending. The combined integration8 managed
capture reports **12,498,151,248 bytes (11.639810 GiB)** sampled allocation weight,
versus integration5 **14,033,517,160 bytes (13.069731 GiB)**: **−10.9407%**.
Against the saved qualified b84 snapshot's14.642716 GiB, the combined change is
**−20.5078%**. The older matching Server GC baseline was35.167694 GiB (−66.902%
from that earlier point); the pre-Server-GC profile is not pooled into this comparison.
No event loss/truncation/export overflow was reported;108,641 allocation ticks,
with2 missing stacks. Disjoint closure weight fell906.394→504.194 MB, delegates
689.409→440.375 MB, arrays5.047→4.648 GB. These are combined sampled managed
measurements, not isolated savings or exact Native AOT totals.
The [profile comparison](../artifacts/vscode-allocation-rollout/integration-8/managed-profile/comparison.json)
retains provider, binary and workload attribution. Sampled managed stacks are heavily
influenced by waits/PollGC and do not establish native on-CPU shares. No new SVG or
performance claim is based on the profiled managed runtime.
See [integration8 evidence](../artifacts/vscode-allocation-rollout/integration-8/boundary-summary.json).

Residual review after integration8 distinguishes required allocations (instantiated
symbols and links, canonical AST records, immutable source buffers, owned union/member
arrays) from the remaining concrete omissions. Seven synchronous callback sites and
four internal Checker text projections, plus an Evaluator borrowed-text path, are now
being closed using the established techniques. Their source-only changes are not
covered by the frozen integration8 measurements; sampled attribution is not promised
savings. Broader regression qualification is still pending.

The [source-grounded arena assessment](../artifacts/vscode-allocation-rollout/integration-8/managed-profile/arena-assessment.json)
adds an important distinction: required logical records need not be separate CLR
objects. Go stores Symbol and ValueSymbolLinks structs in2→256-element chunks and
returns stable element pointers. Standard arrays of C# class references do not pack
the objects. Internal links might admit struct chunks and ref-return access, with
standard Memory only where locations escape; that requires connected mutation,
null and identity review. Wider location descriptors and retained storage are costs,
so no net benefit is assumed. Public Symbol packing requires a broader reference-model
change and is not authorized by this assessment. No custom handle framework is being
introduced. ValueSymbolLinks storage, its production consumers and existing observers
are now implemented and independently source-reviewed, with runtime verification pending.
The related 26 map-backed internal stores are authorized as one connected implementation
family. Source-file deferred-set initialization, the embedded two-table member/export
value, nullable JSX locations and recursive cache aliases require explicit adaptations;
the ordinary compiler algorithms and public reference objects remain unchanged.

After a VM interruption, integration9 records/snapshots were empty and remain
unqualified. Recovery verified the reviewed source successors. Integration10 published
Native AOT, then VS Code failed with `InvalidCastException` at the template-expression
consumer's remaining string assertion on a byte-backed Evaluator result. The failed
run produced no diagnostic comparison result. A finite consumer repair and independent
review corrected this defect; the failure is not a performance result or evidence
about the VM restart. [Failure evidence](../artifacts/vscode-allocation-rollout/integration-10/boundary-summary.json)
is preserved. Integration11 then passed the full ten diagnostic observations under
Native AOT: **22.03s wall, 92.84s CPU, 9,393,184 KiB peak RSS**. It used the repaired
library81548d13 and nativec81f5d31 with no source/config/binary drift. This is a single
observation, not a proven speedup. [Integration11 evidence](../artifacts/vscode-allocation-rollout/integration-11/boundary-summary.json)
is saved separately. Integration8 remains the latest allocation profile; it does not
measure these subsequent receiver changes. Broad regression acceptance remains pending.

Integration12 completes the connected checker-link source rollout: all26 actual map
owners use arena-backed value records, ValueSymbolLinks uses the source sparse-pointer
arena, and SymbolNodeLinks uses inline page values. Required live aliases, the captured
JSX location, deferred-set field replacement and enum-member map ownership were reviewed
together. The production build and actual Linux x64 Native AOT VS Code comparison pass:
**21.23s wall, 83.18s CPU, 9,367,284 KiB peak RSS**, all10 complete diagnostics and
available pinned chains exact. This single observation is not a proven speedup.
[Integration12 evidence](../artifacts/vscode-allocation-rollout/integration-12/boundary-summary.json)
binds the source, compiled library and native executable. One matched managed allocation
capture reports **11.262826 GiB** sampled allocation weight: **−3.23875%** versus
integration8 and **−67.97394%** versus the original matched Server GC capture. All103,631
events have typed stacks, with no loss/truncation/aggregation overflow and matching
complete diagnostics. There is no integration11 profile, so intervening receiver and
storage work cannot be assigned separate effects from this comparison.

Packing is a measured tradeoff: named value-arena/location types rise18.14%, inline
node-page types fall45.48%, and map-link types rise6.19%. These disjoint link buckets
increase112.6MB combined while total sampled allocations fall404.8MB; closure/delegate
reductions contribute to the latter. Wider standard Memory entries offset eliminated
object headers. This does not establish either a throughput win or loss for packing.
See the [complete profile comparison](../artifacts/vscode-allocation-rollout/integration-12/managed-profile/comparison.json).
Broad regression and cold-observer execution remain pending.

Integration14 closes the borrowed typed-path ownership family through resolver,
Program, configuration, VFS directory entries and the Glob matcher. Rooted/relative
paths and retained keys use immutable byte descriptors with content equality/hash;
unchanged subpaths borrow backing storage. Actual concatenation/normalization and
native-directory-buffer ownership transfers still copy as upstream requires.
Public display, JSON and OS text boundaries remain explicit. The previous temporary
borrowed-path overlay is removed in favor of the directly mapped source counterparts.
Remaining identified synchronous contextual-object/awaited/declaration callbacks use
explicit state in this same integration. Independent source review covered the owner
and consumer families, including existing cold adapters without weakening observations.

Production build and actual Linux x64 Native AOT VS Code comparison passed:
**19.23s wall,80.76s CPU,9,242,060KiB peak RSS**, expected exit1 and all ten complete
diagnostics/available pinned chains exact. Source/config/binary guards passed.
This single observation is not proof of a throughput improvement. Integration13's
four API-binding failures and the reviewed descriptor/Glob repair are retained;
no behavioral expectation changed. [Integration14 evidence](../artifacts/vscode-allocation-rollout/integration-14/boundary-summary.json)
binds the exact binaries and source snapshot. The [matched allocation capture](../artifacts/vscode-allocation-rollout/integration-14/managed-profile/comparison.json)
reports **11.074105GiB**, −1.675614% versus12 and −68.510575% versus the original
matched Server GC capture. Strings fall349.8→61.2MB while byte arrays rise246.0→337.2MB;
required source buffers and newly byte-backed owned outputs remain. All101,566 allocation
ticks have types/stacks, with zero loss/truncation/overflow and exact diagnostics.
Named link-storage buckets are essentially unchanged. These sampled CoreCLR weights
are not exact Native AOT totals or isolated technique savings. Broader regression
and cold-observer execution remain pending. The rollout
continues through remaining actual source omissions rather than treating this checkpoint
as completion.

Integration15 closes the source-prescribed parser/binder/file-task reuse and spelling
search family, plus the remaining identified immediate checker callbacks and shared
known-size byte concatenation. Parser and binder returns drop arena headers without
clearing published storage; all parser acquire paths return in finally. Losing file
parse candidates alone return to the task pool. Spelling uses one borrowed-name
selection algorithm and standard ArrayPool distance-buffer leases across candidates.
Fixed concatenation allocates its known-size result once and copies with .NET spans;
genuine incremental builders and required detached outputs remain.

Standard ConcurrentBag owner reuse is a documented adaptation: unlike Go sync.Pool,
it does not evict idle owners at GC. An idle parser retains its scanner and last source,
token and error-callback references, not merely an empty small wrapper. No public AST
or Symbol reference-model redesign or custom pool framework was introduced.

Production build and actual Linux x64 Native AOT VS Code pass with all ten complete
diagnostics and available pinned chains exact: **18.02s wall,74.72s CPU,
10,049,996KiB peak RSS**. Source/config/binary guards passed; this single observation
does not isolate a throughput effect. Integration14 remains the latest allocation
profile and excludes these successor changes. Comprehensive retained regression
qualification is now underway, with public parser reuse/lifetime controls added to
existing xUnit/native harnesses; no broader acceptance claim is made yet.

The consolidated first xUnit pass completed **40,451 cases:39,788passed/663failed**.
All **15,304 original no-emit corpus records** matched their complete expected outputs.
The failures exposed outdated shared observers plus two production defects: optional
alternate dictionary lookup assumed every comparer supported it, and bundled-directory
headers captured a generated name table before its static initialization. Both are
fixed without changing the original failing assertions. Standard collection fallback
preserves comparer semantics; an explicit static constructor initializes the bundled
header after generated field initialization. Remaining adapters preserve existing
schemas and nil/empty/order behavior. Authored ownership controls now observe Go slice
backing/range and immutable filename content across scalar promotion rather than boxed
or cross-projection CLR reference identity; a detached required shared slice still fails.

Integration16 builds and publishes actual Linux x64 Native AOT with the reviewed fixes.
VS Code again matches ten complete diagnostics and available pinned chains:
**22.12s wall,77.65s CPU,9,976,960KiB peak RSS**, empty stderr and all guards unchanged.
Previous15 was18.02s; these individual runs do not establish a causal performance change.
The repaired managed gate has **40,451 unique passing test outcomes**. This is
composite evidence: the full twelve-suite retry passed40,434/40,451; the final
cold-only observer/control repair passed the entire264-case Program suite and all11
filename controls. Both full managed runs independently matched **all15,304 original
no-emit outputs** by name and complete JSON. The original failures and finite reruns
are retained in the [composite report](../artifacts/vscode-allocation-rollout/integration-16/regressions/managed-composite-acceptance.json).
This does not mean40,451 upstream test functions: the total includes data-driven
cases, authored controls and differential fixtures with their existing provenance.

All17 Native validation executables have published using the exact frozen compiler;
the full15,304 original noemit comparison now passes under actual Native AOT
(350.06s, complete JSON exact, no drift). Broad parser and scanner comparisons also
pass17,723 inputs each; remaining semantic corpus/control commands are running. A commit during an earlier
publication attempt changed generated assembly/SourceLink metadata, so its hash guard
stopped before behavioral execution. Reusing the frozen compiler and compiling the
cold dependencies explicitly with `BuildProjectReferences=false` resolved this without
weakening the identity check. Failed publication logs remain available. Final Native
acceptance and one matched allocation capture are pending; Go speed parity is not met.

## Comprehensive rollout: integrated results (2026-10-11)

The established allocation techniques now run through their connected production
owners and consumers: borrowed UTF8/WTF8 source and keys, typed paths, retained and
scratch slices, synchronous callbacks with explicit state, embedded records and link
storage, upstream parser/binder/task reuse, shared spelling buffers and fixed-size
concatenation. Actual Go copies, Clip/Clone, owned changed outputs and escaping
callbacks remain. This closes the identified implementation families; it does not
mean identical runtime representations or Go performance parity.

The [durable result](../validation/results/allocation-rollout-verified.json) binds
these results to compiler SHA256 `c20f7f9e175528acdd99194acc8d896c47faeb1b3340266efc70b378eab4fa0c`:

| Acceptance evidence | Result and what it establishes |
| --- | --- |
| Managed xUnit |40,451 unique passing outcomes across12 suites, using the full run plus275 affected cold-only retries. Failures/retries remain explicit; this is not one all-green execution or40,451 upstream test functions. |
| Native original no-emit corpus |All15,304 complete recorded checking/walker/type/symbol/diagnostic outputs match pinned Go, including ordering and parent relationships. |
| Native foundation and binding input comparisons |Fresh pinned-Go comparisons match17,723 inputs each for scanner, parser and binder. Reusing compiler inputs here is not full compiler-test success. |
| Native direct/shared controls |3,153 foundation assertions,17,633 finite fuzz seeds,87 additional shared controls and23 Slice assertions pass; fuzz seeds are not all structural comparisons. Smaller scanner1,276/parser471/gap8 comparisons also pass. |
| Native semantic/environment coverage |All83 existing commands pass; decoded configurations12,824=12,817 exact plus7 matched original expansion failures. Nine supported disk CLI cases pass. Channels overlap and must not be summed as unique tests. |
| Native VS Code |22.12s wall,77.65s CPU,9,976,960KiB peak RSS; ten complete diagnostics and available pinned chains exact, expected exit1, empty stderr. |
| Final matched managed allocation capture |11,639,613,928 sampled bytes =10.840235GiB, −2.111860% vs integration14 and −69.175587% vs original matched Server GC. |

All17 validation executables were actually AOT-published and executed on Linux x64.
Production build has zero warnings/errors; cold observer compilation retains12
reviewed nullable-flow warnings. Source/binary guards stayed exact. The additional
ListFilesOnly-via-config attempt correctly received6266: the existing CLI has no
corresponding override flag. Its original public-API control passed managed xUnit;
this attempted transport is explicitly **not** a Native success. No CLI expansion,
expected-output change or diagnostic suppression was introduced.

The final capture has99,239 typed allocation events,99,237 with resolved stacks,
no event loss/truncation/export overflow. The2 missing stacks limit attribution.
Raw trace and ETLX remain available; no SVG was generated. These are sampled CoreCLR
weights, not exact Native AOT totals, retained memory or exact Go allocation counts.
Config hashes match the earlier captures; the recipe does not hash every project
input. Single unprofiled observations ranged18.02–22.12s over the last three combined
snapshots, versus saved pinned Go9.61s. This is not a controlled isolated-technique
speedup experiment and the eventual speed target has not been met.

The combined final continuation reduces sampled byte arrays337.2→278.0MB, builder
objects37.1→13.6MB, parser owners30.6→0.11MB and parser-task map storage. These are
sampled associations, not exact per-technique savings. Remaining costs include
canonical Symbol/AST/type records, required detached arrays, wider managed Memory
locations and dictionary entries. ConcurrentBag does not reproduce Go sync.Pool's
GC eviction: idle parsers retain their scanner and last source until reused or the
pool itself becomes unreachable. Existing source algorithms and published ownership
are retained; runtime-layout/retention differences remain explicit.

The final [comparison](../artifacts/vscode-allocation-rollout/integration-16/managed-profile/comparison.json)
corrects the prior “26 map payloads” subtotal: integration14 omitted VarianceLinks
543,176B, so the consistent26-type subtotal is652,708,008B. The overall historical
total is unchanged. Final named link buckets do not uniformly shrink; wider location
storage is a cost, not a claimed isolated benefit.

Reproduce the production workload from the repository root (the final command is
expected to return1 for the pinned VS Code diagnostics):

```sh
dotnet build src/TypeScript.Check/TypeScript.Check.csproj -c Release
dotnet publish src/TypeScript.Check/TypeScript.Check.csproj -c Release -r linux-x64 -p:PublishAot=true -p:BuildProjectReferences=false -p:CppCompilerAndLinker="$PWD/tools/.cache/native/bin/cc" -o artifacts/allocation-reproduction/cli
artifacts/allocation-reproduction/cli/tscheck --config /home/greg/projects/vscode/src/tsconfig.json --noEmit --json
```

This uses the real project config, including `skipLibCheck: true`. Prepare the
committed no-emit registry and export the required environment variables using the
[existing reproduction guide](./CHECKER-NOEMIT-REPRODUCTION.md#prepare-this-checkout)
before running the managed suites with
`dotnet test tests/<suite>/<suite>.csproj -c Release`. That guide also supplies a
fresh-build Native AOT command and complete15304-record comparison against the
committed reference, without requiring the ignored local execution artifacts.
The exact [acceptance command manifest](../artifacts/vscode-allocation-rollout/integration-16/regressions/acceptance-summary.json)
links the17 publication commands,83 semantic commands, foundation commands and
120 noemit chunk commands (up to128 records each), plus the explicit dependency-build order. Existing
corpora and original expected outputs are unchanged. Retained frozen artifacts can
be rerun directly; a fresh build has new commit metadata and must establish its own
binary hashes. Avoid committing during publication or implicitly rebuilding a
previously frozen compiler through a harness dependency.

Execution lessons: completing connected source families avoided per-edit test/profile
loops. Deferring the broad gate exposed accumulated cold-observer binding debt;
coordinated repair batches and finite affected retries closed it without a third full
managed corpus pass. Commit-driven SourceLink churn wasted two publication attempts;
explicit frozen-artifact reuse resolved it. One source-family independent reviewer
was sufficient; reusing the three Sol lanes reduced repeated context transfer.
The final source-role review found one additional concrete adaptation cost:
about12.97MB of sampled Char boxing at generic Latin1 range checks, shared by
concatenation, builders, hashing and ingress. The whole-family follow-up is accepted separately in integration17 below;
the frozen16 measurements do not cover that later source. No repeated profile
or benchmark is justified solely for this small change.

No token or monetary usage measurement is available. These process observations
are not estimates of subscription cost.

### Shared Latin1 guard follow-through

The final profile's concrete residual was addressed across all14 generic range-guard
sites in10 files. One cached standard `SearchValues<char>` covers exactly
U+0000..U+00FF; borrowed `ContainsAnyExcept` calls preserve null/empty handling,
strict typed-path rejection, byte-key equality/hash behavior, and the existing
builder/concatenation Unicode and surrogate fallback. Source and the extension to
one existing shared test body passed independent review; its six cases and original
assertions were preserved.

Integration17 production builds with zero warnings/errors. The actual Linux x64
Native AOT VS Code run matches ten complete diagnostics and available pinned chains:
**19.83s wall,72.27s CPU,9,441,460KiB peak RSS**, expected exit1, empty stderr and
unchanged source/config/binary guards. This is the latest unprofiled observation;
it does not isolate the speed effect of this small change. The10.840235GiB allocation
capture remains attributed to integration16 and excludes this successor.

The follow-up passes4,741 managed cases (Scanner3842,Program264, selectedParser635)
and affected actual Native AOT controls: foundation3153 plus55 shared byte/text/hash,
Program78 plus11 filename, and scanner1276 complete comparison rows. Existing controls
and the single-body extension cover all256 byte values,empty/>FF/surrogate boundaries,
legacy output/lifetime and hash coherence. Four AOT executables consume the same
frozen compiler. The full15,304 Native noemit comparison also PASSED (348.10s), with complete
records, union ordering and parent relationships exact. Its aggregate output hash
matches integration16. Earlier comprehensive evidence is retained with its original
attribution; no third full managed corpus or repeat83-channel pass was claimed.
No additional allocation capture was run for this small successor. All587 source
and five Native binary guards remained exact. The [final successor evidence](../artifacts/vscode-allocation-rollout/integration-17/boundary-summary.json)
and [durable proof](../validation/results/allocation-rollout-verified.json) preserve
this attribution. Independent review found no further concrete missed Go ownership
or reuse path among the dominant sampled allocation roles. The eventual Go speed
target remains unmet; that result is distinct from completing this rollout.

## Workload and compiler identity

Target: `/home/greg/projects/vscode`, commit
`4038aefd2c4aa51457420ef764c4c2358ae0ddfa`, main `src/tsconfig.json` project.
The configuration sets `skipLibCheck: true`. Build tooling and extension projects
are not included. No VS Code files or dependencies were modified.

The requested `./node_modules/.bin/tsc` resolves to `@typescript/native/bin/tsc`,
version **7.0.2**, and runs Go. The separately installed JavaScript TypeScript
6.0.2 compiler was **not run**. The port's exact upstream revision remains
`fed0bf24149fb1ed36039212648bafdafc1ea10e`, reporting **7.1.0-dev**.
No conclusions about revision chronology are implied by these version labels.

| Run | Result | Wall | CPU (user + system) | Peak RSS |
| --- | --- | ---: | ---: | ---: |
| Accepted original native CLI | Aborted before diagnostics | 65.17s | 152.23s | 12.722 GiB |
| Native with nil-distribution repair | Exit 1; 12 diagnostics | 68.07s | 149.97s | 12.593 GiB |
| Installed Go 7.0.2 | Exit 0; no diagnostics | 11.72s | 58.68s | 8.073 GiB |
| Exact pinned Go | Exit 2; 10 diagnostics | 9.61s | 46.86s | 7.374 GiB |
| Corrected Native AOT CLI, matching noEmit | Exit 1; 10 diagnostics | 62.37s | 147.17s | 13.490 GiB |

The last two runs use the original configuration with `noEmit:true` and
`skipLibCheck:true`. Earlier .NET runs performed no output but retained configured
`NoEmit=false`; the narrow new override removes that option mismatch without moving
the config or changing type-root discovery. The pinned Go executable uses original
tracked compiler source; build-info `vcs.modified=true` reflects existing untracked
oracle/access helpers, not changes to tracked compiler algorithms.

## Repairs and verification

[TypeDiscriminator.matches](../src/TypeScript.Scanner/Checker/Relater.cs) now uses
the loop from pinned `checker/relater.go:1044`. The upstream distribution of `never`
is nil; Go's range visits zero elements, while the former C# `.Any()` threw on null.
The producer remains unchanged, and the consumer returns false for no members.
[Three regressions](../tests/Checker.NoEmit.Tests/DiscriminantNeverTests.cs) compare
complete genuine pinned-Go Program observations with real ES2022 libraries. The old
accepted DLL reproduces two crashes and passes the ordinary control; the repaired
build passes all three. The first repaired native executable also passes the three
disk cases, including exact TS2353 message/span and exit statuses 0/1/0.

[Project.Check](../src/TypeScript.Scanner/Projects/Project.cs) now follows the full
relevant upstream diagnostic aggregation order and gates. It invokes binding but
discards the raw result, allowing semantic policy to apply library skipping and
preceding directives. This removes the two extra Playwright TS2300 reports. It does
not suppress a diagnostic category. Reported-error controls remain errors.
The [ordinary CLI](../src/TypeScript.Check/Program.cs) accepts `--noEmit` for its
existing `--config` route through genuine existing-options config parsing.

The coherent Release build has zero warnings/errors. **14/14 focused xUnit cases
pass**: three discriminator cases, ten complete policy records, and one override
file-discovery assertion. The corrected native executable matches nine applicable
complete diagnostic records and passes flag-order/rejection controls. The remaining
ListFilesOnly record is API-only and tested in xUnit. Independent Sol review covered
the changed upstream functions, adjacent nil-distribution consumers, option join,
reference provenance, and assertions. See [support evidence](progress/vscode-diagnostics-support.md).

The full corrected native check has empty stderr and exactly the pinned Go's ten
TS2345/TS2322/TS2416 diagnostics concerning conditional generic returns. File owners,
codes, line/column locations, and complete rendered diagnostic chains agree.
Go's text CLI does not expose end offsets or structured related-information JSON;
those fields are not claimed equivalent in this full-workload comparison. The
focused policy tests compare complete structured records. Exit 1 for our CLI versus
2 for Go is an existing failure-status adaptation; both communicate checking failure.
The installed 7.0.2 compiler accepts the same project, so this is **not a clean VS
Code check against the pinned implementation**. Changing the pin is separate work.

## Bounded performance assessment

With aligned options and revision, this single native run takes **6.49 times** the
pinned Go wall time and **1.83 times** its peak RSS. It consumes **3.14 times** the
CPU work and averages **2.36 CPU cores**, versus Go's **4.88**. Thus both more CPU
work and lower average utilization contribute to the wall-time gap. This is an
arithmetic decomposition, not attribution to particular code or runtime phases.

Concrete areas for later investigation:

- Allocation and representation costs: Go byte strings use .NET UTF-16 characters
  with conversion allocations; link-store pages contain separately allocated
  managed cells rather than Go value-array entries. Their measured cost is unknown.
- GC and scheduling: the release Native AOT build uses optimized code and default
  workstation GC configuration. Both implementations default to four checker
  instances. Lower utilization is observed; GC cost, serial work, and ThreadPool
  scheduling contributions are not yet separated.
- Authored project-wrapper work: eager file/path/import-edge projections and
  repeated cached diagnostic collection exist before/around checking. No evidence
  establishes that this work dominates.

No optimization was attempted. The ordinary CLI does not run the full semantic
observation harness. A one-second OS sampler was attached after startup during the
already-required native check; it observes CPU/RSS/thread state, not checker phases
or GC pause/allocation totals. Single sequential runs have cache/scheduling noise,
and the .NET run has this small extra observation overhead. The next useful step
would be a targeted native CPU/allocation/GC profile to test these hypotheses,
not speculative algorithm changes. No performance tools were installed.

## Follow-up: informed performance hypothesis

After checkpoint `9629210`, the user requested a source-grounded hypothesis, without
resuming subsystem work. Two Sol investigators reused the captured workload and
examined checker adaptations and runtime evidence. No new full check, production
change, or optimization was performed.

**Leading hypothesis (moderate confidence): repeated allocation and copying in the
managed compatibility layer, together with larger retained compiler structures,
accounts for a substantial part of the extra CPU work. Workstation GC may amplify
that cost and reduce effective parallelism.** Confidence is high that the following
cost differences exist, but their shares of workload time are not yet measured.

1. **Checker cache-key construction is a primary profiling target.**
   [keyBuilder](../src/TypeScript.Scanner/Checker/Checker.cs) creates a managed
   byte list, copies it to an array, then Base64-encodes the entire key input into a
   UTF-16 string. This happens when constructing keys for lookups, including cache
   hits, in relation and instantiation paths. Upstream uses a 192-byte inline buffer
   and a fixed 128-bit XXH3 result. The adaptation preserves key content but adds
   allocations, copying, encoding, variable-length dictionary hashing/equality and
   retained key storage. The semantic algorithm can be faithful while its cost is not.
2. **Go slice compatibility changes the cost of common operations.**
   [Append and Slice](../src/TypeScript.Scanner/Runtime/ParserCompat.cs) copy lists.
   [Inference stack pushes and pops](../src/TypeScript.Scanner/Checker/Inference.cs)
   copied stack contents on both entry and exit in the measured baseline, whereas upstream can reuse
   backing capacity and adjust slice length. Object-member accumulation also uses
   these helpers. Repeated single-element append can turn linear accumulation into
   quadratic copying. This is a concrete mechanism, not a claim that it dominates
   this particular project. Global replacement by mutation would require an aliasing
   and correctness assessment; no such replacement was made.
3. **Object representation raises the baseline memory burden.**
   [NodeData](../src/TypeScript.Scanner/Ast/Ast.cs) and
   [TypeData](../src/TypeScript.Scanner/Checker/Types.cs) each allocate a separate
   wrapper and payload instead of Go's embedded base structure. AST arena allocation
   becomes individual managed allocation. A
   [link-store page](../src/TypeScript.Scanner/Core/Linkstore.cs) allocates an array
   and 256 cell objects before any separately materialized values, versus one Go
   page of inline values. More objects and indirection plausibly affect allocation,
   traversal locality and GC scanning; peak RSS alone does not measure live heap size.
4. **Workstation GC is a credible multiplier, not an established root cause.**
   The Native AOT runtime targets select workstation GC by default; the published
   debug symbols contain WKS runtime symbols and no SVR runtime symbols. This is
   stronger evidence than the absent project setting alone. The captured workload
   shows four substantial worker threads (22.63–27.39 lifetime CPU seconds each), so
   a single active checker is not the explanation. The named background-GC thread's
   5.44 CPU seconds exclude foreground work and pauses and cannot establish total
   GC cost. Testing server GC requires a separate Native AOT publish with the proper
   property; flipping an environment variable on this binary is insufficient.

The 6.49x wall ratio decomposes into 3.14x CPU work and about 2.07x lower average
utilization. At Go's observed average utilization, the measured .NET CPU work would
still imply about 30.2 seconds. This arithmetic is not a predicted optimization
result; it shows why scheduling alone is an insufficient working explanation.

A concrete secondary cost is [regular-expression grammar checking](../src/TypeScript.Scanner/Checker/GrammarChecks.cs):
for each regex literal it converts the entire source string to bytes, then the
[scanner SetText overload](../src/TypeScript.Scanner/Scanner/Scanner.cs) converts
those bytes back into a string. Upstream passes the existing source string.
Its workload contribution is unmeasured. General Unicode decoding and the authored
project wrapper remain secondary candidates: there is less evidence that either
explains the bulk of the gap. No unmeasured
percentage is assigned to any candidate. The strongest next investigation is a
CPU/allocation profile targeting cache-key creation, slice copies and runtime GC;
a controlled disposable server-GC build can separately test the collector's role.
Every experiment should retain the same pinned compiler, original config, complete
ten-diagnostic result and native execution, with paired runs to limit noise.

## Implemented follow-up: slice support and inference stacks

The user authorized implementation after the hypothesis review. From `9629210`,
[Slice<T>](../src/TypeScript.Scanner/Runtime/Slice.cs) now provides an array-backed
ordinary value-type descriptor with offset, length and capacity. Assignment copies
its header; indexing and `AsSpan` access shared storage. Two- and three-index slices
create views, append reuses capacity or allocates on growth, overlapping operations
use `Span.CopyTo`, and enumeration captures the header while reading shared elements.
Default/nil and allocated-empty slices remain distinct. No pooling is involved.

[InferenceState](../src/TypeScript.Scanner/Checker/Inference.cs) uses this type for
only its source and target stacks. Push, pop and pooled-state reset preserve the
upstream function boundaries and now retain/reuse storage. The shared recursion-depth
helper in [Relater](../src/TypeScript.Scanner/Checker/Relater.cs) borrows a span from
either the new slice or existing lists. Its intersection loop retains upstream
short-circuit behavior without capturing a span in a delegate. No other lists,
cache keys or GC settings were migrated.

**Boundary:** this is the first validated adoption, not general Go-slice parity
throughout the port. Nominal growth follows Go 1.27.1 `runtime/slice.go:nextslicecap`,
with the Go license retained. CLR arrays do not reproduce Go allocator size-class
rounding, so automatic capacity sequences and alias effects conditional on those
exact sequences are not guaranteed. Explicit capacities and their sharing rules
are supported; the migrated inference stacks do not observe the growth sequence.
CLR array-size limits and .NET bounds exceptions are adaptations. Clear/clone,
insert/delete/sort, pooling and replacement of the old list adapters remain outside
this implementation.

Verification completed:

- 23 authored slice-semantics xUnit cases pass. The exact same assertion bodies pass
  in the actual Linux x64 Native AOT [Slice.Validation](../validation/Slice.Validation)
  executable. These are Go-spec conformance tests, not ported TypeScript test bodies.
- The authored [Go witness](../tools/slice-semantics-reference/main.go) executes on
  genuine Go 1.27.1 and validates 16 corresponding public observations. It does not
  compare automatic growth capacities or stand in for upstream compiler tests.
- All 84 existing type-system cases pass (77 complete semantic records and seven
  shared checks); all 14 prior crash/diagnostic-policy cases remain green.
- Twelve selected original compiler inference fixtures pass unchanged complete Go
  observations, including existing union-order and parent-pointer assertions. They
  cover recursive mapped/conditional inference, tuple rest, reverse mappings,
  contravariance, generic local functions, default and constrained type parameters.
  These are reused compiler inputs compared through the existing no-emit harness.
- Total managed execution for this change: 133 passed, zero failed/skipped. This is
  focused regression evidence, not another complete whole-checker acceptance run.
- Sol support independently reviewed the complete production slice and upstream
  inference join; Sol core independently reviewed the shared assertions, native
  consumer and genuine Go witness. No unresolved review findings remain.

Both native candidates completed the VS Code check, exercising the production
`Slice<Checker.Type>` specialization in addition to the direct `Slice<int>` checks.
All four balanced sequential runs (baseline, candidate, candidate, baseline) retained
all ten complete .NET diagnostic records and the available pinned-Go message/location
comparison. No full semantic observer, process sampler, concurrent build, GC change
or cache-key change participated in timed runs.

| Uninstrumented run | Wall | CPU | Peak RSS (KiB) |
| --- | ---: | ---: | ---: |
| Baseline 1 | 62.07s | 148.12s | 14,059,144 |
| Candidate 1 | 70.28s | 158.94s | 13,211,260 |
| Candidate 2 | 64.97s | 158.09s | 13,225,392 |
| Baseline 2 | 62.27s | 136.08s | 13,221,912 |
| Baseline mean | 62.17s | 142.10s | 13,640,528 |
| Candidate mean | 67.625s | 158.515s | 13,218,326 |

**The candidate did not improve speed:** mean wall time rose 8.77% and CPU time
11.55%. Mean peak RSS was 3.10% lower, but baseline variation overlaps candidate
measurements; this is not persuasive evidence of a memory improvement. Two runs per
binary do not establish statistical significance or isolate a cause. The bounded
implementation and correctness verification are complete, but this is not an
accepted performance improvement or a reason for broader migration. The candidate
remains uncommitted for evaluation.

The user correctly asked why allocation/CPU profiling had not preceded this change.
Source inspection established costly operations, not their workload contribution.
Profiling should guide the next decision; do not expand this migration or tune other
subsystems to hide the negative measurement. The slice machinery is now available
for controlled investigation, with unchanged expected semantic results.

Before the authorized profiling follow-up below, read-only tool assessment found `perf`, `dotnet-trace`, `dotnet-counters`,
`dotnet-gcdump`, `heaptrack` and `valgrind` absent. The installed Native AOT targets
support EventPipe when publishing with `-p:EventSourceSupport=true`, consistent with
[official Native AOT diagnostics documentation](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/diagnostics); the measured
binary has it disabled. A separate diagnostic native build plus a locally installed
`dotnet-trace` can therefore be attempted for GC/allocation tracing. Confirm actual
sampled allocation/type/stack events before claiming attribution: Native AOT's
provider coverage is partial, and this is not an exact allocation census. An
uninstrumented native timing remains separate from profiling overhead. A Release
CoreCLR allocation trace is a fallback for locating managed allocation sites, not
proof of Native AOT costs. Native CPU sampling can use `perf` and existing `.dbg`
symbols; own-process user-mode access must be probed on this host. No profiler was
installed or run during this implementation. Profile the baseline before expanding
or tuning the candidate.


To reproduce the new abstraction and focused managed checks:

```bash
dotnet test tests/Checker.NoEmit.Tests/Checker.NoEmit.Tests.csproj -c Release \
  --filter 'FullyQualifiedName~SliceSemanticsTests|FullyQualifiedName~DiscriminantNeverTests|FullyQualifiedName~ProjectDiagnosticPolicyTests'
dotnet test tests/TypeSystem.Tests/TypeSystem.Tests.csproj -c Release
tools/.cache/go/bin/go run tools/slice-semantics-reference/main.go
dotnet publish validation/Slice.Validation/Slice.Validation.csproj -c Release \
  -r linux-x64 -p:PublishAot=true -o artifacts/vscode-slice-migration/native/slice
artifacts/vscode-slice-migration/native/slice/Slice.Validation
```

The optional native compiler shim property below applies to this publish too.
The ordinary CLI commands below also reproduce the migrated production path when
built from the current source. Local evidence is retained in
[managed results](../artifacts/vscode-slice-migration/managed/),
[native publish and direct checks](../artifacts/vscode-slice-migration/native/),
[original inference selection](../artifacts/vscode-slice-migration/focused-original/selection.json),
[Go witness provenance](../artifacts/slice-inference/go/provenance.json), and
[balanced timing runs](../artifacts/vscode-slice-migration/timing/).

## Measured profiling follow-up (2026-10-10)

Profiling uses the committed `9629210` baseline, not the uncommitted inference-slice
candidate. The original configuration, genuine libraries, `skipLibCheck`, and no-emit
invocation remain unchanged. Three existing GPT-6.1 Sol agents handled capture,
analysis and independent interpretation; Astra coordinated serial workload runs.
The user installed `linux-perf`; `dotnet-trace` was installed in the ignored local
cache. No production optimization or GC-mode change was made.

The native CPU capture uses the existing, unchanged Native AOT executable and its
symbols: `perf record -F 99 -e cpu-clock:u --call-graph dwarf,8192`. It collected
12,865 samples with zero reported loss. All ten complete diagnostic JSON records
match the previously verified baseline. The profiled run took 67.86s; this is not
an additional uninstrumented benchmark. Native CPU evidence and exact period-weighted
aggregation are in [the CPU analysis](../artifacts/vscode-profiling/native-cpu-baseline/analysis.json).

**The strongest measured bottleneck is workstation-GC runtime work:** identified
`WKS::` leaf functions account for 51.74% of sampled user CPU, including allocator
and locking paths. The GC `enter_spin_lock_msl_helper` alone accounts for 17.69%
(a subset of that total), and background marking for 8.36%. These are exclusive
sampled CPU shares, not percentages of wall time or stop-the-world pause time.
Kernel CPU and sleeping threads are excluded. The sample does not establish which
allocation producer caused a particular collection or lock wait.

This host resolves native leaf symbols but cannot unwind most worker-thread call
chains. Consequently the native graph must expose that limitation instead of
inventing callers. The `/proc/schedstat` warning also means scheduler statistics
were unavailable; no off-CPU attribution is claimed. CPU profiling still provides
valid leaf hotspot evidence, independently reviewed against exact sample periods.

A separate Native AOT build of the same baseline enabled only EventSource support
for GC tracing. The GC-only full run preserved identical diagnostic JSON and has
zero reported loss, zero incomplete collections, and zero unmatched suspension pairs.
There are 2,614 completed collections and 2,621 GC/GC-preparation suspension intervals.
Their merged union is **36.739s of the 61.950s trace (59.30%)**. This interval spans
suspension initiation through restart completion, including handshake/restart overhead;
it is not GC CPU time. It is also a separate run from the CPU capture. Most collections
were generation 0 or 1 (1,329 and 1,249 respectively). See the
[native GC analysis](../artifacts/vscode-profiling/native-gc-baseline/analysis.json).

A separate Release CoreCLR baseline supplies allocation attribution because the
Native AOT EventPipe smoke had unnamed allocation types and no allocation stacks.
Its full trace has 412,722 allocation ticks, all with type names and at least one resolved frame per captured stack, and zero
reported event loss. The sum of sampled interval weights is
48.443 GiB; strings account for approximately 12.758 GiB. These are estimates of
allocation traffic, not live memory, exact per-type allocation totals, or measurements
of Native AOT allocation volume. Managed sampled-thread-time events include waiting
threads and are not used as CPU percentages.

Source-grounded allocation candidates now have workload evidence. JSDoc-associated
stacks account for approximately 6.689 GiB of inclusive sampled allocation weight;
[the JSDoc parser](../src/TypeScript.Scanner/Parser/Jsdoc.cs) repeatedly slices source
text through [the string compatibility adapter](../src/TypeScript.Scanner/Runtime/ParserCompat.cs),
which uses allocating `Substring` operations. Regex checking also appears among
large allocation stacks, consistent with its full-source byte/string conversions.
Cache-key construction and list copying remain observed candidates, but the trace
does not establish either as the sole or dominant cause of overall slowdown.

The complete `invokeOnce` subtree accounts for 4.939 GiB, including unrelated
nested inference work. A narrower leaf/direct-`GetRange` selection estimates
2.146 GiB (inlining-sensitive); it is still not a demonstrated saving from the slice
candidate. Inclusive families overlap and must not be added. No candidate allocation
profile was collected: this baseline investigation **does not explain why the slice
candidate's earlier timings regressed**, nor prove that retained references caused it.

The pinned Go compiler was profiled through its original `--pprofDir` support,
without rebuilding or modifying compiler code. It preserved all ten full diagnostic
text records. The profiled invocation took 10.72s, with 54.48 CPU seconds represented
in the CPU profile; neither is a new uninstrumented speed comparison. The allocation
profile estimates 5,557,988,034 cumulative bytes (5.176 GiB) and 4,736,809,691 retained
bytes (4.411 GiB). These are distinct `alloc_space` and `inuse_space` measures, not RSS.
The sampling period is 524,288 bytes. Upstream writes the profile without forcing a
GC; Go's memory profile may be up to two GC cycles old, so the final omitted amount
is unbounded by this capture. The roughly 48.44-versus-5.18 GiB estimates suggest a
large allocation gap, **not an exact 9.36x native allocation ratio**. See the
[Go totals and limitations](../artifacts/vscode-profiling/go-baseline/profile-totals.json).
Go's profile prominently includes map operations, arena/slice growth, object
allocation and GC scanning. Its GC percentage is not equated to the broader .NET
`WKS::` allocator/locking category.

The measured evidence promotes Server GC to a high-priority controlled experiment:
republish the same baseline with `-p:ServerGarbageCollection=true`, then compare
uninstrumented time, CPU, peak memory and unchanged diagnostics. The current AOT
executable links workstation GC; changing an environment variable alone is not the
proposed comparison. No Server GC speedup has been measured. Its potential benefit
does not eliminate the need to reduce costly compatibility allocations.


Interactive graphs (open the SVG directly in a browser, click frames to zoom, use
Search to highlight names):

- [.NET Native AOT CPU](../artifacts/vscode-profiling/native-cpu-baseline/native-cpu-flamegraph.svg):
  deliberately flat thread-to-leaf view; worker call chains are unavailable.
- [.NET CoreCLR allocations](../artifacts/vscode-profiling/managed-baseline/managed-allocations-flamegraph-browse.svg):
  full sampled allocation stacks, weighted estimated bytes; not native CPU time.
- [Pinned Go CPU](../artifacts/vscode-profiling/go-baseline/go-cpu-flamegraph-browse.svg).
- [Pinned Go allocations](../artifacts/vscode-profiling/go-baseline/go-allocations-flamegraph-browse.svg):
  `alloc_space`, with the sampling/final-cycle limitations above.

Widths are shares of each graph's own total, not directly comparable absolute widths
across runtimes. Browsing graphs shorten parameter/generic labels and omit boxes
narrower than one pixel, while preserving the complete root weight; full folded
stacks and provenance remain beside them. XML, interactive-control presence and
root weights were validated; the integrated browser was unavailable for a click
smoke test. Rendering uses original FlameGraph scripts pinned to
`41fee1f99f9276008b7cd112fca19dc3ea84ac32`, with license notices retained.

Four full workload captures were run sequentially (native CPU, managed allocation,
native GC, pinned Go CPU/allocations), plus small coverage smokes. Existing Sol
agents were reused for capture, analysis and independent review. Native allocation
EventPipe coverage was tested before relying on the managed fallback. Invalid
worker-stack visualization and misleading sample/period labels were caught and
corrected before delivery. No token or monetary usage measurements are available.
No new correctness tests were needed for this read-only investigation; each full
run retained its complete previously verified diagnostics. No optimization was
applied, and the slice candidate remains uncommitted.

Capture reproduction from the retained local baseline binaries (these deliberately
exclude the current uncommitted slice changes):

```bash
perf record -F 99 -e cpu-clock:u --call-graph dwarf,8192 \
  -o artifacts/vscode-profiling/native-cpu-baseline/repeat.data -- \
  artifacts/vscode-diagnostic-fix/native/tscheck \
  --config /home/greg/projects/vscode/src/tsconfig.json --noEmit --json
perf report -i artifacts/vscode-profiling/native-cpu-baseline/repeat.data \
  --stdio --no-children --call-graph none

tools/.cache/diagnostics/dotnet-trace collect \
  --profile gc-verbose,dotnet-sampled-thread-time --buffersize 512 --show-child-io \
  --output artifacts/vscode-profiling/managed-baseline/repeat.nettrace -- \
  dotnet artifacts/vscode-profiling/baseline-src/src/TypeScript.Check/bin/Release/net10.0/tscheck.dll \
  --config /home/greg/projects/vscode/src/tsconfig.json --noEmit --json

tools/.cache/diagnostics/dotnet-trace collect \
  --profile gc-collect --buffersize 256 --show-child-io \
  --output artifacts/vscode-profiling/native-gc-baseline/repeat.nettrace -- \
  artifacts/vscode-profiling/native-baseline/tscheck \
  --config /home/greg/projects/vscode/src/tsconfig.json --noEmit --json

artifacts/vscode-diagnostic-fix/pinned-cli/tsc \
  --project /home/greg/projects/vscode/src/tsconfig.json \
  --noEmit --skipLibCheck --pretty false \
  --pprofDir artifacts/vscode-profiling/go-repeat
# Use the new PID-named files for a repeat; these paths inspect the saved capture:
tools/.cache/go/bin/go tool pprof -top -sample_index=alloc_space \
  artifacts/vscode-profiling/go-baseline/2839709-memprofile.pb.gz
tools/.cache/go/bin/go tool pprof -top \
  artifacts/vscode-profiling/go-baseline/2839709-cpuprofile.pb.gz
```

The .NET baseline binaries come from exact commit `9629210` in the retained detached
worktree. The GC diagnostic native build additionally uses `-p:EventSourceSupport=true`;
CPU profiling uses the original native binary without that flag. Local tooling is
`perf 7.0.14`, `dotnet-trace 10.0.750501`, SDK 10.0.401/runtime 10.0.12, and Go 1.27.1.
The profiled machine is Linux x64 under WSL2, with 16 exposed logical CPUs and roughly
31 GiB of memory. Tool commands, hashes, profiles and rendering provenance are
retained under [the profiling evidence](../artifacts/vscode-profiling/). These local
artifacts and profiler tools are ignored by Git. See the compact
[combined findings](../artifacts/vscode-profiling/profile-findings.json) for sample
coverage and caveats; no claim of exhaustive wall-time or allocation attribution is made.


## Shared text views: verified implementation and measured improvement

The user authorized JSDoc-first implementation and expansion when measurements
justify it. The comparison baseline includes the pre-existing uncommitted inference
slice candidate, frozen as native SHA256 `d92a35193993a69f0d2619a192a12ea673f3917602fa6b6ba5e6a2eedec85418`.
This prevents attributing inference-slice differences to text views. GC mode, SDK,
project/options and source inputs remain unchanged. The earlier slice experiment's
negative performance result is not reversed by this separate experiment.

[ByteStringView](../src/TypeScript.Scanner/Runtime/ByteStringView.cs) is an immutable
string/offset/length header over the existing one-character-per-byte source
representation. It uses standard readonly spans/memory and shares storage when
sliced. There is no implicit conversion back to string: token/AST/public outputs
materialize explicitly. Parser/scanner internal text fields and necessary helpers use
the view. Upstream JSDoc save, prefix slice, scanning and restoration ordering remain
unchanged. Public Unicode APIs, byte offsets and AST string storage are preserved.
This follows Go string slicing more closely without changing source encoding or
introducing mutable generic slices into string handling. Source mappings classify
this new compatibility support separately from directly mapped upstream ports.

The first packet passes 18,726 parser and 3,838 scanner xUnit cases; 3,153 existing
shared foundation assertions and 17 text-view cases under actual Linux x64 Native
AOT. The 17 cases contain 14 authored controls and reuse three unchanged upstream
assertion bodies. Counts overlap in intent and are not unique upstream test functions.
No existing expectations changed. Independent Sol review covers production changes
and shared assertion/native wiring in both directions.

The preliminary uninstrumented native pair is **67.97 → 57.46 seconds** wall,
**156.68 → 138.56 seconds** CPU, and **13,334,384 → 11,701,452 KiB** peak RSS,
with all ten diagnostics unchanged. This one-pair observation justified the next
narrow expansion; it is not the final balanced performance result.

Matched managed allocation profiles reinforce the result:

| Sampled allocation weight | Frozen baseline | JSDoc-only | Final (including regex) |
| --- | ---: | ---: | ---: |
| Total | 46.334 GiB | 38.649 GiB | 36.928 GiB |
| JSDoc-associated stacks | 6.678 GiB | 0.232 GiB | 0.230 GiB |
| Visible JSDoc Substring stacks | 1.073 GiB | 0.0094 GiB | 0.00765 GiB |
| Regex grammar-check stacks | 1.683 GiB | 1.694 GiB | 0.00687 GiB |
| String allocations | 12.775 GiB | 4.984 GiB | 3.858 GiB |
| Invocation-cache control | 2.876 GiB | 2.880 GiB | 2.904 GiB |

Total sampled allocation falls about **20.3%**. These are sampled interval weights,
not exact bytes or Native AOT allocation totals; stack subsets overlap. There is one
capture per variant. All three report zero event loss/nontruncated data/no stack
overflow and preserve all ten diagnostic records; each contains one unstacked tick.
Incomplete GC restart pairing means these allocation captures do not establish GC
pause time. The stable invocation-cache control supports attribution to the selected
source-copying paths. JSDoc-only and final captures show the additional regex-path
allocation reduction, but no balanced JSDoc-only-versus-final native benchmark was
run, so its marginal wall-time effect is not separately established.
Evidence: [first milestone](../artifacts/vscode-jsdoc-view/preliminary-findings.json)
and [final consolidated findings](../artifacts/vscode-jsdoc-view/final-findings.json).

The extension removes a second profile-supported whole-source copy at the regex
checker/scanner handoff in [GrammarChecks.cs](../src/TypeScript.Scanner/Checker/GrammarChecks.cs),
sharing the existing raw byte string instead of converting the whole file to a byte
array and back for each regex. Source review also exposed an existing Unicode
regex-diagnostic defect: the public scanner callback decoded byte-string arguments
before the checker consumed them as raw byte strings. Pinned Go displays `'â'` in
TS1532, whereas the old native executable displayed U+FFFD. Setup and cleanup now
use the internal raw-byte callback. This correctness repair is separate from the
copy-removal motivation; diagnostic expectations were not weakened. Three authored
complete Program records (invalid Unicode, invalid ASCII suggestion, valid Unicode)
are preserved in [the shared regression resource](../tests/Checker.NoEmit.Tests/RegexByteArgumentCases.jsonl.gz).
[Capture provenance](../artifacts/vscode-jsdoc-view/regex-unicode-witness/three-case-provenance.json)
records the original requests/options, untouched reference outputs and hashes.

Final source is frozen at [snapshot 2](../artifacts/vscode-jsdoc-view/build/snapshot-2/snapshot.json).
The final managed gate passes 23,056 case executions with zero failures/skips: parser
18,726, scanner 3,838, type system 84, selected binder 122, selected checker 262,
21 unchanged complete original noemit records and three authored regex records.
Fresh pinned-Go comparisons against rebuilt Linux x64 Native AOT parser/scanner
executables agree on all 17,723 source requests each. Native default foundation
3,153 and additional text-view 17 checks pass, as do the 21 complete original noemit
records. All three native regex controls also pass. The sequential final gate takes
231.973 seconds and detects no source/input drift; see [stage commands and results](../artifacts/vscode-jsdoc-view/final-regressions/report.json).
The final uninstrumented **B,C,C,B** comparison passes complete diagnostics on all
four runs, with unchanged binaries/configuration:

| Metric | Baseline mean (range) | Final candidate mean (range) | Change |
| --- | ---: | ---: | ---: |
| Wall seconds | 65.72 (63.47–67.97) | 54.76 (53.86–55.66) | −16.68% |
| CPU seconds | 147.245 (138.25–156.24) | 126.57 (124.24–128.90) | −14.04% |
| Peak RSS, KiB | 13,261,720 (13,187,788–13,335,652) | 10,978,576 (10,926,064–11,031,088) | −17.22% |

[Full timing evidence](../artifacts/vscode-jsdoc-view/timing-final/comparison.json)
qualifies every run against the complete .NET diagnostic DTO and all available
pinned-Go owner/code/location/rendered-chain fields. Exit 1 is expected because the
pinned revision reports ten diagnostics for this VS Code checkout; no internal error
or stderr is present. Two observations per binary are useful evidence, not a precise
statistical estimate or a general-project guarantee. No sampler, build or oracle ran
concurrently with these timings. Final allocation capture and analysis also pass.
First milestone binaries/profiles remain separate from final candidate outputs.
Independent source review found no additional persistent whole-source retention:
SourceFile already retains its source, token/AST outputs remain strings, temporary
JSDoc views are restored, and the regex scanner clears text after scanning. A future
long-lived exported view could retain backing; no such escape is introduced here.
This change does not replace the existing UTF16-backed byte representation, pooling,
cache keys or GC mode. It does not eliminate legitimate token/output allocations.

### Scope, effort and next investigations

Astra coordinated three reused GPT-6.1 Sol agents: production implementation,
shared tests/independent review, and profiling/measurement. There were three explicit
central production-library build invocations, including one initial compile failure
from two narrow string-to-view adaptation sites. The initial JSDoc milestone passed
its focused gates before expansion; the final consolidated regression ran once in
231.973 seconds. Six uninstrumented full VS Code runs comprise the preliminary pair
and final balanced four-run set; the final four total 240.96 wall seconds. Three
matched CoreCLR captures cover the same baseline, JSDoc-only and final variants.
Heavy jobs were serialized and immutable binaries preserved between milestones.
A single-worker native publish avoided the copy retries seen during the first gate.
No additional packages, permissions or user intervention were needed for this work.
Final CLI managed/native builds take 13.505 seconds; the final profiled workload takes
59.321 seconds, which is not an uninstrumented timing sample. Token usage and monetary
cost are unavailable; no estimate is asserted.

The targeted text-sharing change is complete and performance-qualified. Broader
string-storage migration and full compiler optimization remain separate work. The
performance-qualified extension is the checker regex handoff; no further consumers
were migrated speculatively. Server GC, cache-key allocation, output-string
materialization, and representation size remain separate candidates for measured
investigation. The earlier generic Slice inference experiment remains independently
uncommitted with its negative timing result; this work does not validate expanding
that experiment. The current text-view changes are also uncommitted.

### Reproducing the text-view checks

The new assertions are ordinary xUnit tests; original corpus files and expectations
remain unchanged. From the repository root:

```sh
dotnet test tests/Parser.Tests/Parser.Tests.csproj -c Release
dotnet test tests/Scanner.Tests/Scanner.Tests.csproj -c Release
dotnet test tests/Checker.NoEmit.Tests/Checker.NoEmit.Tests.csproj -c Release --filter FullyQualifiedName~RegexByteArgumentTests

dotnet publish validation/Foundation.Validation/Foundation.Validation.csproj -c Release -r linux-x64 -p:PublishAot=true -p:CppCompilerAndLinker="$PWD/tools/.cache/native/bin/cc" -m:1 -o artifacts/text-view-reproduction/foundation
artifacts/text-view-reproduction/foundation/Foundation.Validation --text-view
dotnet publish validation/Program.Validation/Program.Validation.csproj -c Release -r linux-x64 -p:PublishAot=true -p:CppCompilerAndLinker="$PWD/tools/.cache/native/bin/cc" -m:1 -o artifacts/text-view-reproduction/program
artifacts/text-view-reproduction/program/Program.Validation --regex-byte-arguments tests/Checker.NoEmit.Tests/RegexByteArgumentCases.jsonl.gz
```

The [final regression report](../artifacts/vscode-jsdoc-view/final-regressions/report.json)
records every exact command, count and stage duration, including the broad fresh-Go
comparisons. Its [driver](../artifacts/vscode-jsdoc-view/run-final-regressions.py) accepts
`--snapshot artifacts/vscode-jsdoc-view/build/snapshot-2/snapshot.json --output <fresh-directory>`.
The [final performance build driver](../artifacts/vscode-jsdoc-view/publish-final-performance.py)
and [timing driver](../artifacts/vscode-jsdoc-view/compare-native-final.py) preserve frozen
binary hashes, the B,C,C,B order and complete diagnostic qualification. They deliberately
refuse occupied output directories. The timing invocation is:

```sh
python3 artifacts/vscode-jsdoc-view/compare-native-final.py --candidate artifacts/vscode-jsdoc-view/native/final-cli/tscheck
```

These saved performance scripts depend on the named local frozen baseline artifacts
and original VS Code checkout. They are experiment reproduction aids, not new compiler
interfaces. Allocation captures use matched CoreCLR builds and a separate run from
uninstrumented Native AOT timing. No observation-only compiler mode is enabled.

## Filename byte-role sharing: verified

The next bounded change follows the completed JSDoc/regex result. Its baseline is
the frozen final native CLI `929a14fef2528450ff45e12b68f3762292869327e407f7861a274623109c8d10`
and managed build under `artifacts/vscode-jsdoc-view/managed-final`. Earlier slice
and text-view changes remain unchanged, allowing the filename adaptation to be
measured separately. The existing final managed trace supplies baseline allocation
evidence; it is not recaptured merely because another milestone began.

[Selection evidence](../artifacts/vscode-text-followup/selection.json) identifies
1.818 GiB sampled allocation weight in the union of Latin1 conversion and Go-string
UTF8 encoding, including 1.624 GiB under selected filename-consuming methods. These
are inclusive stack groups, not a claim that every byte in either group is avoidable.
The disk [Project adapter](../src/TypeScript.Scanner/Projects/Project.cs) changes
filenames from internal Go-byte strings into its existing public Unicode role. Later
[SourceFile queries](../src/TypeScript.Scanner/Ast/Ast.cs) encode them back for Program
resolution/mode/reference operations. Upstream Program directly reuses its filename.

The implementation retains an immutable Unicode/raw-byte filename pair per source
file. Project's role conversion seeds it from the original raw value. A scalar-role
query reuses it only when the current filename string identity matches; replacement
recomputes it, while raw-role reads remain direct. The immutable pair is published
atomically for concurrent readers; concurrent mutation of source files is not newly
supported. No global interning or source-text retention is added. Public filenames,
parse options, typed path identity and checker algorithms remain unchanged.

Clone/update/reparse paths continue to use their own source-file options: new scalar
files encode once, raw-role files retain their original raw string. Unicode promotion
uses the existing strict UTF8 decoder, which rejects malformed bytes before mutation;
it does not seed a silently corrupted filename. This is a managed representation
adaptation around directly ported source behavior, not a new upstream cache algorithm.
Source mapping records it as such.

Alternative cache-key work is deferred: `invokeOnce` already has a typed key; its
large inclusive stack contains nested checker work. The actual key builder has
1.590 GiB inclusive sampled weight, but replacing its complete-preimage/Base64 keys
with Go's inline buffer/XXH3 representation requires additional compatibility work.
The tiny isolated ToArray-to-span option accounts for only about 0.071 GiB. Remaining
visible JSDoc tag-scan slicing is likewise small (about 0.013 GiB). We did not make
blanket replacements or redesign cache observers based on these observations.

The focused correctness gate passes in 195.507 seconds: 447 managed cases
(Program 264, project 75, declaration 77, filename 11, regex 3, text-view 17), with
zero failures/skips. Actual Linux x64 Native AOT passes filename 11, Program 78,
regex 3, text-view 17, project controls 27 and 48 unchanged complete Go project
records. Existing Program/project Unicode-path and diagnostic-owner comparisons
remain intact. No broad parser/scanner rerun was needed for this filename-only
change; their preceding native acceptance remains preserved. See [commands and
results](../artifacts/vscode-text-followup/filename-regressions/report.json).
Independent source/shared-test review passes. The balanced B,C,C,B native comparison
retains all ten complete diagnostics on all four runs:

| Metric | Baseline mean (range) | Filename candidate mean (range) | Change |
| --- | ---: | ---: | ---: |
| Wall seconds | 62.77 (61.77–63.77) | 60.715 (59.96–61.47) | −3.27% |
| CPU seconds | 155.745 (144.31–167.18) | 151.625 (136.00–167.25) | −2.65% |
| Peak RSS, KiB | 10,934,804 | 10,933,094 | effectively unchanged |

Only two observations per binary were taken, with substantial CPU variation. This is
a modest descriptive timing result, not a statistical speed guarantee. Compare these
contemporaneous paired means, not earlier campaign times taken under different runs.

The matched CoreCLR allocation evidence more strongly confirms the mechanism:
conversion-union sampled weight falls **1.818 → 0.00409 GiB (−99.78%)**, total
**36.928 → 35.181 GiB (−4.73%)**, and strings **3.858 → 2.715 GiB**. The selected
filename-consumer conversion stacks have no visible candidate samples; that does not
prove literal zero allocations. These subsets overlap and are sampled, not an exact
Native AOT census. The trace has zero reported loss/truncation/overflow; no GC-pause
claim is derived from its incomplete restart pairing.

[Final findings](../artifacts/vscode-text-followup/filename-final-findings.json)
preserve source-at-build, executable and input identities. Final native SHA256 is
`d4fb926ac90c0531dd94d304a3e062442bc15c80fe3967a613f64813fe455c59`.
The source snapshot and a complete worktree diff were retained before the next JSDoc
source-only edits began. Frozen executables/configuration remained unchanged during
all measurements; no build/test workloads overlapped them. Three reused Sol agents
performed implementation, shared assertions/review and measurement. One central
library build passed (9.139 seconds); final CLI builds took 13.488 seconds, four native
runs 246.97 seconds, and the separate profiled workload 62.408 seconds. No new user
intervention/packages were required; tokens/cost are unavailable. A stray initial
character in the previously accepted view file was restored only after its exact
baseline hash was verified; this was not a semantic change.

The new shared checks run through the filename xUnit theory and the published
Program validation mode:

```sh
dotnet test tests/Checker.NoEmit.Tests/Checker.NoEmit.Tests.csproj -c Release --filter FullyQualifiedName~SourceFileNameTests
artifacts/vscode-text-followup/filename-regressions/native/program/Program.Validation --filename-role-checks
```

The [focused regression report](../artifacts/vscode-text-followup/filename-regressions/report.json)
records exact native publish/run commands and unchanged upstream comparisons. Raw
artifacts are under [filename follow-up evidence](../artifacts/vscode-text-followup/).
The representation change is accepted; broader speed parity is not claimed. Changes
remain uncommitted alongside the preserved earlier experiments.

## Comprehensive JSDoc comment/link text sharing: verified implementation

The user clarified that started text-sharing work should be comprehensive and that
longer-term allocation behavior should approach upstream Go across the compiler.
The next packet is therefore source-led, including cold JSDoc text paths. Profiles
remain prioritization and measurement tools, not a substitute for accounting for the
selected conversion. Matching Go's overall speed remains an unachieved measured target.

The [source inventory](../artifacts/vscode-text-followup/jsdoc-slice-inventory.json)
accounts for every explicit slice in pinned `jsdoc.go`: seven text slices and fourteen
collection-header slices, plus associated scanner/token/trim/normalization helpers.
This packet completes comment/link text sharing through:

- All seven explicit text slices and related whitespace/margin/trim operations.
- Borrowed JSDoc comment tokens and source-text fragment access, preserving scanner
  reset, mark/rewind and public materialization behavior.
- Four JSDoc-specific comment/link AST text-fragment models retaining readonly source
  views until an explicit public string-list projection is needed.
- Correct public mutable-list/null/set/clone/update behavior: public projection becomes
  the canonical mutable list and shared clones continue observing that list.
- Direct final-string joins and relevant temporary scanner/helper slices, with a
  source-based disposition for materialization that remains necessary.

The generated AST adaptation must preserve current source-level access and assertion
behavior. A field-to-property bridge does not promise compatibility with already
compiled external field access or reflection; no such ABI claim is made. Actual
Native AOT execution remains mandatory. Holding a detached JSDoc fragment may retain
its original source backing, matching Go's sharing semantics; this must be assessed
and documented rather than hidden behind an allocation count.

Compiler-wide identifier strings and symbol-table keys remain a separate representation
dependency. Diagnostic/public string outputs and actual concatenation/join results
still materialize. The fourteen collection-header slices are explicitly inventoried
but their existing list adaptations are not changed in this text-sharing packet.
Thus this is comprehensive JSDoc comment/link text sharing, not a claim to eliminate
all scanner allocations, every JSDoc allocation or all compiler string copies.

Acceptance requires independent review of omissions and alias/lifetime semantics,
shared xUnit/native controls, retained original JSDoc assertion bodies and full
compiler-input records, broad fresh pinned-Go scanner/parser comparisons, retained
semantic coverage, actual Linux x64 Native AOT and unchanged full VS Code diagnostics.
Measure the new build against the immutable filename candidate. The preceding
filename benchmark/profile finished on frozen binaries while source-only work
proceeded here; no builds/tests overlap timed workloads. Evidence lives under
[the completion packet](../artifacts/vscode-jsdoc-fragments/).

The [final correctness gate](../artifacts/vscode-jsdoc-fragments/final-regressions/report.json)
passed in 378.387 seconds with no frozen-source or input drift. It executed 23,496
managed assertions with zero failures/skips, native foundation 3,153 + 17 text-view
+ 13 fragment controls, fresh complete Go/native parser and scanner comparisons of
17,723 records each, 21 complete no-emit records, 78 Program controls + 11 filename
+ 3 regex cases, and 27 Project controls + 48 complete Project records. These are
executed data-driven assertions, not that many distinct original upstream test bodies.
The new thirteen fragment cases are authored compatibility checks; original upstream
assertions and reused compiler-input differential records remain separately identified.

The narrow [generator update](../tools/generate-ast.py) reproduces the four affected
model families and internal factories/updates. Eleven compared excerpts were token-identical
in scratch output; the live generated file was not overwritten. This does not claim
full-file reproduction across unrelated historical generator drift. Independent Sol
review covered the full affected function families, scanner save/restore state, shared
mutable projection and detached-node retention. Four uninstrumented VS Code runs preserve the same ten complete diagnostics and
available pinned-Go rendered chains/locations. The balanced workstation comparison
([records](../artifacts/vscode-jsdoc-fragments/timing/comparison.json)) found:

| Measure | Filename-sharing baseline | Comprehensive JSDoc fragments |
|---|---:|---:|
| Mean wall seconds (two runs) | 61.770 | 62.420 |
| Wall range | 58.67–64.87 | 61.37–63.47 |
| Mean CPU seconds | 162.405 | 161.780 |
| Mean peak RSS, KiB | 10,990,580 | 10,916,082 |

This establishes **no incremental native speedup**: wall time is 1.05% higher and
ranges overlap, CPU is 0.39% lower and peak RSS 0.68% lower. Completion of source
sharing is separately supported by source reconciliation and correctness evidence.
The earlier larger JSDoc/source handoff improvement remains distinct; these numbers
must not be presented as another comparable speed win. The matched CoreCLR allocation capture also finds no net allocation improvement:
sampled weight changes from 35.181 to 35.284 GiB (+0.29%). String weight falls from
2.715 to 2.609 GiB (−3.88%), while new view arrays account for 0.283 GiB. Inclusive
JSDoc parser weight rises from 0.230 to 0.359 GiB even as visible substring and
materialization frames fall 75.5% and 67.4%. The descriptor/list storage trades off
against string-copy savings; reducing copies alone does not establish lower total
allocation. These overlapping sampled categories are not additive or an exact native
allocation census. Zero event loss/truncation was reported; 260 unmatched GC restarts
prevent a complete pause-time claim. See [consolidated findings](../artifacts/vscode-jsdoc-fragments/final-findings.json).

This packet is accepted for comprehensive selected text-sharing behavior, **not as a
performance improvement**. Further allocation work should account for descriptor width,
list growth and lifetime, not blindly replace every string with a larger wrapper.
One central compiler build took 9.24 seconds, CLI publication 10.81 seconds, the broad
correctness gate 378.39 seconds, four native workloads 248.38 seconds and the one
profiled workload 59.62 seconds. The existing baseline profile was reused. Three Sol
agents were reused with one owner per implementation/test/measurement surface and
serialized heavy work. Token and monetary measurements are unavailable.

## Server GC: enabled and verified under Native AOT

Following the user's explicit request on 2026-10-10, [the CLI project](../src/TypeScript.Check/TypeScript.Check.csproj)
now sets `ServerGarbageCollection=true`. The exact one-argument `--runtime-info`
command reports actual `GCSettings.IsServerGC` and processor count. Existing checking
routes are unchanged. Source review checked the installed SDK's managed and Native
AOT mappings. Published Linux x64 native variants from identical source report
`false`/`true` respectively; the managed default reports `true`. All report 16 processors.
Help succeeds and extra probe arguments remain invalid. No heap-count tuning was added.

The separate balanced workstation/server comparison uses the same JSDoc-complete
compiler library and CLI source. All four runs preserve the ten complete diagnostic
records plus available pinned-Go rendered chains and locations, expected exit1 and
empty stderr. [Complete comparison](../artifacts/vscode-server-gc/timing/comparison.json):

| Measure | Workstation GC | Server GC | Change |
|---|---:|---:|---:|
| Mean wall seconds (two runs) | 60.565 | 28.490 | −52.96% |
| Wall range | 55.26–65.87 | 26.64–30.34 | |
| Mean CPU seconds | 134.790 | 123.760 | −8.18% |
| Mean peak RSS, KiB | 10,963,370 | 12,178,528 | +11.08% |

This supports keeping Server GC as the default for this compiler workload. It is a
runtime-policy result, not evidence that allocation volume or the remaining Go gap
has been eliminated. Only two observations per variant on this Linux x64 host were
measured. Neither run reported swapping; the final workstation run reported three
major page faults, others zero. There are no GC environment overrides or cgroup CPU/
memory caps; host memory is 32,668,240 KiB. Source and binary hashes are retained in
[the build/probe binding](../artifacts/vscode-server-gc/performance-build/binding.json).
The original workstation JSDoc measurements remain separate and unchanged.

Reproduce the default and inspect the actual native runtime:

```bash
dotnet publish src/TypeScript.Check/TypeScript.Check.csproj -c Release \
  -r linux-x64 -p:PublishAot=true \
  -p:CppCompilerAndLinker="$PWD/tools/.cache/native/bin/cc" \
  -o artifacts/local-server-gc
artifacts/local-server-gc/tscheck --runtime-info
artifacts/local-server-gc/tscheck \
  --config /home/greg/projects/vscode/src/tsconfig.json --noEmit --json
# For a separately published control, add -p:ServerGarbageCollection=false.
```

The frozen accepted server binary is
[tscheck](../artifacts/vscode-server-gc/native/server/tscheck), SHA256
`722d03c285cc52cc67910760d4d80294b46284051eb74c6f4377d761bee6a84f`.
All performance work remains uncommitted. Subsequent byte-backed source changes must
be compared separately against this default-GC baseline.

## Byte-backed UTF8 representation: source-grounded next direction

The user highlighted `System.Text.Json` as an example of processing UTF8 input through
readonly spans. Microsoft's [Utf8JsonReader API](https://learn.microsoft.com/en-us/dotnet/api/system.text.json.utf8jsonreader?view=net-10.0)
accepts `ReadOnlySpan<byte>` and exposes raw token slices; this avoids requiring a
UTF16 string for the whole input, while decoded string results can still allocate.
A retained compiler representation would need owned/read-only memory or a backing
array plus offset/length, with spans used for local operations; a span alone cannot
be stored in ordinary heap AST nodes.

This aligns closely with the pinned Go implementation. Its
[VFS loader](../upstream/TypeScript/tsc/internal/vfs/internal/internal.go) constructs a
string over the read byte buffer, strips a UTF8 BOM and converts UTF16 BOM inputs.
Thus ordinary UTF8 files can retain byte storage, but assuming every accepted source
file is already UTF8 would weaken the loader contract. The scanner also preserves
Go's invalid-byte behavior and JavaScript surrogate/escape semantics; byte positions
and externally reported UTF16 positions remain distinct.

By contrast, [GoString](../src/TypeScript.Scanner/Runtime/GoString.cs) currently uses
one .NET char per encoded byte. Shared views remove copies without removing that
UTF16-backed storage overhead or all byte/string conversions. Byte-backed source and
text views are therefore a concrete subsequent representation campaign, with explicit
ownership, text-value/key compatibility, escape construction and diagnostic boundaries.
It is not automatically a zero-allocation pipeline and does not itself establish Go
performance parity. The JSDoc packet was kept coherent and verified before beginning the byte-backed
source change below. Its explicit view/materialization boundaries support that work.
The migration described below is implemented and behavior-qualified with an explicit
comment-only source-drift qualification. Controlled performance measurement is running;
this historical motivation does not itself establish a performance improvement.

## Owned UTF8 source pipeline: active completion contract

Following the user's explicit instruction to continue regardless of the Server GC
result, the next packet replaces char-per-byte source backing with privately owned
bytes. The contract is a genuine path from disk/bundled source loading through
parser/scanner state into retained SourceFile/JSDoc text, with local
`ReadOnlySpan<byte>` operations and shared byte views. Normal internal Program paths
must not immediately recreate and retain a full char-per-byte source string.
Existing public raw-string/Unicode APIs remain explicit compatibility boundaries;
caller-owned byte arrays are defensively copied, while trusted internal loaders can
transfer ownership. Source slices share immutable storage and preserve byte offsets.

Required behavior includes unchanged UTF8 BOM handling and UTF16 BOM transcoding,
Go-compatible malformed UTF8/WTF8 and escape handling, externally visible UTF16
positions, lazy JSDoc/reparsing, regex source access, content mapping and diagnostic
text. Optional internal byte-reading host transport may preserve existing public
string-based hosts through a fallback; fallback costs must remain visible.

To address the measured view-array growth, the same packet includes JSDoc text
fragment/indent/link collection headers using the existing Slice descriptor, with
correct persistent snapshots and public mutable list projection. Node/diagnostic
collections and global identifier/symbol string keys are not silently migrated.
Required joined/public/diagnostic strings still materialize. Detached source retention
remains intentional; no zero-allocation or Go performance-parity claim is implied.

Acceptance: independent Sol source/omission review; preserved and adapted tests with
explicit storage-assertion changes; byte-ingress/ownership/invalid-text/BOM/position
regressions; broad pinned-Go scanner/parser agreement and retained semantic/Program/
Project checks; actual Linux x64 Native AOT; unchanged complete VS Code diagnostics;
separate measured comparison using the default Server GC policy on both versions.
Implementation and performance outcomes will be reported separately. One compiler
integration build and one comprehensive final gate are preferred, with targeted
checks when a concrete risk or failure calls for them. Work on frozen GC measurements
and source-only implementation may overlap; heavy workloads remain serialized.

The owned-byte implementation now compiles in snapshot1 (35 source files changed
relative to the frozen Server GC baseline). Two central build invocations were used:
six binding errors on the first were corrected, then the second passed with zero
warnings/errors. The focused 61-case gate passed, including twelve authored byte-source
cases, retained text/fragment/filename/regex assertions, four new complete pinned-Go
arrow-rune cases, and one existing OS BOM case. Independent core and VFS reviews found
no substantive issue.

All functional stages of the comprehensive gate passed in 1238.374 seconds:
38,868 managed assertions, including 15,395 current no-emit cases and 15,304 complete
original records; actual native no-emit 15,304 exact; fresh pinned-Go/native parser
and scanner 17,723 exact each; historical Foundation 3153 plus view17/fragment13/byte12;
Program 78 controls, regex3/arrow4/filename11; Projects 27 controls and 48 complete records.
Six Linux x64 ELF targets were checked without CoreCLR dependencies. Counts are
executed assertions/records across overlapping domains, not distinct upstream bodies.

The [original report](../artifacts/vscode-byte-source/final-regressions/report.json)
remains failed at its final source/input guard. An unknown editor action removed one
trailing ASCII space from a scanner comment during the run. The approved
[postgate qualification](../artifacts/vscode-byte-source/final-regressions/postgate-qualification/review.json)
binds the exact frozen/current diff and hashes. Managed tests and earlier native
builds used frozen Scanner `4d79859d`; later Program/Projects publishes used the
comment-only rebuild `bcfc6f7c`. All six actual native executable hashes are retained.
This is qualified behavioral acceptance, not a claim that current source has the
unchanged frozen fingerprint. The failed report was preserved; no broad rerun was needed.

Before the first workload run, verification restored frozen `4d79859d` DLL/PDB inputs
and published the performance executable without rebuilding project references. The
[first full native VS Code result](../artifacts/vscode-byte-source/first-vscode/result.json)
is 37.96s wall /154.56s CPU /12,096,368KiB peak, exit 1 with empty stderr. All ten complete
diagnostic records and available pinned-Go owners/codes/locations/chains match. This
single observation does not establish a controlled speedup. Verification is running
the matched C,B,B,C comparison with Server GC on both versions.

For subsequent packets, the user agreed to build and focused checks first, then an
actual Native AOT VS Code correctness/performance run, then broad acceptance once
source is coherent. The twenty-minute broad gate should follow initial workload
feedback rather than delay it. Heavy builds, tests and profiles remain serialized.

The arrow-rune cases establish one exposed prior grammar omission: Go decodes runes
when rejecting a line break before an arrow; the old char-per-byte check missed U+2028
and U+2029. Actual pinned-Go outputs require TS1200 for those and LF, while the space
control has no corresponding error. The complete reference records were added; existing
expected records were not rewritten.

## Current callback, snapshot and append allocation work

The next matched CoreCLR Server GC allocation capture reduced sampled interval weight
from 28.335 to 23.289 GiB (17.81%) across accumulated source adaptations. This is
[one profile per version](../artifacts/vscode-relation-inference/allocation-comparison.json),
not an exact allocation census or per-patch attribution. The
[joint type/caller report](../artifacts/vscode-relation-inference/candidate-managed/allocation-type-callers.json)
partitions the same events by allocated type and leafward application caller, skipping
only the Append/Slice bridge frames. Full stacks remain available for audit; the
partitions and overlapping inclusive weights must not be added together.

Scanner snapshots now retain source-shaped directive headers and borrowed token views.
[Four focused controls](../tests/Scanner.Tests/ScannerSnapshotTests.cs) passed, including
complete original observations for zero-state rewind and an actual public Go witness
for mutation visible through two saved directive headers. Public managed directive
arrays retain their defensive projection. The inputs and driver are authored; they
are not newly ported original upstream tests.

The [current compiled snapshot](../artifacts/vscode-append-capacities/build/snapshot-1/snapshot.json)
contains explicit-state generic-signature callbacks, deferred error-only callback
creation, original local capacities and owned-buffer appends. The shared
[Append adapter](../src/TypeScript.Scanner/Runtime/ParserCompat.cs) preserves its existing
managed detached-result policy. A non-params array bridge preserves null/default
binding while scalar calls use params spans; eager enumerable materialization still
precedes source copying. This removes temporary params arrays and repeated backing
growth without claiming general Go slice sharing. All
[five authored compatibility controls](../validation/Foundation.Checks/ListAppendChecks.cs)
passed on managed .NET and actual Native AOT.

The [first native workload](../artifacts/vscode-append-capacities/first-vscode/result.json)
completed in 24.53s wall /103.92s CPU /10,749,308 KiB peak with all ten complete diagnostics
and available pinned-Go owners/codes/locations/chains exact. This single observation
is not a controlled speedup. The additional
[native CPU profile](../artifacts/vscode-append-capacities/native-cpu/analysis.json)
contains 9,018 samples, weighted to 91.091 user CPU seconds; 55.66% falls in SVR runtime
leaves. These categories include runtime allocation/GC helpers, not an exact GC CPU
census. All 3,913 worker samples lack caller chains, so the
[interactive graph](../artifacts/vscode-append-capacities/native-cpu/native-cpu-flamegraph.svg)
is explicitly thread-to-leaf rather than caller attribution. The
[independent qualification](../artifacts/vscode-append-capacities/independent-review.json)
checks result hashes, five-case managed/native evidence, graph XML and conserved
period weights. Work continues through the native-first feedback loop; no broad
finalization gate was scheduled for this packet.

## Latest qualified iteration and path batch

The [7bab snapshot](../artifacts/vscode-range-pathkey/build/snapshot-1/snapshot.json)
adds List-value and integer value iteration, raw-byte PathKey range validation, and
source-shaped candidate insertion. Concrete List value loops retain the original
versioned List iterator behavior, including the distinct empty-at-creation interface
route; this does not change the existing Go fixed-header distinction. The PathKey
guard accepts every byte value 0..255, including malformed UTF8/WTF8 bytes, and keeps
Unicode ingress separate. Candidate grouping and literal-signature specialization
order remain unchanged while the owned output uses original capacity and insertion.

[Ten focused managed controls](../artifacts/vscode-range-pathkey/focused/report.json)
passed: seven iteration checks and three path checks. The seven shared iteration
checks also passed under actual Native AOT. These are authored compatibility tests,
not newly ported original upstream test bodies; the path checks have no separate
direct native-test claim. Existing reference expectations and the historical default
Foundation registry are unchanged.

The [native VS Code workload](../artifacts/vscode-range-pathkey/first-vscode/result.json)
completed in 23.33s wall /95.87s CPU /11,411,500 KiB peak. All ten complete diagnostic
records and available pinned-Go owner/code/location/chains match, with empty stderr.
The [independent evidence review](../artifacts/vscode-range-pathkey/independent-review.json)
checks result/binary hashes, actual TRX counts and native seven-case output. This is
one workload observation, not a controlled benchmark or whole optimization completion.
The progress checkpoint preserves the ongoing native-first feedback loop; it does
not imply a new broad finalization gate.

## Internal signature slice rollout

The [signature snapshot](../artifacts/vscode-signature-slices/build/snapshot-1/snapshot.json)
replaces internal signature List storage end to end with Go-like value slice headers:
structured/interface/composite producers, instantiation, relation and display consumers,
and the resolution/candidate output chain. Type, Symbol and IndexInfo collections are
unchanged. The original structured getters clip capacity while the internal checker
getter preserves its unclipped two-index view. Empty setter inputs normalize to nil;
a single signature family shares clipped storage and two nonempty families concatenate
into detached storage. Unchanged maps retain the original header, while changed maps
allocate an ordered shallow result. Reordered overload candidates own separate storage
before indexed replacements. These distinctions were checked across whole affected
functions and direct helper paths by independent reviewers.

Existing public List-returning signature APIs explicitly materialize nil-aware detached
shallow snapshots. Internal consumers use descriptors rather than these projections.
The existing named validation record schemas also project only at their record boundary;
their expectations remain unchanged. Raw signature field representation changes are not
binary or reflection ABI equivalence claims. Descriptor fields are larger than reference
fields, and Go allocator size-class rounding is not reproduced by nominal slice growth;
net memory and speed effects require measurement.

A coherent Release retry compiled with zero warnings/errors after nine mechanical
bindings were corrected. The [first native VS Code run](../artifacts/vscode-signature-slices/first-vscode/result.json)
returned all ten complete diagnostic records and available pinned-Go chains exactly in
22.73s wall /97.14s CPU /11,323,472 KiB peak. This is a single observation, not an
attributed speed improvement. Four authored shared boundary controls and four authored
Program inputs compare against [real Go slice observations](../validation/signature-slice-corpus/slice-witness-provenance.json)
and [complete pinned-Go Program records](../validation/signature-slice-corpus/reference-provenance.json).
All eight managed checks passed. Native AOT separately passed the four shared boundary
checks and [four complete Program comparisons](../artifacts/vscode-signature-slices/focused/native-program-four.jsonl).
The [consolidated focused report](../artifacts/vscode-signature-slices/focused/report.json)
also records Callable89, Collections44 and TypeSystem77 managed checks passing with
zero skips. Named record adapters were corrected without changing their expectations.
The controls are not original upstream compiler test bodies, and no new private
observation acceptance was introduced.

The [matched CoreCLR allocation comparison](../artifacts/vscode-signature-slices/allocation-comparison.json)
reports sampled interval weight of 20.946 versus 20.436 GiB (2.44% lower), with
Server GC and the same workload. Signature List weight fell from 451,909,624 bytes
to no sampled events; Signature array weight fell from 234,309,112 to 68,657,256
bytes. This is consistent with removing internal List projections and copies; an
absence of sampled events does not prove zero actual allocations. ObjectType,
TypeReference and InterfaceType sample weights increased, reported separately
from the removed buffers; larger descriptor fields are a representation cost,
not a proven attribution of those exact increases. Type array weight was broadly
unchanged. Both traces preserve all ten diagnostic records and report zero lost
events, no truncation and no aggregation overflow. Each variant has one sampled
trace, so these weights are estimates rather than an exact allocation census.
They describe CoreCLR rather than Native AOT allocations; the native timing
observations above do not establish a causal speed improvement.

## Cached assignment visitor and union deduplication

The [visitor/union snapshot](../artifacts/vscode-flow-union-allocation/build/snapshot-1/snapshot.json)
restores the pinned Go once-per-checker assignment visitor (checker.go:1268), replacing
fresh delegate construction at recursive child traversal. Root and recursive calls use
the same cached worker; assignment flags, positions, exclusions and traversal ordering
are unchanged. Sorted union deduplication now borrows the suffix through ordered indexed
reads instead of allocating a Skip iterator and snapshot array. The write cursor never
overtakes the read cursor; tail clearing and truncation remain explicit. The existing
sort and owned Type List storage are unchanged. [Independent whole-path review](../artifacts/vscode-flow-union-allocation/build/independent-review.json)
found no semantic issue.

The sole library build took 3.992s and Native AOT publication took 9.487s. The
[first native VS Code run](../artifacts/vscode-flow-union-allocation/first-vscode/result.json)
returned all ten complete diagnostic records and available pinned-Go chains exactly in
23.73s wall /98.32s CPU /11,210,476 KiB peak. This is a single observation with no
speedup claim or new allocation profile. The [focused report](../artifacts/vscode-flow-union-allocation/focused/report.json)
records eight existing assignment-sensitive compiler inputs passing complete pinned-Go
record comparison and original assertions, plus TypeSystem77 public checks passing
with zero failures/skips. Expected records are unchanged; this is focused qualification,
not a new broad acceptance gate.

## Cached checker callbacks and private JSDoc records

The [callback/JSDoc snapshot](../artifacts/vscode-cached-assignable-comparer/build/snapshot-1/snapshot.json)
restores original callback reuse for assignability, symbol ordering and three existing
primitive/missing/string-index predicates. Setup follows the pinned checker initialization
points. The assignability worker calls the existing simple relation directly and retains
the original ignored reportErrors parameter. Symbol ordering needs one cached Func and
one cached Comparison because the managed spelling and sort APIs require distinct delegate
types; both invoke the unchanged worker. Existing stored-comparer observations are unchanged.

The private parser JSDocInfo collection now uses a slice header for ordered append and
speculative rewind, matching Go reslicing instead of copying a List prefix. Existing
reparser append sites use the same descriptor helpers. The final JSDoc cache retains
parent/document references independently; public AST storage and parser lifetime are
unchanged. This removes known callback construction and prefix-copy sites structurally,
without claiming a measured allocation total for the batch.

The [native VS Code run](../artifacts/vscode-cached-assignable-comparer/first-vscode/result.json)
returned all ten complete diagnostic records and available pinned-Go chains exactly in
24.74s wall /105.34s CPU /11,188,996 KiB peak. The [focused report](../artifacts/vscode-cached-assignable-comparer/focused/report.json)
records four unchanged original inference inputs passing complete records and original
assertions, plus TypeSystem77 public and 21 existing JSDoc source/restoration checks
passing with zero failures/skips. No new profile was collected for this batch; the native
run is one observation with no speedup or broad acceptance claim.

## Fixed link-store reference slots

The [ref-slot snapshot](../artifacts/vscode-link-slots/build/snapshot-1/snapshot.json)
replaces exposed page-cell wrappers with ordinary managed references into fixed
256-entry V reference arrays. Page presence remains distinct from a nil value.
Node lookups materialize zero-value records only when their page exists; symbol
lookups retain nil pointer values. Checker callers receive the same stable V objects.
The separate optional container-metadata cells are unchanged. [Source packet](../artifacts/vscode-link-slots/build/source-ready.json)
records the bounded Core/Checker change and existing raw-page validation adapter;
raw observations keep their names/order/expected booleans, and retained refs write
through after page-table growth. Existing node/symbol identity assertions are unchanged.

The [first native VS Code run](../artifacts/vscode-link-slots/first-vscode/result.json)
returned all ten complete diagnostic records and available pinned-Go chains exactly
in 24.94s wall /101.57s CPU /10,842,564 KiB peak. The [focused report](../artifacts/vscode-link-slots/focused/report.json)
records Collections44 and one original Utilities comparison passing managed with
zero failures/skips; actual Native AOT Utilities matches all 20 unchanged observations.
This is focused qualification, not a broad gate. The [cumulative allocation comparison](../artifacts/vscode-link-slots/allocation-comparison.json)
reports sampled CoreCLR interval weight of 20.43598 to 19.32139 GiB (-5.4541%)
since the signature baseline, including visitor/union/callback/JSDoc headers and
direct page slots. Each variant has one matched Server GC trace with zero lost
events or truncation. These estimates do not isolate this packet or measure exact
allocated bytes; the single native observation establishes no speedup.

The [six alternating native observations](../artifacts/vscode-link-slots/paired-native/report.json)
compare the frozen signature checkpoint to the frozen ref-slot candidate, without
profiling: mean wall23.17→21.36s (−7.81%), mean CPU100.017→95.76s (−4.26%).
All six complete diagnostic records remain exact. There are only three observations
per version; candidate wall spans19.72–23.83s and mean peak RSS is0.90% higher.
These are descriptive cumulative results, not per-change attribution or statistical
significance. Ownership-token and static-reporter follow-ups are not included.

## Opaque ownership tokens and unbound reporters

The [ownership/reporter snapshot](../artifacts/vscode-symbol-ownership-token/build/snapshot-1/snapshot.json)
replaces two authored public-API ownership sets with one opaque token per checker.
Only the existing symbol/signature creation paths stamp it; foreign/default objects,
null guards and declaration fallbacks retain the previous admission behavior.
The token has no checker back-reference. This adds a reference field to every Symbol
and Signature, so removed set arrays and strong roots do not establish a net memory
win by inspection. The private non-null helper now receives an explicit checker and
two static reporters, matching Go's unbound method callbacks without changing
errors, narrowing or callback timing. The [independent review](../artifacts/vscode-symbol-ownership-token/build/independent-review.json)
binds these four source files only; subsequent unstaged loop/Relater work is excluded.

The [first native run](../artifacts/vscode-symbol-ownership-token/first-vscode/result.json)
returned all ten complete diagnostic records and available pinned-Go chains exactly
in 22.32s wall /90.99s CPU /10,298,208 KiB peak. The [focused report](../artifacts/vscode-symbol-ownership-token/focused/report.json)
records four unchanged original nullable/unknown/inference inputs passing complete
record comparison, three managed public-API Facts passing, and actual Native AOT
controls passing with 14 self-test and ten callable observations. One new authored
Fact covers own/foreign/default/null signatures; original expectations are unchanged.
This is focused qualification, not a broad gate or a native speedup claim.

The [matched allocation comparison](../artifacts/vscode-symbol-ownership-token/allocation-comparison.json)
reports CoreCLR sampled interval weight of 19.321389 to 18.701305 GiB (−3.2093%).
Ownership-set entry arrays have no candidate samples, while Symbol and Signature
object weights increase, consistent with the added fields; these type weights do
not establish exact layout costs. The net sample includes both ownership and reporter
changes and does not isolate their effects. Each version has one matched Server GC
trace with zero lost events or truncation; one of 179,980 candidate events has no
stack. These are sampled estimates, separate from native timing, not an allocation
census or a native speedup claim.

## Contextual, deprecation and relationship callbacks

The [qualified callback snapshot](../artifacts/vscode-relater-callbacks/build/snapshot-1/snapshot.json)
retains contextual literal recursion and deprecation any/all decisions through ordered
loops, with original first-match, parent/value-declaration ordering and cached flags
checks. Relationship variance evaluation uses direct local calls with the same shared
per-operation result and error state. Property callbacks carry explicit state and stay
lazy after the original target/property guards; discrimination reads the same combination
slot at callback time. The source-zero exclusion set now uses the existing null adapter,
which all affected read-only consumers treat like an empty set. No public or named
observation schema changes. [Source reviews](../artifacts/vscode-relater-callbacks/build/independent-review.json)
and [contextual/deprecation review](../artifacts/vscode-contextual-deprecation-loops/build/independent-review.json)
cover complete affected functions and direct helpers.

[Compiled metadata](../artifacts/vscode-relater-callbacks/metadata/result.json) verifies
that this variance capture is a value type passed by reference at two direct calls.
This is finite lowering evidence, not a claim that every relationship callback is
allocation-free, nor evidence for subsequent uncompiled template-local changes.
The [native VS Code run](../artifacts/vscode-relater-callbacks/first-vscode/result.json)
returned all ten complete diagnostic records and available pinned-Go chains exactly
in 22.43s wall /97.81s CPU /10,028,472 KiB peak. The [focused report](../artifacts/vscode-relater-callbacks/focused/report.json)
records eight unchanged original full-record comparisons passing, covering contextual
literals, deprecated overloads, variance fallback, private/protected properties, tuples
and discriminated relationships. This is focused qualification with no new matched
allocation profile or causal native speedup claim.

## Template inference local callbacks

The [template inference snapshot](../artifacts/vscode-template-inference-locals/build/snapshot-1/snapshot.json)
uses direct local functions for the original `getSourceText` and `addMatch` bodies,
retaining operation-local segment, position and match state. The
[source review](../artifacts/vscode-template-inference-locals/build/independent-review.json)
covers the complete prefix/suffix, delimiter and Unicode matching path. Compiled
[metadata](../artifacts/vscode-template-inference-locals/metadata/result.json)
confirms by-reference value-type state at both helpers and direct calls; this is
limited to the inspected worker and helpers.

The [native workload](../artifacts/vscode-template-inference-locals/first-vscode/result.json)
took 22.54 seconds wall and 99.99 seconds CPU, peaking at 9,903,144 KiB, with all ten
complete diagnostics and available pinned-Go chains exact. The
[focused gate](../artifacts/vscode-template-inference-locals/focused/report.json)
passed six unchanged original full records. This single native observation is not
a speedup claim.

The [matched allocation comparison](../artifacts/vscode-template-inference-locals/allocation-comparison.json)
reports 18.701305 to 18.122111 GiB of sampled allocation interval weight, down
3.0971%. This cumulative comparison starts at the ownership/reporter snapshot and
includes the preceding contextual/deprecation and relationship callback changes
committed as `162cdc2`; it does not isolate template inference savings. Each role
has one CoreCLR trace, with zero lost/truncated events and aggregation overflow;
one baseline event lacks a stack. The
[site comparison](../artifacts/vscode-template-inference-locals/site-comparison.json)
and [evidence review](../artifacts/vscode-template-inference-locals/acceptance-review.json)
retain the attribution and sampling limits. These are estimates, separate from
NativeAOT timing.

## Contextual callback state

The [qualified contextual snapshot](../artifacts/vscode-contextual-callback-state/build/snapshot-1/snapshot.json)
uses the existing generic mapper with static callbacks and explicit receiver/index/
length/spread state. The [source review](../artifacts/vscode-contextual-callback-state/build/independent-review.json)
confirms the complete apparent-type and tuple-element branches retain lazy callback
work, recursive mapping order and `noReductions=true`.

The [native workload](../artifacts/vscode-contextual-callback-state/first-vscode/result.json)
took 23.33 seconds wall and 97.80 seconds CPU, peaking at 9,959,248 KiB, with all ten
complete diagnostic records and available pinned-Go chains exact. The
[focused controls](../artifacts/vscode-contextual-callback-state/focused/report.json)
passed eight unchanged original full records with zero skips. This qualifies the
`affe6316` snapshot; the later mapper scratch-buffer proposal is separate. There
is no matched allocation result or causal speedup claim for this packet.

## Local mapper and parser scratch

The [mapper snapshot](../artifacts/vscode-map-scratch/build/snapshot-1/snapshot.json)
and [parser snapshot](../artifacts/vscode-parser-inline-scratch/build/snapshot-1/snapshot.json)
restore upstream local capacity-16 scratch with inline reference buffers and a
slice spill above sixteen elements. Published lists remain detached, and public
AST/type storage remains unchanged. Proven unaliased contextual-property scratch
also uses amortized append. Both changes received independent whole-function
reviews; the extra reference slots in recursive stack frames remain a cost to
measure, not an automatic performance benefit.

Native VS Code retained all ten complete diagnostics and available pinned chains:
mapper23.23s wall/94.05s CPU; parser23.63s wall/98.38s CPU. These are single runs.
The [mapper controls](../artifacts/vscode-map-scratch/focused/report.json) retain ten
existing complete Program comparisons. One additional authored15/16/17-member
case has a genuine pinned-Go reference, xUnit registration and an exact
[Native AOT comparison](../artifacts/vscode-map-scratch/focused/native-boundary-report.json).
The [parser controls](../artifacts/vscode-parser-inline-scratch/focused/report.json)
pass two shared scratch cases and nine complete parser records under managed and
Native AOT execution, including list widths, detached publication, recovery,
reentry, Unicode and parent-pointer observations. The parser records reuse eight
upstream compiler inputs plus one authored Unicode case; this does not claim
those full compiler tests passed. Expected records were unchanged. An initial
native test flag typo was rejected by the harness and corrected without rerunning
already successful managed work or changing production behavior.

Matched sampled CoreCLR allocation intervals fell
[18.122111→17.572668GiB](../artifacts/vscode-map-scratch/allocation-comparison.json)
(−3.0319%, includes preceding contextual callbacks), then
[17.572668→17.107677GiB](../artifacts/vscode-parser-inline-scratch/allocation-comparison.json)
(−2.6461%). Both comparisons have zero lost/truncated events; the parser candidate
has one event without a stack. These are sampled estimates, not exact Native AOT
bytes or isolated timing effects. Persistent public lists and their array
allocations remain; upstream arena slabs require a broader storage-boundary change.

## Checker and flow callback state

The [qualified snapshot](../artifacts/vscode-checker-state-filters/build/snapshot-1/snapshot.json)
uses explicit synchronous state and static callbacks across contextual inference,
constraint/facts filtering and flow discriminant narrowing. Shared ordered filter
and mapping algorithms preserve origin/Never bypasses, first-change copying and
lazy property/predicate work; [Checker](../artifacts/vscode-checker-state-filters/build/independent-review.json)
and [Flow](../artifacts/vscode-flow-callback-state/build/independent-review.json)
received complete affected-path reviews. Native VS Code took 22.93s wall/96.33s CPU
with all ten complete diagnostics and pinned available chains exact; the
[focused gate](../artifacts/vscode-checker-state-filters/focused/report.json)
passed ten unchanged original full records.

The [matched CoreCLR allocation comparison](../artifacts/vscode-checker-state-filters/allocation-comparison.json)
reports sampled interval weight17.107677→16.497538GiB (−3.5665%), one trace per
variant, zero lost/truncated events or aggregation overflow, and one baseline event
without a stack. This estimates the combined packet, not isolated callback savings
or Native AOT allocation bytes. Six
[cumulative native observations](../artifacts/vscode-checker-state-filters/paired-native/report.json)
comparing ownership/reporter282c to7c0f report wall+5.19%, CPU−1.47% and peakRSS−1.63%.
They include intervening changes and support no speedup claim. Allocation profiling
and flat native CPU samples remain separate measurements; unresolved worker callers
cannot establish a causal allocation-to-GC timing relationship.

## Path fast paths and singleton capacity

The [qualified b81a packet](../artifacts/vscode-path-normalization-fastpath/build/snapshot-1/snapshot.json)
restores the pinned Go simple-normalization entry and its root/trailing/relative
path guards. Scalar disk-boundary helpers reuse ASCII spelling while preserving
strict non-ASCII, invalid UTF-8/WTF-8 and null behavior. Singleton arena results
retain one mutable shallow element with explicit initial capacity 1. Native VS Code
[retained all ten complete diagnostics](../artifacts/vscode-path-normalization-fastpath/first-vscode/result.json)
at 22.64s wall/91.50s CPU; this single observation is not a speedup claim.

The [focused checks](../artifacts/vscode-path-normalization-fastpath/focused/report.json)
passed five existing path/fuzz cases, 21 existing shared ProjectSupport cases,
27 native controls and 48 complete canonical pinned-Go project records. The stale
project signature observer was repaired with an explicit nil-aware List snapshot;
null/allocated-empty/order and expected DTOs are unchanged. Native publication also
[warned](../artifacts/vscode-path-normalization-fastpath/focused/publish-native-projects.stdout)
that stale dependent Collections StoredGraph.SignatureSeed metadata would throw.
The selected 27/48 channels passed; that dependency must be refreshed before its
mode or a final gate. This does not qualify every validator mode.

The [matched CoreCLR allocation estimate](../artifacts/vscode-path-normalization-fastpath/allocation-comparison.json)
fell 16.497538→16.404452GiB (−0.5642%) for the combined path/ASCII/singleton packet.
Each role has one trace with zero loss/truncation or missing stacks; weights are
sampled interval estimates, not exact Native AOT bytes or per-change savings.

## Borrowed tokens, flow slices and conditional targets

The [frozen93159964 packet](../artifacts/vscode-scanner-token-views/build/snapshot-1/snapshot.json)
borrows ordinary unchanged token values and projects cached raw strings only when
needed. Flow scratch/loop descriptors preserve Go header sharing and restoration;
union input workers borrow spans while retaining owned persistent outputs.
Conditional target spans preserve true-before-false evaluation and original
inference priority/order. Identifier and numeric rune traversal and builder rune
writes avoid temporary owned input/output conversions without changing decoders.

[Native VS Code](../artifacts/vscode-scanner-token-views/first-vscode/result.json)
retained all ten complete diagnostics and available pinned chains at 24.34s wall /
96.57s CPU /9,829,460 KiB peak. [Managed checks](../artifacts/vscode-scanner-token-views/focused/report.json)
passed 3,842 scanner cases, 13 unchanged original complete Program records and five
shared token controls; [native checks](../artifacts/vscode-scanner-token-views/focused/native-report.json)
compared 539 original scanner records and the same five shared controls exactly.
Expected records are unchanged. The [matched CoreCLR allocation comparison](../artifacts/vscode-scanner-token-views/allocation-comparison.json)
reports sampled interval weight16.404452→15.453668GiB (−5.7959%), one trace per role,
zero event loss/truncation and all147,193 candidate allocations typed/stack-resolved.
This estimates the combined packet, not exact native bytes or per-site savings; the
single native observation establishes no speedup. Subsequent callback source edits
and expanded authored token-lifetime assertions are outside this qualified snapshot.

The subsequent [frozen4bc callback/span packet](../artifacts/vscode-instantiation-state/build/snapshot-1/snapshot.json)
uses static operation state for instantiation/inference and evolving-array maps,
borrows temporary template inference text for comparisons/search/rune decoding,
and provides borrowed rune traversal with explicit owned-array boundaries. Original
first-change cloning, bivariant restoration, WTF-8 decoding and retained string
outputs remain unchanged. Expanded authored token-lifetime assertions retain the
existing 17-case registry.

[Native VS Code](../artifacts/vscode-instantiation-state/first-vscode/result.json)
kept all ten diagnostics exact at 23.33s wall /97.59s CPU /10,030,632 KiB peak.
[Focused managed checks](../artifacts/vscode-instantiation-state/focused/report.json)
passed 19 original complete records, 77 TypeSystem records, 3,842 scanner cases,
17 lifetime checks and five token checks;
[native checks](../artifacts/vscode-instantiation-state/focused/native-report.json)
passed 539 original scanner records and 17 shared lifetime checks. No expected
records changed. No new allocation capture or speedup claim accompanies this
snapshot; later private-stack/candidate and AST source work is separate.

## AST text, inference candidates and private stacks

The [qualified c896 checkpoint](../artifacts/vscode-ast-text-values/qualification.json)
retains borrowed AST text through parser/Binder lookup, with canonical public string
projection and explicit persistent string keys. Private checker/flow stacks and
inference candidate headers preserve Go sharing, clone/reset and recursive borrowed
input lifetimes. Numeric parsing borrows spans and restores the pinned signed-64
fast path using standard .NET parsers where available; overflow retains the original
BigInteger fallback. These are managed storage adaptations, not changed algorithms.

[Native VS Code](../artifacts/vscode-ast-text-values/first-vscode/result.json)
kept all ten complete diagnostics exact at 24.73s wall /102.16s CPU /
10,290,800 KiB peak. [Managed checks](../artifacts/vscode-ast-text-values/focused/report.json)
passed 34 unchanged original Program records, 3,842 scanner cases, 163 Binder cases,
77 TypeSystem cases, 17 lifetime checks and five token checks. The
[five authored AST controls](../artifacts/vscode-ast-text-values/focused/ast-public/attempt-2/report.json)
passed in both managed and actual Native AOT runs; these are public ownership,
null/set/clone/hook, lookup and concurrent projection controls, not upstream test
ports. [Native comparisons](../artifacts/vscode-ast-text-values/focused/native-report.json)
also retained 539 scanner and 162 parser complete records plus 17 lifetime checks.
Original expectations are unchanged; the preserved first AST-control compile
failure was repaired only in its test binding.

The [cumulative sampled CoreCLR estimate](../artifacts/vscode-ast-text-values/allocation-comparison.json)
fell 15.453668→15.300418GiB (−0.992%), including the preceding unprofiled callback/rune
followthrough. String weight increased 865.8→943.9MB; stored string keys and later
public AST projections remain intermediate boundaries, and the next checker lookup
join is outside this snapshot. One matched trace per role has zero event loss or
truncation, with one candidate allocation lacking a stack. These interval weights
are not exact native bytes or isolated savings; the single native run establishes
no speedup or comprehensive final acceptance.

## Checker borrowed-text consumers

The [e793 receiver qualification](../artifacts/vscode-checker-text-values/qualification.json)
extends borrowed names into checker property, literal and fixed-text lookups.
Dictionary hits reuse the actual stored key; required persistent strings are
materialized on misses and shared with the resulting cache entries. The standard
Latin1 span conversion widens raw bytes for existing ordinal string dictionaries;
this is an intermediate key-storage boundary, not a fully byte-keyed implementation.

Native AOT VS Code matched the ten complete diagnostics at **23.74s wall**, 103.97s
CPU and 9,956,328 KiB peak. Focused checks passed 42 unchanged complete compiler
records, Binder163, TypeSystem77 and five authored AST controls; the same five
controls passed in Native AOT, including lookup across all256 byte values. This
single run establishes no speedup, and no extra allocation trace was collected.
Reviewed public slice/arena header helpers compile in this checkpoint; their
end-to-end use and behavioral qualification belong to the next AST list migration.
Full final regressions remain deferred while allocation work continues.

## AST child slices and arena publication

The [061c checkpoint](../artifacts/vscode-node-list-slices/qualification.json)
carries Slice<Node> through child lists, typed getters, JSDoc caches, parser/visitor,
binder, checker and node-builder consumers. Parser node and string-fragment arenas
publish clipped shared regions using Span.CopyTo. Cloned headers share elements but
keep independent lengths; external List-taking factory adapters explicitly copy
into stable storage. Generated iterator helpers now return value headers directly.
This changes provisional public C# list APIs to preserve Go storage behavior.

Native AOT VS Code kept ten exact complete diagnostics at **25.13s wall**, 101.88s CPU,
9,753,968 KiB peak. Focused checks passed **906 managed** cases, plus **162 native
parser records, 29 shared native controls and one complete native spread record**.
The new spread test is authored differential evidence, not an original upstream
unit test. Existing assertions/expected records remain unchanged; observer adapters
use Go header/slot semantics instead of incidental managed List identity.

The first compiled migration overwrote call-argument suffixes: the previous List
copy had hidden a missing source `slices.Clip` before tuple-spread expansion. Restoring
that exact operation resolved six extra VS Code diagnostics. A genuine pinned-Go
semantic regression with valid/invalid suffixes, repeated checks and type queries
fails against the defective binary and passes against the repaired one. Failed
builds/runs and the initial environment-only test capture remain preserved. Selected
consumer dependencies were rebuilt to resolve stale harness DLL bindings.

[Sampled CoreCLR allocation](../artifacts/vscode-node-list-slices/allocation-comparison.json)
fell **15.300418→14.869364GiB (−2.8173%)**, cumulatively including the preceding text
receiver checkpoint. Node lists, node arrays and strings fell; larger inline headers
increased NodeList/ModifierList object sizes. Zero lost/truncated events; all141334
candidate allocation events have resolved frames. These sampled weights are not
exact Native AOT allocations or isolated per-change savings. The
[allocation flamegraph](../artifacts/vscode-node-list-slices/candidate-allocation/allocation-flamegraph.svg)
is distinct from native CPU profiling. No speedup or final regression acceptance is
claimed; parser scratch and checker constituent storage work continues. Repeated
timing is deferred to the next combined implementation milestone.

## Type constituent slices and parser scratch

The [67bd snapshot](../artifacts/vscode-type-constituent-slices/build/snapshot-1/snapshot.json)
carries union/intersection constituents and template placeholders through checker,
relation, inference and printer consumers as shared slice headers. Source-required
clipping and detached mutation buffers remain explicit. Parser reparse/await/JSDoc
scratch also retains Go length/capacity behavior. The shared sort works over spans;
union normalization now uses the actual upstream stable-sort algorithm. Persistent
type headers grow by 16 bytes relative to a List reference, partly offsetting the
removal of List objects. This is not completion of all checker collection storage.

Focused verification passed **442 managed cases**, **162 complete native parser
records** and **three native scratch/sort controls**. The existing sort case was
then extended with authored stable-order checks and passed in managed and Native
AOT execution. Original Go result rows and semantic expectations remain unchanged.
Finite observer adapters needed updating for the new headers; failed build/test
artifacts are retained. This is targeted validation, not the deferred full gate.

[Six alternating Linux x64 Native AOT runs](../artifacts/vscode-type-constituent-slices/paired-native/report.json)
all matched the ten complete VS Code diagnostics and available pinned-Go chains.
Baseline 061c/candidate 67bd means were **24.4033/23.97s wall**, **102.4067/99.00s CPU**,
and **10,473,164/9,889,178.67 KiB peak RSS**. Wall ranges overlap; this is no strong
throughput claim. The candidate's first 33.64-second observation did not recur.

The matched CoreCLR allocation capture decreased sampled interval weight from
**14.869364 to 14.642716 GiB (−1.5243%)**. All 139,008 allocation events have type and
resolved stack information, with no lost or truncated events. These samples are
not exact native allocation totals. Raw traces are retained for performance viewers;
new SVG flamegraphs are no longer produced at the user's request.

## Reproduction and records

```bash
dotnet test tests/Checker.NoEmit.Tests/Checker.NoEmit.Tests.csproj -c Release \
  --filter 'FullyQualifiedName~DiscriminantNeverTests|FullyQualifiedName~ProjectDiagnosticPolicyTests'
dotnet publish src/TypeScript.Check/TypeScript.Check.csproj -c Release \
  -r linux-x64 -p:PublishAot=true -o artifacts/vscode-diagnostic-fix/native
# If the repository's native compiler shim is needed, add:
# -p:CppCompilerAndLinker="$PWD/tools/.cache/native/bin/cc"
artifacts/vscode-diagnostic-fix/native/tscheck \
  --config /home/greg/projects/vscode/src/tsconfig.json --noEmit --json

# Build the exact pinned compiler using the existing local Go toolchain:
(cd upstream/TypeScript/tsc && ../../../tools/.cache/go/bin/go build \
  -o ../../../artifacts/vscode-diagnostic-fix/pinned-cli/tsc ./cmd/tsc)
artifacts/vscode-diagnostic-fix/pinned-cli/tsc \
  --project /home/greg/projects/vscode/src/tsconfig.json \
  --noEmit --skipLibCheck --pretty false
```

Local evidence: [final result](../artifacts/vscode-diagnostic-fix/native-run/result.json),
[final comparison](../artifacts/vscode-diagnostic-fix/native-run/comparison.json),
[performance observations](../artifacts/vscode-diagnostic-fix/performance-observations.json),
[pinned CLI reference](../artifacts/vscode-diagnostic-fix/pinned-vscode/comparison.json),
[policy native controls](../artifacts/vscode-first-check/cli-diagnostic-policy/native-report.json),
[reduced discriminator](../artifacts/vscode-first-check/reduced-discriminator/), and
[first repaired comparison](../artifacts/vscode-never-fix/comparison.json).

For the diagnostic-policy follow-up, one coordinated Release build took 10.57s and
one actual Native AOT publish took 19.51s, followed by the focused controls and one
full native retry. Three existing Sol agents were reused for implementation,
reference investigation and cross-review. The relation reduction avoided an
unjustified checker change; the pinned run removed revision ambiguity. No token or
monetary measurements are available. Full historical gates were not redundantly
rerun for these narrow changes.
