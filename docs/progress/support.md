# Scanner support checkpoint

Final coordinator disposition: **complete at the accepted scanner checkpoint**.
All final gates passed; see [VERIFICATION.md](../VERIFICATION.md) and
[ASSESSMENT.md](../ASSESSMENT.md). No active next action remains; any earlier
pending-gate notes below describe prior milestones and are superseded.

Upstream: `microsoft/TypeScript`, commit `fed0bf24149fb1ed36039212648bafdafc1ea10e`, under `tsc/internal/`.

Owned implementation:

- `src/TypeScript.Scanner/Scanner/Regexp.cs`: complete regexp parser, flags, Unicode sets, captures, quantifiers, property escapes and spelling suggestions.
- `src/TypeScript.Scanner/Scanner/UnicodeProperties.cs`: complete upstream property data.
- `Ast/KindGenerated.cs`: token-only kinds and token boundary aliases, including latest `SourceKeyword = 167`.
- `Ast/TokenFlags.cs`: complete token flags.
- `Ast/Ast.cs`: comment directive kind/record only.
- `Core/OptionsGenerated.cs`: ScriptTarget enum only.
- `Core/LanguageVariant.cs`: complete enum.
- `Core/Text.cs`: TextRange only.
- `Core/Core.cs`: scanner spelling suggestion and weighted Levenshtein slice, plus generic conditional helper.
- `Diagnostics/DiagnosticsGenerated.cs`: scanner-used diagnostics only.
- `Diagnostics/Diagnostics.cs`: diagnostic metadata and English argument formatting only.
- `StringUtil/IdentifierPartsGenerated.cs`: complete generated Unicode 15.1 identifier ranges.
- `StringUtil/Identifier.cs`: range membership for identifiers.
- `StringUtil/Util.cs`: scanner character helpers and JS string surrogate sentinels only.
- `JsNum/String.cs`: scanner number parsing/formatting slice.
- `JsNum/PseudoBigInt.cs`: ParsePseudoBigInt only.
- `Runtime/GoString.cs`: new byte-string/UTF8/GoBuilder compatibility support, not an upstream file port.
- `Runtime/UnicodeCase.cs`: Go1.27.1 unicode/letter.go ToLower/SimpleFold slice and fold equality adapter.
- `Runtime/UnicodeCaseTables.cs`: Go1.27.1 unicode/tables.go CaseRanges+caseOrbit slice (Unicode17.0.0,336 ranges94 orbit pairs); generator `tools/generate-unicode-case.py`.
- `licenses/Go-LICENSE.txt`: preserved Go BSD license.

Dependencies/API:

- Shared namespace `TypeScript.Scanner`; regexp is nested `regExpParser` in partial Scanner.
- Scanner fields retain upstream names; `char` renamed `charCode` because of C# keyword.
- Internal source/value strings store one Latin1 char per byte. Utf8 consumes invalid bytes with Go semantics. Public byte APIs preserve malformed UTF8 and lone surrogate sentinels.
- Scanner agent owns `Scanner.cs`, project and `Runtime/ScannerCompat.cs` (Debug assertions are unconditional).
- Regexp uses scanner `scanIdentifier(int,identifierVariant)` and `scanEscapeSequence` as upstream.

Latest verification: Release validation build succeeded with zero warnings/errors and no warning suppressions. Managed full oracle corpus:1276 cases17977 tokens1547 diagnostics, zero mismatches. Targeted Unicode casing corpus:36 cases108 tokens52 diagnostics, zero mismatches. Earlier K/k versus Kelvin and ß versus capital sharp-S failures motivated pinned Go Unicode compatibility. All scalar ToLower+SimpleFold outputs from -2 through0x110002 match Go SHA256 `01652f5d8dfef72894bf8c865d8512d556f637864680463364026dcb0f64c0ab`. Span decoders remove copied suffix allocation.

Remaining:

1. Verification agent final xUnit and Native AOT pass on latest source.
2. Root final docs/source-map and workspace checkpoint.

Completed independent review: `docs/progress/review-scanner.md`; fixed conflict marker loop and suffix allocation defects. Scanner agent support review: `docs/progress/review-support.md`.

Meaningful adaptations: .NET BigInteger replaces Go math/big, with explicit nearest-even rounding before double conversion; .NET shortest round-trip formatting is normalized to JS exponent/fixed boundaries. Spelling suggestions use pinned Go1.27.1 Unicode17.0.0 lower/fold data; identifier classification uses maintained TypeScript Unicode15.1 generated ranges exactly as upstream. Support is scanner-only; AST/compiler/locale facilities remain unported.
