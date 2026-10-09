# Configuration dependency review

Independent core review against the pinned production source and the complete
[configuration contract](../CONFIGURATION-CONTRACT.md). This is a bounded source
review, not an acceptance claim or a re-audit of retained checker foundations.

Final runtime qualification is separately recorded in the
[configuration proof](../../validation/results/configuration-verified.json):
source snapshot14, complete configuration-first native checks and one retained
Projects cascade passed with all drift guards false. The scoped external/OS
omissions below remain unchanged; the proof does not expand this source review.

## Reviewed source boundaries

- [DiagnosticWriter](../../src/TypeScript.Scanner/DiagnosticWriter/DiagnosticWriter.cs)
  against `internal/diagnosticwriter/diagnosticwriter.go`: generic plain/colored
  formatting, UTF16 locations, multi-line squiggles, related information, message
  chains, summaries, category/source prefixes and screen/status helpers. The
  previously reported missing summary colon and Tristate receiver issues are
  fixed. Slice conversion helpers deliberately return allocated empty lists,
  matching the original `make(len(diags))`, including nil input. Ordinary and
  configuration AST filename adapters are explicit; transformed/canonical source
  adapters remain the declared external content-runtime boundary. Default English
  message localization is the retained dependency; generic diagnostics retain
  their actual locale callback. Verifier's 22 complete writer comparisons on
  copied build 4 provide independent output evidence, not a substitute for this
  source review.
- [VfsMatch](../../src/TypeScript.Scanner/Vfs/VfsMatch/VfsMatch.cs) against
  `internal/vfs/vfsmatch/vfsmatch.go`: include/exclude/prefix distinctions,
  implicit globs, package/hidden folders, `.min.js`, dynamic roots, rune-consuming
  wildcard backtracking, include buckets, depth, base-path ordering and realpath
  visitation. No concrete source mismatch found in the reviewed family. Full
  original assertion bodies are being ported separately.
- [WalkDir](../../src/TypeScript.Scanner/Vfs/WalkDir.cs) against
  `internal/vfs/walkdir.go`: root and child link reporting, available-versus-nil
  symlink metadata, canonical visited directories, foreign-root filtering,
  sorted names, `SkipDir`/`SkipAll`, missing-root callbacks and entry information.
  Error-sentinel identity follows managed exception/inner-exception adaptation;
  this is not a general Go `errors.Is` framework claim.
- [read OS adapter](../../src/TypeScript.Scanner/Vfs/OsVfs/Os.cs),
  [LinuxStat](../../src/TypeScript.Scanner/Vfs/OsVfs/LinuxStat.cs) and
  [BOM decoding](../../src/TypeScript.Scanner/Vfs/Internal.cs) against the read-side
  `osvfs/os.go` and `vfs/internal/internal.go` routines. Scalar decoding is confined
  to the OS boundary; virtual paths remain original bytes. Directory entries are
  sorted by raw UTF8 bytes; links are classified through their actual target;
  nonregular devices are excluded from accessible file lists. UTF16 BOM decoding
  drops an unmatched terminal byte and replaces unpaired surrogates, as upstream.
  Linux statx preserves real file modes through an explicit platform adapter.
  Its .NET modification-time representation has 100ns precision and its opaque
  syscall storage is not asserted as Go `Sys` identity. Other OS branches are not
  claimed runtime-verified by Linux evidence.
- [config resolver](../../src/TypeScript.Scanner/Module/Resolver.cs) against the
  selected `ResolveConfig`/`resolveConfig` and `ResolvePackageDirectory` closure:
  fixed NodeNext JSON/CJS config lookup, ancestor node_modules, scoped names,
  directory-only early return, package scope, exports/imports condition order,
  blocked null targets, target traversal validation, nested import restoration,
  typesVersions and symlink projection. The source-required self-name priority
  pass and pattern helpers are present. Ordinary source module resolution remains
  the separate accepted Projects branch; configuration completion does not claim
  all general resolver consumers, redirected project checking or source emit.

The initial OS review did not detect .NET's replacement decoding of directory
entry names. A later raw-byte collision probe found that an invalid `FF.ts` name
could silently alias a real U+FFFD filename. Immutable build 8 remains development
evidence and does not close that finding. The subsequent
[LinuxReadPaths](../../src/TypeScript.Scanner/Vfs/OsVfs/LinuxReadPaths.cs) source
packet was independently reviewed: it reads actual `readdir64` names once, checks
record bounds and native EOF/error state, validates scalar UTF8 before any stat,
and checks actual `readlink` and canonical-result bytes. Root's source-based
long-chain challenge required the exact nativepath producer instead of libc
canonicalization. [NativePath.Realpath](../../src/TypeScript.Scanner/NativePath/RealpathLinux.cs)
retains upstream's once-only proc availability test, `O_PATH` open/readlink/close
fast path, EINTR retries and selected POSIX
[filepath walk](../../src/TypeScript.Scanner/Runtime/GoFilePathSymlink.cs) fallback.
The fallback preserves its 255-link bound, lstat/non-directory/missing checks,
absolute versus relative target rewrites and final cleaning. An additional
validation walk included in build 9 was subsequently removed: failed kernel
lookup retains the original path, while an existing non-scalar canonical path is
rejected from its actual raw proc result. No measured libc-versus-Go chain mismatch
is inferred from the original source challenge. Directory and file-descriptor
handles have `finally` cleanup. Unknown directory entry kinds use no-follow statx for link
classification. This is a Linux x64/glibc boundary adaptation, not a claim of
general OS ABI parity. Runtime closure awaits the final cleanup snapshot and
the owner's raw-name/target controls.

