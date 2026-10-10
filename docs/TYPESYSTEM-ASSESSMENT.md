# Remaining type-system completion assessment

**Accepted: shared TypeScript type-system contract T1–T6.**
Baseline `09630d6`;
upstream `fed0bf24149fb1ed36039212648bafdafc1ea10e` unchanged. The
[fixed contract](TYPESYSTEM-CONTRACT.md) defines T1–T6. Follow the
[checkpoint](TYPESYSTEM-STATUS.md) for the current executable evidence and next action.
The complete frozen gate passed on actual Linux x64 Native AOT. The
[durable proof](../validation/results/typesystem-verified.json) binds the verified source
and assets; this is shared TypeScript type-system contract completion, not whole-checker parity.

## Scope and implementation reconciliation

This campaign reconciles shared TypeScript generic construction/instantiation/constraints,
variance, union/intersection/indexed/mapped/conditional/template/intrinsic operators,
inference, relationships and tuple/callable/predicate consumers against upstream.
Existing algorithms are retained and completed; no miniature checker or new solver is
substituted. Source authorities and function-family status are recorded in the
[core inventory](progress/typesystem-core-inventory.json) and
[support inventory](progress/typesystem-support-inventory.json). Name presence,
source-branch review and runtime evidence are distinct states. Historical callable-era
function inventories are not current completeness authority.

Initial missing functionality included the entire StringMappingType producer/model,
alias/instantiation/constraint consumers, relation/inference/display branches and
explicit variance validation. The required JS casing dependency now follows the
original algorithms and pinned generated data. Further source reconciliation found
missing generic target-signature canonicalization, a generic mapped/string-index
relation, private-conflicting intersection diagnostics, generic tuple constraints,
pattern-template intersection reduction and non-strict nullable/widening behavior.
These are real source omissions; tests and expectations are not altered to hide them.
Source-family reconciliation and independent source review are complete with no known
remaining in-scope omission. Actual Native AOT and all retained acceptance gates pass.

One Promise-labeled diagnostic guard was reachable from callbacks passed to ordinary
constructors. Restoring upstream constructor-symbol discrimination is shared
synchronous call checking, not an async implementation. Other residual type-resolution
and object-payload defaults are upstream invariant failures after complete represented
selectors, not unsupported semantic features. Historical named-profile restrictions
remain explicitly test adapters; ordinary Program follows its actual options/libraries.

## Dependencies and adaptations

Original [casing algorithms](../src/TypeScript.Scanner/StringUtil/JsCase.cs) and
[pinned generated data](../src/TypeScript.Scanner/StringUtil/JsCaseGenerated.cs) map
directly to upstream files. All 2,927 mappings, 147 Cased ranges and 377 Case_Ignorable
ranges were compared to the pinned source. Raw Go-byte strings and JS surrogate sentinels
reuse existing compatibility helpers. Expansions and final-sigma context require the
upstream implementation; .NET default casing would change behavior. No framework
upgrade, network data regeneration or speculative optimization is involved.

Ordinary .NET dictionaries/lists/builders remain appropriate storage facilities,
with missing-key/nil-slice semantics preserved at original algorithm boundaries.
The required display dependency preserves actual NoErrorTruncation flags, limits and
upstream cyclic/list/object formatting. Its small writer/context additions serve
semantic diagnostics and do not implement JavaScript or declaration emit.
All production changes remain in direct source counterparts; harnesses are authored
verification support. Private-state equality is diagnostic, not an independent gate.

## Contract-based acceptance

The following source responsibilities and acceptance evidence pass, including actual
Linux x64 Native AOT and retained regressions for every row. Domain witnesses are
listed individually in the
[test catalog](typesystem-tests.json), independently of the implementation inventories.

