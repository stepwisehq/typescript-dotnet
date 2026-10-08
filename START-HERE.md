# Resume this project

To restart work, give the new session this instruction:

> Read START-HERE.md in /home/greg/projects/typescript-dotnet and resume the
> scanner feasibility study. Follow the original user prompt and subsequent
> directions recorded here. Inspect existing work and the last verified
> subsystem checkpoints before taking action; do not restart the port.

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

The scope remains scanner-only. A checkpoint can be accepted, incomplete, or
blocked; never infer completion from source presence, an agent finishing, or a
small set of passing tests. A later full compiler phase needs a new bounded mandate.
