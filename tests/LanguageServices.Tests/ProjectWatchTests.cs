using LanguageServices.Checks;
using Xunit;

namespace LanguageServices.Tests;
public sealed class ProjectWatchTests
{
    public static IEnumerable<object[]> Cases => ProjectWatchChecks.Names.Select(name => new object[] { name });

    [Theory]
    [MemberData(nameof(Cases))]
    public void Original(string name) => ProjectWatchChecks.Run(name);
}
