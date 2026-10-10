# Shared TypeScript type-system responsibility inventory

Baseline `09630d6`, unchanged upstream `fed0bf24149fb1ed36039212648bafdafc1ea10e`.
This is the current T1–T6 reconciliation index. It supersedes the historical
[callable-era function catalog](checker-functions.json) for these responsibilities,
without rewriting that historical evidence. The [contract](TYPESYSTEM-CONTRACT.md)
defines scope, and the [assessment](TYPESYSTEM-ASSESSMENT.md) controls runtime acceptance.

## Implementation and independent source review

The [core inventory](progress/typesystem-core-inventory.json) names pinned entry points,
source locations, counterparts, concrete repairs, implementation/review state, current
counterpart hashes and the central build ledger. It reconciles intrinsic mapping;
generic object/alias/signature instantiation; constraints/defaults and variance;
union/intersection reduction; keyof/indexed access; mapped/key-remapped types;
conditional/infer; template construction; tuples/variadics; predicates/assertions
and required signature/call consumers. Families share helpers, so adding their function
counts does not yield a unique function count or percentage of the checker.

The [support inventory](progress/typesystem-support-inventory.json) accounts for all
79 pinned inference functions and all 188 relation definitions, with 185 relation
functions selected for shared TypeScript behavior. Definition matching is separate
from actual branch review. Shared TypeScript branches are reconciled; JS/JSX-only
branches in mixed functions remain external. The three excluded relation definitions
are `isHyphenatedJsxName`, `isIgnoredJsxProperty` and enabled-tracing-only
`traceUnionsOrIntersectionsTooLarge`. Semantic recursion/complexity diagnostics remain
in scope despite the tracing exclusion.

Required type display and options are source responsibilities, even where the files
also support emit. The NoErrorTruncation repair includes original type-list/object/
cyclic display and the small writer/emission-context helpers it demands, without
implementing compiler emit. Whole checker/printer counterparts remain partial.

Two new direct counterparts complete the demanded casing dependency:
[js_case.go](../upstream/TypeScript/tsc/internal/stringutil/js_case.go) maps to
[JsCase.cs](../src/TypeScript.Scanner/StringUtil/JsCase.cs), and
[js_case_generated.go](../upstream/TypeScript/tsc/internal/stringutil/js_case_generated.go)
maps to [JsCaseGenerated.cs](../src/TypeScript.Scanner/StringUtil/JsCaseGenerated.cs).
All eight algorithm functions, 2,927 mappings, 147 Cased ranges and 377 Case_Ignorable
ranges were source-compared. The original casing test body retains all 26 assertions.

No known shared advanced-type algorithm is excluded from T1–T5. Historical named
Collections/Projects admission guards are test adapters, not the genuine Program
language boundary. Broad private-state equivalence is not an implementation obligation.

## Verification coverage and remaining checker obligations

Source review does not establish that every branch executed. See the separate
[test dispositions](typesystem-tests.json) and runtime proof linked from the assessment
for finite public type/relationship, diagnostic, original assertion and compiler-input
coverage. New test totals are not an implementation-completeness measure. Source-reviewed
nil/empty type-list construction has no separately identified new public trigger;
no private instrumentation is added merely to create one. Recursive/complexity,
Unicode, options and feature interactions have finite witnesses, not exhaustive coverage.

The next missing checker responsibilities are actual async/await/generator/yield/
for-await/disposal protocol execution; JS/JSDoc/JSX/decorator semantics; broader module
interoperability/import attributes and JSON-specific checking; and the final whole-
checker reconciliation of remaining no-emit semantic/display/utility paths. Their
shared type machinery is completed within this contract; implementing those consumers
may still expose defects or evidence gaps. Emit, watch/build scheduling, services and
enabled tracing remain outside no-emit checking. The [roadmap](CHECKER-COMPLETION-ROADMAP.md)
defines the eventual all-source-kind/options/library no-emit gate. Large-project
comparison and optimization remain deferred until that gate passes.
