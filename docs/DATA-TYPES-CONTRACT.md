# Data-type checking campaign completion contract

Status: accepted on 2026-10-08. All D1–D7 rows passed the frozen Linux x64
Native AOT gate and independent review. See the [assessment](./DATA-TYPES-ASSESSMENT.md)
and [durable evidence](../validation/results/data-types-verified.json).
The original capability and external boundaries below remain unchanged.
Mandate: [campaign prompt](./DATA-TYPES-PROMPT.md). Baseline: verified anonymous-object
checkpoint `1f7c953`; upstream remains `fed0bf24149fb1ed36039212648bafdafc1ea10e`.
This contract establishes a coherent single-file data-type checking capability,
not completion of the upstream checker or all TypeScript object programming.
Do not narrow this contract to label a partial implementation complete.

## Capability and external boundaries

One TS script parsed and bound by this port, existing fixed strict-null-checking,
noLib/noResolve options. Preserve all admitted scalar/anonymous-object behavior.
New source capability:

- Non-generic type aliases to admitted scalar, literal, object, named, union and
  intersection types; forward references, lexical scopes and value/type meanings.
- Data-only interfaces with typed static properties, optional/readonly modifiers,
  one or multiple named bases, declaration merging and compatible/incompatible
  inherited or redeclared members. Alias-based heritage is included.
- Direct and mutually recursive object property shapes through aliases/interfaces;
  real termination, relation caches and recursive diagnostic rendering. Invalid
  circular aliases and heritage cycles produce upstream diagnostics.
- Source unions and intersections of admitted scalar/object/named types, including
  nullable/literal-discriminated object alternatives, property availability and
  merged property types, contextual checking, excess properties, narrowing from
  existing supported initializer/straight-line flow, and structural relationships.
- The `object` annotation keyword, ordinary dot reads, object literal construction,
  shorthand and nesting interacting with named and composite types.

Successful inputs and semantic failures are both part of the capability: unresolved
or wrongly used names, duplicate declarations, invalid bases, missing/extra or
incompatible properties, optionality, recursive mismatches and impossible
intersections. Diagnostics must preserve codes, locations, messages, nested chains
and related declarations. An invalid in-contract input is checked and diagnosed,
not rejected simply because it is invalid.

Prominent ordinary-use limitations: no functions/methods/call/construct/index
signatures, arrays/tuples, classes, source type parameters or type arguments,
source `this` types, mapped/conditional/indexed-access/keyof types, assertions,
optional/element access, spreads/computed property expressions, explicit writes,
branching statements or cross-file declarations. No standard library declarations
are added: missing-global diagnostics remain separately identified (normally eight;
source declarations with builtin names may change the exact upstream result).
No declaration files/JS/JSX/modules/projects, module resolution, broad infrastructure,
emit, incremental compilation or language services. Malformed syntax remains parser
regression scope and is rejected by semantic admission. This is useful for checking
standalone data shapes and their literal consumers, not typical complete applications.

## Necessary upstream dependencies

Port original name/type resolution, alias links, interface declared/base members,
merge validation, union/intersection construction and member/relation algorithms.
Include selected synthetic internal thisType/reference/mapper machinery required
by non-generic alias-based interface heritage; the source generic exclusion is not
permission to drop those semantics. Reuse existing relation recursion machinery,
fill required upstream branches rather than substitute a second checker.

An interface inheriting from an alias to `any` can synthesize upstream string-to-any
index information. Preserve that internal index payload and its selected relationships;
the exclusion of written index signatures does not permit dropping this behavior.

Named/recursive diagnostic serialization must use actual symbol/alias identity and
upstream enclosing-declaration context, including the diagnostic pseudochecker.
Extend selected type/reference/alias/base links and caches with preserved identity.
Assess binder table sharing/merging and source ranges on these paths. Empty tables
and absent tables must stay distinguishable. Port additional support only where the
contract requires it, preserving file mapping, origin/license notices and explicit
partial status. Broad external boundary pressure requires coordinator assessment
and a user decision if material; internal checker dependency work is authorized.

## Acceptance matrix

Each row needs implementation, differential cases, native execution and independent
omission review evidence. Record concrete case groups and uncovered branches in the
[status inventory](./DATA-TYPES-STATUS.md); test totals alone do not close a row.

| ID | Required behavior | Observable acceptance |
| --- | --- | --- |
| D1 | Aliases and scoped type-name resolution | Canonical type/symbol/alias/declaration links, forward/shadowed names, unknown/value-as-type and duplicate diagnostics |
| D2 | Interfaces, inheritance and merges | Declared/resolved/inherited members and bases, table/target identities, valid/invalid multiple bases and merged declarations |
| D3 | Recursive shapes | Direct/mutual alias/interface properties, equivalent and mismatching recursive relations, repeated-query identities, circular alias/base diagnostics |
| D4 | Unions | Canonical constituents, common/optional properties, discriminated and nullable contexts, valid/invalid directed relationships and diagnostics |
| D5 | Intersections | Canonical/reduced constituents, combined/conflicting properties, never reductions, relation and diagnostic behavior |
| D6 | Integrated object use | Construction/shorthand/nested dot reads, contextual typing/freshness/widening, object keyword, named/composite/recursive cross-feature cases |
| D7 | Boundaries and regression | Explicit unsupported controls; exact missing-global accounting; preserved scanner/parser/binder/scalar/anonymous-object comparisons |

Compare actual pinned Go execution and C# with a defined deterministic observation
order. Compare meaningful graph structure/identity, type and symbol queries,
directional relations and full diagnostic structure. Inspect both pre-check syntax
and post-check semantics as appropriate; document any observation-induced caches.
Do not weaken existing field comparisons or suppress diagnostic categories.
Add identity/cache probes for newly selected paths; zero observed edges do not
establish nonzero behavior. Use failure/recursive/interaction classes, not just
isolated happy paths. Original Go expected outputs are authoritative.

Acceptance requires all contract rows closed, no unresolved correctness/omission
review blocker, successful actual Native AOT publish and execution, xUnit tests,
and earlier native regressions. Linux x64 is the verified platform; other platforms
are not implied. Record exact source/assets/binary fingerprints after source freeze.
A successfully completed internal milestone is not campaign acceptance.

## Execution and stopping

Reuse three GPT-6.1 Sol owners: checker/name/member/flow algorithms; models,
relations and diagnostic support; oracle/admission/tests/evidence. Assign concrete
file ownership before edits and hand off explicitly. Coordinator owns scope,
dependency decisions, docs, integration assessment and commits. Cross-review actual
upstream paths and omissions after implementation; verification owner independently
reviews the contract matrix. The established inability to independently confirm the
coordinator model through metadata remains disclosed.

Internal sequence: dependency contract and preserved baseline; named resolution /
interfaces with minimal interaction corpus; recursive and union/intersection closure;
expanded contract corpus and independent review; one frozen comprehensive native
acceptance followed by committed delivery. Move between milestones autonomously.
Use focused JIT/differential checks during integration; preserve old regression
fixtures and batch full native regressions at acceptance unless risk warrants earlier.
Run formatting and whitespace checks before final freeze. No optimization/profiling
or process framework. Update small per-owner recovery notes at useful checkpoints.

Stop only when the contract is met and committed, or a material dependency/access
blocker requires a decision. Document partial status without redefining completion.
