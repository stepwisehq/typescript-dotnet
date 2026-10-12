# Independent formatting review

The protocol lane independently compared all ten formatting counterparts against
pinned `internal/format` at `fed0bf24149fb1ed36039212648bafdafc1ea10e`:235
original functions,135 ordered rule entries and seven original test functions.
The final source/hash binding is
[formatting.json](../../artifacts/language-services/source-review/formatting.json).
Production is in [Formatting](../../src/TypeScript.Scanner/LanguageServices/Formatting/);
shared original assertions are in
[FormattingChecks.cs](../../validation/LanguageServices.Checks/FormattingChecks.cs).

Review covers rule/token/context classification, priority and packed bucket insertion,
scanner rescan/cache, byte/rune positions, monotonic error-range traversal, enclosing
node/list traversal, dynamic indentation mutation, trivia/edit order, all explicit
capacities and both required trivia clones. The explicit request context retains
settings and borrowed host-newline headers across derivations. All135 rule argument
lists and their ordering match; readable individual entries replaced compressed
arrays without changing arguments.

Resolved review/integration findings are retained in the binding: the original
Unicode-containing checker fixture now enters parsing and edit application through
the same original byte array, and the only two two-argument byte-view slices use
Go end offsets (indentation prefix and multiline-comment LF count). All remaining
formatting slice calls are suffix slices. The seven original functions preserve
all assertions and inputs, including the two distinct chained-call regressions.

Source review is clear. Root reported clean build23, all142 combined ordinary
managed cases passing and all seven original pinned Go formatting functions passing.
Actual Native23 dependency publication also passed; the execution result is owned
by the coordinator. This source review does not claim long-lived server or full
language-services acceptance. No reviewer builds or tests
were run. Raw artifact hashes bind the reviewed snapshot rather than introducing a
second source inventory.
