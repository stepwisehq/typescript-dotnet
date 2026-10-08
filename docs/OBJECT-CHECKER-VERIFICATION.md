# Bounded object checker verification

**Accepted bounded milestone on 2026-10-08.** The actual Linux x64 Native AOT
executable agrees with pinned Go on the selected object contract below. The
[durable evidence](../validation/results/object-checker-verified.json) records the
source, verification assets, fixtures and executed binary. This is not full checker
parity or a complete standard-library environment.

## Results

| Check | Passed |
| --- | ---: |
| Native object cases / queries / assignability pairs | 43 / 398 / 705 |
| Native historical scalar cases | 45 |
| Current admission controls (56 rejected, two admitted) | 58 |
| Actual Go/native relation-key equality observations | 258 |
| Actual Go/native cache/table utility observations | 20 |
| Authored native public-API assertions | 10 |
| Checker / binder / parser / scanner xUnit | 149 / 122 / 475 / 1,287 |
| Native binder / parser / scanner cases | 118 / 471 / 1,276 |
| Native scanner helper outputs | 178 |

All 2,033 xUnit tests and native comparisons passed. The final guarded gate started
at 19:57:00 UTC and completed in 45.201 seconds with exit zero and unchanged
source/assets. An earlier successful 70.14-second gate was repeated after removing
one accidental extra EOF newline; its proof and the reason are retained in the
durable evidence. The same final checker native binary was used for object, scalar
and boundary verification. These timings describe verification, not compiler performance.

The object cases contain 885 AST nodes, 363 canonical types, 475 symbols, 292 tables,
27 file diagnostics and 344 separately identified global diagnostics. Of the 705
relation results, 485 are true and 220 false. Scalar baseline bytes and all previous
durable acceptance records remain unchanged.

## Reproduction

Use the Linux x64 prerequisites in [README](../README.md): .NET SDK 10.0.401, Git,
Python 3, curl and native compiler/linker dependencies. The scripts bootstrap pinned
Go 1.27.1. An installed clang is supported; this environment also has the previously
prepared GCC fallback. From the repository root:

```sh
./tools/verify-object-checker.sh
```

This builds the actual pinned Go oracle, compares fresh expectations for both corpora,
checks original utility observations, runs xUnit, publishes Native AOT, executes all
object/scalar/boundary checks and runs the complete binder/parser/scanner gates.
Initial setup needs network access for upstream, Go and NuGet. No library declarations
or external source dependencies are loaded by the semantic specimens.

To capture timing and guard source/assets before recording durable evidence:

```sh
python3 tools/run-object-checker-gate.py
python3 tools/record-object-checker-evidence.py
```

For managed development only:

```sh
dotnet test tests/Checker.Tests/Checker.Tests.csproj -c Release
```

Stop all source/tool/corpus writers before an evidence run, including inventory
generators. Native publication alone is insufficient: the resulting executable
must pass the comparisons. Normal verification does not rewrite expected results.
The earlier scalar-only gate asserts its historical admission boundary and should
be reproduced at scalar commit `7e692dd`; use the object gate on current source.

## Supported contract

The fixed profile remains one TS script, ESNext, strictNullChecks enabled,
noImplicitAny and strictBindCallApply disabled, no exactOptionalPropertyTypes,
noLib/noResolve. The source tree must remain immutable to callers while its checker
is used. There is no general concurrent-use or cancellation API.

The [admission adapter](../src/TypeScript.Scanner/Checker/Admission.cs) retains the
scalar grammar and adds anonymous object literals, inline type literals, nested
data properties, optional/readonly property signatures, identifier/string/numeric
names, shorthand and ordinary dot reads. Property signatures require explicit type
annotations. Structural relationships are exercised through initialization and the
public relation API. Repeated public widening and property/type/symbol queries also
use ported upstream methods.

Semantic failures include excess properties, missing required members, optional-to-
required incompatibility, weak-type mismatches, nested incompatible property types,
missing/misspelled properties and shorthand names, duplicate properties/modifiers,
possibly-undefined reads, forward/self-reference and definite-assignment cases.
Readonly-to-mutable structural compatibility follows Go. The
[manifest](../validation/object-checker-corpus/manifest.json) and
[coverage inventory](../validation/object-checker-corpus/coverage.json) identify the
eight integration cases and 35 interaction/review cases.

Named aliases/interfaces/type references, the standalone `object` annotation keyword,
generics, calls/signatures, index signatures, arrays, element/optional-chain access,
computed expressions/spreads/methods, explicit writes, branching flow and source
unions/intersections/keyof remain excluded. Internal nullable unions and finite key
reductions needed by the selected paths do not imply support for that source syntax.
Modules, libraries, projects, general emit and services remain excluded. Parser
errors, checking directives and attached JSDoc tags are rejected explicitly.

