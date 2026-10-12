# Autoimport and language-service core independent review

The complete assigned 35-file source and assertion packet is clear against pinned
TypeScript `fed0bf24149fb1ed36039212648bafdafc1ea10e`. The current hash bindings are in
[autoimport-core.json](../../artifacts/language-services/source-review/autoimport-core.json).
This covers all eleven autoimport counterparts (190 original functions), actual
language-service Host/LanguageService/Api owners, the focused checker/module/path
and preference joins, WrapVfs and the opt-in reusable workgroup. The external
11-file regex/Unicode/quotation closure has separate protocol-lane review.

The comparison includes discovery, realpath deduplication, primary and @types
fallback extraction, granular dirty-package index updates, bucket installation
and failed ambient-alias second passes. Original waits separate each phase. Dirty
map finalization retains the original immutable identity; cloned bucket state,
import lists, index mappings and resolver option values retain required copies.
Checker leases release in finally blocks and the private bounded pool closes only
after its owner finishes work. Successful quiescent registry phases may reuse the
workgroup; failure stays terminal and ordinary one-shot callers remain unchanged.

Review corrections preserve the nil-registry readiness result rather than throwing
or inventing an empty registry, carry default export identifier bytes directly
from the actual value producer, retain Go token spelling in two panic paths, and
reuse the exact existing extension sets at the new entrypoint-ending consumer.
The later MapNonNilSlice/API qualification fixes retain the original nil header
and actual prepared-cache invariant without a fallback owner.

All seven direct original test functions retain their inputs and assertions:
word suffixes, index clone/filter/nil behavior, module ID variants, three symlink
realpath scenarios and the original alias-resolver diagnostic no-panic check.
The existing compiler baseline filesystem supplies the genuine fixture symlinks.
The separate authored control checks repeated workgroup phases, nested work and
terminal failure. Five registry tests requiring real Session remain pending and
are not included in the seven-function credit.

This is source review, not managed or Native acceptance. The coordinator retains
failed declaration/body integration attempts and owns the corrected execution
snapshot. CLR ThreadPool/BlockingCollection behavior is a documented standard
runtime adaptation, not a claim of Go scheduling equivalence. Unchanged upstream
TODO and panic leaves remain identifiable; no service consumer is replaced by an
empty-success implementation.
