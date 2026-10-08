# Resume this project

To restart work, give the new session this instruction:

> Read START-HERE.md in /home/greg/projects/typescript-dotnet and resume the
> current bounded phase. Follow the original user prompt and subsequent
> directions recorded here. Inspect existing work and the last verified
> subsystem checkpoints before taking action; do not restart the port.

Follow the active phase in [STATE.md](./docs/STATE.md). If it records no active
work, report that checkpoint and wait for a new bounded mandate; this resume
instruction does not authorize starting the full compiler.

## User mandate

Read [the original prompt, preserved verbatim](./docs/INITIAL-PROMPT.md).
It remains the governing task. The main session owns planning, coordination,
significant decisions, and acceptance; substantive implementation, investigation,
testing, and independent review are delegated explicitly to GPT-6.1 Sol.
Report model/tool limitations rather than silently substituting.

Later user directions, in chronological order:

1. The user pointed out that `typescript-go` was archived and directed: **“I think
   we should use the latest; not compare.”** The reference was switched before
   implementation to maintained `microsoft/TypeScript`, under `tsc/`. The selected
   latest revision is pinned in [upstream.lock.json](./upstream.lock.json). Resume
   against that fixed revision; do not silently update it mid-study.
2. The user has root access and offered to install needed packages. An asynchronous
   request asked for `clang` and `zlib1g-dev`. A local compiler fallback has already
   published Native AOT successfully; check the latest verification checkpoint for
   current system prerequisites and behavioral results. Do not assume a pending
   package request has been completed.
3. The user requested efficient crash-resistant progress records, with a possible
   future compiler port in mind, while leaving the mechanism and timing to the
   coordinator. Use the small per-subsystem checkpoints and source map now;
   introduce more infrastructure only when there is a demonstrated need.
4. The user installed `sqlite3` as an optional tool. No database is currently needed
   or required. This does not expand the task into a full compiler port.
5. The user requested preservation of the initial prompt and a clean re-entry point.
   This file and [INITIAL-PROMPT.md](./docs/INITIAL-PROMPT.md) fulfill that request.
6. After scanner acceptance, the user asked for a basic performance comparison
   with Go. This authorizes a bounded benchmark and assessment, not optimization
   or a broader compiler implementation. See current state for progress/results.
7. After the benchmark, the user explicitly said **“We don't want to do any
   optimization now”** and asked to record possible explanations and investigation
   ideas. The performance investigation backlog is documentation only; it does
   not authorize profiling, experiments, or implementation changes on resumption.
   The user reiterated that actual deeper investigation should wait until later;
   retain brief hypotheses from existing evidence rather than extending analysis.
8. The user directed committing progress and proceeding to **parser and required
   syntax AST support**, with structural and syntax-diagnostic agreement on valid
   and malformed input, Native AOT execution, and scanner regressions. Read the
   exact [parser mandate](./docs/PARSER-PROMPT.md). The scanner checkpoint was
   committed as `0a50e67`. This supersedes the initial scanner-only scope for the
   parser phase; binding was then excluded. Direction 11 below authorizes the
   subsequent bounded binder phase; checking, resolution and emit remain excluded.
9. During parser work the user reconfirmed GPT-6.1 Sol delegation and close Go
   source fidelity. They clarified that raw speed is not the concern: efficient
   work should reduce repeated effort and token cost. **“Slow and steady wins the
   race.”** Treat time estimates as observations, not deadlines that justify
   shortcuts, extra agents, or weakened verification.
10. After parser acceptance, the user asked for **basic performance tests against
    Go and recorded findings, like the scanner comparison**. This authorizes a
    separate benchmark harness and measurements on the accepted parser snapshot.
    It does not authorize optimization, profiling or deeper performance diagnosis.
    The [basic comparison](./docs/PARSER-PERFORMANCE.md) is complete; do not rerun
    or extend it automatically on resumption.
11. The user authorized committing parser/performance work and proceeding to
    **source-file binding and required support**. That checkpoint is `de25b66`.
    Read the exact [binder mandate](./docs/BINDER-PROMPT.md). Review parser evidence
    and the 29 unobserved diagnostic codes before extending code; compare symbols,
    declarations, scopes, binder diagnostics and flow structures with pinned Go.
    Type checking, cross-file semantic resolution, module resolution and emit
    remain excluded. Native AOT and scanner/parser regressions are mandatory.
    The [bounded binder checkpoint](./docs/BINDER-ASSESSMENT.md) is now accepted;
    implementation work is saved uncommitted. No next compiler phase is authorized.

## Recovery sequence

1. Read [docs/STATE.md](./docs/STATE.md) for overall scope and acceptance status.
2. Read [docs/RESUMING.md](./docs/RESUMING.md) and the relevant records under
   [docs/progress/](./docs/progress/), including independent review findings.
3. Inspect `git status --short`, existing source, and [source-map.json](./source-map.json).
   Uncommitted files are saved work. Do not discard or regenerate them from an
   earlier temporary translation script.
4. Follow the next unresolved action in the checkpoints. Reuse active agents if
   available; after session loss, delegate using those checkpoint files instead
   of reconstructing the old chat.
5. Use [README.md](./README.md) for reproducible verification. Required acceptance
   includes executing the actual Native AOT binary against the oracle corpus.
   Recheck results after changes; a prior publish does not establish current success.

The current scope is source-file binding and required support. A checkpoint can
be accepted, incomplete, or blocked; never infer completion from source presence,
an agent finishing, or a small set of passing tests. A later full compiler phase
needs a new bounded mandate. Follow STATE.md for the active phase instead of
resuming completed scanner or performance work.
