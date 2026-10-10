using TypeScript.Foundation;
using Xunit;

namespace TypeScript.Scanner.ParserTests;

public sealed class KeyBuilderTests
{
    public static IEnumerable<object[]> Cases() => KeyBuilderChecks.Cases.Select(name => new object[] { name });
    [Theory]
    [MemberData(nameof(Cases))]
    public void MatchesPinnedGoCacheKeyHash(string name) => KeyBuilderChecks.Run(name);
}
