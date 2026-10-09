# Generic collections: support investigation

Status: accepted under the fixed bounded [COLLECTIONS-CONTRACT](../COLLECTIONS-CONTRACT.md), baseline `cfa6d2f`, upstream `fed0bf24149fb1ed36039212648bafdafc1ea10e`. The independently inspected [durable proof](../../validation/results/collections-verified.json) records the final gate passing in 658.892 s with no source, asset or historical drift: actual Linux x64 Native AOT matches all 44 rich cases, executes 12 shared bodies and matches 18 order replies per runtime; all 23,467 xUnit cases pass with no skips. Required overload/declaration witnesses, negative predicate diagnostics and selected retained cache associations are verified. No unresolved known blocker remains. This is bounded collection capability, not whole-checker/all-library parity or exhaustive retained-state coverage. Owned source/tools remain frozen. Earlier checkpoints below are historical.

## Ownership and first model checkpoint (historical)

Support exclusively owns Types.cs, Mapper.cs, Relater.cs and selected diagnostic NodeBuilder/Pseudo/Printer counterparts. Core owns Checker.cs, the new Inference.cs, utilities/grammar/exports and every shared library build. Verifier owns Flow/admission/fixed library loader and observation/tests.

Landed first dependency batch: real TupleType/element model, MappedType/ReverseMappedType/EvolvingArrayType, IndexedAccessType/SubstitutionType, ConditionalRoot/ConditionalType, casts/static payload names and conditional mapper accessor. ReverseMappedSymbolLinks/ArrayLiteralLinks/ComputedNameNodeLinks retain exact fields. The actual backreference and fixing/nonfixing inference mapper packet is prepared in `/tmp/collections-inference-mapper.cs`, awaiting core's concrete inference models/helpers so the first environment build has no dangling APIs. It does not use a nil context fallback. No successful integration build or runtime result claimed yet.

Delivered original Checker-origin global/symbol merge family to core as reviewed scratch `/tmp/collections-merge-reviewed.cs`; core alone integrates it. Includes clone/merged parent/value declaration and duplicate diagnostic/related-info paths. Managed map clone preserves nil/empty and symbols; declaration-list copy retains the source's full-slice append-isolation requirement. Existing createDiagnosticForNode is reused. Source-preserving scaffolds are aids; edited counterparts remain authoritative, not complete-regeneration products.

Next: early genuine library/generic boot integration, then complete selected generic/array/tuple/utility relation and diagnostic formatting families. No support library builds, fixture changes, or blanket source exclusions added.

## Two genuine environments

| Environment | Exact bundled files | Mandatory iteration dependency |
| --- | --- | --- |
| Explicit ES5 collection profile | `lib.es5.d.ts`, `lib.decorators.d.ts`, `lib.decorators.legacy.d.ts` | Array-like fallback, numeric index type, loop flow. Plain synchronous array/tuple `for-of`; no custom iterable promise. |
| ESNext collection profile | Those three plus `lib.es2015.symbol.d.ts`, `lib.es2015.iterable.d.ts` | Real `Symbol.iterator`, unique computed names, synchronous iteration resolver/cache, iterator yield/return/next relationships. |

These are complete, unmodified pinned declarations. ES5 references both decorator files. Iterable references Symbol; `lib.es2015.symbol.wellknown.d.ts` is **not** part of that reference closure and need not be silently added. No DOM, host runtime, ES2015 collection, or ambient user-independent substitute declarations are proposed.

The first set contains 5,001 lines; the second adds 647 lines. All carry Microsoft's Apache 2.0 license. The bundled-file wrapper is [bundled.go](../../upstream/TypeScript/tsc/internal/bundled/bundled.go), distinct from implementing a broad project host.

Important pinned-source detail: [getIteratedTypeOrElementType](../../upstream/TypeScript/tsc/internal/checker/checker.go) at line 6307 chooses iterable resolution from actual `Iterable` availability, not directly from the target. Without that global it takes `isArrayLikeType` and numeric-index fallback. ES5 must still be an explicit profile option if selected; this is semantic support, with no transformation or emitted-loop claim.

Initial recommendation: the ES5 environment is the smaller defensible first generic-collection contract if array/tuple-only iteration meets the requested demonstration. A third coherent choice retains **ESNext target with exactly the ES5/decorator library set**; the missing Iterable global still selects the authentic array-like fallback. The coordinator now favors this third choice. ESNext plus iterable preserves the target but is a materially larger iterator campaign; it should be chosen explicitly rather than treated as two harmless additional files. Every choice supports genuine Array/ReadonlyArray methods.

## Minimum loading and global merge

The fixed host should own one user TS script plus the ordered fixed library set; parse and bind every complete file. Preserve real declaration-file/default-library classification, file identity, source spans, symbol declarations, and cross-file merged globals. The caller must not provide arbitrary extra files, project references, modules, or resolution hooks through this entry.

[NewChecker](../../upstream/TypeScript/tsc/internal/checker/checker.go) at line 913 binds all program sources. `initializeChecker` at 1309 merges each script's locals with `mergeGlobalSymbol`/`mergeSymbol`, then resolves IArguments, Array, Object, Function, String, Number, Boolean, RegExp, ReadonlyArray, and ThisType. Existing C# initialization is explicitly single-file and throws on a collision; a real multi-file counterpart is necessary. This also needs original file order/index lookup, symbol accessibility across fixed libraries, and actual default-library checks in relation diagnostic elaboration. The current relation implementation assumes every declaration is outside a default library; that assumption becomes false.

