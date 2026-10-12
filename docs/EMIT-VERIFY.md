# Emit verification

## Accepted whole-emit record

The ManyFiles workload exposed nested ThreadPool/Lazy starvation. The reviewed
WorkGroup fix lets callbacks and waiters claim ready actions exactly once, preserving
queue-start, wakeup and recursive completion/failure rules without changing the
workload or thread limits. Fresh build, all 2,223 managed records, six Native
publications and direct Native controls passed. Native ManyFiles completes, and the
isolated 50-run original workload cohort is complete. Fresh Native corpus, command and full retained noEmit revalidation passed; older results below remain explicitly prior-candidate evidence. See the [assessment](EMIT-ASSESSMENT.md) for exact timings/allocation ranges,
source adaptation and current limits.

The complete emit implementation passed final integrated verification and independent
review, and is accepted within the contract. Earlier accepted dependency results below remain historical evidence;
they do not establish whole-emit acceptance. Follow [STATE](STATE.md),
[scope](EMIT-CONTRACT.md), [implementation inventory](EMIT-IMPLEMENTATION.md) and
[test dispositions](EMIT-TEST-INVENTORY.md). Prior-candidate managed and Native emit/corpus/command gates completed as described below.
Prior-candidate retained noEmit also passed all 15,304 public records. Fresh retained noEmit also passed all 15,304 records. Independent final reviews are clear and the coordinator accepted the contract-qualified scope.

## Accepted final gate

The final [source-bound proof](../validation/results/emit-verified.json) records the
completed execution and the independently reviewed contract-qualified acceptance.

The [new gate](../artifacts/emit/final-acceptance-workgroup-fix/) passed fresh build,
all 2,223 managed records and six Native publications. Actual Native direct Emit
passed 152 original functions plus 251 authored controls; retained direct native
Foundation/utilities/Program controls also passed. These completed phases use freeze
hash `a111b465d715c41d88daa85e49ccd3a12c8ea9a41612112d4e2255fa2689f752`.
Fresh full Native emit and command phases passed under the same freeze. The corpus
observed all 15,330 records, matching 15,285 public outputs and 13,571 assertion sets;
only six reviewed external metadata differences remain visible. Commands passed all
6/3/3 disk/guard/runtime cases on attempt 1. Full retained noEmit passed all 15,304 exact public records.

The [workload cohort](../artifacts/emit/final-benchmarks-workgroup-fix/result.json)
completed all 50 runs. [Assessment](EMIT-ASSESSMENT.md) records every summary timing
and allocation range. Best Native/Go ratios are 1.629×, 1.406×, 1.403×, 2.250× and
2.112× for the three long-line sizes, 200-file variant and line-break variant. These
are observations without a performance threshold, before/after claim or new external
project comparison. Keep the stopped prior ManyFiles candidate as failure evidence.

## Prior candidate correctness evidence

The managed gate passed 2,223 records (493 Emit including 122 selected originals and
1,730 retained records). Six fresh Native publications bind every actual ILC root and
project dependency to rebuilt paths/hashes. Direct Native Emit passed 152 original
functions plus 251 authored controls; retained native Foundation passed 3,153 + 87,
utilities 20 and Program 9. The complete Native emit corpus observed all 15,330
configurations: 15,285 exact public matches, 13,571 original assertion sets, 39 original
preparation skips, 1,720 option policies and only six reviewed incremental metadata
differences. Comparator exit code 1 remains visible; the qualified gate requires that
each difference is exclusively the original external build-info owner and no original
channel fails. [Corpus result](../artifacts/emit/final-acceptance/native-emit-corpus/comparison.json).

Command attempt 2 passed six disk, three guard and three runtime observations. Attempt 1
failed in the Go-side Node observer on embedded quoting. The one-file JSON transport
fix changed only [runtime_controls.py](../tools/emit-oracle/runtime_controls.py), leaving
source fixtures, observer expressions, expectations and compiler binaries untouched.
The [recorded transition](../artifacts/emit/final-acceptance/command/attempt-1/harness-only-transition.json)
preserves earlier phase freeze hash `ca3a8a541f2c9795412ef3ee0464062cdda8e5e007ce1393bab9502116cf47a9`;
command attempt 2 uses `d00e7738cd6294826ea8a2a821877cd3eee98c31bf41dc8d41a1a4477196fea8`.
Earlier completion records were not rewritten and no build/corpus reran for this fix.
Retained Native noEmit matched all 15,304 complete public records
([comparison](../artifacts/emit/final-acceptance/noemit/attempt-1/comparison.json)).
The reviewed in-repository benchmark cohort and final proof/commit closeout follow.

