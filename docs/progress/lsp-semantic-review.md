# Semantic production review

Independent GPT-6.1 Sol review is clear for the ten complete pinned LS counterparts
(268 functions) and the connected shared leaves in the
[promotion manifest](./lsp-semantic-promotion.json). Current source hashes are bound
in [the review artifact](../../artifacts/language-services/source-review/semantic.json).
This is source review, not full service or server acceptance.

The review compared utilities, source mapping, display-part writing, JSDoc, hover,
hover icons, signature help, formatting requests, definitions and source definitions
against pinned upstream. It also followed checker leases, configured/raw printers,
request-context checkout, byte escaping, required copy boundaries and generic
replacement into their actual consumers. Factory/writer cleanup, verbosity restore,
nil slices, mapped locations and original traversal/filter order were reconciled.

Two findings were corrected before binding. Zero-value sets now allocate only on
first insertion throughout the affected semantic owner paths. Signature help's
return-type fallback now uses the ordinary TypeToString default flags through the
borrowed-value worker; an explicit None argument had dropped unique-symbol and
outside-scope alias behavior.

The four original formatting test functions retain all eight rows and genuine
zero-value LanguageService admission. The generic replacer assertion channel retains
49 original generic Replace rows, including all-byte keys and empty-key priority;
it does not claim the separate WriteString assertions or optimized replacer engines.
Required final immutable output copying remains intact. Build38 compiled production;
shared-check API corrections and subsequent managed/Native execution remain separate
gates. Full reference/completion services are not implied by their shared leaf joins.
