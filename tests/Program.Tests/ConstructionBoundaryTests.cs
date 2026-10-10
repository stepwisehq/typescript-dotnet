using Program.Checks;
using Xunit;

namespace Program.Tests;

public sealed class ConstructionBoundaryTests
{
    public static IEnumerable<object[]> Cases => ConstructionBoundaryChecks.Cases.Select(name => new object[] { name });

    [Theory]
    [MemberData(nameof(Cases))]
    public void AuthoredBehavior(string name) => ConstructionBoundaryChecks.Run(name);
}
