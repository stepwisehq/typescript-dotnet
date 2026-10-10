using TypeScript.Foundation;
using Xunit;

namespace TypeScript.Scanner.ParserTests;

public sealed class ListValueRangeTests
{
    public static IEnumerable<object[]> Cases() => ListValueRangeChecks.Cases.Select(name => new object[] { name });
    [Theory]
    [MemberData(nameof(Cases))]
    public void PreserveExistingEnumerationBehavior(string name) => ListValueRangeChecks.Run(name);
}
