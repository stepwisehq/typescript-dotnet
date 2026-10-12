// Exact pinned printer/utilities_test.go tables and assertions; Go byte-string boundary retained.
#nullable disable
using TypeScript.Scanner;
using TypeScript.DiagnosticPrinting;
namespace Emit.Checks;
public static class PrinterUtilityChecks
{
    public static readonly string[] Names = ["TestEscapeString", "TestEscapeNonAsciiString", "TestEscapeJsxAttributeString", "TestIsRecognizedTripleSlashComment"];
    public static void Run(string name) { switch(name) { case "TestEscapeString": TestEscapeString(); break; case "TestEscapeNonAsciiString": TestEscapeNonAsciiString(); break; case "TestEscapeJsxAttributeString": TestEscapeJsxAttributeString(); break; case "TestIsRecognizedTripleSlashComment": TestIsRecognizedTripleSlashComment(); break; default: throw new ArgumentException(name); } }
    public static int RunAll() { foreach(var name in Names) Run(name); return Names.Length; }
    private static void Equal<T>(T expected, T actual) { if (!EqualityComparer<T>.Default.Equals(expected,actual)) throw new InvalidOperationException($"Original utility assertion: expected {expected}, actual {actual}"); }
    private readonly record struct EscapeCase(string s, QuoteChar quoteChar,string expected);
    private readonly record struct TripleCase(string s,Kind kind,bool expected);
    private static void TestEscapeString()
    {
        EscapeCase[] data = [
            new("", QuoteChar.DoubleQuote, ""),
            new("abc", QuoteChar.DoubleQuote, "abc"),
            new("ab\"c", QuoteChar.DoubleQuote, "ab\\\"c"),
            new("ab\tc", QuoteChar.DoubleQuote, "ab\\tc"),
            new("ab\nc", QuoteChar.DoubleQuote, "ab\\nc"),
            new("ab'c", QuoteChar.DoubleQuote, "ab'c"),
            new("ab'c", QuoteChar.SingleQuote, "ab\\'c"),
            new("ab\"c", QuoteChar.SingleQuote, "ab\"c"),
            new("ab`c", QuoteChar.Backtick, "ab\\`c"),
            new("\u001f", QuoteChar.Backtick, "\\u001F"),
        ];
        foreach(var rec in data) Equal(GoString.ToUtf8(rec.expected), PrinterUtilities.EscapeString(GoString.ToUtf8(rec.s),rec.quoteChar));
    }
    private static void TestEscapeNonAsciiString()
    {
        EscapeCase[] data = [
            new("", QuoteChar.DoubleQuote, ""),
            new("abc", QuoteChar.DoubleQuote, "abc"),
            new("ab\"c", QuoteChar.DoubleQuote, "ab\\\"c"),
            new("ab\tc", QuoteChar.DoubleQuote, "ab\\tc"),
            new("ab\nc", QuoteChar.DoubleQuote, "ab\\nc"),
            new("ab'c", QuoteChar.DoubleQuote, "ab'c"),
            new("ab'c", QuoteChar.SingleQuote, "ab\\'c"),
            new("ab\"c", QuoteChar.SingleQuote, "ab\"c"),
            new("ab`c", QuoteChar.Backtick, "ab\\`c"),
            new("ab\u008fc", QuoteChar.DoubleQuote, "ab\\u008Fc"),
            new("\ud835\udfd8\ud835\udfd9", QuoteChar.DoubleQuote, "\\uD835\\uDFD8\\uD835\\uDFD9"),
        ];
        foreach(var rec in data) Equal(GoString.ToUtf8(rec.expected), PrinterUtilities.EscapeNonAsciiString(GoString.ToUtf8(rec.s),rec.quoteChar));
    }
    private static void TestEscapeJsxAttributeString()
    {
        EscapeCase[] data = [
            new("", QuoteChar.DoubleQuote, ""),
            new("abc", QuoteChar.DoubleQuote, "abc"),
            new("ab\"c", QuoteChar.DoubleQuote, "ab&quot;c"),
            new("ab\tc", QuoteChar.DoubleQuote, "ab&#x9;c"),
            new("ab\nc", QuoteChar.DoubleQuote, "ab&#xA;c"),
            new("ab'c", QuoteChar.DoubleQuote, "ab'c"),
            new("ab'c", QuoteChar.SingleQuote, "ab&apos;c"),
            new("ab\"c", QuoteChar.SingleQuote, "ab\"c"),
            new("ab\u008fc", QuoteChar.DoubleQuote, "ab\u008fc"),
            new("\ud835\udfd8\ud835\udfd9", QuoteChar.DoubleQuote, "\ud835\udfd8\ud835\udfd9"),
        ];
        foreach(var rec in data) Equal(GoString.ToUtf8(rec.expected), PrinterUtilities.EscapeJsxAttributeString(GoString.ToUtf8(rec.s),rec.quoteChar));
    }
    private static void TestIsRecognizedTripleSlashComment()
    {
        TripleCase[] data = [
            new("", Kind.MultiLineCommentTrivia, false),
            new("", Kind.SingleLineCommentTrivia, false),
            new("/a", Kind.Unknown, false),
            new("//", Kind.Unknown, false),
            new("//a", Kind.Unknown, false),
            new("///", Kind.Unknown, false),
            new("///a", Kind.Unknown, false),
            new("///<reference path=\"foo\" />", Kind.Unknown, true),
            new("///<reference types=\"foo\" />", Kind.Unknown, true),
            new("///<reference lib=\"foo\" />", Kind.Unknown, true),
            new("///<reference no-default-lib=\"foo\" />", Kind.Unknown, true),
            new("///<amd-dependency path=\"foo\" />", Kind.Unknown, true),
            new("///<amd-module />", Kind.Unknown, true),
            new("/// <reference path=\"foo\" />", Kind.Unknown, true),
            new("/// <reference types=\"foo\" />", Kind.Unknown, true),
            new("/// <reference lib=\"foo\" />", Kind.Unknown, true),
            new("/// <reference no-default-lib=\"foo\" />", Kind.Unknown, true),
            new("/// <amd-dependency path=\"foo\" />", Kind.Unknown, true),
            new("/// <amd-module />", Kind.Unknown, true),
            new("/// <reference path=\"foo\"/>", Kind.Unknown, true),
            new("/// <reference types=\"foo\"/>", Kind.Unknown, true),
            new("/// <reference lib=\"foo\"/>", Kind.Unknown, true),
            new("/// <reference no-default-lib=\"foo\"/>", Kind.Unknown, true),
            new("/// <amd-dependency path=\"foo\"/>", Kind.Unknown, true),
            new("/// <amd-module/>", Kind.Unknown, true),
            new("/// <reference path='foo' />", Kind.Unknown, true),
            new("/// <reference types='foo' />", Kind.Unknown, true),
            new("/// <reference lib='foo' />", Kind.Unknown, true),
            new("/// <reference no-default-lib='foo' />", Kind.Unknown, true),
            new("/// <amd-dependency path='foo' />", Kind.Unknown, true),
            new("/// <reference path=\"foo\" />  ", Kind.Unknown, true),
            new("/// <reference types=\"foo\" />  ", Kind.Unknown, true),
            new("/// <reference lib=\"foo\" />  ", Kind.Unknown, true),
            new("/// <reference no-default-lib=\"foo\" />  ", Kind.Unknown, true),
            new("/// <amd-dependency path=\"foo\" />  ", Kind.Unknown, true),
            new("/// <amd-module />  ", Kind.Unknown, true),
            new("/// <foo />", Kind.Unknown, false),
            new("/// <reference />", Kind.Unknown, false),
            new("/// <amd-dependency />", Kind.Unknown, false),
        ];
        foreach(var rec in data)
        {
            var text = GoString.ToUtf8(rec.s);
            var range = new CommentRange(rec.kind,0,0,false);
            if(range.Kind == Kind.Unknown) range = new CommentRange(Kind.SingleLineCommentTrivia,0,text.Length,false);
            Equal(rec.expected,PrinterUtilities.IsRecognizedTripleSlashComment(text,range));
        }
    }
    public static int RunAuthoredControls()
    {
        // Pinned escapeStringWorker uses \x00 before a decimal digit to avoid octal parsing.
        Equal("\\x001", PrinterUtilities.EscapeString("\0" + "1", QuoteChar.DoubleQuote));
        return 1;
    }
}
