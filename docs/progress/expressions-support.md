# Expression and flow support checkpoint

Final qualification: the corrected frozen gate passed, and the
[durable proof](../../validation/results/expressions-verified.json) records bounded
E1–E6 acceptance. Earlier pending statuses below describe source-packet checkpoints.

Baseline `5efa4e5`; pinned upstream `fed0bf24149fb1ed36039212648bafdafc1ea10e`.
Support owns the existing [Flow counterpart](../../src/TypeScript.Scanner/Checker/Flow.cs),
GrammarChecks, Relater, Inference and diagnostic-display dependencies. Core is the
sole live Checker/Types/Links writer and central builder. Source-function-bounded
Checker patches are handed to core; no extra production fragments or admission
framework are planned.

## Source-derived boundary

Complete the pinned flow unit's synchronous TypeScript responsibilities: assignment
and default/destructuring transfers; branch/loop/evolving-array and capture analysis;
optional-chain/equality, property-presence/private-in/instanceof narrowing; predicate
and assertion effects; discriminant/typeof/true switch narrowing, exhaustiveness and
reachability; constant-reference tracking and actual binding property names. Reuse
accepted declaration, iterator type-model, caches and flow reentrancy algorithms.
JS expando/JSDoc-specific operations, for-await/async/generator execution and disposal
remain distinct external consumers. Existing synchronous functions must preserve
their source conditions even when an external caller never invokes a particular arm.

Initial current-source comparison finds 29 absent flow helper names and 17 explicit
unsupported branches. These are triage locations, not an implementation percentage.
Absent families are switch narrowing/exhaustiveness/witness caches, instanceof and
hasInstance/private-in/in/optional containment, and binding/destructuring assignment
helpers. Existing branch/loop/array/predicate/constructor/static engines are retained,
with their newly reached source arms reviewed while completing those families.
Historical callable-era function catalogs are not current branch authority.

## Dependency handoff and implementation

Core was asked to provide the exact switch link/state models (`switchTypes`,
`switchTypesComputed`, `witnesses`, `witnessesComputed`, `exhaustiveState`) and genuine
Checker optional-expression, synchronous for-of, binding/default/rest and statement
callers. Flow uses the existing property/private-symbol/global Record/type-model
helpers. Any material new type/protocol pressure will be reported before expansion;
no tuple shortcut, library alteration or scalar/profile fallback is allowed.

Four coherent library snapshots compiled cleanly through core's central builder;
immutable snapshot 4 includes the saved source corrections and is with verifier.
No owned build or runtime acceptance is claimed.

| Source family | Current state |
| --- | --- |
| Property-presence, private-in, instanceof and optional containment | Original narrowing and hasInstance/optional-call effects restored; source comparison and core independent review completed, runtime qualification pending. |
| Binding/destructuring transfers and constant references | Original initial/assigned/default/array/property/rest transfers restored. Existing cached initializer and indexed-undefined helpers retained with actual options. |
| Switch flow | Original discriminant/property/typeof/true narrowing, witnesses/types caches, recursive exhaustiveness state, reachability and delayed bypass merge restored. |
| Shared/loop/array/capture/predicate engines | Existing source algorithms retained; selected synchronous caller and branch review completed. No completion claim from helper-name presence alone. |
| Grammar | Original binding declaration/name recursion, exported marker condition, labeled break/continue/switch rules and scanner-backed regex grammar restored. Async/using and import phase consumers remain distinct external boundaries. |
| Relation/inference/display and runtime unused consumers | Source object-spread diagnostic elaboration restores the original skip of spread properties; other properties still elaborate. Instantiation-expression display restores actual type-node equivalence/reuse and visited recursion guards. Selected consumer review completed; behavioral qualification remains separate. |

The constructor-property guard flagged during spot checking was traced by core to
the original assignment-declaration producer (rather than general property-flow
dispatch); core restored its source constructor arm without admitting checked JS
expando inference. Regular expression grammar reuses the actual scanner parser,
its target/flag validation and byte-span callbacks; it does not substitute a host
regular-expression engine. Missing-name triage now resolves every flow source
name, but this does not prove complete branch behavior or close independent review.

## Independent core packet review

Compared core's E1 runtime parent/binding/contextual/default/padding/rest/assignment
families with checker.go17038–17279, 18087–18445 and 12791–12975. Actual strict-null,
NoUncheckedIndexedAccess, access flags, tuple bounds/rest, non-inferrable contextual
pattern state, inherited private/protected omission and lazy Omit lookup match source.
Unused-pattern traversal retains rest-removal usage, underscore conditions, combined
diagnostics, renamed parameters and actual options. Three Unicode public-display
calls in unused-renaming diagnostic arguments were handed to core for byte-role
correction. This is a concrete internal diagnostic-role issue; no diagnostic count or
expected value is normalized.

Compared the E4 for-in/for-of and ordinary loop, switch/fallthrough/default, label,
try/catch/finally, with and throw publication/traversal branches. Catch lookups use
the represented SymbolTable's null-returning indexer, preserving Go missing keys;
no dictionary workaround is needed. No additional source discrepancy identified
in this packet. Independent review of owned Flow/grammar/display is core's task;
own source comparison and a clean compile do not replace it.

