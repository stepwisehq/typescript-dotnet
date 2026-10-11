# Resume this project

To restart work, give the new session this instruction:

> Read START-HERE.md in /home/greg/projects/typescript-dotnet and resume the
> current campaign. Follow the original user prompt and subsequent
> directions recorded here. Inspect existing work and the last verified
> subsystem checkpoints before taking action; do not restart the port.

Follow the active phase in [STATE.md](./docs/STATE.md). If it records no active
work, report that checkpoint and wait for a new bounded mandate; this resume
instruction does not authorize emit or unrelated compiler infrastructure. The whole-checker
mandate below authorizes successive no-emit checking campaigns until its final gate.

## Current operating procedure: comprehensive allocation rollout

The whole-checker gate was accepted. The user subsequently authorized VS Code
checking, profiling and comprehensive Go-faithful allocation reduction. Older
instructions below that defer optimization or stop at checker acceptance are
historical; they do not stop this active work.

**Read this section and the top of [STATE.md](./docs/STATE.md) after every restart,
compaction or agent handoff.** The user's latest direction is to stop the inefficient
cycle of one or two local changes followed by targeted tests and benchmarks.

- Work through complete ownership paths: storage owners, producers, retained
  references, consumers and cache boundaries together. Apply the established slice,
  borrowed UTF8 text, stateful callback and embedded-object techniques consistently
  where they match Go. An intermediate conversion bridge is not a finished rollout.
- Carry UTF8 byte buffers through internal comparisons and text consumers wherever
  Go does. Avoid intermediate string resolution, encoding and decoding. Use efficient
  token/rune advancement when scanning buffers, with matching .NET span operations
  where appropriate. Preserve Go byte offsets and malformed-UTF8/WTF8 advancement;
  materialize strings only at real consumer or external boundaries.
- Preserve pinned Go algorithms, control flow, function boundaries, organization,
  and actual allocation/ownership rules. Required copies, Clip/Clone operations and
  detachments stay. Use standard .NET operations where their semantics fit. Do not
  introduce a different compiler design or assume every upstream allocation can go.
- The main Astra coordinator owns decisions and integration; GPT-6.1 Sol agents own
  substantive implementation and independent review. Assign coherent, disjoint
  source ownership, coordinate shared APIs and continue between internal families
  without seeking another assignment. Current lanes and files are in STATE.md.
- **Do not restart per-small-change targeted tests, repeated VS Code benchmarks,
  allocation captures or full gates after compaction.** Use coordinated integration
  builds as the larger rollout comes together. Run behavioral and performance
  verification after the complete integrated change; investigate concrete defects
  proportionately. This changes verification timing, not correctness requirements.
- Retain source review, original upstream expectations, meaningful Go comparisons,
  xUnit coverage and actual Linux x64 Native AOT acceptance before finalization.
  Preserve useful existing evidence; never call a partially verified change accepted.
  Do not weaken expectations or suppress discrepancies.
- Keep concise resumable state and source mapping. Preserve verified checkpoints
  separately from ongoing changes. A commit, subagent completion, internal milestone
  or context reset is not a reason to stop or request renewed authorization.
- Do not generate more SVG flamegraphs. Keep raw traces and useful analysis for a
  performance viewer. Do not build replacement process or viewer infrastructure.

The immediate objective is comprehensive implementation of the established
Go-compatible allocation techniques, followed by integrated verification and
measurement. Identical allocation counts between runtimes are not assumed, and
performance gains must be measured rather than declared from source changes.

## User mandate

Read [the original prompt, preserved verbatim](./docs/INITIAL-PROMPT.md).
It remains the governing task. The main session owns planning, coordination,
significant decisions, and acceptance; substantive implementation, investigation,
testing, and independent review are delegated explicitly to GPT-6.1 Sol.
Report model/tool limitations rather than silently substituting.

Later user directions, in chronological order:

1. The user pointed out that `typescript-go` was archived and directed: **“I think
   we should use the latest; not compare.”** The reference was switched before
   implementation to maintained `microsoft/TypeScript`, under `tsc/`. The selected
   latest revision is pinned in [upstream.lock.json](./upstream.lock.json). Resume
   against that fixed revision; do not silently update it mid-study.
