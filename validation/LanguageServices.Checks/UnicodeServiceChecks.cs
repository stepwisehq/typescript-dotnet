// Copyright 2009 The Go Authors. All rights reserved.
// BSD-style license: licenses/Go-LICENSE.txt.
// Pinned Go1.27.1 unicode: three complete original tests plus selected lower/letter optimization channels.
using TypeScript.Scanner;

namespace LanguageServices.Checks;
public static class UnicodeServiceChecks
{
    public static readonly string[] Names = ["TestIsLetter", "TestDigit", "TestDigitOptimization", "TestLetterOptimizations.LetterAndLower"];
    public static int RunAll()
    {
        foreach (var name in Names)
            Run(name);
        return Names.Length;
    }

    public static void Run(string name)
    {
        switch (name)
        {
            case "TestIsLetter":
                foreach (var rune in UpperLetters)
                    Check(GoUnicode.IsLetter(rune), true, rune);
                foreach (var rune in Letters)
                    Check(GoUnicode.IsLetter(rune), true, rune);
                foreach (var rune in NonLetters)
                    Check(GoUnicode.IsLetter(rune), false, rune);
                break;
            case "TestDigit":
                foreach (var rune in Digits)
                    Check(GoUnicode.IsDigit(rune), true, rune);
                foreach (var rune in DigitNonLetters)
                    Check(GoUnicode.IsDigit(rune), false, rune);
                break;
            case "TestDigitOptimization":
                for (var rune = 0; rune <= 255; rune++)
                    Check(GoUnicode.IsDigit(rune), GoUnicode.Is(GoUnicode.Nd, rune), rune);
                break;
            case "TestLetterOptimizations.LetterAndLower":
                for (var rune = 0; rune <= 255; rune++)
                {
                    Check(GoUnicode.IsLetter(rune), GoUnicode.Is(GoUnicode.L, rune), rune);
                    Check(GoUnicode.IsLower(rune), GoUnicode.Is(GoUnicode.Ll, rune), rune);
                }

                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(name));
        }
    }

    private static void Check(bool actual, bool expected, int rune)
    {
        if (actual != expected)
            throw new InvalidOperationException("Unicode predicate differs at U+" + rune.ToString("X"));
    }

    private static readonly int[] UpperLetters = [0x41, 0xc0, 0xd8, 0x100, 0x139, 0x14a, 0x178, 0x181, 0x376, 0x3cf, 0x13bd, 0x1f2a, 0x2102, 0x2c00, 0x2c10, 0x2c20, 0xa650, 0xa722, 0xff3a, 0x10400, 0x1d400, 0x1d7ca];
    private static readonly int[] Letters = [0x41, 0x61, 0xaa, 0xba, 0xc8, 0xdb, 0xf9, 0x2ec, 0x535, 0x620, 0x6e6, 0x93d, 0xa15, 0xb99, 0xdc0, 0xedd, 0x1000, 0x1200, 0x1312, 0x1401, 0x2c00, 0xa800, 0xf900, 0xfa30, 0xffda, 0xffdc, 0x10000, 0x10300, 0x10400, 0x20000, 0x2f800, 0x2fa1d];
    private static readonly int[] NonLetters = [0x20, 0x35, 0x375, 0x619, 0x700, 0x1885, 0xfffe, 0x1ffff, 0x10ffff];
    private static readonly int[] Digits = [0x0030, 0x0039, 0x0661, 0x06F1, 0x07C9, 0x0966, 0x09EF, 0x0A66, 0x0AEF, 0x0B66, 0x0B6F, 0x0BE6, 0x0BEF, 0x0C66, 0x0CEF, 0x0D66, 0x0D6F, 0x0E50, 0x0E59, 0x0ED0, 0x0ED9, 0x0F20, 0x0F29, 0x1040, 0x1049, 0x1090, 0x1091, 0x1099, 0x17E0, 0x17E9, 0x1810, 0x1819, 0x1946, 0x194F, 0x19D0, 0x19D9, 0x1B50, 0x1B59, 0x1BB0, 0x1BB9, 0x1C40, 0x1C49, 0x1C50, 0x1C59, 0xA620, 0xA629, 0xA8D0, 0xA8D9, 0xA900, 0xA909, 0xAA50, 0xAA59, 0xFF10, 0xFF19, 0x104A1, 0x1D7CE];
    private static readonly int[] DigitNonLetters = [0x0041, 0x0061, 0x00AA, 0x00BA, 0x00C8, 0x00DB, 0x00F9, 0x02EC, 0x0535, 0x06E6, 0x093D, 0x0A15, 0x0B99, 0x0DC0, 0x0EDD, 0x1000, 0x1200, 0x1312, 0x1401, 0x1885, 0x2C00, 0xA800, 0xF900, 0xFA30, 0xFFDA, 0xFFDC, 0x10000, 0x10300, 0x10400, 0x20000, 0x2F800, 0x2FA1D];
}
