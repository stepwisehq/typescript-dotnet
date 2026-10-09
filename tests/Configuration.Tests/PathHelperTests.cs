using Configuration.Checks;
using Xunit;

namespace Configuration.Tests;

public sealed class PathHelperTests
{
    public static IEnumerable<object[]> Cases => PathHelperChecks.Cases.Select(name => new object[] { name });

    [Theory]
    [MemberData(nameof(Cases))]
    public void OriginalSelectedPathAssertions(string name) => PathHelperChecks.Run(name);
}
