// New managed compatibility support for immutable Go string headers.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
using System.Runtime.InteropServices;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace TypeScript.Scanner;

// One heap-compatible header over either owned raw byte-character text or borrowed
// immutable source bytes. No conversion back to byte[] is needed for owned text.
// Default means empty; a supplied null string remains distinguishable from empty.
public struct ByteStringValue
{
    private object? owner;
    private int offset;
    public int Length { get; private set; }
    public readonly bool IsNull => owner is null && offset == -1;
    public readonly bool IsBytes => owner is byte[] || owner is null && offset != -1;
    public static ByteStringValue FromString(string? text) => new()
    {
        owner = text,
        offset = text is null ? -1 : 0,
        Length = text?.Length ?? 0
    };
    // Fixed Go concatenation computes its complete length before owning output.
    // Empty operands can reuse the sole nonempty header. Byte-role output needs
    // one exact array, without an intermediate builder or growth copies.
    internal static ByteStringValue Concat(params ReadOnlySpan<ByteStringValue> parts)
    {
        int count = 0;
        int length = 0;
        ByteStringValue single = default;
        bool needsLegacyText = false;
        foreach (var part in parts)
        {
            if (part.Length == 0) continue;
            count++;
            single = part;
            length = checked(length + part.Length);
            needsLegacyText |= !part.IsBytes && !GoString.IsByteText(part.CharSpan);
        }
        if (count <= 1) return single;
        if (needsLegacyText)
        {
            // Supplied non-byte UTF16 is an existing managed compatibility role;
            // preserve those characters rather than truncate or UTF8 encode them.
            var builder = new GoBuilder();
            builder.Grow(length);
            foreach (var part in parts) builder.WriteString(part);
            return builder.Value();
        }
        var bytes = new byte[length];
        int offset = 0;
        foreach (var part in parts)
        {
            var destination = bytes.AsSpan(offset, part.Length);
            if (part.IsBytes) part.ByteSpan.CopyTo(destination);
            else System.Text.Encoding.Latin1.GetBytes(part.CharSpan, destination);
            offset += part.Length;
        }
        return FromView(ByteStringView.FromOwnedBytes(bytes));
    }
    internal static ByteStringValue FromView(ByteStringView text)
    {
        if (!MemoryMarshal.TryGetArray(text.Memory, out var memory))
            throw new InvalidOperationException("Source text must have an immutable array owner");
        return new() { owner = memory.Array, offset = memory.Offset, Length = memory.Count };
    }
    public readonly ReadOnlySpan<byte> ByteSpan => owner is byte[] bytes
        ? bytes.AsSpan(offset, Length)
        : owner is null && !IsNull ? default
        : throw new InvalidOperationException("Text has a raw string owner");
    public readonly ReadOnlySpan<char> CharSpan => owner is string text
        ? text.AsSpan(text.Length == Length ? 0 : offset, Length)
        : owner is null ? default
        : throw new InvalidOperationException("Text has a byte owner");
    internal readonly ByteStringView ByteView => owner is byte[] bytes
        ? ByteStringView.FromOwnedBytes(bytes).Slice(offset, offset + Length)
        : owner is null && !IsNull ? default
        : throw new InvalidOperationException("Text has a raw string owner");
    public readonly string? Materialize()
    {
        if (IsNull) return null;
        if (owner is string text) return text.Length == Length ? text : text.Substring(offset, Length);
        return GoString.FromBytes(ByteSpan);
    }
    // Offset/length are immutable during read-only use. First string projection
    // atomically replaces only the owner reference with the complete projected
    // token. Its exact length makes the original offset no longer applicable.
    // Concurrent first readers obtain one canonical projected string. Public
    // setter mutation remains outside concurrent read-only compiler use.
    public string? Project()
    {
        while (true)
        {
            var current = Volatile.Read(ref owner);
            if (current is string text && text.Length == Length) return text;
            if (current is null && IsNull) return null;
            var projected = current is byte[] bytes
                ? GoString.FromBytes(bytes.AsSpan(offset, Length))
                : current is string source ? source.Substring(offset, Length) : string.Empty;
            if (ReferenceEquals(Interlocked.CompareExchange(ref owner, projected, current), current))
                return projected;
        }
    }
    // Like Go string slicing, both backing roles retain only a header. A public
    // projection replaces the owner with the complete sliced string; its length
    // then distinguishes that canonical owner from the original shared string.
    public ByteStringValue Slice(int start) => Slice(start, Length);
    public ByteStringValue Slice(int start, int end)
    {
        if (IsNull) throw new NullReferenceException();
        if (start < 0 || end < start || end > Length) throw new ArgumentOutOfRangeException(nameof(start));
        var current = Volatile.Read(ref owner);
        int sourceOffset = current is string text && text.Length == Length ? 0 : offset;
        return new() { owner = current, offset = sourceOffset + start, Length = end - start };
    }
    public readonly int this[int index] => IsBytes ? ByteSpan[index] : CharSpan[index];
    public ByteStringValue Snapshot() => new()
    {
        owner = Volatile.Read(ref owner),
        offset = offset,
        Length = Length
    };
    // Compatible Dictionary comparers borrow alternate keys. Caller-supplied
    // comparers without that optional interface use normal key lookup and retain
    // the actual stored key; string compatibility may then own a projection.
    // A rented long buffer never escapes.
    public readonly bool TryGetValue<T>(Dictionary<string, T> values,
        [MaybeNullWhen(false)] out string actualKey, [MaybeNullWhen(false)] out T value) =>
        TryGetStringValue(values, true, out actualKey, out value);
    private readonly bool TryGetStringValue<T>(Dictionary<string, T> values, bool needActualKey,
        [MaybeNullWhen(false)] out string actualKey, [MaybeNullWhen(false)] out T value)
    {
        if (IsNull)
        {
            actualKey = null!;
            return values.TryGetValue(null!, out value); // Original null-key failure.
        }
        if (!values.TryGetAlternateLookup<ReadOnlySpan<char>>(out var lookup))
        {
            string key = Materialize()!;
            if (values.TryGetValue(key, out value))
            {
                if (!needActualKey)
                {
                    actualKey = key;
                    return true;
                }
                foreach (string storedKey in values.Keys)
                    if (values.Comparer.Equals(key, storedKey))
                    {
                        actualKey = storedKey;
                        return true;
                    }
            }
            actualKey = null!;
            return false;
        }
        if (!IsBytes)
            return lookup.TryGetValue(CharSpan, out actualKey, out value);
        char[]? rented = null;
        Span<char> chars = Length <= 256 ? stackalloc char[Length]
            : (rented = System.Buffers.ArrayPool<char>.Shared.Rent(Length)).AsSpan(0, Length);
        try
        {
            var bytes = ByteSpan;
            System.Text.Encoding.Latin1.GetChars(bytes, chars);
            return lookup.TryGetValue(chars, out actualKey, out value);
        }
        finally
        {
            if (rented is not null) System.Buffers.ArrayPool<char>.Shared.Return(rented);
        }
    }
    public readonly bool TryGetValue<T>(Dictionary<string, T> values, [MaybeNullWhen(false)] out T value) =>
        TryGetStringValue(values, false, out _, out value);
    // Retained Go-byte keys use the same standard Dictionary alternate comparer
    // for lookup and insertion, with no widening/projection on a byte-owned hit.
    public readonly bool TryGetValue<T>(Dictionary<ByteStringKey, T> values,
        out ByteStringValue actualKey, [MaybeNullWhen(false)] out T value)
    {
        if (values.TryGetAlternateLookup<ByteStringValue>(out var lookup))
        {
            if (lookup.TryGetValue(this, out var storedKey, out value))
            {
                actualKey = storedKey.Value;
                return true;
            }
        }
        else
        {
            var key = ByteStringKey.FromValue(this);
            if (values.TryGetValue(key, out value))
                foreach (var storedKey in values.Keys)
                    if (values.Comparer.Equals(key, storedKey))
                    {
                        actualKey = storedKey.Value;
                        return true;
                    }
        }
        actualKey = default;
        return false;
    }
    public readonly bool TryGetValue<T>(Dictionary<ByteStringKey, T> values,
        [MaybeNullWhen(false)] out T value) =>
        values.TryGetAlternateLookup<ByteStringValue>(out var lookup)
            ? lookup.TryGetValue(this, out value)
            : values.TryGetValue(ByteStringKey.FromValue(this), out value);
    public readonly bool ContainsIn(HashSet<string> values)
    {
        if (IsNull) return values.Contains(null!);
        if (!values.TryGetAlternateLookup<ReadOnlySpan<char>>(out var lookup))
            return values.Contains(Materialize()!);
        if (!IsBytes) return lookup.Contains(CharSpan);
        char[]? rented = null;
        Span<char> chars = Length <= 256 ? stackalloc char[Length]
            : (rented = System.Buffers.ArrayPool<char>.Shared.Rent(Length)).AsSpan(0, Length);
        try { System.Text.Encoding.Latin1.GetChars(ByteSpan, chars); return lookup.Contains(chars); }
        finally { if (rented is not null) System.Buffers.ArrayPool<char>.Shared.Return(rented); }
    }
    public readonly bool ContainsIn(HashSet<ByteStringKey> values) =>
        values.TryGetAlternateLookup<ByteStringValue>(out var lookup)
            ? lookup.Contains(this)
            : values.Contains(ByteStringKey.FromValue(this));
    // Search the existing raw byte role; supplied UTF16 characters above FF
    // remain distinguishable instead of being truncated or UTF8 encoded.
    public readonly int IndexOf(int character)
    {
        if (IsNull) throw new NullReferenceException();
        return IsBytes ? (uint)character <= byte.MaxValue ? ByteSpan.IndexOf((byte)character) : -1
            : (uint)character <= char.MaxValue ? CharSpan.IndexOf((char)character) : -1;
    }
    public readonly int LastIndexOf(int character)
    {
        if (IsNull) throw new NullReferenceException();
        return IsBytes ? (uint)character <= byte.MaxValue ? ByteSpan.LastIndexOf((byte)character) : -1
            : (uint)character <= char.MaxValue ? CharSpan.LastIndexOf((char)character) : -1;
    }
    public int IndexOf(ByteStringValue value, int start) => IndexOfFrom(value, start);
    private int IndexOfFrom(ByteStringValue value, int start)
    {
        int found = Slice(start).IndexOf(value);
        return found < 0 ? -1 : start + found;
    }
    public int LastIndexOf(ByteStringValue value)
    {
        if (IsNull || value.IsNull) throw new NullReferenceException();
        if (IsBytes && value.IsBytes) return ByteSpan.LastIndexOf(value.ByteSpan);
        if (!IsBytes && !value.IsBytes) return CharSpan.LastIndexOf(value.CharSpan);
        if (value.Length == 0) return Length;
        for (int start = Length - value.Length; start >= 0; start--)
            if (Slice(start, start + value.Length).EqualsValue(value)) return start;
        return -1;
    }
    public ByteStringValue TrimEnd(int character)
    {
        if (IsNull) throw new NullReferenceException();
        int end = Length;
        while (end > 0 && this[end - 1] == character) end--;
        return Slice(0, end);
    }
    public readonly int IndexOfAny(int first, int second)
    {
        if (IsNull) throw new NullReferenceException();
        if (IsBytes)
        {
            if ((uint)first > byte.MaxValue) return IndexOf(second);
            if ((uint)second > byte.MaxValue) return IndexOf(first);
            return ByteSpan.IndexOfAny((byte)first, (byte)second);
        }
        if ((uint)first > char.MaxValue) return IndexOf(second);
        if ((uint)second > char.MaxValue) return IndexOf(first);
        return CharSpan.IndexOfAny((char)first, (char)second);
    }
    public readonly bool Contains(int character) => IndexOf(character) >= 0;
    public int IndexOf(ByteStringValue value)
    {
        if (IsNull || value.IsNull) throw new NullReferenceException();
        if (IsBytes && value.IsBytes) return ByteSpan.IndexOf(value.ByteSpan);
        if (!IsBytes && !value.IsBytes) return CharSpan.IndexOf(value.CharSpan);
        if (value.Length == 0) return 0;
        int limit = Length - value.Length;
        for (int start = 0; start <= limit;)
        {
            int found = Slice(start).IndexOf(value[0]);
            if (found < 0) return -1;
            start += found;
            if (start > limit) return -1;
            if (Slice(start, start + value.Length).EqualsValue(value)) return start;
            start++;
        }
        return -1;
    }
    public int IndexOf(ReadOnlySpan<byte> value)
    {
        if (IsNull) throw new NullReferenceException();
        if (IsBytes) return ByteSpan.IndexOf(value);
        if (value.Length == 0) return 0;
        int limit = Length - value.Length;
        for (int start = 0; start <= limit;)
        {
            int found = CharSpan.Slice(start).IndexOf((char)value[0]);
            if (found < 0) return -1;
            start += found;
            if (start > limit) return -1;
            if (Slice(start, start + value.Length).EqualsBytes(value)) return start;
            start++;
        }
        return -1;
    }
    public bool Contains(ReadOnlySpan<byte> value) => IndexOf(value) >= 0;

