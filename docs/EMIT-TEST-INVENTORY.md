# Whole emit inventory and acceptance

## Accepted whole-emit record

The ManyFiles workload exposed nested ThreadPool/Lazy starvation. The reviewed
WorkGroup fix lets callbacks and waiters claim ready actions exactly once, preserving
queue-start, wakeup and recursive completion/failure rules without changing the
workload or thread limits. Fresh build, all 2,223 managed records, six Native
publications and direct Native controls passed. Native ManyFiles completes, and the
isolated 50-run original workload cohort is complete. Fresh Native corpus, command and full retained noEmit revalidation passed; older results below remain explicitly prior-candidate evidence. See the [assessment](EMIT-ASSESSMENT.md) for exact timings/allocation ranges,
source adaptation and current limits.

Pinned reference: `fed0bf24149fb1ed36039212648bafdafc1ea10e`, recorded in
[upstream.lock.json](../upstream.lock.json). Campaign baseline: `e3131a5b`.
This inventory records whole-emit scope and current execution dispositions. Completed
gates below remain distinct from final campaign acceptance.

## Prior candidate correctness dispositions

The final managed gate passed 2,223 records with zero failures/skips: 493 Emit records
(371 direct/data records plus 122 selected original compiler configurations), Checker
832, TypeSystem 84, selected Parser 695, selected Program 18 and selected noEmit 101.
The actual Native direct assertion executable passed 152 original functions and 251
authored controls. Counts of functions, data rows, compiler configurations and authored
controls are intentionally separate.

The complete original reference and actual Native consumer each observed 15,330
configurations. Dispositions: 13,571 complete original assertion sets, 1,720 unchanged
original option-policy records and 39 preconfiguration skips. Eight output-only skips
remain separate from other assertion channels; seven original expansion failures remain
in the fixture inventory. The Native comparison has 15,285 exact public matches and
six visible incremental-owned build-info metadata differences, with no missing rows or
other mismatches. All nine-channel assertion sets for those six still match; their raw
public Program comparisons remain blocked, never silently passing. Seven configurations
require the original incremental wrapper, including one whose ordinary output matches.

Native command attempt 2 passed six exact-Go disk controls, three explicit boundary
guards and three generated-JS runtime observations. The latter are authored observers
over unchanged upstream fixtures, not original baseline assertions. Runtime attempt 1
failed before completing observation on embedded Node quoting; its one-file tool-only
freeze transition and original failures remain recorded. Compiler inputs, expectations
and binaries did not change, and earlier completion freeze hashes were preserved.

Retained full Native noEmit passed all 15,304 complete public records. The original workload cohort is complete; fresh repair-candidate correctness revalidation also passed in full. Independent final reviews are clear and the contract-qualified scope is accepted; see the [final proof](../validation/results/emit-verified.json). Reproduction from a
fresh checkout is in [EMIT-REFERENCE](EMIT-REFERENCE.md); current corpus evidence is in
[the final comparison](../artifacts/emit/final-acceptance/native-emit-corpus/comparison.json).

The total direct original inventory is 154 functions: printer 106, source map 42,
TS transform 2, OrderedSet 2 and command emit 2. Of these, 152 portable functions
execute in shared managed/Native assertions. Original TestOrderedSetWithSizeHint is
Go-only allocation evidence; original incremental declaration timing is outside the
builder-free contract. Neither is relabeled as a CLR assertion pass. The three
compiler benchmarks are separately inventoried performance workloads.

## Direct upstream assertions

| Family | Original files | Top-level Test functions |
| --- | ---: | ---: |
| printer | 3 | 106 |
| sourcemap | 2 | 42 |
| transformers/tstransforms | 2 | 2 |
| collections ordered-set portable assertions | 1 | 1 |
| execute/tsc portable logger assertion | 1 | 1 |