2. The user has root access and offered to install needed packages. An asynchronous
   request asked for `clang` and `zlib1g-dev`. A local compiler fallback has already
   published Native AOT successfully; check the latest verification checkpoint for
   current system prerequisites and behavioral results. Do not assume a pending
   package request has been completed.
3. The user requested efficient crash-resistant progress records, with a possible
   future compiler port in mind, while leaving the mechanism and timing to the
   coordinator. Use the small per-subsystem checkpoints and source map now;
   introduce more infrastructure only when there is a demonstrated need.
4. The user installed `sqlite3` as an optional tool. No database is currently needed
   or required. This does not expand the task into a full compiler port.
5. The user requested preservation of the initial prompt and a clean re-entry point.
   This file and [INITIAL-PROMPT.md](./docs/INITIAL-PROMPT.md) fulfill that request.
6. After scanner acceptance, the user asked for a basic performance comparison
   with Go. This authorizes a bounded benchmark and assessment, not optimization
   or a broader compiler implementation. See current state for progress/results.
7. After the benchmark, the user explicitly said **“We don't want to do any
   optimization now”** and asked to record possible explanations and investigation
   ideas. The performance investigation backlog is documentation only; it does
   not authorize profiling, experiments, or implementation changes on resumption.
   The user reiterated that actual deeper investigation should wait until later;
   retain brief hypotheses from existing evidence rather than extending analysis.
8. The user directed committing progress and proceeding to **parser and required
   syntax AST support**, with structural and syntax-diagnostic agreement on valid
   and malformed input, Native AOT execution, and scanner regressions. Read the
   exact [parser mandate](./docs/PARSER-PROMPT.md). The scanner checkpoint was
   committed as `0a50e67`. This supersedes the initial scanner-only scope for the
   parser phase; binding was then excluded. Direction 11 below authorizes the
   subsequent bounded binder phase; checking, resolution and emit remain excluded.
9. During parser work the user reconfirmed GPT-6.1 Sol delegation and close Go
   source fidelity. They clarified that raw speed is not the concern: efficient
   work should reduce repeated effort and token cost. **“Slow and steady wins the
   race.”** Treat time estimates as observations, not deadlines that justify
   shortcuts, extra agents, or weakened verification.
10. After parser acceptance, the user asked for **basic performance tests against
    Go and recorded findings, like the scanner comparison**. This authorizes a
    separate benchmark harness and measurements on the accepted parser snapshot.
    It does not authorize optimization, profiling or deeper performance diagnosis.
    The [basic comparison](./docs/PARSER-PERFORMANCE.md) is complete; do not rerun
    or extend it automatically on resumption.
11. The user authorized committing parser/performance work and proceeding to
    **source-file binding and required support**. That checkpoint is `de25b66`.
    Read the exact [binder mandate](./docs/BINDER-PROMPT.md). Review parser evidence
    and the 29 unobserved diagnostic codes before extending code; compare symbols,
    declarations, scopes, binder diagnostics and flow structures with pinned Go.
    Type checking, cross-file semantic resolution, module resolution and emit
    remain excluded. Native AOT and scanner/parser regressions are mandatory.
    The [bounded binder checkpoint](./docs/BINDER-ASSESSMENT.md) is now accepted;
    implementation work was subsequently committed as `e94cc6f`.

12. The user authorized committing binding and proceeding toward the **first bounded
    end-to-end type-checking milestone**. Read the exact
    [checker mandate](./docs/CHECKER-PROMPT.md). Use real upstream name resolution
    and type relationships on valid and invalid inputs; define standard-library
    treatment and distinguish unsupported input from a successful check. Preserve
    actual Native AOT and all prior regression gates. Broad module resolution,
    project infrastructure, emit, incremental compilation and language services
    remain excluded unless concrete scope pressure is reported and reconsidered.
    The [first semantic checkpoint](./docs/CHECKER-ASSESSMENT.md) is now accepted;
    scalar checker work was subsequently committed as `7e692dd`.

13. During checker work, the user again asked whether Go was being ported as directly
    as possible. Preserve upstream function/file boundaries, control flow and models.
    Selected partial branches and scoped initialization must be explicit; authored
    admission/harness support must stay distinct from actual semantic algorithms.

