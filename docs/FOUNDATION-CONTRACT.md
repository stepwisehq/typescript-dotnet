# Foundation completion contract

Status: satisfied by the frozen Linux x64 Native AOT gate; see the
[acceptance assessment](./FOUNDATION-ASSESSMENT.md) and
[evidence](../validation/results/foundation-verified.json). Baseline `b6b22b9`; upstream
`fed0bf24149fb1ed36039212648bafdafc1ea10e`. Governing [mandate](./FOUNDATION-PROMPT.md).
This contract completes a defined foundation surface, not exhaustive correctness
or all functions in the upstream AST package. Do not narrow it to declare success.

## Capability and boundary

One source text can be scanned, parsed, inspected and represented faithfully in
syntax nodes without binding/checking or project setup. Required dependencies are
included even if they were omitted by earlier feasibility slices.

F1. All lexical scanner APIs and regexp syntax scanning: scan/rescan/state reset/
mark/rewind, targets, trivia/comments/directives, JSX/JSDoc, raw text/value, Unicode,
identifiers, literals and scanner diagnostics. Include scanner syntax-node helpers
and text/line/UTF8/UTF16 coordinate helpers previously excluded. Audit all functions
in scanner.go, regexp.go, utilities.go and pinned tables; necessary numeric/string
support must preserve semantics. Existing .NET facilities may substitute equivalent
Go library facilities with documented reasoning and differential evidence.

F2. All initial source-file parser paths in the five declared non-Unknown modes
JS, JSX, TS, TSX and JSON: declaration filenames, explicit external-module options,
syntax-reference metadata, malformed recovery, lazy/eager/repeated JSDoc resolution,
required internal reparsing/top-level await, and isolated entity-name parsing.
Unknown mode preserves the upstream failure; reserved/invalid enum values are not
supported modes. Account for every function in the six parser source files.

F3. Complete syntax-node representation and generated syntax behavior for all
parseable node kinds: payload fields, flags, nullable/empty lists, ranges, parents,
syntax identity, accessors, factories, update/clone/deep-clone behavior and ordinary
visitor/traversal APIs. Include syntax predicates/helpers and precedence used to
inspect or construct these trees, plus full pure syntax subtree facts with actual
cache/mask behavior. These are foundations even when consumed by later stages.
Schema-generated structure and explicit exceptions must be discoverable by origin.

F4. Source text coordinates and syntax diagnostics: complete PositionMap and lazy
SourceFile position mapping; byte/UTF16/line conversions, malformed UTF8 and lone
surrogate behavior; diagnostic identity/arguments/ordering/related chains, collection
deduplication and equivalent-source lookup. Default English diagnostic formatting is
included; translated locale catalogs and project/config-derived diagnostics are not.

F5. Systematic upstream test incorporation and broad differential coverage. All
applicable direct foundation tests retain their intended assertions in xUnit, with
source hashes/provenance, table counts and adaptations documented. Separately compare
all applicable source units extracted by the original upstream splitter from tracked
compiler, conformance and transpile input suites; inventory every input disposition.
Compare scanner behavior where directly scanned and meaningful complete syntax graphs,
diagnostics, parent/traversal identity and full facts under a defined observation
order. Reusing compiler input is not passing its compiler test. Supplement direct
and broad tests with targeted helper/identity/mode/malformed and diagnostic-gap cases.

F6. Actual Linux x64 Native AOT publish and execution of applicable foundation
verification, all direct foundation assertions or an equivalent shared executable
assertion target, broad upstream-input comparisons, and retained binder/scalar/object/
data-type checker regressions. Independent review challenges correctness, original
assertions, missing methods, exclusions and comparison blind spots. Freeze and hash
source/assets before the comprehensive gate; retain separate historical evidence.

## External boundaries and partial shared files

No binding/checking semantics, symbol/flow/store ownership, project/module resolution,
compiler-options derivation from project/package metadata, source transformation,
emit, incremental edit infrastructure, language services or concurrency API guarantee.
Existing later-stage code remains available and regression-tested, not claimed
completed by this campaign. General synthetic transform/checker-only node payloads,
transformation factories/hooks and emit-original metadata are excluded unless needed
by ordinary syntax construction/clone/update or a direct foundation assertion.
Allocation/pooling/arena mechanics may use managed adaptations; syntax identity and
observable state must be preserved. Numeric IDs may be canonicalized; identity
relationships must remain compared. Parse inputs specify external-module options;
computing them from project configuration is outside this surface.

Every shared AST/support function must have a disposition based on its actual
behavior/dependencies, not merely its consumer's subsystem. Pure syntax helpers are
in scope. Later-stage-owned behavior must identify its reason and downstream effect.
Benchmarks are inventoried but do not constitute correctness assertions or authorize
performance optimization. Fuzz seed assertions are preserved; finite deterministic
fuzz/differential samples are additional evidence, not exhaustive fuzzing. Unbounded
resource exhaustion and upstream failures must be recorded, never disguised as passes.

## Observable acceptance and accounting

Implementation inventory links upstream files/functions/generated structures to
counterparts and marks complete, adapted, missing/partial, or excluded with reasons.
No in-scope missing implementation or permissive placeholder may remain at acceptance.
Required support is traced in the same source mapping, preserving licenses/pin.

Test inventory separates (1) original assertions preserved, (2) adapted assertions
with exact differences, (3) compiler inputs reused only for foundation comparisons,
(4) newly authored tests/differentials, (5) justified excluded/blocked/skipped tests.
Count test functions, table instances and source units separately. Unknown dispositions
block acceptance. Direct assertions alone do not establish implementation completeness.

Reconcile all historical gaps: scanner1513, the original29 parser codes (14 already
exercised by accepted binder-readiness fixtures,15 currently unobserved), source modes,
isolated entity parsing, position maps and helper/clone/update/cache identities.
For each unobserved code, produce a witness where reachable or source-based explanation
of why it is unreachable/not applicable at this pin; retain unproven coverage as a
visible limitation, not an observed result. Independent review must challenge reasons.

Compare raw bytes/positions, syntax scalars and named fields, list nullness/content,
parents/child order and identities, ordered diagnostic metadata/arguments/chains and
relevant cached results. Enumerate intentional representation omissions and adapters.
Do not change upstream expectations to fit C#. Broad corpus extraction/options must
be reproducible and dispositioned, and malformed inputs must compare recovery details.

## Execution and stopping

Reuse three GPT-6.1 Sol owners with disjoint source/test ownership. Coordinator owns
scope, integration, assessment and commits. Milestones: inventories/contract;
original direct assertion ports and their implementation repairs as the first gate;
remaining inventory-driven implementation and broad comparisons/gap closure; independent
review; frozen AOT/regressions; committed delivery and clean tree. Move between them
autonomously, batch shared builds, and avoid repeating unchanged accepted suites.
Small owner checkpoints and source/test inventories suffice; no process framework.

Stop when this contract is met and committed, or a material boundary/access obstacle
requires a decision. A completed internal milestone is not campaign acceptance.
Report what downstream consumers may assume, what remains outside scope, actual
observed effort, platform limits and recommended next capability work.

User priority clarification: original upstream subsystem tests guide the first work.
Port and satisfy applicable scanner/parser/AST/support direct assertions before broad
input reuse or newly authored coverage. Missing required APIs are repaired as those
ports expose them. This sequence does not reduce the final implementation contract.
