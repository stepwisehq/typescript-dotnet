using LanguageServices.Checks;
using Xunit;

namespace LanguageServices.Tests;
public sealed class FsWatchInternalTests
{
    public static IEnumerable<object[]> Cases => FsWatchInternalChecks.Names.Select(name => new object[] { name });

    [Theory]
    [MemberData(nameof(Cases))]
    public void Original(string name) => FsWatchInternalChecks.Run(name);
}
