// Copyright 2011 The Go Authors. All rights reserved.
// BSD-style license: licenses/Go-LICENSE.txt.
// Pinned golang.org/x/text v0.42.0 unicode/norm/trie.go and tables17.0.0.go.
using TypeScript.Scanner;

namespace TypeScript.UnicodeNormalization;
internal static class NfcTrie
{
    // trie.go:37. Sparse owner/table references retain the original global immutable data.
    private static ushort LookupSparse(int block, int value)
    {
        int offset = NormTables.nfcSparseOffset[block];
        var header = NormTables.nfcSparseValues[offset];
        int low = offset + 1;
        int high = low + header.Low;
        while (low < high)
        {
            int middle = low + (high - low) / 2;
            var range = NormTables.nfcSparseValues[middle];
            if (range.Low <= value && value <= range.High)
                return unchecked((ushort)(range.Value + (value - range.Low) * header.Value));
            if (value < range.Low)
                high = middle;
            else
                low = middle + 1;
        }

        return 0;
    }

    // tables17.0.0.go:2988. Original dense/sparse block cutoff.
    private static ushort LookupValue(int block, int value) => block < 46 ? NormTables.nfcValues[(block << 6) + value] : LookupSparse(block - 46, value);
    // tables17.0.0.go:2898. Byte-role string lookup, including original incomplete/illegal prefix widths.
    internal static (ushort Value, int Size) LookupString(ByteStringValue source)
    {
        int c0 = source[0];
        if (c0 < 0x80)
            return (NormTables.nfcValues[c0], 1);
        if (c0 < 0xC2)
            return (0, 1);
        if (c0 < 0xE0)
        {
            if (source.Length < 2)
                return (0, 0);
            int index = NormTables.nfcIndex[c0];
            int c1 = source[1];
            if (c1 < 0x80 || 0xC0 <= c1)
                return (0, 1);
            return (LookupValue(index, c1), 2);
        }

        if (c0 < 0xF0)
        {
            if (source.Length < 3)
                return (0, 0);
            int index = NormTables.nfcIndex[c0];
            int c1 = source[1];
            if (c1 < 0x80 || 0xC0 <= c1)
                return (0, 1);
            index = NormTables.nfcIndex[(index << 6) + c1];
            int c2 = source[2];
            if (c2 < 0x80 || 0xC0 <= c2)
                return (0, 2);
            return (LookupValue(index, c2), 3);
        }

        if (c0 < 0xF8)
        {
            if (source.Length < 4)
                return (0, 0);
            int index = NormTables.nfcIndex[c0];
            int c1 = source[1];
            if (c1 < 0x80 || 0xC0 <= c1)
                return (0, 1);
            index = NormTables.nfcIndex[(index << 6) + c1];
            int c2 = source[2];
            if (c2 < 0x80 || 0xC0 <= c2)
                return (0, 2);
            index = NormTables.nfcIndex[(index << 6) + c2];
            int c3 = source[3];
            if (c3 < 0x80 || 0xC0 <= c3)
                return (0, 3);
            return (LookupValue(index, c3), 4);
        }

        return (0, 1);
    }

    // tables17.0.0.go:2960. Caller guarantees one full valid encoding, as upstream.
    internal static ushort LookupStringUnsafe(ByteStringValue source)
    {
        int c0 = source[0];
        if (c0 < 0x80)
            return NormTables.nfcValues[c0];
        int index = NormTables.nfcIndex[c0];
        if (c0 < 0xE0)
            return LookupValue(index, source[1]);
        index = NormTables.nfcIndex[(index << 6) + source[1]];
        if (c0 < 0xF0)
            return LookupValue(index, source[2]);
        index = NormTables.nfcIndex[(index << 6) + source[2]];
        if (c0 < 0xF8)
            return LookupValue(index, source[3]);
        return 0;
    }
}
