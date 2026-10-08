# Foundation contract acceptance

**F1–F6 satisfied on Linux x64.** See the [assessment](./FOUNDATION-ASSESSMENT.md),
[fixed contract](./FOUNDATION-CONTRACT.md),
[durable native evidence](../validation/results/foundation-verified.json) and
[complete Go numeric baseline replay](../validation/results/foundation-go-numeric-fuzz-verified.json).
Committed delivery closes this campaign; no next campaign is authorized.

| Contract | Acceptance evidence |
| --- | --- |
| F1 scanner/regex/helpers | All 158 scanner functions dispositioned; exact pinned tables, original assertions and 16,705 additional Go/Node numeric samples; full native scans exact |
| F2 parser/modes/recovery | All six parser files accounted for; five declared modes; 17,633 original finite seeds and 17,723 full native graphs pass |
| F3 syntax AST | 2,752 source function rows dispositioned; full facts/lazy iterators/helpers; original clone assertions plus rich clone/factory/update/visitor/cache observations; independent body reviews closed |
| F4 positions/diagnostics | PositionMap and source services; 2,222 exact English records; diagnostic/identity/coordinate probes pass natively; historical gaps observed or individually source-dispositioned |
| F5 upstream tests and input provenance | 144 inventoried functions: 39 preserved, one adapted, 102 excluded, two later-stage blocked, none unresolved; 13,017 physical inputs, 25 excluded units and four explicit ancillary mode adaptations |
| F6 native/regression/review | 22,930 xUnit cases, 3,153 shared native bodies, finite seeds, full native streams and all retained semantic/stage regressions pass; no source/asset drift; independent reviews complete |

The xUnit total is data-driven: **17,633 no-panic seed cases plus 5,297 other cases**.
It is not a count of handwritten upstream tests. Native shared bodies overlap xUnit;
full graph/scan streams are separate runs. Test totals alone are not completeness.

Four defensive codes remain unobserved: scanner 1513; parser 1140, 1179, 1439.
Their independently reviewed public-unreachability/non-publication arguments are
recorded, not counted as observed coverage. The 17,723 broad parser requests compare
2,647,487 nodes and 4,118 root diagnostics; ordinary scans compare 3,894,564 tokens
and 2,384 diagnostics. No final discrepancy or upstream crash remains in that corpus.

The first managed broad run identified 26 differences: 21 declaration-filename flags
and five equal-key reparsed-clone orders. All are repaired without changing expected
behavior. Source review also repaired callbacks, accessors, token bounds, declaration
maps and numeric conversion. Harness mistakes and their corrections are recorded
separately in the assessment and owner notes.

The final frozen gate took 231.230 seconds. Source SHA:
`d14cb48750c8ce35f0767f97885b01f9b0d63c853390f6cfb4f9b3f4cb50fd67`.
Asset SHA: `5aea5174cca50b6fb7d3b62af93c5056781dedeec5dfd79b11554dbedfe3e526`.
Current state and next-scope limits are in [STATE](./STATE.md).
