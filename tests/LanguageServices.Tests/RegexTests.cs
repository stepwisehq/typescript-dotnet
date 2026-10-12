using LanguageServices.Checks;
using Xunit;

namespace LanguageServices.Tests;
public sealed class RegexTests
{
    public static IEnumerable<object[]> OriginalNames() => RegexChecks.Names.Select(name => new object[] { name });
    [Theory]
    [MemberData(nameof(OriginalNames))]
    public void OriginalSelectedAssertions(string name) => RegexChecks.Run(name);
}
