using TypeScript.Foundation;
using Xunit;

namespace TypeScript.Scanner.ParserTests;
public sealed class FoundationCloneGraphTests
{
    public static IEnumerable<object[]> Cases() => CloneGraphChecks.Cases.Select(name => new object[] { name });
    [Theory]
    [MemberData(nameof(Cases))]
    public void MatchesFullOriginalCloneGraphs(string name) => CloneGraphChecks.Run(name);
}
