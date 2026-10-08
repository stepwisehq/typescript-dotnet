# Data-type checking campaign assessment

**Accepted on 2026-10-08: the D1–D7 contract is met on Linux x64.**
This document records the disposition of the fixed
[completion contract](./DATA-TYPES-CONTRACT.md), not full upstream checker completion.
The anonymous-object baseline was committed cleanly as `1f7c953` before this campaign.
Upstream remains `fed0bf24149fb1ed36039212648bafdafc1ea10e`.

## Capability

The selected capability checks standalone data shapes and their literal consumers:
named scalar/object aliases, forward and scoped type names, data-only interfaces
with multiple bases and declaration merging, recursive properties, unions and
intersections, and their interaction with object construction, shorthand, optional/
readonly members, dot reads and structural relationships. Invalid names, cycles,
inheritance/merge conflicts, missing/excess/incompatible properties and impossible
intersections must produce the actual upstream diagnostics.

Functions/methods/call/construct/index signatures, arrays/tuples, classes, written
generics/this/mapped/conditional/keyof/indexed-access types, explicit writes,
branch statements, libraries, source modules/projects and emit/services remain
excluded. These materially limit ordinary application checking. Unsupported input
throws explicitly; a successful admission is not a promise of an error-free source.
Malformed syntax retains its separate parser regression scope.

No standard library is supplied. Environment diagnostics remain separately visible:
the corpus has eight missing globals in 88 cases and seven in four source-builtin
cases, 732 total. Source declarations can also produce actual builtin shape/arity
errors. No diagnostic category was suppressed to obtain agreement.

## Acceptance evidence

The actual Native AOT executable matches pinned Go on all 92 rich cases, with 1,601
queries and 1,138 directed relationships (823 true, 315 false). The compared graph
contains 2,859 AST nodes, 1,273 types, 1,277 symbols and 833 tables. It includes 61
checker diagnostic records, four binder diagnostics and the separate globals.
Codes in primary/nested/related diagnostic data span 30 semantic codes.

Repeated declared-type queries preserve identity in 152 observations. Actual
observations include 65 alias identities, 10 hidden type parameters, two synthetic
IndexInfo identities, four internal IndexType payloads, nine deferred-symbol link
entries, one module export cache, ten property-key cache associations, and 13
allocated-empty tables. Populated reference-cache lookups and constrained key
normalization are compared with actual upstream operations. A ten-arm discriminated
union populates its real ten-entry lookup map; Cartesian relationship cases test
compatible and incompatible combinations separately from that optimization.

The frozen gate passed in **82.979 seconds**, exit 0, with unchanged source/assets.
Native execution also passed all 43 anonymous-object and 45 scalar comparisons,
78 admission controls, 20 Go utility observations and 10 authored API checks.
All **2,146 xUnit tests** passed: 262 checker, 122 binder, 475 parser and 1,287 scanner.
Earlier native regressions passed 118 binder, 471 parser and 1,276 scanner cases,
plus 178 scanner helper outputs. All five historical evidence records and earlier
scalar/object corpus bytes remain unchanged from `1f7c953`.

All contract rows are accepted and all three independent reviews have no remaining
selected-boundary correctness or omission blocker. See
[verification](./DATA-TYPES-VERIFICATION.md), [capability inventory](./DATA-TYPES-STATUS.md)
and [durable evidence](../validation/results/data-types-verified.json).
The native ELF is 6,377,064 bytes and runs without an installed .NET runtime.
Its SHA256 is `cef51515a21d5f087fb1bc356a33dcf25747833fb5e4b3f109b650c1a26135e1`;
the source fingerprint is `ac9763058844d0ce1daa50fd8c6ba22748c083713592ebd7d2aabf81cfe7b928`.
Reproduce from this campaign's delivery commit with `./tools/verify-data-types.sh`.

## Fidelity and dependency costs

Actual upstream name/member/base resolution, recursive relation caches and ternary
results, contextual typing, reductions and diagnostic algorithms remain the source.
No separate miniature checker or fixture-specific rule was introduced. See the
[source map](../source-map.json) and [adaptations](./DATA-TYPES-DEPARTURES.md).
All 91 C# library files map exactly once across 126 manifest entries; pinned origin
hashes were validated. The function inventory contains 941 selected methods, not
941 claims of complete upstream function coverage.

