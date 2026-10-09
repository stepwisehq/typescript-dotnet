using Xunit;

namespace TypeScript.Scanner.Tests;

public class UtilityTests
{
    [Fact]
    public void PublicValueGetterPreservesIsolatedJavaScriptSurrogates()
    {
        var scanner = new Scanner();
        scanner.SetText("\"\\uD800x\\uDC00\\uD83E\\uDD80\"");
        Assert.Equal(Kind.StringLiteral, scanner.Scan());
        Assert.Equal("\uD800x\uDC00🦀", scanner.TokenValue());
        Assert.Equal(Convert.FromHexString("EDA08078EDB080F09FA680"), scanner.TokenValueBytes());
    }

    [Fact]
    public void DiagnosticArgumentsUseUnicodeText()
    {
        var scanner = new Scanner();
        var arguments = new List<string>();
        scanner.SetOnError((_, _, _, args) => arguments.AddRange(args.OfType<string>()));
        scanner.SetText(@"/(?<é>x)\k<É>/u");
        Assert.Equal(Kind.SlashToken, scanner.Scan());
        Assert.Equal(Kind.RegularExpressionLiteral, scanner.ReScanSlashToken(true));
        Assert.Equal(new[] { "É", "é" }, arguments);
    }

    [Fact]
    public void ResetRestoresScannerDefaultsAndClearsDirectives()
    {
        var scanner = new Scanner();
        scanner.SetSkipTrivia(false);
        scanner.SetScriptTarget(ScriptTarget.ES5);
        scanner.SetLanguageVariant(LanguageVariant.JSX);
        scanner.SetSkipJSDocLeadingAsterisks(true);
        scanner.SetText("// @ts-ignore\nvalue");
        Assert.Equal(Kind.SingleLineCommentTrivia, scanner.Scan());
        Assert.Single(Assert.IsType<CommentDirective[]>(scanner.CommentDirectives()));
        scanner.SetOnError((_, _, _, _) => throw new InvalidOperationException("Reset must remove the callback"));
        scanner.Reset();
        Assert.Empty(scanner.TextBytes());
        Assert.Null(scanner.CommentDirectives());
        Assert.Equal(Kind.Unknown, scanner.Token());
        scanner.SetText("  \"\\8\"");
        Assert.Equal(Kind.StringLiteral, scanner.Scan());
        Assert.Equal(2, scanner.TokenStart());
    }

    [Fact]
    public void RewindRestoresDirectiveAndJsDocState()
    {
        var scanner = new Scanner();
        scanner.SetText("first\n// @ts-ignore\nsecond");
        Assert.Equal(Kind.Identifier, scanner.Scan());
        ScannerState mark = scanner.Mark();
        Assert.Equal(Kind.Identifier, scanner.Scan());
        Assert.Single(Assert.IsType<CommentDirective[]>(scanner.CommentDirectives()));
        scanner.Rewind(mark);
        Assert.Equal("first", scanner.TokenValue());
        Assert.Null(scanner.CommentDirectives());
        Assert.Equal(Kind.Identifier, scanner.Scan());
        Assert.Equal("second", scanner.TokenValue());
        Assert.Single(Assert.IsType<CommentDirective[]>(scanner.CommentDirectives()));
    }

    [Fact]
    public void DirectiveStatePreservesAbsenceAcrossTextReuseAndSavedPresentState()
    {
        var scanner = new Scanner();
        Assert.Null(scanner.CommentDirectives());
        scanner.SetText("// @ts-ignore\nfirst\n// @ts-expect-error\nsecond");
        Assert.Null(scanner.CommentDirectives());
        Assert.Equal(Kind.Identifier, scanner.Scan());
        ScannerState present = scanner.Mark();
        Assert.Single(Assert.IsType<CommentDirective[]>(scanner.CommentDirectives()));
        Assert.Equal(Kind.Identifier, scanner.Scan());
        Assert.Equal(2, scanner.CommentDirectives()?.Length);
        scanner.Rewind(present);
        Assert.Single(Assert.IsType<CommentDirective[]>(scanner.CommentDirectives()));
        Assert.Equal(Kind.Identifier, scanner.Scan());
        Assert.Equal(2, scanner.CommentDirectives()?.Length);
        scanner.SetText("plain");
        Assert.Null(scanner.CommentDirectives());
        Assert.Equal(Kind.Identifier, scanner.Scan());
        Assert.Null(scanner.CommentDirectives());
    }

    [Fact]
    public void TriviaOptionsRespectBytePositionsAndCommentBoundaries()
    {
        const string text = " \t/*a*/\r\n * value";
        Assert.Equal(2, Scanner.SkipTriviaEx(text, 0, new SkipTriviaOptions { StopAtComments = true }));
        Assert.Equal(9, Scanner.SkipTriviaEx(text, 0, new SkipTriviaOptions { StopAfterLineBreak = true }));
        Assert.Equal(10, Scanner.SkipTrivia(text, 0));
        Assert.Equal(12, Scanner.SkipTriviaEx(text, 0, new SkipTriviaOptions { InJSDoc = true }));
        Assert.Equal(-1, Scanner.SkipTrivia(text, -1));
    }

    [Fact]
    public void CommentRangesPreserveByteOffsetsAndTrailingLineBreaks()
    {
        const string text = "// α\r\n/*b*/ value";
        var comments = Scanner.GetLeadingCommentRanges(text, 0).ToArray();
        Assert.Equal(new[] {
            new CommentRange(Kind.SingleLineCommentTrivia, 0, 5, true),
            new CommentRange(Kind.MultiLineCommentTrivia, 7, 12, false)
        }, comments);
        Assert.Equal(new[] { comments[0] }, Scanner.GetTrailingCommentRanges(text, 0));
    }

    [Fact]
    public void Utf16PositionsConsumeWholeUtf8CharactersLikeGo()
    {
        int[] lines = [0, 7];
        const string text = "α🦀\nnext";
        Assert.Equal(2, Scanner.ComputePositionOfLineAndUTF16Character(lines, 0, 1, text, false));
        Assert.Equal(6, Scanner.ComputePositionOfLineAndUTF16Character(lines, 0, 2, text, false));
        Assert.Equal(6, Scanner.ComputePositionOfLineAndUTF16Character(lines, 0, 3, text, false));
        Assert.Equal(7, Scanner.ComputePositionOfLineAndUTF16Character(lines, 0, 4, text, false));
        Assert.Equal(0, Scanner.ComputeLineOfPosition(lines, 6));
        Assert.Equal(1, Scanner.ComputeLineOfPosition(lines, 7));
        Assert.Equal(11, Scanner.ComputePositionOfLineAndByteOffset(lines, 1, 4));
        Assert.Throws<InvalidOperationException>(() => Scanner.ComputePositionOfLineAndUTF16Character(lines, 0, 5, text, false));
        Assert.Equal(7, Scanner.ComputePositionOfLineAndUTF16Character(lines, 0, 5, text, true));
    }

    [Fact]
    public void MalformedUtf8CountsOneCharacterPerInvalidByte()
    {
        byte[] text = [0xE2, 0x82, (byte)'x'];
        Assert.Equal(1, Scanner.ComputePositionOfLineAndUTF16Character([0], 0, 1, text, false));
        Assert.Equal(2, Scanner.ComputePositionOfLineAndUTF16Character([0], 0, 2, text, false));
        Assert.False(Scanner.IsValidIdentifier(text));
        Assert.True(Scanner.IsValidIdentifier("αβ"));
        Assert.False(Scanner.IsValidIdentifier("α-β"));
    }
}
