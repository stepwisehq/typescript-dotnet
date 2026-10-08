using TypeScript.Foundation;
using Xunit;
namespace TypeScript.Scanner.ParserTests;
public sealed class FoundationDirectTests
{
    public static IEnumerable<object[]> Cases() => ParserAstChecks.Cases.Select(name => new object[] { name });
    [Theory]
    [MemberData(nameof(Cases))]
    public void PreservesOriginalFoundationAssertions(string name) => ParserAstChecks.Run(name);
}
