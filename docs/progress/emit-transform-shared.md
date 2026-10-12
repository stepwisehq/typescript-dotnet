# Shared transform prerequisites

Prior candidate integrated correctness update: 2,223 managed records passed; actual Native Emit
passed 152 original functions and 251 authored controls; all 15,330 emit configurations
were observed, with only six reviewed external incremental metadata deltas visible.
All 13,571 original assertion sets and all 15,304 retained Native noEmit public records
match. Disk/guard/runtime controls passed 6/3/3. A reviewed nested WorkGroup starvation fix is now undergoing fresh coordinated
revalidation. Fresh managed/direct/publication gates and all 50 original workload
observations passed. Fresh Native corpus and command revalidation also passed;
full retained noEmit passed. Independent final reviews are clear; the coordinator accepted the contract-qualified scope. Benchmark summaries/allocation semantics are in the assessment. Earlier checkpoint/pending descriptions below are historical;
[assessment](../EMIT-ASSESSMENT.md) records current scope and freeze qualification.

Whole-emit baseline `e3131a5b`; pinned Go
`fed0bf24149fb1ed36039212648bafdafc1ea10e`. Source implementation is prepared,
without behavior acceptance or a new central build. Owner: `emit_inventory`.

[Utilities.cs](../../src/TypeScript.Scanner/Transformers/Utilities.cs) ports the
all22 upstream functions: shared identifier classification, binding-to-assignment conversion,
super-call path discovery, source-range adjustment and compound operator projection.
[Destructuring.cs](../../src/TypeScript.Scanner/Transformers/Destructuring.cs) ports
all28 upstream functions (constructor adaptation included): both flattening
entrypoints, all assignment/binding mode callbacks, temporary/default
handling, full object/array/rest flattening and exported pattern helpers.

Storage uses established Slice value headers and required append ownership. Pending
variable declarations and rest pairs are value records; the Go pointer to the final
pending declaration is represented by explicit owner-slot writeback. Mode callbacks
retain the flattener state argument with static delegates, avoiding captured closures.
Internal identifier names use borrowed ByteStringValue/TextValue equality and factory
text ownership instead of intermediate string materialization. The constructor adapts
`newFlattener`; optional callback text ranges use nullable copied values, matching actual
read-only upstream consumers. No transform consumer or alternative pipeline was added.

Root coordinates the first coherent printer/resolver/shared-transform build. Independent
source review by another Sol remains required, followed by integrated assertions and
actual Native AOT. Accepted context/factory and source-map families are not reopened.

The next demanded dependency is now source-ready: complete [TypeEraser](../../src/TypeScript.Scanner/Transformers/TsTransforms/TypeEraser.cs), all five upstream functions and the genuine runtimesyntax.go:990 dotted-module leaf. It preserves ancestor restoration through try/finally, every elision/update branch, parameter-property/decorator modifier owners and required nil/accessor-block behavior. Original TestTypeEraser69 rows and the formerly pending printer original are shared xUnit/Native source-ready. [Provenance](../../artifacts/emit/source-review/typeeraser-provenance.json) binds files/options/output assertions. Independent foundation source/assertion review and root integrated build/gates are pending; no other transform families have started.

Independent complete TypeEraser production/table review is now [clear](../../artifacts/emit/source-review/typeeraser-independent-review.json); all69 original options/string rows exact. Activated printer original independently reviewed by the printer owner. Original Go package reference passed2 functions/89 leaf rows. Root integrated managed/Native validation remains pending.

Central test3 stopped before execution on four TypeEraser declaration/API joins: subtree enum versus uint mask, and three unavailable IsTrue extension calls. Corrected explicit uint mask and direct genuine Tristate enum comparisons together; original branches, assertions and upstream anchors unchanged. Foundation narrow hash rebind requested, source frozen again.