| Contract | Implementation and independent review | Finite behavioral evidence |
| --- | --- | --- |
| T1 | Generic models/instantiation/defaults/constraints, recursion and variance reconciled with original producers and consumers. | Dependent/circular constraints, invalid defaults, variance, generic alias instantiation and depth diagnostics. |
| T2 | Union/intersection, index/keyof, mapped/remapped, conditional/infer, template/intrinsic construction and reductions complete within the shared TS boundary. | Key filtering/collisions/unique-symbol keys, modifiers, constrained/distributive inference, recursive aliases, template complexity, nested intrinsic membership and non-strict null behavior. |
| T3 | All 79 inference definitions reconciled for shared synchronous TS branches; contextual/callback, priority, constraint, reverse-mapped and variadic inference preserved. | Generic callbacks/returns/overloads, contravariant candidates, NoInfer, template placeholders, mapped callbacks and tuple/rest inference. |
| T4 | 185 of 188 relation definitions selected; shared TS branches reviewed. Only two JSX definitions and enabled-tracing helper excluded, with mixed JS-only branches explicit. | 42 direct public assignability pairs plus complete invalid signature/variance/private-origin/mapped/index/template/tuple diagnostics and finite order checks. |
| T5 | Tuple normalization/constraints, signature expansion/canonicalization, predicate/assertion consumers and demanded type-display options completed. | Generic variadic calls and errors, readonly/rest relationships, inferred/explicit predicates with flow, constructor callback arity, untruncated recursive mapped diagnostics. |
| T6 | Original casing algorithm/data and all 26 original assertions preserved; source mapping, original assertions and authored evidence separated. | 77 complete managed/native records, 84 new xUnit cases, seven shared checks and genuine strict ES2022 disk projects; complete retained cascade passes. |

## Verification evidence and limitations

Managed and actual Linux x64 Native AOT both pass all 77 complete records, seven shared
checks in four dispatcher groups, and 22 reversed/repeated-order executions each.
All 84 new xUnit cases pass. The shared checks preserve two original checker bodies,
three restored mapper bodies and the newly ported casing body (26 table assertions),
plus one authored external-consumer control. Five retained originals are not new ports.

Both ordinary native disk invocations match Go. Valid exits 0 with no diagnostics;
invalid exits 1 with all six semantic diagnostics (2820, 2322, 2322, 2739, 2353, 2322)
and no missing globals. The example combines remapped handler names, intrinsic/template
keys, conditional payloads, generic variadic dispatch and assertion narrowing across
three files with genuine strict ES2022/Bundler options and unchanged libraries;
`skipLibCheck` remains false. It establishes useful integration, not the scope boundary.
See the [reproduction guide](TYPESYSTEM-VERIFY.md) for exact user commands.

Frozen corpus: 43 authored source-family records, two disk projects and 32
reused compiler inputs, including 42 finite public assignability observations. The
selected upstream inventory contains 35 whole inputs: 32 reused and three excluded.
These are finite observations, not an exhaustive type-system test inventory. Selected inputs
are not full compiler-test passes or an exhaustive upstream corpus inventory. Original
checker and restored mapper assertion bodies remain reused; the casing body is newly
ported, with all its assertions preserved. Explicit external JSX/async/dynamic-import fixtures
remain whole-input exclusions, without source deletion or option weakening.

The optional relationship query directly calls public IsTypeAssignableTo on both
checkers and records the chosen source/target public types. Existing absent-field
outputs remain unchanged; malformed query selectors are harness errors, not successful
checking. No private cache/identity/storage census is added. Complete diagnostics,
inferred/narrowed observations and independent source review remain essential.
The complete 77-record managed and native corpus matches pinned Go. The one initial broad
discrepancy was diagnostic truncation under genuine NoErrorTruncation:true; restoring
source option/flag/list/object/cyclic display and bounded writer dependencies fixed it
without changing input, options or expectations. All retained regression gates also pass.
The selected TRX aggregate is **24,849 passed
case executions, zero failures/skips**; it is not a unique-test inventory or a count of
every inherited repeated run. All five drift guards are clear and all 256 protected
historical fixture/proof files remain byte-identical. Native evidence includes 21
actual Linux x64 ELF executable paths across new and retained layers; other native
platforms remain unverified. See the [current inventory](TYPESYSTEM-INVENTORY.md) for source
closure versus finite verification and remaining checker obligations.

## Execution strategy and remaining checker work