Use real `skipLibCheck=true`, not a checker dispatcher which ignores unfamiliar library nodes. The upstream decision belongs to [Program.SkipTypeChecking](../../upstream/TypeScript/tsc/internal/compiler/program.go) at line 872 and diagnostic collection at 719/752: it skips semantic checking of declaration files, after loading/binding. It does not skip initialization, requested member resolution, instantiation, or errors arising while checking the user. A small fixed-environment adapter can preserve this decision without porting general projects/modules. Library syntactic/binding state must not be rewritten or suppressed to achieve zero diagnostics.

Library globals must remain authentic, including globalThis/table identity and merged Array members when iterable is selected. Explicit `lib` selection/reference closure should prevent incidental default ESNext/DOM loading. The fixed profile must record target, actual library filenames/hashes/order, skipLibCheck, and all retained strict flags; legacy entries must retain their noLib behavior.

## What laziness really permits

[resolveDeclaredMembers](../../upstream/TypeScript/tsc/internal/checker/checker.go) at 20016 eagerly obtains the member table, internal call/new signatures, and index infos. It does **not** resolve the type of every ordinary named method. `instantiateSymbolTable` at 21144 creates symbols with original target/mapper/declarations; `instantiateSignatureEx` at 21023 deliberately leaves return/predicate unresolved so inference is not fixed prematurely.

Consequently, loading ES5 does not itself instantiate decorator contexts, Partial/Required/Readonly/Pick/Record/Omit, Parameters/ConstructorParameters, ThisParameterType/OmitThisParameter, Awaited, typed arrays, or every Array method. Loading iterable additionally does not itself resolve all Map/Set/WeakMap/typed-array constructors. Iterable defines Map/Set interfaces and constructor **interfaces**, but does not supply the ordinary Map/Set constructor values supplied by the separate collection library.

This is a producer-based boundary, not a promise that a library declaration is always unused. Requested properties, structural comparisons, callback inference, diagnostic suggestions, or type formatting can force those declarations. Explicit guards may remain only for unselected paths; skipLibCheck cannot justify a guard on a selected member.

Current [DataGraph](../../validation/Checker.Validation/DataGraph.cs) at lines 367–370 explicitly asks for every structured property type. Applied to Array, this forces push/concat/every and their rest/predicate families even when the user's expression used only map. Verification should deliberately include full finite Array/ReadonlyArray closure, or use an explicitly nonforcing stored-state observation for unrelated library declarations. Enumerating bound global symbols must not force all library aliases and then claim that demand came from loading. Source-node canonical IDs must distinguish user and library owners rather than drop library spans.

## Required model and algorithm families

| Family | Concrete required support / authentic producers |
| --- | --- |
| Generic interface/reference | Real Array/ReadonlyArray type parameters, hidden this, constraints/default caches, reference arguments/instantiations, substitution keys/alias identity. Initialization itself resolves generic globals. |
| Generic signatures and inference | `map<U>`, `filter<S extends T>`, `reduce<U>`; `chooseOverload` at 9246 allocates real inference contexts. [inference.go](../../upstream/TypeScript/tsc/internal/checker/inference.go) contains 1,697 lines of covariance/contravariance/property/index/signature/union candidates, priorities, fixing, defaults, constraints and intra-expression sites; no nil-context substitute is valid. |
| Mapper state | Real fixing/nonfixing inference mappers and backreferences, deferred targets, merged/composite mapper order, clone context and return inference. Current [Mapper.cs](../../src/TypeScript.Scanner/Checker/Mapper.cs) still guards inference/backreference branches. |
| Relations | Generic signature comparison/instantiation, Array/ReadonlyArray covariance special case, general marker-based generic variance/restart, tuple comparisons, optional/rest expansion, numeric index relations and callback contravariance. Current [Relater.cs](../../src/TypeScript.Scanner/Checker/Relater.cs) explicitly guards these families. |
| Predicates | The actual **written library** filter predicate `(value:T)=>value is S`, constraint S extends T, instantiated predicate identity; inferred user predicates already exist. Library every additionally has `this is ...` if full Array member observation is selected. |
| Array/index state | Real array references and readonly references, array-literal/contextual/evolving arrays, IndexInfo(number→T), numeric element reads/writes and range errors. No fabricated Array object. |
| Tuple/rest | Actual TupleType→InterfaceType/reference target, element flags and labels, fixed/min lengths, readonly, base Array union and interned target keys; rest parameter arity/expansion and tuple slices. ES5 selected simple `T[]` rest and finite tuple rest can be bounded separately from arbitrary variadic generic algebra. |
| Diagnostic formatting | Generic parameter declarations/constraints/defaults, reference arguments, array/readonly/tuple/rest nodes, written predicates, library-aware related-info suppression; source pseudochecker reuse of those type forms. Library construct signatures require genuine storage/serialization when reached even if user `new` remains excluded. |

