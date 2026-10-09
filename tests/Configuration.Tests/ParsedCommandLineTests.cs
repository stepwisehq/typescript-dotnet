using Configuration.Checks;
using Xunit;

namespace Configuration.Tests;
public sealed class ParsedCommandLineTests
{
    public static IEnumerable<object[]> Cases => ParsedCommandLineChecks.Cases.Select(name => new object[] { name });

    [Theory]
    [MemberData(nameof(Cases))]
    public void OriginalAssertions(string name) => ParsedCommandLineChecks.Run(name);
}
