# Anonymous object checker support checkpoint

Owner: support agent. Accepted bounded checkpoint; no active implementation work.
Upstream `fed0bf24149fb1ed36039212648bafdafc1ea10e`; historical scalar
checkpoint `7e692dd` and its acceptance evidence remain unchanged.

Boundary: finite anonymous type/object literals, nested required/optional/readonly
static identifier/string/numeric data properties, shorthand, dot reads and contextual
scalar/object checking. No interfaces/aliases/source unions/spreads/computed names/
methods/calls/index signatures/explicit writes/libraries/general emit.

Support owns [Types](../../src/TypeScript.Scanner/Checker/Types.cs), cache-model
additions, [Relater](../../src/TypeScript.Scanner/Checker/Relater.cs), diagnostic-only
[NodeBuilder](../../src/TypeScript.Scanner/Checker/NodeBuilder.cs)/
[NodeBuilderImpl](../../src/TypeScript.Scanner/Checker/NodeBuilderImpl.cs),
[checker printer](../../src/TypeScript.Scanner/Checker/Printer.cs),
[printer counterparts](../../src/TypeScript.Scanner/Printer/), AST/diagnostic helpers.
Scanner owns Checker/flow/admission, SourceFileLinks, WideningContext,
complete-preimage relation keys and **all library builds**.

Implemented: actual typed absent signature/index slots; real member cache/link
fields; recursive ternary relations/maybe stacks and cache/identity/property/excess/
weak/error-chain/elaboration branches; separate Call/Construct signature checks
with nonempty guards. No simplified structural-property-loop replacement.

The initial nil-context-only formatter assumption was rejected after the actual
relation diagnostic caller selected a nonnil enclosing declaration. Genuine context
reuse is now integrated in [PseudoTypeNodeBuilder](../../src/TypeScript.Scanner/Checker/PseudoTypeNodeBuilder.cs),
[NodeCopy](../../src/TypeScript.Scanner/Checker/NodeCopy.cs),
[NodeBuilderScopes](../../src/TypeScript.Scanner/Checker/NodeBuilderScopes.cs),
[SymbolTracker](../../src/TypeScript.Scanner/Checker/SymbolTracker.cs),
[EmitSupport](../../src/TypeScript.Scanner/Checker/EmitSupport.cs) and three mapped
[Pseudochecker counterparts](../../src/TypeScript.Scanner/Pseudochecker/).
Context stacks retain enclosing file, visited/depth state, enclosing symbol types,
source-node clones/original chains and serialized cache reads/writes. Selected
[EmitContext](../../src/TypeScript.Scanner/Printer/EmitContext.cs)/
[EmitFlags](../../src/TypeScript.Scanner/Printer/EmitFlags.cs) retain actual per-node
ASCII escaping and flag copying, correcting the observed Unicode property-name
formatting mismatch without general emit. Cache keys
project only fixed default flags/internal flags/infer parameters; retain varying
fully-qualified/InObjectTypeLiteral states. External diagnostic trackers are actually nil. The wrapper still records genuine
cyclic-structure reportedDiagnostic state and the corresponding cache-write guard;
static data reuse excludes entity tracking/deferred tracker error branches.

Last shared compiler milestone: scanner-owned build10 passed with zero warnings/
errors after final LF formatting; build9 managed43/43 full object cases match.
Owner's independent relation/formatter review reports no unresolved blocker.
Concrete nil-slice/assignment-traversal/CheckMode and Unicode/union-cache fixes
passed the matrix; actual declaration-cache-miss widening is retained.
Pinned required diagnostics total273 messages.

All executable source/tool/corpus writes remained **frozen** through the final
[guarded gate](../../validation/results/object-checker-verified.json): exit 0,
43 object + 45 scalar Native AOT cases; 149 checker/122 binder/475 parser/1287
scanner xUnit tests; full earlier native regressions all pass. No library builds
or new diagnostic experiments were run by this owner.

Source fingerprint `1ce0c30edf2f470ee4a7c527f336980e5a80d8afc17108774c8764d99d6c15b9`
rechecked read-only after docs/mapping changes. Final map contains 85 library C#
files exactly once, 112 entries, 78 verified upstream hashes, all destinations
present and no draft state. Scalar accepted checkpoint/history remain separate;
no object commit was invented. [Departures](../OBJECT-CHECKER-DEPARTURES.md) record
managed keys/storage, original flags/clone/context branches and unexercised edges.
No active work or implicit next phase. Historical acceptance records unchanged.
