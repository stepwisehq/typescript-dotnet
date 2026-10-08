# Parser feasibility checkpoint

**Accepted bounded checkpoint, 2026-10-08.** The scanner checkpoint was committed
as `0a50e67` before parser work. Parser implementation, verification and performance
evidence form the next local checkpoint; nothing has been pushed.
The upstream revision remains `fed0bf24149fb1ed36039212648bafdafc1ea10e`.

## Implemented boundary

The port contains initial source-file parsing, required syntax AST payloads and
factories, traversal and cloning, syntax diagnostics, syntax reference metadata,
and the parser's JSDoc/lazy-resolution and reparsing paths. The supported-mode
target is explicit JS, JSX, TS, TSX and JSON, including declaration filenames and
external-module JSX/force options. Binding, checking, module resolution and code
generation are excluded. Syntax import classification does not perform resolution.

## Language and dependency costs

- Source and AST strings retain the scanner's internal UTF8 byte representation.
  Public .NET text enters through an explicit conversion; raw byte overloads
  preserve invalid input. Positions remain byte offsets.
- Tagged nodes and typed payloads preserve upstream identity and syntax fields.
  Flattened interfaces replace Go embedding; managed allocation replaces arenas.
  Arena zero-length results must preserve Go's nil behavior, as demonstrated by
  malformed-input comparisons.
- Go slice headers become managed lists with explicit missing-list identity.
  Append and slice adapters copy lists to isolate saved parser-state lengths;
  referenced nodes remain shared. This is an allocation cost accepted for fidelity,
  not an optimization. Fresh managed parsers replace Go pooling; costs are
  unmeasured. Performance investigation remains deferred.
- Syntax-only AST support is a substantial dependency: factories, visitors,
  clone/update routines, flags, syntax predicates and source metadata are needed.
  Binder symbols, flow state, service caches and checker-owned synthetic nodes
  remain excluded.
- External-module JSX detection needs upstream subtree pruning. A private JSX
  projection preserves relevant syntax propagation; the full public transform
  facts API is excluded.
- The C# validation encoder uses static typed metadata for Native AOT. The Go
  oracle discovers concrete upstream syntax fields independently using reflection,
  and separately records the actual child traversal order.

## Verification evidence

The final frozen-source gate passed: 464 parser xUnit tests and actual Native AOT
agreement on 460 cases, 19,268 nodes and 318 diagnostics. Scanner regressions passed
1,287 xUnit tests and 1,276 native cases plus 178 helper results. Independent
parser/runtime and AST/harness reviews are complete. The comparison includes
recovery nodes, null/empty/missing lists, raw scalars, flags, parents, child order
and separate diagnostic channels. See [verification](./PARSER-VERIFICATION.md)
and [machine-readable evidence](../validation/results/parser-verified.json).

All 186 payload types directly referenced by parser factory calls were observed,
but this is not branch coverage. Twenty-nine directly referenced diagnostic codes
remain unobserved. The corpus includes 207 units extracted from 142 pinned upstream
files, ten committed fuzz regressions and targeted valid/malformed mode cases.
Other platforms, incremental editing, full AST/compiler services, position maps,
general transform facts and exhaustive recovery behavior remain unverified or
explicitly excluded. The isolated-entity-name API is outside corpus acceptance.

The first differences revealed empty-list versus nil semantics, default list
spans and JSX module detection. Review also caught swapped supporting enum values.
A separate Go Tristate JSON encoding mismatch was a harness representation issue;
the final protocol compares actual numeric enum values. Review also found valid
nil JSDoc callback results and zero-addition append semantics. Expected behavior
was always taken from unchanged Go; it was not adjusted to fit C#.

## Process observations

Three explicitly selected GPT-6.1 Sol agents were reused for parser, AST support
and independent verification. Shared compilation has one owner; each agent owns
distinct files. Persistent generators and subsystem checkpoints reduce recovery
and repeated translation work. Independent canonical output found differences
that successful compilation could not establish.

The AST dependency closure delayed the first integrated parse, and several
checkpoints briefly retained stale next steps during compile triage. Keep state
updates tied to verified milestones and reject unsupported generator constructs.
Cross-review and source freeze preceded the final native gate. Regenerating the
five parser algorithm files in scratch produced byte-for-byte matches after the
documented adapters and formatting. Handwritten runtime/support files remain
authoritative and are not overwritten by regeneration.

Token counts and subscription cost are unavailable; no cost estimate is inferred.
Elapsed time is not a substitute for total effort. No additional user intervention
was required for parser implementation or Native AOT verification. The recorded
phase window began at 15:29 UTC and the final gate ended at 16:30 UTC; that elapsed
window includes coordination and scanner commit work, not measured active labor.
The final combined gate took 24.001 seconds. SQLite and new process infrastructure
were unnecessary; source mapping and small subsystem checkpoints sufficed.

## Assessment and next bounded phase

A subsequently authorized basic benchmark is complete; see
[PARSER-PERFORMANCE.md](./PARSER-PERFORMANCE.md). It measured the unchanged accepted
parser against Go, with no optimization or deeper diagnosis. The real TypeScript
workload showed about17% more elapsed time and2.2x heap allocation volume in C#;
small fixture groups differ and do not establish application-wide performance.

The milestone is complete: supported initial parsing produces syntax trees and
diagnostics with meaningful upstream agreement in a real Native AOT executable.
It is not full parser parity. The AST dependency was sizeable but remained within
syntax; no binder, checker, resolver or emitter implementation was needed.

Another bounded phase is justified. First target the remaining diagnostic and
malformed-recovery gaps, larger real-program parsing, and required public AST API
behavior. A separate binder dependency inventory can then estimate the next
implementation boundary before authorizing it. Do not begin semantic compiler
work or deferred performance investigation from this checkpoint alone. The results
support the syntax port and this working process; they do not establish that the
full compiler will have similar dependency costs or verification difficulty.
