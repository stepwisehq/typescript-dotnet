# Completing the checker

**Reviewed source-grounded roadmap from the accepted Program campaign; not a checker
completion claim.**
Latest accepted expression campaign baseline `5efa4e5`; pinned upstream `fed0bf24149fb1ed36039212648bafdafc1ea10e`.
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
Operational string mappings, generator/async execution and general JS/JSDoc checking
remain later responsibilities.

The expression/statement/checker-flow campaign has passed its
[source-grounded E1–E6 contract](EXPRESSIONS-CONTRACT.md). Selected synchronous runtime
binding, composite/optional expression, statement and flow responsibilities now have
independent review, actual Linux x64 Native AOT and retained-regression acceptance.
See the [assessment](EXPRESSIONS-ASSESSMENT.md) and [proof](../validation/results/expressions-verified.json).
No implementation campaign is active; the next recommendation remains type-system closure.

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
from successful TypeScript checks; see the [current checkpoint](DECLARATIONS-STATUS.md).

There are actual algorithm gaps beyond the named adapter restrictions. Declaration
class/base models, computed names and required TS alias producers in
[Checker](../src/TypeScript.Scanner/Checker/Checker.cs) have been restored. Async/generator return inference and broader
module interoperability remain later work. The expression campaign has restored and
independently reviewed synchronous switch/optional/destructuring/`instanceof`/`in` and
iteration-related paths in [Flow](../src/TypeScript.Scanner/Checker/Flow.cs); its
bounded E1–E6 runtime/native acceptance has passed. For-await and external JS/protocol consumers
remain distinct later obligations.
[Relater](../src/TypeScript.Scanner/Checker/Relater.cs) and
[Inference](../src/TypeScript.Scanner/Checker/Inference.cs) retain missing template/string
mapping and JSX branches. Selected private-class and declaration option-sensitive branches have been restored.
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
own the interacting flow/relation/inference algorithms. The async return, yield and
operational string-mapping producers in
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
| Remaining type-system closure | Operational intrinsic string mappings and remaining template/relation/inference interactions, key remapping, broader unique-symbol expression uses, generic variadic/rest completion evidence, assertion predicates and missing constraint branches. Declaration keys and template syntax construction are already implemented. | Original type/inference assertions and meaningful positive/negative combinations with existing generic, conditional, tuple, index and mapped-type algorithms. |
| Async, generators and runtime protocol types | Async/await, generators/yield, remaining iterator execution and async-iterator protocols, promise checking, custom iteration and disposal/using. Reuse the completed synchronous iterator type-model helpers. | Real pinned libraries and custom protocols; all global/semantic diagnostics, return/yield inference and invalid protocol cases. The Program mapper generator test is a concrete outstanding consumer. |
| JS, JSX and remaining module/options semantics | JS/JSDoc checking, JSX, broader CommonJS/import-equals/export-equals/UMD interoperability, import attributes, decorators and option-dependent rules. Reuse completed TS internal aliases/export-target resolution and namespace/augmentation merges. | Applicable TS/JS/JSX/JSDoc compiler inputs, platform-independent module modes and option combinations, retaining original parser/checker diagnostics and file attribution. |
| Whole-checker reconciliation | Complete remaining pinned semantic responsibilities and demanded utility/model/printer dependencies; remove accidental fixed-profile assumptions. | A responsibility-level source audit and independent omission review spanning every applicable source kind, option, module and library mode, rather than extrapolation from campaign fixtures. |

Program construction, lazy checker acquisition and metadata queries are distinct from
these semantic algorithms. The accepted Program campaign supplies genuine
library/type/reference loading and local reuse/update. It excluded emitter/builder/watch
execution and new checker semantics; an environment can be constructed successfully
while checking it remains explicitly unsupported. Libraries must not be simplified or
omitted to avoid that distinction. The declaration campaign restored the three mapper tests'
original semantic API channels, which pass managed and actual Native AOT execution. The generator test still must regain
its original global and incremental assertions in the protocol campaign.

With expression E1–E6 accepted, the next recommended campaign completes remaining type-system responsibilities:
operational intrinsic string mappings, key remapping, remaining template/relation/
inference/constraint branches, generic variadic/rest and assertion-predicate closure.
Do not reimplement existing conditional/mapped/tuple algorithms; reconcile missing
source branches separately from insufficient combination coverage. The restored
synchronous body and declaration models provide their real consumers.

Async/generator/iteration execution, disposal and JS/JSX/module-option work follow
according to actual source dependencies. Existing synchronous iterator type-model
helpers are reusable, not evidence that those protocols are complete. Whole-checker
reconciliation still covers every applicable semantic responsibility.

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
