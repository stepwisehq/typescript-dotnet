using TypeScript.Foundation;
using Xunit;

namespace TypeScript.Scanner.ParserTests;

public sealed class ParserPoolTests
{
    public static IEnumerable<object[]> Cases => ParserPoolChecks.Cases.Select(name => new object[] { name });

    [Theory]
    [MemberData(nameof(Cases))]
    public void PreservePublishedResultsAcrossReusableParserRequests(string name) => ParserPoolChecks.Run(name);
}