## Final gate reproduction

Run from the repository root with the .NET 10 SDK and Linux x64 Native toolchain.
The coordinator freezes source, assets and build metadata before publication and
records actual ILC root/compiler/dependency inputs. Refresh current managed and
RID-specific dependencies first; an old RID assembly must not survive into a new
publication. Do not commit or rebuild consumed binaries during corpus execution.
The commands below designate fresh final outputs, not earlier checkpoint binaries.
Emit.Validation explicitly uses server GC; retained validators preserve their
existing project policies, and the actual ILC runtime knobs are recorded.
The coordinator recipe [run.py](../artifacts/emit/final-acceptance-workgroup-fix/run.py) applies
these stages with fresh dependency/ILC binding, exact count/flag checks and all
15,304 noEmit comparisons. Run its phases in order (`build`, `managed`, `publish`,
`native`, `corpus`, `command`, `noemit`); it refuses overwritten attempt logs and
requires each earlier phase to pass. All phases completed; their evidence is recorded separately.

```sh
mkdir -p artifacts/emit/final

dotnet build tests/Emit.Tests/Emit.Tests.csproj -c Release --nologo
EMIT_CORPUS_ROOT="$PWD" \
EMIT_CORPUS_REQUESTS="$PWD/artifacts/emit/final-managed/requests.jsonl" \
EMIT_CORPUS_REFERENCE="$PWD/artifacts/emit/whole-reference/original.jsonl.gz" \
EMIT_CORPUS_ACTUAL="$PWD/artifacts/emit/final/direct-discovery-actual.jsonl" \
dotnet test tests/Emit.Tests/Emit.Tests.csproj -c Release --no-build --no-restore \
  --filter 'FullyQualifiedName!~OriginalProgramOutputsDiagnosticsAndInvariants' \
  --logger 'trx;LogFileName=direct.trx' --results-directory artifacts/emit/final/managed

dotnet publish validation/Emit.Validation/Emit.Validation.csproj \
  -c Release -r linux-x64 -p:PublishAot=true \
  -p:OptimizationPreference=Speed -p:UseSizeOptimizedLinq=false \
  -p:ServerGarbageCollection=true -o artifacts/emit/final/direct-native
artifacts/emit/final/direct-native/Emit.Validation

dotnet publish validation/Checker.NoEmit.Validation/Checker.NoEmit.Validation.csproj \
  -c Release -r linux-x64 -p:PublishAot=true \
  -p:OptimizationPreference=Speed -p:UseSizeOptimizedLinq=false \
  -o artifacts/emit/final/corpus-native

dotnet publish src/TypeScript.Check/TypeScript.Check.csproj \
  -c Release -r linux-x64 -p:PublishAot=true \
  -p:OptimizationPreference=Speed -p:UseSizeOptimizedLinq=false \
  -p:ServerGarbageCollection=true -o artifacts/emit/final/cli-native
```

Use the established compiler/linker fallback property if the host lacks installed
clang; [PROJECTS](PROJECTS.md) records that existing adaptation. Direct Native runs
share the original/assertion bodies with xUnit. The filtered direct xUnit command
still supplies all four corpus paths because MemberData discovery can require them;
the coordinated all-493 managed run used the same explicit environment.
Benchmark invocations are separate
observations and never added to assertion counts.

For a fresh checkout, regenerate the registry, unchanged original reference,
eleven-record supplement and exact 122-row selection using
[EMIT-REFERENCE](EMIT-REFERENCE.md). Do not regenerate during a frozen gate.

The complete original registry has 15,330 configurations. Use the existing
bounded orchestration: 100 requests per fresh process, compressed observations and
one complete comparison with the unchanged original record supplement. The result
directory must be new.

```sh
python3 tools/run-emit-corpus.py \
  artifacts/emit/final/corpus-native/Program.Checks \
  artifacts/emit/reference-infrastructure/registry/requests.jsonl \
  "$PWD" artifacts/emit/final/native-emit-corpus --batch-size 100
```

The comparator checks raw public output/result/diagnostics and every original
formatted baseline/assertion channel, including declaration recompile/noCheck,
source-map records, errors, content mapper, resolution and type/symbol observations.
The eleven-record supplement repairs original source-map-record tracking filename
collisions without changing the original assertions or outputs. Preserve option
policies, 39 preparation skips, eight output-only skips and seven expansion failures
separately. Exact incremental-wrapper build-info differences remain visible blocked
originals; comparator nonzero for those qualified external-owner differences is not
an unconditional pass. It must not conceal another output or channel discrepancy.