Printer includes printing/parenthesization, names and escaping utilities. Source map
includes 32 generator functions and 10 mapper functions. Transform functions are
data-driven type erasure and import elision; report executed data rows separately.
Original portable assertions are ported into shared `validation/Emit.Checks` code called by both
xUnit and the actual Native AOT executable. Authored controls remain separately
identified. Existing accepted compiler/AST/Program regressions remain required.

## Compiler configurations and output assertions

The existing inventories contain 12,824 unchanged regression/conformance fixtures.
The historical raw inventory expands to 15,190 configurations; the decoded inventory
expands to 15,330. Use the latter decoded fixture/configuration identity and unchanged
original configuration producers; the previously accepted 15,304 no-emit rows are
not an emit registry or evidence of output correctness.

Reuse the original compiler runner and `harnessutil.CompileFiles` through additive Go
overlay observation, retaining configuration names, options, file bytes, roots,
symlinks, libraries and harness options. Preserve original pre-configuration skips,
preparation failures and channel-specific skips explicitly. The runner's eight
`skippedEmitTests` affect the output baseline only; they must not suppress map or
diagnostic channels. Keep original baselines and actual-Go comparisons distinct.

The pinned full-output harness rejects AMD and outFile and skips UMD/System,
Node10/Classic resolution, explicit false interop/default-import options, baseUrl,
ES5 and explicit alwaysStrict=false. Inventory those original dispositions rather
than broadening this Go-faithful epic to historical JavaScript compiler capabilities.
Their retained checking observations remain valid no-emit regressions.
Each policy branch is backed by a removed-option diagnostic in pinned
`compiler/program.go`; the eight output skips are instead harness-only dispositions.

The full harness adds operations excluded by the no-emit bridge:

- Pre-emit declaration diagnostics and a separate post-emit Program; actual Emit,
  declaration diagnostics after emit, and the original pre/post diagnostic invariant.
- Exact JS, declaration and map output file names, contents, order and file sets,
  emit result skipped state, declaration diagnostics, emitted paths and source maps.
- Original JS output baseline, including declaration output compilation, declaration
  cardinality/emit-only invariants, and `noCheck` repeated compilation comparisons.
- Original sourcemap baseline and decoded sourcemap record baseline, including inline
  maps, declaration maps, sources/content/name indexes and mapped source positions.
- Existing error, type/symbol, content-mapper and resolution baselines where the
  original runner invokes them; original parent and union-order assertions retained.

Generated JS execution checks are authored runtime evidence, not original compiler
baseline assertions. The three runtime controls use the complete integrated pipeline; exact emitted Go
output remains the primary oracle.

## Dependency and scope boundaries

Complete emission foundation and maps before full printer; complete transform
traversal/synthesis before script transforms. Preserve the original script order:
metadata, type erasure, import elision, runtime TypeScript syntax, legacy decorators,
JSX, target-dependent ES transforms, use-strict, module transforms, const-enum inlining.
Declaration transform and supplemental references require the checker resolver and
NodeBuilder projection APIs before Program orchestration can be accepted.

`printer.EmitResolver` has 38 directly declared methods plus six inherited binder
reference-resolver methods. This is a real checker integration dependency, including
seven declaration node-construction APIs, not a links-only adapter. The completed non-hover NodeBuilder and actual declaration transform are integrated;
older diagnostic-only support was insufficient for this responsibility.

Program emission includes target-file selection, force/emit-only modes, writer and
checker ownership, output paths, write callbacks/errors, noEmit/noEmitOnError and
declaration diagnostics. Build/watch orchestration and language services stay outside
this epic; incremental fixtures that invoke builder wrappers must receive explicit
dispositions and retain reusable full Program assertions. Builder-signature mode is
an emitter API dependency, not authorization for build/watch implementation.

Whole-emit acceptance requires independent source review, integrated managed xUnit,
original upstream inventories and full output channels, and actual Linux x64 Native
AOT executing the shared assertions and compiler registry. No external project runs
precede this acceptance. The dependency checks below do not establish whole-emit
acceptance.

## Registry preparation and historical dependency evidence

