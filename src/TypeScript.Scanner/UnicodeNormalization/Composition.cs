// Copyright 2011 The Go Authors. All rights reserved.
// BSD-style license: licenses/Go-LICENSE.txt.
// Pinned x/text v0.42.0 unicode/norm/composition.go: NFD.String reorder/stream-safe closure.
using System.Runtime.CompilerServices;
using TypeScript.Scanner;

namespace TypeScript.UnicodeNormalization;
internal enum StreamSafeState
{
    Success,
    Starter,
    Overflow
}

internal struct StreamSafe
{
    private byte count;
    // composition.go:44. Count original leading/trailing non-starters before decomposition.
    internal StreamSafeState Next(NormalizationProperties properties)
    {
        if (count > 30)
            throw new InvalidOperationException("streamSafe was not reset");
        int leading = properties.NLeadingNonStarters();
        count = (byte)(count + leading);
        if (count > 30)
        {
            count = 0;
            return StreamSafeState.Overflow;
        }

        if (leading == 0)
        {
            count = properties.NTrailingNonStarters();
            return StreamSafeState.Starter;
        }

        return StreamSafeState.Success;
    }
}

[InlineArray(32)]
internal struct NormalizationRuneBuffer
{
    private NormalizationProperties element;
}

[InlineArray(128)]
internal struct NormalizationByteBuffer
{
    private byte element;
}

// Go's fixed arrays embed into the per-call mutable value owner, not per-rune heap records.
internal struct ReorderBuffer
{
    internal NormalizationRuneBuffer Runes;
    internal NormalizationByteBuffer Bytes;
    internal byte ByteCount;
    internal StreamSafe StreamSafe;
    internal int RuneCount;
    internal ByteStringValue Source;
    internal int SourceLength;
    internal ByteStringValue TemporaryBytes;
    internal Slice<byte> Output;
    // composition.go:134/139/149. NFD has composing=false and appendFlush as its sole selected flusher.
    internal void Reset()
    {
        RuneCount = 0;
        ByteCount = 0;
    }

    internal bool DoFlush()
    {
        bool result = AppendFlush();
        Reset();
        return result;
    }

    private bool AppendFlush()
    {
        for (int i = 0; i < RuneCount; i++)
        {
            int start = Runes[i].Position;
            int end = start + Runes[i].Size;
            Output = Output.Append(((ReadOnlySpan<byte>)Bytes)[start..end]);
        }

        return true;
    }

    // composition.go:184. Stable insertion, preserving equal CCC order and fixed four-byte slots.
    private void InsertOrdered(NormalizationProperties information)
    {
        int n = RuneCount;
        int combiningClass = information.CombiningClass;
        if (combiningClass > 0)
            for (; n > 0; n--)
            {
                if (Runes[n - 1].CombiningClass <= combiningClass)
                    break;
                Runes[n] = Runes[n - 1];
            }

        RuneCount++;
        information.Position = ByteCount;
        ByteCount += 4;
        Runes[n] = information;
    }

    // composition.go:218. NFD append flusher cannot report a short destination.
    internal int InsertFlush(ByteStringValue source, int index, NormalizationProperties information)
    {
        int hangul = Norm.Hangul(source, index);
        if (hangul != 0)
        {
            DecomposeHangul(hangul);
            return 0;
        }

        if (information.HasDecomposition())
            return InsertDecomposed(information.Decomposition());
        InsertSingle(source, index, information);
        return 0;
    }

    // composition.go:249. Flush each real decomposition boundary without resetting stream-safe counting.
    private int InsertDecomposed(ByteStringValue decomposition)
    {
        TemporaryBytes = decomposition;
        for (int i = 0; i < decomposition.Length;)
        {
            var information = NormalizationProperties.LookupInfoNFC(TemporaryBytes, i);
            if (information.BoundaryBefore() && RuneCount > 0 && !DoFlush())
                return -1;
            int copied = Norm.CopySlice(decomposition, ((Span<byte>)Bytes)[ByteCount..], i, i + information.Size);
            i += copied;
            InsertOrdered(information);
        }

        return 0;
    }

    // composition.go:267/273. CGJ is the original two-byte UTF8 encoding, not a Unicode boundary conversion.
    private void InsertSingle(ByteStringValue source, int index, NormalizationProperties information)
    {
        Norm.CopySlice(source, ((Span<byte>)Bytes)[ByteCount..], index, index + information.Size);
        InsertOrdered(information);
    }

    internal void InsertCGJ() => InsertSingle(ByteStringValue.FromString("\xCD\x8F"), 0, new() { Size = 2 });
    // composition.go:278.
    private void AppendRune(int rune)
    {
        byte position = ByteCount;
        int size = GoString.EncodeRune(rune, ((Span<byte>)Bytes)[position..]);
        ByteCount += 4;
        Runes[RuneCount] = new()
        {
            Position = position,
            Size = (byte)size
        };
        RuneCount++;
    }

    // composition.go:411. Original algorithmic Hangul decomposition.
    private void DecomposeHangul(int rune)
    {
        rune -= 0xAC00;
        int trailing = rune % 28;
        rune /= 28;
        AppendRune(0x1100 + rune / 21);
        AppendRune(0x1161 + rune % 21);
        if (trailing != 0)
            AppendRune(0x11A7 + trailing);
    }
}
