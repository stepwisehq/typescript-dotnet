using Configuration.Checks;
using Xunit;

namespace Configuration.Tests;
public sealed class ParsedOptionTests
{
    public static IEnumerable<object[]> Cases => ParsedOptionChecks.Cases.Select(name => new object[] { name });

    [Theory, MemberData(nameof(Cases))]
    public void OriginalAssertions(string name) => ParsedOptionChecks.Run(name);
}
