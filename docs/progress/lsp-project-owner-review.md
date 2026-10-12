# Project low-level owner source review

Independent reviewer: semantic-services lane. Pinned upstream:
`fed0bf24149fb1ed36039212648bafdafc1ea10e`.

The complete dirty eight-file family, reference-count/owner/parse/extended-config
caches, background queue and focused Collections.SyncMap constructor/stateful Range
joins were compared with their actual Go counterparts. The eight original direct
assertion functions were reviewed, including dirty four and background five subtests.
Full Session/cache lifetime integration, overlay/snapshot operations and logging are
outside this review; no build or runtime acceptance is claimed.

The mutation, proxy and lock boundaries align with upstream: losing dirty entries
route changes through the winner; Delete and locked Delete retain their original
distinction; clone-on-first-mutation and finalization hooks preserve original owners.
Reference acquisition publishes parsed/bound values under the entry lock and final
release deletion retains the original stale-entry retry. Nil versus allocated maps,
full parse-option/content identities and canonical/supplemental bundle lifetimes are
preserved. Range retains the original two traversal phases, including its unusual
phase-local callback stop. Snapshot enumeration is a managed adaptation to permitted
Go callback mutation, not an ordered-traversal guarantee. Background cancellation,
nested pending accounting and Close's enqueue exclusion match the original scope.

**Source review clear.** The one narrow finding is resolved: the optional SyncMap
comparer now survives both Clone and ToMap copy constructors. The added authored
control checks case-insensitive identity, copied-owner independence and unchanged
source values; it is counted separately from eight original functions. Final hashes
below bind these reviewed changes. Behavioral/integration acceptance remains pending.

## Reviewed content bindings

- [Box.cs](../../src/TypeScript.Scanner/ProjectSystem/Dirty/Box.cs): `b38fbdfbcbbdbf822649fd0638c5b026503b6244d3bffaf1a4a29b51006e69f3`
- [CloneableMap.cs](../../src/TypeScript.Scanner/ProjectSystem/Dirty/CloneableMap.cs): `82040b87c53a8a5e47da827664656ec71b45e76f79e641479c6dd2d1a3bb3215`
- [Entry.cs](../../src/TypeScript.Scanner/ProjectSystem/Dirty/Entry.cs): `7078d19d46b3d7fbcfe04ae860a327a452fdc2631d5f541c62826ff82deda352`
- [Interfaces.cs](../../src/TypeScript.Scanner/ProjectSystem/Dirty/Interfaces.cs): `c447614f0a863a6f49295039cdfb9f634b30a5fc433370336c801080d3683fd0`
- [Map.cs](../../src/TypeScript.Scanner/ProjectSystem/Dirty/Map.cs): `ea8e5bb668fed216740dfff188804bde33e620a1528a4cf98088fedda8408030`
- [MapBuilder.cs](../../src/TypeScript.Scanner/ProjectSystem/Dirty/MapBuilder.cs): `2ff55e8c1914450629ca9291b6f9877ad2aa1f6ecd00496900a2f2ba05b9805c`
- [SyncMap.cs](../../src/TypeScript.Scanner/ProjectSystem/Dirty/SyncMap.cs): `81ac8b0559d4c3d782dbb7474354a6c11d4a2e1d49719cbdf889244456aaf6c6`
- [Util.cs](../../src/TypeScript.Scanner/ProjectSystem/Dirty/Util.cs): `7ce6549bc0ca1fa9e149e2af4c4249db6175cc290d3e2109d5d6b5e95586a832`
- [RefCountCache.cs](../../src/TypeScript.Scanner/ProjectSystem/RefCountCache.cs): `e2a832359cb1ff291f6cbd2b81425043ae86dbf99cc2e73a80ed030fd360e8d4`
- [OwnerCache.cs](../../src/TypeScript.Scanner/ProjectSystem/OwnerCache.cs): `801fb382f513ac0cda3aab3bbb2c0f251a41024a066cc34ba179d303f51766f4`
- [ParseCache.cs](../../src/TypeScript.Scanner/ProjectSystem/ParseCache.cs): `64c24b9b26db074564242b6e67304cfc6fe624ac93f219e3db0fade5ffd9aecd`
- [ExtendedConfigCache.cs](../../src/TypeScript.Scanner/ProjectSystem/ExtendedConfigCache.cs): `8665bdb2d9a2bbfcb83adccd455f35f33a7f1ad1eab53459e43445a13cb07379`
- [Queue.cs](../../src/TypeScript.Scanner/ProjectSystem/Background/Queue.cs): `8bf48390bc3bfff8ade5220b8b7b9fa9412039ee35458b99917ea1efdc7ac127`
- [SyncMap.cs](../../src/TypeScript.Scanner/Collections/SyncMap.cs): `b29bfbf5ddb28a23c43e450e3659ac6f0dad23a6a6e042d730e186407b0a6750`
- [ProjectCacheChecks.cs](../../validation/LanguageServices.Checks/ProjectCacheChecks.cs): `e84988329e8a8abac8a42c228ab7b41fb36051e381db41c0e39908461f3fefb8`
- [ProjectDirtyChecks.cs](../../validation/LanguageServices.Checks/ProjectDirtyChecks.cs): `49b79325e67597c2dde259e58da4018da90738cfdc83bbd1a26ecafa2267a5e4`
- [ProjectBackgroundChecks.cs](../../validation/LanguageServices.Checks/ProjectBackgroundChecks.cs): `f2906cdb373d3b433c8330b65f722f77185970ec2fdd85499037b528be0c2984`
- [ProjectOwnerTests.cs](../../tests/LanguageServices.Tests/ProjectOwnerTests.cs): `e6936ffc7c2b86c6eeff112e8256c16c8ef548c362579ed62ad9312199624f41`

The first integration declaration correction qualifies both duplicate-file parameters
as `TypeScript.Compiler.DuplicateSourceFile`. Their field access and key algorithms
are unchanged; the ParseCache binding above includes this narrow reviewed join.

## Promoted project graph review complete

The separate promoted graph review covers the complete Project44,
ProjectCompilerHost10, ProjectCollection20, ConfigFileRegistryBuilder26 and
ProjectCollectionBuilder44 functions plus the caller-grounded diagnostic equality
adapter and graph ownership checks. It excludes the accepted low-level owner review
above. The source review is clear and bound separately in [the graph review](./lsp-project-graph-review.md); execution and Session integration remain separate.

Read-through of the compiler host, project state/update/clone/ATA and collection
selection/BFS/clone owners has found no behavior discrepancy so far. Required parse
cache acquire/ref/deref boundaries, resolver cleanup, frozen backing source, stable
project order, shared BFS visited owner and default-project tie-breaks match source.
The diagnostic adapter is being assessed at its actual inferred/synthetic callers,
without treating it as a general replacement for Go reflection over arbitrary ASTs.

The source owner corrected the allocation-owner finding: the local extended
config path set now allocates only at the first Add, matching the original zero-value Set. Four
related collection-builder producers and a nil Core.Map output header were likewise
reconciled and narrowly reviewed. These corrections preserve control flow and outputs.
