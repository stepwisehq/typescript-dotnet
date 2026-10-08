# Parser performance review

Independent bounded review of [C# adapter](../../benchmarks/Parser.Benchmarks/Program.cs),
[Go adapter](../../tools/parser-benchmark/main.go),
[runner](../../tools/parser-benchmark.py) and
[40 recorded samples](../../benchmarks/parser-results.json). No blocking finding.
Accepted source remained `d441e371cf28045777b3c8041d51e524ce96904ddecf6ff7e4f105edaf2edf8d`.
No accepted implementation/tests/validation or historical results changed; no
optimization, profiling, diagnostic experiments or additional benchmark runs.

## Checked agreement and measurement scope

All four groups retain original bytes, file boundaries, filenames, script kinds
and module options from verified cases. The real TS group is four files/102,533
bytes; TSX is three microfixtures/687 bytes; eager-JSDoc JS is seven/1,312 bytes;
malformed recovery is 13/4,795 bytes, including empty files and mixed modes.
These are selected corpora, not representative distributions of all programs.

Canonical preflight passed 34 structural cases: 27 matching timed options and
seven supplemental eager-JS JSDoc reads. Primary `resolveJSDoc=false` matches the
timed API path. TS lazy JSDoc is not forced; JS eager parsing/reparsing is included.

Both adapters hash identical ordered root kind/span/flags, factory node/text/
identifier counters and diagnostic channel/count/code/span/category fields.
Every sample agrees, and signatures are tethered to untimed canonical Go output.
This observer is intentionally cheaper than syntax traversal and does not compare
all child/scalar/JSDoc fields during timing; full-tree preflight provides that
check. Observers, pass validation and timer checks are timed harness costs.

File loading, input options construction, process startup, JSON, structural
comparison and explicit pre-sample GC are excluded. Naturally triggered GC is
included. Go retains parser pooling/arenas and preloaded source strings; C# uses
fresh managed parser state and timed public byte-input conversion. These existing
API/lifetime effects remain intentional; this is neither equal-storage parse-only
work nor cold-start/first-parse latency. Warmup followed by forced GC is part of
the run condition, including its effects on runtime pools.

Allocation counters measure process-wide managed heap allocation volume (Go
TotalAlloc/.NET GetTotalAllocatedBytes), not live/peak heap, RSS, native memory or
GC time. C# thread allocations are supplementary. GC counts across these runtime
schemes must not be treated as directly comparable events or timing attribution.

## Evidence and confidence

Five alternating rounds used CPU 0, one-processor settings, 150 ms warmup and
at least 300 ms samples; all 40 elapsed samples meet the minimum. Runtime versions
are Go 1.27.1 and .NET 10.0.12; C# reports workstation GC and no inherited .NET
overrides. Release publication used Native AOT; the inspected benchmark is a
stripped Linux x64 ELF (3,630,888 bytes), requiring only libc/libm and the loader.
Source/harness/validator/input/config/manifest/binary drift guards are present.
Current recorded harness and config hashes match files on disk.

C# / Go median throughput is 0.851 on real TS, 0.982 on TSX microfixtures, 0.716 on
eager-JSDoc JS and 0.792 on malformed recovery. C# allocation volume is higher in
all groups. TSX observed ranges overlap; its roughly 2% median gap is not evidence
of a reliable speed distinction. Other gaps are descriptive findings on this
machine, with five short samples and visible run variation; they establish no
intrinsic language ranking or causal explanation. Fixed per-file/observer work
matters on the small fixture groups, so byte-normalized rates and allocation
ratios should not be generalized to large JSX/JSDoc/recovery files or full compiler
work. Historical scanner ratios used different workloads and paths and cannot
establish an improvement in parser performance.

Fresh-checkout reproduction first needs the accepted native parser validator and
its report from [verify-parser.sh](../../tools/verify-parser.sh), then the
[benchmark script](../../tools/parser-benchmark.sh). Existing evidence supports
publishing this basic comparison with these limits; no further work is requested.
