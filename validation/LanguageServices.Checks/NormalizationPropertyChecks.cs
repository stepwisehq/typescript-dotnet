// Copyright 2013 The Go Authors. All rights reserved.
// BSD-style license: licenses/Go-LICENSE.txt.
// x/text v0.42.0 unicode/norm/forminfo_test.go:11 TestProperties.
// Selected canonical properties used by NFD.String; compatibility and composing
// observations remain outside this channel. This is not whole-function credit.
using System.IO.Compression;
using TypeScript.Scanner;
using TypeScript.UnicodeNormalization;

namespace LanguageServices.Checks;
public static class NormalizationPropertyChecks
{
    private readonly record struct Row(int Rune, byte Ccc, byte Lead, byte Trail,
        byte QuickCheck, byte[] Decomposition);

    public static int RunAll()
    {
        using var resource = typeof(NormalizationPropertyChecks).Assembly.GetManifestResourceStream(
            "LanguageServices.Checks.RegexData.nfd-canonical-properties.bin.gz")
            ?? throw new InvalidOperationException("Missing original canonical property data");
        using var compressed = new GZipStream(resource, CompressionMode.Decompress);
        using var reader = new BinaryReader(compressed);
        int count = reader.ReadInt32();
        var rows = new Row[count];
        for (int index = 0; index < count; index++)
        {
            int rune = reader.ReadInt32();
            byte ccc = reader.ReadByte(), lead = reader.ReadByte(), trail = reader.ReadByte();
            byte quickCheck = reader.ReadByte();
            int length = reader.ReadInt32();
            byte[] decomposition = reader.ReadBytes(length);
            if (decomposition.Length != length)
                throw new EndOfStreamException("Original canonical property data truncated");
            rows[index] = new Row(rune, ccc, lead, trail, quickCheck, decomposition);
        }
        if (reader.BaseStream.ReadByte() != -1)
            throw new InvalidOperationException("Unexpected canonical property data suffix");

        Row current = new(0, 0, 0, 0, 0, []);
        for (int index = 1, rune = 0; rune < 0x2ffff; rune++)
        {
            if (index < count && rune == rows[index].Rune)
                current = rows[index++];
            var properties = NormalizationProperties.LookupInfoNFC(GoString.FromRuneValue(rune), 0);
            if (properties.CCC() != current.Ccc ||
                properties.CombinesBackward() != (current.QuickCheck == 2) ||
                properties.NLeadingNonStarters() != current.Lead ||
                properties.NTrailingNonStarters() != current.Trail)
                throw new InvalidOperationException($"Original canonical property mismatch U+{rune:X}");
            // Original assertions skip algorithmic Hangul decomposition.
            if (rune >= 0xAC00 && rune < 0xD7A4)
                continue;
            if (properties.HasDecomposition() &&
                (current.Decomposition.Length == 0 || !properties.Decomposition().EqualsValue(
                    ByteStringValue.FromView(ByteStringView.FromOwnedBytes(current.Decomposition)))))
                throw new InvalidOperationException($"Original canonical decomposition mismatch U+{rune:X}");
        }
        return 1;
    }
}
