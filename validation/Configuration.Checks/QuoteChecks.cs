// Copyright 2009 The Go Authors. All rights reserved.
// BSD-style license: licenses/Go-LICENSE.txt.
// Modified selected strconv/quote_test.go assertions; see provenance for scope.
using TypeScript.Scanner;

namespace Configuration.Checks;

public static partial class QuoteChecks
{
    public static IReadOnlyList<string> Cases { get; } = ["TestIsPrint", "TestQuote.SelectedQuoteOperation"];

    public static void Run(string name)
    {
        switch (name)
        {
            case "TestIsPrint":
                // Preserve the complete original rune domain. Expected Unicode
                // values are independent of the ported strconv print tables.
                for (int rune = 0; rune <= 0x10FFFF; rune++)
                {
                    bool expected = (expectedPrint[rune / 8] & (1 << (rune % 8))) != 0;
                    if (GoQuote.IsPrint(rune) != expected)
                        throw new InvalidOperationException($"IsPrint(U+{rune:X}) differs from locked Go unicode.IsPrint.");
                }
                break;
            case "TestQuote.SelectedQuoteOperation":
                foreach (var (input, expected) in quoteRows)
                {
                    string rawInput = GoString.FromBytes(Convert.FromBase64String(input));
                    string rawExpected = GoString.FromBytes(Convert.FromBase64String(expected));
                    Check.Equal(GoQuote.Quote(rawInput), rawExpected, "Original Quote literal assertion");
                }
                break;
            default:
                throw new InvalidOperationException("Unknown quoting assertion group: " + name);
        }
    }
}
