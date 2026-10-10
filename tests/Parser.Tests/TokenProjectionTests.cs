using TypeScript.Foundation;
using Xunit;

namespace TypeScript.Scanner.ParserTests;

public sealed class TokenProjectionTests
{
    public static IEnumerable<object[]> Cases() => TokenProjectionChecks.Cases.Select(name => new object[] { name });
    [Theory]
    [MemberData(nameof(Cases))]
    public void PreserveTokenAndAstText(string name) => TokenProjectionChecks.Run(name);
}
