# Independent data-type core review

Independent selected-path review closed; bounded campaign accepted on Linux x64.
[Final evidence](../../validation/results/data-types-verified.json) records the
frozen Native AOT gate. No active review or implementation work remains. Pinned upstream:
`fed0bf24149fb1ed36039212648bafdafc1ea10e`.
Reviewed the stable D1/D2 sections of [Checker](../../src/TypeScript.Scanner/Checker/Checker.cs)
against [the selected contract](../DATA-TYPES-CONTRACT.md) and upstream
`tsc/internal/checker/checker.go`. No core source edits or library builds by reviewer.
Review includes D1–D6 stable named, construction, member, reduction, relation/context and flow paths; any future executable edits require a new scoped review and verification.

## Findings routed to core owner

1. **Reachable old-scope guard:** `isConstTypeVariable` rejects ordinary Reference
payloads and all TypeParameters. Upstream checker.go:13916 returns false for an
ordinary reference; its TypeParameter arm checks actual declaration Const modifiers.
An alias-based interface can carry a genuine hidden-this reference into contextual
object initialization. The source generic exclusion does not justify rejecting it.
Owner restored the original selected branches; central build11 is clean, the corresponding managed campaign rows now match in build18.

2. **Invalid supported source rejected:** `reportNonexistentProperty` rejects every
Intersection when no spelling suggestion exists. Upstream checker.go:11767 calls
`elaborateNeverIntersection` and emits the normal missing-property diagnostic.
Example: `type Both={x:number}&{y:string};let v:Both={x:1,y:""};const z=v.missing;`.
This is an in-contract failure, so an unsupported exception is incorrect. Owner
restored elaborateNeverIntersection and the normal diagnostic arm for build11;
central build11 is clean, the corresponding managed campaign rows now match in build18. The intersection-failure campaign rows include the actual missing-property path.

3. **Publication-order fidelity:** `resolveDeclaredMembers` sets its resolved flag
before `getMembersOfSymbol`, whereas upstream checker.go:20016 computes members
first, then publishes flag/table. No observed mismatch asserted; restore the
original order to preserve reentrant resolution semantics. Owner restored it for
build11 (clean); no observed mismatch claimed.

4. **Diagnostic caller flags:** `reportCircularBaseType` uses default type-format
flags, whereas upstream checker.go:19952 explicitly uses WriteArrayAsGenericType.
Arrays are outside admission, so no observed mismatch asserted. Restore the actual
caller flags if bounded; do not silently describe the default wrapper as exact.
Support added the selected WriteArrayAsGenericType-only wrapper and genuine
NodeBuilder flags propagation; core caller is wired and build11 clean. The circular-base managed rows now match in build18.

## Retained branches checked

Alias declared-type resolution retains the real resolution stack, circular alias
2456 diagnostic, delayed publication and nullable alias caches. Type references
preserve unresolved-symbol identity/error-type alias keys, ordinary name meanings
and unknown/value-as-type diagnostics. Source parameters/arguments remain explicit
guards. Fixed single-script initialization uses the actual binder symbols; binder
handles same-file declaration merging before checker global-table publication.

Interface declaration allocation publishes its identity before the genuine
isThislessInterface base walk. Alias heritage therefore retains the synthetic
thisType, self target, instantiation table and hidden argument. Declared/base member
resolution preserves inherited-order lookup, copy-before-inheritance tables,
this-argument instantiation and real synthetic any-base index information. Base
cycles, invalid scalar/union heritage and incompatible inherited/redeclared members
retain upstream checks rather than a second structural validator. Merge checks run
once per checker and report on the first interface declaration.

Omitted class, callable/index syntax, source generic and module branches are guarded
outside admission; hidden-this/reference/mapper branches are not treated as excluded.
Qualified-name exported-member failures were also initially guarded. Core restored
the actual namespace/value lookup/suggestion diagnostics and internal globalThis
module export cache; support restored DoNotIncludeSymbolChain propagation and
actual ModuleSymbolLinks storage. The qualified globalThis missing-member row now matches in build18, including its populated export-cache observation.

Missing-global validation still consults source builtin declarations rather than
assuming a fixed count. Owned/core formatting and final source-snapshot consistency are checked before freeze; broader source syntax remains outside this review.

A support self-review also corrected the inaccessible-symbol result arm to pass
the actual enclosingDeclaration/meaning into symbolToStringEx, and to retain the
Go empty ErrorModuleName rather than null. No observed output discrepancy asserted;
this owned fix compiled and its campaign rows match in build18.

