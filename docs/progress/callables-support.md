# Callable support implementation checkpoint

Accepted bounded completion of the [fixed contract](../CALLABLES-CONTRACT.md), with
[durable exact-source evidence](../../validation/results/callables-verified.json).
Baseline `de855ca`, retaining foundation/binding baseline `2886d47`; upstream pin
`fed0bf24149fb1ed36039212648bafdafc1ea10e`.

Final source SHA: `32e043af7ec8419dbee4ce132be7cb72df84ef56a148d564c1a081dc989302df`.
The successful comprehensive Native AOT gate took 512.357 seconds with no source,
asset or historical-proof drift. It passed 89 callable graphs, 43 admission controls,
10 API assertions, 304 original/Node numeric assertions, 21 exact request-order rows
per runtime and all 23,411 xUnit tests, including 701 checker tests. All retained
foundation, binding and Legacy semantic native regressions also passed.

Support owned Types, Mapper, Relater, diagnostic NodeBuilder/Printer/Pseudochecker,
selected evaluator/numeric dependencies, helpers and composed gate/recorder. Core
owned Checker/GrammarChecks/Utilities/Exports and the sole shared-library builds;
verification owned Flow plus admission, oracle, graph and test adapters. The final
review includes local-interface ancestors, natural anonymous-instantiation cache
associations, global Symbol identity/unique types and the direct-if-body diagnostic.
All known source and observer findings are resolved. No active work or pending
executable/tool/corpus writes remain; broader excluded checker families are unchanged.

The earlier 529.436-second gate was pre-closure regression expense, not accepted
proof. Its [snapshot](../../artifacts/callables-validation/before-cache-closure-470449302c/preservation.json)
remains intact. Notes below are historical implementation/review chronology; their
pending statuses and earlier freeze verdicts are superseded by this final disposition.

## Historical implementation milestones

The Go AST scaffold is syntax tooling only, not a regeneration contract; handwritten
guarded counterparts are authoritative.

Inference mapper allocation audit: original allocation is through generic overload
candidates, generic signature comparison or conditional inference. Admitted ordinary
contextual callbacks use the actual nil-context arm; inherited hidden-this uses real
ordinary type mappers. InferenceTypeMapper and backreference inference mappers remain
excluded unless a retained source caller demonstrates allocation. No fake inference
context is created to satisfy these unselected paths.

Profiles fixed: Legacy retains previous admission/strictNullChecks; Callable adds
expanded syntax, strictFunctionTypes, noImplicitAny and noImplicitThis. Neither is
strict-all. Source written predicates/typequery/polymorphic-this are excluded; actual
inferred predicates and hidden-this mapper state remain required.

## Selected substantial boundary

Retain the accepted named/recursive data-type syntax. Add ordinary non-generic function
declarations, expressions and arrows; function types/call signatures; overload declarations
and implementations; callable object data/method properties; higher-order/contextual
callbacks; callable unions/intersections; required/optional/default identifier parameters;
annotated/inferred returns and recursive calls. Include ordinary explicit `this` parameters,
receiver checks, object-method receivers and lexical arrow capture. Function bodies need
blocks, locals, ordinary expressions/calls, if/else, return/throw and the real supporting
branch/call/reachability flow. The exact expression/operator family must be fixed explicitly;
support does not assume arbitrary executable TypeScript is admitted.

Recommend defer rest parameters, array/tuple literals/types, argument spreads, binding
patterns, source generics, classes/constructors/new expressions, async/generator functions,
modules and library loading. Rest/tuple arity and array creation require real generic
Array/ReadonlyArray/iterator support even when generic *functions* are excluded. Fixed
noLib semantics can be preserved for the useful required/optional/default/callback path;
actual missing-global diagnostics stay visible. Construct signatures could be a later
separate extension rather than silently treating them as calls.

## Required mapped model and cache closure

[types.go](../../upstream/TypeScript/tsc/internal/checker/types.go:1312) has **14** Signature
fields: id, flags, minArgumentCount, resolvedMinArgumentCount, declaration, typeParameters,
parameters, thisParameter, resolvedReturnType, resolvedTypePredicate, target, mapper,
isolatedSignatureType and composite. The previous absent-storage-only model omitted mapper/predicate/composite behavior.
The implementation now restores real flags,
all getters, CompositeSignature(isUnion, ordered signatures), TypePredicate(kind,
parameterIndex, raw parameterName, type), and [SignatureLinks](../../upstream/TypeScript/tsc/internal/checker/types.go:425)
resolved/effects/decorator slots. Maintain signatures' ordered parameter Symbol identity,
nullable slices and call-vs-construct split on StructuredType; no phantom signatures.

Non-generic source does not justify discarding target/mapper/composite: inherited hidden
interface-this mapping remains selected, and contextual callable union/intersection
signatures use real composites. Return resolution needs Signature targets in the existing
tagged resolution stack, genuine recursive/cache publication and cycle diagnostics.
Signature/effects links must be observable statically with canonical IDs assigned before
following cycles. Existing Symbol, tables and binder flow are reusable; no binder expansion
is currently indicated.

