# Generic-collection completion contract

**Fixed target accepted under Linux x64 Native AOT; see [assessment](./COLLECTIONS-ASSESSMENT.md) and [proof](../validation/results/collections-verified.json).** [Exact mandate](./COLLECTIONS-PROMPT.md).
Baseline `cfa6d2f`; upstream `fed0bf24149fb1ed36039212648bafdafc1ea10e`.
This is a substantial single-file collection-checking capability, not complete generic
type checking, a complete standard library implementation, or a project compiler.
Do not narrow this contract later to declare success.

## Environment and authentic libraries

Add an explicit authored Collections profile around the actual ported checker.
Keep Legacy and Callable admission/options and their existing evidence unchanged.
The new profile takes one syntactically valid TypeScript script and a fixed, genuine
upstream declaration environment. There is no user project or module loader.

Load these complete, unmodified files from `tsc/internal/bundled/libs/` at the pin:

| File | SHA-256 |
| --- | --- |
| lib.es5.d.ts | 45cc92f8dd3df7cb07b7d19e2b2ca34e78bc005c910c1e53ee1cf9bf37910215 |
| lib.decorators.d.ts | 1ce14b81c5cc821994aa8ec1d42b220dd41b27fcc06373bce3958af7421b77d4 |
| lib.decorators.legacy.d.ts | b3a048b3e9302ef9a34ef4ebb9aecfb28b66abb3bce577206a79fee559c230da |

The latter two are the actual reference-lib closure of ES5. Preserve their notices
and reference directives. Embed exact bytes in the .NET library with a small authored
fixed-file loader and manifest; the native executable must not require a Go process,
an upstream checkout or arbitrary filesystem discovery during ordinary use. The Go
oracle uses the same explicit roots and bytes: ES5, decorators, decorators.legacy,
then the user script. Record this exact order in the environment manifest. Initial
main-first investigation probes are not canonical acceptance goldens.

The pinned Program classifies all four explicit roots as **non-default libraries**;
the three library roots are independently declaration files. Preserve this distinction
in diagnostic-related-information policy and observations. A `lib.*` filename or
declaration-file flag does not confer default-library status.

Options: ESNext target; strictNullChecks, strictFunctionTypes, noImplicitAny and
noImplicitThis enabled; strictBindCallApply, noImplicitReturns,
exactOptionalPropertyTypes, noUncheckedIndexedAccess and downlevelIteration disabled.
Use noLib/noResolve with an explicit complete library root list to disable implicit
library/module discovery, not to remove these declarations. Module resolution remains
disabled. Record concrete options and file order in every new verification result.

Use **skipLibCheck=true** with the actual upstream Program diagnostic policy: parse
and bind every complete library file, merge the real globals and resolve demanded
declarations normally; do not perform whole-library declaration-body checking. Report
that checking disposition explicitly, not as a successful full-library check. Compare
all applicable parse/bind/user/global diagnostics and Program-level semantic results
under these identical options. A selected library member's type, constraints, predicates,
defaults, rest parameters, overloads or inferred results must never be weakened or
skipped because its declaration is in a library. Unsupported demanded paths must fail
explicitly. No fake declarations, changed signatures, diagnostic suppression or fallback
types may stand in for unported behavior. Original upstream any/error fallbacks remain
distinct from placeholders.

This environment has no Iterable global. At this pinned revision, original
getIteratedTypeOrElementType consequently uses its actual array-like fallback, even
with ESNext target. Port that path, including its real invalid-input diagnostics.
No custom iterator, ES2015 Map/Set, Symbol.iterator or general iterable ecosystem is
claimed. Loading decorator declarations does not add source decorator support.

## Required capability

Carry forward the admitted scalar, object, named/recursive data and callable behavior
under real library globals, including valid and invalid interactions.

