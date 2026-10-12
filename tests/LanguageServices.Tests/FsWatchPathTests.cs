using LanguageServices.Checks;
using Xunit;

namespace LanguageServices.Tests;
public sealed class FsWatchPathTests
{
    public static IEnumerable<object[]> Cases => FsWatchPathChecks.Names.Select(name => new object[] { name });

    [Theory]
    [MemberData(nameof(Cases))]
    public void Original(string name) => FsWatchPathChecks.Run(name);
}
