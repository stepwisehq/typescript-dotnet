using Xunit;

namespace TypeScript.Scanner.ParserTests;

// Authored managed raw-byte ingress controls, not original upstream test bodies.
public sealed class PathKeyByteValidationTests
{
    [Fact]
    public void AcceptEveryByteWithoutChangingTheString()
    {
        var raw = new string(Enumerable.Range(0, 256).Select(value => (char)value).ToArray());
        Assert.Same(raw, PathKey.FromGoString(raw).AsString());
        Assert.Equal(default(PathKey), PathKey.FromGoString(""));
        // Invalid UTF8 and WTF8 are still valid raw byte strings at this boundary.
        var malformed = "/\xff\xed\xa0\x80";
        Assert.Same(malformed, PathKey.FromGoString(malformed).AsString());
    }

    [Fact]
    public void RejectScalarCharactersOutsideTheByteRange()
    {
        foreach (var text in new[] { "\u0100", "/a\u0100", "\ud800", "\udfff", "\U0001f980", "\uffff" })
        {
            var error = Assert.Throws<ArgumentException>(() => PathKey.FromGoString(text));
            Assert.Equal("bytes", error.ParamName);
            Assert.StartsWith("Go byte strings require one managed character per byte.", error.Message);
        }
        Assert.Equal("bytes", Assert.Throws<ArgumentNullException>(() => PathKey.FromGoString(null!)).ParamName);
    }

    [Fact]
    public void KeepTheExplicitUnicodeBoundaryAndCaseCanonicalization()
    {
        var key = PathKey.FromUnicode("/A/é/🦀.ts");
        Assert.Equal("/A/\xc3\xa9/\xf0\x9f\xa6\x80.ts", key.AsString());
        Assert.Equal("/A/é/🦀.ts", key.ToUnicode());
        Assert.Equal("/a/é/🦀.ts", key.CaseInsensitiveKey().ToUnicode());
        Assert.Equal(key, PathKey.FromGoString(key.AsString()));
    }
}