Exact new payload candidates in [types.go](../../upstream/TypeScript/tsc/internal/checker/types.go): TupleElementInfo/TupleType at 1073/1081; EvolvingArrayType at 1144; IndexedAccessType at 1223; SubstitutionType at 1249; ConditionalRoot/ConditionalType at 1258/1269; MappedType/ReverseMappedType at 1110/1135. InferenceContext/InferenceInfo originate checker.go at 277/291; their state is core-owned, while mapper behavior belongs to mapper.go. Existing TypeParameter has real fields but its source-generic resolution arms are incomplete.

Arrays/map/filter/reduce do not directly name mapped or conditional aliases. `NonNullable<T>` is a genuine generic intersection alias (`T & {}`), and flow can select it internally. Finite array/tuple/rest alone does not establish a requirement for full conditional/mapped/reverse-mapped algorithms. However Function.bind selects ThisParameterType/OmitThisParameter; supplied promise/thenable diagnostics can select Awaited; generic keyof/indexed utilities and inference from mapped inputs demand their actual models. These must receive caller/producer audits before exclusion, especially because the prior callable contract already admitted user thenables. No missing-global stand-in may replace a now-present real library alias.

ESNext-specific mandatory pressure: Iterator defaults, ArrayIterator→IteratorObject bases, `BuiltinIteratorReturn=intrinsic` (strictBuiltinIteratorReturn stays an explicit option), computed unique-symbol keys, IteratorResult discriminated unions, and library `next(...[value]: [] | [TNext])` destructured tuple-rest parameters. Arrays alone can select that declaration during iteration; a source destructuring exclusion does not make the library parameter path unreachable. String iteration and malformed/noniterable diagnostics also need the genuine selected iterator paths, not fabricated array element extraction.

## Proposed substantial source boundary and obligations

Retain Callable behavior, add ordinary generic functions/interfaces/aliases with explicit type arguments, constraints/defaults; mutable/readonly arrays, finite required/optional/labeled tuples, array and tuple rest parameters/calls; ordinary Array/ReadonlyArray methods (especially map/filter/reduce and predicate overloads), contextual callbacks, element accesses and assignments, and synchronous array/tuple `for-of` with loop flow. The final contract must decide spread/destructuring forms rather than accidentally admitting unsupported syntax. General user classes/new, async, modules/project resolution, DOM, user custom iterators, type-level conditional/mapped/template programs and arbitrary variadic type programming can remain excluded only if required internal/library paths are retained.

Verify successful and failing constrained generic calls, readonly/mutable relationships, generic callback inference and overloaded errors, predicate narrowing, accumulator inference, recursive generic members/cache identity, tuple optional/rest arity, default type arguments, empty evolving arrays, nested generic declarations, library name collisions and builtin-driven suggestions. Include real declaration/default-library ownership and cross-file merged table identity. For ESNext add actual iterator result/unique-key/cache witnesses, not just array results with symbols ignored.

The material boundary decision is the iteration environment and source type-level utility breadth. ES5 array-only is manageable without a general module system, but this remains a substantial inference/tuple/relations campaign; it is not a small library-loading patch. ESNext adds an independently coherent iteration family. No fundamental blocker found; feasibility is source-based, not a passed experiment.

### Coordinator's expanded utility candidate: dependency challenge

The pending candidate keeps ESNext + the complete ES5/decorator set and includes all actual ES5 Array/ReadonlyArray instance methods plus Record, Partial, Readonly, Pick, Omit, Exclude, Extract and NonNullable. This is coherent and substantial; it changes the earlier conditional/mapped laziness recommendation into required selected algorithms:

- Partial/Readonly require mapped modifier propagation and homomorphic preservation of arrays/tuples, optional members and index infos. Pick/Record require generic keyof/indexed access, string/number/symbol index keys and declared constraints. Omit composes Pick, keyof and distributed Exclude.
- Exclude/Extract require real distributive and deferred conditional types, any/never behavior, conditional constraints/instantiation and root keys. Existing depth/cycle diagnostics must remain actual source behavior.
- Awaited requires infer parameters, covariant/function parameter inference, function-rest patterns and recursive conditional resolution even with async/await source syntax excluded. Caller-supplied thenables were already admitted; their diagnostic paths cannot be retired merely because genuine Promise/Awaited globals are now present.
- Generic inference from mapped inputs can create ReverseMappedType. No source remapping is necessary to make that payload reachable; include its actual source-selected resolution if generic APIs accept such inputs.
- Real SubstitutionType and fixed/nonfixing/backreference mappers are part of the model/cache graph. Generic inferred return signatures and constraints cannot use the prior nil-inference-context boundary.

Recommended source limit: ordinary generic declarations/constraints/defaults, keyof/indexed access, plain mapped types without `as` remapping, and the selected conditional/infer families over existing scalar/object/function/array/tuple forms. The coordinator must specify whether arbitrary written conditional/infer syntax is admitted or only genuine utility/library declarations; either way required internal producers remain implemented. Template-literal keys, intrinsic string transformations, user classes/new/construct behavior, general libraries/modules/custom iterators may remain excluded. Library construct signatures need faithful storage/serialization when reached, not manufactured empty slots. With strictBindCallApply still false, CallableFunction/NewableFunction are not eagerly resolved; Function property augmentation is real, so using bind/call/apply must be an explicit supported-or-rejected API boundary rather than presumed unused by spelling.

