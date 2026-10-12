// Copyright 2011 The Go Authors. All rights reserved.
// BSD-style license: licenses/Go-LICENSE.txt.
// Pinned x/text v0.42.0 unicode/norm/normalize.go NFD.String and its selected worker closure.
using TypeScript.Scanner;

namespace TypeScript.UnicodeNormalization;
internal readonly struct NfdForm
{
    internal string String(string source) => Norm.String(ByteStringValue.FromString(source)).Materialize()!;
}

internal static class Norm
{
    internal static readonly NfdForm NFD = default;
    // normalize.go:59. Return the original header on the original quick-span fast path.
    internal static ByteStringValue String(ByteStringValue source)
    {
        var(length, complete) = QuickSpan(source, 0, source.Length, true);
        if (complete)
            return source;
        var output = Slice<byte>.Make(length, source.Length);
        CopySlice(source, output.AsSpan(), 0, length);
        var buffer = new ReorderBuffer
        {
            Source = source,
            SourceLength = source.Length,
            Output = output
        };
        var result = DoAppendInner(ref buffer, length);
        // Original []byte-to-string conversion owns an immutable copy of the completed output.
        return ByteStringValue.FromView(ByteStringView.FromOwnedBytes(result.AsSpan().ToArray()));
    }

    // input.go:34/46. Borrowed source byte access; no encoding or owned text bridge.
    private static int SkipASCII(ByteStringValue source, int position, int maximum)
    {
        while (position < maximum && source[position] < 0x80)
            position++;
        return position;
    }

    // input.go:58/68.
    private static Slice<byte> AppendSlice(ByteStringValue source, Slice<byte> output, int begin, int end)
    {
        if (source.IsBytes)
            return output.Append(source.ByteSpan[begin..end]);
        for (int i = begin; i < end; i++)
            output = output.Append((byte)source[i]);
        return output;
    }

    internal static int CopySlice(ByteStringValue source, Span<byte> destination, int begin, int end)
    {
        int count = Math.Min(destination.Length, end - begin);
        if (source.IsBytes)
            source.ByteSpan.Slice(begin, count).CopyTo(destination);
        else
            for (int i = 0; i < count; i++)
                destination[i] = (byte)source[begin + i];
        return count;
    }

    // input.go:92 and composition.go:358. Keep the upstream byte-prefix test before decoding.
    internal static int Hangul(ByteStringValue source, int position)
    {
        var bytes = source.Slice(position);
        if (!IsHangulString(bytes))
            return 0;
        var(rune, size) = bytes.IsBytes ? Utf8.DecodeRuneInString(bytes.ByteSpan) : Utf8.DecodeRuneInString(bytes.CharSpan);
        return size == 3 ? rune : 0;
    }

    private static bool IsHangulString(ByteStringValue source)
    {
        if (source.Length < 3)
            return false;
        int first = source[0];
        if (first < 0xEA)
            return false;
        int second = source[1];
        if (first == 0xEA)
            return second >= 0xB0;
        if (first < 0xED)
            return true;
        if (first > 0xED)
            return false;
        if (second < 0x9E)
            return true;
        return second == 0x9E && source[2] < 0xA4;
    }

    // normalize.go:182/251. Every source segment appends the original following quick span.
    private static int AppendQuick(ref ReorderBuffer buffer, int position)
    {
        if (buffer.SourceLength == position)
            return position;
        var(end, _) = QuickSpan(buffer.Source, position, buffer.SourceLength, true);
        buffer.Output = AppendSlice(buffer.Source, buffer.Output, position, end);
        return end;
    }

    private static Slice<byte> DoAppendInner(ref ReorderBuffer buffer, int position)
    {
        int length = buffer.SourceLength;
        while (position < length)
        {
            position = DecomposeSegment(ref buffer, position, true);
            position = AppendQuick(ref buffer, position);
        }

        return buffer.Output;
    }

    // normalize.go:304. Only the NFD form is in this closure: composing=false, compatibility=false.
    internal static (int Length, bool Complete) QuickSpan(ByteStringValue source, int position, int end, bool atEOF)
    {
        byte lastCombiningClass = 0;
        StreamSafe safe = default;
        int lastSegmentStart = position;
        int length = end;
        while (position < length)
        {
            int asciiEnd = SkipASCII(source, position, length);
            if (position != asciiEnd)
            {
                position = asciiEnd;
                lastSegmentStart = position - 1;
                lastCombiningClass = 0;
                safe = default;
                continue;
            }

            var information = NormalizationProperties.LookupInfoNFC(source, position);
            if (information.IsInvalid())
                return atEOF ? (length, true) : (lastSegmentStart, true);
            switch (safe.Next(information))
            {
                case StreamSafeState.Starter:
                    lastSegmentStart = position;
                    break;
                case StreamSafeState.Overflow:
                    return (lastSegmentStart, false);
                case StreamSafeState.Success:
                    if (lastCombiningClass > information.CombiningClass)
                        return (lastSegmentStart, false);
                    break;
            }

            if (!information.IsYesD())
                break;
            lastCombiningClass = information.CombiningClass;
            position += information.Size;
        }

        if (position == length)
            return (atEOF ? length : lastSegmentStart, true);
        return (lastSegmentStart, false);
    }

    // normalize.go:504. Preserve first-rune consumption, stream-safe boundary and invalid-prefix behavior.
    internal static int DecomposeSegment(ref ReorderBuffer buffer, int position, bool atEOF)
    {
        var information = NormalizationProperties.LookupInfoNFC(buffer.Source, position);
        if (information.IsInvalid())
            return position + 1;
        var state = buffer.StreamSafe.Next(information);
        if (state == StreamSafeState.Starter)
        {
            if (buffer.RuneCount > 0)
                goto end;
        }
        else if (state == StreamSafeState.Overflow)
        {
            buffer.InsertCGJ();
            goto end;
        }

        int error = buffer.InsertFlush(buffer.Source, position, information);
        if (error != 0)
            return error;
        while (true)
        {
            position += information.Size;
            if (position >= buffer.SourceLength)
            {
                if (!atEOF && !information.BoundaryAfter())
                    return -2;
                break;
            }

            information = NormalizationProperties.LookupInfoNFC(buffer.Source, position);
            if (information.IsInvalid())
            {
                if (!atEOF)
                    return -2;
                break;
            }

            state = buffer.StreamSafe.Next(information);
            if (state == StreamSafeState.Starter)
                break;
            if (state == StreamSafeState.Overflow)
            {
                buffer.InsertCGJ();
                break;
            }

            error = buffer.InsertFlush(buffer.Source, position, information);
            if (error != 0)
                return error;
        }

        end:
            if (!buffer.DoFlush())
                return -1;
        return position;
    }
}
