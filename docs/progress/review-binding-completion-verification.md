# Independent binding completion verification review

Reviewed the opt-in [completion observer](../../validation/Binder.Validation/Completion.cs),
[graph encoder](../../validation/Binder.Validation/Runner.cs), independent
[Go composition](../../tools/binding-completion-oracle/prepare.py),
[Go completion driver](../../tools/binding-completion-oracle/main.go),
[comparison](../../tools/compare-binding-completion.py),
[gate](../../tools/verify-binding-completion.sh),
[drift recorder](../../tools/run-binding-completion-gate.py) and
[evidence recorder](../../tools/record-binding-completion-evidence.py).
This review did not modify verification code or expectations.

- Fresh binding is performed on a primary parse untouched by observers. The unbound
  baseline comes from a separate parse. Explicit warm controls and post-bind lazy
  JSDoc observations remain separately labeled. Repeated binding checks syntax,
  symbol, table, flow and linked-cell identities before helper observations.
- Found and resolved a repeat-snapshot omission: normalized diagnostics alone did
  not distinguish nil arguments/chain/related channels or all diagnostic metadata.
  Both observers now include the complete recursive diagnostic state and the nil
  bind-diagnostic channel in their repeated-bind snapshots.
- Full diagnostic state augments the retained canonical code/category/span/key/raw
  arguments/formatted-text/ordered related-chain data. Twelve source metadata nil
  channels are observed directly rather than inferred from normalized empty arrays.
  This exposed the real scanner CommentDirectives nil-state defect; the coordinated
  nullable-storage/getter/reset/text/Mark/Rewind repair passed all 26 targeted graph
  comparisons and the actual-Go lifecycle control.
- The sole initial broad graph mismatch was an observer assumption: generated private
  names encode the lexical class identity even when malformed syntax leaves another
  actual Symbol.Parent. Completion-only validation now verifies the true lexical
  owner, exact signed ID spelling and raw prefix/suffix while retaining Symbol.Parent.
  Historical observer defaults and historical fixture bytes remain unchanged.
- Independent [seeded observer controls](../../tools/binding-completion-oracle/sensitivity.go)
  compare shared allocated-empty table slots, a shared FlowList tail and a cycle using
  actual graph encoding. These are explicitly authored controls, separate from the
  naturally reachable empty-table and shared-tail witnesses. Directive lifecycle
  calls the genuine Go scanner and compares nil state and contents across sixteen stages.
- Stream equality is strict over all keys, ordered arrays and values. Process errors,
  mismatches, limited runs and wrong request counts fail acceptance. Every source unit
  preserves its source bytes, mode and parse options; observation controls alone change.
- The gate verifies eight support groups, six helper/control groups and all targeted
  xUnit/native registry counts, then the full 17,723-request native stream. Original
  numeric finite baseline argument sets are separately checked; the frozen foundation
  smoke invocations are not relabeled as full finite execution.
- Executable source hashing includes all library, test and validation C#/project files.
  Asset hashing covers tools and corpus assets, including the scanner corpus. Historical
  tracked corpora and accepted result records are checked against baseline 2886d47
  before and after execution. The new native binary is hashed before its first execution.

Current conclusion: no unresolved verification blocker on the reviewed stable packet.
The coordinated Release library/completion runner/checks, Binder.Tests and legacy scanner
adapter build without warnings or errors. The scanner test-only nullable assertions were
repaired and UtilityTests passes 9/9. All eight support and six helper/control groups and
26 targeted graphs are exact against Go. The final frozen-source native gate and recorder
delta remain required before acceptance. These checks do not establish cross-file semantics,
arbitrary seeded binding behavior, allocation capacity equality or a universal scheduler
guarantee for the bounded concurrent wait observation.

## Gate and omission challenge delta

The first final-gate attempt exposed an authored observation defect: an Ast.SymbolName
display string could retain the actual process-global private-owner ID on malformed
property syntax. Stored expected bytes then depended on xUnit allocation order. Acceptance
is blocked until that helper observation reuses a strict genuine-owner descriptor while
preserving the real raw prefix/suffix and actual parent relationship. This is an observer
correction, not a binder algorithm failure; historical expectations must remain unchanged.

The [payload coverage reconciliation](../binding-completion-payload-coverage.md) separates
85/178 source-model cells from 81/172 cold observed classes/cells and explicitly avoids
inferring payload-specific states from aggregate field-role counts. No missing source
path was identified; final acceptance remains contingent on the repaired native gate.
