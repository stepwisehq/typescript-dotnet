# Complete remaining ES runtime transforms

Prior candidate integrated correctness update: 2,223 managed records passed; actual Native Emit
passed 152 original functions and 251 authored controls; all 15,330 emit configurations
were observed, with only six reviewed external incremental metadata deltas visible.
All 13,571 original assertion sets and all 15,304 retained Native noEmit public records
match. Disk/guard/runtime controls passed 6/3/3. A reviewed nested WorkGroup starvation fix is now undergoing fresh coordinated
revalidation. Fresh managed/direct/publication gates and all 50 original workload
observations passed. Fresh Native corpus and command revalidation also passed;
full retained noEmit passed. Independent final reviews are clear; the coordinator accepted the contract-qualified scope. Benchmark summaries/allocation semantics are in the assessment. Earlier checkpoint/pending descriptions below are historical;
[assessment](../EMIT-ASSESSMENT.md) records current scope and freeze qualification.

From accepted TS checkpoint `b0cf6af4`, inventory owns all13 remaining ES counterparts: Async, ForAwait, Using, ObjectRestSpread, Exponentiation, LogicalAssignment, NullishCoalescing, OptionalCatch, OptionalChain, TaggedTemplate, UseStrict, Utilities and Definitions. Original source totals4,319 lines/165 functions; final mapping will count actual counterparts. Foundation owns ESDecorators/NamedEvaluation, maps owns ClassFields/ClassThis. Root coordinates complete-family integration; no independent .NET builds.

Shared namespace is `TypeScript.Transformers.EsTransforms`; package helpers live in internal static partial `EsTransforms`. Shared Utilities exposes actual class-conversion/accessor-backing helpers. Async and ForAwait share a `SuperAccessTransformer : Transformer` base containing the original embedded superAccessState storage/methods, avoiding wrapper forwarding and preserving separate initialization/state ownership. All definitions call genuine complete factory implementations in pinned target order, with no placeholder transforms.

Implementation is underway. Shared genuine Program controls will reuse accepted TS order and complete public diagnostic projection, then the real ES target chain. There are no original direct tests in the pinned ES transform package; authored differential controls remain distinct from later original compiler-output corpus acceptance. No behavior/AOT acceptance yet.

Complete165 ES functions plus8 OrderedSet functions and the demanded live-key iterator surface are source-written and independently reviewed in [es-runtime.json](../../artifacts/emit/source-review/es-runtime.json). Review caught required Slice capacity hints in ObjectRestSpread/Using; those exact owners are restored. Root caught one transient discarded backing-map allocation in hinted/clone OrderedSet construction; explicit constructors now retain exactly one real owner. Four central production compile attempts exposed syntax/declaration/API joins, all corrected as coherent batches; no behavior ran during those attempts. Newly owned ES files were formatted with the existing Roslyn tool before freeze.

Single reused-cache Go invocation captured80 actual full-stage observations:45 runtime/operator/target cases,21 class-field interactions and14 decorator interactions. All compare real TS→GetESTransformer(target)→UseStrict pipelines and target-aware printer output with full public diagnostic channels/fields. Root module/JSX/Program stages are later obligations. [Capture evidence](../../artifacts/emit/es-runtime/go-capture-result.json) binds exact source/options/output/diagnostics; three case tables are materialized by the small existing-style generator into shared assertions. Independent shared-runtime/decorator/fixture review is [es-shared-controls.json](../../artifacts/emit/source-review/es-shared-controls.json); independent class-field assertion review is [es-classfields-controls.json](../../artifacts/emit/source-review/es-classfields-controls.json).

Original Go TestOrderedSet and TestOrderedSetWithSizeHint both passed in the same cached invocation. Only the former's public assertions are ported; the latter's Go AllocsPerRun criterion remains explicitly Go-runtime-only evidence. CLR hinted1024 behavior is a separate authored control, with no allocation-count instrumentation. Expected current shared gate totals:287 xUnit records,151 original functions and168 authored logical Native controls; actual coordinated execution/AOT remains pending. Source/test/assembly metadata is frozen for root full-Emit gate. Mapping packet `/tmp/emit-es-runtime-source-map-entries.json` is ready for root integration.

Central full Emit test1 passed287, then retained832 checker/84 type-system/101 selected-noemit passed, total1,304 managed with zero failures/skips. Actual Linux x64 Native publish15.651s and execute4.106s exited0 with151 originalfunctions/168 authoredlogicalcontrols. [Current proof](../../validation/results/emit-es-transforms-verified.json) binds actual TRXs, ILC response/root/dependencies, ELF, source/assertion reviews and reference capture. Final independent execution-evidence ACK is pending; sources/assembly metadata remain frozen until root commit. Whole ES actual pinned count is407 functions across17 files; earlier NamedEvaluation19 estimate corrected to20 without source-body change.

Reproduce managed gate with `dotnet test tests/Emit.Tests/Emit.Tests.csproj -c Release --no-restore --nologo --logger "trx;LogFileName=es-transforms-1.trx" --results-directory artifacts/emit/es-transforms/managed`; retained exact commands and Native publish/execute command are bound in proof. Pinned Go observation replay uses the existing tool/cache from `upstream/TypeScript/tsc`:

```sh
GOCACHE=/home/greg/.cache/go-build \
EMIT_ES_CASES=/home/greg/projects/typescript-dotnet/tools/emit-oracle/es_runtime_cases.json,/home/greg/projects/typescript-dotnet/tools/emit-oracle/es_classfields_cases.json,/home/greg/projects/typescript-dotnet/tools/emit-oracle/es_decorator_cases.json \
/home/greg/projects/typescript-dotnet/tools/.cache/go/bin/go test \
-overlay /home/greg/projects/typescript-dotnet/artifacts/emit/es-runtime/overlay.json \
-json -run '^Test(AuthoredESFullTargetChains|OrderedSet|OrderedSetWithSizeHint)$' \
./internal/compiler ./internal/collections
```

The recorded initial observation command also set `EMIT_ES_CAPTURE=1` to populate new expectations; replay omits it to assert the frozen outputs/full diagnostics. No additional replay was run merely to aggregate evidence. Preserve frozen source metadata and actual Native dependencies per START-HERE when reproducing.
