# Snapshot lifetime, references and diagnostics/emit dependencies

This checkpoint passes its combined managed and Linux x64 Native AOT checks. The
[language-services contract](../LANGUAGE-SERVICES-CONTRACT.md) remains unfinished;
there is no operational public server claim.

The implementation adds the complete five upstream References-family counterparts
(183 workers), all six diagnostic-service workers, and the genuine local Session
API update/adoption path. Request context now reaches actual checker leases through
the existing Emit algorithm. Compiler token callers retain their API. No second
emitter, ambient request state or simplified service implementation was introduced.

The new original assertions cover TestSnapshot (ten groups), project-ID narrowing,
TestRefCountingCaches (eleven groups), and the reference implementation worklist's
unchanged 40/80 limits. Existing SnapshotFS assertions remain unchanged. Four new
authored comparisons use real Sessions and genuine bundled declarations: valid and
invalid sources, each with noEmitOnError off/on. Complete diagnostics before and
after Emit, complete EmitResult diagnostics, output paths/content and skip disposition
match pinned Go. These controls receive no original-test credit.

The [execution record](../../validation/results/language-services-dependencies-55.json)
binds 400/400 managed cases, 316 whole TypeScript-repository functions under Native
AOT, one separately adapted original runtime observation, and 60 authored controls.
External and partial assertion channels retain their previous qualifications. All
ten affected existing Emit mode/mutex/write-failure controls pass; Program and Emit
compatibility builds are clean. The native executable's 179 actual compiler inputs
and 43 changed compiled source paths are recorded, with no drift through execution.
The isolated fatal-notification control also terminates with its required marker.

The [complete original-test inventory](../language-services-upstream-tests.json)
now has 316 whole passes, one adapted pass, four partial channels, 4,658 unexecuted
functions, 26 excluded Darwin backend functions and one external generator test:
5,006 test functions total. Six package lifecycle entries are separate. Function
counts differ from xUnit cases and table rows.

Independent source reviews cover the
[Snapshot assertions](./lsp-snapshot-assertions-review.md),
[References closure](./lsp-references-review.md),
[request-context emit path](./lsp-emit-request-context.md), diagnostic service and
authored comparison adapter. The
[final evidence review](./lsp-dependencies-55-review.json) separately assesses this
checkpoint's claims. Reproduction is in
[LANGUAGE-SERVICES-VERIFY.md](../LANGUAGE-SERVICES-VERIFY.md).

Concrete corrections preserved the original expectations: SourceFile consumers now
read actual borrowed source text, original snapshot cleanup follows its original
lifetime, and References invokes existing printer/name-resolution helpers. Several
central build failures were enum/API binding mistakes. Seven builds49–55 reported
104.14 seconds combined, plus3.92 seconds for compatibility; this is build-tool time,
not total elapsed or model effort. No token or cost measurements are available.
The process adjustment is to compile coherent source bodies sooner, leaving manual
review to algorithm fidelity, ownership and omissions rather than duplicating the
compiler's binding checks.

Full References results are still unverified: its worklist test is not a substitute
for service/cross-project comparisons. The four diagnostics controls do not establish
all supplemental/content-mapper/locale behavior. Registry originals, remaining editor
services, the real server transport, original fourslash backend, repeated-edit resource
checks and final retained compiler/emit gates remain work. Completion, edit-producing
services and the server/document-query owners continue in parallel after this checkpoint.
