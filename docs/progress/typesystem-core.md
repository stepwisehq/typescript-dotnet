# Type-system core checkpoint

Baseline `09630d6`; pin `fed0bf24149fb1ed36039212648bafdafc1ea10e`.
GPT-6.1 Sol core owns live [Checker](../../src/TypeScript.Scanner/Checker/Checker.cs),
[Types](../../src/TypeScript.Scanner/Checker/Types.cs) and central library/CLI builds.
The fixed [T1–T6 contract](../TYPESYSTEM-CONTRACT.md) is active. Support owns inference,
relation and agreed display/casing/utility files; verification owns expectations,
consumers and final gate. No commit. Accepted foundations and historical proofs are
unchanged. Private storage equality remains a diagnostic aid, not acceptance criterion.

## Source responsibilities

The [finite inventory](typesystem-core-inventory.json) records original entrypoints,
implementation, review and runtime qualification separately. Method presence was used
only for triage. Core compared surrounding producers, flags, diagnostics, mapper and
recursion/circularity handling across generic/alias/signature instantiation; constraints
and defaults; union/intersection/reduction; keyof/indexed access; mapped/remapped types;
conditional distribution/infer/tail recursion; templates/intrinsics; tuples/variadic
normalization/rest; predicate/assertion production. Existing solvers were retained.

The first packet added the actual StringMapping payload, all mapping producers and
cache, alias/instantiate/base-constraint arms, and deferred variance validation. It uses
support's pinned JS casing algorithms/data, not framework casing. All 2,927 mapping
entries and 147+377 ranges match the original; all eight algorithms were independently
reviewed. The original casing test body passed its 26 assertions.

Further source reconciliation restored generic-tuple base-constraint admission,
pattern-template intersection reduction, nonstrict nullable union elision/widening,
source nil-slice constructor semantics, Never-preserving filter and everyType handling.
The shared constructor-call arity helper now checks the genuine global Promise symbol,
so ordinary callback constructors do not hit an unrelated unsupported branch. All ten
resolution-property selectors and eight object payload selectors are represented;
invalid selectors retain source invariant failures. Support reviewed these changes.

Core independently reviewed peer intrinsic inference/relation/display, preferred
object-literal recovery, canonical generic signature, mapped/string-index comparison,
private conflicting-intersection explanation and variance markers. Full selected peer inference/relation review covered recursion/error publication, structural,
predicate/signature, reverse/contextual/priority inference and supporting displays.
No remaining selected algorithm omission was found. Actual NoErrorTruncation display
closure is now source-restored and independently reviewed, compiled in snapshot4. Passing selected records alone does not establish
those responsibilities.

## Current immutable producer/evidence

| Snapshot | Library wall seconds | CLI wall seconds | Outcome |
| --- | ---: | ---: | --- |
| 1 | 9.74 | 2.34 | Both clean, zero warnings/errors |
| 2 | 9.28 | 2.22 | Both clean, zero warnings/errors |
| 3 | 9.39 | 0.99 | Both clean, zero warnings/errors |
| 4 | 3.89 | 1.11 | Both clean, zero warnings/errors |

Four library and four CLI invocations total **38.96 seconds** (library32.30, CLI6.66).
Logs/timings and immutable copies are under `artifacts/typesystem-validation/build1–4`.
Current snapshot4 DLL SHA is
`e52022b57c864d624901e8ec72d723f7664665d6638e4dfa0fd98f6896d125d3`;
its ordinary CLI copy contains identical library bytes. No pending known producer delta.

Snapshot1 passed actual original casing/checker bodies and complete public relation/demo
records. Snapshot2 passed all eight targeted complete records, including the previously
mismatched variance diagnostic display. Snapshot3 passed arity representatives and its first full77 replay matched76 records.
The sole mismatch is genuine NoErrorTruncation diagnostic display in a recursive mapped
type compiler input; support restored the exact printer/builder option, source finite limits and elision
branches. Snapshot4 fixed that complete record and passed **all77/77** complete records (43 authored
source families, 2 disk examples, 32 reused compiler inputs), with unchanged options and
expected bytes. The failed snapshot3 artifacts remain preserved. No native/full retained acceptance yet and
no claim that every listed function was exercised.

## Actual boundaries / next work

Async/generator/JS/JSDoc/JSX/decorator and broader module/emit/service/tracing consumers
remain external as contracted. Their reused shared type algorithms are selected.
Historical named generic-rest/remapping/profile admission is separate from genuine
Program semantics. No permissive substitute, replacement solver or private-state
instrumentation was added. Shared advanced TS type algorithms have no blanket external disposition.
Source/review records are now stable for freeze. Await coordinator-authorized final
gate and runtime qualification, classify any
concrete broad-replay findings together, then coordinate one final frozen native/retained
gate with verification. Do not launch duplicate broad suites or rewrite old evidence.
