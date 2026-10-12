using LanguageServices.Checks;
using Xunit;

namespace LanguageServices.Tests;
public sealed class FsWatchFallbackTests
{
    public static IEnumerable<object[]> Cases => FsWatchFallbackChecks.Names.Select(name => new object[] { name });

    [Theory]
    [MemberData(nameof(Cases))]
    public void Original(string name) => FsWatchFallbackChecks.Run(name);
}