Compared the new E2/E3 array and object literal/spread producers, optional expression
markers and property/element/call chains, tagged-template signature and synthetic
arguments, template evaluation/context, satisfies/non-null, meta-property, delete,
in/instanceof and unary/binary operator families against their pinned functions.
The source contextual/inferential flags, readonly/private spread exclusions, optional
property merging, exact-optional delete/assignment diagnostics, TypeOnly equality
policy and short-circuit result types are retained. Source comparison found two newly
added ordinary Dictionary reads whose missing entries must return null: the contextual
binding-pattern lookup and optional-call signature cache. Core repaired both. The
represented SymbolTable already has the required null-returning indexer; its spread
member lookups need no change.

Two directly reached source discrepancies were handed to core and saved after snapshot 2:
private-access diagnostic names must use the existing byte-string display producer,
and bigint exponentiation target diagnostics now use the captured languageVersion
field rather than recomputing the option. The demanded unchecked-JS suggestion leaf
was source-ported in Utilities with its actual file/declaration exclusions; it returns
false for TS through the original file-kind condition, without admitting JS semantics.
No expected values or diagnostic categories were changed. These are bounded source
review findings; complete managed/native comparisons and final gate remain pending.
Core's independent grammar review also found a named hostless test-adapter dependency:
the original module-format host lookup now falls back to its captured module kind only
when that adapter has no Program host. Genuine Program paths retain the original call.

The finite gate scaffold was read for corpus/shared/order/ordinary CLI coverage,
genuine unmodified Go construction, one retained cascade, native/current executable
checks and recorder nesting. No omitted execution stage identified. Verifier was
asked to check each declared group provenance digest explicitly and add meaningful
public observations for the newly restored switch(true) and no-default exhaustive
switch branches; neither request adds private instrumentation or changes expectations.

The immutable-2 broad comparison exposed real downstream source dependencies rather
than altered expectations: Evaluator now has the original template interpolation
helper, preserving callback locations, raw byte text, early nonconstant return and
external-reference flags. EmitSupport restores the original TS property-declaration/
signature implicit-undefined condition, including actual reverse-mapped links; it
does not introduce emit execution or broaden JS semantics. Required symbol-table and
import/export assignment-name helper leaves were source-ported in Utilities.
Core independently reviewed these additions; they compiled in snapshot 3. Their
complete behavioral comparison remains verifier's task.

The new public binding/pattern/meta type-query arms match their pinned producer.
Additional ordinary this/super/constructor/computed-literal and import-equals symbol
query omissions were handed to core for finite source restoration. Dynamic import,
import attributes and broader unfinished module/type consumers remain distinct;
no observer expansion was requested to conceal a missing producer.

The whole original controlFlowInstanceof compiler input is correctly external because
its actual allowJs/checkJs directives include the virtual uglify.js runtime/JSDoc unit.
Its complete prior request/reference/failure remains investigation evidence; no JS
erasure or option change is justified. TS instanceof/custom-hasInstance cases retain
their public narrowed-type observations. Final manifest/runtime reports control
acceptance counts, including the additional ordinary binding/meta public-query
witness. No compiler assertion or emit-baseline claim is attached to input reuse.

The final finite scaffold review found no omitted new corpus/shared/order/CLI stage,
weakened full-record comparison, unsupported-success conversion or duplicate retained
cascade. Group provenance hashes, request counts/digests and source-file hashes are
now checked explicitly. Recorder wording separates new per-file/bulk/repeated
expression observations from incremental/update/reuse evidence retained from earlier
campaigns. Managed/native replay and final drift-bound proof remain pending.

After snapshot 3, core restored the ordinary keyword/export/literal-import-type
symbol-query tail and the actual unchecked-JS suggestion condition in missing-member
publication. Finite comparison of these corrections and deferred overload checking,
contextual-deprecation cancellation, resolved-signature cache/reentry and dispatch
identified no remaining selected-source discrepancy. Decorator/JSX consumers retain
explicit external failures, and no observer or expected output was changed to hide
a missing producer. These saved changes compiled in snapshot 4; behavioral replay
remains necessary before runtime qualification.

## Final evidence review

The first frozen gate identified loss of existing passive object-producer observation
metadata and four genuine public-symbol regressions. The passive hook was restored
at the actual literal producer, retaining named-adapter gating and no genuine Program
observation work. The public lookup repair restores the source expression-context
predicate before type/namespace resolution; missing type members return unresolved
symbols rather than null or unrelated value properties. Complete affected DataTypes
92 and Collections 44 records match their unchanged historical expectations on
snapshot 5. No residual private-state investigation or broader instrumentation was
needed.

Independent final CLI/retained proof review found no blocker: actual Linux x64 native
85 records, six shared groups, 26 order executions per runtime and both ordinary CLI
cases pass. The valid project exits 0; invalid exits 1 with all five original diagnostic
records, locations, chains and related information. Retained current Configuration
14 and Resolution 11 controls preserve their explicit checking-environment migration
and authored unsupported boundaries. Named legacy Projects controls and retained
regression suites pass. The proof records 24,765 selected-suite xUnit executions,
including 91 new cases, with cheap repeated checks counted separately; this is not a
distinct upstream-test inventory. Nineteen native artifact paths are qualified, 242
historical assets are protected, and all five drift checks remain clear. Gate elapsed
time is 1,398.790 seconds. No additional tests or production changes were performed
for this final review.

This verdict applies to the fixed synchronous contract. Whole-checker completion,
other native platforms, general async/generator/JS/JSX/decorator semantics and remaining
advanced-type consumers are separate obligations. The new corpus has no incremental
lifecycle sequence; earlier lifecycle evidence remains in retained campaigns.
