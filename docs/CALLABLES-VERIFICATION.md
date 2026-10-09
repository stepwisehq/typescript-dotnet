# Callable verification and reproduction

The fixed [contract](./CALLABLES-CONTRACT.md) adds an authored Callable admission/option
profile around the real upstream checker. Legacy admission/options and accepted fixtures
remain protected. Upstream is `fed0bf24149fb1ed36039212648bafdafc1ea10e`.
The final [durable proof](../validation/results/callables-verified.json) records all
89 semantic cases exact on actual Linux x64 Native AOT and all 23,411 xUnit cases
passing, including 701 checker cases. The complete gate took 512.357 seconds with
no source, asset or historical-fixture drift. Accepted source is
`32e043af7ec8419dbee4ce132be7cb72df84ef56a148d564c1a081dc989302df`.

## Evidence by capability

| Contract row | Independent valid and invalid interactions |
| --- | --- |
| C1 signatures/parameters | Required/optional/default/receiver parameters, wrong order/self/forward defaults, missing/extra arity, original/reversed overload order, incompatible implementation, actual source declare signatures and invalid supported modifiers. |
| C2 calls | Typed/untyped/not-callable/nullable calls, nested/fresh argument mismatch, context-overload selection, explicit receiver/detached call, optional member narrowing, thenable argument/property errors and related await suggestions. |
| C3 inference | Nested callbacks, wrong contextual returns, object returns, self/mutual recursive return cycles, annotations, captures/defaults, implicit-any parameters, inferred predicates plus assignment/annotation/multiple/conditional negative controls. |
| C4 relationships | Strict function contravariance versus method bivariance, void compatibility, merged/inherited callable objects, recursive data/call shapes, union/intersection composite calls, synthetic parameters, hidden-this signature target/mapper identity and nested methods. |
| C5 bodies/effects | Scalar/object auto assignment, captures, no-return/void/fallthrough/multiple returns, throw/never effects, method/arrow this, local type shadowing, function expando data, all admitted operator families, internal bigint-zero union truthiness, lexical arguments capture. |
| C6 boundary/identity |43 independently authored controls (35 reject,8 admit),10 shared public API assertions, repeated queries/calls and eight counter/order-sensitive full-graph replays. Actual Native AOT and the complete historical regression cascade pass. |

[Corpus manifest](../validation/callables-corpus/manifest.json) identifies five integration
and84 matrix inputs. These are authored additional comparisons against actual Go;
no full upstream compiler-test assertion pass is claimed. No libraries are injected.
Globals are exact separate diagnostics: usually eight missing types, but admitted source
builtin declarations can alter this number. C3-context-wrong-return produces a ninth
missing Promise diagnostic through genuine synchronous-callable error elaboration;
C6-source-builtins supplies Object/Function and leaves six missing globals. Ordinary lexical `arguments` follows the
actual missing-IArguments empty-object fallback. TS/noLib Object.defineProperty witnesses
retain missing Object/property diagnostics; they do not claim JS expando semantics.

[Coverage](./callables-coverage.json) inventories all observed signature/predicate/composite
fields and each case. Current Go expectations contain 178 signature records,105 calls,
1208 repeated queries, two composite signatures and two inferred predicates in addition
to 89 shared no-predicate sentinel records. Declared/composite signature lists and resolved/
recovery signatures retain actual order. Ephemeral local overload candidate work arrays
are not separately serialized; their selection/order consequences and diagnostics are
compared. Public-return, parameter, receiver, declaration, target/mapper and cache identities
remain actual compiler objects. Full nullable diagnostics retain raw arguments, chain/
related ordering, flags, source/file metadata, formatted text and nil distinctions.

## Original tests and support

[Assertion inventory](./callables-tests.json) and [numeric provenance](../validation/callables-corpus/numeric-provenance.json)
record ten newly applicable original jsnum functions:152 literal table assertions and152
independent Node assertions,304 shared xUnit/native cases. Exact literals remain exact;
only original Node exponentiation checks allow one ULP. NaNs compare NaN-aware and signed
zeros compare equal as in original helpers. Node is required explicitly at acceptance.
Benchmarks are excluded as performance measurements, not claimed correctness tests.
No direct evaluator test file exists at the pin; selected evaluator dependencies are
source-reviewed and covered by additional semantic operand/diagnostic interactions.

Original assertion bodies are shared between [xUnit](../tests/Checker.Tests/CallableNumericTests.cs)
and [native checks](../validation/Checker.Validation/CallableNumericChecks.cs). Independent
admission controls are not generated from C# admission. The actual synthetic-parameter API
control uses number/string callable-union arms (Never parameter); number/number arms reuse
a declared original parameter and were an authored fixture correction, not a compiler fix.

## Observation scope and omissions