The selected original xUnit batch requires the four environment paths. The
coordinator supplies a frozen selection of genuine full-harness pass identities
from the same registry/reference, excluding qualified incremental-wrapper rows;
`emit_selected_requests` below names the prepared 122-row selection. No expected
behavior is regenerated to make a failing row pass.

```sh
emit_selected_requests="$PWD/artifacts/emit/final-managed/requests.jsonl"
EMIT_CORPUS_ROOT="$PWD" \
EMIT_CORPUS_REQUESTS="$emit_selected_requests" \
EMIT_CORPUS_REFERENCE="$PWD/artifacts/emit/whole-reference/original.jsonl.gz" \
EMIT_CORPUS_ACTUAL="$PWD/artifacts/emit/final/selected-original-actual.jsonl" \
dotnet test tests/Emit.Tests/Emit.Tests.csproj -c Release --no-build --no-restore \
  --filter 'FullyQualifiedName~OriginalProgramOutputsDiagnosticsAndInvariants' \
  --logger 'trx;LogFileName=original.trx' --results-directory artifacts/emit/final/managed
```

Actual disk and runtime gates use the production Native command and retained Go
executable/reference. Boundary guards are explicitly authored policy observations,
not Go-equal incremental output assertions:

```sh
python3 tools/emit-oracle/command_controls.py \
  --executable artifacts/emit/final/cli-native/tscheck \
  --reference artifacts/emit/command-reference/go-controls.json \
  --result artifacts/emit/final/native-command.json
python3 tools/emit-oracle/command_guard_controls.py \
  --executable artifacts/emit/final/cli-native/tscheck \
  --result artifacts/emit/final/native-command-guards.json
python3 tools/emit-oracle/runtime_controls.py \
  --go-executable artifacts/emit/command-reference/tsgo \
  --executable artifacts/emit/final/cli-native/tscheck \
  --result artifacts/emit/final/native-runtime.json
```

The three write-failure/raw-byte assertions and four direct public Emit mode
controls already execute in the shared direct Native/xUnit bodies. Runtime controls
copy three upstream fixtures unchanged and compare all emitted bytes plus the same
Node exit/stdout/stderr; their runtime observers are authored. No external project,
VS Code prerequisite or new profiling run belongs to this gate.

Retained correctness uses the accepted delivery recipe: all Checker/TypeSystem
managed tests, the original selected Parser foundation/allocation tests and Program
pool/construction/runtime-policy tests, selected noEmit controls, Native foundation
and shared byte/slice/pool modes, utilities/Program checks, and all 15,304 original
Native noEmit records. [The prior accepted recipe](../artifacts/vscode-allocation-rollout/integration-31-production-delivery/build-1/regressions/selected-result.json)
records exact filters and commands. Adapt only output paths and fresh binaries;
[the prepared retained plan review](../artifacts/emit/source-review/retained-command-plan.json)
verifies the unchanged request identities/filters. Original noEmit reference data
remains under `validation/checker-noemit-corpus`; its exact records and original
parent/union assertions are mandatory alongside emit evidence.

Optional performance preservation observations use only the existing original
compiler workloads. Run the original Go benchmarks unchanged and the corresponding
Native names with equal repeats; retain every result, without inventing an emit
performance threshold or asserting allocation identity:

```sh
(cd upstream/TypeScript/tsc && ../../../tools/.cache/go/bin/go test \
  -run '^$' -bench '^BenchmarkEmit(LongLines|ManyFiles|LongLinesWithLineBreaks)$' \
  -benchtime=1x -count=5 ./internal/compiler)
artifacts/emit/final/direct-native/Emit.Validation --benchmark LongLines1000 1
artifacts/emit/final/direct-native/Emit.Validation --benchmark LongLines5000 1
artifacts/emit/final/direct-native/Emit.Validation --benchmark LongLines10000 1
artifacts/emit/final/direct-native/Emit.Validation --benchmark ManyFiles 1
artifacts/emit/final/direct-native/Emit.Validation --benchmark LongLinesWithLineBreaks 1
```

Repeat each Native command five times when comparing that five-run Go cohort.
These timers exclude fixture/Program preparation and include actual repeated Emit
with the original discarded-write callback. Existing no-GC policy/performance
artifacts remain preserved; these observations do not reopen the optimization epic.

