# Bounded semantic checker verification

The **first bounded semantic milestone passed** on 2026-10-08, against unchanged
upstream `fed0bf24149fb1ed36039212648bafdafc1ea10e`. The actual Native AOT executable
passed the comparisons below. [Durable evidence](../validation/results/checker-verified.json)
records source, verification-asset, corpus and native hashes. This establishes the
selected contract, not full checker parity or a complete library environment.

## Results

| Gate | Passed |
| --- | ---: |
| Native checker semantic cases / queries / assignability pairs | 45 / 274 / 121 |
| Native admission controls (33 rejected, two admitted) | 35 |
| Actual Go/native cache and table observations | 20 |
| Authored native public-API assertions | 10 |
| Checker / binder / parser / scanner xUnit | 82 / 122 / 475 / 1,287 |
| Native binder / parser / scanner cases | 118 / 471 / 1,276 |
| Native scanner helper outputs | 178 |

All checks passed with zero observed discrepancies. The stable combined gate
started at 18:31:08 UTC and took 47.701 seconds; source and verification assets
remained unchanged. These are verification timings, not a performance comparison.

The first gate passed runtime/native checks but was correctly rejected by its asset
guard: a metadata-only inventory-tool edit landed during execution. Executable source
was unchanged. The full gate was repeated after all writers stopped; the rejected
run and reason remain in the evidence instead of being reported as acceptance.

## Reproduction

Use the Linux x64 prerequisites in [README](../README.md): .NET SDK 10.0.401,
Git, Python 3, curl and native compiler/linker dependencies. From the repository root:

```sh
./tools/verify-checker.sh
```

The gate rebuilds the actual pinned Go checker oracle, compares fresh Go output
with stored expectations, checks direct Go utility observations, runs checker
xUnit, publishes the checker harness with Native AOT and executes semantic,
admission, utility and API verification. It then runs the complete binder gate,
including parser/scanner xUnit and native regressions. An installed clang is used
when available; the already prepared local GCC fallback remains supported.
A fresh machine can install clang and zlib development packages as in README.
Initial setup requires network access for pinned upstream, Go and NuGet packages.

To time the full gate, guard source and verification assets, and save durable evidence:

```sh
python3 tools/run-checker-gate.py
python3 tools/record-checker-evidence.py
```

For a managed development loop only:

```sh
dotnet test tests/Checker.Tests/Checker.Tests.csproj -c Release
```

Managed success or native publication alone is insufficient. Never modify expected
results to accommodate the C# implementation. Normal verification compares fresh
Go output without rewriting the baseline. Prior scanner/parser/binder evidence
records stay historical and unchanged; current regression results belong to the
checker record. Logs and native binaries are rebuildable ignored artifacts.

## Supported input and outcome contract

[Checker.Create](../src/TypeScript.Scanner/Checker/Admission.cs) accepts a parsed
TypeScript script and binds it through the existing binder before selected checker
initialization. A caller starts with the existing parser; the executable harness
exercises the complete source-text path. The tree must remain immutable during use.
The checker instance has no general concurrent-use or cancellation API.

The fixed profile is one TS script, ESNext, strictNullChecks enabled,
noImplicitAny and strictBindCallApply disabled, noLib/noResolve, and no external
files. There are no standard-library declarations. Actual missing-global-type
errors remain visible through GetGlobalDiagnostics; GetDiagnostics returns file
semantic errors. A completed local semantic check does **not** mean the source is
a complete, error-free standard-library program.

Admitted syntax includes var/let/const declarations, nested blocks, identifier
names, primitive keyword annotations, string/number/boolean/null literal annotations,
parenthesized types and literal/identifier/parenthesized initializers. Keyword
annotations include any, unknown, never, void, undefined, bigint and symbol.
Bigint values, unary expressions (including negative literal syntax), source-level
union syntax and other unselected expressions are excluded. Some internal boolean/
undefined unions are necessary for admitted scalar semantics and are ported.

The selected checks include declaration inference/widening, literal freshness,
annotation compatibility, primary/secondary declaration agreement, real name
resolution, shadowing, missing-name and spelling diagnostics, primitive names used
as values, built-in global-name conflicts, and straight-line initialization/forward/
self-reference flow. Queries expose types and resolved symbols; selected scalar
assignability is also compared in both successful and failing directions.

[Admission](../src/TypeScript.Scanner/Checker/Admission.cs) is authored support,
separate from the ported semantic algorithms. It rejects non-TS/declaration/module
files, parser errors, checking directives, external references, JSDoc tags, modifiers,
destructuring, using, definite-assignment assertions and unselected statements,
types or expressions. Plain documentation and local scalar shadowing of globalThis
are allowed. Reading the actual built-in globalThis resolves through upstream logic
and then rejects unsupported object semantics. Node queries reject foreign/orphan
nodes; type APIs reject another checker's types. Omitted semantic branches throw
UnsupportedSemanticFeatureException; they do not return a successful check.

