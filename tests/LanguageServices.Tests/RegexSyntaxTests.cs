using LanguageServices.Checks;
using Xunit;

namespace LanguageServices.Tests;
public sealed class RegexSyntaxTests
{
    [Fact]
    public void OriginalCompilerProgramAssertions() => Assert.Equal(1, RegexCompileChecks.RunAll());
    public static IEnumerable<object[]> OriginalNames() => RegexSyntaxChecks.Names.Select(name => new object[] { name });
    [Theory]
    [MemberData(nameof(OriginalNames))]
    public void OriginalParserAssertions(string name) => RegexSyntaxChecks.Run(name);
}