The final development mapper repair matched all nine original channels for its six
unchanged requests (1.540s). The original Go
`TestContentMapperLoggerEnvironmentVariable` passed in `./internal/execute/tsc`
(package 0.004s, command 4.247s); see
[the logger evidence](../artifacts/emit/original-command-logger/). These bounded
results do not replace the final gates above.

## Historical synthesis/source-map prerequisite gate

Run from the repository root with the existing .NET 10 SDK and Linux x64 Native
AOT prerequisites. Restore is implicit in the first build:

```sh
dotnet build tests/Emit.Tests/Emit.Tests.csproj -c Release --nologo
dotnet test tests/Emit.Tests/Emit.Tests.csproj -c Release --no-build --no-restore --nologo
dotnet publish validation/Emit.Validation/Emit.Validation.csproj \
  -c Release -r linux-x64 -p:PublishAot=true \
  -p:OptimizationPreference=Speed -p:UseSizeOptimizedLinq=false \
  -p:ServerGarbageCollection=true -o artifacts/emit/foundation/native
artifacts/emit/foundation/native/Emit.Validation
```

Accepted result: 90 managed cases; native exit0 with 78 original test functions
(42 source-map, 36 generated-name) and30 authored controls. Original functions,
data rows and assertion counts are distinct. Both runners execute the same shared
assertions. Original pinned-Go tests also passed; see the provenance inventory.
Independent review covered original assertions, source algorithms/adaptations,
actual native publication and the evidence manifest.

Retained regressions at this prerequisite checkpoint:

```sh
dotnet test tests/Checker.Tests/Checker.Tests.csproj -c Release --nologo
dotnet test tests/TypeSystem.Tests/TypeSystem.Tests.csproj -c Release --nologo
CHECKER_NOEMIT_ROOT="$PWD" \
CHECKER_NOEMIT_REQUESTS="$PWD/validation/checker-noemit-corpus/requests.jsonl" \
CHECKER_NOEMIT_REFERENCE="$PWD/validation/checker-noemit-corpus/expected.jsonl.gz" \
CHECKER_NOEMIT_PREPARATION_DISPOSITIONS="$PWD/validation/checker-noemit-corpus/preparation-dispositions.json" \
CHECKER_NOEMIT_ACTUAL="$PWD/artifacts/emit/foundation/noemit-actual.jsonl" \
dotnet test tests/Checker.NoEmit.Tests/Checker.NoEmit.Tests.csproj -c Release --nologo \
  --filter 'FullyQualifiedName!~OriginalNoEmitDiagnosticsQueriesAndInvariants'
```

Accepted retained results: 832 checker,84 type-system,101 selected no-emit cases;
combined with emit,1,107 passed with no failures/skips. The filter excludes the
complete original compiler corpus at this prerequisite checkpoint. It does not
remove that later integrated acceptance obligation. Existing corpus regeneration
instructions remain in the no-emit documentation; do not regenerate expected
behavior to accommodate a failure.

Three central managed build attempts resolved namespace/accessibility/overload
integration errors; the third passed with zero warnings. Native publication
succeeded on its first attempt. Existing nullable warnings in retained observation
harnesses are separate from the production build. Token/cost measurements are not
available. No external-project or performance run was performed.

Full printer tests, actual JS/DTS/map outputs, original compiler configurations,
final Native AOT corpus comparison and comprehensive retained gates remain ahead.
The prepared reference registry and one successful original Go smoke establish
oracle readiness only. Linux x64 is the only verified native platform.

## Full printer and semantic dependency checkpoint

The same commands now exercise the complete printer, genuine emit resolver and
non-hover declaration serialization, shared transform utilities/destructuring and
TypeEraser. Use `artifacts/emit/printer-resolver/native` as the publish/output path.
Managed results are 169 emit cases plus the unchanged retained gates above
(832 checker,84 type-system,101 selected no-emit): 1,186 passed, zero failed/skipped.
Actual Linux x64 Native AOT executed149 original functions and 46 authored controls.
The 149 originals comprise 42 source-map, 106 printer (including 36 names), and one TypeEraser function;
the original TypeEraser function preserves its 69 table rows. Pinned Go reference
leaf outcomes across these families total 863, separate from function/xUnit counts.