The [InferenceContext family](../../upstream/TypeScript/tsc/internal/checker/checker.go:277)
originates in checker.go and remains core ownership. Ordinary return inference and contextual
parameter assignment are required without source-generic inference. Retain actual nil-context
branches; inspect every allocation/push site before deciding which inference fields are
unreachable. Existing partial context fields cannot stand in for newly reachable state.

## Relationship and option obligations

The prior [signature relationship methods](../../src/TypeScript.Scanner/Checker/Relater.cs:1552)
guarded all nonempty signatures. This phase restores upstream signaturesRelatedTo/signaturesIdenticalTo,
signatureRelatedTo, compareSignaturesRelated/Identical, parameter-position/minimum-count,
this/rest boundaries, union/composite matching and error-chain helpers. Preserve Ternary,
recursive caches, strict arity, callback-special comparison, return-to-void rules and
ordered overload selection; a simple contravariant parameter loop is insufficient.

[compareSignaturesRelated](../../upstream/TypeScript/tsc/internal/checker/relater.go:1491)
selects strict variance using strictFunctionTypes **and declaration kind**: methods stay
bivariant, ordinary function properties and callbacks use the actual selected rules.
The current fixed checker has strictNullChecks=true but strictFunctionTypes/noImplicitThis
are unselected; it must not be described as strict-all. Recommend preserve the existing
default profile and add an explicit narrow callable profile enabling strictFunctionTypes
and noImplicitThis. State noImplicitAny independently rather than accidentally enabling it.
The fixed Callable profile selects all three options; Legacy remains unchanged.

[getContextualThisParameterType](../../upstream/TypeScript/tsc/internal/checker/checker.go:12262)
uses explicit/contextual this first; inferred object-method this is gated by noImplicitThis
or JS. Source `this` parameters require actual thisParameter Symbol/type identity and call
receiver applicability checks; they are not ordinary arguments. Keep classes/super and
generic ThisType<T> paths guarded unless separately selected.

Written predicate/assertion syntax may be deferred, but implicit predicates cannot be
blanket nil: [getTypePredicateFromBody](../../upstream/TypeScript/tsc/internal/checker/checker.go:20939)
can infer predicates from ordinary boolean-return bodies. Actual parameter assignment and
true/false flow refinements determine the result. This requires real TypePredicate payloads,
effectsSignature caching and selected flow, or an explicitly narrower body contract.

## Diagnostic formatter closure and ownership proposal

Support should own Types/Links, Relater and diagnostic NodeBuilder/Printer/Pseudochecker
counterparts; core retains checker.go algorithms/fields/inference; support now owns Mapper.
Add actual [signature serialization](../../upstream/TypeScript/tsc/internal/checker/nodebuilderimpl.go:1865),
parameter/optional/default/rest classification, this parameter, return/predicate nodes,
signature scope/name tracking, declaration reuse and original printer signature/parameter/
function-type/call/method emit branches. Diagnostic printing remains separate from general
emit. Nonnil enclosing contexts, recursive/shadowed aliases, inherited callable interfaces
and Unicode/static names must use genuine existing accessibility/clone/emit-flag behavior.
Pseudochecker reuse may newly reach function/return-expression inference; audit actual selected
branches rather than disabling reuse. Existing signature guards are implementation work,
not new unsupported syntax acceptance controls.

Concrete first deliverable after approval: full 14-field signature/predicate/composite/link
models and enums, then real selected signature relationship core and diagnostic serialization.
Central builds stay with the coordinator-assigned sole owner; no separate builds now.
Verification proposes opt-in cyclic signature graphs plus links/return/predicate/composite
identities, alongside original-Go diagnostics, overload outcomes and contextual callbacks.
The contract should exercise opposite-variance parameters, method-vs-property variance,
optional/default/minimum arity, void/never/recursive returns and receiver compatibility.

## Selected evaluator/numeric dependency

Core operator diagnostics require the real [Evaluator](../../src/TypeScript.Scanner/Evaluator/Evaluator.cs)
(`evaluator/evaluator.go`): original numeric/string/unary/binary/entity branches and
result provenance flags are retained; source template interpolation/element access
remain explicit guards. `AnyToString`/`IsTruthy` retain the actual scalar values.
New [JsNum.cs](../../src/TypeScript.Scanner/JsNum/JsNum.cs) maps `jsnum/jsnum.go`:
ToInt32 modulo coercion, masked shifts, bitwise operators, signed-zero remainder,
NaN/infinity decisions and integer exponentiation are on disk. CLR binary64 operators
replace Go float64 operations. Integer exponentiation preserves the exact integer
and intermediate 256-bit nearest-even rounding before binary64 conversion; it reuses
the existing correctly rounded integer-to-double helper. `String.cs` is now a partial
Number declaration; Number's float equality and diagnostic Stringer behavior are
retained explicitly. No shared library build is run by support.

