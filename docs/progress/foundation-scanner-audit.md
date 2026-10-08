# Scanner foundation audit

Pin: `fed0bf24149fb1ed36039212648bafdafc1ea10e`. Implementation inventory:
[158 scanner functions](../foundation-scanner-functions.json),
[original assertion catalog](../foundation-scanner-tests.json), and
[table audit](../foundation-scanner-table-audit.json). Final bounded foundation gate passed: [durable proof](../../validation/results/foundation-verified.json),
source `d14cb48750c8ce35f0767f97885b01f9b0d63c853390f6cfb4f9b3f4cb50fd67`.
Scanner shared2552 native assertions and3837 xUnit pass; all17723 broad scanner
comparisons are exact. No unresolved blocker or active work. Numeric and diagnostic
limitations below remain explicit.

## Diagnostic 1513

`Undetermined_character_escape` has one source emission site:
`regexp.go:465`, `scanCharacterEscape` case `char()==-1`. Its assertion requires
that the preceding raw byte is a backslash. The three callers consume a raw
backslash before invoking it (atom escape, Unicode-sets class character, ordinary
class atom). The parser's end is exactly the regex body end established by
`Scanner.ReScanSlashToken`.

The public rescan's first pass tracks `inEscape` across every raw byte. A slash
terminates the body only outside the first-pass character class and when it is
not escaped. Therefore a body accepted for detailed regexp parsing cannot end
with an unpaired raw backslash: that backslash would instead escape the proposed
closing slash. Paired trailing backslashes are consumed as a pair by the detailed
escape scanner. Unicode/hex escape values do not replace the underlying bytes or
turn a decoded backslash into a new raw escape introducer. Annex-B control-escape
rewinds retain the raw escaped position; they do not move an unpaired final
backslash into an otherwise accepted body.

If EOF/newline is encountered without a delimiter, rescan sets `Unterminated` and
reports1161 in its recovery arm. It never constructs/runs `regExpParser` in that
arm. `reportErrors=false` also never runs the detailed parser. Nested Unicode-sets
classes do not alter this delimiter precondition: the first pass intentionally
uses the ordinary nonnested class rule. The private1513 defensive arm remains
ported. This is a source-based public-unreachability argument, not observed1513
coverage or a claim that arbitrary private parser state cannot reach it.
Independent support review challenged all three callers, body-end setup, escape parity, ordinary/nonnested classes and Annex-B `c` rewind and accepted this argument. This reviewed source rationale closes the diagnostic-gap disposition; it does not claim observed1513 coverage.

## Tables, bytes and state

All524 quoted property-table atoms and all1365 identifier-range triples (including
strides) match pinned Go data in source order. Maps/sets use ordinal raw byte strings;
Script and Script_Extensions share the same value set. Identifier data isUnicode15.1;
Go casing/folding support separately uses the accepted pinned GoUnicode17 tables.
Raw scanner input and stored scalar values remain byte strings; public UTF16 wrappers
convert once. Lone-surrogate sentinel bytes are treated differently by ordinary UTF8
and JS-string decoding, as in Go. Coordinate fixtures include both malformed UTF8
and sentinel bytes. SetText resets ScannerState, including directives; Reset preserves
nil versus allocated-cleared regexp maps. Mark/Rewind preserve state snapshots with
explicit managed slice copies. Viable keyword map order is unspecified and compared
as membership. CommentRange is a Go value; factory construction has no creation hook.
TokenToString now uses the original fixed-size array and fails invalid indices rather
than silently returning an empty result; valid non-token kinds still return empty.

## Numeric facility adaptation

Original Number.String safe integers use FormatInt; other finite values use
encoding/json/v2→jsonwire.AppendFloat→strconv shortest formatting, with fixed notation
in[1e-6,1e21), lowercase exponent and no exponent-leading zero. C# uses the corresponding
presentation rules over invariant R digits, verifies the emitted digits roundtrip to
the original value, and falls back to invariant G17 if they do not. The actual original
fixture `3e60000000000000` exposed R/G16 spelling parsing to `3e5fffffffffffff`; G17
roundtrips and matches upstream/Node. No value-specific special case was added.

All original formatter/parser assertions and16,705 focused IEEE samples match Go and
Node formatting plus parsed bits (NaN payload/sign is compared as NaN class). Samples
cover every binary power and immediate neighbors/both signs, notation/safe-integer
boundaries and4096 fixed recurrence values. This is finite evidence: G17 guarantees a
roundtrip but does not by itself establish shortestness/tie equivalence for all IEEE
values. Number parsing additionally retains arbitrary-size integer nearest-even
rounding. Go base-zero BigInt underscore rules were repaired after three original
assertion failures. Arithmetic/bitwise/exponentiation and unrelated URI/casing
operations are excluded because no scanner/parser dependency calls them.