14. The user authorized committing scalar checking and proceeding to **object types,
    property access and structural assignability**. Read the exact
    [object checker mandate](./docs/OBJECT-CHECKER-PROMPT.md) and current state.
    Keep successful checks distinct from unsupported paths, exercise interacting
    features and semantic failures, assess newly relevant binder identity/table
    behavior, and retain actual Native AOT plus all earlier regression coverage.
    Keep missing-global diagnostics separate. Generics, broad libraries, modules,
    emit and services remain excluded; report concrete dependency pressure before
    expanding the boundary.
    The [object checkpoint](./docs/OBJECT-CHECKER-ASSESSMENT.md) is now accepted;
    object work was subsequently committed as `1f7c953`. Direction 15 authorizes
    the subsequent completion campaign.

15. The user authorized committing the anonymous-object milestone and a **larger
    data-type checking completion campaign**, managing internal milestones without
    feature-by-feature approval. The baseline is `1f7c953`. Read the exact
    [campaign mandate](./docs/DATA-TYPES-PROMPT.md), fixed
    [completion contract](./docs/DATA-TYPES-CONTRACT.md) and live
    [inventory](./docs/DATA-TYPES-STATUS.md). Continue until the contract is met and
    committed or a material boundary/access decision is needed; do not stop merely
    because an internal slice passes. Earlier stopping directions apply to their
    historical phases, not this newly authorized campaign. The
    [campaign assessment](./docs/DATA-TYPES-ASSESSMENT.md) now records accepted D1–D7
    behavior with actual Native AOT and all earlier regressions. This mandate is
    fulfilled by the campaign delivery commit; no next campaign is authorized.

16. The user redirected the next campaign to **completion and verification of scanner,
    parser and syntax-AST foundations**, with systematic upstream test incorporation.
    Read the [exact foundation mandate](./docs/FOUNDATION-PROMPT.md). Baseline is
    `b6b22b9`; no callable work was started. The coordinator must define a substantial
    source-based contract and finish its internal milestones autonomously. Original
    test assertions, adaptations, compiler-input reuse and additional comparisons
    must remain distinct. Actual Native AOT and binder/checker regressions are gates.
    Direction 15 is completed. This foundation contract is now accepted with actual
    Linux x64 Native AOT and retained regressions; see the
    [assessment](./docs/FOUNDATION-ASSESSMENT.md). No subsequent campaign is authorized.
    Report executed data-driven cases separately from upstream test functions and
    no-panic seeds separately from structural comparisons, as the user clarified.

17. The user authorized **completion and systematic verification of source-file
    binding** atop `2886d47` before callable checking. Read the
    [exact mandate](./docs/BINDING-COMPLETION-PROMPT.md) and fixed
    [contract](./docs/BINDING-COMPLETION-CONTRACT.md). Include every binder-produced
    symbol/scope/flow/diagnostic relationship, required support and historical
    identity/mode/diagnostic gaps; keep later checker/project/emit work outside.
    Proceed autonomously through actual Linux x64 Native AOT and all regressions,
    independent review and clean committed delivery. This mandate is fulfilled;
    the [assessment](./docs/BINDING-COMPLETION-ASSESSMENT.md) records B1–B6 acceptance
    with actual Linux x64 Native AOT. Callable checking is ready for a subsequent
    bounded campaign, but no new campaign is automatically authorized.

18. The user authorized **functions and callable-object checking**, building on binder
    `de855ca` and foundation `2886d47`. Read the exact
    [mandate](./docs/CALLABLES-PROMPT.md), fixed [contract](./docs/CALLABLES-CONTRACT.md)
    and [status](./docs/CALLABLES-STATUS.md). Retain close upstream fidelity and explicit
    Sol delegation. Complete the substantial non-generic capability autonomously,
    including selected body/parameter/inference dependencies, meaningful actual-Go
    comparison and Linux x64 Native AOT plus all regressions. Do not broaden into
    generics/libraries/projects/modules without a material scope decision. Direction
    17 is complete. This callable mandate is also fulfilled: the
    [assessment](./docs/CALLABLES-ASSESSMENT.md) and
    [proof](./validation/results/callables-verified.json) record fixed C1–C6 acceptance.
    No subsequent campaign is automatically authorized.

