# TypeScript expressions/statements: core progress

Active baseline `5efa4e5`, pinned upstream
`fed0bf24149fb1ed36039212648bafdafc1ea10e`. Core owns live Checker/Types/Links and
central builds; support owns Flow/grammar/relation/inference/display. Genuine
Program construction/options/library bytes remain the semantic entry. Prior proofs
are immutable. Root owns the contract/source map and final acceptance.

## Source responsibility assessment

| Family | Current implementation and required closure |
| --- | --- |
| Binding and assignment | Declaration-only typed binding and synchronous iterator type helpers already exist. Restore runtime/default/contextual binding producers (checker.go17038/17183/18087–18450), object/array rest and assignment-pattern checking (12791–13000), actual widening/initializer checks and flow consumers. Current guards are implementation gaps, not source policy. |
| Spread and optional chains | Existing array/spread-argument and tuple algorithms are genuine. Object spread merging/override diagnostics (13625–13839) and optional property/element/call producers, signature flags and markers (9232/29593–29611) remain partial. Support restores corresponding narrowing/effect transfers. |
| Synchronous expression dispatch | Complete delete, bigint, regex, no-substitution/template/tagged-template, meta properties, non-null/satisfies, in/instanceof/comma and omitted-node consumers. Assertion main producers are already source-shaped; remaining contextual/deferred/relation/flow callers need closure. |
| Synchronous statements | Existing if/ordinary loops/return/block checking is reusable. Restore for-in/switch/labels/try/catch/with diagnostics (3833–4304), synchronous for-of binding/assignment, fallthrough/reachability and error recovery. |
| Flow and shared dependencies | Support restores switch exhaustiveness/caches, binding/assignment/default/delete transfers, optional chains, in/instanceof/private-in and predicate/effect narrowing. Core supplies source links/models and real caller APIs; no lexical approximation or blanket successful traversal. |

Recommended coherent milestones: runtime binding plus synchronous statement callers;
spread/optional and remaining expression dispatch plus matching flow; independent
source review and complete genuine-Program comparisons; one frozen native/retained
acceptance. Source implementation, review and runtime qualification are separate.

Async/await/yield/generator/for-await/resource-management execution, checked JS/JSX,
decorators and dynamic import remain later responsibilities. Operational advanced type
constructors/string mapping remain external unless a concrete demanded dependency is
reported and approved. Regex/meta dependencies will be assessed at their source caller,
without assuming emission from their names. Existing generic rest/variadic source
algorithms execute where supported; blanket historical profile exclusions do not
describe the genuine path or establish whole-family completion.

Initial integrated packet: source for-in/switch/labels/try/catch/with methods and
dispatch; switch links/models; runtime binding/type-pattern/default/padding/contextual
producers, recursive contextual parameter assignment, rest-property type selection,
and destructuring assignment methods. Declaration-only capability guards are removed
from these completed source consumers. Matching Flow/grammar/shared helper closure is
support-owned and now paused for first grouped compiler triage. Synchronous for-of assignment patterns and numeric for-in index recognition use their actual source callers.

The old class-constructor-property-flow guard was in the JS expando-symbol path:
the original binder's bindThisPropertyAssignment returns immediately for TS. The
cheap underlying constructor arm now calls the already ported source flow producer,
preserving its non-null constructor precondition. Checked-JS admission remains external;
this restores source control flow without claiming a JS checking environment.

First coherent library snapshot compiled cleanly (0 warnings/errors) after four central compiler invocations; logs and immutable DLL are retained under artifacts/expressions-validation/build1. Binding/statements/Flow/grammar are ready for initial consumer triage; E2/E3 implementation remains pending. No new managed/native acceptance yet. Accepted D1–D6 is not
being re-audited, and the stale historical function inventory is not current authority.

Second producer packet is saved: original object-literal spread folding/override/property
model, optional property/element/call/signature markers, tagged/template/regex/bigint,
meta-property/delete/non-null/satisfies/in/instanceof/comma callers and debugger grammar.
Small shared source leaves supply tuple access, Extract/string selection, index-info
readonly copying and optional markers. Operational advanced StringMapping consumers
remain explicit; the reused rest/Omit and template constructors retain their source
algorithms. First body compile exposed mechanical type/enum/API bindings, not a new
subsystem requirement. Independent source review repaired two actual Dictionary
missing-entry lookups and three unused-renaming Unicode diagnostic arguments.

Immutable snapshot 1 consumer compiled cleanly. Original checker 2 passed; initial
10 meaningful requests had 7 complete Go-record matches and 3 failures in still-pending
object assignment/instanceof/debugger callers, now replaced in the second packet.
No broad differential or native acceptance is claimed.

Independent support-source review underway: flow.go2273–2388 binding/default/array/rest/
property assignment transfers match the actual source recursion and option handling;
flow.go1989–2038 switch witness/type caches retain empty-string initialization, nil
unsupported witnesses and computed flags. The old catch-map suspicion was withdrawn:
its actual SymbolTable indexer already returns the Go nil value on an absent key.

Snapshot 2 composite/remaining-dispatch packet compiles cleanly (0 warnings/errors).
Six measured central library invocations across two coherent packets; immutable DLLs
and every compiler log retained. Production now waits only for concrete source/runtime
findings while independent source review and copied-consumer replay proceed.

Independent peer review (current saved source, aggregate not whole-checker certification):
- Flow switch discriminant/typeof/true/optional/property narrowing, witness/type caches,
  reentrant exhaustive-state handling and bypass/reachability preserve upstream families
  flow.go1059–1409/1949–2038, including duplicate/default witness behavior.
