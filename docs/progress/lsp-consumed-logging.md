# Consumed LSP and project logging closure

The first real editor setup reached configuration and then failed because the
shared formatter rejected `%+v`. This supporting boundary is now source-complete
for the actual pinned Server, ProjectSystem and LanguageServices logger consumers;
candidate execution is pending. This is not a general Go `fmt` implementation.

| Consumed role | Preserved representation |
| --- | --- |
| Configuration `%T` / `%+v` | json/v2 nil-any owners: null, bool, raw string, float64, map[string]interface {}, []interface {}. Empty maps/slices remain distinguishable from a nil interface. |
| Path, URI, project/watcher ID `%s` / `%q` | Existing raw header and explicit Go Stringer projections; source Go quoting, no display round trip. |
| Error `%s` / `%v` / `%q` | Actual exception error message encoded once at this display boundary, then formatted/quoted as Go Error text. |
| Count / ID `%d`, cache-stat `%6d` | Existing exact integer and rune-width padding. |
| Duration `%v` | Existing explicit nanosecond Go Duration.String projection. |
| Documents / project IDs / errors / paths `%v` | Existing closed Slice element roles and ordinary Go bracket order. |
| Dynamic configuration maps / arrays | Required sorted raw-key map snapshot and source element order. An explicit traversal stack preserves admitted deep JSON without CLR recursive formatting. |

The pinned source anchors are `internal/lsp/server.go:836–895` (configuration),
`internal/lsp/logger.go`, `internal/project/snapshot.go:448–501`, and the actual
project/session/ATA/autoimport logger calls. The supporting Go1.27.1 workers are
`fmt/print.go` (printArg, printValue and plus-v/type admission), `fmt/format.go`
(formatting), and `strconv/ftoa.go` (shortest g exponent selection). Standard .NET
shortest-roundtrip digits supply the numeric boundary; Go's precision=-1 exponent
threshold, exponent sign/minimum digits and negative zero spelling are explicit.
No reflection, permissive unknown-struct ToString, DTO field introspection, or
invented address strings are introduced. `%+v` consumes decoded maps/primitives
here, so no unknown structured field-name implementation is claimed. Existing
explicit Stringer owners remain responsible for their actual source spelling.

The actual valid logger literals consume s/q/v/T/d and fixed decimal width, plus
`+v`. No caller needs arbitrary precision, star/index arguments, # flags or a
complete malformed-format formatter. Unsupported authored format/owner admission
continues to reject explicitly; it is not a supported upstream logging path.
Go pointer addresses and CLR exception stack/native diagnostic representations are
runtime diagnostics, not fabricated Go values or stable protocol parity claims.

[Tracked authored witness](../../tools/language-services-oracle/log_format_controls_test.go)
adds one function with13 observations to pinned `internal/lsp`. Root captured all13
successfully in `integration-7/log-format-reference.jsonl` (.006s package); raw
base64 outputs exactly match all13 shared expected values. These are authored
controls, never original upstream-function credit. A fourteenth authored control
checks the supported decoded10,000-level array spelling without CLR recursion.
[Shared checks](../../validation/LanguageServices.Checks/ConsumedLogFormatChecks.cs)
and [xUnit wrapper](../../tests/LanguageServices.Tests/ConsumedLogFormatTests.cs)
are source-written; managed/Native execution and actual editor setup remain pending.