No additional mandatory class or project machinery was found for this candidate. The main implementation load is genuine inference, generic relations, mapped/conditional/indexed resolution and corresponding diagnostic reuse/printing. These are algorithm families to batch, not one method-at-a-time patches.

## Avoidable prior cost and proposed batching

Callable integration used 24 central builds. Its first frozen full gate passed in 529.436 s but was only pre-closure evidence; late local-interface ancestor guards, merged anonymous cache producers and caller-provided Symbol paths required reopening. The accepted exact-source gate took another 512.357 s. Existing successful fixtures had not established those producer exclusions. The second cascade was avoidable validation expense, not evidence of an optimization need.

For this campaign: agree concrete model/API names once, then compile coherent families (fixed multi-file/global initialization; generic reference/constraint/mapper; inference+generic overloads; array/tuple/rest+relations; iteration and formatting). Source-owner compiler packets should be batched; do not build for each helper visibility change. Direct upstream inference tests and small vertical library witnesses should precede a broad matrix. Audit remaining guards against **nested declarations, real builtin/library names and cache producers** before freezing. Keep one shared builder and isolated adapter snapshots. Run one comprehensive historical/native cascade after the full selected matrix, source-family reviews, new direct assertions, whitespace and all-owner freeze have closed; never use a premature full gate as a substitute for closure review.

## Pinned library fingerprints

| File | SHA-256 |
| --- | --- |
| lib.es5.d.ts | `45cc92f8dd3df7cb07b7d19e2b2ca34e78bc005c910c1e53ee1cf9bf37910215` |
| lib.decorators.d.ts | `1ce14b81c5cc821994aa8ec1d42b220dd41b27fcc06373bce3958af7421b77d4` |
| lib.decorators.legacy.d.ts | `b3a048b3e9302ef9a34ef4ebb9aecfb28b66abb3bce577206a79fee559c230da` |
| lib.es2015.iterable.d.ts | `ebd9f816d4002697cb2864bea1f0b70a103124e18a8cd9645eeccc09bdf80ab4` |
| lib.es2015.symbol.d.ts | `83e3871cda2abd4421970fa923cca51a9ce8cdbece6f898285643ab030cb80f3` |

## Current implementation checkpoint

Central build 2 compiles the fixed library/global initialization and generic reference/default batch with zero warnings or errors. This is a dependency milestone, not a runtime or native acceptance result.

The support model now contains the actual Tuple, Mapped, ReverseMapped, EvolvingArray, IndexedAccess, Substitution and Conditional payloads and roots. The fixing/nonfixing inference mapper retains the core-owned live context, consumes intra-expression inference before fixing, and clears unfixed inference caches. The deferred mapper retains its upstream function storage; its sole producer uses a fresh MapIndex callback parameter per returned closure rather than capturing a shared C# loop index.

The next coherent dependency compile includes tuple/rest count, arity, positions and labels; generic signature comparison; full variance marker probes and recursive restart; mapped/indexed/conditional/substitution structural relationships; and array/tuple/reference plus mapped/conditional diagnostic node construction. Source-excluded template/intrinsic-string and user-class branches retain explicit guards. Required core operators, inference APIs and formatter visibility are still integrating; no successful collections runtime entry is claimed.

Pending: finish the source-family and guard inventory, generic/rest/construct diagnostic printer and pseudochecker closure, cache/identity observations, the genuine realistic demonstration, focused managed verification and final native acceptance. This note authorizes no scope beyond the fixed contract.

## Grouped dependency checkpoint: after central build 4

Core's central build 4 exposed 204 dependency/translation errors; this was not a runtime milestone. The support packet now fixes its reported tuple enum/mask/static-call translations and restores the original `compareTypesAssignable` boundary. Selected Core helpers preserve nil/empty and captured slice length; managed `List` prefix copies remain an explicit slice-header representation adaptation, with no claim that future backing mutations alias. Core is integrating the missing mapped/conditional/index/signature families in one grouped compile.

Support delivered the three original Checker-origin mapped materialization functions (`resolveMappedTypeMembers`, `getTypeOfMappedSymbol`, `getLowerBoundOfKeyType`) as reviewed scratch; only core integrated them. Publication precedes recursive member work, mapped property flags and declaration/synthetic-origin sharing are retained, and circular type resolution uses the real push/pop/error path.

The diagnostic batch now retains array/readonly/tuple/rest/labeled output, generic references and declarations, conditional extends/infer precedence, indexed/mapped nodes, library-internal construct signatures, reverse-mapped display stack/declaration depth and ordered infer-parameter cache keys. Source classes/new/constructor syntax remains excluded, but genuine library construct storage is printable. Default diagnostic expansion stays disabled at the actual upstream early return; configured hover expansion is an explicit unsupported API boundary.

A concrete additional internal dependency is `${number}` (`numericStringType`) from homomorphic mapped-array conditional flow. Its actual `TemplateLiteralType` constrained payload, factory-facing fields, relation matching/inference and diagnostic template output are retained. Written template-literal types and intrinsic string transformations remain excluded. This is representation closure, not an admission change.

