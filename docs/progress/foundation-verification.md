# Foundation verification checkpoint

Status: final guarded gate passed; no remaining verification blocker. Original
shared direct milestone passed on actual Native AOT. Final source/tool/corpus
freeze follows the Release11 owner snapshot and verification whitespace cleanup.
Current source SHA: `d14cb48750c8ce35f0767f97885b01f9b0d63c853390f6cfb4f9b3f4cb50fd67`.

Completed focused closure: 26/26 broad repair replays, five latest syntax service
groups (append callbacks and 32 original-Go sort patterns), three diagnostic
witness graphs and four ancillary-input adaptations all match. Broad final corpus
is 17,723 requests; 17,633 original finite fuzz seeds remain unchanged. Shared
native default assertion count is 3,153 (scanner 2,552; parser/AST 563; syntax
services 14; clone graphs 21; diagnostic witnesses 3). Three parser diagnostic
codes and one regexp code have independently reviewed source-unreachable
dispositions, with no sampled-coverage claim. Original 17,719-request managed
reports and all 26 mismatch payloads remain retained as defect-discovery evidence.

Executed: `python3 tools/run-foundation-gate.py`, then
`python3 tools/record-foundation-evidence.py`. This runs original Go tests, all
xUnit layers, five actual Native AOT applications, shared direct/fuzz assertions
and full fresh-Go parser/scanner comparisons. Drift, incomplete counts, any
upstream/port failure, or a changed native executable blocks durable acceptance.
Final elapsed time: 231.230 seconds. Source and assets did not drift.
No second broad managed pass was run.
Historical proof JSON and fixture bytes remain unchanged. Baseline `b6b22b9`; upstream remains
`fed0bf24149fb1ed36039212648bafdafc1ea10e`.

Verification owns the unified test disposition catalog, broader compiler-input
inventory, upstream runner/assertion semantics, comparison omissions, and eventual
foundation oracle/corpus/native gate. Scanner owns direct scanner/regexp and
required core/string/number/position tests/source-function inventory. Support owns
direct parser/JSDoc/syntax-AST tests/source functions. Root owns the completion
contract and dependency decisions. No extra agents.

Catalog rows are actual upstream test functions, with original table/subtest counts
reported separately and an explicit counting basis. Dispositions distinguish
assertion-preserving ports, adapted assertions, input-only reuse, added differential
tests, exclusions, later-stage blockers and unresolved entries. Original input reuse
does not establish preservation of original assertions. Only git-tracked upstream
tests count as original; authored untracked oracle tests are excluded from that total.
Owner inventories: `docs/foundation-scanner-tests.json` and
`docs/foundation-parser-ast-tests.json`; verification will aggregate after handoff.

Initial findings: compiler/conformance suites contain 6891/5935 tracked case files,
with 25 transpile cases separately (12,851 physical inputs, 8,708,184 bytes).
The tracked path/size/SHA/disposition manifest is
[`source-inputs.jsonl`](../../validation/foundation-corpus/source-inputs.jsonl),
generated with [`foundation-test-catalog.py`](../../tools/foundation-test-catalog.py).
The unified [`test catalog`](../foundation-test-catalog.json) aggregates both owner
inventories, framework assertions and explicit shared-package exclusions. Counts
of physical inputs are not original table/configuration counts or extracted units.
Full compiler runners assert all-program
diagnostics, emit/declaration output, source maps, type/symbol baselines, module
resolution, union ordering and parent pointers. Parser-only input reuse preserves
none of the later-stage baselines; the parent-pointer assertion can be adapted
directly as a foundation invariant. Existing parser source extraction already calls
the actual Go multi-file/directive splitter, avoiding fake parsing of harness metadata.

