// New .NET compatibility support; not an upstream source port.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// .NET adaptation for Go byte strings. Internal strings contain one char per byte.
using System.Text;

namespace TypeScript.Scanner;
internal static class GoString
{
    internal static int EncodeRune(int rune, Span<byte> bytes)
    {
        if (rune < 0 || rune > 0x10FFFF || rune is >= 0xD800 and <= 0xDFFF)
            rune = Utf8.RuneError;
        return new Rune(rune).EncodeToUtf8(bytes);
    }
    public static string FromRune(int rune)
    {
        Span<byte> bytes = stackalloc byte[4];
        int size = EncodeRune(rune, bytes);
        return Encoding.Latin1.GetString(bytes[..size]);
    }
    // string(rune) owns its encoded result. Internal consumers retain those
    // bytes directly; the raw CLR string is only an explicit output boundary.
    public static ByteStringValue FromRuneValue(int rune)
    {
        Span<byte> bytes = stackalloc byte[4];
        int size = EncodeRune(rune, bytes);
        return ByteStringValue.FromView(ByteStringView.FromOwnedBytes(bytes[..size].ToArray()));
    }

    public static string FromBytes(ReadOnlySpan<byte> bytes) => Encoding.Latin1.GetString(bytes);
    public static string ToUtf8(string text) => Encoding.Latin1.GetString(Encoding.UTF8.GetBytes(text));
    public static string ToString(string bytes) => Encoding.UTF8.GetString(Bytes(bytes));
    public static byte[] Bytes(string bytes) => Encoding.Latin1.GetBytes(bytes);
    // Go range reads an immutable UTF-8 string without owning a rune array.
    public static GoRuneRange EnumerateRunes(string bytes) => new(bytes);
    public static GoRuneRange EnumerateRunes(ByteStringValue bytes) => new(bytes);
    public static int CountRunes(string bytes) => CountRunes(ByteStringValue.FromString(bytes));
    public static int CountRunes(ByteStringValue bytes)
    {
        int count = 0;
        foreach (int rune in EnumerateRunes(bytes)) count++;
        return count;
    }

    // Go runtime/string.go stringtoslicerune: count then fill the owned output.
    // See licenses/Go-LICENSE.txt. Indexing/mutation clients retain this array API;
    // ordinary range clients use EnumerateRunes and never allocate this storage.
    public static int[] Runes(string bytes) => Runes(ByteStringValue.FromString(bytes));
    public static int[] Runes(ByteStringValue bytes)
    {
        int count = CountRunes(bytes);
        if (count == 0) return Array.Empty<int>();
        var runes = new int[count];
        int index = 0;
        foreach (int rune in EnumerateRunes(bytes)) runes[index++] = rune;
        return runes;
    }

}

// Heap-compatible immutable header; direct foreach uses the value enumerator.
// Explicit enumerable interfaces box only at the requested interface boundary.
internal readonly struct GoRuneRange : IEnumerable<int>
{
    private readonly ByteStringValue source;
    private readonly int length;
    internal GoRuneRange(string source)
    {
        this.source = ByteStringValue.FromString(source);
        length = source.Length;
    }
    internal GoRuneRange(ByteStringValue source)
    {
        if (source.IsNull) throw new NullReferenceException();
        this.source = source.Snapshot();
        length = source.Length;
    }
    public Enumerator GetEnumerator() => new(source, length);
    IEnumerator<int> IEnumerable<int>.GetEnumerator() => GetEnumerator();
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();

    public struct Enumerator : IEnumerator<int>
    {
        private readonly ByteStringValue source;
        private readonly int length;
        private int offset;
        public int Current { get; private set; }
        internal int ByteIndex { get; private set; }
        object System.Collections.IEnumerator.Current => Current;
        internal Enumerator(ByteStringValue source, int length)
        {
            this.source = source;
            this.length = length;
            offset = 0;
            Current = default;
            ByteIndex = default;
        }
        public bool MoveNext()
        {
            if (offset >= length) return false;
            var (rune, size) = source.IsBytes ? Utf8.DecodeRuneInString(source.ByteSpan.Slice(offset, length - offset))
                : Utf8.DecodeRuneInString(source.CharSpan.Slice(offset, length - offset));
            ByteIndex = offset;
            Current = rune;
            offset += size;
            return true;
        }
        public void Dispose() { }
        public void Reset() => throw new NotSupportedException();
    }
}

