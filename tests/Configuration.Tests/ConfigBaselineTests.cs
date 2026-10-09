using Configuration.Checks;
using Xunit;

namespace Configuration.Tests;
public sealed class ConfigBaselineTests
{
    public static IEnumerable<object[]> Cases => ConfigBaselineChecks.Cases.Select(name => new object[] { name });

    [Theory]
    [MemberData(nameof(Cases))]
    public void OriginalAssertions(string name) => ConfigBaselineChecks.Run(name);
}
