# Emit implementation inventory

## Accepted whole-emit record

The ManyFiles workload exposed nested ThreadPool/Lazy starvation. The reviewed
WorkGroup fix lets callbacks and waiters claim ready actions exactly once, preserving
queue-start, wakeup and recursive completion/failure rules without changing the
workload or thread limits. Fresh build, all 2,223 managed records, six Native
publications and direct Native controls passed. Native ManyFiles completes, and the
isolated 50-run original workload cohort is complete. Fresh Native corpus, command and full retained noEmit revalidation passed; older results below remain explicitly prior-candidate evidence. See the [assessment](EMIT-ASSESSMENT.md) for exact timings/allocation ranges,
source adaptation and current limits.

Pinned upstream: `fed0bf24149fb1ed36039212648bafdafc1ea10e`. Baseline: `e3131a5b`.
The source inventory below records the initial baseline and subsequent dependency checkpoints.
Current implementation is source-complete and accepted within the contract.
The [contract](EMIT-CONTRACT.md) defines completion and dependencies; the
[test inventory](EMIT-TEST-INVENTORY.md) separately tracks verification.
In-scope counterparts were reconciled function by function against upstream;
partial external owners remain explicitly outside the contract.
Agent progress records list implemented symbols, adaptations and review evidence.

## Current integrated state

The remaining declaration family (181 functions), compiler EmitHost/emitter and Program
output, filesystem write path, and user-facing compilation command are implemented
and independently source-reviewed. Required checker comment ranges and contextual
parameter-cache behavior were restored from upstream when full output testing exposed
omissions. Source mapping records the focused changes to existing owners.

The full 15,330-configuration managed development run completed without runtime
failures. The six formatted content-mapper discrepancies were subsequently repaired
and independently reviewed. Final execution passed 2,223 managed records, including
493 Emit records and 1,730 retained checks. Six fresh Linux x64 Native publications
bind actual ILC root and dependency paths/hashes; direct Emit and retained native
controls passed. The full Native emit corpus observed all 15,330 configurations:
15,285 exact public matches and 13,571 complete original assertion sets. Only six
incremental metadata differences remain visible under the reviewed external boundary.
Command execution passed six disk, three guard and three runtime cases.

The prior candidate matched all 15,304 retained Native noEmit public records.
Fresh scheduling-repair noEmit revalidation also matched all 15,304 records. All 50 isolated in-repository
benchmark observations completed; independent final execution and completion-claim reviews are clear.
The coordinator accepted the contract-qualified scope.

Incremental snapshot/build-info scheduling and language-service-only serialization
remain external owners. The command explicitly rejects unsupported incremental or
composite execution after preserving genuine configuration diagnostics.

## Historical synthesis prerequisite checkpoint

Complete implementations: all six source-map files (52 functions), synthesis
EmitContext (85), Factory (91), NameGenerator (25), generated-name flags (9),
helper descriptors/text/callbacks, emit flags, Transformer and ModifierVisitor.
Required Core.Splice and AST inheritance/borrowed coordinate joins are complete.
At this historical checkpoint printer utilities were partial and the remaining
printer, resolver, transforms and Program output were pending; subsequent families
completed those implementations. The baseline table
below remains historical; live file status is in [source-map.json](../source-map.json).

Independent source/assertion review, 1,107 managed cases and actual Linux x64
Native AOT (78 original functions plus 30 authored controls) passed. See
[the reproducible prerequisite gate](EMIT-VERIFY.md) and
[durable evidence](../validation/results/emit-foundation-verified.json).
This completes prerequisites, not the whole-emit contract or output comparison.

## Historical printer and semantic dependency checkpoint

The next packet completes all 378 printer functions and 51 utilities, the writer
implementations, Binder ReferenceResolver 15 functions, the 44-method resolver
contract and 55 checker EmitResolver functions. Non-hover NodeBuilder declaration
serialization, tracker hooks, CopyOnWrite scopes and the transform chain now use
genuine implementations. The three hover-only methods stay outside emit; the
large checker files are reviewed for their demanded changes, not relabeled as
new full-file ports. Shared transformer Utilities 22 and Destructuring 28 are complete,
as is TypeEraser 5 with its direct original 69-row test table. At this historical checkpoint only the demanded dotted-module RuntimeSyntax leaf
was present. The subsequent complete TS family replaced that partial state.

