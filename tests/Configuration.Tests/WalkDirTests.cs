using Configuration.Checks;
using Xunit;

namespace Configuration.Tests;
public sealed class WalkDirTests
{
    public static IEnumerable<object[]> Cases => WalkDirChecks.Cases.Select(name => new object[] { name });

    [Theory, MemberData(nameof(Cases))]
    public void OriginalAssertions(string name) => WalkDirChecks.Run(name);
}
