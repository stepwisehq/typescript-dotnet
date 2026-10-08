# Verification progress

Final coordinator disposition: **complete at the accepted scanner checkpoint**.
All final gates passed; see [VERIFICATION.md](../VERIFICATION.md) and
[ASSESSMENT.md](../ASSESSMENT.md). No active next action remains; any earlier
pending-gate notes below describe prior milestones and are superseded.

Status: all final gates passed; durable evidence is in docs/VERIFICATION.md and validation/results/verified.json.

Upstream: `microsoft/TypeScript`, commit `fed0bf24149fb1ed36039212648bafdafc1ea10e`, source module `tsc`.
Go: 1.27.1 linux/amd64; official SHA256 `63d339f0da5ab53635a56f2490a7984dfe12dfcff22ad749f63edaf590168445`.
.NET: SDK 10.0.401, runtime 10.0.12, Ubuntu 26.04 linux-x64.

## Completed

- `tools/bootstrap-upstream.sh` verifies repository origin, pinned HEAD and unchanged tracked files; shallow-clones missing checkout without overwriting existing state.
- `tools/bootstrap-go.sh` establishes checksum-verified local Go with fresh extraction, avoiding mixed-version toolchain trees.
- `tools/build-oracle.sh` builds an authored scanner-only JSONL adapter inside the upstream module (required by Go internal imports).
- `tools/generate-corpus.py` generates deterministic requests and source hashes in `validation/corpus/manifest.json`.
- Corpus: 1,206 cases, 17,798 token snapshots, 1,485 diagnostic emissions, 158 distinct token kinds, 69 of 70 referenced lexical diagnostic codes.
- Includes 120 upstream TypeScript conformance sources; Apache-2.0 license preserved under `licenses`.
- Cases exercise all scanning/rescan methods, JSX/JSDoc, raw malformed UTF8, Unicode identifiers, lone surrogate escape values, numeric/string/template errors, trivia/directives, state rewind/reset and seeded token mixtures.
- `tests/Scanner.Tests` contains 1,206 structural JSON comparisons and named lone-surrogate/source-keyword tests copied in behavior from upstream, plus UTF8 byte-offset API test.
- `validation/Scanner.Validation` uses source-generated System.Text.Json metadata for a Native AOT-compatible JSONL executable.
- `tools/compare.py` compares parsed JSON structures and records case/token/diagnostic counts, hashes and elapsed time.
- Local GCC15 + libc/zlib headers extracted under ignored `tools/.cache/native`; C compilation and execution probe passed. System clang/zlib1g-dev installation requested by root; local fallback remains available.
- Native AOT packages restored successfully; final publication awaits compilable scanner.

## Commands/results

- `tools/build-oracle.sh` — passed against maintained pinned upstream.
- `tools/.cache/scanner-oracle < validation/corpus/requests.jsonl > validation/corpus/expected.jsonl` — produced all 252 rows.
- `tools/.cache/go/bin/go -C upstream/TypeScript/tsc test ./internal/scanner` — passed, package time 0.004s (cached repeat may differ).
- `dotnet test tests/Scanner.Tests/Scanner.Tests.csproj -c Release` — first run 249/255 passed, six failed (101 ms test duration). All differences point to generated conflict-marker loop losing its body; scanner owner notified.
- `dotnet publish validation/Scanner.Validation/Scanner.Validation.csproj -c Release -r linux-x64 -p:PublishAot=true -p:CppCompilerAndLinker=/home/greg/projects/typescript-dotnet/tools/.cache/native/bin/cc -o artifacts/validation/native` — restore passed; blocked by scanner compilation.

## Caveats and next action

Oracle expected values must never be hand-adjusted to conceal implementation differences. Regenerate only from the pinned, unchanged upstream checkout. Go and C# scan loops fail if EOF is not reached before the progress guard.

Diagnostics compare code, byte span and string arguments; strings use JSON encoding, while token text/value use Base64 to preserve malformed UTF8 and WTF8 lone-surrogate bytes. Operation capture records tokens after scan/rescan operations and omits state-only mark/rewind/reset operations.

Next: after scanner agent fixes conflict-marker loop, run `tools/verify.sh`; fix genuine differences in the implementation; publish Native AOT and execute the same stored corpus. Confirm output is ELF/native and record final commands/results/hashes in `docs/VERIFICATION.md`. Do not expand corpus merely to increase counts.

Final gates: tools/verify.sh passed 1,209 xUnit tests (187 ms), byte-identical fresh oracle baseline, and Native AOT executable comparison (1,206 cases, 65 ms). ELF64 binary SHA256 1019dbf07f49a06a7f1e4cee4902359409495a91436c54be1ff737805186bf92. No remaining verification blocker. Optional next action: root review, source manifest/assessment and repository commit.

Follow-up scope: public pure helper oracle coverage and Unicode case-folding boundary pairs added. Current1276 managed differential cases all pass;17977 token snapshots,1547 diagnostics. Helper protocol adds optional typed utilityOperations/utilityResults for13 APIs and Reset/SetText scanner reuse. Final1276 corpus xUnit+AOT rerun awaits scanner agent UtilityTests.cs completion.

Latest final gates: tools/verify.sh passed all1,287 xUnit tests (200 ms), fresh unchanged Go oracle check, and native differential1,276 cases/178 utility results (67 ms). Counts17,977 tokens/1,547 diagnostics/69 of70 codes. Native SHA256363b3fabd803832c02c3de1943945ec0cd28cd411260a1c157b29bd6cbb50d4b, 3018152 bytes. docs/VERIFICATION.md and validation/results/verified.json updated. All requested verification work complete.

Definitive frozen-source verification: tools/verify.sh passed all1,287 xUnit tests (193 ms) and1,276 native cases/178 utility outputs (61 ms). Source-tree hash3649d542b4bf031beb995de72c02fe9852e205dd4233b94ea7426250d38a30fd; binary hashb588719dd27de95d29847eb6c5e22319f13887d72644746fc5ebfb4bccff8323. Source modifications predate native publication; verify.sh now rejects edits made during verification. Support/scanner executable-source edits frozen. Durable results updated.
