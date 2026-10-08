# Source-file binding feasibility checkpoint

**Accepted as a bounded source-file binding milestone**, with no unresolved
observed discrepancy. The actual Native AOT executable agrees with pinned Go on
118 canonical graph/diagnostic cases and four concurrency/failure/API checks.
All 122 binder, 475 parser and 1,287 scanner xUnit tests passed, together with
native parser/scanner regressions. See [verification](./BINDER-VERIFICATION.md)
and the [durable evidence](../validation/results/binder-verified.json).
This is not a claim of full binder parity.

The preceding parser/performance work was committed as `de25b66` before this
phase began. Binder work was saved uncommitted at this checkpoint, then committed as `e94cc6f`
before the next bounded checker phase. Nothing has been pushed. The unchanged upstream pin is
`fed0bf24149fb1ed36039212648bafdafc1ea10e`.

## Implemented scope and dependency cost

[Binder.cs](../src/TypeScript.Scanner/Binder/Binder.cs) maps directly to binder.go
and retains its source-file binding algorithms, names, function boundaries and
control flow. It constructs declarations/symbols, local/member/export scopes,
container chains, binder diagnostics and cyclic control-flow graphs. Local
namespace/module-instantiation classification, CommonJS/expando handling and
ambient pattern metadata stay within the source file. Resolver sibling files
are not called by BindSourceFile and remain excluded.

The parser AST was a reusable foundation, but syntax fields alone were insufficient.
The phase required declaration/export/local/flow bases on exactly the upstream
payloads, symbol and flow models, synthetic flow data, source-file binding state,
additional AST helpers, diagnostic metadata, source-file diagnostic span helpers,
ECMA line caching, ambient-pattern handling and filename extension removal.
The [source map](../source-map.json) identifies partial files and excluded APIs.
No checker, cross-file resolver or emitter implementation was needed.

The independent oracle compares meaningful graph structure, not symbol counts:
identity, ordered declaration relationships, local/export distinctions, scope
chains, full flags, raw names, flow edges/cycles/sharing and exact diagnostics.
Process-global generated-name counters are normalized only after verifying their
owner relationships. The C# harness is statically typed and AOT-safe; the Go oracle
reflects actual upstream fields independently of the translation schema.

## Readiness and remaining uncertainty

Before extension, the parser evidence established pre-bind syntax structure and
syntax diagnostics across 460 cases, including all 186 payload types directly
constructed by the parser. It did not establish any binding output. We classified
all 29 unobserved diagnostic codes against pinned callsites and added 11 targeted
recovery fixtures, exercising 14 additional codes with no parser defect found.
The 15 remaining codes and their risk categories stay explicit in the
[readiness assessment](./BINDER-PARSER-READINESS.md) and verification report.

Binding coverage includes all atomic flow flags, all binder-produced symbol flags
and 27 of 30 referenced diagnostic codes. The other three are in an unused helper.
Shared table identities, shared linked-flow-list identities and allocated empty
tables are represented by the protocol but unexercised. The selected corpus is not
exhaustive, and some script modes have few specimens. Scanner diagnostic 1513 also
remains unobserved. There is no current failing comparison, but these are real
limits on the acceptance claim.

Excluded work remains: name/reference resolver APIs, type checking, checker-owned
CheckFlags/transient machinery, cross-file/global semantic merges, module resolution,
code generation, incremental rebinding and language services. General transform
facts and full source-position mapping remain partial as documented previously.

## Language adaptations and review findings

Managed allocation replaces Go binder pooling and object arenas. Standard ordinal
dictionaries preserve raw byte-string keys, with an explicit missing-key-null
indexer and nullable tables matching Go map reads. Reference identity preserves
symbol and flow sharing. Interlocked operations supply stable IDs; a non-reentrant
gate and volatile publication preserve BindOnce success, failure/no-retry and
concurrency semantics. No speculative optimization was introduced.

Existing raw UTF-8 byte-string representation remains authoritative. Public .NET
filenames encode once at the module-symbol boundary. Review caught the signed
64-bit Go `int` conversion for private-name IDs; the C# adaptation now uses `long`
invariant decimal formatting. The default .NET parse-options filename can be null,
so binding coalesces it to Go's empty-string value. Both adjustments are small and
persisted in regeneration logic or owned source. The absent diagnostic-list test
originally expected an empty collection; that was a harness assertion defect,
corrected to the upstream null behavior rather than changing implementation output.

Independent Sol cross-reviews covered binder algorithms/emitter/scanner helpers,
AST/symbol/flow support and the oracle/AOT harness. Generator scratch comparisons
preserve reproducibility for generated portions; handwritten support remains
explicitly owned source. The diagnostic protocol now asserts unrepresented fields'
required defaults instead of silently assuming them.

## Effort and working strategy

Three reused agents were explicitly selected as GPT-6.1 Sol for algorithms,
AST/support, and independent verification. The main session owned scope, contracts,
coordination and acceptance. Available metadata did not let the session independently
verify or change its own coordinator model; that limitation is retained rather
than claiming a verified Astra selection.

Recorded activity ran approximately 16:51–17:23 UTC on 2026-10-08, about 32 minutes
through the final gate, followed by documentation. This is elapsed time with
concurrent work, not measured labor. The final gate took 37.26 seconds. Token totals,
per-agent compute usage and monetary cost were unavailable; no cost estimate is
inferred. No additional user intervention or system package installation was
needed during this phase. Binder performance was not measured.

Useful practices were reusing the parser AST/serialization foundation, classifying
concrete readiness risks before extending fixtures, separating Go oracle ownership
from implementation, disjoint file ownership, one shared-build owner, focused
compiler-error handoffs, independent cross-review and source freezing before the
final combined gate. Small file-based checkpoints were sufficient; SQLite added
no demonstrated value yet.

The initial small integration plan needed adjustment because binder dispatch,
container handling and child traversal form one large mutually recursive component.
A real low-level model followed by integrated handlers avoided fake passing stubs.
Generated helper identifier issues were fixed in the emitter once rather than
patched repeatedly downstream. Some early progress notes became stale after gate
completion; final state now replaces pending next steps to avoid repeating work on
re-entry. These are observed process improvements, not quantified token savings.

## Recommendation

This provides a sound foundation for **another bounded, single-file initial
checker slice**: the declaration/scope/flow structures it would consume now have
meaningful native differential evidence. It does not establish checker feasibility
at compiler scale or authorize starting it.

A next mandate should first inventory the name/type machinery required for a small
slice such as local identifier lookup, primitive literals and basic assignments.
Resolve that dependency boundary before implementation: the excluded resolver APIs,
checker-owned caches/flags and type model may have a much larger closure than the
binder. Define type and diagnostic observables against Go, exercise the currently
unobserved structures when the slice depends on them, and retain all native
regression gates. Cross-file resolution and full inference should require their
own explicit scope decisions.
