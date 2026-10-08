# Basic parser performance comparison

The current C# Native AOT parser took about **17% more elapsed time than Go** on
the selected real TypeScript workload: 60.4 versus 71.0 MiB/s. The small TSX fixtures
were close, with overlapping observed ranges. JSDoc and malformed-input workloads
took about 40% and 26% more time respectively. C# allocated more managed heap bytes
in every group. These are measurements of this port and its current public API on
one host, not intrinsic language-speed comparisons or full compiler measurements.

| Workload | Bytes / files | Go MiB/s median [min–max] | C# Native AOT MiB/s median [min–max] | C# / Go elapsed-time ratio | Go allocated bytes/input byte | C# allocated bytes/input byte |
| --- | --- | --- | --- | --- | --- | --- |
| Real TypeScript sources | 102,533 / 4 | 71.0 [66.8–75.8] | 60.4 [55.9–63.4] | 1.17× | 13.85 | 30.64 |
| TSX microfixtures | 687 / 3 | 38.4 [36.1–41.1] | 37.7 [35.0–38.1] | 1.02× | 30.52 | 72.52 |
| Eager JSDoc JS microfixtures | 1,312 / 7 | 25.3 [21.3–25.7] | 18.1 [17.4–18.4] | 1.40× | 41.35 | 237.78 |
| Malformed recovery/fuzz | 4,795 / 13 | 47.8 [39.1–48.8] | 37.9 [34.1–39.1] | 1.26× | 28.44 | 80.00 |

The larger TypeScript workload took a median 1.38 ms per four-file pass in Go and
1.62 ms in C#. Allocation volume was approximately 2.2× Go on that workload,
2.4× on TSX, 5.8× on JSDoc, and 2.8× on malformed input. Allocation volume is not
peak/live heap, RSS or total native memory. The TSX median difference is smaller
than the sample variation; these runs do not establish a meaningful 2% difference.

[Raw samples and environment](../benchmarks/parser-results.json) and the
[workload manifest](../benchmarks/parser-workloads.json) retain exact hashes,
options, modes, source cases and per-pass observations. MiB means 1,048,576 bytes.

## Scope and workload selection

All inputs come from the verified pinned parser corpus. The TypeScript group
contains an upstream parser harness and three TypeScript AST utility source files.
The other groups contain small conformance fixtures and retained fuzz regressions;
they are not representative applications or statistical samples of all programs.
Original per-file bytes, boundaries, logical filenames, script kinds and module
indicator options are retained. The real-source group produces one syntax diagnostic
in both implementations; the JSDoc group produces five JSDoc diagnostics and the
recovery group produces 46 diagnostics across its three channels.

The timed loop calls public `ParseSourceFile`, reads factory node/identifier/text
counters and root flags, and checksums ordered diagnostic codes/categories/spans.
It keeps each returned source file alive through observation. Factory node counts
include constructed nodes, not just unique reachable tree nodes. Every measured
pass is checked against its expected signature. No full-tree traversal or graph
serialization is included in timing.

File IO, initial source loading, JSON output, process startup, canonical verification,
explicit pre-sample GC and allocation-counter snapshots are outside timing. Natural
collections during measurement remain included. Timer checks and the small observer
are included; their overhead matters proportionally more for microfixtures and
diagnostic-heavy input. This is parse-plus-observation throughput, not isolated
parser instruction time or end-to-end application latency.

Ordinary eager JavaScript JSDoc parsing/reparsing is included. TypeScript lazy
JSDoc resolution is not forced, and binding, checking, resolution and emit are not
performed. The parser benchmark is a different operation and workload from the
[earlier scanner comparison](./PERFORMANCE.md); its ratios cannot quantify a
change in scanner performance.

## Compatibility and measurement controls

Before timing, the independent Go oracle and accepted Native AOT validation
executable agreed on 34 canonical cases: 27 with exactly the timed parsing options
and seven supplemental reads of eager JavaScript JSDoc attachments. They matched
13,807 reachable graph nodes and 57 diagnostics. Timed observer signatures were
derived from Go preflight output and matched all 40 samples across both binaries.
Full graph agreement establishes more than the cheap timed checksum alone.

The run used five alternating rounds per workload, with at least 150 ms warmup
and 300 ms measurement per process. Both native executables ran on allowed CPU0
of an AMD Ryzen 9 9950X under Linux 6.18.40.1 WSL2. Go1.27.1 used GOMAXPROCS=1,
GOGC=100 and GOMEMLIMIT=off. Native AOT10.0.12, published by SDK10.0.401, used
DOTNET_PROCESSOR_COUNT=1 and workstation GC. No inherited .NET runtime overrides
were present. Results report medians and observed ranges, not confidence intervals.

Allocation counters are process-wide Go `runtime.MemStats.TotalAlloc` and .NET
`GC.GetTotalAllocatedBytes(true)` deltas. Thread-local .NET counts and runtime GC
collection counts are also retained. GC generations and collection counts are not
directly interchangeable between runtimes. A single host and short repeated
samples limit generalization; large application and scaling behavior remain unknown.

The C# benchmark was actually Native AOT-published and executed. It is a stripped
ELF64 x86-64 PIE linked to libc/libm. The accepted parser source hash stayed
`d441e371cf28045777b3c8041d51e524ce96904ddecf6ff7e4f105edaf2edf8d` before and after.
Harness sources, validators, executable binaries, input bytes, workload configuration
and preflight assets were checked for drift. No accepted implementation, existing
correctness harness or historical scanner result was changed.

## Deferred questions, not measured causes

Several known adaptations are plausible contributors, but this benchmark does not
attribute elapsed time or allocation volume to them:

- C# converts the public byte-array input to its internal byte-preserving string on
  each parse; Go binds a string loaded before timing.
- C# creates a fresh parser and managed AST objects/lists, while Go retains its
  existing pooling and arenas.
- Copying slice adapters preserve Go parser-state semantics but may amplify list
  allocation and copying. AST payload wrappers and JSDoc cloning are other possible
  contributors; their shares are unmeasured.
- Scanner substring, builder and numeric-conversion questions from the earlier
  deferred backlog may also matter inside parsing, but scanner-only measurements
  cannot establish their contribution here.

No profiling, optimization, scaling experiment or deeper diagnosis was performed.
Future work could attribute allocation sources and test larger representative
programs, but those investigations remain deferred.

## Reproduce

Use the Linux x64 prerequisites in the [README](../README.md). A clean checkout
first needs the accepted native validator and its local evidence:

```sh
./tools/verify-parser.sh
```

Then run the separate performance harness:

```sh
./tools/parser-benchmark.sh
```

The benchmark verifies the pinned source/toolchain, publishes its own Native AOT
target, recreates workload files, performs canonical preflight and runs all40samples.
It uses the existing native compiler fallback when system clang is unavailable.
Durations/rounds can be deliberately overridden with `--sample-ms`, `--warmup-ms`
and `--rounds`. Results go to `benchmarks/parser-results.json`; original scanner
measurements remain separate. Independent review is recorded in
[review-parser-performance.md](./progress/review-parser-performance.md).
