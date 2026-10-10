using TypeScript.Foundation;
using Xunit;

namespace TypeScript.Scanner.ParserTests;

public sealed class ListAppendTests
{
    public static IEnumerable<object[]> Cases() => ListAppendChecks.Cases.Select(name => new object[] { name });
    [Theory]
    [MemberData(nameof(Cases))]
    public void PreserveManagedAppendBehavior(string name) => ListAppendChecks.Run(name);
}
