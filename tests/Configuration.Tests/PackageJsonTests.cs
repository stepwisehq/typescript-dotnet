using Configuration.Checks;
using Xunit;

namespace Configuration.Tests;

public sealed class PackageJsonTests
{
    public static IEnumerable<object[]> Cases => PackageJsonChecks.Cases.Select(name => new object[] { name });

    [Theory]
    [MemberData(nameof(Cases))]
    public void OriginalAssertions(string name) => PackageJsonChecks.Run(name);
}
