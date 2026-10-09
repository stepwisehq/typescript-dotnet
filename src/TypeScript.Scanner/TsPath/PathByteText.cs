// Authored explicit Unicode/Go byte-string API boundary. Pure paths never decode.
using System.Text;

namespace TypeScript.Scanner;
public static class PathByteText
{
    private static readonly UTF8Encoding ScalarUtf8 = new(false, true);
    public static string FromUnicode(string text) => GoString.FromBytes(ScalarUtf8.GetBytes(text));
    public static string ToUnicode(string bytes) => ScalarUtf8.GetString(GoString.Bytes(bytes));
}
