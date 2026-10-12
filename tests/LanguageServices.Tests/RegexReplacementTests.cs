using LanguageServices.Checks;
using Xunit;

namespace LanguageServices.Tests;
public sealed class RegexReplacementTests
{
    public static IEnumerable<object[]> Names() => RegexReplacementChecks.Names.Select(name => new object[] { name });
    [Theory]
    [MemberData(nameof(Names))]
    public void OriginalSelectedReplacementAssertions(string name) => RegexReplacementChecks.Run(name);
    [Fact]
    public void OriginalCanBackquote() => CanBackquoteChecks.RunAll();
}
