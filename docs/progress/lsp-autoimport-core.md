# Autoimport and language-service core owner

The complete pinned autoimport family is now the next dependency owner. Production
and checks remain frozen for the current combined dependency gate; new source drafts
are outside the compile glob under `artifacts/language-services/drafts/autoimport`
and `artifacts/language-services/drafts/core`. No autoimport execution or service
acceptance is claimed.

The source pin is `fed0bf24149fb1ed36039212648bafdafc1ea10e`. The eleven
production/generated autoimport counterparts contain 5,507 lines and 190 functions.
The five original assertion files contain twelve Test functions; TestMain remains
a separate harness entry. The draft source inventory retains every source hash.

The dependency order is index and module/export identities, symbol extraction and
alias resolution, registry/bucket rebuilding and view/specifier caches, then the
change-tracker-backed import fixes and import adder. Registry rebuilding must retain
original project/file/package dirty state, package-entrypoint conditions, extraction
owners and parallel work boundaries. A registry substitute or repeated full Program
compilation is not an implementation of those responsibilities.

The genuine core counterpart is `internal/ls/languageservice.go`, with its actual
`internal/ls/host.go` contract. It owns the active preference value, existing Program,
converter owner and lazy document-position-mapper cache. Its prepared autoimport
query must preserve the canonical-file check and real ErrNeedsAutoImports result.
Directory queries retain the upstream limitation: they are used for module-specifier
completions and intentionally cross the snapshot-in-time boundary.

Namespaces are `TypeScript.LanguageServices.AutoImport` and
`TypeScript.LanguageServices`. Foundation owns
`TypeScript.LanguageServices.Change.Tracker` and complete formatting. The supplied
Tracker constructor takes request context, CompilerOptions, FormatCodeSettings and
Converters; GetChanges returns grouped edits and unmappable paths. The actual
ImportAdder/Fix consumers will determine all additional API joins before promotion.

The first index draft retains the original pointer owner, borrowed name headers,
UTF8 byte offsets, Unicode case/fold operations, required result capacities and
clone rebuilding. Its empty-prefix result returns the existing entry header. It is
a draft, not yet compiler-verified or independently reviewed.

The actual LSP stack sanitizer now requires additional pinned regexp APIs:
MustCompile, ReplaceAllString and FindStringIndex. Their complete replacement
worker closure (replaceAll, expand and extract) is drafted separately, preserving
capture ownership, original empty-match advancement and the final string-copy
boundary. Existing three matching engines already accept requested capture counts.
This prospective extension is not represented by the matching-only dependency23
proof. It also needs the genuine strconv.CanBackquote worker (existing GoQuote
already supplies Quote) and shared Unicode IsLetter/IsDigit tables. The previously
required autoimport IsLower table and these L/Nd tables will reuse/rehome the exact
pinned generated range owners, without a version-dependent .NET substitute or
retained table duplication. Required borrowed checker member-lookup overloads and
Core.UnorderedEqual will be promoted with the coherent consumer family.

The original AliasResolver intentionally panics for its unused checker-host methods
and returns nil diagnostics-only maps. These leaves retain upstream anchors and
limitations. All actual binding, resolution and concurrent cache workers are being
ported; the original GetDiagnostics crash assertion remains required. No authored
empty-success replacements are added.
