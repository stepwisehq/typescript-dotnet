# Parser benchmark checkpoint

- Status: basic benchmark completed; independent review complete, no blockers. No further measurements/optimization/profiling planned. Root writes user-facing assessment.
- Scope/ownership: scanner/parser agent owns new benchmark adapters/runner/workload/results. Accepted src/tests/validation and historical scanner benchmarks/results unchanged.
- Accepted hash before/after: d441e371cf28045777b3c8041d51e524ce96904ddecf6ff7e4f105edaf2edf8d. Upstream pin fed0bf24149fb1ed36039212648bafdafc1ea10e.
- Files: benchmarks/Parser.Benchmarks/{Program.cs,Parser.Benchmarks.csproj}; tools/parser-benchmark/main.go; tools/parser-benchmark.py; tools/parser-benchmark.sh; benchmarks/parser-{workloads,results}.json. Regenerate/build/validate/measure with `tools/parser-benchmark.sh`.
- Inputs preserve exact verified corpus bytes, file boundaries, logical filenames/PathKey, ScriptKinds and module-indicator options. TypeScript4files102533B; validTSX3microfixtures687B; eagerJSDocJS7microfixtures1312B; malformed13files4795B (TS9/JS3/TSX1, includes empty fuzz inputs).
- Timing: public ParseSourceFile plus cheap SourceFile factory NodeCount/IdentifierCount/TextCount/rootflags and ordered diagnostic codes/spans/category checksum; result kept alive. No graph serialization/traversal. NodeCount counts factory-created nodes, including discarded/reparsed allocations, not merely statements or unique reachable nodes.
- JS eager JSDoc parsing/reparsing included in public call. TS lazy JSDoc not forced. Canonical primary27cases use resolveJSDoc=false matching timing; supplemental7JS-only true queries outside timing expose already-eager attached documents without forcing TS lazy parses.
- Full canonical preflight34/34cases passed (13807serialized nodes57diagnostics); existing pinned Go oracle and accepted NativeAOT validation executable. Timed expected signatures derive from canonical metadata/root/diagnostics and match both runtimes in all40samples; checksums alone were not the semantic gate.
- Included implementation differences: C# public byte[] conversion/fresh managed parser/managed arenas versus Go preloaded string input/pooling/arenas. No source changes or attempt to equalize these costs.
- Method: 5alternating rounds,300ms measured loops,150ms warmup in each fresh process, one forced GC boundary before measurement (.NET full collection twice, Go runtime.GC once). Startup/IO/JSON outside timing; results do not measure cold startup and Go sync.Pool state is affected by forced GC.
- Host: AMD Ryzen9 9950X, WSL2/Linuxx64, CPUaffinity0; GOMAXPROCS1/DOTNET_PROCESSOR_COUNT1, GoGOGC100/GOMEMLIMIToff; actual .NETworkstationGCfalse server flag. Go1.27.1/.NET10.0.12 NativeAOT. Runtime overrides/settings, GC counts and all raw rounds recorded.
- Allocations: Go process-wide TotalAlloc versus .NET GetTotalAllocatedBytes(precise), with .NET thread allocation auxiliary. Different counters/GC schemes and observer costs limit allocation interpretation; no cause attribution or investigation.

| Group | Go median MiB/s | NativeAOT median MiB/s | C#/Go throughput | Go allocated bytes/input byte | C# allocated bytes/input byte |
|---|---:|---:|---:|---:|---:|
| TypeScript | 70.984 | 60.417 | 0.851 | 13.849 | 30.642 |
| TSX microfixtures | 38.383 | 37.692 | 0.982 | 30.523 | 72.524 |
| Eager JSDoc JS microfixtures | 25.286 | 18.107 | 0.716 | 41.352 | 237.780 |
| Malformed recovery | 47.830 | 37.867 | 0.792 | 28.436 | 80.003 |

- Per-pass factory nodes/parse-JS-JSDoc diagnostic counts: TS12788/1-0-0; TSX155/0-0-0; JS331/0-0-5; malformed940/43-1-2. Checksums and identifier/text counters also match canonical expectations.
- Interpretation: port throughput below Go medians in this run, approximately85% on larger TS batch; tiny TSX medians close with overlapping ranges. Microfixture groups include fixed per-file API/parser overhead and do not establish large application throughput. Short5round WSL measurements are descriptive, not statistically generalized.
- Evidence: benchmarks/parser-results.json has40rawsamples, medians/ranges/perpassms, environment, accepted-source before/after, harness/validator/workload/config/manifest/request/expected/binary hashes. artifacts/parser-benchmarks/{canonical-preflight.json,run.log,binary-info.json,native/Parser.Benchmarks} captures semantic gate and native ELF/dependencies.
- Guards passed: accepted source unchanged; benchmark/validator sources, benchmark/validator binaries, raw input bytes, workload options/config, manifest and preflight files unchanged during measurements. Historical scanner files were not touched.
- Independent review: review-parser-performance.md; no measurement blocker. Next: root records findings in final assessment and checkpoint; no remaining benchmark work.