The pseudochecker now preserves generic signature/type-parameter reuse and the original ordinary-array inferred/error-node fallback, plus its real const-tuple skeleton. Conditional/mapped scope traversal in NodeCopy, binding-pattern library parameter names, and relation diagnostic elaboration are still being completed; syntax-stable files are not asserted source-complete merely because they compile. That central-build-4 checkpoint is historical. Central build 7 has since compiled the combined dependency families; current central-build-8 work includes call inference/fixing and diagnostic scope reuse. No Collections runtime/native acceptance claimed.

Independent review of core inference/instantiation is reserved until compile repair is coherent. It will examine candidate/fixing order, deferred mapper captures, tuple/rest normalization, demanded Awaited/conditional paths and producer guards before final validation. No broad unchanged-foundation audit or full gate is underway.

## Current support handoff and bounded storage adaptations

The real type-parameter, mapped and conditional clone/reuse scopes are now retained in NodeCopy and NodeBuilderScopes. A verifier-requested authored [CollectionsStoredAccess](../../src/TypeScript.Scanner/Checker/CollectionsStoredAccess.cs) statically exposes actual fields without resolution, reflection or invoking stored functions. Every null field retains its declared kind; entity IDs are exposed only as allocation presence for canonicalization. Actual caches/delegates are returned unchanged. Nil Type owners are allowed, nonnil foreign owners rejected. This adapter is verification support, not a semantic API or a direct upstream implementation.

`Core.Same` currently compares whole managed list identity. Current selected producers compare instantiateTypes/union/tuple SameMap results with their original whole lists, inference getCommonSupertype's SameMap result, or iteration Core.Filter output with its original list. No selected producer compares independent shared backing subviews. Empty nil/list comparisons retain Go's length-zero equality. `GoSlices.Delete` is used by the private tuple normalizer's fresh type/element builders and by union/intersection reductions. The latter receive fresh addTypesToUnion or orderedTypes builder lists, not retained constituent headers. No backing view is published before deletion; the returned shortened prefix replaces the private builder slot. These are bounded managed representations, not general Go shared-slice guarantees. TupleElementInfo is a value struct, preserving copied element-flag modifications.

Independent review has started with candidate priority/fixing/cache ordering and mapper captures. The initial contextual-type inference guard is source-fixed by core's actual nonfixing/return mapper branches. Remaining source work includes generic/index diagnostic errors, array error elaboration, real default-library related-info gate (all four explicit roots are false in the actual fixed host), residual library parameter reuse and selected cache/guard review. Review is not complete and no native gate is authorized by this note.

### Central build 9 and next source packet

Central build 9 compiled the concrete fixing/contextual call family, actual Checker-owned factory and selected SyntheticExpression payload/factory with zero warnings/errors. The payload is a handwritten direct supplement to ast_generated.go; the foundation generator deliberately excludes checker-owned Type state. Its genuine child/clone/update/hook boundaries are retained. No semantic runtime/native acceptance claimed.

Support's next stable packet restores array contextual tuple error elaboration, indexed-access/distributed/variance-marker relation diagnostic paths and generic base-constraint singleton recursion. Property-name serialization now consumes actual mapped/instantiated nameType literal and unique-symbol payloads, including generated negative numeric computed names and printer output. The original default-library gate is retained; actual fixed explicit noLib roots all report false, rather than equating library membership with default-library status. Full English diagnostic metadata already contains the new required messages.

Four original grammar boundaries for index-signature parameters, index signatures, for-in/of grammar and unlabeled break/continue have been supplied to core as reviewed scratch; only core integrates that originating counterpart. Source labels/switch/for-await branches remain explicit exclusions, while genuine library index declarations retain full parameter/key/type checks. Source work and independent review remain active.

### Logical symbol-link correction

Verifier's source/state audit found that several earlier symbol link stores used the arena adapter although pinned checker.go declares `core.LinkStore[*ast.Symbol,V]`. That representation could allocate a SymbolId eagerly on Get/TryGet, unlike the original identity-keyed store. Support restored `varianceLinks` to the genuine logical [LinkStore](../../src/TypeScript.Scanner/Core/Linkstore.cs); core restored its selected stores and verifier restored Flow's assignment store in the same coherent compiler batch. No observed mismatch is claimed before the new whole-store protocol runs. The source fidelity correction preserves actual logical key presence and prevents a false allocation side effect.

Central build 11 is green with the selected relation/formatter/grammar packet. Independent review remains active; runtime boot/Legacy/Callable comparisons are pending with verifier, and no comprehensive native gate is underway.

### Loaded-library API boundary and ordered deferred nodes

The complete ES5 file contains `declare namespace Intl` at line 4416. Its direct exports and qualified diagnostic name chains would require additional bounded module-container behavior. The fixed contract does not promise every API merely because its declaration is loaded. Array/ReadonlyArray `toLocaleString()` at lines 1335/1201 has no Intl argument or return dependency; the later Intl-dependent overloads belong to Number/Date. No required collection producer was found. User-selected Intl paths therefore remain explicit unsupported branches; there is no builtin-name admission ban, replacement type, or namespace parity claim. This disposition must be reopened if a required collection diagnostic actually reaches those declarations.

