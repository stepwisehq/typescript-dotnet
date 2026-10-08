# First bounded type-checking feasibility checkpoint

**Accepted as a bounded end-to-end semantic milestone.** Source text passes through
the existing parser and binder into selected faithfully ported upstream checker
logic, producing semantic diagnostics and type/name observations that agree with
Go. The actual Native AOT executable passed all 45 selected semantic cases,
274 queries and 121 assignability comparisons, plus separate admission/API/utility
checks. All 1,966 xUnit tests and the native scanner/parser/binder regressions passed.
See [verification](./CHECKER-VERIFICATION.md) and
[durable evidence](../validation/results/checker-verified.json).

The preceding binder work was committed as `e94cc6f` before implementation. New
checker work is saved uncommitted; nothing was pushed. The upstream pin remains
`fed0bf24149fb1ed36039212648bafdafc1ea10e`. No further phase is started by this report.

## What works

The slice handles one TS script with scalar var/let/const declarations, nested
blocks, literal or identifier initializers, selected primitive/literal annotations
and parentheses. It exercises real NameResolver lookup, shadowing, missing names
and spelling suggestions, declaration inference/widening, literal freshness,
annotation compatibility, repeated-declaration agreement, global-name conflicts,
and straight-line initialization flow including forward/self references.

Invalid inputs meaningfully exercise the checker: the corpus contains mismatched
types, missing names, typos, used-before-declaration/assignment, conflicting
redeclared types, built-in-name conflicts, type names used as values and a missing
const initializer. Thirty-one file diagnostics across nine codes, five related
diagnostics and 121 relation results (50 true, 71 false) match upstream. Type flags,
literal values/freshness, boolean union constituents, resolved declaration links
and shared symbol tables are compared, not merely diagnostic counts.

The environment is deliberately **no-library**: there are no standard declarations
or external dependencies. Each case reports the real eight missing-global 2318
diagnostics separately from file errors. A locally valid scalar specimen therefore
still has environment errors; this is not a claim that a complete TypeScript program
was checked successfully. Unsupported input is rejected explicitly through an
authored admission adapter or guarded semantic branch. It never becomes a successful
check with skipped behavior.

## Fidelity and dependency boundary

The unrestricted checker closure is much larger than the binder. A conservative
lexical investigation estimated roughly 1,900 functions/40,000 source lines across
about 15 checker files; that is not a sound reachability proof or a cost forecast.
Even scalars require initialization, name resolution, type/cache identity, flow,
diagnostic collections and type/symbol formatting. These dependencies were reported
before proceeding with a guarded partial port.

The port keeps direct file mapping and selected upstream function/control-flow
boundaries. The [function inventory](./checker-functions.json) locates 223 selected
methods, retained conditions and explicit unsupported paths. This is an inventory
of the partial implementation, not 223 fully verified upstream functions. NameResolver
has all 13 original functions; the selected checker hooks remain bounded.
ReferenceResolver's emit-related path is still excluded.

[Departures](./checker-departures.md) records scoped initialization, fixed options,
remaining branches and diagnostic-only NodeBuilder/Printer support. Actual scalar
type nodes and identifier-printing paths are retained for diagnostic formatting;
there is no general code-generation API. The checker algorithms are reviewed hand
ports, not a claim of complete mechanical regeneration. Support enum/resolver/helper
generators have scratch equality checks, and the [source map](../source-map.json)
distinguishes ported, partial, excluded and authored support.

Necessary C# adaptations include typed payload inheritance for Go embedding,
reference-identity graphs, ordinal raw-byte-string dictionaries, managed allocation
instead of pools/arenas, and managed cells preserving paged-store slot identity.
The selected union intern key uses the same sorted constituent identities with a
managed key instead of importing Go's XXH3 hash machinery. Public .NET text decodes
once; raw byte arguments and positions remain authoritative internally. Standard
.NET collections, locking/atomics, UTF-8 facilities and source-generated JSON are
used where their semantics fit. No optimization or performance study was performed.

## Foundation gaps and verification limits

The accepted binder source/assets/native hashes were confirmed before extension.
The following previously unexercised areas were assessed specifically:

