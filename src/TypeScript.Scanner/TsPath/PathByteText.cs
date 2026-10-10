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
    public static string ToUnicode(string bytes) =>
        bytes is not null && Ascii.IsValid(bytes.AsSpan()) ? bytes : ScalarUtf8.GetString(GoString.Bytes(bytes!));
}
