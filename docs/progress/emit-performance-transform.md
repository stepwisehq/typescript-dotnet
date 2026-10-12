# Emit performance: transformer and synthesis ownership

Verified delivery: the coherent packet passed independent source review, retained
managed/Native AOT correctness gates and the scoped emit performance target. See
[final evidence](../../validation/results/vscode-emit-performance.json) and
[assessment](../VSCODE-CHECK-STATUS.md). Notes below preserve investigation chronology;
intermediate pending statements are historical, not outstanding tasks.

Active after `edc53e02`. This lane owns transform consumers and required AST factory/
visitor joins. The existing algorithms, Go clones and identity/cache behavior remain;
root coordinates builds, VS Code emit qualification and broader acceptance.

The first source-written ownership packet replaces the separate CLR factory/visitor
hook objects with the embedded value records used by pinned `ast.go` and `visitor.go`.
Constructor arguments are copied into their owners. Per-owner hook mutation cannot
mutate another owner. Printer ownership separately caches the context's five visitor
callbacks once and copies its hook template into every new visitor. This removes the
extra hook wrapper and repeated context delegate setup from ordinary transform visitors
and the thirteen `ExtractModifiers` call sites without pooling mutable modifier state.

Two generated AST slice-map consumers now pass the visitor as explicit state through
cached static callbacks, retaining Core.SameMap's original unchanged identity and changed
prefix clone. The ES decorator ordered-map consumer uses the accepted value enumerator;
the declaration expando syntax-list consumer ranges its existing Children slice directly.
These preserve original order and live owner semantics without allocating IEnumerable
bridges. Declaration comment-range consumers will join the printer-owned scanner value
sequence once that producer is ready; required comments and lazy scanner behavior stay.

Existing FoundationChecks factory construction controls cover hook argument copies,
independent visitor mutation and subsequent context visitor creation. Logical control
counts are unchanged. No local builds, test runs or performance observations were made.
Independent source/assertion review and coordinated integration remain required. Required
pointer-owned scope/map metadata and VisitSlice clones were inspected and retained.

Current files: Ast/Ast.cs, Ast/Visitor.cs, Ast/AstGenerated.cs,
Transformers/EsTransforms/ESDecorator.cs, Transformers/Declarations/Transform.cs,
and validation/Emit.Checks/FoundationChecks.cs. Printer/EmitContext.cs and scanner
producer ownership remain with the printer lane. Risk to review: mutable callback copy
semantics and nested visitors, ordered map step capture, and retained slice header identity.

The baseline allocation sample identified the missing original parent-setter pool:
Ast.SetParentInChildren previously constructed a recursive closure/delegate per call.
The full owner now follows utilities.go:870-906 with a ConcurrentBag of explicit parent
state plus one cached callback per owner, independent nested/concurrent leases and
finally return. Parent restoration also occurs on exceptions, avoiding a poisoned
retained owner. Existing controls cover root-parent preservation and successive tree
ownership; no extra logical case is counted.

The single Checker.FindManyAncestors caller now retains its five immutable ordered
predicate callbacks in static storage instead of allocating a params array each call.
The actual ancestor result slice remains freshly allocated and original search order,
first-match behavior and callback targets are unchanged. This narrowly demanded checker
join was explicitly assigned to this lane. These two paths match concrete profile
attribution; sampled weights are not predictions of Native savings.

All four declaration comment scans now use the printer-owned scanner value sequence
without interface boxing. The source generator's two SameMapNodes templates match the
checked-in explicit-state consumers; no generator or workload was executed.

Profile-driven recursion joins are also source-written in Ast.cs: IdentifierCollector
and SourceFile.NameTableCollector retain one callback for the entire traversal instead
of constructing a delegate when each recursive call passes its local function. HashSet
and name-table result owners remain fresh/retained as before, including null identifier
handling, duplicate position -1, JSDoc traversal, capacity, locks and initialization.
The initial parent-setter interpretation was corrected by joint attribution: the
identifier callback was the larger sampled Visitor contributor. Both independent owner
paths are now covered; required result arrays are unchanged.

All nine owned code/test/generator files are source-frozen and whitespace checks pass.
The printer lane independently reviews this packet; foundation independently reviews
printer/scanner producers. Root integration builds and behavior/performance results
remain pending. No local build, workload or performance run was made by this lane.

First integrated VS Code execution retained exact outputs/diagnostics. Root reports
Native allocation traffic fell from 19.915 GB to 14.755 GB, but the requested no-GC
region did not remain active; this does not establish the speed target. Source remains
frozen while root investigates the real budget partition and integrated workload.

Read-only baseline joint attribution found these disjoint sampled type totals:

| Allocation type | Baseline sampled bytes |
| --- | ---: |
| emitNode[] | 414,702,776 |
| Node[] | 653,196,064 |
| Type[] | 617,175,984 |
| ValueSymbolLinks[] | 766,936,272 |
| ObjectType | 302,086,336 |
| TypeReference | 139,871,912 |

Their sum is 2,893,969,344 sampled bytes, not a predicted saving or current Native
measurement. FindManyAncestors' required result is 287,777,288 of the Node[] weight;
VisitSlice's required changed-prefix/result owner is 56,625,944. LinkStoreArena's
2/4/8/.../256 growth matches the real pinned Arena.New ownership, with stable retained
slots. No unsupported chunk reduction, result pooling or broad type rewrite followed.
A matched pinned-Go allocation profile could clarify size/rounding versus allocation
frequency; no such profile was run by this lane.