    public bool StartsWith(ReadOnlySpan<byte> prefix)
    {
        if (IsNull) throw new NullReferenceException();
        if (IsBytes) return ByteSpan.StartsWith(prefix);
        return prefix.Length <= Length && Slice(0, prefix.Length).EqualsBytes(prefix);
    }
    public bool EndsWith(ReadOnlySpan<byte> suffix)
    {
        if (IsNull) throw new NullReferenceException();
        if (IsBytes) return ByteSpan.EndsWith(suffix);
        return suffix.Length <= Length && Slice(Length - suffix.Length).EqualsBytes(suffix);
    }
    public bool StartsWith(ByteStringValue prefix)
    {
        if (IsNull || prefix.IsNull) throw new NullReferenceException();
        if (IsBytes && prefix.IsBytes) return ByteSpan.StartsWith(prefix.ByteSpan);
        if (!IsBytes && !prefix.IsBytes) return CharSpan.StartsWith(prefix.CharSpan);
        return prefix.Length <= Length && Slice(0, prefix.Length).EqualsValue(prefix);
    }
    public bool EndsWith(ByteStringValue suffix)
    {
        if (IsNull || suffix.IsNull) throw new NullReferenceException();
        if (IsBytes && suffix.IsBytes) return ByteSpan.EndsWith(suffix.ByteSpan);
        if (!IsBytes && !suffix.IsBytes) return CharSpan.EndsWith(suffix.CharSpan);
        return suffix.Length <= Length && Slice(Length - suffix.Length).EqualsValue(suffix);
    }
    public readonly bool EqualsBytes(ReadOnlySpan<byte> bytes)
    {
        if (IsNull || Length != bytes.Length) return false;
        if (IsBytes) return ByteSpan.SequenceEqual(bytes);
        var chars = CharSpan;
        for (int i = 0; i < bytes.Length; i++)
            if (chars[i] != bytes[i]) return false;
        return true;
    }
    public readonly bool EqualsString(string? text) => EqualsValue(FromString(text));
    public readonly bool EqualsValue(ByteStringValue text)
    {
        if (IsNull || text.IsNull) return IsNull && text.IsNull;
        if (Length != text.Length) return false;
        if (IsBytes && text.IsBytes) return ByteSpan.SequenceEqual(text.ByteSpan);
        if (!IsBytes && !text.IsBytes) return CharSpan.SequenceEqual(text.CharSpan);
        return IsBytes ? text.EqualsBytes(ByteSpan) : EqualsBytes(text.ByteSpan);
    }
}