19. The user authorized the **generic-collection checking campaign** atop `cfa6d2f`.
    Read the [exact mandate](./docs/COLLECTIONS-PROMPT.md), fixed
    [contract](./docs/COLLECTIONS-CONTRACT.md) and [current state](./docs/STATE.md).
    Include genuine bounded upstream libraries, generic/array/tuple/utility/loop behavior,
    a realistic single-file demonstration with invalid variants, independent review,
    actual Linux x64 Native AOT and all prior regressions. Complete autonomously through
    clean committed delivery; do not silently expand into broad modules/projects/emit.
    Direction 18 is complete. This collection contract is now satisfied; read the
    [assessment](./docs/COLLECTIONS-ASSESSMENT.md) and
    [durable proof](./validation/results/collections-verified.json). No subsequent
    module/project campaign is automatically authorized.

20. The user authorized **bounded multi-file ES-module project checking** atop
    `2598ceb`. Read the [exact mandate](./docs/PROJECTS-PROMPT.md) and
    [current state](./docs/STATE.md). Define and complete a useful relative-module
    disk/config/program/checker workflow, preserving genuine pinned libraries,
    complete cross-file diagnostics and meaningful semantic relationships, cycles,
    independent Sol review and Linux x64 Native AOT. Packages, broad platform module
    behavior, references, incremental builds, emit and services remain outside scope.
    Direction 19 is fulfilled. This project contract is also accepted; see the
    [assessment](./docs/PROJECTS-ASSESSMENT.md) and
    [durable proof](./validation/results/projects-verified.json). The delivery commit
    containing this entry fulfills the mandate; no next campaign is authorized.

21. The user authorized **upstream subsystem completion**, baseline `2c850db`.
    Read the [exact mandate](./docs/CONFIGURATION-PROMPT.md) and current state.
    Configuration parsing/discovery is the leading candidate. Define completion
    from upstream responsibilities, not the demonstration or already passing fixtures;
    preserve separate implementation and evidence inventories, original tests,
    actual Linux x64 Native AOT and all regressions. Complete autonomously through
    independent Sol review and committed delivery, or report a material blocker.
    This supersedes the prior instruction to await a new campaign.

22. **Source-import module-resolution completion.** Baseline `37bd57d`; read the
    [mandate](./docs/RESOLUTION-PROMPT.md), fixed
    [contract](./docs/RESOLUTION-CONTRACT.md) and
    [resumption checkpoint](./docs/RESOLUTION-STATUS.md). Full resolver behavior may
    exceed checker capabilities; preserve that distinction and truthful graph loading.
    Continue autonomously through independent Sol review, actual Native AOT and
    committed delivery. .NET 11/optimization work remains deferred. This supersedes
    the earlier instruction to await a new campaign.

23. **Prospective verification efficiency.** Prioritize implementation progress and
    efficient verification while retaining upstream fidelity/tests, meaningful Go
    comparison, independent review and actual Native AOT acceptance. Private-state
    instrumentation needs a concrete correctness, upstream-invariant or resource
    reason; exhaustive private equivalence is not an automatic completion gate.
    Preserve existing machinery and useful evidence, classify observer limitations
    proportionately, and never hide defects by changing expectations. Do not restart,
    reopen accepted work, launch cleanup or add a process framework. Simplify only
    an active bottleneck, make substantive observation changes explicit, and disable
    observation-only overhead before performance comparisons. Let any ongoing final
    acceptance run finish. This applies to the remaining current and future work.

24. **Program construction and environment assembly.** Baseline `1d6bd61`; read
    [the mandate](./docs/PROGRAM-PROMPT.md), [contract](./docs/PROGRAM-CONTRACT.md)
    and [checkpoint](./docs/PROGRAM-STATUS.md). Complete genuine upstream environments,
    references and Program diagnostics independently of unsupported checker semantics.
    Produce a source-grounded remaining-checker roadmap with a checker-wide gate;
    planning supplements implementation. Finish checker and required infrastructure
    before large-project Go/JavaScript-tsc comparisons with emit disabled. Continue
    through independent review, actual Native AOT, retained regressions and clean
    committed delivery. Prior efficiency/fidelity requirements remain in force.

