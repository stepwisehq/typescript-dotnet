// Copyright 2011 The Go Authors. All rights reserved.
// BSD-style license: licenses/Go-LICENSE.txt.
// normalize_test.go:596–643/924 TestString selected NFD cross-form bootstrap.
// The direct NFD table is empty. Original length/equality invariants are retained;
// the test-only empty AppendString setup is adapted to actual NFD.String(left).
using System.IO.Compression;
using TypeScript.Scanner;
using TypeScript.UnicodeNormalization;
namespace LanguageServices.Checks;
public static class NormalizationBootstrapChecks
{
    public static int RunAll()
    {
        using var source = typeof(NormalizationBootstrapChecks).Assembly.GetManifestResourceStream(
            "LanguageServices.Checks.RegexData.nfd-string-bootstrap.bin.gz")
            ?? throw new InvalidOperationException("Missing original NFD bootstrap input data");
        using var compressed = new GZipStream(source, CompressionMode.Decompress);
        using var reader = new BinaryReader(compressed);
        int count = reader.ReadInt32();
        if (count != 109) throw new InvalidOperationException("Original bootstrap input count differs");
        for (int index = 0; index < count; index++)
        {
            var left = Read(reader);
            var right = Read(reader);
            var want = Norm.String(ByteStringValue.Concat(left, right));
            var have = Norm.String(ByteStringValue.Concat(Norm.String(left), right));
            if (have.Length != want.Length || !have.EqualsValue(want))
                throw new InvalidOperationException("Original NFD TestString bootstrap invariant row " + index);
        }
        if (reader.BaseStream.ReadByte() != -1)
            throw new InvalidOperationException("Unexpected original bootstrap input suffix");
        return 1;
    }
    private static ByteStringValue Read(BinaryReader reader)
    {
        int length = reader.ReadInt32();
        byte[] bytes = reader.ReadBytes(length);
        if (bytes.Length != length) throw new EndOfStreamException("Original bootstrap data truncated");
        return ByteStringValue.FromView(ByteStringView.FromOwnedBytes(bytes));
    }
}
