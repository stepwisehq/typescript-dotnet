# Independent binding core review

Initial review, before campaign runtime comparisons. Upstream/baseline:
`fed0bf24149fb1ed36039212648bafdafc1ea10e` / `2886d47`.
No binder algorithm or library source changes requested by this review so far.

Reviewed mapped [Binder.cs](../../src/TypeScript.Scanner/Binder/Binder.cs)
against [binder.go](../../upstream/TypeScript/tsc/internal/binder/binder.go),
with the independent [167-function inventory](../binding-completion-functions.json).
Source presence and scaffold regeneration are supporting evidence, not behavioral acceptance.
An independent name/declaration check found all 166 represented counterparts; only
putBinder pooling return is absent. The five upstream panic branches and sole Assert
have five managed throwing branches and one unconditional Assert counterpart. These
counts detect missing exceptional boundaries but do not prove every body by themselves.

- Symbol/table family (upstream 131–448, 954–1298, 2541–2567): missing-name
  declarations allocate symbols without table insertion; merge conflicts retain
  original table entries where upstream does, preserve declaration order, local/export
  links and parent validation. Missing nullable map reads use null-conditional adapters.
- Initial dispatch, containers and children (576–753, 1478–1787): source-kind dispatch,
  parent container save/restore, IIFE/constructor flow boundaries, ContainsThis and
  reachability/emit flags retain upstream order. Commented-out upstream eager-locals
  initialization is not an obligation to invent empty tables. Fresh Binder instances
  replace pooling and isolate fields; no resolver/checker calls were introduced.
- Flow families (456–574, 2014–2163): linked antecedents retain reference deduplication,
  head-copy/tail-sharing, try/finally reduce snapshots, ordered case ranges and cyclic
  flow identities. Ordinary class/reference storage represents these directly.
- Iteration/mutation adapters: declaration diagnostic loops finish before adding the
  new declaration; deferred expando declaration does not append its own work list;
  CommonJS promotion inserts Symbol references into another table rather than mutating
  the ranged source map. These routes do not depend on arbitrary callback slice mutation.
- Names/diagnostics: raw byte names and source/error-span helpers remain byte-oriented;
  generated private IDs use signed platform-int formatting through long storage. Array
  args are forwarded as variadic arrays, rather than wrapped as one argument.

## Reachability challenge

The private helper getStrictModeBlockScopeFunctionDeclarationMessage has only its
method declaration in the entire pinned binder package. No call, method-expression,
method-value or exported callback references it. Its three messages (1250/1251/1252)
therefore cannot be published by source-file binding; the separate strict-mode
identifier/eval/arguments handlers are reachable and remain included. The retained
helper body is not removed or falsely counted as a diagnostic witness.

Distinct published table-owner aliases cannot be naturally produced on fresh parser
state: all owner slots begin nil, lazy helpers allocate into exactly one slot, and
both explicit Members assignments allocate new maps. CommonJS promotion shares values,
not table storage. Scanner independently challenged this producer proof. Explicit
seeded-table support assertions must remain distinct from ordinary graph coverage.

Allocated-empty Locals has actual Go witness `class {}`. Shared FlowList tails have
actual Go witness `(function(){try{if(a)return 1;x=2;}finally{x=3;}return 4;})();z;`.
The final `z` retains the relevant published flow; shared flags alone are insufficient
identity evidence. Verifier owns these graph probes and their immutable expectations.

Campaign graph/diagnostic mismatch and observer review deltas are recorded below.
Final native acceptance remains a separate required gate.

## Shared observation review delta

Reviewed [support.go](../../tools/binding-completion-oracle/support.go) independently
against all eight shared C# observation bodies. Actual Go APIs produce expectations;
C# outputs do not seed expected JSON. Original map identities use Go-side reflection
(the existing oracle facility), matching C# ReferenceEquals without serializing addresses
or introducing observer keys. Initial content-only getter probes were strengthened
before fixture generation. Raw bytes, nil states and controlled transient/synthetic
payloads retain their explicit API/control classification.

Scanner independently caught an authored concurrency coverage weakness: final joined
state alone could accept callers returning before the held callback finished. Both
shared groups now record immediate caller-return states and a synchronized entry plus
bounded 100 ms any-return window. Dedicated C# competitors avoid ThreadPool starvation;
Go observes the same source operations with goroutines/atomic completion state. The
window is scheduler-limited progress evidence, not a universal timing guarantee.
Independent current producer/body review found no unresolved blocker. Execution and
final broad comparison review remain pending.

## First managed milestone and exported helper producer

Central Release library/completion runner/Binder.Tests compile with no warnings or
errors. Verifier ran the eight shared support groups and 24 targeted cold/warm graphs:
all exact, including natural allocated-empty tables and a retained shared tail. Broad
17,723 managed comparison completed; no native campaign acceptance claim yet.

Independently reviewed [helpers.go](../../tools/binding-completion-oracle/helpers.go):
15 private-name rows (five nonzero 64-bit patterns × three raw descriptions), 12
strict-prologue rows, nine ordered value-declaration sequences and three parsed-mode
container traversal inputs call the four genuine exported APIs. IDs are written only
to the isolated real atomic Symbol cell; signed platform-int formatting is observed,
not guessed. ID zero allocation is covered through genuine production/API paths, not
a frozen process counter. Go-only reflection/unsafe does not add native reflection.
Identifier/access/call inputs to SetValueDeclaration are source-legitimate because
the original isAssignmentDeclaration predicate explicitly includes them. No producer
input/claim discrepancy found; C# mirror and execution are verifier-owned.

## Completed broad/review delta

Initial broad source binding comparison was 17,722/17,723 exact. The remaining case
was an observer assumption about private name ownership in malformed object/class
syntax; completion observers now validate the actual lexical class ID while retaining
the distinct actual Symbol.Parent edge. No binder algorithm discrepancy was found.

Richer source metadata nil observations then found an implementation defect previously
masked by empty-array projection: Scanner.CommentDirectives allocated storage in the
absent state. Scanner ownership repaired the real nullable storage/getter and lifecycle;
the parser's nullable forwarding was already correct. Actual-Go lifecycle controls and
all 26 targeted richer metadata graphs are exact on the repaired Release snapshot. Historical JSON projection
alone keeps its old empty-array convention.

The [independent verification review](./review-binding-completion-verification.md) closed
the repeated-bind full-diagnostic-state omission and reviewed strict equality, fresh/warm
observation ordering, seeded identity sensitivity controls, counts and drift/provenance.
No unresolved source/observer blocker remains; final native/regression acceptance pending.