[Bound dependency evidence](../validation/results/emit-printer-resolver-verified.json)
records exact commands, raw TRX, actual ILC input assemblies, native binary/run,
source/assertion reviews and Go witnesses. Genuine Program resolver controls and
reproduction are in [semantic progress](progress/emit-resolver.md).
Five central managed test invocations followed three diagnostic build attempts;
failed compilation and authored fixture corrections remain recorded. The final
169-case run passed. Native publication succeeded on its first attempt (20.206s).
[Independent acceptance review](../artifacts/emit/printer-resolver/acceptance-independent-review.json)
confirmed the execution evidence and scoped completion claim.

This is prerequisite acceptance: most TS transforms, the ES/JSX/module/declaration
families and Program output are still ahead. Three hover-only serialization methods
remain outside emit; only one RuntimeSyntax leaf exists until the next family.
Full original no-emit and complete output-corpus gates remain final integrated
obligations. No external-project or new performance measurements were run.

## Complete TypeScript transform family

Use the same commands above with output path `artifacts/emit/ts-transforms/native`.
The integrated managed run passed 205 emit cases; retained gates passed 832 checker,
84 type-system and 101 selected no-emit cases: 1,222 total, no failures/skips.
Actual Linux x64 Native AOT publication passed in 15.602s; execution passed in
1.397s with 150 original test functions and 87 authored controls.

[Evidence](../validation/results/emit-ts-transforms-verified.json) binds source reviews,
raw TRX, reference observations, actual ILC inputs and the executable. The original
ImportElision function's 20 rows retain exact outputs with a documented genuine
Program host adaptation. Authored runtime/decorator/metadata controls (34 cases)
compare output and full public diagnostic fields/channels, including recursive
messages and related information; only fixture-root relocation is normalized.
Seven authored MultiMap controls cover public behavior. Original output-only tests
are not represented as semantic/compiler acceptance. Declaration-emit diagnostics
remain a later dependency; full output-corpus and original no-emit acceptance remain
mandatory at final integration.

Initial build and two test attempts failed compilation at API/interface joins; no
assertions executed then. The third test attempt passed, followed by first-attempt
retained gates and Native publication. No external-project/performance run occurred.

## Complete ES transform family

Use the same commands with output path `artifacts/emit/es-transforms/native`.
First full managed behavior run: 287 emit cases passed; retained gates passed
832 checker, 84 type-system and 101 selected no-emit cases (1,304 total, none
failed/skipped). Actual Linux x64 Native AOT published in 15.651s and executed
151 original functions plus 168 authored controls in 4.106s, exit0.

[Bound proof](../validation/results/emit-es-transforms-verified.json) includes exact
commands, reviews, raw TRX, current actual ILC inputs and executable. The 80 new
comparisons (45 runtime, 21 class fields, 14 decorators) use genuine pinned libraries,
Program/resolver and target-selected chains, exact output and five full public
diagnostic channels. Successful Go observation capture is not an original baseline
assertion; agreement is established by the managed and native comparisons.
Original OrderedSet public assertions are shared by both runners. Its unchanged
Go allocation-count test passed only in Go; a CLR hinted-size behavior control does
not satisfy that allocation assertion.

Four central production diagnostic builds resolved syntax and API/declaration joins;
the full test invocation was the first behavior run and passed. Source review also
restored explicit upstream capacities and cached callbacks. Retained and Native gates
passed on their first attempts. No source edits followed the managed run. Complete
compiler-output/no-emit corpus, JS runtime execution and Program output remain final
integration obligations; no external-project or performance measurement was made.

## Complete JSX, module and const-enum family

Use the same commands with output path `artifacts/emit/script-transforms/native`.
Final managed run:362 emit cases; retained832 checker,84 type-system,101 selected
no-emit:1,379 passed, no failures/skips. Actual Linux x64 Native AOT published in
15.785s and executed151 original functions plus243 authored controls in6.574s, exit0.

[Proof](../validation/results/emit-script-transforms-verified.json) records full
commands, TRX, source/assertion reviews, Go observations, actual ILC dependencies
and executable. The75 added authored cases are21 shared modules,23 CommonJS and
31 JSX/inliner cases. Exact output and five full public diagnostic channels are
compared under equivalent genuine Program options/files/packages/libraries. Original
compiler-corpus expected outputs and declaration-emit diagnostics remain later gates.

Two central production builds resolved ordinary API/type joins. First test attempt
failed at absent-option fixture defaults and overlapped an ingress edit; it is not
accepted frozen evidence. Second attempt exposed two const-enum borrowed-string
consumer failures. The reviewed fix uses the existing borrowed literal factory;
reference outputs and diagnostics were unchanged. Third attempt and subsequent
retained/Native gates passed. No external-project/performance run occurred.
