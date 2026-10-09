# Resolution verification checkpoint

Accepted within the fixed [R1–R6 contract](../RESOLUTION-CONTRACT.md). The complete final source10 gate passed with exit 0 in 912.603 seconds and all five drift flags false. The durable [proof](../../validation/results/resolution-verified.json) records source `26523aec2698fd044ce71826b223e7b779ffbd051404d6e60d8fccbc3f291c8b` and verification assets `56f058a5b1479e5249b8650bbfecb3b946fe0b0c41cc3dcde9618271cb2058f4`. Historical proofs/corpora and the unchanged upstream pin are preserved.

## Actual final execution and independent review

The original Go package replay passes all 38 original Module bodies with no failures/skips. Actual Linux x64 NativeAOT passes all 40 shared groups (38 preserved bodies plus two authored controls), every one of the 216 complete canonical records, and 30 executions per runtime under forward/reverse sequences with intervening repeated requests. The Resolution xUnit TRX reports 256 passed, zero failed/skipped: 216 corpus cases plus the same 40 shared bodies. Across the single retained cascade and the new suite, 24,334 xUnit cases pass.

The 216 requests comprise 69 core/default/source-API requests with 310 queries, 105 adapted compiler-input graphs, 16 targeted graphs, 22 utility requests with 368 queries, and four example API requests. They are compound differentials, not original test-body counts. The 76 selected upstream compiler inputs have explicit dispositions: 72 reused with original extracted payload bytes and four removed-mode-only exclusions. Checker/emit assertions and unapplied embedded-config harness overrides are not claimed as passing original compiler tests.

All 95 ordinary original API records match a pristine companion byte-for-byte. The final original38 replay uses no overlay. The original graph observer instead invokes genuine loader and diagnostic producers through its explicitly scoped overlay; its external-stage omissions are documented. The final native ordinary CLI passes all 11 controls: six genuine Program semantic comparisons, three original-loader graph comparisons, and two authored unsupported boundaries. Support independently reviewed the ordinary CLI and retained project/cascade evidence.

Verification independently correlated complete original/native payloads with every committed golden, current request/expected/report/binary hashes, original Go test events, the native registry/control output, TRX classes/counts, order records, and pristine stdout bytes. No reruns or new probes were needed for that review. All 38 assertion-preservation reviews were already closed by disjoint reviewers. The gate composition review and 12 tool-only invalid-manifest controls passed. There is no known acceptance blocker within the fixed scope.

## Implementation and observation limits

The selected Module unit contains five production files, 3,272 lines and 149 functions/methods. Its function inventory distinguishes source implementation from runtime witnesses. Required Compiler/AST/host/binder/formatter changes do not claim completion of those surrounding subsystems. Standalone automatic type-name discovery and explicit type resolution are implemented; complete Program/global/default-library environments, library-reference scheduling, reference-project execution, content-mapper execution, emit/watch/services remain external to the graph entry.

Public records retain complete actual result fields, diagnostic messages/chains/owners/spans, ordered search/trace sequences, graph source/target relationships and selected API identity invariants. Unknown reached structures fail explicitly. Private lazy-cache/storage topology is not exhaustively observed or an additional completion requirement. Nonempty type ResolutionDiagnostics publication remains implemented and source-reviewed but unexercised; actual nil/missing-type results, 2688 diagnostics and the complete forwarding slot are exercised. No exhaustive unreachable claim is made.

The Resolution-only virtual host reproduces the reached original vfstest stored-case/strict-UTF8/nil-list roles without changing accepted Configuration support. Arbitrary-byte native disk paths, writable VFS equivalence and Windows/macOS execution are unclaimed. The three casing constructors use source-supported root ordering to load canonical spelling before an alias joins an unordered Go task map. Earlier divergent complete outputs remain preserved; the same casing/package-format assertions remain. Ordered search/traces are never normalized. Only declared association tables use stable owner/span ordering.

The shipped mixed-form package graph and blocked variant are graph-only integration witnesses. Their JSX package is an authored resolution fixture, not React semantic coverage. The separately admitted TS package examples use the genuine pinned three-library checking environment. Resolution capability and checker syntax/type capability remain separate.

## Preserved failures and repairs

Early immutable snapshot3 AOT established actual-field reflection and gated race execution but exposed the subsequently repaired nil package-cache receiver. Snapshot7 established the final missing-path edge owner/span and self-reference repairs. These earlier results remain historical risk/investigation evidence rather than final acceptance.

The first full attempt stopped on the public checking-project Unicode filename adaptation. Support restored that public role on the private checking graph while preserving raw graph/parser identities, metadata, and source byte consumers. The obsolete outside-root checking rejection was explicitly replaced with successful full-resolution checking under R5; the named legacy policy remains bounded. The second attempt exposed default parse-options null filename conversion. Support normalized only the byte adapter to Go's empty-string zero; the existing authored filename-role group now asserts default/empty scalar/raw binding to a quoted-empty module symbol with its real export. All four retained managed suites passed cheaply before the successful third full gate. Original38 obligations and historical expected bytes were unchanged.

## Reproduction and delivery review

Run `python3 tools/run-resolution-gate.py` using the committed [manifest](../../validation/resolution-corpus/manifest.json). Only after a successful source-matched gate, `python3 tools/record-resolution-evidence.py` records a new proof and refuses overwriting an existing proof. Original expectations are replayed unchanged, never regenerated by the gate or target.

Read-only delivery-document review corrected the [guide](../RESOLUTION.md) and [assessment](../RESOLUTION-ASSESSMENT.md)'s CLI provenance split, replay wording and three casing-witness attribution. This focused review found no other substantive scope/CLI discrepancy. Independent source/assertion reviews and runtime statuses are recorded separately in the [test inventory](../resolution-tests.json) and existing review notes.
