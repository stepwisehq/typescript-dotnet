using LanguageServices.Checks;
using Xunit;

namespace LanguageServices.Tests;
public sealed class ProjectCheckerPoolTests
{
    public static IEnumerable<object[]> Cases => ProjectCheckerPoolChecks.Names.Select(name => new object[] { name });

    [Theory]
    [MemberData(nameof(Cases))]
    public void Original(string name) => ProjectCheckerPoolChecks.Run(name);
    [Fact]
    public void AuthoredLongIdleTimeout() => Assert.Equal(1, ProjectCheckerPoolChecks.RunAuthoredControls());
}
