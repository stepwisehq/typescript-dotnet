# ES class fields — active

From accepted TypeScript-transform checkpoint `b0cf6af4`, maps owns the complete
pinned `fed0bf24149fb1ed36039212648bafdafc1ea10e`
[classfields.go](../../upstream/TypeScript/tsc/internal/transformers/estransforms/classfields.go)
(134 functions) and
[classthis.go](../../upstream/TypeScript/tsc/internal/transformers/estransforms/classthis.go)
(one function). The counterparts are
[ClassFields.cs](../../src/TypeScript.Scanner/Transformers/EsTransforms/ClassFields.cs)
and [ClassThis.cs](../../src/TypeScript.Scanner/Transformers/EsTransforms/ClassThis.cs).
All bodies are written and the initial integration joins/source reconciliation
are complete. Production is build-ready and frozen for independent source review
and the next central family compilation; three compile-only attempts exposed
managed declaration/API joins. The ClassFields batch now uses the actual Visitor
delegate, record-kind guarded equality, enum complements and typed nil Slice
headers; original branches and ownership are retained. Independent review restored
all four original explicit Slice capacity hints and cached the modifier callback
once after factory setup. The complete independent source pass is clear and bound
in [es-classfields.json](../../artifacts/emit/source-review/es-classfields.json).
Acceptance is pending. No independent
builds run.

The real factory is `ClassFieldsTransformer.NewClassFieldsTransformer`; its
ESNext/useDefineForClassFields bailout returns null as upstream does. Genuine
package-level helpers live in shared `EsTransforms`: ClassThis checks and the
four ClassFields helpers consumed by ESDecorator. Foundation owns the complete
ESDecorator and NamedEvaluation counterpart; inventory owns remaining ES files,
utilities and complete definitions. No chain uses placeholders.

Mutable private-identifier info records retain reference identity across paired
accessor updates and lexical lookup. Class environments retain linked lifetimes;
normal private names use borrowed byte keys, generated private names use original
node identity keys. Pending expressions/statements retain slice headers and
required original copies. Nine upstream visitors and named-evaluation callbacks
are retained. The comma-list sequence stores its original root by value and calls
the same recursive worker through a cached collector, avoiding per-use iterator
and closure allocations. Generated concatenated names own their required output;
source-name inspections and literal construction remain borrowed.

Foundation independently reviews this source. Maps independently reviews
foundation ESDecorator/NamedEvaluation: all 107 original functions are source-clear,
with the resolved four cached descriptor callbacks bound in
[the independent review](../../artifacts/emit/source-review/es-decorators-named.json).
Twenty-one authored ClassFields cases are captured from pinned Go in
[es_classfields_cases.json](../../tools/emit-oracle/es_classfields_cases.json) for
inventory’s combined 80-case reference capture (all passed). Exact shared
[ClassFieldsChecks.cs](../../validation/Emit.Checks/ClassFieldsChecks.cs) constants
are independently clear in
[es-classfields-controls.json](../../artifacts/emit/source-review/es-classfields-controls.json).
Shared assertions use the genuine Program/bundled-library fixture, exact TS
then whole-ES target-selected chain, complete public diagnostic channels and
complete pinned-Go LF output. They are separate from original assertions and
whole compiler emission acceptance. The full shared ES harness and runtime45/decorator14 authored assertions are
independently clear in
[es-shared-controls.json](../../artifacts/emit/source-review/es-shared-controls.json).
The coordinated family gate passed: all 287 emit and 1,304 total managed
cases, plus actual Linux x64 Native execution of 151 original functions and
168 authored controls (exit0). Publication took15.651s, execution4.106s.
Final independent evidence ACK and root acceptance commit remain pending.
No local builds run.

Read-only next-family preparation: JSX is one 1,209-line source file; the module
family has five files/3,098 lines (CommonJS 2,166, ESModule 372,
ExternalModuleInfo 389, ImpliedModule 53, Utilities 118). ExternalModuleInfo uses
accepted MultiMap and OrderedSet; inventory now owns complete OrderedSet as an
actual current-ES super-access dependency, removing that future gap. A coherent
later split is JSX, CommonJS, and module metadata/ES/implied/utilities. No
next-family source is edited before the ES checkpoint.
