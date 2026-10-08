# Parser readiness for source-file binding

Read-only assessment of checkpoint `de25b66`, upstream TypeScript
`fed0bf24149fb1ed36039212648bafdafc1ea10e`. The accepted executable-source hash
remains `d441e371cf28045777b3c8041d51e524ce96904ddecf6ff7e4f105edaf2edf8d`.

## What the parser checkpoint establishes

The [460-case evidence](../validation/results/parser-verified.json) compares
19,268 syntax nodes, 247 kinds, all 186 payloads referenced by direct parser
factory calls, exact byte spans/flags/raw scalars, field roles, child order,
null/empty/missing lists, parent identities, reparsed/lazy JSDoc, source metadata,
and 318 ordered diagnostics. It includes real sources, declaration filenames,
JS/JSX/TS/TSX/JSON, malformed recovery, Unicode/raw bytes, and top-level await.
It establishes meaningful pre-bind input agreement for those cases. It does not
establish symbol construction, declaration merging, scope correctness, flow
graphs, binder diagnostics, or exhaustive malformed recovery. All parent refs in
the accepted corpus have canonical IDs; no positional-only fallback hid a parent.

Previously excluded fields now required by binding:

| Original AST field/base | Binder observable |
| --- | --- |
| DeclarationBase.Symbol | Declaration-to-symbol identity |
| ExportableBase.LocalSymbol | Local/export symbol split |
| LocalsContainerBase.Locals, NextContainer | Scope tables and container chain |
| FlowNodeBase.FlowNode | Flow attached to references and expressions |
| BodyBase.EndFlowNode; concrete ReturnFlowNode | Function/constructor exit flow |
| SourceFile bound state, BindDiagnostics, SymbolCount | Binding status and diagnostics |
| SourceFile.GlobalExports, PatternAmbientModules | Global and pattern module publication |
| FlowSwitchClauseData, FlowReduceLabelData | Synthetic KindUnknown flow payloads |

General transform facts, checker/transient state and atomic numeric IDs remain
outside the comparison unless actually needed by the binder. The binder entry
file has no direct SubtreeFacts call. Atomic IDs can occur inside generated symbol
names and require identity-aware treatment described below.

## The 29 unobserved parser diagnostic codes

Classification follows the actual pinned parser/jsdoc callsites, rather than
assuming that the diagnostic name identifies the triggering grammar.

| Codes | Binding impact and priority |
| --- | --- |
| 1359,1389,1439,2427,2457,17021,18016,18029 | Reserved, missing, escaped and private declaration names can change table keys/missing symbols. Close representative reserved function, invalid interface/type alias, and private variable recovery before claiming malformed-binding coverage. Other variants remain explicit gaps. |
| 8033,8034,8039 | JSDoc typedef/callback child-tag recovery affects generated JS declarations and includes related diagnostics. Close exact multiple-type-tag and callback/template-order cases. |
| 1129,1132,1139,1140,1144,1179,1181,1478 | Statement/declaration/parameter/typeargument/binding/import-attribute recovery can change scope children, missing lists and function bodies. Close compact class/enum/typeparameter recovery, array-binding recovery and malformed import attributes; do not blindly regenerate all grammar. |
| 1142,1228,1441,1443 | Type-keyword line break, misplaced predicate, property annotation and module-name fallback. Lower priority after representative container/name recovery; inspect if a binder differential points here. |
| 1206,1486,8038 | Invalid decorator placement, including a related-info companion. Modifier ordering is already structurally compared; keep targeted decorator boundary coverage for binder containers, without requiring every diagnostic spelling first. |
| 8011,8016 | JS-only type argument/assertion diagnostics. AST type forms already covered in TS; add a small JS assertion boundary if preserving invalid-JS binding behavior. |
| 17002 | JSX closing-name mismatch. A compact actual mismatch case closes this recovery shape; JSX attributes still need binder symbol tests. |