## D3–D6 review delta

Current stable core snapshot retains named/composite constructor and member paths. Build18 managed comparison is integration evidence; the final Native AOT gate now confirms those fixes on the frozen snapshot.

5. **Selected instantiation budget/context boundaries omitted:** checkSourceElement
assigned currentNode without restoring it and both that helper/checkExpressionEx
omitted upstream instantiationCount=0 resets (checker.go:2285/7759). With real hidden
instantiation selected, this makes a budget cumulative across unrelated nodes and
can give the wrong overflow diagnostic context. Owner restored original
save/reset/check/restore boundaries for clean build18; all 92 managed campaign rows match. No threshold-output discrepancy claimed.

6. **Deferred property write-call fidelity:** createUnionOrIntersectionProperty
omitted original getWriteTypeOfSymbol and conditional writeTypes prefix collection,
although actual deferred writeConstituents storage is now selected. Ordinary data
read/write types usually coincide; no discrepancy claimed. Owner restored the
original call, prefix clone/append and deferred/write-type branches by clean build15.

7. **Owned formatter omission and follow-up correction:** the earlier object-scope
unresolvedType guard rejected eight actual private TypeAliasLinks graph types.
Support restored the real synthesized leading comment and per-call printer option.
The first patch emitted it unconditionally at type dispatch; original printer.go:742
also requires a current source file. Verifier found eight text differences. Support
then restored that actual guard at keyword/token comment dispatch, retaining the
attached comment but producing `any` for nil-context private-cache formatting.
The follow-up is verified by build18: all eight formerly differing raw-cache type texts match. No observer/expected changes.

8. **Owned diagnostic flags:** getTypeNameForErrorDisplay and
typeToStringNoTypeReduction retained default alias flags in addition to the explicit
upstream flags (relater.go:1291/4769). Support restored the actual
UseFullyQualifiedType-only and NoTypeReduction-only calls through the existing
flags override; syntax/LF formatting passed. No observed output difference claimed;
clean build18 and the 92 managed campaign comparisons include these exact calls.

Reviewed retained union/intersection properties use actual allocated caches, ordered
constituent/declaration identities, readonly/optional propagation and deferred >2
member normalization. Intersection reduction uses original ordered repeated-property
counts and never/discriminant checks, not blanket incompatible-type rejection.
Construction keeps original key/alias/origin order, scalar/disjoint reductions,
Cartesian limits and nullable-union handling. Instantiation uses genuine mapper/cache
stacks and identity-preserving SameMap/list operations; source written parameters,
callables and mapped operators remain guards. Contextual stack lookup and optional
member discrimination retain the actual selected branches. The former blanket
Instantiable contextual guard is restored to the genuine inferenceContextInfos scan:
an absent inference context returns the input contextual type; nonnil callable/
source-generic contexts remain explicitly unsupported. Partial original context
models are in their original Checker counterpart, not fabricated successful inference.

Finite named best-overlap diagnostics can create IndexType origins through
getLiteralTypeFromProperties. Such types can be retained only in the actual
propertiesTypes private cache, outside current query/member graph roots. Root/verifier
received a focused nondiscriminated interface-union mismatch specimen. Its actual-Go
run produces two 2322 diagnostics and retains two genuine IndexType origins in the
existing propertiesTypes cache. The verifier now observes existing cache associations
without calling getIndexType or creating edges. No written keyof expansion.

Declaration/start/shared-flow traversal, initializer cache lookup and assignment-union
reduction were compared with flow.go. Straight-line declaration antecedents are
iterative; recursive branch/call/reduce-label depth cases are excluded. Per-query
shared-flow append/truncate and boolean freshness preservation retain source order.
Source alias/interface/type syntax is skipped by assignment marking as upstream.
No new selected-path flow mismatch was identified.

No unresolved selected-path blocker remains in this review. All known findings are
source-corrected and build18 managed comparison covers 92 rows; depth/budget limits
and omitted generic/callable paths are not claimed as exercised. Independent
formatter/model/relations review is owned by the core agent. The final gate confirms 92 native data-type cases, 43 anonymous-object and 45 scalar
regressions, all 2,146 xUnit tests and the earlier native gates. Exact source/assets
are recorded in the linked final evidence; historical acceptance remains separate. No core source
edits or library builds by reviewer; historical acceptance is preserved separately.
