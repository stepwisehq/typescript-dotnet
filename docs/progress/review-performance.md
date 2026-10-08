# Independent performance comparison review

Status: complete. Adapters, orchestration and all30 final measured samples reviewed; no blocking finding.

Agreed scope from root/verifier: ordinary lexical `Reset + SetText + Scan` only, numeric caches cleared per pass, same three byte workloads (176279-byte/23-file pinned TypeScript source set,262224-byte ASCII synthetic file,262260-byte Unicode/string/number file), Native AOT C# versus optimized Go, one CPU, warmup, minimum300ms samples,5alternating rounds. Source file IO, process startup, JSON output and forced GC are outside timing.

Representation boundary: Go preloads a byte string before timing and `SetText` binds it. C# preloads bytes before timing and public `SetText(byte[])` converts to its Latin1 UTF16 byte-preserving backing string on every timed pass. The benchmark intentionally measures that public scanner API cost. It does not isolate lexer loops or compare arbitrary .NET/Go language performance.

Reviewed adapter/orchestration behavior:

- Runner manifests exact source file SHA/length. Both adapters retain scanner defaults (skip trivia, target latest, no error callback) and count EOF per source. Runner rejects differing input size/files, token count, or checksum.
- Both use identical FNV-style uint64 hashing of token kind/end/flags with matching constants and unchecked arithmetic. They hash metadata, not text/value getters; getter conversion is excluded.
- Reset clears numeric cache entries; scanner object/dictionary capacities may remain reused after warmup. Do not describe as fresh scanner or cold process.
- Timer starts after forced GC and allocation-counter baselines; file IO/warmup/process startup/output excluded.
- Allocation-counter scopes are process-wide on both runtimes; Go NumGC and .NET generation counters are recorded separately rather than summed as equivalent events.
- Shell runner builds optimized Go1.27.1 in the pinned module and publishes C# Release linux-x64 with PublishAot=true. Python runner uses the same lowest allowed CPU and sets GOMAXPROCS=1/DOTNET_PROCESSOR_COUNT=1.
- Five samples alternate Go/C# order by round. Throughput and process-wide allocated bytes are normalized by measured passes/input bytes. Summary reports median/min/max throughput and allocation medians. Ratio is ratio of throughput medians.
- Scope excludes parser-driven regexp/JSX/template rescanning, diagnostics callback rendering and full compilation.

No blocking adapter/orchestration defect found. Reproducibility hardening was implemented: benchmark adapter/runner source hashes recorded separately from frozen scanner-source hash; binary hashes checked before/after execution; Go GOGC=100/GOMEMLIMIT=off enforced and inherited .NET runtime overrides recorded (none present).

No scanner optimization is part of this work. Review is read-only except this checkpoint.


## Final measured artifact review

Reviewed `benchmarks/results.json` (2026-10-08T15:16:42Z), on CPU0 of an AMD Ryzen9 9950X. All30 samples (3workloads×2implementations×5rounds) ran at least0.3000304seconds. Each workload has one consistent input size/file count/token count/checksum signature across both runtimes and all rounds. Recomputed current input-file and benchmark-source hashes match the report. Frozen scanner/verification source hash is `3649d542b4bf031beb995de72c02fe9852e205dd4233b94ea7426250d38a30fd`, unchanged from final correctness verification.

| Workload | Go median MiB/s | C# AOT median MiB/s | C#/Go median throughput | Go alloc bytes/input byte | C# alloc bytes/input byte |
| --- | ---: | ---: | ---: | ---: | ---: |
| Real TypeScript |492.16|262.53|0.533|1.505|6.352|
| ASCII synthetic |372.70|139.66|0.375|0.0453|12.464|
| Unicode/string/number |160.96|84.88|0.527|0.9295|21.493|

Within-group throughput coefficient of variation is approximately0.6–6.3%; the raw min/max range remains recorded. All five samples support the same directional conclusion: the current C# port is slower and allocates more in this API mode. Five short samples on one host support a basic baseline, not confidence bounds or a general language/runtime claim.

Observed .NET generation counters show full collections in the two large synthetic-file workloads; Go NumGC is separate and must not be equated with a sum of .NET generation counters. Allocation differences and the known UTF16 backing conversion are relevant observations, but this benchmark does not identify how much of elapsed time each operation costs. No profiler attribution or scanner optimization was performed.

The real workload is23 files from the pinned VSCode TypeScript package, totaling176279bytes; it is not a compiler-wide representative corpus. Synthetic files exercise unique identifiers and numeric/string paths with repeated constants. Rescanning, diagnostics rendering, compilation, multithreading and startup remain outside this measurement.
