# Foundation verification

The frozen campaign passed against upstream
`fed0bf24149fb1ed36039212648bafdafc1ea10e`, from checkpoint `b6b22b9`.
The [durable proof](../validation/results/foundation-verified.json) records exact
source, asset and native binary fingerprints. The main gate took 231.230 seconds;
source and verification assets did not drift. Root's completion assessment remains
separate from this execution report.

| Verification | Passing evidence |
| --- | --- |
| All xUnit layers | 22,930 tests, zero failures or skips |
| Shared native assertions | 3,153 cases, exactly matching xUnit registrations |
| Original finite parser fuzz bodies | 17,633 in both xUnit and native, zero skips |
| Fresh Go/native broad syntax graphs | 17,723 exact; 2,647,487 nodes |
| Fresh Go/native ordinary scanner streams | 17,723 exact; 3,894,564 tokens |
| Additional targeted native graphs | 8 exact; 154 nodes |
| Retained native regressions | scanner 1,276; parser 471; binder 118; scalar 45; objects 43; named data types 92; API/boundary controls |

The xUnit totals are scanner 3,837, parser/AST 18,709, binder 122 and checker 262.
Native shared cases comprise scanner 2,552; parser/AST/default-English diagnostics
563; syntax services 14; full clone graphs 21; diagnostic witnesses three. Scanner
shared cases separate 2,532 original assertion cases from 20 authored grouped
checks. Parser original syntax assertions comprise 561 cases (20 function groups
and 541 clone-table rows); default-English diagnostic assertions add two groups.
Table rows, function groups, seeds and source requests are different counts.

All five adapters were published and executed as Linux x64 Native AOT. The shared
foundation executable is a 15,960,016-byte ELF depending on libc/libm, with SHA256
`c7a8b3588ccf5fe4685d1892713e587a20eee474d5d9cc049268cff02ee1c0fe`.
Five binary fingerprints were captured after the historical regression cascade
and remained stable through the subsequent shared/targeted/broad foundation
phase. They are not pre-first-execution fingerprints for that entire cascade;
the checker has its own earlier pre-comparison fingerprint. The source SHA256
is `d14cb48750c8ce35f0767f97885b01f9b0d63c853390f6cfb4f9b3f4cb50fd67`;
asset SHA256 is `5aea5174cca50b6fb7d3b62af93c5056781dedeec5dfd79b11554dbedfe3e526`.

## Original assertion provenance

The [catalog](foundation-test-catalog.json) inventories 144 distinct original
Go test/fuzz/benchmark functions: 39 assertion-preserving ports, one explicit
adapted default-English localization test, 102 excluded functions and two
later-stage-blocked compiler/transpile runners. There are no unresolved
function dispositions. Equivalent byte-array comparisons preserve original Go
byte-string assertions; changing assertion or case scope is recorded as an
adaptation. Benchmarks and full path/collection/project APIs are not correctness
ports of the selected foundation surface.

The unchanged pinned Go scanner, parser and AST test packages ran, along with
selected original diagnostics, numeric formatting/parsing/bigint, required path
and source-unit splitter tests. Original assertions were ported first into
[Foundation.Checks](../validation/Foundation.Checks/). Thin xUnit wrappers and
[Foundation.Validation](../validation/Foundation.Validation/) execute those exact
bodies. Node supplies the original JavaScript numeric references; an absent Node
executable blocks execution rather than counting a skipped reference as success.
The [initial 3,001-case direct milestone](../validation/results/foundation-direct-verified.json)
remains unchanged, distinct from the final campaign proof.

The original Go numeric fuzz commands in the main frozen shell are `1x` smoke
runs, not full baseline replay. A separate
[bounded original Go baseline proof](../validation/results/foundation-go-numeric-fuzz-verified.json)
records full command arguments, original pin, runtime/binary hashes, raw log and
log hash: `FuzzStringJS` completed 156/156 deduplicated arguments with
`-fuzztime=156x`; `FuzzFromStringJS` completed 236/236 with `-fuzztime=236x`.
Both commands use `-run ^$ -parallel=1` and the original jsnum package. Together
they took 9.239 seconds and generated no new interesting inputs. The shared C#
assertions independently retain all 248 original seed rows per function, including
repeated arguments; no unbounded mutation campaign is claimed.

## Broad input and observation contract

