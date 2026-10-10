using TypeScript.Foundation;
using Xunit;

namespace TypeScript.Scanner.ParserTests;

public sealed class AstTextValueTests
{
    public static IEnumerable<object[]> Cases() => AstTextValueChecks.Cases.Select(name => new object[] { name });

    [Theory]
    [MemberData(nameof(Cases))]
    public void PreservesBorrowedTextAndPublicLifecycle(string name) => AstTextValueChecks.Run(name);
}