[PseudoBigInt.cs](../../src/TypeScript.Scanner/JsNum/PseudoBigInt.cs) now includes the
actual scalar model/NewPseudoBigInt/ParseValidBigInt/String/Sign/Compare needed for
original definitely-falsy bigint-zero state. Written bigint values are not newly
admitted. Managed default-null backing string compares as Go's empty zero string.
Verifier owns the newly applicable original arithmetic tests: 10 Go functions,
152 original table assertions and 152 independent Node assertions shared with native.
Build 13 managed execution passed all 304 original literal + Node assertions; only
the original Node exponent allowance is 1 ULP. Native execution remains pending.

## Gate ownership

Support now owns the small [composed gate](../../tools/verify-callables.sh),
[drift guard](../../tools/run-callables-gate.py) and
[successful evidence recorder](../../tools/record-callables-evidence.py).
Exactly one retained binding-completion cascade includes all current xUnit and all
foundation/binding/Legacy native regressions; new callable graphs/control/API/numeric
checks then reuse its freshly published checker AOT executable. Historical corpus
and proof bytes through `de855ca` are protected. Source/assets and all six native
binaries are fingerprinted. Scripts were syntax-checked only; the full gate must wait
for the coordinator's frozen-source authorization.

Latest reported runtime milestone (verification, build 13): tiny 4/5 exact with the
remaining contextual callback cache-presence discrepancy core-owned. Affected xUnit
611/612 passed, including all 304 original arithmetic/Node cases and Legacy regressions;
the remaining ambient-variable guard is also core-owned. Full matrix/native acceptance
is pending; no source/tool/corpus freeze is claimed.

## Build 14 handoff

Restored the actual internal bigint-literal diagnostic type arm and printer literal
text/dispatch, exposed by `value: bigint` truthiness narrowing to the upstream zero
payload. Written bigint literal syntax remains excluded. `PseudoTypeNodeBuilder`
already retains its literal reuse/type branches. Required English diagnostics were
regenerated with all 2,222 entries retained. Support files are syntax-stable and handed
to the sole library builder; the complete callable matrix/native gate is still pending.

Build 13 independently exercised all 304 new numeric original/Node assertions and
all Legacy regressions; the remaining contextual-cache and ambient-declaration
findings are core-owned. Current semantic corpus has 76 independently generated
Go cases. New gate tooling remains unexecuted pending root's final freeze authorization.

## Freeze-ready handoff

Independent final core/Flow review is closed for the selected source families. Three
material omission findings (internal bigint facts, hidden-this reference filtering/
anonymous instantiation, and thenable missing-property diagnostics) are source-fixed
and reviewed in build 21; the consolidated 79-case runtime comparison remains pending.
Build 19 had 76/76 semantic and 688/688 checker xUnit agreement before those fixes.

Owned support source and gate tools have no pending executable writes. LF formatting,
owned `git diff --check`, Python compilation and shell syntax checks pass. Gate now
requires 10 API assertions, 304 original/Node numeric assertions and four request-order
controls (13 exact rows per runtime across two sequences). Manifest semantic counts
remain dynamic. No full gate has been executed; root must authorize the frozen run.
Docs may update final evidence/dispositions after the gate. No acceptance is claimed.

## Pre-closure gate and reopened work

Build 22 independently passed all 80 semantic cases and 692 checker xUnit cases.
The first comprehensive native gate then passed in 529.436 seconds with no guarded
drift, all 23,402 xUnit tests, all new callable controls and retained regressions.
Its [preserved reports and native binaries](../../artifacts/callables-validation/before-cache-closure-470449302c/preservation.json)
are regression-only evidence; no accepted durable callable proof exists.

The earlier final-review closure is superseded by source findings for local
interfaces under admitted callable bodies and a naturally nonempty merged
hidden-this anonymous-instantiation cache. Core owns the exact outer-parameter
wrapper/cache repair. Support independently reviews it and the original `Symbol`
call condition; genuine internal unique-symbol model/formatter dependencies will
be added if that admitted ambient-value path requires them. Source/assets thaw is
authorized, acceptance remains pending, and a full exact-source rerun is mandatory.

The new support snapshot adds the exact `UniqueESSymbolType` payload (`TypeBase`
plus raw `name`), wrapper cast/static payload dispatch, original nodebuilder unique
symbol reference/operator arm, tracker/recovery forwarding and diagnostic printer
operator. It is syntax-stable and LF formatted. Core owns the genuine global `Symbol`
call test, per-symbol allocation/cache and unsigned-ID raw name. Verifier owns strict
owner-associated name canonicalization and natural cache/witness observations.
Independent review additionally found the admitted direct if-body let/const grammar
diagnostic guard; core restored the original 1156 arm. Central compilation, focused
observations and final delta review are pending; no accepted proof is authorized yet.
