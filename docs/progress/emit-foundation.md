# Emit synthesis foundation — accepted prerequisite checkpoint

Pinned upstream: `fed0bf24149fb1ed36039212648bafdafc1ea10e`. Whole Emit epic is active; this packet is an accepted prerequisite subsystem, not whole-emit or performance acceptance.

Completed and independently reviewed source responsibilities:

- `printer/emitcontext.go`: all85 functions (constructor spelling adapted), including nested variable/lexical environment lifecycle, prologue merge order, metadata/original links, helper dependencies and visitor hooks.
- `printer/factory.go`: all91 functions (constructor spelling adapted), including generated names and all helper AST construction.
- `printer/namegenerator.go`: all25 functions; scope reuse/reservations, private names and pinned generated-name caches.
- `printer/generatedidentifierflags.go`: all9 functions and complete flags.
- `printer/helpers.go`: all27 original descriptors, exact25 script texts, two callbacks, priorities/dependencies and comparator.
- `printer/emitflags.go`: full flags and composites.
- `printer/utilities.go`: required generated-name formatting, ASCII module-name conversion and span-search functions added to existing counterpart; other printer utilities remain partial.
- `core/core.go:315`: required Slice-based Splice, preserving fresh-owner Concat semantics.
- `transformers/transformer.go` and `modifiervisitor.go`: complete prerequisite implementation. Chain deferred until genuine binder.ReferenceResolver/printer.EmitResolver interfaces exist; no placeholder options.

Language adaptations: printer factory inherits AST factory to represent Go embedding; remove sealed from allfour AST partial factory files, update generate-ast.py template. AST/visitor APIs still receive the same hook-bearing instance. Emit-node records use established stable LinkStore arena; slices retain required header sharing, clone/map copies, source ownership and nil behavior. OrderedSet helper storage uses insertion-ordered Slice plus reference-identity HashSet; ReadEmitHelpers collects a fresh owner and clears retained entries. Emitted names and script text are string output boundaries; AST literal text copying uses ByteStringValue directly. Generic AST visitor/deepclone algorithms were not reopened.

Concrete prior diagnostic subset divergence: its SetOriginal copied synthesized leading/trailing comments. Pinned emitNode.copyFrom does NOT copy comments/typeNode and DOES copy flags, ranges, token map clone, helper slice clone, external-helper module name and optional snippet value. Source now follows pinned copying. Two diagnostic Printer foreach consumers join Slice comment API; existing diagnostic expectations must remain unchanged, with any failure investigated. No expectation edits.

Validation: all36 original name-generator functions are ported by inventory lane into shared Emit.Checks; coordinated compile and new context/factory public controls pending. No compiler builds, behavioral gates, Native AOT publication, benchmark or acceptance claim by this lane.

Independent review corrections: Core.Splice widens the delete endpoint addition before clamping to preserve 64-bit Go behavior for int.MaxValue; control added. NewStringLiteralFromNode starts with explicit Go-empty ByteStringValue rather than the managed default/null projection; nontext-source control added. Factory named return locals removed where all exits return tuples directly; synthesized comments use explicit constructor to resolve Slice.Append overload.

Snippet adaptation: optional readonly SnippetElement value preserves setter capture and detached copyFrom contents. Reviewer traced every pinned consumer: printer reads the snippet (printer.go:4156); LS completion setters are outside Emit epic and no getter mutation/pointer-identity consumer exists. Root explicitly retained the proportionate value adaptation. Future mutable getter/identity consumers require reassessment; no blanket pointer-identity parity is claimed.

Coordinated build3 and all90 current Emit xUnit cases passed, zero warnings.
Actual Linux x64 Native AOT passed78 original functions and30 authored controls;
retained Checker832, TypeSystem84 and selected no-emit101 cases passed. Independent
source, original-assertion and final-evidence review is clear. See
[durable proof](../../validation/results/emit-foundation-verified.json).
Full printer/transforms/resolver/Program output acceptance remains pending.
