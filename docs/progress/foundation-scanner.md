# Foundation scanner checkpoint

Completed bounded foundation campaign at TypeScript pin
`fed0bf24149fb1ed36039212648bafdafc1ea10e`; baseline `b6b22b9`.
[Contract](../FOUNDATION-CONTRACT.md), [final proof](../../validation/results/foundation-verified.json).
No active source/tool/test work or unresolved blocker.

- Final gate exit0 in231.23s, no source/asset drift. Source SHA256
  `d14cb48750c8ce35f0767f97885b01f9b0d63c853390f6cfb4f9b3f4cb50fd67`.
- Scanner3837/3837 xUnit; identical shared native scanner bodies2552/2552:
  **2532 original assertions** (2438 scanner/numeric +94 required path rows) and
  **20 authored groups**. All3153 shared foundation bodies and22930 total xUnit pass.
- Broad native scanner and parser each17723/17723 exact original-Go comparisons;
  scanner3894564 tokens, parser2647487 nodes. This is source-input reuse plus graph/
  diagnostic comparison, not original compiler/checker/emit baseline parity.
- All158 named scanner/regexp/utilities functions have counterparts/adaptations
  ([inventory](../foundation-scanner-functions.json));524 property atoms and1365
  identifier ranges match pinned data ([tables](../foundation-scanner-table-audit.json)).
  Coordinate quartet, byte/raw Unicode boundaries, comment factory/embedding and
  original TokenToString array bounds are retained.
- Number.String general R roundtrip verification/G17 fallback and BigInt separator
  rules repaired actual original assertion failures without changing expectations.
  All16705 focused Go/Node formats/parse-bit patterns pass in the shared native path.
  Original Go finite formatter/parser fuzz baselines156/236 deduplicated arguments
  also pass; source ports retain248 rows/function. No unbounded fuzz or exhaustive
  binary64 shortest/tie guarantee.
- Source-unreachable regexp1513 reviewed independently ([audit](foundation-scanner-audit.md));
  no observed-code claim. Unterminated public trailing backslash reports1161.
- Exclusive Utilities/Precedence handoff completed:411/422 free utility functions
  plus2 filename methods retained;11 actual later-stage bodies excluded; all6
  precedence functions ported ([body inventory](../foundation-utilities-source-inventory.json)).
  Callback captured-length defects repaired and actual Go/native probes pass.
- Required path originals preserve NormalizeSlashes4/GetRootLength48/
  IsRootedDiskPath18/PathIsRelative24 exact assertions. Go1.27.1 SortFunc dependency
  restores actual equal-span reparse order; source/license/hash review complete.
- Independent syntax-support and diagnostic-gap challenge recorded in
  [review](review-foundation-syntax-support.md). Parser1140/1179/1439 dispositions
  are accepted source proofs, not observed diagnostic coverage.

Recovery: reviewed source remains authoritative. Explicit-list scaffold plus patch,
LF formatter/comment cleanup reproduces134simple/48complex marked utility fragments
exactly. Manual models/filename wrappers/parent setter and precedence have no whole-
file regeneration claim. Helper fixture regeneration is documented in its existing
script; all executable/tool/assets remain frozen after final gate. Future work must
preserve this checkpoint and the contract's later-stage exclusions.
