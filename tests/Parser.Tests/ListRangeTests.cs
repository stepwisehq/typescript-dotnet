using TypeScript.Foundation;
using Xunit;

namespace TypeScript.Scanner.ParserTests;

public sealed class ListRangeTests
{
    public static IEnumerable<object[]> Cases() => ListRangeChecks.Cases.Select(name => new object[] { name });
    [Theory]
    [MemberData(nameof(Cases))]
    public void PreserveManagedRangeBehavior(string name) => ListRangeChecks.Run(name);
}
