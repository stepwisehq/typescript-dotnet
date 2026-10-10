# Completing the checker

**Reviewed source-grounded roadmap from the accepted Program campaign; not a checker
completion claim.**
Active [whole-checker completion mandate](CHECKER-COMPLETION-PROMPT.md) from accepted
shared type-system checkpoint `bb035b3`; pinned upstream `fed0bf24149fb1ed36039212648bafdafc1ea10e`.
The [Program contract](PROGRAM-CONTRACT.md) supplies the environment required by
later checker campaigns. Accepted semantic, configuration and resolution proofs remain
the foundation; this document does not reopen them or start large-project benchmarks.

The declaration/object campaign's [fixed contract](DECLARATIONS-CONTRACT.md) includes
genuine checker initialization, class/static/constructor models, member initialization,
enums, TS namespaces/augmentation and demanded declaration diagnostics. Source
implementation, independent review, actual Linux x64 Native AOT and all retained
acceptance gates passed. The [assessment](DECLARATIONS-ASSESSMENT.md) records scope and evidence.

Required shared dependencies now include declaration computed/unique-symbol names,
actual object-literal method/accessor producers, type assertions, TS declaration
metadata, intrinsic-alias declaration validation, typed ambient/bodyless binding
elements and synchronous iterator type-model helpers. Template syntax dispatch uses
the existing construction/cache algorithms. These responsibilities must be reused;
the expression campaign below completes selected runtime destructuring/object spread.
Operational string mappings are now source-complete in the type-system campaign below;
generator/async execution is now accepted under the protocol contract; general JS/JSDoc
checking remains part of the active language campaign.

The expression/statement/checker-flow campaign has passed its
[source-grounded E1–E6 contract](EXPRESSIONS-CONTRACT.md). Selected synchronous runtime
binding, composite/optional expression, statement and flow responsibilities now have
independent review, actual Linux x64 Native AOT and retained-regression acceptance.
See the [assessment](EXPRESSIONS-ASSESSMENT.md) and [proof](../validation/results/expressions-verified.json).
Shared type-system completion is accepted from baseline `09630d6` under its
[source-defined contract](TYPESYSTEM-CONTRACT.md) and [checkpoint](TYPESYSTEM-STATUS.md).
Source-family reconciliation and independent review are complete, including earlier
partial implementations rather than only explicit guards. All 77 complete managed/native
Go records, 84 new xUnit cases, seven shared checks and retained gates pass. The
[current inventory](TYPESYSTEM-INVENTORY.md) separates source closure from finite coverage.

## What is missing, and what is merely unverified

The current checker contains genuine symbol/type resolution, generic inference,
relations, conditional/mapped/indexed types, tuples, callable overloads, flow analysis,
collection APIs and ES-module aliases. The accepted tests prove bounded domains of
those implementations. They do not prove every library declaration, compiler option,
source form or interaction that can reach those algorithms.

[CollectionAdmission](../src/TypeScript.Scanner/Checker/CollectionAdmission.cs) is
a retained named-profile test adapter, not the genuine Program gate or the pinned
checker's language definition. In that adapter it rejects user
declaration files, non-TS source kinds, semantic JSDoc, directives/references, generators,
binding/assignment patterns, object spread, computed names, optional chains, assertion
signatures, key remapping and other syntax. Its accepted-kind list also omits classes,
enums, namespaces, constructors/new, await/yield, JSX and several statements.
[ProjectAdmission](../src/TypeScript.Scanner/Checker/ProjectAdmission.cs) adds bounded
ES-module syntax to another named adapter. The declaration campaign removed the
former authored fixed-option admission gate and library injection from the genuine
Program path. Actual unfinished semantic consumers retain explicit failures, distinct
from successful TypeScript checks; see the [current checkpoint](TYPESYSTEM-STATUS.md).

There are actual algorithm gaps beyond the named adapter restrictions. Declaration
class/base models, computed names and required TS alias producers in
[Checker](../src/TypeScript.Scanner/Checker/Checker.cs) have been restored. Async/generator return inference is now accepted; broader module interoperability is
part of the active language campaign. The expression campaign has restored and
independently reviewed synchronous switch/optional/destructuring/`instanceof`/`in` and
iteration-related paths in [Flow](../src/TypeScript.Scanner/Checker/Flow.cs); its
bounded E1–E6 runtime/native acceptance has passed. For-await/protocol consumers are now accepted; JS consumers remain language obligations.
[Relater](../src/TypeScript.Scanner/Checker/Relater.cs) and
[Inference](../src/TypeScript.Scanner/Checker/Inference.cs) now include the reconciled
shared template/string-mapping branches. JS/JSX-only branches remain external.
Private-class and declaration option-sensitive branches have been restored.
Removing obsolete admission must follow completion of actual producers and consumers.

