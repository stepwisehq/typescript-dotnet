# Independent object checker review

Scope: read-only comparison of selected [Checker](../../src/TypeScript.Scanner/Checker/Checker.cs),
[Flow](../../src/TypeScript.Scanner/Checker/Flow.cs),
[GrammarChecks](../../src/TypeScript.Scanner/Checker/GrammarChecks.cs) and authored
[Admission](../../src/TypeScript.Scanner/Checker/Admission.cs) against pinned upstream
`fed0bf24149fb1ed36039212648bafdafc1ea10e`. Scanner owner reviews support-owned
relations/formatter separately; this is not another correctness gate or benchmark.

## Concrete findings

1. **Reachable omitted traversal:** Flow.markNodeAssignmentsWorker retained its
scalar-only allowlist. Checking `let x:number; let obj={a:1}; let y=x;` enters
isSymbolAssignedDefinitely and visits the object sibling; it threw instead of
following upstream assignment-target inspection and ordinary child traversal.
**Resolved:** owner restored selected traversal and actual AssignmentKind
inspection. Reviewer2454 object-sibling fixture matches actual Go in the43/43
managed matrix on build9.

2. **Go nil range:** Checker.checkObjectTypeForDuplicateDeclarations and
reportDuplicateMemberErrors iterated nullable Node.Members without nil handling.
`let x:{}={};` reaches the first loop. Verifier already independently observed the
failure in its empty-table tiny case; owner restored nil handling. The follow-on
empty-type synthetic symbol name was also corrected to actual InternalSymbolNameType.
**Resolved:** empty-table tiny comparison and full43-case managed matrix pass build9.

3. **Missing parameter propagation:** checkObjectLiteral called
checkShorthandPropertyAssignment without the upstream checkMode argument, and
that helper used Normal when checking its expression. Contextual elaboration is
selected, so retaining the real parameter/caller boundary avoids assuming modes
are interchangeable. Owner restored parameter propagation before build8; no
observed output discrepancy claimed. **Resolved:** restored helper/caller compiles
in build9 and all43 managed cases match.

## Reviewed retained boundaries

Contextual stack lookup preserves upstream forward order and cache visibility;
static-property contextual lookup maps actual constituents and removes missing
property types through the real fixed-option helper. Optional internal unions use
actual discriminator/property caches, not a custom object filter. Nil/allocated
member tables and late-binding cache access follow upstream combine-table empty
branches; transient property symbols retain declaration/parent/value/target edges.
Fresh/regular and widening caches preserve their original selected keys and lookup
branches. Deferred node order and callback snapshot iteration mirror Go range;
there is no fake successful deferred hook.

Finite-key intersection support follows actual primitive-union reductions and
cross-product enumeration, guards retained structured intersection payloads, and
retains the original overflow limit check. Its managed key stores the selected
complete preimage; opaque Go hash bits are not claimed identical. Property reads
retain unknown/null diagnostics and declaration/start-flow analysis; explicit
writes/branching/constructor flows remain guarded outside admission.

Admission rejects unsupported syntax/environment before checking, including
external references/directives, modules, aliases/interfaces/source unions,
methods/indexes/computed keys/spreads/calls/optional chains/writes. It admits
shorthand, optional/readonly static data signatures and nested contextual records.
The new pseudochecker path likewise explicitly rejects signatures/tuples/JS/entity
references/dynamic syntax; its real static-data inference/reuse branches are under
scanner's independent review. No general emit/type-reference support is implied.

Current status: all three findings are resolved by owner fixes and build9
43/43 managed comparisons; the final [Native AOT evidence](../../validation/results/object-checker-verified.json)
now passes all 43 object cases, 45 scalar cases and full earlier regressions. No source edits
or library builds performed during this review. Historical scalar evidence is
unchanged. Final acceptance requires the differential corpus and Native AOT gate.

Formatter dependency review also corrected an earlier support assumption: cyclic
structure reporting sets actual reportedDiagnostic even with nil external tracker.
Support restored that field/callback and the original cache-write conjunction.
Verifier independently found Unicode generated property names need ASCII escaping
while scalar literal nodes carry NoAsciiEscaping. A narrow original/flag EmitContext
restores that distinction and copying across original/clone/cache edges; validation
passed in the owner-built43/43 managed matrix. These are implementation defects, not a change
to the upstream behavior or admission boundary.

Owner's independent support review additionally required two actual boundaries:
serializeTypeForDeclaration now applies widened-literal selection on an enclosing
symbol-type cache miss; call/construct elaboration now invokes getSignaturesOfType
for each original SignatureKind, preserving union MembersResolved cache effects.
The earlier recursive absent-signature guard skipped that genuine resolution call.
Both fixes and pinned diagnostic additions passed build9 and43/43 managed
comparisons. Support and algorithm executable source/tools/corpus are frozen;
only documentation/source mapping changed afterward. The final guarded gate passes;
all review findings are resolved and no active implementation work remains.

Final disposition: accepted bounded checkpoint only. Native source fingerprint
`1ce0c30edf2f470ee4a7c527f336980e5a80d8afc17108774c8764d99d6c15b9`
was rechecked read-only after final mapping. No executable/tool/corpus edits occurred
after freeze, no extra builds/experiments were run by this reviewer.
