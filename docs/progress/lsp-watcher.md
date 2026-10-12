# LSP watcher dependency

The complete in-process fallback owner is required by the language-services
contract, including its original direct tests. The normal pinned Linux server
branch disables builtin watching when the client lacks dynamic registration and
the selected backend is not fast-recursive; that branch does not exclude direct
watcher consumers or tests.

Approved implementation scope: full pinned `internal/lsp/lspwatcher/lspwatcher.go`
(621 lines,16 original test functions), the shared `internal/fswatch` algorithms,
and the complete Linux inotify/fanotify/fallback closure (about3,270 lines including
platform-selected path/walk workers). Preserve Default's fanotify availability
selection and per-filesystem inotify fallback, kernel rename/FID pairing, descriptor
sharing, debounce/event ordering, overflow/termination errors and close lifetime.
Use focused Native-AOT-safe Linux native transport; BCL FileSystemWatcher would
hide consequential original low-level behavior and is not the selected substitute.

Windows/macOS/BSD native implementations remain explicit platform implementation
boundaries. Keep generic backend dispatch and source-defined unavailability errors;
do not claim cross-platform implementation from Linux acceptance. Permission,
kernel and filesystem-specific original skips must remain visible. No root
privileges or system mutation is required for verification.

Work is preserved outside compilation under
`artifacts/language-services/drafts/fswatch` and the existing server draft directory.
No watcher source has been promoted or behavior accepted yet. Root coordinates
whole-owner integration, original assertions and actual Native transport gates.