`tools/prepare-emit-registry.py DESTINATION` validates the accepted decoded fixture
hashes and emits the full configuration requests, fixture dispositions, direct
function inventory and source-qualified manifest. It does not execute compilation.
`tools/prepare-emit-overlay.py OUTPUT` creates an additive overlay exposing
`TestAuthoredWholeEmit`. The overlay reuses the accepted diagnostic projection and
calls unchanged original compiler preparation and every original assertion method.
Enable original `TSGO_BASELINE_TRACKING_DIR` and supply `EMIT_REQUESTS`/`EMIT_OUTPUT`
when integration authorizes compilation/execution. Original baseline failures remain
failures; generated contents and original local/reference origin are captured through
the existing tracking facility, without replacing baseline assertions.

Pinned `compiler/emit_test.go` supplies three in-repository benchmarks,
`BenchmarkEmitLongLines`, `BenchmarkEmitManyFiles` and
`BenchmarkEmitLongLinesWithLineBreaks`, without direct Test functions.
The `execute/tsc` emit tests include `TestContentMapperLoggerEnvironmentVariable`
and `TestIncrementalDeclarationEmitTimeIsExcludedFromCheckTime`; the latter's builder
timing dependency stays an explicit external disposition. All three original in-repository benchmarks ran through five workload variants,
with five fresh-process repeats per runtime: 50 successful observations. Those
performance observations do not add assertion functions or xUnit records. The portable logger original passed in Go and shared managed/Native
assertions. The incremental timing test remains explicitly external. These do not
authorize external project evaluation.

The first dependency packet executed all42 source-map functions (49 original leaf
rows), all36 name-generator functions, and30 separately authored controls. Managed
xUnit passed90 rows with0 skipped; the actual Linux x64 Native AOT executable ran
78 original functions and30 controls through identical shared bodies. Pinned Go
references also passed42 source-map functions/49 leaf cases and36 selected name
functions. This does not claim the remaining70 printer functions or transform
functions have executed. See the [resumable test checkpoint](progress/emit-tests.md)
and [independent source reviews](../artifacts/emit/source-review/).

Final additive Go harness compilation and one representative original configuration
passed all nine original assertion channels, observing one JS output and five original
baseline records; [source-bound evidence](../artifacts/emit/reference-infrastructure/result.json)
verifies reference machinery. The registry preserves12,824 fixtures/15,330 configurations
and seven original expansion failures with failure text. At that historical stage full oracle export and compiler-output comparison were
pending. The current full original/Native corpus results above supersede that state.

## Historical dependency gates

Final coordinated managed gate passed169 emit cases,832 checker,84 type-system and101 selected no-emit cases (1,186 total), zero failures/skips. Actual Linux x64 Native AOT executed149 original functions and46 authored logical controls through shared assertions. Original families are42 source-map,36 name-generator,70 remaining printer/utility and1 TypeEraser; original Go references cover863 leaf outcomes for those families. Function counts, table leaves, authored controls and managed test records are distinct.

[Source-bound proof](../validation/results/emit-printer-resolver-verified.json) retains earlier failures and compile-only attempts. Test4's sole authored comment expectation omitted one original indentation space; direct pinned Go and managed byte output matched before its fixture correction. All original assertions remain unchanged, zero originals pending/skipped. This subsystem gate does not establish whole JavaScript/declaration/Program output acceptance; full output corpus and remaining transform families remain pending.

Current complete TypeScript-transform dependency gate passed205 Emit xUnit records plus832 checker,84 type-system and101 selected no-emit (1,222 managed total), zero failures/skips. Actual Linux x64 Native AOT executed150 original functions and87 authored logical controls. Original ImportElision adds one function/20 unchanged table rows, explicitly adapting original fake host to genuine Program ModuleESNext/NoLib and node_modules/other; pinned Go confirms all20 expectations. Original reference leaves now883 for implemented test families. The new34 authored TS-stage cases compare exact outputs plus full public diagnostic fields and Config/Program(options)/Syntactic/Global/Semantic channels, strengthening historical code-only observations without recapturing output expectations. MultiMap adds seven authored public controls; no original direct test exists. Declaration emit diagnostics and remaining ES/JSX/module/Program outputs remain future work. [Source-bound proof](../validation/results/emit-ts-transforms-verified.json) preserves compile-only attempts and actual ILC dependency/ELF bindings.

