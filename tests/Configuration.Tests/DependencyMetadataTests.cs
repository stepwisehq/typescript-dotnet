using Configuration.Checks;
using Xunit;

namespace Configuration.Tests;
public sealed class DependencyMetadataTests
{
    public static IEnumerable<object[]> Cases => DependencyMetadataChecks.Cases.Select(name => new object[] { name });

    [Theory]
    [MemberData(nameof(Cases))]
    public void SelectedDependencyAssertions(string name) => DependencyMetadataChecks.Run(name);
}