1. **Generic declarations and use.** Type parameters on aliases, interfaces, functions,
   methods and call signatures; constraints and defaults; lexical parameter scope;
   explicit and inferred type arguments; recursive instantiation and cycle/depth
   behavior; generic inheritance/merging, unions/intersections and structural relations.
   Infer through ordinary arguments, contextual callbacks/returns, constrained objects,
   arrays, tuple positions and overloads. Preserve original inference priority, candidate,
   fixing, mapper, deferred result and cache behavior; no fixture-shaped inference.
2. **Arrays and tuples.** Mutable/readonly array types and literals, empty/evolving array
   inference, numeric element reads/writes and length; required, optional, rest, labeled
   and readonly concrete tuple types, contextual construction, bounds/readonly failures
   and relationships. Generic element types in these concrete tuple positions are allowed.
   Generic array constraints and actual array/tuple instantiation must remain real.
3. **Collection APIs and parameter forms.** All instance members of the genuine ES5
   Array and ReadonlyArray interfaces, including map/filter/every predicates, reduce/
   reduceRight overloads, concat/slice/sort, and mutable push/pop/splice/shift/unshift.
   Include Array.isArray and ordinary Array call signatures. Identifier rest parameters
   typed as arrays (including T[]) or concrete tuples, array/tuple argument spreads and
   array-literal spreads are required. Ordinary required/default/optional/this parameters
   retain earlier behavior. Source construct/new expressions remain excluded.
4. **Keyed and utility types.** String/number/symbol index signatures and element access,
   keyof and indexed-access types, mapped modifiers and homomorphic mapped types,
   distributive/deferred conditional types and infer. Include meaningful use of the
   unmodified ES5 Record, Partial, Required, Readonly, Pick, Omit, Exclude, Extract and
   NonNullable aliases. Preserve recursive constraints, readonly/optional propagation,
   array/tuple preservation, reverse-mapped inference, never/any and union behavior.
   Real internal Awaited and conditional/inferred-rest dependencies must work when
   ordinary checking or diagnostics reach them, without authorizing source async.
5. **Bodies and iteration.** Existing statements plus ordinary for, while, do/while and
   synchronous for-of with the environment's actual array-like semantics, unlabeled
   break/continue, scalar compound assignments and prefix/postfix increment/decrement.
   Retain original loop/capture/assignment/reachability/narrowing flow, diagnostic
   ordering, and invalid cases. This enables practical collection accumulation rather
   than restricting every computation to one expression.

Prominent exclusions: source destructuring/binding patterns, object rest/spread,
generic variadic tuple compositions (`[...T]`) and tuple-constrained rest parameters
(`...args: T`), type assertions including `as const`, optional chaining, source const/
variance type-parameter modifiers, template-literal types, mapped key remapping and
intrinsic string manipulations, explicit assertion signatures, classes/new/super,
decorators/enums/namespaces, async/generators/custom iterators, switch/try/catch/finally,
labels, modules/imports/projects, emit/incremental/services, JS/JSX/TSX/JSON and malformed
syntax at semantic entry. Existing foundation/binder coverage remains intact.
Ordinary explicit predicate return annotations are required for library and collection
filtering; assertion-function syntax remains excluded.

These source exclusions do not justify guarding internal representations that an
admitted operation or genuine library declaration produces. In particular, inferred
rest parameters inside Awaited and library-only binding patterns/construct signatures
must retain demanded upstream behavior. Other loaded ES5 declarations are not a claim
of full library API parity. Do not advertise unused declarations as verified semantics;
retain explicit failures for unported branches and account for observation limits.

## Early demonstration and independent verification

Use a realistic single-file record-processing program: recursive typed records;
generic constrained selection/grouping over readonly input; Record-based groups;
map/filter/reduce with contextual callbacks and overloads; tuple summaries and rest/
spread calls; and a loop accumulator. Integrate a mapped utility view/update and
readonly data. Choose inputs now, before implementation, and retain actual-Go results.
The initial upstream probe already establishes the plain generic array/tuple/rest/loop
core with zero user/library/global errors. Extend it to the agreed grouped/utility
demonstration before relying on its success. This is type checking, not program execution
or emit. No untyped declaration harness may replace application logic.

