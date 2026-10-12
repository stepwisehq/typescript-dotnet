using LanguageServices.Checks;
using Xunit;

namespace LanguageServices.Tests;
public sealed class FsWatchEventTests
{
    public static IEnumerable<object[]> Cases => FsWatchEventChecks.Names.Select(name => new object[] { name });

    [Theory]
    [MemberData(nameof(Cases))]
    public void Original(string name) => FsWatchEventChecks.Run(name);
}