Complete ES dependency gate passed287 Emit xUnit records plus832 checker,84 type-system and101 selected no-emit (1,304 managed total), zero failures/skips. Actual Linux x64 Native AOT executed151 original functions and168 authored logical controls. All17 pinned ES source files/407 functions are covered (NamedEvaluation actual20, correcting an earlier19 estimate). Eight OrderedSet support functions and the narrow OrderedMap live-key enumerator join are reviewed. Original TestOrderedSet public assertions add one portable function; original TestOrderedSetWithSizeHint passed unchanged in Go but its allocation assertion remains Go-runtime-only. The separate CLR hinted1024 behavior is authored. Implemented original reference leaves now884; Go-only allocation reference function remains separately counted.

The new80 authored cases (runtime45/class-fields21/decorators14) compare actual target-selected TS→ES→UseStrict bytes plus complete public diagnostics, retaining diagnosed inputs and all observed fields/channels. The ES package has no direct original tests; these controls are stage differential evidence and do not replace original compiler output corpus acceptance. [Bound proof](../validation/results/emit-es-transforms-verified.json) preserves four compile-only attempts, independent reviews and actual ILC inputs/ELF. JSX/module/inliner/declaration and Program output remain subsequent work.

The complete script-transform family now passes362 Emit xUnit records plus832 checker,
84 type-system and101 selected no-emit regressions (1379 total, zero failures/skips).
Actual Linuxx64 Native AOT executes151 original functions and243 authored controls.
This family adds75 authored exact-Go full-chain output/full public diagnostic cases:
shared modules21, CommonJS23, JSX/inliner31. No original direct tests exist for these
transform packages. Existing original assertions/function counts remain unchanged.
Genuine virtual filenames (.mts/.cts/.js/.tsx), package.json and auxiliary module/JSON
inputs use the existing baseline filesystem; raw byte ingress is explicit. Chain
order: TS, conditional JSX, target-selected ES, UseStrict, module selection, conditional
const-enum inlining last. Full public diagnostic contract remains unchanged.

Development evidence is retained separately: production build1 API joins, successful
build2; Go observer compile-only field casing correction before75 successful captures;
managed test1 prior287 passed/new75 failed before transforms on optional empty enums
(with a crossed sourcefreeze/ingress fix, so no frozen acceptance claim); test2 had360
pass/two genuine borrowed-string inliner failures; independently reviewed fix then
all362 passed test3. References and expected outputs/diagnostics never changed.
Final evidence: validation/results/emit-script-transforms-verified.json. Declaration,
compiler Program output, original whole corpus/runtime/declaration recompile and
output/map assertions remain outstanding. This is subsystem acceptance only.

The original whole-output reference now contains all15,330 configurations:13,571
full-harness assertion passes,1,720 original option-policy dispositions and39 original
preconfiguration skips. Eight output-channel-only skips remain distinct; seven expansion
failures are separately inventoried. Eleven source-map-record filename collisions were
recovered in one fresh process with exact source/configuration identities; the main
reference is unchanged. The managed consumer asserts all nine original channels,
including genuine declaration recompilation/noCheck repeats and formatted baselines.

Development repair83 matched all 79 originally passing assertion sets and 77 raw
results, leaving only the six external build-info metadata cases. The full managed
development run then exposed six content-mapper formatted baselines; exact upstream
repairs passed the six-case retest. Final Native corpus comparisons now match every
in-scope public record and all 13,571 original assertion sets. Separate authored public
Program mode 4, actual write-failure/raw-byte 3 and portable logger original 1/authored
1 controls passed through the final managed and actual Native assertion bodies.
