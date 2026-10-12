# Watcher original assertion review

Independent source review is clear for the selected 46 whole original functions in
[the assertion manifest](./lsp-watcher-assertions.json). The
[hash-bound review](../../artifacts/language-services/source-review/watcher-assertions.json)
covers all nineteen paths, fixture adapters and thin shared/xUnit dispatch.

The packet preserves sixteen LSP watcher functions, seven event-list functions,
five fallback functions, eight directory-walk functions (each runs both actual
native and generic workers), four path functions and six internal path/backend/error
functions. Conditions, callback error identity, rollback, sequence filtering,
ancestor promotion, synthetic recursive depth and concurrent Close remain faithful.
The path generator retains all thirty source rows and raw malformed UTF8 bytes.
Real backend tests use the production watcher; injected backends match the original
fixtures. Cleanup retains actual watch closure and filesystem removal.

The only production joins are readonly projections of retained fallback backend
owners and package-level fanotify visibility for original identity assertions.
Previously reviewed watcher algorithm bodies were reused rather than re-audited.
The unreadable-subdirectory skip is exactly the original Windows/root condition;
no additional skips were introduced. Parallel test hints become sequential assertion
scheduling. Linux x64 is the actual acceptance platform; platform-specific fixture
execution elsewhere is not implied. Parent reports managed39 passed305 cases with
zero failures/skips; Native execution of this packet remains pending. The remaining
77 broad watcher functions and eight fanotify functions remain explicitly pending.
