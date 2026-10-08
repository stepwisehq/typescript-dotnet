# Bounded object checker plan

Status: bounded milestone accepted. All four milestones below are complete. Scalar baseline committed as
`7e692dd` on 2026-10-08 at 18:43:44 UTC. Upstream stays
`fed0bf24149fb1ed36039212648bafdafc1ea10e`. See the exact
[mandate](./OBJECT-CHECKER-PROMPT.md) and [scalar evidence](./CHECKER-VERIFICATION.md).

## Selected boundary

Extend the existing fixed single-file TS-script, ESNext, strictNullChecks,
noLib/noResolve checker with anonymous object literals and inline type literals.
Include nested data properties, optional and readonly property declarations,
static identifier/string/numeric property names, shorthand properties, dot-property reads and structural
relationships through variable initialization and the public relation API.
Preserve all previously admitted scalar behavior.

The intended semantic interactions include contextual typing, literal widening and
freshness, excess properties on fresh versus indirect objects, required versus
optional members, nested type mismatches, missing properties and spelling suggestions,
weak object types, and readonly structural compatibility according to upstream.
Self-reference or unusual names must not bypass the boundary: retain actual upstream
behavior when supported and otherwise fail explicitly.

Exclude named aliases/interfaces/type references, the standalone `object` annotation
keyword, generic types, source union syntax,
methods/signatures/calls, index signatures, spreads, computed-name expressions,
element/optional-chain access and explicit property writes. Existing exclusions for
branching flow, modules, libraries, projects, emit and services remain. Interfaces
were deferred because even non-generic forms introduce InterfaceType/thisType,
base/merge and type-parameter machinery. This phase does not add library declarations;
all eight actual missing-global diagnostics remain separate and visible.

The input admission layer is authored support; selected checker algorithms and
diagnostic NodeBuilder/Printer paths are ported from upstream. Runtime guards must
reject unselected paths instead of returning placeholder success. A real empty
signature/index list for an admitted data-only type is different from a fabricated
successful signature implementation; nonempty excluded payloads must be rejected.

## Dependency findings

Existing binder property/type-literal symbols, member tables, flags and flow-node
attachments provide the foundation. Checker-created transient property symbols must
remain distinct from their binder originals, retaining declaration/parent/target
relationships. Member resolution needs sorted named properties and nil versus
allocated-empty tables. Structural comparison must retain upstream recursive
relation/cache/error machinery rather than replacing it with a shallow property loop.

Readonly is not blanket structural immutability. Optional properties require real
undefined-union and read/write/missing distinctions. Nested objects require contextual
member checking and recursive diagnostic serialization. Ordinary data-object reads
do not introduce branch/loop FlowLists, so the previous shared-FlowList gap remains
visible rather than being reopened without a selected dependency.

Fresh object regularization lives in upstream cachedTypes, not an invented ObjectType
field. Observe actual populated cache entries where practical; do not force cache
population merely to report coverage. Repeated public widening can exercise actual
fresh/widened identity. No full mutable-cache parity is implied by public observations.

The first source inventory identifies 70 upstream functions covering about 2,647 Go
lines before the remaining helper/grammar/formatter closure. This is a lower-bound
source inventory, not an implementation estimate. Required machinery includes real
deferred diagnostics, type-resolution cycles and ternary relation/cache/maybe stacks;
the scalar relation helper alone is insufficient for these paths.

Approved language adaptation: retain getRelationKey but use a managed value key
containing the complete selected upstream preimage (source ID, target ID and
intersection state), including the original identity-order normalization. Upstream
uses XXH3-128 for this opaque key. Reference/generic key branches remain guarded;
this does not replace or simplify recursive relation/cache algorithms.

Reported dependency pressure during implementation: best-match diagnostics for
object/nullable unions call findMostOverlappyType, finite getIndexType and
intersections of literal property-key unions. Port those actual bounded internal
branches; do not substitute a custom overlap score. Source-level keyof, union and
intersection syntax remain excluded. Any retained intersection payload that requires
general recursive algebra must be reported before widening this internal boundary.

A second fidelity pressure surfaced in getTypeNamesForErrorDisplay: ordinary object
symbols can supply a nonnil enclosing declaration, and serializeTypeForDeclaration
can then invoke pseudochecker source-type reuse for data properties/scalar expressions.
Projecting that context to nil is not accepted as an unrecorded shortcut. Investigate
and port the narrowly required original diagnostic-reuse closure with direct file
mapping; report a broad closure before expanding. This does not authorize general
emit, declarations, libraries or new semantic syntax.

