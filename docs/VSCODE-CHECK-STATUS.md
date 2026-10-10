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
