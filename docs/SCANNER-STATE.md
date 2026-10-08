# Scanner feasibility study

Status: **accepted for the bounded scanner feasibility checkpoint**. Started
2026-10-08 14:46 UTC; final Native AOT publication 2026-10-08 15:05:47 UTC.
All mandatory gates passed against the frozen source snapshot. No active
implementation or verification work remains. See [ASSESSMENT.md](./ASSESSMENT.md)
and [VERIFICATION.md](./VERIFICATION.md) for the decision, evidence, and limits.

Follow-up authorized 2026-10-08 15:10 UTC: the user requested a basic measured
performance comparison with Go. **Complete**; see [PERFORMANCE.md](./PERFORMANCE.md)
and [raw samples](../benchmarks/results.json). Across three workloads the current
Native AOT scanner API takes about 1.9–2.7 times Go's time per byte and allocates
more managed heap memory. Five alternating samples per implementation/workload
matched token counts/checksums. Independent Sol review found no measurement
blockers. The scanner acceptance source hash is unchanged; no optimization was
performed. No active benchmark work remains.

The user subsequently requested investigation ideas while explicitly deferring
optimization. [PERFORMANCE-INVESTIGATIONS.md](./PERFORMANCE-INVESTIGATIONS.md)
records observations, hypotheses, and possible future measurements. No profiling,
additional benchmark runs, or implementation changes are authorized by that note.

Recovery entry point: [START-HERE.md](../START-HERE.md), then
[RESUMING.md](./RESUMING.md). Fine-grained subsystem records
live under [progress/](./progress/); update them at verified implementation,
integration, and review boundaries.

## Reference and scope

The reference is the Go compiler in `microsoft/TypeScript`, revision
`fed0bf24149fb1ed36039212648bafdafc1ea10e`, under `tsc/`.
See [the lock file](../upstream.lock.json). The archived `typescript-go` repository
was initially fetched in error; the user directed use of the latest maintained
source before implementation. It is not the reference for this port.

Port `scanner.go`, `regexp.go`, `unicodeproperties.go`, and the minimum support
needed to exercise scanner behavior independently. Text-only utilities are in
scope where useful; utilities requiring compiler AST nodes are excluded. Do not
implement a parser, binder, checker, emitter, language service, or compiler AST.

## Acceptance and checkpoint

1. Build a readable, traceable C# scanner with every scanner entry point either
   implemented or explicitly identified as incomplete. Keep upstream algorithms,
   function boundaries, and control flow where practical.
2. Record source-to-destination mappings, dependency slices, meaningful language
   adaptations, and upstream license/attribution notices.
3. Verify byte offsets, token kinds, raw text/value bytes, token flags, diagnostics,
   and comment directives against the pinned Go implementation. Exercise ordinary
   scans and explicit contextual rescan/JSX/JSDoc/state operations.
4. Run xUnit tests, including relevant upstream cases and a deterministic oracle
   corpus covering malformed input, Unicode, numeric boundaries, and regex syntax.
5. Publish the validation executable with .NET Native AOT and run behavioral
   comparisons against that actual native executable. A JIT pass is insufficient.
6. Obtain independent Sol review of fidelity, readability, test limitations, and
   AOT evidence; resolve acceptance-critical findings.
7. Deliver reproducible commands and an evidence-based feasibility assessment.

The checkpoint is one scanner implementation/integration/review cycle, including
fixes needed for these gates. Stop and report an incomplete or blocked outcome if
isolation requires compiler implementation or an environmental prerequisite cannot
be obtained. Passing a bounded corpus does not prove correctness for all inputs.
AST-dependent utility exclusions do not silently become scanner API exclusions.

## Working arrangement

The main session coordinates scope, technical decisions, integration gates, and
assessment. Three explicitly selected GPT-6.1 Sol agents own:

- `scanner`: scanner translation and its project.
- `support`: regex scanner and minimal support dependencies.
- `verification`: Go oracle, local toolchain, xUnit, corpus, and native harness.

Reuse completed agents for independent review instead of starting another wave.
Shared filesystem ownership is split to avoid overlapping edits. Agents coordinate
the representation and public API directly; root resolves scope questions.
The collaboration tool explicitly accepted `gpt-6.1-sol` for all three agents.
The main session's model identity/selection is not exposed by the session metadata
tool, so this record does not claim it independently verified Astra selection.

## Decisions and risks

- .NET 10 SDK 10.0.401 is installed. Go and a native compiler were missing initially;
  the verification agent is establishing pinned local tools without sudo.
- Go positions are byte offsets. The implementation uses a byte-preserving internal
  string representation to retain indexing and slicing behavior, with explicit
  conversions at C# API boundaries. Raw-byte APIs are needed for malformed UTF-8
  and Go's encoding of lone JavaScript surrogate values.
- .NET regex matching is not a substitute for the ECMAScript regex syntax scanner.
- Preserve upstream Unicode tables rather than relying on potentially different
  runtime Unicode classifications.
- Tests must retain raw value bytes, since ordinary JSON strings cannot faithfully
  transmit all upstream values.
- The byte-preserving representation is a feasibility choice, not a performance
  claim: .NET strings use UTF-16 storage, and substring/conversion allocation may
  exceed Go's cost. Measure before recommending a production representation.
- The user offered root access. Requested Ubuntu prerequisites are `clang` and
  `zlib1g-dev`; Go 1.27.1 is available locally. A local extracted compiler passed
  a C probe, but this is not evidence of Native AOT acceptance.

## Measurements

Three Sol agents were used and reused for review. Final results: 1,287 xUnit tests
passed; the published native executable matched 1,276 oracle cases and 178 helper
outputs. Source hashes and detailed measurements are retained in
[verified.json](../validation/results/verified.json). Token consumption and cost
were unavailable. The user corrected source selection, offered root/package help,
and requested recovery documentation; no manual code fixes were required.

## Next action after recovery

Read [START-HERE.md](../START-HERE.md) and the checkpoint assessment. Do not
automatically start a compiler port or continue adding tests after this accepted
checkpoint. If source differs from the recorded hash, rerun applicable verification
before relying on acceptance. A next bounded phase requires the user's direction.