Unsupported source or runtime branches raise UnsupportedSemanticFeatureException,
not a successful semantic result. Reading the actual builtin globalThis value remains
unsupported, determined by symbol identity; local scalar shadows remain admitted.
Foreign/orphan node and foreign-checker type guards are retained.

## What is compared

The oracle invokes the real pinned Go Program/checker. Its
[private-store observer](../tools/checker-oracle/object_access.go) is authored read-only
instrumentation copied as an untracked file into the original checker package;
tracked upstream source and behavior are unchanged. The
[C# observer](../validation/Checker.Validation/ObjectGraph.cs) uses static typed access
and source-generated JSON, with no reflection dependency in the native harness.

Both sides compare pre-check syntax plus the checked diagnostic/symbol/type graph,
including:

- Ordered public and raw properties, property types, declaration symbols, modifiers,
  optional/readonly flags, transient symbols and target links.
- Member-table identity, null versus allocated-empty storage, aliases to binder
  member tables and genuine distinct copies.
- Type flags/object flags, literal freshness, query identities, public formatting,
  repeated widening identity and existing selected cache entries.
- Full file/global/binder diagnostic channels: code/category/key, raw arguments,
  rendered text, byte spans, file identity, metadata, related information and chains.
- Real directed assignability outcomes and relation-key equality/state/order behavior.

Addresses and allocation counters are canonicalized without merging distinct objects.
The documented observation order is identical: diagnostics, queries/base graph,
relations, global snapshot, key probes, widening, property/storage discovery, link
work queue, existing caches and final flags. Cache reads do not force regular-object
cache population. Excluded nonempty signature/index/mapper payloads are asserted
absent instead of silently discarded.

There are 179 structured-type records, 229 property/type edges, 122 target links and
190 transient/declaration-symbol differences. Table observations include 15 allocated
empty tables and 169 binder/structured-table comparisons: 35 non-null aliases, two
null/null matches and 132 distinct pairs. All 43 globals tables alias globalThis exports.
All 46 widening probes are stable on repetition and produce distinct widened identities;
53 existing widening-cache edges are observed.

Top-level file diagnostic codes are 1030, 1117, 18004, 18048, 2300, 2322, 2339,
2353, 2448, 2454, 2551, 2559, 2717, 2739 and 2741. Nested and related diagnostics
are recorded separately in the inventory. Each case also retains the actual eight
2318 errors for Array, Boolean, Function, IArguments, Number, Object, RegExp and String.
No diagnostic category is broadly suppressed to obtain agreement.

The existing nested-chain case also has source-path evidence for actual nonnil
enclosing-declaration/pseudochecker reuse in diagnostic serialization. Its native
diagnostic and type text agree with Go. This is not an instrumented branch count.

Two old admission negatives (`object`, `object-type`) intentionally become supported.
Their historical fixture bytes stay intact; current test selection retires exactly
those two and adds positive object coverage. The 58 current controls comprise 33
still-applicable historical controls and 25 new controls. They are authored boundary
assertions, distinct from Go semantic agreement.

## Limits and frozen identity

RegularObjectLiteral cache entries, shared member tables across distinct object
records, ObjectType target edges and property write/name/containing-type edges are
unhit. Comparing their absence is not evidence of their nonzero behavior. Private
union-property/context/intersection caches and union regular/origin fields are not
fully serialized. Syntax is captured before checking; this is not a full post-check
AST/cache comparison. Binder flow graphs retain their separate regression gate.

Shared FlowList identities and full source-position mapping remain unverified for
this slice; byte positions suffice for selected diagnostics. Historical scanner
1513, the 15 remaining parser codes and the unused binder diagnostic-helper gaps
remain listed in prior verification reports. Malformed semantic recovery and other
OS/architecture targets remain excluded. No unresolved observed discrepancy remains
within the accepted contract.

- Executable source SHA256: `1ce0c30edf2f470ee4a7c527f336980e5a80d8afc17108774c8764d99d6c15b9`.
- Verification assets SHA256: `d27278a4a82192fc3b13fc54f2f90c7914c068b3628828e31723527f3ac2e30d`.
- Checker native SHA256: `f66e0c539ef8b237fbcfa4974e183eaf36e3e0bf1d9b7b1661a71ec36062eab8`.

The executed checker is a stripped Linux x64 ELF, 5,682,440 bytes, with native
system-library dependencies and no installed .NET runtime requirement. Independent
[main checker review](./progress/review-object-checker.md) and
[support/formatter/harness review](./progress/review-object-checker-support.md)
have no unresolved blocking findings.
