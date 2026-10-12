using LanguageServices.Checks;
using Xunit;

namespace LanguageServices.Tests;
public sealed class ProjectRemainingTests
{
    public static IEnumerable<object[]> Cases => ProjectRemainingChecks.Names.Select(static name => new object[] { name });

    [Theory]
    [MemberData(nameof(Cases))]
    public void Original(string name) => ProjectRemainingChecks.Run(name);
}
