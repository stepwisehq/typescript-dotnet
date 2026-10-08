using TypeScript.Foundation;
using Xunit;

namespace TypeScript.Scanner.ParserTests;
public sealed class FoundationFuzzTests
{
    public static IEnumerable<object[]> Cases() => ParserFuzzChecks.Cases();
    [Theory]
    [MemberData(nameof(Cases))]
    public void PreservesOriginalFiniteFuzzParserBody(string name, string extension, string sourceBase64, bool jsx, bool force, int expectedKind) => Assert.True(ParserFuzzChecks.Run(name, extension, sourceBase64, jsx, force, expectedKind), "Unsupported original extension must be recorded as skipped, not passed");
}
