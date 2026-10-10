// New managed compatibility support for immutable Go string headers.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
using System.Runtime.InteropServices;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace TypeScript.Scanner;

// One heap-compatible header over either owned raw byte-character text or borrowed
// immutable source bytes. No conversion back to byte[] is needed for owned text.
// Default means empty; a supplied null string remains distinguishable from empty.
internal struct ByteStringValue
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
    public static ByteStringValue FromView(ByteStringView text)
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
        ? text.AsSpan()
        : owner is null ? default
        : throw new InvalidOperationException("Text has a byte owner");
    public readonly ByteStringView ByteView => owner is byte[] bytes
        ? ByteStringView.FromOwnedBytes(bytes).Slice(offset, offset + Length)
        : owner is null && !IsNull ? default
        : throw new InvalidOperationException("Text has a raw string owner");
    public readonly string? Materialize()
    {
        if (IsNull) return null;
        if (owner is string text) return text;
        return GoString.FromBytes(ByteSpan);
    }
    // Offset/length are immutable during read-only use. First string projection
    // atomically replaces only the owner reference; string owners always contain
    // the full token, so the original byte offset is no longer interpreted.
    // Concurrent first readers obtain one canonical projected string. Public
    // setter mutation remains outside concurrent read-only compiler use.
    public string? Project()
    {
        var current = Volatile.Read(ref owner);
        if (current is string text) return text;
        if (current is null && IsNull) return null;
        var projected = current is byte[] bytes
            ? GoString.FromBytes(bytes.AsSpan(offset, Length)) : string.Empty;
        var prior = Interlocked.CompareExchange(ref owner, projected, current);
        return prior as string ?? projected;
    }
    public ByteStringValue Snapshot() => new()
    {
        owner = Volatile.Read(ref owner),
        offset = offset,
        Length = Length
    };
    // Ordinal Dictionary lookup adapts byte characters to the standard library
    // alternate comparer. No string is owned on a hit; persistent inserted keys
    // retain the existing string contract. A rented long buffer never escapes.
    public readonly bool TryGetValue<T>(Dictionary<string, T> values,
        [MaybeNullWhen(false)] out string actualKey, [MaybeNullWhen(false)] out T value)
    {
        if (IsNull)
        {
            actualKey = null!;
            return values.TryGetValue(null!, out value); // Original null-key failure.
        }
        var lookup = values.GetAlternateLookup<ReadOnlySpan<char>>();
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
        TryGetValue(values, out _, out value);
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
