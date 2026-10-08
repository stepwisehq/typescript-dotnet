# Independent support review

- Reviewed JsNum String/PseudoBigInt against upstream `tsc/internal/jsnum/{string,pseudobigint}.go`: radix validation, signed-decimal treatment, overflow round-to-nearest-even, leading/trailing-zero cleanup, exponent syntax, safe-integer formatting, ECMAScript fixed/scientific thresholds. No blocking logic discrepancy identified in scanner-used slice. .NET `R` shortest formatting remains an adaptation relying on runtime behavior and deserves differential random-number coverage.
- Reviewed UTF8 forward/reverse decoder: invalid leading/continuation bytes, overlongs, surrogate rejection, >U+10FFFF, zero-length and one-byte malformed recovery align with Go. CESU8 is separate JS string helper; scanner public value getter now preserves isolated UTF16 surrogates.
- Reviewed Unicode identifier range binary search and generated table naming/stride: algorithm aligns with Go RangeTable. No platform Unicode classifier substituted for identifier tables.
- Reviewed regex flags, character/class escape machinery, pending low-surrogate path, named-group tracking, and Unicode property lookup against upstream regexp.go. Main differential corpus currently supports compatibility; no additional logic discrepancy found.
- Core spelling suggestions initially used platform casing; expanded oracle exposed non-ASCII short-name misses. Support added pinned Go Unicode case tables and fold semantics; verifier reports all short/long-name cases now pass.
- Support identified decoder/ASCII suffix copying causing quadratic allocation. Scanner now uses ReadOnlySpan source slices at UTF8 decoders, ASCII scanning and suffix prefix/index checks; support added reverse UTF8/JSString decoder span overloads and updated regex too.
- Blocking issue found independently by support and verification: conflict-marker range-loop body dropped by temporary emitter. Scanner fixed explicit seven-byte validation; verification will re-run full corpus.

## Final pinned casing delta

- Reviewed UnicodeCase.cs against Go 1.27.1 unicode/letter.go: binary-search range lookup, alternating UpperLower ranges, out-of-range rune identity, special-case fold orbit lookup, upper/lower fallback, and EqualFold orbit traversal match upstream semantics. Omitting ASCII fast paths affects speed only because equivalent entries remain in CaseRanges/caseOrbit.
- Reviewed generator: verifies the exact tables.go SHA-256 before extraction; enforces 336 case ranges and 94 fold orbit entries; keeps upper/lower deltas and explicit UpperLower sentinel. Output records Go version and Unicode 17.0.0. BSD notices retained and licenses/Go-LICENSE.txt contains full license.
- Core now uses the pinned ToLower and EqualFold adapters, resolving the platform-casing adaptation flagged above. Support reports exhaustive scalar ToLower/SimpleFold hash equality with Go; verifier reports all 39 added short/long-name regressions pass. No remaining blocking finding in this delta.