The existing `GoOrderedNodeSet` adapter retains duplicate Add suppression, Node reference identity, insertion order and live-length iteration. The latter is deliberate: pinned OrderedMap.Keys uses an index loop to enumerate newly added items. The only selected production operations on source-file deferredNodes are Add and Values (checker.go:2532/2538). Verifier's stored-state protocol may project ordered Values, membership and size inline, but must not invent raw OrderedMap backing topology or retained record identity. Has/Size now retain the original membership/size operations and are included in the pending central 13 snapshot. Delete/Clear/Clone are not implemented or claimed; the original full TestOrderedSet assertions cannot be represented as a full preserved test through this subset.

### Central 12 leaf packet

The grouped central 12 compile failed on only two source-required leaf boundaries: core-owned `isSomeSymbolAssigned` and Relater-origin `isMemberOfStringMapping`. Support restored the latter's exact ordered Any, String/Template, StringMapping and false arms; the actual intrinsic mapping arm remains explicitly unsupported under the fixed source/API boundary. Existing template-placeholder callers use that original method rather than a separate throwing helper. This packet and ordered-set Has/Size are syntax-stable for central 13; no runtime or final acceptance claim follows.

### Independent producer findings after central 13

Core's union/intersection property constructor used the Prototype symbol flag where upstream calls `isPrototypeProperty`: Method or SyntheticMethod. Array-union and callable-interface method producers are in contract. Core restored the original helper and caller; source-fixed, not yet runtime-verified. Verifier will retain an independent actual-Go targeted case observing the resulting synthetic CheckFlags and declaration identities, not just the inferred map return text.

Support also restored the missing original `isWeakType` Substitution-to-base recursion (relater.go:687). Genuine generic conditional/mapped state now includes SubstitutionType, so the former finite-object helper could no longer omit that arm. No actual mismatch is asserted; this is a source-fidelity repair pending the next grouped snapshot and selected runtime coverage.

### Central 14 and first full reference-cache association mirror

Central 14 is clean (zero warnings/errors, 5.15 s) with both source review fixes and authored [CollectionsCacheAccess](../../src/TypeScript.Scanner/Checker/CollectionsCacheAccess.cs). The new accessor reads actual ObjectType.instantiations without resolution. Nonempty selected reference-target caches require exact self target ownership; each actual value's retained resolvedTypeArguments rebuilds the original getTypeListKey, and the accessor verifies actual key equality, lookup returning that exact value, unique rebuilt keys and full cardinality. Nil/allocated-empty cache state and nil/empty argument lists remain distinct. Unknown nonempty object-cache families fail explicitly pending their own source-derived association scheme. The accessor exposes actual entity references for independent canonicalization, never late-registers them or assigns IDs, and does not call getTypeArguments or invoke mappers.

The union method predicate and weak substitution recursion are compiler-verified in this snapshot, not yet asserted exact by a targeted runtime result. The verifier owns independent original Go association reconstruction and tests; no library build or expected fixture change is performed by support. Full selected guard/formatter review and systematic runtime verification remain active.


### Central 16: actual logical owners and fixed Program policy

The authored [VerificationKeys](../../src/TypeScript.Scanner/Core/Linkstore.cs) returns a nullable snapshot of actual logical-store keys without Get, resolution or ID allocation. The verifier's 17-store inventory checks complete cardinality and requires every owner already rooted; it must reject unrooted owners rather than register them afterward. This is observation instrumentation, not a new upstream public collection API or arena-topology parity claim.

The remaining [symbolContainerLinks](../../src/TypeScript.Scanner/Checker/SymbolAccessibility.cs) arena adaptation is corrected to the original logical LinkStore (checker.go:700), matching the earlier variance and other logical-symbol stores. This avoids incidental lazy Symbol ID allocation and preserves true absent entries. No old runtime discrepancy is claimed; central 16 verifies compilation, while full stored-key observations remain verifier-owned.

Independent review found no material issue in the selected fixed [Program diagnostic composition](../../src/TypeScript.Scanner/Compiler/Program.cs) or the formatted [CompareTypes](../../src/TypeScript.Scanner/Checker/Utilities.cs) tuple/mapped/reverse/conditional/index/substitution/template branches. Both retain actual upstream boundaries. These are source-review dispositions, not general Program API or all-type comparison acceptance. The first rich scalar protocol matches after sorting raw flattened field descriptors before entity registration on both runtimes; unknown nonempty cache/function families still fail explicitly.


### Central 17 pending: generic diagnostics, reduced cache keys and direct support assertions

Independent review restored the missing original post-error source-TypeParameter constraint suggestion in [Relater](../../src/TypeScript.Scanner/Checker/Relater.cs), relater.go:4777: clone the parameter, instantiate the proposed constraint, check its actual circularity and append related diagnostic 2208. Verifier is retaining a natural generic assignment failure witness. This is source-fixed; compilation/runtime are pending the central 17 packet.

The [cache accessor](../../src/TypeScript.Scanner/Checker/CollectionsCacheAccess.cs) now also validates the previously reviewed anonymous object family from its stored outer parameters and Simple/Array (or SingleSignature Composite.m1) mapping. Alias and ConditionalRoot cache keys can lead to reduced values that lose argument provenance. Their new managed decoder reads the exact full preimage, requires already-known actual ID-to-entity ownership, preserves ordered key arguments/alias symbol/alias argument sequence/constraint tag, and validates complete exact membership. It does not invoke a mapper, assign IDs, fabricate alias identity, or claim recovery of input nil-versus-empty headers erased by the original key. Unknown nonempty families or IDs fail. The independent Go producer must retain metadata at the exact original key sites; its integration and source review remain pending.

