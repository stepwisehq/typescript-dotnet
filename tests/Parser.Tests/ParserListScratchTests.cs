using TypeScript.Foundation;
using Xunit;

namespace TypeScript.Scanner.ParserTests;

public sealed class ParserListScratchTests
{
    public static IEnumerable<object[]> Cases() => ParserListScratchChecks.Cases.Select(name => new object[] { name });
    [Theory]
    [MemberData(nameof(Cases))]
    public void PreservePublishedListsDuringNestedParsing(string name) => ParserListScratchChecks.Run(name);
}
