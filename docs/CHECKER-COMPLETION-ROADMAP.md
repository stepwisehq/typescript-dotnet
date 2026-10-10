# Completing the checker

**Reviewed source-grounded roadmap from the accepted Program campaign; not a checker
completion claim.**
Baseline `1d6bd61`; pinned upstream `fed0bf24149fb1ed36039212648bafdafc1ea10e`.
The [Program contract](PROGRAM-CONTRACT.md) supplies the environment required by
later checker campaigns. Accepted semantic, configuration and resolution proofs remain
the foundation; this document does not reopen them or start large-project benchmarks.

## What is missing, and what is merely unverified

The current checker contains genuine symbol/type resolution, generic inference,
relations, conditional/mapped/indexed types, tuples, callable overloads, flow analysis,
collection APIs and ES-module aliases. The accepted tests prove bounded domains of
those implementations. They do not prove every library declaration, compiler option,
source form or interaction that can reach those algorithms.

[CollectionAdmission](../src/TypeScript.Scanner/Checker/CollectionAdmission.cs) is
an authored boundary, not the pinned checker's language definition. It rejects user
declaration files, non-TS source kinds, semantic JSDoc, directives/references, generators,
binding/assignment patterns, object spread, computed names, optional chains, assertion
signatures, key remapping and other syntax. Its accepted-kind list also omits classes,
enums, namespaces, constructors/new, await/yield, JSX and several statements.
[ProjectAdmission](../src/TypeScript.Scanner/Checker/ProjectAdmission.cs) adds bounded
ES-module syntax; full resolution support does not remove the remaining checker
language boundary. Named legacy profiles and
[ProjectCheckingAdmission](../src/TypeScript.Scanner/Projects/ProjectCheckingAdmission.cs)
also limit options and environments. Admission failures are evidence of an unsupported
domain, not successful TypeScript checks.

There are corresponding algorithm gaps, not just conservative admission. For example,
[Checker](../src/TypeScript.Scanner/Checker/Checker.cs) explicitly rejects source class
declared/base types, async/generator return inference, computed properties and parts of
module interoperability. [Flow](../src/TypeScript.Scanner/Checker/Flow.cs) still has
unsupported switch, optional-chain, destructuring, `instanceof`/`in` and iteration paths.
[Relater](../src/TypeScript.Scanner/Checker/Relater.cs) and
[Inference](../src/TypeScript.Scanner/Checker/Inference.cs) retain missing template/string
mapping, private-class, JSX and option-sensitive branches. A syntax gate cannot be
removed before those actual producers and consumers are completed.

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
own the interacting flow/relation/inference algorithms. The class declaration/base-type,
computed-property, async return and yield/type-template producers in
[checker.go](../upstream/TypeScript/tsc/internal/checker/checker.go) demonstrate actual
missing families behind current admission. Later campaigns must follow those producer
and consumer chains, not simply enlarge an allowed-kind list.

No-emit correctness also depends on diagnostic display. The selected portions of
[NodeBuilder](../src/TypeScript.Scanner/Checker/NodeBuilder.cs),
[Printer](../src/TypeScript.Scanner/Checker/Printer.cs),
[SymbolAccessibility](../src/TypeScript.Scanner/Checker/SymbolAccessibility.cs) and
their [upstream counterparts](../upstream/TypeScript/tsc/internal/checker/)
must format actual types, symbols, signatures and module paths faithfully in diagnostics
and checking queries. Missing construct/enum/class/alias display branches are semantic
consumer dependencies even when nothing is emitted. Emit-only serialization, transform
and service routines can be external only where their actual consumers are outside the
no-emit gate; a file name or an emit-related helper name is not sufficient justification.

## Substantial campaigns

These are coherent source responsibilities, not an exhaustive inventory or a fixed
schedule. Their precise dependencies should be established from the pinned source
before implementation; shared primitives should be completed once and reused.

| Campaign | Missing responsibilities to complete | Evidence that must extend the accepted foundation |
| --- | --- | --- |
| Declaration and object model | Classes, constructors/new, inheritance, accessors, private/protected members, `super`, initialization rules, enums, namespaces, ambient declarations and declaration merging. | Genuine declaration/library environments; visibility, merge, overload, constructor and invalid inheritance interactions across files. |
| Expressions, statements and flow | Binding/assignment destructuring, rest/spread, computed names, optional chains, assertions, switch/try/catch/for-in/labels and missing narrowing/flow transfers. | Complete source diagnostics and narrowing behavior through interacting control paths, including unreachable/error recovery and directive ownership. |
| Remaining type-system closure | Template literals and intrinsic string mappings, key remapping, unique symbols, generic variadic/rest cases, assertion predicates, and missing relation/inference/constraint branches. | Original type/inference assertions and meaningful positive/negative combinations with existing generic, conditional, tuple, index and mapped-type algorithms. |
| Async, generators and runtime protocol types | Async/await, generators/yield, iterator and async-iterator protocols, promise checking, custom iteration and disposal/using. | Real pinned libraries and custom protocols; all global/semantic diagnostics, return/yield inference and invalid protocol cases. The Program mapper generator test is a concrete outstanding consumer. |
| JS, JSX and remaining module/options semantics | JS/JSDoc checking, JSX, CommonJS/import-equals/export-equals/UMD interoperability, augmentations, import attributes, decorators and option-dependent rules. | Applicable TS/JS/JSX/JSDoc compiler inputs, platform-independent module modes and option combinations, retaining original parser/checker diagnostics and file attribution. |
| Whole-checker reconciliation | Complete remaining pinned semantic responsibilities and demanded utility/model/printer dependencies; remove accidental fixed-profile assumptions. | A responsibility-level source audit and independent omission review spanning every applicable source kind, option, module and library mode, rather than extrapolation from campaign fixtures. |

Program construction, lazy checker acquisition and metadata queries are distinct from
these semantic algorithms. The current campaign includes genuine library/type/reference
loading and local reuse/update. It excludes emitter/builder/watch execution and new
checker semantics; an environment can be constructed successfully while checking it
remains explicitly unsupported. Libraries must not be simplified or omitted to avoid
that distinction. The three mapper tests adapted to construction diagnostic channels
must return to their full original semantic API assertions when that entry is supported;
the generator test must also regain its original global and incremental assertions.
These are concrete verification obligations, not already-complete semantic tests.

The recommended first checker campaign is declaration/object-model closure on the
accepted Program environment. Real bundled libraries and project references
make unsupported class/constructor/ambient declaration paths consequential. Establish
genuine checker initialization first: the pinned
[NewChecker](../upstream/TypeScript/tsc/internal/checker/checker.go) consumes actual
Program options, ambient/global symbols and diagnostic collection. The current
[CreateForProgram/CreateCore](../src/TypeScript.Scanner/Checker/Checker.cs) entry still
uses authored semantic profiles; successful Program construction must not imply that
this initialization or arbitrary environment checking is complete. Exercise supported
acquisition and explicit unsupported acquisition separately before extending admission.
Then establish
the symbol/type/base-member model and grammar rules first, then constructor/member
checking and relations/inference, then demanded diagnostic builders/accessibility and
source admission. Complete expression/control-flow work follows or proceeds as a
disjoint family once those shared models are stable. Async/iterator and JS/JSX work
requires the real declaration/protocol environment and should reuse it. Advanced type
work can proceed independently where its actual producer dependencies permit; the
final reconciliation must still cover every pinned semantic responsibility.

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
