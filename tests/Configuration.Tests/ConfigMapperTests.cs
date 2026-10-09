using Configuration.Checks;
using Xunit;

namespace Configuration.Tests;
public sealed class ConfigMapperTests
{
    public static IEnumerable<object[]> Cases => ConfigMapperChecks.Cases.Select(name => new object[] { name });

    [Theory]
    [MemberData(nameof(Cases))]
    public void OriginalAssertions(string name) => ConfigMapperChecks.Run(name);
}
