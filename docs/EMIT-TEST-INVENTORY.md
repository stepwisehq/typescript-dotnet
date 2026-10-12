# Whole emit inventory and acceptance

Pinned reference: `fed0bf24149fb1ed36039212648bafdafc1ea10e`, recorded in
[upstream.lock.json](../upstream.lock.json). Campaign baseline: `e3131a5b`.
This inventory defines whole-emit scope; the verified dependency packet below
is separate from pending whole-emit execution and acceptance.

## Direct upstream assertions

| Family | Original files | Top-level Test functions |
| --- | ---: | ---: |
| printer | 3 | 106 |
| sourcemap | 2 | 42 |
| transformers/tstransforms | 2 | 2 |

Printer includes printing/parenthesization, names and escaping utilities. Source map
includes 32 generator functions and 10 mapper functions. Transform functions are
data-driven type erasure and import elision; report executed data rows separately.
Port original assertions into shared `validation/Emit.Checks` code called by both
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
baseline assertions. Add representative transform behavior controls only after the
integrated pipeline exists; exact emitted Go output remains the primary oracle.

## Dependency and scope boundaries

Complete emission foundation and maps before full printer; complete transform
traversal/synthesis before script transforms. Preserve the original script order:
metadata, type erasure, import elision, runtime TypeScript syntax, legacy decorators,
JSX, target-dependent ES transforms, use-strict, module transforms, const-enum inlining.
Declaration transform and supplemental references require the checker resolver and
NodeBuilder projection APIs before Program orchestration can be accepted.

`printer.EmitResolver` has 38 directly declared methods plus six inherited binder
reference-resolver methods. This is a real checker integration dependency, including
seven declaration node-construction APIs, not a links-only adapter. Current diagnostic
NodeBuilder/printer support cannot establish declaration emission completeness.

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

## Registry preparation and verified dependency packet

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
timing dependency stays an explicit external disposition. These are inventories,
not evidence of execution or authorization for external project evaluation.

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
and seven original expansion failures with failure text. Full oracle export and
managed/AOT compiler-output comparison remain pending the genuine emission pipeline.

## Current printer and resolver gate

Final coordinated managed gate passed169 emit cases,832 checker,84 type-system and101 selected no-emit cases (1,186 total), zero failures/skips. Actual Linux x64 Native AOT executed149 original functions and46 authored logical controls through shared assertions. Original families are42 source-map,36 name-generator,70 remaining printer/utility and1 TypeEraser; original Go references cover863 leaf outcomes for those families. Function counts, table leaves, authored controls and managed test records are distinct.

[Source-bound proof](../validation/results/emit-printer-resolver-verified.json) retains earlier failures and compile-only attempts. Test4's sole authored comment expectation omitted one original indentation space; direct pinned Go and managed byte output matched before its fixture correction. All original assertions remain unchanged, zero originals pending/skipped. This subsystem gate does not establish whole JavaScript/declaration/Program output acceptance; full output corpus and remaining transform families remain pending.

Current complete TypeScript-transform dependency gate passed205 Emit xUnit records plus832 checker,84 type-system and101 selected no-emit (1,222 managed total), zero failures/skips. Actual Linux x64 Native AOT executed150 original functions and87 authored logical controls. Original ImportElision adds one function/20 unchanged table rows, explicitly adapting original fake host to genuine Program ModuleESNext/NoLib and node_modules/other; pinned Go confirms all20 expectations. Original reference leaves now883 for implemented test families. The new34 authored TS-stage cases compare exact outputs plus full public diagnostic fields and Config/Program(options)/Syntactic/Global/Semantic channels, strengthening historical code-only observations without recapturing output expectations. MultiMap adds seven authored public controls; no original direct test exists. Declaration emit diagnostics and remaining ES/JSX/module/Program outputs remain future work. [Source-bound proof](../validation/results/emit-ts-transforms-verified.json) preserves compile-only attempts and actual ILC dependency/ELF bindings.