// Indexed Go range shares the same decoder/immutable source header.
internal readonly struct GoRuneIndexRange(ByteStringValue source)
{
    public Enumerator GetEnumerator() => new(new GoRuneRange(source).GetEnumerator());
    public struct Enumerator(GoRuneRange.Enumerator runes)
    {
        public KeyValuePair<int, int> Current => new(runes.ByteIndex, runes.Current);
        public bool MoveNext() => runes.MoveNext();
    }
}

// unicode/utf8.DecodeRuneInString semantics: an invalid sequence consumes one byte;
// an empty sequence returns (RuneError,0); surrogates and overlong encodings fail.
internal static class Utf8
{
    public const int RuneError = 0xFFFD;
    public const int RuneSelf = 0x80;
    public static (int Rune, int Size) DecodeRuneInString(string text) => DecodeRuneInString(text.AsSpan());
    public static (int Rune, int Size) DecodeRuneInString(ReadOnlySpan<char> text)
    {
        if (text.IsEmpty)
            return (RuneError, 0);
        int first = text[0];
        if (first < 0x80)
            return (first, 1);
        int size, value, secondLow = 0x80, secondHigh = 0xBF;
        if (first is >= 0xC2 and <= 0xDF)
        {
            size = 2;
            value = first & 0x1F;
        }
        else if (first is >= 0xE0 and <= 0xEF)
        {
            size = 3;
            value = first & 0xF;
            if (first == 0xE0)
                secondLow = 0xA0;
            if (first == 0xED)
                secondHigh = 0x9F;
        }
        else if (first is >= 0xF0 and <= 0xF4)
        {
            size = 4;
            value = first & 7;
            if (first == 0xF0)
                secondLow = 0x90;
            if (first == 0xF4)
                secondHigh = 0x8F;
        }
        else
            return (RuneError, 1);
        if (text.Length < size || text[1] < secondLow || text[1] > secondHigh)
            return (RuneError, 1);
        value = (value << 6) | (text[1] & 0x3F);
        for (int i = 2; i < size; i++)
        {
            if (text[i] < 0x80 || text[i] > 0xBF)
                return (RuneError, 1);
            value = (value << 6) | (text[i] & 0x3F);
        }

        return (value, size);
    }

    public static (int Rune, int Size) DecodeLastRuneInString(string text) => DecodeLastRuneInString(text.AsSpan());
    public static (int Rune, int Size) DecodeLastRuneInString(ReadOnlySpan<char> text)
    {
        if (text.Length == 0)
            return (RuneError, 0);
        int start = text.Length - 1;
        int lower = Math.Max(0, text.Length - 4);
        while (start > lower && text[start] is >= (char)0x80 and <= (char)0xBF)
            start--;
        var result = DecodeRuneInString(text[start..]);
        return start + result.Size == text.Length ? result : (RuneError, 1);
    }

    public static (int Rune, int Size) DecodeRuneInString(ReadOnlySpan<byte> text)
    {
        if (text.IsEmpty)
            return (RuneError, 0);
        int first = text[0];
        if (first < 0x80)
            return (first, 1);
        int size, value, secondLow = 0x80, secondHigh = 0xBF;
        if (first is >= 0xC2 and <= 0xDF)
        {
            size = 2;
            value = first & 0x1F;
        }
        else if (first is >= 0xE0 and <= 0xEF)
        {
            size = 3;
            value = first & 0xF;
            if (first == 0xE0)
                secondLow = 0xA0;
            if (first == 0xED)
                secondHigh = 0x9F;
        }
        else if (first is >= 0xF0 and <= 0xF4)
        {
            size = 4;
            value = first & 7;
            if (first == 0xF0)
                secondLow = 0x90;
            if (first == 0xF4)
                secondHigh = 0x8F;
        }
        else
            return (RuneError, 1);
        if (text.Length < size || text[1] < secondLow || text[1] > secondHigh)
            return (RuneError, 1);
        value = (value << 6) | (text[1] & 0x3F);
        for (int i = 2; i < size; i++)
        {
            if (text[i] < 0x80 || text[i] > 0xBF)
                return (RuneError, 1);
            value = (value << 6) | (text[i] & 0x3F);
        }

        return (value, size);
    }

