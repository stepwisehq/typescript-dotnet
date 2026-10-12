# Watcher production source review

Reviewed the promoted [mapping packet](./lsp-watcher-mapping.json) against pinned
TypeScript `fed0bf24149fb1ed36039212648bafdafc1ea10e`, selected Linux x64 x/sys
v0.48.0 boundaries and the documented Go1.27.1 path/directory leaves. This is a
production source review, not execution or original assertion acceptance.

The shared subscription/rollback/base/callback owners, event sequence/drain and
coalescing rules, path comparison/rebasing, native and portable walks, Linux
inotify/fanotify start/shutdown/mark/subscription/error paths and complete LSP
registration/ancestor-promotion/synthetic-create/throttle owners reconcile.
Explicit capacities and required name/handle copies survive reused native
buffers; retained slice headers and callback state updates preserve Go ownership.
Linux stat, statfs, dirent64, poll, inotify and fanotify ABI projections are scoped
to Linux x64. Native backend platform exclusions and default/fallback selection
are explicit; this does not claim other native operating-system backends.
The focused PreferredMarkupKind leaf also preserves the first open value and
empty-input PlainText default.

Two concrete source findings were sent to the author and root:

- The original watcher.String() Stringer projection is missing. Name() alone is
  not a substitute; the author confirmed the omission and will restore the raw
  Stringer role.
- Debounce.CoalesceWait passes mutable MinWaitTime directly to Task.Delay.
  Negative durations differ from Go time.After, which is immediately ready for
  all nonpositive values. Preserve that actual timer admission in the owner.

Both narrow corrections are saved and independently reconciled. The final source review is clear and hash-bound in [watcher.json](../../artifacts/language-services/source-review/watcher.json). Existing
original Go watcher reference passes remain reference evidence only. Candidate
original fixtures, managed behavior and actual Native transport gates remain
pending. No builds or workloads were run by this reviewer.
