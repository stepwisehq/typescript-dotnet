# Scanner feasibility verification

All verification gates passed against maintained Microsoft TypeScript commit `fed0bf24149fb1ed36039212648bafdafc1ea10e` on Ubuntu 26.04 linux-x64. This validates the scanner slice against the stored corpus, not the complete compiler or every possible input.

| Gate | Result |
| --- | --- |
| Pinned upstream tracked files unchanged | Passed |
| Fresh Go oracle equals stored expectations | Passed, byte-identical JSONL |
| Upstream Go scanner package tests | Passed |
| C# Release build | Passed, zero warnings/errors |
| xUnit | 1,287 passed, 0 failed; 193 ms test duration |
| Native AOT publication | Passed |
| Native executable differential corpus | 1,276 passed, 0 differing cases; 61 ms elapsed |

The corpus contains 17,977 token snapshots and 1,547 diagnostic emissions, including 120 upstream conformance sources. It covers 158 distinct token kinds and 69 of 70 diagnostic codes referenced by the lexical scanner and regular-expression parser. Diagnostic 1513 (`Undetermined_character_escape`) was not observed. These counts measure exercised behavior; they do not establish exhaustive correctness. See [coverage](../validation/corpus/coverage.json), [source manifest](../validation/corpus/manifest.json), and [durable results](../validation/results/verified.json).

Coverage includes all scan/rescan entry points, JSX/JSDoc modes, trivia/directives, marker recognition, state rewind/reset, identifier table edges, malformed UTF8 leading bytes, lone-surrogate string escapes, string/template errors, decimal subnormal/overflow boundaries, long radix integers around powers of two, bigint values, and regular-expression syntax in Unicode/non-Unicode modes at different language targets. The native oracle additionally compares 178 pure-helper results across 13 helper entry points: trivia options, shebang extraction, leading/trailing comment ranges, identifier validation/token lookup and predicates, line lookup, byte offsets and UTF16 positions. Utility cases include astral characters, malformed UTF8 and clamped edited positions. Reset/SetText reuse is checked through token snapshots. Eight additional xUnit tests check public surrogate/Unicode APIs, error callbacks, Reset defaults, directive rewind, comments, trivia and coordinate behavior.

The two named upstream scanner tests cover lone surrogates and the `source` keyword.

Short Unicode case-folding pairs and long capture-name spellings verify the Go Unicode casing/suggestion semantics used by regular-expression diagnostics.

Each token comparison includes numeric kind/flags, byte positions/full start, and Base64 raw token text/value. Diagnostics compare numeric code, byte span and string arguments; comment directives compare span and kind. The executable uses source-generated System.Text.Json metadata. Go and C# adapters omit state-only operations from token snapshots and fail loudly if normal scanning does not reach EOF within its progress guard.

The published executable is a stripped ELF64 x86-64 PIE, 3,018,152 bytes, dynamically linked only to libc/libm; it requires no installed .NET runtime. Publication used .NET SDK 10.0.401, Native AOT runtime 10.0.12 and GCC 15.2.0 extracted locally from Ubuntu packages. A clean machine can install ordinary Native AOT prerequisites with `sudo apt-get install -y clang zlib1g-dev`.

## Reproduction

Run `tools/verify.sh` from the repository root. It establishes checksum-verified Go 1.27.1, clones/verifies the exact upstream commit, rebuilds the scanner-only oracle inside the module to satisfy Go internal imports, checks stored expectations, runs upstream scanner tests and xUnit, publishes Native AOT, and executes differential comparison. Source hashes before and after the run must match, so concurrent edits cannot silently invalidate native evidence. Existing upstream checkout mismatches or tracked modifications cause an error without overwriting user state.

For deliberate fixture regeneration, run `python3 tools/generate-corpus.py`, `tools/build-oracle.sh`, then `tools/.cache/scanner-oracle < validation/corpus/requests.jsonl > validation/corpus/expected.jsonl`. Run `python3 tools/coverage.py` and the full verification gates afterward. Expected values must come from the pinned unchanged upstream source.

The publication command used in this environment was `dotnet publish validation/Scanner.Validation/Scanner.Validation.csproj -c Release -r linux-x64 -p:PublishAot=true -p:CppCompilerAndLinker=/home/greg/projects/typescript-dotnet/tools/.cache/native/bin/cc -o artifacts/validation/native`. The optional local wrapper is an environment cache; system clang is the standard reproducible prerequisite.

| SHA256 artifact | Digest |
| --- | --- |
| Requests | `cd5e7a2226138491631e5220c8a2b051dfb1c7c0e261aceb4f80444363649c9e` |
| Go expectations | `8c2873a5b8aeabd608fc75c3bcae7c3fb356dcd7c292de3721bc7870e2f8da8f` |
| Published native binary | `b588719dd27de95d29847eb6c5e22319f13887d72644746fc5ebfb4bccff8323` |

Differential verification discovered and corrected a lost conflict-marker loop body, non-ASCII diagnostic argument/suggestion handling, and Unicode casing differences involving Kelvin sign, sharp S and dotted I. Expected baseline values were never modified to accommodate port defects.

The verified source-tree SHA256 is `3649d542b4bf031beb995de72c02fe9852e205dd4233b94ea7426250d38a30fd` across 30 C# project/source files (sorted relative path + NUL + raw file bytes + NUL, excluding bin/obj). The last source modification was `2026-10-08T15:05:28.619997+00:00`; the native binary was published at `2026-10-08T15:05:47.716918+00:00`. The complete hashed-file list is in the durable results.