The nine original collection test functions are now individually classified in [collections-tests](../collections-tests.json). Only TestOrderedSet has a selected adapter subset: original Add(1,2,3) plus three Has assertions, with distinct Node identities; Delete/Clear/Clone and post-deletion assertions remain explicitly omitted. No full original OrderedMap/SyncMap API is provided or claimed. [CollectionSupportChecks](../../validation/Collections.Validation/CollectionSupportChecks.cs) supplies eight shared, clearly labelled groups through the [thin xUnit wrapper](../../tests/Checker.Tests/CollectionsSupportTests.cs), including actual ordered-node order/duplicate/live insertion and list nil/identity/captured-length/delete-tail controls. Original-Go supplementary projection, managed/native execution and final statuses are pending. No later Go backing write-through is claimed by the managed prefix-copy representation.

A separate constructor producer challenge identified the old exactly-one-construct-signature guard in core. Ordinary genuine ArrayConstructor can occur in inferred generic object intersections without source new/class syntax. Core restored actual isMixinConstructorType and isConstructorType constraint arms; verifier is examining the natural witness. The build 16 loop/evolving-array flag mismatch is also still under investigation: the base helper itself retains upstream mutation/gating, so no flag masking or changed expectation is authorized.

### Central 18 compiler repair and actual-Go support provenance

The central 17 accessor packet failed only on its missing Symbol namespace import; central 18 exposed one static diagnostic call qualification. Both support translations are repaired and syntax-stable for the next grouped compile. No runtime verdict is inferred from those repairs.

Independent actual pinned Go supplementary [support operations](../../tools/collections-oracle/support_projections.go) now execute with [saved output](../../artifacts/collections-validation/support-projections/actual-go.json) and [SHA provenance](../../artifacts/collections-validation/support-projections/provenance.json). They match all requested operation expectations used by the eight shared bodies. The adapted original OrderedSet prefix remains three assertions; no general collection API or private topology claim is added. Managed/native execution is pending.

The loop/evolving-array flag producer is source-fixed: core restored the original convertAutoToAny autoArrayType-to-anyArrayType arm. This changes the proper resulting array identity, not the observed bitset or expectation. The original build 16 witness must confirm the source correction at runtime.

### Shared control milestone and reduced-state closure

The eight support bodies now pass in the isolated build 19 xUnit/CLI registry; combined with three verifier controls, the registry contains 11 groups. [collections-tests](../collections-tests.json) records managed execution and independent Go projection evidence while native remains pending. The original OrderedSet prefix continues to be labelled adapted.

Mapped instantiation can reduce to an array, union, primitive or other value whose mapper no longer identifies the original arguments. The managed [cache reader](../../src/TypeScript.Scanner/Checker/CollectionsCacheAccess.cs) therefore exposes complete preimage key sequences for mapped and deferred-reference owners, alongside actual value identity, alias symbol/argument sequence, SingleSignature tag and self association. No surviving Alias pointer, nil input header or result target is invented. Verifier retains the independent original producer metadata.

The sole deferred inference-target closure now has an explicit managed representation in [Mapper](../../src/TypeScript.Scanner/Checker/Mapper.cs), retaining exactly checker, parent node, ordered type parameters and index. Core integrates its factory at the original construction site. Its callback remains deferred; the observer reads exact captures without invoking targets or adding semantic registries. This is an authored closure representation, not an upstream model-field addition. Compilation and runtime confirmation are pending.

### Central 20 and finite retained-capture accounting

Central 20 compiled the mapped/deferred-reference full-preimage reader and explicit deferred-target factory with zero warnings/errors (5.18 s). Support now adds finite comparer projections, tuple-target global interning associations and complete cached-signature associations for the next grouped snapshot. The earlier fixed-key callable signature reader is not treated as sufficient for generic argument-keyed entries.

The signature-related comparer retains exactly the relater and intersection state captured by upstream relater.go:4587. Method-bound checker/relater comparers are validated against their exact known method and owning checker. The projection preserves actual current relation category, including nil after pooling; it does not substitute historical construction state. Relater owner identity is retained, while unrelated transient scratch and private pool topology are explicitly outside the projection. Completed relation/flow caches still need bounded selected association coverage; an empty work stack is not a substitute.

Core's rich alias-wrapper finding is independently source-confirmed: instantiateTypeAlias must always create a fresh nonnil TypeAlias, even if its argument list is unchanged. The exact repair awaits grouped compilation/runtime confirmation.

### Whole completed relation associations (implementation pending compile)

The managed [relation reader](../../src/TypeScript.Scanner/Checker/CollectionsCacheAccess.cs) covers all five named completed caches, retaining actual nil/empty state, complete entry counts and numeric result flags. It decodes the complete stored preimage, checks known allocated type ownership, reconstructs the original byte sequence and validates exact membership and unique full cardinality. Simple keys retain actual directional types; identity keys validate their normalized stored order. Generalized reference keys retain actual targets and ordered nested terms, with unconstrained parameters represented only by the source-normalized first-occurrence index. It does not fabricate the original parameter or reference-wrapper identities erased by that key, call getRelationKey/getTypeArguments/constraint getters, or force type IDs.

