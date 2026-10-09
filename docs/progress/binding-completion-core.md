# Source-file binding completion core checkpoint

Owner: scanner/binder algorithm agent, GPT-6.1 Sol role retained. Baseline `2886d47`
clean at investigation start; TypeScript pin
`fed0bf24149fb1ed36039212648bafdafc1ea10e` unchanged.
This new campaign assesses complete initial source-file binding; no checker,
NameResolver/ReferenceResolver expansion, project/module resolution or emit API.
The [fixed contract](../BINDING-COMPLETION-CONTRACT.md) is selected. Binder algorithm edits have not been needed. A required scanner dependency nil-state
repair is now landed; systematic behavior acceptance remains pending.

## Exact inventory and current finding

[Per-function inventory](../binding-completion-functions.json) covers all **167**
pinned binder.go functions, source lines/hashes, counterpart lines, dependencies and
within-binder call relationships. Current port contains163 emitted function bodies,
2 manual ActiveLabel accessors, managed getBinder allocation and omitted putBinder
pool reset/return. No missing declaration/control-flow/diagnostic handler, successful
placeholder or source-feature guard was identified in this initial inventory.
The five C# throws correspond to original assertions/panics, not unsupported syntax.
Named/body provenance is not an exhaustive equivalence claim; full family review and
systematic observable evidence are required before completion acceptance.

[Binder.cs](../../src/TypeScript.Scanner/Binder/Binder.cs) maps binder.go only.
Existing fail-fast [translation scaffold](../../tools/binder-port) preserves source
lexical scope, range bodies, fallthrough, labeled control flow and manual lifecycle
adaptations. Current scratch output reproduced the reviewed counterpart byte-for-byte (emitter,
patch, then LF formatter). All 10 scaffold sanity tests passed. These were isolated
Go tool/scratch checks; no shared-library build or algorithm/tool mutation occurred.

## Remaining completion obligations

- Preserve every binder-produced graph/state field, including source HasAsyncFunctions,
  ContainsThis/return/reachability/parse-error propagation, Symbol/LocalSymbol splits,
  local/member/export tables and container chains. Source-local namespace/enum
  instantiation, patterns, CJS/exports/prototype/Object.defineProperty and deferred
  expando work remain mandatory; they are not cross-file resolution.
- Systematically cover all five parser source modes, declaration/exclusion merges,
  strict-mode contextual/private names, malformed/raw-byte input, nested controls,
  IIFEs/constructors/async/generator boundaries, optional/logical/destructuring/array
  mutation flow, labels/switch and try-catch-finally reduction.
- Historical118 graph cases omit allocated-empty tables, shared table identity and
  shared FlowList tails. Exercise actual producers or give exact source dispositions;
  do not relax the identity/null/ordering protocol around existing fixtures.
- Retain independent original Go discovery, strict generated-name owner checks,
  exact diagnostics/related info and binding idempotence, including raw filename
  boundary and private ID signed64 adaptation. Foundation repairs now underpin these
  paths; the old assessment's scanner/PositionMap/parser gaps are historical.
- Binder's only tracked direct upstream function is BenchmarkBind (binder_test.go:15),
  with fixture-dependent timing and zero assertions. No upstream binder test/fuzz
  bodies exist to port. Performance is not requested; verification must be authored
  actual-Go comparison and source-based obligations, never benchmark-as-test parity.
- Codes1250/1251/1252 appear only in the retained private helper
  getStrictModeBlockScopeFunctionDeclarationMessage (binder.go:1380), with no callsite
  anywhere in the pinned binder package. Public BindSourceFile cannot emit them;
  preserve source helper and document unobserved/unreachable rather than inventing a
  new call or checker behavior. All other referenced diagnostics require coverage.

## Dependency and risk boundary

Binder imports AST symbol/flow/state/helpers; core arenas, reference AppendIfUnique,
Some/IfElse/OrElse/patterns; scanner raw name/text/error spans/token names; Set;
Assert; slices.Index and decimal strconv. No NameResolver or ReferenceResolver call
appears. Current implementations already exist. Support owns the AST/symbol/flow/
module-state/BindOnce closure and is sole shared-library build owner. Algorithm owner
owns Binder.cs and binder-port tools; verification owns canonical oracle/tests/gates.

Managed fresh binders/object arenas replace Go pools/arenas. Go slice headers use
existing managed copies/nullable ranges; no binder ranged work list was found to
append itself while iterating. Map iteration order remains unordered; ordered
symbol declarations/flow lists must remain exact. Finite stack/resource exhaustion
and arbitrary reused bound nodes/incremental rebinding are separate exclusions,
not accepted success shortcuts. C# SourceFile public filename is encoded exactly
once into raw module-symbol names; internal FE/private/pattern bytes remain raw.

Suggested sequence: finalize source-file contract and instrumentation obligations;
reuse118 old cases as baseline, then actual producer witnesses for previously
unobserved identity/flag structures and broader syntax families; repair concrete
source discrepancies only; independent support/core+oracle review; scratch scaffold
comparison and LF cleanup; explicit all-writer freeze; final native and foundation/
checker historical regressions with source/asset guards. No algorithm edit proposed
solely to make this a larger phase. Source-family audit is complete with no identified
new algorithm defect; next action is verifier's targeted then broad actual-Go binding
comparison and independent source/observer review.

## Source audit and natural identity producers

