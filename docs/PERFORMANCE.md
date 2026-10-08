# Basic scanner performance comparison

The current C# Native AOT port was about 1.9 times slower than the pinned Go scanner on the real TypeScript and Unicode stress workloads, and 2.7 times slower on the ASCII workload. It also allocated more managed heap memory. These are measurements of this scanner implementation and its current public input path on one machine; they are not an intrinsic language-speed comparison.

Possible explanations and ways to test them are recorded separately in
[PERFORMANCE-INVESTIGATIONS.md](./PERFORMANCE-INVESTIGATIONS.md). They are
unmeasured hypotheses where stated. The user has explicitly deferred optimization;
the backlog is not authorization to change the implementation or start experiments.

| Workload | Input bytes / files | Go MiB/s, median [min–max] | C# Native AOT MiB/s, median [min–max] | Go / C# throughput | Go allocated bytes/input byte | C# allocated bytes/input byte |
| --- | --- | --- | --- | --- | --- | --- |
| Upstream TypeScript sources | 176,279 / 23 | 492.2 [437.1–502.6] | 262.5 [244.1–275.7] | 1.87× | 1.505 | 6.352 |
| ASCII synthetic | 262,224 / 1 | 372.7 [344.9–401.1] | 139.7 [129.2–141.4] | 2.67× | 0.045 | 12.464 |
| Unicode/string/number stress | 262,260 / 1 | 161.0 [147.5–161.4] | 84.9 [83.9–85.2] | 1.90× | 0.930 | 21.493 |

MiB/s uses 1,048,576 input bytes per MiB. Allocation numbers are allocation volume divided by all scanned input bytes, not peak/live heap, RSS, or total native memory. Raw samples and metadata are stored in [results](../benchmarks/results.json); exact workload files and SHA256 hashes are in [workloads](../benchmarks/workloads.json).

## What was measured

Each corpus pass calls `Reset()`, `SetText(...)`, and ordinary `Scan()` through EOF for each file. The scanner instance and map capacities are reused, but each reset clears numeric cache contents. The real workload preserves boundaries across 23 maintained upstream TypeScript package source files; the two generated workloads each use one approximately 256 KiB file. Script target/language variant and skip-trivia defaults are the same. There is no parsing, type checking, parser-directed rescan, or compiler work.

The timed loops include reset, the public text setter, scanning, numeric-kind/end/flags FNV64 checksumming, token counting, per-pass result validation, and the loop timer check. File reading, initial source loading, JSON output, process startup, explicit pre-sample GC, and allocation-counter snapshots are outside timing. Internal scanner token-value construction remains timed; the benchmark does not materialize raw token text or serialize tokens.

Go source bytes are converted to immutable source strings once before timing. C# source bytes are loaded once before timing, while `SetText(byte[])` converts those bytes into the port’s internal byte-preserving .NET string on every timed file pass. That asymmetric boundary cost is intentional here: this measures the current public API path. It does not isolate scan-only work on equally preconverted representations.

Every measured pass was checked against an initial untimed checksum/token count. Every sample across the two binaries had matching input byte/file counts, token counts, and checksums:

| Workload | Tokens per corpus pass, including EOF | FNV64 checksum |
| --- | --- | --- |
| Upstream TypeScript sources | 12,573 | `1741ec47efca8e08` |
| ASCII synthetic | 52,033 | `0409076a5c829ee8` |
| Unicode/string/number stress | 36,196 | `00adf3abc9f61ea4` |

## Run conditions

Measured on `AMD Ryzen 9 9950X 16-Core Processor`, `Linux-6.18.40.1-microsoft-standard-WSL2-x86_64-with-glibc2.43`. Both binaries were pinned to allowed CPU 0 with `taskset`. Go used 1.27.1 with `GOMAXPROCS=1`, `GOGC=100`, `GOMEMLIMIT=off`; C# used .NET Native AOT 10.0.12 built by SDK 10.0.401, `DOTNET_PROCESSOR_COUNT=1`, and workstation GC. No inherited .NET GC/runtime overrides were present.

Five rounds per workload alternated which binary ran first. Each process warmed up for at least 150 ms, then performed a full collection before a measured sample lasting at least 300 ms. Naturally triggered collections during measurement were included. The table reports the median and observed minimum/maximum across five samples, rather than treating short-run timing as a precise universal rate. No performance optimization or profiling was performed for this comparison.

Primary allocation scopes are process-wide managed heap allocation deltas: Go `runtime.MemStats.TotalAlloc` and .NET `GC.GetTotalAllocatedBytes(true)`. C# current-thread allocations and per-generation collection counts are also saved; Go collection counts are saved separately. Generation counts are not summed or treated as directly equivalent across runtimes.

The C# benchmark was actually published with Native AOT and identified as a stripped ELF64 x86-64 executable, dynamically linked only to libc/libm. The Go executable is a native ELF64 x86-64 binary. Source files, benchmark program files, workload files and both executable hashes were checked before/after the run to reject concurrent changes.

## Reproduce

Run `tools/benchmark.sh` from the repository root. This verifies the pinned upstream checkout and Go toolchain, builds the inside-module Go adapter and the C# Native AOT executable, regenerates deterministic workload inputs, and runs all 30 samples. Standard Native AOT system prerequisites are clang and zlib development headers; this environment used the existing local GCC wrapper. Override durations or round count with `--sample-ms`, `--warmup-ms`, and `--rounds` when intentionally performing another measurement.

The original scanner acceptance source-tree hash is unchanged: `3649d542b4bf031beb995de72c02fe9852e205dd4233b94ea7426250d38a30fd`. Benchmark-specific source hashes and executable hashes are recorded in the raw results. The measurement adds a separate benchmark project and does not change the verified scanner or validation harness.
