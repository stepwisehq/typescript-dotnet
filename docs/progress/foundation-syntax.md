# Foundation syntax owner checkpoint

Baseline `b6b22b9`; TypeScript pin `fed0bf24149fb1ed36039212648bafdafc1ea10e`.
[Contract](../FOUNDATION-CONTRACT.md). Source/tool/assertion/fixture snapshot frozen; final foundation gate passed. No active work.
Support owns Parser/* and Ast/* syntax support except Utilities.cs/Precedence.cs (scanner owner).
Support is the single shared library build owner. Verifier owns independent oracle, adapters and broad/native gates.

The user's original-test-first order was retained. The [historical direct gate](../../validation/results/foundation-direct-verified.json)
passed 2,438 scanner and 563 parser xUnit bodies, and the same 3,001 Native AOT bodies.
Parser checks preserve 20 original function cases plus all 541 deep-clone rows;
all rows parse TypeScript(false), including JSX-labeled inputs. Default-English diagnostic tests
add two groups; the localization rows are explicitly an English subset, not full translated parity.
[Direct catalog](../foundation-parser-ast-tests.json) distinguishes original assertions, fuzz, benchmarks and historical input reuse.

Implemented PositionMap/source map identity; full syntax SubtreeFacts (152 concrete computations,
72 cached payloads); 166 lazy IterChildren overrides; stage-neutral SourceFileDataKey/compute-once;
names/identifiers/token caches, read-only declaration map, access classification and decorators;
complete syntax factories/accessors/updates/clones/visitor paths; initial isolated entity parsing;
serialized/external/text diagnostics and all 2,222 pinned default-English message records.
No binder/checker/transform execution or content/project metadata was added by this campaign.

Authoritative source remains handwritten/generated C# with documented adaptations.
`generate-subtreefacts.go` and `generate-syntax-iterators.py` replace only marked sections.
Do not rerun `generate-ast-accessors.go` over Ast.cs: its historical truncation would remove manual supplements.
`generate-required-diagnostics.py` emits the complete English catalog. Shared assertion bodies are
in Foundation.Checks, called by xUnit/native wrappers; no reflection-only native serializer.
[Source inventory](../foundation-syntax-source-inventory.json) records 2,752 function rows,
review evidence, embedded-base/runtime adaptations and explicit later-state/content/options exclusions.

Latest central11 Release build of Foundation.Checks/library/Parser.Validation passed with
zero warnings/errors in 2.28 seconds. Against Release9: nine syntax-service groups and 21 full
clone graphs matched original Go; all 17,633 finite original FuzzParser seeds passed managed and Native AOT.
Broad 17,719 comparisons exposed exactly 21 declaration filename flag differences and five
ordered ReparsedClones tie differences. Current source restores arbitrary `.d.*.ts` recognition
and the required pinned Go SortFunc PDQ closure (326 C# lines, BSD license, two direct counterparts).
All 26 focused repair replays passed exactly against Release11; the final native gate also passed.

Concrete reviewed fixes: Symbol method-group comparisons, function-expression declaration-map arm,
required parameter accessor/Node.Text kind behavior, and initial-count capture in SameMapNodes,
VisitNodes and VisitSlice. Managed prefix copies preserve callback-captured length; arbitrary Go
backing-array capacity aliasing is not claimed. Scanner independently reviewed source caches,
PositionMap, facts, factories, English metadata and all 13 required PDQ helper bodies.
[Utility review](review-foundation-utilities.md) and [scanner review](review-foundation-scanner.md) have no unresolved source blocker.

Scanner's central11 focused 114 checks passed (94 original path rows plus 20 helper/numeric groups).
The six historical parser code dispositions are in [the source-proof record](../foundation-parser-unreachable-diagnostics.md):
actual witnesses for 1129/1486-related/18016; specific source arguments for 1140/1179/1439,
with scanner's independent challenge closed without a counterexample. Diagnostic 1513 public-unreachability was independently challenged.
Final focused result: five additional service groups passed (including 32 actual-Go sort identity patterns,
append callbacks, Unicode diagnostic text boundary, modes and isolated entity names), three diagnostic
witness graphs and four asset-adapted graphs matched exactly. Source and tools/assertions/fixtures are
frozen; `git diff --check` was clean before freeze. No source blocker, code changes or library builds remain.
Final [frozen gate](../../validation/results/foundation-verified.json) passed in 231.230 seconds, without source or asset drift.
All 22,930 xUnit tests passed; 3,153 shared native cases and 17,633 finite original fuzz bodies passed.
Both native streams matched 17,723 requests exactly (parser: 2,647,487 nodes; scanner: 3,894,564 tokens).
The parser shared categories are 563 original/adapted groups, 14 syntax-service groups, 21 clone graphs
and three diagnostic witness graphs. Earlier bounded semantic/binder regressions also passed.
No source/tool/test/fixture edits or builds remain; final catalog/mapping consolidation is documentation only.
