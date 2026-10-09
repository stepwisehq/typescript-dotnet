using Configuration.Checks;
using Xunit;

namespace Configuration.Tests;
public sealed class OrderedMapTests
{
    public static IEnumerable<object[]> Cases => OrderedMapChecks.Cases.Select(name => new object[] { name });

    [Theory]
    [MemberData(nameof(Cases))]
    public void SelectedDependencyAssertions(string name) => OrderedMapChecks.Run(name);
}