    public static (int Rune, int Size) DecodeLastRuneInString(ReadOnlySpan<byte> text)
    {
        if (text.Length == 0)
            return (RuneError, 0);
        int start = text.Length - 1;
        int lower = Math.Max(0, text.Length - 4);
        while (start > lower && text[start] is >= (byte)0x80 and <= (byte)0xBF)
            start--;
        var result = DecodeRuneInString(text[start..]);
        return start + result.Size == text.Length ? result : (RuneError, 1);
    }

    public static int RuneLen(int rune) => rune < 0 || rune > 0x10FFFF || rune is >= 0xD800 and <= 0xDFFF ? -1 : rune < 0x80 ? 1 : rune < 0x800 ? 2 : rune < 0x10000 ? 3 : 4;
}

// strings/builder.go: retain one byte-slice field, sharing immutable published
// prefixes. Reference-type API prevents copying a nonzero builder. CLR arrays
// omit Go allocator size-class rounding; Grow retains its 2*cap+n rule.
internal sealed class GoBuilder
{
    private Slice<byte> buffer;
    // Only legacy callers supplying non-byte UTF16 text require this fallback.
    // Ordinary Go-byte writes never create a second builder/backing object.
    private StringBuilder? legacy;
    public void Grow(int count)
    {
        if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
        if (legacy is not null) { legacy.EnsureCapacity(checked(legacy.Length + count)); return; }
        if (buffer.Capacity - buffer.Length >= count) return;
        var grown = Slice<byte>.Make(buffer.Length, checked(2 * buffer.Capacity + count));
        grown.CopyFrom(buffer);
        buffer = grown;
    }
    public void Reset() { buffer = default; legacy = null; }
    public void WriteString(string bytes) => WriteString(bytes.AsSpan());
    public void WriteString(ReadOnlySpan<char> bytes)
    {
        if (legacy is not null) { legacy.Append(bytes); return; }
        if (bytes.IndexOfAnyExceptInRange('\0', '\u00ff') >= 0)
        {
            UseLegacy().Append(bytes);
            return;
        }
        int start = buffer.Length;
        buffer = buffer.Grow(bytes.Length).Slice2(0, checked(start + bytes.Length));
        Encoding.Latin1.GetBytes(bytes, buffer.AsSpan()[start..]);
    }
    public void WriteString(ByteStringValue bytes)
    {
        if (bytes.IsBytes) WriteString(bytes.ByteSpan);
        else WriteString(bytes.CharSpan);
    }
    public void WriteString(ReadOnlySpan<byte> bytes)
    {
        if (legacy is null) { buffer = buffer.Append(bytes); return; }
        Span<char> chars = stackalloc char[256];
        while (!bytes.IsEmpty)
        {
            int count = Math.Min(bytes.Length, chars.Length);
            Encoding.Latin1.GetChars(bytes[..count], chars);
            legacy.Append(chars[..count]);
            bytes = bytes[count..];
        }
    }
    private StringBuilder UseLegacy()
    {
        legacy = new StringBuilder(buffer.Length);
        WriteString(buffer.AsSpan());
        buffer = default;
        return legacy;
    }
    public void WriteRune(int rune)
    {
        Span<byte> bytes = stackalloc byte[4];
        int size = GoString.EncodeRune(rune, bytes);
        WriteString(bytes[..size]);
    }
    internal void WriteJSStringRune(int rune)
    {
        Span<byte> bytes = stackalloc byte[4];
        int size = StringUtil.EncodeJSStringRune(rune, bytes);
        WriteString(bytes[..size]);
    }
    public void WriteByte(int b)
    {
        char ch = unchecked((char)b);
        if (legacy is not null) legacy.Append(ch);
        else if (ch <= 0xFF) buffer = buffer.Append((byte)ch);
        else UseLegacy().Append(ch);
    }
    public ByteStringValue Value() => legacy is not null
        ? ByteStringValue.FromString(legacy.ToString())
        : ByteStringValue.FromView(ByteStringView.FromBuilderSlice(buffer));
    public string String() => legacy is not null ? legacy.ToString() : GoString.FromBytes(buffer.AsSpan());
    public int Len() => legacy?.Length ?? buffer.Length;
}