Three reused GPT-6.1 Sol agents have disjoint implementation/review and verification
ownership; core performs central library builds and verifier consumes immutable
snapshots. Original references and independent source review proceed alongside
implementation. Focused representatives run before broad replay, which waits for a
coherent source packet. Measured build/gate effort is recorded below; no token or
monetary measurements are available. Source inventories were frozen before the gate and retain their source-stage
status; final runtime status comes from the proof and test catalog, not rewritten inventories.

The preceding 1,398.790-second gate repeated Checker831 three times and published the
same ordinary CLI into multiple destinations. This campaign avoids adding another
full early suite on top of the retained predecessor; the inherited execution sequence and assertions remain
unchanged. More elaborate same-run receipt/binary reuse was deferred because its
plumbing would add unproven complexity. The slow broad binding tail is meaningful
retained coverage, not evidence of a duplicate to remove.

Async/generator/disposal execution, JS/JSDoc/JSX/decorators, broader module/import-
attribute/JSON consumers and whole-checker reconciliation remain later responsibilities.
Shared type algorithms are included here even when those later consumers also use
them. Emit/watch/build/services and enabled tracing are external. Linux x64 Native AOT
is the required platform; other platforms are not claimed. Large-project correctness
and performance comparisons remain deferred until the whole-checker no-emit gate.

A concrete preflight blocker required a narrow change to the initial no-old-tool-edits
plan: the declaration observer provenance pins whole source-file hashes. The new
optional public assignability query changes those files while preserving absent-field
records. The old guard may recognize only the exact original observer version or an
explicitly reviewed additive extension whose new provenance binds the same original
source set/hashes and exact new hashes. Original requests/expectations/provenance/proofs
remain immutable; no generic hash bypass, duplicate companion observer or temporary
source mutation is allowed. Independent negative controls and all retained behavioral
comparisons pass. This is a concrete compatibility fix, not a redesign or
relaxation of diagnostic/property comparison. Duplicate-suite scripts remain unchanged.

## Observed effort

Four coherent library builds and four CLI builds all succeeded: 32.30 and 6.66 seconds
respectively, 38.96 seconds total. Builds were centralized; agents consumed snapshots
rather than each rebuilding the production library. One full 77-record managed replay
found a real NoErrorTruncation display discrepancy; candidate 4 repairs it and passes
all records. Focused source-linked witnesses caught missing branches before the freeze.

The first final gate passed the new managed checks but stopped after 65.547 seconds
on a report-reader field mismatch, before retained regressions or Native AOT. All five
drift flags were false. The independently reviewed repair reads the producer's actual
Go/target execution counts against the frozen 22-execution obligation and preserves
the per-sequence, failure, exit and binary checks; it also checks the request hash.
No input, expected behavior or source implementation changed. This was verification
plumbing overhead, distinct from the actual source/display repairs above. The corrected
full gate passed in **1,492.728 seconds** (about 24.9 minutes), with all
five drift flags false. Unavailable token/cost or total model effort measurements are
not inferred from elapsed time. No additional user decision
or package installation was needed. Reusing three agents with disjoint writers and
cross-review reduced context setup; source reconciliation exposed genuine omissions
before runtime acceptance. The report-field failure shows that orchestration interface
checks still need care. Broad private-state observation or a new process framework
would not have addressed that issue.

## Downstream assumptions and next campaign

Subsequent work can use the source-reconciled shared generic/advanced-type/inference/
relationship and callable/tuple machinery through genuine Program environments. Former
intrinsic-mapping and explicit-variance restrictions and the recorded recovery/display
shortcuts are replaced by original algorithms. Successful environment construction
still does not imply support for an external semantic consumer. Whole checker/printer
files remain partial, and future consumers can expose defects or untested combinations.

Recommend async, generators and runtime protocol checking next: actual await/yield/
for-await/using consumers, return/yield inference, genuine promise/iterator/disposal
libraries, and restoration of the original mapper generator test's remaining semantic/
incremental assertions. JS/JSDoc/JSX/decorators and remaining module/options consumers
follow, then responsibility-level whole-checker no-emit reconciliation. The refreshed
[roadmap](CHECKER-COMPLETION-ROADMAP.md) defines that gate. No percentage estimate of
remaining checker work, whole-library API claim or large-project comparison is inferred
from this campaign.