Twenty read-only Go/current-native probes found 14 previously unobserved codes
and **zero structural differences**: 1132,1139,1144,1181,1359,1478,2427,2457,
8016,8033,8034,8039,17002,18029. These are exploratory readiness results, not an
addition to the historical accepted 460 fixtures. Eleven stored readiness fixtures subsequently preserved these paths, including JS assertion and JSX
mismatch boundaries; they passed the unchanged accepted native parser before
binder extension (156 nodes, 29 diagnostics). See the
[readiness evidence](../validation/results/binder-parser-readiness.json). No parser defect was found. The 29 gaps do not
block starting binding on valid syntax; the representative recovery closures are now included in final regression coverage.
The remaining 15 unobserved codes are 1129, 1140, 1142, 1179, 1206, 1228, 1389,
1439, 1441, 1443, 1486, 8011, 8038, 17021 and 18016. This historical assessment
should be read with the [final verification](./BINDER-VERIFICATION.md).

## Independent binder oracle contract

Scope: parse and BindSourceFile for one source, including JS CommonJS/expando,
JSDoc-derived declarations, ambient namespaces/pattern modules, and TSX. Exclude
NameResolver/ReferenceResolver, checker hooks, global program merging, cross-file
module resolution and emit. [binder_test.go](../upstream/TypeScript/tsc/internal/binder/binder_test.go)
contains BenchmarkBind only, with no assertion tests; running it without a
benchmark provides no substantive acceptance evidence.

Inputs reuse parser bytes/modes/file/module options, with explicit optional
JSDoc resolution before binding. Default binding must not force extra lazy
JSDoc simply to help serialization. Check the second BindSourceFile call is
idempotent; add a focused concurrent BindOnce test separately.

Response retains the exact syntax graph/diagnostic channels and adds:

- Per-node bound fields, discovered independently from actual Go embedded
  payloads: symbol/localSymbol IDs, nullable locals table, next-container ID,
  flow/end-flow/return-flow IDs, complete post-bind NodeFlags.
- Symbols: canonical identity, full flags, raw name, ordered
  declaration IDs, valueDeclaration ID, parent/exportSymbol IDs, nullable
  members/exports tables. Anonymous and duplicate-name distinct symbols remain
  distinct. Never summarize by name or declaration count.
- Symbol-table entries sorted by canonical byte key (ordinary names stay raw; generated names use verified owner identity), with raw Base64 keys and symbol
  IDs; preserve null versus empty. Preserve shared table identity where exposed.
- Flow nodes: canonical identity, complete flags, associated AST ID,
  antecedent ID and ordered linked antecedent-list graph. Assign IDs before
  recursion so actual loop cycles and shared edges survive. Include synthetic
  switch clause range/statement and reduce-label target/temporary antecedents.
- Source: IsBound, SymbolCount, GlobalExports, PatternAmbientModules with raw
  pattern and symbol reference, post-bind module indicators, ordered exact bind
  diagnostics with code/category/span/key/raw args/text/related information.

Canonical IDs are deterministic graph-discovery identities, not Go allocation
counters. Ordinary names stay exact raw bytes. Only known generated private
`FE#<symbolId>@<description>` and attributed ambient-pattern
`FE"<pattern>"pattern@<nodeId>` names may replace the numeric counter with the
canonical owner symbol/node reference. Assert the original encoded numeric ID
matches that actual owner and retain exact prefix/suffix bytes; do not strip
arbitrary digits or normalize names/counts. This prevents JSONL/xUnit execution
order from causing false differences while still detecting wrong ownership.

Go read-only reflection must discover new bound fields independently, fail on
unknown types, and never consume the C# metadata generator's field manifest.
C# uses generated static bound-field dispatch and source-generated JSON for
Native AOT. Syntax/symbol/flow IDs occupy separate namespaces; lists and graph
edges retain order. Parent/scope/container chains and declaration ownership get
separate invariant checks. Begin with tiny declarations plus a loop/try-finally
cycle case, then broaden by observed symbol flags, flow flags and diagnostic
paths. Final acceptance requires actual native comparisons and preserved parser
and scanner regression gates.

Final scope decision: checker-only Symbol.CheckFlags remains excluded. The Go
oracle asserts zero for every encountered symbol; the C# model does not fabricate
a field to claim equivalence. All contract gates described above passed at the
bounded checkpoint; limits are recorded in the final verification report.
