# Parser phase plan and acceptance boundary

Status: **accepted bounded checkpoint**; all four milestones completed. See
[verification](./PARSER-VERIFICATION.md) and [assessment](./PARSER-ASSESSMENT.md).
This plan records the chosen boundary, not a claim of full parser parity.
Source pin is unchanged from the scanner phase.
Scanner checkpoint commit: `0a50e67`. See [PARSER-PROMPT.md](./PARSER-PROMPT.md)
for the user mandate and [STATE.md](./STATE.md) for current progress.

## Dependency boundary

The main parser directly depends on scanner, AST, core, collections, diagnostic,
debug and string helpers. It does not directly require a binder or checker.
AST definitions mix syntax fields with compiler/service state; only the required
syntax fields, constructors, traversal, cloning, flags and diagnostics are in scope.
Managed allocation replaces Go arenas; flattened typed payloads represent Go
embedded syntax bases behind the tagged Node wrapper. These adaptations must be
documented in the source map and verified structurally, not assumed equivalent.

Included candidate files are `parser.go`, `types.go`, `utilities.go`, `references.go`,
`jsdoc.go` and `reparser.go`. JSDoc reparsing is part of the upstream syntax
pipeline, not permission to implement semantic checking. Reference/import metadata
collection is syntax-only; no module lookup/resolution is permitted. Source-file
path wrappers may implement filename/extension predicates needed by parsing.

Supporting AST slices include `ast.go`, `ast_generated.go`, complete kind and
parse flags, syntax utilities/precedence, parse options, diagnostics, and required
cloning/subtree facts. New dependencies must be reviewed against this boundary.
Do not introduce fake symbols, checker results, or default-success placeholders.

## Scanner readiness

The committed scanner matched 1,276 Native AOT cases and 178 helper outputs, with
1,287 xUnit tests. Its exact source hash matched the recorded result before this
phase. Known limits are 14 excluded AST/source-file helpers, a partial utilities
file, directive snapshot adaptation, and unobserved lexical diagnostic 1513.

Bring in the node/text helpers actually required by parser/JSDoc. Keep internal
Go byte strings across the parser/scanner boundary; the public scanner API's .NET
Unicode conversion must not double-encode source or lose surrogate sentinels.
Use narrow internal raw-string accessors where necessary and retain public API
regressions. Exercise speculative parsing and directive rewind at parser level to
check the snapshot adaptation. The unobserved diagnostic is a coverage limitation,
not license to change its behavior or block unrelated parsing automatically.

## Supported-mode target

The target is initial source-file parsing with explicit JS, JSX, TS, TSX and JSON
script kinds; unknown kind inference is not part of the public contract until
implemented and tested. Include declaration files via filenames. Compare external
module JSX/force options because they affect parsing and top-level await recovery.
JSDoc attachment is explicit: ordinary eager behavior and an optional deterministic
lazy-resolution walk are separate verification modes. No mode is accepted merely
because the parser returns a tree. Unsupported behavior must fail visibly or be
excluded by the documented entry point/corpus, never silently omitted.

Binding, type checking, module resolution, code generation, incremental edits,
language services, performance work, and cross-platform AOT proof are excluded.
Parse/reparse routines needed by the supported source-file modes remain in scope.

## Verification contract

An independent Go oracle reflects actual upstream syntax payloads and separately
records upstream child traversal order. It must not reuse the C# AST generator's
field filter as its expected-tree specification. Canonical output includes:

- Node identity within a flat graph, kind, byte span, full parse flags, payload type,
  named child roles, list ordering/spans/flags, null versus empty/missing lists,
  scalar values (raw-byte Base64 where appropriate), operators and literal flags.
- Ordered child traversal and parent relationships, without recursive JSON cycles.
- Relevant source metadata and ordered parse, JS and JSDoc diagnostic channels:
  code/category/span, messages/arguments as exposed upstream, related information.

Keep every recovery node and zero-width missing node. Normalize nondeterministic
map order only; do not reorder child lists or diagnostics to conceal differences.
Use valid grammar cases, ambiguity/speculation, truncated and malformed cases,
upstream regression/fuzz seeds, and selected real/conformance sources. Initial
smoke cases establish the contract; they cannot establish phase completeness.
Retain scanner regressions. Run parser comparisons and applicable scanner behavior
using actual Native AOT executables, not managed tests alone.

## Milestones and stopping condition

1. **Boundary and contracts:** dependency inventory, scanner gap assessment,
   shared AST/raw-text API, working Go structural oracle.
2. **First integrated slice:** compile and compare small TS expressions,
   declarations and a malformed source. This is a milestone, not parser acceptance.
3. **Supported grammar/modes:** integrate source metadata, JS/JSDoc and required
   reparsing; expand corpus, distinguish implementation from harness defects,
   record function/file completeness and every actual exclusion.
4. **Checkpoint:** resolve independent review findings, freeze source, execute
   full parser Native AOT and scanner regression gates, record hashes and results,
   and assess remaining parity/cost risks. Stop without starting later compiler work.

If an excluded compiler stage becomes necessary, report the concrete dependency
before expanding. If a milestone remains incomplete or blocked, label it accordingly
and persist a precise next action. Do not call the phase accepted based on a build,
no-crash result, valid-input acceptance, or an exhausted time estimate.

## Execution improvements

Reuse the three Sol agents by domain, with disjoint parser/AST/verification files.
Keep the AST/API contract small and settle it early. Persist generators in the
repository; generators must reject unsupported constructs instead of dropping loop
bodies or returning placeholder values. Inspect generated output through the
independent oracle. Record compile/coverage progress at useful boundaries, and
prefer small integrated milestones to large untested generated changes. Cross-review
different owners' code, and freeze source before the final AOT run. No optimization
work is authorized by this phase.
