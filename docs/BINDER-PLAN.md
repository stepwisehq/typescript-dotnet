# Source-file binding plan and acceptance boundary

Status: **accepted bounded checkpoint** on 2026-10-08; all four milestones completed.
See [final verification](./BINDER-VERIFICATION.md) and [assessment](./BINDER-ASSESSMENT.md). Parser/performance checkpoint
commit `de25b66`; unchanged TypeScript pin
`fed0bf24149fb1ed36039212648bafdafc1ea10e`. Governing request:
[BINDER-PROMPT.md](./BINDER-PROMPT.md).

## Boundary

Port `tsc/internal/binder/binder.go` and required syntax-local support. Its
BindSourceFile entry point does not call the sibling name/reference resolver
implementations and has no direct checker dependency. Those resolvers, type
checking, cross-file semantic/global merging, module resolution, emit, incremental
rebinding and language services are outside this phase. A concrete dependency on
an excluded stage must be assessed and reported before expansion.

Required support includes complete relevant symbol flags, symbols and local/member/
export tables, declaration/local-export associations, container chains, binder
diagnostics, source-file binding state and graph-shaped control flow. Include
synthetic flow-node payloads omitted from the syntax parser slice. Preserve
identity/sharing, declaration ordering and nil/empty distinctions; managed maps
use ordinal byte-string semantics. Keep upstream filenames and function boundaries.

Syntax-only module-instantiation classification, pattern ambient-module metadata
and CommonJS/assignment binding are allowed when called by the binder. They do not
authorize looking up other files or types. Preserve binder-written NodeFlags even
when named for emit. No general transform-facts or emitter implementation is needed.

Use `Binder.BindSourceFile(SourceFile)` with exact bound-once completion semantics.
Managed allocation may replace Go arenas/pooling as documented in earlier phases;
no performance optimization is authorized. Match Go's publication and failure
behavior, not merely a successful single-thread call. IDs used as graph identity
must not become fake checker state or mask separate symbols.

## Parser readiness before extension

The accepted parser corpus compares pre-bind syntax payloads, child order, parent
identity, flags/spans, lists, raw scalars and ordered syntax-diagnostic channels.
It does not establish symbol, scope or flow fidelity: those fields were explicitly
excluded. All 186 parser-factory payload types were exercised, not every branch.

Classify the 29 unobserved diagnostic codes against actual Go callsites, focusing
on names, declaration recovery, containers and JSDoc declaration transformations
that affect binding. Add concrete missing cases where they reduce that risk and
compare them with unchanged Go. Keep the remaining exclusions visible rather
than reopening all completed grammar work. Record findings in
[BINDER-PARSER-READINESS.md](./BINDER-PARSER-READINESS.md).

## Verification contract

Build an independent Go binder oracle, not expected output from C# schemas.
Retain the pre-bind parser comparison and add post-bind state:

- AST declaration Symbol and LocalSymbol links, Locals and NextContainer,
  flow attachments/return/end/fallthrough state and binder-mutated flags.
- Canonical symbol graph with exact flags, raw names, declaration order,
  ValueDeclaration, Parent, ExportSymbol, Members and Exports. Tables retain
  nil/empty state; normalize unordered map enumeration only.
- Scope/container organization and source-file binding metadata, including
  diagnostics, global-export and pattern ambient-module records where produced.
- Canonical cyclic flow graph: identities, flags, referenced syntax/synthetic
  payloads, antecedent/linked-list edge order and sharing. Counts are coverage
  information, not acceptance criteria.
- Ordered binder diagnostics with message metadata, raw arguments, related
  information and byte spans; verify stable repeated binding and file isolation.

Process-global ID numbers may require canonicalization, including synthetic symbol
names containing such IDs. Preserve their relationship to node/symbol identity;
do not erase distinctions or blindly strip numeric substrings. Settle the exact
oracle contract against actual upstream outputs before bulk serialization.

Use declaration/scope merges and conflicts, functions/classes/namespaces/modules,
JS/CommonJS/JSDoc, private/computed names, destructuring, ambient declarations,
control-flow branches/loops/labels/switch/try/finally and malformed recovery.
Expand by observed unhit output kinds/branches, with explicit exclusions. Upstream
binder tests contain only a benchmark; successful benchmark execution is not a
behavioral oracle. Keep named xUnit regressions for discovered compatibility bugs.

## Milestones and stopping condition

1. **Readiness and contract:** assess parser evidence/gaps; settle support and
   canonical identity contracts; confirm the bounded dependency closure.
2. **Small integrated binding:** compile and compare simple declarations/scopes,
   a binding error and a branch/loop flow graph. This is only a milestone.
   Inspection found the main bind/child/container dispatch functions mutually
   recursive across most binder handlers. Compile the genuine low-level model
   first, then integrate that connected component together; do not fabricate
   successful stub handlers to make a smaller public binder appear complete.
3. **Full selected source-file pipeline:** port the remaining binder algorithms
   and required helper closure, expand meaningful symbol/scope/flow/diagnostic
   comparisons and inspect coverage gaps.
4. **Checkpoint:** resolve independent review findings, finalize source mapping,
   freeze sources, run xUnit and actual Native AOT binder verification plus full
   parser/scanner regression gates. Record hashes, remaining discrepancies and
   whether the verified structures support a proposed initial checker slice.

Stop at this bounded checkpoint, with accepted/incomplete/blocked status grounded
in actual results. Never infer full binder parity or checker readiness from counts,
successful execution, compilation or a few valid declarations.

## Delegation and efficiency

Reuse the three explicitly selected Sol agents with disjoint algorithm/support/
verification ownership. Main coordinates boundary/decisions/acceptance. Contract
first, then a small vertical slice before broad generated integration. Keep one
owner for shared library builds, use focused error packets, persist generator fixes
and replace stale next steps at verified boundaries. Cross-review other owners'
code and freeze before final AOT gates. Report measured elapsed/usage only where
available; token totals and subscription cost cannot be inferred from wall time.

## Approved readiness decisions

Twenty targeted Go/current-native probes exercised 14 previously unobserved parser
codes with no syntax or diagnostic mismatch. Eleven stored fixtures preserve those binding-relevant
closures and passed the unchanged parser before binding implementation, then the final regression gate. No parser defect blocked extension.
Checker-only Symbol.CheckFlags remains excluded: the Go oracle must assert zero
for encountered symbols, never fabricate a C# field/value to claim equivalence.
If binding requires a nonzero value, reassess that concrete dependency.
