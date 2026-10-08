# Scanner feasibility checkpoint

**Accepted for the bounded scanner phase**, on Linux x64, against maintained
TypeScript revision `fed0bf24149fb1ed36039212648bafdafc1ea10e`. The actual Native
AOT executable passed the Go differential corpus. No acceptance blocker remains.
This is evidence for a scanner port, not proof of full compiler feasibility.

## Delivered behavior and limits

The port includes ordinary scanning, contextual rescans, JSX/JSDoc modes, regex
syntax validation, Unicode identifiers and JavaScript string values, numeric
literals, token flags, diagnostics, directives, state save/restore, and pure
text/position/comment helpers. Source boundaries and function names are retained
where practical. [source-map.json](../source-map.json) records each translated
file, partial dependency slice, new support file, and excluded compiler helper.

Fourteen AST/source-file functions in `scanner.go` are excluded. `utilities.go`
contains only the token predicate required by the scanner; its remaining helpers
are excluded. There is no parser, binder, type checker, emitter, or compiler AST.
The Go oracle imports the real upstream dependencies; the C# library does not.

All required gates passed: 1,287 xUnit tests, fresh pinned-Go baseline comparison,
upstream Go scanner package tests, a warning-free Release build, and Native AOT
publication/execution. The native corpus has 1,276 cases, 17,977 token snapshots,
1,547 diagnostics, and 178 helper results, including 120 upstream source samples.
It exercises 69 of 70 referenced lexical diagnostic codes; code 1513 was not
observed. Detailed commands, hashes, limitations, and tool versions are in
[VERIFICATION.md](./VERIFICATION.md).

These are deterministic boundary and regression checks, not exhaustive fuzzing
or branch coverage. Public input preconditions and arbitrary combinations of
scanner operations are not exhaustively tested. Only Linux x64 was AOT-verified.
Source/corpus/binary hashes tie the recorded evidence to this checkpoint.

## Adaptations and dependency cost

- Internal strings store one UTF-8 byte per .NET character to preserve Go byte
  indexing. Explicit public conversions and raw-byte APIs retain malformed source
  and lone-surrogate values. This costs two bytes of character storage per source
  byte, plus allocations; spans avoid repeated copying of source suffixes.
- .NET `BigInteger`, invariant `double` parsing/round-trip formatting, collections,
  and `StringBuilder` replace corresponding Go facilities. Integer-to-double
  conversion explicitly rounds ties to even, and numeric presentation is adjusted
  to the upstream ECMAScript conventions. Numeric boundary tests support this
  adaptation; exhaustive binary64 equivalence is not established.
- Identifier/property tables remain pinned upstream data. Runtime case handling
  produced observable Kelvin-sign/sharp-S suggestion mismatches, so a small pinned
  Go Unicode case-mapping slice replaces that use of .NET casing. Its complete
  scalar mapping hash matched Go. Apache and Go BSD notices are preserved.
- Go regex syntax scanning is ported directly; .NET regex matching is not used as
  a syntax substitute. Immutable comment records replace compiler NodeFactory
  allocation. Directive getters and Mark/Rewind use snapshots rather than exposing
  Go slice-storage aliasing; snapshot allocation is a possible future cost.
- Generic initialization and Go helpers are adapted into narrow C# support.
  System.Text.Json source generation keeps the validation executable compatible
  with Native AOT. The xUnit runner remains managed.

The library has 22 C# files and 8,628 lines, including generated tables and
compatibility support. This is a measured inventory, not handwritten effort or a
compiler extrapolation. The native validation binary is 3,018,152 bytes and needs
libc/libm but no .NET runtime installation. Its 61 ms corpus execution includes
process startup and JSON protocol work; it is not a scanner throughput benchmark.

## Working process and observed effort

Three explicitly selected GPT-6.1 Sol agents handled scanner translation, support
and regex translation, and verification. The same implementation agents performed
independent cross-review. The main session owned scope, decisions, coordination,
documentation, and acceptance. Main-session model identity was not exposed by
the metadata tool, so Astra selection could not be independently verified.

Measured elapsed time from the first toolchain/source inspection at 14:46 UTC to
the final native publication at 15:05:47 UTC was about 20 minutes, with three
concurrent workers at peak. This is wall time for this environment and task, not
summed agent effort. Token usage and monetary/subscription cost were unavailable;
no prices or token estimates are inferred.

The user corrected the initial archived-repository choice and clarified recovery
requirements. One optional system-package request was sent after the user offered
root access. A local extracted compiler completed AOT verification without manual
package installation being required. The user's optional SQLite installation was
not needed. No human agent management or manual implementation fixes were needed.

Helpful strategies were early protocol agreement, split file ownership, raw-byte
oracle results, a small initial corpus followed by gap-driven expansion, reuse of
agents for review, and freezing/hashing source before final verification. Together
they caught a lost translated loop body, Unicode boundary conversion defects, and
runtime case-folding differences without changing expected upstream behavior.

Wasted work included fetching the archived staging repository first, bootstrapping
the initially inferred Go version before switching to the maintained source, and
an extra native publish after a late formatting edit. Mechanical translation
accelerated the first draft but introduced a real loop defect and required C#
cleanup. It should remain a scaffold behind differential verification. The local
toolchain workaround took effort but ultimately removed the AOT blocker.

[START-HERE.md](../START-HERE.md), the verbatim original prompt, subsystem
checkpoints, and the source map support recovery without reconstructing chat.
These files provide process-loss recovery, not an external backup against disk loss.

## Recommended next bounded phase

A scanner-hardening phase is justified before broader compiler work. Use
grammar-directed differential fuzzing and minimization to test longer stateful
operation sequences, regex combinations, numeric formatting, and the unobserved
diagnostic. Measure throughput and allocation against Go on real large sources,
especially byte-string storage and directive snapshots; verify another AOT target
only if deployment needs it. Establish a small retained regression corpus for each
discovered defect. A later parser/AST isolation investigation should have its own
scope and budget. This study does not establish its dependency or semantic cost.

Subsequent user-authorized measurement: [PERFORMANCE.md](./PERFORMANCE.md) now
provides a basic throughput/allocation comparison. The current Native AOT port was
about 1.9–2.7 times slower across three workloads, with greater allocation volume.
These results include its source-representation conversion cost. They strengthen
the case for profiling allocations before choosing optimizations, without
identifying the individual causes of the timing gap or changing this checkpoint's
behavioral acceptance.