The accepted valid demonstration must have zero applicable syntax/binding/semantic/
global diagnostics, including zero missing globals. Its deliberately invalid variants
must exercise constraints, wrong properties/element types, tuple arity, callback/reducer
returns, readonly writes, and non-array iteration with actual upstream diagnostics.
Compare meaningful inferred results, relationships and identities, not only acceptance.
Maintain separate targeted cases beyond the demonstration: adversarial inference,
generic and recursive relationships, empty/default/union cases, mapped/conditional
interactions, overloads, errors, and cache/order reuse. No demonstration-specific code.

Extend observations for multiple fixed source files using file-qualified node identity.
Compare complete passive binding/global relationships while explicitly rooting semantic
observations in user/reached types and public queries; do not force all unrelated library
members merely to enumerate a graph. Record query order and genuine lazy states.
Observe actual inference/instantiation/tuple/mapped/conditional/index/cache representations
and key relationships needed by the selected paths. Unknown reached payloads or keys
must fail discovery; do not collapse them to counts or opaque success. Document untouched
or omitted states and review their producer paths before acceptance.

Inventory applicable original upstream tests and preserve executable assertions in
xUnit with provenance. Distinguish original assertions, adaptations, reused compiler
inputs and new differential tests. No binder/parser-only reuse is a full compiler-test
pass. Preserve prior fixture/proof bytes and all accepted regression gates.

## Acceptance and execution

| Row | Required evidence |
| --- | --- |
| G1 genuine environment | Exact complete library hashes/loading/order, authentic global merging and ownership, explicit skip policy, zero missing globals in valid demo |
| G2 generics/inference | Declarations/constraints/defaults, explicit/inferred calls, contextual/recursive/overload success and failures, real models and caches |
| G3 arrays/tuples/APIs | Mutable/readonly/evolving arrays, concrete tuples, all selected ES5 instance methods, rest/spread, index and relationship failures |
| G4 keyed/utility types | Actual mapped/indexed/conditional/infer/reverse-mapped behavior, documented utility aliases and recursive/composite interactions |
| G5 bodies/demo | Original loop/assignment flow plus realistic valid demonstration and independent invalid variants with complete diagnostics |
| G6 defensible delivery | Independent correctness/omission/library/cache review; xUnit; actual Linux x64 Native AOT for targeted and demo checks; all retained regressions; committed clean delivery |

Reuse three explicit GPT-6.1 Sol owners. Core owns Checker/Utilities/GrammarChecks/
Exports and the new direct inference.go counterpart, plus sole central library builds.
Support owns Types/Mapper/Relater, associated diagnostic/pseudo builders and printers;
Checker-origin merge/support candidates go through core, preserving one file counterpart.
Verification owns Flow/admission, authored fixed-library loading, independent oracles,
observers, corpora/demo/tests and gate extensions. Coordinate API/model signatures before
parallel implementation. Root owns contract, substantial decisions, mapping/state,
acceptance and commits. Preserve direct source mapping and notices throughout.

Internal milestones: authentic library/global plus generic-declaration environment;
array/tuple/call inference and relations; keyed utilities and loop integration;
independent review/coverage closure; frozen native acceptance and delivery. Use early
small managed probes and complete helper families, then affected regressions. Do not
repeat broad accepted-foundation audits without a concrete new discrepancy. Review
reachable guards/cache producers before the full gate, unlike the prior premature
freeze. Prior 24 builds are activity, not automatically waste; record actual counts
and time, never unavailable token/cost estimates. Do not build a process framework.

Continue autonomously until G1–G6 are satisfied or a material boundary/access dependency
requires a user decision. Expanding into broad library/project/module/class/emit work
requires reporting the concrete pressure first. Internal milestones do not end the
campaign. Report contract completion separately from exhaustive correctness or full
compiler parity. Only Linux x64 Native AOT acceptance is authorized to be claimed.