Conversely, missing broad tests for an implemented algorithm are verification gaps.
The eventual source audit must reconcile every pinned checker semantic responsibility
with its implementation and evidence, including families not currently represented by
a C# method. Counting guards, translated lines or passing fixtures is not that audit.

Pinned source entry points make the distinction concrete:
[checkSourceFile/checkSourceElementWorker](../upstream/TypeScript/tsc/internal/checker/checker.go)
dispatch the semantic responsibilities;
[grammarchecks.go](../upstream/TypeScript/tsc/internal/checker/grammarchecks.go)
owns context/modifier/source grammar diagnostics;
[flow.go](../upstream/TypeScript/tsc/internal/checker/flow.go),
[relater.go](../upstream/TypeScript/tsc/internal/checker/relater.go) and
[inference.go](../upstream/TypeScript/tsc/internal/checker/inference.go)
own the interacting flow/relation/inference algorithms. The async return and yield producers in
[checker.go](../upstream/TypeScript/tsc/internal/checker/checker.go) identify actual
remaining families. Later campaigns must follow those producer
and consumer chains, not simply enlarge an allowed-kind list.

No-emit correctness also depends on diagnostic display. The selected portions of
[NodeBuilder](../src/TypeScript.Scanner/Checker/NodeBuilder.cs),
[Printer](../src/TypeScript.Scanner/Checker/Printer.cs),
[SymbolAccessibility](../src/TypeScript.Scanner/Checker/SymbolAccessibility.cs) and
their [upstream counterparts](../upstream/TypeScript/tsc/internal/checker/)
must format actual types, symbols, signatures and module paths faithfully in diagnostics
and checking queries. Declaration construct/enum/class/alias display dependencies were included in this
campaign; remaining diagnostic-display branches are semantic consumer dependencies
even when nothing is emitted. Emit-only serialization, transform
and service routines can be external only where their actual consumers are outside the
no-emit gate; a file name or an emit-related helper name is not sufficient justification.

## Substantial campaigns

These are coherent source responsibilities, not an exhaustive inventory or a fixed
schedule. Their precise dependencies should be established from the pinned source
before implementation; shared primitives should be completed once and reused.

| Campaign | Missing responsibilities to complete | Evidence that must extend the accepted foundation |
| --- | --- | --- |
| Declaration and object model | Accepted D1–D6 responsibility closure; see the declaration assessment. | Genuine ES2022/default libraries, complete diagnostics and finite public relationships; actual Linux x64 native and retained gates passed. |
| Expressions, statements and flow | Accepted E1–E6 selected synchronous responsibility closure; whole checker counterparts remain partial. | 85 complete managed/native records, six shared groups, 26 order executions per runtime, two ordinary CLI cases and retained gates pass. See the expression assessment for exact scope and observation limits. |
| Remaining type-system closure | T1–T6 source responsibilities implemented and independently reviewed: shared generics, inference, relationships, advanced operators, tuple/callable/predicate consumers and demanded Unicode/display dependencies. Actual Linux x64 Native AOT acceptance passed. | 77 complete managed/native records, 84 new xUnit cases, seven shared checks, order/CLI checks and all retained gates pass. The current inventory distinguishes source-reviewed paths from finite exercised combinations. |
| Async, generators and runtime protocol types | P1–P6 accepted source responsibilities and original complete mapper generator assertions. | 94 complete managed/native Go records, 102 new xUnit cases, eight shared checks, order/CLI checks and retained gates pass; finite evidence remains distinct from exhaustive correctness. |
| JS, JSX and remaining module/options semantics | JS/JSDoc checking, JSX, broader CommonJS/import-equals/export-equals/UMD interoperability, import attributes, decorators and option-dependent rules. Reuse completed TS internal aliases/export-target resolution and namespace/augmentation merges. | Applicable TS/JS/JSX/JSDoc compiler inputs, platform-independent module modes and option combinations, retaining original parser/checker diagnostics and file attribution. |
| Whole-checker reconciliation | Complete remaining pinned semantic responsibilities and demanded utility/model/printer dependencies; remove accidental fixed-profile assumptions. | A responsibility-level source audit and independent omission review spanning every applicable source kind, option, module and library mode, rather than extrapolation from campaign fixtures. |