25. **Genuine checker initialization and declaration/object-model completion.**
    Baseline `e7b81a2`; follow the [mandate](./docs/DECLARATIONS-PROMPT.md),
    [fixed contract](./docs/DECLARATIONS-CONTRACT.md) and
    [checkpoint](./docs/DECLARATIONS-STATUS.md). Implement actual semantic initialization
    and declaration relationships over unchanged genuine environments; complete review,
    actual Linux x64 Native AOT and retained regressions, then commit cleanly. Continue
    autonomously; do not stop at planning or an internal milestone. Earlier no-active-work
    statements describe historical checkpoints. Large-project comparisons stay deferred.

26. **Direct parity convergence.** The user clarified: “we are wanting to converge
    on that very direct and parity port of the golang code to c#. So we should be
    looking to strip out any temporary hacks put in place to support the "vertical
    slice" campaigns and replace them with the comprehensive golang port work.”
    Replace obsolete slice scaffolding when completing its upstream responsibilities;
    retain only justified language adaptations and clearly authored test adapters.
    Regression assertions remain evidence, not a requirement to preserve temporary
    production architecture. Explicit unsupported paths remain until actually ported.

27. Proceed from `5efa4e5` with TypeScript expressions/statements/checker-flow completion.
    The exact [mandate](./docs/EXPRESSIONS-PROMPT.md) and
    [contract](./docs/EXPRESSIONS-CONTRACT.md) govern autonomous delivery. Retain genuine
    Program/libraries, actual Native AOT, complete diagnostics/inferred types and all
    regression gates; advanced-type/protocol/JS boundaries remain explicit.

28. From `09630d6`, complete remaining upstream type-system responsibilities and
    reconcile earlier partial generic/inference/relation/mapped/conditional support.
    Follow the exact [mandate](./docs/TYPESYSTEM-PROMPT.md). Keep genuine environments,
    meaningful public semantic comparisons, independent Sol review and actual Native
    AOT/retained acceptance. Async/generator and JS-specific boundaries stay explicit;
    large-project comparison remains deferred until checker completion.

## Recovery sequence

1. Read [docs/STATE.md](./docs/STATE.md) for overall scope and acceptance status.
2. Read [docs/RESUMING.md](./docs/RESUMING.md) and the relevant records under
   [docs/progress/](./docs/progress/), including independent review findings.
3. Inspect `git status --short`, existing source, and [source-map.json](./source-map.json).
   Uncommitted files are saved work. Do not discard or regenerate them from an
   earlier temporary translation script.
4. Follow the next unresolved action in the checkpoints. Reuse active agents if
   available; after session loss, delegate using those checkpoint files instead
   of reconstructing the old chat.
5. Use the [Program guide](./docs/PROGRAM.md),
   [resolution guide](./docs/RESOLUTION.md),
   [configuration guide](./docs/CONFIGURATION.md),
   [project guide](./docs/PROJECTS.md) and [README.md](./README.md) for
   reproducible invocation and verification. Required acceptance
   includes executing the actual Native AOT binary against the oracle corpus.
   Recheck results after changes; a prior publish does not establish current success.

Source-file binding completion is accepted at `de855ca`, callable checking at
`cfa6d2f`, generic collections at `2598ceb`, and bounded project checking at `2c850db`.
Configuration interpretation/discovery is accepted in the delivery commit containing
this entry. See its [assessment](./docs/CONFIGURATION-ASSESSMENT.md) and
[proof](./validation/results/configuration-verified.json). A checkpoint can be accepted,
incomplete or blocked; never infer completion from source presence or test totals.
The subsequent source-import resolution contract is accepted in the delivery commit
containing this entry; see its [assessment](./docs/RESOLUTION-ASSESSMENT.md) and
[proof](./validation/results/resolution-verified.json). Follow [STATE.md](./docs/STATE.md)
for delivery recovery or a new mandate. Do not restart accepted work or optimization.

The Program construction/environment contract is accepted in the commit containing
this entry, with actual Linux x64 Native AOT and retained regression evidence. Read its
[assessment](./docs/PROGRAM-ASSESSMENT.md), [checkpoint](./docs/PROGRAM-STATUS.md) and
[proof](./validation/results/program-verified.json). The reviewed
[checker completion roadmap](./docs/CHECKER-COMPLETION-ROADMAP.md) recommends the next
campaign; it does not authorize starting it without a new mandate. Large-project
correctness/performance comparisons remain after checker/infrastructure completion.
