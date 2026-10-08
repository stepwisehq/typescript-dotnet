# Bounded checker adaptations and excluded paths

Upstream: `fed0bf24149fb1ed36039212648bafdafc1ea10e`.
The reviewed C# is authoritative. [checker-functions.json](checker-functions.json)
records selected functions, upstream and destination locations, retained conditional
branches and explicit rejection features. [CHECKER-PLAN.md](CHECKER-PLAN.md) is the
input contract. This is a partial checker port, not the complete checker package.

## Initialization and entry boundary

`Checker.Create` is an authored admission adapter over `CreateCore`, the selected
`newChecker` initialization. Inputs are one already parsed TS script, no library,
no resolution, ESNext, strict null checking, no implicit-any errors, no strict
bind/call/apply, and no optional-property mode. Semantic options are fixed, not an
unrestricted CompilerOptions API. Parse errors, semantic directives and external
references, modules, non-TS modes, JSDoc tags and all unselected declaration/type/
expression forms are rejected explicitly. Node queries reject foreign-source
nodes; type APIs reject types owned by a different checker. The source tree is
expected to remain immutable while the checker is used. General concurrency and
cancellation APIs are not provided.

Initialization retains transient symbols, ordinary name and suggestion resolvers,
scalar intrinsics, error/unknown/undefined markers, fresh/regular literals, real
boolean unions, selected empty-object/generic markers, shared globals/globalThis
exports identity, primitive symbol type links and real required-global lookups.
The missing no-library globals therefore produce the actual eight 2318 diagnostics.
Single-file globals are populated from the already bound script. The selected
built-in conflict paths are retained; other global-symbol merging is unsupported.
Program host/cross-file module state, type parameters, signatures, arrays/tuples,
iterators, callable/global function models and unrelated initialization caches are
excluded. Their corresponding semantic branches cannot silently supply fabricated
values: source admission or explicit runtime exceptions bound the omitted paths.

## Selected functions and runtime boundaries

Scalar declaration checking preserves annotation/inference, cached initializers,
mutable widening, duplicate-declaration flags/type agreement, genuine name
resolution and spelling suggestions, diagnostic collection/deduplication, and
straight-line declaration-assignment flow. Self and forward references retain type
resolution cycles, used-before-declaration and definite-assignment diagnostics.
`getTypeOfNode` retains the real final `errorType` arm for admitted nonsemantic
locations, including string/numeric literal children in type annotations; it is
not a successful stub for omitted declarations. Optional internal undefined unions
are used by flow despite source-level union syntax being excluded.

Relationships retain primitive/fresh literal checks, small scalar-union
constituent comparison, nullable-target normalization and scalar error chains.
General recursive relation caching, structured comparisons, constraints and
signature/property/call elaboration are unsupported. The fixed scalar path has
no callable signatures, so the omitted call/construct elaboration cannot succeed
there. Flow retains start, declaration assignment, shared nodes and unreachable
state. Branch/loop/call/reduce/evolving-array flow, explicit assignments and the
upstream general analysis-depth fallback are rejected explicitly.

## Diagnostic-only serialization

The selected `typeToString`/`typeToStringEx` and `symbolToString`/`symbolToStringEx`
wrappers preserve their function boundaries. Parameters for general flags and
verbosity are excluded rather than exposed as falsely supported APIs. The retained
context is nil enclosing declaration, default type formatting flags, no verbosity,
no error-truncation override and scalar diagnostic output. `typeToStringEx`
retains the recursion bound and 320-byte final cutoff. NodeBuilder creates actual
keyword/literal type nodes; the selected Printer writes those nodes and synthesized
identifier expressions for symbol suggestions. General object/alias/qualified,
computed-name/declaration/signature formatting, emit context, source/comments/maps,
trackers, flag variants and public emit APIs are excluded. Exact selected string
escaping and lone-surrogate/raw-byte handling remain observable.

Internal formatter results and diagnostic arguments are raw UTF-8 byte strings.
Public `TypeToString` decodes once to .NET text. Public source input conversion,
raw literal storage and byte positions use the existing scanner/parser adapters.
This public conversion is separate from upstream byte-returning APIs.

## Managed representation

The selected union intern table uses constituent identity keys rather than Go's
opaque XXH3 key. Keys include the same sorted constituent IDs; aliases/origins and
unselected reductions are rejected. Type/literal/union cache identity remains
observable; absolute allocation IDs are not cross-runtime canonical IDs.
Type payload inheritance preserves Go embedded payload casts and one stable Type
wrapper. Unselected Signature/Mapper fields and models are excluded, not fake
instances. `PagedLinkStore` preserves 256-slot page presence; node stores expose
zero-value struct slots on an allocated page, while symbol stores retain nil pointer
slots until accessed. Managed cells replace Go slot addresses and arenas.

Checker, relation helper, NodeBuilder, Printer and TextWriter instances are fresh
managed allocations instead of upstream pools/arenas. No performance measurement,
optimization or concurrency claim belongs to this phase. Managed list copying and
existing raw byte-string allocation adaptations remain from accepted earlier
phases. General language/runtime helpers outside selected usage are not implied by
these partial counterparts.

## Re-entry

Run `python3 tools/checker-function-inventory.py` after source formatting to refresh
function locations. Support enum/resolver/query-helper generation has its own
scratch equality checks; the hand-ported checker algorithms do not claim complete
source regeneration. Central library builds belong to the algorithm owner;
independent Go/managed/native comparisons and xUnit gates belong to verification.
