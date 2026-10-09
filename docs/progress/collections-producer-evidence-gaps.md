# Collection producer evidence gaps

Status: closed within the bounded [contract](../COLLECTIONS-CONTRACT.md). The
[recorded final proof](../../validation/results/collections-verified.json) reports
44/44 exact native cases, full original-overlay parity, 12 shared checks, 18 exact
order replies per runtime and all 23,467 xUnit cases passing without skips or drift.
The seven source-review requests below are historical: raised after central 16,
then covered by actual original-Go/managed specimens and the final native corpus. The corrected ten-case
[closure packet](../../artifacts/collections-validation/build20/closure-nested-shadow/report.json)
passed 10/10 at build 20 (3.024s); its request SHA matches the current
[closure requests](../../validation/collections-corpus/closure-requests.jsonl).
The [targeted packet](../../artifacts/collections-validation/build19/targeted/report.json)
passed 18/18 at build 19, and the whole-cache
[rich packet](../../artifacts/collections-validation/build25/rich-global22/report.json)
passed 22/22 at build 25 (6.389s). These are managed results, not native acceptance.
The [contract](../COLLECTIONS-CONTRACT.md) remains unchanged.
The existing initial/member/environment/targeted packets already cover defaults,
constraints and cycles, simple recursive references, generic overload success, all
selected Array APIs, reverse Readonly/Partial inference, tuple/rest spreads, utilities,
and loops. Do not duplicate those cases or infer rich cache coverage from their text
and diagnostic comparisons.

These are the distinct producer interactions identified by that historical review.
Each has a corresponding named specimen in the closure packet: merged/local
inheritance, symbol/number indices, distinct recursive relations, failed overload
retry, NoInfer/callback/default inference, constraint suggestion and Array-constructor
intersections. The constraint-suggestion output retains diagnostic 2322 with related
2208. The packet also covers array-rest aliases/infer, correctly nested local alias
shadowing and intersection-size failure. Matching those specimens does not establish
every branch or the final whole-cache corpus. Verification owns query selection,
independent expectations and the final canonical manifest.

| Historical review interaction | Evidence gap before the closure packet | Required observation |
| --- | --- | --- |
| Merged/local generic interfaces and inherited members | Global Array augmentation and same-target Link recursion do not establish local outer parameters, merged generic defaults/constraints, or instantiated inherited methods. | Ordered merged declarations; default/constraint diagnostics; reference target/arguments, hidden-this and actual member/signature mapper/cache associations. |
| Numeric and symbol index keys | The current dictionary case establishes only string indexing and finite string-property keyof. | String-index keyof includes number; independent number/symbol indices; union/intersection keyof; indexed writes/read types, wrong-key diagnostics and propertiesTypes origin/key associations. |
| Recursive relations across different generic targets | Link<number> versus Link<string> shares one target; it does not establish recursive structural comparison or variance/relation cache reuse between distinct declarations. | Positive and negative recursive assignments, callback parameter contravariance, relation identity and repeat/order stability. |
| Failed overload inference followed by successful calls | Existing generic choose overloads succeed; invalid demo constraints do not force callback skip/retry and failed generic candidate selection across overloads. | Exact diagnostics, selected candidate/error signature, parameter/return identities, fixed/nonfixing inference mapper state and repeat query results after failure. |
| NoInfer and callback input/return inference | Successful map and reverse inference do not establish excluded candidates, contravariant candidates, contextual return priority or default-on-no-candidate behavior. | Genuine substitution/base/constraint state, candidate/inferred-type records when retained, successful callback result and invalid callback diagnostics; no fake any/nil context fallback. |
| Generic constraint suggestion | The newly restored relater source-TypeParameter error arm is not established by concrete-value constraint failures. | Related diagnostic 2208 when the original predicate succeeds, including its span/order and formatting; no unconditional suggestion. |
| ES5 constructor values in generic intersections | Source new/construct signatures remain excluded, but real Array has construct signatures and can enter constructor/mixin classification through an ordinary generic intersection result. | Ordinary call result, exact construct/call signatures and target/mapper identity; actual predicate result rather than a source-new guard. |

The evolving-array loop case previously had a concrete extra
IdenticalBaseTypeCalculated flag. Its original setter is source-faithful; the earlier
normalization/relation producer needed identification, rather than flag normalization.
Support found the omitted convertAutoToAny automatic-array conversion; the original
autoArrayType-to-anyArrayType arm is restored; central 19 now matches all 18 targeted
actual-Go results, including that earlier identity/flag mismatch.

Opaque cache evidence was a separate obligation during implementation; the final
recorded corpus and whole-cache observations close the selected families. Build 25's 22 exact
rich cases close four managed relation-key owner lookups and the automatic-array
loop initial-state mismatch. Build 26 compiles the scoped applicability provenance
needed to distinguish identical fresh literals from candidate testing versus
diagnostic rechecking in the remaining original discovery case, plus whole property
type associations and normal/star intern membership checks. Its
[rich comparison](../../artifacts/collections-validation/build26/rich-current-managed/report.json)
is 23/23 exact and [overlay behavior](../../artifacts/collections-validation/build26/metadata-parity-current/report.json)
is 23/23 byte-exact. Expanded canonical discovery required passive lookup of the
already-captured regular-object Source edge and a real predicate-diagnostic visibility
arm; build 28 compiles these corrections. The temporary Awaited classification was
wrong (kind 14 is RegularObjectLiteral, Awaited is kind 5), and its unnecessary authored
metadata was removed. The refreshed [canonical original](../../artifacts/collections-validation/build30/canonical-original/report.json)
succeeds 44/44 and [complete-overlay parity](../../artifacts/collections-validation/build30/metadata-parity-canonical/report.json)
is 44/44 byte-exact. The finite choose-overload relation frame distinguishes the
actual subtype/assignable retry producers without inventing candidate identity.
Managed build 29 was 43/44: the invalid filter case exposed a missing original
parameter-name fast path in NodeCopy, causing an extra accessibility cache entry.
The exact short-circuit is restored and independently reviewed in clean build 30.
The [paired build-30 comparison](../../artifacts/collections-validation/build30/canonical-managed/report.json)
is 44/44 exact (12.560s), including the corrected filter producer. Order controls and Native AOT acceptance subsequently passed in the recorded
comprehensive gate. No evidence request below remains an active campaign blocker. Continue to root only actual stored
owners/values, preserve nil versus empty and full key preimages, check every association
and backing cardinality, and fail on unknown producer families. Text-only success is
not acceptance of inference, conditional, alias, anonymous or reference cache identity.

Source anchors: checker.go checkTypeParameterListsIdentical (4489), chooseOverload
(9246), inferTypeArguments (9615), isConstructorType/isMixinConstructorType
(17400/17413), resolveBaseTypesOfInterface (19902), getIndexTypeEx (27218) and
getLiteralTypeFromProperties (27254); inference.go inferFromContravariantTypes
(319), inferToMappedType (959), inferFromIntraExpressionSites (1313); relater.go
recursiveTypeRelatedTo (3094) and reportErrorResults generic constraint suggestion
(4777). These are source-review citations, not a claim that each branch is exercised.
