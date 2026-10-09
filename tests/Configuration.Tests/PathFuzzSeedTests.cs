using Configuration.Checks;
using Xunit;

namespace Configuration.Tests;

public sealed class PathFuzzSeedTests
{
    public static IEnumerable<object[]> Cases => PathFuzzSeedChecks.Cases.Select(name => new object[] { name });

    [Theory]
    [MemberData(nameof(Cases))]
    public void SelectedOriginalSeedAssertions(string name) => PathFuzzSeedChecks.Run(name);
}