Program construction, lazy checker acquisition and metadata queries are distinct from
these semantic algorithms. The accepted Program campaign supplies genuine
library/type/reference loading and local reuse/update. It excluded emitter/builder/watch
execution and new checker semantics; an environment can be constructed successfully
while checking it remains explicitly unsupported. Libraries must not be simplified or
omitted to avoid that distinction. The declaration campaign restored the three mapper tests'
original semantic API channels, which pass managed and actual Native AOT execution. The protocol campaign restored the generator test’s complete original global and
incremental assertions; all four mapper bodies now pass managed and actual native execution.

**P1–P6 protocols are accepted.** The [assessment](PROTOCOLS-ASSESSMENT.md) records
94 complete managed/native Go comparisons, eight shared checks, 102 new xUnit cases,
order/CLI checks and all retained gates. Genuine async/await, generator, iteration and
disposal checking and the original mapper generator assertions are complete within that
source-defined contract. Finite coverage is not whole-checker acceptance.

Proceed directly with the [L1–L6 remaining language contract](LANGUAGE-CONTRACT.md):
JS/JSDoc/JSX, decorators, remaining module/options consumers and demanded diagnostic/query
closure. Full original type/symbol tests require source ModuleSpecifiers naming, shared
NodeBuilder context/flags and remaining public wrappers; these are checking dependencies,
not emit/service expansion. The source indexes and implementation plans frontload them.

The [complete original inventory](../validation/checker-noemit-inventory/provenance.json)
records 12,824 inputs, 15,190 configurations and 42 original skips as obligations only.
Use its genuine no-emit runner for targeted feedback and complete final qualification;
avoid another parallel finite-corpus framework. One final native/retained run may satisfy
both language and whole-checker acceptance when source and assertion closure are ready.
Keep external output operations, genuine unavailable environments and implemented-but-
unverified behavior distinct. The active mandate authorizes continuation without another
assignment. Large-project comparisons still require checker acceptance and separate authorization.

## Checker-wide no-emit completion gate

Completion must cover **all pinned upstream checker semantic responsibilities needed
for complete no-emit checking**, across applicable TS, declaration TS, JS, JSX, JSDoc,
module, compiler-option and library modes. It must not be defined around the existing
admission profiles or a selected set of supported fixtures. Any exclusions must name
actual external responsibilities such as emitting output or running a watch/build
scheduler; excluded syntax, globals or semantic algorithms prevent a whole-checker
completion claim.

The gate requires:

1. A complete responsibility reconciliation against the pinned implementation, with
   absent/partial, implemented, reviewed and behaviorally verified states separated.
   Unknown paths and unsupported exceptions cannot count as successful checks.
2. Genuine Program construction and environments, unchanged library/declaration bytes,
   source resolution/reference behavior and options; no fake globals, alternate text,
   diagnostic suppression or silent profile overrides.
3. Applicable original assertions and compiler checking baselines, with full assertion
   domains and explicit operation-level provenance for adaptations. Compare complete
   config, Program, syntactic, bind, global and semantic diagnostics, including invalid
   inputs, related information, file locations, directive policy and error recovery.
   Excluding emit baselines must not exclude their checker obligations.
4. Meaningful behavioral comparisons for types, calls, symbols and consequences where
   diagnostics alone miss a correctness risk. Preserve required identity/lifetime
   invariants from upstream tests; private storage observation is a diagnostic aid,
   not an automatic exhaustive completion requirement.
5. Original/managed repeat, root/query order, local reuse/update and worker controls
   where the APIs promise stable behavior, with source-owned unordered observations
   represented explicitly rather than lucky golden generation.
6. Independent algorithm/assertion/omission review, actual Linux x64 Native AOT
   execution and one frozen complete retained regression cascade. Run cheap affected
   managed consumers before expensive publishing or retained gates. Virtual platform
   hosts do not establish native acceptance for those other platforms.

Large-project correctness and performance comparisons against Go and JavaScript tsc
with emit disabled follow this checker/infrastructure gate. They are not substitutes
for closing source responsibilities, and no such benchmarking starts in this campaign.
