using TypeScript.Foundation;
using Xunit;

namespace TypeScript.Scanner.ParserTests;

public sealed class JSDocFragmentTests
{
    public static IEnumerable<object[]> Cases() => JSDocFragmentChecks.Cases.Select(name => new object[] { name });

    [Theory]
    [MemberData(nameof(Cases))]
    public void PreservesSharedFragmentCompatibility(string name) => JSDocFragmentChecks.Run(name);
}