Historical parser coverage JSON describes460 accepted original parser cases and29
unhit referenced diagnostic codes. Eleven later binder-readiness inputs closed14
codes; actual accepted current parser regression has471 cases, so treating all29
as current gaps would be stale. Scanner coverage has1276 cases/69of70 referenced
diagnostics; code1513 remains unobserved. All parser factory-produced payloads were
observed, while transform/flow/synthetic-only payloads were explicitly excluded.
Mode counts and full position-map/clone/traversal omissions need current-contract
assessment, not another unchanged baseline test run.

Shared assertions: owners write `ScannerChecks` and `ParserAstChecks` under
[`Foundation.Checks`](../../validation/Foundation.Checks/), with public `Cases`
and `Run(name)`; xUnit and the native
[`Foundation.Validation`](../../validation/Foundation.Validation/) reference
those exact bodies. Node reference assertions require Node explicitly rather than
counting a skipped reference as a pass. Support owns shared C# library builds.

Pinned ScriptKind declares JS1/JSX2/TS3/TSX4/JSON6; values 5 and 7 are reserved.
Unknown0 failure and invalid-value controls need low-level direct coverage because
the historical request adapter defaults unspecified mode to TS. Filename directives
include non-code assets; broad extraction must record those dispositions explicitly.

First direct gate: `bash tools/verify-foundation-direct.sh` passed in 22.313 seconds.
xUnit scanner 2438/2438 (includes two legacy forwarding names), parser/AST/default
English diagnostics 563/563; native executes the same 3001 bodies. Original pinned
Go `TestMakeUnitsFromTest` passes as splitter infrastructure evidence. No source
drift. Source SHA `32b8147face48cf804c0fcd014f1ea374bd6cd3799c0ab46e8f169b5c3d53093`;
native SHA `48bc5958df41cbad3f37645a542dc7045186a85ffd33301efcb744aba3402678`,
4,346,536-byte ELF, libc/libm only. [Durable direct milestone](../../validation/results/foundation-direct-verified.json).
Observed formatting and bigint separator defects were repaired without changing
expected fixtures. Writers resumed after the successful paused-source milestone.

