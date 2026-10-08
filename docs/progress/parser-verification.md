# Parser verification checkpoint

**Complete; accepted bounded checkpoint.** No pending verification work.
See [verification](../PARSER-VERIFICATION.md), [coverage](../../validation/parser-corpus/coverage.json)
and [durable evidence](../../validation/results/parser-verified.json).

## Contract and recovery

Independent Go reflection discovers actual syntax fields and separately records
ForEachChild order; C# static typed metadata supports Native AOT. JSONL canonical
graphs retain raw scalar bytes, kind/flags/spans, payload fields, parent identity,
list null/empty/missing/trailing state and ordered parse/JS/JSDoc diagnostics.
Unknown Go field types fail loudly. Exclude only compiler-owned state; keep syntax
fields inside mixed syntax/compiler bases. Expected output comes from pinned Go.

Corpus groups cover authored grammar/malformed modes,207units from142upstream
files,10committed fuzz regressions, targeted Unicode/byte/directive/reparse cases,
and normalized text/token-position helper probes. Cold/warm JSDoc resolution
checks node identity and valid nil results. Payload coverage is not branch coverage.
Position-map API and isolated entity-name acceptance remain excluded.

Reproduce with `python3 tools/run-parser-gate.py`, then
`python3 tools/record-parser-evidence.py`. Freeze executable source and oracle/
corpus/script inputs first; before/after hashes must match. Evidence recording
rejects failures or post-gate drift. Original scanner result remains historical.
Only regenerate expected fixtures deliberately from unchanged pinned Go.

## Final result

Final milestone (2026-10-08 16:29 UTC): independent AST/parser/harness reviews complete, implementation and verification assets frozen. Ran `python3 tools/run-parser-gate.py` then `python3 tools/record-parser-evidence.py`; 24.001 seconds overall, exit0. Selected upstream Go tests and fresh Go fixture comparisons passed. Parser xUnit464/464, scanner xUnit1287/1287. Actual Native AOT parser460/460 cases matched19268nodes318diagnostics (0.291seconds including JSON/harness execution); native scanner1276/1276 matched17977tokens1547diagnostics and178utility outputs. Final corpus247kinds,186/186direct-parser-factory payloads,78diagnosticcodes and68/97direct parser-referenced codes;29unobserved codes remain explicit. Added malformed JSX/JSON mode coverage and reviewer-requested includeJSDoc token-position true/false probes (0/9).

Final source SHA256 `d441e371cf28045777b3c8041d51e524ce96904ddecf6ff7e4f105edaf2edf8d`; executable4,088,712bytes SHA256 `c0d12d543b3f3ae226dfa94ed080f8fc69183393855410c3635a9f920d762ffa`. File/ldd prove Linuxx64 ELF PIE linked only libc/libm plus loader. Verification-asset SHA256 `2209c69e98cf52a6129859c2aa504e1bd08c5613a2b4b9fb2417752da12d319d`; both source and asset drift guards passed. Log/timing under ignored artifacts/parser-validation/final-gate.{log,json}; durable result includes exact hashes, dependencies, group fixture hashes, xUnit counters, scanner rerun summary, provenance/coverage links. Historical scanner verified.json unchanged. No further executable or verification-asset mutations planned.
