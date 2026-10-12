using LanguageServices.Checks;
using Xunit;

namespace LanguageServices.Tests;
public sealed class LspWatcherTests
{
    public static IEnumerable<object[]> Cases => LspWatcherChecks.Names.Select(name => new object[] { name });

    [Theory]
    [MemberData(nameof(Cases))]
    public void Original(string name) => LspWatcherChecks.Run(name);
}
