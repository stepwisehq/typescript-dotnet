# Genuine disk compilation command

The authored existing `tscheck --config` entry now uses genuine Program compilation and output. `Projects/Compilation.cs` maps the nonincremental diagnostic, emission, deduplication, listing and final exit-status branches from pinned `execute/tsc/emit.go`. `--listFilesOnly` is an actual command-line override (pinned Go rejects it inside tsconfig). `--noEmit` remains a real compiler-option override; the existing optional no-GC-region policy is unchanged. The legacy `--project` configuration remains an explicitly check-only compatibility entry.

`Project.Compile` invokes `Program.GetDiagnosticsOfAnyProgram`, skips emission for `ListFilesOnly`, appends real emission diagnostics, sorts/deduplicates, and returns exit0 with no diagnostics, exit1 for diagnostics with skipped output, or exit2 for diagnostics with generated output. `ListEmittedFiles`, `ExplainFiles`, and `ListFiles`/`ListFilesOnly` use genuine Program paths. Retained listing text is decoded at the public result boundary. Compiler filenames and output contents retain their raw-byte channels internally. Project implements the actual four `HasFileName` module-specifier host signatures, while retaining its public SourceFile overloads.

The wrapper is an authored API boundary; it does not claim the complete incremental/watch/statistics command infrastructure. Existing CLI JSON now reports emission status and listing alongside complete diagnostics. `tools/emit-oracle/command_controls.py` supplies six focused real executable/disk controls: output with JS/declaration maps, generated-versus-skipped error statuses, noEmit, ListFilesOnly and Unicode output paths. It builds nothing. Independent review and central managed/actual Native execution remain pending; source presence is not acceptance.

Pinned Go cmd/tsc was built once and executed the same six configurations; exact files/statuses and complete rendered diagnostics are retained in artifacts/emit/command-reference/go-controls.json. The focused executable runner can compare all emitted byte sequences with that reference. The portable original TestContentMapperLoggerEnvironmentVariable is preserved in CommandLoggerChecks and xUnit with narrow environment/writer adapters: nil off, nonnil on, ten synchronized exact lines. This is direct helper evidence; external process mapper hosting and incremental timing remain explicitly outside this disk command wrapper.

The pure logger now writes raw Go byte strings directly to a Stream under the original mutex boundary, with a separate authored Unicode transport control. Foundation independent source/assertion review is clear in artifacts/emit/source-review/command-output.json; actual managed disk runs and Native still remain central gates. The six pinned Go observations were recaptured using read_bytes().decode('utf-8') at the JSON artifact boundary, preserving original CRLF/BOM and complete emitted bytes. No newline-normalizing read_text comparison is used.

The actual command incremental selector is now explicitly bounded: either
Incremental or Composite requests the excluded snapshot/build-info owner.
Compile retains genuine configuration/program option diagnostics before rejecting
that unsupported execution; check-only Project.Check and ordinary Program.Emit
remain their established algorithms. Six original incremental-wrapper output
rows remain explicitly blocked, not accepted as ordinary-Program results; the
source-grounded decision is bound in incremental-boundary.json. Foundation
independently reviewed this narrow command boundary.

Three genuine ordinary Program.Emit mode witnesses are prepared in the durable
Go overlay: single-target selection, forced JS despite NoEmit, and builder
signature despite NoEmit/declaration-disabled. Exact callback bytes and public
result/data fields await the combined Go capture before managed assertions land.

All four authored ordinary Program.Emit public-mode controls are source-written
from actual pinned Go capture: selected target, forced JS despite NoEmit,
builder-signature despite NoEmit, and forced DTS whose real callback marks
SkippedDtsWrite. The last case retains exact callback bytes while its emitted-file
slice remains nil. Shared xUnit/Native assertions and full capture JSON preserve
the original three observations. Inventory independently reviews the final packet.

The three unchanged upstream fixture runtime observers and five original emitter
benchmark variants are independently source-reviewed. `Emit.Validation --benchmark
NAME ITERATIONS` selects the existing benchmark adapter before normal assertions;
without this flag, direct assertion execution is unchanged. Names are
LongLines1000, LongLines5000, LongLines10000, ManyFiles, and
LongLinesWithLineBreaks. Inputs, compiler options, Program-once ownership and
repeated Emit/no-op callback match compiler/emit_test.go. Elapsed time and whole
.NET allocated bytes are observations, not a new acceptance threshold or claims
of identical Go allocation counts. Actual execution remains coordinator-owned.

Three separate authored disk boundary controls are prepared in
`tools/emit-oracle/command_guard_controls.py`: valid incremental and composite
requests must explicitly reject the excluded execution owner without writes;
invalid composite/declaration options must retain genuine TS6304 before that
boundary. These are explicitly not Go-equal output assertions. The prior six
Go-equal disk configurations/reference script remain unchanged. Actual execution
awaits final coordinated Native publication.

Retained verification commands were derived directly from the accepted production
delivery recipe into `/tmp/emit-retained-commands.json`: same managed filters,
foundation Native modes, utilities/Program controls and original 15,304 noEmit
records. No VS Code prerequisite or new runner was added. Fresh managed and RID
compiler dependencies plus actual ILC bindings and metadata/binary freezes are
required before the coordinator executes these prepared commands.
