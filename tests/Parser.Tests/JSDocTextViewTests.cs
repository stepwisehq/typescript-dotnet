using TypeScript.Foundation;
using Xunit;

namespace TypeScript.Scanner.ParserTests;

public sealed class JSDocTextViewTests
{
    public static IEnumerable<object[]> Cases() => JSDocTextViewChecks.Cases.Select(name => new object[] { name });

    [Theory]
    [MemberData(nameof(Cases))]
    public void PreservesByteViewsAndJSDocRestoration(string name) => JSDocTextViewChecks.Run(name);
}
