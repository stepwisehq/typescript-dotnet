# Data-type campaign support checkpoint

Owner: support. Bounded Linux x64 Native AOT acceptance passed; no active work.
[Final evidence](../../validation/results/data-types-verified.json).
Baseline: `1f7c953`; upstream `fed0bf24149fb1ed36039212648bafdafc1ea10e`.
Scope is [the selected contract](../DATA-TYPES-CONTRACT.md); no narrowing.

Owned: [Types](../../src/TypeScript.Scanner/Checker/Types.cs),
[Links](../../src/TypeScript.Scanner/Checker/Links.cs),
[Relater](../../src/TypeScript.Scanner/Checker/Relater.cs), Checker NodeBuilder,
NodeCopy/PseudoTypeNodeBuilder/SymbolTracker/EmitSupport/Printer counterparts,
[SymbolAccessibility](../../src/TypeScript.Scanner/Checker/SymbolAccessibility.cs),
selected EmitResolver/NodeBuilder flag counterparts,
[Pseudochecker](../../src/TypeScript.Scanner/Pseudochecker/) and
[Printer](../../src/TypeScript.Scanner/Printer/), required Diagnostics generation.
Core owner owns Checker/Flow/Utilities/Exports/GrammarChecks/Mapper and all builds.
Verifier owns admission/oracle/harness/tools/tests/corpus. No delegated child work.

Implemented: alias/interface/reference/hidden-this/mapper/cache identities; recursive
union/intersection/discriminant/synthetic-index relations; exact selected diagnostic
flags/context, symbol accessibility/tracking, pseudochecker reuse and printer syntax.
Actual node/type-node LinkStore fields restored; paged symbol stores remain distinct.
Finite-key IndexType origins, synthetic any-base indexes and globalThis module
export caches preserve genuine upstream identities. Source generics/callables/index
syntax/classes/modules/libraries/general emit remain explicit unsupported boundaries.

Final frozen gate passed in 82.979 seconds: 92 rich native cases
(14 tiny + 78 matrix), 43 anonymous-object/45 scalar native regressions, 78 controls,
20 Go utility observations and 10 API selfchecks, without expected-output changes.
All 2,146 xUnit tests and prior scanner/parser/binder native gates pass. Selected nil-source unresolved-type comments,
explicit-only diagnostic flags and genuine empty inference-context lookup included.
Independent [core review](./review-data-types-core.md) has no remaining selected-path
blocker. Core independent models/relations/formatter review also has no unresolved blocker.

Last support checks: LF syntax normalization of 22 owned sources succeeded;
`git diff --check` clean. Source-map validates 91 library C# files exactly once,
126 entries, 941 inventoried methods, every destination and pinned TypeScript origin hash. Historical accepted
hashes/scopes remain separate. [Departures](../DATA-TYPES-DEPARTURES.md) records
managed storage/key adaptations, omitted syntax/fields and coverage limits.

No pending source/tool/corpus or implementation work. All executable assets stay
frozen. Final source SHA256: ac9763058844d0ce1daa50fd8c6ba22748c083713592ebd7d2aabf81cfe7b928;
asset SHA256: 80052551e9d70e28315750ee041d4774e4ab48c31994cbbed8b2eb2aaacf47ce.
Next only if requested: parent documentation consistency review; no new phase inferred.
Handwritten ports are authoritative; no complete checker regeneration claim.
Depth/budget limits and all unhit internal generic/callable paths remain unverified.
