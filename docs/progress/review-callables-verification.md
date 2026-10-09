# Independent callable verification review

Owner: verification; original revision `fed0bf24149fb1ed36039212648bafdafc1ea10e`.
Review covers [contract](../CALLABLES-CONTRACT.md), admission, core/support integration,
independent Go discovery, static C# graph, shared numeric assertions and composed gate.
Final postclosure build 24 review disposition: no unresolved source/observer finding;
89 exact semantic cases and 701 checker xUnit cases pass. The final [durable proof](../../validation/results/callables-verified.json) now records
89 exact Native AOT cases and all 23,411 xUnit cases with no drift, plus all numeric,
admission, API, order and historical regressions. This source/observer review is
separate from that actual execution.

## Findings and dispositions

- Callable admission preserves Legacy routing and flags; unsupported syntax is tested
  separately from actual semantic failures. The duplicate authored function-modifier
  routing was repaired without changing async-control expectations. Invalid supported
  readonly/accessibility modifiers reach genuine checker grammar diagnostics.
- Full signatures are independently reflected from Go: allocator ID becomes canonical
  identity; all thirteen other fields are statically mirrored. Composite order,
  parameter/receiver/declaration identities, target/mapper links, predicate payloads
  and the shared no-predicate sentinel remain distinct from null. Unknown fields and
  retained unrecognized signature-cache keys fail discovery rather than disappear.
- Real source-file worklists were initially omitted by the proposed two-field observer.
  All eleven fields are now compared; actual function/interface/block/type-alias/
  function-type registrations were restored in core. Disabled unused-check options
  do not justify omitting registration. `deferredNodes` compares its actual ordered
  Values sequence, not the private Set backing map's capacity or identity.
- The contextual-arrow extra cache and missing call-effects sentinel were concrete
  runtime differences. Original non-Normal cache bypass and source-element reachability
  pre-dispatch/save/restore restored their actual causes; no observer cache writes or
  flag normalization were added.
- Hidden-this inherited signatures exposed a preexisting incomplete isThisless test.
  Genuine source predicates restored original parameter-symbol sharing. Signature
  target/mapper links are naturally observed; no synthetic retained cache is seeded.
- The authored synthetic-parameter API specimen originally used number/number arms;
  actual Go reuses the original declared parameter there. The corrected number/string
  arms produce a genuine transient nil-declaration Never parameter. Owned access and
  foreign-checker rejection are now directly asserted; compiler expectations unchanged.
- Source review found three additional admitted failures after the 76-case milestone:
  truthiness of a union containing internal bigint zero, nested methods under hidden-this
  instantiation, and missing thenable properties. Original Go accepts all three sources;
  build 19 throws explicit implementation guards. These are retained corpus regressions
  closed by genuine core source paths in build 21/22; they remain regression rows and are not excluded or normalized.
- Flow state pooling/reset, shared memoization, start/capture traversal, branch assignment,
  auto variables, truthiness/equality/typeof/predicate narrowing, effects and reachability
  were compared against original source. Core/support independently review this owned
  file. The latent auto-assignment initializer-null precondition omission was repaired
  by restoring original `isEmptyArrayAssignment`, including the binding-element check.
- Relater signature arity/variance/receiver/return comparison retains original ternary
  handling and strict method-bivariance exception. Inferred predicate construction and
  sentinel return boundary are genuine. Numeric ToInt32/shift/modulo/integer-power
  adapters retain original operation order and precision boundary; 304 shared original
  literal/Node assertions passed managed execution, without broader Math.Pow claims.

## Observation order and sensitivity

Admission may resolve documentation to check unsupported tags; actual Go observations
resolve equivalent syntax before semantic graph registration. Diagnostics precede public
node queries and directed relations. Callable calls resolve actual signatures twice;
new signatures resolve return/predicate and parameter types before field snapshotting.
Logical signature/source-file links are read at that defined stage. Structured-type/
symbol traversal then extends a preregistered graph; remaining signature-cache associations
are enumerated without invoking cache-producing APIs. These are queue-visit snapshots,
not a simultaneous final-state snapshot. Nullable diagnostic arguments, chains, related
information and full flags/source/file values retain their actual states and order.

