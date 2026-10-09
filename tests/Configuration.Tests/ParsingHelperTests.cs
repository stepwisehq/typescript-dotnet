using Configuration.Checks;
using Xunit;

namespace Configuration.Tests;

public sealed class ParsingHelperTests
{
    public static IEnumerable<object[]> Cases => ParsingHelperChecks.Cases.Select(name => new object[] { name });

    [Theory]
    [MemberData(nameof(Cases))]
    public void OriginalAssertions(string name) => ParsingHelperChecks.Run(name);
}