All source bodies were read across declaration/symbol/scope, dispatch/container,
flow/narrowing and diagnostic families. Retained range loops do not append their
iterated worklists, and CJS promotion writes a different table while iterating
module exports. The managed slice/map adapters therefore introduce no identified
current binder callback-mutation discrepancy. No exhaustive runtime branch claim.

- Allocated-empty table: verifier's original-Go `class {}` witness uses GetLocals
  allocation followed by missing-name declaration without insertion. It replaces
  earlier speculative malformed-variable candidates, which do not produce a table.
- Shared FlowList tail: pinned binder.go:2070 combines copied normal/exception heads
  with original return-label tail; :2082 publishes that same return list in an IIFE
  reduce snapshot. Conditional try return plus normal exit and reachable finally was
  confirmed by original Go after a trailing `z;` publishes the IIFE return flow.
  Actual graph: 7 FlowList cells, one with 2 incoming references, 2 reduce-data nodes.
  Without the trailing flow-bearing node the shared tail is not publicly rooted;
  managed/native campaign comparison remains pending.
- Distinct-owner table alias absence: whole-field Members writes at :1003/:1638 are
  fresh allocations; all other owner slots use ast/utilities.go:46 nil-slot allocation.
  There is no table-to-table assignment. CJS :1050 shares Symbol values between
  separate tables. Multiple nodes sharing one Symbol have one owner slot, not two
  distinct owners. This depends on freshly parsed nil binding tables and excludes
  synthetic/prebound/checker inputs. Independent support challenge remains required.

Four helper requirements sent verification: strict raw prologue syntax/stopping;
value-declaration assignment/module priorities; every legitimate container kind and
parent/initializer distinction; private raw byte names and full signed64 owner IDs.
Unused strict helper1250–1252 remains source-preserved with no new caller.

## Shared support review and isolated producer ownership

[Independent support review](./review-binding-completion-support.md) found a final-
snapshot-only concurrency assertion gap. Support repaired started/returned observation
and immediate per-caller published/completed state reads. The real BindOnce source
already preserves blocking, no-retry and partial-write semantics; no runtime fix needed.
Root assigned this owner only [support.go](../../tools/binding-completion-oracle/support.go),
the eight-group actual-Go support producer; verifier owns build/fixture/dispatch.
Producer is gofmt-normalized and stable, original execution pending. Go map reference
identity uses the existing oracle reflect.UnsafePointer facility, never serialized
addresses. Support independently reviews this newly authored producer. No shared
library build or Binder.cs/tool mutation occurred; authored producer is the sole
new executable file owned by this agent so far.

Actual Go compiled the isolated support/helper producers and generated independent
fixtures. Verifier's first managed integration reports 24 cold/prewarm graphs plus
8 support and 4 helper groups exact. Initial broad comparison retained case10003:
private destructuring generates the raw name from the lexical class while its
Symbol.Parent is the object-literal symbol. The completion-only oracle/native repair
now validates that exact lexical-owner preimage, canonicalizes that owner and still
encodes the actual Parent independently. Source review accepts the repair; replay
and final broad/native evidence remain verifier-owned. Historical observer defaults
and fixture bytes remain unchanged.

The new metadata-null extension found a required scanner dependency defect:
CommentDirectives was always allocated-empty in C#, whereas the upstream default,
Reset and SetText zero the slice. Nullable backing/getter, append-only allocation,
and nullable Mark/Rewind snapshots now preserve absence. Existing parser forwarding
already preserves null. Two authored absence assertions now use Assert.Null and a
present-mark/text-reuse test was added; independent Go/shared lifecycle evidence and
legacy []-projection are verifier-owned. No binder algorithm/tool change occurred.
The scanner's initial temporary translation scaffold is not authoritative or safely
regenerative; this small reviewed source repair has no durable port patch to update.

Support's consolidated nullable-directive Release library/checks snapshot compiled
with zero warnings/errors. Five authored test nullable warnings were then repaired
with explicit array-presence assertions; no library change was needed.
Next: exact affected-case/metadata replay,
final observer dispositions, all-writer freeze and native/regression gate. This owner
has no additional source edit pending; support remains sole shared-library builder.


All owned executable/tool/corpus writes are now frozen. Final consumer review of
six helper groups (including directive lifecycle and seeded graph sensitivity)
passes with original-Go provenance; private-name signed preimage and lexical-owner
normalization are exact in completion mode. Documentation remains writable; support owns shared builds and verifier owns final managed/native results.
The one tests-only nullable warning correction was completed before the renewed
all-owned-file freeze; no suppressions or runtime changes were introduced.


The first final gate stopped on case10003's SymbolName helper projection, which
encoded a legitimate raw generated ID instead of its validated owner relationship.
An independent original-Go witness/order experiment reproduced exactly that one
field difference; binder and SymbolName source remain correct/unchanged. Verifier
owns the completion-only BoundName schema repair and affected replay; final gate
must repeat from a frozen corrected snapshot. This owner's source/tool freeze
continues, with docs-only review evidence updates.


The completion helper DisplayName descriptor and corrected OrderSensitivity were
independently reviewed. Exact original10003 and exact shared regression each fail
the preserved old observer's counter-history equality and pass the repaired full
canonical equality after eight private-prefix requests. Generated-private and raw
ordinary display presence are both observed; no weakened name check or runtime
change. Review records live in artifacts/binding-completion-validation. All owned
executable/tool files remain frozen; final corrected gate evidence is still pending.