See the [independent review](./progress/review-callables-verification.md). Go reflection
independently discovers actual fields; C# uses an explicit static mirror and source-generated
JSON with reflection disabled. Unknown fields, payloads, retained key families and unobserved
signature-cache owners fail comparison. Observation order is fixed; resolving return/
predicate/parameter types is an intentional public operation before signature snapshots.
Later structured/symbol/cache traversal yields queue-visit snapshots, not one simultaneous
final-state snapshot. Source-file deferredNodes compares actual ordered Values, not Set
backing allocation/capacity. Logical link presence is compared; unportable untouched
paged-slot allocation remains outside the named-data observation boundary.

All actual retained cachedSignatures entries are zero, verified against total map cardinality.
Generic/optional-chain producers remain excluded; hidden-this instantiation naturally retains
real target/mapper pairs without that map. isolatedSignatureType and decorator links stay
null; their source producers are excluded and these states are not advertised as exercised.
The original scalar observation omits union regular-type mutable cache; the opt-in rich
data graph retains its actual field along with literal fresh/regular links and members/
composite constituents. No fake compiler state is
seeded to increase coverage. Arrays/rest/generics/classes/async/modules/libs/emit and other
[explicit grammar exclusions](./CALLABLES-CONTRACT.md) remain outside semantic admission;
parser/binder/foundation proofs protect their separately accepted syntax behavior.

The late omission review added local interfaces inside ordinary functions, arrows and
object methods; both declaration orders of a merged hidden-this interface now naturally
retain three anonymous instantiation entries each. Every actual key is reconstructed from
the original ordered outer arguments, alias and single-signature flag and checked against
actual key/value membership. Unknown key families and unrooted key arguments/aliases
fail explicitly. No getter populates this cache during observation. Actual Symbol/Symbol.for
calls expose distinct const unique-symbol types and ordinary let/var symbol widening;
ordinary SymbolConstructor calls remain primitive. Full raw unique names validate against
actual owner/name/unsigned-ID before canonical owner identity replaces the allocation
counter. Whole-map cardinality and every actual symbol-key membership are compared.
The eight order controls include these populated anonymous and unique caches: two
sequences give 21 exact results per runtime. The original 80 fixture bytes are preserved.

The first source-frozen gate passed 529.436 seconds but is preserved as preclosure regression
evidence under [preservation](../artifacts/callables-validation/before-cache-closure-470449302c/preservation.json).
Independent source challenges afterward exposed these required cache/scope dependencies;
no accepted durable phase proof was emitted for that gate. The final gate must use the
repaired source, strict observers and all 89 cases.

## Reproduction

After obtaining the pinned upstream/toolchain using existing bootstrap scripts:

```sh
bash tools/build-checker-oracle.sh
dotnet build validation/Checker.Validation/Checker.Validation.csproj -c Release
python3 tools/compare-callables.py --managed --executable validation/Checker.Validation/bin/Release/net10.0/Checker.Validation.dll --output artifacts/callables-validation/managed
python3 tools/check-callables-boundary.py --managed --executable validation/Checker.Validation/bin/Release/net10.0/Checker.Validation.dll --output artifacts/callables-validation/managed-boundary.json
python3 tools/check-callable-order.py --managed --executable validation/Checker.Validation/bin/Release/net10.0/Checker.Validation.dll --output artifacts/callables-validation/managed-order
dotnet test tests/Checker.Tests/Checker.Tests.csproj -c Release
```

The final frozen gate is `python3 tools/run-callables-gate.py`, followed only on success by
`python3 tools/record-callables-evidence.py`. Its [composition](../tools/verify-callables.sh)
runs exactly one complete retained binding/foundation/data/object/scalar cascade, current
original-Go callable output reproduction, ten original Go arithmetic tests, actual native
semantic/admission/API/numeric/order checks. This includes the complete original Go numeric
finite supplement, not only a one-seed smoke. Source/assets and historical fixture/result
bytes are guarded; no count limit, skipped request or expected rewrite can satisfy acceptance.
Six native binary hashes are captured after the historical cascade and before callable
execution, then checked afterward; prior cascade stages retain their own earlier guards.

The [durable proof](../validation/results/callables-verified.json) records six actual
Linux x64 ELF Native AOT binary hashes/dependencies, exact source/assets, timing,
counts and regression results. It includes all 89 native semantic comparisons,
304 original numeric assertions, 43 admission controls, 10 API assertions and
21 original-Go plus 21 native order observations across eight control sources.
The retained cascade passes 17,723 syntax/scanner/binding inputs, 3,153 foundation
direct cases, finite original fuzz seeds and all earlier semantic stages.
Managed integration failures and fixes remain in the
[resumable checkpoint](./progress/callables-verification.md); they are not accepted results.
