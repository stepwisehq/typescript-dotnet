using Configuration.Checks;
using Xunit;

namespace Configuration.Tests;

public sealed class CommandLineTests
{
    public static IEnumerable<object[]> Cases => CommandLineChecks.Cases.Select(name => new object[] { name });

    [Theory]
    [MemberData(nameof(Cases))]
    public void OriginalAssertions(string name) => CommandLineChecks.Run(name);
}