Independent source and original-assertion review is clear. The integrated managed
gate passed 1,186 cases (169 emit,832 checker,84 type-system,101 selected no-emit), with
no failures/skips. Actual Linux x64 Native AOT published and executed 149 original functions plus 46 authored
controls successfully; independent evidence review confirms this prerequisite checkpoint. Whole transformation/output acceptance remains
separate. See [printer progress](progress/emit-printer.md),
[semantic progress](progress/emit-resolver.md), and
[shared transform progress](progress/emit-transform-shared.md).

## TypeScript transform dependency checkpoint

All seven upstream files are now implemented: TypeEraser 5, ImportElision 7,
RuntimeSyntax 40, Utilities 1, Metadata 19, TypeSerializer 22, LegacyDecorators 48
(142 functions). Complete MultiMap contributes 11 supporting functions. The former
single RuntimeSyntax leaf now lives in its direct counterpart. Necessary narrow
adaptations add borrowed-name/helper overloads and explicitly declare SourceFile's
existing HasFileName interface; no algorithms or identity boundaries changed.

Independent source review, 1,222 managed cases and actual Linux x64 Native AOT
(150 original functions plus 87 authored controls) passed. The 20 original import
elision rows retain original output assertions with documented Program-host adaptation.
Thirty-four authored stage controls compare exact output and full public diagnostics.
This is complete TS-family implementation with scoped behavioral evidence, not full
compiler-output parity. ES, JSX, module and declaration families, Program output and
the whole original corpus remain ahead. See [verification](EMIT-VERIFY.md) and
[bound evidence](../validation/results/emit-ts-transforms-verified.json).

## ES transform dependency checkpoint

All 17 pinned files and 407 functions are implemented: full target-dependent
lowering, async/iteration, resource management, class fields/private elements,
decorators, named evaluation, operators, and shared utilities/chain definitions.
OrderedSet's eight functions and its required OrderedMap key enumerator are present.
Original clones, explicit capacities, byte-backed names and cached callbacks remain;
source review corrected concrete capacity and callback-allocation omissions before
acceptance. The initially estimated NamedEvaluation count was 19; all 20 actual
functions were already implemented and reviewed (metadata correction only).

The first behavioral run passed 287 emit cases; retained gates bring the total to
1,304 managed passes. Actual Linux x64 Native AOT executed 151 original functions
and 168 authored controls successfully. Eighty genuine Program cases compare exact
TS→ES-selector→UseStrict outputs and full public diagnostics against pinned Go.
The ES package has no original direct tests; original compiler-corpus acceptance
remains later work. OrderedSet's original public test is ported; its allocation-count
test is Go-runtime-only evidence, separate from authored CLR hinted behavior.
[Verification](EMIT-VERIFY.md) and [proof](../validation/results/emit-es-transforms-verified.json)
distinguish this subsystem from pending JSX/module/declaration/Program output.

## JSX, module and const-enum dependency checkpoint

All seven pinned files/175 functions are implemented and independently reviewed:
JSX48, ConstEnum3, CommonJS83 and shared module files41. Required adaptations are
borrowed entry enumeration and genuine Program format lookup for HasFileName owners.
The original module utility TODO nil/empty leaves remain pinned upstream limitations;
they are not new fallback implementations. The 253 JSX entity records are preserved.

The 75 new authored comparisons use genuine Program/library environments, exact
TS→JSX→ES→UseStrict→Module→ConstEnum output and five complete public pre-emit
diagnostic channels. Managed acceptance totals1,379 passes. Actual Linux x64 Native
AOT executed151 original functions plus243 authored controls successfully. A real
const-enum borrowed-string consumer omission was fixed without altering expectations.
[Proof](../validation/results/emit-script-transforms-verified.json) separates this
family from pending declaration transforms and full Program/compiler-corpus output.

## Package responsibilities

