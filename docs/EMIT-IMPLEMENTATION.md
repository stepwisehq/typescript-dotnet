# Emit implementation inventory

Pinned upstream: `fed0bf24149fb1ed36039212648bafdafc1ea10e`. Baseline: `e3131a5b`.
This is the initial source inventory, not an implementation or execution claim.
The [contract](EMIT-CONTRACT.md) defines completion and dependencies; the
[test inventory](EMIT-TEST-INVENTORY.md) separately tracks verification.
Existing partial files must be reconciled function by function against upstream.
Agent progress records list implemented symbols, adaptations and review evidence.

## Current prerequisite checkpoint

Complete implementations: all six source-map files (52 functions), synthesis
EmitContext (85), Factory (91), NameGenerator (25), generated-name flags (9),
helper descriptors/text/callbacks, emit flags, Transformer and ModifierVisitor.
Required Core.Splice and AST inheritance/borrowed coordinate joins are complete.
Printer utilities remain partial; full printer, resolver/serialization, chains,
lowering transforms and Program output are still pending. The baseline table
below remains historical; live file status is in [source-map.json](../source-map.json).

Independent source/assertion review, 1,107 managed cases and actual Linux x64
Native AOT (78 original functions plus 30 authored controls) passed. See
[the reproducible prerequisite gate](EMIT-VERIFY.md) and
[durable evidence](../validation/results/emit-foundation-verified.json).
This completes prerequisites, not the whole-emit contract or output comparison.

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

Function counts are lexical upstream declarations, not coverage or test totals. No emit
subsystem is accepted yet. Source-file mappings are updated when actual counterparts
are implemented; missing files listed here are not silently marked ported.