Several dependencies are required even without their source syntax: alias-based
interface inheritance creates internal this parameters/references/mappers; an any
base creates an index; named diagnostic overlap creates key-type origins; qualified
globalThis type lookup reaches module-symbol export caches. Named/recursive
formatting needs real accessibility, tracked symbols, source context, synthesized
unresolved-type comments and precedence. These do not imply generic source, module
resolution or general emit support.

Managed references replace pointers/arenas; nullable collections preserve nil versus
empty and identity. Keys retain the complete upstream byte preimage using .NET
binary facilities rather than opaque XXH3 bits. Existing ordered collection and raw
UTF-8 representation choices remain. Rich observation found and corrected an older
storage defect: nodeLinks/typeNodeLinks need core identity maps, while only the
appropriate symbol-node store is paged. Independent review restored publication
order, selected guard branches, diagnostic caller flags, per-node state resets and
conditional read/write-member paths.

## Limits and confidence

The comparison is a defined observation sequence, not a simultaneous dump of every
checker cache. Unknown Go payload fields fail discovery; C# uses explicit static
fields and source-generated serialization for AOT. Physical untouched-page presence
is excluded where process-global IDs change allocation placement; actual resolved
symbols and fixed-ID store probes remain. Opaque hash bits are not comparable;
actual populated key associations/equality are tested instead. Diagnostic channels
retain the historical null-to-empty-list normalization; individual argument nullness,
values, order and diagnostic chains remain compared. These limits are explicit.

Retained mapper identities and mapped/merged-symbol stores remain unhit; selected
ephemeral mapping is assessed through reference arguments/results and source review.
Other unselected stores, cache associations and zero-edge paths remain unverified.
Historical linked-FlowList sharing, full source-position mapping and unobserved
scanner/parser/binder diagnostic exclusions remain documented in earlier assessments;
they were not reopened without a concrete dependency. Only Linux x64 Native AOT is
the verified acceptance platform. This is not exhaustive behavior for all input sizes,
all compiler options, a standard-library environment or full checker parity.

## Work process and next campaign

Three existing GPT-6.1 Sol agents were reused for core, support and independent
verification, followed by cross-review. The coordinator managed the fixed contract,
dependency decisions, review findings and acceptance. Available metadata still does
not independently confirm the coordinator's model; no substitute was silently chosen.
No package installation or further user intervention was required.

One core owner compiled shared library batches; the verifier used isolated CopyLocal
adapter snapshots with before/after hashes. Original Go corpus preparation and rich
observation development ran alongside implementation. Focused checks advanced into
the full matrix, with comprehensive native/regression validation reserved for the
final frozen tree. Per-owner files and a capability table sufficed for recovery; no
new database, agents or process framework was needed.

The richer observer exposed correctness gaps that diagnostic counts alone missed.
Independent review found additional in-contract guards and targeted branch omissions.
Several triage packets initially confused adjacent cache fields; adding exact paths
and original field/store labels made observations distinct from provisional causes.
Diagnostic/cache dependency work remained substantial despite a data-only source
boundary. No optimization or performance investigation was performed.

From the baseline commit at 20:13:35 UTC through final gate completion at about
21:38:49.575 UTC, this campaign took **85 minutes 14.575 seconds** of concurrent
wall time, excluding final documentation and delivery commit. There were 19 numbered
shared-library integration builds, plus separate adapter/oracle builds and the final
gate; this is not a count of every build command. The final gate took 82.979 seconds.
These are wall-clock observations, not labor, throughput or token-cost measurements.
Token and monetary totals are unavailable.

The next recommended substantial capability is **functions and callable object
shapes**: non-generic signatures, parameter/return checks, calls, method properties,
contextual function expressions and an explicitly chosen statement/flow boundary.
That would test the largest ordinary-use gap in these data shapes. Inspect its
library and inference dependencies before choosing the next contract; do not assume
this campaign establishes generic, library or representative-project feasibility.