| Upstream file | Lines / functions | Baseline and active disposition |
|---|---:|---|
| [printer/changetrackerwriter.go](../upstream/TypeScript/tsc/internal/printer/changetrackerwriter.go) | 250 / 36 | Missing at baseline |
| [printer/emitcontext.go](../upstream/TypeScript/tsc/internal/printer/emitcontext.go) | 1046 / 85 | Existing partial counterpart; full emit not accepted; implementation assigned to synthesis lane |
| [printer/emitflags.go](../upstream/TypeScript/tsc/internal/printer/emitflags.go) | 38 / 0 | Existing partial counterpart; full emit not accepted; implementation assigned to synthesis lane |
| [printer/emithost.go](../upstream/TypeScript/tsc/internal/printer/emithost.go) | 22 / 0 | Missing at baseline |
| [printer/emitresolver.go](../upstream/TypeScript/tsc/internal/printer/emitresolver.go) | 128 / 0 | Existing partial counterpart; full emit not accepted |
| [printer/emittextwriter.go](../upstream/TypeScript/tsc/internal/printer/emittextwriter.go) | 36 / 0 | Missing at baseline |
| [printer/factory.go](../upstream/TypeScript/tsc/internal/printer/factory.go) | 1315 / 91 | Missing at baseline; implementation assigned to synthesis lane |
| [printer/generatedidentifierflags.go](../upstream/TypeScript/tsc/internal/printer/generatedidentifierflags.go) | 57 / 9 | Missing at baseline; implementation assigned to synthesis lane |
| [printer/helpers.go](../upstream/TypeScript/tsc/internal/printer/helpers.go) | 558 / 1 | Missing at baseline; implementation assigned to synthesis lane |
| [printer/namegenerator.go](../upstream/TypeScript/tsc/internal/printer/namegenerator.go) | 405 / 25 | Missing at baseline; implementation assigned to synthesis lane |
| [printer/printer.go](../upstream/TypeScript/tsc/internal/printer/printer.go) | 6341 / 378 | Existing partial counterpart; full emit not accepted |
| [printer/semicolon_writer.go](../upstream/TypeScript/tsc/internal/printer/semicolon_writer.go) | 143 / 28 | Existing partial counterpart; full emit not accepted |
| [printer/singlelinestringwriter.go](../upstream/TypeScript/tsc/internal/printer/singlelinestringwriter.go) | 159 / 27 | Missing at baseline |
| [printer/sourcefilemetadataprovider.go](../upstream/TypeScript/tsc/internal/printer/sourcefilemetadataprovider.go) | 10 / 0 | Missing at baseline |
| [printer/syntheticfile.go](../upstream/TypeScript/tsc/internal/printer/syntheticfile.go) | 53 / 2 | Missing at baseline |
| [printer/textwriter.go](../upstream/TypeScript/tsc/internal/printer/textwriter.go) | 227 / 33 | Existing partial counterpart; full emit not accepted |
| [printer/utilities.go](../upstream/TypeScript/tsc/internal/printer/utilities.go) | 935 / 51 | Existing partial counterpart; full emit not accepted |
| [sourcemap/decoder.go](../upstream/TypeScript/tsc/internal/sourcemap/decoder.go) | 253 / 17 | Missing at baseline; implementation assigned to source-map lane |
| [sourcemap/generator.go](../upstream/TypeScript/tsc/internal/sourcemap/generator.go) | 387 / 20 | Missing at baseline; implementation assigned to source-map lane |
| [sourcemap/lineinfo.go](../upstream/TypeScript/tsc/internal/sourcemap/lineinfo.go) | 30 / 3 | Missing at baseline; implementation assigned to source-map lane |
| [sourcemap/source.go](../upstream/TypeScript/tsc/internal/sourcemap/source.go) | 12 / 0 | Missing at baseline; implementation assigned to source-map lane |
| [sourcemap/source_mapper.go](../upstream/TypeScript/tsc/internal/sourcemap/source_mapper.go) | 402 / 11 | Missing at baseline; implementation assigned to source-map lane |
| [sourcemap/util.go](../upstream/TypeScript/tsc/internal/sourcemap/util.go) | 29 / 1 | Missing at baseline; implementation assigned to source-map lane |
| [transformers/chain.go](../upstream/TypeScript/tsc/internal/transformers/chain.go) | 62 / 2 | Missing at baseline |
| [transformers/declarations/diagnostics.go](../upstream/TypeScript/tsc/internal/transformers/declarations/diagnostics.go) | 736 / 32 | Missing at baseline |
| [transformers/declarations/supplementalreferences.go](../upstream/TypeScript/tsc/internal/transformers/declarations/supplementalreferences.go) | 51 / 3 | Missing at baseline |
| [transformers/declarations/tracker.go](../upstream/TypeScript/tsc/internal/transformers/declarations/tracker.go) | 258 / 21 | Missing at baseline |
| [transformers/declarations/transform.go](../upstream/TypeScript/tsc/internal/transformers/declarations/transform.go) | 3005 / 110 | Missing at baseline |
| [transformers/declarations/util.go](../upstream/TypeScript/tsc/internal/transformers/declarations/util.go) | 186 / 15 | Missing at baseline |
| [transformers/destructuring.go](../upstream/TypeScript/tsc/internal/transformers/destructuring.go) | 510 / 28 | Missing at baseline |
| [transformers/estransforms/async.go](../upstream/TypeScript/tsc/internal/transformers/estransforms/async.go) | 984 / 42 | Missing at baseline |
| [transformers/estransforms/classfields.go](../upstream/TypeScript/tsc/internal/transformers/estransforms/classfields.go) | 3618 / 134 | Missing at baseline |
| [transformers/estransforms/classthis.go](../upstream/TypeScript/tsc/internal/transformers/estransforms/classthis.go) | 28 / 1 | Missing at baseline |
| [transformers/estransforms/definitions.go](../upstream/TypeScript/tsc/internal/transformers/estransforms/definitions.go) | 44 / 1 | Missing at baseline |
| [transformers/estransforms/esdecorator.go](../upstream/TypeScript/tsc/internal/transformers/estransforms/esdecorator.go) | 2751 / 87 | Missing at baseline |
| [transformers/estransforms/exponentiation.go](../upstream/TypeScript/tsc/internal/transformers/estransforms/exponentiation.go) | 90 / 5 | Missing at baseline |
| [transformers/estransforms/forawait.go](../upstream/TypeScript/tsc/internal/transformers/estransforms/forawait.go) | 856 / 29 | Missing at baseline |
| [transformers/estransforms/logicalassignment.go](../upstream/TypeScript/tsc/internal/transformers/estransforms/logicalassignment.go) | 113 / 3 | Missing at baseline |
| [transformers/estransforms/namedevaluation.go](../upstream/TypeScript/tsc/internal/transformers/estransforms/namedevaluation.go) | 535 / 20 | Missing at baseline |
| [transformers/estransforms/nullishcoalescing.go](../upstream/TypeScript/tsc/internal/transformers/estransforms/nullishcoalescing.go) | 49 / 3 | Missing at baseline |
| [transformers/estransforms/objectrestspread.go](../upstream/TypeScript/tsc/internal/transformers/estransforms/objectrestspread.go) | 593 / 24 | Missing at baseline |
| [transformers/estransforms/optionalcatch.go](../upstream/TypeScript/tsc/internal/transformers/estransforms/optionalcatch.go) | 37 / 3 | Missing at baseline |
| [transformers/estransforms/optionalchain.go](../upstream/TypeScript/tsc/internal/transformers/estransforms/optionalchain.go) | 240 / 11 | Missing at baseline |
| [transformers/estransforms/taggedtemplate.go](../upstream/TypeScript/tsc/internal/transformers/estransforms/taggedtemplate.go) | 175 / 8 | Missing at baseline |
| [transformers/estransforms/usestrict.go](../upstream/TypeScript/tsc/internal/transformers/estransforms/usestrict.go) | 50 / 3 | Missing at baseline |
| [transformers/estransforms/using.go](../upstream/TypeScript/tsc/internal/transformers/estransforms/using.go) | 799 / 23 | Missing at baseline |
| [transformers/estransforms/utilities.go](../upstream/TypeScript/tsc/internal/transformers/estransforms/utilities.go) | 289 / 10 | Missing at baseline |
| [transformers/inliners/constenum.go](../upstream/TypeScript/tsc/internal/transformers/inliners/constenum.go) | 102 / 3 | Missing at baseline |
| [transformers/jsxtransforms/jsx.go](../upstream/TypeScript/tsc/internal/transformers/jsxtransforms/jsx.go) | 1209 / 48 | Missing at baseline |
| [transformers/modifiervisitor.go](../upstream/TypeScript/tsc/internal/transformers/modifiervisitor.go) | 28 / 2 | Missing at baseline |
| [transformers/moduletransforms/commonjsmodule.go](../upstream/TypeScript/tsc/internal/transformers/moduletransforms/commonjsmodule.go) | 2166 / 83 | Missing at baseline |
| [transformers/moduletransforms/esmodule.go](../upstream/TypeScript/tsc/internal/transformers/moduletransforms/esmodule.go) | 372 / 11 | Missing at baseline |
| [transformers/moduletransforms/externalmoduleinfo.go](../upstream/TypeScript/tsc/internal/transformers/moduletransforms/externalmoduleinfo.go) | 389 / 17 | Missing at baseline |
| [transformers/moduletransforms/impliedmodule.go](../upstream/TypeScript/tsc/internal/transformers/moduletransforms/impliedmodule.go) | 53 / 3 | Missing at baseline |
| [transformers/moduletransforms/utilities.go](../upstream/TypeScript/tsc/internal/transformers/moduletransforms/utilities.go) | 118 / 10 | Missing at baseline |
| [transformers/transformer.go](../upstream/TypeScript/tsc/internal/transformers/transformer.go) | 41 / 5 | Missing at baseline |
| [transformers/tstransforms/importelision.go](../upstream/TypeScript/tsc/internal/transformers/tstransforms/importelision.go) | 153 / 7 | Missing at baseline |
| [transformers/tstransforms/legacydecorators.go](../upstream/TypeScript/tsc/internal/transformers/tstransforms/legacydecorators.go) | 1038 / 48 | Missing at baseline |
| [transformers/tstransforms/metadata.go](../upstream/TypeScript/tsc/internal/transformers/tstransforms/metadata.go) | 400 / 19 | Missing at baseline |
| [transformers/tstransforms/runtimesyntax.go](../upstream/TypeScript/tsc/internal/transformers/tstransforms/runtimesyntax.go) | 995 / 40 | Missing at baseline |
| [transformers/tstransforms/typeeraser.go](../upstream/TypeScript/tsc/internal/transformers/tstransforms/typeeraser.go) | 400 / 5 | Missing at baseline |
| [transformers/tstransforms/typeserializer.go](../upstream/TypeScript/tsc/internal/transformers/tstransforms/typeserializer.go) | 511 / 22 | Missing at baseline |
| [transformers/tstransforms/utilities.go](../upstream/TypeScript/tsc/internal/transformers/tstransforms/utilities.go) | 29 / 1 | Missing at baseline |
| [transformers/utilities.go](../upstream/TypeScript/tsc/internal/transformers/utilities.go) | 375 / 22 | Missing at baseline |

## Required consumer integration

- Compiler emitter/EmitHost and Program emit/declaration-diagnostic entry points: output selection/path/blocking scaffolding exists; execution and full diagnostic publication incomplete.
- Printer EmitResolver and checker counterpart: existing diagnostic/links subset; reconcile all 38 direct plus six inherited interface methods and corresponding checker implementation.
- Checker NodeBuilder/serialization/tracker: diagnostic support exists; complete declaration-emission responsibilities and verify genuine environments.
- AST factory/update/clone/visitor and generated structures: accepted foundations retained; minimal inheritance join and concrete emit dependencies only, with generator changes preserved.
- OutputPaths/options/VFS/configuration: accepted foundations retained; wire full write behavior and investigate only concrete omissions.
- execute/tsc command integration: production disk invocation and observable output/error behavior required; language services and unrelated builder/watch orchestration excluded.

Function counts are lexical upstream declarations, not coverage or test totals. The
accepted prerequisite checkpoint and live packet above supersede baseline statuses. Source-file mappings are updated when actual counterparts
are implemented; missing files listed here are not silently marked ported.