| Area | Result for this slice |
| --- | --- |
| Shared symbol-table identity | Directly relevant: globals and globalThis.Exports share identity in all 45 semantic cases; compared natively. |
| Allocated empty tables | Go/native utility probes verify null-to-empty allocation, count and identity. Scalar graph cases do not themselves produce empty tables. |
| Shared linked FlowList identity | Branch/loop/try/reduce flow is excluded; no selected dependency justified reopening it. The gap remains visible. |
| Partial source-position mapping | No full mapping API required; byte diagnostic spans and existing ECMA helpers suffice for this path. |

Twenty independent Go/native utility observations also verify page-presence behavior,
zero versus nil slots and the high-map boundary of link stores. The current binder
regression still compares its original declaration/scope/flow graphs, and parser/
scanner regression evidence remains intact.

Limits are substantial. No object/function/call, alias/generic, source-level union,
branching-flow, module, standard-library, project, emit, incremental or language-service
semantics are accepted. Malformed syntax is explicitly rejected by this semantic
slice; parser recovery remains independently covered. The public type graph does
not expose every mutable cache field, including union regular/origin caches. No
semantic diagnostic chains were exercised. The syntax snapshot is taken before
checking, not a complete post-check AST/cache comparison. Historical scanner/parser/
binder unobserved diagnostic codes remain listed in the verification report.
There are no unresolved observed discrepancies within the accepted contract.

## Findings and execution efficiency

Three reused, explicitly selected GPT-6.1 Sol agents owned checker algorithms,
support and independent verification. Cross-review swapped implementation ownership;
the main session owned scope, adaptations, planning and acceptance. Available metadata
still did not independently verify/set the main coordinator model; that established
limitation is retained rather than claiming a verified Astra selection.

Review was important for the manual partial port. It caught substituted declaration
flag logic, provisional diagnostic ordering, omitted relationship/suggestion branches,
parenthesized literal handling and member-list ordering/null behavior. These were
restored from upstream, not accommodated in expected results. Expanded differential
coverage then caught a public query-dispatch omission and UTF-8 diagnostic rendering:
Go collapses contiguous invalid byte runs in diagnostic arguments. The narrow
formatter correction preserved raw arguments and scanner/public JS decoding.
A null-list test assertion was separately corrected as a harness defect.

Reusing AST and canonical graph machinery, independent Go expectations, one shared
build owner, compact failure packets and early cross-review helped. The nine-case
vertical slice proved integration before expansion to 45; explicit unsupported
controls prevented admitted syntax from being confused with a complete checker.
A source/function inventory is useful now because selected branches are substantially
harder to track than the earlier whole-file algorithm ports.

One avoidable coordination failure cost a full extra gate: an inventory-tool metadata
edit landed after the initial hash capture. All runtime checks passed, but the guard
correctly rejected that evidence. The 51.463-second run was preserved, then a stable
47.701-second repeat passed. Future freezes must include an explicit final stop for
**every source, tool and corpus writer**, including documentation-generation tools.
Do not relax the guard or claim the rejected run passed.

The binder commit was at 17:40:42 UTC and the accepted gate ended about 18:31:56 UTC
on 2026-10-08: roughly 51 minutes through verification, followed by documentation.
This is elapsed concurrent work, not measured labor. Token totals and monetary cost
were unavailable. No package installation or manual debugging was required; the
user reconfirmed source fidelity during the phase. No subscription cost is inferred.
Small files and per-owner checkpoints remained sufficient; SQLite was not needed.

## Next informative slice and representative-project outlook

The result demonstrates a working route into real checker semantics, including
negative cases and native execution. It does **not** demonstrate that checking a
representative real project is now close or that the remaining checker will have
similar cost. Ordinary projects need object members, signatures/calls, library
contracts, richer inference and eventually cross-file/module behavior, all absent here.

The next most informative bounded slice would add **simple object/interface types,
property lookup and non-generic function signatures/calls**, with a dependency
inventory before committing to the whole group. First test whether it can remain
single-file with a deliberately pinned, explicitly described minimal declaration
fixture drawn from upstream libraries. Library declarations must be actual checked
inputs, not invented successful global types. If even a minimal fixture requires a
large generic/array/signature closure, report that pressure and reduce the milestone
before expansion.

That slice would test structural type relationships and signature machinery—the
next major uncertainty beyond scalar comparisons—and move closer to code found in
real projects. General library loading, modules and project infrastructure should
still be separate scope decisions. Retain all native regressions, close newly
relevant cache/flow gaps, and keep unsupported outcomes explicit.
