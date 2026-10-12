# Complete original Server assertion review

Independent source review is **CLEAR** for the 25-function packet: 24 default
runnable originals and the original optional-input Replay skip. Exact current
source/hash binding is in the ignored `artifacts/language-services/source-review/lsp-originals.json`.
Compilation, actual candidate behavior and Native acceptance remain pending.

Reviewed the full framed client and completion, internal contribution/resolve,
shutdown/write recovery, diagnostics, progress, project info/reference,
semantic-token, component-mapper and supplied replay assertion paths against
pinned upstream bodies. Expected values, raw fixtures and actual service owners
remain intact; there is no shadow Session. Standard 30-second safety bounds are
explicitly authored for otherwise untimed joins. Genuine source timing values
and the original optional Replay prerequisite remain separate.

One concrete fixture failure-propagation defect was corrected: selecting the
Server cancellation result before an originating router error could hide that
error. Both actual workers now retain the first returned error before sibling
cancellation; disposal joins both and applies the original canceled cleanup
filter to that retained outcome, matching upstream errgroup behavior.
