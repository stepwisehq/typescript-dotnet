using LanguageServices.Checks;
using Xunit;

namespace LanguageServices.Tests;
public sealed class ProjectOverlayTests
{
    public static IEnumerable<object[]> Cases => ProjectOverlayChecks.Names.Select(name => new object[] { name });

    [Theory]
    [MemberData(nameof(Cases))]
    public void Original(string name) => ProjectOverlayChecks.Run(name);
}
