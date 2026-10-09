using Configuration.Checks;
using Xunit;

namespace Configuration.Tests;
public sealed class IntegrationTests
{
    public static IEnumerable<object[]> Cases => IntegrationChecks.Cases.Select(name => new object[] { name });

    [Theory]
    [MemberData(nameof(Cases))]
    public void AuthoredIntegrationAssertions(string name) => IntegrationChecks.Run(name);
}
