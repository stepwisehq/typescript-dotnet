# Benchmark progress

Status: guarded five-round measurements complete; independent reviewer approved adapters and runner methodology. Final document/results signed off by independent read-only reviewer.

Primary mode: repeated logical file on a reused scanner, each file pass calls Reset + SetText + Scan through EOF. Reset clears number-cache contents but scanner and map capacity remain reused. No parser/rescan work.

Go loads source bytes into immutable strings once outside timing. C# loads byte arrays once outside timing and uses the public SetText(byte[]) conversion per file inside timing. This measures the current public API boundary cost, not an intrinsic language speed comparison.

Three reproducible workloads: actual maintained upstream TypeScript package sources, retaining individual file boundaries; about256KiB ASCII synthetic; about256KiB Unicode/string/number stress. All loading/path IO and JSON output are outside timed loops. Per-pass FNV64 hashes kind/end/flags and token counts must match across all samples/programs.

Planned run: tools/benchmark.sh, five alternating-order rounds,300ms measured sample after150ms warmup. Both programs single-processor configuration (GOMAXPROCS=1, DOTNET_PROCESSOR_COUNT=1) on the same allowed CPU via taskset. Full GC before timing; naturally triggered collections stay included. Process-wide managed heap allocation deltas: Go TotalAlloc, .NET GetTotalAllocatedBytes(true); C# current-thread allocation delta also recorded separately.

Files: benchmarks/Scanner.Benchmarks contains Native AOT C# program; tools/benchmark/main.go is copied inside pinned tsc module for internal imports; tools/benchmark.py generates inputs, runs programs, rejects checksum drift and saves raw/summarized results; tools/benchmark.sh builds both programs. Workload source hashes are in benchmarks/workloads.json. Generated synthetic inputs live under ignored tools/.cache/benchmark-data.

Next: independent review of timing/cache/counter scopes, build native executable, execute measurements, inspect raw variation and matching signatures, write docs/PERFORMANCE.md. No scanner library edits or optimization in this task.

Completed: tools/benchmark.sh built and ran both native binaries sequentially on CPU0 with30samples. All per-pass counts/checksums matched. Median Go/C# MiB/s: real492.2/262.5; ASCII372.7/139.7; Unicode161.0/84.9. C# slowdown1.87x/2.67x/1.90x. Raw results benchmarks/results.json and docs/PERFORMANCE.md saved. Explicit GOGC100/GOMEMLIMIToff; processwide allocation counters primary and C#thread supplemental. Benchmark files, executable binaries, workload files and original accepted source hashes unchanged across run. No source library edits.
Next: root assessment/final response. Benchmark executable source frozen.

Independent read-only reviewer signed off final docs/raw evidence: medians, ranges, ratios, checksums, timing/setup/GC/allocation scopes and input boundary caveat all match. Review record docs/progress/review-performance.md. No remaining benchmark tasks or source edits.
