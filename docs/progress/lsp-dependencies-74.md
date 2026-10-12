# Integrated semantic-service owners and original dependency assertions

The source-reviewed service implementation is integrated and the shared dependency
checks pass under managed execution and actual Linux x64 Native AOT. The
[whole epic](../LANGUAGE-SERVICES-CONTRACT.md) is still in progress: the actual server,
original editor corpus and final integration gates remain ahead.

The new source counterparts contain the complete completion/string-completion/JSDoc
family, code-action/import-fix/organize-import/file-rename family, semantic tokens,
inlay hints, call hierarchy, code lenses, document symbols, automatic insertion,
folding, selection ranges and linked editing. Their required checker, byte-string,
printer and module-specifier joins use genuine existing owners. Direct mappings and
necessary C# adaptations are recorded in [source-map.json](../../source-map.json).
Source presence and worker counts establish traceability, not service-result parity.

Seven more whole upstream assertion functions now pass: all five Registry originals
(including their twenty subtests), the unchanged deep-selection original and the
case-folded completion-prefix original. Two authored controls additionally compare
complete hierarchical/flat document-symbol responses with actual pinned-Go Session
results. Separate external Go string-trimming and slice-predicate originals retain
all27 and15 comparisons respectively; they are not TypeScript test functions.

The [execution record](../../validation/results/language-services-dependencies-74.json)
binds **420/420 managed cases**, **323 whole TypeScript original functions** under
Native AOT, one separately adapted original runtime observation and62 authored
controls. The eight retained decoder-depth probes are included in420. External and
partial channels retain their qualifications. The retained parser suite passes all
18,799 data-driven cases. Native source binding covers79 changed compiled paths and
179 actual compiler inputs, with no drift. The isolated fatal IPC child terminates
with its required marker and no timeout.

The original12,000-parenthesis selection input exposed a real CLR parser stack
limit. A synchronous low-stack continuation runs the same parser with exclusive
ownership on a fresh stack, preserving context, errors and pool lifetime. The exact
original depth,1,000-node selection chain, ranges and serialization pass in both
runtimes. This uses an OS-thread chain, unlike Go's growable stack; arbitrary depth
and resource equivalence are not claimed. The retained parser suite protects the
ordinary path after this concrete foundation adaptation.

Two fixture defects were also corrected without changing expected behavior. Registry
removal now retains empty ancestor directories as its original virtual filesystem
does. Untimed original IPC receives no longer inherit an extra one-second deadline;
actual upstream timed observations remain intact, with a30-second harness guard for
otherwise unbounded waits. The failed intermediate67 run remains recorded. Review
also withdrew two unproven defect attributions: document-symbol private-header alias
behavior and supposed UTF8 decoding by Materialize. No speculative alias framework
or unrelated Unicode fix was retained.

The [test inventory](../language-services-upstream-tests.json) accounts for5,006
original entries:323 whole passes,1adapted pass,4partial entries,4,651 unexecuted,
26Darwin-backend exclusions and1external generator. Six package lifecycle functions
are separate. Most pending entries are the4,571 original editor tests. The candidate
backend preserves their original assertions, baselines and fixture mutations; it
must execute them through the real server before they receive credit.

Independent source review covers each promoted family. The
[checkpoint evidence review](./lsp-dependencies-74-review.json) assesses these claims
separately. [Reproduction](../LANGUAGE-SERVICES-VERIFY.md) includes the shared runner,
original Go witnesses and isolated controls. Native dependency publication may trim
uncalled service methods: it does not replace actual Native server request execution.
Comprehensive compiler/emit regressions and repeated-edit responsiveness/memory remain
final epic gates; no external project was rerun.

Nineteen central builds56–74 reported198.34 seconds combined; this measures build-tool
time, not elapsed implementation effort. Repeated API and small wrapper errors were
avoidable overhead. Whole-family integration, disjoint correction ownership and
focused failure checks avoided repeating the full gate per correction. Independent
review remains focused on algorithms, lifetime and omissions rather than duplicating
compiler binding checks. No token or monetary measurements are available.
