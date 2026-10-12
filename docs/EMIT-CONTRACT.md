# Whole-emit completion contract

Active from accepted performance implementation `e3131a5b` and its summary
`56ed2a75`. Upstream remains `fed0bf24149fb1ed36039212648bafdafc1ea10e`,
under `upstream/TypeScript/tsc`. This contract completes pinned upstream
responsibilities; it is not a selection of features needed by one demonstration.
The [original mandate](EMIT-PROMPT.md), [implementation inventory](EMIT-IMPLEMENTATION.md)
and [test inventory](EMIT-TEST-INVENTORY.md) preserve scope and current dispositions.

## Scope and dependency order

1. **Synthesis and transformation foundations.** Complete printer NodeFactory,
   EmitContext, generated identifiers/names, helper definitions and ownership,
   original-node and emit metadata, comments/source ranges, lexical environments,
   initialization statements and visitor hooks. Reuse accepted AST factory,
   update/clone/visitor behavior; change it only for demonstrated missing dependencies.
2. **Source maps and printing.** Complete the source-map package (encoding,
   decoding, generation, source/line information and map-position lookup), writers,
   printer syntax coverage, comments, precedence, formatting, generated names,
   helpers and map hooks. Complete the upstream packages rather than leaving new
   partial leaves for each consumer. Language-service consumers of these APIs remain
   outside scope.
3. **Transformation machinery and JavaScript output.** Complete base transformer,
   chains, modifier visiting, shared utilities and destructuring before consumers.
   Complete every transform in the pinned compiler emitter's chain: TypeScript
   metadata, type erasure, import elision, runtime syntax and legacy decorators;
   JSX; the pinned ES lowering family; use-strict; implied/ES/CommonJS modules;
   and constant-enum inlining. Preserve upstream transform ordering and target,
   module, JSX and option decisions. The pinned compiler defines supported targets,
   including its original errors for removed/unsupported options.
4. **Declaration output and semantic emit support.** Complete checker EmitResolver,
   serialization/tracking dependencies and declaration diagnostics required by
   declaration transforms, including supplemental references. Reuse genuine Program,
   source/library and checker environments; no simplified declarations or silent
   fallback. Shared semantic dependencies may land earlier when needed by transforms.
5. **Program output and invocation.** Complete compiler EmitHost/emitter and Program
   emit entry points, JS/declaration/map output selection and names, write callbacks,
   output failure/blocking diagnostics, BOM/newline/comment options, noEmit,
   noEmitOnError, emitDeclarationOnly and related pinned option behavior. Expose a
   reproducible disk command using the existing configuration/environment machinery.
   Account for upstream emitter entry modes individually, including helpers called
   by builder-signature paths, without adding build/watch orchestration.
6. **Whole-emit acceptance.** Reconcile implementations and tests, compare complete
   outputs/diagnostics/behavior against genuine pinned Go, review the completion
   claim independently, execute Linux x64 Native AOT and retained regressions,
   then commit a clean final checkpoint.

These are dependency-ordered subsystem campaigns, not vertical feature slices.
Parallel implementation is appropriate for independent prerequisites. Complete and
verify coherent responsibilities before relying on them in higher-level consumers.
Do not narrow this contract because a dependency is difficult or a fixture fails.

## Observable acceptance

- Inventory applicable direct upstream tests and compiler/conformance configurations
  with original provenance, assertions, option combinations and skip dispositions.
  Separate original tests, assertion-preserving adaptations, reused fixture inputs,
  authored controls and later-stage exclusions. Existing 15,304 no-emit records are
  retained regression evidence, not emit-output evidence.
- Compare complete emitted file sets, paths and contents, diagnostics and return
  status under equivalent inputs/options/libraries. Preserve genuine upstream
  expected output; any necessary host/root normalization must be explicit and cannot
  erase semantically meaningful differences.
- Exercise JavaScript, declarations and maps, their interactions, output options,
  malformed/error inputs, comments/names/helpers, and upstream pre/post-emit
  invariants. Reuse applicable upstream runtime/declaration/source-map checks and add
  focused assertions only for concrete missing coverage. A successful emission or
  a matching count alone does not establish correctness.
- Direct xUnit tests and shared Native AOT assertions must execute actual ported
  implementations. Full output comparisons must run the production native pipeline.
  The xUnit runner itself need not be Native AOT.
- Independently review correctness, omissions, original assertion fidelity, input
  dispositions and the final completion claim. Track implemented-but-unexercised
  paths separately from missing implementations; neither is silently completed.

## Preserved requirements and boundaries

Keep direct discoverable Go-to-C# mapping, upstream algorithms/function boundaries,
licenses and readable code. Use standard .NET facilities when semantics fit.
Preserve accepted UTF8 borrowing, slice sharing/required copies, callback state,
arena/identity behavior, parser pooling and runtime adaptations. Record necessary
departures and actual verification in the existing source map/progress documents.

Actual Linux x64 Native AOT, accepted correctness regressions and applicable
performance regression protection remain mandatory. Develop with targeted checks;
run comprehensive gates at coherent checkpoints, with coordinated builds and frozen
inputs. Preserve the accepted performance artifacts and runtime settings. Use upstream
workloads for development/regression checks; VS Code and other external-project
evaluations are deferred until emit acceptance by explicit user direction. No new
external-project speed claim follows automatically from an emit change.

Language services, unrelated build/watch orchestration, new project systems and
upstream upgrades are outside scope. Include genuinely necessary shared dependencies;
raise material expansion rather than silently implementing excluded infrastructure.
Platform claims are Linux x64 only unless additional evidence is actually collected.

## Execution and delivery

Astra coordinates and accepts; GPT-6.1 Sol implements and independently reviews.
Keep source ownership disjoint, reuse agents/findings, coordinate integration builds,
and preserve concise resumable state. Continue across subsystem commits without
renewed user authorization. Ordinary failures are work to resolve, not blockers.

Final delivery requires committed implementation, a clean working tree, reproducible
compilation/acceptance commands, source mapping, test dispositions, and a completion
assessment with residual verification limitations. Report observed effort only;
token or monetary measurements are not available unless actually recorded.
