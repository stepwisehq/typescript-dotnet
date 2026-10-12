using LanguageServices.Checks;
using Xunit;
namespace LanguageServices.Tests;
public sealed class RegexRe2Tests
{
    public static IEnumerable<object[]> OriginalNames() => RegexRe2Checks.Names.Select(name => new object[] { name });
    [Theory]
    [MemberData(nameof(OriginalNames))]
    public void OriginalMatchStringChannels(string name) => RegexRe2Checks.Run(name);
}