The exact recursive comparator retains JSON types, all keys, array order and raw bytes.
Failure requests/outputs remain failures even when a process is restarted. The separate
[order control](../../tools/check-callable-order.py) verifies eight existing identity-sensitive
cases in original order and reversed after five prefix requests, in fresh actual-Go and
C# processes. Build24 gave21 exact outputs per runtime, using unchanged stored full
expectations. This varies global allocation order; it does not normalize away cache or
symbol identities. Native repetition is mandatory in the composed final gate.

## Explicit limits and completed gate

Retained cachedSignatures entries are zero, checked against whole-map cardinality after graph traversal (unobserved owners fail): erased/canonical/base insertion requires
nonempty signature type parameters; inner/outer insertion is an excluded optional-call
path. Hidden-this instantiation directly retains target/mapper without that map.
`isolatedSignatureType` remains null; its original callers require generic inference,
JSX/decorators, construct printing or synthesized accessor/decorator functions. Fields
are still serialized if encountered. Decorator signature links and source JSX/emit-helper
fields retain zero/null states; no successful source capability is claimed for them.
Physical untouched paged-store allocation is excluded as already documented in the
named-data protocol; logical resolved symbols and cache presence remain compared.
The original scalar graph omits the union regular-type mutable cache; the opt-in
rich data graph retains its actual field. Literal fresh/regular and actual composite/
type/member edges are retained.

Historical fixture/result bytes are guarded against `de855ca`. The final gate composes
one complete retained binding/foundation/semantic cascade, then current original-Go
callable reproduction, original ten arithmetic tests, actual Native AOT graph/admission/
API/numeric/order observations. Source/assets and all six native binaries are guarded;
native prehashes are taken after the historical cascade and before callable execution.
Final source review/formatting is complete. Build 24 passes 89/89 exact comparisons
and all 701 checker xUnit cases. The final actual Native AOT gate passes 89/89
comparisons, 43 admission controls, 10 API checks and both 21-output order streams,
plus all original numeric and historical regressions. No claim of full compiler tests,
standard library availability or unrestricted generic/function syntax follows from this
bounded corpus.

## Post-gate independent cache/scope omission closure

The first successful 80-case native gate was preserved as preclosure evidence, not
accepted phase proof. Root's late source challenge independently exposed local
interface ancestor guards and an unknown nonempty anonymous instantiation-key
family. Ordinary SymbolConstructor versus actual global Symbol call recognition
and if-let grammar1156 were also checked against actual original Go.

The richer anonymous observer now retains every actual cache entry. Reference keys
use original ordered type arguments. Anonymous keys reconstruct the exact original
preimage from actual outer type-node parameters, effective stored Simple/Array
mapper arguments, actual alias and single-signature flag. The prototype seed uses
its original false flag. Selected composite mapper wrapping is unwrapped through
its genuine first mapper only on the original single-signature path. All other
unknown mapper/key variants still fail; map observation does not instantiate types.
Actual key equality, lookup-to-identical-value, repeated membership, distinct
preimages and canonical owner/value/argument/alias edges are checked. Go and C#
explicitly reject an unrooted argument/alias after fixedpoint rather than extend
incomplete queues. Core and support independently challenged this observer.

UniqueESSymbolType validates the complete original raw name against its actual
owner symbol and unsigned allocation ID before replacing the counter with that
owner identity; ordinary names are untouched. Whole actual cache cardinality plus
each genuine symbol-key membership/repeated identity prevents omitted owners or
misassociated values. Const/let/var and ordinary SymbolConstructor cases compare
real upstream semantics, not an authored special-case expectation. Source unique
symbol syntax remains excluded.

The original 80 request/expected bytes are unchanged. Nine additional cases cover
three local declaration containers, two merged declaration orders, ordinary
SymbolConstructor, two genuine unique-symbol call forms, and original 1156 grammar.
Eight order controls compare full stored graphs in original and reverse-after-five
prefix request orders; actual Go and managed 23 both 21/21 exact. Source fixes are
reviewed separately from authored observer fixes. Build 24 final 89/89 comparisons, Go 21/21 and managed 21/21 request-order observations,
and all 701 Checker.Tests now pass; The actual final Native AOT rerun now passes in the durable proof. No ignored guard, field erasure or semantic golden normalization is used.
