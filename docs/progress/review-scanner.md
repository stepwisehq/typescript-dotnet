# Independent scanner implementation review

Reviewed `Scanner/Scanner.cs` and `Runtime/ScannerCompat.cs` against maintained upstream `tsc/internal/scanner/scanner.go` at `fed0bf24149fb1ed36039212648bafdafc1ea10e`.

## Findings

- **High, fixed during review:** `isConflictMarkerTrivia` initially lost the body of upstream range loop (Go lines 2431–2435). It checked only the first two bytes and trailing delimiter. This falsely reported merge conflicts on `<<abcde rest` and `==abcdeX`. Scanner agent restored comparison of all seven bytes after oracle failures and reviewer report. Regression corpus covers malformed/real markers.
- **Medium, fixed during review:** decoder calls and `scanASCIIWhile` initially created copied suffix strings with `Substring`. Go slices are constant-time views. Repeated decoding of long Unicode source/comments could therefore allocate quadratically. Scanner agent changed these calls to spans; support agent added string/span decoder overloads and changed regexp decoders.
- **Low, addressed:** generated scanner code initially retained redundant expression parentheses, unreachable terminal `break` statements, and `return ;`. Scanner agent applied syntax-aware formatting and removed unreachable terminal statements; no warning suppression remains. Explicit switch scopes preserve local declaration boundaries.

## Checked behavior and scope

- Go fallthrough is represented by C# `goto case`; labelled loop exits use `goto` labels. Identifier escape speculation restores scanner byte position/token flags on failure.
- Token values retain upstream CESU-8 lone-surrogate sentinels internally. `TokenValueBytes` exposes exact bytes; `TokenValue` decodes sentinels into .NET UTF-16 code units. Source/token text remains UTF8 and exposes exact byte APIs.
- Latest `SourceKeyword` and JSX hyphen/colon distinction follow current upstream. Identifier Unicode ranges are pinned data rather than runtime category tests.
- Numeric separators, legacy octal errors, extended escape handling and regexp parser state save/restore were inspected against the Go functions.
- Reset and SetText clear scanner state while preserving configured language/target callbacks as appropriate. Mark/Rewind copies mutable comment records defensively; upstream uses value state with shared Go slice storage.
- Text-only line position and comment-range helpers are included; AST/source-file functions are excluded. Replacing the comment NodeFactory with immutable records is the intended narrow adaptation.
- Runtime compatibility helpers only implement called slices of Go APIs. Strconv.ParseInt is used on unsigned digit strings here, so its unsupported sign parsing and nondecimal negative FormatInt behavior do not affect scanner paths.
- Debug.Assert is an ordinary unconditional helper, preserving required argument evaluation under Release and Native AOT. No reflection/dynamic code was observed in these files.

Latest reviewed build: `dotnet build src/TypeScript.Scanner/TypeScript.Scanner.csproj --no-restore -v:q`, zero warnings/errors. Managed Release differential corpus1276 cases passes; Native AOT validation is owned by verification agent.
