// New managed compatibility support for immutable Go string map keys.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
using System.Buffers;
using System.IO.Hashing;
using System.Text;

namespace TypeScript.Scanner;

// Go map keys retain a string header, not an eagerly projected managed string.
// Snapshots never call Project: changing a node's projection or Name cannot
// change a retained key. Public byte input is copied; internal source owners are
// already immutable. Dictionary remains the standard .NET implementation.
public readonly struct ByteStringKey : IEquatable<ByteStringKey>, IComparable<ByteStringKey>
{
    private readonly ByteStringValue value;
    private ByteStringKey(ByteStringValue value) => this.value = value.Snapshot();
    internal ByteStringValue Value => value;
    public static ByteStringKey FromString(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        return new(ByteStringValue.FromString(text));
    }
    public static ByteStringKey FromBytes(byte[] bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        return new(ByteStringValue.FromView(ByteStringView.CopyFrom(bytes)));
    }
    internal static ByteStringKey FromValue(ByteStringValue key)
    {
        if (key.IsNull) throw new ArgumentNullException(nameof(key));
        return new(key);
    }
    public static IEqualityComparer<ByteStringKey> Comparer => KeyComparer.Instance;
    public bool Equals(ByteStringKey other) => value.EqualsValue(other.value);
    public override bool Equals(object? obj) => obj is ByteStringKey key && Equals(key);
    public override int GetHashCode() => ContentHash(value);
    public static bool operator ==(ByteStringKey left, ByteStringKey right) => left.Equals(right);
    public static bool operator !=(ByteStringKey left, ByteStringKey right) => !left.Equals(right);
    public override string ToString() => value.Materialize()!;
    public int CompareTo(ByteStringKey other)
    {
        if (value.IsBytes && other.value.IsBytes)
            return value.ByteSpan.SequenceCompareTo(other.value.ByteSpan);
        if (!value.IsBytes && !other.value.IsBytes)
            return value.CharSpan.SequenceCompareTo(other.value.CharSpan);
        int count = Math.Min(value.Length, other.value.Length);
        for (int i = 0; i < count; i++)
        {
            int left = value.IsBytes ? value.ByteSpan[i] : value.CharSpan[i];
            int right = other.value.IsBytes ? other.value.ByteSpan[i] : other.value.CharSpan[i];
            if (left != right) return left - right;
        }
        return value.Length.CompareTo(other.value.Length);
    }

    // CLR map implementation adaptation: standard XXH3 truncated to Dictionary's
    // int hash replaces XXH32, without changing logical Go byte keys or equality.
    // Go's runtime string-map hash is not XXH3; these hash bits are not compiler
    // cache identities. Retained and alternate keys share this exact operation.
    // A static standard hash has no per-lookup state object. Latin1 represents
    // the raw byte-character compatibility owner exactly, never UTF8/WTF8-decodes
    // it. Non-byte public strings cannot equal byte owners and retain ordinal
    // string hashing instead of replacement/truncation. Hash collisions are
    // still checked with exact content equality by Dictionary.
    internal static int ContentHash(ByteStringValue key)
    {
        if (key.IsNull) throw new ArgumentNullException(nameof(key));
        if (key.IsBytes) return unchecked((int)XxHash3.HashToUInt64(key.ByteSpan));
        var chars = key.CharSpan;
        if (!GoString.IsByteText(chars))
            return string.GetHashCode(chars, StringComparison.Ordinal);
        byte[]? rented = null;
        Span<byte> bytes = chars.Length <= 256 ? stackalloc byte[chars.Length]
            : (rented = ArrayPool<byte>.Shared.Rent(chars.Length)).AsSpan(0, chars.Length);
        try
        {
            Encoding.Latin1.GetBytes(chars, bytes);
            return unchecked((int)XxHash3.HashToUInt64(bytes));
        }
        finally
        {
            if (rented is not null) ArrayPool<byte>.Shared.Return(rented);
        }
    }

    private sealed class KeyComparer : IEqualityComparer<ByteStringKey>,
        IAlternateEqualityComparer<ByteStringValue, ByteStringKey>
    {
        internal static readonly KeyComparer Instance = new();
        public bool Equals(ByteStringKey left, ByteStringKey right) => left.Equals(right);
        public int GetHashCode(ByteStringKey key) => key.GetHashCode();
        public bool Equals(ByteStringValue alternate, ByteStringKey key) => alternate.EqualsValue(key.value);
        public int GetHashCode(ByteStringValue alternate) => ContentHash(alternate);
        public ByteStringKey Create(ByteStringValue alternate) => FromValue(alternate);
    }
}
