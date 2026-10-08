# Anonymous object checker adaptations and limits

Pinned upstream: `fed0bf24149fb1ed36039212648bafdafc1ea10e`. This phase extends
accepted scalar checkpoint `7e692dd`; its original [evidence](./CHECKER-VERIFICATION.md)
and [departures](./checker-departures.md) remain historical. Current
[plan](./OBJECT-CHECKER-PLAN.md), [partial function inventory](./checker-functions.json)
and [source map](../source-map.json) describe the extension. The [final guarded evidence](../validation/results/object-checker-verified.json)
accepts this bounded extension: 43 object and 45 scalar Native AOT cases, 149 checker
xUnit tests and all earlier native/xUnit regression gates pass. This is not broad
checker/compiler acceptance.

## Selected semantics and explicit boundary

The same fixed single-file TS script/noLib/noResolve/ESNext/strict-null options now
admit finite anonymous inline type literals and object literals, nested required/
optional/readonly data signatures, static identifier/string/numeric keys, shorthand,
dot reads, contextual initializers and structural relationships. Eight actual 2318
missing-library global diagnostics remain separate from file/binder diagnostics.
No libraries or invented global declarations are supplied.

Admission is authored input/API support, not ported checker logic. It rejects named
aliases/interfaces/type references, source union/intersection/keyof syntax, methods/
signatures/calls/index signatures, spreads/computed expressions, arrays, optional
chains/element access, explicit writes, branching flow, modules, checking directives,
external references and tagged JSDoc. Existing scalar admits remain supported.
Two prior scalar admission negatives (`let x={}` and `let x:{}`) are intentionally
retired in current selection; historical fixture bytes/results are preserved.
Queries reject foreign nodes/types/symbol declarations. Sources must remain immutable;
no general checker concurrency/cancellation/Program host API is introduced.

## Actual object algorithms and storage

The selected checker retains upstream anonymous member resolution, late-binding
container caches, ordered named properties, contextual typing, declaration cycles,
transient member symbols, fresh/regular/widened identities and deferred diagnostics.
Transient symbols retain actual bound declaration/parent/value/target relationships.
Optional declarations create genuine internal undefined unions; readonly behavior
follows upstream property/modifier comparisons rather than blanket immutability.
Straight-line declaration/start/shared flow supports identifier and property reads;
there is no new branch/loop/linked FlowList analysis.

Types use stable managed wrappers/payload inheritance for Go embedded structs and
pointers. Actual StructuredType members/properties/signatures/call-count/index slots
are retained. Signature/IndexInfo are partial typed storage; admitted signature/index
slots are genuinely empty, and nonempty semantic behavior rejects. Mapper, generic
instantiation, type-parameter, signature resolution and retained general intersection
models are excluded instead of represented by fake valid instances. Existing paged
link-store semantics and shared globals/globalThis exports identity remain intact.
Go arenas/pointer cells use managed allocations and stable reference identity.
Ordered sets/maps use managed insertion order plus identity membership; nullable
lists/tables preserve nil versus allocated-empty states where observed.

Relations retain the real Ternary recursive cache, failed-cache diagnostic retry,
maybe-key/source-target stacks, recursion budget/reliability flags, property identity,
missing/optional/readonly checks, fresh excess checks, weak targets, elaboration,
error-chain elision and speculative restoration. Managed list snapshots preserve
Go slice-prefix isolation for related diagnostics. Nonempty signature/index/generic/
constraint/named/intersection paths reject; empty Call and Construct branches still
invoke actual resolution helpers and preserve union cache effects.

`getRelationKey` uses a value key containing the complete selected preimage
(source ID, target ID, intersection state), including upstream identity ordering,
in place of opaque XXH3-128 hash storage. Earlier union/type intern adaptations also
retain complete selected identities. This changes representation without replacing
recursive relation logic; actual hash bits are not compared or claimed equal.
Finite property-key extraction and literal-key union intersection reductions are
required by genuine best-match diagnostics. They preserve upstream reductions and
cross-product limits, while retained structural intersection payloads reject.
Source-level keyof/unions/intersections remain outside admission.

## Diagnostic-only reuse and printing

Object diagnostics require the real nonnil enclosing-declaration path. NodeBuilder
retains context stacking, property serialization, truncation/cycle/depth guards,
enclosing symbol types, actual PseudoChecker inference/equivalence/fallback and
selected NodeCopy source reuse. The initially proposed nil-context-only projection
was rejected. Recovery retains original restoration/length/clone behavior; static
selected syntax rejects entity/reference/JS/signature/dynamic branches before their
unselected wrapped tracker/deferred-report paths. No general declaration emit is
provided. Reverse-mapped implicit-undefined/accessor/parameter behavior rejects.

Serialized type cache keys project fixed default flags/internal flags/no infer
parameters onto genuinely varying fully-qualified/InObjectTypeLiteral states.
Cache reads/writes, added length, truncation and clone identity remain actual.
No external tracker is supplied. Inference fallback preserves the original nil
callback behavior, while cyclic reports still set actual reportedDiagnostic and
prevent cache writes. Symbol tracking and general tracker interfaces are excluded.

A narrow mapped EmitContext stores actual original chains and selected per-node
flags; first original links copy flags across clones, including cache hits. Scalar
string type nodes use NoAsciiEscaping; synthesized quoted property names use normal
ASCII escaping. Selected source text reuse and original quote/numeric separator
rules remain observable. General transforms, generated names, comments, maps,
source maps and public emit APIs remain excluded. Single-line diagnostic nodes
retain default spacing/order/semicolon behavior. Internal formatter/diagnostic
arguments remain raw byte strings; public TypeToString decodes once. Diagnostic
invalid-UTF8 run replacement remains separate from scanner JS/surrogate decoding.

## Evidence limits and maintenance

Current [coverage](../validation/object-checker-corpus/coverage.json) observes actual
properties, transient/bound declaration targets, nil/allocated-empty members,
46 repeated widening observations and populated Widened cache edges. No existing
RegularObjectLiteral cache edge is hit. ObjectType.target/shared nonnull member-table/
property writeType/nameType/containingType edges are zero, so those nonzero paths
remain unexercised. Private union-property/discriminated-context/intersection cache
contents are not exhaustively compared. Observers use the same documented query
order, which itself populates some real caches; existing cache observation does not
populate entries solely to claim coverage. No broad mutable cache parity is implied.

Reviewed C# hand ports are authoritative; complete algorithm regeneration is not
claimed. Narrow enum/helper generators and authored inventory/verification tools
are mapped separately. All executable source/tool/corpus writers froze before the
final gate; documentation/source-map edits may continue. No optimization, profiling,
performance/concurrency guarantee or full checker/compiler acceptance belongs to
this checkpoint. The guarded gate passed with exit 0; no implementation or next-phase work is active.
