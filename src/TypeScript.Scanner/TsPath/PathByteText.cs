// Authored explicit Unicode/Go byte-string API boundary. Pure paths never decode.
using System.Text;

namespace TypeScript.Scanner;
public static class PathByteText
{
    private static readonly UTF8Encoding ScalarUtf8 = new(false, true);
    // ASCII has identical scalar and raw-byte spellings. Non-ASCII and null
    // retain the existing strict conversion and exception boundary.
    public static string FromUnicode(string text) =>
    text is not null && Ascii.IsValid(text.AsSpan()) ? text : GoString.FromBytes(ScalarUtf8.GetBytes(text!));
    internal static ByteStringValue FromUnicodeValue(string text) =>
    text is not null && Ascii.IsValid(text.AsSpan()) ? ByteStringValue.FromString(text)
    : ByteStringValue.FromView(ByteStringView.FromOwnedBytes(ScalarUtf8.GetBytes(text!)));
    public static string ToUnicode(ByteStringValue bytes)
    {
        if (bytes.IsNull)
        {
            throw new ArgumentNullException(nameof(bytes));
        }
        if (bytes.IsBytes)
        {
            return ScalarUtf8.GetString(bytes.ByteSpan);
        }
        if (Ascii.IsValid(bytes.CharSpan))
        {
            return bytes.Materialize()!;
        }
        byte[]? rented = null;
        Span<byte> buffer = bytes.Length <= 256 ? stackalloc byte[bytes.Length]
        : (rented = System.Buffers.ArrayPool<byte>.Shared.Rent(bytes.Length)).AsSpan(0, bytes.Length);
        try
        {
            System.Text.Encoding.Latin1.GetBytes(bytes.CharSpan, buffer);
            return ScalarUtf8.GetString(buffer);
        }
        finally
        {
            if (rented is not null)
            {
                System.Buffers.ArrayPool<byte>.Shared.Return(rented);
            }
        }
    }
    public static string ToUnicode(string bytes) =>
    bytes is not null && Ascii.IsValid(bytes.AsSpan()) ? bytes : ScalarUtf8.GetString(GoString.Bytes(bytes!));
}