Opt-in observation milestone: central Release5 plus adapter1 passed 8/8 targeted
requests (154 nodes, two diagnostics). New [Go observer](../../tools/parser-oracle/foundation.go)
and [static C# observer](../../validation/Parser.Validation/Foundation.cs) compare
all initial facts slots, first/repeated values and actual cache bits; identity
visitor child order/result identity; list identities; PositionMap mappings/reuse;
diagnostic flags and nullable argument/chain/channel state. Source scope includes
raw malformed UTF8, JSON, JS JSDoc, prepopulated JSX facts, type-erasure scopes and
top-level await. Requests/expected are stored under `validation/foundation-corpus`;
result is `artifacts/foundation-validation/targeted-managed-result.json`.

The [foundation source extractor](../../tools/parser-oracle/foundation_sources.go)
uses the original splitter, records original options/directory/symlinks and explicit
errors, and keeps all JSON source units, including package/config names. Fixtures
retain raw bytes and original fuzz extension membership. No broad run yet.
Binder/checker oracle preparation now copies the optional observer helper, preserving
historical outputs when the request flag is false; final regression builds verify
this integration. Support continues sole shared-library build ownership.

Next action: implement shared finite original `FuzzParser` assertions in owned
`ParserFuzzChecks.cs`/`FoundationFuzzTests.cs`, including 166 raw fixture sources,
compiler/conformance splitter seeds and ten committed regressions. Add opt-in
foundation graph/facts/cache/list/position observations and streamed comparisons;
do not modify historical expected files. Full final gate must run actual original
Go scanner/parser/AST/default diagnostic and required numeric tests, plus all native
foundation and retained semantic regressions. Only one broad managed triage run
before the frozen native gate unless a concrete defect justifies another.

Extraction milestone: all13,017physical inputs extracted by original splitter/raw
fixture path without failures.17,697units,29excluded assets,17,719 deduplicated
option requests across allfive declared modes; ordinaryJSON configs/packages
included. Canonical corpus+unitprovenance use deterministic gzip, expandedJSONL
is ignored and reproducible. [Manifest](../../validation/foundation-corpus/source-manifest.json).

Original finite `FuzzParser` body now shared in
[ParserFuzzChecks](../../validation/Foundation.Checks/ParserFuzzChecks.cs), xUnit and
native `--fuzz`:17,633/17,633 managed seed bodies passed (Release8,0skips). Excludes
transpile, duplicate option variants and unrecognized extensions; includes166raw
fixtures and10committed regressions. Native finitebody pending. Six actual-Go
syntaxservice groups passed5/6: unbound overload map retained extra C#overload;
support found method-group comparison instead of Symbol() call and repairedsource.
Names/tokencache/Decorators/DataKey/IterChildren mutation groups match. Added
factoryhooks/listclone/visitorlifting/accessor probes and selected fullsyntax
clone graphs (mixed TS, JSX, lazyJSDoc, await; deep/reparse/shallow) next. Shared
check sources now reference static Parser.Validation encoder, no reflection.
No broadmanaged run yet. Next: central9 snapshot, targeted service/clone checks,
then streamed wholecorpus comparisons with digests/fullmismatchretention.

Broad managed baseline completed against isolated Release9:17,693/17,719exact in
107.799s,2,647,355nodes,3,839parse+178JS+101JSDoc diagnostics.26mismatches:
21arbitrary-extension declaration-file flags and5ReparsedClones sorted tie order;
root confirmed actual pinned parser ordering, owner will port smallest required
Go sort dependency rather than weaken ordered comparisons. Broad ordinary scanner
passed17,719/17,719 in21.603s,3,894,205tokens,2,384diagnostics. Initial reports/
exact failed payloads retained under `artifacts/foundation-validation`.
Broadclosed9of15remaining diagnostic gaps;1129,1140,1179,1439,1486,18016 remain
for source callsite disposition/witnesses (support owns). Current targeted9syntax
service groups and21full clone graphs allpass; finite original seeds17633passed.

Final orchestration drafts now [verify-foundation.sh](../../tools/verify-foundation.sh)
and [run-foundation-gate.py](../../tools/run-foundation-gate.py): originalsGo tests,
allhistorical xUnit/native viaonecascade, shared direct/fuzz NativeAOT, full native
scanner/parser streams, full requestcounts andsource/assetdrift guards (gzip included).
No finalgate yet; wait writerfreeze/reviews and narrow repairchecks. Recorder pending.


Final handoff: [foundation-verified.json](../../validation/results/foundation-verified.json)
records source SHA `d14cb48750c8ce35f0767f97885b01f9b0d63c853390f6cfb4f9b3f4cb50fd67`,
asset SHA `5aea5174cca50b6fb7d3b62af93c5056781dedeec5dfd79b11554dbedfe3e526`,
foundation native SHA `c7a8b3588ccf5fe4685d1892713e587a20eee474d5d9cc049268cff02ee1c0fe`
(15,960,016-byte Linux x64 ELF, libc/libm). All 22,930 xUnit tests, 3,153 shared
native assertion cases, 17,633 original finite parser fuzz bodies, eight targeted
native graphs, and both complete 17,723-request native streams passed.
Broad native parser: 2,647,487 nodes, 106.919s; scanner: 3,894,564 tokens, 21.177s.

The main shell's original Go numeric `1x` commands are smoke checks. Separate full
finite Go baselines passed 156/156 and 236/236 deduplicated inputs in 9.239s;
[durable supplement](../../validation/results/foundation-go-numeric-fuzz-verified.json)
stores command arguments, pin, runtime/binary fingerprints and complete log.
It is referenced from the final proof; shared C#/native preserve all 248 seed rows
per original numeric fuzz function. No mutation fuzz campaign is claimed.

All executable sources, tools and corpus assets remain frozen. Verification docs
and catalog execution statuses may update without altering guarded evidence.
Exact next action: root's final assessment/state/source-map review and commit.
No further builds/tests or source/asset edits are needed absent a concrete defect.
