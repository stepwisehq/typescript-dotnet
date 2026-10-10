# Async, generator and runtime-protocol checking contract

Baseline `bb035b3`; upstream `fed0bf24149fb1ed36039212648bafdafc1ea10e` unchanged.
This is the first campaign under the [whole-checker mandate](CHECKER-COMPLETION-PROMPT.md).
The contract follows original checker responsibilities, including absent producers and
partial consumers. It is not defined by a selected demonstration or unsupported-guard count.

| Obligation | Required upstream behavior and observable acceptance |
| --- | --- |
| P1 Await and async functions | Promise/thenable recognition; recursive Awaited resolution, cycle/cache behavior and error recovery; async return annotations, return/body inference, contextual returns and awaited operands; genuine globals and ES5/modern target diagnostics. Preserve original valid and invalid thenable behavior, not just built-in Promise calls. |
| P2 Generators and async generators | Generator construction and return-annotation validation; yield/yield-star, next-input, return aggregation and inference; contextual yield/return types, widening and signature checks; delegation sent-type relationships, annotations and related diagnostic information. Include interactions with generics, overloads, callbacks, unions and existing predicates/flow where upstream permits them. |
| P3 Iteration protocols | Synchronous and asynchronous iterable/iterator/result/method resolution, fast and slow paths, async-from-sync conversion/fallback, optional done/value types, for-await execution checking, custom protocols and malformed signatures. Reuse original iteration caches and global resolver semantics; compare meaningful inferred/result types and invalid protocol diagnostics without a private-state census. |
| P4 Resource management | Using and await-using declaration/initializer checking, Disposable/AsyncDisposable protocol requirements, nullish handling, async fallback, lexical/grammar restrictions, target/module/source-format and demanded helper diagnostics. Semantic helper requirements are included even where the eventual emitter is external; producing executable JS is not included. |
| P5 Shared consumers and original assertions | Complete demanded expression/deferred/signature/return/contextual/Flow/Grammar/utility/display consumers and original global diagnostic publication under genuine Program/options/libraries. Restore the complete TestContentMapperDirectivesPreserveIncrementalGlobals assertion body, including both Ignore/Expect policies and bulk/incremental global/file diagnostics. Previously partial assertions must not be relabeled as original full coverage. |
| P6 Acceptance and handoff | Source responsibility and applicable test inventories separate implementation/review from runtime evidence. Original assertions, adapted assertions, reused compiler inputs and authored comparisons have distinct provenance/dispositions. Valid/invalid programs, complete diagnostics and relevant inferred types/relationships must agree with pinned Go, including real disk invocation, actual Linux x64 Native AOT and retained regressions. Independent Sol omission/correctness review, reproducible evidence, committed checkpoint and immediate continuation to the next authorized campaign are required. |

## Source boundary and dependencies

Primary authorities: [checker.go](../upstream/TypeScript/tsc/internal/checker/checker.go),
[types.go](../upstream/TypeScript/tsc/internal/checker/types.go),
[grammarchecks.go](../upstream/TypeScript/tsc/internal/checker/grammarchecks.go),
[flow.go](../upstream/TypeScript/tsc/internal/checker/flow.go),
[utilities.go](../upstream/TypeScript/tsc/internal/checker/utilities.go), demanded
[emitsupport.go](../upstream/TypeScript/tsc/internal/checker/emitsupport.go) and
public diagnostic/type-display counterparts. Each remains in its direct C# source
counterpart. Existing original global, Awaited/thenable and synchronous iteration
algorithms are substantial inputs to reconcile; their existence does not establish
closure of absent consumers. Initial estimated newly selected closure is roughly
1,500–2,000 upstream lines plus bounded grammar/Flow/utility dependencies, not a measured
implementation size or completion percentage.

Program/loading/libraries, syntax/binding, TS declarations, synchronous expressions/flow
and shared type-system contracts remain accepted foundations. Investigate them only for
a concrete defect or demanded dependency. All 115 genuine pinned library bytes remain
unchanged; examples may select actual upstream library environments through real config.
No injected declarations, diagnostic suppression, permissive fallback or fixture-specific
algorithm. Standard .NET facilities are used where semantics match; Go nil/slice/byte and
identity requirements retain documented compatibility at the original boundaries.

General JavaScript/JSDoc/JSX/decorator consumers and remaining module interoperability,
import-attribute/JSON/options responsibilities follow in subsequent authorized campaigns.
Shared protocol algorithms required by them belong here; that external caller boundary
cannot justify missing shared behavior. No emission, services, unrelated infrastructure,
large-project comparisons, framework upgrade or speculative optimization. Source-language
and compiler-option restrictions encountered by protocol checking must produce upstream
behavior rather than an artificial profile limitation. Material expansion beyond checker
completion is escalated; ordinary missing checker dependencies are work to implement.

## Execution and verification

Astra owns scope, sequencing and final acceptance; reused GPT-6.1 Sol agents own core
Checker/Types and central builds, disjoint support counterparts with cross-review, and
verification. Start with coherent await/generator producers and contextual consumers,
then complete async iteration/resource handling and the source reconciliation. Support
and original test inventory proceed in parallel. Do not split production counterparts
into new authored mini-checkers. Focused differential witnesses run on coherent snapshots;
one comprehensive retained/native gate follows independent review. No redundant broad
gate before the source packet is ready; no new private cache/closure instrumentation
without a concrete correctness/resource reason.

The final whole-checker inventory is developed alongside this campaign using upstream's
actual test-file/configuration producers. Protocol-specific finite coverage is not a
substitute for the eventual complete no-emit inventory and gate. An inherited original
input that also emits still retains all applicable semantic assertions; omitting output
operations does not authorize forcing compiler options or dropping checking errors.
Source review, executed coverage and unresolved discrepancies remain distinct.

After this campaign passes and is committed, continue automatically to the next
source/dependency-aware checker campaign under the existing mandate. Final stopping
condition is the [whole-checker no-emit gate](CHECKER-COMPLETION-ROADMAP.md), not P1–P6.