General objects, functions/calls, aliases, generics, branches/loops/try, explicit
assignment expressions, modules, libraries, cross-file resolution, projects, emit,
incremental compilation and language services are outside this milestone. Invalid
**semantic** inputs are tested; malformed syntax is explicitly unsupported by this
checker slice, with parser recovery still verified by its separate regression gate.

## Independent comparison

The [Go oracle](../tools/checker-oracle/main.go) constructs a real in-memory
compiler.NewProgram, asserts one source file, and invokes the actual upstream
checker. No miniature Go checker or matching C# admission whitelist is used.
The [C# runner](../validation/Checker.Validation/Runner.cs) uses statically typed
fields and source-generated JSON, suitable for actual Native AOT.

The [coverage inventory](../validation/checker-corpus/coverage.json) describes
45 selected cases (nine tiny and 36 expanded), with 713 pre-check AST nodes,
205 symbols, 88 tables, 215 types, 274 queries and 121 assignability comparisons
(50 true, 71 false). It compares:

- Pre-check parser structure and separate parser, binder, file semantic and global
  diagnostic channels; exact diagnostic code/category/key, raw arguments, rendered
  text, file identity, byte spans, flags, ordered related information and chains.
- Resolved symbol/declaration relationships, raw names, complete represented symbol
  and CheckFlags values, parent/export links and nullable member/export tables.
- Canonical type identity, flags/object flags, public text, intrinsic names,
  exact literal bytes or IEEE numeric bits, ordered union constituents, literal
  fresh/regular links and represented alias/symbol references.
- Shared globals/globalThis.Exports table identity and explicit relation results.

Canonical identities replace addresses/allocation counters without merging distinct
objects. Table enumeration is sorted by raw keys. Literal fresh/regular cycles and
union order remain visible. There are 142 fresh and 142 regular literal edges,
ten union-member edges and 45 observed globals/export aliases.

The cases produce 31 file semantic diagnostics across nine codes: 1155, 2304,
2322, 2397, 2403, 2448, 2454, 2552 and 2693. Five related diagnostics are observed;
no semantic message chains are exercised. The separate binder channel includes
two 2451 errors. All cases retain eight real 2318 global diagnostics for Array,
Boolean, Function, IArguments, Number, Object, RegExp and String (360 total).
These are expected environment errors, not filtered results.

Separate checks assess the boundary without pretending Go rejects valid TypeScript:
35 authored admission controls (33 rejected, two admitted), ten public-API assertions,
and 20 actual-Go-versus-C# cache/table observations. The latter exercise page-level
Get/TryGet/Has behavior, zero slots, stable/distinct identities, the low-page/high-map
boundary at key 16,777,216, symbol nil slots and allocated empty lazy tables.
The actual native executable must pass these checks too.

## What remains unverified

This is selected semantic agreement, not full checker parity. The type graph covers
IntrinsicType, LiteralType and UnionType payloads. It does not serialize every
mutable cache field: union regular/origin caches are not fully compared. That was
the observation boundary from the initial oracle, not a field removed to hide a
failure. Literal fresh/regular links, returned type identities, flags, union members,
queries and relationships support the narrower claim.

The syntax snapshot precedes admission/checking; lazy documentation cache changes
are not serialized afterward. Binder symbol/flow graphs remain covered by the full
binder regression rather than duplicated wholesale in semantic JSON. Allocated
empty tables are covered by direct utility probes, not produced by these scalar
graph cases. Shared FlowList identities and branching flow are unexercised/excluded.
Full source-position mapping is still partial; the selected path uses verified byte
spans and required ECMA line helpers.

Historical gaps remain: scanner diagnostic 1513, 15 parser diagnostic codes listed
in [binder verification](./BINDER-VERIFICATION.md), and three binder codes in an
unused helper. No unrelated grammar reopening was needed. Other operating systems
and architectures have not been verified.

## Frozen evidence

- Executable source SHA256: `ac38adb32995090a01777c010452f0405aed99e768a370c0178febc196cb940e`.
- Verification assets SHA256: `c9f669c200dab74c04f908780cdff2ad95ff44ff655bcef034f24d8a52897824`.
- Checker native SHA256: `d886b2e58750043164efa856c984e1e79451967626b4bf669a1a2ee348ecca62`.

The checker executable is a stripped Linux x64 ELF, 5,234,456 bytes, depending on
libc, libm and the system loader, with no installed .NET runtime dependency. Go
1.27.1 and .NET SDK 10.0.401 were used. The durable record defines hash inputs and
includes all regression binaries/results. Independent
[algorithm review](./progress/review-checker.md) and
[support/harness review](./progress/review-checker-support.md) have no unresolved
blocking findings.