Core owns independent Go producer capture at the original relation key return sites; verifier owns graph integration and the separate Flow cache associations. Their execution and overlay semantic parity remain pending. Released recursive work stacks and current pooled comparer state remain distinct from completed relation results.

### Central 21 compile boundary

Central 21 is clean (zero warnings/errors, 4.41 s). The whole retained relation, tuple and generic-signature readers, finite comparer owner accessor and core fresh TypeAlias wrapper correction compile together. Verifier is refreshing the isolated rich snapshot. Compilation does not establish association parity or final acceptance; independent Go capture/consumer integration and selected runtime evidence remain pending.

### Central 22 and concrete fresh-object cache provenance

Central 22 is clean (zero warnings/errors, 6.83 s), including whole reverse-mapped cache association readers and the verifier's finite fresh-object registry. Independent review confirms the two core capture calls follow original fresh flags/regular cache publication and reuse already-computed node, mode, contextual type and source association. These labels distinguish semantically different fresh types from the same syntax without using ordinals, lazy IDs or extra context queries. The earlier thirteen-site original overlay matched all 23 behavior streams; the two new object hooks need refreshed semantic parity. No new semantic compiler correction is inferred from observer metadata. Rich associated-state comparisons and the final frozen native gate remain pending.

### Central 23 and whole global/flow associations

Central 23 is clean (zero warnings/errors, 5.17 s). The new [global reader](../../src/TypeScript.Scanner/Checker/CollectionsGlobalAccess.cs) compiles with the verifier's whole retained Flow associations. It observes cached-types, indexed-access, substitution and template-literal maps, preserving actual nullable values, complete counts and exact existing key/value membership. Indexed keys retain full access flags and alias symbol/argument sequences; the payload independently preserves its Persistent flags and real Alias object. No erased alias wrapper or input slice header is invented. Cached decorator-context kinds 18–21 remain explicit unsupported producers; selected kinds 0–17 are handled.

Independent review of the [Go global counterpart](../../tools/collections-oracle/global_access.go.inc) and its [two-site overlay](../../tools/collections-oracle/instrument_global_caches.py) found no remaining source/schema issue. Captures follow original stores, use computed keys/inputs, and perform no semantic getter, mapper or ID allocation. Final full-overlay behavior proof must include these new sites plus object and Flow hooks. Rich association discovery and native acceptance remain pending.

### Passive key-owner census after central 23

Reduced cached values can erase the owner needed to validate a retained key. The authored [direct-slot census](../../src/TypeScript.Scanner/Checker/CollectionsGlobalAccess.cs) therefore returns named actual current Type/Signature field values for lookup only: 75 managed Type fields and four sentinel Signatures. It neither resolves globals nor registers graph entities. A missing ephemeral/nonfield owner still fails and requires genuine original producer provenance.

This is not a Checker field-parity assertion. The original Checker has 78 direct Type/Signature fields; 72 names overlap the managed census. Seven additional managed slots hold existing memoized global results (BigInt, ESSymbol, Promise and four Iterable variants), represented by closures upstream. Six original slots are not retained managed fields: varianceTypeParameter, templateConstraintType, globalRegExpType, two JSX empties and deferredGlobalImportMetaExpressionType. No missing original slot is padded with a fake null. Exact named-slot scope and this representation distinction remain separate from observed semantic cache associations.

### Erased Widened input and iteration-cache completion pending central 24

Concrete original discovery now succeeds in 20 of 23 rich cases; the remaining three unknown owners are Widened cached-type keys (kind 13). The original sole context-free widening publication retains only a reduced result, so a small Collections-only metadata helper now captures its already-computed source/result after original publication. The reader validates source identity and current key/value membership; conflicting metadata fails during observation rather than changing checking. Passive census exposes the actual captured source only. Core owns the exact Checker and independent Go insertion sites.

The managed [global reader](../../src/TypeScript.Scanner/Checker/CollectionsGlobalAccess.cs) also now exposes complete iterationTypesCache owner/use/yield/return/next associations, including actual all-null negative results. Keys use the original CacheFlags mask, exact stored membership and full cardinality; no iterator or missing-global substitute is manufactured. This source packet is formatted and stable for grouped central 24, with compilation/runtime confirmation pending.

### Central 25 relation inputs and automatic-array source correction

Central 25 is clean (zero warnings/errors, 5.23 s). The original completed relation hash now captures its actual normalized source/target pair for passive key-owner lookup only; generalized keys still do not claim erased parameter/reference-wrapper identity. Four missing managed relation terms were concrete observer census gaps; capture uses no key recomputation, getter or ID assignment.

The rich loop witness independently exposed the old scalar-only automatic-type adaptation in checkIdentifier: its initial key term was a union where original Go used undefined. Core restored all five automatic-array distinctions. Narrow upstream use-site review found no further in-contract omission; two unported class constructor/static-block Flow producers remain source-excluded. Paired rich execution and final native validation remain pending.
