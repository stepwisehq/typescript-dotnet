# Binding completion support checkpoint

Owner: AST/support; baseline `2886d47`; upstream `fed0bf24149fb1ed36039212648bafdafc1ea10e`.
Implementation and independent reviews complete; final campaign acceptance remains pending.

## Ownership and current implementation

- Own binding support in [Symbol.cs](../../src/TypeScript.Scanner/Ast/Symbol.cs),
  [Flow.cs](../../src/TypeScript.Scanner/Ast/Flow.cs), [Ast.cs](../../src/TypeScript.Scanner/Ast/Ast.cs),
  generated embedded bound fields and required AST helpers. No checker expansion.
- Scanner agent owns binder algorithms; verifier owns independent oracle/corpus.
  Support agent alone owns shared-library builds; central Release integration is clean.
- Existing Symbol/SymbolTable and FlowNode/FlowList models contain all required
  upstream fields. Keep real CheckFlags, but require zero on binder-only graphs.
- Independent flattened-schema comparison found all 178 binding fields in 85
  generated payloads, with matching BoundFields role/order; this is structural
  source evidence, not a substitute for behavioral verification.
- [Support inventory](../binding-completion-support-inventory.json) records current
  source/destination hashes and exact reviewed scope, including binder-required core,
  collection, scanner, diagnostic and runtime dependencies with explicit exclusions. [Core inventory](../binding-completion-functions.json)
  independently inventories all 167 binder functions.

## Identity and lifecycle obligations

- Keep raw byte keys, missing-read null, nil tables distinct from allocated empty
  tables, shared Symbol/declaration/export identities and ordered linked FlowLists.
- Binder never assigns one table slot to another; lazy helpers allocate per nil
  slot. Fresh source-file binding appears unable to alias two distinct table slots.
  Scanner independently confirmed this producer proof; preseeded model sharing is a separate API probe.
- Empty type literals need a nil-state probe. The explicit allocation at binder.go:1003
  immediately inserts a signature. Actual pinned Go witness `class {}` allocates
  empty SourceFile Locals before declareSymbolEx skips insertion for its missing name;
  class Exports contains prototype. `const = 1` and missing interface properties were
  tested candidates, not witnesses.
- combineFlowLists copies the head and retains the tail identity. Actual Go witness:
  `(function(){try{if(a)return 1;x=2;}finally{x=3;}return 4;})();z;` retains seven cells,
  including a tail shared by chain Next and reduce snapshot. The trailing use matters.
- SourceFile.BindOnce uses a non-reentrant gate: callback completion blocks waiters,
  failure marks the attempt done without publishing IsBound or retrying; partial writes remain.
  IsBound true publishes earlier successful writes. No general concurrent AST mutation guarantee.
- Shallow clone/changed update starts with fresh binding state while sharing upstream
  syntax references; unchanged update retains original identity and bound state.

## Next action and verification state

Binder algorithms and historical acceptance artifacts remain unchanged. A richer nil-state
observer exposed the real Scanner.CommentDirectives absence defect; scanner ownership repaired
nullable storage/getter, Reset/SetText and Mark/Rewind. The public nullable array reflects actual
Go absence; only the historical JSON projection normalizes absence to an empty array.
New shared assertion files and authored Runtime/BindingCompletionAccess.cs friend are written.
Last commands: `dotnet build validation/BindingCompletion.Validation/BindingCompletion.Validation.csproj -c Release --no-restore`
and `dotnet build tests/Binder.Tests/Binder.Tests.csproj -c Release -p:BuildProjectReferences=false`:
zero warnings/errors. Eight shared support groups and 24 targeted graphs initially passed
against actual Go, including two natural empty tables and one shared tail. Initial broad
managed comparison was 17,722/17,723 exact: the sole private-name lexical-owner assumption
was corrected in the opt-in observers, without changing bound parent relationships.
New twelve-channel source metadata observations exposed the directive absence defect.
The repaired Release library/completion checks/adapter, Binder.Tests and legacy scanner
adapter compile cleanly; repaired targeted 26/26 graphs and all shared eight support plus six helper/control groups
are exact against Go; Scanner UtilityTests passes 9/9 without warnings. The final
frozen-source native gate remains.

[BindingSupportChecks.cs](../../validation/BindingCompletion.Checks/BindingSupportChecks.cs)
and [xUnit wrapper](../../tests/Binder.Tests/BindingSupportTests.cs) are stable; verifier owns
projects/native dispatch/fixtures and four exported-helper checks plus directive lifecycle
and seeded observer sensitivity controls. Scanner independently
reviewed assertions; waiter-return coverage was strengthened before original fixture generation.
[Core review](./review-binding-completion-core.md) and [verification review](./review-binding-completion-verification.md)
have no unresolved blocker. Repeat-bind snapshots now include full recursive diagnostic
state and diagnostic channel absence. No required symbol/flow/AST model defect identified.
Owned executable sources are frozen. Next: verifier formatting/asset freeze, then the final
comprehensive Native AOT gate and a read-only recorder delta review.
Root fixed [the contract](../BINDING-COMPLETION-CONTRACT.md); required strata include all five
modes, declaration files, external/CommonJS/ambient modules, JS JSDoc and recovery.
Do not reopen accepted foundation/semantic behavior without a concrete dependency defect.
