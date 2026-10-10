// New managed compatibility support; not an upstream source port.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
#nullable enable
namespace TypeScript.Scanner;

// Go string slicing shares immutable storage. The backing is privately owned UTF8/WTF8 or arbitrary bytes; offsets remain
// bytes, not Unicode scalar positions. FromOwnedBytes transfers immutable ownership.
internal readonly struct ByteStringView
{
    private readonly byte[]? backing;
    private readonly int offset;
    public int Length { get; }
    public bool IsDefault => backing is null;
    public ReadOnlyMemory<byte> Memory => backing.AsMemory(offset, Length);

    private ByteStringView(byte[]? backing, int offset, int length) =>
        (this.backing, this.offset, Length) = (backing, offset, length);

    // Legacy raw Go strings are an explicit compatibility ingress. Source-file
    // loading/parser ownership uses byte arrays directly, without this projection.
    public static implicit operator ByteStringView(string? text) => FromGoBytes(text);
    internal static ByteStringView FromGoBytes(string? text) => text is null
        ? default : FromOwnedBytes(text.Length == 0 ? Array.Empty<byte>() : GoString.Bytes(text));
    internal static ByteStringView FromUnicode(string text) => FromOwnedBytes(System.Text.Encoding.UTF8.GetBytes(text));
    internal static ByteStringView FromOwnedBytes(byte[]? bytes) => new(bytes, 0, bytes?.Length ?? 0);
    internal static ByteStringView CopyFrom(ReadOnlySpan<byte> bytes) => FromOwnedBytes(bytes.ToArray());

    public char this[int index]
    {
        get
        {
            if ((uint)index >= (uint)Length)
                throw new ArgumentOutOfRangeException(nameof(index));
            return (char)backing![offset + index];
        }
    }

    public ReadOnlySpan<byte> AsSpan() => backing.AsSpan(offset, Length);
    public ReadOnlySpan<byte> AsSpan(int start) => Slice(start).AsSpan();
    public ReadOnlySpan<byte> AsSpan(int start, int length) =>
        AsSpan().Slice(start, length);

    public ByteStringView Slice(int start) => Slice(start, Length);
    public ByteStringView Slice(int start, int end)
    {
        if (start < 0 || end < start || end > Length)
            throw new ArgumentOutOfRangeException(nameof(start));
        return new(backing, offset + start, end - start);
    }

    // Materialization is intentional at persistent token/AST/public boundaries.
    // Raw-string compatibility projects Latin1 byte characters; byte source
    // consumers use spans instead. Unicode decoding is a separate explicit API.
    public string Materialize() => GoString.FromBytes(AsSpan());
    internal string DecodeUnicode() => System.Text.Encoding.UTF8.GetString(AsSpan());
    internal byte[] ToArray() => AsSpan().ToArray();
    public string Substring(int start, int length)
    {
        if ((uint)start > (uint)Length || (uint)length > (uint)(Length - start))
            throw new ArgumentOutOfRangeException(nameof(start));
        return Slice(start, start + length).Materialize();
    }
}