- Flow binding/default/deletion/destructuring transfers, constant-reference/effect and
  instanceof/private-in/in/optional containment paths preserve source filtering and
  custom hasInstance predicates (flow.go811–1058/2273–2388). No TS synchronous flow
  capability guard remains; for-await is explicit at its own consumer.
- Grammar source regex scanner reuse retains byte text, duplicate error/related chains,
  parse-diagnostic policy and scanner cleanup; tagged/meta/bigint and binding/name/label/
  switch grammar match source target/option/error selection. Named hostless CreateCore
  needs explicit compatibility module-format lookup; genuine Program host remains strict.
- NodeBuilder instantiation display retains original visited-type reuse/recovery guard
  and annotation equivalence; Relater object-spread elaboration skips only the spread
  property as source does and checks other properties.
No substantive finding remains in these reviewed peer algorithms. Latest source has two
small core diagnostic corrections (private-name raw byte spelling and captured bigint
exponentiation target) plus the named hostless grammar compatibility beyond immutable2;
these will be batched with any actual replay findings. CLI2 is coherent with immutable2.

Snapshot3 compiles cleanly after the whole correction batch: exact template evaluator,
isSuperCall/tagged predicate, QualifiedName typeof dispatch and PropertyDeclaration
optional diagnostic display. Immutable2 broad83 had74 exact and9 exceptions, with no
value/diagnostic mismatches in successful records; one whole mixed checked-JS fixture
is outside contract and verifier-owned classification. Historical request/expected
prefixes remain intact. Source query omission review now restores actual binding/pattern
node typing, meta transient symbols, this/super/private/computed/object-binding-property
and numeric/string indexed symbol calls. No observation-schema extension or new private
probe was added. Support independently reviews those finite API producers.

Measured central ledger currently8 library invocations across3 coherent packets,
with all logs/timing files retained, plus CLI2 compiled separately. No frozen fullgate
or native acceptance yet. Next verifier action is copied3 failed representatives then
one complete current matrix if clear. Evaluator20line helper and EmitSupport optional
property conjunction independently compared to source (including nil substitution
flag reset and actual reverse-mapped link predicate); no finding.

Snapshot4 closes the source deferred-check dispatch family: tagged-template failed-overload
arguments and instanceof binary callbacks now run the original selected producers;
unknown source cases retain original no-action behavior, with explicit decorator/JSX
consumer failures. Cancellation checks are retained in deferred/contextual-deprecation
loops. Cached expression loop isolation and resolved-signature cache/reentry already
matched upstream. Ordinary query keyword/export/import-type tail arms and the actual
unchecked-JS suggestion predicate replace remaining local shortcuts without admitting
checked JS. Support independently compared the saved dispatch/cancellation/cache/query tail and
unchecked-JS suggestion predicate; no substantive source discrepancy found.

Immutable3 representatives5/6 exact; tagged-template deferred dispatch was the only
remaining representative failure. Seven retained callable/collection/ownership smoke
controls passed on3. Snapshot4 compiles clean0/0 (7.36wall seconds); central ledger9
library invocations64.68seconds across4 packets, separate3 CLI invocations5.18seconds.
Every log retained. CLI executable assembly is tscheck.dll, with exact snapshot4 library
SHA. Next verification: tagged representative then current85 full records if clear.
No native/final acceptance claim.

Final finite peer source verdict received for E1–E5: no unresolved substantive
omission in reviewed synchronous binding/assignment/statements, spread/optional,
operator/template/meta and selected public-query/deferred/cache families. Core
disjoint review of supporting Flow/grammar/relation/display/evaluator/visibility
producers likewise has no current finding. This qualifies the reviewed source
responsibilities, not whole-Checker completeness or operational external consumers.
Runtime matrix/native/retained qualification is still pending. Source4 is stable.

Candidate4 readiness: tagged-template representative exact, then current85/85
complete original records exact47.774seconds; shared6 exit0, ordinaryCLI2/2 full
original DTO exact1.285seconds (valid0/invalid1 with all5 diagnostics). These are
focused candidate results, not final native/retained acceptance. Core freeze ACK
sent: no pending source/tools/assets edits. Verifier alone owns prepared fresh
cheap retained suites before NativeAOT and the one retained cascade; no duplicate
broad replay or publish by core. Narrative progress-only updates remain permitted.

First gate failed/archived exit1/165.99seconds, all5 drift checks false, no native
stage. Thirteen Collections failures traced to lost authored passive literal
registration after replacing its old mini producer; no upstream model field was
removed. The existing named-Collections-gated hook is restored inside the actual
source object-literal creation closure, with no algorithm/expectation change.

Four DataTypes failures exposed a genuine public-query producer regression: the
property/QualifiedName checking arm preceded Ast.IsExpressionNode, so type-space
names incorrectly used expression resolution and lost/wrongly selected symbols.
Restored exact source32333 predicate/else type-namespace-unresolved ordering and
source TypePredicate tail. Archived complete records establish meaningful public
symbol deltas, not allocation-number equivalence. Support independently reviewed
both repairs and found no issue. No blanket graph parity repair is authorized;
any residual private-only observation delta will be classified separately.

Snapshot5 clean0/0 in8.55wall seconds, immutable DLL/CLI handed to verifier for
failed17 replay and affected retained consumer checks. Central ledger10 actual
library invocations73.23seconds across5 packets; no native/acceptance claim.

Copied5 focused archived17 complete records all exact. Affected DataTypes92 and
Collections44 checks are verifier-owned before the authorized corrected finalgate.
Core source-stable ACK: no pending source/tools/assets edits, no duplicate runs.
Separate4 coherent CLI builds7.55wall seconds; producer ledger above unchanged.