The [input manifest](../validation/foundation-corpus/source-manifest.json) covers
6,891 compiler, 5,935 conformance and 25 transpile physical files, plus 166 raw
fixture files. The actual pinned splitter returned 17,697 units: 17,672 parsed
units and 25 explicitly excluded non-source/custom-host assets. Config variants
and ten committed regressions produce 17,723 unique comparison requests. There
were no extraction failures. Final request modes are TS 15,084, JS 1,480, TSX 486,
JSX 16 and JSON 657. Unknown/reserved/invalid enum behavior has separate controls,
and is not counted as another supported mode.

Four ancillary inputs have explicit authored mode adaptations using original
bytes/names: one serialized build-info file is checked as JSON syntax; three
`.js.map` units containing JavaScript source are checked in JS mode. These are
not filename-inferred modes or compiler/source-map assertions. Remaining assets
retain exact text hashes and concrete exclusion reasons. Original finite
`FuzzParser` membership is unchanged: fixture, compiler/conformance splitter and
committed-regression inputs only; transpile and authored option/mode variants are
excluded from the 17,633 original no-panic bodies.

Canonical syntax comparison retains every ordered syntax field, raw scalar,
span/flag, nullable list, list identity, parent reference, child order, JSDoc and
reparsed identity, source metadata and diagnostic channel. Original Go discovery
is independent of the generated C# syntax schema. Opt-in foundation observations
add full subtree facts and actual cache bits, first/repeated identity traversal,
PositionMap mappings/cache identity, and nullable diagnostic arguments/chains/
related information/flags. Initial facts may already be cached by parsing; the
observer compares actual before/after state rather than assuming a cold cache.
The ordinary scanner independently compares token kinds, raw values/text, flags,
byte ranges and diagnostics with trivia included. Parser-directed rescans remain
covered by the separate curated scanner contract.

Comparisons stream exact responses, preserving JSON value types and every ordered
array. Successes retain canonical digests; failures retain full requests/Go/C#
outputs and the first differing path. Crashes, extraction failures, resource limits,
extra response rows and incomplete/limited counts cannot become passing evidence.
Full native broad comparisons took 106.919 seconds for syntax and 21.177 seconds
for scanning. Syntax roots contain 3,839 parse, 178 JS and 101 JSDoc diagnostics;
116 distinct codes including related/chain records were observed. Ordinary scan
contains 2,384 diagnostics and 21 distinct codes. Curated historical coverage and
three new full-graph diagnostic witnesses supplement these broad populations.

## Defects, limits and reproduction

The initial 17,719-request managed syntax run found 26 mismatches: 21 declaration
filename flags and five reparsed-clone tie orders. Full failed payloads and that
report remain preserved. The pinned parser itself sorts reparsed clones, so its
required Go PDQsort dependency was ported; ordered comparison was retained. All
26 focused repair replays then matched. Original numeric assertions also exposed
round-trip formatting and bigint separator defects. Focused probes exposed
unbound declaration-map grouping and captured callback-slice length issues;
repairs preserved original Go observations and expected values.

[Diagnostic dispositions](../validation/foundation-corpus/coverage-dispositions.json)
record actual witnesses for 1129, related-information 1486 and 18016. Parser codes
1140, 1179 and 1439 and regexp code 1513 have individually documented
source-unreachable paths, independently challenged in review; they are not
observed-coverage claims. Managed callback prefix-copy preserves captured Go
slice contents/order under append; arbitrary backing-array alias/capacity
mutation is not claimed. Full locale files/generation, content-mapper
interpretation, incremental diagnostic repopulation, project/module resolution,
compiler/emit baselines and the unbounded fuzz engine remain excluded. Broader
compiler inputs are input reuse, not original full compiler passes. Historical
binder/checker limits remain in their separate assessment/proof records.

Reproduce the frozen main gate with
[run-foundation-gate.py](../tools/run-foundation-gate.py), which invokes
[verify-foundation.sh](../tools/verify-foundation.sh); then use
[record-foundation-evidence.py](../tools/record-foundation-evidence.py) only on
success. The supplementary numeric proof records the two complete original Go
finite commands separately. The main gate checks actual registered/executed
xUnit/native counts, all source/project files (including shared assertions and
native harness), all tool/corpus/lock/runtime bytes, and native binary stability.
Deterministic gzip assets retain exact canonical request bytes; expanded cache
files cannot replace them. Historical fixtures/proofs remain unchanged.

Independent [verification omission review](progress/review-foundation-verification.md),
[syntax support review](progress/review-foundation-syntax-support.md) and owner
source inventories accompany the proof. The
[recovery checkpoint](progress/foundation-verification.md) records the final action
and handoff. No additional broad managed pass was run after the focused repairs.
