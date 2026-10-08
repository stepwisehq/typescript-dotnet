# Scanner port checkpoint

Final coordinator disposition: **complete at the accepted scanner checkpoint**.
All final gates passed; see [VERIFICATION.md](../VERIFICATION.md) and
[ASSESSMENT.md](../ASSESSMENT.md). No active next action remains; any earlier
pending-gate notes below describe prior milestones and are superseded.

- Owner: scanner agent. Maintained upstream `upstream/TypeScript/tsc/internal/scanner/scanner.go`, SHA `fed0bf24149fb1ed36039212648bafdafc1ea10e`.
- Owned files: `src/TypeScript.Scanner/Scanner/Scanner.cs`, `Scanner/Utilities.cs`, `Runtime/ScannerCompat.cs`, `TypeScript.Scanner.csproj`.
- Scanner main loop, strings/templates/numerics, JSX/JSDoc, rescans, directives, state save/restore, identifier and trivia APIs translated preserving upstream function boundaries/control flow.
- Backing source/value strings hold one Latin-1 char per UTF-8 byte; string public getters decode, byte getters authoritative. Public source accepts string and raw byte[]/ReadOnlySpan<byte>.
- Excluded AST-facing helpers upstream lines 2506–2655 and 2673–2730; pure text/line helpers included. Pure leading/trailing comment iterators implemented directly, replacing NodeFactory with CommentRange record.
- Translation scaffold: temporary `/tmp/scanner-port.go` Go AST emitter, `/tmp/patch-scanner.py` initial patch only; current Scanner.cs additionally edited, DO NOT rerun initial emitter over it.
- Last command: `dotnet build src/TypeScript.Scanner/TypeScript.Scanner.csproj --nologo`; passed, 0 warnings/errors, no warning suppression.
- Own compiler errors fixed: C# reserved names, Go switch fallthrough, tuple declaration scopes, dictionary lookup presence, AST helper removal.
- Dependencies: support agent owns Regexp.cs, identifiers tables, JsNum, enums/diagnostics/GoString. all dependencies integrated and built.
- Completed defects: restored conflict-marker seven-byte validation, Unicode callback argument decoding, public isolated-surrogate token value decoding, and removed unreachable generated terminal breaks. Source decoder/ASCII scanning uses span slices to avoid suffix copies.
- Review: docs/progress/review-support.md.
- Verification: 1276 managed differential cases currently pass (including utility protocol), 8/8 direct UtilityTests pass. Original defects fixed; final full xUnit/AOT gate pending.
- Next: await final xUnit/AOT full gates. Own files cleaned with Roslyn safe-parentheses rewrite plus whitespace/LF formatting; no warning suppression.

- API adaptations: CommentDirectives returns an array snapshot; Mark/Rewind copy the directive list. Byte-backed source requires two bytes of .NET char storage per UTF-8 byte plus object overhead; necessary token/value substrings and output encoding allocate. No throughput/zero-allocation claim; decoder and ASCII slices now avoid copying source suffixes.
