# Resuming work

Read [STATE.md](./STATE.md), [upstream.lock.json](../upstream.lock.json), and the
relevant file in [progress/](./progress/). Each subsystem checkpoint records its
owned files, last verified result, unresolved issues, and next action. Do not infer
completion from an agent's exit or a source file's existence.

1. Inspect `git status --short` and existing changes before editing. Source changes
   and progress files are saved directly in the shared working tree, including
   unfinished work; no chat history is required to discover them.
2. Run `tools/bootstrap-upstream.sh` to obtain/validate the pinned reference.
   Never silently update the pin or modify upstream behavior to pass a test.
3. Resume the narrowest unfinished subsystem checkpoint. Respect listed ownership
   if other agents are active. Update the checkpoint after a useful boundary such
   as compiling, running tests, reproducing a defect, or completing a review.
4. Reuse the stored oracle fixtures for fast local tests. The complete verification
   script also rebuilds the oracle and checks the stored results against Go.
5. Preserve the distinction between implementation coverage in `source-map.json`
   and behavioral evidence. A previous JIT result is not Native AOT acceptance.
6. Before reporting acceptance, rerun validation if executable code, fixtures,
   toolchain, or harness changed after the last recorded evidence.

Keep subsystem checkpoints concise and replace stale next steps. Record results
only after commands complete, with enough detail to locate failures. Failed or
interrupted commands are not successful checkpoints. Write larger machine-readable
manifests atomically (temporary file then rename) when generating them.

This is recovery from session/process loss using the working tree. It is not a
backup against disk loss; source control publication or external backups are
separate operations. A later compiler phase can use the same file-level mapping
and per-subsystem checkpoints before introducing a database or task service.
