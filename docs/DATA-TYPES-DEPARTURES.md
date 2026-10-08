# Data-type campaign adaptations and limits

The bounded data-type contract is **accepted on Linux x64 Native AOT**. No active
implementation work remains. Acceptance is recorded in [final evidence](../validation/results/data-types-verified.json). The target is
[the completion contract](./DATA-TYPES-CONTRACT.md), extending accepted anonymous-object
checkpoint `1f7c953` at upstream `fed0bf24149fb1ed36039212648bafdafc1ea10e`.
Historical [object departures](./OBJECT-CHECKER-DEPARTURES.md), fixtures and acceptance
hashes remain unchanged. [Source mapping](../source-map.json) marks expanded/new
campaign counterparts as accepted within their recorded bounded partial scopes. This document does not narrow the contract.

## Selected models and algorithms

Retained data models now include actual alias links, InterfaceType declared/base
caches, TypeReference targets/arguments/instantiations, internal this TypeParameters,
IntersectionType and union alias/origin/property state. Non-generic alias heritage
can require hidden this/reference/mappers; these are real selected dependencies,
not excluded simply because written generics are rejected. Mapper wrappers/data
classes flatten Go interfaces, retaining the original selected factories and Map
operations; an authored static kind discriminator supports reflection-free inspection.
Source generic/inference-context APIs are not implied. The genuine empty inference
context-stack scan is retained; hidden-this contextual types return unchanged when
no inference context exists. A nonnil callable/generic inference context is guarded.

Types and symbols use stable managed references instead of Go pointers/arenas.
Embedded payloads use inheritance and explicit wrapper casts, with most-derived
static DataType dispatch. Lists/tables remain nullable and distinct from allocated
empty collections. Interface inheritance retains original copy-before-add members,
ordered properties and identity checks. Ordered Go sets/maps use managed ordered
collections or insertion-order lists with identity membership, not sorted substitutes.

Actual source nodeLinks/typeNodeLinks fields use the existing core.LinkStore identity
map. They previously used the paged checker nodeLinkStore, whose page-presence TryGet
semantics exposed unrelated empty cells; rich comparison identified that implementation
error. Paged symbol stores and the original paged utility implementation remain
separate and unchanged. No broad mutable cache equivalence is claimed.

Checker keys now store the **complete original key-builder byte preimage** rather
than XXH3-128 bits. Original ordering, tags, alias/origin and hidden-argument keys
remain meaningful; opaque Go hashes are not compared. Recursive relation/maybe
caches, stacks, budgets, failed-result retries, ternary results and error snapshots
retain the selected upstream algorithms. Managed prefix copies preserve Go slice
length isolation during speculative recovery and related-diagnostic rollback.

Source unions/intersections and named recursive shapes are selected, including
real reductions, deferred synthetic property types, discriminant Cartesian fallback,
contextual object checks and missing/excess/optional/readonly member diagnostics.
Broad class/callable/array/mapped/generic branches remain explicit guards. This is
not a replacement property-loop checker or full TypeScript semantic implementation.

## Necessary internal indexes and globals

Written index signatures/keyof/indexed access remain excluded. An interface based
on an alias to any can genuinely synthesize string-to-any IndexInfo; selected
construction, membership, relation and diagnostic formatting preserve it. Finite
named property-key extraction can also create actual IndexType origins needed by
best-overlap diagnostics. Those origins may be retained only in private property-key
caches rather than ordinary source-query graph roots. The named best-overlap failure
case now observes two actual IndexType origins by reading existing cache entries;
no getter is forced to create them and written keyof remains excluded. Unhit
origin/index branches are not implied by those observations.

Fixed single-script noLib/noResolve initialization still consults actual source
builtin declarations. Missing global diagnostics remain separate, with the exact
count determined by the source. Internal globalThis exports share the globals table;
actual module resolved-export caches and filtered anonymous members can separately
clone tables. ModuleSymbolLinks records the genuine cache; source modules/re-exports
remain unsupported. No standard-library declarations are invented.

## Diagnostic-only support

Named/recursive serialization retains actual symbol meaning/accessibility and
nullable accessible-chain caches, lexical shadowing, hidden-this/reference handling,
alias/origin identity, mapper-aware reuse and enclosing declaration context. Selected
SymbolAccessibility, EmitResolver and nodebuilder flag counterparts are partial;
general declaration emit/cross-file containers are excluded.

Serialized node keys retain selected full flags/internal flags, replay tracked symbols
and honor original reportedDiagnostic/cache conditions. Circular-base formatting uses
its actual explicit WriteArrayAsGenericType flags. Fully-qualified collision and
unreduced-intersection diagnostics retain their explicit-only flags rather than
inheriting the default alias flags; qualified-name diagnostics use
actual DoNotIncludeSymbolChain state. Unsupported callback/verbosity/inference/emit
contexts are guarded, not silently projected to ordinary successful output.

NodeCopy and diagnostic pseudochecker retain selected source reference/union/intersection
reuse, symbol tracking and recovery prefix rollback. Printer type precedence preserves
parenthesized union/intersection origins. Per-node original/escape flags preserve the
existing unusual Unicode/static-key behavior. Bare unresolved types now attach the
actual synthesized leading `/*unresolved*/` comment; only that root type selects the
upstream comment-preserving printer mode. The actual shouldEmitComments source-file
guard still applies: a nil-context private-cache type formats as `any`, despite its
real attached comment. Selected single-line synthetic-comment
helpers/storage are included, while source comments/multiline/general emit remain
excluded. Public output decodes once; internal raw strings and diagnostic argument
invalid-UTF8 replacement remain separate from scanner surrogate semantics.

## Evidence and maintenance

Handwritten selected ports remain authoritative. Enum/helper generators are narrow;
no complete checker regeneration claim is made. [Independent core review](./progress/review-data-types-core.md)
records source fixes, managed dispositions and final native confirmation; no
selected-path review blocker remains. Canonical observations may cause
actual lazy resolution/formatting cache effects; observation order must be shared and
missing/empty/cache-only state compared rather than inferred from counts.

The frozen gate passed in 82.979 seconds: 92 rich Native AOT data-type cases
(14 tiny + 78 matrix), 43 anonymous-object and 45 scalar native regressions, 78
boundary controls, 20 original-Go utility observations and 10 API selfchecks.
All 2,146 xUnit tests and prior scanner/parser/binder native regressions passed.
Mapping validates 91 library C# files exactly once, 126 entries and 941 inventoried
methods. Unhit internal model/cache edges remain explicit coverage limits; these
counts do not imply broader compiler or branch coverage. Frozen source SHA256:
`ac9763058844d0ce1daa50fd8c6ba22748c083713592ebd7d2aabf81cfe7b928`;
verification assets SHA256:
`80052551e9d70e28315750ee041d4774e4ab48c31994cbbed8b2eb2aaacf47ce`.
No optimization/profiling, performance guarantee, checker concurrency API or full
compiler/project/library/emit acceptance is part of this work.