The parsed-root integration repair in
[Project](../../src/TypeScript.Scanner/Projects/Project.cs) was also reviewed:
actual normalized absolute discovered filenames pass the existing bounded-disk
admission, while only the legacy compatibility entry resolves relative filenames.
It preserves parsed file order and root-reason indices. Runtime smoke is pending.

## Remaining dependency source review

The following review covers the current source packet after build 9 and the
subsequent nativepath fast-path cleanup. A clean compile and a source verdict are
separate from final managed/native acceptance.

- [Locale](../../src/TypeScript.Scanner/Locale/Locale.cs) and the selected
  [x/text parser](../../src/TypeScript.Scanner/Locale/XText/InternalLanguage/Parse.cs)
  were compared with x/text v0.42.0 scanner/parse/tag/lookup, Default canonicalization
  and compact String producers. The review checked scanner offsets after buffer
  splices, syntax-error precedence, retained partial tags, extlang replacement,
  variant ordering/duplicates, Unicode-extension stable key ordering and conflicting
  values, transformed/private extensions, script/region/language aliases and `rg`
  reconstruction. Default excludes Macro and SuppressScript. The managed Locale
  canonical-byte representation replaces private comparable compact IDs; exact core
  and two special-tag registry membership determine the same selected String result,
  while inexact tags retain full text. Matcher, likely-subtag/parent queries,
  composition, collators and private compact topology are not supplied by this
  closure. The table generator's byte/hash check passed during this independent
  review. No new source mismatch was found; applicable original assertion subsets
  and final native output remain separately required.
- The new typed path closure in [RootedPath](../../src/TypeScript.Scanner/TsPath/RootedPath.cs),
  [RelativePath](../../src/TypeScript.Scanner/TsPath/RelativePath.cs),
  [PathKey](../../src/TypeScript.Scanner/TsPath/PathKey.cs),
  [Dynamic](../../src/TypeScript.Scanner/TsPath/Dynamic.cs) and
  [Extension](../../src/TypeScript.Scanner/TsPath/Extension.cs) retains raw-byte
  identity and explicit Unicode boundaries. Review checked zero/empty sentinels,
  normalized/rooted factories, component containment, rune-count prefix trimming
  under canonicalization, case-insensitive roots versus case-sensitive dynamic URI
  roots, common-directory spelling, UTF8 byte ordering, extension/stem changes and
  dynamic segment hex validation. Pinned Go lower/EqualFold semantics remain distinct
  from identity canonicalization. Existing accepted ordinary path algorithms were
  not broadly re-audited. No selected valid-path discrepancy was found.
- [Semver](../../src/TypeScript.Scanner/Semver/Version.cs) and
  [VersionRange](../../src/TypeScript.Scanner/Semver/VersionRange.cs) were compared
  with both complete pinned production files. Review checked nil comparisons,
  prerelease/build precedence, uint32 overflow and wraparound, lexical numeric
  overflow fallback, partial/wildcard/hyphen/caret/tilde comparator expansion,
  nil alternatives and formatting. The .NET regex adapter deliberately preserves
  RE2 ASCII shorthand/end anchors and the two non-ASCII simple-fold characters;
  raw invalid bytes cannot become a matching replacement character. Source `%q`
  errors use the separately mapped Go quote producer. No source issue was found.
- All six [PackageJson](../../src/TypeScript.Scanner/PackageJson/) counterparts
  were reviewed, including the late typed-state decoder. Ordered duplicate raw
  occurrences, unknown numeric token skipping, default duplicate-policy validation
  before custom Expected callbacks, retained partial maps/slices, null resets and
  non-resetting Valid/Null on later wrong types match the source decoder boundaries.
  Completed field semantics precede later lexical errors; the shared raw-token
  reader owns grammar/error production rather than a second package lexer. Number
  falsiness preserves the original int-interface versus decoded-float distinction.
  Export/import object-kind methods operate on copies as the original value receivers
  do. VersionPaths is returned by value and lazily caches paths on that returned
  value; InfoCache retains canonical keys, first writer, shallow clone and caller
  presentation copies. Monitor/snapshot iteration replaces Go sync storage without
  asserting Go scheduling or unspecified dependency-map iteration order. The current
  source has no unresolved finding from this review; the known build-8 precedence
  discrepancy requires actual unchanged-expectation replay on build 9.

Raw JSON adapter production is core-owned and is not represented as an independent
self-review here. Integration capability admission remains verifier-owned for its
independent review. Final source/test inventories, full outputs and native gates
remain required; presence of translated files alone establishes none of them.

## Assertion handoff

Core owns all 20 original matcher functions and all eight original walker functions
as shared assertion bodies, with thin xUnit and NativeAOT reuse. Verification owns
independent review and original execution/protocol. Test-host writes, OS clocks,
watching and mutation are not introduced by this handoff.

## Read-side time precision disposition

A focused consumer search found no modification-time or opaque-stat access in
TsOptions parsing/discovery, content-mapper metadata or config/package resolution.
Those routines use the declared read/path/entry operations; inherited-config caching
is keyed by configuration paths and actual parsed payloads, not nanosecond stat times.
The explicit 100ns OS timestamp adaptation therefore does not select configuration
results. It remains a limitation of the read-OS API projection, not a claim of complete
platform syscall identity or timestamp precision.
