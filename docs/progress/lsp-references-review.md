# Complete References source review

The five pinned counterparts and their connected dependency/assertion packet are
source-reviewed clear. The [promotion manifest](./lsp-references-promotion.json)
binds the complete 183-worker source closure; the detailed hash-bound review is
[references.json](../../artifacts/language-services/source-review/references.json).
This is source review, not service-corpus or Native acceptance. No workloads ran
by the reviewer.

The review covered actual import/reexport and module graphs, special reference
searches, implementation worklist deduplication, checker leases, lazy sets and
required copies, cross-project phases and invoked iterator lifetime, content-map
fidelity, rename conflict/alias behavior, and syntactic highlight ownership.
Original Worklist assertions retain both unchanged 40/80 growth limits and the
actual bundled Program fixture. Accepted shared leaves were rehomed.

One concrete source finding was corrected: common reference scanning and else/if
highlighting called inherited token/name TextValue on SourceFile owners. These
consumers now borrow the actual SourceFile.TextView; no token semantics or
expectations changed. Remaining text consumers are genuine token/name/display
owners. Runtime exception stacks retain CLR representation as documented.

Central compilation, original candidate execution, actual Native and full
semantic/cross-project/LSP corpus behavior remain separate pending evidence.
