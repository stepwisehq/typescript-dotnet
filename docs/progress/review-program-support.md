# Independent selected Program dependency review

Core review against pin `fed0bf24149fb1ed36039212648bafdafc1ea10e`;
current clean producer snapshot is 6; earlier consumer evidence is explicitly labelled below. This records source correspondence and
omission accounting, not whole-Compiler or per-function execution coverage.

[CachedVfs](../../src/TypeScript.Scanner/Vfs/CachedVfs/CachedVfs.cs) preserves
read-through lookup/publication conditions, negative and present-null caches,
enabled transitions and clearing each selected cache. ReadFile and casing remain
forwarded. Writable FS operations are the declared external remainder.
[SyncMap](../../src/TypeScript.Scanner/Collections/SyncMap.cs) and
[SyncSet](../../src/TypeScript.Scanner/Collections/SyncSet.cs) preserve atomic
load/store/first-wins and present-null values. Captured traversal is an explicit
managed implementation of the source non-snapshot Range contract; callbacks run
outside locks, and no internal sync.Map topology equivalence is asserted.

[KnownSymlinks](../../src/TypeScript.Scanner/Symlinks/KnownSymlinks.cs) preserves
copy-before-setting directory links, first-observation reverse-map insertion,
nullable known-directory entries, typed path/case comparisons, ignored-directory
boundaries and both module/type resolution callers. No filesystem operation is
invented by the path-guessing helper.

[WorkGroup](../../src/TypeScript.Scanner/Core/WorkGroup.cs) preserves recursively
queued work and single-threaded LIFO draining. Managed Task scheduling and captured
worker exceptions are explicit language/runtime adaptations; the waiting caller
receives the actual failure. ThrottleGroup is external. No scheduler-performance
or exact goroutine-topology claim is made.

[ProjectReferenceFileMapper publication](../../src/TypeScript.Scanner/Compiler/ProjectReferenceFileMapper.cs)
now publishes a base data object with the actual shared dictionaries/SyncMap,
config and source-use flag. Construction Builder host/virtual symlink dispatch
is excluded from the retained object, matching publication of the source embedded
data; Reuse shares that published object. This fixes a concrete producer-lifetime
issue, not a requirement for a wider private-state census.

[FileInclude](../../src/TypeScript.Scanner/Compiler/FileInclude.cs) resolves the
current generation's AST by canonical path and the original import/augmentation
or reference index. Synthetic import nodes remain the original synthetic node,
type/path/lib reference package identity stays source-zero, and diagnostic,
location, related-info and redirect explanation publication uses first-wins
Program-local SyncMaps. The zero-Program root-reason assertion uses actual lazy
processor initialization; tests do not seed an invented host or processor.

[ProgramInspection](../../src/TypeScript.Scanner/Projects/ProgramInspection.cs)
is an authored OS/config wrapper around the actual constructor. It publishes
config, syntactic, binder-only, construction and source-policy per-file inclusion channels,
sorts/deduplicates actual diagnostics and acquires no semantic checker. Its host
retains the genuine bundled environment and configured settings. Snapshot 4b
ordinary smoke loaded 57 library files for the valid example and preserved missing
reference 6053 for the invalid example; comprehensive original/native acceptance
is still pending.

The selected loader/reference scheduling, final table publication and Host callback
source review is recorded below. Prior passing shared tests are evidence for their
assertions; final ordinary/native campaign acceptance remains separate.

## Loader/reference publication pass

Compared source roots/default and explicit library scheduling, automatic-type
pseudo-task publication, library replacement lookups/order, host parse/mapper
callbacks and error budgets, explicit path/type/lib processing, synthetic import
and augmentation scheduling, depth elision, reference recursion and DTS-faking
read host against the current selected counterparts. Reference parsing keeps
actual recursive config/source-output stores; DTS faking answers existence from
source without fabricating declaration text. The four write/panic API operations
are external to the selected read host. Host callback errors remain actual errors.

Three concrete [FilesParser](../../src/TypeScript.Scanner/Compiler/FilesParser.cs)
source omissions were sent to the independent owner before acceptance: freshly
loaded redirects must force child scheduling; the original redirected input
must skip its own include-reason publication; repeated walks of the same acquired
casing must record a duplicate owner only once. These are source branches with
caller consequences, not incidental private representation requirements. Source
`recordedDuplicates` exists to prevent repeated release of one acquisition.
Focused behavioral evidence is verifier-owned and pending repair execution.

A fourth small [ProcessingDiagnostic](../../src/TypeScript.Scanner/Compiler/ProcessingDiagnostic.cs)
condition was reported: a nonempty diagnostic file creates an allocated include
list before walking even zero reasons. The selected chain construction must
preserve that empty-versus-absent condition. No expected bytes or original body
assertions were changed by this review. Current source corrections and actual
focused/ordinary/native evidence remain prerequisites for runtime qualification.

## Current repaired producer qualification

All four reported conditions were re-read after the independent owner repaired
them and are compiled in snapshot 6. Exact source branches are retained; no
original expectations or assertion domains changed. Focused redirected dependency
inclusion and first casing diagnostic owner evidence is verifier-owned.

The current ordinary inspection leaf uses Program.GetIncludeProcessorDiagnostics
per actual source, matching the public source SkipTypeChecking and preceding-directive
filter. Fileless include diagnostics stay in GetProgramDiagnostics, and no checker
is acquired. The earlier raw-inclusive smoke is historical evidence for that
explicit observation seam rather than the ordinary public policy now selected.

Both typed configuration source producers set FileNameIsGoBytes because their
RootedFilePath.AsString values are original Go bytes. This was independently reviewed
by support; getDiagnosticPath consumes FileNameGoBytes, preserving scalar/default
roles elsewhere. The Unicode owner comparison must now be replayed on snapshot 6.

## Supplemental relative-name caller repair

The actual mapper corpus exposed one omitted source toFileName call for the
supplemental canonical filename. The repaired branch applies source relative
path presentation. Review also checked its actual case owner: all three Program
relative renderers now use Program.CaseSensitivity (host FS), with prior parsed/null
fallbacks solely for named legacy callers. This matches source fileInclude.go
148–159 and211–213 without changing absolute diagnostic publication. Snapshot7
compiles the owner repair; actual mapper21 replay is verifier-owned.
