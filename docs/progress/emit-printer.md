# Full printer implementation — active

Baseline prerequisite checkpoint: `3e78aede`; pinned upstream printer package at
`fed0bf24149fb1ed36039212648bafdafc1ea10e`. The print lane owns complete printer,
text/semicolon/single-line/change-tracker writers, synthetic file and utilities.
Root owns central integration builds and acceptance. Production compilation and first
managed behavior signal are recorded below; actual Native acceptance remains pending.

The complete upstream printer and utility function bodies are translated into
[Printer.cs](../../src/TypeScript.Scanner/Printer/Printer.cs) and
[Utilities.cs](../../src/TypeScript.Scanner/Printer/Utilities.cs); structural and
API joins are complete. Text, semicolon, pooled single-line, change-tracker and
synthetic-file writers, the complete writer interface and direct source-file Source
implementation are complete. Borrowed byte text reaches comment/literal/name/JSX
writers and scanner consumers; snapshots and dictionary zero values were reconciled.

The retained namespace is `TypeScript.DiagnosticPrinting`. NewPrinter accepts all
13 upstream options and eight handlers. Emit/EmitSourceFile and writer String return
Go byte strings; existing diagnostic constructor/Write remain forwarding adapters,
including the existing empty-newline behavior. SourceFile implements Source directly
with borrowed TextView and existing line map, preserving reference identity. Go
comment/source-map snapshots use nullable readonly values where consumers do not
mutate shared snapshot identity. Slice ownership/copies and byte/UTF16 distinctions
remain explicit. Standard writer pooling follows upstream acquisition/release.

Inventory owns the original 66 printer and four utility shared xUnit/Native AOT
functions. Expected text remains unchanged; reparse converts emitted Go bytes at
the parser boundary. All 70 originals now have runnable bodies, including the genuine TypeEraser case.
Independent original assertion and production source review are clear; integrated
managed/Native acceptance remains pending.

Integration build 1 ran centrally: 120 print-lane API/compiler joins were batched
and fixed; build 2 is pending. All writer-role overloads now carry ByteStringValue,
including source identifier/literal/comment producers and template RawTextValue.
Required scanner borrowed comment/shebang entry points forward existing workers.
[StringUtil/EmitText.cs](../../src/TypeScript.Scanner/StringUtil/EmitText.cs) supplies
the original SplitLines/GuessIndentation pair with borrowed headers and the original
slice capacity. Core line-start sequence uses an explicit value enumerator at the
hot writer consumer, preserving both byte and raw-character owners without a yield
allocation or projection. Independent review found and corrected semicolon commits
before indentation and exact NUL-before-digit output (`\\x00`). A string-literal
comparison against the pinned Go AST now matches all 57 utility function bodies and
borrowed overloads. No behavior or performance acceptance is claimed.

Central build 3 reached a zero-warning/error production compiler; harness joins
remain before original execution. Production is frozen through the first behavior
signal. The original printer assertion review is recorded independently in
`artifacts/emit/source-review/printer-tests.json`; 69 of 70 shared functions are
runnable, with the real TypeEraser original remaining explicitly pending. This
source review does not claim managed or Native execution.

The first central managed execution passed all 65 runnable original printer
functions and original triple-slash utility function. Three escaping utility
functions exposed the old decimal-only `Strconv.FormatUint` admission guard;
the demanded adapter now accepts all Go radices 2..36. Invariant .NET decimal and
lowercase hexadecimal formatting handle the common bases; other bases fill a
bounded 64-character stack buffer backwards, then own exactly the result. Illegal
bases use the managed argument exception with the Go panic message. This is the
required formatting join in [BinderCompat.cs](../../src/TypeScript.Scanner/Runtime/BinderCompat.cs),
not a broader strconv port. Shared authored endpoints and illegal-base controls
are in [FormatUintControls.cs](../../validation/Emit.Checks/FormatUintControls.cs).

Independent review also corrected comment-line trimming to the existing exact
GoUnicode.IsSpace predicate (U+200B/U+FEFF must remain), with a separate real-output
witness in [PrinterCommentControls.cs](../../validation/Emit.Checks/PrinterCommentControls.cs).
Numeric property-access dot checks now inspect borrowed literal headers directly.
Production source and authored controls are ready. Full printer independent source
review is clear, bound in [printer.json](../../artifacts/emit/source-review/printer.json).
The activated original partially emitted expression body and dispatch were separately
reviewed against pinned Go; [printer-tests.json](../../artifacts/emit/source-review/printer-tests.json)
now binds all 70 originals with zero pending. TypeEraser production/direct-table
review belongs to the foundation lane. Subsequent managed and actual Native AOT
acceptance remain root-owned obligations.

Integrated test4 passed all original assertions (168 pass, one authored control
failure, no skips). The comment witness expected text omitted the one indentation
space that pinned Go calculateIndent emits before U+200B. Direct pinned Go and
the existing managed DLL produced identical raw bytes, preserving both U+200B
and U+FEFF; corrected authored expectation adds that space. No production change
was required. [Observation](../../artifacts/emit/printer-resolver/comment-control-observation.json)
and [Go witness](../../artifacts/emit/printer-resolver/comment-control-go.log) retain
the evidence; corrected shared assertion execution remains root-owned.
