using TypeScript.Foundation;
using Xunit;

namespace TypeScript.Scanner.ParserTests;
public sealed class FoundationSyntaxServiceTests
{
    public static IEnumerable<object[]> Cases() => SyntaxServiceChecks.Cases.Select(name => new object[] { name });
    [Theory]
    [MemberData(nameof(Cases))]
    public void MatchesOriginalSyntaxServiceObservations(string name) => SyntaxServiceChecks.Run(name);
}