Differential verification then exposed per-node escaping flags: synthesized
nonidentifier property names use different ASCII-escaping behavior from scalar
literal type nodes. A narrow actual printer/emitcontext.go counterpart is required
to retain selected flags and original/clone propagation. This is diagnostic
serialization support; general emission, source maps and transforms remain excluded.

## Milestones and ownership

1. Confirm accepted scalar source/assets/native hashes; commit baseline; inspect
   upstream dependencies and choose boundary. **Complete.** No old gate rerun needed.
2. Integrate a small object construction/read/relation vertical slice, then expand
   to the selected interactions and negative cases. **Complete.**
3. Independent source-fidelity and harness/boundary reviews; resolve findings and
   complete differential/xUnit verification. **Complete.**
4. Stop all source/tool/corpus writers, run the complete guarded Native AOT and
   regression gate, preserve evidence, mapping and checkpoint assessment. **Complete.**

Reuse the three existing explicitly selected GPT-6.1 Sol agents:

- Algorithm owner (`scanner`): Checker/Flow/Utilities/GrammarChecks and
  selected exports; sole owner of shared library builds during implementation.
- Support owner (`support`): Types/Links, required AST/diagnostic support and real
  diagnostic NodeBuilder/Printer paths; Relater after the explicit handoff below;
  source-map reconciliation after integration.
- Verification owner (`verification`): authored admission, independent Go oracle,
  typed native harness, corpus, xUnit, utility controls and evidence scripts.
- Main coordinator: scope, significant adaptations, cross-review assignment,
  acceptance and durable project documentation.

Owners coordinate small shared API needs directly and exchange concise failure
packets. Do not have multiple agents repeatedly build the same evolving library.
Save progress in separate object-checker owner files; preserve prior checkpoint
records as history. Token/cost metrics are unavailable; record elapsed work and
actual reruns without presenting elapsed time as labor or subscription cost.

Implementation adjustment: once support completed the model/formatter syntax pass,
the larger checker/context closure remained active. Relater had not yet been edited,
so ownership transferred wholly to support with an explicit helper contract. This
uses available capacity without overlapping writes. The algorithm owner will review
the relation port independently; support will review central checker paths.

## Verification and stopping condition

Use real pinned Go Program/checker output on valid and invalid sources. Add opt-in
object observations to preserve the scalar expected files byte-for-byte if feasible.
Compare ordered properties and their types, symbol/declaration/target relationships,
member table identities and nil/empty distinctions, object/literal flags, query
results, widening identities and bidirectional relation results. Exact file/global/
binder diagnostic channels include raw arguments, byte spans, related information
and nested message chains. Preserve missing-global errors without broad filtering.

Authored admission controls remain distinct from Go equivalence. Verify excluded
payloads explicitly instead of silently omitting them from the graph. Assess newly
relevant empty/shared member tables and transient symbols using real cases or direct
Go/C# utility probes. Record caches and identity relationships not exercised.

Acceptance requires actual Native AOT execution of the object contract, xUnit, and
scanner/parser/binder/scalar regressions. The checkpoint may be accepted only for
the documented boundary; unexpected dependence on libraries/generics/project work
must be reported before expansion. If a useful coherent boundary cannot be completed,
report the incomplete milestone and specific dependency, not broad checker parity.

Freeze every source, tool and corpus writer—including inventory generators—before
capturing final hashes. Preserve historical evidence and save this phase separately.
At the checkpoint stop and report supported behavior, failures resolved, remaining
risks, effort and the next informative slice. Do not begin that next slice automatically.

Two historical scalar admission negatives (`let x={}` and `let x:{}`) become
supported in this phase. Preserve their historical fixture bytes/evidence, record
their retirement explicitly in current test selection, and cover them positively.
The new gate retains all 45 scalar semantic baselines and the still-applicable
boundary controls; it cannot blindly invoke an old gate that asserts the earlier
public admission boundary. No versioned admission API is added solely for testing.

## Final disposition

The actual Native AOT checker matched 43 object and 45 scalar cases. All 2,033 xUnit
checks and native binder/parser/scanner regressions passed, with no source/assets
drift in the final 45.201-second gate. An earlier successful gate was repeated only
after removing an accidental EOF newline, with both proofs retained. See [verification](./OBJECT-CHECKER-VERIFICATION.md),
[assessment](./OBJECT-CHECKER-ASSESSMENT.md) and [durable evidence](../validation/results/object-checker-verified.json).
The required finite-key and diagnostic reuse/emit-flag support was completed within
the stated source boundary. No unresolved observed discrepancy or review blocker
remains. The two historical negative controls were explicitly retired as planned.
Stop here; the recommendation in the assessment does not authorize another phase.
