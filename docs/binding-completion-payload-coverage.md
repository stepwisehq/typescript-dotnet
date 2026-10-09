# Binding payload coverage limits

This is a documentation-only reconciliation of the [178-field source inventory](./binding-completion-support-inventory.json)
with saved original-Go observations. Source completeness, payload occurrence and observed
field states are separate claims. The broad managed report is initial triage, not final
acceptance. The final [native proof](../validation/results/binding-completion-verified.json)
now succeeds on the frozen corrected observer and confirms the same 81/172 occurrence
counts. The finite state-coverage limits below remain unchanged.

The 85 generated payload classes contain 178 flattened binding fields. Broad plus targeted
cold graphs contain 81 of those classes, accounting for 172 statically listed field cells.
Every observed instance's ordered fields and references are compared by the independent
reflection oracle and static managed serializer. This does not establish both null and
non-null coverage for each of the 172 cells. The broad report aggregates null/reference
counts by role/type, not by concrete payload; those aggregate counts must not be interpreted
as payload-specific state coverage. Saved targeted expectations observe 59 concrete cells
on 29 generated classes. SourceFile and the two flow-snapshot payloads originate in other
counterparts and are additional to these generated-class counts.

| Absent from cold broad/targeted graph | Fields | Disposition |
| --- | --- | --- |
| NotEmittedStatement | FlowNode | Factory-created transform marker; no parser factory caller. Arbitrary transform/synthetic input is outside fresh parsed source binding. |
| NotEmittedTypeElement | Symbol | Factory-created transform marker; no parser factory caller. The retained model is not claimed as naturally produced binding coverage. |
| JSDocSignature | Symbol, Locals, NextContainer | Original attached JSDoc syntax is omitted by the cold observer and not traversed by binder. Historical warm-JSDoc binder expectations contain two instances with all three cells null. |
| JSDocTypeLiteral | Symbol | Original attached JSDoc syntax is omitted by the cold observer and not traversed by binder. No saved bound-cell witness identified; foundation syntax occurrence is not a binding-state witness. |

The JSDoc rows remain implemented parseable syntax, rather than excluded compiler machinery.
[Binder.bindEachChild](../upstream/TypeScript/tsc/internal/binder/binder.go:1744) uses ForEachChild;
the pinned binder has no JSDoc/EagerJSDoc getter call. Original attached documents are not
ordinary children. [The reparser](../upstream/TypeScript/tsc/internal/parser/reparser.go:22)
explicitly states that binding attaches symbols to reparsed nodes rather than original
JSDoc. Callback/overload signatures become ordinary function/method/constructor/type nodes
in [reparseJSDocSignature](../upstream/TypeScript/tsc/internal/parser/reparser.go:146);
property-tag literals become TypeLiteralNode/property signatures in
[reparseJSDocTypeLiteral](../upstream/TypeScript/tsc/internal/parser/reparser.go:244).
Those binder-facing classes are present in cold broad comparisons. A post-bind lazy JSDoc
syntax observation does not retroactively bind original documentation nodes.

Remaining state limits are legitimate finite-evidence limits, not exclusions of source
paths: not every payload/field combination can be classified as reachable from aggregate
counts, and the campaign does not claim exhaustive branch or state-space execution. The
complete source-field inventory and independent binder body/producer review support the
implementation boundary. Naturally allocated-empty tables and shared linked tails have
actual source witnesses; distinct table-owner aliasing has an independently challenged
fresh-producer absence proof and separate seeded observer controls.

Independent recommendation: no missing fresh-source binder implementation path was found
by this payload challenge. Its conditions—corrected strict generated-name observation
and a complete native/regression gate—are now satisfied. The coverage limits above
remain visible in the final assessment.
