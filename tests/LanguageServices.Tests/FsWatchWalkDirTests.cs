using LanguageServices.Checks;
using Xunit;

namespace LanguageServices.Tests;
public sealed class FsWatchWalkDirTests
{
    public static IEnumerable<object[]> Cases => FsWatchWalkDirChecks.Names.Select(name => new object[] { name });

    [Theory]
    [MemberData(nameof(Cases))]
    public void Original(string name) => FsWatchWalkDirChecks.Run(name);
}
