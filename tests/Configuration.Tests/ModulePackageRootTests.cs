using Configuration.Checks;
using Xunit;

namespace Configuration.Tests;

public sealed class ModulePackageRootTests
{
    public static IEnumerable<object[]> Cases => ModulePackageRootChecks.Cases.Select(name => new object[] { name });

    [Theory]
    [MemberData(nameof(Cases))]
    public void OriginalPackageRootAssertions(string name) => ModulePackageRootChecks.Run(name);
}
