// Copyright 2009 The Go Authors. All rights reserved.
// BSD-style license: licenses/Go-LICENSE.txt.
// Pinned Go1.27.1 strconv/quote_test.go: complete TestCanBackquote original assertions.
using TypeScript.Scanner;

namespace LanguageServices.Checks;
public static class CanBackquoteChecks
{
    public static int RunAll()
    {
        foreach (var(hex, expected)in Cases)
        {
            var text = ByteStringValue.FromView(ByteStringView.FromOwnedBytes(Convert.FromHexString(hex)));
            if (GoQuote.CanBackquote(text) != expected)
                throw new InvalidOperationException("CanBackquote differs for bytes " + hex);
        }

        return 1;
    }

    private static readonly (string Hex, bool Expected)[] Cases = [("60", false), ("00", false), ("01", false), ("02", false), ("03", false), ("04", false), ("05", false), ("06", false), ("07", false), ("08", false), ("09", true), ("0a", false), ("0b", false), ("0c", false), ("0d", false), ("0e", false), ("0f", false), ("10", false), ("11", false), ("12", false), ("13", false), ("14", false), ("15", false), ("16", false), ("17", false), ("18", false), ("19", false), ("1a", false), ("1b", false), ("1c", false), ("1d", false), ("1e", false), ("1f", false), ("7f", false), ("27202122232425262728292a2b2c2d2e2f3a3b3c3d3e3f405b5c5d5e5f7b7c7d7e", true), ("30313233343536373839", true), ("4142434445464748494a4b4c4d4e4f505152535455565758595a", true), ("6162636465666768696a6b6c6d6e6f707172737475767778797a", true), ("e298ba", true), ("80", false), ("61e0a07a", false), ("efbbbf616263", false), ("61efbbbf7a", false), ];
}
